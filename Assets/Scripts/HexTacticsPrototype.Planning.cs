using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed partial class HexTacticsPrototype
{
    private void HandlePlanningPointerInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            BeginPlanningPointerPress();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            UpdatePlanningPointerHold();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndPlanningPointerPress();
        }
    }

    private void BeginPlanningPointerPress()
    {
        isPlanningPointerPressed = true;
        planningPointerStartedOverUi = IsPointerOverUi();
        planningPointerHoldTriggered = false;
        planningPointerPressPosition = Mouse.current.position.ReadValue();
        planningPointerPressStartTime = Time.unscaledTime;
        planningPointerPressUnit = null;

        if (!planningPointerStartedOverUi)
        {
            TryResolvePlanningPointerTarget(planningPointerPressPosition, out planningPointerPressUnit, out _);
        }
    }

    private void UpdatePlanningPointerHold()
    {
        if (!isPlanningPointerPressed || planningPointerStartedOverUi)
        {
            return;
        }

        var pointerPosition = Mouse.current.position.ReadValue();
        if (planningPointerHoldTriggered)
        {
            UpdateSkillPopupHover(pointerPosition);
            return;
        }

        if (planningPointerPressUnit == null ||
            planningPointerPressUnit.Team != Team.Blue ||
            !planningPointerPressUnit.HasMultipleSkills)
        {
            return;
        }

        if ((pointerPosition - planningPointerPressPosition).sqrMagnitude > SkillPopupMoveTolerance * SkillPopupMoveTolerance)
        {
            return;
        }

        if (Time.unscaledTime - planningPointerPressStartTime < SkillPopupHoldDuration)
        {
            return;
        }

        OpenSkillPopup(planningPointerPressUnit, planningPointerPressPosition);
        planningPointerHoldTriggered = true;
        UpdateSkillPopupHover(pointerPosition);
    }

    private void EndPlanningPointerPress()
    {
        if (!isPlanningPointerPressed)
        {
            return;
        }

        var startedOverUi = planningPointerStartedOverUi;
        var holdTriggered = planningPointerHoldTriggered;
        var mainCamera = Camera.main;
        var pointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : planningPointerPressPosition;
        ResetPlanningPointerTracking();

        if (startedOverUi)
        {
            return;
        }

        if (holdTriggered)
        {
            CommitSkillPopupSelection(pointerPosition);
            return;
        }

        if (isSkillPopupOpen)
        {
            CloseSkillPopup();
            RefreshVisuals();
            return;
        }

        if (mainCamera == null)
        {
            return;
        }

        var ray = mainCamera.ScreenPointToRay(pointerPosition);
        var hits = Physics.RaycastAll(ray, 200f);
        if (hits.Length == 0)
        {
            RefreshVisuals();
            return;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        if (TryResolvePlanningUnitHit(hits, out var clickedUnit))
        {
            HandlePlanningUnitClick(clickedUnit);
            return;
        }

        if (TryResolvePlanningCellHit(hits, out var clickedCell))
        {
            if (clickedCell.Occupant != null)
            {
                HandlePlanningUnitClick(clickedCell.Occupant);
                return;
            }

            HandlePlanningCellClick(clickedCell);
            return;
        }

        RefreshVisuals();
    }

    private bool TryResolvePlanningPointerTarget(Vector2 pointerPosition, out HexUnit clickedUnit, out HexCell clickedCell)
    {
        clickedUnit = null;
        clickedCell = null;

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        var ray = mainCamera.ScreenPointToRay(pointerPosition);
        var hits = Physics.RaycastAll(ray, 200f);
        if (hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        if (TryResolvePlanningUnitHit(hits, out clickedUnit))
        {
            return true;
        }

        return TryResolvePlanningCellHit(hits, out clickedCell);
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void ResetPlanningPointerTracking()
    {
        isPlanningPointerPressed = false;
        planningPointerStartedOverUi = false;
        planningPointerHoldTriggered = false;
        planningPointerPressPosition = Vector2.zero;
        planningPointerPressStartTime = 0f;
        planningPointerPressUnit = null;
    }

    private void OpenSkillPopup(HexUnit unit, Vector2 screenPosition)
    {
        if (unit == null || unit.Team != Team.Blue || !unit.HasMultipleSkills || currentFlowState != FlowState.Planning)
        {
            return;
        }

        SelectUnit(unit);
        if (selectedUnit == null || selectedUnit != unit || !selectedUnit.HasMultipleSkills)
        {
            return;
        }

        isSkillPopupOpen = true;
        skillPopupScreenPosition = HexTacticsSkillPopupView.CalculatePopupScreenPosition(
            screenPosition,
            selectedUnit.SkillCount,
            new Vector2(Screen.width, Screen.height));
        skillPopupHoveredSkillIndex = -1;
        RefreshVisuals();
    }

    private void CloseSkillPopup()
    {
        isSkillPopupOpen = false;
        skillPopupHoveredSkillIndex = -1;
        skillPopupScreenPosition = Vector2.zero;
    }

    private void UpdateSkillPopupHover(Vector2 pointerPosition)
    {
        if (!isSkillPopupOpen || selectedUnit == null)
        {
            return;
        }

        var hoveredSkillIndex = HexTacticsSkillPopupView.ResolveSkillIndexAtScreenPosition(
            pointerPosition,
            skillPopupScreenPosition,
            selectedUnit.SkillCount);
        if (hoveredSkillIndex == skillPopupHoveredSkillIndex)
        {
            return;
        }

        skillPopupHoveredSkillIndex = hoveredSkillIndex;
        RefreshVisuals();
    }

    private void CommitSkillPopupSelection(Vector2 pointerPosition)
    {
        if (!isSkillPopupOpen || selectedUnit == null)
        {
            RefreshVisuals();
            return;
        }

        var hoveredSkillIndex = HexTacticsSkillPopupView.ResolveSkillIndexAtScreenPosition(
            pointerPosition,
            skillPopupScreenPosition,
            selectedUnit.SkillCount);
        if (CanSelectSkill(selectedUnit, hoveredSkillIndex) && hoveredSkillIndex != selectedUnit.SelectedSkillIndex)
        {
            SetUnitSelectedSkill(selectedUnit, hoveredSkillIndex);
            return;
        }

        CloseSkillPopup();
        RefreshVisuals();
    }

    private bool TryResolvePlanningUnitHit(RaycastHit[] hits, out HexUnit clickedUnit)
    {
        clickedUnit = null;
        var bestPriority = int.MaxValue;
        var bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!unitLookups.TryGetValue(hit.collider, out var candidate))
            {
                continue;
            }

            var priority = GetPlanningUnitHitPriority(candidate);
            if (priority >= 10)
            {
                continue;
            }

            if (priority < bestPriority || (priority == bestPriority && hit.distance < bestDistance))
            {
                bestPriority = priority;
                bestDistance = hit.distance;
                clickedUnit = candidate;
            }
        }

        return clickedUnit != null;
    }

    private bool TryResolvePlanningCellHit(RaycastHit[] hits, out HexCell clickedCell)
    {
        clickedCell = null;
        foreach (var hit in hits)
        {
            if (cellLookups.TryGetValue(hit.collider, out clickedCell))
            {
                return true;
            }
        }

        return false;
    }

    private int GetPlanningUnitHitPriority(HexUnit unit)
    {
        if (unit == null)
        {
            return 10;
        }

        if (unit.Team != Team.Blue)
        {
            return selectedUnit != null && selectedUnit.Team == Team.Blue && CanAssignEnemyTarget(selectedUnit, unit.Coord) ? 0 : 10;
        }

        if (selectedUnit != null && selectedUnit != unit && IsMoveOption(unit.Coord))
        {
            return 1;
        }

        return 2;
    }

    private void HandlePlanningUnitClick(HexUnit clickedUnit)
    {
        if (clickedUnit.Team == Team.Blue)
        {
            if (selectedUnit != null && selectedUnit != clickedUnit && IsMoveOption(clickedUnit.Coord))
            {
                SetUnitMoveCommand(selectedUnit, clickedUnit.Coord);
                return;
            }

            SelectUnit(selectedUnit == clickedUnit ? null : clickedUnit);
            return;
        }

        if (selectedUnit != null && selectedUnit.Team == Team.Blue && CanAssignEnemyTarget(selectedUnit, clickedUnit.Coord))
        {
            SetUnitAttackCommand(selectedUnit, clickedUnit.Coord);
            return;
        }
    }

    private void HandlePlanningCellClick(HexCell clickedCell)
    {
        if (selectedUnit != null && IsMoveOption(clickedCell.Coord))
        {
            SetUnitMoveCommand(selectedUnit, clickedCell.Coord);
        }
    }

    private void AssignDirectionalCommand(HexUnit unit, HexCoord direction)
    {
        var target = unit.Coord + direction;
        if (!cells.TryGetValue(target, out var targetCell))
        {
            return;
        }

        if (targetCell.Occupant != null && targetCell.Occupant.Team != unit.Team)
        {
            SetUnitAttackCommand(unit, target);
        }
        else
        {
            SetUnitMoveCommand(unit, target);
        }
    }

    private void SelectUnit(HexUnit unit)
    {
        var previousSelectedUnit = selectedUnit;
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();

        unit = ResolvePlanningSelectionTarget(unit);
        if (unit != previousSelectedUnit)
        {
            CloseSkillPopup();
        }

        if (unit != null && unit.Team == Team.Blue && currentFlowState == FlowState.Planning)
        {
            EnsureUnitSelectedSkillUsable(unit);
            selectedUnit = unit;
            foreach (var cell in GetMoveOptions(unit))
            {
                moveCells.Add(cell);
            }

            foreach (var cell in GetAttackOptions(unit))
            {
                attackCells.Add(cell);
            }
        }

        RefreshVisuals();
    }

    private HexUnit ResolvePlanningSelectionTarget(HexUnit requestedUnit)
    {
        if (currentFlowState != FlowState.Planning)
        {
            return requestedUnit;
        }

        if (AreAllBlueCommandsAssigned())
        {
            return requestedUnit != null && requestedUnit.Team == Team.Blue ? requestedUnit : null;
        }

        if (requestedUnit != null && requestedUnit.Team == Team.Blue && !requestedUnit.HasAssignedCommand)
        {
            return requestedUnit;
        }

        return FindFirstBlueUnitWithoutCommand();
    }

    private bool IsMoveOption(HexCoord coord)
    {
        foreach (var cell in moveCells)
        {
            if (cell.Coord == coord)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAttackOption(HexCoord coord)
    {
        foreach (var cell in attackCells)
        {
            if (cell.Coord == coord)
            {
                return true;
            }
        }

        return false;
    }

    private void SetUnitMoveCommand(HexUnit unit, HexCoord target)
    {
        if (unit == null || unit.Team != Team.Blue)
        {
            return;
        }

        if (IsMoveCommand(unit) && unit.PlannedMoveTarget == target)
        {
            AssignWaitCommand(unit);
            HandleAfterPlayerCommandChanged(unit);
            return;
        }

        if (TryAssignMoveCommand(unit, target))
        {
            HandleAfterPlayerCommandChanged(unit);
        }
    }

    private void SetUnitAttackCommand(HexUnit unit, HexCoord target)
    {
        if (unit == null || unit.Team != Team.Blue || !CanAssignEnemyTarget(unit, target))
        {
            return;
        }

        EnsureUnitSelectedSkillUsable(unit);
        if (IsEnemyCommand(unit) &&
            unit.PlannedEnemyTargetUnit != null &&
            unit.PlannedEnemyTargetUnit.Coord == target &&
            unit.PlannedSkillIndex == unit.SelectedSkillIndex)
        {
            AssignWaitCommand(unit);
            HandleAfterPlayerCommandChanged(unit);
            return;
        }

        TryAssignAttackCommand(unit, target, unit.SelectedSkillIndex);
        HandleAfterPlayerCommandChanged(unit);
    }

    private void SetUnitWaitCommand(HexUnit unit)
    {
        if (unit == null || unit.Team != Team.Blue)
        {
            return;
        }

        AssignWaitCommand(unit);
        HandleAfterPlayerCommandChanged(unit);
    }

    private void AutoPlanCpuCommands()
    {
        foreach (var unit in units)
        {
            if (unit.Team != Team.Red)
            {
                continue;
            }

            AssignWaitCommand(unit);

            var attackSelection = ChooseCpuAttackSelection(unit);
            if (attackSelection.HasValue)
            {
                TryAssignAttackCommand(unit, attackSelection.Target.Coord, attackSelection.SkillIndex);
            }

            if (!IsEnemyCommand(unit))
            {
                var moveTarget = ChooseCpuMoveTarget(unit);
                if (moveTarget.HasValue)
                {
                    TryAssignMoveCommand(unit, moveTarget.Value);
                }
            }
        }
    }

    private void HandleAfterPlayerCommandChanged(HexUnit commandingUnit)
    {
        CloseSkillPopup();

        if (currentFlowState != FlowState.Planning || isResolving || isAnimating)
        {
            RefreshVisuals();
            return;
        }

        if (AreAllBlueCommandsAssigned())
        {
            SelectUnit(null);
            TryResolvePlanningRound();
            return;
        }

        SelectUnit(FindNextBlueUnitWithoutCommand(commandingUnit) ?? FindFirstBlueUnitWithoutCommand());
    }

    private AttackSelection ChooseCpuAttackSelection(HexUnit unit)
    {
        return ChooseAttackSelectionFromOrigin(unit, unit != null ? unit.Coord : default, Team.Blue, requireCurrentRange: false);
    }

    private AttackSelection ChooseAttackSelectionFromOrigin(
        HexUnit unit,
        HexCoord origin,
        Team enemyTeam,
        int? forcedSkillIndex = null,
        bool preferNonConsumingSkill = false,
        bool requireCurrentRange = true)
    {
        if (preferNonConsumingSkill)
        {
            var freeSelection = ChooseAttackSelectionFromOriginInternal(unit, origin, enemyTeam, forcedSkillIndex, zeroCostOnly: true, requireCurrentRange: requireCurrentRange);
            if (freeSelection.HasValue)
            {
                return freeSelection;
            }
        }

        return ChooseAttackSelectionFromOriginInternal(unit, origin, enemyTeam, forcedSkillIndex, zeroCostOnly: false, requireCurrentRange: requireCurrentRange);
    }

    private AttackSelection ChooseAttackSelectionFromOriginInternal(
        HexUnit unit,
        HexCoord origin,
        Team enemyTeam,
        int? forcedSkillIndex,
        bool zeroCostOnly,
        bool requireCurrentRange)
    {
        if (unit == null || unit.SkillCount <= 0)
        {
            return default;
        }

        if (forcedSkillIndex.HasValue && (forcedSkillIndex.Value < 0 || forcedSkillIndex.Value >= unit.SkillCount))
        {
            return default;
        }

        AttackSelection bestSelection = default;
        var bestScore = int.MinValue;
        var startIndex = forcedSkillIndex.HasValue ? Mathf.Clamp(forcedSkillIndex.Value, 0, unit.SkillCount - 1) : 0;
        var endIndex = forcedSkillIndex.HasValue ? startIndex + 1 : unit.SkillCount;

        foreach (var candidate in units)
        {
            if (candidate == null || candidate.Team != enemyTeam)
            {
                continue;
            }

            for (var skillIndex = startIndex; skillIndex < endIndex; skillIndex++)
            {
                var skill = unit.GetSkillAt(skillIndex);
                if (skill == null || (zeroCostOnly && skill.EnergyCost > 0))
                {
                    continue;
                }

                var canUseCandidate = requireCurrentRange
                    ? CanSelectAttackTargetFromOrigin(unit, origin, candidate.Coord, skill)
                    : HasEnemyTargetAt(unit, candidate.Coord) && CanUseSkill(unit, skill);
                if (!canUseCandidate)
                {
                    continue;
                }

                var score = GetUnitThreatPower(candidate) * 10 - candidate.CurrentHealth - HexDistance(origin, candidate.Coord) * 2;
                score += skill.Power * 4;
                if (candidate.CurrentHealth <= skill.Power)
                {
                    score += 50;
                }

                if (skill.EnergyCost == 0)
                {
                    score += 10;
                }
                else
                {
                    score -= skill.EnergyCost * 4;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestSelection = new AttackSelection(candidate, skillIndex);
                }
            }
        }

        return bestSelection;
    }

    private int ChooseSkillIndexAgainstTarget(
        HexUnit unit,
        HexCoord origin,
        HexUnit target,
        int? forcedSkillIndex = null,
        bool preferNonConsumingSkill = false)
    {
        if (preferNonConsumingSkill)
        {
            var freeSkillIndex = ChooseSkillIndexAgainstTargetInternal(unit, origin, target, forcedSkillIndex, zeroCostOnly: true);
            if (freeSkillIndex >= 0)
            {
                return freeSkillIndex;
            }
        }

        return ChooseSkillIndexAgainstTargetInternal(unit, origin, target, forcedSkillIndex, zeroCostOnly: false);
    }

    private int ChooseSkillIndexAgainstTargetInternal(
        HexUnit unit,
        HexCoord origin,
        HexUnit target,
        int? forcedSkillIndex,
        bool zeroCostOnly)
    {
        if (unit == null || target == null || unit.SkillCount <= 0)
        {
            return -1;
        }

        if (forcedSkillIndex.HasValue && (forcedSkillIndex.Value < 0 || forcedSkillIndex.Value >= unit.SkillCount))
        {
            return -1;
        }

        var bestSkillIndex = -1;
        var bestScore = int.MinValue;
        var startIndex = forcedSkillIndex.HasValue ? forcedSkillIndex.Value : 0;
        var endIndex = forcedSkillIndex.HasValue ? startIndex + 1 : unit.SkillCount;

        for (var skillIndex = startIndex; skillIndex < endIndex; skillIndex++)
        {
            var skill = unit.GetSkillAt(skillIndex);
            if (skill == null || (zeroCostOnly && skill.EnergyCost > 0))
            {
                continue;
            }

            if (!CanSelectAttackTargetFromOrigin(unit, origin, target.Coord, skill))
            {
                continue;
            }

            var score = skill.Power * 4;
            if (target.CurrentHealth <= skill.Power)
            {
                score += 50;
            }

            if (skill.EnergyCost == 0)
            {
                score += 10;
            }
            else
            {
                score -= skill.EnergyCost * 4;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestSkillIndex = skillIndex;
            }
        }

        return bestSkillIndex;
    }

    private HexCoord? ChooseCpuMoveTarget(HexUnit unit)
    {
        HexCoord? bestTarget = null;
        var bestScore = int.MaxValue;

        foreach (var cell in GetMoveOptions(unit))
        {
            var target = cell.Coord;
            var distance = GetDistanceToNearestEnemy(target, Team.Blue);
            var centerBias = HexDistance(target, new HexCoord(0, 0));
            var occupantPenalty = cell.Occupant == null ? 0 : 6;
            var score = distance * 10 + centerBias + occupantPenalty;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    private int GetDistanceToNearestEnemy(HexCoord origin, Team enemyTeam)
    {
        var bestDistance = int.MaxValue;
        foreach (var unit in units)
        {
            if (unit.Team != enemyTeam)
            {
                continue;
            }

            var distance = HexDistance(origin, unit.Coord);
            if (distance < bestDistance)
            {
                bestDistance = distance;
            }
        }

        return bestDistance == int.MaxValue ? 0 : bestDistance;
    }

    private HexUnit FindNextBlueUnitWithoutCommand(HexUnit currentUnit)
    {
        if (currentUnit == null)
        {
            return FindFirstBlueUnitWithoutCommand();
        }

        var blueUnits = new List<HexUnit>();
        foreach (var unit in units)
        {
            if (unit.Team == Team.Blue)
            {
                blueUnits.Add(unit);
            }
        }

        if (blueUnits.Count == 0)
        {
            return null;
        }

        var currentIndex = blueUnits.IndexOf(currentUnit);
        if (currentIndex < 0)
        {
            return FindFirstBlueUnitWithoutCommand();
        }

        for (var offset = 1; offset <= blueUnits.Count; offset++)
        {
            var candidate = blueUnits[(currentIndex + offset) % blueUnits.Count];
            if (!candidate.HasAssignedCommand)
            {
                return candidate;
            }
        }

        return null;
    }

}
