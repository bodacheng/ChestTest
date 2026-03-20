using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsSelectedRosterRowView : HexTacticsUiGeneratedView, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image avatarBackground;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Text avatarFallbackText;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statsText;
    [SerializeField] private Text deploymentText;
    [SerializeField] private Button removeButton;

    private HexTacticsSelectionEntryUiData currentData;
    private Action<HexTacticsSelectionEntryUiData, Vector2> beginDragHandler;
    private Action<Vector2> dragHandler;
    private Action<Vector2> endDragHandler;

    protected override int CurrentLayoutVersion => 10;

    protected override bool HasCurrentBindings =>
        avatarBackground != null &&
        avatarImage != null &&
        avatarFallbackText != null &&
        titleText != null &&
        statsText != null &&
        deploymentText != null &&
        removeButton != null;

    public void Bind(
        HexTacticsSelectionEntryUiData data,
        Action<int> onRemove,
        Action<HexTacticsSelectionEntryUiData, Vector2> onBeginDrag,
        Action<Vector2> onDrag,
        Action<Vector2> onEndDrag)
    {
        EnsureBuilt();
        currentData = data;
        beginDragHandler = onBeginDrag;
        dragHandler = onDrag;
        endDragHandler = onEndDrag;

        ApplyAvatar(data.Avatar);
        titleText.text = $"{data.DisplayIndex}. {data.DisplayName}";
        statsText.text = data.DeploymentText;
        deploymentText.text = $"HP {data.MaxHealth}  /  攻 {data.AttackPower}  /  移 {data.MoveRange}  /  费 {data.Cost}";
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => onRemove?.Invoke(data.EntryId));
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        beginDragHandler?.Invoke(currentData, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragHandler?.Invoke(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        endDragHandler?.Invoke(eventData.position);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = (RectTransform)transform;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(0f, 124f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.06f, 0.09f, 0.10f, 0.72f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.04f), 0f);
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 124f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", root);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 56f, preferredHeight: 56f);
        avatarBackground = HexTacticsUiFactory.AddImage(avatarRoot.gameObject, new Color(0.25f, 0.34f, 0.40f, 1f));
        HexTacticsUiFactory.StylePanel(avatarBackground, new Color(1f, 1f, 1f, 0.04f), 0f);
        var avatarIconRoot = HexTacticsUiFactory.CreateRect("Icon", avatarRoot);
        HexTacticsUiFactory.Stretch(avatarIconRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(avatarIconRoot, 4f, 4f, 4f, 4f);
        avatarImage = HexTacticsUiFactory.AddImage(avatarIconRoot.gameObject, Color.white, false);
        HexTacticsUiFactory.Stretch(avatarImage.rectTransform, Vector2.zero, Vector2.one);
        avatarFallbackText = HexTacticsUiFactory.CreateText(avatarRoot, "AvatarFallback", string.Empty, 22, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(avatarFallbackText.rectTransform, Vector2.zero, Vector2.one);

        var content = HexTacticsUiFactory.CreateRect("Content", root);
        HexTacticsUiFactory.AddLayoutElement(content.gameObject, flexibleWidth: 1f);
        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 5f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(content, "Title", string.Empty, 18, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 15;
        titleText.resizeTextMaxSize = 18;
        titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 26f);

        statsText = HexTacticsUiFactory.CreateText(content, "Stats", string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.73f, 0.80f, 0.84f));
        statsText.resizeTextForBestFit = true;
        statsText.resizeTextMinSize = 12;
        statsText.resizeTextMaxSize = 14;
        statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statsText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(statsText.gameObject, preferredHeight: 22f);

        deploymentText = HexTacticsUiFactory.CreateText(content, "Deployment", string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.60f, 0.70f, 0.74f));
        deploymentText.resizeTextForBestFit = true;
        deploymentText.resizeTextMinSize = 12;
        deploymentText.resizeTextMaxSize = 13;
        deploymentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        deploymentText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(deploymentText.gameObject, preferredHeight: 20f);

        var actionArea = HexTacticsUiFactory.CreateRect("ActionArea", root);
        HexTacticsUiFactory.AddLayoutElement(actionArea.gameObject, preferredWidth: 78f, preferredHeight: 44f);
        removeButton = HexTacticsUiFactory.CreateButton(actionArea, "RemoveButton", "移出", new Color(0.52f, 0.29f, 0.27f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.Stretch(removeButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
    }

    public static HexTacticsSelectedRosterRowView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsSelectedRosterRow", parent);
        var view = root.gameObject.AddComponent<HexTacticsSelectedRosterRowView>();
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
