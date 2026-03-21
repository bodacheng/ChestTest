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
    public int BlueAssignedCommandCount;
    public int BluePendingCommandCount;
    public int PlayerCostLimit;
    public int CpuCostLimit;
    public int PlayerUsedCost;
    public bool HasSelectedUnit;
    public bool CanStartBattle;
    public string CurrentCommandSummary = string.Empty;
    public string CommandProgressSummary = string.Empty;
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

public readonly struct HexTacticsAvatarUiData
{
    public HexTacticsAvatarUiData(Sprite sprite, string fallbackText, Color backgroundColor)
    {
        Sprite = sprite;
        FallbackText = fallbackText;
        BackgroundColor = backgroundColor;
    }

    public Sprite Sprite { get; }
    public string FallbackText { get; }
    public Color BackgroundColor { get; }
}

public readonly struct HexTacticsRosterEntryUiData
{
    public HexTacticsRosterEntryUiData(
        int rosterIndex,
        string displayName,
        string description,
        int maxHealth,
        int attackPower,
        int attackRange,
        int speed,
        int moveRange,
        int cost,
        int maxEnergy,
        int skillCount,
        string primarySkillName,
        HexTacticsAvatarUiData avatar,
        bool canAdd)
    {
        RosterIndex = rosterIndex;
        DisplayName = displayName;
        Description = description;
        MaxHealth = maxHealth;
        AttackPower = attackPower;
        AttackRange = attackRange;
        Speed = speed;
        MoveRange = moveRange;
        Cost = cost;
        MaxEnergy = maxEnergy;
        SkillCount = skillCount;
        PrimarySkillName = primarySkillName;
        Avatar = avatar;
        CanAdd = canAdd;
    }

    public int RosterIndex { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int MaxHealth { get; }
    public int AttackPower { get; }
    public int AttackRange { get; }
    public int Speed { get; }
    public int MoveRange { get; }
    public int Cost { get; }
    public int MaxEnergy { get; }
    public int SkillCount { get; }
    public string PrimarySkillName { get; }
    public HexTacticsAvatarUiData Avatar { get; }
    public bool CanAdd { get; }
}

public readonly struct HexTacticsSelectionEntryUiData
{
    public HexTacticsSelectionEntryUiData(
        int entryId,
        int displayIndex,
        string displayName,
        int maxHealth,
        int attackPower,
        int attackRange,
        int speed,
        int moveRange,
        int cost,
        int maxEnergy,
        int skillCount,
        string primarySkillName,
        string deploymentText,
        HexTacticsAvatarUiData avatar)
    {
        EntryId = entryId;
        DisplayIndex = displayIndex;
        DisplayName = displayName;
        MaxHealth = maxHealth;
        AttackPower = attackPower;
        AttackRange = attackRange;
        Speed = speed;
        MoveRange = moveRange;
        Cost = cost;
        MaxEnergy = maxEnergy;
        SkillCount = skillCount;
        PrimarySkillName = primarySkillName;
        DeploymentText = deploymentText;
        Avatar = avatar;
    }

    public int EntryId { get; }
    public int DisplayIndex { get; }
    public string DisplayName { get; }
    public int MaxHealth { get; }
    public int AttackPower { get; }
    public int AttackRange { get; }
    public int Speed { get; }
    public int MoveRange { get; }
    public int Cost { get; }
    public int MaxEnergy { get; }
    public int SkillCount { get; }
    public string PrimarySkillName { get; }
    public string DeploymentText { get; }
    public HexTacticsAvatarUiData Avatar { get; }
}

public readonly struct HexTacticsCommandEntryUiData
{
    public HexTacticsCommandEntryUiData(
        int unitId,
        string unitName,
        string commandText,
        bool canCycleSkill,
        bool isSelected,
        bool hasAssignedCommand,
        HexTacticsAvatarUiData avatar)
    {
        UnitId = unitId;
        UnitName = unitName;
        CommandText = commandText;
        CanCycleSkill = canCycleSkill;
        IsSelected = isSelected;
        HasAssignedCommand = hasAssignedCommand;
        Avatar = avatar;
    }

    public int UnitId { get; }
    public string UnitName { get; }
    public string CommandText { get; }
    public bool CanCycleSkill { get; }
    public bool IsSelected { get; }
    public bool HasAssignedCommand { get; }
    public HexTacticsAvatarUiData Avatar { get; }
}

public readonly struct HexTacticsWorldLabelUiData
{
    public HexTacticsWorldLabelUiData(
        int unitId,
        Vector3 worldPosition,
        string title,
        string detail,
        bool isBlueTeam,
        int currentHealth,
        int maxHealth)
    {
        UnitId = unitId;
        WorldPosition = worldPosition;
        Title = title;
        Detail = detail;
        IsBlueTeam = isBlueTeam;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }

    public int UnitId { get; }
    public Vector3 WorldPosition { get; }
    public string Title { get; }
    public string Detail { get; }
    public bool IsBlueTeam { get; }
    public int CurrentHealth { get; }
    public int MaxHealth { get; }
    public float HealthNormalized => MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)CurrentHealth / MaxHealth);
}
