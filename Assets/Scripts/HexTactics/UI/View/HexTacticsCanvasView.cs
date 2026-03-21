using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HexTacticsCanvasView : HexTacticsUiGeneratedView
{
    private readonly List<HexTacticsWorldLabelView> worldLabelViews = new();

    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private RectTransform rootLayer;
    [SerializeField] private RectTransform safeAreaLayer;
    [SerializeField] private RectTransform worldLabelLayer;
    [SerializeField] private HexTacticsModeSelectScreenView modeSelectScreenPrefab;
    [SerializeField] private HexTacticsTeamBuilderScreenView teamBuilderScreenPrefab;
    [SerializeField] private HexTacticsPlanningScreenView planningScreenPrefab;
    [SerializeField] private HexTacticsResolvingScreenView resolvingScreenPrefab;
    [SerializeField] private HexTacticsVictoryOverlayView victoryOverlayPrefab;
    [SerializeField] private HexTacticsWorldLabelView worldLabelPrefab;

    private HexTacticsModeSelectScreenView modeSelectScreen;
    private HexTacticsTeamBuilderScreenView teamBuilderScreen;
    private HexTacticsPlanningScreenView planningScreen;
    private HexTacticsResolvingScreenView resolvingScreen;
    private HexTacticsVictoryOverlayView victoryOverlay;

    protected override int CurrentLayoutVersion => 7;

    protected override bool HasCurrentBindings =>
        canvas != null &&
        canvasScaler != null &&
        graphicRaycaster != null &&
        rootLayer != null &&
        safeAreaLayer != null &&
        worldLabelLayer != null &&
        modeSelectScreen != null &&
        teamBuilderScreen != null &&
        planningScreen != null &&
        resolvingScreen != null &&
        victoryOverlay != null;

    public readonly struct Actions
    {
        public Actions(
            Action startCpuMode,
            Action returnToModeSelect,
            Action<int> addRosterEntry,
            Action<int, Vector2> placeRosterEntryAt,
            Action<int> removeSelectionEntry,
            Action<int, Vector2> moveSelectionEntryAt,
            Action startBattle,
            Action clearSelection,
            Action waitSelectedUnit,
            Action<int> selectCommandUnit,
            Action<int> waitCommandUnit,
            Action returnToTeamBuilder,
            Action retryBattle)
        {
            StartCpuMode = startCpuMode;
            ReturnToModeSelect = returnToModeSelect;
            AddRosterEntry = addRosterEntry;
            PlaceRosterEntryAt = placeRosterEntryAt;
            RemoveSelectionEntry = removeSelectionEntry;
            MoveSelectionEntryAt = moveSelectionEntryAt;
            StartBattle = startBattle;
            ClearSelection = clearSelection;
            WaitSelectedUnit = waitSelectedUnit;
            SelectCommandUnit = selectCommandUnit;
            WaitCommandUnit = waitCommandUnit;
            ReturnToTeamBuilder = returnToTeamBuilder;
            RetryBattle = retryBattle;
        }

        public Action StartCpuMode { get; }
        public Action ReturnToModeSelect { get; }
        public Action<int> AddRosterEntry { get; }
        public Action<int, Vector2> PlaceRosterEntryAt { get; }
        public Action<int> RemoveSelectionEntry { get; }
        public Action<int, Vector2> MoveSelectionEntryAt { get; }
        public Action StartBattle { get; }
        public Action ClearSelection { get; }
        public Action WaitSelectedUnit { get; }
        public Action<int> SelectCommandUnit { get; }
        public Action<int> WaitCommandUnit { get; }
        public Action ReturnToTeamBuilder { get; }
        public Action RetryBattle { get; }
    }

    public void ConfigurePrefabs(
        HexTacticsModeSelectScreenView modeSelect,
        HexTacticsTeamBuilderScreenView teamBuilder,
        HexTacticsPlanningScreenView planning,
        HexTacticsResolvingScreenView resolving,
        HexTacticsVictoryOverlayView victory,
        HexTacticsWorldLabelView worldLabel)
    {
        modeSelectScreenPrefab = modeSelect;
        teamBuilderScreenPrefab = teamBuilder;
        planningScreenPrefab = planning;
        resolvingScreenPrefab = resolving;
        victoryOverlayPrefab = victory;
        worldLabelPrefab = worldLabel;
    }

    public void Render(HexTacticsUiSnapshot snapshot, Actions actions)
    {
        EnsureBuilt();
        ApplyResponsiveLayout();
        RenderPanels(snapshot);

        modeSelectScreen.Bind(actions.StartCpuMode);
        teamBuilderScreen.Bind(snapshot, actions.ReturnToModeSelect, actions.AddRosterEntry, actions.PlaceRosterEntryAt, actions.RemoveSelectionEntry, actions.MoveSelectionEntryAt, actions.StartBattle);
        planningScreen.Bind(snapshot, actions.ClearSelection, actions.WaitSelectedUnit, actions.SelectCommandUnit, actions.WaitCommandUnit);
        resolvingScreen.Bind(snapshot);
        victoryOverlay.Bind(snapshot, actions.ReturnToTeamBuilder, actions.RetryBattle);

        RenderWorldLabels(snapshot);
    }

    public override void BuildDefaultHierarchy()
    {
        worldLabelViews.Clear();
        modeSelectScreen = null;
        teamBuilderScreen = null;
        planningScreen = null;
        resolvingScreen = null;
        victoryOverlay = null;

        HexTacticsUiFactory.ResetViewRoot(this);

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.55f;

        graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        rootLayer = HexTacticsUiFactory.CreateRect("RootLayer", transform);
        HexTacticsUiFactory.Stretch(rootLayer, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(rootLayer, 0f, 0f, 0f, 0f);

        var backdrop = HexTacticsUiFactory.CreateRect("Backdrop", rootLayer);
        HexTacticsUiFactory.Stretch(backdrop, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(backdrop, 0f, 0f, 0f, 0f);
        HexTacticsUiFactory.AddImage(backdrop.gameObject, new Color(0.01f, 0.03f, 0.04f, 0.02f), false);

        safeAreaLayer = HexTacticsUiFactory.CreateRect("SafeAreaLayer", rootLayer);
        HexTacticsUiFactory.Stretch(safeAreaLayer, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(safeAreaLayer, 0f, 0f, 0f, 0f);

        worldLabelLayer = HexTacticsUiFactory.CreateRect("WorldLabels", rootLayer);
        HexTacticsUiFactory.Stretch(worldLabelLayer, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(worldLabelLayer, 0f, 0f, 0f, 0f);

        modeSelectScreen = HexTacticsUiFactory.InstantiateView(
            modeSelectScreenPrefab,
            HexTacticsUiResourcePaths.ModeSelectScreen,
            safeAreaLayer,
            HexTacticsModeSelectScreenView.CreateStandalone);

        teamBuilderScreen = HexTacticsUiFactory.InstantiateView(
            teamBuilderScreenPrefab,
            HexTacticsUiResourcePaths.TeamBuilderScreen,
            safeAreaLayer,
            HexTacticsTeamBuilderScreenView.CreateStandalone);

        planningScreen = HexTacticsUiFactory.InstantiateView(
            planningScreenPrefab,
            HexTacticsUiResourcePaths.PlanningScreen,
            safeAreaLayer,
            HexTacticsPlanningScreenView.CreateStandalone);

        resolvingScreen = HexTacticsUiFactory.InstantiateView(
            resolvingScreenPrefab,
            HexTacticsUiResourcePaths.ResolvingScreen,
            safeAreaLayer,
            HexTacticsResolvingScreenView.CreateStandalone);

        victoryOverlay = HexTacticsUiFactory.InstantiateView(
            victoryOverlayPrefab,
            HexTacticsUiResourcePaths.VictoryOverlay,
            rootLayer,
            HexTacticsVictoryOverlayView.CreateStandalone);
    }

    private void RenderPanels(HexTacticsUiSnapshot snapshot)
    {
        modeSelectScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.ModeSelect);
        teamBuilderScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.TeamBuilder);
        planningScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.Planning);
        resolvingScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.Resolving || snapshot.FlowState == HexTacticsUiFlowState.Victory);
        victoryOverlay.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.Victory);
        worldLabelLayer.gameObject.SetActive(
            snapshot.FlowState == HexTacticsUiFlowState.Planning ||
            snapshot.FlowState == HexTacticsUiFlowState.Resolving ||
            snapshot.FlowState == HexTacticsUiFlowState.Victory);
    }

    private void ApplyResponsiveLayout()
    {
        ApplyCanvasScale();
        ApplySafeArea();
        ApplyScreenRects();
    }

    private void ApplyCanvasScale()
    {
        if (canvasScaler == null || Screen.height <= 0)
        {
            return;
        }

        var aspect = (float)Screen.width / Screen.height;
        if (Screen.height > Screen.width * 1.05f)
        {
            canvasScaler.matchWidthOrHeight = Mathf.Lerp(0.88f, 0.76f, Mathf.InverseLerp(0.56f, 1.0f, aspect));
            return;
        }

        canvasScaler.matchWidthOrHeight = Mathf.Lerp(0.54f, 0.18f, Mathf.InverseLerp(1.0f, 2.35f, aspect));
    }

    private void ApplySafeArea()
    {
        if (safeAreaLayer == null)
        {
            return;
        }

        var safeArea = Screen.safeArea;
        var width = Mathf.Max(1f, Screen.width);
        var height = Mathf.Max(1f, Screen.height);
        safeAreaLayer.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
        safeAreaLayer.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
        safeAreaLayer.offsetMin = Vector2.zero;
        safeAreaLayer.offsetMax = Vector2.zero;
    }

    private void ApplyScreenRects()
    {
        if (safeAreaLayer == null)
        {
            return;
        }

        var safeWidth = Mathf.Max(1f, safeAreaLayer.rect.width);
        var safeHeight = Mathf.Max(1f, safeAreaLayer.rect.height);
        var isPortrait = safeHeight > safeWidth * 1.05f;
        var screenMargin = Mathf.Clamp(safeWidth * 0.016f, 14f, 24f);
        var battlePanelWidth = Mathf.Clamp(safeWidth * 0.245f, 320f, 408f);

        ConfigureCenteredCard(
            modeSelectScreen.Root,
            isPortrait ? Mathf.Clamp(safeWidth * 0.82f, 320f, 520f) : Mathf.Clamp(safeWidth * 0.30f, 460f, 520f),
            isPortrait ? Mathf.Clamp(safeHeight * 0.18f, 200f, 260f) : Mathf.Clamp(safeHeight * 0.22f, 220f, 260f),
            new Vector2(0f, Mathf.Clamp(safeHeight * 0.03f, 16f, 40f)));

        HexTacticsUiFactory.Stretch(teamBuilderScreen.Root, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(teamBuilderScreen.Root, screenMargin, screenMargin, screenMargin, screenMargin);

        if (isPortrait)
        {
            ConfigureBottomCenteredCard(
                planningScreen.Root,
                safeWidth - screenMargin * 2f,
                Mathf.Clamp(safeHeight * 0.22f, 220f, 300f),
                screenMargin);

            ConfigureTopCenteredCard(
                resolvingScreen.Root,
                Mathf.Clamp(safeWidth * 0.78f, 300f, 400f),
                Mathf.Clamp(safeHeight * 0.12f, 120f, 152f),
                screenMargin);
        }
        else
        {
            ConfigureTopLeftCard(
                planningScreen.Root,
                battlePanelWidth,
                Mathf.Clamp(safeHeight * 0.46f, 420f, 500f),
                screenMargin,
                screenMargin);

            ConfigureTopLeftCard(
                resolvingScreen.Root,
                Mathf.Clamp(battlePanelWidth * 0.92f, 300f, 376f),
                Mathf.Clamp(safeHeight * 0.16f, 140f, 176f),
                screenMargin,
                screenMargin);
        }
    }

    private void RenderWorldLabels(HexTacticsUiSnapshot snapshot)
    {
        if (!worldLabelLayer.gameObject.activeSelf)
        {
            for (var i = 0; i < worldLabelViews.Count; i++)
            {
                worldLabelViews[i].gameObject.SetActive(false);
            }

            return;
        }

        HexTacticsUiFactory.EnsurePool(
            worldLabelViews,
            snapshot.WorldLabels.Count,
            worldLabelPrefab,
            HexTacticsUiResourcePaths.WorldLabel,
            worldLabelLayer,
            HexTacticsWorldLabelView.CreateStandalone);

        var camera = Camera.main;
        for (var i = 0; i < worldLabelViews.Count; i++)
        {
            var active = i < snapshot.WorldLabels.Count;
            worldLabelViews[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            var data = snapshot.WorldLabels[i];
            worldLabelViews[i].Bind(data);

            if (camera == null)
            {
                worldLabelViews[i].gameObject.SetActive(false);
                continue;
            }

            var screenPoint = camera.WorldToScreenPoint(data.WorldPosition);
            if (screenPoint.z <= 0f)
            {
                worldLabelViews[i].gameObject.SetActive(false);
                continue;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(worldLabelLayer, screenPoint, null, out var localPoint))
            {
                var targetPosition = localPoint + new Vector2(0f, 14f);
                worldLabelViews[i].RectTransform.anchoredPosition = ClampWorldLabelPosition(worldLabelViews[i].RectTransform, targetPosition);
            }
        }
    }

    private Vector2 ClampWorldLabelPosition(RectTransform label, Vector2 position)
    {
        var layerRect = worldLabelLayer.rect;
        var labelSize = label.rect.size;
        var minX = layerRect.xMin + 10f + labelSize.x * label.pivot.x;
        var maxX = layerRect.xMax - 10f - labelSize.x * (1f - label.pivot.x);
        var minY = layerRect.yMin + 10f + labelSize.y * label.pivot.y;
        var maxY = layerRect.yMax - 10f - labelSize.y * (1f - label.pivot.y);
        return new Vector2(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY));
    }

    private static void ConfigureCenteredCard(RectTransform rect, float width, float height, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = anchoredPosition;
    }

    private static void ConfigureTopLeftCard(RectTransform rect, float width, float height, float left, float top)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(left, -top);
    }

    private static void ConfigureBottomCenteredCard(RectTransform rect, float width, float height, float margin)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0f, margin);
    }

    private static void ConfigureTopCenteredCard(RectTransform rect, float width, float height, float top)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(0f, -top);
    }
}
