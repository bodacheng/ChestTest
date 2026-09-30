using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsModeSelectScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Button hexModeButton;
    [SerializeField] private Button squareModeButton;

    protected override int CurrentLayoutVersion => 6;

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
        root.sizeDelta = new Vector2(520f, 360f);
        root.anchoredPosition = new Vector2(0f, 40f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.82f));
        HexTacticsModernUiSkin.ApplyPopupPanel(panel, new Color(1f, 1f, 1f, 0.96f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.05f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 22, 22);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
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
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 46f);

        var description = HexTacticsUiFactory.CreateText(root, "Description", "六边形采用六方向移动，方形采用四方向移动。", 16, TextAnchor.MiddleCenter, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(description.gameObject, preferredHeight: 52f);

        var hint = HexTacticsUiFactory.CreateText(root, "Hint", "编队部署 → 下达指令 → 同步结算\n六边形棋盘仅在操作时显示位置提示。", 13, TextAnchor.MiddleCenter, new Color(0.72f, 0.82f, 0.86f));
        HexTacticsUiFactory.AddLayoutElement(hint.gameObject, preferredHeight: 44f);

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
