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

    protected override int CurrentLayoutVersion => 14;

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
        statsText.text = $"{data.DeploymentText}  ·  {TrimLabel(data.PrimarySkillName, 6)}";
        deploymentText.text = BuildCompactStats(data.MaxHealth, data.AttackPower, data.AttackRange, data.MaxEnergy, data.Speed, data.MoveRange, data.Cost, data.SkillCount);
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
        root.sizeDelta = new Vector2(0f, 114f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.06f, 0.09f, 0.10f, 0.72f));
        HexTacticsModernUiSkin.ApplyCardPanel(panel, new Color(1f, 1f, 1f, 0.92f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.04f), 0f);
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 114f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var avatarRoot = HexTacticsUiFactory.CreateRect("Avatar", root);
        HexTacticsUiFactory.AddLayoutElement(avatarRoot.gameObject, preferredWidth: 58f, preferredHeight: 58f);
        avatarView = HexTacticsAvatarView.CreateStandalone(avatarRoot, "AvatarView", 5f, 21);

        var content = HexTacticsUiFactory.CreateRect("Content", root);
        var contentElement = HexTacticsUiFactory.AddLayoutElement(content.gameObject, flexibleWidth: 1f);
        contentElement.minWidth = 0f;
        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 2f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(content, "Title", string.Empty, 17, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 14;
        titleText.resizeTextMaxSize = 17;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 24f);

        statsText = HexTacticsUiFactory.CreateText(content, "Stats", string.Empty, 12, TextAnchor.UpperLeft, new Color(0.73f, 0.80f, 0.84f));
        statsText.resizeTextForBestFit = true;
        statsText.resizeTextMinSize = 10;
        statsText.resizeTextMaxSize = 12;
        statsText.horizontalOverflow = HorizontalWrapMode.Overflow;
        statsText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(statsText.gameObject, preferredHeight: 18f);

        deploymentText = HexTacticsUiFactory.CreateText(content, "Deployment", string.Empty, 11, TextAnchor.UpperLeft, new Color(0.60f, 0.70f, 0.74f));
        deploymentText.resizeTextForBestFit = true;
        deploymentText.resizeTextMinSize = 9;
        deploymentText.resizeTextMaxSize = 11;
        deploymentText.horizontalOverflow = HorizontalWrapMode.Overflow;
        deploymentText.verticalOverflow = VerticalWrapMode.Truncate;
        HexTacticsUiFactory.AddLayoutElement(deploymentText.gameObject, preferredHeight: 16f);

        var actionArea = HexTacticsUiFactory.CreateRect("ActionArea", root);
        HexTacticsUiFactory.AddLayoutElement(actionArea.gameObject, preferredWidth: 64f, preferredHeight: 34f);
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

    private static string TrimLabel(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "通常攻";
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed.Substring(0, Mathf.Max(1, maxLength - 3)) + "...";
    }

    private static string BuildCompactStats(int maxHealth, int attackPower, int attackRange, int maxEnergy, int speed, int moveRange, int cost, int _skillCount)
    {
        return $"HP{maxHealth} 攻{attackPower} 射{attackRange} EN{maxEnergy} 速{speed} 移{moveRange} 费{cost}";
    }
}
