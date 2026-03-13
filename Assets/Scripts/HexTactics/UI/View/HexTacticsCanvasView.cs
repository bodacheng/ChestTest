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

    protected override int CurrentLayoutVersion => 3;

    protected override bool HasCurrentBindings =>
        canvas != null &&
        canvasScaler != null &&
        graphicRaycaster != null &&
        rootLayer != null &&
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
            Action<int> removeSelectionEntry,
            Action startBattle,
            Action clearSelection,
            Action waitSelectedUnit,
            Action confirmCommands,
            Action<int> selectCommandUnit,
            Action<int> waitCommandUnit,
            Action returnToTeamBuilder,
            Action retryBattle)
        {
            StartCpuMode = startCpuMode;
            ReturnToModeSelect = returnToModeSelect;
            AddRosterEntry = addRosterEntry;
            RemoveSelectionEntry = removeSelectionEntry;
            StartBattle = startBattle;
            ClearSelection = clearSelection;
            WaitSelectedUnit = waitSelectedUnit;
            ConfirmCommands = confirmCommands;
            SelectCommandUnit = selectCommandUnit;
            WaitCommandUnit = waitCommandUnit;
            ReturnToTeamBuilder = returnToTeamBuilder;
            RetryBattle = retryBattle;
        }

        public Action StartCpuMode { get; }
        public Action ReturnToModeSelect { get; }
        public Action<int> AddRosterEntry { get; }
        public Action<int> RemoveSelectionEntry { get; }
        public Action StartBattle { get; }
        public Action ClearSelection { get; }
        public Action WaitSelectedUnit { get; }
        public Action ConfirmCommands { get; }
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
        RenderPanels(snapshot);

        modeSelectScreen.Bind(actions.StartCpuMode);
        teamBuilderScreen.Bind(snapshot, actions.ReturnToModeSelect, actions.AddRosterEntry, actions.RemoveSelectionEntry, actions.StartBattle);
        planningScreen.Bind(snapshot, actions.ClearSelection, actions.WaitSelectedUnit, actions.ConfirmCommands, actions.SelectCommandUnit, actions.WaitCommandUnit);
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
        HexTacticsUiFactory.AddImage(backdrop.gameObject, new Color(0.01f, 0.03f, 0.04f, 0.04f), false);

        modeSelectScreen = HexTacticsUiFactory.InstantiateView(
            modeSelectScreenPrefab,
            HexTacticsUiResourcePaths.ModeSelectScreen,
            rootLayer,
            HexTacticsModeSelectScreenView.CreateStandalone);

        teamBuilderScreen = HexTacticsUiFactory.InstantiateView(
            teamBuilderScreenPrefab,
            HexTacticsUiResourcePaths.TeamBuilderScreen,
            rootLayer,
            HexTacticsTeamBuilderScreenView.CreateStandalone);

        planningScreen = HexTacticsUiFactory.InstantiateView(
            planningScreenPrefab,
            HexTacticsUiResourcePaths.PlanningScreen,
            rootLayer,
            HexTacticsPlanningScreenView.CreateStandalone);

        resolvingScreen = HexTacticsUiFactory.InstantiateView(
            resolvingScreenPrefab,
            HexTacticsUiResourcePaths.ResolvingScreen,
            rootLayer,
            HexTacticsResolvingScreenView.CreateStandalone);

        worldLabelLayer = HexTacticsUiFactory.CreateRect("WorldLabels", rootLayer);
        HexTacticsUiFactory.Stretch(worldLabelLayer, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(worldLabelLayer, 0f, 0f, 0f, 0f);

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
                worldLabelViews[i].RectTransform.anchoredPosition = localPoint;
            }
        }
    }
}
