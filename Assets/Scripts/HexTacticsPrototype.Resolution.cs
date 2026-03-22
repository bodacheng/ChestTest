using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    private void TryResolvePlanningRound()
    {
        if (currentFlowState != FlowState.Planning || isResolving || isAnimating)
        {
            return;
        }

        if (!AreAllBlueCommandsAssigned())
        {
            return;
        }

        AutoPlanCpuCommands();
        StartCoroutine(ResolvePlanningRound());
    }

    private IEnumerator ResolvePlanningRound()
    {
        isResolving = true;
        isAnimating = true;
        currentFlowState = FlowState.Resolving;
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();
        ResetRoundResolutionState();

        yield return new WaitForSeconds(cpuThinkDelay);

        var openingEnemyAttacks = CollectReadyEnemyCommandAttacks();
        var openingAttacks = new List<AttackEvent>(openingEnemyAttacks);
        if (openingAttacks.Count > 0)
        {
            lastResolutionKind = ResolutionKind.Attack;
            resolutionStatus = $"第 {planningRoundNumber} 轮进入攻击回合：{openingAttacks.Count} 次攻击依次结算，之后继续执行移动命令";
            yield return ResolveAttackTurn(openingAttacks);
            resolvedTurnCount++;

            if (CheckVictory())
            {
                isAnimating = false;
                isResolving = false;
                currentFlowState = FlowState.Victory;
                RefreshVisuals();
                yield break;
            }
        }

        var moveTurnIndex = 1;
        while (true)
        {
            if (!HasPendingMoveSteps())
            {
                break;
            }

            var stepResult = EvaluateMoveTurn();
            lastResolutionKind = ResolutionKind.Move;
            yield return ResolveMoveTurn(resolvedTurnCount > 0, moveTurnIndex, stepResult);
            ResolveFriendlyOccupiedMoveGoals(stepResult);

            if (stepResult.SuccessfulMoves.Count > 0)
            {
                AdvanceMovePaths(stepResult.SuccessfulMoves);
                resolvedTurnCount++;

                if (CheckVictory())
                {
                    isAnimating = false;
                    isResolving = false;
                    currentFlowState = FlowState.Victory;
                    RefreshVisuals();
                    yield break;
                }
            }

            if (stepResult.HasEnemyContest)
            {
                LockUnitsForEnemyContact(stepResult.EnemyContacts);
            }

            var postMoveAttacks = CollectPostMoveAttacks(stepResult);
            if (postMoveAttacks.Count == 0 && stepResult.SuccessfulMoves.Count == 0)
            {
                break;
            }

            if (postMoveAttacks.Count > 0)
            {
                lastResolutionKind = ResolutionKind.Attack;
                resolutionStatus = $"第 {planningRoundNumber} 轮移动第 {moveTurnIndex} 回合后，{postMoveAttacks.Count} 名棋子在接敌后发起攻击";
                yield return ResolveAttackTurn(postMoveAttacks);
                resolvedTurnCount++;

                if (CheckVictory())
                {
                    isAnimating = false;
                    isResolving = false;
                    currentFlowState = FlowState.Victory;
                    RefreshVisuals();
                    yield break;
                }
            }

            moveTurnIndex++;
        }

        isAnimating = false;
        isResolving = false;

        if (CheckVictory())
        {
            currentFlowState = FlowState.Victory;
            RefreshVisuals();
            yield break;
        }

        planningRoundNumber++;
        BeginPlanningRound();
    }

    private void ResetRoundResolutionState()
    {
        foreach (var unit in units)
        {
            unit.AttackConsumed = false;
            unit.MovementLocked = false;
            unit.PlannedMoveProgress = 0;
        }
    }

    private List<AttackEvent> CollectReadyEnemyCommandAttacks()
    {
        var events = new List<AttackEvent>();
        foreach (var unit in units)
        {
            if (!IsEnemyCommand(unit) || unit.AttackConsumed)
            {
                continue;
            }

            if (!TryResolveTrackedAttackTarget(unit, out var target, allowAlternateTarget: false))
            {
                if (!IsUnitAlive(unit.PlannedEnemyTargetUnit))
                {
                    unit.MovementLocked = true;
                }

                continue;
            }

            unit.PlannedAttackTarget = target.Coord;
            events.Add(new AttackEvent(unit, target, unit.PlannedSkillIndex));
        }

        return events;
    }

    private List<AttackEvent> CollectReadyFollowUpAttacks()
    {
        var events = new List<AttackEvent>();
        foreach (var unit in units)
        {
            if (unit.AttackConsumed || !IsEnemyCommand(unit))
            {
                continue;
            }

            HexUnit target = null;
            if (!TryResolveTrackedAttackTarget(unit, out target, allowAlternateTarget: true))
            {
                if (!IsUnitAlive(unit.PlannedEnemyTargetUnit))
                {
                    unit.MovementLocked = true;
                }

                continue;
            }

            if (target == null)
            {
                continue;
            }

            unit.PlannedAttackTarget = target.Coord;
            events.Add(new AttackEvent(unit, target, unit.PlannedSkillIndex));
        }

        return events;
    }

    private void ConfigureAnimator(HexUnit unit)
    {
        var animator = unit != null ? unit.Animator : null;
        if (animator == null)
        {
            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.Rebind();
        animator.Update(0f);
        if (UsesDirectStatePlayback(unit))
        {
            PlayBoundState(unit, unit.AnimationBinding.IdleStatePath, restart: true);
            return;
        }

        SetAnimatorLocomotion(animator, 0f, false);
        animator.SetBool(Attack1Hash, false);
        animator.SetBool(DamagedHash, false);
        animator.SetBool(StandHash, false);
        animator.SetBool(SwimHash, false);
        animator.SetBool(FallHash, false);
        animator.SetBool(ActionHash, false);
        animator.SetBool(StunnedHash, false);
    }

    private static void SetAnimatorLocomotion(Animator animator, float vertical, bool shift)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(VerticalHash, vertical);
        animator.SetFloat(HorizontalHash, 0f);
        animator.SetBool(ShiftHash, shift);
    }

    private static bool UsesDirectStatePlayback(HexUnit unit)
    {
        return unit?.Animator != null &&
               unit.AnimationBinding != null &&
               !unit.AnimationBinding.UsesParameterDriver;
    }

    private static void PlayBoundState(HexUnit unit, string statePath, bool restart)
    {
        if (unit?.Animator == null || string.IsNullOrWhiteSpace(statePath))
        {
            return;
        }

        unit.Animator.Play(statePath, 0, restart ? 0f : float.NegativeInfinity);
        unit.Animator.Update(0f);
    }

    private void SetUnitIdle(HexUnit unit)
    {
        if (unit == null || unit.Animator == null)
        {
            return;
        }

        if (UsesDirectStatePlayback(unit))
        {
            var idleState = !string.IsNullOrWhiteSpace(unit.AnimationBinding.IdleStatePath)
                ? unit.AnimationBinding.IdleStatePath
                : unit.AnimationBinding.MoveStatePath;
            PlayBoundState(unit, idleState, restart: true);
            return;
        }

        SetAnimatorLocomotion(unit.Animator, 0f, false);
        unit.Animator.SetBool(Attack1Hash, false);
        unit.Animator.SetBool(DamagedHash, false);
        unit.Animator.SetBool(ActionHash, false);
        unit.Animator.SetBool(FallHash, false);
        unit.Animator.SetBool(SwimHash, false);
        unit.Animator.SetBool(StunnedHash, false);
    }

    private void SetUnitMoving(HexUnit unit, Vector3 worldTarget)
    {
        if (unit == null)
        {
            return;
        }

        FaceUnitTowards(unit, worldTarget, immediate: false);
        if (unit.Animator == null)
        {
            return;
        }

        if (UsesDirectStatePlayback(unit))
        {
            var moveState = !string.IsNullOrWhiteSpace(unit.AnimationBinding.MoveStatePath)
                ? unit.AnimationBinding.MoveStatePath
                : unit.AnimationBinding.IdleStatePath;
            PlayBoundState(unit, moveState, restart: true);
            return;
        }

        var usesFastStride = unit.Speed >= 5 || unit.MoveRange >= 3;
        SetAnimatorLocomotion(unit.Animator, usesFastStride ? 1.15f : 0.82f, usesFastStride);
    }

    private void PlayUnitAttack(HexUnit unit, Vector3 worldTarget)
    {
        if (unit == null)
        {
            return;
        }

        FaceUnitTowards(unit, worldTarget, immediate: true);
        if (unit.Animator == null)
        {
            return;
        }

        if (UsesDirectStatePlayback(unit))
        {
            PlayBoundState(unit, unit.AnimationBinding.AttackStatePath, restart: true);
            return;
        }

        SetAnimatorLocomotion(unit.Animator, 0f, false);
        unit.DamagedAnimationRevision++;
        unit.Animator.SetBool(DamagedHash, false);
        unit.Animator.SetBool(Attack1Hash, false);
        unit.Animator.SetBool(Attack1Hash, true);
    }

    private void StopUnitAttack(HexUnit unit)
    {
        if (unit?.Animator == null)
        {
            return;
        }

        if (UsesDirectStatePlayback(unit))
        {
            return;
        }

        unit.Animator.SetBool(Attack1Hash, false);
    }

    private void PlayUnitDamaged(HexUnit unit)
    {
        if (unit == null || unit.Animator == null)
        {
            return;
        }

        unit.DamagedAnimationRevision++;
        var revision = unit.DamagedAnimationRevision;
        if (UsesDirectStatePlayback(unit))
        {
            PlayBoundState(unit, unit.AnimationBinding.DamagedStatePath, restart: true);
            StartCoroutine(ResetUnitDamagedAfterDelay(unit, revision, ResolveDamagedAnimationDuration(unit)));
            return;
        }

        unit.Animator.SetBool(DamagedHash, false);
        unit.Animator.SetBool(DamagedHash, true);
        StartCoroutine(ResetUnitDamagedAfterDelay(unit, revision, damagedReactionDuration));
    }

    private void PlayUnitHitShake(HexUnit unit, HexUnit attacker)
    {
        if (unit?.VisualRoot == null || hitShakeDuration <= 0.01f || hitShakeDistanceNormalized <= 0.001f)
        {
            return;
        }

        unit.HitShakeRevision++;
        unit.VisualRoot.localPosition = unit.VisualBaseLocalPosition;

        var localDirection = Vector3.forward;
        if (unit.Transform != null)
        {
            var worldDirection = unit.Transform.position - (attacker?.Transform != null
                ? attacker.Transform.position
                : unit.Transform.position - unit.Transform.forward);
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude >= 0.0001f)
            {
                localDirection = unit.Transform.InverseTransformDirection(worldDirection.normalized);
                localDirection.y = 0f;
            }
        }

        if (localDirection.sqrMagnitude < 0.0001f)
        {
            localDirection = Vector3.forward;
        }

        localDirection.Normalize();
        StartCoroutine(AnimateUnitHitShake(unit, unit.HitShakeRevision, localDirection));
    }

    private IEnumerator AnimateUnitHitShake(HexUnit unit, int revision, Vector3 localDirection)
    {
        if (unit?.VisualRoot == null)
        {
            yield break;
        }

        var baseLocalPosition = unit.VisualBaseLocalPosition;
        var amplitude = Mathf.Clamp(unit.SelectionRadius * hitShakeDistanceNormalized, 0.02f, hexRadius * 0.14f);
        var sideDirection = Vector3.Cross(Vector3.up, localDirection);
        if (sideDirection.sqrMagnitude < 0.0001f)
        {
            sideDirection = Vector3.right;
        }

        sideDirection.Normalize();
        var duration = Mathf.Max(0.02f, hitShakeDuration);
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (unit?.VisualRoot == null || unit.HitShakeRevision != revision)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(elapsed / duration);
            var envelope = 1f - Mathf.SmoothStep(0f, 1f, normalizedTime);
            var recoil = Mathf.Sin(normalizedTime * Mathf.PI * 2.35f);
            var lateral = Mathf.Sin(normalizedTime * Mathf.PI * 5.4f) * 0.18f;
            var vertical = Mathf.Sin(normalizedTime * Mathf.PI * 4.6f) * 0.08f;
            var offset =
                localDirection * (recoil * amplitude * envelope) +
                sideDirection * (lateral * amplitude * envelope) +
                Vector3.up * (vertical * amplitude * envelope);
            unit.VisualRoot.localPosition = baseLocalPosition + offset;
            yield return null;
        }

        if (unit?.VisualRoot != null && unit.HitShakeRevision == revision)
        {
            unit.VisualRoot.localPosition = baseLocalPosition;
        }
    }

    private IEnumerator ResetUnitDamagedAfterDelay(HexUnit unit, int revision, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (unit != null &&
            unit.Animator != null &&
            unit.DamagedAnimationRevision == revision)
        {
            if (UsesDirectStatePlayback(unit))
            {
                SetUnitIdle(unit);
            }
            else
            {
                unit.Animator.SetBool(DamagedHash, false);
            }
        }
    }

    private void PlayUnitDeath(HexUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        SetSelectionColliderEnabled(unit, false);
        if (unit.Animator == null)
        {
            return;
        }

        if (UsesDirectStatePlayback(unit))
        {
            PlayBoundState(unit, unit.AnimationBinding.DeathStatePath, restart: true);
            return;
        }

        SetAnimatorLocomotion(unit.Animator, 0f, false);
        unit.Animator.SetBool(Attack1Hash, false);
        unit.Animator.SetBool(DamagedHash, false);
        unit.Animator.SetTrigger(DeathHash);
    }

    private IEnumerator ResetAnimatorBoolAfterDelay(Animator animator, int parameterHash, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null)
        {
            animator.SetBool(parameterHash, false);
        }
    }

    private static void SetSelectionColliderEnabled(HexUnit unit, bool enabled)
    {
        if (unit?.Transform == null)
        {
            return;
        }

        var collider = unit.Transform.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = enabled;
        }
    }

    private void SetAttackCameraFocus(HexUnit attacker, HexUnit defender)
    {
        activeCameraFocusOverride = null;
    }

    private void ClearAttackCameraFocus()
    {
        activeCameraFocusOverride = null;
    }

    private void FaceUnitTowards(HexUnit unit, Vector3 worldTarget, bool immediate)
    {
        if (unit?.Transform == null)
        {
            return;
        }

        var direction = worldTarget - unit.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        unit.Transform.rotation = immediate
            ? targetRotation
            : Quaternion.Slerp(unit.Transform.rotation, targetRotation, 0.4f);
    }

    private void ApplySkillMovementOnCast(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        if (attacker == null || defender == null || skill == null)
        {
            return;
        }

        switch (skill.SelfMovementAttribute)
        {
            case HexTacticsSelfMovementAttribute.Advance:
                TryMoveUnitRelativeToOther(attacker, defender, towardOther: true);
                break;
            case HexTacticsSelfMovementAttribute.Retreat:
                TryMoveUnitRelativeToOther(attacker, defender, towardOther: false);
                break;
        }
    }

    private void ApplySkillCollisionOnHit(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        if (attacker == null || defender == null || skill == null || defender.CurrentHealth <= 0)
        {
            return;
        }

        if (skill.CollisionAttribute == HexTacticsCollisionAttribute.PushTarget)
        {
            TryMoveUnitRelativeToOther(defender, attacker, towardOther: false);
        }
    }

    private bool TryMoveUnitRelativeToOther(HexUnit unitToMove, HexUnit otherUnit, bool towardOther)
    {
        if (unitToMove?.Transform == null || otherUnit?.Transform == null)
        {
            return false;
        }

        var worldDirection = towardOther
            ? otherUnit.Transform.position - unitToMove.Transform.position
            : unitToMove.Transform.position - otherUnit.Transform.position;
        return TryMoveUnitOneCell(unitToMove, worldDirection);
    }

    private bool TryMoveUnitOneCell(HexUnit unit, Vector3 worldDirection)
    {
        if (unit == null || !TryResolveNeighborDirection(worldDirection, out var direction))
        {
            return false;
        }

        var destination = unit.Coord + direction;
        return TryRepositionUnit(unit, destination);
    }

    private bool TryResolveNeighborDirection(Vector3 worldDirection, out HexCoord direction)
    {
        direction = default;
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        var normalizedDirection = worldDirection.normalized;
        var bestDot = float.NegativeInfinity;
        var foundDirection = false;
        foreach (var candidate in NeighborDirections)
        {
            var candidateWorld = HexToWorld(candidate);
            candidateWorld.y = 0f;
            if (candidateWorld.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            var dot = Vector3.Dot(normalizedDirection, candidateWorld.normalized);
            if (dot > bestDot)
            {
                bestDot = dot;
                direction = candidate;
                foundDirection = true;
            }
        }

        return foundDirection;
    }

    private bool TryRepositionUnit(HexUnit unit, HexCoord destination)
    {
        if (unit?.Transform == null || destination == unit.Coord)
        {
            return false;
        }

        if (!cells.TryGetValue(destination, out var destinationCell) || destinationCell.Occupant != null)
        {
            return false;
        }

        if (cells.TryGetValue(unit.Coord, out var sourceCell) && sourceCell.Occupant == unit)
        {
            sourceCell.Occupant = null;
        }

        unit.Coord = destination;
        destinationCell.Occupant = unit;
        unit.Transform.localPosition = CellToUnitPosition(destination);
        return true;
    }

    private IEnumerator ResolveAttackTurn(List<AttackEvent> attacks)
    {
        if (attacks == null || attacks.Count == 0)
        {
            RefreshVisuals();
            yield break;
        }

        var resolvedCount = CountResolvableAttacks(attacks, 0);

        if (resolvedCount == 0)
        {
            RefreshVisuals();
            yield break;
        }

        if (attacks.Count > 1)
        {
            attacks.Sort(CompareAttackEventsByInitiative);
        }

        for (var i = 0; i < attacks.Count; i++)
        {
            var attack = attacks[i];
            if (!CanResolveAttackEvent(attack))
            {
                continue;
            }

            resolvedCount = CountResolvableAttacks(attacks, i);
            var skillLabel = attack.Skill != null ? $" [{attack.Skill.DisplayName}]" : string.Empty;
            resolutionStatus = resolvedCount == 1
                ? $"{attack.Attacker.RoleName}{skillLabel} 攻击 {attack.Defender.RoleName}"
                : $"依次结算：{attack.Attacker.RoleName}{skillLabel} 攻击 {attack.Defender.RoleName}";

            SetAttackCameraFocus(attack.Attacker, attack.Defender);
            yield return ResolveSingleAttack(attack);

            if (TryFocusNextResolvableAttack(attacks, i + 1))
            {
                if (sequentialAttackGap > 0f)
                {
                    yield return new WaitForSeconds(sequentialAttackGap);
                }
            }
            else
            {
                resolvedCount = 0;
            }
        }

        ClearAttackCameraFocus();
        RefreshVisuals();
    }

    private IEnumerator ResolveSingleAttack(AttackEvent attack)
    {
        if (!CanResolveAttackEvent(attack))
        {
            yield break;
        }

        var attacker = attack.Attacker;
        var defender = attack.Defender;
        var skill = attack.Skill;
        if (skill == null)
        {
            yield break;
        }

        attacker.AttackConsumed = true;
        attacker.MovementLocked = true;
        SpendSkillEnergy(attacker, skill);
        ApplySkillMovementOnCast(attacker, defender, skill);

        var attackerStart = attacker.Transform.localPosition;
        var impactToken = attacker.AnimationEventRelay != null ? attacker.AnimationEventRelay.AttackImpactCount : -1;

        PlayUnitAttack(attacker, defender.Transform.position);
        yield return null;

        var attackClip = ResolveAttackClip(attacker);
        var impactDelay = ResolveAttackImpactDelay(attacker, attackClip);
        var swingDuration = ResolveAttackSwingDuration(attacker, impactDelay, attackClip);
        var usesRangedPresentation = UsesRangedAttackPresentation(skill);
        var elapsed = 0f;
        var hitApplied = false;

        if (usesRangedPresentation)
        {
            SpawnAttackReleaseEffect(attacker, defender, impactDelay, skill);
        }

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            if (!usesRangedPresentation)
            {
                var normalizedTime = swingDuration > 0.001f ? Mathf.Clamp01(elapsed / swingDuration) : 1f;
                attacker.Transform.localPosition = CalculateAttackLungePosition(attackerStart, defender, normalizedTime);
            }

            if (!hitApplied && ShouldApplyAttackImpact(attacker, impactToken, elapsed, impactDelay))
            {
                hitApplied = true;
                ApplyAttackImpact(attacker, defender, skill);
            }

            yield return null;
        }

        if (!hitApplied)
        {
            ApplyAttackImpact(attacker, defender, skill);
        }

        attacker.Transform.localPosition = attackerStart;
        StopUnitAttack(attacker);

        if (attacker.CurrentHealth > 0)
        {
            SetUnitIdle(attacker);
        }

        if (defender.CurrentHealth > 0)
        {
            SetUnitIdle(defender);
        }
        else if (units.Contains(defender))
        {
            yield return AnimateUnitDefeat(new List<HexUnit> { defender });
            RemoveUnit(defender);
        }

        RefreshVisuals();
    }

    private bool CanResolveAttackEvent(AttackEvent attack)
    {
        return IsUnitAlive(attack.Attacker) &&
               IsUnitAlive(attack.Defender) &&
               attack.Attacker.Team != attack.Defender.Team &&
               attack.Skill != null &&
               CanUseSkill(attack.Attacker, attack.Skill) &&
               attack.Attacker.Transform != null &&
               attack.Defender.Transform != null &&
               IsWithinAttackRange(attack.Attacker, attack.Defender, attack.Skill);
    }

    private static int CompareAttackEventsByInitiative(AttackEvent left, AttackEvent right)
    {
        var speedComparison = right.Attacker.Speed.CompareTo(left.Attacker.Speed);
        if (speedComparison != 0)
        {
            return speedComparison;
        }

        var tieBreakerComparison = right.InitiativeTieBreaker.CompareTo(left.InitiativeTieBreaker);
        if (tieBreakerComparison != 0)
        {
            return tieBreakerComparison;
        }

        var attackerIdComparison = left.Attacker.Id.CompareTo(right.Attacker.Id);
        if (attackerIdComparison != 0)
        {
            return attackerIdComparison;
        }

        return left.Defender.Id.CompareTo(right.Defender.Id);
    }

    private int CountResolvableAttacks(List<AttackEvent> attacks, int startIndex)
    {
        var count = 0;
        if (attacks == null)
        {
            return count;
        }

        for (var i = Mathf.Max(0, startIndex); i < attacks.Count; i++)
        {
            if (CanResolveAttackEvent(attacks[i]))
            {
                count++;
            }
        }

        return count;
    }

    private bool TryFocusNextResolvableAttack(List<AttackEvent> attacks, int startIndex)
    {
        if (attacks == null)
        {
            return false;
        }

        for (var i = Mathf.Max(0, startIndex); i < attacks.Count; i++)
        {
            var nextAttack = attacks[i];
            if (!CanResolveAttackEvent(nextAttack))
            {
                continue;
            }

            SetAttackCameraFocus(nextAttack.Attacker, nextAttack.Defender);
            return true;
        }

        return false;
    }

    private Vector3 CalculateAttackLungePosition(Vector3 attackerStart, HexUnit defender, float normalizedTime)
    {
        if (defender?.Transform == null)
        {
            return attackerStart;
        }

        var lungeT = normalizedTime < 0.5f
            ? normalizedTime / 0.5f
            : 1f - ((normalizedTime - 0.5f) / 0.5f);
        var smoothed = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lungeT));
        var direction = defender.Transform.localPosition - attackerStart;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();
        var target = attackerStart + direction * (hexRadius * 0.48f);
        return Vector3.Lerp(attackerStart, target, smoothed);
    }

    private static bool ShouldApplyAttackImpact(HexUnit attacker, int impactToken, float elapsed, float impactDelay)
    {
        return (attacker.AnimationEventRelay != null && attacker.AnimationEventRelay.AttackImpactCount > impactToken) ||
               elapsed >= impactDelay;
    }

    private void ApplyAttackImpact(HexUnit attacker, HexUnit defender, HexTacticsSkillConfig skill)
    {
        if (!IsUnitAlive(attacker) || !IsUnitAlive(defender) || skill == null)
        {
            return;
        }

        FaceUnitTowards(defender, attacker.Transform.position, immediate: false);
        defender.CurrentHealth = Mathf.Max(0, defender.CurrentHealth - skill.Power);
        GainSkillEnergy(attacker, skill);
        SpawnHitEffect(attacker, defender, skill);
        PlayUnitHitShake(defender, attacker);
        if (defender.CurrentHealth > 0)
        {
            ApplySkillCollisionOnHit(attacker, defender, skill);
            PlayUnitDamaged(defender);
        }
    }

    private AnimationClip ResolveAttackClip(HexUnit attacker)
    {
        if (attacker?.Animator == null)
        {
            return null;
        }

        if (UsesDirectStatePlayback(attacker) && attacker.AnimationBinding?.AttackClip != null)
        {
            return attacker.AnimationBinding.AttackClip;
        }

        var attackClip = FindAttackClip(attacker.Animator.GetNextAnimatorClipInfo(0));
        if (attackClip != null)
        {
            return attackClip;
        }

        attackClip = FindAttackClip(attacker.Animator.GetCurrentAnimatorClipInfo(0));
        if (attackClip != null)
        {
            return attackClip;
        }

        var controller = attacker.Animator.runtimeAnimatorController;
        if (controller == null)
        {
            return null;
        }

        AnimationClip fallback = null;
        foreach (var clip in controller.animationClips)
        {
            if (!IsAttackClip(clip))
            {
                continue;
            }

            if (fallback == null ||
                clip.name.IndexOf("attack01", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                fallback = clip;
            }
        }

        return fallback;
    }

    private static AnimationClip FindAttackClip(AnimatorClipInfo[] clipInfos)
    {
        if (clipInfos == null)
        {
            return null;
        }

        foreach (var clipInfo in clipInfos)
        {
            if (IsAttackClip(clipInfo.clip))
            {
                return clipInfo.clip;
            }
        }

        return null;
    }

    private static bool IsAttackClip(AnimationClip clip)
    {
        return clip != null &&
               clip.name.IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private float ResolveDamagedAnimationDuration(HexUnit unit)
    {
        var clipLength = unit?.AnimationBinding?.DamagedClip != null && unit.AnimationBinding.DamagedClip.length > 0.01f
            ? unit.AnimationBinding.DamagedClip.length
            : damagedReactionDuration;
        if (UsesDirectStatePlayback(unit) && clipLength > 0.01f)
        {
            return Mathf.Clamp(clipLength * 0.92f, 0.03f, clipLength * 0.98f);
        }

        return Mathf.Clamp(clipLength, damagedReactionDuration, 0.9f);
    }

    private float ResolveAttackImpactDelay(HexUnit attacker, AnimationClip attackClip)
    {
        var clipLength = attackClip != null && attackClip.length > 0.01f
            ? attackClip.length
            : attackDuration;
        var normalizedTime = attacker?.CharacterConfig != null
            ? attacker.CharacterConfig.ResolveAttackImpactNormalizedTime(attackClip)
            : 0.45f;
        return Mathf.Clamp(clipLength * normalizedTime, 0.03f, clipLength);
    }

    private float ResolveAttackSwingDuration(HexUnit attacker, float impactDelay, AnimationClip attackClip)
    {
        var clipLength = attackClip != null && attackClip.length > 0.01f
            ? attackClip.length
            : attackDuration;
        if (UsesDirectStatePlayback(attacker) && clipLength > 0.01f)
        {
            var postImpactSettle = Mathf.Min(0.14f, clipLength * 0.2f);
            var directPlaybackDuration = Mathf.Max(impactDelay + postImpactSettle, clipLength * 0.82f);
            return Mathf.Clamp(directPlaybackDuration, 0.08f, clipLength * 0.98f);
        }

        var settleDuration = Mathf.Max(damagedReactionDuration, 0.1f);
        return Mathf.Max(attackDuration, Mathf.Min(clipLength, impactDelay + settleDuration));
    }

    private IEnumerator ResolveMoveTurn(bool followsAttackPhase, int moveTurnIndex, MoveStepResolution stepResult = default)
    {
        var effectiveResult = stepResult.HasValue ? stepResult : EvaluateMoveTurn();
        if (effectiveResult.Intents.Count == 0)
        {
            resolutionStatus = followsAttackPhase
                ? $"第 {planningRoundNumber} 轮攻击回合结束，移动第 {moveTurnIndex} 回合没有可执行的移动命令"
                : $"第 {planningRoundNumber} 轮进入移动第 {moveTurnIndex} 回合：所有棋子待机";
            yield return new WaitForSeconds(moveDuration * 0.6f);
            RefreshVisuals();
            yield break;
        }

        resolutionStatus = effectiveResult.ConflictCount > 0
            ? $"第 {planningRoundNumber} 轮移动第 {moveTurnIndex} 回合：{effectiveResult.ConflictCount} 处抢格已按速度优先判定，同速随机"
            : followsAttackPhase
                ? $"第 {planningRoundNumber} 轮攻击回合结束，{effectiveResult.SuccessfulMoves.Count} 名棋子继续同步前进一格"
                : $"第 {planningRoundNumber} 轮进入移动第 {moveTurnIndex} 回合：{effectiveResult.SuccessfulMoves.Count} 名棋子同步前进一格";

        if (effectiveResult.SuccessfulMoves.Count == 0)
        {
            yield return new WaitForSeconds(moveDuration * 0.6f);
            RefreshVisuals();
            yield break;
        }

        yield return AnimateMoveTurn(effectiveResult.SuccessfulMoves);
        ApplyMoveResults(effectiveResult.SuccessfulMoves);
        RefreshVisuals();
    }

    private List<MoveIntent> CollectMoveIntents()
    {
        var intents = new List<MoveIntent>();
        foreach (var unit in units)
        {
            if (!HasPendingMoveStep(unit))
            {
                continue;
            }

            if (!TryGetNextMoveStep(unit, out var nextStep))
            {
                continue;
            }

            intents.Add(new MoveIntent(unit, nextStep));
        }

        return intents;
    }

    private HexCoord? GetMoveGoal(HexUnit unit)
    {
        if (unit == null || !unit.HasPlannedMove)
        {
            return null;
        }

        if (IsEnemyCommand(unit))
        {
            return IsUnitAlive(unit.PlannedEnemyTargetUnit) ? unit.PlannedEnemyTargetUnit.Coord : null;
        }

        return unit.PlannedMoveTarget;
    }

    private bool IsMoveCommand(HexUnit unit)
    {
        return unit != null && unit.HasPlannedMove && !IsEnemyCommand(unit);
    }

    private bool IsEnemyCommand(HexUnit unit)
    {
        return unit != null && unit.HasPlannedMove && unit.HasPlannedAttack && unit.PlannedEnemyTargetUnit != null;
    }

    private bool IsUnitAlive(HexUnit unit)
    {
        return unit != null && units.Contains(unit) && unit.CurrentHealth > 0;
    }

    private bool TryGetNextMoveStep(HexUnit unit, out HexCoord nextStep)
    {
        nextStep = default;
        if (!HasPendingMoveStep(unit))
        {
            return false;
        }

        var moveGoal = GetMoveGoal(unit);
        if (!moveGoal.HasValue)
        {
            return false;
        }

        var remainingSteps = Mathf.Max(0, unit.MoveRange - unit.PlannedMoveProgress);
        if (remainingSteps <= 0)
        {
            return false;
        }

        var plannedSkill = IsEnemyCommand(unit) ? unit.PlannedSkill : null;
        var desiredAttackReach = plannedSkill != null ? GetAttackReach(plannedSkill) : 0;
        var path = FindShortestPath(
            unit,
            unit.Coord,
            moveGoal.Value,
            remainingSteps,
            desiredAttackReach,
            plannedSkill,
            allowFriendlyWaitingOccupants: false);

        if (path.Count <= 1)
        {
            return false;
        }

        nextStep = path[1];
        return true;
    }

    private bool TryResolveTrackedAttackTarget(HexUnit unit, out HexUnit target, bool allowAlternateTarget)
    {
        target = null;
        if (unit == null || !IsEnemyCommand(unit))
        {
            return false;
        }

        var plannedSkill = unit.PlannedSkill;
        if (plannedSkill == null || !CanUseSkill(unit, plannedSkill))
        {
            return false;
        }

        if (IsUnitAlive(unit.PlannedEnemyTargetUnit))
        {
            if (!CanSelectAttackTargetFromOrigin(unit, unit.Coord, unit.PlannedEnemyTargetUnit.Coord, plannedSkill))
            {
                if (!allowAlternateTarget)
                {
                    return false;
                }

                var alternateSelection = ChooseAttackSelectionFromOrigin(
                    unit,
                    unit.Coord,
                    unit.Team == Team.Blue ? Team.Red : Team.Blue,
                    forcedSkillIndex: unit.PlannedSkillIndex);
                target = alternateSelection.Target;
                return alternateSelection.HasValue;
            }

            target = unit.PlannedEnemyTargetUnit;
            return true;
        }

        var selection = ChooseAttackSelectionFromOrigin(
            unit,
            unit.Coord,
            unit.Team == Team.Blue ? Team.Red : Team.Blue,
            forcedSkillIndex: unit.PlannedSkillIndex);
        target = selection.Target;
        return selection.HasValue;
    }

    private int GetMoveStepPriority(HexUnit unit, HexCell candidateCell)
    {
        var occupant = candidateCell.Occupant;
        if (occupant == null)
        {
            return 0;
        }

        if (occupant.Team == unit.Team)
        {
            return HasPendingMoveStep(occupant) ? 1 : 2;
        }

        return 3;
    }

    private List<HexCoord> FindShortestPath(
        HexUnit mover,
        HexCoord start,
        HexCoord goal,
        int maxSteps,
        int desiredAttackReach,
        HexTacticsSkillConfig desiredAttackSkill,
        bool allowFriendlyWaitingOccupants)
    {
        var path = new List<HexCoord>();
        if (mover == null || maxSteps < 0 || !cells.ContainsKey(start) || !cells.ContainsKey(goal))
        {
            return path;
        }

        if (desiredAttackSkill != null)
        {
            if (CanSelectAttackTargetFromOrigin(mover, start, goal, desiredAttackSkill))
            {
                path.Add(start);
                return path;
            }
        }
        else if (desiredAttackReach > 0)
        {
            if (HexDistance(start, goal) <= desiredAttackReach)
            {
                path.Add(start);
                return path;
            }
        }
        else if (start == goal)
        {
            path.Add(start);
            return path;
        }

        var frontier = new Queue<HexCoord>();
        var parents = new Dictionary<HexCoord, HexCoord>();
        var distances = new Dictionary<HexCoord, int>();
        var bestCoord = start;
        var bestGoalDistance = GetPathGoalDistance(start, goal, desiredAttackReach);
        var bestTravelDistance = 0;

        frontier.Enqueue(start);
        distances[start] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            var distance = distances[current];
            if (distance >= maxSteps)
            {
                continue;
            }

            foreach (var neighbor in GetPathNeighbors(mover, start, current, goal, desiredAttackReach, allowFriendlyWaitingOccupants))
            {
                if (distances.ContainsKey(neighbor))
                {
                    continue;
                }

                distances[neighbor] = distance + 1;
                parents[neighbor] = current;
                if (IsPathGoalReached(mover, neighbor, goal, desiredAttackReach, desiredAttackSkill))
                {
                    return ReconstructPath(start, neighbor, parents);
                }

                var goalDistance = GetPathGoalDistance(neighbor, goal, desiredAttackReach);
                var travelDistance = distance + 1;
                if (goalDistance < bestGoalDistance ||
                    (goalDistance == bestGoalDistance && travelDistance > bestTravelDistance))
                {
                    bestCoord = neighbor;
                    bestGoalDistance = goalDistance;
                    bestTravelDistance = travelDistance;
                }

                frontier.Enqueue(neighbor);
            }
        }

        if (bestCoord == start)
        {
            path.Add(start);
            return path;
        }

        return ReconstructPath(start, bestCoord, parents);
    }

    private IEnumerable<HexCoord> GetPathNeighbors(
        HexUnit mover,
        HexCoord start,
        HexCoord origin,
        HexCoord goal,
        int desiredAttackReach,
        bool allowFriendlyWaitingOccupants)
    {
        var candidates = new List<HexCoord>();
        foreach (var direction in NeighborDirections)
        {
            var candidate = origin + direction;
            if (!cells.TryGetValue(candidate, out var candidateCell))
            {
                continue;
            }

            if (!CanTraversePathCell(mover, start, origin, candidateCell, goal, desiredAttackReach, allowFriendlyWaitingOccupants))
            {
                continue;
            }

            candidates.Add(candidate);
        }

        candidates.Sort((left, right) =>
        {
            var leftPriority = GetPathCellPriority(mover, cells[left], goal, allowFriendlyWaitingOccupants);
            var rightPriority = GetPathCellPriority(mover, cells[right], goal, allowFriendlyWaitingOccupants);
            if (leftPriority != rightPriority)
            {
                return leftPriority.CompareTo(rightPriority);
            }

            var leftGoalDistance = HexDistance(left, goal);
            var rightGoalDistance = HexDistance(right, goal);
            if (leftGoalDistance != rightGoalDistance)
            {
                return leftGoalDistance.CompareTo(rightGoalDistance);
            }

            var leftCenterBias = HexDistance(left, new HexCoord(0, 0));
            var rightCenterBias = HexDistance(right, new HexCoord(0, 0));
            if (leftCenterBias != rightCenterBias)
            {
                return leftCenterBias.CompareTo(rightCenterBias);
            }

            if (left.Q != right.Q)
            {
                return left.Q.CompareTo(right.Q);
            }

            return left.R.CompareTo(right.R);
        });

        return candidates;
    }

    private bool CanTraversePathCell(
        HexUnit mover,
        HexCoord start,
        HexCoord origin,
        HexCell candidateCell,
        HexCoord goal,
        int desiredAttackReach,
        bool allowFriendlyWaitingOccupants)
    {
        if (candidateCell == null)
        {
            return false;
        }

        if (desiredAttackReach > 0 && candidateCell.Coord == goal)
        {
            return false;
        }

        var occupant = candidateCell.Occupant;
        if (occupant == null || occupant == mover)
        {
            return true;
        }

        if (occupant.Team != mover.Team)
        {
            return false;
        }

        if (allowFriendlyWaitingOccupants || candidateCell.Coord == goal)
        {
            return true;
        }

        var isImmediateStep = origin == start;
        if (isImmediateStep && HasPendingMoveStep(occupant))
        {
            return true;
        }

        if (!HasPendingMoveStep(occupant))
        {
            return false;
        }

        return false;
    }

    private int GetPathCellPriority(HexUnit mover, HexCell candidateCell, HexCoord goal, bool allowFriendlyWaitingOccupants)
    {
        var occupant = candidateCell.Occupant;
        if (occupant == null || occupant == mover)
        {
            return 0;
        }

        if (occupant.Team != mover.Team)
        {
            return 3;
        }

        if (HasPendingMoveStep(occupant))
        {
            return 1;
        }

        return allowFriendlyWaitingOccupants ? 2 : 3;
    }

    private bool IsPathGoalReached(
        HexUnit mover,
        HexCoord coord,
        HexCoord goal,
        int desiredAttackReach,
        HexTacticsSkillConfig desiredAttackSkill)
    {
        if (desiredAttackSkill != null)
        {
            return CanSelectAttackTargetFromOrigin(mover, coord, goal, desiredAttackSkill);
        }

        return desiredAttackReach > 0 ? HexDistance(coord, goal) <= desiredAttackReach : coord == goal;
    }

    private static int GetPathGoalDistance(HexCoord coord, HexCoord goal, int desiredAttackReach)
    {
        var distance = HexDistance(coord, goal);
        return desiredAttackReach > 0 ? Mathf.Max(0, distance - desiredAttackReach) : distance;
    }

    private static List<HexCoord> ReconstructPath(HexCoord start, HexCoord end, Dictionary<HexCoord, HexCoord> parents)
    {
        var path = new List<HexCoord> { end };
        var current = end;
        while (current != start)
        {
            current = parents[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private MoveStepResolution EvaluateMoveTurn()
    {
        var intents = CollectMoveIntents();
        if (intents.Count == 0)
        {
            return new MoveStepResolution(intents, new Dictionary<HexUnit, HexCoord>(), 0, new Dictionary<HexUnit, HashSet<HexUnit>>());
        }

        var winners = ChooseMoveWinners(intents, out var conflictCount);
        var successfulMoves = FilterSuccessfulMoves(winners);
        var enemyContacts = CollectEnemyMoveContacts(intents);
        return new MoveStepResolution(intents, successfulMoves, conflictCount, enemyContacts);
    }

    private List<AttackEvent> CollectPostMoveAttacks(MoveStepResolution stepResult)
    {
        var plannedAttacks = CollectReadyFollowUpAttacks();
        if (!stepResult.HasEnemyContest)
        {
            return plannedAttacks;
        }

        var contactAttacks = BuildContactAttackEvents(stepResult.EnemyContacts);
        return MergeAttackEvents(plannedAttacks, contactAttacks);
    }

    private static List<AttackEvent> MergeAttackEvents(List<AttackEvent> preferredAttacks, List<AttackEvent> fallbackAttacks)
    {
        var merged = new List<AttackEvent>();
        var queuedAttackers = new HashSet<HexUnit>();
        AppendUniqueAttacks(merged, preferredAttacks, queuedAttackers);
        AppendUniqueAttacks(merged, fallbackAttacks, queuedAttackers);
        return merged;
    }

    private static void AppendUniqueAttacks(List<AttackEvent> destination, List<AttackEvent> source, HashSet<HexUnit> queuedAttackers)
    {
        if (source == null)
        {
            return;
        }

        foreach (var attack in source)
        {
            if (attack.Attacker == null || !queuedAttackers.Add(attack.Attacker))
            {
                continue;
            }

            destination.Add(attack);
        }
    }

    private Dictionary<HexUnit, HashSet<HexUnit>> CollectEnemyMoveContacts(List<MoveIntent> intents)
    {
        var contacts = new Dictionary<HexUnit, HashSet<HexUnit>>();
        var groupedByTarget = new Dictionary<HexCoord, List<HexUnit>>();

        foreach (var intent in intents)
        {
            if (!groupedByTarget.TryGetValue(intent.Target, out var contenders))
            {
                contenders = new List<HexUnit>();
                groupedByTarget.Add(intent.Target, contenders);
            }

            contenders.Add(intent.Unit);
        }

        foreach (var pair in groupedByTarget)
        {
            for (var i = 0; i < pair.Value.Count; i++)
            {
                for (var j = i + 1; j < pair.Value.Count; j++)
                {
                    if (pair.Value[i].Team == pair.Value[j].Team)
                    {
                        continue;
                    }

                    RegisterContact(contacts, pair.Value[i], pair.Value[j]);
                }
            }
        }

        foreach (var intent in intents)
        {
            if (!cells.TryGetValue(intent.Target, out var targetCell) || targetCell.Occupant == null)
            {
                continue;
            }

            if (targetCell.Occupant.Team == intent.Unit.Team)
            {
                continue;
            }

            RegisterContact(contacts, intent.Unit, targetCell.Occupant);
        }

        return contacts;
    }

    private void RegisterContact(Dictionary<HexUnit, HashSet<HexUnit>> contacts, HexUnit left, HexUnit right)
    {
        if (!contacts.TryGetValue(left, out var leftTargets))
        {
            leftTargets = new HashSet<HexUnit>();
            contacts.Add(left, leftTargets);
        }

        if (!contacts.TryGetValue(right, out var rightTargets))
        {
            rightTargets = new HashSet<HexUnit>();
            contacts.Add(right, rightTargets);
        }

        leftTargets.Add(right);
        rightTargets.Add(left);
    }

    private List<AttackEvent> BuildContactAttackEvents(Dictionary<HexUnit, HashSet<HexUnit>> contacts)
    {
        var events = new List<AttackEvent>();
        foreach (var pair in contacts)
        {
            var attacker = pair.Key;
            if (!units.Contains(attacker) || attacker.CurrentHealth <= 0 || attacker.AttackConsumed)
            {
                continue;
            }

            HexUnit bestTarget = null;
            var bestScore = int.MinValue;
            foreach (var candidate in pair.Value)
            {
                if (!units.Contains(candidate) || candidate.CurrentHealth <= 0)
                {
                    continue;
                }

                var score = GetUnitThreatPower(candidate) * 10 - candidate.CurrentHealth;
                if (candidate.CurrentHealth <= GetUnitThreatPower(attacker))
                {
                    score += 50;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            if (bestTarget != null)
            {
                var skillIndex = ChooseSkillIndexAgainstTarget(attacker, attacker.Coord, bestTarget, preferNonConsumingSkill: true);
                if (skillIndex >= 0)
                {
                    events.Add(new AttackEvent(attacker, bestTarget, skillIndex));
                }
            }
        }

        return events;
    }

    private void LockUnitsForEnemyContact(Dictionary<HexUnit, HashSet<HexUnit>> contacts)
    {
        foreach (var pair in contacts)
        {
            pair.Key.MovementLocked = true;
        }
    }

    private Dictionary<HexUnit, HexCoord> ChooseMoveWinners(List<MoveIntent> intents, out int conflictCount)
    {
        var grouped = new Dictionary<HexCoord, List<HexUnit>>();
        foreach (var intent in intents)
        {
            if (!grouped.TryGetValue(intent.Target, out var contenders))
            {
                contenders = new List<HexUnit>();
                grouped.Add(intent.Target, contenders);
            }

            contenders.Add(intent.Unit);
        }

        var winners = new Dictionary<HexUnit, HexCoord>();
        conflictCount = 0;

        foreach (var pair in grouped)
        {
            if (pair.Value.Count > 1)
            {
                conflictCount++;
            }

            var highestSpeed = int.MinValue;
            var fastestContenders = new List<HexUnit>();
            foreach (var contender in pair.Value)
            {
                if (contender.Speed > highestSpeed)
                {
                    highestSpeed = contender.Speed;
                    fastestContenders.Clear();
                }

                if (contender.Speed == highestSpeed)
                {
                    fastestContenders.Add(contender);
                }
            }

            var winner = fastestContenders[Random.Range(0, fastestContenders.Count)];
            winners[winner] = pair.Key;
        }

        return winners;
    }

    private Dictionary<HexUnit, HexCoord> FilterSuccessfulMoves(Dictionary<HexUnit, HexCoord> winners)
    {
        var movable = new HashSet<HexUnit>(winners.Keys);
        var changed = true;

        while (changed)
        {
            changed = false;
            var snapshot = new List<HexUnit>(movable);
            foreach (var unit in snapshot)
            {
                var target = winners[unit];
                var occupant = cells[target].Occupant;
                if (occupant == null)
                {
                    continue;
                }

                if (!movable.Contains(occupant))
                {
                    movable.Remove(unit);
                    changed = true;
                }
            }
        }

        var successful = new Dictionary<HexUnit, HexCoord>();
        foreach (var unit in movable)
        {
            successful[unit] = winners[unit];
        }

        return successful;
    }

    private IEnumerator AnimateMoveTurn(Dictionary<HexUnit, HexCoord> moves)
    {
        if (moves.Count == 0)
        {
            yield return new WaitForSeconds(moveDuration * 0.6f);
            yield break;
        }

        var startPositions = new Dictionary<HexUnit, Vector3>();
        foreach (var pair in moves)
        {
            startPositions[pair.Key] = pair.Key.Transform.localPosition;
            SetUnitMoving(pair.Key, unitsRoot.TransformPoint(CellToUnitPosition(pair.Value)));
        }

        var elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / moveDuration);
            var curvedT = Mathf.SmoothStep(0f, 1f, t);

            foreach (var pair in moves)
            {
                var start = startPositions[pair.Key];
                var target = CellToUnitPosition(pair.Value);
                FaceUnitTowards(pair.Key, unitsRoot.TransformPoint(target), immediate: false);
                var position = Vector3.Lerp(start, target, curvedT);
                position.y += Mathf.Sin(curvedT * Mathf.PI) * 0.15f;
                pair.Key.Transform.localPosition = position;
            }

            yield return null;
        }

        foreach (var pair in moves)
        {
            pair.Key.Transform.localPosition = CellToUnitPosition(pair.Value);
            SetUnitIdle(pair.Key);
        }
    }

    private void ApplyMoveResults(Dictionary<HexUnit, HexCoord> moves)
    {
        foreach (var pair in moves)
        {
            if (cells.TryGetValue(pair.Key.Coord, out var sourceCell) && sourceCell.Occupant == pair.Key)
            {
                sourceCell.Occupant = null;
            }
        }

        foreach (var pair in moves)
        {
            pair.Key.Coord = pair.Value;
        }

        foreach (var pair in moves)
        {
            if (cells.TryGetValue(pair.Value, out var destinationCell))
            {
                destinationCell.Occupant = pair.Key;
            }
        }
    }

    private void AdvanceMovePaths(Dictionary<HexUnit, HexCoord> moves)
    {
        foreach (var pair in moves)
        {
            pair.Key.PlannedMoveProgress = Mathf.Min(pair.Key.PlannedMoveProgress + 1, pair.Key.MoveRange);
        }
    }

    private void ResolveFriendlyOccupiedMoveGoals(MoveStepResolution stepResult)
    {
        if (!stepResult.HasValue)
        {
            return;
        }

        foreach (var intent in stepResult.Intents)
        {
            var unit = intent.Unit;
            if (!IsMoveCommand(unit) || unit.Coord == unit.PlannedMoveTarget)
            {
                continue;
            }

            if (HexDistance(unit.Coord, unit.PlannedMoveTarget) != 1 ||
                !cells.TryGetValue(unit.PlannedMoveTarget, out var targetCell))
            {
                continue;
            }

            var occupant = targetCell.Occupant;
            if (occupant == null || occupant == unit || occupant.Team != unit.Team)
            {
                continue;
            }

            unit.PlannedMoveTarget = unit.Coord;
            unit.PlannedPath.Clear();
        }
    }

    private IEnumerator AnimateUnitDefeat(List<HexUnit> defeatedUnits)
    {
        var startScales = new Dictionary<HexUnit, Vector3>();
        foreach (var unit in defeatedUnits)
        {
            startScales[unit] = unit.Transform.localScale;
            PlayUnitDeath(unit);
        }

        yield return new WaitForSeconds(0.22f);

        const float defeatDuration = 0.16f;
        var elapsed = 0f;
        while (elapsed < defeatDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / defeatDuration);
            foreach (var pair in startScales)
            {
                pair.Key.Transform.localScale = Vector3.Lerp(pair.Value, Vector3.zero, t);
            }

            yield return null;
        }

        foreach (var pair in startScales)
        {
            pair.Key.Transform.localScale = Vector3.zero;
        }
    }

    private void RemoveUnit(HexUnit unit)
    {
        if (cells.TryGetValue(unit.Coord, out var source))
        {
            source.Occupant = null;
        }

        var collidersToRemove = new List<Collider>();
        foreach (var pair in unitLookups)
        {
            if (pair.Value == unit)
            {
                collidersToRemove.Add(pair.Key);
            }
        }

        foreach (var collider in collidersToRemove)
        {
            unitLookups.Remove(collider);
        }

        units.Remove(unit);

        if (Application.isPlaying)
        {
            Destroy(unit.Transform.gameObject);
        }
        else
        {
            DestroyImmediate(unit.Transform.gameObject);
        }
    }

    private bool CheckVictory()
    {
        var blueCount = CountAliveUnits(Team.Blue);
        var redCount = CountAliveUnits(Team.Red);

        if (blueCount <= 0)
        {
            winningTeam = Team.Red;
        }
        else if (redCount <= 0)
        {
            winningTeam = Team.Blue;
        }

        return winningTeam.HasValue;
    }

    private int CountAliveUnits(Team team)
    {
        var count = 0;
        foreach (var unit in units)
        {
            if (unit.Team == team)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsValidRosterIndex(int rosterIndex)
    {
        return rosterIndex >= 0 && rosterIndex < characterRoster.Count;
    }

    private int GetTeamCost(List<int> selection)
    {
        var total = 0;
        foreach (var rosterIndex in selection)
        {
            if (IsValidRosterIndex(rosterIndex))
            {
                var config = characterRoster[rosterIndex];
                if (config != null)
                {
                    total += config.Cost;
                }
            }
        }

        return total;
    }

    private int GetPlayerTeamCost()
    {
        var total = 0;
        foreach (var entry in playerDeploymentEntries)
        {
            if (entry.Definition != null)
            {
                total += entry.Definition.Cost;
            }
        }

        return total;
    }

    private bool TryAssignMoveCommand(HexUnit unit, HexCoord target)
    {
        if (!CanSelectMoveTarget(unit, target))
        {
            return false;
        }

        unit.HasPlannedMove = true;
        unit.HasPlannedAttack = false;
        unit.PlannedMoveTarget = target;
        unit.PlannedEnemyTargetUnit = null;
        unit.PlannedAttackTarget = unit.Coord;
        unit.PlannedSkillIndex = -1;
        unit.PlannedPath.Clear();
        unit.PlannedMoveProgress = 0;
        unit.AttackConsumed = false;
        unit.MovementLocked = false;
        unit.HasAssignedCommand = true;
        return true;
    }

    private void TryAssignAttackCommand(HexUnit unit, HexCoord target, int skillIndex)
    {
        if (unit == null)
        {
            return;
        }

        if (!cells.TryGetValue(target, out var targetCell) || targetCell.Occupant == null || targetCell.Occupant.Team == unit.Team)
        {
            return;
        }

        var skill = unit.GetSkillAt(skillIndex);
        if (!HasEnemyTargetAt(unit, target) || !CanUseSkill(unit, skill))
        {
            return;
        }

        unit.HasPlannedMove = true;
        unit.HasPlannedAttack = true;
        unit.PlannedMoveTarget = unit.Coord;
        unit.PlannedEnemyTargetUnit = targetCell.Occupant;
        unit.PlannedAttackTarget = targetCell.Occupant.Coord;
        unit.PlannedSkillIndex = Mathf.Clamp(skillIndex, 0, Mathf.Max(0, unit.SkillCount - 1));
        unit.PlannedPath.Clear();
        unit.PlannedMoveProgress = 0;
        unit.AttackConsumed = false;
        unit.MovementLocked = false;
        unit.HasAssignedCommand = true;
    }

    private void AssignWaitCommand(HexUnit unit, bool markAsAssigned = true)
    {
        unit.HasPlannedMove = false;
        unit.HasPlannedAttack = false;
        unit.PlannedMoveTarget = unit.Coord;
        unit.PlannedAttackTarget = unit.Coord;
        unit.PlannedEnemyTargetUnit = null;
        unit.PlannedSkillIndex = -1;
        unit.PlannedAttackTiming = AttackTiming.BeforeMove;
        unit.PlannedPath.Clear();
        unit.PlannedMoveProgress = 0;
        unit.AttackConsumed = false;
        unit.MovementLocked = false;
        unit.HasAssignedCommand = markAsAssigned;
    }

    private List<HexCell> GetMoveOptions(HexUnit unit)
    {
        var options = new List<HexCell>();
        if (unit == null)
        {
            return options;
        }

        foreach (var cell in cells.Values)
        {
            if (CanSelectMoveTarget(unit, cell.Coord))
            {
                options.Add(cell);
            }
        }

        return options;
    }

    private List<HexCell> GetAttackOptions(HexUnit unit)
    {
        var options = new List<HexCell>();
        if (unit == null)
        {
            return options;
        }

        foreach (var cell in cells.Values)
        {
            if (cell.Occupant != null && CanAssignEnemyTarget(unit, cell.Coord))
            {
                options.Add(cell);
            }
        }

        return options;
    }

    private bool CanSelectMoveTarget(HexUnit unit, HexCoord target)
    {
        if (unit == null || !cells.TryGetValue(target, out var targetCell))
        {
            return false;
        }

        if (target == unit.Coord)
        {
            return false;
        }

        if (HexDistance(unit.Coord, target) > unit.MoveRange)
        {
            return false;
        }

        if (targetCell.Occupant == null)
        {
            return true;
        }

        if (targetCell.Occupant.Team != unit.Team)
        {
            return false;
        }

        return WillUnitAttemptToLeaveCurrentCell(targetCell.Occupant);
    }

    private bool WillUnitAttemptToLeaveCurrentCell(HexUnit unit)
    {
        if (unit == null || !unit.HasPlannedMove || unit.MovementLocked || unit.PlannedMoveProgress >= unit.MoveRange)
        {
            return false;
        }

        if (IsEnemyCommand(unit))
        {
            var plannedSkill = unit.PlannedSkill;
            return plannedSkill != null &&
                   IsUnitAlive(unit.PlannedEnemyTargetUnit) &&
                   !CanSelectAttackTargetFromOrigin(unit, unit.Coord, unit.PlannedEnemyTargetUnit.Coord, plannedSkill);
        }

        return unit.Coord != unit.PlannedMoveTarget;
    }

    private bool CanAssignEnemyTarget(HexUnit unit, HexCoord target)
    {
        return HasEnemyTargetAt(unit, target) && CanUseSkill(unit, unit != null ? unit.SelectedSkill : null);
    }

    private bool HasEnemyTargetAt(HexUnit unit, HexCoord target)
    {
        if (unit == null || !cells.TryGetValue(target, out var targetCell) || targetCell.Occupant == null)
        {
            return false;
        }

        return targetCell.Occupant.Team != unit.Team;
    }

    private bool CanSelectAttackTargetFromOrigin(HexUnit unit, HexCoord origin, HexCoord target, HexTacticsSkillConfig skill)
    {
        if (!HasEnemyTargetAt(unit, target) || !CanUseSkill(unit, skill))
        {
            return false;
        }

        return IsWithinAttackRange(origin, target, skill);
    }

    private void ClearMoveCommand(HexUnit unit)
    {
        AssignWaitCommand(unit);
    }

    private void ClearAttackCommand(HexUnit unit)
    {
        AssignWaitCommand(unit);
    }

    private void ToggleAttackTiming(HexUnit unit)
    {
        AssignWaitCommand(unit);
    }

    private void SanitizeAttackPlan(HexUnit unit)
    {
        if (unit != null && IsEnemyCommand(unit) && !IsUnitAlive(unit.PlannedEnemyTargetUnit))
        {
            AssignWaitCommand(unit);
        }
    }

    private bool HasPendingMoveStep(HexUnit unit)
    {
        if (unit == null || !unit.HasPlannedMove || unit.MovementLocked || unit.PlannedMoveProgress < 0 || unit.PlannedMoveProgress >= unit.MoveRange)
        {
            return false;
        }

        if (IsEnemyCommand(unit))
        {
            var plannedSkill = unit.PlannedSkill;
            return plannedSkill != null &&
                   IsUnitAlive(unit.PlannedEnemyTargetUnit) &&
                   !CanSelectAttackTargetFromOrigin(unit, unit.Coord, unit.PlannedEnemyTargetUnit.Coord, plannedSkill);
        }

        return unit.Coord != unit.PlannedMoveTarget;
    }

    private static bool CanUseSkill(HexUnit unit, HexTacticsSkillConfig skill)
    {
        return unit != null && skill != null && unit.CurrentEnergy >= skill.EnergyCost;
    }

    private static void SpendSkillEnergy(HexUnit unit, HexTacticsSkillConfig skill)
    {
        if (unit == null || skill == null || skill.EnergyCost <= 0)
        {
            return;
        }

        unit.CurrentEnergy = Mathf.Max(0, unit.CurrentEnergy - skill.EnergyCost);
    }

    private static void GainSkillEnergy(HexUnit unit, HexTacticsSkillConfig skill)
    {
        if (unit == null || skill == null || skill.EnergyGainOnHit <= 0)
        {
            return;
        }

        unit.CurrentEnergy = Mathf.Min(unit.MaxEnergy, unit.CurrentEnergy + skill.EnergyGainOnHit);
    }

    private static int GetUnitThreatPower(HexUnit unit)
    {
        if (unit == null || unit.SkillCount <= 0)
        {
            return 1;
        }

        var bestPower = 1;
        for (var i = 0; i < unit.SkillCount; i++)
        {
            var skill = unit.GetSkillAt(i);
            if (skill != null)
            {
                bestPower = Mathf.Max(bestPower, skill.Power);
            }
        }

        return bestPower;
    }

    private bool HasPendingMoveSteps()
    {
        foreach (var unit in units)
        {
            if (HasPendingMoveStep(unit))
            {
                return true;
            }
        }

        return false;
    }

    private HexUnit FindFirstBlueUnitWithoutCommand()
    {
        foreach (var unit in units)
        {
            if (unit.Team == Team.Blue && !unit.HasAssignedCommand)
            {
                return unit;
            }
        }

        return null;
    }

    private bool AreAllBlueCommandsAssigned()
    {
        var hasBlueUnit = false;
        foreach (var unit in units)
        {
            if (unit.Team != Team.Blue)
            {
                continue;
            }

            hasBlueUnit = true;
            if (!unit.HasAssignedCommand)
            {
                return false;
            }
        }

        return hasBlueUnit;
    }

}
