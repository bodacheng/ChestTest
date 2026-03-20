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

    protected override int CurrentLayoutVersion => 3;

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
        HexTacticsUiFactory.AddImage(root.gameObject, new Color(0f, 0f, 0f, 0.34f));

        var panel = HexTacticsUiFactory.CreatePanel(root, "VictoryPanel", new Color(0.04f, 0.07f, 0.08f, 0.84f), new Vector2(436f, 188f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.anchoredPosition = Vector2.zero;

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 22, 20);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(panel, "Title", "战斗结束", 26, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 32f);

        summaryText = HexTacticsUiFactory.CreateText(panel, "Summary", string.Empty, 18, TextAnchor.MiddleCenter, new Color(0.90f, 0.94f, 0.96f));
        HexTacticsUiFactory.AddLayoutElement(summaryText.gameObject, preferredHeight: 26f);

        var buttons = HexTacticsUiFactory.CreateRect("Buttons", panel);
        HexTacticsUiFactory.AddLayoutElement(buttons.gameObject, preferredHeight: 38f);
        var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 10f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = true;

        returnButton = HexTacticsUiFactory.CreateButton(buttons, "ReturnButton", "回到编队", new Color(0.23f, 0.28f, 0.32f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(returnButton.gameObject, preferredHeight: 38f);

        retryButton = HexTacticsUiFactory.CreateButton(buttons, "RetryButton", "再来一局", new Color(0.19f, 0.46f, 0.46f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(retryButton.gameObject, preferredHeight: 38f);
    }

    public static HexTacticsVictoryOverlayView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsVictoryOverlay", parent);
        var view = root.gameObject.AddComponent<HexTacticsVictoryOverlayView>();
        view.EnsureBuilt();
        return view;
    }
}
