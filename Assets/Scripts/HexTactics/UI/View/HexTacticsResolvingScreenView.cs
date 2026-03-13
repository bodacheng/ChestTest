using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsResolvingScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text roundText;
    [SerializeField] private Text countText;
    [SerializeField] private Text statusText;

    protected override int CurrentLayoutVersion => 1;

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
        root.sizeDelta = new Vector2(530f, 230f);
        root.anchoredPosition = new Vector2(18f, -18f);

        HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.05f, 0.08f, 0.10f, 0.86f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        roundText = HexTacticsUiFactory.CreateText(root, "RoundText", string.Empty, 19, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(roundText.gameObject, preferredHeight: 26f);

        countText = HexTacticsUiFactory.CreateText(root, "CountText", string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.82f, 0.90f, 0.92f));
        HexTacticsUiFactory.AddLayoutElement(countText.gameObject, preferredHeight: 22f);

        statusText = HexTacticsUiFactory.CreateText(root, "StatusText", string.Empty, 16, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.98f));
        HexTacticsUiFactory.AddLayoutElement(statusText.gameObject, preferredHeight: 64f);

        var hint1 = HexTacticsUiFactory.CreateText(root, "Hint1", "范围外的格子或敌人也能作为目标；本轮会按最短路尽量接近，追击只有在最终贴到指定敌人时才会攻击", 14, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(hint1.gameObject, preferredHeight: 38f);

        var hint2 = HexTacticsUiFactory.CreateText(root, "Hint2", "同一格抢位会随机判定胜者；若目标格暂时被别的棋子占住，本回合会原地等待，等后续空间腾出再继续尝试", 14, TextAnchor.MiddleLeft, new Color(0.66f, 0.80f, 0.79f));
        HexTacticsUiFactory.AddLayoutElement(hint2.gameObject, preferredHeight: 40f);
    }

    public static HexTacticsResolvingScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsResolvingScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsResolvingScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
