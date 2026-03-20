using UnityEngine;

[CreateAssetMenu(menuName = "Hex Tactics/Character Config", fileName = "CharacterConfig")]
public sealed class HexTacticsCharacterConfig : ScriptableObject
{
    [SerializeField] private string displayName = "角色";
    [SerializeField, TextArea(2, 4)] private string description = "近战";
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(1)] private int attackPower = 3;
    [SerializeField, Min(1)] private int cost = 3;
    [SerializeField, Min(1)] private int moveRange = 2;
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

    public string DisplayName => displayName;
    public string Description => description;
    public int MaxHealth => maxHealth;
    public int AttackPower => attackPower;
    public int Cost => cost;
    public int MoveRange => moveRange;
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
        HexTacticsCharacterVisualArchetype newVisualArchetype)
    {
        displayName = newDisplayName;
        description = newDescription;
        maxHealth = newMaxHealth;
        attackPower = newAttackPower;
        cost = newCost;
        moveRange = newMoveRange;
        visualArchetype = newVisualArchetype;
        avatar = null;
        battleUnitPrefab = null;
        visualHeightScale = 1f;
        attackImpactFrame = 0;
        attackImpactNormalizedTime = 0.45f;
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
