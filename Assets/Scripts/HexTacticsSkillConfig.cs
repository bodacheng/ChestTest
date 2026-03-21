using UnityEngine;

[CreateAssetMenu(menuName = "Hex Tactics/Skill Config", fileName = "SkillConfig")]
public sealed class HexTacticsSkillConfig : ScriptableObject
{
    [SerializeField] private string displayName = "通常攻撃";
    [SerializeField, TextArea(2, 4)] private string description = "エネルギーを溜める基本技";
    [SerializeField, Min(1)] private int power = 3;
    [SerializeField, Min(0)] private int attackRange = 0;
    [SerializeField, Min(0)] private int energyCost = 0;
    [SerializeField, Min(0)] private int energyGainOnHit = 1;
    [Header("Effects")]
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField, Min(0.1f)] private float projectileEffectScale = 1f;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField, Min(0.1f)] private float impactEffectScale = 1f;
    [SerializeField, Range(0.2f, 1.4f)] private float impactHeightNormalized = 0.58f;
    [SerializeField, Min(0f)] private float impactForwardOffset = 0.08f;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "通常攻撃" : displayName;
    public string Description => description ?? string.Empty;
    public int Power => Mathf.Max(1, power);
    public int AttackRange => Mathf.Max(0, attackRange);
    public int EnergyCost => Mathf.Max(0, energyCost);
    public int EnergyGainOnHit => Mathf.Max(0, energyGainOnHit);
    public int AttackReach => AttackRange + 1;
    public bool IsEnergyConsuming => EnergyCost > 0;
    public GameObject ProjectileEffectPrefab => projectileEffectPrefab;
    public float ProjectileEffectScale => Mathf.Max(0.1f, projectileEffectScale);
    public GameObject ImpactEffectPrefab => impactEffectPrefab;
    public float ImpactEffectScale => Mathf.Max(0.1f, impactEffectScale);
    public float ImpactHeightNormalized => Mathf.Clamp(impactHeightNormalized, 0.2f, 1.4f);
    public float ImpactForwardOffset => Mathf.Max(0f, impactForwardOffset);
    public bool HasProjectileEffect => projectileEffectPrefab != null;
    public bool HasImpactEffect => impactEffectPrefab != null;
    public bool RequiresDedicatedRangedEffects => AttackReach >= 2;

    public void ConfigureRuntime(
        string newDisplayName,
        string newDescription,
        int newPower,
        int newAttackRange,
        int newEnergyCost,
        int newEnergyGainOnHit)
    {
        displayName = newDisplayName;
        description = newDescription;
        power = Mathf.Max(1, newPower);
        attackRange = Mathf.Max(0, newAttackRange);
        energyCost = Mathf.Max(0, newEnergyCost);
        energyGainOnHit = Mathf.Max(0, newEnergyGainOnHit);
        projectileEffectPrefab = null;
        projectileEffectScale = 1f;
        impactEffectPrefab = null;
        impactEffectScale = 1f;
        impactHeightNormalized = 0.58f;
        impactForwardOffset = 0.08f;
    }
}
