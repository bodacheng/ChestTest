using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsCommandRowView : HexTacticsUiGeneratedView
{
    [SerializeField] private Image panelImage;
    [SerializeField] private Image avatarBackground;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Text avatarFallbackText;
    [SerializeField] private Text commandText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Text selectButtonText;
    [SerializeField] private Button waitButton;

    protected override int CurrentLayoutVersion => 8;

    protected override bool HasCurrentBindings =>
        panelImage != null &&
        avatarBackground != null &&
        avatarImage != null &&
        avatarFallbackText != null &&
        commandText != null &&
        selectButton != null &&
        selectButtonText != null &&
        waitButton != null;

    public void Bind(HexTacticsCommandEntryUiData data, Action<int> onSelect, Action<int> onWait)
    {
        EnsureBuilt();
        ApplyAvatar(data.Avatar);
        var stateLabel = data.IsSelected
            ? data.HasAssignedCommand ? "当前查看" : "当前下令"
            : data.HasAssignedCommand ? "已下令" : "待下令";
        panelImage.color = ResolvePanelColor(data);
        commandText.text = $"{stateLabel}  |  {data.UnitName}\n{data.CommandText}";
        commandText.color = data.IsSelected
            ? Color.white
            : data.HasAssignedCommand
                ? new Color(0.92f, 0.95f, 0.97f)
                : new Color(0.98f, 0.88f, 0.72f);
        selectButtonText.text = data.IsSelected ? "当前" : "定位";
        selectButton.interactable = !data.IsSelected;
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelect?.Invoke(data.UnitId));
        waitButton.onClick.RemoveAllListeners();
        waitButton.onClick.AddListener(() => onWait?.Invoke(data.UnitId));
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = (RectTransform)transform;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(0f, 60f);

        panelImage = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.06f, 0.08f, 0.10f, 0.74f));
        HexTacticsUiFactory.StylePanel(panelImage, new Color(1f, 1f, 1f, 0.04f), 0f);
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 60f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", root);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 38f, preferredHeight: 38f);
        avatarBackground = HexTacticsUiFactory.AddImage(avatarRoot.gameObject, new Color(0.25f, 0.34f, 0.40f, 1f));
        HexTacticsUiFactory.StylePanel(avatarBackground, new Color(1f, 1f, 1f, 0.04f), 0f);
        var avatarIconRoot = HexTacticsUiFactory.CreateRect("Icon", avatarRoot);
        HexTacticsUiFactory.Stretch(avatarIconRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(avatarIconRoot, 3f, 3f, 3f, 3f);
        avatarImage = HexTacticsUiFactory.AddImage(avatarIconRoot.gameObject, Color.white, false);
        HexTacticsUiFactory.Stretch(avatarImage.rectTransform, Vector2.zero, Vector2.one);
        avatarFallbackText = HexTacticsUiFactory.CreateText(avatarRoot, "AvatarFallback", string.Empty, 16, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(avatarFallbackText.rectTransform, Vector2.zero, Vector2.one);

        commandText = HexTacticsUiFactory.CreateText(root, "CommandText", string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        commandText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(commandText.gameObject, flexibleWidth: 1f, preferredHeight: 36f);

        selectButton = HexTacticsUiFactory.CreateButton(root, "SelectButton", "定位", new Color(0.24f, 0.36f, 0.50f, 0.94f), Color.white, out selectButtonText);
        HexTacticsUiFactory.AddLayoutElement(selectButton.gameObject, preferredWidth: 72f, preferredHeight: 32f);

        waitButton = HexTacticsUiFactory.CreateButton(root, "WaitButton", "待机", new Color(0.30f, 0.44f, 0.28f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(waitButton.gameObject, preferredWidth: 64f, preferredHeight: 32f);
    }

    public static HexTacticsCommandRowView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsCommandRow", parent);
        var view = root.gameObject.AddComponent<HexTacticsCommandRowView>();
        view.EnsureBuilt();
        return view;
    }

    private void ApplyAvatar(HexTacticsAvatarUiData avatar)
    {
        avatarBackground.color = avatar.BackgroundColor;
        avatarImage.sprite = avatar.Sprite;
        avatarImage.color = Color.white;
        avatarImage.preserveAspect = true;
        avatarImage.enabled = avatar.Sprite != null;
        avatarFallbackText.text = avatar.FallbackText;
        avatarFallbackText.gameObject.SetActive(avatar.Sprite == null);
    }

    private static Color ResolvePanelColor(HexTacticsCommandEntryUiData data)
    {
        if (data.IsSelected && !data.HasAssignedCommand)
        {
            return new Color(0.30f, 0.22f, 0.08f, 0.92f);
        }

        if (data.IsSelected)
        {
            return new Color(0.10f, 0.20f, 0.30f, 0.88f);
        }

        return data.HasAssignedCommand
            ? new Color(0.06f, 0.09f, 0.10f, 0.74f)
            : new Color(0.18f, 0.13f, 0.08f, 0.78f);
    }
}
