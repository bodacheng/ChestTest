using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HexTacticsSkillConfigMigration
{
    public static void RunBatchMode()
    {
        var exitCode = RunInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/Hex Tactics/Migrate Character Skills")]
    public static void RunFromMenu()
    {
        RunInternal();
    }

    private static bool RunInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var defaultProjectile = AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.RangedWaveEffectAssetPath);
        var lightImpact = AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.DefaultRangedHitLightAssetPath);
        var mediumImpact = AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.DefaultRangedHitMediumAssetPath);
        var heavyImpact = AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.DefaultRangedHitHeavyAssetPath);

        var updatedConfigCount = 0;
        var createdSkillCount = 0;
        var updatedSkillCount = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsCharacterConfig", new[] { HexTacticsAssetPaths.CharacterConfigFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(path);
            if (config == null)
            {
                continue;
            }

            var sharedSkill = HexTacticsSharedSkillUtility.GetOrCreateSharedBasicSkill(
                config.LegacyAttackPowerForMigration,
                config.LegacyAttackRangeForMigration,
                defaultProjectile,
                lightImpact,
                mediumImpact,
                heavyImpact,
                ref createdSkillCount,
                ref updatedSkillCount);
            var configUpdated = EnsureCharacterUsesSkillAssets(config, sharedSkill, defaultProjectile, lightImpact, mediumImpact, heavyImpact, ref updatedSkillCount);
            if (configUpdated)
            {
                updatedConfigCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressablesSync.Sync();

        Debug.Log($"[HexTacticsSkillConfigMigration] Updated {updatedConfigCount} character config(s), created {createdSkillCount} skill asset(s), touched {updatedSkillCount} skill asset(s).");
        return true;
    }

    private static bool EnsureCharacterUsesSkillAssets(
        HexTacticsCharacterConfig config,
        HexTacticsSkillConfig sharedSkill,
        GameObject defaultProjectile,
        GameObject lightImpact,
        GameObject mediumImpact,
        GameObject heavyImpact,
        ref int updatedSkillCount)
    {
        var serializedConfig = new SerializedObject(config);
        var updated = false;

        var maxEnergyProperty = serializedConfig.FindProperty("maxEnergy");
        if (maxEnergyProperty.intValue < 1)
        {
            maxEnergyProperty.intValue = 3;
            updated = true;
        }

        var startingEnergyProperty = serializedConfig.FindProperty("startingEnergy");
        var clampedStartingEnergy = Mathf.Clamp(startingEnergyProperty.intValue, 0, Mathf.Max(1, maxEnergyProperty.intValue));
        if (startingEnergyProperty.intValue != clampedStartingEnergy)
        {
            startingEnergyProperty.intValue = clampedStartingEnergy;
            updated = true;
        }

        var skillsProperty = serializedConfig.FindProperty("skills");
        if (CountAssignedSkills(skillsProperty) == 0 && sharedSkill != null)
        {
            skillsProperty.arraySize = 1;
            skillsProperty.GetArrayElementAtIndex(0).objectReferenceValue = sharedSkill;
            updated = true;
        }
        else if (skillsProperty.arraySize == 1 && sharedSkill != null)
        {
            var assignedSkill = skillsProperty.GetArrayElementAtIndex(0).objectReferenceValue as HexTacticsSkillConfig;
            if (HexTacticsSharedSkillUtility.IsEquivalentBasicSkill(
                    assignedSkill,
                    config.LegacyAttackPowerForMigration,
                    config.LegacyAttackRangeForMigration) &&
                assignedSkill != sharedSkill)
            {
                skillsProperty.GetArrayElementAtIndex(0).objectReferenceValue = sharedSkill;
                updated = true;
            }
        }

        for (var i = 0; i < skillsProperty.arraySize; i++)
        {
            var skill = skillsProperty.GetArrayElementAtIndex(i).objectReferenceValue as HexTacticsSkillConfig;
            if (skill == null)
            {
                continue;
            }

            if (HexTacticsSharedSkillUtility.EnsureSkillHasDefaultRangedEffects(skill, defaultProjectile, lightImpact, mediumImpact, heavyImpact))
            {
                updatedSkillCount++;
            }
        }

        if (updated)
        {
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        return updated;
    }

    private static int CountAssignedSkills(SerializedProperty skillsProperty)
    {
        if (skillsProperty == null)
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i < skillsProperty.arraySize; i++)
        {
            if (skillsProperty.GetArrayElementAtIndex(i).objectReferenceValue != null)
            {
                count++;
            }
        }

        return count;
    }
}
