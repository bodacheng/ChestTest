using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class HexTacticsModernUiSkin
{
    public const string HudPanelSpritePath = "Assets/Modern & Clean GUI/HUD/HUD_Menu_Extended_Rounded.png";
    public const string PopupPanelSpritePath = "Assets/Modern & Clean GUI/HUD/HUD_Announcement_Rounded.png";
    public const string CardPanelSpritePath = "Assets/Modern & Clean GUI/Menus & Windows/Window_SubHeading_Rounded.png";
    public const string WindowFrameSpritePath = "Assets/Modern & Clean GUI/Menus & Windows/Window_Frame_Rounded.png";
    public const string ButtonSpritePath = "Assets/Modern & Clean GUI/Buttons/Button_Main_NoHover_Rounded.png";
    public const string SkillSlotEmptySpritePath = "Assets/Modern & Clean GUI/HUD/HUD_ActionBar_Slot_Empty_Rounded.png";
    public const string SkillSlotFilledSpritePath = "Assets/Modern & Clean GUI/HUD/HUD_ActionBar_Slot_Filled_Rounded.png";
    public const string ActionBarFrameSpritePath = "Assets/Modern & Clean GUI/HUD/HUD_ActionBar_Frame_Rounded.png";
    public const string HealthBarEmptySpritePath = "Assets/Modern & Clean GUI/HUD/HUD_Target_HealthBar_Empty_Rounded.png";
    public const string HealthBarFillSpritePath = "Assets/Modern & Clean GUI/HUD/HUD_Target_HealthBar_Fill_Rounded.png";
    public const string PowerIconSpritePath = "Assets/Modern & Clean GUI/Icons/Rounded/64x64/Icons_Power_Rounded.png";
    public const string WaitIconSpritePath = "Assets/Modern & Clean GUI/Icons/Rounded/64x64/Icons_Undo_Rounded.png";

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    public static readonly string[] RequiredSpriteAssetPaths =
    {
        HudPanelSpritePath,
        PopupPanelSpritePath,
        CardPanelSpritePath,
        WindowFrameSpritePath,
        ButtonSpritePath,
        SkillSlotEmptySpritePath,
        SkillSlotFilledSpritePath,
        ActionBarFrameSpritePath,
        HealthBarEmptySpritePath,
        HealthBarFillSpritePath,
        PowerIconSpritePath,
        WaitIconSpritePath
    };

    public static void ApplyHudPanel(Image image, Color? tint = null)
    {
        ApplySprite(image, HudPanelSpritePath, tint ?? new Color(1f, 1f, 1f, 0.96f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyPopupPanel(Image image, Color? tint = null)
    {
        ApplySprite(image, PopupPanelSpritePath, tint ?? new Color(1f, 1f, 1f, 0.98f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyCardPanel(Image image, Color? tint = null)
    {
        ApplySprite(image, CardPanelSpritePath, tint ?? new Color(1f, 1f, 1f, 0.94f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyWindowFrame(Image image, Color? tint = null)
    {
        ApplySprite(image, WindowFrameSpritePath, tint ?? new Color(1f, 1f, 1f, 0.96f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyButton(Image image, Color tint)
    {
        ApplySprite(image, ButtonSpritePath, tint, preserveAspect: false, preferSliced: true);
    }

    public static void ApplySkillSlot(Image image, bool filled, Color tint)
    {
        ApplySprite(image, filled ? SkillSlotFilledSpritePath : SkillSlotEmptySpritePath, tint, preserveAspect: false, preferSliced: true);
    }

    public static void ApplyActionBarFrame(Image image, Color? tint = null)
    {
        ApplySprite(image, ActionBarFrameSpritePath, tint ?? new Color(1f, 1f, 1f, 0.94f), preserveAspect: false, preferSliced: true);
    }

    public static void ApplyHealthBarBackground(Image image)
    {
        ApplySprite(image, HealthBarEmptySpritePath, Color.white, preserveAspect: false, preferSliced: true);
    }

    public static void ApplyHealthBarFill(Image image)
    {
        ApplySprite(image, HealthBarFillSpritePath, Color.white, preserveAspect: false, preferSliced: true);
    }

    public static void ApplyIcon(Image image, string assetPath, Color? tint = null)
    {
        ApplySprite(image, assetPath, tint ?? Color.white, preserveAspect: true, preferSliced: false);
    }

    public static Sprite LoadSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        if (SpriteCache.TryGetValue(assetPath, out var cachedSprite))
        {
            return cachedSprite;
        }

        var sprite = HexTacticsAddressables.LoadAsset<Sprite>(assetPath);
        SpriteCache[assetPath] = sprite;
        return sprite;
    }

    private static void ApplySprite(Image image, string assetPath, Color tint, bool preserveAspect, bool preferSliced)
    {
        if (image == null)
        {
            return;
        }

        var sprite = LoadSprite(assetPath);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = preferSliced && sprite.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.preserveAspect = preserveAspect;
        }

        image.color = tint;
    }
}
