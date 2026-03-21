using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HexTacticsHierarchyAuditUtility
{
    public static int ValidateHierarchy(string assetPath, GameObject root, List<string> issues)
    {
        var totalMissingScriptCount = 0;
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            var gameObject = transform.gameObject;
            totalMissingScriptCount += ReportMissingScripts(assetPath, gameObject, issues);

            foreach (var renderer in gameObject.GetComponents<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    issues.Add($"{assetPath}: {GetHierarchyPath(gameObject)} has no assigned materials.");
                    continue;
                }

                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        issues.Add($"{assetPath}: {GetHierarchyPath(gameObject)} has a missing material at slot {i}.");
                    }
                }

                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh == null)
                {
                    issues.Add($"{assetPath}: {GetHierarchyPath(gameObject)} is missing its skinned mesh.");
                }
            }

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshFilter != null && meshFilter.sharedMesh == null)
            {
                issues.Add($"{assetPath}: {GetHierarchyPath(gameObject)} is missing its mesh filter mesh.");
            }

            var animator = gameObject.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                issues.Add($"{assetPath}: {GetHierarchyPath(gameObject)} is missing its animator controller.");
            }
        }

        return totalMissingScriptCount;
    }

    public static int CountMissingScripts(string assetPath, GameObject root, List<string> issues)
    {
        var totalMissingScriptCount = 0;
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            totalMissingScriptCount += ReportMissingScripts(assetPath, transform.gameObject, issues);
        }

        return totalMissingScriptCount;
    }

    public static int RemoveMissingScripts(string assetPath, GameObject root, List<string> fixes)
    {
        var removedScriptCount = 0;
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            var gameObject = transform.gameObject;
            var removedOnObject = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            if (removedOnObject <= 0)
            {
                continue;
            }

            removedScriptCount += removedOnObject;
            fixes?.Add($"{assetPath}: removed {removedOnObject} missing script reference(s) from {GetHierarchyPath(gameObject)}.");
        }

        return removedScriptCount;
    }

    public static string GetHierarchyPath(GameObject gameObject)
    {
        var path = gameObject.name;
        var current = gameObject.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static int ReportMissingScripts(string assetPath, GameObject gameObject, List<string> issues)
    {
        var missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
        if (missingScriptCount > 0)
        {
            issues?.Add($"{assetPath}: {GetHierarchyPath(gameObject)} has {missingScriptCount} missing script reference(s).");
        }

        return missingScriptCount;
    }
}
