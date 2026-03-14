using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsResolvingScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text roundText;
    [SerializeField] private Text countText;
    [SerializeField] private Text statusText;

    protected override int CurrentLayoutVersion => 2;

    protected override bool HasCurrentBindings =>
        roundText != null &&
        countText != null &&
        statusText != null;

    public RectTransform Root => (RectTransform)transform;

    public void Bind(HexTacticsUiSnapshot snapshot)
    {
        EnsureBuilt();
        roundText.text = $"第 {snapshot.PlanningRoundNumber} 轮同步结算  |  {snapshot.TurnTypeLabel}";
        countText.text = $"蓝方剩余 {snapshot.BlueAliveCount} 名  |  红方剩余 {snapshot.RedAliveCount} 名";
        statusText.text = snapshot.ResolutionStatus;
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(420f, 248f);
        root.anchoredPosition = new Vector2(22f, -22f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.86f));
        HexTacticsUiFactory.StylePanel(panel, new Color(0.75f, 0.90f, 0.88f, 0.08f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var badge = HexTacticsUiFactory.CreateText(root, "Badge", "RESOLUTION", 13, TextAnchor.MiddleLeft, new Color(0.63f, 0.82f, 0.80f), FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(badge.gameObject, preferredHeight: 18f);

        roundText = HexTacticsUiFactory.CreateText(root, "RoundText", string.Empty, 20, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(roundText.gameObject, preferredHeight: 28f);

        countText = HexTacticsUiFactory.CreateText(root, "CountText", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 0.92f));
        HexTacticsUiFactory.AddLayoutElement(countText.gameObject, preferredHeight: 20f);

        statusText = HexTacticsUiFactory.CreateText(root, "StatusText", string.Empty, 16, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.98f));
        HexTacticsUiFactory.AddLayoutElement(statusText.gameObject, preferredHeight: 44f);

        var hint1 = HexTacticsUiFactory.CreateText(root, "Hint1", "范围外的格子或敌人也能作为目标，单位会沿最短路持续逼近。", 14, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(hint1.gameObject, preferredHeight: 30f);

        var hint2 = HexTacticsUiFactory.CreateText(root, "Hint2", "若目标格被占用，本回合会原地等待；抢位冲突则随机判定先后。", 14, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(hint2.gameObject, preferredHeight: 30f);
    }

    public static HexTacticsResolvingScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsResolvingScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsResolvingScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
