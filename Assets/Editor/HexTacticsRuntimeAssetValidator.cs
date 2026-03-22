using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class HexTacticsRuntimeAssetValidator
{
    private const string UrpShaderPrefix = "Universal Render Pipeline/";

    [MenuItem("Tools/Hex Tactics/Validate Runtime Asset Loading")]
    public static void Validate()
    {
        RunInternal(exitOnComplete: false);
    }

    public static void RunBatchMode()
    {
        RunInternal(exitOnComplete: true);
    }

    private static void RunInternal(bool exitOnComplete)
    {
        var errors = new List<string>();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressables.ReleaseAll();

        ValidateUiAssets(errors);
        ValidateBattleUnitAssets(errors);
        ValidateCharacterConfigs(errors);
        ValidateSkillConfigs(errors);
        ValidateAttackEffects(errors);
        ValidateHitEffects(errors);

        HexTacticsAddressables.ReleaseAll();

        if (errors.Count == 0)
        {
            Debug.Log("[HexTacticsRuntimeAssetValidator] Runtime asset loading validated successfully.");
            ExitIfNeeded(exitOnComplete, 0);
            return;
        }

        foreach (var error in errors)
        {
            Debug.LogError("[HexTacticsRuntimeAssetValidator] " + error);
        }

        ExitIfNeeded(exitOnComplete, 1);
    }

    private static void ValidateUiAssets(List<string> errors)
    {
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.CanvasRoot, "UI Canvas Root", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.ModeSelectScreen, "UI Mode Select Screen", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.TeamBuilderScreen, "UI Team Builder Screen", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.PlanningScreen, "UI Planning Screen", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.ResolvingScreen, "UI Resolving Screen", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.VictoryOverlay, "UI Victory Overlay", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.RosterRow, "UI Roster Row", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.SelectedRosterRow, "UI Selected Roster Row", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.CommandRow, "UI Command Row", errors);
        ValidateAsset<GameObject>(HexTacticsUiResourcePaths.WorldLabel, "UI World Label", errors);
    }

    private static void ValidateBattleUnitAssets(List<string> errors)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { HexTacticsAssetPaths.BattleUnitFolder });
        if (prefabGuids == null || prefabGuids.Length == 0)
        {
            errors.Add("No battle unit prefabs could be found.");
            return;
        }

        foreach (var guid in prefabGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                continue;
            }

            var address = ToAddress(assetPath);
            var label = "Battle Unit " + Path.GetFileNameWithoutExtension(assetPath);
            var prefab = HexTacticsAddressables.LoadAsset<GameObject>(address);
            if (prefab == null)
            {
                errors.Add($"{label} could not be loaded from '{address}'.");
                continue;
            }

            ValidateBattleUnitPrefab(prefab, label, errors);
        }
    }

    private static void ValidateCharacterConfigs(List<string> errors)
    {
        var configs = HexTacticsAddressables.LoadCharacterConfigs();
        if (configs.Count == 0)
        {
            errors.Add("No character configs could be loaded.");
            return;
        }

        for (var i = 0; i < configs.Count; i++)
        {
            if (configs[i] == null)
            {
                errors.Add($"Character config at index {i} is null.");
                continue;
            }

            if (configs[i].Avatar == null)
            {
                errors.Add($"Character config '{configs[i].name}' is missing its avatar icon.");
            }
            else if (!Mathf.Approximately(configs[i].Avatar.rect.width, configs[i].Avatar.rect.height))
            {
                errors.Add($"Character config '{configs[i].name}' avatar icon is not square.");
            }

            ValidateCharacterAttackAnimationCoverage(configs[i], errors);
        }
    }

    private static void ValidateSkillConfigs(List<string> errors)
    {
        var skills = HexTacticsAddressables.LoadSkillConfigs();
        if (skills.Count == 0)
        {
            errors.Add("No skill configs could be loaded.");
            return;
        }

        for (var i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill == null)
            {
                errors.Add($"Skill config at index {i} is null.");
                continue;
            }

            if (skill.Power < 1)
            {
                errors.Add($"Skill config '{skill.name}' has invalid power.");
            }

            if (skill.RequiresDedicatedRangedEffects &&
                (!skill.HasProjectileEffect || !skill.HasImpactEffect))
            {
                errors.Add($"Ranged skill config '{skill.name}' is missing projectile or impact effect.");
            }
        }
    }

    private static void ValidateHitEffects(List<string> errors)
    {
        var catalog = HexTacticsHitEffectCatalog.LoadDefault();
        if (catalog == null)
        {
            errors.Add("Hit effect catalog could not be loaded.");
            return;
        }

        if (!catalog.TryResolveAutoEffect(2, 0, out var lightEntry) || lightEntry?.Prefab == null)
        {
            errors.Add("Light auto-selected hit effect could not be resolved.");
        }
        else
        {
            ValidateEffectPrefab(lightEntry.Prefab, lightEntry.DisplayName, errors);
        }

        if (!catalog.TryResolveAutoEffect(4, 0, out var mediumEntry) || mediumEntry?.Prefab == null)
        {
            errors.Add("Medium auto-selected hit effect could not be resolved.");
        }
        else
        {
            ValidateEffectPrefab(mediumEntry.Prefab, mediumEntry.DisplayName, errors);
        }

        if (!catalog.TryResolveAutoEffect(6, 1, out var heavyEntry) || heavyEntry?.Prefab == null)
        {
            errors.Add("Heavy auto-selected hit effect could not be resolved.");
            return;
        }

        ValidateEffectPrefab(heavyEntry.Prefab, heavyEntry.DisplayName, errors);
    }

    private static void ValidateAttackEffects(List<string> errors)
    {
        var rangedWavePrefab = HexTacticsAddressables.LoadAsset<GameObject>(HexTacticsAssetPaths.RangedWaveEffectAddress);
        if (rangedWavePrefab == null)
        {
            errors.Add("Ranged wave attack effect could not be loaded.");
            return;
        }

        ValidateEffectPrefab(rangedWavePrefab, "Ranged Wave", errors);
    }

    private static void ValidateEffectPrefab(GameObject prefab, string label, List<string> errors)
    {
        var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
        var trailRenderers = prefab.GetComponentsInChildren<TrailRenderer>(true);
        if (particleSystems.Length == 0 && trailRenderers.Length == 0)
        {
            errors.Add($"Hit effect '{label}' has no particle or trail renderers.");
        }
    }

    private static void ValidateBattleUnitPrefab(GameObject prefab, string label, List<string> errors)
    {
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            errors.Add($"{label} has no renderers.");
            return;
        }

        var usesScriptableRenderPipeline = GraphicsSettings.defaultRenderPipeline != null;
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                errors.Add($"{label} renderer '{renderer.name}' has no shared materials.");
                continue;
            }

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null)
                {
                    errors.Add($"{label} renderer '{renderer.name}' has a missing material at slot {i}.");
                    continue;
                }

                if (material.shader == null)
                {
                    errors.Add($"{label} material '{material.name}' has a missing shader.");
                    continue;
                }

                if (usesScriptableRenderPipeline && UsesBuiltinShader(material.shader))
                {
                    errors.Add($"{label} material '{material.name}' is still using built-in shader '{material.shader.name}' in a SRP project.");
                }
            }
        }
    }

    private static void ValidateCharacterAttackAnimationCoverage(HexTacticsCharacterConfig config, List<string> errors)
    {
        if (config == null || config.SkillCount <= 0 || config.BattleUnitPrefab == null)
        {
            return;
        }

        var animator = config.BattleUnitPrefab.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            errors.Add($"Character config '{config.name}' battle prefab '{config.BattleUnitPrefab.name}' is missing an Animator.");
            return;
        }

        var controller = animator.runtimeAnimatorController;
        if (controller == null)
        {
            errors.Add($"Character config '{config.name}' battle prefab '{config.BattleUnitPrefab.name}' is missing a runtime animator controller.");
            return;
        }

        var hasMeleeSkill = false;
        var hasRangedSkill = false;
        foreach (var skill in config.Skills)
        {
            if (skill == null)
            {
                continue;
            }

            if (skill.AttackRange > 0)
            {
                hasRangedSkill = true;
            }
            else
            {
                hasMeleeSkill = true;
            }
        }

        var neutralAttackCount = 0;
        var meleeAttackCount = 0;
        var rangedAttackCount = 0;
        foreach (var clip in controller.animationClips)
        {
            if (!IsAttackAnimationClip(clip))
            {
                continue;
            }

            switch (ClassifyAttackAnimationFlavor(clip.name))
            {
                case AttackAnimationFlavor.Melee:
                    meleeAttackCount++;
                    break;
                case AttackAnimationFlavor.Ranged:
                    rangedAttackCount++;
                    break;
                default:
                    neutralAttackCount++;
                    break;
            }
        }

        if (neutralAttackCount == 0 && meleeAttackCount == 0 && rangedAttackCount == 0)
        {
            errors.Add($"Character config '{config.name}' battle prefab '{config.BattleUnitPrefab.name}' has no recognizable attack animation clips.");
            return;
        }

        if (hasMeleeSkill && meleeAttackCount == 0 && neutralAttackCount == 0)
        {
            errors.Add($"Character config '{config.name}' has melee skills but no melee-capable attack animation clips.");
        }

        if (hasRangedSkill && rangedAttackCount == 0 && neutralAttackCount == 0)
        {
            errors.Add($"Character config '{config.name}' has ranged skills but no ranged-capable attack animation clips.");
        }

        if (config.SkillCount > 1 && hasMeleeSkill && hasRangedSkill)
        {
            if (meleeAttackCount == 0 && neutralAttackCount > 0)
            {
                Debug.LogWarning($"[HexTacticsRuntimeAssetValidator] Character config '{config.name}' mixes melee and ranged skills, but only neutral/non-melee attack clip names were found. Neutral attack clips will be reused for melee skills.");
            }

            if (rangedAttackCount == 0 && neutralAttackCount > 0)
            {
                Debug.LogWarning($"[HexTacticsRuntimeAssetValidator] Character config '{config.name}' mixes melee and ranged skills, but only neutral/non-ranged attack clip names were found. Neutral attack clips will be reused for ranged skills.");
            }
        }
    }

    private static bool UsesBuiltinShader(Shader shader)
    {
        if (shader == null)
        {
            return false;
        }

        return !shader.name.StartsWith(UrpShaderPrefix) &&
            (shader.name == "Standard" || shader.name.StartsWith("Legacy Shaders/"));
    }

    private static void ValidateAsset<T>(string address, string label, List<string> errors) where T : Object
    {
        if (HexTacticsAddressables.LoadAsset<T>(address) == null)
        {
            errors.Add($"{label} could not be loaded from '{address}'.");
        }
    }

    private static bool IsAttackAnimationClip(AnimationClip clip)
    {
        if (clip == null)
        {
            return false;
        }

        var normalizedName = NormalizeAnimationName(clip.name);
        return normalizedName.Contains("attack") ||
            normalizedName.Contains("shoot") ||
            normalizedName.Contains("shot") ||
            normalizedName.Contains("cast") ||
            normalizedName.Contains("breath") ||
            normalizedName.Contains("fireball") ||
            normalizedName.Contains("spit") ||
            normalizedName.Contains("slash") ||
            normalizedName.Contains("bite") ||
            normalizedName.Contains("claw") ||
            normalizedName.Contains("tail") ||
            normalizedName.Contains("wing");
    }

    private static AttackAnimationFlavor ClassifyAttackAnimationFlavor(string clipName)
    {
        var normalizedName = NormalizeAnimationName(clipName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return AttackAnimationFlavor.Neutral;
        }

        var rangedScore = 0;
        if (ContainsAny(normalizedName, "rpt", "shoot", "shot", "bolt", "projectile", "missile", "beam", "laser", "throw", "toss"))
        {
            rangedScore += 4;
        }

        if (ContainsAny(normalizedName, "fireball", "firebreath", "breath", "spit", "cast", "magic", "orb", "ball", "wave"))
        {
            rangedScore += 6;
        }

        var meleeScore = 0;
        if (ContainsAny(normalizedName, "bite", "claw", "slash", "tail", "wing", "paw", "stomp", "stump"))
        {
            meleeScore += 6;
        }

        if (ContainsAny(normalizedName, "bash", "strike", "swing", "rush", "dive", "stab", "punch", "kick"))
        {
            meleeScore += 4;
        }

        if (rangedScore > meleeScore && rangedScore > 0)
        {
            return AttackAnimationFlavor.Ranged;
        }

        if (meleeScore > 0)
        {
            return AttackAnimationFlavor.Melee;
        }

        return AttackAnimationFlavor.Neutral;
    }

    private static string NormalizeAnimationName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
    }

    private static bool ContainsAny(string normalizedName, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(normalizedName) || keywords == null)
        {
            return false;
        }

        foreach (var keyword in keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) && normalizedName.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private static void ExitIfNeeded(bool exitOnComplete, int exitCode)
    {
        if (exitOnComplete && Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    private static string ToAddress(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith(HexTacticsAssetPaths.AddressablesRoot + "/"))
        {
            return assetPath;
        }

        var relativePath = assetPath.Substring(HexTacticsAssetPaths.AddressablesRoot.Length + 1);
        var extension = Path.GetExtension(relativePath);
        return string.IsNullOrEmpty(extension)
            ? relativePath
            : relativePath.Substring(0, relativePath.Length - extension.Length);
    }

    private enum AttackAnimationFlavor
    {
        Neutral,
        Melee,
        Ranged
    }
}
