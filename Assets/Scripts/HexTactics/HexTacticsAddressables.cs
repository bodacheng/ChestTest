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
            if (initializationFailed)
            {
                if (!initializationFailureLogged)
                {
                    var failureMessage = initializationHandle.OperationException?.Message;
                    Debug.LogWarning(string.IsNullOrWhiteSpace(failureMessage)
                        ? "[HexTacticsAddressables] Addressables initialization did not succeed."
                        : $"[HexTacticsAddressables] Addressables initialization did not succeed: {failureMessage}");
                    initializationFailureLogged = true;
                }

                ReleaseHandle(ref initializationHandle);
            }
        }
        catch (Exception exception)
        {
            if (!initializationFailureLogged)
            {
                Debug.LogWarning($"[HexTacticsAddressables] Failed to initialize Addressables: {exception.Message}");
                initializationFailureLogged = true;
            }

            ReleaseHandle(ref initializationHandle);
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

        AsyncOperationHandle<T> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<T>(address);
            var asset = handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
            {
                ReleaseHandle(ref handle);
                return null;
            }

            AssetCache[address] = asset;
            AssetHandles[address] = handle;
            return asset;
        }
        catch (Exception exception)
        {
            ReleaseHandle(ref handle);
            Debug.LogWarning($"[HexTacticsAddressables] Failed to load '{address}': {exception.Message}");
            return null;
        }
    }

    public static List<HexTacticsCharacterConfig> LoadCharacterConfigs()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            cachedCharacterConfigs ??= LoadCharacterConfigsFromEditor();
            return new List<HexTacticsCharacterConfig>(cachedCharacterConfigs);
        }
#endif

        return LoadConfigList(
            ref cachedCharacterConfigs,
            ref characterConfigsHandle,
            HexTacticsAssetPaths.CharacterConfigsLabel,
            config => config.DisplayName,
            "character configs");
    }

    public static List<HexTacticsSkillConfig> LoadSkillConfigs()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            cachedSkillConfigs ??= LoadSkillConfigsFromEditor();
            return new List<HexTacticsSkillConfig>(cachedSkillConfigs);
        }
#endif

        return LoadConfigList(
            ref cachedSkillConfigs,
            ref skillConfigsHandle,
            HexTacticsAssetPaths.SkillConfigsLabel,
            config => config.name,
            "skill configs");
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

        ReleaseHandle(ref characterConfigsHandle);
        cachedCharacterConfigs = null;

        ReleaseHandle(ref skillConfigsHandle);
        cachedSkillConfigs = null;

        ReleaseHandle(ref initializationHandle);
        isInitialized = false;
        initializationFailed = false;
        initializationFailureLogged = false;
    }

    private static List<TConfig> LoadConfigList<TConfig>(
        ref List<TConfig> cachedConfigs,
        ref AsyncOperationHandle<IList<TConfig>> configsHandle,
        string label,
        Func<TConfig, string> sortKeySelector,
        string logLabel) where TConfig : Object
    {
        if (cachedConfigs != null)
        {
            return new List<TConfig>(cachedConfigs);
        }

        if (!EnsureInitialized())
        {
            return new List<TConfig>();
        }

        try
        {
            configsHandle = Addressables.LoadAssetsAsync<TConfig>(label, null);
            var assets = configsHandle.WaitForCompletion();
            if (configsHandle.Status != AsyncOperationStatus.Succeeded || assets == null)
            {
                ReleaseHandle(ref configsHandle);
                return new List<TConfig>();
            }

            cachedConfigs = new List<TConfig>(assets);
            cachedConfigs.RemoveAll(config => config == null);
            cachedConfigs.Sort((left, right) => string.CompareOrdinal(sortKeySelector(left), sortKeySelector(right)));
            return new List<TConfig>(cachedConfigs);
        }
        catch (Exception exception)
        {
            ReleaseHandle(ref configsHandle);
            cachedConfigs = null;
            Debug.LogWarning($"[HexTacticsAddressables] Failed to load {logLabel}: {exception.Message}");
            return new List<TConfig>();
        }
    }

    private static void ReleaseHandle<T>(ref AsyncOperationHandle<T> handle)
    {
        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }

        handle = default;
    }

    private static void ReleaseHandle(ref AsyncOperationHandle handle)
    {
        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }

        handle = default;
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
