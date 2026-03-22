using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex Tactics/Character Config", fileName = "CharacterConfig")]
public sealed class HexTacticsCharacterConfig : ScriptableObject
{
    [SerializeField] private string displayName = "角色";
    [SerializeField, TextArea(2, 4)] private string description = "近战";
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(1)] private int cost = 3;
    [SerializeField, Min(1)] private int moveRange = 1;
    [SerializeField, Min(1)] private int speed = 3;
    [Header("Energy")]
    [SerializeField, Min(1)] private int maxEnergy = 3;
    [SerializeField, Min(0)] private int startingEnergy = 0;
    [Header("Skills")]
    [SerializeField] private List<HexTacticsSkillConfig> skills = new();
    [SerializeField] private HexTacticsCharacterVisualArchetype visualArchetype = HexTacticsCharacterVisualArchetype.Stag;
    [SerializeField] private Sprite avatar;
    [Header("Battle Visual")]
    [SerializeField] private GameObject battleUnitPrefab;
    [SerializeField, Min(0.4f)] private float visualHeightScale = 1f;
    [Header("Attack Sync")]
    [SerializeField, Min(0), Tooltip("1-based attack impact frame. Set to 0 to use normalized impact time instead.")]
    private int attackImpactFrame = 0;
    [SerializeField, Range(0.05f, 0.95f), Tooltip("Fallback impact point when no frame or animation event is configured.")]
    private float attackImpactNormalizedTime = 0.45f;
    [Header("Legacy Skill Fallback")]
    [SerializeField, HideInInspector, Min(1)] private int attackPower = 3;
    [SerializeField, HideInInspector, Min(0)] private int attackRange = 0;

    [System.NonSerialized] private HexTacticsSkillConfig runtimeFallbackSkill;
    [System.NonSerialized] private List<HexTacticsSkillConfig> resolvedSkillsBuffer;
    [System.NonSerialized] private List<HexTacticsSkillConfig> fallbackSkillBuffer;

    public string DisplayName => displayName;
    public string Description => description;
    public int MaxHealth => maxHealth;
    public int Cost => cost;
    public int MoveRange => moveRange;
    public int Speed => speed;
    public int MaxEnergy => Mathf.Max(1, maxEnergy);
    public int StartingEnergy => Mathf.Clamp(startingEnergy, 0, MaxEnergy);
    public IReadOnlyList<HexTacticsSkillConfig> Skills => ResolveSkills();
    public HexTacticsSkillConfig PrimarySkill => ResolvePrimarySkill();
    public bool HasAssignedSkills => CountAssignedSkills() > 0;
    public int SkillCount => ResolveSkills().Count;
    public int PreviewAttackPower => PrimarySkill != null ? PrimarySkill.Power : Mathf.Max(1, attackPower);
    public int PreviewAttackRange => PrimarySkill != null ? PrimarySkill.AttackRange : Mathf.Max(0, attackRange);
    public int LegacyAttackPowerForMigration => Mathf.Max(1, attackPower);
    public int LegacyAttackRangeForMigration => Mathf.Max(0, attackRange);
    public HexTacticsCharacterVisualArchetype VisualArchetype => visualArchetype;
    public Sprite Avatar => avatar;
    public GameObject BattleUnitPrefab => battleUnitPrefab;
    public float VisualHeightScale => visualHeightScale;
    public int AttackImpactFrame => attackImpactFrame;
    public float AttackImpactNormalizedTime => attackImpactNormalizedTime;

    public void ConfigureRuntime(
        string newDisplayName,
        string newDescription,
        int newMaxHealth,
        int newAttackPower,
        int newCost,
        int newMoveRange,
        int newAttackRange,
        int newSpeed,
        HexTacticsCharacterVisualArchetype newVisualArchetype)
    {
        displayName = newDisplayName;
        description = newDescription;
        maxHealth = newMaxHealth;
        cost = newCost;
        moveRange = newMoveRange;
        speed = newSpeed;
        maxEnergy = 3;
        startingEnergy = 0;
        attackPower = Mathf.Max(1, newAttackPower);
        attackRange = Mathf.Max(0, newAttackRange);
        if (skills == null)
        {
            skills = new List<HexTacticsSkillConfig>();
        }
        else
        {
            skills.Clear();
        }

        runtimeFallbackSkill = null;
        resolvedSkillsBuffer = null;
        fallbackSkillBuffer = null;
        visualArchetype = newVisualArchetype;
        avatar = null;
        battleUnitPrefab = null;
        visualHeightScale = 1f;
        attackImpactFrame = 0;
        attackImpactNormalizedTime = 0.45f;
    }

    public void ConfigureRuntimeVisual(GameObject newBattleUnitPrefab, float newVisualHeightScale, Sprite newAvatar = null)
    {
        battleUnitPrefab = newBattleUnitPrefab;
        visualHeightScale = Mathf.Max(0.4f, newVisualHeightScale);
        avatar = newAvatar;
    }

    public float ResolveAttackImpactNormalizedTime(AnimationClip attackClip)
    {
        if (attackImpactFrame > 0 && attackClip != null && attackClip.frameRate > 0.01f && attackClip.length > 0.01f)
        {
            var totalFrames = Mathf.Max(1f, attackClip.frameRate * attackClip.length);
            return Mathf.Clamp01((attackImpactFrame - 1f) / totalFrames);
        }

        return Mathf.Clamp01(attackImpactNormalizedTime);
    }

    private HexTacticsSkillConfig ResolvePrimarySkill()
    {
        var resolvedSkills = ResolveSkills();
        return resolvedSkills.Count > 0 ? resolvedSkills[0] : null;
    }

    private IReadOnlyList<HexTacticsSkillConfig> ResolveSkills()
    {
        resolvedSkillsBuffer ??= new List<HexTacticsSkillConfig>();
        resolvedSkillsBuffer.Clear();

        if (skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null)
                {
                    resolvedSkillsBuffer.Add(skill);
                }
            }
        }

        if (resolvedSkillsBuffer.Count > 0)
        {
            return resolvedSkillsBuffer;
        }

        fallbackSkillBuffer ??= new List<HexTacticsSkillConfig>(1);
        fallbackSkillBuffer.Clear();
        fallbackSkillBuffer.Add(ResolveLegacyFallbackSkill());
        return fallbackSkillBuffer;
    }

    private HexTacticsSkillConfig ResolveLegacyFallbackSkill()
    {
        if (runtimeFallbackSkill == null)
        {
            runtimeFallbackSkill = CreateInstance<HexTacticsSkillConfig>();
            runtimeFallbackSkill.hideFlags = HideFlags.HideAndDontSave;
        }

        var fallbackName = "基本技";
        var fallbackDescription = string.IsNullOrWhiteSpace(description)
            ? "既存キャラデータから自動補完された基本技"
            : description;
        runtimeFallbackSkill.ConfigureRuntime(
            fallbackName,
            fallbackDescription,
            Mathf.Max(1, attackPower),
            Mathf.Max(0, attackRange),
            0,
            1);
        return runtimeFallbackSkill;
    }

    private int CountAssignedSkills()
    {
        if (skills == null)
        {
            return 0;
        }

        var count = 0;
        foreach (var skill in skills)
        {
            if (skill != null)
            {
                count++;
            }
        }

        return count;
    }
}

public enum HexTacticsCharacterVisualArchetype
{
    Stag,
    Doe,
    Elk,
    Fawn,
    Tiger,
    WhiteTiger
}
