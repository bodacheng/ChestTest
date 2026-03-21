using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HexTacticsHovlSkillEffectImporter
{
    private const string HovlOrbFolder = "Assets/Hovl Studio/Glowing orbs Vol 3/Prefabs";

    private static readonly EffectRecipe[] EffectRecipes =
    {
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 21.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightRose.prefab",
            loopParticles: false,
            fallbackLifetime: 1.15f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 24.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightVerdant.prefab",
            loopParticles: false,
            fallbackLifetime: 1.15f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 27.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactMediumAzure.prefab",
            loopParticles: false,
            fallbackLifetime: 1.18f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 38.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyEmber.prefab",
            loopParticles: false,
            fallbackLifetime: 1.25f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 39.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            loopParticles: false,
            fallbackLifetime: 1.3f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 34.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedMist.prefab",
            loopParticles: false,
            fallbackLifetime: 1.2f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 33.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            loopParticles: false,
            fallbackLifetime: 1.24f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 40.prefab",
            targetPrefabPath: HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileFrost.prefab",
            loopParticles: true,
            fallbackLifetime: 1.1f),
        new(
            sourcePrefabPath: HovlOrbFolder + "/Glowing orb 37.prefab",
            targetPrefabPath: HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileArcane.prefab",
            loopParticles: true,
            fallbackLifetime: 1.16f)
    };

    private static readonly SkillRecipe[] SkillRecipes =
    {
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P2_R0.asset",
            impactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightRose.prefab",
            impactScale: 0.82f,
            impactHeightNormalized: 0.48f,
            impactForwardOffset: 0.04f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P3_R0.asset",
            impactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightVerdant.prefab",
            impactScale: 0.9f,
            impactHeightNormalized: 0.52f,
            impactForwardOffset: 0.05f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P4_R0.asset",
            impactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactMediumAzure.prefab",
            impactScale: 0.98f,
            impactHeightNormalized: 0.56f,
            impactForwardOffset: 0.06f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P5_R0.asset",
            impactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyEmber.prefab",
            impactScale: 1.08f,
            impactHeightNormalized: 0.6f,
            impactForwardOffset: 0.08f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Melee_P6_R0.asset",
            impactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            impactScale: 1.16f,
            impactHeightNormalized: 0.62f,
            impactForwardOffset: 0.09f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Ranged_P3_R1.asset",
            projectilePrefabPath: HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileFrost.prefab",
            projectileScale: 0.88f,
            impactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedMist.prefab",
            impactScale: 1f,
            impactHeightNormalized: 0.58f,
            impactForwardOffset: 0.06f),
        new(
            skillAssetPath: HexTacticsAssetPaths.SkillConfigFolder + "/BasicAttack_Ranged_P4_R1.asset",
            projectilePrefabPath: HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileArcane.prefab",
            projectileScale: 0.96f,
            impactPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            impactScale: 1.08f,
            impactHeightNormalized: 0.62f,
            impactForwardOffset: 0.08f)
    };

    public static void RunBatchMode()
    {
        var exitCode = ImportInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/Hex Tactics/Import Hovl Skill Effects")]
    public static void Import()
    {
        ImportInternal();
    }

    [MenuItem("Tools/Hex Tactics/Validate Hovl Skill Effects")]
    public static void Validate()
    {
        ValidateInternal(logSuccess: true);
    }

    private static bool ImportInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureTargetFoldersExist();

        foreach (var recipe in EffectRecipes)
        {
            if (!GeneratePrefab(recipe))
            {
                return false;
            }
        }

        foreach (var recipe in SkillRecipes)
        {
            if (!ApplySkillRecipe(recipe))
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
        foreach (var recipe in EffectRecipes)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(recipe.TargetPrefabPath) == null)
            {
                Debug.LogError("[HexTacticsHovlSkillEffectImporter] Missing generated prefab: " + recipe.TargetPrefabPath);
                return false;
            }
        }

        foreach (var recipe in SkillRecipes)
        {
            var asset = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(recipe.SkillAssetPath);
            if (asset == null)
            {
                Debug.LogError("[HexTacticsHovlSkillEffectImporter] Missing skill config: " + recipe.SkillAssetPath);
                return false;
            }

            var serializedObject = new SerializedObject(asset);
            if (!MatchesObjectReference(serializedObject.FindProperty("projectileEffectPrefab"), recipe.ProjectilePrefabPath) ||
                !MatchesObjectReference(serializedObject.FindProperty("impactEffectPrefab"), recipe.ImpactPrefabPath))
            {
                Debug.LogError("[HexTacticsHovlSkillEffectImporter] Skill config was not updated correctly: " + recipe.SkillAssetPath);
                return false;
            }
        }

        if (logSuccess)
        {
            Debug.Log("[HexTacticsHovlSkillEffectImporter] Generated Hovl skill effects and updated current skill configs.");
        }

        return true;
    }

    private static bool GeneratePrefab(EffectRecipe recipe)
    {
        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(recipe.SourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError("[HexTacticsHovlSkillEffectImporter] Could not load source prefab: " + recipe.SourcePrefabPath);
            return false;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(recipe.SourcePrefabPath);
        try
        {
            prefabRoot.name = recipe.AssetName;
            NormalizeRootTransform(prefabRoot);
            ConfigureParticleSystems(prefabRoot, recipe.LoopParticles);
            DisableShadows(prefabRoot);
            EnsureTransientEffect(prefabRoot, recipe.FallbackLifetime);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, recipe.TargetPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return true;
    }

    private static bool ApplySkillRecipe(SkillRecipe recipe)
    {
        var skill = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(recipe.SkillAssetPath);
        if (skill == null)
        {
            Debug.LogError("[HexTacticsHovlSkillEffectImporter] Could not load skill config: " + recipe.SkillAssetPath);
            return false;
        }

        var serializedObject = new SerializedObject(skill);
        SetObjectReference(serializedObject.FindProperty("projectileEffectPrefab"), recipe.ProjectilePrefabPath);
        serializedObject.FindProperty("projectileEffectScale").floatValue = recipe.ProjectileScale;
        SetObjectReference(serializedObject.FindProperty("impactEffectPrefab"), recipe.ImpactPrefabPath);
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
        EnsureFolder(HexTacticsAssetPaths.EffectsFolder, "AttackEffects");
        EnsureFolder(HexTacticsAssetPaths.EffectsFolder, "HitEffects");
        EnsureFolder(HexTacticsAssetPaths.AttackEffectsFolder, "Variants");
        EnsureFolder(HexTacticsAssetPaths.HitEffectsFolder, "Variants");
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

    private static void ConfigureParticleSystems(GameObject prefabRoot, bool loopParticles)
    {
        foreach (var particleSystem in prefabRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            main.loop = loopParticles;
            main.prewarm = false;
            main.playOnAwake = true;
            if (!loopParticles)
            {
                main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }

    private static void DisableShadows(GameObject prefabRoot)
    {
        foreach (var renderer in prefabRoot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static void EnsureTransientEffect(GameObject prefabRoot, float fallbackLifetime)
    {
        var transientEffect = prefabRoot.GetComponent<HexTacticsTransientEffect>();
        if (transientEffect == null)
        {
            transientEffect = prefabRoot.AddComponent<HexTacticsTransientEffect>();
        }

        var serializedObject = new SerializedObject(transientEffect);
        serializedObject.FindProperty("fallbackLifetime").floatValue = fallbackLifetime;
        serializedObject.FindProperty("destroyDelayPadding").floatValue = 0.12f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

    private readonly struct EffectRecipe
    {
        public EffectRecipe(string sourcePrefabPath, string targetPrefabPath, bool loopParticles, float fallbackLifetime)
        {
            SourcePrefabPath = sourcePrefabPath;
            TargetPrefabPath = targetPrefabPath;
            LoopParticles = loopParticles;
            FallbackLifetime = fallbackLifetime;
        }

        public string SourcePrefabPath { get; }
        public string TargetPrefabPath { get; }
        public bool LoopParticles { get; }
        public float FallbackLifetime { get; }
        public string AssetName => System.IO.Path.GetFileNameWithoutExtension(TargetPrefabPath);
    }

    private readonly struct SkillRecipe
    {
        public SkillRecipe(
            string skillAssetPath,
            string projectilePrefabPath = null,
            float projectileScale = 1f,
            string impactPrefabPath = null,
            float impactScale = 1f,
            float impactHeightNormalized = 0.58f,
            float impactForwardOffset = 0.08f)
        {
            SkillAssetPath = skillAssetPath;
            ProjectilePrefabPath = projectilePrefabPath;
            ProjectileScale = projectileScale;
            ImpactPrefabPath = impactPrefabPath;
            ImpactScale = impactScale;
            ImpactHeightNormalized = impactHeightNormalized;
            ImpactForwardOffset = impactForwardOffset;
        }

        public string SkillAssetPath { get; }
        public string ProjectilePrefabPath { get; }
        public float ProjectileScale { get; }
        public string ImpactPrefabPath { get; }
        public float ImpactScale { get; }
        public float ImpactHeightNormalized { get; }
        public float ImpactForwardOffset { get; }
    }
}
