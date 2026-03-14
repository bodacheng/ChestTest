using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsModeSelectScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Button cpuButton;

    protected override int CurrentLayoutVersion => 2;

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
        root.sizeDelta = new Vector2(560f, 320f);
        root.anchoredPosition = new Vector2(0f, 72f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.88f));
        HexTacticsUiFactory.StylePanel(panel, new Color(0.85f, 0.94f, 0.92f, 0.10f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 28, 28);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var badge = HexTacticsUiFactory.CreateText(root, "Badge", "HEX TACTICS PROTOTYPE", 13, TextAnchor.MiddleCenter, new Color(0.63f, 0.82f, 0.80f), FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(badge.gameObject, preferredHeight: 18f);

        var title = HexTacticsUiFactory.CreateText(root, "Title", "六方向战棋原型", 31, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(title.gameObject, preferredHeight: 38f);

        var subtitle = HexTacticsUiFactory.CreateText(root, "Subtitle", "模式选择", 18, TextAnchor.MiddleCenter, new Color(0.72f, 0.88f, 0.82f), FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(subtitle.gameObject, preferredHeight: 24f);

        var description = HexTacticsUiFactory.CreateText(root, "Description", "当前提供“对战 CPU”模式。先完成编队，再进入同步指令结算战斗。", 18, TextAnchor.MiddleCenter, new Color(0.90f, 0.92f, 0.94f));
        HexTacticsUiFactory.AddLayoutElement(description.gameObject, preferredHeight: 64f);

        cpuButton = HexTacticsUiFactory.CreateButton(root, "CpuButton", "对战 CPU", new Color(0.22f, 0.56f, 0.55f, 0.96f), Color.white, out _);
        HexTacticsUiFactory.AddLayoutElement(cpuButton.gameObject, preferredHeight: 52f, preferredWidth: 236f);
    }

    public static HexTacticsModeSelectScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsModeSelectScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsModeSelectScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
