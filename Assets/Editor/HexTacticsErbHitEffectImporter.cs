using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

public static class HexTacticsErbHitEffectImporter
{
    private const string SourceHitEffectsFolder = "Assets/ErbGameArt/AAA Projectiles/Prefabs/Hits";
    private const string TargetShaderName = "Universal Render Pipeline/Particles/Unlit";
    private const string HiddenFallbackMaterialName = "HiddenFallback_URP";

    private static readonly Dictionary<int, EffectPreset> PresetsByNumber = new()
    {
        { 4, new EffectPreset(HexTacticsHitEffectStyle.Medium, true, 0.56f, 0.08f, 0.96f) },
        { 16, new EffectPreset(HexTacticsHitEffectStyle.Heavy, true, 0.60f, 0.10f, 1.04f) },
        { 19, new EffectPreset(HexTacticsHitEffectStyle.Light, true, 0.50f, 0.05f, 0.82f) },
        { 25, new EffectPreset(HexTacticsHitEffectStyle.Heavy, true, 0.58f, 0.10f, 1.08f) }
    };

    public static void RunBatchMode()
    {
        var exitCode = ImportInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/Hex Tactics/Import Erb Hit Effects")]
    public static void Import()
    {
        ImportInternal();
    }

    [MenuItem("Tools/Hex Tactics/Validate Erb Hit Effects")]
    public static void Validate()
    {
        ValidateGeneratedEffects(logSuccess: true);
    }

    private static bool ImportInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureTargetFoldersExist();

        var sourcePrefabPaths = FindSourcePrefabPaths();
        if (sourcePrefabPaths.Count == 0)
        {
            Debug.LogError("[HexTacticsErbHitEffectImporter] No source hit prefabs were found under ErbGameArt.");
            return false;
        }

        var materialCache = new Dictionary<string, Material>();
        var generatedEntries = new List<HexTacticsHitEffectEntry>(sourcePrefabPaths.Count);

        foreach (var sourcePrefabPath in sourcePrefabPaths)
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            if (sourcePrefab == null)
            {
                Debug.LogError("[HexTacticsErbHitEffectImporter] Could not load source prefab: " + sourcePrefabPath);
                continue;
            }

            var effectNumber = ExtractEffectNumber(sourcePrefab.name);
            var assetName = BuildVariantAssetName(effectNumber);
            var prefabPath = HexTacticsAssetPaths.HitEffectsVariantFolder + "/" + assetName + ".prefab";
            var prefabRoot = PrefabUtility.LoadPrefabContents(sourcePrefabPath);

            try
            {
                prefabRoot.name = assetName;
                ConvertRendererMaterials(prefabRoot, materialCache);
                NormalizeParticleSystems(prefabRoot);
                EnsureTransientEffect(prefabRoot);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            var generatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (generatedPrefab == null)
            {
                Debug.LogError("[HexTacticsErbHitEffectImporter] Failed to generate prefab: " + prefabPath);
                continue;
            }

            var preset = ResolvePreset(effectNumber);
            generatedEntries.Add(new HexTacticsHitEffectEntry(
                id: assetName,
                displayName: BuildDisplayName(effectNumber),
                sourceAssetPath: sourcePrefabPath,
                prefab: generatedPrefab,
                style: preset.Style,
                autoSelect: preset.AutoSelect,
                heightNormalized: preset.HeightNormalized,
                forwardOffset: preset.ForwardOffset,
                scale: preset.Scale));
        }

        if (generatedEntries.Count == 0)
        {
            Debug.LogError("[HexTacticsErbHitEffectImporter] No hit effects were generated.");
            return false;
        }

        CreateOrUpdateCatalog(generatedEntries);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressablesSync.Sync();
        return ValidateGeneratedEffects(logSuccess: true);
    }

    private static void EnsureTargetFoldersExist()
    {
        EnsureFolder(HexTacticsAssetPaths.AddressablesRoot, "Effects");
        EnsureFolder(HexTacticsAssetPaths.EffectsFolder, "HitEffects");
        EnsureFolder(HexTacticsAssetPaths.HitEffectsFolder, "Variants");
        EnsureFolder(HexTacticsAssetPaths.HitEffectsFolder, "Materials");
    }

    private static void EnsureFolder(string parentPath, string childFolderName)
    {
        var targetPath = parentPath + "/" + childFolderName;
        if (!AssetDatabase.IsValidFolder(targetPath))
        {
            AssetDatabase.CreateFolder(parentPath, childFolderName);
        }
    }

    private static List<string> FindSourcePrefabPaths()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { SourceHitEffectsFolder });
        var paths = new List<string>(guids.Length);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrWhiteSpace(path))
            {
                paths.Add(path);
            }
        }

        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    private static void ConvertRendererMaterials(GameObject root, Dictionary<string, Material> materialCache)
    {
        var fallbackMaterial = GetOrCreateHiddenFallbackMaterial();
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                renderer.sharedMaterials = new[] { fallbackMaterial };
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                continue;
            }

            var changed = false;
            for (var i = 0; i < sharedMaterials.Length; i++)
            {
                if (sharedMaterials[i] == null)
                {
                    sharedMaterials[i] = fallbackMaterial;
                    changed = true;
                    continue;
                }

                var convertedMaterial = ConvertMaterial(sharedMaterials[i], materialCache);
                if (convertedMaterial == null || convertedMaterial == sharedMaterials[i])
                {
                    continue;
                }

                sharedMaterials[i] = convertedMaterial;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static Material GetOrCreateHiddenFallbackMaterial()
    {
        var fallbackPath = HexTacticsAssetPaths.HitEffectsMaterialFolder + "/" + HiddenFallbackMaterialName + ".mat";
        var fallbackMaterial = AssetDatabase.LoadAssetAtPath<Material>(fallbackPath);
        if (fallbackMaterial == null)
        {
            fallbackMaterial = new Material(ResolveTargetShader())
            {
                name = HiddenFallbackMaterialName
            };
            AssetDatabase.CreateAsset(fallbackMaterial, fallbackPath);
        }

        fallbackMaterial.shader = ResolveTargetShader();
        fallbackMaterial.SetFloat("_Surface", 1f);
        fallbackMaterial.SetFloat("_Blend", 0f);
        fallbackMaterial.SetFloat("_Cull", 0f);
        fallbackMaterial.SetFloat("_ZWrite", 0f);
        fallbackMaterial.SetFloat("_ColorMode", 0f);
        fallbackMaterial.SetFloat("_QueueOffset", 0f);
        fallbackMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
        fallbackMaterial.SetColor("_EmissionColor", Color.black);
        fallbackMaterial.SetTexture("_BaseMap", null);
        fallbackMaterial.SetTexture("_EmissionMap", null);
        fallbackMaterial.DisableKeyword("_EMISSION");
        BaseShaderGUI.SetupMaterialBlendMode(fallbackMaterial);
        ParticleGUI.SetMaterialKeywords(fallbackMaterial);
        EditorUtility.SetDirty(fallbackMaterial);
        return fallbackMaterial;
    }

    private static Material ConvertMaterial(Material sourceMaterial, Dictionary<string, Material> materialCache)
    {
        if (sourceMaterial == null)
        {
            return null;
        }

        var sourcePath = AssetDatabase.GetAssetPath(sourceMaterial);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return sourceMaterial;
        }

        if (materialCache.TryGetValue(sourcePath, out var cachedMaterial) && cachedMaterial != null)
        {
            return cachedMaterial;
        }

        var materialName = Path.GetFileNameWithoutExtension(sourcePath) + "_URP";
        var targetPath = HexTacticsAssetPaths.HitEffectsMaterialFolder + "/" + materialName + ".mat";
        var convertedMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (convertedMaterial == null)
        {
            convertedMaterial = new Material(ResolveTargetShader())
            {
                name = materialName
            };
            AssetDatabase.CreateAsset(convertedMaterial, targetPath);
        }

        ApplyConvertedMaterialSettings(sourceMaterial, convertedMaterial);
        EditorUtility.SetDirty(convertedMaterial);
        materialCache[sourcePath] = convertedMaterial;
        return convertedMaterial;
    }

    private static void ApplyConvertedMaterialSettings(Material sourceMaterial, Material targetMaterial)
    {
        targetMaterial.shader = ResolveTargetShader();
        targetMaterial.SetFloat("_Surface", 1f);
        targetMaterial.SetFloat("_Cull", 0f);
        targetMaterial.SetFloat("_ZWrite", 0f);
        targetMaterial.SetFloat("_Blend", ResolveBlendMode(sourceMaterial));
        targetMaterial.SetFloat("_ColorMode", 0f);
        targetMaterial.SetFloat("_QueueOffset", 0f);

        var baseTextureProperty = sourceMaterial.HasProperty("_MainTex")
            ? "_MainTex"
            : sourceMaterial.HasProperty("_BaseMap")
                ? "_BaseMap"
                : string.Empty;
        var baseTexture = !string.IsNullOrEmpty(baseTextureProperty)
            ? sourceMaterial.GetTexture(baseTextureProperty)
            : null;
        if (baseTexture != null)
        {
            targetMaterial.SetTexture("_BaseMap", baseTexture);
            targetMaterial.SetTextureScale("_BaseMap", sourceMaterial.GetTextureScale(baseTextureProperty));
            targetMaterial.SetTextureOffset("_BaseMap", sourceMaterial.GetTextureOffset(baseTextureProperty));
        }

        var tintColor = ResolveTintColor(sourceMaterial);
        var emission = ResolveEmissionIntensity(sourceMaterial);
        targetMaterial.SetColor("_BaseColor", tintColor);
        targetMaterial.SetColor("_EmissionColor", tintColor * Mathf.Max(0f, emission));
        if (baseTexture != null && emission > 0.01f)
        {
            targetMaterial.SetTexture("_EmissionMap", baseTexture);
            targetMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            targetMaterial.SetTexture("_EmissionMap", null);
            targetMaterial.DisableKeyword("_EMISSION");
        }

        BaseShaderGUI.SetupMaterialBlendMode(targetMaterial);
        ParticleGUI.SetMaterialKeywords(targetMaterial);
    }

    private static Shader ResolveTargetShader()
    {
        var shader = Shader.Find(TargetShaderName);
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            throw new InvalidOperationException("A URP particle shader could not be resolved for imported hit effects.");
        }

        return shader;
    }

    private static float ResolveBlendMode(Material sourceMaterial)
    {
        var shaderName = sourceMaterial.shader != null ? sourceMaterial.shader.name : string.Empty;
        return shaderName.IndexOf("blend", StringComparison.OrdinalIgnoreCase) >= 0
            ? 0f
            : 2f;
    }

    private static Color ResolveTintColor(Material sourceMaterial)
    {
        if (sourceMaterial.HasProperty("_TintColor"))
        {
            return sourceMaterial.GetColor("_TintColor");
        }

        if (sourceMaterial.HasProperty("_Color"))
        {
            return sourceMaterial.GetColor("_Color");
        }

        if (sourceMaterial.HasProperty("_BaseColor"))
        {
            return sourceMaterial.GetColor("_BaseColor");
        }

        return Color.white;
    }

    private static float ResolveEmissionIntensity(Material sourceMaterial)
    {
        var emission = sourceMaterial.HasProperty("_Emission")
            ? sourceMaterial.GetFloat("_Emission")
            : 0f;
        return Mathf.Max(0.6f, emission);
    }

    private static void NormalizeParticleSystems(GameObject root)
    {
        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.None;

            var collision = particleSystem.collision;
            if (collision.enabled)
            {
                collision.enabled = false;
            }
        }
    }

    private static void EnsureTransientEffect(GameObject root)
    {
        if (root.GetComponent<HexTacticsTransientEffect>() == null)
        {
            root.AddComponent<HexTacticsTransientEffect>();
        }
    }

    private static void CreateOrUpdateCatalog(List<HexTacticsHitEffectEntry> entries)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<HexTacticsHitEffectCatalog>(HexTacticsAssetPaths.HitEffectCatalogAssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<HexTacticsHitEffectCatalog>();
            AssetDatabase.CreateAsset(catalog, HexTacticsAssetPaths.HitEffectCatalogAssetPath);
        }

        entries.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        catalog.ReplaceEntries(entries);
        EditorUtility.SetDirty(catalog);
    }

    private static bool ValidateGeneratedEffects(bool logSuccess)
    {
        var issues = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<HexTacticsHitEffectCatalog>(HexTacticsAssetPaths.HitEffectCatalogAssetPath);
        if (catalog == null)
        {
            issues.Add("Hit effect catalog is missing.");
        }
        else
        {
            var autoSelectedCount = 0;
            foreach (var entry in catalog.HitEffects)
            {
                if (entry == null)
                {
                    issues.Add("Catalog contains a null hit effect entry.");
                    continue;
                }

                if (entry.Prefab == null)
                {
                    issues.Add($"Catalog entry '{entry.Id}' is missing its prefab reference.");
                    continue;
                }

                if (entry.AutoSelect)
                {
                    autoSelectedCount++;
                }

                var transientEffect = entry.Prefab.GetComponent<HexTacticsTransientEffect>();
                if (transientEffect == null)
                {
                    issues.Add($"Generated prefab is missing HexTacticsTransientEffect: {entry.Prefab.name}");
                }

                foreach (var renderer in entry.Prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null)
                        {
                            issues.Add($"Generated prefab has a missing material reference: {entry.Prefab.name}");
                            break;
                        }

                        if (material.shader == null)
                        {
                            issues.Add($"Generated material has a missing shader: {material.name}");
                        }
                    }
                }
            }

            if (autoSelectedCount < 3)
            {
                issues.Add("At least three curated auto-select hit effects are required.");
            }
        }

        if (issues.Count == 0)
        {
            if (logSuccess)
            {
                Debug.Log("[HexTacticsErbHitEffectImporter] Imported hit effects validated successfully.");
            }

            return true;
        }

        foreach (var issue in issues)
        {
            Debug.LogError("[HexTacticsErbHitEffectImporter] " + issue);
        }

        Debug.LogError($"[HexTacticsErbHitEffectImporter] Validation failed with {issues.Count} issue(s).");
        return false;
    }

    private static EffectPreset ResolvePreset(int effectNumber)
    {
        return PresetsByNumber.TryGetValue(effectNumber, out var preset)
            ? preset
            : new EffectPreset(HexTacticsHitEffectStyle.Medium, false, 0.58f, 0.08f, 1f);
    }

    private static string BuildVariantAssetName(int effectNumber)
    {
        return effectNumber > 0
            ? $"ErbHit{effectNumber:00}"
            : "ErbHit";
    }

    private static string BuildDisplayName(int effectNumber)
    {
        return effectNumber > 0
            ? $"Erb Hit {effectNumber:00}"
            : "Erb Hit";
    }

    private static int ExtractEffectNumber(string effectName)
    {
        if (string.IsNullOrWhiteSpace(effectName))
        {
            return 0;
        }

        var digits = string.Empty;
        for (var i = 0; i < effectName.Length; i++)
        {
            if (char.IsDigit(effectName[i]))
            {
                digits += effectName[i];
            }
        }

        return int.TryParse(digits, out var value) ? value : 0;
    }

    private readonly struct EffectPreset
    {
        public EffectPreset(
            HexTacticsHitEffectStyle style,
            bool autoSelect,
            float heightNormalized,
            float forwardOffset,
            float scale)
        {
            Style = style;
            AutoSelect = autoSelect;
            HeightNormalized = heightNormalized;
            ForwardOffset = forwardOffset;
            Scale = scale;
        }

        public HexTacticsHitEffectStyle Style { get; }
        public bool AutoSelect { get; }
        public float HeightNormalized { get; }
        public float ForwardOffset { get; }
        public float Scale { get; }
    }
}
