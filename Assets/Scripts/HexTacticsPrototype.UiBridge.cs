using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    public HexTacticsUiSnapshot BuildUiSnapshot()
    {
        var playerUsedCost = GetPlayerTeamCost();
        var blueAssignedCommandCount = CountBlueAssignedCommands();
        var bluePendingCommandCount = CountBluePendingCommands();
        var snapshot = new HexTacticsUiSnapshot
        {
            FlowState = ConvertFlowState(currentFlowState),
            PlanningRoundNumber = planningRoundNumber,
            ResolvedTurnCount = resolvedTurnCount,
            BlueAliveCount = CountAliveUnits(Team.Blue),
            RedAliveCount = CountAliveUnits(Team.Red),
            BlueAssignedCommandCount = blueAssignedCommandCount,
            BluePendingCommandCount = bluePendingCommandCount,
            PlayerCostLimit = playerTeamCostLimit,
            CpuCostLimit = cpuTeamCostLimit,
            PlayerUsedCost = playerUsedCost,
            CurrentCommandSummary = BuildCurrentCommandSummary(bluePendingCommandCount),
            CommandProgressSummary = BuildCommandProgressSummary(blueAssignedCommandCount, bluePendingCommandCount),
            BuilderStatus = builderStatus ?? string.Empty,
            ResolutionStatus = resolutionStatus ?? string.Empty,
            SkillPopupTitle = BuildSkillPopupTitle(),
            TurnTypeLabel = GetTurnTypeLabel(),
            HasSelectedUnit = selectedUnit != null,
            IsSkillPopupOpen = ShouldShowSkillPopup(),
            CanStartBattle = playerDeploymentEntries.Count > 0 && playerUsedCost <= playerTeamCostLimit,
            SelectedUnitSummary = BuildSelectedUnitSummary(),
            SkillPopupScreenPosition = skillPopupScreenPosition,
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
                definition.PreviewAttackPower,
                definition.PreviewAttackRange,
                definition.Speed,
                definition.MoveRange,
                definition.Cost,
                definition.MaxEnergy,
                definition.SkillCount,
                definition.PrimarySkill != null ? definition.PrimarySkill.DisplayName : "通常攻撃",
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
                entry.Definition.PreviewAttackPower,
                entry.Definition.PreviewAttackRange,
                entry.Definition.Speed,
                entry.Definition.MoveRange,
                entry.Definition.Cost,
                entry.Definition.MaxEnergy,
                entry.Definition.SkillCount,
                entry.Definition.PrimarySkill != null ? entry.Definition.PrimarySkill.DisplayName : "通常攻撃",
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
                    BuildCommandEntrySummary(unit),
                    CountUsableSkills(unit) > 1,
                    selectedUnit == unit,
                    unit.HasAssignedCommand,
                    BuildAvatarUiData(unit.CharacterConfig)));
            }

            if (currentFlowState == FlowState.Planning || currentFlowState == FlowState.Resolving || currentFlowState == FlowState.Victory)
            {
                snapshot.WorldLabels.Add(new HexTacticsWorldLabelUiData(
                    unit.Id,
                    unit.Transform.position + new Vector3(0f, Mathf.Max(0.2f, unit.LabelHeight - 0.16f), 0f),
                    string.Empty,
                    string.Empty,
                    unit.Team == Team.Blue,
                    unit.CurrentHealth,
                    unit.MaxHealth));
            }
        }

        if (selectedUnit != null && selectedUnit.Team == Team.Blue)
        {
            EnsureUnitSelectedSkillUsable(selectedUnit);
            for (var skillIndex = 0; skillIndex < selectedUnit.SkillCount; skillIndex++)
            {
                var skill = selectedUnit.GetSkillAt(skillIndex);
                if (skill == null)
                {
                    continue;
                }

                var isSelected = skillIndex == selectedUnit.SelectedSkillIndex;
                var isHovered = ShouldShowSkillPopup() && skillIndex == skillPopupHoveredSkillIndex;
                var isPlannedSkill = skillIndex == selectedUnit.PlannedSkillIndex && IsEnemyCommand(selectedUnit);
                var isAvailable = CanUseSkill(selectedUnit, skill);
                var stateLabel = isSelected
                    ? string.Empty
                    : isHovered
                        ? "  松手"
                        : isPlannedSkill
                            ? "  已定"
                            : isAvailable
                                ? string.Empty
                                : "  不足";
                snapshot.SelectedUnitSkillEntries.Add(new HexTacticsSkillChoiceUiData(
                    skillIndex,
                    skill.DisplayName,
                    $"攻{skill.Power} 射{skill.AttackRange} 消{skill.EnergyCost}{stateLabel}",
                    isSelected,
                    isHovered,
                    isAvailable,
                    isPlannedSkill));
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

    public void UiSelectSelectedUnitSkill(int skillIndex)
    {
        SetUnitSelectedSkill(selectedUnit, skillIndex);
    }

    public void UiCycleUnitSkill(int unitId)
    {
        var unit = FindUnitById(unitId);
        if (unit != null)
        {
            CycleUnitSkill(unit);
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

    private void CycleUnitSkill(HexUnit unit)
    {
        if (unit == null || unit.SkillCount <= 1)
        {
            return;
        }

        var nextSkillIndex = FindNextUsableSkillIndex(unit, unit.SelectedSkillIndex);
        if (nextSkillIndex < 0 || nextSkillIndex == unit.SelectedSkillIndex)
        {
            RefreshVisuals();
            return;
        }

        unit.SelectedSkillIndex = nextSkillIndex;
        unit.HasManualSkillOverride = true;
        CloseSkillPopup();
        SyncSelectedSkillToPlannedCommand(unit);
        if (selectedUnit == unit && currentFlowState == FlowState.Planning)
        {
            SelectUnit(unit);
            return;
        }

        RefreshVisuals();
    }

    private void SetUnitSelectedSkill(HexUnit unit, int skillIndex)
    {
        if (!CanSelectSkill(unit, skillIndex))
        {
            return;
        }

        unit.SelectedSkillIndex = skillIndex;
        unit.HasManualSkillOverride = true;
        CloseSkillPopup();
        SyncSelectedSkillToPlannedCommand(unit);
        if (selectedUnit == unit && currentFlowState == FlowState.Planning)
        {
            SelectUnit(unit);
            return;
        }

        RefreshVisuals();
    }

    private string BuildSelectedUnitSummary()
    {
        if (selectedUnit == null)
        {
            return currentFlowState == FlowState.Planning
                ? "点棋盘上的蓝方角色，或点下方列表继续下令。"
                : string.Empty;
        }

        EnsureUnitSelectedSkillUsable(selectedUnit);
        var assignmentSummary = selectedUnit.HasAssignedCommand ? "已下令" : "待下令";
        var selectedSkill = selectedUnit.SelectedSkill;
        var skillSummary = BuildCompactSkillSummary(selectedSkill);
        return $"{selectedUnit.RoleName}  HP {selectedUnit.CurrentHealth}/{selectedUnit.MaxHealth}  EN {selectedUnit.CurrentEnergy}/{selectedUnit.MaxEnergy}  {skillSummary}  {assignmentSummary}";
    }

    private bool ShouldShowSkillPopup()
    {
        return isSkillPopupOpen &&
               currentFlowState == FlowState.Planning &&
               selectedUnit != null &&
               selectedUnit.Team == Team.Blue &&
               selectedUnit.HasMultipleSkills;
    }

    private string BuildSkillPopupTitle()
    {
        return selectedUnit != null ? selectedUnit.RoleName : string.Empty;
    }

    private string BuildCurrentCommandSummary(int pendingCount)
    {
        if (currentFlowState != FlowState.Planning)
        {
            return string.Empty;
        }

        if (selectedUnit != null && selectedUnit.Team == Team.Blue)
        {
            return selectedUnit.HasAssignedCommand
                ? $"{selectedUnit.RoleName} · {selectedUnit.SelectedSkill?.DisplayName ?? "未选技"}"
                : $"下令 {selectedUnit.RoleName} · {selectedUnit.SelectedSkill?.DisplayName ?? "未选技"}";
        }

        return pendingCount > 0
            ? "请选择蓝方角色继续下令"
            : "所有蓝方命令已完成";
    }

    private string BuildCommandProgressSummary(int assignedCount, int pendingCount)
    {
        if (currentFlowState != FlowState.Planning)
        {
            return string.Empty;
        }

        var totalCount = assignedCount + pendingCount;
        if (totalCount <= 0)
        {
            return "当前没有可下令单位";
        }

        return pendingCount > 0
            ? $"待下令 {pendingCount}/{totalCount}"
            : $"命令完成 {assignedCount}/{totalCount}";
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

    private string BuildCommandEntrySummary(HexUnit unit)
    {
        EnsureUnitSelectedSkillUsable(unit);
        var currentEnergy = unit != null ? unit.CurrentEnergy : 0;
        var maxEnergy = unit != null ? unit.MaxEnergy : 0;
        var skillSummary = BuildCompactSkillSummary(unit != null ? unit.SelectedSkill : null);
        var commandSummary = unit != null ? DescribeCommand(unit, compact: true) : "未设置";
        return $"EN {currentEnergy}/{maxEnergy}  {skillSummary}\n{commandSummary}";
    }

    private static string BuildCompactSkillSummary(HexTacticsSkillConfig skill)
    {
        if (skill == null)
        {
            return "未设置";
        }

        return $"{skill.DisplayName} 攻{skill.Power}/射{skill.AttackRange}/消{skill.EnergyCost}";
    }

    private static string BuildSkillSummary(HexTacticsSkillConfig skill)
    {
        if (skill == null)
        {
            return "未设置";
        }

        return $"{skill.DisplayName}  攻 {skill.Power}  射 {skill.AttackRange}  消 {skill.EnergyCost}  充 {skill.EnergyGainOnHit}";
    }

    private int CountBlueAssignedCommands()
    {
        var count = 0;
        foreach (var unit in units)
        {
            if (unit.Team == Team.Blue && unit.HasAssignedCommand)
            {
                count++;
            }
        }

        return count;
    }

    private int CountBluePendingCommands()
    {
        var count = 0;
        foreach (var unit in units)
        {
            if (unit.Team == Team.Blue && !unit.HasAssignedCommand)
            {
                count++;
            }
        }

        return count;
    }

    private void EnsureUnitSelectedSkillUsable(HexUnit unit)
    {
        if (unit == null || unit.SkillCount <= 0)
        {
            return;
        }

        var preferredSkillIndex = FindPreferredAvailableSkillIndex(unit);
        if (!unit.HasManualSkillOverride || !CanUseSkill(unit, unit.SelectedSkill))
        {
            unit.SelectedSkillIndex = preferredSkillIndex >= 0 ? preferredSkillIndex : 0;
            SyncSelectedSkillToPlannedCommand(unit);
            return;
        }

        if (!CanUseSkill(unit, unit.SelectedSkill))
        {
            unit.SelectedSkillIndex = preferredSkillIndex >= 0 ? preferredSkillIndex : 0;
            SyncSelectedSkillToPlannedCommand(unit);
        }
    }

    private int CountUsableSkills(HexUnit unit)
    {
        if (unit == null)
        {
            return 0;
        }

        var count = 0;
        for (var skillIndex = 0; skillIndex < unit.SkillCount; skillIndex++)
        {
            if (CanUseSkill(unit, unit.GetSkillAt(skillIndex)))
            {
                count++;
            }
        }

        return count;
    }

    private bool CanSelectSkill(HexUnit unit, int skillIndex)
    {
        if (unit == null || skillIndex < 0 || skillIndex >= unit.SkillCount)
        {
            return false;
        }

        return CanUseSkill(unit, unit.GetSkillAt(skillIndex));
    }

    private int FindNextUsableSkillIndex(HexUnit unit, int currentIndex)
    {
        if (unit == null || unit.SkillCount <= 0)
        {
            return -1;
        }

        for (var offset = 1; offset <= unit.SkillCount; offset++)
        {
            var candidateIndex = (Mathf.Max(0, currentIndex) + offset) % unit.SkillCount;
            if (CanUseSkill(unit, unit.GetSkillAt(candidateIndex)))
            {
                return candidateIndex;
            }
        }

        return -1;
    }

    private int FindPreferredAvailableSkillIndex(HexUnit unit)
    {
        if (unit == null || unit.SkillCount <= 0)
        {
            return -1;
        }

        var bestSkillIndex = -1;
        var bestEnergyCost = int.MinValue;
        var bestPower = int.MinValue;
        var bestRange = int.MinValue;

        for (var skillIndex = 0; skillIndex < unit.SkillCount; skillIndex++)
        {
            var skill = unit.GetSkillAt(skillIndex);
            if (!CanUseSkill(unit, skill))
            {
                continue;
            }

            if (skill.EnergyCost > bestEnergyCost ||
                (skill.EnergyCost == bestEnergyCost && skill.Power > bestPower) ||
                (skill.EnergyCost == bestEnergyCost && skill.Power == bestPower && skill.AttackRange > bestRange))
            {
                bestSkillIndex = skillIndex;
                bestEnergyCost = skill.EnergyCost;
                bestPower = skill.Power;
                bestRange = skill.AttackRange;
            }
        }

        return bestSkillIndex;
    }

    private void SyncSelectedSkillToPlannedCommand(HexUnit unit)
    {
        if (unit == null || !IsEnemyCommand(unit))
        {
            return;
        }

        if (HasEnemyTargetAt(unit, unit.PlannedAttackTarget) && CanUseSkill(unit, unit.SelectedSkill))
        {
            unit.PlannedSkillIndex = unit.SelectedSkillIndex;
        }
    }
}
