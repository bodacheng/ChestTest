using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsSelectedRosterRowView : HexTacticsUiGeneratedView, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private HexTacticsAvatarView avatarView;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statsText;
    [SerializeField] private Text deploymentText;
    [SerializeField] private Button removeButton;

    private HexTacticsSelectionEntryUiData currentData;
    private Action<HexTacticsSelectionEntryUiData, Vector2> beginDragHandler;
    private Action<Vector2> dragHandler;
    private Action<Vector2> endDragHandler;

    protected override int CurrentLayoutVersion => 11;

    protected override bool HasCurrentBindings =>
        avatarView != null &&
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
        deploymentText.text = $"HP {data.MaxHealth}  /  攻 {data.AttackPower}  /  射 {data.AttackRange}  /  速 {data.Speed}  /  移 {data.MoveRange}  /  费 {data.Cost}";
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
        root.sizeDelta = new Vector2(0f, 132f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.06f, 0.09f, 0.10f, 0.72f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.04f), 0f);
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 132f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", root);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 68f, preferredHeight: 68f);
        avatarView = HexTacticsAvatarView.CreateStandalone(avatarRoot, "AvatarView", 5f, 21);

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
        avatarView.Bind(avatar);
    }
}
