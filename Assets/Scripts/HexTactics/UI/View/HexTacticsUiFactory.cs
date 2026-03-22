using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class HexTacticsUiFactory
{
    private static Font defaultFont;
    private static readonly string[] BuiltinFontCandidates =
    {
        "LegacyRuntime.ttf",
        "Arial.ttf"
    };

    public static Font DefaultFont
    {
        get
        {
            if (defaultFont == null)
            {
                defaultFont = LoadBuiltinFont();
            }

            return defaultFont;
        }
    }

    private static Font LoadBuiltinFont()
    {
        for (var i = 0; i < BuiltinFontCandidates.Length; i++)
        {
            try
            {
                var font = Resources.GetBuiltinResource<Font>(BuiltinFontCandidates[i]);
                if (font != null)
                {
                    return font;
                }
            }
            catch (ArgumentException)
            {
                // Unity 6 removed Arial.ttf from built-in fonts, so we try the next candidate.
            }
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        var rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    public static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
    }

    public static void SetOffsets(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static Image AddImage(GameObject gameObject, Color color, bool raycastTarget = true)
    {
        var image = gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static void StylePanel(Image image, Color borderColor, float shadowAlpha = 0.20f)
    {
        if (image == null)
        {
            return;
        }

        if (borderColor.a <= 0.01f)
        {
            return;
        }

        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(borderColor.r, borderColor.g, borderColor.b, Mathf.Min(borderColor.a, 0.08f));
        outline.effectDistance = new Vector2(0.6f, -0.6f);
        outline.useGraphicAlpha = true;
    }

    public static Text CreateText(
        Transform parent,
        string name,
        string content,
        int fontSize,
        TextAnchor alignment,
        Color color,
        FontStyle fontStyle = FontStyle.Normal)
    {
        var rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = fontStyle;
        text.lineSpacing = 1.0f;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.text = content;
        text.raycastTarget = false;

        return text;
    }

    public static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Color backgroundColor,
        Color textColor,
        out Text labelText)
    {
        var rect = CreateRect(name, parent);
        var image = AddImage(rect.gameObject, backgroundColor);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        HexTacticsModernUiSkin.ConfigureButton(button, image, backgroundColor);
        StylePanel(image, new Color(1f, 1f, 1f, 0.05f), 0f);

        var colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.08f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a * 0.32f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        labelText = CreateText(rect, "Label", label, 18, TextAnchor.MiddleCenter, textColor, FontStyle.Bold);
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 8;
        labelText.resizeTextMaxSize = 18;
        Stretch(labelText.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    public static RectTransform CreateScrollView(Transform parent, string name, out ScrollRect scrollRect, out RectTransform content)
    {
        var root = CreateRect(name, parent);
        var background = AddImage(root.gameObject, new Color(0.01f, 0.03f, 0.04f, 0.10f));
        background.raycastTarget = true;
        HexTacticsModernUiSkin.ApplyWindowFrame(background, new Color(1f, 1f, 1f, 0.92f));
        StylePanel(background, new Color(1f, 1f, 1f, 0.04f), 0f);

        var viewport = CreateRect("Viewport", root);
        Stretch(viewport, Vector2.zero, Vector2.one);
        SetOffsets(viewport, 4f, 4f, 4f, 4f);
        AddImage(viewport.gameObject, new Color(0f, 0f, 0f, 0f), false);
        viewport.gameObject.AddComponent<RectMask2D>();

        content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var verticalLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childControlWidth = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.spacing = 8f;
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = root.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 22f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        return root;
    }

    public static LayoutElement AddLayoutElement(GameObject gameObject, float preferredHeight = -1f, float preferredWidth = -1f, float flexibleHeight = -1f, float flexibleWidth = -1f)
    {
        var element = gameObject.AddComponent<LayoutElement>();
        if (preferredHeight >= 0f)
        {
            element.preferredHeight = preferredHeight;
        }

        if (preferredWidth >= 0f)
        {
            element.preferredWidth = preferredWidth;
        }

        if (flexibleHeight >= 0f)
        {
            element.flexibleHeight = flexibleHeight;
        }

        if (flexibleWidth >= 0f)
        {
            element.flexibleWidth = flexibleWidth;
        }

        return element;
    }

    public static void BindButton(Button button, Action handler)
    {
        button.onClick.RemoveAllListeners();
        if (handler != null)
        {
            button.onClick.AddListener(() => handler());
        }
    }

    public static void ForceRebuildLayout(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        LayoutRebuilder.MarkLayoutForRebuild(rect);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    public static void ResetViewRoot<TView>(TView view) where TView : Component
    {
        DestroyChildrenImmediate(view.transform);

        var components = view.gameObject.GetComponents<Component>();
        for (var i = components.Length - 1; i >= 0; i--)
        {
            var component = components[i];
            if (component == null || component is Transform || ReferenceEquals(component, view))
            {
                continue;
            }

            DestroyObjectImmediate(component);
        }
    }

    public static RectTransform CreatePanel(Transform parent, string name, Color backgroundColor, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
    {
        var panel = CreateRect(name, parent);
        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;
        panel.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        panel.sizeDelta = sizeDelta;
        var image = AddImage(panel.gameObject, backgroundColor);
        StylePanel(image, new Color(1f, 1f, 1f, 0.05f));
        return panel;
    }

    public static RectTransform CreateSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float height)
    {
        var section = CreateRect(name, parent);
        section.anchorMin = anchorMin;
        section.anchorMax = anchorMax;
        section.pivot = new Vector2(0.5f, anchorMax.y > 0.5f ? 1f : 0f);
        section.sizeDelta = new Vector2(0f, height);
        section.anchoredPosition = Vector2.zero;
        return section;
    }

    public static T LoadViewPrefab<T>(string resourcePath) where T : HexTacticsUiGeneratedView
    {
        var prefab = LoadViewPrefabAsset(resourcePath);
        return prefab != null ? prefab.GetComponent<T>() : null;
    }

    public static T InstantiateView<T>(T prefab, string resourcePath, Transform parent, Func<Transform, T> fallbackFactory) where T : HexTacticsUiGeneratedView
    {
        var resolvedPrefab = prefab != null ? prefab : LoadViewPrefab<T>(resourcePath);
        T instance;
        if (resolvedPrefab != null)
        {
            instance = UnityEngine.Object.Instantiate(resolvedPrefab, parent);
            instance.name = resolvedPrefab.gameObject.name;
        }
        else
        {
            instance = fallbackFactory(parent);
        }

        instance.EnsureBuilt();
        instance.gameObject.SetActive(true);
        return instance;
    }

    public static void EnsurePool<T>(
        List<T> pool,
        int count,
        T prefab,
        string resourcePath,
        Transform parent,
        Func<Transform, T> fallbackFactory) where T : HexTacticsUiGeneratedView
    {
        var resolvedPrefab = prefab != null ? prefab : LoadViewPrefab<T>(resourcePath);

        while (pool.Count < count)
        {
            T instance;
            if (resolvedPrefab != null)
            {
                instance = UnityEngine.Object.Instantiate(resolvedPrefab, parent);
                instance.name = resolvedPrefab.gameObject.name;
            }
            else
            {
                instance = fallbackFactory(parent);
            }

            instance.EnsureBuilt();
            instance.name = $"{instance.name}_{pool.Count + 1}";
            instance.gameObject.SetActive(true);
            pool.Add(instance);
        }
    }

    private static void DestroyChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            DestroyObject(parent.GetChild(i).gameObject);
        }
    }

    private static void DestroyChildrenImmediate(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
            return;
        }

        UnityEngine.Object.DestroyImmediate(target);
    }

    private static void DestroyObjectImmediate(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        UnityEngine.Object.DestroyImmediate(target);
    }

    private static GameObject LoadViewPrefabAsset(string address)
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{HexTacticsAssetPaths.AddressablesRoot}/{address}.prefab");
        }
#endif

        return HexTacticsAddressables.LoadAsset<GameObject>(address);
    }
}
