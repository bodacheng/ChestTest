using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HexTacticsSkillBalanceRebuild
{
    private static readonly SkillDefinition[] SkillDefinitions =
    {
        new(
            "ScoutBurst_Melee_P4_R0_C1",
            "俯冲重击",
            "消耗 1 点能量向目标踏前扑击，适合轻型单位补足斩杀线。",
            4,
            0,
            1,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactMediumAzure.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit16.prefab",
            1.04f,
            0.56f,
            0.06f,
            selfMovementAttribute: HexTacticsSelfMovementAttribute.Advance),
        new(
            "VenomStrike_Melee_P4_R0_C1",
            "毒袭",
            "消耗 1 点能量踏前咬刺，将毒伤更稳定地送进前排。",
            4,
            0,
            1,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactMediumAzure.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit16.prefab",
            1.0f,
            0.56f,
            0.05f,
            selfMovementAttribute: HexTacticsSelfMovementAttribute.Advance),
        new(
            "ThornShot_Ranged_P4_R1_C1",
            "棘刺射击",
            "消耗 1 点能量射出带刺弹体，命中后可将目标逼退 1 格。",
            4,
            1,
            1,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileFrost.prefab",
            0.84f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit16.prefab",
            1.02f,
            0.60f,
            0.07f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget),
        new(
            "SkirmishCombo_Melee_P5_R0_C1",
            "追猎连击",
            "消耗 1 点能量踏前切入后快速连击，是中速近战的主力爆发。",
            5,
            0,
            1,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyEmber.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit16.prefab",
            1.12f,
            0.60f,
            0.07f,
            selfMovementAttribute: HexTacticsSelfMovementAttribute.Advance),
        new(
            "BulwarkBash_Melee_P5_R0_C1",
            "壁垒猛撞",
            "消耗 1 点能量用盾肩或甲壳猛撞，将前方目标顶退 1 格。",
            5,
            0,
            1,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyEmber.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit16.prefab",
            1.1f,
            0.60f,
            0.07f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget),
        new(
            "BreakArmor_Melee_P6_R0_C2",
            "破甲重击",
            "消耗 2 点能量砸出破甲重击，并将目标从前线击退。",
            6,
            0,
            2,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.18f,
            0.62f,
            0.09f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget),
        new(
            "CrushingBlow_Melee_P7_R0_C2",
            "粉碎重击",
            "消耗 2 点能量打出重型爆发，适合高体型近战强行推线。",
            7,
            0,
            2,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.24f,
            0.64f,
            0.09f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget),
        new(
            "PredatorRush_Melee_P6_R0_C1",
            "猎杀突击",
            "消耗 1 点能量朝目标猛扑，适合高机动近战连续施压。",
            6,
            0,
            1,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyEmber.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.14f,
            0.62f,
            0.08f,
            selfMovementAttribute: HexTacticsSelfMovementAttribute.Advance),
        new(
            "ExecutionDive_Melee_P7_R0_C2",
            "断头突进",
            "消耗 2 点能量先突进贴身，再以重击把目标撞退。",
            7,
            0,
            2,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.28f,
            0.64f,
            0.10f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget,
            selfMovementAttribute: HexTacticsSelfMovementAttribute.Advance),
        new(
            "PowerShot_Ranged_P5_R1_C1",
            "贯穿射击",
            "消耗 1 点能量射出强化弹道，在安全距离上打出更稳定的伤害。",
            5,
            1,
            1,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileFrost.prefab",
            0.92f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit16.prefab",
            1.08f,
            0.62f,
            0.07f),
        new(
            "BackstepShot_Ranged_P4_R1_C1",
            "后撤射击",
            "消耗 1 点能量射击后立即后撤 1 格，是游击单位的保命手段。",
            4,
            1,
            1,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileFrost.prefab",
            0.82f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit16.prefab",
            0.98f,
            0.60f,
            0.06f,
            selfMovementAttribute: HexTacticsSelfMovementAttribute.Retreat),
        new(
            "Snipe_Ranged_P5_R2_C2",
            "狙猎远射",
            "消耗 2 点能量进行更远距离的狙击，是远程单位的重要战术选择。",
            5,
            2,
            2,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileFrost.prefab",
            1.0f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.16f,
            0.66f,
            0.09f),
        new(
            "RepulseBolt_Ranged_P5_R1_C2",
            "斥力法球",
            "消耗 2 点能量发出斥力弹，命中后可将目标震退 1 格。",
            5,
            1,
            2,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileArcane.prefab",
            0.94f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactRangedNova.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.10f,
            0.64f,
            0.07f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget),
        new(
            "ArcBurst_Ranged_P6_R1_C2",
            "奥术爆裂",
            "消耗 2 点能量引爆奥术弹，命中后可将目标震开。",
            6,
            1,
            2,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileArcane.prefab",
            0.98f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.18f,
            0.66f,
            0.09f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget),
        new(
            "ShadowBolt_Ranged_P5_R2_C2",
            "暗蚀弹",
            "消耗 2 点能量发出远程暗蚀法球，兼顾射程与终结能力。",
            5,
            2,
            2,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileArcane.prefab",
            0.92f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.04f,
            0.66f,
            0.08f),
        new(
            "WaveSlash_Ranged_P6_R1_C2",
            "震荡斩波",
            "消耗 2 点能量挥出中距离冲击，可将命中的敌人震退 1 格。",
            6,
            1,
            2,
            0,
            HexTacticsAssetPaths.AttackEffectsVariantFolder + "/HovlProjectileArcane.prefab",
            1.04f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.2f,
            0.66f,
            0.09f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget),
        new(
            "RoyalExecution_Melee_P8_R0_C3",
            "王者处决",
            "消耗 3 点能量突进处决并将目标撞退，是首领级单位的终结手段。",
            8,
            0,
            3,
            0,
            null,
            1f,
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/HovlImpactHeavyCrimson.prefab",
            HexTacticsAssetPaths.HitEffectsVariantFolder + "/ErbHit25.prefab",
            1.36f,
            0.68f,
            0.10f,
            collisionAttribute: HexTacticsCollisionAttribute.PushTarget,
            selfMovementAttribute: HexTacticsSelfMovementAttribute.Advance)
    };

    private static readonly CharacterLoadout[] CharacterLoadouts =
    {
        new(new[] { "Bat", "BattleBee", "MushroomSmile", "Slime" }, 3, 0, "BasicAttack_Melee_P2_R0", "ScoutBurst_Melee_P4_R0_C1"),
        new(new[] { "Spider" }, 4, 0, "BasicAttack_Melee_P3_R0", "VenomStrike_Melee_P4_R0_C1", "ScoutBurst_Melee_P4_R0_C1"),
        new(new[] { "Cactus", "MonsterPlant" }, 4, 0, "BasicAttack_Melee_P3_R0", "ThornShot_Ranged_P4_R1_C1"),
        new(new[] { "StingRay" }, 4, 0, "BasicAttack_Melee_P3_R0", "ThornShot_Ranged_P4_R1_C1", "BackstepShot_Ranged_P4_R1_C1"),
        new(new[] { "MushroomAngry", "Skeleton" }, 4, 0, "BasicAttack_Melee_P3_R0", "SkirmishCombo_Melee_P5_R0_C1"),
        new(new[] { "RatAssassin" }, 4, 0, "BasicAttack_Melee_P4_R0", "ScoutBurst_Melee_P4_R0_C1", "SkirmishCombo_Melee_P5_R0_C1"),
        new(new[] { "BishopKnight", "ChestMonster", "LizardWarrior", "Orc" }, 4, 0, "BasicAttack_Melee_P4_R0", "BulwarkBash_Melee_P5_R0_C1", "BreakArmor_Melee_P6_R0_C2"),
        new(new[] { "Golem", "WormMonster" }, 5, 0, "BasicAttack_Melee_P4_R0", "BreakArmor_Melee_P6_R0_C2", "CrushingBlow_Melee_P7_R0_C2"),
        new(new[] { "CrabMonster", "RockDragon", "TurtleShell" }, 5, 0, "BasicAttack_Melee_P3_R0", "BulwarkBash_Melee_P5_R0_C1", "BreakArmor_Melee_P6_R0_C2"),
        new(new[] { "Fishman" }, 4, 0, "BasicAttack_Melee_P4_R0", "BackstepShot_Ranged_P4_R1_C1", "PowerShot_Ranged_P5_R1_C1"),
        new(new[] { "BlackKnight", "Cyclops" }, 5, 0, "BasicAttack_Melee_P5_R0", "CrushingBlow_Melee_P7_R0_C2", "ExecutionDive_Melee_P7_R0_C2"),
        new(new[] { "NatureDragon" }, 5, 0, "BasicAttack_Melee_P4_R0", "PredatorRush_Melee_P6_R0_C1", "ExecutionDive_Melee_P7_R0_C2"),
        new(new[] { "LavaDragon" }, 5, 1, "BasicAttack_Melee_P5_R0", "WaveSlash_Ranged_P6_R1_C2", "CrushingBlow_Melee_P7_R0_C2"),
        new(new[] { "Werewolf" }, 5, 0, "BasicAttack_Melee_P5_R0", "PredatorRush_Melee_P6_R0_C1", "ScoutBurst_Melee_P4_R0_C1"),
        new(new[] { "Dragon" }, 5, 1, "BasicAttack_Melee_P5_R0", "WaveSlash_Ranged_P6_R1_C2", "ExecutionDive_Melee_P7_R0_C2"),
        new(new[] { "Salamander" }, 5, 0, "BasicAttack_Melee_P5_R0", "WaveSlash_Ranged_P6_R1_C2", "PredatorRush_Melee_P6_R0_C1"),
        new(new[] { "DemonKing" }, 6, 2, "BasicAttack_Melee_P6_R0", "RepulseBolt_Ranged_P5_R1_C2", "WaveSlash_Ranged_P6_R1_C2", "RoyalExecution_Melee_P8_R0_C3"),
        new(new[] { "IceDragon" }, 5, 1, "BasicAttack_Ranged_P4_R1", "RepulseBolt_Ranged_P5_R1_C2", "Snipe_Ranged_P5_R2_C2"),
        new(new[] { "CrystalDragon" }, 5, 1, "BasicAttack_Ranged_P4_R1", "ArcBurst_Ranged_P6_R1_C2", "ShadowBolt_Ranged_P5_R2_C2"),
        new(new[] { "EvilMage", "NagaWizard" }, 5, 1, "BasicAttack_Ranged_P3_R1", "ShadowBolt_Ranged_P5_R2_C2", "RepulseBolt_Ranged_P5_R1_C2"),
        new(new[] { "Beholder" }, 5, 1, "BasicAttack_Ranged_P4_R1", "ShadowBolt_Ranged_P5_R2_C2", "ArcBurst_Ranged_P6_R1_C2"),
        new(new[] { "Specter", "FlyingDemon" }, 5, 1, "BasicAttack_Ranged_P4_R1", "ArcBurst_Ranged_P6_R1_C2", "BackstepShot_Ranged_P4_R1_C1")
    };

    public static void RunBatchMode()
    {
        var exitCode = RunInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/Hex Tactics/Rebuild Skill Balance")]
    public static void RunFromMenu()
    {
        RunInternal();
    }

    private static bool RunInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureFolderExists(HexTacticsAssetPaths.SkillConfigFolder);

        var skillLookup = new Dictionary<string, HexTacticsSkillConfig>();
        foreach (var definition in SkillDefinitions)
        {
            var skill = CreateOrUpdateSkill(definition);
            if (skill == null)
            {
                return false;
            }

            skillLookup[definition.AssetName] = skill;
        }

        foreach (var basicAssetName in new[]
                 {
                     "BasicAttack_Melee_P2_R0",
                     "BasicAttack_Melee_P3_R0",
                     "BasicAttack_Melee_P4_R0",
                     "BasicAttack_Melee_P5_R0",
                     "BasicAttack_Melee_P6_R0",
                     "BasicAttack_Ranged_P3_R1",
                     "BasicAttack_Ranged_P4_R1"
                 })
        {
            var basicSkill = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>($"{HexTacticsAssetPaths.SkillConfigFolder}/{basicAssetName}.asset");
            if (basicSkill == null)
            {
                Debug.LogError("[HexTacticsSkillBalanceRebuild] Missing shared basic skill: " + basicAssetName);
                return false;
            }

            skillLookup[basicAssetName] = basicSkill;
        }

        var configuredCharacters = new HashSet<string>();
        foreach (var loadout in CharacterLoadouts)
        {
            foreach (var characterAssetName in loadout.CharacterAssetNames)
            {
                if (!ApplyLoadout(characterAssetName, loadout, skillLookup))
                {
                    return false;
                }

                configuredCharacters.Add(characterAssetName);
            }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsCharacterConfig", new[] { HexTacticsAssetPaths.CharacterConfigFolder }))
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(assetPath);
            if (config == null)
            {
                continue;
            }

            if (!configuredCharacters.Contains(config.name))
            {
                Debug.LogError("[HexTacticsSkillBalanceRebuild] Character config was not assigned a skill loadout: " + config.name);
                return false;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressablesSync.Sync();
        Debug.Log($"[HexTacticsSkillBalanceRebuild] Rebuilt {SkillDefinitions.Length} special skill assets and updated {configuredCharacters.Count} character configs.");
        return true;
    }

    private static HexTacticsSkillConfig CreateOrUpdateSkill(SkillDefinition definition)
    {
        var assetPath = $"{HexTacticsAssetPaths.SkillConfigFolder}/{definition.AssetName}.asset";
        var skill = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(assetPath);
        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<HexTacticsSkillConfig>();
            skill.name = definition.AssetName;
            AssetDatabase.CreateAsset(skill, assetPath);
        }

        var projectilePrefab = LoadEffectPrefab(definition.ProjectileEffectPath, HexTacticsAssetPaths.RangedWaveEffectAssetPath);
        var impactPrefab = LoadEffectPrefab(definition.ImpactEffectPath, definition.FallbackImpactEffectPath);

        var serializedSkill = new SerializedObject(skill);
        serializedSkill.FindProperty("displayName").stringValue = definition.DisplayName;
        serializedSkill.FindProperty("description").stringValue = definition.Description;
        serializedSkill.FindProperty("power").intValue = definition.Power;
        serializedSkill.FindProperty("attackRange").intValue = definition.AttackRange;
        serializedSkill.FindProperty("energyCost").intValue = definition.EnergyCost;
        serializedSkill.FindProperty("energyGainOnHit").intValue = definition.EnergyGainOnHit;
        serializedSkill.FindProperty("collisionAttribute").enumValueIndex = (int)definition.CollisionAttribute;
        serializedSkill.FindProperty("selfMovementAttribute").enumValueIndex = (int)definition.SelfMovementAttribute;
        serializedSkill.FindProperty("projectileEffectPrefab").objectReferenceValue = definition.AttackRange > 0 ? projectilePrefab : null;
        serializedSkill.FindProperty("projectileEffectScale").floatValue = definition.AttackRange > 0 ? definition.ProjectileEffectScale : 1f;
        serializedSkill.FindProperty("impactEffectPrefab").objectReferenceValue = impactPrefab;
        serializedSkill.FindProperty("impactEffectScale").floatValue = definition.ImpactEffectScale;
        serializedSkill.FindProperty("impactHeightNormalized").floatValue = definition.ImpactHeightNormalized;
        serializedSkill.FindProperty("impactForwardOffset").floatValue = definition.ImpactForwardOffset;
        serializedSkill.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
        return skill;
    }

    private static bool ApplyLoadout(string characterAssetName, CharacterLoadout loadout, Dictionary<string, HexTacticsSkillConfig> skillLookup)
    {
        var assetPath = $"{HexTacticsAssetPaths.CharacterConfigFolder}/{characterAssetName}.asset";
        var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(assetPath);
        if (config == null)
        {
            Debug.LogError("[HexTacticsSkillBalanceRebuild] Could not load character config: " + assetPath);
            return false;
        }

        var serializedConfig = new SerializedObject(config);
        serializedConfig.FindProperty("maxEnergy").intValue = loadout.MaxEnergy;
        serializedConfig.FindProperty("startingEnergy").intValue = Mathf.Clamp(loadout.StartingEnergy, 0, loadout.MaxEnergy);

        var skillsProperty = serializedConfig.FindProperty("skills");
        skillsProperty.arraySize = loadout.SkillAssetNames.Length;
        for (var i = 0; i < loadout.SkillAssetNames.Length; i++)
        {
            if (!skillLookup.TryGetValue(loadout.SkillAssetNames[i], out var skill) || skill == null)
            {
                Debug.LogError("[HexTacticsSkillBalanceRebuild] Missing skill asset for loadout: " + loadout.SkillAssetNames[i]);
                return false;
            }

            skillsProperty.GetArrayElementAtIndex(i).objectReferenceValue = skill;
        }

        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        return true;
    }

    private static GameObject LoadEffectPrefab(string preferredPath, string fallbackPath)
    {
        if (!string.IsNullOrWhiteSpace(preferredPath))
        {
            var preferredPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(preferredPath);
            if (preferredPrefab != null)
            {
                return preferredPrefab;
            }
        }

        return string.IsNullOrWhiteSpace(fallbackPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var segments = folderPath.Split('/');
        var currentPath = segments[0];
        for (var i = 1; i < segments.Length; i++)
        {
            var nextPath = currentPath + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }

    private readonly struct SkillDefinition
    {
        public SkillDefinition(
            string assetName,
            string displayName,
            string description,
            int power,
            int attackRange,
            int energyCost,
            int energyGainOnHit,
            string projectileEffectPath,
            float projectileEffectScale,
            string impactEffectPath,
            string fallbackImpactEffectPath,
            float impactEffectScale,
            float impactHeightNormalized,
            float impactForwardOffset,
            HexTacticsCollisionAttribute collisionAttribute = HexTacticsCollisionAttribute.None,
            HexTacticsSelfMovementAttribute selfMovementAttribute = HexTacticsSelfMovementAttribute.None)
        {
            AssetName = assetName;
            DisplayName = displayName;
            Description = description;
            Power = power;
            AttackRange = attackRange;
            EnergyCost = energyCost;
            EnergyGainOnHit = energyGainOnHit;
            ProjectileEffectPath = projectileEffectPath;
            ProjectileEffectScale = projectileEffectScale;
            ImpactEffectPath = impactEffectPath;
            FallbackImpactEffectPath = fallbackImpactEffectPath;
            ImpactEffectScale = impactEffectScale;
            ImpactHeightNormalized = impactHeightNormalized;
            ImpactForwardOffset = impactForwardOffset;
            CollisionAttribute = collisionAttribute;
            SelfMovementAttribute = selfMovementAttribute;
        }

        public string AssetName { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Power { get; }
        public int AttackRange { get; }
        public int EnergyCost { get; }
        public int EnergyGainOnHit { get; }
        public string ProjectileEffectPath { get; }
        public float ProjectileEffectScale { get; }
        public string ImpactEffectPath { get; }
        public string FallbackImpactEffectPath { get; }
        public float ImpactEffectScale { get; }
        public float ImpactHeightNormalized { get; }
        public float ImpactForwardOffset { get; }
        public HexTacticsCollisionAttribute CollisionAttribute { get; }
        public HexTacticsSelfMovementAttribute SelfMovementAttribute { get; }
    }

    private readonly struct CharacterLoadout
    {
        public CharacterLoadout(string[] characterAssetNames, int maxEnergy, int startingEnergy, params string[] skillAssetNames)
        {
            CharacterAssetNames = characterAssetNames;
            MaxEnergy = maxEnergy;
            StartingEnergy = startingEnergy;
            SkillAssetNames = skillAssetNames;
        }

        public string[] CharacterAssetNames { get; }
        public int MaxEnergy { get; }
        public int StartingEnergy { get; }
        public string[] SkillAssetNames { get; }
    }
}
