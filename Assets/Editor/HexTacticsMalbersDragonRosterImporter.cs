using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class HexTacticsMalbersDragonRosterImporter
{
    private const string AttackParameterName = "Attack1";
    private const float DefaultAttackImpactNormalizedTime = 0.45f;
    private const string TargetBattleUnitFolder = HexTacticsAssetPaths.BattleUnitFolder + "/Dragons";
    private const string ElementalDragonModelFolder = "Assets/Malbers Animations/Dragons/6 - Elemental Dragon/Models";
    private const string ElementalDragonMaterialFolder = "Assets/Malbers Animations/Dragons/6 - Elemental Dragon/Materials/Realistic";
    private const string UrpShaderPrefix = "Universal Render Pipeline/";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    private static List<MaterialUpgrader> urpMaterialUpgraders;

    private static readonly string[] LegacyCharacterAssetNames =
    {
        "StagVanguard",
        "DoeRider",
        "ElkGuardian",
        "FawnScout",
        "TigerFighter",
        "WhiteTigerHunter"
    };

    private static readonly string[] LegacyBattleUnitAssetPaths =
    {
        HexTacticsAssetPaths.BattleUnitFolder + "/Stag.prefab",
        HexTacticsAssetPaths.BattleUnitFolder + "/Doe.prefab",
        HexTacticsAssetPaths.BattleUnitFolder + "/Elk.prefab",
        HexTacticsAssetPaths.BattleUnitFolder + "/Fawn.prefab",
        HexTacticsAssetPaths.BattleUnitFolder + "/Tiger.prefab",
        HexTacticsAssetPaths.BattleUnitFolder + "/WhiteTiger.prefab"
    };

    private static readonly DragonImportSpec[] Specs =
    {
        new(
            "NatureDragon",
            "森护龙",
            "自然护鳞，迅捷追猎与连续压迫",
            ElementalDragonModelFolder + "/Elemental Dragon Realistic Small.prefab",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Nature_Body.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Nature_Wings.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Nature_Eye.mat",
            HexTacticsCharacterVisualArchetype.Stag,
            13,
            4,
            2,
            5,
            5,
            0,
            0.95f,
            AttackTimingProfile.Slash,
            "BasicAttack_Melee_P4_R0",
            "PredatorRush_Melee_P6_R0_C1",
            "ExecutionDive_Melee_P7_R0_C2"),
        new(
            "IceDragon",
            "冰霜龙",
            "寒息射线，擅长中距离控制与击退",
            ElementalDragonModelFolder + "/Elemental Dragon Realistic Small.prefab",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Ice_Body.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Ice_Wings.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Ice_Eye.mat",
            HexTacticsCharacterVisualArchetype.WhiteTiger,
            11,
            5,
            2,
            4,
            5,
            1,
            0.91f,
            AttackTimingProfile.Cast,
            "BasicAttack_Ranged_P4_R1",
            "RepulseBolt_Ranged_P5_R1_C2",
            "Snipe_Ranged_P5_R2_C2"),
        new(
            "CrystalDragon",
            "晶簇龙",
            "晶能吐息，远程爆裂与终结兼备",
            ElementalDragonModelFolder + "/Elemental Dragon Realistic.prefab",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Crystal_Body.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Crystal_Wings.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Crystal_Eye.mat",
            HexTacticsCharacterVisualArchetype.Doe,
            12,
            5,
            1,
            4,
            5,
            1,
            1.00f,
            AttackTimingProfile.Cast,
            "BasicAttack_Ranged_P4_R1",
            "ArcBurst_Ranged_P6_R1_C2",
            "ShadowBolt_Ranged_P5_R2_C2"),
        new(
            "LavaDragon",
            "熔岩龙",
            "熔岩鳞甲，近战爆发与灼烧压迫",
            ElementalDragonModelFolder + "/Elemental Dragon Realistic.prefab",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Lava_Body.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Lava_Wings.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Lava_Eye.mat",
            HexTacticsCharacterVisualArchetype.Tiger,
            15,
            5,
            1,
            4,
            5,
            1,
            1.10f,
            AttackTimingProfile.Heavy,
            "BasicAttack_Melee_P5_R0",
            "WaveSlash_Ranged_P6_R1_C2",
            "CrushingBlow_Melee_P7_R0_C2"),
        new(
            "RockDragon",
            "岩甲龙",
            "岩壳厚重，前排顶线与破甲推进",
            ElementalDragonModelFolder + "/Elemental Dragon Realistic Big.prefab",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Rock_Body.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Rock_Wings.mat",
            ElementalDragonMaterialFolder + "/Elemental_Dragon_Rock_Eye.mat",
            HexTacticsCharacterVisualArchetype.Elk,
            18,
            6,
            1,
            2,
            5,
            0,
            1.17f,
            AttackTimingProfile.Heavy,
            "BasicAttack_Melee_P3_R0",
            "BulwarkBash_Melee_P5_R0_C1",
            "BreakArmor_Melee_P6_R0_C2")
    };

    [MenuItem("Tools/Hex Tactics/Import Malbers Dragons Roster")]
    public static void RunFromMenu()
    {
        RunInternal(exitOnComplete: false);
    }

    public static void RunBatchMode()
    {
        RunInternal(exitOnComplete: true);
    }

    private static void RunInternal(bool exitOnComplete)
    {
        var exitCode = RunImport() ? 0 : 1;
        if (exitOnComplete && Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    private static bool RunImport()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureFolder(TargetBattleUnitFolder);
        EnsureFolder(HexTacticsAssetPaths.CharacterConfigFolder);

        var issues = new List<string>();
        RemoveLegacyAnimalRoster(issues);

        var importedConfigPaths = new List<string>();
        foreach (var spec in Specs)
        {
            var battleUnitPrefab = CreateOrUpdateBattleUnitPrefab(spec, issues);
            if (battleUnitPrefab == null)
            {
                continue;
            }

            var config = CreateOrUpdateCharacterConfig(spec, battleUnitPrefab, issues);
            if (config != null)
            {
                importedConfigPaths.Add(AssetDatabase.GetAssetPath(config));
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var generatedIconCount = HexTacticsCharacterIconGenerator.GenerateIconsForConfigPaths(importedConfigPaths, issues);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressables.ReleaseAll();
        HexTacticsAddressablesSync.Sync();
        HexTacticsAddressables.ReleaseAll();

        if (issues.Count > 0)
        {
            foreach (var issue in issues)
            {
                Debug.LogError("[HexTacticsMalbersDragonRosterImporter] " + issue);
            }

            Debug.LogError($"[HexTacticsMalbersDragonRosterImporter] Import failed with {issues.Count} issue(s).");
            return false;
        }

        Debug.Log($"[HexTacticsMalbersDragonRosterImporter] Imported {importedConfigPaths.Count} dragon configs and generated {generatedIconCount} icons.");
        return true;
    }

    private static void RemoveLegacyAnimalRoster(List<string> issues)
    {
        foreach (var assetName in LegacyCharacterAssetNames)
        {
            DeleteAssetAndRemoveAddressableEntry($"{HexTacticsAssetPaths.CharacterConfigFolder}/{assetName}.asset", issues);
            DeleteAssetIfExists($"{HexTacticsAssetPaths.CharacterIconFolder}/{assetName}Icon.png", issues);
        }

        foreach (var assetPath in LegacyBattleUnitAssetPaths)
        {
            DeleteAssetAndRemoveAddressableEntry(assetPath, issues);
        }
    }

    private static void DeleteAssetAndRemoveAddressableEntry(string assetPath, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !AssetExists(assetPath))
        {
            return;
        }

        RemoveAddressableEntry(assetPath);
        if (!AssetDatabase.DeleteAsset(assetPath))
        {
            issues?.Add("Could not delete asset: " + assetPath);
        }
    }

    private static void DeleteAssetIfExists(string assetPath, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !AssetExists(assetPath))
        {
            return;
        }

        if (!AssetDatabase.DeleteAsset(assetPath))
        {
            issues?.Add("Could not delete asset: " + assetPath);
        }
    }

    private static bool AssetExists(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return false;
        }

        var absolutePath = GetAbsoluteProjectPath(assetPath);
        return File.Exists(absolutePath) || Directory.Exists(absolutePath);
    }

    private static void RemoveAddressableEntry(string assetPath)
    {
        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrWhiteSpace(guid))
        {
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            return;
        }

        if (settings.FindAssetEntry(guid) == null)
        {
            return;
        }

        settings.RemoveAssetEntry(guid);
        EditorUtility.SetDirty(settings);
    }

    private static GameObject CreateOrUpdateBattleUnitPrefab(DragonImportSpec spec, List<string> issues)
    {
        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.SourcePrefabPath);
        if (sourcePrefab == null)
        {
            issues?.Add("Source dragon prefab could not be loaded: " + spec.SourcePrefabPath);
            return null;
        }

        var bodyMaterial = LoadAssetAtPath<Material>(spec.BodyMaterialPath, issues, spec.AssetName + " body material");
        var wingMaterial = LoadAssetAtPath<Material>(spec.WingMaterialPath, issues, spec.AssetName + " wing material");
        var eyeMaterial = LoadAssetAtPath<Material>(spec.EyeMaterialPath, issues, spec.AssetName + " eye material");
        bodyMaterial = EnsureMaterialCompatibleWithUrp(bodyMaterial, issues, spec.AssetName + " body material");
        wingMaterial = EnsureMaterialCompatibleWithUrp(wingMaterial, issues, spec.AssetName + " wing material");
        eyeMaterial = EnsureMaterialCompatibleWithUrp(eyeMaterial, issues, spec.AssetName + " eye material");
        if (bodyMaterial == null || wingMaterial == null || eyeMaterial == null)
        {
            return null;
        }

        var instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (instance == null)
        {
            issues?.Add("Dragon prefab could not be instantiated: " + spec.SourcePrefabPath);
            return null;
        }

        try
        {
            instance.name = spec.AssetName;

            ApplySingleMaterial(instance.transform, "Elemental_Body", bodyMaterial, issues, spec.AssetName);
            ApplySingleMaterial(instance.transform, "Elemental_Eyes", eyeMaterial, issues, spec.AssetName);
            ApplySingleMaterial(instance.transform, "Elemental_Wings", wingMaterial, issues, spec.AssetName);
            ApplySingleMaterial(instance.transform, "Elemental_Tail", wingMaterial, issues, spec.AssetName);

            var prefabPath = $"{TargetBattleUnitFolder}/{spec.AssetName}.prefab";
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            if (savedPrefab == null)
            {
                issues?.Add("Dragon prefab could not be saved: " + prefabPath);
            }

            return savedPrefab;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void ApplySingleMaterial(Transform root, string childName, Material material, List<string> issues, string assetName)
    {
        var child = root != null ? root.Find(childName) : null;
        var renderer = child != null ? child.GetComponent<Renderer>() : null;
        if (renderer == null)
        {
            issues?.Add($"{assetName} is missing renderer '{childName}'.");
            return;
        }

        renderer.sharedMaterials = new[] { material };
        EditorUtility.SetDirty(renderer);
    }

    private static Material EnsureMaterialCompatibleWithUrp(Material material, List<string> issues, string label)
    {
        if (material == null)
        {
            return null;
        }

        if (material.shader == null)
        {
            issues?.Add($"{label} has a missing shader: {AssetDatabase.GetAssetPath(material)}");
            return null;
        }

        if (!GraphicsSettings.defaultRenderPipeline)
        {
            return material;
        }

        if (!IsUrpShader(material.shader))
        {
            var message = string.Empty;
            if (!MaterialUpgrader.Upgrade(
                    material,
                    GetOrCreateUrpMaterialUpgraders(),
                    MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound,
                    ref message))
            {
                issues?.Add($"{label} could not be upgraded for URP: {AssetDatabase.GetAssetPath(material)} ({message})");
                return null;
            }
        }

        if (material.shader == null || !IsUrpShader(material.shader))
        {
            var shaderName = material.shader != null ? material.shader.name : "<missing>";
            issues?.Add($"{label} is not using a URP shader after upgrade: {AssetDatabase.GetAssetPath(material)} -> {shaderName}");
            return null;
        }

        if (material.shader.name == UrpLitShaderName)
        {
            BaseShaderGUI.SetupMaterialBlendMode(material);
            LitGUI.SetMaterialKeywords(material);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static bool IsUrpShader(Shader shader)
    {
        return shader != null && shader.name.StartsWith(UrpShaderPrefix);
    }

    private static List<MaterialUpgrader> GetOrCreateUrpMaterialUpgraders()
    {
        urpMaterialUpgraders ??= MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));
        return urpMaterialUpgraders;
    }

    private static HexTacticsCharacterConfig CreateOrUpdateCharacterConfig(
        DragonImportSpec spec,
        GameObject battleUnitPrefab,
        List<string> issues)
    {
        var skillAssets = ResolveSkillAssets(spec, issues);
        if (skillAssets == null)
        {
            return null;
        }

        var configPath = $"{HexTacticsAssetPaths.CharacterConfigFolder}/{spec.AssetName}.asset";
        var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(configPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<HexTacticsCharacterConfig>();
            config.name = spec.AssetName;
            AssetDatabase.CreateAsset(config, configPath);
        }

        config.name = spec.AssetName;

        var timing = ResolveAttackTiming(spec, battleUnitPrefab, issues);
        var serializedObject = new SerializedObject(config);
        serializedObject.FindProperty("displayName").stringValue = spec.DisplayName;
        serializedObject.FindProperty("description").stringValue = spec.Description;
        serializedObject.FindProperty("maxHealth").intValue = spec.MaxHealth;
        serializedObject.FindProperty("cost").intValue = spec.Cost;
        serializedObject.FindProperty("moveRange").intValue = spec.MoveRange;
        serializedObject.FindProperty("speed").intValue = spec.Speed;
        serializedObject.FindProperty("maxEnergy").intValue = spec.MaxEnergy;
        serializedObject.FindProperty("startingEnergy").intValue = Mathf.Clamp(spec.StartingEnergy, 0, spec.MaxEnergy);
        serializedObject.FindProperty("visualArchetype").enumValueIndex = (int)spec.VisualArchetype;
        serializedObject.FindProperty("avatar").objectReferenceValue = config.Avatar;
        serializedObject.FindProperty("battleUnitPrefab").objectReferenceValue = battleUnitPrefab;
        serializedObject.FindProperty("visualHeightScale").floatValue = spec.VisualHeightScale;
        serializedObject.FindProperty("attackImpactFrame").intValue = timing.ImpactFrame;
        serializedObject.FindProperty("attackImpactNormalizedTime").floatValue = timing.ImpactNormalizedTime;
        serializedObject.FindProperty("attackPower").intValue = skillAssets[0].Power;
        serializedObject.FindProperty("attackRange").intValue = skillAssets[0].AttackRange;

        var skillsProperty = serializedObject.FindProperty("skills");
        skillsProperty.arraySize = skillAssets.Length;
        for (var i = 0; i < skillAssets.Length; i++)
        {
            skillsProperty.GetArrayElementAtIndex(i).objectReferenceValue = skillAssets[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        return config;
    }

    private static HexTacticsSkillConfig[] ResolveSkillAssets(DragonImportSpec spec, List<string> issues)
    {
        var skills = new HexTacticsSkillConfig[spec.SkillAssetNames.Length];
        for (var i = 0; i < spec.SkillAssetNames.Length; i++)
        {
            var assetPath = $"{HexTacticsAssetPaths.SkillConfigFolder}/{spec.SkillAssetNames[i]}.asset";
            skills[i] = LoadAssetAtPath<HexTacticsSkillConfig>(assetPath, issues, $"{spec.AssetName} skill '{spec.SkillAssetNames[i]}'");
            if (skills[i] == null)
            {
                return null;
            }
        }

        return skills;
    }

    private static T LoadAssetAtPath<T>(string assetPath, List<string> issues, string label) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            issues?.Add($"{label} could not be loaded: {assetPath}");
        }

        return asset;
    }

    private static AttackTimingResolution ResolveAttackTiming(DragonImportSpec spec, GameObject prefab, List<string> issues)
    {
        if (prefab == null)
        {
            return new AttackTimingResolution(0, DefaultAttackImpactNormalizedTime, "Missing Prefab", 0);
        }

        var attackClip = FindPrimaryAttackClip(prefab);
        var normalizedImpact = GetImpactNormalizedTime(spec.AttackTimingProfile);
        if (attackClip == null)
        {
            issues?.Add($"Attack clip could not be resolved for {spec.AssetName}: {spec.SourcePrefabPath}");
            return new AttackTimingResolution(0, normalizedImpact, "Unknown", 0);
        }

        if (attackClip.frameRate <= 0.01f || attackClip.length <= 0.01f)
        {
            issues?.Add($"Attack clip has invalid timing data for {spec.AssetName}: {attackClip.name}");
            return new AttackTimingResolution(0, normalizedImpact, attackClip.name, 0);
        }

        var totalFrames = Mathf.Max(1, Mathf.RoundToInt(attackClip.frameRate * attackClip.length));
        var impactFrame = Mathf.Clamp(Mathf.RoundToInt(totalFrames * normalizedImpact) + 1, 1, totalFrames);
        return new AttackTimingResolution(impactFrame, normalizedImpact, attackClip.name, totalFrames);
    }

    private static AnimationClip FindPrimaryAttackClip(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        var animator = prefab.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            return null;
        }

        var controller = animator.runtimeAnimatorController;
        if (controller is AnimatorOverrideController overrideController)
        {
            controller = overrideController.runtimeAnimatorController;
        }

        if (controller is AnimatorController animatorController)
        {
            var controllerDrivenClip = FindControllerDrivenAttackClip(animatorController);
            if (controllerDrivenClip != null)
            {
                return controllerDrivenClip;
            }
        }

        return ChoosePreferredAttackClip(controller != null ? controller.animationClips : null);
    }

    private static AnimationClip FindControllerDrivenAttackClip(AnimatorController controller)
    {
        var candidates = new List<AnimationClip>();
        foreach (var layer in controller.layers)
        {
            CollectAttackTransitionClips(layer.stateMachine, candidates);
        }

        return ChoosePreferredAttackClip(candidates);
    }

    private static void CollectAttackTransitionClips(AnimatorStateMachine stateMachine, List<AnimationClip> clips)
    {
        if (stateMachine == null)
        {
            return;
        }

        foreach (var transition in stateMachine.anyStateTransitions)
        {
            if (TransitionUsesAttackParameter(transition))
            {
                CollectDestinationMotionClips(transition, clips);
            }
        }

        foreach (var childState in stateMachine.states)
        {
            foreach (var transition in childState.state.transitions)
            {
                if (TransitionUsesAttackParameter(transition))
                {
                    CollectDestinationMotionClips(transition, clips);
                }
            }
        }

        foreach (var childStateMachine in stateMachine.stateMachines)
        {
            CollectAttackTransitionClips(childStateMachine.stateMachine, clips);
        }
    }

    private static bool TransitionUsesAttackParameter(AnimatorStateTransition transition)
    {
        if (transition == null || transition.conditions == null)
        {
            return false;
        }

        foreach (var condition in transition.conditions)
        {
            if (condition.parameter == AttackParameterName)
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectDestinationMotionClips(AnimatorStateTransition transition, List<AnimationClip> clips)
    {
        if (transition == null)
        {
            return;
        }

        if (transition.destinationState != null)
        {
            CollectMotionClips(transition.destinationState.motion, clips);
        }
        else if (transition.destinationStateMachine != null && transition.destinationStateMachine.defaultState != null)
        {
            CollectMotionClips(transition.destinationStateMachine.defaultState.motion, clips);
        }
    }

    private static void CollectMotionClips(Motion motion, List<AnimationClip> clips)
    {
        if (motion == null)
        {
            return;
        }

        if (motion is AnimationClip animationClip)
        {
            if (IsAttackClip(animationClip))
            {
                clips.Add(animationClip);
            }

            return;
        }

        if (motion is BlendTree blendTree)
        {
            foreach (var childMotion in blendTree.children)
            {
                CollectMotionClips(childMotion.motion, clips);
            }
        }
    }

    private static AnimationClip ChoosePreferredAttackClip(IEnumerable<AnimationClip> clips)
    {
        AnimationClip bestClip = null;
        var bestScore = int.MinValue;
        if (clips == null)
        {
            return null;
        }

        foreach (var clip in clips)
        {
            if (!IsAttackClip(clip))
            {
                continue;
            }

            var score = GetAttackClipScore(clip.name);
            if (bestClip == null || score > bestScore)
            {
                bestClip = clip;
                bestScore = score;
            }
        }

        return bestClip;
    }

    private static bool IsAttackClip(AnimationClip clip)
    {
        return clip != null &&
               clip.name.IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int GetAttackClipScore(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return 0;
        }

        var normalizedName = clipName.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (normalizedName.Contains("attack01"))
        {
            return 300;
        }

        if (normalizedName.Contains("attack1"))
        {
            return 260;
        }

        if (normalizedName.Contains("attack02"))
        {
            return 220;
        }

        if (normalizedName.Contains("attack2"))
        {
            return 200;
        }

        if (normalizedName.Contains("attack03"))
        {
            return 180;
        }

        if (normalizedName.Contains("attack3"))
        {
            return 160;
        }

        return normalizedName.Contains("attack") ? 100 : 0;
    }

    private static float GetImpactNormalizedTime(AttackTimingProfile profile)
    {
        return profile switch
        {
            AttackTimingProfile.Lunge => 0.40f,
            AttackTimingProfile.Snap => 0.44f,
            AttackTimingProfile.Slash => 0.48f,
            AttackTimingProfile.BodySlam => 0.52f,
            AttackTimingProfile.Heavy => 0.58f,
            AttackTimingProfile.Cast => 0.62f,
            _ => DefaultAttackImpactNormalizedTime
        };
    }

    private static void EnsureFolder(string folderPath)
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

    private static string GetAbsoluteProjectPath(string assetPath)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot ?? string.Empty, assetPath);
    }

    private readonly struct DragonImportSpec
    {
        public DragonImportSpec(
            string assetName,
            string displayName,
            string description,
            string sourcePrefabPath,
            string bodyMaterialPath,
            string wingMaterialPath,
            string eyeMaterialPath,
            HexTacticsCharacterVisualArchetype visualArchetype,
            int maxHealth,
            int cost,
            int moveRange,
            int speed,
            int maxEnergy,
            int startingEnergy,
            float visualHeightScale,
            AttackTimingProfile attackTimingProfile,
            params string[] skillAssetNames)
        {
            AssetName = assetName;
            DisplayName = displayName;
            Description = description;
            SourcePrefabPath = sourcePrefabPath;
            BodyMaterialPath = bodyMaterialPath;
            WingMaterialPath = wingMaterialPath;
            EyeMaterialPath = eyeMaterialPath;
            VisualArchetype = visualArchetype;
            MaxHealth = maxHealth;
            Cost = cost;
            MoveRange = moveRange;
            Speed = speed;
            MaxEnergy = maxEnergy;
            StartingEnergy = startingEnergy;
            VisualHeightScale = visualHeightScale;
            AttackTimingProfile = attackTimingProfile;
            SkillAssetNames = skillAssetNames ?? System.Array.Empty<string>();
        }

        public string AssetName { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string SourcePrefabPath { get; }
        public string BodyMaterialPath { get; }
        public string WingMaterialPath { get; }
        public string EyeMaterialPath { get; }
        public HexTacticsCharacterVisualArchetype VisualArchetype { get; }
        public int MaxHealth { get; }
        public int Cost { get; }
        public int MoveRange { get; }
        public int Speed { get; }
        public int MaxEnergy { get; }
        public int StartingEnergy { get; }
        public float VisualHeightScale { get; }
        public AttackTimingProfile AttackTimingProfile { get; }
        public string[] SkillAssetNames { get; }
    }

    private readonly struct AttackTimingResolution
    {
        public AttackTimingResolution(int impactFrame, float impactNormalizedTime, string clipName, int totalFrames)
        {
            ImpactFrame = impactFrame;
            ImpactNormalizedTime = impactNormalizedTime;
            ClipName = clipName;
            TotalFrames = totalFrames;
        }

        public int ImpactFrame { get; }
        public float ImpactNormalizedTime { get; }
        public string ClipName { get; }
        public int TotalFrames { get; }
    }

    private enum AttackTimingProfile
    {
        Default,
        Lunge,
        Snap,
        Slash,
        BodySlam,
        Heavy,
        Cast
    }
}
