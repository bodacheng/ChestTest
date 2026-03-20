using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Hex Tactics/Hit Effect Catalog", fileName = "HexTacticsHitEffectCatalog")]
public sealed class HexTacticsHitEffectCatalog : ScriptableObject
{
    [SerializeField] private List<HexTacticsHitEffectEntry> hitEffects = new();

    public IReadOnlyList<HexTacticsHitEffectEntry> HitEffects => hitEffects;

    public bool TryResolveAutoEffect(int attackPower, int cycleIndex, out HexTacticsHitEffectEntry entry)
    {
        var preferredStyle = ResolvePreferredStyle(attackPower);
        return TryResolveEffect(preferredStyle, autoSelectOnly: true, cycleIndex, out entry) ||
               TryResolveEffect(preferredStyle, autoSelectOnly: false, cycleIndex, out entry) ||
               TryResolveEffect(HexTacticsHitEffectStyle.Medium, autoSelectOnly: true, cycleIndex, out entry) ||
               TryResolveEffect(HexTacticsHitEffectStyle.Light, autoSelectOnly: true, cycleIndex, out entry) ||
               TryResolveEffect(HexTacticsHitEffectStyle.Heavy, autoSelectOnly: true, cycleIndex, out entry) ||
               TryResolveEffect(HexTacticsHitEffectStyle.Medium, autoSelectOnly: false, cycleIndex, out entry) ||
               TryResolveEffect(HexTacticsHitEffectStyle.Light, autoSelectOnly: false, cycleIndex, out entry) ||
               TryResolveEffect(HexTacticsHitEffectStyle.Heavy, autoSelectOnly: false, cycleIndex, out entry);
    }

    public void ReplaceEntries(List<HexTacticsHitEffectEntry> entries)
    {
        hitEffects = entries ?? new List<HexTacticsHitEffectEntry>();
    }

    public static HexTacticsHitEffectCatalog LoadDefault()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            return AssetDatabase.LoadAssetAtPath<HexTacticsHitEffectCatalog>(HexTacticsAssetPaths.HitEffectCatalogAssetPath);
        }
#endif
        return HexTacticsAddressables.LoadAsset<HexTacticsHitEffectCatalog>(HexTacticsAssetPaths.HitEffectCatalogAddress);
    }

    private bool TryResolveEffect(
        HexTacticsHitEffectStyle style,
        bool autoSelectOnly,
        int cycleIndex,
        out HexTacticsHitEffectEntry entry)
    {
        entry = null;
        var matchCount = 0;
        if (hitEffects == null)
        {
            return false;
        }

        for (var i = 0; i < hitEffects.Count; i++)
        {
            var candidate = hitEffects[i];
            if (!IsEffectMatch(candidate, style, autoSelectOnly))
            {
                continue;
            }

            matchCount++;
        }

        if (matchCount == 0)
        {
            return false;
        }

        var targetIndex = Mathf.Abs(cycleIndex) % matchCount;
        var currentIndex = 0;
        for (var i = 0; i < hitEffects.Count; i++)
        {
            var candidate = hitEffects[i];
            if (!IsEffectMatch(candidate, style, autoSelectOnly))
            {
                continue;
            }

            if (currentIndex == targetIndex)
            {
                entry = candidate;
                return true;
            }

            currentIndex++;
        }

        return false;
    }

    private static bool IsEffectMatch(HexTacticsHitEffectEntry candidate, HexTacticsHitEffectStyle style, bool autoSelectOnly)
    {
        return candidate != null &&
               candidate.Prefab != null &&
               candidate.Style == style &&
               (!autoSelectOnly || candidate.AutoSelect);
    }

    private static HexTacticsHitEffectStyle ResolvePreferredStyle(int attackPower)
    {
        if (attackPower <= 2)
        {
            return HexTacticsHitEffectStyle.Light;
        }

        if (attackPower <= 4)
        {
            return HexTacticsHitEffectStyle.Medium;
        }

        return HexTacticsHitEffectStyle.Heavy;
    }
}

[Serializable]
public sealed class HexTacticsHitEffectEntry
{
    [SerializeField] private string id = string.Empty;
    [SerializeField] private string displayName = string.Empty;
    [SerializeField] private string sourceAssetPath = string.Empty;
    [SerializeField] private GameObject prefab;
    [SerializeField] private HexTacticsHitEffectStyle style = HexTacticsHitEffectStyle.Medium;
    [SerializeField] private bool autoSelect = false;
    [SerializeField, Range(0.2f, 1.4f)] private float heightNormalized = 0.58f;
    [SerializeField, Min(0f)] private float forwardOffset = 0.08f;
    [SerializeField, Min(0.1f)] private float scale = 1f;

    public HexTacticsHitEffectEntry(
        string id,
        string displayName,
        string sourceAssetPath,
        GameObject prefab,
        HexTacticsHitEffectStyle style,
        bool autoSelect,
        float heightNormalized,
        float forwardOffset,
        float scale)
    {
        this.id = id;
        this.displayName = displayName;
        this.sourceAssetPath = sourceAssetPath;
        this.prefab = prefab;
        this.style = style;
        this.autoSelect = autoSelect;
        this.heightNormalized = heightNormalized;
        this.forwardOffset = forwardOffset;
        this.scale = scale;
    }

    public string Id => id;
    public string DisplayName => displayName;
    public string SourceAssetPath => sourceAssetPath;
    public GameObject Prefab => prefab;
    public HexTacticsHitEffectStyle Style => style;
    public bool AutoSelect => autoSelect;
    public float HeightNormalized => heightNormalized;
    public float ForwardOffset => forwardOffset;
    public float Scale => scale;
}

public enum HexTacticsHitEffectStyle
{
    Light,
    Medium,
    Heavy
}
