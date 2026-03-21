using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class RPGMonsterBundlePolyartValidator
{
    private const string BundleRoot = "Assets/RPGMonsterBundlePolyart";
    private const string CommonStuffsRoot = BundleRoot + "/CommonStuffs";
    private const string MaterialRoot = CommonStuffsRoot + "/Materials";
    private const string PrefabRoot = CommonStuffsRoot + "/Prefab";
    private const string SceneRoot = CommonStuffsRoot + "/Scene";
    private const string CharacterConfigRoot = HexTacticsAssetPaths.CharacterConfigFolder;

    public static void RunBatchMode()
    {
        var exitCode = RunInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/RPG Monster Bundle Polyart/Validate URP Assets")]
    public static void RunFromMenu()
    {
        RunInternal();
    }

    private static bool RunInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var issues = new List<string>();

        ValidateMaterials(issues);
        ValidatePrefabs(issues);
        ValidateScenes(issues);
        ValidateHexTacticsUi(issues);
        ValidateHexTacticsCharacterConfigs(issues);

        if (issues.Count == 0)
        {
            Debug.Log("[RPGMonsterBundlePolyartValidator] Validation passed.");
            return true;
        }

        foreach (var issue in issues)
        {
            Debug.LogError("[RPGMonsterBundlePolyartValidator] " + issue);
        }

        Debug.LogError($"[RPGMonsterBundlePolyartValidator] Validation failed with {issues.Count} issue(s).");
        return false;
    }

    private static void ValidateMaterials(List<string> issues)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                issues.Add($"Material could not be loaded: {path}");
                continue;
            }

            if (material.shader == null)
            {
                issues.Add($"Material shader is missing: {path}");
                continue;
            }

            var fileName = Path.GetFileName(path);
            if ((fileName == "PolyartDefault.mat" || fileName == "Stage.mat") &&
                material.shader.name != "Universal Render Pipeline/Lit")
            {
                issues.Add($"Material is not using URP/Lit: {path} -> {material.shader.name}");
            }

            if (fileName.StartsWith("PAMaskTint") &&
                !material.shader.name.Contains("URPMasktTintPA"))
            {
                issues.Add($"Mask tint material is not using the URP shader graph: {path} -> {material.shader.name}");
            }
        }
    }

    private static void ValidatePrefabs(List<string> issues)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                ValidateHierarchy(path, root, issues);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ValidateScenes(List<string> issues)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
            {
                ValidateHierarchy(path, root, issues);
            }
        }
    }

    private static void ValidateHierarchy(string assetPath, GameObject root, List<string> issues)
    {
        HexTacticsHierarchyAuditUtility.ValidateHierarchy(assetPath, root, issues);
    }

    private static void ValidateHexTacticsUi(List<string> issues)
    {
        HexTacticsCanvasView view = null;
        EventSystem eventSystem = null;

        try
        {
            view = HexTacticsCanvasBootstrap.EnsureView();
            if (view == null)
            {
                issues.Add("Hex Tactics UI bootstrap did not create a canvas view.");
                return;
            }

            view.EnsureBuilt();
            if (view.GetComponent<Canvas>() == null)
            {
                issues.Add("Hex Tactics UI canvas root is missing its Canvas component.");
            }

            eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                issues.Add("Hex Tactics UI bootstrap did not create an EventSystem.");
            }
        }
        finally
        {
            if (view != null)
            {
                Object.DestroyImmediate(view.gameObject);
            }

            if (eventSystem != null)
            {
                Object.DestroyImmediate(eventSystem.gameObject);
            }
        }
    }

    private static void ValidateHexTacticsCharacterConfigs(List<string> issues)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsCharacterConfig", new[] { CharacterConfigRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(path);
            if (config == null)
            {
                issues.Add($"Character config could not be loaded: {path}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(config.DisplayName))
            {
                issues.Add($"Character config is missing a display name: {path}");
            }

            if (!config.HasAssignedSkills)
            {
                issues.Add($"Character config is missing assigned skill assets: {path}");
            }

            if (config.MaxHealth < 1 ||
                config.PreviewAttackPower < 1 ||
                config.Cost < 1 ||
                config.MoveRange < 1 ||
                config.Speed < 1 ||
                config.MaxEnergy < 1)
            {
                issues.Add($"Character config has invalid combat stats: {path}");
            }

            if (config.VisualHeightScale < 0.4f)
            {
                issues.Add($"Character config has an invalid visual height scale: {path}");
            }

            if (config.Avatar == null)
            {
                issues.Add($"Character config is missing a square avatar icon: {path}");
            }
            else if (!Mathf.Approximately(config.Avatar.rect.width, config.Avatar.rect.height))
            {
                issues.Add($"Character config avatar icon is not square: {path}");
            }

            var assetName = Path.GetFileNameWithoutExtension(path);
            var requiresDirectPrefab = HexTacticsRpgMonsterRosterImporter.IsImportedMonsterAssetName(assetName);
            if (requiresDirectPrefab && config.BattleUnitPrefab == null)
            {
                issues.Add($"Imported RPG monster config is missing its battle prefab reference: {path}");
            }

            var skills = config.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                if (skill == null)
                {
                    issues.Add($"Character config contains a null skill reference: {path}");
                    continue;
                }

                if (skill.Power < 1)
                {
                    issues.Add($"Skill has invalid power: {path} -> {skill.name}");
                }

                if (skill.RequiresDedicatedRangedEffects &&
                    (!skill.HasProjectileEffect || !skill.HasImpactEffect))
                {
                    issues.Add($"Ranged skill is missing projectile or impact effect: {path} -> {skill.name}");
                }
            }
        }
    }

}
