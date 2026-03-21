using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;

public static class HexTacticsAddressables
{
    private static readonly Dictionary<string, Object> AssetCache = new();
    private static readonly Dictionary<string, AsyncOperationHandle> AssetHandles = new();

    private static AsyncOperationHandle<IResourceLocator> initializationHandle;
    private static AsyncOperationHandle<IList<HexTacticsCharacterConfig>> characterConfigsHandle;
    private static AsyncOperationHandle<IList<HexTacticsSkillConfig>> skillConfigsHandle;
    private static List<HexTacticsCharacterConfig> cachedCharacterConfigs;
    private static List<HexTacticsSkillConfig> cachedSkillConfigs;
    private static bool isInitialized;
    private static bool initializationFailed;
    private static bool initializationFailureLogged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        ReleaseAll();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterQuitHandler()
    {
        Application.quitting -= ReleaseAll;
        Application.quitting += ReleaseAll;
    }

    public static bool EnsureInitialized()
    {
        if (isInitialized)
        {
            return true;
        }

        if (initializationFailed)
        {
            return false;
        }

        try
        {
            initializationHandle = Addressables.InitializeAsync();
            initializationHandle.WaitForCompletion();
            isInitialized = initializationHandle.Status == AsyncOperationStatus.Succeeded;
            initializationFailed = !isInitialized;
        }
        catch (Exception exception)
        {
            if (!initializationFailureLogged)
            {
                Debug.LogWarning($"[HexTacticsAddressables] Failed to initialize Addressables: {exception.Message}");
                initializationFailureLogged = true;
            }

            isInitialized = false;
            initializationFailed = true;
        }

        return isInitialized;
    }

    public static T LoadAsset<T>(string address) where T : Object
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        if (AssetCache.TryGetValue(address, out var cachedAsset))
        {
            return cachedAsset as T;
        }

#if UNITY_EDITOR
        if (Application.isEditor)
        {
            var editorAsset = LoadAssetFromEditor<T>(address);
            if (editorAsset != null)
            {
                AssetCache[address] = editorAsset;
            }

            return editorAsset;
        }
#endif

        if (!EnsureInitialized())
        {
            return null;
        }

        AsyncOperationHandle<T> handle;
        try
        {
            handle = Addressables.LoadAssetAsync<T>(address);
            var asset = handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                return null;
            }

            AssetCache[address] = asset;
            AssetHandles[address] = handle;
            return asset;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[HexTacticsAddressables] Failed to load '{address}': {exception.Message}");
            return null;
        }
    }

    public static List<HexTacticsCharacterConfig> LoadCharacterConfigs()
    {
        if (cachedCharacterConfigs != null)
        {
            return new List<HexTacticsCharacterConfig>(cachedCharacterConfigs);
        }

        cachedCharacterConfigs = new List<HexTacticsCharacterConfig>();
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            cachedCharacterConfigs.AddRange(LoadCharacterConfigsFromEditor());
            return new List<HexTacticsCharacterConfig>(cachedCharacterConfigs);
        }
#endif

        if (!EnsureInitialized())
        {
            return new List<HexTacticsCharacterConfig>();
        }

        try
        {
            characterConfigsHandle = Addressables.LoadAssetsAsync<HexTacticsCharacterConfig>(HexTacticsAssetPaths.CharacterConfigsLabel, null);
            var assets = characterConfigsHandle.WaitForCompletion();
            if (characterConfigsHandle.Status != AsyncOperationStatus.Succeeded || assets == null)
            {
                if (characterConfigsHandle.IsValid())
                {
                    Addressables.Release(characterConfigsHandle);
                    characterConfigsHandle = default;
                }

                cachedCharacterConfigs.Clear();
                return new List<HexTacticsCharacterConfig>();
            }

            cachedCharacterConfigs.AddRange(assets);
            cachedCharacterConfigs.RemoveAll(config => config == null);
            cachedCharacterConfigs.Sort((left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
            return new List<HexTacticsCharacterConfig>(cachedCharacterConfigs);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[HexTacticsAddressables] Failed to load character configs: {exception.Message}");
            cachedCharacterConfigs.Clear();
            return new List<HexTacticsCharacterConfig>();
        }
    }

    public static List<HexTacticsSkillConfig> LoadSkillConfigs()
    {
        if (cachedSkillConfigs != null)
        {
            return new List<HexTacticsSkillConfig>(cachedSkillConfigs);
        }

        cachedSkillConfigs = new List<HexTacticsSkillConfig>();
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            cachedSkillConfigs.AddRange(LoadSkillConfigsFromEditor());
            return new List<HexTacticsSkillConfig>(cachedSkillConfigs);
        }
#endif

        if (!EnsureInitialized())
        {
            return new List<HexTacticsSkillConfig>();
        }

        try
        {
            skillConfigsHandle = Addressables.LoadAssetsAsync<HexTacticsSkillConfig>(HexTacticsAssetPaths.SkillConfigsLabel, null);
            var assets = skillConfigsHandle.WaitForCompletion();
            if (skillConfigsHandle.Status != AsyncOperationStatus.Succeeded || assets == null)
            {
                if (skillConfigsHandle.IsValid())
                {
                    Addressables.Release(skillConfigsHandle);
                    skillConfigsHandle = default;
                }

                cachedSkillConfigs.Clear();
                return new List<HexTacticsSkillConfig>();
            }

            cachedSkillConfigs.AddRange(assets);
            cachedSkillConfigs.RemoveAll(config => config == null);
            cachedSkillConfigs.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return new List<HexTacticsSkillConfig>(cachedSkillConfigs);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[HexTacticsAddressables] Failed to load skill configs: {exception.Message}");
            cachedSkillConfigs.Clear();
            return new List<HexTacticsSkillConfig>();
        }
    }

    public static void ReleaseAll()
    {
        foreach (var handle in AssetHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        AssetHandles.Clear();
        AssetCache.Clear();

        if (characterConfigsHandle.IsValid())
        {
            Addressables.Release(characterConfigsHandle);
            characterConfigsHandle = default;
        }

        cachedCharacterConfigs = null;

        if (skillConfigsHandle.IsValid())
        {
            Addressables.Release(skillConfigsHandle);
            skillConfigsHandle = default;
        }

        cachedSkillConfigs = null;

        if (initializationHandle.IsValid())
        {
            Addressables.Release(initializationHandle);
            initializationHandle = default;
        }

        isInitialized = false;
        initializationFailed = false;
        initializationFailureLogged = false;
    }

#if UNITY_EDITOR
    private static T LoadAssetFromEditor<T>(string address) where T : Object
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        if (address.StartsWith("Assets/", StringComparison.Ordinal))
        {
            return AssetDatabase.LoadAssetAtPath<T>(address);
        }

        var asset = AssetDatabase.LoadAssetAtPath<T>($"{HexTacticsAssetPaths.AddressablesRoot}/{address}.prefab");
        if (asset != null)
        {
            return asset;
        }

        asset = AssetDatabase.LoadAssetAtPath<T>($"{HexTacticsAssetPaths.AddressablesRoot}/{address}.asset");
        if (asset != null)
        {
            return asset;
        }

        return AssetDatabase.LoadAssetAtPath<T>($"{HexTacticsAssetPaths.AddressablesRoot}/{address}.mat");
    }

    private static List<HexTacticsCharacterConfig> LoadCharacterConfigsFromEditor()
    {
        var roster = new List<HexTacticsCharacterConfig>();
        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsCharacterConfig", new[] { HexTacticsAssetPaths.CharacterConfigFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(path);
            if (config != null)
            {
                roster.Add(config);
            }
        }

        roster.Sort((left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
        return roster;
    }

    private static List<HexTacticsSkillConfig> LoadSkillConfigsFromEditor()
    {
        var skills = new List<HexTacticsSkillConfig>();
        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsSkillConfig", new[] { HexTacticsAssetPaths.SkillConfigFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsSkillConfig>(path);
            if (config != null)
            {
                skills.Add(config);
            }
        }

        skills.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return skills;
    }
#endif
}
