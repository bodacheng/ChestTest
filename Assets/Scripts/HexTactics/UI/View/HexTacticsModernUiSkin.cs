using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class HexTacticsModernUiSkin
{
    public const string HudPanelSpritePath = "builtin://hextactics-ui/hud-panel";
    public const string PopupPanelSpritePath = "builtin://hextactics-ui/popup-panel";
    public const string CardPanelSpritePath = "builtin://hextactics-ui/card-panel";
    public const string WindowFrameSpritePath = "builtin://hextactics-ui/window-frame";
    public const string ButtonSpritePath = "builtin://hextactics-ui/button";
    public const string SkillSlotEmptySpritePath = "builtin://hextactics-ui/skill-slot-empty";
    public const string SkillSlotFilledSpritePath = "builtin://hextactics-ui/skill-slot-filled";
    public const string ActionBarFrameSpritePath = "builtin://hextactics-ui/action-bar-frame";
    public const string HealthBarEmptySpritePath = "builtin://hextactics-ui/health-bar-empty";
    public const string HealthBarFillSpritePath = "builtin://hextactics-ui/health-bar-fill";
    public const string PowerIconSpritePath = "builtin://hextactics-ui/icon-power";
    public const string WaitIconSpritePath = "builtin://hextactics-ui/icon-wait";

    // The skin is generated procedurally, so there are no external UI sprites to register.
    public static readonly string[] RequiredSpriteAssetPaths = Array.Empty<string>();

    private enum SkinSpriteKind
    {
        HudPanel,
        PopupPanel,
        CardPanel,
        WindowFrame,
        Button,
        SkillSlotEmpty,
        SkillSlotFilled,
        ActionBarFrame,
        HealthBarBackground,
        HealthBarFill,
        HeaderChip,
        LoadingBarBackground,
        LoadingBarFill,
        IconGeneric,
        IconPower,
        IconWait
    }

    private static readonly Dictionary<SkinSpriteKind, Sprite> SpriteCache = new();
    private static readonly Dictionary<string, Sprite> IconCache = new(StringComparer.Ordinal);

    public static void ApplyHudPanel(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.HudPanel, tint ?? new Color(1f, 1f, 1f, 0.96f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyPopupPanel(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.PopupPanel, tint ?? new Color(1f, 1f, 1f, 0.98f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyCardPanel(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.CardPanel, tint ?? new Color(1f, 1f, 1f, 0.94f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyWindowFrame(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.WindowFrame, tint ?? new Color(1f, 1f, 1f, 0.96f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyButton(Image image, Color tint)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.Button, tint, preserveAspect: false, preferSliced: true);
    }

    public static void ConfigureButton(Button button, Image image, Color tint)
    {
        ApplyButton(image, tint);
        if (button == null)
        {
            return;
        }

        button.transition = Selectable.Transition.ColorTint;
    }

    public static void ApplySkillSlot(Image image, bool filled, Color tint)
    {
        ApplyGeneratedSprite(image, filled ? SkinSpriteKind.SkillSlotFilled : SkinSpriteKind.SkillSlotEmpty, tint, preserveAspect: false, preferSliced: true);
    }

    public static void ApplyActionBarFrame(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.ActionBarFrame, tint ?? new Color(1f, 1f, 1f, 0.94f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyHeaderChip(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.HeaderChip, tint ?? new Color(1f, 1f, 1f, 0.96f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyHealthBarBackground(Image image)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.HealthBarBackground, Color.white, preserveAspect: false, preferSliced: true);
    }

    public static void ApplyHealthBarFill(Image image)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.HealthBarFill, Color.white, preserveAspect: false, preferSliced: true);
    }

    public static void ApplyLoadingBarBackground(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.LoadingBarBackground, tint ?? new Color(1f, 1f, 1f, 0.94f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyLoadingBarFill(Image image, Color? tint = null)
    {
        ApplyGeneratedSprite(image, SkinSpriteKind.LoadingBarFill, tint ?? new Color(1f, 1f, 1f, 0.98f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyIcon(Image image, string assetPath, Color? tint = null)
    {
        ApplyLoadedSprite(image, LoadSprite(assetPath), tint ?? Color.white, preserveAspect: true, preferSliced: false);
    }

    public static Sprite LoadSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return GetSprite(SkinSpriteKind.IconGeneric);
        }

        if (TryResolveBuiltInKind(assetPath, out var builtInKind))
        {
            return GetSprite(builtInKind);
        }

        if (IconCache.TryGetValue(assetPath, out var cachedSprite))
        {
            return cachedSprite;
        }

        var sprite = CreateHashedIconSprite(assetPath);
        IconCache[assetPath] = sprite;
        return sprite;
    }

    private static void ApplyGeneratedSprite(Image image, SkinSpriteKind kind, Color tint, bool preserveAspect, bool preferSliced)
    {
        ApplyLoadedSprite(image, GetSprite(kind), tint, preserveAspect, preferSliced);
    }

    private static void ApplyLoadedSprite(Image image, Sprite sprite, Color tint, bool preserveAspect, bool preferSliced)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = preferSliced && sprite != null && sprite.border.sqrMagnitude > 0f
            ? Image.Type.Sliced
            : Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = tint;
    }

    private static Sprite GetSprite(SkinSpriteKind kind)
    {
        if (SpriteCache.TryGetValue(kind, out var cachedSprite))
        {
            return cachedSprite;
        }

        var sprite = kind switch
        {
            SkinSpriteKind.HudPanel => CreateHudPanelSprite(),
            SkinSpriteKind.PopupPanel => CreatePopupPanelSprite(),
            SkinSpriteKind.CardPanel => CreateCardPanelSprite(),
            SkinSpriteKind.WindowFrame => CreateWindowFrameSprite(),
            SkinSpriteKind.Button => CreateButtonSprite(),
            SkinSpriteKind.SkillSlotEmpty => CreateSkillSlotSprite(filled: false),
            SkinSpriteKind.SkillSlotFilled => CreateSkillSlotSprite(filled: true),
            SkinSpriteKind.ActionBarFrame => CreateActionBarFrameSprite(),
            SkinSpriteKind.HealthBarBackground => CreateHealthBarBackgroundSprite(),
            SkinSpriteKind.HealthBarFill => CreateHealthBarFillSprite(),
            SkinSpriteKind.HeaderChip => CreateHeaderChipSprite(),
            SkinSpriteKind.LoadingBarBackground => CreateLoadingBarBackgroundSprite(),
            SkinSpriteKind.LoadingBarFill => CreateLoadingBarFillSprite(),
            SkinSpriteKind.IconPower => CreatePowerIconSprite(),
            SkinSpriteKind.IconWait => CreateWaitIconSprite(),
            _ => CreateGenericIconSprite()
        };

        SpriteCache[kind] = sprite;
        return sprite;
    }

    private static bool TryResolveBuiltInKind(string assetPath, out SkinSpriteKind kind)
    {
        switch (assetPath)
        {
            case HudPanelSpritePath:
                kind = SkinSpriteKind.HudPanel;
                return true;
            case PopupPanelSpritePath:
                kind = SkinSpriteKind.PopupPanel;
                return true;
            case CardPanelSpritePath:
                kind = SkinSpriteKind.CardPanel;
                return true;
            case WindowFrameSpritePath:
                kind = SkinSpriteKind.WindowFrame;
                return true;
            case ButtonSpritePath:
                kind = SkinSpriteKind.Button;
                return true;
            case SkillSlotEmptySpritePath:
                kind = SkinSpriteKind.SkillSlotEmpty;
                return true;
            case SkillSlotFilledSpritePath:
                kind = SkinSpriteKind.SkillSlotFilled;
                return true;
            case ActionBarFrameSpritePath:
                kind = SkinSpriteKind.ActionBarFrame;
                return true;
            case HealthBarEmptySpritePath:
                kind = SkinSpriteKind.HealthBarBackground;
                return true;
            case HealthBarFillSpritePath:
                kind = SkinSpriteKind.HealthBarFill;
                return true;
            case PowerIconSpritePath:
                kind = SkinSpriteKind.IconPower;
                return true;
            case WaitIconSpritePath:
                kind = SkinSpriteKind.IconWait;
                return true;
        }

        if (assetPath.IndexOf("power", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            kind = SkinSpriteKind.IconPower;
            return true;
        }

        if (assetPath.IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0 ||
            assetPath.IndexOf("undo", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            kind = SkinSpriteKind.IconWait;
            return true;
        }

        kind = SkinSpriteKind.IconGeneric;
        return false;
    }

    private static Sprite CreateHudPanelSprite()
    {
        return CreateFrameSprite(
            "HexTacticsHudPanel",
            96,
            56,
            6,
            2,
            0.10f,
            0.36f,
            0.68f,
            topBandHeight: 10,
            topBandBrightness: 0.90f,
            topBandAlpha: 0.08f,
            topHighlightHeight: 2,
            topHighlightAlpha: 0.14f,
            bottomShadowHeight: 6,
            bottomShadowAlpha: 0.22f,
            border: new Vector4(16f, 16f, 16f, 16f));
    }

    private static Sprite CreatePopupPanelSprite()
    {
        return CreateFrameSprite(
            "HexTacticsPopupPanel",
            88,
            72,
            6,
            2,
            0.12f,
            0.38f,
            0.74f,
            topBandHeight: 8,
            topBandBrightness: 0.94f,
            topBandAlpha: 0.10f,
            topHighlightHeight: 2,
            topHighlightAlpha: 0.16f,
            bottomShadowHeight: 5,
            bottomShadowAlpha: 0.16f,
            border: new Vector4(16f, 16f, 16f, 16f));
    }

    private static Sprite CreateCardPanelSprite()
    {
        return CreateFrameSprite(
            "HexTacticsCardPanel",
            80,
            52,
            5,
            2,
            0.12f,
            0.30f,
            0.56f,
            topBandHeight: 6,
            topBandBrightness: 0.84f,
            topBandAlpha: 0.08f,
            topHighlightHeight: 2,
            topHighlightAlpha: 0.12f,
            bottomShadowHeight: 4,
            bottomShadowAlpha: 0.18f,
            border: new Vector4(14f, 14f, 14f, 14f));
    }

    private static Sprite CreateWindowFrameSprite()
    {
        return CreateFrameSprite(
            "HexTacticsWindowFrame",
            88,
            72,
            6,
            3,
            0.08f,
            0.28f,
            0.78f,
            topBandHeight: 4,
            topBandBrightness: 0.96f,
            topBandAlpha: 0.04f,
            topHighlightHeight: 2,
            topHighlightAlpha: 0.12f,
            bottomShadowHeight: 5,
            bottomShadowAlpha: 0.20f,
            border: new Vector4(18f, 18f, 18f, 18f));
    }

    private static Sprite CreateButtonSprite()
    {
        return CreateFrameSprite(
            "HexTacticsButton",
            72,
            32,
            4,
            1,
            0.18f,
            0.42f,
            0.82f,
            topBandHeight: 5,
            topBandBrightness: 1f,
            topBandAlpha: 0.18f,
            topHighlightHeight: 2,
            topHighlightAlpha: 0.16f,
            bottomShadowHeight: 4,
            bottomShadowAlpha: 0.24f,
            border: new Vector4(10f, 10f, 10f, 10f));
    }

    private static Sprite CreateSkillSlotSprite(bool filled)
    {
        var pixels = CreatePixelBuffer(52, 52, Gray(0.74f));
        FillRect(pixels, 52, 52, 2, 2, 48, 48, Gray(0.34f));
        FillRect(pixels, 52, 52, 4, 4, 44, 44, Gray(filled ? 0.18f : 0.10f));
        BlendRect(pixels, 52, 52, 4, 40, 44, 4, WhiteAlpha(0.10f));
        BlendRect(pixels, 52, 52, 4, 4, 44, 4, BlackAlpha(0.18f));
        BlendRect(pixels, 52, 52, 6, 6, 4, 40, WhiteAlpha(filled ? 0.22f : 0.06f));
        BlendRect(pixels, 52, 52, 42, 6, 4, 40, BlackAlpha(0.08f));
        if (filled)
        {
            BlendRect(pixels, 52, 52, 10, 36, 26, 6, WhiteAlpha(0.08f));
        }

        return CreateSprite("HexTacticsSkillSlot" + (filled ? "Filled" : "Empty"), 52, 52, pixels, new Vector4(12f, 12f, 12f, 12f));
    }

    private static Sprite CreateActionBarFrameSprite()
    {
        var pixels = CreatePixelBuffer(84, 60, Gray(0.70f));
        FillRect(pixels, 84, 60, 2, 2, 80, 56, Gray(0.34f));
        FillRect(pixels, 84, 60, 5, 5, 74, 50, Gray(0.08f));
        BlendRect(pixels, 84, 60, 5, 47, 74, 6, WhiteAlpha(0.12f));
        BlendRect(pixels, 84, 60, 5, 5, 74, 5, BlackAlpha(0.20f));
        BlendRect(pixels, 84, 60, 10, 39, 64, 4, WhiteAlpha(0.08f));
        BlendRect(pixels, 84, 60, 8, 9, 4, 38, WhiteAlpha(0.08f));
        BlendRect(pixels, 84, 60, 72, 9, 4, 38, BlackAlpha(0.10f));
        return CreateSprite("HexTacticsActionBarFrame", 84, 60, pixels, new Vector4(18f, 18f, 18f, 18f));
    }

    private static Sprite CreateHealthBarBackgroundSprite()
    {
        return CreateFrameSprite(
            "HexTacticsHealthBarBackground",
            48,
            8,
            2,
            1,
            0.08f,
            0.24f,
            0.60f,
            topBandHeight: 1,
            topBandBrightness: 0.84f,
            topBandAlpha: 0.08f,
            topHighlightHeight: 1,
            topHighlightAlpha: 0.08f,
            bottomShadowHeight: 1,
            bottomShadowAlpha: 0.12f,
            border: new Vector4(3f, 3f, 3f, 3f));
    }

    private static Sprite CreateHealthBarFillSprite()
    {
        var pixels = CreatePixelBuffer(48, 8, Gray(0.80f));
        FillRect(pixels, 48, 8, 1, 1, 46, 6, Gray(0.58f));
        BlendRect(pixels, 48, 8, 1, 5, 46, 2, WhiteAlpha(0.18f));
        BlendRect(pixels, 48, 8, 1, 1, 46, 1, BlackAlpha(0.10f));
        return CreateSprite("HexTacticsHealthBarFill", 48, 8, pixels, new Vector4(2f, 2f, 2f, 2f));
    }

    private static Sprite CreateHeaderChipSprite()
    {
        var pixels = CreatePixelBuffer(76, 24, Gray(0.76f));
        FillRect(pixels, 76, 24, 2, 2, 72, 20, Gray(0.24f));
        BlendRect(pixels, 76, 24, 2, 16, 72, 5, WhiteAlpha(0.12f));
        BlendRect(pixels, 76, 24, 2, 2, 72, 3, BlackAlpha(0.16f));
        BlendRect(pixels, 76, 24, 5, 4, 5, 16, WhiteAlpha(0.10f));
        return CreateSprite("HexTacticsHeaderChip", 76, 24, pixels, new Vector4(8f, 8f, 8f, 8f));
    }

    private static Sprite CreateLoadingBarBackgroundSprite()
    {
        return CreateFrameSprite(
            "HexTacticsLoadingBarBackground",
            56,
            12,
            3,
            1,
            0.08f,
            0.26f,
            0.64f,
            topBandHeight: 2,
            topBandBrightness: 0.90f,
            topBandAlpha: 0.08f,
            topHighlightHeight: 1,
            topHighlightAlpha: 0.08f,
            bottomShadowHeight: 2,
            bottomShadowAlpha: 0.18f,
            border: new Vector4(4f, 4f, 4f, 4f));
    }

    private static Sprite CreateLoadingBarFillSprite()
    {
        var pixels = CreatePixelBuffer(56, 12, Gray(0.88f));
        FillRect(pixels, 56, 12, 2, 2, 52, 8, Gray(0.54f));
        BlendRect(pixels, 56, 12, 2, 7, 52, 3, WhiteAlpha(0.18f));
        BlendRect(pixels, 56, 12, 2, 2, 52, 2, BlackAlpha(0.12f));
        return CreateSprite("HexTacticsLoadingBarFill", 56, 12, pixels, new Vector4(4f, 4f, 4f, 4f));
    }

    private static Sprite CreateGenericIconSprite()
    {
        var pixels = CreatePixelBuffer(32, 32, new Color(0f, 0f, 0f, 0f));
        FillRect(pixels, 32, 32, 6, 6, 20, 20, WhiteAlpha(0.18f));
        FillRect(pixels, 32, 32, 10, 10, 12, 12, Gray(0.96f));
        FillRect(pixels, 32, 32, 14, 4, 4, 24, Gray(0.96f));
        FillRect(pixels, 32, 32, 4, 14, 24, 4, Gray(0.96f));
        return CreateSprite("HexTacticsIconGeneric", 32, 32, pixels, Vector4.zero);
    }

    private static Sprite CreatePowerIconSprite()
    {
        var pixels = CreatePixelBuffer(32, 32, new Color(0f, 0f, 0f, 0f));
        FillRect(pixels, 32, 32, 13, 5, 6, 12, Gray(0.98f));
        FillRect(pixels, 32, 32, 8, 15, 16, 5, Gray(0.98f));
        FillRect(pixels, 32, 32, 11, 20, 10, 7, Gray(0.98f));
        return CreateSprite("HexTacticsIconPower", 32, 32, pixels, Vector4.zero);
    }

    private static Sprite CreateWaitIconSprite()
    {
        var pixels = CreatePixelBuffer(32, 32, new Color(0f, 0f, 0f, 0f));
        FillRect(pixels, 32, 32, 7, 8, 18, 4, Gray(0.96f));
        FillRect(pixels, 32, 32, 7, 14, 12, 4, Gray(0.96f));
        FillRect(pixels, 32, 32, 7, 20, 8, 4, Gray(0.96f));
        FillRect(pixels, 32, 32, 20, 14, 5, 10, Gray(0.96f));
        return CreateSprite("HexTacticsIconWait", 32, 32, pixels, Vector4.zero);
    }

    private static Sprite CreateHashedIconSprite(string key)
    {
        var hash = key.GetHashCode();
        var pixels = CreatePixelBuffer(32, 32, new Color(0f, 0f, 0f, 0f));
        FillRect(pixels, 32, 32, 6, 6, 20, 20, WhiteAlpha(0.14f));

        for (var i = 0; i < 4; i++)
        {
            var shift = i * 4;
            var width = 4 + ((hash >> shift) & 0x3) * 3;
            var height = 4 + ((hash >> (shift + 2)) & 0x3) * 3;
            var x = 6 + ((hash >> (shift + 6)) & 0x3) * 4;
            var y = 6 + ((hash >> (shift + 8)) & 0x3) * 4;
            FillRect(pixels, 32, 32, x, y, width, height, Gray(0.96f));
        }

        return CreateSprite("HexTacticsIconHash" + Mathf.Abs(hash), 32, 32, pixels, Vector4.zero);
    }

    private static Sprite CreateFrameSprite(
        string name,
        int width,
        int height,
        int outerBorder,
        int innerBorder,
        float fillBrightness,
        float innerBrightness,
        float outerBrightness,
        int topBandHeight,
        float topBandBrightness,
        float topBandAlpha,
        int topHighlightHeight,
        float topHighlightAlpha,
        int bottomShadowHeight,
        float bottomShadowAlpha,
        Vector4 border)
    {
        var pixels = CreatePixelBuffer(width, height, Gray(outerBrightness));
        FillRect(pixels, width, height, outerBorder, outerBorder, width - outerBorder * 2, height - outerBorder * 2, Gray(innerBrightness));
        FillRect(
            pixels,
            width,
            height,
            outerBorder + innerBorder,
            outerBorder + innerBorder,
            width - (outerBorder + innerBorder) * 2,
            height - (outerBorder + innerBorder) * 2,
            Gray(fillBrightness));

        if (topBandHeight > 0)
        {
            BlendRect(
                pixels,
                width,
                height,
                outerBorder + innerBorder,
                height - outerBorder - innerBorder - topBandHeight,
                width - (outerBorder + innerBorder) * 2,
                topBandHeight,
                new Color(topBandBrightness, topBandBrightness, topBandBrightness, Mathf.Clamp01(topBandAlpha)));
        }

        if (topHighlightHeight > 0)
        {
            BlendRect(
                pixels,
                width,
                height,
                1,
                height - topHighlightHeight - 1,
                width - 2,
                topHighlightHeight,
                WhiteAlpha(topHighlightAlpha));
        }

        if (bottomShadowHeight > 0)
        {
            BlendRect(
                pixels,
                width,
                height,
                outerBorder,
                outerBorder,
                width - outerBorder * 2,
                bottomShadowHeight,
                BlackAlpha(bottomShadowAlpha));
        }

        return CreateSprite(name, width, height, pixels, border);
    }

    private static Color[] CreatePixelBuffer(int width, int height, Color baseColor)
    {
        var pixels = new Color[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = baseColor;
        }

        return pixels;
    }

    private static void FillRect(Color[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color color)
    {
        var xMin = Mathf.Clamp(x, 0, width);
        var yMin = Mathf.Clamp(y, 0, height);
        var xMax = Mathf.Clamp(x + rectWidth, 0, width);
        var yMax = Mathf.Clamp(y + rectHeight, 0, height);

        for (var py = yMin; py < yMax; py++)
        {
            var row = py * width;
            for (var px = xMin; px < xMax; px++)
            {
                pixels[row + px] = color;
            }
        }
    }

    private static void BlendRect(Color[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color overlay)
    {
        if (overlay.a <= 0f)
        {
            return;
        }

        var xMin = Mathf.Clamp(x, 0, width);
        var yMin = Mathf.Clamp(y, 0, height);
        var xMax = Mathf.Clamp(x + rectWidth, 0, width);
        var yMax = Mathf.Clamp(y + rectHeight, 0, height);

        for (var py = yMin; py < yMax; py++)
        {
            var row = py * width;
            for (var px = xMin; px < xMax; px++)
            {
                var index = row + px;
                pixels[index] = BlendOver(pixels[index], overlay);
            }
        }
    }

    private static Sprite CreateSprite(string name, int width, int height, Color[] pixels, Vector4 border)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: true)
        {
            name = name + "_Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        texture.SetPixels(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            border);

        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Color BlendOver(Color destination, Color source)
    {
        var inverseAlpha = 1f - source.a;
        return new Color(
            source.r * source.a + destination.r * inverseAlpha,
            source.g * source.a + destination.g * inverseAlpha,
            source.b * source.a + destination.b * inverseAlpha,
            source.a + destination.a * inverseAlpha);
    }

    private static Color Gray(float brightness)
    {
        var clamped = Mathf.Clamp01(brightness);
        return new Color(clamped, clamped, clamped, 1f);
    }

    private static Color WhiteAlpha(float alpha)
    {
        return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private static Color BlackAlpha(float alpha)
    {
        return new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
    }
}
