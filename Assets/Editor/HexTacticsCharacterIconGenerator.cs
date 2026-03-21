using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexTacticsCharacterIconGenerator
{
    private const int IconSize = 256;

    [MenuItem("Tools/Hex Tactics/Generate Character Icons")]
    public static void RunFromMenu()
    {
        RunInternal(exitOnComplete: false);
    }

    public static void RunBatchMode()
    {
        RunInternal(exitOnComplete: true);
    }

    public static bool GenerateIconForConfig(HexTacticsCharacterConfig config, List<string> issues)
    {
        EnsureFolder(HexTacticsAssetPaths.CharacterIconFolder);
        HexTacticsCharacterThumbnailStudio.EnsureSceneAsset();

        using var studio = new HexTacticsCharacterThumbnailStudio();
        return GenerateIconForConfig(config, issues, studio);
    }

    public static int GenerateIconsForConfigs(IEnumerable<HexTacticsCharacterConfig> configs, List<string> issues)
    {
        var configPaths = new List<string>();
        foreach (var config in configs)
        {
            if (config == null)
            {
                issues?.Add("Character icon generation received a null config in a batch.");
                continue;
            }

            configPaths.Add(AssetDatabase.GetAssetPath(config));
        }

        return GenerateIconsForConfigPaths(configPaths, issues);
    }

    public static int GenerateIconsForConfigPaths(IEnumerable<string> configPaths, List<string> issues)
    {
        EnsureFolder(HexTacticsAssetPaths.CharacterIconFolder);
        HexTacticsCharacterThumbnailStudio.EnsureSceneAsset();

        var generatedCount = 0;
        var processedAssetPaths = new HashSet<string>();
        using var studio = new HexTacticsCharacterThumbnailStudio();

        foreach (var configPath in configPaths)
        {
            var normalizedPath = configPath?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                issues?.Add("Character icon generation received an empty config path in a batch.");
                continue;
            }

            if (!processedAssetPaths.Add(normalizedPath))
            {
                continue;
            }

            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(normalizedPath);
            if (config == null)
            {
                issues?.Add($"Character icon generation could not reload config: {normalizedPath}");
                continue;
            }

            if (GenerateIconForConfig(config, issues, studio))
            {
                generatedCount++;
            }
        }

        return generatedCount;
    }

    private static bool GenerateIconForConfig(
        HexTacticsCharacterConfig config,
        List<string> issues,
        HexTacticsCharacterThumbnailStudio studio)
    {
        if (config == null)
        {
            issues?.Add("Character icon generation received a null config.");
            return false;
        }

        var prefab = ResolvePreviewPrefab(config);
        if (prefab == null)
        {
            issues?.Add($"Character icon generation could not resolve a preview prefab for '{config.name}'.");
            return false;
        }

        var texture = studio.Capture(prefab, IconSize, ResolveThumbnailYaw(config), issues);
        if (texture == null)
        {
            issues?.Add($"Character icon generation failed to render '{config.name}'.");
            return false;
        }

        if (IsNearUniformCapture(texture))
        {
            issues?.Add($"Character icon generation produced an empty-looking capture for '{config.name}'.");
            Object.DestroyImmediate(texture);
            return false;
        }

        try
        {
            var iconAssetPath = GetIconAssetPath(config);
            EnsureFolder(Path.GetDirectoryName(iconAssetPath)?.Replace('\\', '/'));
            File.WriteAllBytes(GetAbsoluteProjectPath(iconAssetPath), texture.EncodeToPNG());

            AssetDatabase.ImportAsset(iconAssetPath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(iconAssetPath);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
            if (sprite == null)
            {
                issues?.Add($"Generated icon sprite could not be loaded: {iconAssetPath}");
                return false;
            }

            var serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("avatar").objectReferenceValue = sprite;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return true;
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void RunInternal(bool exitOnComplete)
    {
        var issues = new List<string>();
        var configs = new List<HexTacticsCharacterConfig>();

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsCharacterConfig", new[] { HexTacticsAssetPaths.CharacterConfigFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(path);
            if (config == null)
            {
                issues.Add($"Character config could not be loaded for icon generation: {path}");
                continue;
            }

            configs.Add(config);
        }

        var generatedCount = GenerateIconsForConfigs(configs, issues);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        if (issues.Count == 0)
        {
            Debug.Log($"[HexTacticsCharacterIconGenerator] Generated or updated {generatedCount} character icons.");
            ExitIfNeeded(exitOnComplete, 0);
            return;
        }

        foreach (var issue in issues)
        {
            Debug.LogError("[HexTacticsCharacterIconGenerator] " + issue);
        }

        Debug.LogError($"[HexTacticsCharacterIconGenerator] Character icon generation finished with {issues.Count} issue(s).");
        ExitIfNeeded(exitOnComplete, 1);
    }

    private static bool IsNearUniformCapture(Texture2D texture)
    {
        if (texture == null)
        {
            return true;
        }

        var pixels = texture.GetPixels32();
        if (pixels == null || pixels.Length == 0)
        {
            return true;
        }

        var minR = 255;
        var minG = 255;
        var minB = 255;
        var maxR = 0;
        var maxG = 0;
        var maxB = 0;

        for (var i = 0; i < pixels.Length; i += 8)
        {
            var pixel = pixels[i];
            minR = Mathf.Min(minR, pixel.r);
            minG = Mathf.Min(minG, pixel.g);
            minB = Mathf.Min(minB, pixel.b);
            maxR = Mathf.Max(maxR, pixel.r);
            maxG = Mathf.Max(maxG, pixel.g);
            maxB = Mathf.Max(maxB, pixel.b);
        }

        const int minimumColorRange = 10;
        return maxR - minR < minimumColorRange &&
               maxG - minG < minimumColorRange &&
               maxB - minB < minimumColorRange;
    }

    private static GameObject ResolvePreviewPrefab(HexTacticsCharacterConfig config)
    {
        if (config == null)
        {
            return null;
        }

        if (config.BattleUnitPrefab != null)
        {
            return config.BattleUnitPrefab;
        }

        return config.VisualArchetype switch
        {
            HexTacticsCharacterVisualArchetype.Stag => AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.BattleUnitFolder + "/Stag.prefab"),
            HexTacticsCharacterVisualArchetype.Doe => AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.BattleUnitFolder + "/Doe.prefab"),
            HexTacticsCharacterVisualArchetype.Elk => AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.BattleUnitFolder + "/Elk.prefab"),
            HexTacticsCharacterVisualArchetype.Fawn => AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.BattleUnitFolder + "/Fawn.prefab"),
            HexTacticsCharacterVisualArchetype.Tiger => AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.BattleUnitFolder + "/Tiger.prefab"),
            HexTacticsCharacterVisualArchetype.WhiteTiger => AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.BattleUnitFolder + "/WhiteTiger.prefab"),
            _ => null
        };
    }

    private static float ResolveThumbnailYaw(HexTacticsCharacterConfig config)
    {
        return config != null && config.BattleUnitPrefab != null ? 180f : 0f;
    }

    private static void ConfigureTextureImporter(string assetPath)
    {
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static string GetIconAssetPath(HexTacticsCharacterConfig config)
    {
        var configAssetPath = AssetDatabase.GetAssetPath(config).Replace('\\', '/');
        var fileName = Path.GetFileNameWithoutExtension(configAssetPath) + "Icon.png";
        var configRootPrefix = HexTacticsAssetPaths.CharacterConfigFolder + "/";
        var relativeDirectory = string.Empty;

        if (configAssetPath.StartsWith(configRootPrefix))
        {
            relativeDirectory = Path.GetDirectoryName(configAssetPath.Substring(configRootPrefix.Length))?.Replace('\\', '/') ?? string.Empty;
        }

        return string.IsNullOrEmpty(relativeDirectory)
            ? $"{HexTacticsAssetPaths.CharacterIconFolder}/{fileName}"
            : $"{HexTacticsAssetPaths.CharacterIconFolder}/{relativeDirectory}/{fileName}";
    }

    private static string GetAbsoluteProjectPath(string assetPath)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot ?? string.Empty, assetPath);
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (string.IsNullOrWhiteSpace(assetFolderPath) || AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return;
        }

        var normalizedPath = assetFolderPath.Replace('\\', '/');
        var parentPath = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return;
        }

        EnsureFolder(parentPath);
        AssetDatabase.CreateFolder(parentPath, Path.GetFileName(normalizedPath));
    }

    private static void ExitIfNeeded(bool exitOnComplete, int exitCode)
    {
        if (exitOnComplete && Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }
}
