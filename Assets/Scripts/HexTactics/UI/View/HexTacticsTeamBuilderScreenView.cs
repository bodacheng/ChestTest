using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsTeamBuilderScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text budgetText;
    [SerializeField] private Text selectionHintText;
    [SerializeField] private RectTransform rosterContentRoot;
    [SerializeField] private RectTransform selectedRosterContentRoot;
    [SerializeField] private Text costText;
    [SerializeField] private Text cpuHintText;
    [SerializeField] private Text emptyText;
    [SerializeField] private Text statusText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button startButton;
    [SerializeField] private HexTacticsRosterRowView rosterRowPrefab;
    [SerializeField] private HexTacticsSelectedRosterRowView selectedRosterRowPrefab;

    private readonly List<HexTacticsRosterRowView> rosterRows = new();
    private readonly List<HexTacticsSelectedRosterRowView> selectedRosterRows = new();

    protected override int CurrentLayoutVersion => 1;

    protected override bool HasCurrentBindings =>
        budgetText != null &&
        selectionHintText != null &&
        rosterContentRoot != null &&
        selectedRosterContentRoot != null &&
        costText != null &&
        cpuHintText != null &&
        emptyText != null &&
        statusText != null &&
        backButton != null &&
        startButton != null;

    public RectTransform Root => (RectTransform)transform;

    public void ConfigureItemPrefabs(HexTacticsRosterRowView rosterPrefab, HexTacticsSelectedRosterRowView selectedPrefab)
    {
        rosterRowPrefab = rosterPrefab;
        selectedRosterRowPrefab = selectedPrefab;
    }

    public void Bind(
        HexTacticsUiSnapshot snapshot,
        Action returnToModeSelect,
        Action<int> addRosterEntry,
        Action<int> removeSelectionEntry,
        Action startBattle)
    {
        EnsureBuilt();

        budgetText.text = $"玩家总 cost 上限：{snapshot.PlayerCostLimit}    CPU 总 cost 上限：{snapshot.CpuCostLimit}";
        selectionHintText.text = $"当前已选：{snapshot.PlayerSelectionEntries.Count}    同角色可重复选择，直到 cost 用尽";
        costText.text = $"已用 cost：{snapshot.PlayerUsedCost} / {snapshot.PlayerCostLimit}";
        cpuHintText.text = "CPU 会按同一套角色池自动组队";
        emptyText.gameObject.SetActive(snapshot.PlayerSelectionEntries.Count == 0);
        emptyText.text = "还没有选择任何角色";
        statusText.text = snapshot.BuilderStatus;
        statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(snapshot.BuilderStatus));
        startButton.interactable = snapshot.CanStartBattle;

        HexTacticsUiFactory.BindButton(backButton, returnToModeSelect);
        HexTacticsUiFactory.BindButton(startButton, startBattle);

        HexTacticsUiFactory.EnsurePool(
            rosterRows,
            snapshot.RosterEntries.Count,
            rosterRowPrefab,
            HexTacticsUiResourcePaths.RosterRow,
            rosterContentRoot,
            HexTacticsRosterRowView.CreateStandalone);

        for (var i = 0; i < rosterRows.Count; i++)
        {
            var active = i < snapshot.RosterEntries.Count;
            rosterRows[i].gameObject.SetActive(active);
            if (active)
            {
                rosterRows[i].Bind(snapshot.RosterEntries[i], addRosterEntry);
            }
        }

        HexTacticsUiFactory.EnsurePool(
            selectedRosterRows,
            snapshot.PlayerSelectionEntries.Count,
            selectedRosterRowPrefab,
            HexTacticsUiResourcePaths.SelectedRosterRow,
            selectedRosterContentRoot,
            HexTacticsSelectedRosterRowView.CreateStandalone);

        for (var i = 0; i < selectedRosterRows.Count; i++)
        {
            var active = i < snapshot.PlayerSelectionEntries.Count;
            selectedRosterRows[i].gameObject.SetActive(active);
            if (active)
            {
                selectedRosterRows[i].Bind(snapshot.PlayerSelectionEntries[i], removeSelectionEntry);
            }
        }
    }

    public override void BuildDefaultHierarchy()
    {
        rosterRows.Clear();
        selectedRosterRows.Clear();
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        HexTacticsUiFactory.Stretch(root, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(root, 18f, 18f, 18f, 18f);

        var leftPanel = HexTacticsUiFactory.CreatePanel(root, "RosterPanel", new Color(0.05f, 0.08f, 0.10f, 0.86f), Vector2.zero, new Vector2(0f, 0f), new Vector2(0.47f, 1f));
        HexTacticsUiFactory.SetOffsets(leftPanel, 0f, 0f, 8f, 0f);

        var leftHeader = HexTacticsUiFactory.CreateSection(leftPanel, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), 124f);
        var leftHeaderLayout = leftHeader.gameObject.AddComponent<VerticalLayoutGroup>();
        leftHeaderLayout.padding = new RectOffset(18, 18, 18, 6);
        leftHeaderLayout.spacing = 8f;
        leftHeaderLayout.childAlignment = TextAnchor.UpperLeft;
        leftHeaderLayout.childControlHeight = false;
        leftHeaderLayout.childControlWidth = true;
        leftHeaderLayout.childForceExpandHeight = false;
        leftHeaderLayout.childForceExpandWidth = true;

        var leftTitle = HexTacticsUiFactory.CreateText(leftHeader, "Title", "角色选择", 24, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(leftTitle.gameObject, preferredHeight: 30f);
        budgetText = HexTacticsUiFactory.CreateText(leftHeader, "BudgetText", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 0.92f));
        HexTacticsUiFactory.AddLayoutElement(budgetText.gameObject, preferredHeight: 22f);
        selectionHintText = HexTacticsUiFactory.CreateText(leftHeader, "SelectionHintText", string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(selectionHintText.gameObject, preferredHeight: 20f);

        var leftScroll = HexTacticsUiFactory.CreateRect("RosterScroll", leftPanel);
        HexTacticsUiFactory.Stretch(leftScroll, new Vector2(0f, 0f), new Vector2(1f, 1f));
        HexTacticsUiFactory.SetOffsets(leftScroll, 18f, 18f, 18f, 138f);
        var leftScrollRoot = HexTacticsUiFactory.CreateScrollView(leftScroll, "ScrollRoot", out _, out rosterContentRoot);
        HexTacticsUiFactory.Stretch(leftScrollRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(leftScrollRoot, 0f, 0f, 0f, 0f);

        var rightPanel = HexTacticsUiFactory.CreatePanel(root, "SelectionPanel", new Color(0.05f, 0.08f, 0.10f, 0.86f), Vector2.zero, new Vector2(0.47f, 0f), new Vector2(1f, 1f));
        HexTacticsUiFactory.SetOffsets(rightPanel, 8f, 0f, 0f, 0f);

        var rightHeader = HexTacticsUiFactory.CreateSection(rightPanel, "Header", new Vector2(0f, 1f), new Vector2(1f, 1f), 116f);
        var rightHeaderLayout = rightHeader.gameObject.AddComponent<VerticalLayoutGroup>();
        rightHeaderLayout.padding = new RectOffset(18, 18, 18, 6);
        rightHeaderLayout.spacing = 6f;
        rightHeaderLayout.childAlignment = TextAnchor.UpperLeft;
        rightHeaderLayout.childControlHeight = false;
        rightHeaderLayout.childControlWidth = true;
        rightHeaderLayout.childForceExpandHeight = false;
        rightHeaderLayout.childForceExpandWidth = true;

        var rightTitle = HexTacticsUiFactory.CreateText(rightHeader, "Title", "我的队伍", 24, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(rightTitle.gameObject, preferredHeight: 30f);
        costText = HexTacticsUiFactory.CreateText(rightHeader, "CostText", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 0.92f));
        HexTacticsUiFactory.AddLayoutElement(costText.gameObject, preferredHeight: 22f);
        cpuHintText = HexTacticsUiFactory.CreateText(rightHeader, "CpuHintText", string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(cpuHintText.gameObject, preferredHeight: 20f);

        var selectedScroll = HexTacticsUiFactory.CreateRect("SelectedScroll", rightPanel);
        HexTacticsUiFactory.Stretch(selectedScroll, new Vector2(0f, 0f), new Vector2(1f, 1f));
        HexTacticsUiFactory.SetOffsets(selectedScroll, 18f, 120f, 18f, 128f);
        var selectedScrollRoot = HexTacticsUiFactory.CreateScrollView(selectedScroll, "ScrollRoot", out _, out selectedRosterContentRoot);
        HexTacticsUiFactory.Stretch(selectedScrollRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(selectedScrollRoot, 0f, 0f, 0f, 0f);

        emptyText = HexTacticsUiFactory.CreateText(rightPanel, "EmptyText", "还没有选择任何角色", 16, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.88f));
        emptyText.rectTransform.anchorMin = new Vector2(0f, 1f);
        emptyText.rectTransform.anchorMax = new Vector2(1f, 1f);
        emptyText.rectTransform.pivot = new Vector2(0.5f, 1f);
        emptyText.rectTransform.offsetMin = new Vector2(18f, -156f);
        emptyText.rectTransform.offsetMax = new Vector2(-18f, -132f);

        var footer = HexTacticsUiFactory.CreateSection(rightPanel, "Footer", new Vector2(0f, 0f), new Vector2(1f, 0f), 102f);
        var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
        footerLayout.padding = new RectOffset(18, 18, 10, 14);
        footerLayout.spacing = 10f;
        footerLayout.childAlignment = TextAnchor.UpperLeft;
        footerLayout.childControlHeight = false;
        footerLayout.childControlWidth = true;
        footerLayout.childForceExpandHeight = false;
        footerLayout.childForceExpandWidth = true;

        statusText = HexTacticsUiFactory.CreateText(footer, "BuilderStatus", string.Empty, 16, TextAnchor.MiddleLeft, new Color(1.0f, 0.90f, 0.72f));
        HexTacticsUiFactory.AddLayoutElement(statusText.gameObject, preferredHeight: 38f);

        var footerButtons = HexTacticsUiFactory.CreateRect("Buttons", footer);
        HexTacticsUiFactory.AddLayoutElement(footerButtons.gameObject, preferredHeight: 38f);
        var footerButtonsLayout = footerButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerButtonsLayout.spacing = 12f;
        footerButtonsLayout.childAlignment = TextAnchor.MiddleCenter;
        footerButtonsLayout.childControlHeight = true;
        footerButtonsLayout.childControlWidth = true;
        footerButtonsLayout.childForceExpandHeight = false;
        footerButtonsLayout.childForceExpandWidth = true;

        backButton = HexTacticsUiFactory.CreateButton(footerButtons, "BackButton", "返回模式", new Color(0.25f, 0.31f, 0.36f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(backButton.gameObject, preferredHeight: 38f);

        startButton = HexTacticsUiFactory.CreateButton(footerButtons, "StartButton", "开始对战", new Color(0.22f, 0.56f, 0.55f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(startButton.gameObject, preferredHeight: 38f);
    }

    public static HexTacticsTeamBuilderScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsTeamBuilderScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsTeamBuilderScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
