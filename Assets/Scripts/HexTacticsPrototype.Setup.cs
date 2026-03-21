using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    private Material CreateLitMaterial(Color color, Color emission)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        runtimeMaterials.Add(material);
        return material;
    }

    private void BuildBoard()
    {
        var platform = new GameObject("Platform");
        platform.transform.SetParent(boardRoot, false);
        platform.transform.localPosition = new Vector3(0f, -tileHeight * 0.72f, 0f);

        var platformFilter = platform.AddComponent<MeshFilter>();
        platformFilter.sharedMesh = BuildHexPrismMesh(hexRadius * (boardRadius * 1.9f + 1.5f), tileHeight * 1.25f);
        var platformRenderer = platform.AddComponent<MeshRenderer>();
        platformRenderer.sharedMaterial = platformMaterial;

        for (var q = -boardRadius; q <= boardRadius; q++)
        {
            var minR = Mathf.Max(-boardRadius, -q - boardRadius);
            var maxR = Mathf.Min(boardRadius, -q + boardRadius);

            for (var r = minR; r <= maxR; r++)
            {
                var coord = new HexCoord(q, r);
                var cellObject = new GameObject($"Hex {q},{r}");
                cellObject.transform.SetParent(boardRoot, false);
                cellObject.transform.localPosition = HexToWorld(coord);

                var filter = cellObject.AddComponent<MeshFilter>();
                filter.sharedMesh = cellMesh;

                var renderer = cellObject.AddComponent<MeshRenderer>();
                var baseMaterial = ((q - r) & 1) == 0 ? tilePrimaryMaterial : tileSecondaryMaterial;
                renderer.sharedMaterial = baseMaterial;

                var collider = cellObject.AddComponent<MeshCollider>();
                collider.sharedMesh = cellMesh;

                var cell = new HexCell(coord, renderer, baseMaterial);
                cells.Add(coord, cell);
                cellLookups.Add(collider, cell);
            }
        }
    }

    private void ConfigureCamera(bool immediate = false)
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        const float attackZoomBlend = 0.18f;

        var framingPoints = ResolveCameraFramingPoints();
        if (framingPoints.Count == 0)
        {
            return;
        }

        var bounds = new Bounds(framingPoints[0], Vector3.zero);
        for (var i = 1; i < framingPoints.Count; i++)
        {
            bounds.Encapsulate(framingPoints[i]);
        }

        var rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        var inverseRotation = Quaternion.Inverse(rotation);
        var focus = bounds.center;
        var verticalHalfFov = cameraFieldOfView * Mathf.Deg2Rad * 0.5f;
        var tanVertical = Mathf.Tan(verticalHalfFov);
        var tanHorizontal = tanVertical * mainCamera.aspect;
        var viewportMargins = GetViewportMargins();
        var safeWidth = Mathf.Max(0.58f, 1f - viewportMargins.x - viewportMargins.y);
        var safeHeight = Mathf.Max(0.68f, 1f - viewportMargins.z - viewportMargins.w);
        var safeTanHorizontal = tanHorizontal * safeWidth;
        var safeTanVertical = tanVertical * safeHeight;

        var conservativeMinScale = Mathf.Max(0.9f, attackCameraMinDistanceScale);
        var conservativeDistanceScale = Mathf.Max(0.92f, attackCameraDistanceScale);
        var attackMinDistanceScale = Mathf.Lerp(1f, conservativeMinScale, attackZoomBlend);
        var attackDistanceScale = Mathf.Lerp(1f, conservativeDistanceScale, attackZoomBlend);
        var minimumDistance = activeCameraFocusOverride != null
            ? cameraMinDistance * attackMinDistanceScale
            : cameraMinDistance;
        var requiredDistance = minimumDistance;
        foreach (var point in framingPoints)
        {
            var localPoint = inverseRotation * (point - focus);
            localPoint.x *= cameraFitPadding;
            localPoint.y *= cameraFitPadding;

            requiredDistance = Mathf.Max(requiredDistance, Mathf.Abs(localPoint.x) / safeTanHorizontal - localPoint.z);
            requiredDistance = Mathf.Max(requiredDistance, Mathf.Abs(localPoint.y) / safeTanVertical - localPoint.z);
        }

        if (activeCameraFocusOverride != null)
        {
            requiredDistance = Mathf.Max(minimumDistance, requiredDistance * attackDistanceScale);
        }

        var focusOffset = new Vector3(
            (viewportMargins.y - viewportMargins.x) * tanHorizontal * requiredDistance * 0.9f,
            (viewportMargins.z - viewportMargins.w) * tanVertical * requiredDistance * 0.5f,
            0f);
        focus += rotation * focusOffset;

        var forward = rotation * Vector3.forward;
        SetCameraTarget(
            mainCamera,
            focus - forward * requiredDistance,
            rotation,
            cameraFieldOfView,
            Mathf.Max(200f, requiredDistance * 6f),
            immediate);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastCameraFlowState = currentFlowState;
    }

    private List<Vector3> ResolveCameraFramingPoints()
    {
        return TryCollectAttackCameraFramingPoints(out var focusedPoints)
            ? focusedPoints
            : CollectCameraFramingPoints();
    }

    private bool TryCollectAttackCameraFramingPoints(out List<Vector3> points)
    {
        points = null;
        if (activeCameraFocusOverride == null)
        {
            return false;
        }

        var attacker = activeCameraFocusOverride.Attacker;
        var defender = activeCameraFocusOverride.Defender;
        if (attacker?.Transform == null || defender?.Transform == null)
        {
            activeCameraFocusOverride = null;
            return false;
        }

        points = new List<Vector3>(12);
        AddUnitCameraFramingPoints(points, attacker, 1.16f, 1.08f);
        AddUnitCameraFramingPoints(points, defender, 1.16f, 1.08f);

        var attackerCenter = attacker.Transform.position;
        var defenderCenter = defender.Transform.position;
        var midpoint = Vector3.Lerp(attackerCenter, defenderCenter, 0.5f) + Vector3.up * attackCameraFocusLift;
        var direction = defenderCenter - attackerCenter;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.right;
        }

        var lateral = Vector3.Cross(Vector3.up, direction.normalized);
        var spread = Mathf.Max(hexRadius * 0.82f, direction.magnitude * 0.36f);
        var longitudinal = direction.normalized * Mathf.Max(hexRadius * 0.46f, direction.magnitude * 0.22f);
        points.Add(midpoint);
        points.Add(midpoint + lateral * spread);
        points.Add(midpoint - lateral * spread);
        points.Add(midpoint + longitudinal);
        points.Add(midpoint - longitudinal);
        return true;
    }

    private void SetCameraTarget(Camera mainCamera, Vector3 position, Quaternion rotation, float fieldOfView, float farClipPlane, bool immediate)
    {
        cameraTargetPosition = position;
        cameraTargetRotation = rotation;
        cameraTargetFieldOfView = fieldOfView;
        cameraTargetFarClipPlane = farClipPlane;
        hasCameraTarget = true;
        if (immediate || !Application.isPlaying || !cameraTransformInitialized)
        {
            cameraPositionVelocity = Vector3.zero;
            cameraFovVelocity = 0f;
            mainCamera.transform.SetPositionAndRotation(cameraTargetPosition, cameraTargetRotation);
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = cameraTargetFarClipPlane;
            mainCamera.fieldOfView = cameraTargetFieldOfView;
            mainCamera.backgroundColor = new Color(0.72f, 0.79f, 0.78f);
            cameraTransformInitialized = true;
        }
    }

    private void UpdateCameraTransform()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null || !hasCameraTarget)
        {
            return;
        }

        if (!Application.isPlaying || !cameraTransformInitialized)
        {
            mainCamera.transform.SetPositionAndRotation(cameraTargetPosition, cameraTargetRotation);
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = cameraTargetFarClipPlane;
            mainCamera.fieldOfView = cameraTargetFieldOfView;
            mainCamera.backgroundColor = new Color(0.72f, 0.79f, 0.78f);
            cameraTransformInitialized = true;
            return;
        }

        var deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        var rotationBlend = 1f - Mathf.Exp(-cameraRotationSmoothness * deltaTime);
        var position = Vector3.SmoothDamp(
            mainCamera.transform.position,
            cameraTargetPosition,
            ref cameraPositionVelocity,
            cameraMoveSmoothTime,
            Mathf.Infinity,
            deltaTime);
        var rotation = Quaternion.Slerp(mainCamera.transform.rotation, cameraTargetRotation, rotationBlend);
        var fieldOfView = Mathf.SmoothDamp(
            mainCamera.fieldOfView,
            cameraTargetFieldOfView,
            ref cameraFovVelocity,
            cameraZoomSmoothTime,
            Mathf.Infinity,
            deltaTime);

        mainCamera.transform.SetPositionAndRotation(position, rotation);
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = Mathf.Lerp(mainCamera.farClipPlane, cameraTargetFarClipPlane, rotationBlend);
        mainCamera.fieldOfView = fieldOfView;
        mainCamera.backgroundColor = new Color(0.72f, 0.79f, 0.78f);
    }

    private Vector4 GetViewportMargins()
    {
        var isPortrait = Screen.height > Screen.width * 1.05f;
        return currentFlowState switch
        {
            FlowState.TeamBuilder => isPortrait ? new Vector4(0.05f, 0.05f, 0.14f, 0.34f) : new Vector4(0.22f, 0.24f, 0.08f, 0.06f),
            FlowState.Planning => isPortrait ? new Vector4(0.06f, 0.06f, 0.06f, 0.30f) : new Vector4(0.26f, 0.04f, 0.06f, 0.04f),
            FlowState.Resolving => isPortrait ? new Vector4(0.06f, 0.06f, 0.20f, 0.05f) : new Vector4(0.22f, 0.04f, 0.06f, 0.04f),
            FlowState.Victory => new Vector4(0.08f, 0.08f, 0.06f, 0.06f),
            _ => new Vector4(0.06f, 0.06f, 0.05f, 0.05f)
        };
    }

    private void StartCpuMode()
    {
        currentFlowState = FlowState.TeamBuilder;
        builderStatus = string.Empty;
        ReturnToNonBattleBoardState();
    }

    private void ReturnToModeSelect()
    {
        currentFlowState = FlowState.ModeSelect;
        builderStatus = string.Empty;
        ReturnToNonBattleBoardState();
    }

    private void ReturnToTeamBuilder()
    {
        currentFlowState = FlowState.TeamBuilder;
        builderStatus = string.Empty;
        ReturnToNonBattleBoardState();
    }

    private void ReturnToNonBattleBoardState()
    {
        activeCameraFocusOverride = null;
        ClearUnits();
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();
        winningTeam = null;
        lastResolutionKind = ResolutionKind.None;
        resolvedTurnCount = 0;
        planningRoundNumber = 1;
        isAnimating = false;
        isResolving = false;
        resolutionStatus = string.Empty;
        if (currentFlowState == FlowState.TeamBuilder)
        {
            SpawnTeamBuilderPreviewUnits();
        }

        ConfigureCamera();
        RefreshVisuals();
    }

    private void ClearUnits()
    {
        activeCameraFocusOverride = null;
        unitLookups.Clear();
        units.Clear();
        nextUnitId = 1;
        nextHitEffectVariantIndex = 0;
        DestroyChildren(unitsRoot);
        DestroyChildren(effectsRoot);

        foreach (var cell in cells.Values)
        {
            cell.Occupant = null;
        }
    }

    private void TryAddCharacterToPlayerTeam(int rosterIndex)
    {
        if (currentFlowState != FlowState.TeamBuilder || !IsValidRosterIndex(rosterIndex))
        {
            return;
        }

        if (!TryGetNextAvailableBlueDeploySlot(out var target))
        {
            builderStatus = "蓝方部署区已经放满了";
            return;
        }

        TryPlacePlayerDeployment(rosterIndex, target);
    }

    private void RemovePlayerCharacterAt(int entryId)
    {
        for (var i = 0; i < playerDeploymentEntries.Count; i++)
        {
            if (playerDeploymentEntries[i].EntryId != entryId)
            {
                continue;
            }

            playerDeploymentEntries.RemoveAt(i);
            builderStatus = string.Empty;
            RefreshTeamBuilderPreview();
            return;
        }

        builderStatus = "未找到要移除的布阵角色";
    }

    private bool TryStartCpuBattle()
    {
        if (playerDeploymentEntries.Count == 0)
        {
            builderStatus = "至少放置 1 名角色到蓝方部署区才能开始";
            return false;
        }

        if (!BuildCpuTeamSelection())
        {
            builderStatus = "CPU 无法在当前预算下组成队伍，请调整角色配置";
            return false;
        }

        StartBattleFromSelections();
        return true;
    }

    private bool BuildCpuTeamSelection()
    {
        cpuTeamSelection.Clear();

        var remainingCost = cpuTeamCostLimit;
        while (true)
        {
            var affordable = new List<int>();
            for (var i = 0; i < characterRoster.Count; i++)
            {
                var config = characterRoster[i];
                if (config != null && config.Cost <= remainingCost)
                {
                    affordable.Add(i);
                }
            }

            if (affordable.Count == 0)
            {
                break;
            }

            var pick = affordable[Random.Range(0, affordable.Count)];
            cpuTeamSelection.Add(pick);
            remainingCost -= characterRoster[pick].Cost;
        }

        return cpuTeamSelection.Count > 0;
    }

    private void StartBattleFromSelections()
    {
        ClearUnits();
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();
        winningTeam = null;
        planningRoundNumber = 1;
        resolvedTurnCount = 0;
        lastResolutionKind = ResolutionKind.None;
        resolutionStatus = "第 1 轮计划开始";
        isAnimating = false;
        isResolving = false;

        SpawnConfiguredPlayerTeam();
        SpawnConfiguredTeam(cpuTeamSelection, Team.Red, redDeploySlots, "CPU");
        BeginPlanningRound();
    }

    private void BeginPlanningRound()
    {
        currentFlowState = FlowState.Planning;
        isResolving = false;
        isAnimating = false;
        selectedUnit = null;
        CloseSkillPopup();
        ResetPlanningPointerTracking();
        skillPopupHoveredSkillIndex = -1;
        moveCells.Clear();
        attackCells.Clear();

        foreach (var unit in units)
        {
            AssignWaitCommand(unit, markAsAssigned: false);
            unit.HasManualSkillOverride = false;
            EnsureUnitSelectedSkillUsable(unit);
        }

        SelectUnit(FindFirstBlueUnitWithoutCommand());
    }

    private void CacheDeploySlots()
    {
        blueDeploySlots.Clear();
        redDeploySlots.Clear();
        blueDeploySlotLookup.Clear();

        blueDeploySlots.AddRange(GetDeploySlots(Team.Blue));
        redDeploySlots.AddRange(GetDeploySlots(Team.Red));

        foreach (var coord in blueDeploySlots)
        {
            blueDeploySlotLookup.Add(coord);
        }
    }

    private void SpawnConfiguredPlayerTeam()
    {
        var displayCounts = new Dictionary<string, int>();
        foreach (var entry in playerDeploymentEntries)
        {
            if (entry.Definition == null)
            {
                continue;
            }

            SpawnConfiguredCharacter(entry.Definition, Team.Blue, entry.Coord, "玩家", displayCounts);
        }
    }

    private void SpawnTeamBuilderPreviewUnits()
    {
        if (currentFlowState != FlowState.TeamBuilder)
        {
            return;
        }

        SpawnConfiguredPlayerTeam();
    }

    private void RefreshTeamBuilderPreview()
    {
        if (currentFlowState != FlowState.TeamBuilder)
        {
            return;
        }

        ClearUnits();
        SpawnTeamBuilderPreviewUnits();
        ConfigureCamera();
        RefreshVisuals();
    }

    private bool TryPlacePlayerDeployment(int rosterIndex, HexCoord target)
    {
        if (currentFlowState != FlowState.TeamBuilder || !IsValidRosterIndex(rosterIndex))
        {
            return false;
        }

        if (!blueDeploySlotLookup.Contains(target))
        {
            builderStatus = "请把角色放到蓝方高亮部署区";
            return false;
        }

        if (TryGetPlayerDeploymentEntryAt(target, out _))
        {
            builderStatus = "该部署格已经有角色了";
            return false;
        }

        var definition = characterRoster[rosterIndex];
        if (definition == null)
        {
            builderStatus = "角色配置丢失，无法放置";
            return false;
        }

        if (GetPlayerTeamCost() + definition.Cost > playerTeamCostLimit)
        {
            builderStatus = "已超过队伍总 cost 上限";
            return false;
        }

        playerDeploymentEntries.Add(new PlayerDeploymentEntry(nextPlayerDeploymentEntryId++, definition, target));
        builderStatus = string.Empty;
        RefreshTeamBuilderPreview();
        return true;
    }

    private bool TryMovePlayerDeployment(int entryId, HexCoord target)
    {
        if (currentFlowState != FlowState.TeamBuilder || !blueDeploySlotLookup.Contains(target))
        {
            builderStatus = "请把角色放到蓝方高亮部署区";
            return false;
        }

        var entry = FindPlayerDeploymentEntry(entryId);
        if (entry == null)
        {
            builderStatus = "未找到要移动的角色";
            return false;
        }

        if (entry.Coord == target)
        {
            builderStatus = string.Empty;
            return true;
        }

        if (TryGetPlayerDeploymentEntryAt(target, out _))
        {
            builderStatus = "目标部署格已经被占用";
            return false;
        }

        entry.Coord = target;
        builderStatus = string.Empty;
        RefreshTeamBuilderPreview();
        return true;
    }

    private bool TryPlacePlayerDeploymentFromScreenPoint(int rosterIndex, Vector2 screenPosition)
    {
        return TryResolveBlueDeployCellFromScreenPoint(screenPosition, out var target) && TryPlacePlayerDeployment(rosterIndex, target);
    }

    private bool TryMovePlayerDeploymentFromScreenPoint(int entryId, Vector2 screenPosition)
    {
        return TryResolveBlueDeployCellFromScreenPoint(screenPosition, out var target) && TryMovePlayerDeployment(entryId, target);
    }

    private bool TryResolveBlueDeployCellFromScreenPoint(Vector2 screenPosition, out HexCoord target)
    {
        target = default;

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            builderStatus = "当前没有可用的主相机";
            return false;
        }

        var ray = mainCamera.ScreenPointToRay(screenPosition);
        var hits = Physics.RaycastAll(ray, 200f);
        if (hits.Length == 0)
        {
            builderStatus = "请把角色拖到棋盘上的蓝方部署格";
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (var hit in hits)
        {
            if (!cellLookups.TryGetValue(hit.collider, out var cell))
            {
                continue;
            }

            if (!blueDeploySlotLookup.Contains(cell.Coord))
            {
                builderStatus = "只能放置到蓝方部署区";
                return false;
            }

            target = cell.Coord;
            return true;
        }

        builderStatus = "请把角色拖到蓝方部署格";
        return false;
    }

    private bool TryGetNextAvailableBlueDeploySlot(out HexCoord target)
    {
        foreach (var coord in blueDeploySlots)
        {
            if (!TryGetPlayerDeploymentEntryAt(coord, out _))
            {
                target = coord;
                return true;
            }
        }

        target = default;
        return false;
    }

    private PlayerDeploymentEntry FindPlayerDeploymentEntry(int entryId)
    {
        foreach (var entry in playerDeploymentEntries)
        {
            if (entry.EntryId == entryId)
            {
                return entry;
            }
        }

        return null;
    }

    private bool TryGetPlayerDeploymentEntryAt(HexCoord coord, out PlayerDeploymentEntry entry)
    {
        foreach (var candidate in playerDeploymentEntries)
        {
            if (candidate.Coord == coord)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

}
