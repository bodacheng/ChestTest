using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HexTacticsRuntimeAssetValidator
{
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
        ValidateAsset<GameObject>("BattleUnits/Stag", "Battle Unit Stag", errors);
        ValidateAsset<GameObject>("BattleUnits/Doe", "Battle Unit Doe", errors);
        ValidateAsset<GameObject>("BattleUnits/Elk", "Battle Unit Elk", errors);
        ValidateAsset<GameObject>("BattleUnits/Fawn", "Battle Unit Fawn", errors);
        ValidateAsset<GameObject>("BattleUnits/Tiger", "Battle Unit Tiger", errors);
        ValidateAsset<GameObject>("BattleUnits/WhiteTiger", "Battle Unit WhiteTiger", errors);
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

    private static void ValidateEffectPrefab(GameObject prefab, string label, List<string> errors)
    {
        var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
        var trailRenderers = prefab.GetComponentsInChildren<TrailRenderer>(true);
        if (particleSystems.Length == 0 && trailRenderers.Length == 0)
        {
            errors.Add($"Hit effect '{label}' has no particle or trail renderers.");
        }
    }

    private static void ValidateAsset<T>(string address, string label, List<string> errors) where T : Object
    {
        if (HexTacticsAddressables.LoadAsset<T>(address) == null)
        {
            errors.Add($"{label} could not be loaded from '{address}'.");
        }
    }

    private static void ExitIfNeeded(bool exitOnComplete, int exitCode)
    {
        if (exitOnComplete && Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }
}
