using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsModeSelectScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Button hexModeButton;
    [SerializeField] private Button squareModeButton;

    protected override int CurrentLayoutVersion => 5;

    protected override bool HasCurrentBindings => hexModeButton != null && squareModeButton != null;

    public RectTransform Root => (RectTransform)transform;

    public void Bind(Action startHexMode, Action startSquareMode)
    {
        EnsureBuilt();
        HexTacticsUiFactory.BindButton(hexModeButton, startHexMode);
        HexTacticsUiFactory.BindButton(squareModeButton, startSquareMode);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(520f, 286f);
        root.anchoredPosition = new Vector2(0f, 40f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.82f));
        HexTacticsModernUiSkin.ApplyPopupPanel(panel, new Color(1f, 1f, 1f, 0.96f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.05f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 26, 24);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var chip = HexTacticsUiFactory.CreateRect("ModeChip", root);
        HexTacticsUiFactory.AddLayoutElement(chip.gameObject, preferredHeight: 28f, preferredWidth: 152f);
        var chipImage = HexTacticsUiFactory.AddImage(chip.gameObject, new Color(0.18f, 0.30f, 0.34f, 0.95f), false);
        HexTacticsModernUiSkin.ApplyHeaderChip(chipImage, new Color(0.18f, 0.30f, 0.34f, 0.95f));
        var chipText = HexTacticsUiFactory.CreateText(chip, "ChipText", "同步结算对战", 13, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(chipText.rectTransform, Vector2.zero, Vector2.one);

        var title = HexTacticsUiFactory.CreateText(root, "Title", "选择棋盘模式", 28, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 34f);

        var description = HexTacticsUiFactory.CreateText(root, "Description", "六边格保留原规则，方形格改为每步四方向移动。", 16, TextAnchor.MiddleCenter, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(description.gameObject, preferredHeight: 22f);

        var hint = HexTacticsUiFactory.CreateText(root, "Hint", "当前开放单人对战，可随时返回这里切换棋盘。", 13, TextAnchor.MiddleCenter, new Color(0.62f, 0.72f, 0.76f));
        HexTacticsUiFactory.AddLayoutElement(hint.gameObject, preferredHeight: 18f);

        hexModeButton = HexTacticsUiFactory.CreateButton(root, "HexModeButton", "六边形格模式", new Color(0.19f, 0.46f, 0.46f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(hexModeButton.gameObject, preferredHeight: 44f, preferredWidth: 236f);

        squareModeButton = HexTacticsUiFactory.CreateButton(root, "SquareModeButton", "方形格模式", new Color(0.31f, 0.40f, 0.24f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(squareModeButton.gameObject, preferredHeight: 44f, preferredWidth: 236f);
    }

    public static HexTacticsModeSelectScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsModeSelectScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsModeSelectScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
