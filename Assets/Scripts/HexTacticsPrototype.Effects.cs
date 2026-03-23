using System.Collections;
using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    private const float SkillImpactAccentScaleMin = 0.42f;
    private const float SkillImpactAccentScaleMax = 0.82f;
    private const float DefeatImpactHeightNormalized = 0.56f;
    private const float DefeatImpactForwardOffsetNormalized = 0.04f;
    private const float DefeatImpactNovaHeightNormalized = 0.34f;
    private const float DefeatImpactEchoHeightOffset = 0.06f;

    private void SpawnAttackReleaseEffect(HexUnit attacker, HexUnit defender, float travelDuration, HexTacticsSkillConfig skill)
    {
        if (!enableHitEffects || !UsesRangedAttackPresentation(skill) || effectsRoot == null || attacker?.Transform == null || defender?.Transform == null)
        {
            return;
        }

        var releaseEffectPrefab = ResolveProjectileEffectPrefab(skill);
        if (releaseEffectPrefab == null)
        {
            return;
        }

        var startPosition = ResolveRangedWaveStartPosition(attacker, defender);
        var endPosition = ResolveRangedWaveEndPosition(attacker, defender);
        var effectRotation = ResolveHitEffectRotation(attacker, defender);
        var effectInstance = Instantiate(releaseEffectPrefab, startPosition, effectRotation, effectsRoot);
        effectInstance.name = releaseEffectPrefab.name;
        effectInstance.transform.localScale *= ResolveRangedWaveScale(attacker, defender) * ResolveProjectileEffectScale(skill);

        var travelEffect = effectInstance.GetComponent<HexTacticsTravelingEffect>();
        if (travelEffect == null)
        {
            travelEffect = effectInstance.AddComponent<HexTacticsTravelingEffect>();
        }

        if (effectInstance.GetComponent<HexTacticsTransientEffect>() == null)
        {
            effectInstance.AddComponent<HexTacticsTransientEffect>();
        }

        travelEffect.Initialize(startPosition, endPosition, Mathf.Max(0.06f, travelDuration));
    }

    private void SpawnHitEffect(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        if (!enableHitEffects || skill == null || effectsRoot == null || attacker?.Transform == null || defender?.Transform == null)
        {
            return;
        }

        var effectRotation = ResolveHitEffectRotation(attacker, defender);
        if (skill.ImpactEffectPrefab != null)
        {
            var effectPosition = ResolveConfiguredHitEffectPosition(attacker, defender, skill);
            SpawnTransientEffect(skill.ImpactEffectPrefab, effectPosition, effectRotation, skill.ImpactEffectScale, skill.ImpactEffectPrefab.name);
            TrySpawnDedicatedSkillHitAccent(attacker, defender, skill, effectPosition, effectRotation);

            return;
        }

        if (!TryEnsureHitEffectCatalogLoaded())
        {
            return;
        }

        if (!hitEffectCatalog.TryResolveAutoEffect(skill.Power, nextHitEffectVariantIndex++, out var entry) ||
            entry?.Prefab == null)
        {
            return;
        }

        var autoEffectPosition = ResolveHitEffectPosition(attacker, defender, entry);
        SpawnTransientEffect(entry.Prefab, autoEffectPosition, effectRotation, Mathf.Max(0.1f, entry.Scale), entry.DisplayName);
    }

    private bool TryEnsureHitEffectCatalogLoaded()
    {
        if (hitEffectCatalog != null)
        {
            return true;
        }

        hitEffectCatalog = HexTacticsHitEffectCatalog.LoadDefault();
        return hitEffectCatalog != null;
    }

    private bool TryEnsureRangedWaveEffectLoaded()
    {
        if (rangedWaveEffectPrefab != null)
        {
            return true;
        }

        rangedWaveEffectPrefab = HexTacticsAddressables.LoadAsset<GameObject>(HexTacticsAssetPaths.RangedWaveEffectAddress);
        return rangedWaveEffectPrefab != null;
    }

    private bool TryEnsureDefeatImpactEffectsLoaded()
    {
        var hasPrimary = defeatImpactPrimaryPrefab != null;
        var hasSecondary = defeatImpactSecondaryPrefab != null;
        var hasTertiary = defeatImpactTertiaryPrefab != null;
        if (hasPrimary || hasSecondary || hasTertiary)
        {
            return true;
        }

        defeatImpactPrimaryPrefab = HexTacticsAddressables.LoadAsset<GameObject>(HexTacticsAssetPaths.DefeatImpactPrimaryAddress);
        defeatImpactSecondaryPrefab = HexTacticsAddressables.LoadAsset<GameObject>(HexTacticsAssetPaths.DefeatImpactSecondaryAddress);
        defeatImpactTertiaryPrefab = HexTacticsAddressables.LoadAsset<GameObject>(HexTacticsAssetPaths.DefeatImpactTertiaryAddress);
        return defeatImpactPrimaryPrefab != null || defeatImpactSecondaryPrefab != null || defeatImpactTertiaryPrefab != null;
    }

    private GameObject ResolveProjectileEffectPrefab(HexTacticsSkillConfig skill)
    {
        if (skill?.ProjectileEffectPrefab != null)
        {
            return skill.ProjectileEffectPrefab;
        }

        if (!TryEnsureRangedWaveEffectLoaded())
        {
            return null;
        }

        return rangedWaveEffectPrefab;
    }

    private static float ResolveProjectileEffectScale(HexTacticsSkillConfig skill)
    {
        return skill != null ? skill.ProjectileEffectScale : 1f;
    }

    private void SpawnDefeatHitEffect(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        if (!enableHitEffects || effectsRoot == null || attacker?.Transform == null || defender?.Transform == null)
        {
            return;
        }

        var effectRotation = ResolveHitEffectRotation(attacker, defender);
        var effectPosition = ResolveDefeatHitEffectPosition(attacker, defender);
        var primaryScale = ResolveDefeatImpactScale(defender, skill, 1f);
        var secondaryScale = ResolveDefeatImpactScale(defender, skill, 0.94f);
        var novaScale = ResolveDefeatImpactScale(defender, skill, 1.55f);

        if (TryEnsureDefeatImpactEffectsLoaded())
        {
            if (defeatImpactPrimaryPrefab != null)
            {
                SpawnTransientEffect(defeatImpactPrimaryPrefab, effectPosition, effectRotation, primaryScale, defeatImpactPrimaryPrefab.name + "_Defeat");
            }

            if (defeatImpactSecondaryPrefab != null)
            {
                var secondaryRotation = effectRotation * Quaternion.Euler(0f, 28f, 0f);
                var secondaryPosition = effectPosition + Vector3.up * Mathf.Max(0.06f, defender.VisualHeight * 0.05f);
                SpawnTransientEffect(defeatImpactSecondaryPrefab, secondaryPosition, secondaryRotation, secondaryScale, defeatImpactSecondaryPrefab.name + "_Defeat");
            }

            if (defeatImpactTertiaryPrefab != null)
            {
                var novaPosition = ResolveDefeatNovaEffectPosition(attacker, defender);
                SpawnTransientEffect(defeatImpactTertiaryPrefab, novaPosition, effectRotation, novaScale, defeatImpactTertiaryPrefab.name + "_Defeat");
                StartCoroutine(SpawnDelayedDefeatImpactEcho(defeatImpactTertiaryPrefab, novaPosition, effectRotation, novaScale * 0.78f));
            }

            return;
        }

        if (!TryEnsureHitEffectCatalogLoaded())
        {
            return;
        }

        if (!hitEffectCatalog.TryResolveAutoEffect(Mathf.Max(6, skill != null ? skill.Power + 4 : 6), nextHitEffectVariantIndex++, out var entry) ||
            entry?.Prefab == null)
        {
            return;
        }

        SpawnTransientEffect(entry.Prefab, effectPosition, effectRotation, Mathf.Max(primaryScale, entry.Scale * 1.35f), entry.DisplayName + "_Defeat");
    }

    private IEnumerator SpawnDelayedDefeatImpactEcho(GameObject effectPrefab, Vector3 position, Quaternion rotation, float scale)
    {
        if (effectPrefab == null || defeatImpactEchoDelay <= 0.001f)
        {
            yield break;
        }

        yield return new WaitForSeconds(defeatImpactEchoDelay);
        var echoPosition = position + Vector3.up * DefeatImpactEchoHeightOffset;
        var echoRotation = rotation * Quaternion.Euler(0f, 14f, 0f);
        SpawnTransientEffect(effectPrefab, echoPosition, echoRotation, Mathf.Max(0.1f, scale), effectPrefab.name + "_DefeatEcho");
    }

    private Vector3 ResolveHitEffectPosition(HexUnit attacker, HexUnit defender, HexTacticsHitEffectEntry entry)
    {
        var direction = defender.Transform.position - attacker.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = defender.Transform.forward;
        }

        direction.Normalize();
        var heightFactor = Mathf.Max(0.2f, entry.HeightNormalized);
        var height = Mathf.Max(unitHoverHeight * 0.85f, defender.VisualHeight * Mathf.Lerp(hitEffectHeightNormalized, heightFactor, 0.75f));
        var forwardOffset = defender.SelectionRadius * Mathf.Max(hitEffectForwardOffset, entry.ForwardOffset);
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveConfiguredHitEffectPosition(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        var direction = defender.Transform.position - attacker.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = defender.Transform.forward;
        }

        direction.Normalize();
        var height = Mathf.Max(unitHoverHeight * 0.85f, defender.VisualHeight * skill.ImpactHeightNormalized);
        var forwardOffset = defender.SelectionRadius * Mathf.Max(hitEffectForwardOffset, skill.ImpactForwardOffset);
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveDefeatHitEffectPosition(HexUnit attacker, HexUnit defender)
    {
        var direction = defender.Transform.position - attacker.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = defender.Transform.forward;
        }

        direction.Normalize();
        var height = Mathf.Max(unitHoverHeight, defender.VisualHeight * DefeatImpactHeightNormalized);
        var forwardOffset = defender.SelectionRadius * DefeatImpactForwardOffsetNormalized;
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveDefeatNovaEffectPosition(HexUnit attacker, HexUnit defender)
    {
        var direction = defender.Transform.position - attacker.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = defender.Transform.forward;
        }

        direction.Normalize();
        var height = Mathf.Max(unitHoverHeight * 0.7f, defender.VisualHeight * DefeatImpactNovaHeightNormalized);
        var forwardOffset = defender.SelectionRadius * 0.02f;
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveRangedWaveStartPosition(HexUnit attacker, HexUnit defender)
    {
        var direction = defender.Transform.position - attacker.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = attacker.Transform.forward;
        }

        direction.Normalize();
        var height = Mathf.Max(unitHoverHeight * 0.8f, attacker.VisualHeight * 0.48f);
        var forwardOffset = Mathf.Max(hexRadius * 0.18f, attacker.SelectionRadius * 0.42f);
        return attacker.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveRangedWaveEndPosition(HexUnit attacker, HexUnit defender)
    {
        var direction = defender.Transform.position - attacker.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();
        var height = Mathf.Max(unitHoverHeight * 0.8f, defender.VisualHeight * 0.5f);
        var backwardOffset = Mathf.Max(hexRadius * 0.12f, defender.SelectionRadius * 0.18f);
        return defender.Transform.position + Vector3.up * height - direction * backwardOffset;
    }

    private float ResolveRangedWaveScale(HexUnit attacker, HexUnit defender)
    {
        var distance = Vector3.Distance(attacker.Transform.position, defender.Transform.position);
        return Mathf.Clamp(0.8f + distance * 0.08f, 0.85f, 1.18f);
    }

    private static bool UsesRangedAttackPresentation(HexTacticsSkillConfig skill)
    {
        return skill != null && skill.AttackRange > 0;
    }

    private static Quaternion ResolveHitEffectRotation(HexUnit attacker, HexUnit defender)
    {
        var direction = defender.Transform.position - attacker.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void TrySpawnDedicatedSkillHitAccent(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        Vector3 configuredEffectPosition,
        Quaternion effectRotation)
    {
        if (!ShouldSpawnDedicatedSkillHitAccent(skill) || !TryEnsureHitEffectCatalogLoaded())
        {
            return;
        }

        var accentPower = skill.Power + skill.EnergyCost + (skill.CollisionAttribute != HexTacticsCollisionAttribute.None ? 1 : 0);
        if (!hitEffectCatalog.TryResolveAutoEffect(accentPower, nextHitEffectVariantIndex++, out var entry) ||
            entry?.Prefab == null)
        {
            return;
        }

        var catalogEffectPosition = ResolveHitEffectPosition(attacker, defender, entry);
        var accentPosition = Vector3.Lerp(configuredEffectPosition, catalogEffectPosition, 0.4f);
        var accentScale = ResolveDedicatedSkillHitAccentScale(skill, entry);
        SpawnTransientEffect(entry.Prefab, accentPosition, effectRotation, accentScale, entry.DisplayName + "_Accent");
    }

    private static bool ShouldSpawnDedicatedSkillHitAccent(HexTacticsSkillConfig skill)
    {
        return skill != null && skill.HasImpactEffect && skill.IsEnergyConsuming;
    }

    private static float ResolveDedicatedSkillHitAccentScale(HexTacticsSkillConfig skill, HexTacticsHitEffectEntry entry)
    {
        var energyBoost = skill != null ? skill.EnergyCost * 0.08f : 0f;
        var powerBoost = skill != null ? skill.Power * 0.02f : 0f;
        var normalizedScale = Mathf.Clamp(SkillImpactAccentScaleMin + energyBoost + powerBoost, SkillImpactAccentScaleMin, SkillImpactAccentScaleMax);
        return Mathf.Max(0.1f, entry.Scale * normalizedScale);
    }

    private float ResolveDefeatImpactScale(HexUnit defender, HexTacticsSkillConfig skill, float baseMultiplier)
    {
        var defenderScale = defender != null ? Mathf.Lerp(0.92f, 1.2f, Mathf.InverseLerp(0.8f, 2.8f, defender.VisualHeight)) : 1f;
        var powerScale = skill != null ? Mathf.Lerp(0.96f, 1.18f, Mathf.InverseLerp(1f, 6f, skill.Power)) : 1f;
        return Mathf.Max(0.1f, baseMultiplier * defeatImpactScaleMultiplier * defenderScale * powerScale);
    }

    private GameObject SpawnTransientEffect(
        GameObject effectPrefab,
        Vector3 position,
        Quaternion rotation,
        float scale,
        string instanceName)
    {
        if (effectPrefab == null || effectsRoot == null)
        {
            return null;
        }

        var effectInstance = Instantiate(effectPrefab, position, rotation, effectsRoot);
        effectInstance.name = string.IsNullOrWhiteSpace(instanceName) ? effectPrefab.name : instanceName;
        effectInstance.transform.localScale *= Mathf.Max(0.1f, scale);
        if (effectInstance.GetComponent<HexTacticsTransientEffect>() == null)
        {
            effectInstance.AddComponent<HexTacticsTransientEffect>();
        }

        return effectInstance;
    }
}
