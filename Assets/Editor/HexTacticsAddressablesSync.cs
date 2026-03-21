using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class HexTacticsAddressablesSync
{
    private const string CharacterConfigGroupName = "HexTactics Character Configs";
    private const string SkillConfigGroupName = "HexTactics Skill Configs";
    private const string BattleUnitGroupName = "HexTactics Battle Units";
    private const string EffectsGroupName = "HexTactics Effects";
    private const string UiGroupName = "HexTactics UI";

    public static void RunBatchMode()
    {
        var exitCode = SyncInternal(buildPlayerContent: true) ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/Hex Tactics/Sync Addressables")]
    public static void Sync()
    {
        SyncInternal(buildPlayerContent: false);
    }

    [MenuItem("Tools/Hex Tactics/Build Addressables Content")]
    public static void SyncAndBuild()
    {
        SyncInternal(buildPlayerContent: true);
    }

    private static bool SyncInternal(bool buildPlayerContent)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
        {
            Debug.LogError("[HexTacticsAddressablesSync] Could not create Addressables settings.");
            return false;
        }

        var characterConfigsGroup = GetOrCreateLocalGroup(settings, CharacterConfigGroupName);
        var skillConfigsGroup = GetOrCreateLocalGroup(settings, SkillConfigGroupName);
        var battleUnitsGroup = GetOrCreateLocalGroup(settings, BattleUnitGroupName);
        var effectsGroup = GetOrCreateLocalGroup(settings, EffectsGroupName);
        var uiGroup = GetOrCreateLocalGroup(settings, UiGroupName);

        SyncFolder(settings, HexTacticsAssetPaths.CharacterConfigFolder, "t:HexTacticsCharacterConfig", characterConfigsGroup, HexTacticsAssetPaths.CharacterConfigsLabel);
        SyncFolder(settings, HexTacticsAssetPaths.SkillConfigFolder, "t:HexTacticsSkillConfig", skillConfigsGroup, HexTacticsAssetPaths.SkillConfigsLabel);
        SyncFolder(settings, HexTacticsAssetPaths.BattleUnitFolder, "t:Prefab", battleUnitsGroup, HexTacticsAssetPaths.BattleUnitsLabel);
        SyncFolder(settings, HexTacticsAssetPaths.AttackEffectsFolder, "t:Prefab", effectsGroup, HexTacticsAssetPaths.EffectsLabel);
        SyncFolder(settings, HexTacticsAssetPaths.HitEffectsFolder, "t:Prefab", effectsGroup, HexTacticsAssetPaths.EffectsLabel);
        SyncFolder(settings, HexTacticsAssetPaths.HitEffectsFolder, "t:HexTacticsHitEffectCatalog", effectsGroup, HexTacticsAssetPaths.EffectsLabel);
        SyncFolder(settings, HexTacticsAssetPaths.UiFolder, "t:Prefab", uiGroup, HexTacticsAssetPaths.UiLabel);
        SyncAssetList(settings, HexTacticsModernUiSkin.RequiredSpriteAssetPaths, uiGroup, HexTacticsAssetPaths.UiLabel);

        if (settings.DefaultGroup == null)
        {
            settings.DefaultGroup = uiGroup;
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        if (!buildPlayerContent)
        {
            Debug.Log("[HexTacticsAddressablesSync] Addressables settings synced.");
            return true;
        }

        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            Debug.LogError("[HexTacticsAddressablesSync] Addressables build failed: " + result.Error);
            return false;
        }

        Debug.Log("[HexTacticsAddressablesSync] Addressables settings synced and content built.");
        return true;
    }

    private static AddressableAssetGroup GetOrCreateLocalGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            group = settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));
        }

        var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
        if (bundledSchema != null)
        {
            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            bundledSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bundledSchema.UseUnityWebRequestForLocalBundles = false;
            EditorUtility.SetDirty(bundledSchema);
        }

        var contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
        if (contentUpdateSchema != null)
        {
            contentUpdateSchema.StaticContent = false;
            EditorUtility.SetDirty(contentUpdateSchema);
        }

        EditorUtility.SetDirty(group);
        return group;
    }

    private static void SyncFolder(
        AddressableAssetSettings settings,
        string folderPath,
        string filter,
        AddressableAssetGroup group,
        string label)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        foreach (var guid in AssetDatabase.FindAssets(filter, new[] { folderPath }))
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                continue;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                continue;
            }

            entry.SetAddress(ToAddress(assetPath), false);
            entry.SetLabel(label, true, true, false);
            EditorUtility.SetDirty(entry.parentGroup);
        }
    }

    private static void SyncAssetList(
        AddressableAssetSettings settings,
        IReadOnlyList<string> assetPaths,
        AddressableAssetGroup group,
        string label)
    {
        if (assetPaths == null)
        {
            return;
        }

        for (var i = 0; i < assetPaths.Count; i++)
        {
            var assetPath = assetPaths[i];
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                continue;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                continue;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                continue;
            }

            entry.SetAddress(assetPath, false);
            entry.SetLabel(label, true, true, false);
            EditorUtility.SetDirty(entry.parentGroup);
        }
    }

    private static string ToAddress(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith(HexTacticsAssetPaths.AddressablesRoot + "/", StringComparison.Ordinal))
        {
            return assetPath;
        }

        var relativePath = assetPath.Substring(HexTacticsAssetPaths.AddressablesRoot.Length + 1);
        var extension = System.IO.Path.GetExtension(relativePath);
        return string.IsNullOrEmpty(extension)
            ? relativePath
            : relativePath.Substring(0, relativePath.Length - extension.Length);
    }
}
