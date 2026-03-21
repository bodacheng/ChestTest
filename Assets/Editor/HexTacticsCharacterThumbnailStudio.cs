using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class HexTacticsCharacterThumbnailStudio : IDisposable
{
    public const string ScenePath = "Assets/Scenes/Editor/HexTacticsCharacterThumbnailStudio.unity";

    private const string SceneFolder = "Assets/Scenes";
    private const string EditorSceneFolder = SceneFolder + "/Editor";
    private const string StudioAssetFolder = EditorSceneFolder + "/HexTacticsCharacterThumbnailStudio";
    private const string FloorMaterialPath = StudioAssetFolder + "/HexTacticsThumbnailFloor.mat";
    private const string BackdropMaterialPath = StudioAssetFolder + "/HexTacticsThumbnailBackdrop.mat";
    private const string CameraObjectName = "ThumbnailCamera";
    private const string SubjectAnchorName = "ThumbnailSubjectAnchor";
    private const string LookTargetName = "ThumbnailLookTarget";
    private const string RootName = "HexTacticsCharacterThumbnailStudio";
    private static readonly Color CameraBackgroundColor = new Color(0.84f, 0.87f, 0.88f, 1f);
    private static readonly Vector3 CameraDirection = new Vector3(-0.20f, 0.08f, -1f).normalized;

    private readonly SceneSetup[] previousSetup;
    private readonly bool shouldRestorePreviousSetup;
    private readonly Scene studioScene;
    private readonly Camera thumbnailCamera;
    private readonly Transform subjectAnchor;
    private readonly Transform lookTarget;
    private GameObject currentInstance;

    [MenuItem("Tools/Hex Tactics/Open Character Thumbnail Studio")]
    public static void OpenStudio()
    {
        EnsureSceneAsset();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Hex Tactics/Rebuild Character Thumbnail Studio")]
    public static void RebuildStudio()
    {
        EnsureSceneAsset(forceRebuild: true);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    public static void EnsureSceneAsset(bool forceRebuild = false)
    {
        EnsureFolder(SceneFolder);
        EnsureFolder(EditorSceneFolder);
        EnsureFolder(StudioAssetFolder);

        EnsureStudioMaterial(
            FloorMaterialPath,
            new Color(0.63f, 0.67f, 0.69f, 1f),
            metallic: 0.06f,
            smoothness: 0.24f);
        EnsureStudioMaterial(
            BackdropMaterialPath,
            new Color(0.73f, 0.79f, 0.81f, 1f),
            metallic: 0f,
            smoothness: 0.08f);

        var previous = Application.isBatchMode ? null : EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var hasSceneAsset = File.Exists(GetAbsolutePath(ScenePath));
            var scene = !forceRebuild && hasSceneAsset
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureStudioScene(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            if (!Application.isBatchMode && previous != null && previous.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }
    }

    public HexTacticsCharacterThumbnailStudio()
    {
        EnsureSceneAsset();

        if (!Application.isBatchMode)
        {
            previousSetup = EditorSceneManager.GetSceneManagerSetup();
            shouldRestorePreviousSetup = true;
        }

        studioScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        thumbnailCamera = FindRequiredComponent<Camera>(studioScene, CameraObjectName);
        subjectAnchor = FindRequiredTransform(studioScene, SubjectAnchorName);
        lookTarget = FindRequiredTransform(studioScene, LookTargetName);
        ClearSubjectAnchor();
    }

    public Texture2D Capture(GameObject prefab, int iconSize, float yawDegrees, List<string> issues)
    {
        if (prefab == null)
        {
            issues?.Add("Thumbnail studio received a null prefab.");
            return null;
        }

        ClearSubjectAnchor();
        currentInstance = PrefabUtility.InstantiatePrefab(prefab, studioScene) as GameObject;
        if (currentInstance == null)
        {
            issues?.Add($"Thumbnail studio could not instantiate '{prefab.name}'.");
            return null;
        }

        currentInstance.name = "ThumbnailSubject";
        currentInstance.transform.SetParent(subjectAnchor, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        currentInstance.transform.localScale = Vector3.one;

        PrepareInstance(currentInstance);
        RecenterSubject(currentInstance);

        var bounds = CalculateBounds(currentInstance);
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            issues?.Add($"Thumbnail studio could not resolve render bounds for '{prefab.name}'.");
            return null;
        }

        ConfigureCamera(bounds);
        return Render(iconSize);
    }

    public void Dispose()
    {
        ClearSubjectAnchor();

        if (shouldRestorePreviousSetup && previousSetup != null && previousSetup.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    private static void ConfigureStudioScene(Scene scene)
    {
        foreach (var existingRoot in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(existingRoot);
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.74f, 0.77f, 0.79f, 1f);
        RenderSettings.fog = false;

        var studioRoot = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(studioRoot, scene);

        var subjectAnchor = new GameObject(SubjectAnchorName);
        subjectAnchor.transform.SetParent(studioRoot.transform, false);

        var lookTarget = new GameObject(LookTargetName);
        lookTarget.transform.SetParent(studioRoot.transform, false);
        lookTarget.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        CreateCamera(studioRoot.transform);
        CreateDirectionalLight(studioRoot.transform, "KeyLight", new Vector3(38f, 142f, 0f), 1.30f);
        CreateDirectionalLight(studioRoot.transform, "FillLight", new Vector3(336f, 214f, 0f), 0.62f);
        CreateDirectionalLight(studioRoot.transform, "RimLight", new Vector3(18f, 24f, 0f), 0.44f);
        CreateStudioGeometry(studioRoot.transform);

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void CreateCamera(Transform parent)
    {
        var cameraObject = new GameObject(CameraObjectName);
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(-0.85f, 1.45f, -6.2f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0.16f, -0.03f, 1f).normalized, Vector3.up);

        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = CameraBackgroundColor;
        camera.fieldOfView = 24f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 64f;
        camera.allowHDR = false;
        camera.allowMSAA = true;
        camera.cameraType = CameraType.Game;
    }

    private static void CreateDirectionalLight(Transform parent, string name, Vector3 eulerAngles, float intensity)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.rotation = Quaternion.Euler(eulerAngles);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = Color.white;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateStudioGeometry(Transform parent)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(parent, false);
        floor.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        floor.transform.localScale = new Vector3(9.5f, 0.12f, 9.5f);
        ApplyStudioMaterial(floor, FloorMaterialPath);

        var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backdrop.name = "Backdrop";
        backdrop.transform.SetParent(parent, false);
        backdrop.transform.localPosition = new Vector3(0f, 3.2f, 4.9f);
        backdrop.transform.localScale = new Vector3(10f, 6.4f, 0.18f);
        ApplyStudioMaterial(backdrop, BackdropMaterialPath);
    }

    private static void ApplyStudioMaterial(GameObject target, string materialPath)
    {
        var renderer = target.GetComponent<Renderer>();
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void EnsureStudioMaterial(string path, Color color, float metallic, float smoothness)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
        {
            return;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            return;
        }

        var material = new Material(shader);
        material.name = Path.GetFileNameWithoutExtension(path);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }
        else if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        AssetDatabase.CreateAsset(material, path);
    }

    private void PrepareInstance(GameObject instance)
    {
        foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
        {
            animator.Rebind();
            animator.Update(0f);
        }

        foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.gameObject.SetActive(false);
        }

        foreach (var trail in instance.GetComponentsInChildren<TrailRenderer>(true))
        {
            trail.enabled = false;
        }
    }

    private void RecenterSubject(GameObject instance)
    {
        var bounds = CalculateBounds(instance);
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        var offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        instance.transform.position += offset;
    }

    private void ConfigureCamera(Bounds bounds)
    {
        var focusPoint = new Vector3(0f, bounds.min.y + bounds.size.y * 0.58f, 0f);
        lookTarget.position = focusPoint;

        var verticalHalfFov = thumbnailCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        var horizontalHalfFov = Mathf.Atan(Mathf.Tan(verticalHalfFov) * thumbnailCamera.aspect);
        var requiredHalfHeight = Mathf.Max(bounds.extents.y * 1.16f, 0.65f);
        var requiredHalfWidth = Mathf.Max(bounds.extents.x * 1.28f + bounds.extents.z * 0.22f, 0.65f);
        var distanceByHeight = requiredHalfHeight / Mathf.Tan(verticalHalfFov);
        var distanceByWidth = requiredHalfWidth / Mathf.Tan(horizontalHalfFov);
        var distance = Mathf.Max(distanceByHeight, distanceByWidth) + bounds.extents.z * 1.25f;

        thumbnailCamera.transform.position = focusPoint + CameraDirection * distance;
        thumbnailCamera.transform.rotation = Quaternion.LookRotation(focusPoint - thumbnailCamera.transform.position, Vector3.up);
        thumbnailCamera.nearClipPlane = 0.05f;
        thumbnailCamera.farClipPlane = Mathf.Max(48f, distance + bounds.extents.magnitude * 5f);
    }

    private Texture2D Render(int iconSize)
    {
        RenderTexture renderTexture = null;
        try
        {
            renderTexture = new RenderTexture(iconSize, iconSize, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            thumbnailCamera.targetTexture = renderTexture;
            thumbnailCamera.Render();

            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false, false);
                texture.ReadPixels(new Rect(0f, 0f, iconSize, iconSize), 0, 0);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }
        finally
        {
            thumbnailCamera.targetTexture = null;
            if (renderTexture != null)
            {
                Object.DestroyImmediate(renderTexture);
            }
        }
    }

    private void ClearSubjectAnchor()
    {
        if (currentInstance != null)
        {
            Object.DestroyImmediate(currentInstance);
            currentInstance = null;
        }

        if (subjectAnchor == null)
        {
            return;
        }

        for (var i = subjectAnchor.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(subjectAnchor.GetChild(i).gameObject);
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.zero);
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled)
            {
                continue;
            }

            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component
    {
        var transform = FindRequiredTransform(scene, objectName);
        var component = transform.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        throw new InvalidOperationException($"Thumbnail studio object '{objectName}' is missing component '{typeof(T).Name}'.");
    }

    private static Transform FindRequiredTransform(Scene scene, string objectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == objectName)
                {
                    return transform;
                }
            }
        }

        throw new InvalidOperationException($"Thumbnail studio object '{objectName}' was not found in {ScenePath}.");
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

    private static string GetAbsolutePath(string assetPath)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
        return Path.Combine(projectRoot, assetPath);
    }
}
