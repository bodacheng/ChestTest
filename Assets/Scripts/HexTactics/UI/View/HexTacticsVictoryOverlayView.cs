using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsVictoryOverlayView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text summaryText;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button retryButton;

    protected override int CurrentLayoutVersion => 1;

    protected override bool HasCurrentBindings =>
        summaryText != null &&
        returnButton != null &&
        retryButton != null;

    public RectTransform Root => (RectTransform)transform;

    public void Bind(HexTacticsUiSnapshot snapshot, Action returnToTeamBuilder, Action retryBattle)
    {
        EnsureBuilt();
        summaryText.text = snapshot.VictorySummary;
        HexTacticsUiFactory.BindButton(returnButton, returnToTeamBuilder);
        HexTacticsUiFactory.BindButton(retryButton, retryBattle);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        HexTacticsUiFactory.Stretch(root, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(root, 0f, 0f, 0f, 0f);
        HexTacticsUiFactory.AddImage(root.gameObject, new Color(0f, 0f, 0f, 0.48f));

        var panel = HexTacticsUiFactory.CreatePanel(root, "VictoryPanel", new Color(0.05f, 0.08f, 0.10f, 0.86f), new Vector2(500f, 220f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.anchoredPosition = new Vector2(0f, 4f);

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 20);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(panel, "Title", "战斗结束", 28, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 34f);

        summaryText = HexTacticsUiFactory.CreateText(panel, "Summary", string.Empty, 20, TextAnchor.MiddleCenter, new Color(0.90f, 0.94f, 0.96f));
        HexTacticsUiFactory.AddLayoutElement(summaryText.gameObject, preferredHeight: 28f);

        var condition = HexTacticsUiFactory.CreateText(panel, "Condition", "胜利条件：一方所有棋子被击败", 16, TextAnchor.MiddleCenter, new Color(0.70f, 0.82f, 0.82f));
        HexTacticsUiFactory.AddLayoutElement(condition.gameObject, preferredHeight: 22f);

        var buttons = HexTacticsUiFactory.CreateRect("Buttons", panel);
        HexTacticsUiFactory.AddLayoutElement(buttons.gameObject, preferredHeight: 40f);
        var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 14f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = true;

        returnButton = HexTacticsUiFactory.CreateButton(buttons, "ReturnButton", "返回编队", new Color(0.25f, 0.31f, 0.36f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(returnButton.gameObject, preferredHeight: 40f);

        retryButton = HexTacticsUiFactory.CreateButton(buttons, "RetryButton", "再次挑战", new Color(0.22f, 0.56f, 0.55f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(retryButton.gameObject, preferredHeight: 40f);
    }

    public static HexTacticsVictoryOverlayView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsVictoryOverlay", parent);
        var view = root.gameObject.AddComponent<HexTacticsVictoryOverlayView>();
        view.EnsureBuilt();
        return view;
    }
}
