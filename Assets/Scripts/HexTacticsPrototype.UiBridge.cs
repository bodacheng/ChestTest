using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    public HexTacticsUiSnapshot BuildUiSnapshot()
    {
        var snapshot = new HexTacticsUiSnapshot
        {
            FlowState = ConvertFlowState(currentFlowState),
            PlanningRoundNumber = planningRoundNumber,
            ResolvedTurnCount = resolvedTurnCount,
            BlueAliveCount = CountAliveUnits(Team.Blue),
            RedAliveCount = CountAliveUnits(Team.Red),
            PlayerCostLimit = playerTeamCostLimit,
            CpuCostLimit = cpuTeamCostLimit,
            PlayerUsedCost = GetTeamCost(playerTeamSelection),
            BuilderStatus = builderStatus ?? string.Empty,
            ResolutionStatus = resolutionStatus ?? string.Empty,
            TurnTypeLabel = GetTurnTypeLabel(),
            HasSelectedUnit = selectedUnit != null,
            CanStartBattle = playerTeamSelection.Count > 0 && GetTeamCost(playerTeamSelection) <= playerTeamCostLimit,
            SelectedUnitSummary = BuildSelectedUnitSummary(),
            VictorySummary = winningTeam.HasValue ? $"{TeamDisplayName(winningTeam.Value)}完成歼灭" : string.Empty
        };

        for (var i = 0; i < characterRoster.Count; i++)
        {
            var definition = characterRoster[i];
            snapshot.RosterEntries.Add(new HexTacticsRosterEntryUiData(
                i,
                definition.displayName,
                definition.description,
                definition.maxHealth,
                definition.attackPower,
                definition.moveRange,
                definition.cost,
                snapshot.PlayerUsedCost + definition.cost <= playerTeamCostLimit));
        }

        for (var i = 0; i < playerTeamSelection.Count; i++)
        {
            var rosterIndex = playerTeamSelection[i];
            if (!IsValidRosterIndex(rosterIndex))
            {
                continue;
            }

            var definition = characterRoster[rosterIndex];
            snapshot.PlayerSelectionEntries.Add(new HexTacticsSelectionEntryUiData(
                i,
                definition.displayName,
                definition.maxHealth,
                definition.attackPower,
                definition.moveRange,
                definition.cost));
        }

        foreach (var unit in units)
        {
            if (unit.Team == Team.Blue)
            {
                snapshot.PlayerCommandEntries.Add(new HexTacticsCommandEntryUiData(
                    unit.Id,
                    unit.Name,
                    DescribeCommand(unit, compact: false),
                    selectedUnit == unit));
            }

            if (currentFlowState == FlowState.Planning || currentFlowState == FlowState.Resolving || currentFlowState == FlowState.Victory)
            {
                snapshot.WorldLabels.Add(new HexTacticsWorldLabelUiData(
                    unit.Id,
                    unit.Transform.position + new Vector3(0f, unit.LabelHeight, 0f),
                    unit.Name,
                    currentFlowState == FlowState.Planning
                        ? DescribeCommand(unit, compact: true)
                        : $"HP {unit.CurrentHealth}/{unit.MaxHealth}  ATK {unit.AttackPower}  MOVE {unit.MoveRange}",
                    unit.Team == Team.Blue));
            }
        }

        return snapshot;
    }

    public void UiStartCpuMode()
    {
        StartCpuMode();
    }

    public void UiReturnToModeSelect()
    {
        ReturnToModeSelect();
    }

    public void UiAddCharacterToPlayerTeam(int rosterIndex)
    {
        TryAddCharacterToPlayerTeam(rosterIndex);
    }

    public void UiRemovePlayerCharacterAt(int selectionIndex)
    {
        RemovePlayerCharacterAt(selectionIndex);
    }

    public void UiTryStartCpuBattle()
    {
        TryStartCpuBattle();
    }

    public void UiClearSelection()
    {
        SelectUnit(null);
    }

    public void UiSetSelectedUnitWait()
    {
        if (selectedUnit != null)
        {
            SetUnitWaitCommand(selectedUnit);
        }
    }

    public void UiTryResolvePlanningRound()
    {
        TryResolvePlanningRound();
    }

    public void UiSelectUnit(int unitId)
    {
        var unit = FindUnitById(unitId);
        SelectUnit(selectedUnit == unit ? null : unit);
    }

    public void UiSetUnitWait(int unitId)
    {
        var unit = FindUnitById(unitId);
        if (unit != null)
        {
            SetUnitWaitCommand(unit);
        }
    }

    public void UiReturnToTeamBuilder()
    {
        ReturnToTeamBuilder();
    }

    public void UiRetryBattle()
    {
        TryStartCpuBattle();
    }

    private HexUnit FindUnitById(int unitId)
    {
        foreach (var unit in units)
        {
            if (unit.Id == unitId)
            {
                return unit;
            }
        }

        return null;
    }

    private string BuildSelectedUnitSummary()
    {
        return selectedUnit == null
            ? "当前未选择棋子"
            : $"当前选择：{selectedUnit.RoleName}  HP {selectedUnit.CurrentHealth}/{selectedUnit.MaxHealth}  ATK {selectedUnit.AttackPower}  MOVE {selectedUnit.MoveRange}";
    }

    private string GetTurnTypeLabel()
    {
        if (lastResolutionKind == ResolutionKind.Attack)
        {
            return "攻击回合";
        }

        return lastResolutionKind == ResolutionKind.Move ? "移动回合" : "准备中";
    }

    private static HexTacticsUiFlowState ConvertFlowState(FlowState flowState)
    {
        return flowState switch
        {
            FlowState.ModeSelect => HexTacticsUiFlowState.ModeSelect,
            FlowState.TeamBuilder => HexTacticsUiFlowState.TeamBuilder,
            FlowState.Planning => HexTacticsUiFlowState.Planning,
            FlowState.Resolving => HexTacticsUiFlowState.Resolving,
            FlowState.Victory => HexTacticsUiFlowState.Victory,
            _ => HexTacticsUiFlowState.ModeSelect
        };
    }
}
