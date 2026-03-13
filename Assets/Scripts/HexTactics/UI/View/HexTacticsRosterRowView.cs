using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsRosterRowView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text statsText;
    [SerializeField] private Text hintText;
    [SerializeField] private Button addButton;

    protected override int CurrentLayoutVersion => 1;

    protected override bool HasCurrentBindings =>
        titleText != null &&
        statsText != null &&
        hintText != null &&
        addButton != null;

    public void Bind(HexTacticsRosterEntryUiData data, Action<int> onAdd)
    {
        EnsureBuilt();
        titleText.text = $"{data.DisplayName}  [{data.Description}]";
        statsText.text = $"HP {data.MaxHealth}  ATK {data.AttackPower}  MOVE {data.MoveRange}  COST {data.Cost}";
        hintText.text = "可重复加入队伍，只受总 cost 限制";
        addButton.interactable = data.CanAdd;
        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(() => onAdd?.Invoke(data.RosterIndex));
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = (RectTransform)transform;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(0f, 94f);

        HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.07f, 0.10f, 0.12f, 0.84f));
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 94f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var content = HexTacticsUiFactory.CreateRect("Content", root);
        HexTacticsUiFactory.AddLayoutElement(content.gameObject, flexibleWidth: 1f);
        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 4f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(content, "Title", string.Empty, 19, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 24f);

        statsText = HexTacticsUiFactory.CreateText(content, "Stats", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.80f, 0.87f, 0.91f));
        HexTacticsUiFactory.AddLayoutElement(statsText.gameObject, preferredHeight: 22f);

        hintText = HexTacticsUiFactory.CreateText(content, "Hint", string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.63f, 0.76f, 0.77f));
        HexTacticsUiFactory.AddLayoutElement(hintText.gameObject, preferredHeight: 18f);

        var actionArea = HexTacticsUiFactory.CreateRect("ActionArea", root);
        HexTacticsUiFactory.AddLayoutElement(actionArea.gameObject, preferredWidth: 92f, flexibleHeight: 1f);
        addButton = HexTacticsUiFactory.CreateButton(actionArea, "AddButton", "加入", new Color(0.22f, 0.56f, 0.55f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.Stretch(addButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
    }

    public static HexTacticsRosterRowView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsRosterRow", parent);
        var view = root.gameObject.AddComponent<HexTacticsRosterRowView>();
        view.EnsureBuilt();
        return view;
    }
}
