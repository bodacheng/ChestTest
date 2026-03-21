using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HexTacticsSkillPopupView : HexTacticsUiGeneratedView
{
    public const float PopupWidth = 196f;
    public const float PopupMargin = 12f;
    public const float PopupOffsetX = 16f;
    public const float PopupOffsetY = 18f;
    public const float PopupPadding = 8f;
    public const float PopupSpacing = 4f;
    public const float RowHeight = 30f;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image panelImage;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private HexTacticsSkillChoiceRowView skillChoiceRowPrefab;

    private readonly List<HexTacticsSkillChoiceRowView> skillRows = new();

    protected override int CurrentLayoutVersion => 1;

    protected override bool HasCurrentBindings =>
        rectTransform != null &&
        panelImage != null &&
        contentRoot != null;

    public RectTransform Root => rectTransform;

    public void Bind(string title, List<HexTacticsSkillChoiceUiData> skillEntries, Action<int> onSelectSkill)
    {
        EnsureBuilt();

        var entryCount = skillEntries != null ? skillEntries.Count : 0;
        ApplyPopupSize(entryCount);
        HexTacticsUiFactory.EnsurePool(
            skillRows,
            entryCount,
            skillChoiceRowPrefab,
            HexTacticsUiResourcePaths.SkillChoiceRow,
            contentRoot,
            HexTacticsSkillChoiceRowView.CreateStandalone);

        for (var i = 0; i < skillRows.Count; i++)
        {
            var active = i < entryCount;
            skillRows[i].gameObject.SetActive(active);
            if (active)
            {
                skillRows[i].Bind(skillEntries[i], onSelectSkill);
            }
        }

        HexTacticsUiFactory.ForceRebuildLayout(contentRoot);
        HexTacticsUiFactory.ForceRebuildLayout(Root);
    }

    public override void BuildDefaultHierarchy()
    {
        skillRows.Clear();
        HexTacticsUiFactory.ResetViewRoot(this);

        rectTransform = (RectTransform)transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.sizeDelta = CalculatePopupSize(2);

        panelImage = HexTacticsUiFactory.AddImage(rectTransform.gameObject, new Color(1f, 1f, 1f, 0.98f));
        HexTacticsModernUiSkin.ApplyActionBarFrame(panelImage, new Color(1f, 1f, 1f, 0.96f));
        HexTacticsUiFactory.StylePanel(panelImage, new Color(1f, 1f, 1f, 0.08f), 0f);

        var layout = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)PopupPadding, (int)PopupPadding, (int)PopupPadding, (int)PopupPadding);
        layout.spacing = PopupSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        contentRoot = HexTacticsUiFactory.CreateRect("Content", rectTransform);
        HexTacticsUiFactory.AddLayoutElement(contentRoot.gameObject, preferredHeight: 76f);
        var contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = PopupSpacing;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
    }

    public static HexTacticsSkillPopupView CreateStandalone(Transform parent)
    {
        var root = HexTacticsUiFactory.CreateRect("HexTacticsSkillPopup", parent);
        var view = root.gameObject.AddComponent<HexTacticsSkillPopupView>();
        view.EnsureBuilt();
        return view;
    }

    public static Vector2 CalculatePopupSize(int entryCount)
    {
        var count = Mathf.Max(1, entryCount);
        return new Vector2(
            PopupWidth,
            PopupPadding * 2f + count * RowHeight + Mathf.Max(0, count - 1) * PopupSpacing);
    }

    public static Vector2 CalculatePopupScreenPosition(Vector2 pressScreenPosition, int entryCount, Vector2 screenSize)
    {
        var size = CalculatePopupSize(entryCount);
        var x = Mathf.Clamp(pressScreenPosition.x + PopupOffsetX, PopupMargin, Mathf.Max(PopupMargin, screenSize.x - size.x - PopupMargin));
        var y = Mathf.Clamp(pressScreenPosition.y + PopupOffsetY, PopupMargin, Mathf.Max(PopupMargin, screenSize.y - size.y - PopupMargin));
        return new Vector2(x, y);
    }

    public static int ResolveSkillIndexAtScreenPosition(Vector2 pointerScreenPosition, Vector2 popupScreenPosition, int entryCount)
    {
        if (entryCount <= 0)
        {
            return -1;
        }

        var size = CalculatePopupSize(entryCount);
        var popupRect = new Rect(popupScreenPosition, size);
        if (!popupRect.Contains(pointerScreenPosition))
        {
            return -1;
        }

        var localX = pointerScreenPosition.x - popupRect.xMin;
        var localY = pointerScreenPosition.y - popupRect.yMin;
        if (localX < PopupPadding || localX > size.x - PopupPadding)
        {
            return -1;
        }

        var topY = size.y - PopupPadding;
        for (var i = 0; i < entryCount; i++)
        {
            var rowTop = topY - i * (RowHeight + PopupSpacing);
            var rowBottom = rowTop - RowHeight;
            if (localY >= rowBottom && localY <= rowTop)
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyPopupSize(int entryCount)
    {
        rectTransform.sizeDelta = CalculatePopupSize(entryCount);
        var contentLayout = contentRoot.GetComponent<LayoutElement>();
        if (contentLayout != null)
        {
            contentLayout.preferredHeight = Mathf.Max(1f, entryCount) * RowHeight + Mathf.Max(0, entryCount - 1) * PopupSpacing;
        }
    }
}
