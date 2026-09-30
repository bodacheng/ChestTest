using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsSkillChoiceRowView : HexTacticsUiGeneratedView
{
    [SerializeField] private Image panelImage;
    [SerializeField] private Button button;
    [SerializeField] private Text titleText;
    [SerializeField] private Text detailText;

    protected override int CurrentLayoutVersion => 4;

    public int SkillIndex { get; private set; }

    protected override bool HasCurrentBindings =>
        panelImage != null &&
        button != null &&
        titleText != null &&
        detailText != null;

    public void Bind(HexTacticsSkillChoiceUiData data, Action<int> onSelect)
    {
        EnsureBuilt();
        SkillIndex = data.SkillIndex;
        titleText.text = data.IsSelected ? $"当前 · {data.Title}" : data.Title;
        detailText.text = data.Detail;

        var panelColor = ResolvePanelColor(data);
        HexTacticsModernUiSkin.ApplySkillSlot(panelImage, data.IsSelected || data.IsPlannedSkill || data.IsHovered, panelColor);
        panelImage.color = panelColor;
        titleText.color = data.IsAvailable ? Color.white : new Color(0.76f, 0.78f, 0.82f);
        detailText.color = data.IsHovered
            ? Color.white
            : data.IsAvailable
                ? new Color(0.82f, 0.88f, 0.92f)
                : new Color(0.60f, 0.64f, 0.68f);

        button.interactable = data.IsAvailable;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (!data.IsSelected)
            {
                onSelect?.Invoke(data.SkillIndex);
            }
        });
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = (RectTransform)transform;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(0f, HexTacticsSkillPopupView.RowHeight);

        panelImage = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.09f, 0.12f, 0.16f, 0.86f));
        HexTacticsModernUiSkin.ApplySkillSlot(panelImage, filled: false, new Color(1f, 1f, 1f, 0.90f));
        HexTacticsUiFactory.StylePanel(panelImage, new Color(1f, 1f, 1f, 0.04f), 0f);
        button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = panelImage;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.68f, 0.68f, 0.68f, 0.80f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: HexTacticsSkillPopupView.RowHeight, flexibleWidth: 1f);

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 3, 3);
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(root, "Title", string.Empty, 14, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 12;
        titleText.resizeTextMaxSize = 14;
        titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 24f);

        detailText = HexTacticsUiFactory.CreateText(root, "Detail", string.Empty, 11, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.92f));
        detailText.resizeTextForBestFit = true;
        detailText.resizeTextMinSize = 11;
        detailText.resizeTextMaxSize = 11;
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(detailText.gameObject, preferredHeight: 18f);
    }

    public static HexTacticsSkillChoiceRowView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsSkillChoiceRow", parent);
        var view = root.gameObject.AddComponent<HexTacticsSkillChoiceRowView>();
        view.EnsureBuilt();
        return view;
    }

    private static Color ResolvePanelColor(HexTacticsSkillChoiceUiData data)
    {
        if (data.IsHovered)
        {
            return new Color(0.38f, 0.48f, 0.16f, 0.98f);
        }

        if (data.IsSelected)
        {
            return new Color(0.22f, 0.30f, 0.14f, 0.96f);
        }

        if (data.IsPlannedSkill)
        {
            return new Color(0.20f, 0.16f, 0.08f, 0.92f);
        }

        return data.IsAvailable
            ? new Color(0.09f, 0.12f, 0.16f, 0.86f)
            : new Color(0.08f, 0.08f, 0.09f, 0.62f);
    }
}
