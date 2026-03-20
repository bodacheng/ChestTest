using UnityEngine;

public sealed partial class HexTacticsPrototype
{
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
