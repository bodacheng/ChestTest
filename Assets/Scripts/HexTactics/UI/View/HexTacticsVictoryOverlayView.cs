using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsVictoryOverlayView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text summaryText;
    [SerializeField] private Text statsText;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button retryButton;

    protected override int CurrentLayoutVersion => 6;

    protected override bool HasCurrentBindings =>
        summaryText != null &&
        statsText != null &&
        returnButton != null &&
        retryButton != null;

    public RectTransform Root => (RectTransform)transform;

    public void Bind(HexTacticsUiSnapshot snapshot, Action returnToTeamBuilder, Action retryBattle)
    {
        EnsureBuilt();
        summaryText.text = snapshot.VictorySummary;
        statsText.text = snapshot.VictoryStats;
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

        var panel = HexTacticsUiFactory.CreatePanel(root, "VictoryPanel", new Color(0.04f, 0.07f, 0.08f, 0.84f), new Vector2(468f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.anchoredPosition = Vector2.zero;
        if (panel.TryGetComponent<Image>(out var panelImage))
        {
            HexTacticsModernUiSkin.ApplyPopupPanel(panelImage, new Color(1f, 1f, 1f, 0.96f));
        }

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 22, 22);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var panelFitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var chip = HexTacticsUiFactory.CreateRect("VictoryChip", panel);
        HexTacticsUiFactory.AddLayoutElement(chip.gameObject, preferredHeight: 30f, preferredWidth: 140f);
        var chipImage = HexTacticsUiFactory.AddImage(chip.gameObject, new Color(0.28f, 0.22f, 0.10f, 0.95f), false);
        HexTacticsModernUiSkin.ApplyHeaderChip(chipImage, new Color(0.28f, 0.22f, 0.10f, 0.95f));
        var chipText = HexTacticsUiFactory.CreateText(chip, "ChipText", "战斗结算", 13, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(chipText.rectTransform, Vector2.zero, Vector2.one);

        summaryText = HexTacticsUiFactory.CreateText(panel, "Summary", string.Empty, 19, TextAnchor.MiddleCenter, new Color(0.90f, 0.94f, 0.96f), FontStyle.Bold);
        summaryText.resizeTextForBestFit = true;
        summaryText.resizeTextMinSize = 13;
        summaryText.resizeTextMaxSize = 19;
        HexTacticsUiFactory.AddLayoutElement(summaryText.gameObject, preferredHeight: 36f);

        statsText = HexTacticsUiFactory.CreateText(panel, "Stats", string.Empty, 14, TextAnchor.UpperLeft, new Color(0.82f, 0.88f, 0.90f));
        statsText.resizeTextForBestFit = true;
        statsText.resizeTextMinSize = 11;
        statsText.resizeTextMaxSize = 14;
        statsText.lineSpacing = 1.08f;
        HexTacticsUiFactory.AddLayoutElement(statsText.gameObject, preferredHeight: 104f);

        var buttons = HexTacticsUiFactory.CreateRect("Buttons", panel);
        HexTacticsUiFactory.AddLayoutElement(buttons.gameObject, preferredHeight: 44f);
        var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 12f;
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = true;

        returnButton = HexTacticsUiFactory.CreateButton(buttons, "ReturnButton", "回到编队", new Color(0.23f, 0.28f, 0.32f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(returnButton.gameObject, preferredHeight: 44f);

        retryButton = HexTacticsUiFactory.CreateButton(buttons, "RetryButton", "再来一局", new Color(0.19f, 0.46f, 0.46f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(retryButton.gameObject, preferredHeight: 44f);
    }

    public static HexTacticsVictoryOverlayView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsVictoryOverlay", parent);
        var view = root.gameObject.AddComponent<HexTacticsVictoryOverlayView>();
        view.EnsureBuilt();
        return view;
    }
}
