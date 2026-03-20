using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsModeSelectScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Button cpuButton;

    protected override int CurrentLayoutVersion => 3;

    protected override bool HasCurrentBindings => cpuButton != null;

    public RectTransform Root => (RectTransform)transform;

    public void Bind(Action startCpuMode)
    {
        EnsureBuilt();
        HexTacticsUiFactory.BindButton(cpuButton, startCpuMode);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(500f, 220f);
        root.anchoredPosition = new Vector2(0f, 40f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.82f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.05f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var title = HexTacticsUiFactory.CreateText(root, "Title", "六方向战棋", 30, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 36f);

        var description = HexTacticsUiFactory.CreateText(root, "Description", "先完成编队，再进入同步结算战斗。", 16, TextAnchor.MiddleCenter, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(description.gameObject, preferredHeight: 24f);

        var hint = HexTacticsUiFactory.CreateText(root, "Hint", "当前开放单人对战。", 13, TextAnchor.MiddleCenter, new Color(0.62f, 0.72f, 0.76f));
        HexTacticsUiFactory.AddLayoutElement(hint.gameObject, preferredHeight: 18f);

        cpuButton = HexTacticsUiFactory.CreateButton(root, "CpuButton", "开始对战", new Color(0.19f, 0.46f, 0.46f, 0.94f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(cpuButton.gameObject, preferredHeight: 46f, preferredWidth: 220f);
    }

    public static HexTacticsModeSelectScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsModeSelectScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsModeSelectScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
