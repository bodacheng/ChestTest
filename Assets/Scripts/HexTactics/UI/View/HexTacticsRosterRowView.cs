using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsRosterRowView : HexTacticsUiGeneratedView, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private HexTacticsAvatarView avatarView;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statsText;
    [SerializeField] private Text hintText;
    [SerializeField] private Button addButton;

    private HexTacticsRosterEntryUiData currentData;
    private Action<HexTacticsRosterEntryUiData, Vector2> beginDragHandler;
    private Action<Vector2> dragHandler;
    private Action<Vector2> endDragHandler;

    protected override int CurrentLayoutVersion => 11;

    protected override bool HasCurrentBindings =>
        avatarView != null &&
        titleText != null &&
        statsText != null &&
        hintText != null &&
        addButton != null;

    public void Bind(
        HexTacticsRosterEntryUiData data,
        Action<int> onAdd,
        Action<HexTacticsRosterEntryUiData, Vector2> onBeginDrag,
        Action<Vector2> onDrag,
        Action<Vector2> onEndDrag)
    {
        EnsureBuilt();
        currentData = data;
        beginDragHandler = onBeginDrag;
        dragHandler = onDrag;
        endDragHandler = onEndDrag;

        ApplyAvatar(data.Avatar);
        titleText.text = data.DisplayName;
        statsText.text = BuildCompactDescription(data.Description);
        hintText.text = $"HP {data.MaxHealth}  /  技 {data.SkillCount}  /  主技 {data.PrimarySkillName}  /  攻 {data.AttackPower}  /  射 {data.AttackRange}  /  EN {data.MaxEnergy}  /  速 {data.Speed}  /  移 {data.MoveRange}  /  费 {data.Cost}";
        addButton.interactable = data.CanAdd;
        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(() => onAdd?.Invoke(data.RosterIndex));
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
        root.sizeDelta = new Vector2(0f, 136f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.06f, 0.09f, 0.10f, 0.72f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.04f), 0f);
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 136f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", root);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 72f, preferredHeight: 72f);
        avatarView = HexTacticsAvatarView.CreateStandalone(avatarRoot, "AvatarView", 5f, 22);

        var content = HexTacticsUiFactory.CreateRect("Content", root);
        HexTacticsUiFactory.AddLayoutElement(content.gameObject, flexibleWidth: 1f);
        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(content, "Title", string.Empty, 18, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 15;
        titleText.resizeTextMaxSize = 19;
        titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 28f);

        statsText = HexTacticsUiFactory.CreateText(content, "Stats", string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.73f, 0.80f, 0.84f));
        statsText.resizeTextForBestFit = true;
        statsText.resizeTextMinSize = 12;
        statsText.resizeTextMaxSize = 14;
        statsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statsText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(statsText.gameObject, preferredHeight: 26f);

        hintText = HexTacticsUiFactory.CreateText(content, "Hint", string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.60f, 0.70f, 0.74f));
        hintText.resizeTextForBestFit = true;
        hintText.resizeTextMinSize = 12;
        hintText.resizeTextMaxSize = 13;
        hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        hintText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(hintText.gameObject, preferredHeight: 24f);

        var actionArea = HexTacticsUiFactory.CreateRect("ActionArea", root);
        HexTacticsUiFactory.AddLayoutElement(actionArea.gameObject, preferredWidth: 78f, preferredHeight: 44f);
        addButton = HexTacticsUiFactory.CreateButton(actionArea, "AddButton", "加入", new Color(0.19f, 0.46f, 0.46f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.Stretch(addButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
    }

    public static HexTacticsRosterRowView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsRosterRow", parent);
        var view = root.gameObject.AddComponent<HexTacticsRosterRowView>();
        view.EnsureBuilt();
        return view;
    }

    private void ApplyAvatar(HexTacticsAvatarUiData avatar)
    {
        avatarView.Bind(avatar);
    }

    private static string BuildCompactDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "标准近战单位";
        }

        var sanitized = description.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (sanitized.Length <= 18)
        {
            return sanitized;
        }

        return sanitized.Substring(0, 16) + "...";
    }
}
