using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsWorldLabelView : HexTacticsUiGeneratedView
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text detailText;

    protected override int CurrentLayoutVersion => 1;

    protected override bool HasCurrentBindings =>
        rectTransform != null &&
        backgroundImage != null &&
        titleText != null &&
        detailText != null;

    public RectTransform RectTransform => rectTransform;

    public void Bind(HexTacticsWorldLabelUiData data)
    {
        EnsureBuilt();
        titleText.text = data.Title;
        detailText.text = data.Detail;
        titleText.color = data.IsBlueTeam ? new Color(0.72f, 0.88f, 1.00f) : new Color(1.00f, 0.83f, 0.72f);
        backgroundImage.color = data.IsBlueTeam
            ? new Color(0.03f, 0.08f, 0.11f, 0.82f)
            : new Color(0.12f, 0.07f, 0.05f, 0.82f);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        rectTransform = (RectTransform)transform;
        rectTransform.sizeDelta = new Vector2(168f, 44f);

        backgroundImage = HexTacticsUiFactory.AddImage(rectTransform.gameObject, new Color(0f, 0f, 0f, 0.82f), false);

        var layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = 1f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(rectTransform, "Title", string.Empty, 12, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 14f);

        detailText = HexTacticsUiFactory.CreateText(rectTransform, "Detail", string.Empty, 11, TextAnchor.MiddleLeft, new Color(0.90f, 0.92f, 0.95f));
        HexTacticsUiFactory.AddLayoutElement(detailText.gameObject, preferredHeight: 13f);
    }

    public static HexTacticsWorldLabelView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsWorldLabel", parent);
        var view = root.gameObject.AddComponent<HexTacticsWorldLabelView>();
        view.EnsureBuilt();
        return view;
    }
}
