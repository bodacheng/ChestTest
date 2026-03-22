using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsAvatarView : HexTacticsUiGeneratedView
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text fallbackText;
    [SerializeField, Min(0f)] private float iconInset = 4f;
    [SerializeField, Min(8)] private int fallbackFontSize = 18;
    [SerializeField] private bool raycastTarget = true;

    protected override int CurrentLayoutVersion => 2;

    protected override bool HasCurrentBindings =>
        backgroundImage != null &&
        iconImage != null &&
        fallbackText != null;

    public void Bind(HexTacticsAvatarUiData avatar)
    {
        EnsureBuilt();
        HexTacticsModernUiSkin.ApplySkillSlot(backgroundImage, filled: true, avatar.BackgroundColor);
        backgroundImage.color = avatar.BackgroundColor;
        iconImage.sprite = avatar.Sprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.enabled = avatar.Sprite != null;
        fallbackText.text = avatar.FallbackText;
        fallbackText.gameObject.SetActive(avatar.Sprite == null);
    }

    public override void BuildDefaultHierarchy()
    {
        HexTacticsUiFactory.ResetViewRoot(this);

        var root = (RectTransform)transform;
        HexTacticsUiFactory.Stretch(root, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(root, 0f, 0f, 0f, 0f);

        backgroundImage = HexTacticsUiFactory.AddImage(root.gameObject, new Color(0.25f, 0.34f, 0.40f, 1f), raycastTarget);
        HexTacticsModernUiSkin.ApplySkillSlot(backgroundImage, filled: true, new Color(0.25f, 0.34f, 0.40f, 1f));
        HexTacticsUiFactory.StylePanel(backgroundImage, new Color(1f, 1f, 1f, 0.04f), 0f);

        var iconRoot = HexTacticsUiFactory.CreateRect("Icon", root);
        HexTacticsUiFactory.Stretch(iconRoot, Vector2.zero, Vector2.one);
        HexTacticsUiFactory.SetOffsets(iconRoot, iconInset, iconInset, iconInset, iconInset);
        iconImage = HexTacticsUiFactory.AddImage(iconRoot.gameObject, Color.white, false);
        HexTacticsUiFactory.Stretch(iconImage.rectTransform, Vector2.zero, Vector2.one);

        fallbackText = HexTacticsUiFactory.CreateText(root, "FallbackText", string.Empty, fallbackFontSize, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        HexTacticsUiFactory.Stretch(fallbackText.rectTransform, Vector2.zero, Vector2.one);
    }

    public static HexTacticsAvatarView CreateStandalone(
        Transform parent,
        string name,
        float inset,
        int fontSize,
        bool backgroundRaycastTarget = true)
    {
        var root = HexTacticsUiFactory.CreateRect(name, parent);
        var view = root.gameObject.AddComponent<HexTacticsAvatarView>();
        view.iconInset = Mathf.Max(0f, inset);
        view.fallbackFontSize = Mathf.Max(8, fontSize);
        view.raycastTarget = backgroundRaycastTarget;
        view.EnsureBuilt();
        return view;
    }
}
