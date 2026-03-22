using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class HexTacticsCombatRuleAudit
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags TypeFlags = BindingFlags.Public | BindingFlags.NonPublic;

    [MenuItem("Tools/Hex Tactics/Audit Combat Rules")]
    public static void RunFromMenu()
    {
        RunAudit(logSuccess: true);
    }

    public static void RunBatchMode()
    {
        var success = RunAudit(logSuccess: false);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    private static bool RunAudit(bool logSuccess)
    {
        var failures = new List<string>();

        try
        {
            using (var world = new AuditWorld())
            {
                VerifyAttackTargeting(world, failures);
                VerifyMoveTargeting(world, failures);
                VerifyContactResolution(world, failures);
            }
        }
        catch (Exception exception)
        {
            failures.Add($"Unexpected exception: {exception}");
        }

        if (failures.Count == 0)
        {
            if (logSuccess)
            {
                Debug.Log("HexTactics combat rule audit passed.");
            }

            return true;
        }

        foreach (var failure in failures)
        {
            Debug.LogError(failure);
        }

        Debug.LogError($"HexTactics combat rule audit failed with {failures.Count} issue(s).");
        return false;
    }

    private static void VerifyAttackTargeting(AuditWorld world, List<string> failures)
    {
        var meleeSkill = world.CreateSkill("Audit Strike", power: 3, attackRange: 0, energyCost: 0);
        var blue = world.CreateUnit("Blue Leader", "Blue", 0, 0, meleeSkill, moveRange: 1);
        var blueAlly = world.CreateUnit("Blue Support", "Blue", 1, 0, meleeSkill);
        var red = world.CreateUnit("Red Raider", "Red", 0, 1, meleeSkill);
        world.CreateCell(2, 0);
        world.CreateUnit("Red Far", "Red", 3, 0, meleeSkill);

        Assert(
            !world.CanAssignEnemyTarget(blue, world.Coord(1, 0)),
            "CanAssignEnemyTarget unexpectedly accepted a friendly unit.",
            failures);
        Assert(
            world.CanAssignEnemyTarget(blue, world.Coord(0, 1)),
            "CanAssignEnemyTarget rejected a hostile adjacent unit.",
            failures);
        Assert(
            !world.CanAssignEnemyTarget(blue, world.Coord(3, 0)),
            "CanAssignEnemyTarget accepted an enemy that cannot be reached and attacked this round.",
            failures);

        world.TryAssignAttackCommand(blue, world.Coord(1, 0), 0);
        Assert(
            !world.GetBool(blue, "HasPlannedAttack"),
            "TryAssignAttackCommand created an attack plan against an allied unit.",
            failures);
        Assert(
            world.GetObject(blue, "PlannedEnemyTargetUnit") == null,
            "TryAssignAttackCommand tracked an allied unit as PlannedEnemyTargetUnit.",
            failures);

        world.TryAssignAttackCommand(blue, world.Coord(3, 0), 0);
        Assert(
            !world.GetBool(blue, "HasPlannedAttack"),
            "TryAssignAttackCommand accepted an unreachable enemy target.",
            failures);

        var alliedAttack = world.CreateAttackEvent(blue, blueAlly, 0);
        Assert(
            !world.CanResolveAttackEvent(alliedAttack),
            "CanResolveAttackEvent accepted a same-team attack event.",
            failures);

        var enemyAttack = world.CreateAttackEvent(blue, red, 0);
        Assert(
            world.CanResolveAttackEvent(enemyAttack),
            "CanResolveAttackEvent rejected a valid hostile adjacent attack.",
            failures);
    }

    private static void VerifyMoveTargeting(AuditWorld world, List<string> failures)
    {
        var meleeSkill = world.CreateSkill("Audit Step", power: 2, attackRange: 0, energyCost: 0);
        var mover = world.CreateUnit("Blue Mover", "Blue", 0, 0, meleeSkill, moveRange: 2);
        var waitingAlly = world.CreateUnit("Blue Waiting", "Blue", 1, 0, meleeSkill, moveRange: 2);
        world.CreateUnit("Red Blocker", "Red", 0, 1, meleeSkill, moveRange: 2);
        world.CreateCell(1, -1);
        world.CreateCell(2, 0);

        Assert(
            !world.CanSelectMoveTarget(mover, world.Coord(1, 0)),
            "CanSelectMoveTarget allowed moving onto a friendly unit that is waiting.",
            failures);
        Assert(
            !world.CanSelectMoveTarget(mover, world.Coord(0, 1)),
            "CanSelectMoveTarget allowed moving onto an enemy-occupied cell.",
            failures);
        Assert(
            world.CanSelectMoveTarget(mover, world.Coord(1, -1)),
            "CanSelectMoveTarget rejected an empty in-range cell.",
            failures);
        Assert(
            !world.CanSelectMoveTarget(mover, world.Coord(2, 0)),
            "CanSelectMoveTarget accepted a blocked destination with no traversable path this round.",
            failures);

        world.SetBool(waitingAlly, "HasPlannedMove", true);
        world.SetCoord(waitingAlly, "PlannedMoveTarget", world.Coord(1, -1));
        world.SetInt(waitingAlly, "PlannedMoveProgress", 0);
        world.SetBool(waitingAlly, "MovementLocked", false);

        Assert(
            world.CanSelectMoveTarget(mover, world.Coord(1, 0)),
            "CanSelectMoveTarget did not allow moving into a friendly cell that will be vacated.",
            failures);
        Assert(
            world.CanSelectMoveTarget(mover, world.Coord(2, 0)),
            "CanSelectMoveTarget did not reopen a path once the blocking ally planned to vacate.",
            failures);

        world.SetBool(waitingAlly, "MovementLocked", true);
        Assert(
            !world.CanSelectMoveTarget(mover, world.Coord(1, 0)),
            "CanSelectMoveTarget still allowed a move after the friendly occupant became movement-locked.",
            failures);
        Assert(
            !world.CanSelectMoveTarget(mover, world.Coord(2, 0)),
            "CanSelectMoveTarget still accepted the blocked destination after the path became locked again.",
            failures);
    }

    private static void VerifyContactResolution(AuditWorld world, List<string> failures)
    {
        var meleeSkill = world.CreateSkill("Audit Clash", power: 3, attackRange: 0, energyCost: 0);
        var blueA = world.CreateUnit("Blue A", "Blue", 0, 0, meleeSkill);
        var blueB = world.CreateUnit("Blue B", "Blue", 1, 0, meleeSkill);
        var red = world.CreateUnit("Red A", "Red", 0, 1, meleeSkill);
        world.CreateCell(1, -1);

        var alliedContenders = world.CreateMoveIntentList(
            world.CreateMoveIntent(blueA, world.Coord(1, -1)),
            world.CreateMoveIntent(blueB, world.Coord(1, -1)));
        var alliedContacts = world.CollectEnemyMoveContacts(alliedContenders);
        Assert(
            alliedContacts.Count == 0,
            "CollectEnemyMoveContacts registered contact between same-team movers.",
            failures);

        var contestedCell = world.Coord(1, -1);
        var mixedContenders = world.CreateMoveIntentList(
            world.CreateMoveIntent(blueA, contestedCell),
            world.CreateMoveIntent(red, contestedCell));
        var mixedContacts = world.CollectEnemyMoveContacts(mixedContenders);
        Assert(
            mixedContacts.Count == 2,
            "CollectEnemyMoveContacts failed to register a contested hostile move.",
            failures);

        var alliedContactMap = world.CreateContactMap((blueA, blueB), (blueB, blueA));
        var alliedContactAttacks = world.BuildContactAttackEvents(alliedContactMap);
        Assert(
            alliedContactAttacks.Count == 0,
            "BuildContactAttackEvents produced attacks for same-team contacts.",
            failures);
    }

    private static void Assert(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }

    private sealed class AuditWorld : IDisposable
    {
        private readonly GameObject root;
        private readonly List<UnityEngine.Object> disposableObjects = new();
        private readonly IDictionary cells;
        private readonly IList units;
        private readonly Type coordType;
        private readonly Type cellType;
        private readonly Type unitType;
        private readonly Type teamType;
        private readonly Type moveIntentType;
        private readonly Type attackEventType;
        private readonly Type contactMapType;
        private readonly Type contactSetType;
        private readonly MethodInfo canAssignEnemyTargetMethod;
        private readonly MethodInfo tryAssignAttackCommandMethod;
        private readonly MethodInfo canSelectMoveTargetMethod;
        private readonly MethodInfo canResolveAttackEventMethod;
        private readonly MethodInfo collectEnemyMoveContactsMethod;
        private readonly MethodInfo buildContactAttackEventsMethod;
        private readonly PropertyInfo occupantProperty;

        public AuditWorld()
        {
            var prototypeType = typeof(HexTacticsPrototype);
            coordType = GetNestedType(prototypeType, "HexCoord");
            cellType = GetNestedType(prototypeType, "HexCell");
            unitType = GetNestedType(prototypeType, "HexUnit");
            teamType = GetNestedType(prototypeType, "Team");
            moveIntentType = GetNestedType(prototypeType, "MoveIntent");
            attackEventType = GetNestedType(prototypeType, "AttackEvent");
            contactSetType = typeof(HashSet<>).MakeGenericType(unitType);
            contactMapType = typeof(Dictionary<,>).MakeGenericType(unitType, contactSetType);

            root = new GameObject("HexTacticsCombatRuleAudit");
            Prototype = root.AddComponent<HexTacticsPrototype>();

            cells = (IDictionary)GetField(prototypeType, "cells").GetValue(Prototype);
            units = (IList)GetField(prototypeType, "units").GetValue(Prototype);
            cells.Clear();
            units.Clear();
            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            canAssignEnemyTargetMethod = GetMethod(prototypeType, "CanAssignEnemyTarget");
            tryAssignAttackCommandMethod = GetMethod(prototypeType, "TryAssignAttackCommand");
            canSelectMoveTargetMethod = GetMethod(prototypeType, "CanSelectMoveTarget");
            canResolveAttackEventMethod = GetMethod(prototypeType, "CanResolveAttackEvent");
            collectEnemyMoveContactsMethod = GetMethod(prototypeType, "CollectEnemyMoveContacts");
            buildContactAttackEventsMethod = GetMethod(prototypeType, "BuildContactAttackEvents");

            occupantProperty = cellType.GetProperty("Occupant", InstanceFlags);
        }

        public HexTacticsPrototype Prototype { get; }

        public object Coord(int q, int r)
        {
            return CreateInstance(coordType, q, r);
        }

        public HexTacticsSkillConfig CreateSkill(string displayName, int power, int attackRange, int energyCost)
        {
            var skill = ScriptableObject.CreateInstance<HexTacticsSkillConfig>();
            skill.ConfigureRuntime(displayName, displayName, power, attackRange, energyCost, 1);
            disposableObjects.Add(skill);
            return skill;
        }

        public object CreateCell(int q, int r)
        {
            var coord = Coord(q, r);
            if (cells.Contains(coord))
            {
                return cells[coord];
            }

            var cell = CreateInstance(cellType, coord, null, null);
            cells.Add(coord, cell);
            return cell;
        }

        public object CreateUnit(
            string name,
            string teamName,
            int q,
            int r,
            HexTacticsSkillConfig skill,
            int moveRange = 2,
            int speed = 4)
        {
            var coord = Coord(q, r);
            var cell = CreateCell(q, r);
            var team = Enum.Parse(teamType, teamName);
            var unitRoot = new GameObject(name);
            unitRoot.transform.SetParent(root.transform, false);

            var skills = new List<HexTacticsSkillConfig>();
            if (skill != null)
            {
                skills.Add(skill);
            }

            var unit = CreateInstance(
                unitType,
                units.Count + 1,
                null,
                skills,
                name,
                name,
                team,
                coord,
                unitRoot.transform,
                null,
                null,
                10,
                1,
                moveRange,
                speed,
                3,
                0);

            units.Add(unit);
            occupantProperty.SetValue(cell, unit);
            return unit;
        }

        public bool CanAssignEnemyTarget(object unit, object coord)
        {
            return (bool)canAssignEnemyTargetMethod.Invoke(Prototype, new[] { unit, coord });
        }

        public void TryAssignAttackCommand(object unit, object coord, int skillIndex)
        {
            tryAssignAttackCommandMethod.Invoke(Prototype, new object[] { unit, coord, skillIndex });
        }

        public bool CanSelectMoveTarget(object unit, object coord)
        {
            return (bool)canSelectMoveTargetMethod.Invoke(Prototype, new[] { unit, coord });
        }

        public object CreateAttackEvent(object attacker, object defender, int skillIndex)
        {
            return CreateInstance(attackEventType, attacker, defender, skillIndex);
        }

        public bool CanResolveAttackEvent(object attackEvent)
        {
            return (bool)canResolveAttackEventMethod.Invoke(Prototype, new[] { attackEvent });
        }

        public object CreateMoveIntent(object unit, object target)
        {
            return CreateInstance(moveIntentType, unit, target);
        }

        public IList CreateMoveIntentList(params object[] intents)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(moveIntentType));
            foreach (var intent in intents)
            {
                list.Add(intent);
            }

            return list;
        }

        public IDictionary CollectEnemyMoveContacts(IList moveIntentList)
        {
            return (IDictionary)collectEnemyMoveContactsMethod.Invoke(Prototype, new[] { moveIntentList });
        }

        public IDictionary CreateContactMap(params (object left, object right)[] pairs)
        {
            var map = (IDictionary)Activator.CreateInstance(contactMapType);
            var addMethod = contactSetType.GetMethod("Add", InstanceFlags, null, new[] { unitType }, null);
            var containsMethod = contactSetType.GetMethod("Contains", InstanceFlags, null, new[] { unitType }, null);
            foreach (var pair in pairs)
            {
                if (!map.Contains(pair.left))
                {
                    map.Add(pair.left, Activator.CreateInstance(contactSetType));
                }

                var set = map[pair.left];
                if (!(bool)containsMethod.Invoke(set, new[] { pair.right }))
                {
                    addMethod.Invoke(set, new[] { pair.right });
                }
            }

            return map;
        }

        public IList BuildContactAttackEvents(IDictionary contacts)
        {
            return (IList)buildContactAttackEventsMethod.Invoke(Prototype, new[] { contacts });
        }

        public bool GetBool(object target, string propertyName)
        {
            return (bool)GetProperty(target, propertyName).GetValue(target);
        }

        public void SetBool(object target, string propertyName, bool value)
        {
            GetProperty(target, propertyName).SetValue(target, value);
        }

        public void SetInt(object target, string propertyName, int value)
        {
            GetProperty(target, propertyName).SetValue(target, value);
        }

        public void SetCoord(object target, string propertyName, object coord)
        {
            GetProperty(target, propertyName).SetValue(target, coord);
        }

        public object GetObject(object target, string propertyName)
        {
            return GetProperty(target, propertyName).GetValue(target);
        }

        public void Dispose()
        {
            foreach (var disposable in disposableObjects)
            {
                if (disposable != null)
                {
                    UnityEngine.Object.DestroyImmediate(disposable);
                }
            }

            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Type GetNestedType(Type owner, string name)
        {
            return owner.GetNestedType(name, TypeFlags)
                   ?? throw new MissingMemberException(owner.FullName, name);
        }

        private static FieldInfo GetField(Type owner, string name)
        {
            return owner.GetField(name, InstanceFlags)
                   ?? throw new MissingFieldException(owner.FullName, name);
        }

        private static MethodInfo GetMethod(Type owner, string name)
        {
            return owner.GetMethod(name, InstanceFlags)
                   ?? throw new MissingMethodException(owner.FullName, name);
        }

        private static PropertyInfo GetProperty(object target, string propertyName)
        {
            return target.GetType().GetProperty(propertyName, InstanceFlags)
                   ?? throw new MissingMemberException(target.GetType().FullName, propertyName);
        }

        private static object CreateInstance(Type type, params object[] args)
        {
            foreach (var constructor in type.GetConstructors(InstanceFlags))
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                var compatible = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!IsCompatible(parameters[i].ParameterType, args[i]))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible)
                {
                    return constructor.Invoke(args);
                }
            }

            throw new MissingMethodException($"No compatible constructor found for {type.FullName}.");
        }

        private static bool IsCompatible(Type parameterType, object argument)
        {
            if (argument == null)
            {
                return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;
            }

            if (parameterType.IsInstanceOfType(argument))
            {
                return true;
            }

            if (parameterType.IsEnum && argument.GetType() == parameterType)
            {
                return true;
            }

            return parameterType == argument.GetType();
        }
    }
}
