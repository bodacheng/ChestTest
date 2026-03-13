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

    private void ConfigureCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var framingPoints = CollectCameraFramingPoints();
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

        var requiredDistance = cameraMinDistance;
        foreach (var point in framingPoints)
        {
            var localPoint = inverseRotation * (point - focus);
            localPoint.x *= cameraFitPadding;
            localPoint.y *= cameraFitPadding;

            requiredDistance = Mathf.Max(requiredDistance, Mathf.Abs(localPoint.x) / tanHorizontal - localPoint.z);
            requiredDistance = Mathf.Max(requiredDistance, Mathf.Abs(localPoint.y) / tanVertical - localPoint.z);
        }

        var forward = rotation * Vector3.forward;
        mainCamera.transform.SetPositionAndRotation(focus - forward * requiredDistance, rotation);
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = Mathf.Max(200f, requiredDistance * 6f);
        mainCamera.fieldOfView = cameraFieldOfView;
        mainCamera.backgroundColor = new Color(0.72f, 0.79f, 0.78f);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
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
        RefreshVisuals();
    }

    private void ClearUnits()
    {
        unitLookups.Clear();
        units.Clear();
        DestroyChildren(unitsRoot);

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

        var definition = characterRoster[rosterIndex];
        if (GetTeamCost(playerTeamSelection) + definition.cost > playerTeamCostLimit)
        {
            builderStatus = "已超过队伍总 cost 上限";
            return;
        }

        playerTeamSelection.Add(rosterIndex);
        builderStatus = string.Empty;
    }

    private void RemovePlayerCharacterAt(int selectionIndex)
    {
        if (selectionIndex < 0 || selectionIndex >= playerTeamSelection.Count)
        {
            return;
        }

        playerTeamSelection.RemoveAt(selectionIndex);
        builderStatus = string.Empty;
    }

    private bool TryStartCpuBattle()
    {
        if (playerTeamSelection.Count == 0)
        {
            builderStatus = "至少选择 1 名角色才能开始";
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
                if (characterRoster[i].cost <= remainingCost)
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
            remainingCost -= characterRoster[pick].cost;
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

        SpawnConfiguredTeam(playerTeamSelection, Team.Blue, GetDeploySlots(Team.Blue), "玩家");
        SpawnConfiguredTeam(cpuTeamSelection, Team.Red, GetDeploySlots(Team.Red), "CPU");
        BeginPlanningRound();
    }

    private void BeginPlanningRound()
    {
        currentFlowState = FlowState.Planning;
        isResolving = false;
        isAnimating = false;
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();

        foreach (var unit in units)
        {
            AssignWaitCommand(unit);
        }

        RefreshVisuals();
    }

}
