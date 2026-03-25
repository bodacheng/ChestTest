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
    private const float ImpactEchoHeightOffset = 0.035f;

    private void SpawnAttackReleaseEffect(HexUnit attacker, HexUnit defender, float travelDuration, HexTacticsSkillConfig skill)
    {
        if (!enableHitEffects || !UsesRangedAttackPresentation(skill) || effectsRoot == null || attacker?.Transform == null || defender?.Transform == null)
        {
            return;
        }

        var projectileEffectPrefab = ResolveProjectileEffectPrefab(skill);
        if (projectileEffectPrefab == null)
        {
            return;
        }

        var startPosition = ResolveRangedWaveStartPosition(attacker, defender);
        var endPosition = ResolveRangedWaveEndPosition(attacker, defender, skill);
        var effectRotation = ResolveHitEffectRotation(attacker, defender);
        SpawnProjectileReleaseBurst(attacker, defender, skill, startPosition, effectRotation);

        var effectInstance = Instantiate(projectileEffectPrefab, startPosition, effectRotation, effectsRoot);
        effectInstance.name = projectileEffectPrefab.name;
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

        travelEffect.Initialize(
            startPosition,
            endPosition,
            Mathf.Max(0.06f, travelDuration),
            ResolveProjectileArcHeight(attacker, defender, skill),
            ResolveProjectileLateralSway(attacker, defender, skill),
            skill != null ? skill.ProjectileSpinDegreesPerSecond : 0f);
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
            TrySpawnImpactEcho(skill.ImpactEffectPrefab, effectPosition, effectRotation, skill.ImpactEffectScale, defender, skill);
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
        var autoScale = Mathf.Max(0.1f, entry.Scale);
        SpawnTransientEffect(entry.Prefab, autoEffectPosition, effectRotation, autoScale, entry.DisplayName);
        TrySpawnImpactEcho(entry.Prefab, autoEffectPosition, effectRotation, autoScale, defender, skill);
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

    private GameObject ResolveReleaseEffectPrefab(HexTacticsSkillConfig skill)
    {
        if (skill?.ReleaseEffectPrefab != null)
        {
            return skill.ReleaseEffectPrefab;
        }

        if (skill != null &&
            skill.IsEnergyConsuming &&
            TryEnsureRangedWaveEffectLoaded())
        {
            return rangedWaveEffectPrefab;
        }

        return null;
    }

    private static float ResolveProjectileEffectScale(HexTacticsSkillConfig skill)
    {
        return skill != null ? skill.ProjectileEffectScale : 1f;
    }

    private static float ResolveReleaseEffectScale(HexTacticsSkillConfig skill)
    {
        if (skill == null)
        {
            return 0.66f;
        }

        return skill.HasReleaseEffect
            ? skill.ReleaseEffectScale
            : Mathf.Clamp(skill.ProjectileEffectScale * 0.62f, 0.42f, 0.86f);
    }

    private void SpawnProjectileReleaseBurst(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        Vector3 startPosition,
        Quaternion effectRotation)
    {
        var releaseEffectPrefab = ResolveReleaseEffectPrefab(skill);
        if (releaseEffectPrefab == null)
        {
            return;
        }

        var releaseScale = ResolveRangedWaveScale(attacker, defender) * ResolveReleaseEffectScale(skill);
        SpawnTransientEffect(releaseEffectPrefab, startPosition, effectRotation, releaseScale, releaseEffectPrefab.name + "_Release");
    }

    private void TrySpawnImpactEcho(
        GameObject effectPrefab,
        Vector3 position,
        Quaternion rotation,
        float baseScale,
        HexUnit defender,
        HexTacticsSkillConfig skill)
    {
        if (effectPrefab == null || defender == null || !ShouldSpawnImpactEcho(skill))
        {
            return;
        }

        var echoScale = Mathf.Max(0.1f, baseScale * ResolveImpactEchoScale(skill));
        var echoDelay = ResolveImpactEchoDelay(skill);
        if (echoDelay <= 0.001f)
        {
            return;
        }

        var echoPosition = position + Vector3.up * Mathf.Max(ImpactEchoHeightOffset, defender.VisualHeight * 0.025f);
        var echoRotation = rotation * Quaternion.Euler(0f, 10f, 0f);
        StartCoroutine(SpawnDelayedTransientEffect(effectPrefab, echoPosition, echoRotation, echoScale, echoDelay, effectPrefab.name + "_Echo"));
    }

    private static bool ShouldSpawnImpactEcho(HexTacticsSkillConfig skill)
    {
        return skill != null &&
               (skill.AttackRange > 0 || skill.IsEnergyConsuming || skill.Power >= 6 || skill.CollisionAttribute != HexTacticsCollisionAttribute.None);
    }

    private static float ResolveImpactEchoScale(HexTacticsSkillConfig skill)
    {
        var echoScale = skill != null && skill.AttackRange > 0 ? 0.58f : 0.46f;
        if (skill != null && skill.IsEnergyConsuming)
        {
            echoScale += 0.08f;
        }

        if (skill != null && skill.CollisionAttribute != HexTacticsCollisionAttribute.None)
        {
            echoScale += 0.04f;
        }

        return Mathf.Clamp(echoScale, 0.36f, 0.74f);
    }

    private static float ResolveImpactEchoDelay(HexTacticsSkillConfig skill)
    {
        if (skill == null)
        {
            return 0f;
        }

        return skill.AttackRange > 0
            ? 0.03f
            : 0.045f;
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

    private IEnumerator SpawnDelayedTransientEffect(
        GameObject effectPrefab,
        Vector3 position,
        Quaternion rotation,
        float scale,
        float delay,
        string instanceName)
    {
        if (effectPrefab == null || delay <= 0.001f)
        {
            yield break;
        }

        yield return new WaitForSeconds(delay);
        SpawnTransientEffect(effectPrefab, position, rotation, scale, instanceName);
    }

    private Vector3 ResolveHitEffectPosition(HexUnit attacker, HexUnit defender, HexTacticsHitEffectEntry entry)
    {
        var direction = ResolvePlanarDirection(attacker, defender, defender.Transform.forward);
        var heightFactor = Mathf.Max(0.2f, entry.HeightNormalized);
        var height = Mathf.Max(unitHoverHeight * 0.85f, defender.VisualHeight * Mathf.Lerp(hitEffectHeightNormalized, heightFactor, 0.75f));
        var forwardOffset = defender.SelectionRadius * Mathf.Max(hitEffectForwardOffset, entry.ForwardOffset);
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveConfiguredHitEffectPosition(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        var direction = ResolvePlanarDirection(attacker, defender, defender.Transform.forward);
        var height = Mathf.Max(unitHoverHeight * 0.85f, defender.VisualHeight * skill.ImpactHeightNormalized);
        var forwardOffset = defender.SelectionRadius * Mathf.Max(hitEffectForwardOffset, skill.ImpactForwardOffset);
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveDefeatHitEffectPosition(HexUnit attacker, HexUnit defender)
    {
        var direction = ResolvePlanarDirection(attacker, defender, defender.Transform.forward);
        var height = Mathf.Max(unitHoverHeight, defender.VisualHeight * DefeatImpactHeightNormalized);
        var forwardOffset = defender.SelectionRadius * DefeatImpactForwardOffsetNormalized;
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveDefeatNovaEffectPosition(HexUnit attacker, HexUnit defender)
    {
        var direction = ResolvePlanarDirection(attacker, defender, defender.Transform.forward);
        var height = Mathf.Max(unitHoverHeight * 0.7f, defender.VisualHeight * DefeatImpactNovaHeightNormalized);
        var forwardOffset = defender.SelectionRadius * 0.02f;
        return defender.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveRangedWaveStartPosition(HexUnit attacker, HexUnit defender)
    {
        var direction = ResolvePlanarDirection(attacker, defender, attacker.Transform.forward);
        var preferredAnchor = attacker?.CharacterConfig != null
            ? attacker.CharacterConfig.ProjectileSourceAnchor
            : HexTacticsEffectAnchorKind.Auto;
        var anchor = preferredAnchor == HexTacticsEffectAnchorKind.CenterMass
            ? null
            : ResolveUnitEffectAnchor(attacker, preferredAnchor);
        if (anchor != null)
        {
            var sourceOffset = attacker.CharacterConfig != null
                ? attacker.CharacterConfig.ProjectileSourceOffset
                : Vector3.zero;
            return anchor.position + ResolveDirectionalOffset(direction, sourceOffset);
        }

        var height = Mathf.Max(unitHoverHeight * 0.8f, attacker.VisualHeight * 0.48f);
        var forwardOffset = Mathf.Max(hexRadius * 0.18f, attacker.SelectionRadius * 0.42f);
        return attacker.Transform.position + Vector3.up * height + direction * forwardOffset;
    }

    private Vector3 ResolveRangedWaveEndPosition(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        var direction = ResolvePlanarDirection(attacker, defender, Vector3.forward);
        var height = Mathf.Max(unitHoverHeight * 0.8f, defender.VisualHeight * 0.5f);
        var backwardOffset = Mathf.Max(hexRadius * 0.12f, defender.SelectionRadius * 0.18f);
        var endPosition = defender.Transform.position + Vector3.up * height - direction * backwardOffset;
        if (skill != null && skill.HasImpactEffect)
        {
            var configuredPosition = ResolveConfiguredHitEffectPosition(attacker, defender, skill);
            endPosition = Vector3.Lerp(endPosition, configuredPosition, 0.55f);
        }

        return endPosition;
    }

    private float ResolveRangedWaveScale(HexUnit attacker, HexUnit defender)
    {
        var distance = Vector3.Distance(attacker.Transform.position, defender.Transform.position);
        return Mathf.Clamp(0.8f + distance * 0.08f, 0.85f, 1.18f);
    }

    private float ResolveProjectileArcHeight(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        var distance = attacker != null && defender != null
            ? Vector3.Distance(attacker.Transform.position, defender.Transform.position)
            : 0f;
        return Mathf.Max(0f, (skill != null ? skill.ProjectileArcHeight : 0f) + distance * 0.018f);
    }

    private float ResolveProjectileLateralSway(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        var distance = attacker != null && defender != null
            ? Vector3.Distance(attacker.Transform.position, defender.Transform.position)
            : 0f;
        var baseSway = skill != null ? skill.ProjectileLateralSway : 0f;
        return Mathf.Max(0f, baseSway * Mathf.Lerp(0.9f, 1.35f, Mathf.InverseLerp(1.2f, 6f, distance)));
    }

    private static bool UsesRangedAttackPresentation(HexTacticsSkillConfig skill)
    {
        return skill != null && skill.AttackRange > 0;
    }

    private static Quaternion ResolveHitEffectRotation(HexUnit attacker, HexUnit defender)
    {
        var direction = ResolvePlanarDirection(attacker, defender, Vector3.forward);
        return Quaternion.LookRotation(direction, Vector3.up);
    }

    private static Vector3 ResolvePlanarDirection(HexUnit attacker, HexUnit defender, Vector3 fallback)
    {
        var direction = defender != null && attacker != null
            ? defender.Transform.position - attacker.Transform.position
            : fallback;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = fallback;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        return direction.normalized;
    }

    private static Vector3 ResolveDirectionalOffset(Vector3 forwardDirection, Vector3 directionalOffset)
    {
        var planarForward = forwardDirection;
        planarForward.y = 0f;
        if (planarForward.sqrMagnitude < 0.0001f)
        {
            planarForward = Vector3.forward;
        }

        planarForward.Normalize();
        var side = Vector3.Cross(Vector3.up, planarForward);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.right;
        }

        side.Normalize();
        return side * directionalOffset.x +
               Vector3.up * directionalOffset.y +
               planarForward * directionalOffset.z;
    }

    private Transform ResolveUnitEffectAnchor(HexUnit unit, HexTacticsEffectAnchorKind preferredAnchor)
    {
        if (unit == null)
        {
            return null;
        }

        if (preferredAnchor == HexTacticsEffectAnchorKind.Auto)
        {
            return ResolveAutoUnitEffectAnchor(unit);
        }

        var anchor = TryResolveExactUnitEffectAnchor(unit, preferredAnchor);
        return anchor != null ? anchor : ResolveAutoUnitEffectAnchor(unit, preferredAnchor);
    }

    private Transform ResolveAutoUnitEffectAnchor(HexUnit unit, HexTacticsEffectAnchorKind? excludedKind = null)
    {
        var orderedKinds = new[]
        {
            HexTacticsEffectAnchorKind.Weapon,
            HexTacticsEffectAnchorKind.Mouth,
            HexTacticsEffectAnchorKind.RightHand,
            HexTacticsEffectAnchorKind.LeftHand,
            HexTacticsEffectAnchorKind.Head
        };

        for (var i = 0; i < orderedKinds.Length; i++)
        {
            if (excludedKind.HasValue && orderedKinds[i] == excludedKind.Value)
            {
                continue;
            }

            var anchor = TryResolveExactUnitEffectAnchor(unit, orderedKinds[i]);
            if (anchor != null)
            {
                return anchor;
            }
        }

        return unit.VisualRoot != null ? unit.VisualRoot : unit.Transform;
    }

    private Transform TryResolveExactUnitEffectAnchor(HexUnit unit, HexTacticsEffectAnchorKind anchorKind)
    {
        if (unit == null)
        {
            return null;
        }

        if (unit.EffectAnchorMap != null &&
            unit.EffectAnchorMap.TryResolve(anchorKind, out var mappedAnchor) &&
            mappedAnchor != null)
        {
            return mappedAnchor;
        }

        var humanoidAnchor = TryResolveHumanoidAnchor(unit, anchorKind);
        if (humanoidAnchor != null)
        {
            return humanoidAnchor;
        }

        return anchorKind switch
        {
            HexTacticsEffectAnchorKind.Mouth => FindNamedAnchor(unit, "mouth", "jaw", "upperjaw", "snout", "muzzle", "beak"),
            HexTacticsEffectAnchorKind.Head => FindNamedAnchor(unit, "head", "skull", "neck"),
            HexTacticsEffectAnchorKind.RightHand => FindNamedAnchor(unit, "righthand", "handright", "handr", "rightpalm", "palmright"),
            HexTacticsEffectAnchorKind.LeftHand => FindNamedAnchor(unit, "lefthand", "handleft", "handl", "leftpalm", "palmleft"),
            HexTacticsEffectAnchorKind.Weapon => FindNamedAnchor(unit, "weapon", "staff", "wand", "rod", "bow", "spear", "gun", "blade"),
            _ => null
        };
    }

    private static Transform TryResolveHumanoidAnchor(HexUnit unit, HexTacticsEffectAnchorKind anchorKind)
    {
        if (unit?.Animator == null || !unit.Animator.isHuman)
        {
            return null;
        }

        return anchorKind switch
        {
            HexTacticsEffectAnchorKind.Head => unit.Animator.GetBoneTransform(HumanBodyBones.Head),
            HexTacticsEffectAnchorKind.RightHand => unit.Animator.GetBoneTransform(HumanBodyBones.RightHand),
            HexTacticsEffectAnchorKind.LeftHand => unit.Animator.GetBoneTransform(HumanBodyBones.LeftHand),
            _ => null
        };
    }

    private static Transform FindNamedAnchor(HexUnit unit, params string[] keywords)
    {
        var searchRoot = unit?.VisualRoot;
        if (searchRoot == null || keywords == null || keywords.Length == 0)
        {
            return null;
        }

        Transform bestTransform = null;
        var bestScore = 0;
        foreach (var candidate in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (candidate == null)
            {
                continue;
            }

            var score = ScoreNamedAnchor(candidate.name, keywords);
            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestTransform = candidate;
        }

        return bestTransform;
    }

    private static int ScoreNamedAnchor(string candidateName, params string[] keywords)
    {
        var normalizedName = NormalizeAnimationName(candidateName);
        if (string.IsNullOrWhiteSpace(normalizedName) || keywords == null || keywords.Length == 0)
        {
            return 0;
        }

        var bestScore = 0;
        for (var i = 0; i < keywords.Length; i++)
        {
            var normalizedKeyword = NormalizeAnimationName(keywords[i]);
            if (string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                continue;
            }

            if (normalizedName == normalizedKeyword)
            {
                bestScore = Mathf.Max(bestScore, 140);
                continue;
            }

            if (normalizedName.StartsWith(normalizedKeyword))
            {
                bestScore = Mathf.Max(bestScore, 110);
                continue;
            }

            if (normalizedName.EndsWith(normalizedKeyword))
            {
                bestScore = Mathf.Max(bestScore, 104);
                continue;
            }

            if (normalizedName.Contains(normalizedKeyword))
            {
                bestScore = Mathf.Max(bestScore, 86);
            }
        }

        if (bestScore <= 0)
        {
            return 0;
        }

        return bestScore + Mathf.Clamp(24 - normalizedName.Length, 0, 18);
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
