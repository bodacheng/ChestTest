using System.Collections;
using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    private const float DefeatImpactHeightNormalized = 0.56f;
    private const float DefeatImpactForwardOffsetNormalized = 0.04f;
    private const float DefeatImpactNovaHeightNormalized = 0.34f;
    private const float DefeatImpactEchoHeightOffset = 0.06f;
    private const float ImpactEchoHeightOffset = 0.035f;
    private const float ImpactCenterHeightOffsetWeight = 0.35f;
    private const float ImpactCenterForwardOffsetWeight = 0.16f;
    private const float ImpactConfiguredScaleBlend = 0.42f;
    private const float ImpactScaleCompensationMin = 0.82f;
    private const float ImpactScaleCompensationMax = 1.18f;
    private const float ComplementaryImpactAccentHeightWeight = 0.06f;
    private const float ComplementaryImpactAccentForwardWeight = 0.04f;
    private const float ComplementaryImpactAccentSideWeight = 0.14f;
    private const float ComplementaryImpactAccentDelayMelee = 0.028f;
    private const float ComplementaryImpactAccentDelayRanged = 0.018f;
    private const float ImpactAftershockBaseHeightNormalized = 0.42f;
    private const float ImpactAftershockDelayMelee = 0.038f;
    private const float ImpactAftershockDelayRanged = 0.024f;

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
            var effectScale = ResolveConfiguredImpactEffectScale(skill.ImpactEffectScale);
            SpawnTransientEffect(
                skill.ImpactEffectPrefab,
                effectPosition,
                effectRotation,
                effectScale,
                skill.ImpactEffectPrefab.name,
                alignVisualCenter: true,
                targetVisualExtent: ResolveImpactTargetVisualExtent(defender));
            TrySpawnImpactEcho(skill.ImpactEffectPrefab, effectPosition, effectRotation, effectScale, defender, skill);
            TrySpawnComplementaryImpactAccent(attacker, defender, skill, skill.ImpactEffectPrefab, effectPosition, effectRotation, effectScale);
            TrySpawnImpactAftershock(attacker, defender, skill, skill.ImpactEffectPrefab, effectPosition, effectRotation, effectScale);
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
        var autoScale = ResolveConfiguredImpactEffectScale(entry.Scale);
        SpawnTransientEffect(
            entry.Prefab,
            autoEffectPosition,
            effectRotation,
            autoScale,
            entry.DisplayName,
            alignVisualCenter: true,
            targetVisualExtent: ResolveImpactTargetVisualExtent(defender));
        TrySpawnImpactEcho(entry.Prefab, autoEffectPosition, effectRotation, autoScale, defender, skill);
        TrySpawnComplementaryImpactAccent(attacker, defender, skill, entry.Prefab, autoEffectPosition, effectRotation, autoScale);
        TrySpawnImpactAftershock(attacker, defender, skill, entry.Prefab, autoEffectPosition, effectRotation, autoScale);
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
        StartCoroutine(SpawnDelayedTransientEffect(
            effectPrefab,
            echoPosition,
            echoRotation,
            echoScale,
            echoDelay,
            effectPrefab.name + "_Echo",
            alignVisualCenter: true,
            targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 0.86f));
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
                SpawnTransientEffect(
                    defeatImpactPrimaryPrefab,
                    effectPosition,
                    effectRotation,
                    primaryScale,
                    defeatImpactPrimaryPrefab.name + "_Defeat",
                    alignVisualCenter: true,
                    targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 1.18f);
            }

            if (defeatImpactSecondaryPrefab != null)
            {
                var secondaryRotation = effectRotation * Quaternion.Euler(0f, 28f, 0f);
                var secondaryPosition = effectPosition + Vector3.up * Mathf.Max(0.06f, defender.VisualHeight * 0.05f);
                SpawnTransientEffect(
                    defeatImpactSecondaryPrefab,
                    secondaryPosition,
                    secondaryRotation,
                    secondaryScale,
                    defeatImpactSecondaryPrefab.name + "_Defeat",
                    alignVisualCenter: true,
                    targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 1.08f);
            }

            if (defeatImpactTertiaryPrefab != null)
            {
                var novaPosition = ResolveDefeatNovaEffectPosition(attacker, defender);
                SpawnTransientEffect(
                    defeatImpactTertiaryPrefab,
                    novaPosition,
                    effectRotation,
                    novaScale,
                    defeatImpactTertiaryPrefab.name + "_Defeat",
                    alignVisualCenter: true,
                    targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 1.22f);
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

        SpawnTransientEffect(
            entry.Prefab,
            effectPosition,
            effectRotation,
            Mathf.Max(primaryScale, entry.Scale * 1.35f),
            entry.DisplayName + "_Defeat",
            alignVisualCenter: true,
            targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 1.18f);
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
        SpawnTransientEffect(
            effectPrefab,
            echoPosition,
            echoRotation,
            Mathf.Max(0.1f, scale),
            effectPrefab.name + "_DefeatEcho",
            alignVisualCenter: true);
    }

    private IEnumerator SpawnDelayedTransientEffect(
        GameObject effectPrefab,
        Vector3 position,
        Quaternion rotation,
        float scale,
        float delay,
        string instanceName,
        bool alignVisualCenter = false,
        float targetVisualExtent = 0f)
    {
        if (effectPrefab == null || delay <= 0.001f)
        {
            yield break;
        }

        yield return new WaitForSeconds(delay);
        SpawnTransientEffect(
            effectPrefab,
            position,
            rotation,
            scale,
            instanceName,
            alignVisualCenter,
            targetVisualExtent);
    }

    private Vector3 ResolveHitEffectPosition(HexUnit attacker, HexUnit defender, HexTacticsHitEffectEntry entry)
    {
        var heightFactor = Mathf.Max(0.2f, entry.HeightNormalized);
        return ResolveCenteredImpactEffectPosition(
            attacker,
            defender,
            Mathf.Lerp(hitEffectHeightNormalized, heightFactor, 0.75f),
            Mathf.Max(hitEffectForwardOffset, entry.ForwardOffset));
    }

    private Vector3 ResolveConfiguredHitEffectPosition(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        return ResolveCenteredImpactEffectPosition(
            attacker,
            defender,
            skill.ImpactHeightNormalized,
            Mathf.Max(hitEffectForwardOffset, skill.ImpactForwardOffset));
    }

    private Vector3 ResolveDefeatHitEffectPosition(HexUnit attacker, HexUnit defender)
    {
        return ResolveCenteredImpactEffectPosition(attacker, defender, DefeatImpactHeightNormalized, DefeatImpactForwardOffsetNormalized);
    }

    private Vector3 ResolveDefeatNovaEffectPosition(HexUnit attacker, HexUnit defender)
    {
        return ResolveCenteredImpactEffectPosition(attacker, defender, DefeatImpactNovaHeightNormalized, 0.02f);
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

    private void TrySpawnComplementaryImpactAccent(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        GameObject primaryEffectPrefab,
        Vector3 primaryEffectPosition,
        Quaternion effectRotation,
        float primaryScale)
    {
        if (!ShouldSpawnComplementaryImpactAccent(skill))
        {
            return;
        }

        if (IsHovlImpactEffect(primaryEffectPrefab))
        {
            TrySpawnCatalogImpactAccent(attacker, defender, skill, primaryEffectPrefab, primaryEffectPosition, effectRotation, primaryScale);
            return;
        }

        if (TrySpawnHovlImpactAccent(attacker, defender, skill, primaryEffectPrefab, primaryEffectPosition, effectRotation, primaryScale))
        {
            return;
        }

        TrySpawnCatalogImpactAccent(attacker, defender, skill, primaryEffectPrefab, primaryEffectPosition, effectRotation, primaryScale);
    }

    private static bool ShouldSpawnComplementaryImpactAccent(HexTacticsSkillConfig skill)
    {
        return skill != null;
    }

    private bool TrySpawnCatalogImpactAccent(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        GameObject primaryEffectPrefab,
        Vector3 primaryEffectPosition,
        Quaternion effectRotation,
        float primaryScale)
    {
        if (!TryEnsureHitEffectCatalogLoaded())
        {
            return false;
        }

        var accentStyle = ResolveImpactAccentStyle(skill);
        if (!hitEffectCatalog.TryResolveEffect(accentStyle, nextHitEffectVariantIndex++, autoSelectOnly: false, out var entry) ||
            entry?.Prefab == null ||
            entry.Prefab == primaryEffectPrefab)
        {
            return false;
        }

        var accentPosition = Vector3.Lerp(
            primaryEffectPosition,
            ResolveHitEffectPosition(attacker, defender, entry),
            0.38f);
        accentPosition = OffsetLayeredImpactPosition(attacker, defender, skill, accentPosition, 1f);

        var accentScale = Mathf.Max(
            0.1f,
            entry.Scale * ResolveCatalogImpactAccentScaleMultiplier(skill) * Mathf.Lerp(0.92f, 1.08f, Mathf.InverseLerp(0.8f, 1.24f, primaryScale)));
        var accentDelay = ResolveComplementaryImpactAccentDelay(skill);
        if (accentDelay <= 0.001f)
        {
            SpawnTransientEffect(
                entry.Prefab,
                accentPosition,
                effectRotation,
                accentScale,
                entry.DisplayName + "_Accent",
                alignVisualCenter: true,
                targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 0.88f);
        }
        else
        {
            StartCoroutine(SpawnDelayedTransientEffect(
                entry.Prefab,
                accentPosition,
                effectRotation,
                accentScale,
                accentDelay,
                entry.DisplayName + "_Accent",
                alignVisualCenter: true,
                targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 0.88f));
        }

        return true;
    }

    private bool TrySpawnHovlImpactAccent(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        GameObject primaryEffectPrefab,
        Vector3 primaryEffectPosition,
        Quaternion effectRotation,
        float primaryScale)
    {
        var accentAddress = ResolveHovlImpactAccentAddress(skill, primaryEffectPrefab);
        if (string.IsNullOrWhiteSpace(accentAddress))
        {
            return false;
        }

        var accentPrefab = HexTacticsAddressables.LoadAsset<GameObject>(accentAddress);
        if (accentPrefab == null || accentPrefab == primaryEffectPrefab)
        {
            return false;
        }

        var accentPosition = OffsetLayeredImpactPosition(attacker, defender, skill, primaryEffectPosition, -1f);
        var accentScale = Mathf.Max(0.1f, primaryScale * ResolveHovlImpactAccentScaleMultiplier(skill));
        var accentDelay = ResolveComplementaryImpactAccentDelay(skill) * 0.82f;
        if (accentDelay <= 0.001f)
        {
            SpawnTransientEffect(
                accentPrefab,
                accentPosition,
                effectRotation,
                accentScale,
                accentPrefab.name + "_Accent",
                alignVisualCenter: true,
                targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 0.94f);
        }
        else
        {
            StartCoroutine(SpawnDelayedTransientEffect(
                accentPrefab,
                accentPosition,
                effectRotation,
                accentScale,
                accentDelay,
                accentPrefab.name + "_Accent",
                alignVisualCenter: true,
                targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 0.94f));
        }

        return true;
    }

    private void TrySpawnImpactAftershock(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        GameObject primaryEffectPrefab,
        Vector3 primaryEffectPosition,
        Quaternion effectRotation,
        float primaryScale)
    {
        if (!ShouldSpawnImpactAftershock(skill))
        {
            return;
        }

        var aftershockAddress = ResolveImpactAftershockAddress(skill, primaryEffectPrefab);
        if (string.IsNullOrWhiteSpace(aftershockAddress))
        {
            return;
        }

        var aftershockPrefab = HexTacticsAddressables.LoadAsset<GameObject>(aftershockAddress);
        if (aftershockPrefab == null || aftershockPrefab == primaryEffectPrefab)
        {
            return;
        }

        var aftershockPosition = ResolveImpactAftershockPosition(attacker, defender, skill, primaryEffectPosition);
        var aftershockScale = Mathf.Max(0.1f, primaryScale * ResolveImpactAftershockScaleMultiplier(skill));
        var aftershockDelay = ResolveImpactAftershockDelay(skill);
        StartCoroutine(SpawnDelayedTransientEffect(
            aftershockPrefab,
            aftershockPosition,
            effectRotation,
            aftershockScale,
            aftershockDelay,
            aftershockPrefab.name + "_Aftershock",
            alignVisualCenter: true,
            targetVisualExtent: ResolveImpactTargetVisualExtent(defender) * 1.02f));
    }

    private static bool ShouldSpawnImpactAftershock(HexTacticsSkillConfig skill)
    {
        return skill != null &&
               (skill.AttackRange > 0 ||
                skill.IsEnergyConsuming ||
                skill.Power >= 5 ||
                skill.CollisionAttribute != HexTacticsCollisionAttribute.None ||
                skill.SelfMovementAttribute != HexTacticsSelfMovementAttribute.None);
    }

    private static HexTacticsHitEffectStyle ResolveImpactAccentStyle(HexTacticsSkillConfig skill)
    {
        var accentPower = Mathf.Max(1, skill != null ? skill.Power + skill.EnergyCost + (skill.CollisionAttribute != HexTacticsCollisionAttribute.None ? 1 : 0) : 1);
        if (accentPower <= 3)
        {
            return HexTacticsHitEffectStyle.Light;
        }

        if (accentPower <= 5)
        {
            return HexTacticsHitEffectStyle.Medium;
        }

        return HexTacticsHitEffectStyle.Heavy;
    }

    private static float ResolveCatalogImpactAccentScaleMultiplier(HexTacticsSkillConfig skill)
    {
        var scale = 0.52f;
        if (skill != null)
        {
            scale += Mathf.InverseLerp(2f, 8f, skill.Power) * 0.12f;
            scale += skill.AttackRange > 0 ? 0.08f : 0f;
            scale += skill.IsEnergyConsuming ? 0.05f : 0f;
            scale += skill.CollisionAttribute != HexTacticsCollisionAttribute.None ? 0.04f : 0f;
        }

        return Mathf.Clamp(scale, 0.5f, 0.84f);
    }

    private static float ResolveHovlImpactAccentScaleMultiplier(HexTacticsSkillConfig skill)
    {
        var scale = 0.58f;
        if (skill != null)
        {
            scale += Mathf.InverseLerp(2f, 8f, skill.Power) * 0.14f;
            scale += skill.AttackRange > 0 ? 0.1f : 0f;
            scale += skill.IsEnergyConsuming ? 0.06f : 0f;
            scale += skill.CollisionAttribute != HexTacticsCollisionAttribute.None ? 0.04f : 0f;
        }

        return Mathf.Clamp(scale, 0.56f, 0.94f);
    }

    private static float ResolveImpactAftershockScaleMultiplier(HexTacticsSkillConfig skill)
    {
        var scale = skill != null && skill.AttackRange > 0 ? 0.74f : 0.64f;
        if (skill != null)
        {
            scale += skill.IsEnergyConsuming ? 0.06f : 0f;
            scale += skill.CollisionAttribute != HexTacticsCollisionAttribute.None ? 0.08f : 0f;
            scale += Mathf.InverseLerp(4f, 8f, skill.Power) * 0.12f;
        }

        return Mathf.Clamp(scale, 0.62f, 0.96f);
    }

    private static float ResolveComplementaryImpactAccentDelay(HexTacticsSkillConfig skill)
    {
        if (skill == null)
        {
            return 0f;
        }

        var delay = skill.AttackRange > 0 ? ComplementaryImpactAccentDelayRanged : ComplementaryImpactAccentDelayMelee;
        if (skill.IsEnergyConsuming)
        {
            delay *= 0.8f;
        }

        return Mathf.Max(0f, delay);
    }

    private static float ResolveImpactAftershockDelay(HexTacticsSkillConfig skill)
    {
        if (skill == null)
        {
            return 0f;
        }

        var delay = skill.AttackRange > 0 ? ImpactAftershockDelayRanged : ImpactAftershockDelayMelee;
        if (skill.CollisionAttribute != HexTacticsCollisionAttribute.None)
        {
            delay *= 0.76f;
        }

        return Mathf.Max(0.012f, delay);
    }

    private Vector3 OffsetLayeredImpactPosition(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        Vector3 basePosition,
        float lateralDirectionSign)
    {
        var direction = ResolvePlanarDirection(attacker, defender, Vector3.forward);
        var side = Vector3.Cross(Vector3.up, direction);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.right;
        }

        side.Normalize();
        var sideOffset = side * lateralDirectionSign * Mathf.Max(0.025f, defender != null ? defender.SelectionRadius * ComplementaryImpactAccentSideWeight : 0.04f);
        var heightOffset = Vector3.up * Mathf.Max(0.02f, defender != null ? defender.VisualHeight * ComplementaryImpactAccentHeightWeight : 0.04f);
        var forwardOffset = direction * Mathf.Max(
            0.015f,
            defender != null
                ? defender.SelectionRadius * ComplementaryImpactAccentForwardWeight * (skill != null && skill.AttackRange > 0 ? 1.1f : 0.92f)
                : 0.03f);
        return basePosition + sideOffset + heightOffset - forwardOffset;
    }

    private Vector3 ResolveImpactAftershockPosition(
        HexUnit attacker,
        HexUnit defender,
        HexTacticsSkillConfig skill,
        Vector3 primaryEffectPosition)
    {
        var heightNormalized = skill != null
            ? Mathf.Clamp(skill.ImpactHeightNormalized - 0.16f, ImpactAftershockBaseHeightNormalized, 0.54f)
            : ImpactAftershockBaseHeightNormalized;
        var forwardOffset = skill != null
            ? skill.ImpactForwardOffset * 0.45f
            : hitEffectForwardOffset * 0.45f;
        var centeredPosition = ResolveCenteredImpactEffectPosition(attacker, defender, heightNormalized, forwardOffset);
        return Vector3.Lerp(primaryEffectPosition, centeredPosition, 0.72f);
    }

    private static bool IsHovlImpactEffect(GameObject effectPrefab)
    {
        return effectPrefab != null &&
               effectPrefab.name.IndexOf("HovlImpact", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ResolveHovlImpactAccentAddress(HexTacticsSkillConfig skill, GameObject primaryEffectPrefab)
    {
        var preferredAddress = HexTacticsAssetPaths.HovlImpactLightRoseAddress;
        if (skill != null)
        {
            if (skill.AttackRange > 0)
            {
                preferredAddress = skill.IsEnergyConsuming || skill.Power >= 6
                    ? HexTacticsAssetPaths.HovlImpactRangedNovaAddress
                    : HexTacticsAssetPaths.HovlImpactRangedMistAddress;
            }
            else if (skill.CollisionAttribute != HexTacticsCollisionAttribute.None)
            {
                preferredAddress = skill.Power >= 7
                    ? HexTacticsAssetPaths.HovlImpactHeavyCrimsonAddress
                    : HexTacticsAssetPaths.HovlImpactHeavyEmberAddress;
            }
            else if (skill.Power >= 6)
            {
                preferredAddress = HexTacticsAssetPaths.HovlImpactHeavyEmberAddress;
            }
            else if (skill.Power >= 4)
            {
                preferredAddress = HexTacticsAssetPaths.HovlImpactMediumAzureAddress;
            }
            else if (skill.IsEnergyConsuming)
            {
                preferredAddress = HexTacticsAssetPaths.HovlImpactLightVerdantAddress;
            }
        }

        if (!EffectAddressMatchesPrefab(primaryEffectPrefab, preferredAddress))
        {
            return preferredAddress;
        }

        return ResolveAlternateHovlImpactAddress(skill, preferredAddress);
    }

    private static string ResolveImpactAftershockAddress(HexTacticsSkillConfig skill, GameObject primaryEffectPrefab)
    {
        var preferredAddress = HexTacticsAssetPaths.HovlImpactMediumAzureAddress;
        if (skill != null)
        {
            if (skill.AttackRange > 0)
            {
                preferredAddress = skill.IsEnergyConsuming || skill.Power >= 6
                    ? HexTacticsAssetPaths.HovlImpactRangedNovaAddress
                    : HexTacticsAssetPaths.HovlImpactRangedMistAddress;
            }
            else if (skill.CollisionAttribute != HexTacticsCollisionAttribute.None || skill.Power >= 7)
            {
                preferredAddress = HexTacticsAssetPaths.HovlImpactHeavyCrimsonAddress;
            }
            else if (skill.Power >= 5 || skill.SelfMovementAttribute != HexTacticsSelfMovementAttribute.None)
            {
                preferredAddress = HexTacticsAssetPaths.HovlImpactHeavyEmberAddress;
            }
        }

        if (!EffectAddressMatchesPrefab(primaryEffectPrefab, preferredAddress))
        {
            return preferredAddress;
        }

        return ResolveAlternateHovlImpactAddress(skill, preferredAddress);
    }

    private static string ResolveAlternateHovlImpactAddress(HexTacticsSkillConfig skill, string attemptedAddress)
    {
        if (skill != null && skill.AttackRange > 0)
        {
            return attemptedAddress == HexTacticsAssetPaths.HovlImpactRangedNovaAddress
                ? HexTacticsAssetPaths.HovlImpactRangedMistAddress
                : HexTacticsAssetPaths.HovlImpactRangedNovaAddress;
        }

        if (attemptedAddress == HexTacticsAssetPaths.HovlImpactHeavyCrimsonAddress)
        {
            return HexTacticsAssetPaths.HovlImpactHeavyEmberAddress;
        }

        if (attemptedAddress == HexTacticsAssetPaths.HovlImpactHeavyEmberAddress)
        {
            return HexTacticsAssetPaths.HovlImpactMediumAzureAddress;
        }

        if (attemptedAddress == HexTacticsAssetPaths.HovlImpactMediumAzureAddress)
        {
            return skill != null && skill.IsEnergyConsuming
                ? HexTacticsAssetPaths.HovlImpactLightVerdantAddress
                : HexTacticsAssetPaths.HovlImpactLightRoseAddress;
        }

        return HexTacticsAssetPaths.HovlImpactMediumAzureAddress;
    }

    private static bool EffectAddressMatchesPrefab(GameObject effectPrefab, string effectAddress)
    {
        if (effectPrefab == null || string.IsNullOrWhiteSpace(effectAddress))
        {
            return false;
        }

        var slashIndex = effectAddress.LastIndexOf('/');
        var assetName = slashIndex >= 0 && slashIndex < effectAddress.Length - 1
            ? effectAddress.Substring(slashIndex + 1)
            : effectAddress;
        return effectPrefab.name.Equals(assetName, System.StringComparison.OrdinalIgnoreCase);
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
        string instanceName,
        bool alignVisualCenter = false,
        float targetVisualExtent = 0f)
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

        if (alignVisualCenter)
        {
            NormalizeImpactEffectVisualScale(effectInstance.transform, targetVisualExtent);
            AlignEffectVisualCenter(effectInstance.transform, position);
        }

        return effectInstance;
    }

    private Vector3 ResolveCenteredImpactEffectPosition(HexUnit attacker, HexUnit defender, float normalizedHeight, float normalizedForwardOffset)
    {
        var direction = ResolvePlanarDirection(attacker, defender, defender != null ? defender.Transform.forward : Vector3.forward);
        var bodyCenter = ResolveUnitBodyCenter(defender);
        var heightOffset = ResolveImpactCenterHeightOffset(defender, normalizedHeight);
        var forwardOffset = ResolveImpactCenterForwardOffset(defender, normalizedForwardOffset);
        return bodyCenter + Vector3.up * heightOffset + direction * forwardOffset;
    }

    private Vector3 ResolveUnitBodyCenter(HexUnit unit)
    {
        if (unit?.Transform == null)
        {
            return Vector3.zero;
        }

        return unit.Transform.position + Vector3.up * ResolveUnitBodyCenterHeight(unit);
    }

    private float ResolveUnitBodyCenterHeight(HexUnit unit)
    {
        if (unit == null)
        {
            return unitHoverHeight;
        }

        var visualHeight = Mathf.Max(unitHoverHeight, unit.VisualHeight);
        var fallbackCenterHeight = visualHeight * 0.52f;
        var centerHeight = unit.VisualCenterHeight > 0.001f
            ? unit.VisualCenterHeight
            : fallbackCenterHeight;
        return Mathf.Clamp(centerHeight, unitHoverHeight * 0.8f, visualHeight * 0.84f);
    }

    private float ResolveImpactCenterHeightOffset(HexUnit defender, float normalizedHeight)
    {
        if (defender == null)
        {
            return 0f;
        }

        var visualHeight = Mathf.Max(unitHoverHeight, defender.VisualHeight);
        var centerNormalized = ResolveUnitBodyCenterHeight(defender) / visualHeight;
        var normalizedDelta = Mathf.Clamp(normalizedHeight - centerNormalized, -0.28f, 0.28f);
        return visualHeight * normalizedDelta * ImpactCenterHeightOffsetWeight;
    }

    private static float ResolveImpactCenterForwardOffset(HexUnit defender, float normalizedForwardOffset)
    {
        if (defender == null)
        {
            return 0f;
        }

        var reducedOffset = defender.SelectionRadius * Mathf.Max(0f, normalizedForwardOffset) * ImpactCenterForwardOffsetWeight;
        return Mathf.Min(reducedOffset, defender.SelectionRadius * 0.08f);
    }

    private float ResolveImpactTargetVisualExtent(HexUnit defender)
    {
        if (defender == null)
        {
            return Mathf.Max(hexRadius * 0.2f, baseUnitVisualHeight * 0.22f);
        }

        var basedOnRadius = defender.SelectionRadius * 0.96f;
        var basedOnHeight = defender.VisualHeight * 0.22f;
        return Mathf.Clamp(
            Mathf.Max(basedOnRadius, basedOnHeight),
            hexRadius * 0.2f,
            hexRadius * 0.38f);
    }

    private static float ResolveConfiguredImpactEffectScale(float requestedScale)
    {
        var safeScale = Mathf.Max(0.1f, requestedScale);
        return Mathf.Lerp(1f, safeScale, ImpactConfiguredScaleBlend);
    }

    private static void AlignEffectVisualCenter(Transform effectTransform, Vector3 desiredPosition)
    {
        if (effectTransform == null || !TryGetEffectVisualBounds(effectTransform, out var bounds))
        {
            return;
        }

        var delta = desiredPosition - bounds.center;
        if (delta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        effectTransform.position += delta;
    }

    private static void NormalizeImpactEffectVisualScale(Transform effectTransform, float targetVisualExtent)
    {
        if (effectTransform == null ||
            targetVisualExtent <= 0.001f ||
            !TryGetEffectVisualBounds(effectTransform, out var bounds))
        {
            return;
        }

        var currentExtent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        if (currentExtent <= 0.001f)
        {
            return;
        }

        var scaleAdjustment = Mathf.Clamp(
            targetVisualExtent / currentExtent,
            ImpactScaleCompensationMin,
            ImpactScaleCompensationMax);
        effectTransform.localScale *= scaleAdjustment;
    }

    private static bool TryGetEffectVisualBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        var hasBounds = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            var rendererBounds = renderer.bounds;
            if (rendererBounds.size.sqrMagnitude <= 0.0001f)
            {
                rendererBounds = new Bounds(renderer.transform.position, Vector3.zero);
            }

            if (!hasBounds)
            {
                bounds = rendererBounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(rendererBounds);
        }

        if (hasBounds)
        {
            return true;
        }

        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particleSystem == null || !particleSystem.gameObject.activeInHierarchy)
            {
                continue;
            }

            var pointBounds = new Bounds(particleSystem.transform.position, Vector3.zero);
            if (!hasBounds)
            {
                bounds = pointBounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(pointBounds);
        }

        return hasBounds;
    }
}
