using System.Collections.Generic;
using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    private sealed class HexCell
    {
        public HexCell(HexCoord coord, MeshRenderer renderer, Material baseMaterial)
        {
            Coord = coord;
            Renderer = renderer;
            BaseMaterial = baseMaterial;
        }

        public HexCoord Coord { get; }
        public MeshRenderer Renderer { get; }
        public Material BaseMaterial { get; }
        public HexUnit Occupant { get; set; }
    }

    private sealed class HexUnit
    {
        public HexUnit(
            string name,
            string roleName,
            Team team,
            HexCoord coord,
            Transform transform,
            MeshRenderer ringRenderer,
            Material defaultRingMaterial,
            int maxHealth,
            int attackPower,
            int cost,
            int moveRange)
        {
            Name = name;
            RoleName = roleName;
            Team = team;
            Coord = coord;
            Transform = transform;
            RingRenderer = ringRenderer;
            DefaultRingMaterial = defaultRingMaterial;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            AttackPower = attackPower;
            Cost = cost;
            MoveRange = moveRange;
            PlannedMoveTarget = coord;
            PlannedAttackTarget = coord;
            PlannedEnemyTargetUnit = null;
            PlannedAttackTiming = AttackTiming.BeforeMove;
        }

        public string Name { get; }
        public string RoleName { get; }
        public Team Team { get; }
        public HexCoord Coord { get; set; }
        public Transform Transform { get; }
        public MeshRenderer RingRenderer { get; }
        public Material DefaultRingMaterial { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; set; }
        public int AttackPower { get; }
        public int Cost { get; }
        public int MoveRange { get; }
        public bool HasPlannedMove { get; set; }
        public HexCoord PlannedMoveTarget { get; set; }
        public bool HasPlannedAttack { get; set; }
        public HexCoord PlannedAttackTarget { get; set; }
        public HexUnit PlannedEnemyTargetUnit { get; set; }
        public AttackTiming PlannedAttackTiming { get; set; }
        public int PlannedMoveProgress { get; set; }
        public bool AttackConsumed { get; set; }
        public bool MovementLocked { get; set; }
        public Transform VisualRoot { get; set; }
        public Animator Animator { get; set; }
        public float VisualHeight { get; set; }
        public float LabelHeight { get; set; }
        public float SelectionRadius { get; set; }
        public List<HexCoord> PlannedPath { get; } = new();
    }

    private readonly struct MoveIntent
    {
        public MoveIntent(HexUnit unit, HexCoord target)
        {
            Unit = unit;
            Target = target;
        }

        public HexUnit Unit { get; }
        public HexCoord Target { get; }
    }

    private readonly struct AttackEvent
    {
        public AttackEvent(HexUnit attacker, HexUnit defender)
        {
            Attacker = attacker;
            Defender = defender;
        }

        public HexUnit Attacker { get; }
        public HexUnit Defender { get; }
    }

    private readonly struct MoveStepResolution
    {
        public MoveStepResolution(
            List<MoveIntent> intents,
            Dictionary<HexUnit, HexCoord> successfulMoves,
            int conflictCount,
            Dictionary<HexUnit, HashSet<HexUnit>> enemyContacts)
        {
            Intents = intents;
            SuccessfulMoves = successfulMoves;
            ConflictCount = conflictCount;
            EnemyContacts = enemyContacts;
        }

        public List<MoveIntent> Intents { get; }
        public Dictionary<HexUnit, HexCoord> SuccessfulMoves { get; }
        public int ConflictCount { get; }
        public Dictionary<HexUnit, HashSet<HexUnit>> EnemyContacts { get; }
        public bool HasEnemyContest => EnemyContacts.Count > 0;
        public bool HasValue => Intents != null;
    }

    [System.Serializable]
    private sealed class CharacterDefinition
    {
        public CharacterDefinition(
            string displayName,
            string description,
            int maxHealth,
            int attackPower,
            int cost,
            int moveRange,
            UnitVisualArchetype visualArchetype)
        {
            this.displayName = displayName;
            this.description = description;
            this.maxHealth = maxHealth;
            this.attackPower = attackPower;
            this.cost = cost;
            this.moveRange = moveRange;
            this.visualArchetype = visualArchetype;
        }

        public string displayName = "角色";
        public string description = "近战";
        [Min(1)] public int maxHealth = 10;
        [Min(1)] public int attackPower = 3;
        [Min(1)] public int cost = 3;
        [Min(1)] public int moveRange = 2;
        public UnitVisualArchetype visualArchetype = UnitVisualArchetype.Stag;
    }

    private enum UnitVisualArchetype
    {
        Stag,
        Doe,
        Elk,
        Fawn,
        Tiger,
        WhiteTiger
    }

    private enum Team
    {
        Blue,
        Red
    }

    private enum FlowState
    {
        ModeSelect,
        TeamBuilder,
        Planning,
        Resolving,
        Victory
    }

    private enum ActionType
    {
        Wait,
        Move,
        Attack
    }

    private enum AttackTiming
    {
        BeforeMove,
        AfterMove
    }

    private enum ResolutionKind
    {
        None,
        Move,
        Attack
    }

    private readonly struct HexCoord : System.IEquatable<HexCoord>
    {
        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }
        public int R { get; }

        public static HexCoord operator +(HexCoord left, HexCoord right)
        {
            return new HexCoord(left.Q + right.Q, left.R + right.R);
        }

        public static bool operator ==(HexCoord left, HexCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoord left, HexCoord right)
        {
            return !left.Equals(right);
        }

        public bool Equals(HexCoord other)
        {
            return other.Q == Q && other.R == R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }
    }
}
