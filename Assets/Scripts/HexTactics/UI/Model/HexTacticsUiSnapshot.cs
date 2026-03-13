using System.Collections.Generic;
using UnityEngine;

public enum HexTacticsUiFlowState
{
    ModeSelect,
    TeamBuilder,
    Planning,
    Resolving,
    Victory
}

public sealed class HexTacticsUiSnapshot
{
    public HexTacticsUiFlowState FlowState;
    public int PlanningRoundNumber;
    public int ResolvedTurnCount;
    public int BlueAliveCount;
    public int RedAliveCount;
    public int PlayerCostLimit;
    public int CpuCostLimit;
    public int PlayerUsedCost;
    public bool HasSelectedUnit;
    public bool CanStartBattle;
    public string BuilderStatus = string.Empty;
    public string ResolutionStatus = string.Empty;
    public string TurnTypeLabel = string.Empty;
    public string SelectedUnitSummary = string.Empty;
    public string VictorySummary = string.Empty;
    public readonly List<HexTacticsRosterEntryUiData> RosterEntries = new();
    public readonly List<HexTacticsSelectionEntryUiData> PlayerSelectionEntries = new();
    public readonly List<HexTacticsCommandEntryUiData> PlayerCommandEntries = new();
    public readonly List<HexTacticsWorldLabelUiData> WorldLabels = new();
}

public readonly struct HexTacticsRosterEntryUiData
{
    public HexTacticsRosterEntryUiData(
        int rosterIndex,
        string displayName,
        string description,
        int maxHealth,
        int attackPower,
        int moveRange,
        int cost,
        bool canAdd)
    {
        RosterIndex = rosterIndex;
        DisplayName = displayName;
        Description = description;
        MaxHealth = maxHealth;
        AttackPower = attackPower;
        MoveRange = moveRange;
        Cost = cost;
        CanAdd = canAdd;
    }

    public int RosterIndex { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int MaxHealth { get; }
    public int AttackPower { get; }
    public int MoveRange { get; }
    public int Cost { get; }
    public bool CanAdd { get; }
}

public readonly struct HexTacticsSelectionEntryUiData
{
    public HexTacticsSelectionEntryUiData(
        int selectionIndex,
        string displayName,
        int maxHealth,
        int attackPower,
        int moveRange,
        int cost)
    {
        SelectionIndex = selectionIndex;
        DisplayName = displayName;
        MaxHealth = maxHealth;
        AttackPower = attackPower;
        MoveRange = moveRange;
        Cost = cost;
    }

    public int SelectionIndex { get; }
    public string DisplayName { get; }
    public int MaxHealth { get; }
    public int AttackPower { get; }
    public int MoveRange { get; }
    public int Cost { get; }
}

public readonly struct HexTacticsCommandEntryUiData
{
    public HexTacticsCommandEntryUiData(
        int unitId,
        string unitName,
        string commandText,
        bool isSelected)
    {
        UnitId = unitId;
        UnitName = unitName;
        CommandText = commandText;
        IsSelected = isSelected;
    }

    public int UnitId { get; }
    public string UnitName { get; }
    public string CommandText { get; }
    public bool IsSelected { get; }
}

public readonly struct HexTacticsWorldLabelUiData
{
    public HexTacticsWorldLabelUiData(
        int unitId,
        Vector3 worldPosition,
        string title,
        string detail,
        bool isBlueTeam)
    {
        UnitId = unitId;
        WorldPosition = worldPosition;
        Title = title;
        Detail = detail;
        IsBlueTeam = isBlueTeam;
    }

    public int UnitId { get; }
    public Vector3 WorldPosition { get; }
    public string Title { get; }
    public string Detail { get; }
    public bool IsBlueTeam { get; }
}
