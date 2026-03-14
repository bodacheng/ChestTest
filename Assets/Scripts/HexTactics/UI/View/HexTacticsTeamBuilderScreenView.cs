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
    [SerializeField] private Image dragPreviewAvatarBackground;
    [SerializeField] private Image dragPreviewAvatar;
    [SerializeField] private Text dragPreviewFallbackText;
    [SerializeField] private Text dragPreviewLabel;
    [SerializeField] private HexTacticsRosterRowView rosterRowPrefab;
    [SerializeField] private HexTacticsSelectedRosterRowView selectedRosterRowPrefab;

    private readonly List<HexTacticsRosterRowView> rosterRows = new();
    private readonly List<HexTacticsSelectedRosterRowView> selectedRosterRows = new();
    private Action<int, Vector2> placeRosterCharacterHandler;
    private Action<int, Vector2> movePlacedCharacterHandler;
    private int draggingRosterIndex = -1;
    private int draggingEntryId = -1;

    protected override int CurrentLayoutVersion => 6;

    protected override bool HasCurrentBindings =>
        summaryPanel != null &&
        rosterPanel != null &&
        selectionPanel != null &&
        budgetText != null &&
        selectionHintText != null &&
        rosterContentRoot != null &&
        selectedRosterContentRoot != null &&
        costText != null &&
        cpuHintText != null &&
        emptyText != null &&
        statusText != null &&
        backButton != null &&
        startButton != null &&
        dragPreviewRoot != null &&
        dragPreviewBackground != null &&
        dragPreviewAvatarBackground != null &&
        dragPreviewAvatar != null &&
        dragPreviewFallbackText != null &&
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

        budgetText.text = $"玩家总 cost 上限：{snapshot.PlayerCostLimit}    CPU 总 cost 上限：{snapshot.CpuCostLimit}";
        selectionHintText.text = "把左侧角色头像拖到中间棋盘蓝色高亮格上，直接设置初期站位";
        costText.text = $"已放置 {snapshot.PlayerSelectionEntries.Count} 名角色  |  已用 cost：{snapshot.PlayerUsedCost} / {snapshot.PlayerCostLimit}";
        cpuHintText.text = "右侧已放置角色也可以继续拖回棋盘重新摆位；CPU 会自动在红方部署区组队";
        emptyText.gameObject.SetActive(snapshot.PlayerSelectionEntries.Count == 0);
        emptyText.text = "还没有放置任何角色";
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
        var summaryPanelImage = HexTacticsUiFactory.AddImage(summaryPanel.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.86f));
        HexTacticsUiFactory.StylePanel(summaryPanelImage, new Color(1f, 1f, 1f, 0.08f));

        rosterPanel = HexTacticsUiFactory.CreateRect("RosterPanel", root);
        var leftPanelImage = HexTacticsUiFactory.AddImage(rosterPanel.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.86f));
        HexTacticsUiFactory.StylePanel(leftPanelImage, new Color(1f, 1f, 1f, 0.08f));

        selectionPanel = HexTacticsUiFactory.CreateRect("SelectionPanel", root);
        var rightPanelImage = HexTacticsUiFactory.AddImage(selectionPanel.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.86f));
        HexTacticsUiFactory.StylePanel(rightPanelImage, new Color(1f, 1f, 1f, 0.08f));

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
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(panel, "Title", "战前布阵", 24, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 30f);
        budgetText = HexTacticsUiFactory.CreateText(panel, "BudgetText", string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 0.92f));
        HexTacticsUiFactory.AddLayoutElement(budgetText.gameObject, preferredHeight: 22f);
        selectionHintText = HexTacticsUiFactory.CreateText(panel, "SelectionHintText", string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(selectionHintText.gameObject, preferredHeight: 40f);
    }

    private void BuildLeftPanel(RectTransform leftPanel)
    {
        var header = HexTacticsUiFactory.CreateRect("Header", leftPanel);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 88f);
        header.anchoredPosition = Vector2.zero;
        var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
        headerLayout.padding = new RectOffset(16, 16, 14, 8);
        headerLayout.spacing = 4f;
        headerLayout.childAlignment = TextAnchor.UpperLeft;
        headerLayout.childControlHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(header, "Title", "角色仓库", 22, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 28f);
        var subtitle = HexTacticsUiFactory.CreateText(header, "Subtitle", "拖到棋盘蓝色部署格即可上阵", 13, TextAnchor.MiddleLeft, new Color(0.72f, 0.84f, 0.86f));
        HexTacticsUiFactory.AddLayoutElement(subtitle.gameObject, preferredHeight: 18f);

        var leftScroll = HexTacticsUiFactory.CreateRect("RosterScroll", leftPanel);
        leftScroll.anchorMin = new Vector2(0f, 0f);
        leftScroll.anchorMax = new Vector2(1f, 1f);
        leftScroll.pivot = new Vector2(0.5f, 0.5f);
        HexTacticsUiFactory.SetOffsets(leftScroll, 16f, 16f, 16f, 96f);
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
        header.sizeDelta = new Vector2(0f, 108f);
        header.anchoredPosition = Vector2.zero;
        var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
        headerLayout.padding = new RectOffset(16, 16, 14, 8);
        headerLayout.spacing = 4f;
        headerLayout.childAlignment = TextAnchor.UpperLeft;
        headerLayout.childControlHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(header, "Title", "蓝方部署", 22, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 28f);
        costText = HexTacticsUiFactory.CreateText(header, "CostText", string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 0.92f));
        HexTacticsUiFactory.AddLayoutElement(costText.gameObject, preferredHeight: 22f);
        cpuHintText = HexTacticsUiFactory.CreateText(header, "CpuHintText", string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(cpuHintText.gameObject, preferredHeight: 34f);

        var selectedScroll = HexTacticsUiFactory.CreateRect("SelectedScroll", rightPanel);
        selectedScroll.anchorMin = new Vector2(0f, 0f);
        selectedScroll.anchorMax = new Vector2(1f, 1f);
        selectedScroll.pivot = new Vector2(0.5f, 0.5f);
        HexTacticsUiFactory.SetOffsets(selectedScroll, 16f, 112f, 16f, 116f);
        var selectedScrollRoot = HexTacticsUiFactory.CreateScrollView(selectedScroll, "ScrollRoot", out _, out selectedRosterContentRoot);
        HexTacticsUiFactory.Stretch(selectedScrollRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(selectedScrollRoot, 0f, 0f, 0f, 0f);

        emptyText = HexTacticsUiFactory.CreateText(rightPanel, "EmptyText", "还没有放置任何角色", 16, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.88f));
        emptyText.rectTransform.anchorMin = new Vector2(0f, 1f);
        emptyText.rectTransform.anchorMax = new Vector2(1f, 1f);
        emptyText.rectTransform.pivot = new Vector2(0.5f, 1f);
        emptyText.rectTransform.offsetMin = new Vector2(16f, -138f);
        emptyText.rectTransform.offsetMax = new Vector2(-16f, -110f);

        var footer = HexTacticsUiFactory.CreateRect("Footer", rightPanel);
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.sizeDelta = new Vector2(0f, 98f);
        footer.anchoredPosition = Vector2.zero;
        var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
        footerLayout.padding = new RectOffset(16, 16, 10, 12);
        footerLayout.spacing = 8f;
        footerLayout.childAlignment = TextAnchor.UpperLeft;
        footerLayout.childControlHeight = false;
        footerLayout.childControlWidth = true;
        footerLayout.childForceExpandHeight = false;
        footerLayout.childForceExpandWidth = true;

        statusText = HexTacticsUiFactory.CreateText(footer, "BuilderStatus", string.Empty, 15, TextAnchor.MiddleLeft, new Color(1.0f, 0.90f, 0.72f));
        HexTacticsUiFactory.AddLayoutElement(statusText.gameObject, preferredHeight: 28f);

        var footerButtons = HexTacticsUiFactory.CreateRect("Buttons", footer);
        HexTacticsUiFactory.AddLayoutElement(footerButtons.gameObject, preferredHeight: 42f);
        var footerButtonsLayout = footerButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerButtonsLayout.spacing = 12f;
        footerButtonsLayout.childAlignment = TextAnchor.MiddleCenter;
        footerButtonsLayout.childControlHeight = true;
        footerButtonsLayout.childControlWidth = true;
        footerButtonsLayout.childForceExpandHeight = false;
        footerButtonsLayout.childForceExpandWidth = true;

        backButton = HexTacticsUiFactory.CreateButton(footerButtons, "BackButton", "返回模式", new Color(0.25f, 0.31f, 0.36f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(backButton.gameObject, preferredHeight: 42f);

        startButton = HexTacticsUiFactory.CreateButton(footerButtons, "StartButton", "开始对战", new Color(0.22f, 0.56f, 0.55f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(startButton.gameObject, preferredHeight: 42f);
    }

    private void BuildDragPreview(RectTransform root)
    {
        dragPreviewRoot = HexTacticsUiFactory.CreateRect("DragPreview", root);
        dragPreviewRoot.anchorMin = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.anchorMax = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.pivot = new Vector2(0.5f, 0.5f);
        dragPreviewRoot.sizeDelta = new Vector2(188f, 72f);
        dragPreviewRoot.gameObject.SetActive(false);

        dragPreviewBackground = HexTacticsUiFactory.AddImage(dragPreviewRoot.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.92f), false);
        HexTacticsUiFactory.StylePanel(dragPreviewBackground, new Color(1f, 1f, 1f, 0.10f), 0.16f);

        var layout = dragPreviewRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", dragPreviewRoot);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 44f, preferredHeight: 44f);
        dragPreviewAvatarBackground = HexTacticsUiFactory.AddImage(avatarRoot.gameObject, new Color(0.25f, 0.34f, 0.40f, 1f), false);
        HexTacticsUiFactory.StylePanel(dragPreviewAvatarBackground, new Color(1f, 1f, 1f, 0.10f), 0.10f);
        var avatarIconRoot = HexTacticsUiFactory.CreateRect("Icon", avatarRoot);
        HexTacticsUiFactory.Stretch(avatarIconRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(avatarIconRoot, 3f, 3f, 3f, 3f);
        dragPreviewAvatar = HexTacticsUiFactory.AddImage(avatarIconRoot.gameObject, Color.white, false);
        HexTacticsUiFactory.Stretch(dragPreviewAvatar.rectTransform, Vector2.zero, Vector2.one);
        dragPreviewFallbackText = HexTacticsUiFactory.CreateText(avatarRoot, "Fallback", string.Empty, 16, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(dragPreviewFallbackText.rectTransform, Vector2.zero, Vector2.one);

        dragPreviewLabel = HexTacticsUiFactory.CreateText(dragPreviewRoot, "Label", string.Empty, 16, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(dragPreviewLabel.gameObject, flexibleWidth: 1f, preferredHeight: 26f);
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

        dragPreviewAvatarBackground.color = avatar.BackgroundColor;
        dragPreviewAvatar.sprite = avatar.Sprite;
        dragPreviewAvatar.color = Color.white;
        dragPreviewAvatar.preserveAspect = true;
        dragPreviewAvatar.enabled = avatar.Sprite != null;
        dragPreviewFallbackText.text = avatar.FallbackText;
        dragPreviewFallbackText.gameObject.SetActive(avatar.Sprite == null);
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
        var margin = Mathf.Clamp(width * 0.02f, 12f, 22f);

        if (isPortrait)
        {
            summaryPanel.anchorMin = new Vector2(0f, 1f);
            summaryPanel.anchorMax = new Vector2(1f, 1f);
            summaryPanel.pivot = new Vector2(0.5f, 1f);
            summaryPanel.sizeDelta = new Vector2(0f, 116f);
            summaryPanel.offsetMin = new Vector2(margin, -116f - margin);
            summaryPanel.offsetMax = new Vector2(-margin, -margin);

            var panelHeight = Mathf.Clamp(height * 0.34f, 280f, 420f);

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
        summaryPanel.sizeDelta = new Vector2(Mathf.Clamp(width * 0.32f, 420f, 620f), 112f);
        summaryPanel.anchoredPosition = new Vector2(0f, -margin);

        rosterPanel.anchorMin = new Vector2(0f, 0f);
        rosterPanel.anchorMax = new Vector2(0f, 1f);
        rosterPanel.pivot = new Vector2(0f, 0.5f);
        rosterPanel.sizeDelta = new Vector2(Mathf.Clamp(width * 0.24f, 320f, 400f), 0f);
        rosterPanel.anchoredPosition = new Vector2(margin, 0f);

        selectionPanel.anchorMin = new Vector2(1f, 0f);
        selectionPanel.anchorMax = new Vector2(1f, 1f);
        selectionPanel.pivot = new Vector2(1f, 0.5f);
        selectionPanel.sizeDelta = new Vector2(Mathf.Clamp(width * 0.28f, 360f, 440f), 0f);
        selectionPanel.anchoredPosition = new Vector2(-margin, 0f);
    }
}
