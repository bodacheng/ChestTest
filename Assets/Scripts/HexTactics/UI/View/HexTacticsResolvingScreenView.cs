using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsResolvingScreenView : HexTacticsUiGeneratedView
{
    [SerializeField] private Text roundText;
    [SerializeField] private Text countText;
    [SerializeField] private Text statusText;

    protected override int CurrentLayoutVersion => 4;

    protected override bool HasCurrentBindings =>
        roundText != null &&
        countText != null &&
        statusText != null;

    public RectTransform Root => (RectTransform)transform;

    public void Bind(HexTacticsUiSnapshot snapshot)
    {
        EnsureBuilt();
        roundText.text = $"第 {snapshot.PlanningRoundNumber} 轮  |  {snapshot.TurnTypeLabel}";
        countText.text = $"蓝 {snapshot.BlueAliveCount}  |  红 {snapshot.RedAliveCount}";
        statusText.text = snapshot.ResolutionStatus;
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = Root;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(356f, 136f);
        root.anchoredPosition = new Vector2(18f, -18f);

        var panel = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.04f, 0.07f, 0.08f, 0.80f));
        HexTacticsModernUiSkin.ApplyPopupPanel(panel, new Color(1f, 1f, 1f, 0.96f));
        HexTacticsUiFactory.StylePanel(panel, new Color(1f, 1f, 1f, 0.05f));

        var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var chip = HexTacticsUiFactory.CreateRect("ResolveChip", root);
        HexTacticsUiFactory.AddLayoutElement(chip.gameObject, preferredHeight: 26f, preferredWidth: 132f);
        var chipImage = HexTacticsUiFactory.AddImage(chip.gameObject, new Color(0.26f, 0.32f, 0.18f, 0.95f), false);
        HexTacticsModernUiSkin.ApplyHeaderChip(chipImage, new Color(0.26f, 0.32f, 0.18f, 0.95f));
        var chipText = HexTacticsUiFactory.CreateText(chip, "ChipText", "回合结算中", 12, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(chipText.rectTransform, Vector2.zero, Vector2.one);

        roundText = HexTacticsUiFactory.CreateText(root, "RoundText", string.Empty, 17, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(roundText.gameObject, preferredHeight: 20f);

        countText = HexTacticsUiFactory.CreateText(root, "CountText", string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.82f, 0.88f, 0.90f));
        HexTacticsUiFactory.AddLayoutElement(countText.gameObject, preferredHeight: 18f);

        statusText = HexTacticsUiFactory.CreateText(root, "StatusText", string.Empty, 14, TextAnchor.UpperLeft, new Color(0.92f, 0.94f, 0.96f));
        statusText.resizeTextForBestFit = true;
        statusText.resizeTextMinSize = 10;
        statusText.resizeTextMaxSize = 14;
        HexTacticsUiFactory.AddLayoutElement(statusText.gameObject, preferredHeight: 44f);
    }

    public static HexTacticsResolvingScreenView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsResolvingScreen", parent);
        var view = root.gameObject.AddComponent<HexTacticsResolvingScreenView>();
        view.EnsureBuilt();
        return view;
    }
}
