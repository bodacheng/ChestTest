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
    [SerializeField] private RectTransform floatingHudLayer;
    [SerializeField] private HexTacticsModeSelectScreenView modeSelectScreenPrefab;
    [SerializeField] private HexTacticsTeamBuilderScreenView teamBuilderScreenPrefab;
    [SerializeField] private HexTacticsPlanningScreenView planningScreenPrefab;
    [SerializeField] private HexTacticsResolvingScreenView resolvingScreenPrefab;
    [SerializeField] private HexTacticsVictoryOverlayView victoryOverlayPrefab;
    [SerializeField] private HexTacticsWorldLabelView worldLabelPrefab;
    [SerializeField] private HexTacticsSkillPopupView skillPopupPrefab;

    private HexTacticsModeSelectScreenView modeSelectScreen;
    private HexTacticsTeamBuilderScreenView teamBuilderScreen;
    private HexTacticsPlanningScreenView planningScreen;
    private HexTacticsResolvingScreenView resolvingScreen;
    private HexTacticsVictoryOverlayView victoryOverlay;
    private HexTacticsSkillPopupView skillPopup;

    protected override int CurrentLayoutVersion => 9;

    protected override bool HasCurrentBindings =>
        canvas != null &&
        canvasScaler != null &&
        graphicRaycaster != null &&
        rootLayer != null &&
        safeAreaLayer != null &&
        worldLabelLayer != null &&
        floatingHudLayer != null &&
        modeSelectScreen != null &&
        teamBuilderScreen != null &&
        planningScreen != null &&
        resolvingScreen != null &&
        victoryOverlay != null &&
        skillPopup != null;

    public readonly struct Actions
    {
        public Actions(
            Action startHexMode,
            Action startSquareMode,
            Action returnToModeSelect,
            Action<int> addRosterEntry,
            Action<int, Vector2> placeRosterEntryAt,
            Action<int> removeSelectionEntry,
            Action<int, Vector2> moveSelectionEntryAt,
            Action startBattle,
            Action clearSelection,
            Action waitSelectedUnit,
            Action<int> selectSelectedUnitSkill,
            Action<int> selectCommandUnit,
            Action<int> waitCommandUnit,
            Action<int> cycleCommandUnitSkill,
            Action returnToTeamBuilder,
            Action retryBattle)
        {
            StartHexMode = startHexMode;
            StartSquareMode = startSquareMode;
            ReturnToModeSelect = returnToModeSelect;
            AddRosterEntry = addRosterEntry;
            PlaceRosterEntryAt = placeRosterEntryAt;
            RemoveSelectionEntry = removeSelectionEntry;
            MoveSelectionEntryAt = moveSelectionEntryAt;
            StartBattle = startBattle;
            ClearSelection = clearSelection;
            WaitSelectedUnit = waitSelectedUnit;
            SelectSelectedUnitSkill = selectSelectedUnitSkill;
            SelectCommandUnit = selectCommandUnit;
            WaitCommandUnit = waitCommandUnit;
            CycleCommandUnitSkill = cycleCommandUnitSkill;
            ReturnToTeamBuilder = returnToTeamBuilder;
            RetryBattle = retryBattle;
        }

        public Action StartHexMode { get; }
        public Action StartSquareMode { get; }
        public Action ReturnToModeSelect { get; }
        public Action<int> AddRosterEntry { get; }
        public Action<int, Vector2> PlaceRosterEntryAt { get; }
        public Action<int> RemoveSelectionEntry { get; }
        public Action<int, Vector2> MoveSelectionEntryAt { get; }
        public Action StartBattle { get; }
        public Action ClearSelection { get; }
        public Action WaitSelectedUnit { get; }
        public Action<int> SelectSelectedUnitSkill { get; }
        public Action<int> SelectCommandUnit { get; }
        public Action<int> WaitCommandUnit { get; }
        public Action<int> CycleCommandUnitSkill { get; }
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

        modeSelectScreen.Bind(actions.StartHexMode, actions.StartSquareMode);
        teamBuilderScreen.Bind(snapshot, actions.ReturnToModeSelect, actions.AddRosterEntry, actions.PlaceRosterEntryAt, actions.RemoveSelectionEntry, actions.MoveSelectionEntryAt, actions.StartBattle);
        planningScreen.Bind(snapshot, actions.ClearSelection, actions.WaitSelectedUnit, actions.SelectSelectedUnitSkill, actions.SelectCommandUnit, actions.WaitCommandUnit, actions.CycleCommandUnitSkill);
        resolvingScreen.Bind(snapshot);
        victoryOverlay.Bind(snapshot, actions.ReturnToTeamBuilder, actions.RetryBattle);

        RenderWorldLabels(snapshot);
        RenderSkillPopup(snapshot, actions.SelectSelectedUnitSkill);
    }

    public override void BuildDefaultHierarchy()
    {
        worldLabelViews.Clear();
        modeSelectScreen = null;
        teamBuilderScreen = null;
        planningScreen = null;
        resolvingScreen = null;
        victoryOverlay = null;
        skillPopup = null;

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

        floatingHudLayer = HexTacticsUiFactory.CreateRect("FloatingHud", rootLayer);
        HexTacticsUiFactory.Stretch(floatingHudLayer, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(floatingHudLayer, 0f, 0f, 0f, 0f);

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

        skillPopup = HexTacticsUiFactory.InstantiateView(
            skillPopupPrefab,
            HexTacticsUiResourcePaths.SkillPopup,
            floatingHudLayer,
            HexTacticsSkillPopupView.CreateStandalone);
    }

    private void RenderPanels(HexTacticsUiSnapshot snapshot)
    {
        modeSelectScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.ModeSelect);
        teamBuilderScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.TeamBuilder);
        planningScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.Planning);
        resolvingScreen.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.Resolving);
        victoryOverlay.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.Victory);
        worldLabelLayer.gameObject.SetActive(
            snapshot.FlowState == HexTacticsUiFlowState.Planning ||
            snapshot.FlowState == HexTacticsUiFlowState.Resolving ||
            snapshot.FlowState == HexTacticsUiFlowState.Victory);
        floatingHudLayer.gameObject.SetActive(snapshot.FlowState == HexTacticsUiFlowState.Planning);
        skillPopup.gameObject.SetActive(
            snapshot.FlowState == HexTacticsUiFlowState.Planning &&
            snapshot.IsSkillPopupOpen &&
            snapshot.SelectedUnitSkillEntries.Count > 0);
    }

    private void ApplyResponsiveLayout()
    {
        ApplyCanvasScale();
        ApplySafeArea();
        ApplyScreenRects();
    }

    private void ApplyCanvasScale()
    {
        var displaySize = canvas != null ? canvas.renderingDisplaySize : new Vector2(Screen.width, Screen.height);
        if (canvasScaler == null || displaySize.y <= 0)
        {
            return;
        }

        var aspect = displaySize.x / displaySize.y;
        if (displaySize.y > displaySize.x * 1.05f)
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
        floatingHudLayer.anchorMin = safeAreaLayer.anchorMin;
        floatingHudLayer.anchorMax = safeAreaLayer.anchorMax;
        floatingHudLayer.offsetMin = Vector2.zero;
        floatingHudLayer.offsetMax = Vector2.zero;
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
        var battlePanelWidth = Mathf.Min(safeWidth - screenMargin * 2f, Mathf.Clamp(safeWidth * 0.245f, 400f, 448f));

        ConfigureCenteredCard(
            modeSelectScreen.Root,
            Mathf.Min(safeWidth - screenMargin * 2f, isPortrait ? 520f : Mathf.Clamp(safeWidth * 0.30f, 460f, 520f)),
            360f,
            new Vector2(0f, Mathf.Clamp(safeHeight * 0.03f, 16f, 40f)));

        HexTacticsUiFactory.Stretch(teamBuilderScreen.Root, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(teamBuilderScreen.Root, screenMargin, screenMargin, screenMargin, screenMargin);

        if (isPortrait)
        {
            ConfigureBottomCenteredCard(
                planningScreen.Root,
                safeWidth - screenMargin * 2f,
                Mathf.Clamp(safeHeight * 0.30f, 432f, 492f),
                screenMargin);

            ConfigureTopCenteredCard(
                resolvingScreen.Root,
                Mathf.Clamp(safeWidth * 0.78f, 300f, 400f),
                184f,
                screenMargin);
        }
        else
        {
            ConfigureTopLeftCard(
                planningScreen.Root,
                battlePanelWidth,
                Mathf.Clamp(safeHeight * 0.44f, 448f, 492f),
                screenMargin,
                screenMargin);

            ConfigureTopLeftCard(
                resolvingScreen.Root,
                Mathf.Clamp(battlePanelWidth * 0.92f, 300f, 376f),
                184f,
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

            // WorldLabels spans the canvas. Map its pixel viewport directly to
            // its local rect, avoiding the camera canvas plane's transform,
            // which can still reflect the previous camera pose during layout.
            var viewport = canvas.pixelRect;
            if (viewport.width > 0f && viewport.height > 0f)
            {
                var normalized = new Vector2(
                    (screenPoint.x - viewport.xMin) / viewport.width,
                    (screenPoint.y - viewport.yMin) / viewport.height);
                var layerRect = worldLabelLayer.rect;
                var localPoint = new Vector2(
                    layerRect.xMin + normalized.x * layerRect.width,
                    layerRect.yMin + normalized.y * layerRect.height);
                var targetPosition = localPoint + new Vector2(0f, 14f);
                worldLabelViews[i].RectTransform.anchoredPosition = ClampWorldLabelPosition(worldLabelViews[i].RectTransform, targetPosition);
            }
        }
    }

    private void RenderSkillPopup(HexTacticsUiSnapshot snapshot, Action<int> onSelectSkill)
    {
        if (skillPopup == null || !skillPopup.gameObject.activeSelf)
        {
            return;
        }

        skillPopup.Bind(snapshot.SkillPopupTitle, snapshot.SelectedUnitSkillEntries, onSelectSkill);
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(floatingHudLayer, snapshot.SkillPopupScreenPosition, uiCamera, out var localPoint))
        {
            skillPopup.Root.anchoredPosition = ClampFloatingHudPosition(skillPopup.Root, localPoint);
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

    private Vector2 ClampFloatingHudPosition(RectTransform panel, Vector2 position)
    {
        var layerRect = floatingHudLayer.rect;
        var panelSize = panel.rect.size;
        var minX = layerRect.xMin + 12f + panelSize.x * panel.pivot.x;
        var maxX = layerRect.xMax - 12f - panelSize.x * (1f - panel.pivot.x);
        var minY = layerRect.yMin + 12f + panelSize.y * panel.pivot.y;
        var maxY = layerRect.yMax - 12f - panelSize.y * (1f - panel.pivot.y);
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
