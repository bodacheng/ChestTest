using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsSelectedRosterRowView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text statsText;
    [SerializeField] private Button removeButton;

    protected override int CurrentLayoutVersion => 1;

    protected override bool HasCurrentBindings =>
        titleText != null &&
        statsText != null &&
        removeButton != null;

    public void Bind(HexTacticsSelectionEntryUiData data, Action<int> onRemove)
    {
        EnsureBuilt();
        titleText.text = $"{data.SelectionIndex + 1}. {data.DisplayName}";
        statsText.text = $"HP {data.MaxHealth}  ATK {data.AttackPower}  MOVE {data.MoveRange}  COST {data.Cost}";
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => onRemove?.Invoke(data.SelectionIndex));
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = (RectTransform)transform;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(0f, 66f);

        HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.08f, 0.09f, 0.11f, 0.78f));
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 66f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var content = HexTacticsUiFactory.CreateRect("Content", root);
        HexTacticsUiFactory.AddLayoutElement(content.gameObject, flexibleWidth: 1f);
        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 2f;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(content, "Title", string.Empty, 17, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 22f);

        statsText = HexTacticsUiFactory.CreateText(content, "Stats", string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.91f));
        HexTacticsUiFactory.AddLayoutElement(statsText.gameObject, preferredHeight: 18f);

        var actionArea = HexTacticsUiFactory.CreateRect("ActionArea", root);
        HexTacticsUiFactory.AddLayoutElement(actionArea.gameObject, preferredWidth: 86f, flexibleHeight: 1f);
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
}
