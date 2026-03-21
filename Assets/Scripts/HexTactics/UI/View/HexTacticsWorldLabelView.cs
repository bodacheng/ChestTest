using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsWorldLabelView : HexTacticsUiGeneratedView
{
    private const float BarWidth = 48f;
    private const float BarHeight = 6f;
    private const float FillInset = 1f;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform healthBarRect;
    [SerializeField] private Image healthBackgroundImage;
    [SerializeField] private RectTransform healthFillRect;
    [SerializeField] private Image healthFillImage;

    protected override int CurrentLayoutVersion => 10;

    protected override bool HasCurrentBindings =>
        rectTransform != null &&
        backgroundImage != null &&
        healthBarRect != null &&
        healthBackgroundImage != null &&
        healthFillRect != null &&
        healthFillImage != null;

    public RectTransform RectTransform => rectTransform;

    public void Bind(HexTacticsWorldLabelUiData data)
    {
        EnsureBuilt();
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        healthBackgroundImage.color = data.IsBlueTeam
            ? new Color(0.10f, 0.18f, 0.24f, 0.82f)
            : new Color(0.24f, 0.12f, 0.08f, 0.82f);

        var normalized = Mathf.Clamp01(data.HealthNormalized);
        healthFillRect.sizeDelta = new Vector2((BarWidth - FillInset * 2f) * normalized, BarHeight - FillInset * 2f);
        healthFillImage.color = new Color(0.28f, 0.82f, 0.34f, 0.95f);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        rectTransform = (RectTransform)transform;
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(52f, 10f);

        backgroundImage = HexTacticsUiFactory.AddImage(rectTransform.gameObject, new Color(0f, 0f, 0f, 0f), false);

        healthBarRect = HexTacticsUiFactory.CreateRect("HealthBar", rectTransform);
        healthBarRect.anchorMin = healthBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        healthBarRect.pivot = new Vector2(0.5f, 0.5f);
        healthBarRect.anchoredPosition = Vector2.zero;
        healthBarRect.sizeDelta = new Vector2(BarWidth, BarHeight);

        healthBackgroundImage = HexTacticsUiFactory.AddImage(healthBarRect.gameObject, new Color(0.16f, 0.22f, 0.24f, 0.92f), false);

        healthFillRect = HexTacticsUiFactory.CreateRect("Fill", healthBarRect);
        healthFillRect.anchorMin = new Vector2(0f, 0.5f);
        healthFillRect.anchorMax = new Vector2(0f, 0.5f);
        healthFillRect.pivot = new Vector2(0f, 0.5f);
        healthFillRect.anchoredPosition = new Vector2(FillInset, 0f);
        healthFillRect.sizeDelta = new Vector2(BarWidth - FillInset * 2f, BarHeight - FillInset * 2f);

        healthFillImage = HexTacticsUiFactory.AddImage(healthFillRect.gameObject, new Color(0.28f, 0.82f, 0.34f, 0.95f), false);
    }

    public static HexTacticsWorldLabelView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsWorldLabel", parent);
        var view = root.gameObject.AddComponent<HexTacticsWorldLabelView>();
        view.EnsureBuilt();
        return view;
    }
}
