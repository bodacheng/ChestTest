using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HexTacticsMissingComponentCleaner
{
    private const string AssetRoot = "Assets";

    [MenuItem("Tools/Hex Tactics/Audit Missing Components")]
    public static void AuditFromMenu()
    {
        RunInternal(removeMissingComponents: false, exitOnComplete: false);
    }

    [MenuItem("Tools/Hex Tactics/Clean Missing Components")]
    public static void CleanFromMenu()
    {
        RunInternal(removeMissingComponents: true, exitOnComplete: false);
    }

    public static void RunAuditBatchMode()
    {
        RunInternal(removeMissingComponents: false, exitOnComplete: true);
    }

    public static void RunCleanupBatchMode()
    {
        RunInternal(removeMissingComponents: true, exitOnComplete: true);
    }

    private static void RunInternal(bool removeMissingComponents, bool exitOnComplete)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var messages = new List<string>();
        var affectedAssetCount = 0;
        var missingComponentCount = 0;
        var previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            missingComponentCount += ProcessScenes(removeMissingComponents, messages, ref affectedAssetCount);
            missingComponentCount += ProcessPrefabs(removeMissingComponents, messages, ref affectedAssetCount);
        }
        finally
        {
            RestoreSceneSetup(previousSceneSetup);
        }

        foreach (var message in messages)
        {
            if (removeMissingComponents)
            {
                Debug.Log("[HexTacticsMissingComponentCleaner] " + message);
            }
            else
            {
                Debug.LogError("[HexTacticsMissingComponentCleaner] " + message);
            }
        }

        if (removeMissingComponents && affectedAssetCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        if (missingComponentCount == 0)
        {
            Debug.Log("[HexTacticsMissingComponentCleaner] No missing component references were found in project scenes or prefabs.");
            ExitIfNeeded(exitOnComplete, 0);
            return;
        }

        if (removeMissingComponents)
        {
            Debug.Log($"[HexTacticsMissingComponentCleaner] Removed {missingComponentCount} missing component reference(s) across {affectedAssetCount} asset(s).");
            ExitIfNeeded(exitOnComplete, 0);
            return;
        }

        Debug.LogError($"[HexTacticsMissingComponentCleaner] Found {missingComponentCount} missing component reference(s) across {affectedAssetCount} asset(s).");
        ExitIfNeeded(exitOnComplete, 1);
    }

    private static int ProcessScenes(bool removeMissingComponents, List<string> messages, ref int affectedAssetCount)
    {
        var totalMissingComponentCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { AssetRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var sceneMissingComponentCount = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                sceneMissingComponentCount += removeMissingComponents
                    ? HexTacticsHierarchyAuditUtility.RemoveMissingScripts(path, root, messages)
                    : HexTacticsHierarchyAuditUtility.CountMissingScripts(path, root, messages);
            }

            if (sceneMissingComponentCount <= 0)
            {
                continue;
            }

            totalMissingComponentCount += sceneMissingComponentCount;
            affectedAssetCount++;

            if (removeMissingComponents)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        return totalMissingComponentCount;
    }

    private static int ProcessPrefabs(bool removeMissingComponents, List<string> messages, ref int affectedAssetCount)
    {
        var totalMissingComponentCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { AssetRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                var prefabMissingComponentCount = removeMissingComponents
                    ? HexTacticsHierarchyAuditUtility.RemoveMissingScripts(path, root, messages)
                    : HexTacticsHierarchyAuditUtility.CountMissingScripts(path, root, messages);

                if (prefabMissingComponentCount <= 0)
                {
                    continue;
                }

                totalMissingComponentCount += prefabMissingComponentCount;
                affectedAssetCount++;

                if (removeMissingComponents)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return totalMissingComponentCount;
    }

    private static void RestoreSceneSetup(SceneSetup[] previousSceneSetup)
    {
        if (previousSceneSetup == null || previousSceneSetup.Length == 0)
        {
            return;
        }

        EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
    }

    private static void ExitIfNeeded(bool exitOnComplete, int exitCode)
    {
        if (exitOnComplete && Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }
}
