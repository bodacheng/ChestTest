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

    protected override int CurrentLayoutVersion => 7;

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
        deploymentText.text = $"H{data.MaxHealth}  A{data.AttackPower}  M{data.MoveRange}  C{data.Cost}";
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
        root.sizeDelta = new Vector2(0f, 100f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.08f, 0.09f, 0.11f, 0.78f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.06f), 0.10f);
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 100f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 10f;
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
        HexTacticsUiFactory.SetOffsets(avatarIconRoot, 4f, 4f, 4f, 4f);
        avatarImage = HexTacticsUiFactory.AddImage(avatarIconRoot.gameObject, Color.white, false);
        HexTacticsUiFactory.Stretch(avatarImage.rectTransform, Vector2.zero, Vector2.one);
        avatarFallbackText = HexTacticsUiFactory.CreateText(avatarRoot, "AvatarFallback", string.Empty, 19, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(avatarFallbackText.rectTransform, Vector2.zero, Vector2.one);

        var content = HexTacticsUiFactory.CreateRect("Content", root);
        HexTacticsUiFactory.AddLayoutElement(content.gameObject, flexibleWidth: 1f);
        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 2f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(content, "Title", string.Empty, 15, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 11;
        titleText.resizeTextMaxSize = 15;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 18f);

        statsText = HexTacticsUiFactory.CreateText(content, "Stats", string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.73f, 0.84f, 0.87f));
        statsText.resizeTextForBestFit = true;
        statsText.resizeTextMinSize = 10;
        statsText.resizeTextMaxSize = 13;
        statsText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(statsText.gameObject, preferredHeight: 18f);

        deploymentText = HexTacticsUiFactory.CreateText(content, "Deployment", string.Empty, 12, TextAnchor.MiddleLeft, new Color(0.63f, 0.76f, 0.77f));
        deploymentText.resizeTextForBestFit = true;
        deploymentText.resizeTextMinSize = 9;
        deploymentText.resizeTextMaxSize = 12;
        deploymentText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(deploymentText.gameObject, preferredHeight: 16f);

        var actionArea = HexTacticsUiFactory.CreateRect("ActionArea", root);
        HexTacticsUiFactory.AddLayoutElement(actionArea.gameObject, preferredWidth: 64f, preferredHeight: 38f);
        removeButton = HexTacticsUiFactory.CreateButton(actionArea, "RemoveButton", "移除", new Color(0.69f, 0.28f, 0.26f, 0.95f), Color.white, out _);
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
