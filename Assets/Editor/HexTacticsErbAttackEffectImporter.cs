using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

public static class HexTacticsErbAttackEffectImporter
{
    private const string SourceShockwavePrefabPath = "Assets/ErbGameArt/AAA Projectiles/Prefabs/Flash/Flash 17.prefab";
    private const string TargetShaderName = "Universal Render Pipeline/Particles/Unlit";
    private const string HiddenFallbackMaterialName = "HiddenFallback_URP";

    public static void RunBatchMode()
    {
        var exitCode = ImportInternal() ? 0 : 1;
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    [MenuItem("Tools/Hex Tactics/Import Erb Attack Effects")]
    public static void Import()
    {
        ImportInternal();
    }

    [MenuItem("Tools/Hex Tactics/Validate Erb Attack Effects")]
    public static void Validate()
    {
        ValidateGeneratedEffect(logSuccess: true);
    }

    private static bool ImportInternal()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureTargetFoldersExist();

        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceShockwavePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError("[HexTacticsErbAttackEffectImporter] Could not load source shockwave prefab.");
            return false;
        }

        var materialCache = new Dictionary<string, Material>();
        var prefabRoot = PrefabUtility.LoadPrefabContents(SourceShockwavePrefabPath);
        try
        {
            prefabRoot.name = "ErbAttackWave01";
            ConvertRendererMaterials(prefabRoot, materialCache);
            NormalizeParticleSystems(prefabRoot);
            EnsureTransientEffect(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, HexTacticsAssetPaths.RangedWaveEffectAssetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        HexTacticsAddressablesSync.Sync();
        return ValidateGeneratedEffect(logSuccess: true);
    }

    private static void EnsureTargetFoldersExist()
    {
        EnsureFolder(HexTacticsAssetPaths.AddressablesRoot, "Effects");
        EnsureFolder(HexTacticsAssetPaths.EffectsFolder, "AttackEffects");
        EnsureFolder(HexTacticsAssetPaths.AttackEffectsFolder, "Variants");
        EnsureFolder(HexTacticsAssetPaths.AttackEffectsFolder, "Materials");
    }

    private static void EnsureFolder(string parentPath, string childFolderName)
    {
        var targetPath = parentPath + "/" + childFolderName;
        if (!AssetDatabase.IsValidFolder(targetPath))
        {
            AssetDatabase.CreateFolder(parentPath, childFolderName);
        }
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
        var fallbackPath = HexTacticsAssetPaths.AttackEffectsMaterialFolder + "/" + HiddenFallbackMaterialName + ".mat";
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
        var targetPath = HexTacticsAssetPaths.AttackEffectsMaterialFolder + "/" + materialName + ".mat";
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
            throw new InvalidOperationException("A URP particle shader could not be resolved for imported attack effects.");
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

    private static bool ValidateGeneratedEffect(bool logSuccess)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HexTacticsAssetPaths.RangedWaveEffectAssetPath);
        if (prefab == null)
        {
            Debug.LogError("[HexTacticsErbAttackEffectImporter] Ranged wave prefab is missing.");
            return false;
        }

        if (prefab.GetComponent<HexTacticsTransientEffect>() == null)
        {
            Debug.LogError("[HexTacticsErbAttackEffectImporter] Generated ranged wave prefab is missing HexTacticsTransientEffect.");
            return false;
        }

        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    Debug.LogError("[HexTacticsErbAttackEffectImporter] Generated ranged wave prefab has a missing material reference.");
                    return false;
                }

                if (material.shader == null)
                {
                    Debug.LogError("[HexTacticsErbAttackEffectImporter] Generated ranged wave material has a missing shader.");
                    return false;
                }
            }
        }

        if (logSuccess)
        {
            Debug.Log("[HexTacticsErbAttackEffectImporter] Imported ranged attack effects validated successfully.");
        }

        return true;
    }
}
