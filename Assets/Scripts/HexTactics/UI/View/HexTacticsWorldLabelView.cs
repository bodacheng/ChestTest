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
    [SerializeField] private Image healthBackgroundImage;
    [SerializeField] private RectTransform healthFillRect;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Text healthText;

    protected override int CurrentLayoutVersion => 6;

    protected override bool HasCurrentBindings =>
        rectTransform != null &&
        backgroundImage != null &&
        titleText != null &&
        detailText != null &&
        healthBackgroundImage != null &&
        healthFillRect != null &&
        healthFillImage != null &&
        healthText != null;

    public RectTransform RectTransform => rectTransform;

    public void Bind(HexTacticsWorldLabelUiData data)
    {
        EnsureBuilt();
        titleText.text = data.Title;
        detailText.text = data.Detail;
        healthText.text = $"HP {data.CurrentHealth}/{data.MaxHealth}";
        titleText.color = data.IsBlueTeam ? new Color(0.72f, 0.88f, 1.00f) : new Color(1.00f, 0.83f, 0.72f);
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        healthBackgroundImage.color = data.IsBlueTeam
            ? new Color(0.10f, 0.18f, 0.24f, 0.82f)
            : new Color(0.24f, 0.12f, 0.08f, 0.82f);
        healthFillRect.localScale = new Vector3(Mathf.Clamp01(data.HealthNormalized), 1f, 1f);
        healthFillImage.color = Color.Lerp(new Color(0.90f, 0.24f, 0.18f), new Color(0.28f, 0.82f, 0.34f), data.HealthNormalized);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        rectTransform = (RectTransform)transform;
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(156f, 52f);

        backgroundImage = HexTacticsUiFactory.AddImage(rectTransform.gameObject, new Color(0f, 0f, 0f, 0f), false);

        var layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(7, 7, 4, 4);
        layout.spacing = 1f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        titleText = HexTacticsUiFactory.CreateText(rectTransform, "Title", string.Empty, 12, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.AddLayoutElement(titleText.gameObject, preferredHeight: 14f);

        detailText = HexTacticsUiFactory.CreateText(rectTransform, "Detail", string.Empty, 10, TextAnchor.MiddleLeft, new Color(0.90f, 0.92f, 0.95f));
        HexTacticsUiFactory.AddLayoutElement(detailText.gameObject, preferredHeight: 12f);

        var hpRow = HexTacticsUiFactory.CreateRect("HpRow", rectTransform);
        HexTacticsUiFactory.AddLayoutElement(hpRow.gameObject, preferredHeight: 14f);
        var hpRowLayout = hpRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hpRowLayout.spacing = 6f;
        hpRowLayout.childAlignment = TextAnchor.MiddleLeft;
        hpRowLayout.childControlHeight = true;
        hpRowLayout.childControlWidth = false;
        hpRowLayout.childForceExpandHeight = false;
        hpRowLayout.childForceExpandWidth = false;

        var barRoot = HexTacticsUiFactory.CreateRect("HealthBar", hpRow);
        HexTacticsUiFactory.AddLayoutElement(barRoot.gameObject, flexibleWidth: 1f, preferredHeight: 8f);
        healthBackgroundImage = HexTacticsUiFactory.AddImage(barRoot.gameObject, new Color(0.16f, 0.22f, 0.24f, 0.92f), false);
        healthFillRect = HexTacticsUiFactory.CreateRect("Fill", barRoot);
        HexTacticsUiFactory.Stretch(healthFillRect, Vector2.zero, Vector2.one);
        healthFillRect.pivot = new Vector2(0f, 0.5f);
        HexTacticsUiFactory.SetOffsets(healthFillRect, 1f, 1f, 1f, 1f);
        healthFillRect.localScale = Vector3.one;
        healthFillImage = HexTacticsUiFactory.AddImage(healthFillRect.gameObject, new Color(0.28f, 0.82f, 0.34f, 0.95f), false);
        HexTacticsUiFactory.Stretch(healthFillImage.rectTransform, Vector2.zero, Vector2.one);

        healthText = HexTacticsUiFactory.CreateText(hpRow, "HpText", string.Empty, 9, TextAnchor.MiddleRight, new Color(0.95f, 0.97f, 0.98f));
        HexTacticsUiFactory.AddLayoutElement(healthText.gameObject, preferredWidth: 52f, preferredHeight: 12f);
    }

    public static HexTacticsWorldLabelView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsWorldLabel", parent);
        var view = root.gameObject.AddComponent<HexTacticsWorldLabelView>();
        view.EnsureBuilt();
        return view;
    }
}
