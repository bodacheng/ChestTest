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
    [SerializeField] private Text selectedUnitText;
    [SerializeField] private RectTransform commandContentRoot;
    [SerializeField] private Text commandEmptyText;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button waitButton;
    [SerializeField] private HexTacticsCommandRowView commandRowPrefab;

    private readonly List<HexTacticsCommandRowView> commandRows = new();

    protected override int CurrentLayoutVersion => 5;

    protected override bool HasCurrentBindings =>
        roundText != null &&
        countText != null &&
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
        Action<int> waitCommandUnit)
    {
        EnsureBuilt();

        roundText.text = $"第 {snapshot.PlanningRoundNumber} 轮计划  |  已同步结算 {snapshot.ResolvedTurnCount} 个回合";
        countText.text = $"蓝方剩余 {snapshot.BlueAliveCount} 名  |  红方剩余 {snapshot.RedAliveCount} 名";
        selectedUnitText.text = snapshot.SelectedUnitSummary;
        waitButton.interactable = snapshot.HasSelectedUnit;
        commandEmptyText.gameObject.SetActive(snapshot.PlayerCommandEntries.Count == 0);
        commandEmptyText.text = "当前没有可下令的蓝方棋子";

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
                commandRows[i].Bind(snapshot.PlayerCommandEntries[i], selectCommandUnit, waitCommandUnit);
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
        root.sizeDelta = new Vector2(448f, 596f);
        root.anchoredPosition = new Vector2(22f, -22f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.86f));
        HexTacticsUiFactory.StylePanel(panel, new Color(0.75f, 0.90f, 0.88f, 0.08f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var badge = HexTacticsUiFactory.CreateText(root, "Badge", "BATTLE PLAN", 13, TextAnchor.MiddleLeft, new Color(0.63f, 0.82f, 0.80f), FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(badge.gameObject, preferredHeight: 18f);

        roundText = HexTacticsUiFactory.CreateText(root, "RoundText", string.Empty, 20, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(roundText.gameObject, preferredHeight: 28f);

        countText = HexTacticsUiFactory.CreateText(root, "CountText", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 0.92f));
        HexTacticsUiFactory.AddLayoutElement(countText.gameObject, preferredHeight: 20f);

        var instruction = HexTacticsUiFactory.CreateText(root, "Instruction", "点格子设置移动，点敌人设置追击；Delete / Backspace 可把当前棋子改为待机。", 14, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(instruction.gameObject, preferredHeight: 30f);

        var instruction2 = HexTacticsUiFactory.CreateText(root, "Instruction2", "所有蓝方角色都设置完成后会自动开始结算，不再需要手动确认。", 14, TextAnchor.MiddleLeft, new Color(0.82f, 0.91f, 0.73f));
        HexTacticsUiFactory.AddLayoutElement(instruction2.gameObject, preferredHeight: 30f);

        selectedUnitText = HexTacticsUiFactory.CreateText(root, "SelectedUnitText", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.95f, 0.95f, 0.98f));
        HexTacticsUiFactory.AddLayoutElement(selectedUnitText.gameObject, preferredHeight: 36f);

        var commandScrollContainer = HexTacticsUiFactory.CreateRect("CommandScrollContainer", root);
        HexTacticsUiFactory.AddLayoutElement(commandScrollContainer.gameObject, preferredHeight: 272f);
        var commandScrollRoot = HexTacticsUiFactory.CreateScrollView(commandScrollContainer, "ScrollRoot", out _, out commandContentRoot);
        HexTacticsUiFactory.Stretch(commandScrollRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(commandScrollRoot, 0f, 0f, 0f, 0f);

        commandEmptyText = HexTacticsUiFactory.CreateText(root, "EmptyCommandText", "当前没有可下令的蓝方棋子", 15, TextAnchor.MiddleLeft, new Color(0.80f, 0.86f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(commandEmptyText.gameObject, preferredHeight: 18f);

        var buttonRow = HexTacticsUiFactory.CreateRect("Buttons", root);
        HexTacticsUiFactory.AddLayoutElement(buttonRow.gameObject, preferredHeight: 40f);
        var buttonRowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonRowLayout.spacing = 12f;
        buttonRowLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonRowLayout.childControlHeight = true;
        buttonRowLayout.childControlWidth = true;
        buttonRowLayout.childForceExpandHeight = false;
        buttonRowLayout.childForceExpandWidth = true;

        clearButton = HexTacticsUiFactory.CreateButton(buttonRow, "ClearButton", "清空选择", new Color(0.25f, 0.31f, 0.36f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(clearButton.gameObject, preferredHeight: 40f);

        waitButton = HexTacticsUiFactory.CreateButton(buttonRow, "WaitButton", "当前待机", new Color(0.31f, 0.52f, 0.26f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(waitButton.gameObject, preferredHeight: 40f);
    }

    public static HexTacticsPlanningScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsPlanningScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsPlanningScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
