using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class HexTacticsPrototype
{
    private void HandlePlanningPointerInput()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var pointerPosition = Mouse.current.position.ReadValue();
        var ray = mainCamera.ScreenPointToRay(pointerPosition);
        var hits = Physics.RaycastAll(ray, 200f);
        if (hits.Length == 0)
        {
            SelectUnit(null);
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

        SelectUnit(null);
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
            return selectedUnit != null && selectedUnit.Team == Team.Blue && IsAttackOption(unit.Coord) ? 0 : 10;
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

        if (selectedUnit != null && selectedUnit.Team == Team.Blue && IsAttackOption(clickedUnit.Coord))
        {
            SetUnitAttackCommand(selectedUnit, clickedUnit.Coord);
            return;
        }

        SelectUnit(null);
    }

    private void HandlePlanningCellClick(HexCell clickedCell)
    {
        if (selectedUnit != null && IsMoveOption(clickedCell.Coord))
        {
            SetUnitMoveCommand(selectedUnit, clickedCell.Coord);
            return;
        }

        SelectUnit(null);
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
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();

        if (unit != null && unit.Team == Team.Blue && currentFlowState == FlowState.Planning)
        {
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
            SelectUnit(null);
            return;
        }

        if (TryAssignMoveCommand(unit, target))
        {
            SelectUnit(null);
        }
    }

    private void SetUnitAttackCommand(HexUnit unit, HexCoord target)
    {
        if (unit == null || unit.Team != Team.Blue || !CanSelectAttackTarget(unit, target))
        {
            return;
        }

        if (IsEnemyCommand(unit) && unit.PlannedEnemyTargetUnit != null && unit.PlannedEnemyTargetUnit.Coord == target)
        {
            AssignWaitCommand(unit);
            SelectUnit(null);
            return;
        }

        TryAssignAttackCommand(unit, target);
        SelectUnit(null);
    }

    private void SetUnitWaitCommand(HexUnit unit)
    {
        if (unit == null || unit.Team != Team.Blue)
        {
            return;
        }

        AssignWaitCommand(unit);
        SelectUnit(null);
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

            var attackTarget = ChooseCpuAttackTarget(unit);
            if (attackTarget != null)
            {
                TryAssignAttackCommand(unit, attackTarget.Coord);
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

    private HexUnit ChooseCpuAttackTarget(HexUnit unit)
    {
        HexUnit bestTarget = null;
        var bestScore = int.MinValue;

        foreach (var candidate in units)
        {
            if (candidate.Team != Team.Blue || !CanSelectAttackTarget(unit, candidate.Coord))
            {
                continue;
            }

            var score = candidate.AttackPower * 10 - candidate.CurrentHealth - HexDistance(unit.Coord, candidate.Coord) * 2;
            if (candidate.CurrentHealth <= unit.AttackPower)
            {
                score += 50;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    private HexUnit ChooseCpuAttackTargetFromOrigin(HexUnit unit, HexCoord origin, Team enemyTeam)
    {
        HexUnit bestTarget = null;
        var bestScore = int.MinValue;

        foreach (var direction in NeighborDirections)
        {
            var coord = origin + direction;
            if (!cells.TryGetValue(coord, out var cell) || cell.Occupant == null || cell.Occupant.Team != enemyTeam)
            {
                continue;
            }

            var score = cell.Occupant.AttackPower * 10 - cell.Occupant.CurrentHealth;
            if (cell.Occupant.CurrentHealth <= unit.AttackPower)
            {
                score += 50;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = cell.Occupant;
            }
        }

        return bestTarget;
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

}
