using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    private void SpawnAttackReleaseEffect(HexUnit attacker, HexUnit defender, float travelDuration)
    {
        if (!enableHitEffects || !UsesRangedAttackPresentation(attacker) || effectsRoot == null || attacker?.Transform == null || defender?.Transform == null)
        {
            return;
        }

        if (!TryEnsureRangedWaveEffectLoaded() || rangedWaveEffectPrefab == null)
        {
            return;
        }

        var startPosition = ResolveRangedWaveStartPosition(attacker, defender);
        var endPosition = ResolveRangedWaveEndPosition(attacker, defender);
        var effectRotation = ResolveHitEffectRotation(attacker, defender);
        var effectInstance = Instantiate(rangedWaveEffectPrefab, startPosition, effectRotation, effectsRoot);
        effectInstance.name = rangedWaveEffectPrefab.name;
        effectInstance.transform.localScale *= ResolveRangedWaveScale(attacker, defender);

        var travelEffect = effectInstance.GetComponent<HexTacticsTravelingEffect>();
        if (travelEffect == null)
        {
            travelEffect = effectInstance.AddComponent<HexTacticsTravelingEffect>();
        }

        travelEffect.Initialize(startPosition, endPosition, Mathf.Max(0.06f, travelDuration));
    }

    private void SpawnHitEffect(HexUnit attacker, HexUnit defender)
    {
        if (!enableHitEffects || effectsRoot == null || attacker?.Transform == null || defender?.Transform == null)
        {
            return;
        }

        if (!TryEnsureHitEffectCatalogLoaded())
        {
            return;
        }

        if (!hitEffectCatalog.TryResolveAutoEffect(attacker.AttackPower, nextHitEffectVariantIndex++, out var entry) ||
            entry?.Prefab == null)
        {
            return;
        }

        var effectPosition = ResolveHitEffectPosition(attacker, defender, entry);
        var effectRotation = ResolveHitEffectRotation(attacker, defender);
        var effectInstance = Instantiate(entry.Prefab, effectPosition, effectRotation, effectsRoot);
        effectInstance.name = entry.DisplayName;
        effectInstance.transform.localScale *= Mathf.Max(0.1f, entry.Scale);

        if (effectInstance.GetComponent<HexTacticsTransientEffect>() == null)
        {
            effectInstance.AddComponent<HexTacticsTransientEffect>();
        }
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

    private static bool UsesRangedAttackPresentation(HexUnit attacker)
    {
        return attacker != null && attacker.AttackRange > 0;
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
}
