using UnityEditor;
using UnityEngine;

public static class HexTacticsSharedSkillUtility
{
    public static HexTacticsSkillConfig GetOrCreateSharedBasicSkill(
        int power,
        int attackRange,
        GameObject defaultProjectile,
        GameObject lightImpact,
        GameObject mediumImpact,
        GameObject heavyImpact,
        ref int createdSkillCount,
        ref int updatedSkillCount)
    {
        EnsureFolderPath(HexTacticsAssetPaths.SkillConfigFolder);

        var normalizedPower = Mathf.Max(1, power);
        var normalizedRange = Mathf.Max(0, attackRange);
        var assetPath = BuildSharedBasicSkillAssetPath(normalizedPower, normalizedRange);
        var skill = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(assetPath);
        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<HexTacticsSkillConfig>();
            AssetDatabase.CreateAsset(skill, assetPath);
            createdSkillCount++;
        }

        var serializedSkill = new SerializedObject(skill);
        serializedSkill.FindProperty("displayName").stringValue = normalizedRange > 0 ? "基础射击" : "基础攻击";
        serializedSkill.FindProperty("description").stringValue = normalizedRange > 0
            ? "命中后回复能量的基础远程技能"
            : "命中后回复能量的基础近战技能";
        serializedSkill.FindProperty("power").intValue = normalizedPower;
        serializedSkill.FindProperty("attackRange").intValue = normalizedRange;
        serializedSkill.FindProperty("energyCost").intValue = 0;
        serializedSkill.FindProperty("energyGainOnHit").intValue = 1;
        ApplyDefaultSkillEffects(
            serializedSkill,
            normalizedPower,
            normalizedRange,
            defaultProjectile,
            lightImpact,
            mediumImpact,
            heavyImpact,
            overwriteExisting: true);
        serializedSkill.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
        updatedSkillCount++;
        return skill;
    }

    public static bool EnsureSkillHasDefaultRangedEffects(
        HexTacticsSkillConfig skill,
        GameObject defaultProjectile,
        GameObject lightImpact,
        GameObject mediumImpact,
        GameObject heavyImpact)
    {
        if (skill == null || !skill.RequiresDedicatedRangedEffects)
        {
            return false;
        }

        var serializedSkill = new SerializedObject(skill);
        var changed = ApplyDefaultSkillEffects(
            serializedSkill,
            skill.Power,
            skill.AttackRange,
            defaultProjectile,
            lightImpact,
            mediumImpact,
            heavyImpact,
            overwriteExisting: false);
        if (!changed)
        {
            return false;
        }

        serializedSkill.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
        return true;
    }

    public static bool IsEquivalentBasicSkill(HexTacticsSkillConfig skill, int power, int attackRange)
    {
        if (skill == null)
        {
            return false;
        }

        return skill.Power == Mathf.Max(1, power) &&
            skill.AttackRange == Mathf.Max(0, attackRange) &&
            skill.EnergyCost == 0 &&
            skill.EnergyGainOnHit == 1;
    }

    public static bool IsSharedBasicSkillPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return false;
        }

        return assetPath.StartsWith(HexTacticsAssetPaths.SkillConfigFolder + "/", System.StringComparison.Ordinal) &&
            assetPath.IndexOf('/', HexTacticsAssetPaths.SkillConfigFolder.Length + 1) < 0;
    }

    public static string BuildSharedBasicSkillAssetPath(int power, int attackRange)
    {
        return $"{HexTacticsAssetPaths.SkillConfigFolder}/{BuildSharedBasicSkillAssetName(power, attackRange)}.asset";
    }

    private static string BuildSharedBasicSkillAssetName(int power, int attackRange)
    {
        var style = attackRange > 0 ? "Ranged" : "Melee";
        return $"BasicAttack_{style}_P{Mathf.Max(1, power)}_R{Mathf.Max(0, attackRange)}";
    }

    private static bool ApplyDefaultSkillEffects(
        SerializedObject serializedSkill,
        int power,
        int attackRange,
        GameObject defaultProjectile,
        GameObject lightImpact,
        GameObject mediumImpact,
        GameObject heavyImpact,
        bool overwriteExisting)
    {
        if (serializedSkill == null)
        {
            return false;
        }

        var changed = false;
        var isRanged = attackRange > 0;

        var projectileProperty = serializedSkill.FindProperty("projectileEffectPrefab");
        if (projectileProperty != null)
        {
            var nextProjectile = isRanged ? defaultProjectile : null;
            if ((overwriteExisting || projectileProperty.objectReferenceValue == null) && projectileProperty.objectReferenceValue != nextProjectile)
            {
                projectileProperty.objectReferenceValue = nextProjectile;
                changed = true;
            }
        }

        var projectileScaleProperty = serializedSkill.FindProperty("projectileEffectScale");
        if (projectileScaleProperty != null)
        {
            const float rangedProjectileScale = 1f;
            if ((overwriteExisting || Mathf.Approximately(projectileScaleProperty.floatValue, 0f)) &&
                !Mathf.Approximately(projectileScaleProperty.floatValue, rangedProjectileScale))
            {
                projectileScaleProperty.floatValue = rangedProjectileScale;
                changed = true;
            }
        }

        var impactProperty = serializedSkill.FindProperty("impactEffectPrefab");
        if (impactProperty != null)
        {
            var nextImpact = isRanged ? ResolveDefaultImpactEffect(power, lightImpact, mediumImpact, heavyImpact) : null;
            if ((overwriteExisting || impactProperty.objectReferenceValue == null) && impactProperty.objectReferenceValue != nextImpact)
            {
                impactProperty.objectReferenceValue = nextImpact;
                changed = true;
            }
        }

        var impactScaleProperty = serializedSkill.FindProperty("impactEffectScale");
        if (impactScaleProperty != null)
        {
            const float defaultImpactScale = 1f;
            if ((overwriteExisting || Mathf.Approximately(impactScaleProperty.floatValue, 0f)) &&
                !Mathf.Approximately(impactScaleProperty.floatValue, defaultImpactScale))
            {
                impactScaleProperty.floatValue = defaultImpactScale;
                changed = true;
            }
        }

        var impactHeightProperty = serializedSkill.FindProperty("impactHeightNormalized");
        if (impactHeightProperty != null)
        {
            const float defaultImpactHeight = 0.58f;
            if ((overwriteExisting || Mathf.Approximately(impactHeightProperty.floatValue, 0f)) &&
                !Mathf.Approximately(impactHeightProperty.floatValue, defaultImpactHeight))
            {
                impactHeightProperty.floatValue = defaultImpactHeight;
                changed = true;
            }
        }

        var impactForwardProperty = serializedSkill.FindProperty("impactForwardOffset");
        if (impactForwardProperty != null)
        {
            const float defaultForwardOffset = 0.08f;
            if ((overwriteExisting || Mathf.Approximately(impactForwardProperty.floatValue, 0f)) &&
                !Mathf.Approximately(impactForwardProperty.floatValue, defaultForwardOffset))
            {
                impactForwardProperty.floatValue = defaultForwardOffset;
                changed = true;
            }
        }

        return changed;
    }

    private static GameObject ResolveDefaultImpactEffect(int power, GameObject lightImpact, GameObject mediumImpact, GameObject heavyImpact)
    {
        if (power <= 2)
        {
            return lightImpact;
        }

        if (power <= 4)
        {
            return mediumImpact;
        }

        return heavyImpact;
    }

    private static void EnsureFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
