using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class HexTacticsRpgMonsterRosterImporter
{
    private const string OutputRoot = HexTacticsAssetPaths.CharacterConfigFolder + "/RPGMonsterBundlePolyart";
    private const string DefaultDescription = "近战压制";
    private const float DefaultAttackImpactNormalizedTime = 0.45f;
    private const string AttackParameterName = "Attack1";

    private static readonly MonsterImportSpec[] Specs =
    {
        new("Bat", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/BatPADefault.prefab", "蝙蝠", "高速飞掠", 6, 2, 2, 4, HexTacticsCharacterVisualArchetype.Fawn, 0.70f, AttackTimingProfile.Lunge),
        new("Dragon", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/DragonPADefault.prefab", "幼龙", "重击突袭", 14, 5, 5, 2, HexTacticsCharacterVisualArchetype.Tiger, 1.10f, AttackTimingProfile.Heavy),
        new("EvilMage", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/EvilMagePADefault.prefab", "邪术师", "暗术爆发", 8, 4, 4, 2, HexTacticsCharacterVisualArchetype.Doe, 1.00f, AttackTimingProfile.Cast),
        new("Golem", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/GolemPADefault.prefab", "岩石魔像", "厚甲前排", 16, 4, 5, 1, HexTacticsCharacterVisualArchetype.Elk, 1.25f, AttackTimingProfile.Heavy),
        new("MonsterPlant", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/MonsterPlantPADefault.prefab", "怪物花", "扎根缠斗", 9, 3, 3, 1, HexTacticsCharacterVisualArchetype.Doe, 0.95f, AttackTimingProfile.BodySlam),
        new("Orc", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/OrcPADefault.prefab", "兽人战士", "蛮力推进", 12, 4, 4, 2, HexTacticsCharacterVisualArchetype.Stag, 1.00f, AttackTimingProfile.Heavy),
        new("Skeleton", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/SkeletonPADefault.prefab", "骷髅兵", "脆身快攻", 8, 3, 3, 2, HexTacticsCharacterVisualArchetype.Doe, 0.95f, AttackTimingProfile.Slash),
        new("Slime", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/SlimePADefault.prefab", "史莱姆", "黏性纠缠", 7, 2, 2, 2, HexTacticsCharacterVisualArchetype.Fawn, 0.75f, AttackTimingProfile.BodySlam),
        new("Spider", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/SpiderPADefault.prefab", "毒蛛", "迅捷穿插", 7, 3, 2, 3, HexTacticsCharacterVisualArchetype.Fawn, 0.85f, AttackTimingProfile.Snap),
        new("TurtleShell", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave01/CharacterPA/TurtleShellPA.prefab", "龟壳兽", "高防慢推", 13, 3, 4, 1, HexTacticsCharacterVisualArchetype.Elk, 1.00f, AttackTimingProfile.Heavy),
        new("Beholder", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/BeholderPADefault.prefab", "眼魔", "浮游压迫", 9, 4, 4, 3, HexTacticsCharacterVisualArchetype.WhiteTiger, 0.90f, AttackTimingProfile.Cast),
        new("BlackKnight", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/BlackKnightPADefault.prefab", "黑骑士", "重装推进", 13, 5, 5, 2, HexTacticsCharacterVisualArchetype.Stag, 1.05f, AttackTimingProfile.Heavy),
        new("ChestMonster", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/ChestMonsterPADefault.prefab", "宝箱怪", "伏击反咬", 10, 4, 4, 1, HexTacticsCharacterVisualArchetype.Elk, 0.90f, AttackTimingProfile.Snap),
        new("CrabMonster", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/CrabMonsterPADefault.prefab", "蟹怪", "横移压线", 12, 3, 4, 2, HexTacticsCharacterVisualArchetype.Doe, 0.90f, AttackTimingProfile.BodySlam),
        new("FlyingDemon", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/FylingDemonPADefault.prefab", "飞魔", "俯冲撕咬", 8, 4, 4, 3, HexTacticsCharacterVisualArchetype.WhiteTiger, 0.95f, AttackTimingProfile.Lunge),
        new("LizardWarrior", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/LizardWarriorPADefault.prefab", "蜥蜴战士", "贴身肉搏", 11, 4, 4, 2, HexTacticsCharacterVisualArchetype.Stag, 1.00f, AttackTimingProfile.Slash),
        new("RatAssassin", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/RatAssassinPADefault.prefab", "鼠人刺客", "侧翼突杀", 7, 4, 3, 4, HexTacticsCharacterVisualArchetype.Fawn, 0.90f, AttackTimingProfile.Lunge),
        new("Specter", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/SpecterPADefault.prefab", "幽魂", "幽影追击", 8, 4, 4, 3, HexTacticsCharacterVisualArchetype.WhiteTiger, 1.00f, AttackTimingProfile.Cast),
        new("Werewolf", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/WerewolfPADefault.prefab", "狼人", "狂爪扑击", 12, 5, 5, 3, HexTacticsCharacterVisualArchetype.Tiger, 1.05f, AttackTimingProfile.Slash),
        new("WormMonster", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave02/CharacterPA/WormMonsterPADefault.prefab", "蠕虫怪", "钝击顶线", 14, 4, 5, 1, HexTacticsCharacterVisualArchetype.Elk, 1.15f, AttackTimingProfile.Heavy),
        new("BattleBee", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/BattleBeePADefault.prefab", "战蜂", "急袭穿梭", 6, 2, 2, 4, HexTacticsCharacterVisualArchetype.Fawn, 0.75f, AttackTimingProfile.Lunge),
        new("BishopKnight", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/BishopKnightPADefault.prefab", "主教骑士", "稳步压阵", 12, 4, 4, 2, HexTacticsCharacterVisualArchetype.Stag, 1.00f, AttackTimingProfile.Heavy),
        new("Cactus", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/CactusPADefault.prefab", "仙人掌怪", "迟缓反打", 10, 3, 3, 1, HexTacticsCharacterVisualArchetype.Doe, 0.90f, AttackTimingProfile.BodySlam),
        new("Cyclops", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/CyclopsPADefault.prefab", "独眼巨人", "巨力压制", 15, 5, 5, 2, HexTacticsCharacterVisualArchetype.Elk, 1.15f, AttackTimingProfile.Heavy),
        new("DemonKing", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/DemonKingPADefault.prefab", "魔王", "高压斩击", 16, 6, 6, 2, HexTacticsCharacterVisualArchetype.Tiger, 1.20f, AttackTimingProfile.Heavy),
        new("Fishman", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/FishmanPADefault.prefab", "鱼人", "疾步围杀", 10, 4, 4, 3, HexTacticsCharacterVisualArchetype.Doe, 1.00f, AttackTimingProfile.Slash),
        new("MushroomAngry", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/MushroomAngryPADefault.prefab", "怒蘑菇", "贴地缠斗", 8, 3, 3, 2, HexTacticsCharacterVisualArchetype.Fawn, 0.85f, AttackTimingProfile.BodySlam),
        new("MushroomSmile", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/MushroomSmilePADefault.prefab", "笑脸蘑菇", "低费占格", 7, 2, 2, 2, HexTacticsCharacterVisualArchetype.Fawn, 0.80f, AttackTimingProfile.BodySlam),
        new("NagaWizard", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/NagaWizardPADefault.prefab", "娜迦巫师", "法术牵制", 9, 4, 4, 2, HexTacticsCharacterVisualArchetype.Doe, 1.00f, AttackTimingProfile.Cast),
        new("Salamander", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/SalamanderPADefault.prefab", "火蜥蜴", "灼热突袭", 11, 5, 4, 3, HexTacticsCharacterVisualArchetype.Tiger, 1.00f, AttackTimingProfile.Slash),
        new("StingRay", "Assets/RPGMonsterBundlePolyart/CommonStuffs/Prefab/Wave03/CharacterPA/StingRayPADefault.prefab", "魔鬼鱼", "滑翔切入", 7, 3, 3, 4, HexTacticsCharacterVisualArchetype.WhiteTiger, 0.85f, AttackTimingProfile.Lunge)
    };

    public static void RunBatchMode()
    {
        var exitCode = RunInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/RPG Monster Bundle Polyart/Import Hex Tactics Characters")]
    public static void RunFromMenu()
    {
        RunInternal();
    }

    public static void ReportAttackTimingBatchMode()
    {
        var exitCode = ReportAttackTimingInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/RPG Monster Bundle Polyart/Report Hex Tactics Attack Timings")]
    public static void ReportAttackTimingFromMenu()
    {
        ReportAttackTimingInternal();
    }

    private static bool RunInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureFolder(OutputRoot);

        var importedCount = 0;
        var issues = new List<string>();
        foreach (var spec in Specs)
        {
            if (CreateOrUpdateCharacterConfig(spec, issues))
            {
                importedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressablesSync.Sync();

        if (issues.Count > 0)
        {
            foreach (var issue in issues)
            {
                Debug.LogError("[HexTacticsRpgMonsterRosterImporter] " + issue);
            }

            Debug.LogError($"[HexTacticsRpgMonsterRosterImporter] Import failed with {issues.Count} issue(s).");
            return false;
        }

        Debug.Log($"[HexTacticsRpgMonsterRosterImporter] Imported or updated {importedCount} character config assets.");
        return true;
    }

    private static bool ReportAttackTimingInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var issues = new List<string>();
        foreach (var spec in Specs)
        {
            var timing = ResolveAttackTiming(spec, issues);
            Debug.Log(
                $"[HexTacticsRpgMonsterRosterImporter] {spec.AssetName} -> clip={timing.ClipName} " +
                $"frames={timing.TotalFrames} impactFrame={timing.ImpactFrame} " +
                $"profile={spec.AttackTimingProfile} normalized={timing.ImpactNormalizedTime:0.00}");
        }

        if (issues.Count == 0)
        {
            Debug.Log("[HexTacticsRpgMonsterRosterImporter] Attack timing report completed without issues.");
            return true;
        }

        foreach (var issue in issues)
        {
            Debug.LogError("[HexTacticsRpgMonsterRosterImporter] " + issue);
        }

        Debug.LogError($"[HexTacticsRpgMonsterRosterImporter] Attack timing report found {issues.Count} issue(s).");
        return false;
    }

    private static bool CreateOrUpdateCharacterConfig(MonsterImportSpec spec, List<string> issues)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
        if (prefab == null)
        {
            issues.Add($"Prefab could not be loaded: {spec.PrefabPath}");
            return false;
        }

        var assetPath = $"{OutputRoot}/{spec.AssetName}.asset";
        var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(assetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<HexTacticsCharacterConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
        }

        var timing = ResolveAttackTiming(spec, issues);
        var serializedObject = new SerializedObject(config);
        serializedObject.FindProperty("displayName").stringValue = spec.DisplayName;
        serializedObject.FindProperty("description").stringValue = string.IsNullOrWhiteSpace(spec.Description)
            ? DefaultDescription
            : spec.Description;
        serializedObject.FindProperty("maxHealth").intValue = spec.MaxHealth;
        serializedObject.FindProperty("attackPower").intValue = spec.AttackPower;
        serializedObject.FindProperty("cost").intValue = spec.Cost;
        serializedObject.FindProperty("moveRange").intValue = spec.MoveRange;
        serializedObject.FindProperty("visualArchetype").enumValueIndex = (int)spec.VisualArchetype;
        serializedObject.FindProperty("avatar").objectReferenceValue = null;
        serializedObject.FindProperty("battleUnitPrefab").objectReferenceValue = prefab;
        serializedObject.FindProperty("visualHeightScale").floatValue = spec.VisualHeightScale;
        serializedObject.FindProperty("attackImpactFrame").intValue = timing.ImpactFrame;
        serializedObject.FindProperty("attackImpactNormalizedTime").floatValue = timing.ImpactNormalizedTime;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        return true;
    }

    private static AttackTimingResolution ResolveAttackTiming(MonsterImportSpec spec, List<string> issues)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
        if (prefab == null)
        {
            return new AttackTimingResolution(0, DefaultAttackImpactNormalizedTime, "Missing Prefab", 0);
        }

        var attackClip = FindPrimaryAttackClip(prefab);
        var normalizedImpact = GetImpactNormalizedTime(spec.AttackTimingProfile);
        if (attackClip == null)
        {
            issues?.Add($"Attack clip could not be resolved for {spec.AssetName}: {spec.PrefabPath}");
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

    private sealed class MonsterImportSpec
    {
        public MonsterImportSpec(
            string assetName,
            string prefabPath,
            string displayName,
            string description,
            int maxHealth,
            int attackPower,
            int cost,
            int moveRange,
            HexTacticsCharacterVisualArchetype visualArchetype,
            float visualHeightScale,
            AttackTimingProfile attackTimingProfile)
        {
            AssetName = assetName;
            PrefabPath = prefabPath;
            DisplayName = displayName;
            Description = description;
            MaxHealth = maxHealth;
            AttackPower = attackPower;
            Cost = cost;
            MoveRange = moveRange;
            VisualArchetype = visualArchetype;
            VisualHeightScale = visualHeightScale;
            AttackTimingProfile = attackTimingProfile;
        }

        public string AssetName { get; }
        public string PrefabPath { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int MaxHealth { get; }
        public int AttackPower { get; }
        public int Cost { get; }
        public int MoveRange { get; }
        public HexTacticsCharacterVisualArchetype VisualArchetype { get; }
        public float VisualHeightScale { get; }
        public AttackTimingProfile AttackTimingProfile { get; }
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
