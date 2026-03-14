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

    protected override int CurrentLayoutVersion => 6;

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
        panelImage.color = data.HasAssignedCommand
            ? new Color(0.06f, 0.08f, 0.10f, 0.70f)
            : new Color(0.22f, 0.14f, 0.05f, 0.82f);
        commandText.text = $"{data.UnitName}\n{data.CommandText}";
        commandText.color = data.HasAssignedCommand ? Color.white : new Color(1.00f, 0.88f, 0.62f);
        selectButtonText.text = data.IsSelected ? "已选中" : "定位";
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
        root.sizeDelta = new Vector2(0f, 72f);

        panelImage = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.06f, 0.08f, 0.10f, 0.70f));
        HexTacticsUiFactory.StylePanel(panelImage, new Color(1f, 1f, 1f, 0.05f), 0.08f);
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 72f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", root);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 44f, preferredHeight: 44f);
        avatarBackground = HexTacticsUiFactory.AddImage(avatarRoot.gameObject, new Color(0.25f, 0.34f, 0.40f, 1f));
        HexTacticsUiFactory.StylePanel(avatarBackground, new Color(1f, 1f, 1f, 0.10f), 0.12f);
        var avatarIconRoot = HexTacticsUiFactory.CreateRect("Icon", avatarRoot);
        HexTacticsUiFactory.Stretch(avatarIconRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(avatarIconRoot, 3f, 3f, 3f, 3f);
        avatarImage = HexTacticsUiFactory.AddImage(avatarIconRoot.gameObject, Color.white, false);
        HexTacticsUiFactory.Stretch(avatarImage.rectTransform, Vector2.zero, Vector2.one);
        avatarFallbackText = HexTacticsUiFactory.CreateText(avatarRoot, "AvatarFallback", string.Empty, 16, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(avatarFallbackText.rectTransform, Vector2.zero, Vector2.one);

        commandText = HexTacticsUiFactory.CreateText(root, "CommandText", string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        commandText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(commandText.gameObject, flexibleWidth: 1f, preferredHeight: 42f);

        selectButton = HexTacticsUiFactory.CreateButton(root, "SelectButton", "定位", new Color(0.20f, 0.38f, 0.62f, 0.96f), Color.white, out selectButtonText);
        HexTacticsUiFactory.AddLayoutElement(selectButton.gameObject, preferredWidth: 86f, preferredHeight: 34f);

        waitButton = HexTacticsUiFactory.CreateButton(root, "WaitButton", "待机", new Color(0.31f, 0.52f, 0.26f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(waitButton.gameObject, preferredWidth: 76f, preferredHeight: 34f);
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
}
