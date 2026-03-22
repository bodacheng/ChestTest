using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexTacticsGabrielSkillEffectImporter
{
    private const string GabrielPrefabsRoot = "Assets/GabrielAguiarProductions/UniqueMagicAbilitiesVol_1/Prefabs";
    private const string GabrielProjectileFolder = GabrielPrefabsRoot + "/Projectiles";
    private const string GabrielHitFolder = GabrielPrefabsRoot + "/Hits";
    private const string GabrielMagicFolder = GabrielPrefabsRoot + "/MagicAbilities";
    private static readonly TimingProfile ProjectileTimingProfile = new(
        startDelayMultiplier: 0.18f,
        maxStartDelay: 0.03f,
        durationMultiplier: 0.72f,
        maxDuration: 0.55f,
        lifetimeMultiplier: 0.74f,
        maxLifetime: 0.4f,
        simulationSpeedMultiplier: 1.18f,
        trailTimeMultiplier: 0.45f,
        maxTrailTime: 0.16f,
        destroyDelayPadding: 0.05f);
    private static readonly TimingProfile HitTimingProfile = new(
        startDelayMultiplier: 0.12f,
        maxStartDelay: 0.02f,
        durationMultiplier: 0.52f,
        maxDuration: 0.45f,
        lifetimeMultiplier: 0.58f,
        maxLifetime: 0.32f,
        simulationSpeedMultiplier: 1.32f,
        trailTimeMultiplier: 0.3f,
        maxTrailTime: 0.1f,
        destroyDelayPadding: 0.04f);

    // Keep the existing generated target paths so skill config asset references and addressable GUIDs stay stable.
    private static readonly EffectRecipe[] EffectRecipes =
    {
        new(
            sourcePrefabPath: GabrielHitFolder + "/vfx_Hit_Frag_Red.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightRose.prefab",
            loopParticles: false,
            fallbackLifetime: 0.72f,
            rootScale: 0.94f),
        new(
            sourcePrefabPath: GabrielMagicFolder + "/vfx_MagicAbility_Stripes_Green.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactLightVerdant.prefab",
            loopParticles: false,
            fallbackLifetime: 0.82f,
            rootScale: 0.66f),
        new(
            sourcePrefabPath: GabrielHitFolder + "/vfx_Hit_Comet_Blue.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactMediumAzure.prefab",
            loopParticles: false,
            fallbackLifetime: 0.86f,
            rootScale: 0.72f),
        new(
            sourcePrefabPath: GabrielMagicFolder + "/vfx_MagicAbility_Earthshatter_Fire.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyEmber.prefab",
            loopParticles: false,
            fallbackLifetime: 0.94f,
            rootScale: 0.74f),
        new(
            sourcePrefabPath: GabrielMagicFolder + "/vfx_MagicAbility_CircleExplosion_Red.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            loopParticles: false,
            fallbackLifetime: 1.02f,
            rootScale: 0.7f),
        new(
            sourcePrefabPath: GabrielMagicFolder + "/vfx_MagicAbility_Impact_Blue.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedMist.prefab",
            loopParticles: false,
            fallbackLifetime: 0.78f,
            rootScale: 0.92f),
        new(
            sourcePrefabPath: GabrielMagicFolder + "/vfx_MagicAbility_DarkEnergy_Purple.prefab",
            targetPrefabPath: HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            loopParticles: false,
            fallbackLifetime: 0.98f,
            rootScale: 0.62f),
        new(
            sourcePrefabPath: GabrielProjectileFolder + "/vfx_Projectile_Comet_Blue.prefab",
            targetPrefabPath: HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileFrost.prefab",
            loopParticles: true,
            fallbackLifetime: 0.9f,
            rootScale: 0.82f),
        new(
            sourcePrefabPath: GabrielProjectileFolder + "/vfx_Projectile_Comet.prefab",
            targetPrefabPath: HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileArcane.prefab",
            loopParticles: true,
            fallbackLifetime: 0.94f,
            rootScale: 0.84f)
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

    [MenuItem("Tools/Hex Tactics/Import Gabriel Skill Effects")]
    public static void Import()
    {
        ImportInternal();
    }

    [MenuItem("Tools/Hex Tactics/Validate Gabriel Skill Effects")]
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
                Debug.LogError("[HexTacticsGabrielSkillEffectImporter] Missing generated prefab: " + recipe.TargetPrefabPath);
                return false;
            }
        }

        foreach (var recipe in SkillRecipes)
        {
            var asset = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(recipe.SkillAssetPath);
            if (asset == null)
            {
                Debug.LogError("[HexTacticsGabrielSkillEffectImporter] Missing skill config: " + recipe.SkillAssetPath);
                return false;
            }

            var serializedObject = new SerializedObject(asset);
            if (!MatchesObjectReference(serializedObject.FindProperty("projectileEffectPrefab"), recipe.ProjectilePrefabPath) ||
                !MatchesObjectReference(serializedObject.FindProperty("impactEffectPrefab"), recipe.ImpactPrefabPath))
            {
                Debug.LogError("[HexTacticsGabrielSkillEffectImporter] Skill config was not updated correctly: " + recipe.SkillAssetPath);
                return false;
            }
        }

        if (logSuccess)
        {
            Debug.Log("[HexTacticsGabrielSkillEffectImporter] Generated Gabriel-based skill effects and refreshed current skill configs.");
        }

        return true;
    }

    private static bool GeneratePrefab(EffectRecipe recipe)
    {
        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(recipe.SourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError("[HexTacticsGabrielSkillEffectImporter] Could not load source prefab: " + recipe.SourcePrefabPath);
            return false;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(recipe.SourcePrefabPath);
        try
        {
            prefabRoot.name = recipe.AssetName;
            NormalizeRootTransform(prefabRoot, recipe.RootScale);
            ResetTagsAndLayers(prefabRoot);
            StripGameplayComponents(prefabRoot);
            ConfigureParticleSystems(prefabRoot, recipe.LoopParticles);
            ConfigureTrailRenderers(prefabRoot, recipe.TimingProfile);
            DisableShadows(prefabRoot);
            EnsureTransientEffect(prefabRoot, recipe.FallbackLifetime, recipe.TimingProfile);
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
            Debug.LogError("[HexTacticsGabrielSkillEffectImporter] Could not load skill config: " + recipe.SkillAssetPath);
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

    private static void NormalizeRootTransform(GameObject prefabRoot, float rootScale)
    {
        var transform = prefabRoot.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, rootScale);
    }

    private static void ResetTagsAndLayers(GameObject prefabRoot)
    {
        foreach (var transform in prefabRoot.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.tag = "Untagged";
            transform.gameObject.layer = 0;
        }
    }

    private static void StripGameplayComponents(GameObject prefabRoot)
    {
        foreach (var behaviour in prefabRoot.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is HexTacticsTransientEffect)
            {
                continue;
            }

            Object.DestroyImmediate(behaviour, true);
        }

        foreach (var collider in prefabRoot.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider, true);
        }

        foreach (var rigidbody in prefabRoot.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rigidbody, true);
        }
    }

    private static void ConfigureParticleSystems(GameObject prefabRoot, bool loopParticles)
    {
        var timingProfile = loopParticles ? ProjectileTimingProfile : HitTimingProfile;
        foreach (var particleSystem in prefabRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            if (!loopParticles)
            {
                main.loop = false;
            }

            main.prewarm = false;
            main.playOnAwake = true;
            main.startDelay = ScaleCurve(main.startDelay, timingProfile.StartDelayMultiplier, timingProfile.MaxStartDelay);
            main.startLifetime = ScaleCurve(main.startLifetime, timingProfile.LifetimeMultiplier, timingProfile.MaxLifetime);
            main.duration = Mathf.Clamp(main.duration * timingProfile.DurationMultiplier, 0.05f, timingProfile.MaxDuration);
            main.simulationSpeed = Mathf.Max(0.01f, main.simulationSpeed * timingProfile.SimulationSpeedMultiplier);
            if (!loopParticles)
            {
                main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }

    private static void ConfigureTrailRenderers(GameObject prefabRoot, TimingProfile timingProfile)
    {
        foreach (var trail in prefabRoot.GetComponentsInChildren<TrailRenderer>(true))
        {
            if (trail.time <= 0f)
            {
                continue;
            }

            trail.time = Mathf.Clamp(trail.time * timingProfile.TrailTimeMultiplier, 0.02f, timingProfile.MaxTrailTime);
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

    private static void EnsureTransientEffect(GameObject prefabRoot, float fallbackLifetime, TimingProfile timingProfile)
    {
        var transientEffect = prefabRoot.GetComponent<HexTacticsTransientEffect>();
        if (transientEffect == null)
        {
            transientEffect = prefabRoot.AddComponent<HexTacticsTransientEffect>();
        }

        var serializedObject = new SerializedObject(transientEffect);
        serializedObject.FindProperty("fallbackLifetime").floatValue = fallbackLifetime;
        serializedObject.FindProperty("destroyDelayPadding").floatValue = timingProfile.DestroyDelayPadding;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ParticleSystem.MinMaxCurve ScaleCurve(
        ParticleSystem.MinMaxCurve curve,
        float multiplier,
        float maxValue)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.TwoConstants:
                curve.constantMin = ClampScaledValue(curve.constantMin, multiplier, maxValue);
                curve.constantMax = ClampScaledValue(curve.constantMax, multiplier, maxValue);
                break;
            case ParticleSystemCurveMode.Curve:
            {
                var peak = ResolveCurvePeak(curve.curve);
                curve.curveMultiplier = ClampCurveMultiplier(curve.curveMultiplier, peak, multiplier, maxValue);
                break;
            }
            case ParticleSystemCurveMode.TwoCurves:
            {
                var peak = Mathf.Max(ResolveCurvePeak(curve.curveMin), ResolveCurvePeak(curve.curveMax));
                curve.curveMultiplier = ClampCurveMultiplier(curve.curveMultiplier, peak, multiplier, maxValue);
                break;
            }
            default:
                curve.constant = ClampScaledValue(curve.constant, multiplier, maxValue);
                break;
        }

        return curve;
    }

    private static float ClampScaledValue(float value, float multiplier, float maxValue)
    {
        if (value <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp(value * multiplier, 0f, maxValue);
    }

    private static float ClampCurveMultiplier(float currentMultiplier, float peak, float scaleMultiplier, float maxValue)
    {
        var scaledMultiplier = currentMultiplier * scaleMultiplier;
        if (peak <= 0f)
        {
            return Mathf.Max(0f, scaledMultiplier);
        }

        return Mathf.Clamp(scaledMultiplier, 0f, maxValue / peak);
    }

    private static float ResolveCurvePeak(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return 0f;
        }

        var peak = curve.keys[0].value;
        for (var i = 1; i < curve.length; i++)
        {
            peak = Mathf.Max(peak, curve.keys[i].value);
        }

        return peak;
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
        public EffectRecipe(string sourcePrefabPath, string targetPrefabPath, bool loopParticles, float fallbackLifetime, float rootScale)
        {
            SourcePrefabPath = sourcePrefabPath;
            TargetPrefabPath = targetPrefabPath;
            LoopParticles = loopParticles;
            FallbackLifetime = fallbackLifetime;
            RootScale = rootScale;
        }

        public string SourcePrefabPath { get; }
        public string TargetPrefabPath { get; }
        public bool LoopParticles { get; }
        public float FallbackLifetime { get; }
        public float RootScale { get; }
        public TimingProfile TimingProfile => LoopParticles ? ProjectileTimingProfile : HitTimingProfile;
        public string AssetName => Path.GetFileNameWithoutExtension(TargetPrefabPath);
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

    private readonly struct TimingProfile
    {
        public TimingProfile(
            float startDelayMultiplier,
            float maxStartDelay,
            float durationMultiplier,
            float maxDuration,
            float lifetimeMultiplier,
            float maxLifetime,
            float simulationSpeedMultiplier,
            float trailTimeMultiplier,
            float maxTrailTime,
            float destroyDelayPadding)
        {
            StartDelayMultiplier = startDelayMultiplier;
            MaxStartDelay = maxStartDelay;
            DurationMultiplier = durationMultiplier;
            MaxDuration = maxDuration;
            LifetimeMultiplier = lifetimeMultiplier;
            MaxLifetime = maxLifetime;
            SimulationSpeedMultiplier = simulationSpeedMultiplier;
            TrailTimeMultiplier = trailTimeMultiplier;
            MaxTrailTime = maxTrailTime;
            DestroyDelayPadding = destroyDelayPadding;
        }

        public float StartDelayMultiplier { get; }
        public float MaxStartDelay { get; }
        public float DurationMultiplier { get; }
        public float MaxDuration { get; }
        public float LifetimeMultiplier { get; }
        public float MaxLifetime { get; }
        public float SimulationSpeedMultiplier { get; }
        public float TrailTimeMultiplier { get; }
        public float MaxTrailTime { get; }
        public float DestroyDelayPadding { get; }
    }
}

public static class HexTacticsHovlSkillEffectImporter
{
    public static void RunBatchMode()
    {
        HexTacticsGabrielSkillEffectImporter.RunBatchMode();
    }
}
