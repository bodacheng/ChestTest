using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            var coord = deploySlots[i];
            if (!cells.TryGetValue(coord, out var cell) || cell.Occupant != null)
            {
                continue;
            }

            var unitRoot = new GameObject(CreateBattleUnitName(ownerLabel, definition, displayCounts));
            unitRoot.transform.SetParent(unitsRoot, false);
            unitRoot.transform.localPosition = CellToUnitPosition(coord);

            var ring = new GameObject("Ring");
            ring.transform.SetParent(unitRoot.transform, false);
            ring.transform.localPosition = new Vector3(0f, -unitHoverHeight * 0.55f, 0f);
            ring.transform.localScale = new Vector3(0.58f, 0.16f, 0.58f);
            var ringFilter = ring.AddComponent<MeshFilter>();
            ringFilter.sharedMesh = cellMesh;
            var ringRenderer = ring.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = team == Team.Blue ? blueRingMaterial : redRingMaterial;

            var unit = new HexUnit(
                unitRoot.name,
                definition.displayName,
                team,
                coord,
                unitRoot.transform,
                ringRenderer,
                team == Team.Blue ? blueRingMaterial : redRingMaterial,
                definition.maxHealth,
                definition.attackPower,
                definition.cost,
                definition.moveRange);

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
    }

    private static string CreateBattleUnitName(string ownerLabel, CharacterDefinition definition, Dictionary<string, int> displayCounts)
    {
        if (!displayCounts.TryGetValue(definition.displayName, out var count))
        {
            count = 0;
        }

        count++;
        displayCounts[definition.displayName] = count;
        return $"{ownerLabel} {definition.displayName} {count}";
    }

    private void RegisterUnitCollider(Collider collider, HexUnit unit)
    {
        if (collider != null)
        {
            unitLookups[collider] = unit;
        }
    }

    private bool TryAttachAnimatedVisual(HexUnit unit, CharacterDefinition definition)
    {
        var prefab = LoadUnitVisualPrefab(definition.visualArchetype);
        if (prefab == null)
        {
            return false;
        }

        var visualInstance = Instantiate(prefab);
        visualInstance.name = $"{definition.visualArchetype} Visual";
        visualInstance.transform.SetParent(unit.Transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        PrepareVisualInstance(visualInstance);

        if (!TryFitVisualToCell(unit, visualInstance.transform, definition.visualArchetype, out var fittedBounds))
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
        ConfigureAnimator(unit.Animator);
        UpdateUnitPresentationMetrics(unit, fittedBounds);
        ConfigureUnitSelectionCollider(unit, fittedBounds);
        return true;
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

    private GameObject LoadUnitVisualPrefab(UnitVisualArchetype archetype)
    {
        if (unitVisualPrefabCache.TryGetValue(archetype, out var cachedPrefab) && cachedPrefab != null)
        {
            return cachedPrefab;
        }

        var resourcePath = GetVisualResourcePath(archetype);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null)
        {
            unitVisualPrefabCache[archetype] = prefab;
        }

        return prefab;
    }

    private static string GetVisualResourcePath(UnitVisualArchetype archetype)
    {
        return archetype switch
        {
            UnitVisualArchetype.Stag => "BattleUnits/Stag",
            UnitVisualArchetype.Doe => "BattleUnits/Doe",
            UnitVisualArchetype.Elk => "BattleUnits/Elk",
            UnitVisualArchetype.Fawn => "BattleUnits/Fawn",
            UnitVisualArchetype.Tiger => "BattleUnits/Tiger",
            UnitVisualArchetype.WhiteTiger => "BattleUnits/WhiteTiger",
            _ => string.Empty
        };
    }

    private float GetTargetVisualHeight(UnitVisualArchetype archetype)
    {
        return archetype switch
        {
            UnitVisualArchetype.Elk => baseUnitVisualHeight * 1.20f,
            UnitVisualArchetype.Stag => baseUnitVisualHeight * 1.08f,
            UnitVisualArchetype.Doe => baseUnitVisualHeight * 0.98f,
            UnitVisualArchetype.Fawn => baseUnitVisualHeight * 0.76f,
            UnitVisualArchetype.Tiger => baseUnitVisualHeight * 0.96f,
            UnitVisualArchetype.WhiteTiger => baseUnitVisualHeight * 1.02f,
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

    private bool TryFitVisualToCell(HexUnit unit, Transform visualRoot, UnitVisualArchetype archetype, out Bounds fittedBounds)
    {
        fittedBounds = default;
        if (!TryGetRenderableBounds(visualRoot, out var initialBounds))
        {
            return false;
        }

        var targetHeight = GetTargetVisualHeight(archetype);
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
        var ringScale = Mathf.Clamp((unit.SelectionRadius / hexRadius) * 1.35f, 0.42f, 0.92f);
        unit.RingRenderer.transform.localPosition = new Vector3(0f, -unitHoverHeight * 0.55f, 0f);
        unit.RingRenderer.transform.localScale = new Vector3(ringScale, 0.16f, ringScale);
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
