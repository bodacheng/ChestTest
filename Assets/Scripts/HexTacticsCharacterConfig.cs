using UnityEngine;

[CreateAssetMenu(menuName = "Hex Tactics/Character Config", fileName = "CharacterConfig")]
public sealed class HexTacticsCharacterConfig : ScriptableObject
{
    [SerializeField] private string displayName = "角色";
    [SerializeField, TextArea(2, 4)] private string description = "近战";
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(1)] private int attackPower = 3;
    [SerializeField, Min(1)] private int cost = 3;
    [SerializeField, Min(1)] private int moveRange = 2;
    [SerializeField] private HexTacticsCharacterVisualArchetype visualArchetype = HexTacticsCharacterVisualArchetype.Stag;
    [SerializeField] private Sprite avatar;

    public string DisplayName => displayName;
    public string Description => description;
    public int MaxHealth => maxHealth;
    public int AttackPower => attackPower;
    public int Cost => cost;
    public int MoveRange => moveRange;
    public HexTacticsCharacterVisualArchetype VisualArchetype => visualArchetype;
    public Sprite Avatar => avatar;

    public void ConfigureRuntime(
        string newDisplayName,
        string newDescription,
        int newMaxHealth,
        int newAttackPower,
        int newCost,
        int newMoveRange,
        HexTacticsCharacterVisualArchetype newVisualArchetype)
    {
        displayName = newDisplayName;
        description = newDescription;
        maxHealth = newMaxHealth;
        attackPower = newAttackPower;
        cost = newCost;
        moveRange = newMoveRange;
        visualArchetype = newVisualArchetype;
        avatar = null;
    }
}

public enum HexTacticsCharacterVisualArchetype
{
    Stag,
    Doe,
    Elk,
    Fawn,
    Tiger,
    WhiteTiger
}
