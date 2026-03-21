using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsTeamBuilderScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private RectTransform summaryPanel;
    [SerializeField] private RectTransform rosterPanel;
    [SerializeField] private RectTransform selectionPanel;
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
    [SerializeField] private RectTransform dragPreviewRoot;
    [SerializeField] private Image dragPreviewBackground;
    [SerializeField] private HexTacticsAvatarView dragPreviewAvatarView;
    [SerializeField] private Text dragPreviewLabel;
    [SerializeField] private HexTacticsRosterRowView rosterRowPrefab;
    [SerializeField] private HexTacticsSelectedRosterRowView selectedRosterRowPrefab;

    private readonly List<HexTacticsRosterRowView> rosterRows = new();
    private readonly List<HexTacticsSelectedRosterRowView> selectedRosterRows = new();
    private Action<int, Vector2> placeRosterCharacterHandler;
    private Action<int, Vector2> movePlacedCharacterHandler;
    private int draggingRosterIndex = -1;
    private int draggingEntryId = -1;

    protected override int CurrentLayoutVersion => 11;

    protected override bool HasCurrentBindings =>
        summaryPanel != null &&
        rosterPanel != null &&
        selectionPanel != null &&
        budgetText != null &&
        selectionHintText != null &&
        rosterContentRoot != null &&
        selectedRosterContentRoot != null &&
        costText != null &&
        emptyText != null &&
        statusText != null &&
        backButton != null &&
        startButton != null &&
        dragPreviewRoot != null &&
        dragPreviewBackground != null &&
        dragPreviewAvatarView != null &&
        dragPreviewLabel != null;

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
        Action<int, Vector2> placeRosterCharacterAt,
        Action<int> removeSelectionEntry,
        Action<int, Vector2> moveSelectionEntryTo,
        Action startBattle)
    {
        EnsureBuilt();
        placeRosterCharacterHandler = placeRosterCharacterAt;
        movePlacedCharacterHandler = moveSelectionEntryTo;

        budgetText.text = $"预算  蓝 {snapshot.PlayerCostLimit}  |  红 {snapshot.CpuCostLimit}";
        selectionHintText.text = "从左侧选择角色拖到蓝色部署格，已部署角色也可拖回调整";
        costText.text = $"已部署 {snapshot.PlayerSelectionEntries.Count} 名  |  费用 {snapshot.PlayerUsedCost}/{snapshot.PlayerCostLimit}";
        if (cpuHintText != null)
        {
            cpuHintText.gameObject.SetActive(false);
        }
        emptyText.gameObject.SetActive(snapshot.PlayerSelectionEntries.Count == 0);
        emptyText.text = "尚未部署角色";
        statusText.text = snapshot.BuilderStatus;
        statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(snapshot.BuilderStatus));
        startButton.interactable = snapshot.CanStartBattle;
        ApplyResponsiveLayout();

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
                rosterRows[i].Bind(snapshot.RosterEntries[i], addRosterEntry, BeginRosterDrag, UpdateDragPreview, EndDrag);
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
                selectedRosterRows[i].Bind(snapshot.PlayerSelectionEntries[i], removeSelectionEntry, BeginPlacedDrag, UpdateDragPreview, EndDrag);
            }
        }

        HexTacticsUiFactory.ForceRebuildLayout(rosterContentRoot);
        HexTacticsUiFactory.ForceRebuildLayout(selectedRosterContentRoot);
        HexTacticsUiFactory.ForceRebuildLayout(Root);
    }

    public override void BuildDefaultHierarchy()
    {
        rosterRows.Clear();
        selectedRosterRows.Clear();
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        HexTacticsUiFactory.Stretch(root, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(root, 0f, 0f, 0f, 0f);

        summaryPanel = HexTacticsUiFactory.CreateRect("SummaryPanel", root);
        var summaryPanelImage = HexTacticsUiFactory.AddImage(summaryPanel.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.78f));
        HexTacticsUiFactory.StylePanel(summaryPanelImage, new Color(1f, 1f, 1f, 0.05f));

        rosterPanel = HexTacticsUiFactory.CreateRect("RosterPanel", root);
        var leftPanelImage = HexTacticsUiFactory.AddImage(rosterPanel.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.78f));
        HexTacticsUiFactory.StylePanel(leftPanelImage, new Color(1f, 1f, 1f, 0.05f));

        selectionPanel = HexTacticsUiFactory.CreateRect("SelectionPanel", root);
        var rightPanelImage = HexTacticsUiFactory.AddImage(selectionPanel.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.78f));
        HexTacticsUiFactory.StylePanel(rightPanelImage, new Color(1f, 1f, 1f, 0.05f));

        BuildSummaryPanel(summaryPanel);
        BuildLeftPanel(rosterPanel);
        BuildRightPanel(selectionPanel);
        BuildDragPreview(root);
    }

    public static HexTacticsTeamBuilderScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsTeamBuilderScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsTeamBuilderScreenView>();
        view.EnsureBuilt();
        return view;
    }

    private void BuildSummaryPanel(RectTransform panel)
    {
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 18, 18);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(panel, "Title", "战前布阵", 28, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 34f);
        budgetText = HexTacticsUiFactory.CreateText(panel, "BudgetText", string.Empty, 18, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(budgetText.gameObject, preferredHeight: 24f);
        costText = HexTacticsUiFactory.CreateText(panel, "CostText", string.Empty, 18, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(costText.gameObject, preferredHeight: 24f);
        selectionHintText = HexTacticsUiFactory.CreateText(panel, "SelectionHintText", string.Empty, 15, TextAnchor.UpperLeft, new Color(0.62f, 0.72f, 0.76f));
        HexTacticsUiFactory.AddLayoutElement(selectionHintText.gameObject, preferredHeight: 40f);
    }

    private void BuildLeftPanel(RectTransform leftPanel)
    {
        var header = HexTacticsUiFactory.CreateRect("Header", leftPanel);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 68f);
        header.anchoredPosition = Vector2.zero;
        var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
        headerLayout.padding = new RectOffset(18, 18, 18, 10);
        headerLayout.spacing = 0f;
        headerLayout.childAlignment = TextAnchor.UpperLeft;
        headerLayout.childControlHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(header, "Title", "角色仓库", 24, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 28f);

        var leftScroll = HexTacticsUiFactory.CreateRect("RosterScroll", leftPanel);
        leftScroll.anchorMin = new Vector2(0f, 0f);
        leftScroll.anchorMax = new Vector2(1f, 1f);
        leftScroll.pivot = new Vector2(0.5f, 0.5f);
        HexTacticsUiFactory.SetOffsets(leftScroll, 16f, 16f, 16f, 76f);
        var leftScrollRoot = HexTacticsUiFactory.CreateScrollView(leftScroll, "ScrollRoot", out _, out rosterContentRoot);
        HexTacticsUiFactory.Stretch(leftScrollRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(leftScrollRoot, 0f, 0f, 0f, 0f);
    }

    private void BuildRightPanel(RectTransform rightPanel)
    {
        var header = HexTacticsUiFactory.CreateRect("Header", rightPanel);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 60f);
        header.anchoredPosition = Vector2.zero;
        var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
        headerLayout.padding = new RectOffset(18, 18, 16, 12);
        headerLayout.spacing = 0f;
        headerLayout.childAlignment = TextAnchor.UpperLeft;
        headerLayout.childControlHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(header, "Title", "蓝方部署", 24, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 28f);
        cpuHintText = HexTacticsUiFactory.CreateText(header, "CpuHintText", string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.62f, 0.72f, 0.76f));
        cpuHintText.gameObject.SetActive(false);
        HexTacticsUiFactory.AddLayoutElement(cpuHintText.gameObject, preferredHeight: 0f);

        var selectedScroll = HexTacticsUiFactory.CreateRect("SelectedScroll", rightPanel);
        selectedScroll.anchorMin = new Vector2(0f, 0f);
        selectedScroll.anchorMax = new Vector2(1f, 1f);
        selectedScroll.pivot = new Vector2(0.5f, 0.5f);
        HexTacticsUiFactory.SetOffsets(selectedScroll, 16f, 108f, 16f, 72f);
        var selectedScrollRoot = HexTacticsUiFactory.CreateScrollView(selectedScroll, "ScrollRoot", out _, out selectedRosterContentRoot);
        HexTacticsUiFactory.Stretch(selectedScrollRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(selectedScrollRoot, 0f, 0f, 0f, 0f);

        emptyText = HexTacticsUiFactory.CreateText(rightPanel, "EmptyText", "尚未部署角色", 18, TextAnchor.UpperLeft, new Color(0.74f, 0.80f, 0.84f));
        emptyText.rectTransform.anchorMin = new Vector2(0f, 1f);
        emptyText.rectTransform.anchorMax = new Vector2(1f, 1f);
        emptyText.rectTransform.pivot = new Vector2(0.5f, 1f);
        emptyText.rectTransform.offsetMin = new Vector2(18f, -74f);
        emptyText.rectTransform.offsetMax = new Vector2(-18f, -40f);

        var footer = HexTacticsUiFactory.CreateRect("Footer", rightPanel);
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.sizeDelta = new Vector2(0f, 100f);
        footer.anchoredPosition = Vector2.zero;
        var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
        footerLayout.padding = new RectOffset(18, 18, 10, 12);
        footerLayout.spacing = 8f;
        footerLayout.childAlignment = TextAnchor.UpperLeft;
        footerLayout.childControlHeight = false;
        footerLayout.childControlWidth = true;
        footerLayout.childForceExpandHeight = false;
        footerLayout.childForceExpandWidth = true;

        statusText = HexTacticsUiFactory.CreateText(footer, "BuilderStatus", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.96f, 0.87f, 0.70f));
        HexTacticsUiFactory.AddLayoutElement(statusText.gameObject, preferredHeight: 24f);

        var footerButtons = HexTacticsUiFactory.CreateRect("Buttons", footer);
        HexTacticsUiFactory.AddLayoutElement(footerButtons.gameObject, preferredHeight: 46f);
        var footerButtonsLayout = footerButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerButtonsLayout.spacing = 12f;
        footerButtonsLayout.childAlignment = TextAnchor.MiddleCenter;
        footerButtonsLayout.childControlHeight = true;
        footerButtonsLayout.childControlWidth = true;
        footerButtonsLayout.childForceExpandHeight = false;
        footerButtonsLayout.childForceExpandWidth = true;

        backButton = HexTacticsUiFactory.CreateButton(footerButtons, "BackButton", "返回", new Color(0.23f, 0.28f, 0.32f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(backButton.gameObject, preferredHeight: 46f);

        startButton = HexTacticsUiFactory.CreateButton(footerButtons, "StartButton", "开始", new Color(0.19f, 0.46f, 0.46f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(startButton.gameObject, preferredHeight: 46f);
    }

    private void BuildDragPreview(RectTransform root)
    {
        dragPreviewRoot = HexTacticsUiFactory.CreateRect("DragPreview", root);
        dragPreviewRoot.anchorMin = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.anchorMax = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.pivot = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.sizeDelta = new Vector2(168f, 60f);
        dragPreviewRoot.gameObject.SetActive(false);

        dragPreviewBackground = HexTacticsUiFactory.AddImage(dragPreviewRoot.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.88f), false);
        HexTacticsUiFactory.StylePanel(dragPreviewBackground, new Color(1f, 1f, 1f, 0.05f), 0f);

        var layout = dragPreviewRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", dragPreviewRoot);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 40f, preferredHeight: 40f);
        dragPreviewAvatarView = HexTacticsAvatarView.CreateStandalone(avatarRoot, "AvatarView", 3f, 16, false);

        dragPreviewLabel = HexTacticsUiFactory.CreateText(dragPreviewRoot, "Label", string.Empty, 15, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(dragPreviewLabel.gameObject, flexibleWidth: 1f, preferredHeight: 22f);
    }

    private void BeginRosterDrag(HexTacticsRosterEntryUiData data, Vector2 screenPosition)
    {
        draggingRosterIndex = data.RosterIndex;
        draggingEntryId = -1;
        ShowDragPreview(data.Avatar, data.DisplayName, screenPosition);
    }

    private void BeginPlacedDrag(HexTacticsSelectionEntryUiData data, Vector2 screenPosition)
    {
        draggingRosterIndex = -1;
        draggingEntryId = data.EntryId;
        ShowDragPreview(data.Avatar, data.DisplayName, screenPosition);
    }

    private void UpdateDragPreview(Vector2 screenPosition)
    {
        if (dragPreviewRoot == null || !dragPreviewRoot.gameObject.activeSelf)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screenPosition, null, out var localPoint))
        {
            dragPreviewRoot.anchoredPosition = localPoint + new Vector2(0f, 18f);
        }
    }

    private void EndDrag(Vector2 screenPosition)
    {
        if (draggingRosterIndex >= 0)
        {
            placeRosterCharacterHandler?.Invoke(draggingRosterIndex, screenPosition);
        }
        else if (draggingEntryId >= 0)
        {
            movePlacedCharacterHandler?.Invoke(draggingEntryId, screenPosition);
        }

        draggingRosterIndex = -1;
        draggingEntryId = -1;
        if (dragPreviewRoot != null)
        {
            dragPreviewRoot.gameObject.SetActive(false);
        }
    }

    private void ShowDragPreview(HexTacticsAvatarUiData avatar, string label, Vector2 screenPosition)
    {
        if (dragPreviewRoot == null)
        {
            return;
        }

        dragPreviewAvatarView.Bind(avatar);
        dragPreviewLabel.text = label;
        dragPreviewRoot.gameObject.SetActive(true);
        dragPreviewRoot.SetAsLastSibling();
        UpdateDragPreview(screenPosition);
    }

    private void ApplyResponsiveLayout()
    {
        var width = Mathf.Max(1f, Root.rect.width);
        var height = Mathf.Max(1f, Root.rect.height);
        var isPortrait = height > width * 1.05f;
        var margin = Mathf.Clamp(width * 0.016f, 10f, 20f);

        if (isPortrait)
        {
            summaryPanel.anchorMin = new Vector2(0f, 1f);
            summaryPanel.anchorMax = new Vector2(1f, 1f);
            summaryPanel.pivot = new Vector2(0.5f, 1f);
            summaryPanel.sizeDelta = new Vector2(0f, 186f);
            summaryPanel.offsetMin = new Vector2(margin, -186f - margin);
            summaryPanel.offsetMax = new Vector2(-margin, -margin);

            var panelHeight = Mathf.Clamp(height * 0.33f, 300f, 430f);

            rosterPanel.anchorMin = new Vector2(0f, 0f);
            rosterPanel.anchorMax = new Vector2(0.5f, 0f);
            rosterPanel.pivot = new Vector2(0f, 0f);
            rosterPanel.sizeDelta = new Vector2(0f, panelHeight);
            rosterPanel.offsetMin = new Vector2(margin, margin);
            rosterPanel.offsetMax = new Vector2(-margin * 0.5f, margin + panelHeight);

            selectionPanel.anchorMin = new Vector2(0.5f, 0f);
            selectionPanel.anchorMax = new Vector2(1f, 0f);
            selectionPanel.pivot = new Vector2(1f, 0f);
            selectionPanel.sizeDelta = new Vector2(0f, panelHeight);
            selectionPanel.offsetMin = new Vector2(margin * 0.5f, margin);
            selectionPanel.offsetMax = new Vector2(-margin, margin + panelHeight);
            return;
        }

        summaryPanel.anchorMin = new Vector2(0.5f, 1f);
        summaryPanel.anchorMax = new Vector2(0.5f, 1f);
        summaryPanel.pivot = new Vector2(0.5f, 1f);
        summaryPanel.sizeDelta = new Vector2(Mathf.Clamp(width * 0.34f, 460f, 680f), 186f);
        summaryPanel.anchoredPosition = new Vector2(0f, -margin);

        rosterPanel.anchorMin = new Vector2(0f, 0f);
        rosterPanel.anchorMax = new Vector2(0f, 1f);
        rosterPanel.pivot = new Vector2(0f, 0.5f);
        rosterPanel.sizeDelta = new Vector2(Mathf.Clamp(width * 0.25f, 320f, 420f), 0f);
        rosterPanel.anchoredPosition = new Vector2(margin, 0f);

        selectionPanel.anchorMin = new Vector2(1f, 0f);
        selectionPanel.anchorMax = new Vector2(1f, 1f);
        selectionPanel.pivot = new Vector2(1f, 0.5f);
        selectionPanel.sizeDelta = new Vector2(Mathf.Clamp(width * 0.27f, 340f, 440f), 0f);
        selectionPanel.anchoredPosition = new Vector2(-margin, 0f);
    }
}
