using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsPlanningScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text roundText;
    [SerializeField] private Text countText;
    [SerializeField] private Image currentCommandPanelImage;
    [SerializeField] private Text currentCommandText;
    [SerializeField] private Text commandProgressText;
    [SerializeField] private Text selectedUnitText;
    [SerializeField] private RectTransform commandContentRoot;
    [SerializeField] private Text commandEmptyText;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button waitButton;
    [SerializeField] private HexTacticsCommandRowView commandRowPrefab;

    private readonly List<HexTacticsCommandRowView> commandRows = new();

    protected override int CurrentLayoutVersion => 8;

    protected override bool HasCurrentBindings =>
        roundText != null &&
        countText != null &&
        currentCommandPanelImage != null &&
        currentCommandText != null &&
        commandProgressText != null &&
        selectedUnitText != null &&
        commandContentRoot != null &&
        commandEmptyText != null &&
        clearButton != null &&
        waitButton != null;

    public RectTransform Root => (RectTransform)transform;

    public void ConfigureItemPrefab(HexTacticsCommandRowView prefab)
    {
        commandRowPrefab = prefab;
    }

    public void Bind(
        HexTacticsUiSnapshot snapshot,
        Action clearSelection,
        Action waitSelectedUnit,
        Action<int> selectCommandUnit,
        Action<int> waitCommandUnit,
        Action<int> cycleCommandUnitSkill)
    {
        EnsureBuilt();

        roundText.text = $"第 {snapshot.PlanningRoundNumber} 轮  |  已结算 {snapshot.ResolvedTurnCount}";
        countText.text = $"蓝 {snapshot.BlueAliveCount}  |  红 {snapshot.RedAliveCount}";
        currentCommandText.text = snapshot.CurrentCommandSummary;
        commandProgressText.text = snapshot.CommandProgressSummary;
        currentCommandPanelImage.color = snapshot.HasSelectedUnit && snapshot.BluePendingCommandCount > 0
            ? new Color(0.27f, 0.21f, 0.10f, 0.92f)
            : new Color(0.10f, 0.18f, 0.24f, 0.88f);
        selectedUnitText.text = snapshot.SelectedUnitSummary;
        waitButton.interactable = snapshot.HasSelectedUnit;
        commandEmptyText.gameObject.SetActive(snapshot.PlayerCommandEntries.Count == 0);
        commandEmptyText.text = "暂无可下令单位";

        HexTacticsUiFactory.BindButton(clearButton, clearSelection);
        HexTacticsUiFactory.BindButton(waitButton, waitSelectedUnit);

        HexTacticsUiFactory.EnsurePool(
            commandRows,
            snapshot.PlayerCommandEntries.Count,
            commandRowPrefab,
            HexTacticsUiResourcePaths.CommandRow,
            commandContentRoot,
            HexTacticsCommandRowView.CreateStandalone);

        for (var i = 0; i < commandRows.Count; i++)
        {
            var active = i < snapshot.PlayerCommandEntries.Count;
            commandRows[i].gameObject.SetActive(active);
            if (active)
            {
                commandRows[i].Bind(snapshot.PlayerCommandEntries[i], selectCommandUnit, waitCommandUnit, cycleCommandUnitSkill);
            }
        }

        HexTacticsUiFactory.ForceRebuildLayout(commandContentRoot);
        HexTacticsUiFactory.ForceRebuildLayout(Root);
    }

    public override void BuildDefaultHierarchy()
    {
        commandRows.Clear();
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(408f, 478f);
        root.anchoredPosition = new Vector2(18f, -18f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.80f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.05f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        roundText = HexTacticsUiFactory.CreateText(root, "RoundText", string.Empty, 18, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(roundText.gameObject, preferredHeight: 24f);

        countText = HexTacticsUiFactory.CreateText(root, "CountText", string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(countText.gameObject, preferredHeight: 20f);

        var instruction = HexTacticsUiFactory.CreateText(root, "Instruction", "点地格移动，点敌人设为追击；可用“换技”切换当前技能，移动指令接敌时会优先使用零消耗技。", 13, TextAnchor.MiddleLeft, new Color(0.62f, 0.72f, 0.76f));
        HexTacticsUiFactory.AddLayoutElement(instruction.gameObject, preferredHeight: 34f);

        var currentCommandPanel = HexTacticsUiFactory.CreateRect("CurrentCommandPanel", root);
        HexTacticsUiFactory.AddLayoutElement(currentCommandPanel.gameObject, preferredHeight: 62f);
        currentCommandPanelImage = HexTacticsUiFactory.AddImage(currentCommandPanel.gameObject, new Color(0.10f, 0.18f, 0.24f, 0.88f));
        HexTacticsUiFactory.StylePanel(currentCommandPanelImage, new Color(1f, 1f, 1f, 0.06f));
        var currentCommandLayout = currentCommandPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        currentCommandLayout.padding = new RectOffset(12, 12, 9, 9);
        currentCommandLayout.spacing = 2f;
        currentCommandLayout.childAlignment = TextAnchor.MiddleLeft;
        currentCommandLayout.childControlHeight = false;
        currentCommandLayout.childControlWidth = true;
        currentCommandLayout.childForceExpandHeight = false;
        currentCommandLayout.childForceExpandWidth = true;

        currentCommandText = HexTacticsUiFactory.CreateText(currentCommandPanel, "CurrentCommandText", string.Empty, 17, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(currentCommandText.gameObject, preferredHeight: 22f);

        commandProgressText = HexTacticsUiFactory.CreateText(currentCommandPanel, "CommandProgressText", string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(commandProgressText.gameObject, preferredHeight: 18f);

        selectedUnitText = HexTacticsUiFactory.CreateText(root, "SelectedUnitText", string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.92f, 0.94f, 0.96f));
        selectedUnitText.verticalOverflow = VerticalWrapMode.Overflow;
        HexTacticsUiFactory.AddLayoutElement(selectedUnitText.gameObject, preferredHeight: 78f);

        var commandScrollContainer = HexTacticsUiFactory.CreateRect("CommandScrollContainer", root);
        HexTacticsUiFactory.AddLayoutElement(commandScrollContainer.gameObject, preferredHeight: 222f);
        var commandScrollRoot = HexTacticsUiFactory.CreateScrollView(commandScrollContainer, "ScrollRoot", out _, out commandContentRoot);
        HexTacticsUiFactory.Stretch(commandScrollRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(commandScrollRoot, 0f, 0f, 0f, 0f);

        commandEmptyText = HexTacticsUiFactory.CreateText(root, "EmptyCommandText", "暂无可下令单位", 14, TextAnchor.MiddleLeft, new Color(0.74f, 0.80f, 0.84f));
        HexTacticsUiFactory.AddLayoutElement(commandEmptyText.gameObject, preferredHeight: 18f);

        var buttonRow = HexTacticsUiFactory.CreateRect("Buttons", root);
        HexTacticsUiFactory.AddLayoutElement(buttonRow.gameObject, preferredHeight: 36f);
        var buttonRowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonRowLayout.spacing = 10f;
        buttonRowLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonRowLayout.childControlHeight = true;
        buttonRowLayout.childControlWidth = true;
        buttonRowLayout.childForceExpandHeight = false;
        buttonRowLayout.childForceExpandWidth = true;

        clearButton = HexTacticsUiFactory.CreateButton(buttonRow, "ClearButton", "清除", new Color(0.23f, 0.28f, 0.32f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(clearButton.gameObject, preferredHeight: 36f);

        waitButton = HexTacticsUiFactory.CreateButton(buttonRow, "WaitButton", "待机", new Color(0.30f, 0.44f, 0.28f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(waitButton.gameObject, preferredHeight: 36f);
    }

    public static HexTacticsPlanningScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsPlanningScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsPlanningScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
