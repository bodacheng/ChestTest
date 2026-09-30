using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Exercises generated presentation without saving scenes or regenerating assets.</summary>
public static class HexTacticsPresentationAudit
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [MenuItem("Tools/Hex Tactics/Audit Presentation")]
    public static void RunFromMenu()
    {
        RunAudit();
    }

    public static void RunBatchMode()
    {
        var success = RunAudit();
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    /// <summary>Graphics batch entry; launch once for each desired screen aspect. Never saves the loaded scene.</summary>
    public static void CapturePreviews()
    {
        if (!Application.isBatchMode)
        {
            Debug.LogError("[HexTacticsPresentationAudit] CapturePreviews is a batch-only helper; use Audit Presentation for the current scene.");
            return;
        }

        var exitCode = 1;
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var prototype = UnityEngine.Object.FindAnyObjectByType<HexTacticsPrototype>();
            var camera = Camera.main;
            if (prototype == null || camera == null)
            {
                throw new InvalidOperationException("SampleScene is missing its game controller or main camera.");
            }

            var topology = typeof(HexTacticsPrototype).GetField("boardTopology", InstanceFlags);
            topology.SetValue(prototype, Enum.Parse(topology.FieldType, "Hex"));
            typeof(HexTacticsPrototype).GetMethod("BuildPrototype", InstanceFlags).Invoke(prototype, null);
            var canvasObject = new GameObject("Presentation Preview Canvas", typeof(RectTransform));
            var view = canvasObject.AddComponent<HexTacticsCanvasView>();
            view.EnsureBuilt();
            var canvas = view.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            CaptureFrame(prototype, view, camera, "mode");
            prototype.UiStartCpuHexMode();
            prototype.UiAddCharacterToPlayerTeam(0);
            prototype.UiAddCharacterToPlayerTeam(1);
            CaptureFrame(prototype, view, camera, "deployment");
            prototype.UiTryStartCpuBattle();
            var snapshot = prototype.BuildUiSnapshot();
            if (snapshot.FlowState != HexTacticsUiFlowState.Planning)
            {
                throw new InvalidOperationException("Preview characters could not start a battle: " + snapshot.BuilderStatus);
            }

            if (snapshot.PlayerCommandEntries.Count > 0)
            {
                prototype.UiSelectUnit(snapshot.PlayerCommandEntries[0].UnitId);
            }

            CaptureFrame(prototype, view, camera, "planning");
            Debug.Log($"[HexTacticsPresentationAudit] Captured presentation previews at {PreviewDimension("-screen-width", Screen.width)} x {PreviewDimension("-screen-height", Screen.height)} in Logs/Presentation.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            Debug.LogError("[HexTacticsPresentationAudit] Preview capture failed: " + exception);
        }
        finally
        {
            EditorApplication.Exit(exitCode);
        }
    }

    private static void CaptureFrame(HexTacticsPrototype prototype, HexTacticsCanvasView view, Camera camera, string state)
    {
        var width = PreviewDimension("-screen-width", Screen.width);
        var height = PreviewDimension("-screen-height", Screen.height);
        var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        Texture2D image = null;
        try
        {
            camera.targetTexture = renderTexture;
            typeof(HexTacticsPrototype).GetMethod("ConfigureCamera", InstanceFlags).Invoke(prototype, new object[] { true });
            var canvas = view.GetComponent<Canvas>();
            var scaler = view.GetComponent<CanvasScaler>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            for (var pass = 0; pass < 2; pass++)
            {
                view.Render(prototype.BuildUiSnapshot(), default);
                typeof(CanvasScaler).GetMethod("Handle", InstanceFlags).Invoke(scaler, null);
                Canvas.ForceUpdateCanvases();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)view.transform);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = renderTexture;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            var output = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Presentation");
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output, $"{state}-{width}x{height}.png"), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (image != null)
            {
                UnityEngine.Object.DestroyImmediate(image);
            }

            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    private static int PreviewDimension(string argument, int fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == argument && int.TryParse(args[i + 1], out var value) && value > 0)
            {
                return value;
            }
        }

        return Mathf.Max(1, fallback);
    }

    private static bool RunAudit()
    {
        var failures = new List<string>();
        try
        {
            using (var world = new PresentationWorld())
            {
                VerifyBoard(world, "Hex", 19, failures);
                VerifyBoard(world, "Square", 25, failures);
                VerifyUi(world, failures);
                VerifyEffectAnchoring(world, failures);
            }
        }
        catch (Exception exception)
        {
            failures.Add("Unexpected presentation audit exception: " + exception);
        }

        foreach (var failure in failures)
        {
            Debug.LogError("[HexTacticsPresentationAudit] " + failure);
        }

        if (failures.Count == 0)
        {
            Debug.Log("[HexTacticsPresentationAudit] Presentation audit passed.");
            return true;
        }

        Debug.LogError($"[HexTacticsPresentationAudit] Presentation audit failed with {failures.Count} issue(s).");
        return false;
    }

    private static void VerifyBoard(PresentationWorld world, string topology, int expectedCount, List<string> failures)
    {
        world.BuildBoard(topology);
        var cells = (IDictionary)world.Get("cells");
        var lookups = (IDictionary)world.Get("cellLookups");
        var board = (Transform)world.Get("boardRoot");
        var hidden = topology == "Hex";
        Assert(cells.Count == expectedCount, $"{topology} board has {cells.Count} cells; expected {expectedCount}.", failures);
        Assert(lookups.Count == expectedCount, $"{topology} board lost cell picking lookups.", failures);

        foreach (DictionaryEntry entry in cells)
        {
            var cell = entry.Value;
            var renderer = (MeshRenderer)GetProperty(cell, "Renderer");
            var cellTransform = hidden ? renderer.transform.parent : renderer.transform;
            var collider = cellTransform.GetComponent<MeshCollider>();
            Assert(collider != null && collider.enabled && collider.sharedMesh != null,
                $"{topology} cell {entry.Key} lost its picking collider.", failures);
            Assert(collider != null && lookups.Contains(collider), $"{topology} cell {entry.Key} is no longer pickable.", failures);
            Assert(renderer.enabled == !hidden, $"{topology} cell {entry.Key} has incorrect mode-select visibility.", failures);

            if (hidden)
            {
                Assert(cellTransform.GetComponent<MeshRenderer>() == null,
                    $"Hex cell {entry.Key} still renders a solid tile.", failures);
                var hint = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                Assert(hint != null && hint != collider?.sharedMesh,
                    $"Hex cell {entry.Key} uses the collision prism as its action hint.", failures);
                if (hint != null)
                {
                    foreach (var normal in hint.normals)
                    {
                        Assert(Vector3.Dot(normal, Vector3.up) > 0.99f,
                            $"Hex cell {entry.Key} action hint faces downward.", failures);
                    }
                }
            }
            else
            {
                Assert(renderer.GetComponent<MeshFilter>()?.sharedMesh == collider?.sharedMesh,
                    $"Square cell {entry.Key} changed its visible collision geometry.", failures);
            }
        }

        if (!hidden)
        {
            return;
        }

        var tileHeight = (float)world.Get("tileHeight");
        var platform = board.Find("Platform");
        var platformMesh = platform.GetComponent<MeshFilter>().sharedMesh;
        var platformTop = platform.localPosition.y + platformMesh.bounds.max.y;
        Assert(Mathf.Abs(platformTop - tileHeight * 0.5f) < 0.0001f,
            "Hex arena surface does not coincide with the hidden cell input plane.", failures);

        world.SetEnum("currentFlowState", "TeamBuilder");
        world.Invoke("RefreshVisuals");
        var deployment = world.Get("blueDeploySlotLookup");
        var contains = deployment.GetType().GetMethod("Contains");
        var visibleHints = 0;
        foreach (DictionaryEntry entry in cells)
        {
            var renderer = (MeshRenderer)GetProperty(entry.Value, "Renderer");
            var shouldShow = (bool)contains.Invoke(deployment, new[] { entry.Key });
            Assert(renderer.enabled == shouldShow,
                $"Hex deployment hint {entry.Key} does not match a valid deployment slot.", failures);
            visibleHints += renderer.enabled ? 1 : 0;
        }

        Assert(visibleHints > 0 && visibleHints < expectedCount, "Hex deployment hints reveal the entire board or no valid slots.", failures);
        world.SetEnum("currentFlowState", "Resolving");
        world.Invoke("RefreshVisuals");
        foreach (DictionaryEntry entry in cells)
        {
            Assert(!((MeshRenderer)GetProperty(entry.Value, "Renderer")).enabled,
                "Hex action hints remain visible while combat resolves.", failures);
        }
    }

    private static void VerifyUi(PresentationWorld world, List<string> failures)
    {
        var font = HexTacticsUiFactory.DefaultFont;
        Assert(font != null, "The game UI has no default font.", failures);
        if (font != null)
        {
            Assert(!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(font)), "The game UI still depends on an operating-system font.", failures);
            foreach (var character in "选择攻击部署技能回合胜利方形六边")
            {
                Assert(font.HasCharacter(character), $"The game UI font has no Chinese glyph for '{character}'.", failures);
            }
        }

        var canvasObject = world.CreateObject("Presentation UI", typeof(RectTransform), typeof(Canvas));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = (RectTransform)canvas.transform;
        canvasRect.sizeDelta = new Vector2(1920f, 1080f);

        var mode = HexTacticsModeSelectScreenView.CreateStandalone(canvasRect);
        VerifyLayout(mode.Root, failures);
        mode.gameObject.SetActive(false);

        var snapshot = new HexTacticsUiSnapshot
        {
            PlanningRoundNumber = 12,
            BlueAliveCount = 3,
            RedAliveCount = 3,
            CommandProgressSummary = "已设置 1 / 3 · 尚有 2 位角色待命",
            CurrentCommandSummary = "选择角色，安排移动与攻击",
            SelectedUnitSummary = "森护龙 · HP 13 / 13 · 能量 3 / 5",
            TurnTypeLabel = "同步结算",
            ResolutionStatus = "角色移动与攻击正在结算，请稍候。"
        };
        var planning = HexTacticsPlanningScreenView.CreateStandalone(canvasRect);
        planning.Bind(snapshot, null, null, null, null, null, null);
        VerifyLayout(planning.Root, failures);
        planning.gameObject.SetActive(false);

        var resolving = HexTacticsResolvingScreenView.CreateStandalone(canvasRect);
        resolving.Bind(snapshot);
        VerifyLayout(resolving.Root, failures);
        resolving.gameObject.SetActive(false);

        var popup = HexTacticsSkillPopupView.CreateStandalone(canvasRect);
        popup.Bind("森护龙", new List<HexTacticsSkillChoiceUiData>
        {
            new(4, "通常攻击", "攻击相邻敌人", true, false, true, false),
            new(9, "自然爆裂", "消耗能量发动攻击", false, false, true, false)
        }, null);
        VerifyLayout(popup.Root, failures);
        var rows = (IList)typeof(HexTacticsSkillPopupView).GetField("skillRows", InstanceFlags).GetValue(popup);
        foreach (var scale in new[] { 0.65f, 1.7f })
        {
            popup.Root.localScale = Vector3.one * scale;
            Canvas.ForceUpdateCanvases();
            foreach (HexTacticsSkillChoiceRowView row in rows)
            {
                var rect = (RectTransform)row.transform;
                var screenPoint = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                var result = HexTacticsSkillPopupView.ResolveSkillIndexAtScreenPosition(screenPoint, new Vector2(-10000f, -10000f), rows.Count);
                Assert(result == row.SkillIndex,
                    $"Scaled skill popup ({scale}) selected {result} instead of visible skill {row.SkillIndex}.", failures);
            }
        }
    }

    private static void VerifyEffectAnchoring(PresentationWorld world, List<string> failures)
    {
        // Deliberately separate the bright emitter from its prefab pivot, its
        // emission shape, and a much more distant smoke emitter.
        var prefab = world.CreateObject("Off-center Impact Fixture");
        prefab.transform.localScale = new Vector3(0.8f, 1.2f, 1.6f);
        var flashObject = world.CreateObject("Flash", typeof(ParticleSystem));
        flashObject.transform.SetParent(prefab.transform, false);
        flashObject.transform.localPosition = new Vector3(1.2f, 0.6f, -0.5f);
        flashObject.transform.localRotation = Quaternion.Euler(12f, 30f, 0f);
        var flash = flashObject.GetComponent<ParticleSystem>();
        var main = flash.main;
        main.startSpeed = 0f;
        main.startSize = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = flash.shape;
        shape.enabled = true;
        shape.position = new Vector3(-0.3f, 0.4f, 0.2f);
        flash.GetComponent<ParticleSystemRenderer>().pivot = new Vector3(2f, 0f, 0f);

        var smokeObject = world.CreateObject("Smoke", typeof(ParticleSystem));
        smokeObject.transform.SetParent(prefab.transform, false);
        smokeObject.transform.localPosition = new Vector3(40f, 15f, -12f);
        var smokeMain = smokeObject.GetComponent<ParticleSystem>().main;
        smokeMain.startSpeed = 3f;
        smokeMain.startSize = 30f;
        smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var target = new Vector3(2f, 1.3f, -4f);
        var spawn = typeof(HexTacticsPrototype).GetMethod("SpawnTransientEffect", InstanceFlags);
        var resolve = typeof(HexTacticsPrototype).GetMethod("TryResolveEffectCorePosition", StaticFlags);
        var configureTravel = typeof(HexTacticsPrototype).GetMethod("ConfigureTravelingEffectCore", StaticFlags);
        var effect = (GameObject)spawn.Invoke(world.Prototype,
            new object[] { prefab, target, Quaternion.Euler(20f, 75f, 10f), 1.4f, "Fixture Impact", true, 0.5f, false });
        Assert(effect != null, "Impact fixture could not spawn.", failures);
        if (effect == null)
        {
            return;
        }

        Assert(Vector3.Distance(effect.transform.position, target) < 0.0001f,
            "Centering an impact moved its gameplay origin away from the attack point.", failures);
        var visuals = effect.transform.GetChild(0);
        var arguments = new object[] { visuals, Vector3.zero };
        Assert((bool)resolve.Invoke(null, arguments) && Vector3.Distance((Vector3)arguments[1], target) < 0.0001f,
            "Impact bright core does not coincide with the attack point after spawn scaling/rotation.", failures);
        var spawnedFlash = visuals.Find("Flash").GetComponent<ParticleSystem>();
        Assert(spawnedFlash.GetComponent<ParticleSystemRenderer>().pivot == Vector3.zero,
            "Impact bright core retains a billboard pivot that displaces its visible center.", failures);

        effect.transform.SetPositionAndRotation(new Vector3(-3f, 2f, 1f), Quaternion.Euler(55f, -42f, 22f));
        effect.transform.localScale = Vector3.one * 1.7f;
        arguments[1] = Vector3.zero;
        Assert((bool)resolve.Invoke(null, arguments) && Vector3.Distance((Vector3)arguments[1], effect.transform.position) < 0.0001f,
            "Impact bright core drifts away from its gameplay origin when the effect moves, rotates, or scales.", failures);

        configureTravel.Invoke(null, new object[] { effect.transform });
        Assert(spawnedFlash.main.simulationSpace == ParticleSystemSimulationSpace.Local,
            "Traveling bright core does not follow its projectile origin.", failures);
        Assert(visuals.Find("Smoke").GetComponent<ParticleSystem>().main.simulationSpace == ParticleSystemSimulationSpace.World,
            "Centering the traveling core changed the smoke trail's authored world simulation.", failures);
    }

    private static void VerifyLayout(RectTransform root, List<string> failures)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        foreach (Transform child in root)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rect) || child.GetComponent<LayoutElement>()?.ignoreLayout == true)
            {
                continue;
            }

            Assert(rect.rect.width > 0f && rect.rect.height > 0f, $"{root.name}/{child.name} has an empty layout rectangle.", failures);
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = root.InverseTransformPoint(corner);
                Assert(point.x >= root.rect.xMin - 1f && point.x <= root.rect.xMax + 1f &&
                    point.y >= root.rect.yMin - 1f && point.y <= root.rect.yMax + 1f,
                    $"{root.name}/{child.name} extends outside its panel.", failures);
            }

            var element = child.GetComponent<LayoutElement>();
            if (element != null && element.minHeight > 0f)
            {
                Assert(rect.rect.height + 0.1f >= element.minHeight,
                    $"{root.name}/{child.name} lost its declared minimum height.", failures);
            }
        }

        foreach (var text in root.GetComponentsInChildren<Text>())
        {
            Assert(text.font == HexTacticsUiFactory.DefaultFont, $"{root.name}/{text.name} uses an inconsistent UI font.", failures);
            Assert(!text.raycastTarget, $"{root.name}/{text.name} blocks board or button input.", failures);
            if (!string.IsNullOrWhiteSpace(text.text))
            {
                text.cachedTextGenerator.Populate(text.text, text.GetGenerationSettings(text.rectTransform.rect.size));
                Assert(text.cachedTextGenerator.vertexCount > 4,
                    $"{root.name}/{text.name} has text but renders no glyphs (height {text.rectTransform.rect.height}, preferred {text.preferredHeight}).", failures);
            }
        }
    }

    private static object GetProperty(object target, string name)
    {
        return target.GetType().GetProperty(name, InstanceFlags).GetValue(target);
    }

    private static void Assert(bool condition, string message, List<string> failures)
    {
        if (!condition && !failures.Contains(message))
        {
            failures.Add(message);
        }
    }

    private sealed class PresentationWorld : IDisposable
    {
        private readonly Scene scene;
        private readonly GameObject root;
        private readonly Camera existingCamera;
        private readonly Vector3 cameraPosition;
        private readonly Quaternion cameraRotation;
        private readonly float cameraFieldOfView;
        private readonly float cameraNearClip;
        private readonly float cameraFarClip;
        private readonly Color cameraBackground;

        public PresentationWorld()
        {
            existingCamera = Camera.main;
            if (existingCamera != null)
            {
                cameraPosition = existingCamera.transform.position;
                cameraRotation = existingCamera.transform.rotation;
                cameraFieldOfView = existingCamera.fieldOfView;
                cameraNearClip = existingCamera.nearClipPlane;
                cameraFarClip = existingCamera.farClipPlane;
                cameraBackground = existingCamera.backgroundColor;
            }

            scene = EditorSceneManager.NewPreviewScene();
            root = CreateObject("HexTacticsPresentationAudit");
            root.SetActive(false);
            Prototype = root.AddComponent<HexTacticsPrototype>();
            Set("autoPopulateDefaultRoster", false);
            Set("boardRadius", 2);
        }

        public HexTacticsPrototype Prototype { get; }

        public GameObject CreateObject(string name, params Type[] components)
        {
            var gameObject = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject;
        }

        public void BuildBoard(string topology)
        {
            SetEnum("boardTopology", topology);
            Invoke("BuildPrototype");
        }

        public object Get(string name)
        {
            return typeof(HexTacticsPrototype).GetField(name, InstanceFlags).GetValue(Prototype);
        }

        public void Set(string name, object value)
        {
            typeof(HexTacticsPrototype).GetField(name, InstanceFlags).SetValue(Prototype, value);
        }

        public void SetEnum(string name, string value)
        {
            var field = typeof(HexTacticsPrototype).GetField(name, InstanceFlags);
            field.SetValue(Prototype, Enum.Parse(field.FieldType, value));
        }

        public void Invoke(string name)
        {
            typeof(HexTacticsPrototype).GetMethod(name, InstanceFlags, null, Type.EmptyTypes, null).Invoke(Prototype, null);
        }

        public void Dispose()
        {
            // An inactive MonoBehaviour never receives Awake/OnDestroy; explicitly release its generated meshes/materials.
            Invoke("ReleaseGeneratedAssets");
            EditorSceneManager.ClosePreviewScene(scene);
            if (existingCamera != null)
            {
                existingCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                existingCamera.fieldOfView = cameraFieldOfView;
                existingCamera.nearClipPlane = cameraNearClip;
                existingCamera.farClipPlane = cameraFarClip;
                existingCamera.backgroundColor = cameraBackground;
            }
        }
    }
}
