using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed partial class HexTacticsPrototype
{
    private void SpawnConfiguredTeam(List<int> rosterSelection, Team team, List<HexCoord> deploySlots, string ownerLabel)
    {
        var displayCounts = new Dictionary<string, int>();
        var count = Mathf.Min(rosterSelection.Count, deploySlots.Count);
        for (var i = 0; i < count; i++)
        {
            var rosterIndex = rosterSelection[i];
            if (!IsValidRosterIndex(rosterIndex))
            {
                continue;
            }

            var definition = characterRoster[rosterIndex];
            if (definition == null)
            {
                continue;
            }

            SpawnConfiguredCharacter(definition, team, deploySlots[i], ownerLabel, displayCounts);
        }
    }

    private void SpawnConfiguredCharacter(
        HexTacticsCharacterConfig definition,
        Team team,
        HexCoord coord,
        string ownerLabel,
        Dictionary<string, int> displayCounts)
    {
        if (definition == null || !cells.TryGetValue(coord, out var cell) || cell.Occupant != null)
        {
            return;
        }

        var unitRoot = new GameObject(CreateBattleUnitName(ownerLabel, definition, displayCounts));
        unitRoot.transform.SetParent(unitsRoot, false);
        unitRoot.transform.localPosition = CellToUnitPosition(coord);

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "SelectionMarker";
        ring.transform.SetParent(unitRoot.transform, false);
        ring.transform.localPosition = new Vector3(0f, -unitHoverHeight * 0.78f, 0f);
        ring.transform.localScale = new Vector3(0.34f, 0.02f, 0.34f);
        var ringCollider = ring.GetComponent<Collider>();
        if (ringCollider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(ringCollider);
            }
            else
            {
                DestroyImmediate(ringCollider);
            }
        }

        var ringRenderer = ring.GetComponent<MeshRenderer>();
        ringRenderer.sharedMaterial = team == Team.Blue ? blueRingMaterial : redRingMaterial;
        ringRenderer.enabled = false;

        var unit = new HexUnit(
            nextUnitId++,
            definition,
            unitRoot.name,
            definition.DisplayName,
            team,
            coord,
            unitRoot.transform,
            ringRenderer,
            team == Team.Blue ? blueRingMaterial : redRingMaterial,
            definition.MaxHealth,
            definition.AttackPower,
            definition.Cost,
            definition.MoveRange,
            definition.AttackRange,
            definition.Speed);

        unit.VisualHeight = baseUnitVisualHeight;
        unit.LabelHeight = baseUnitVisualHeight + worldLabelPadding;
        unit.SelectionRadius = hexRadius * 0.42f;

        if (!TryAttachAnimatedVisual(unit, definition))
        {
            AttachFallbackUnitVisual(unit);
        }

        ApplyRingScale(unit);
        FaceUnitTowards(unit, unit.Transform.position + GetTeamFacingDirection(team), immediate: true);
        SetUnitIdle(unit);

        cell.Occupant = unit;
        units.Add(unit);
    }

    private static string CreateBattleUnitName(string ownerLabel, HexTacticsCharacterConfig definition, Dictionary<string, int> displayCounts)
    {
        var displayName = definition != null ? definition.DisplayName : "角色";
        if (!displayCounts.TryGetValue(displayName, out var count))
        {
            count = 0;
        }

        count++;
        displayCounts[displayName] = count;
        return $"{ownerLabel} {displayName} {count}";
    }

    private void RegisterUnitCollider(Collider collider, HexUnit unit)
    {
        if (collider != null)
        {
            unitLookups[collider] = unit;
        }
    }

    private bool TryAttachAnimatedVisual(HexUnit unit, HexTacticsCharacterConfig definition)
    {
        var prefab = LoadUnitVisualPrefab(definition);
        if (prefab == null)
        {
            return false;
        }

        var visualInstance = Instantiate(prefab);
        visualInstance.name = $"{definition.DisplayName} Visual";
        visualInstance.transform.SetParent(unit.Transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        PrepareVisualInstance(visualInstance);

        if (!TryFitVisualToCell(unit, visualInstance.transform, definition, out var fittedBounds))
        {
            if (Application.isPlaying)
            {
                Destroy(visualInstance);
            }
            else
            {
                DestroyImmediate(visualInstance);
            }

            return false;
        }

        unit.VisualRoot = visualInstance.transform;
        unit.Animator = visualInstance.GetComponentInChildren<Animator>(true);
        unit.AnimationBinding = ResolveAnimationBinding(unit.Animator);
        ConfigureAnimator(unit);
        if (unit.Animator != null)
        {
            unit.AnimationEventRelay = unit.Animator.gameObject.GetComponent<HexTacticsAnimationEventRelay>();
            if (unit.AnimationEventRelay == null)
            {
                unit.AnimationEventRelay = unit.Animator.gameObject.AddComponent<HexTacticsAnimationEventRelay>();
            }
        }

        UpdateUnitPresentationMetrics(unit, fittedBounds);
        ConfigureUnitSelectionCollider(unit, fittedBounds);
        return true;
    }

    private UnitAnimationBinding ResolveAnimationBinding(Animator animator)
    {
        if (animator == null)
        {
            return null;
        }

        var controller = animator.runtimeAnimatorController;
        if (controller == null)
        {
            return null;
        }

        var usesParameterDriver =
            AnimatorHasParameter(animator, "Attack1") ||
            AnimatorHasParameter(animator, "Damaged") ||
            AnimatorHasParameter(animator, "Vertical");

        var clips = controller.animationClips;
        var idleClip = ChoosePreferredAnimationClip(clips, GetIdleClipScore);
        var moveClip = ChoosePreferredAnimationClip(clips, GetMoveClipScore);
        var attackClip = ChoosePreferredAnimationClip(clips, GetAttackClipScore);
        var damagedClip = ChoosePreferredAnimationClip(clips, GetDamagedClipScore);
        var deathClip = ChoosePreferredAnimationClip(clips, GetDeathClipScore);

        return new UnitAnimationBinding(
            usesParameterDriver,
            BuildBaseLayerStatePath(idleClip),
            BuildBaseLayerStatePath(moveClip),
            BuildBaseLayerStatePath(attackClip),
            BuildBaseLayerStatePath(damagedClip),
            BuildBaseLayerStatePath(deathClip),
            idleClip,
            moveClip,
            attackClip,
            damagedClip,
            deathClip);
    }

    private static bool AnimatorHasParameter(Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildBaseLayerStatePath(AnimationClip clip)
    {
        return clip != null ? $"Base Layer.{clip.name}" : string.Empty;
    }

    private static AnimationClip ChoosePreferredAnimationClip(IEnumerable<AnimationClip> clips, System.Func<string, int> scoreSelector)
    {
        AnimationClip bestClip = null;
        var bestScore = int.MinValue;
        if (clips == null || scoreSelector == null)
        {
            return null;
        }

        foreach (var clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            var score = scoreSelector(clip.name);
            if (score <= 0)
            {
                continue;
            }

            if (bestClip == null || score > bestScore)
            {
                bestClip = clip;
                bestScore = score;
            }
        }

        return bestClip;
    }

    private static int GetIdleClipScore(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return 0;
        }

        var normalizedName = clipName.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (normalizedName.Contains("idlebattle"))
        {
            return 320;
        }

        if (normalizedName.Contains("idlenormal"))
        {
            return 280;
        }

        if (normalizedName.Contains("idle"))
        {
            return 220;
        }

        return 0;
    }

    private static int GetMoveClipScore(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return 0;
        }

        var normalizedName = clipName.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (normalizedName.Contains("runfwd"))
        {
            return 320;
        }

        if (normalizedName.Contains("run"))
        {
            return 280;
        }

        if (normalizedName.Contains("walkfwd"))
        {
            return 240;
        }

        if (normalizedName.Contains("walk"))
        {
            return 200;
        }

        return 0;
    }

    private static int GetAttackClipScore(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return 0;
        }

        var normalizedName = clipName.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (normalizedName.Contains("attack01"))
        {
            return 320;
        }

        if (normalizedName.Contains("attack1"))
        {
            return 300;
        }

        if (normalizedName.Contains("attack02"))
        {
            return 240;
        }

        if (normalizedName.Contains("attack2"))
        {
            return 220;
        }

        if (normalizedName.Contains("attack"))
        {
            return 160;
        }

        return 0;
    }

    private static int GetDamagedClipScore(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return 0;
        }

        var normalizedName = clipName.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (normalizedName.Contains("gethit"))
        {
            return 320;
        }

        if (normalizedName.Contains("hit"))
        {
            return 240;
        }

        if (normalizedName.Contains("damage"))
        {
            return 200;
        }

        return 0;
    }

    private static int GetDeathClipScore(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return 0;
        }

        var normalizedName = clipName.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (normalizedName.Contains("death"))
        {
            return 320;
        }

        if (normalizedName.Contains("die"))
        {
            return 300;
        }

        if (normalizedName.Contains("dead"))
        {
            return 260;
        }

        return 0;
    }

    private GameObject LoadUnitVisualPrefab(HexTacticsCharacterConfig definition)
    {
        if (definition == null)
        {
            return null;
        }

        if (definition.BattleUnitPrefab != null)
        {
            return definition.BattleUnitPrefab;
        }

        return LoadUnitVisualPrefabFromArchetype(definition.VisualArchetype);
    }

    private void AttachFallbackUnitVisual(HexUnit unit)
    {
        var teamBodyMaterial = unit.Team == Team.Blue ? blueBodyMaterial : redBodyMaterial;
        var teamAccentMaterial = unit.Team == Team.Blue ? blueRingMaterial : redRingMaterial;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(unit.Transform, false);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.62f, 0.55f);
        body.GetComponent<MeshRenderer>().sharedMaterial = teamBodyMaterial;

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(unit.Transform, false);
        head.transform.localPosition = new Vector3(0f, 1.52f, 0f);
        head.transform.localScale = Vector3.one * 0.34f;
        head.GetComponent<MeshRenderer>().sharedMaterial = teamAccentMaterial;

        unit.VisualHeight = 1.78f;
        unit.LabelHeight = 2.12f;
        unit.SelectionRadius = hexRadius * 0.42f;

        RegisterUnitCollider(body.GetComponent<Collider>(), unit);
        RegisterUnitCollider(head.GetComponent<Collider>(), unit);
    }

    private GameObject LoadUnitVisualPrefabFromArchetype(HexTacticsCharacterVisualArchetype archetype)
    {
        if (unitVisualPrefabCache.TryGetValue(archetype, out var cachedPrefab) && cachedPrefab != null)
        {
            return cachedPrefab;
        }

#if UNITY_EDITOR
        if (Application.isEditor)
        {
            var editorPrefab = LoadUnitVisualPrefabFromAssetDatabase(archetype);
            if (editorPrefab != null)
            {
                unitVisualPrefabCache[archetype] = editorPrefab;
                return editorPrefab;
            }

            return null;
        }
#endif

        var address = GetVisualResourcePath(archetype);
        if (string.IsNullOrEmpty(address))
        {
            return null;
        }

        var prefab = HexTacticsAddressables.LoadAsset<GameObject>(address);
        if (prefab != null)
        {
            unitVisualPrefabCache[archetype] = prefab;
        }

        return prefab;
    }

#if UNITY_EDITOR
    private static GameObject LoadUnitVisualPrefabFromAssetDatabase(HexTacticsCharacterVisualArchetype archetype)
    {
        var assetPath = GetVisualAssetPath(archetype);
        return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }
#endif

    private static string GetVisualResourcePath(HexTacticsCharacterVisualArchetype archetype)
    {
        return archetype switch
        {
            HexTacticsCharacterVisualArchetype.Stag => "BattleUnits/Stag",
            HexTacticsCharacterVisualArchetype.Doe => "BattleUnits/Doe",
            HexTacticsCharacterVisualArchetype.Elk => "BattleUnits/Elk",
            HexTacticsCharacterVisualArchetype.Fawn => "BattleUnits/Fawn",
            HexTacticsCharacterVisualArchetype.Tiger => "BattleUnits/Tiger",
            HexTacticsCharacterVisualArchetype.WhiteTiger => "BattleUnits/WhiteTiger",
            _ => string.Empty
        };
    }

    private static string GetVisualAssetPath(HexTacticsCharacterVisualArchetype archetype)
    {
        return archetype switch
        {
            HexTacticsCharacterVisualArchetype.Stag => HexTacticsAssetPaths.BattleUnitFolder + "/Stag.prefab",
            HexTacticsCharacterVisualArchetype.Doe => HexTacticsAssetPaths.BattleUnitFolder + "/Doe.prefab",
            HexTacticsCharacterVisualArchetype.Elk => HexTacticsAssetPaths.BattleUnitFolder + "/Elk.prefab",
            HexTacticsCharacterVisualArchetype.Fawn => HexTacticsAssetPaths.BattleUnitFolder + "/Fawn.prefab",
            HexTacticsCharacterVisualArchetype.Tiger => HexTacticsAssetPaths.BattleUnitFolder + "/Tiger.prefab",
            HexTacticsCharacterVisualArchetype.WhiteTiger => HexTacticsAssetPaths.BattleUnitFolder + "/WhiteTiger.prefab",
            _ => string.Empty
        };
    }

    private float GetTargetVisualHeight(HexTacticsCharacterConfig definition)
    {
        if (definition != null && definition.BattleUnitPrefab != null)
        {
            return baseUnitVisualHeight * Mathf.Max(0.4f, definition.VisualHeightScale);
        }

        return GetDefaultTargetVisualHeight(definition != null
            ? definition.VisualArchetype
            : HexTacticsCharacterVisualArchetype.Stag);
    }

    private float GetDefaultTargetVisualHeight(HexTacticsCharacterVisualArchetype archetype)
    {
        return archetype switch
        {
            HexTacticsCharacterVisualArchetype.Elk => baseUnitVisualHeight * 1.20f,
            HexTacticsCharacterVisualArchetype.Stag => baseUnitVisualHeight * 1.08f,
            HexTacticsCharacterVisualArchetype.Doe => baseUnitVisualHeight * 0.98f,
            HexTacticsCharacterVisualArchetype.Fawn => baseUnitVisualHeight * 0.76f,
            HexTacticsCharacterVisualArchetype.Tiger => baseUnitVisualHeight * 0.96f,
            HexTacticsCharacterVisualArchetype.WhiteTiger => baseUnitVisualHeight * 1.02f,
            _ => baseUnitVisualHeight
        };
    }

    private void PrepareVisualInstance(GameObject visualInstance)
    {
        foreach (var collider in visualInstance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (var rigidBody in visualInstance.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidBody.useGravity = false;
            rigidBody.isKinematic = true;
            rigidBody.detectCollisions = false;
        }

        foreach (var trailRenderer in visualInstance.GetComponentsInChildren<TrailRenderer>(true))
        {
            trailRenderer.enabled = false;
        }

        foreach (var lineRenderer in visualInstance.GetComponentsInChildren<LineRenderer>(true))
        {
            lineRenderer.enabled = false;
        }

        foreach (var particleSystem in visualInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.gameObject.SetActive(false);
        }

        foreach (var audioSource in visualInstance.GetComponentsInChildren<AudioSource>(true))
        {
            audioSource.enabled = false;
        }
    }

    private bool TryFitVisualToCell(HexUnit unit, Transform visualRoot, HexTacticsCharacterConfig definition, out Bounds fittedBounds)
    {
        fittedBounds = default;
        if (!TryGetRenderableBounds(visualRoot, out var initialBounds))
        {
            return false;
        }

        var targetHeight = GetTargetVisualHeight(definition);
        var safeHeight = Mathf.Max(0.01f, initialBounds.size.y);
        var scaleFactor = targetHeight / safeHeight;
        visualRoot.localScale *= scaleFactor;

        if (!TryGetRenderableBounds(visualRoot, out var scaledBounds))
        {
            return false;
        }

        var anchor = unit.Transform.position;
        var offset = new Vector3(
            anchor.x - scaledBounds.center.x,
            anchor.y - scaledBounds.min.y,
            anchor.z - scaledBounds.center.z);
        visualRoot.position += offset;

        if (!TryGetRenderableBounds(visualRoot, out fittedBounds))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetRenderableBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private void UpdateUnitPresentationMetrics(HexUnit unit, Bounds bounds)
    {
        unit.VisualHeight = Mathf.Max(baseUnitVisualHeight * 0.75f, bounds.max.y - unit.Transform.position.y);
        unit.LabelHeight = unit.VisualHeight + worldLabelPadding;
        unit.SelectionRadius = Mathf.Clamp(
            Mathf.Max(bounds.extents.x, bounds.extents.z) * selectionFootprintPadding,
            hexRadius * 0.26f,
            hexRadius * 0.58f);
    }

    private void ConfigureUnitSelectionCollider(HexUnit unit, Bounds bounds)
    {
        var collider = unit.Transform.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = unit.Transform.gameObject.AddComponent<CapsuleCollider>();
        }

        var centerWorld = new Vector3(unit.Transform.position.x, bounds.center.y, unit.Transform.position.z);
        collider.center = unit.Transform.InverseTransformPoint(centerWorld);
        collider.radius = unit.SelectionRadius;
        collider.height = Mathf.Max(0.95f, bounds.size.y);
        collider.direction = 1;
        collider.isTrigger = false;
        collider.enabled = true;
        RegisterUnitCollider(collider, unit);
    }

    private void ApplyRingScale(HexUnit unit)
    {
        if (unit?.RingRenderer == null)
        {
            return;
        }

        var ringScale = Mathf.Clamp((unit.SelectionRadius / hexRadius) * 1.05f, 0.24f, 0.52f);
        unit.RingRenderer.transform.localPosition = new Vector3(0f, -unitHoverHeight * 0.78f, 0f);
        unit.RingRenderer.transform.localScale = new Vector3(ringScale, 0.02f, ringScale);
    }

    private static Vector3 GetTeamFacingDirection(Team team)
    {
        return team == Team.Blue ? Vector3.right : Vector3.left;
    }

    private List<HexCoord> GetDeploySlots(Team team)
    {
        var allCoords = new List<HexCoord>(cells.Keys);
        allCoords.Sort((left, right) =>
        {
            var leftWorld = HexToWorld(left);
            var rightWorld = HexToWorld(right);
            var xCompare = leftWorld.x.CompareTo(rightWorld.x);
            if (xCompare != 0)
            {
                return xCompare;
            }

            return leftWorld.z.CompareTo(rightWorld.z);
        });

        var blueSlotCount = (allCoords.Count + 1) / 2;
        var deploySlots = team == Team.Blue
            ? allCoords.GetRange(0, blueSlotCount)
            : allCoords.GetRange(blueSlotCount, allCoords.Count - blueSlotCount);

        deploySlots.Sort((left, right) =>
        {
            var leftWorld = HexToWorld(left);
            var rightWorld = HexToWorld(right);

            var sideCompare = team == Team.Blue
                ? leftWorld.x.CompareTo(rightWorld.x)
                : rightWorld.x.CompareTo(leftWorld.x);
            if (sideCompare != 0)
            {
                return sideCompare;
            }

            var spreadCompare = Mathf.Abs(rightWorld.z).CompareTo(Mathf.Abs(leftWorld.z));
            if (spreadCompare != 0)
            {
                return spreadCompare;
            }

            return leftWorld.z.CompareTo(rightWorld.z);
        });
        return deploySlots;
    }

}
