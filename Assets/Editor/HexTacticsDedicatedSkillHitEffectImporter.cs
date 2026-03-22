using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexTacticsDedicatedSkillHitEffectImporter
{
    private static readonly SkillHitVariantRecipe[] SkillHitVariantRecipes =
    {
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P2_R0.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightRose.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BasicAttack_Melee_P2_R0_Hit.prefab",
            impactScale: 0.82f,
            impactHeightNormalized: 0.48f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P3_R0.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightVerdant.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BasicAttack_Melee_P3_R0_Hit.prefab",
            impactScale: 0.9f,
            impactHeightNormalized: 0.52f,
            impactForwardOffset: 0.05f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P4_R0.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactMediumAzure.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BasicAttack_Melee_P4_R0_Hit.prefab",
            impactScale: 0.98f,
            impactHeightNormalized: 0.56f,
            impactForwardOffset: 0.06f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P5_R0.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyEmber.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BasicAttack_Melee_P5_R0_Hit.prefab",
            impactScale: 1.08f,
            impactHeightNormalized: 0.6f,
            impactForwardOffset: 0.08f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P6_R0.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BasicAttack_Melee_P6_R0_Hit.prefab",
            impactScale: 1.16f,
            impactHeightNormalized: 0.62f,
            impactForwardOffset: 0.09f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Ranged_P3_R1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedMist.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BasicAttack_Ranged_P3_R1_Hit.prefab",
            impactScale: 1f,
            impactHeightNormalized: 0.58f,
            impactForwardOffset: 0.06f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Ranged_P4_R1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BasicAttack_Ranged_P4_R1_Hit.prefab",
            impactScale: 1.08f,
            impactHeightNormalized: 0.62f,
            impactForwardOffset: 0.08f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/ScoutBurst_Melee_P4_R0_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit01.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/ScoutBurst_Melee_P4_R0_C1_Hit.prefab",
            impactScale: 0.92f,
            impactHeightNormalized: 0.5f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/VenomStrike_Melee_P4_R0_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightVerdant.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/VenomStrike_Melee_P4_R0_C1_Hit.prefab",
            impactScale: 1.02f,
            impactHeightNormalized: 0.54f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BulwarkBash_Melee_P5_R0_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit14.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BulwarkBash_Melee_P5_R0_C1_Hit.prefab",
            impactScale: 1.06f,
            impactHeightNormalized: 0.44f,
            impactForwardOffset: 0.03f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/SkirmishCombo_Melee_P5_R0_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit06.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/SkirmishCombo_Melee_P5_R0_C1_Hit.prefab",
            impactScale: 1.02f,
            impactHeightNormalized: 0.54f,
            impactForwardOffset: 0.05f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/PredatorRush_Melee_P6_R0_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit20.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/PredatorRush_Melee_P6_R0_C1_Hit.prefab",
            impactScale: 1.12f,
            impactHeightNormalized: 0.56f,
            impactForwardOffset: 0.06f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BreakArmor_Melee_P6_R0_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit13.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BreakArmor_Melee_P6_R0_C2_Hit.prefab",
            impactScale: 1.14f,
            impactHeightNormalized: 0.48f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/CrushingBlow_Melee_P7_R0_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit24.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/CrushingBlow_Melee_P7_R0_C2_Hit.prefab",
            impactScale: 1.2f,
            impactHeightNormalized: 0.54f,
            impactForwardOffset: 0.06f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/ExecutionDive_Melee_P7_R0_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit07.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/ExecutionDive_Melee_P7_R0_C2_Hit.prefab",
            impactScale: 1.18f,
            impactHeightNormalized: 0.58f,
            impactForwardOffset: 0.06f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/RoyalExecution_Melee_P8_R0_C3.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/RoyalExecution_Melee_P8_R0_C3_Hit.prefab",
            impactScale: 1.34f,
            impactHeightNormalized: 0.64f,
            impactForwardOffset: 0.08f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/PowerShot_Ranged_P5_R1_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit11.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/PowerShot_Ranged_P5_R1_C1_Hit.prefab",
            impactScale: 1.06f,
            impactHeightNormalized: 0.54f,
            impactForwardOffset: 0.05f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BackstepShot_Ranged_P4_R1_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedMist.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/BackstepShot_Ranged_P4_R1_C1_Hit.prefab",
            impactScale: 0.94f,
            impactHeightNormalized: 0.56f,
            impactForwardOffset: 0.05f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/RepulseBolt_Ranged_P5_R1_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit17.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/RepulseBolt_Ranged_P5_R1_C2_Hit.prefab",
            impactScale: 1.08f,
            impactHeightNormalized: 0.5f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/ArcBurst_Ranged_P6_R1_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit22.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/ArcBurst_Ranged_P6_R1_C2_Hit.prefab",
            impactScale: 1.16f,
            impactHeightNormalized: 0.6f,
            impactForwardOffset: 0.05f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/WaveSlash_Ranged_P6_R1_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit15.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/WaveSlash_Ranged_P6_R1_C2_Hit.prefab",
            impactScale: 1.12f,
            impactHeightNormalized: 0.46f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/ShadowBolt_Ranged_P5_R2_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit18.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/ShadowBolt_Ranged_P5_R2_C2_Hit.prefab",
            impactScale: 1.18f,
            impactHeightNormalized: 0.6f,
            impactForwardOffset: 0.05f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/Snipe_Ranged_P5_R2_C2.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit19.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/Snipe_Ranged_P5_R2_C2_Hit.prefab",
            impactScale: 1.02f,
            impactHeightNormalized: 0.58f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/ThornShot_Ranged_P4_R1_C1.asset",
            sourceImpactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit08.prefab",
            targetImpactPrefabPath: HexTacticsAssetPaths.HitEffectsSkillVariantFolder + "/ThornShot_Ranged_P4_R1_C1_Hit.prefab",
            impactScale: 0.98f,
            impactHeightNormalized: 0.54f,
            impactForwardOffset: 0.05f)
    };

    public static void RunBatchMode()
    {
        var exitCode = ImportInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/Hex Tactics/Refresh Dedicated Skill Hit Effects")]
    public static void Import()
    {
        ImportInternal();
    }

    [MenuItem("Tools/Hex Tactics/Validate Dedicated Skill Hit Effects")]
    public static void Validate()
    {
        ValidateInternal(logSuccess: true);
    }

    private static bool ImportInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureTargetFoldersExist();

        foreach (var recipe in SkillHitVariantRecipes)
        {
            if (!GenerateSkillHitVariant(recipe) || !ApplySkillRecipe(recipe))
            {
                return false;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressablesSync.Sync();
        return ValidateInternal(logSuccess: true);
    }

    private static bool ValidateInternal(bool logSuccess)
    {
        foreach (var recipe in SkillHitVariantRecipes)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(recipe.TargetImpactPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[HexTacticsDedicatedSkillHitEffectImporter] Missing generated prefab: " + recipe.TargetImpactPrefabPath);
                return false;
            }

            var skill = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(recipe.SkillAssetPath);
            if (skill == null)
            {
                Debug.LogError("[HexTacticsDedicatedSkillHitEffectImporter] Missing skill config: " + recipe.SkillAssetPath);
                return false;
            }

            var serializedObject = new SerializedObject(skill);
            if (!MatchesObjectReference(serializedObject.FindProperty("impactEffectPrefab"), recipe.TargetImpactPrefabPath))
            {
                Debug.LogError("[HexTacticsDedicatedSkillHitEffectImporter] Skill config impact prefab was not updated correctly: " + recipe.SkillAssetPath);
                return false;
            }
        }

        if (logSuccess)
        {
            Debug.Log("[HexTacticsDedicatedSkillHitEffectImporter] Dedicated skill hit effects refreshed successfully.");
        }

        return true;
    }

    private static bool GenerateSkillHitVariant(SkillHitVariantRecipe recipe)
    {
        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(recipe.SourceImpactPrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError("[HexTacticsDedicatedSkillHitEffectImporter] Missing source impact prefab: " + recipe.SourceImpactPrefabPath);
            return false;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(recipe.SourceImpactPrefabPath);
        try
        {
            prefabRoot.name = recipe.TargetPrefabName;
            NormalizeRootTransform(prefabRoot);
            ResetTagsAndLayers(prefabRoot);
            EnsureTransientEffect(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, recipe.TargetImpactPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return true;
    }

    private static bool ApplySkillRecipe(SkillHitVariantRecipe recipe)
    {
        var skill = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(recipe.SkillAssetPath);
        if (skill == null)
        {
            Debug.LogError("[HexTacticsDedicatedSkillHitEffectImporter] Could not load skill config: " + recipe.SkillAssetPath);
            return false;
        }

        var serializedObject = new SerializedObject(skill);
        SetObjectReference(serializedObject.FindProperty("impactEffectPrefab"), recipe.TargetImpactPrefabPath);
        serializedObject.FindProperty("impactEffectScale").floatValue = recipe.ImpactScale;
        serializedObject.FindProperty("impactHeightNormalized").floatValue = recipe.ImpactHeightNormalized;
        serializedObject.FindProperty("impactForwardOffset").floatValue = recipe.ImpactForwardOffset;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
        return true;
    }

    private static void EnsureTargetFoldersExist()
    {
        EnsureFolder(HexTacticsAssetPaths.AddressablesRoot, "Effects");
        EnsureFolder(HexTacticsAssetPaths.EffectsFolder, "HitEffects");
        EnsureFolder(HexTacticsAssetPaths.HitEffectsFolder, "SkillVariants");
    }

    private static void EnsureFolder(string parentPath, string childFolderName)
    {
        var targetPath = parentPath + "/" + childFolderName;
        if (!AssetDatabase.IsValidFolder(targetPath))
        {
            AssetDatabase.CreateFolder(parentPath, childFolderName);
        }
    }

    private static void NormalizeRootTransform(GameObject prefabRoot)
    {
        var transform = prefabRoot.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private static void ResetTagsAndLayers(GameObject prefabRoot)
    {
        foreach (var transform in prefabRoot.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.tag = "Untagged";
            transform.gameObject.layer = 0;
        }
    }

    private static void EnsureTransientEffect(GameObject prefabRoot)
    {
        if (prefabRoot.GetComponent<HexTacticsTransientEffect>() == null)
        {
            prefabRoot.AddComponent<HexTacticsTransientEffect>();
        }
    }

    private static bool MatchesObjectReference(SerializedProperty property, string assetPath)
    {
        var expectedAsset = LoadPrefabAsset(assetPath);
        return property != null && property.objectReferenceValue == expectedAsset;
    }

    private static void SetObjectReference(SerializedProperty property, string assetPath)
    {
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = LoadPrefabAsset(assetPath);
    }

    private static GameObject LoadPrefabAsset(string assetPath)
    {
        return string.IsNullOrWhiteSpace(assetPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }

    private readonly struct SkillHitVariantRecipe
    {
        public SkillHitVariantRecipe(
            string skillAssetPath,
            string sourceImpactPrefabPath,
            string targetImpactPrefabPath,
            float impactScale,
            float impactHeightNormalized,
            float impactForwardOffset)
        {
            SkillAssetPath = skillAssetPath;
            SourceImpactPrefabPath = sourceImpactPrefabPath;
            TargetImpactPrefabPath = targetImpactPrefabPath;
            ImpactScale = impactScale;
            ImpactHeightNormalized = impactHeightNormalized;
            ImpactForwardOffset = impactForwardOffset;
        }

        public string SkillAssetPath { get; }
        public string SourceImpactPrefabPath { get; }
        public string TargetImpactPrefabPath { get; }
        public float ImpactScale { get; }
        public float ImpactHeightNormalized { get; }
        public float ImpactForwardOffset { get; }
        public string TargetPrefabName => Path.GetFileNameWithoutExtension(TargetImpactPrefabPath);
    }
}
