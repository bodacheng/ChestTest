using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    public HexTacticsUiSnapshot BuildUiSnapshot()
    {
        var playerUsedCost = GetPlayerTeamCost();
        var snapshot = new HexTacticsUiSnapshot
        {
            FlowState = ConvertFlowState(currentFlowState),
            PlanningRoundNumber = planningRoundNumber,
            ResolvedTurnCount = resolvedTurnCount,
            BlueAliveCount = CountAliveUnits(Team.Blue),
            RedAliveCount = CountAliveUnits(Team.Red),
            PlayerCostLimit = playerTeamCostLimit,
            CpuCostLimit = cpuTeamCostLimit,
            PlayerUsedCost = playerUsedCost,
            BuilderStatus = builderStatus ?? string.Empty,
            ResolutionStatus = resolutionStatus ?? string.Empty,
            TurnTypeLabel = GetTurnTypeLabel(),
            HasSelectedUnit = selectedUnit != null,
            CanStartBattle = playerDeploymentEntries.Count > 0 && playerUsedCost <= playerTeamCostLimit,
            SelectedUnitSummary = BuildSelectedUnitSummary(),
            VictorySummary = winningTeam.HasValue ? $"{TeamDisplayName(winningTeam.Value)}完成歼灭" : string.Empty
        };

        for (var i = 0; i < characterRoster.Count; i++)
        {
            var definition = characterRoster[i];
            if (definition == null)
            {
                continue;
            }

            snapshot.RosterEntries.Add(new HexTacticsRosterEntryUiData(
                i,
                definition.DisplayName,
                definition.Description,
                definition.MaxHealth,
                definition.AttackPower,
                definition.MoveRange,
                definition.Cost,
                BuildAvatarUiData(definition),
                snapshot.PlayerUsedCost + definition.Cost <= playerTeamCostLimit));
        }

        for (var i = 0; i < playerDeploymentEntries.Count; i++)
        {
            var entry = playerDeploymentEntries[i];
            if (entry.Definition == null)
            {
                continue;
            }

            snapshot.PlayerSelectionEntries.Add(new HexTacticsSelectionEntryUiData(
                entry.EntryId,
                i + 1,
                entry.Definition.DisplayName,
                entry.Definition.MaxHealth,
                entry.Definition.AttackPower,
                entry.Definition.MoveRange,
                entry.Definition.Cost,
                $"部署格 ({entry.Coord.Q},{entry.Coord.R})",
                BuildAvatarUiData(entry.Definition)));
        }

        foreach (var unit in units)
        {
            if (unit.Team == Team.Blue)
            {
                snapshot.PlayerCommandEntries.Add(new HexTacticsCommandEntryUiData(
                    unit.Id,
                    unit.Name,
                    DescribeCommand(unit, compact: false),
                    selectedUnit == unit,
                    unit.HasAssignedCommand,
                    BuildAvatarUiData(unit.CharacterConfig)));
            }

            if (currentFlowState == FlowState.Planning || currentFlowState == FlowState.Resolving || currentFlowState == FlowState.Victory)
            {
                snapshot.WorldLabels.Add(new HexTacticsWorldLabelUiData(
                    unit.Id,
                    unit.Transform.position + new Vector3(0f, unit.LabelHeight, 0f),
                    unit.Name,
                    currentFlowState == FlowState.Planning
                        ? DescribeCommand(unit, compact: true)
                        : $"ATK {unit.AttackPower}  MOVE {unit.MoveRange}",
                    unit.Team == Team.Blue,
                    unit.CurrentHealth,
                    unit.MaxHealth));
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

    public void UiPlaceRosterCharacterAt(int rosterIndex, Vector2 screenPosition)
    {
        TryPlacePlayerDeploymentFromScreenPoint(rosterIndex, screenPosition);
    }

    public void UiMovePlacedCharacterTo(int entryId, Vector2 screenPosition)
    {
        TryMovePlayerDeploymentFromScreenPoint(entryId, screenPosition);
    }

    public void UiRemovePlayerCharacterAt(int entryId)
    {
        RemovePlayerCharacterAt(entryId);
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
        var pendingCount = 0;
        foreach (var unit in units)
        {
            if (unit.Team == Team.Blue && !unit.HasAssignedCommand)
            {
                pendingCount++;
            }
        }

        return selectedUnit == null
            ? pendingCount > 0
                ? $"请继续下达命令，剩余 {pendingCount} 名蓝方角色未设置"
                : "所有蓝方角色已设置命令，正在准备自动结算"
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

    private HexTacticsAvatarUiData BuildAvatarUiData(HexTacticsCharacterConfig definition)
    {
        if (definition == null)
        {
            return new HexTacticsAvatarUiData(null, "?", new Color(0.22f, 0.28f, 0.32f, 1f));
        }

        return new HexTacticsAvatarUiData(
            definition.Avatar,
            BuildAvatarFallback(definition.DisplayName),
            GetAvatarBackgroundColor(definition.VisualArchetype));
    }

    private static string BuildAvatarFallback(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "?";
        }

        var trimmed = displayName.Trim();
        return trimmed.Length >= 2 ? trimmed.Substring(0, 2) : trimmed.Substring(0, 1);
    }

    private static Color GetAvatarBackgroundColor(HexTacticsCharacterVisualArchetype archetype)
    {
        return archetype switch
        {
            HexTacticsCharacterVisualArchetype.Fawn => new Color(0.58f, 0.52f, 0.34f, 1f),
            HexTacticsCharacterVisualArchetype.Doe => new Color(0.41f, 0.56f, 0.34f, 1f),
            HexTacticsCharacterVisualArchetype.Stag => new Color(0.34f, 0.44f, 0.24f, 1f),
            HexTacticsCharacterVisualArchetype.Elk => new Color(0.46f, 0.34f, 0.24f, 1f),
            HexTacticsCharacterVisualArchetype.Tiger => new Color(0.72f, 0.40f, 0.20f, 1f),
            HexTacticsCharacterVisualArchetype.WhiteTiger => new Color(0.42f, 0.56f, 0.70f, 1f),
            _ => new Color(0.25f, 0.34f, 0.40f, 1f)
        };
    }
}
