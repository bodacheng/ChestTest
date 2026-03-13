using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsCommandRowView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text commandText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Text selectButtonText;
    [SerializeField] private Button waitButton;

    protected override int CurrentLayoutVersion => 1;

    protected override bool HasCurrentBindings =>
        commandText != null &&
        selectButton != null &&
        selectButtonText != null &&
        waitButton != null;

    public void Bind(HexTacticsCommandEntryUiData data, Action<int> onSelect, Action<int> onWait)
    {
        EnsureBuilt();
        commandText.text = $"{data.UnitName} -> {data.CommandText}";
        selectButtonText.text = data.IsSelected ? "已选中" : "选择";
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
        root.sizeDelta = new Vector2(0f, 42f);

        HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.06f, 0.08f, 0.10f, 0.65f));
        HexTacticsUiFactory.AddLayoutElement(root.gameObject, preferredHeight: 42f);

        var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        commandText = HexTacticsUiFactory.CreateText(root, "CommandText", string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        HexTacticsUiFactory.AddLayoutElement(commandText.gameObject, flexibleWidth: 1f, preferredHeight: 22f);

        selectButton = HexTacticsUiFactory.CreateButton(root, "SelectButton", "选择", new Color(0.20f, 0.38f, 0.62f, 0.96f), Color.white, out selectButtonText);
        HexTacticsUiFactory.AddLayoutElement(selectButton.gameObject, preferredWidth: 88f, preferredHeight: 28f);

        waitButton = HexTacticsUiFactory.CreateButton(root, "WaitButton", "待机", new Color(0.31f, 0.52f, 0.26f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(waitButton.gameObject, preferredWidth: 76f, preferredHeight: 28f);
    }

    public static HexTacticsCommandRowView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsCommandRow", parent);
        var view = root.gameObject.AddComponent<HexTacticsCommandRowView>();
        view.EnsureBuilt();
        return view;
    }
}
