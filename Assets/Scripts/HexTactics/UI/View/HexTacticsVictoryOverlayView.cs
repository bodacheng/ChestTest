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

    protected override int CurrentLayoutVersion => 4;

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

        var panel = HexTacticsUiFactory.CreatePanel(root, "VictoryPanel", new Color(0.04f, 0.07f, 0.08f, 0.84f), new Vector2(424f, 182f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.anchoredPosition = Vector2.zero;
        if (panel.TryGetComponent<Image>(out var panelImage))
        {
            HexTacticsModernUiSkin.ApplyPopupPanel(panelImage, new Color(1f, 1f, 1f, 0.96f));
        }

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 18);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var chip = HexTacticsUiFactory.CreateRect("VictoryChip", panel);
        HexTacticsUiFactory.AddLayoutElement(chip.gameObject, preferredHeight: 28f, preferredWidth: 128f);
        var chipImage = HexTacticsUiFactory.AddImage(chip.gameObject, new Color(0.28f, 0.22f, 0.10f, 0.95f), false);
        HexTacticsModernUiSkin.ApplyHeaderChip(chipImage, new Color(0.28f, 0.22f, 0.10f, 0.95f));
        var chipText = HexTacticsUiFactory.CreateText(chip, "ChipText", "战斗结束", 13, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(chipText.rectTransform, Vector2.zero, Vector2.one);

        summaryText = HexTacticsUiFactory.CreateText(panel, "Summary", string.Empty, 18, TextAnchor.MiddleCenter, new Color(0.90f, 0.94f, 0.96f));
        summaryText.resizeTextForBestFit = true;
        summaryText.resizeTextMinSize = 12;
        summaryText.resizeTextMaxSize = 18;
        HexTacticsUiFactory.AddLayoutElement(summaryText.gameObject, preferredHeight: 28f);

        var buttons = HexTacticsUiFactory.CreateRect("Buttons", panel);
        HexTacticsUiFactory.AddLayoutElement(buttons.gameObject, preferredHeight: 40f);
        var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 10f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = true;

        returnButton = HexTacticsUiFactory.CreateButton(buttons, "ReturnButton", "回到编队", new Color(0.23f, 0.28f, 0.32f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(returnButton.gameObject, preferredHeight: 40f);

        retryButton = HexTacticsUiFactory.CreateButton(buttons, "RetryButton", "再来一局", new Color(0.19f, 0.46f, 0.46f, 0.94f), Color.white, out _);
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
