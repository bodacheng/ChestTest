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

        var openingMoveAttacks = CollectOpeningMoveCommandAttacks();
        var openingEnemyAttacks = CollectReadyEnemyCommandAttacks();
        var openingAttacks = new List<AttackEvent>(openingMoveAttacks);
        openingAttacks.AddRange(openingEnemyAttacks);
        if (openingAttacks.Count > 0)
        {
            lastResolutionKind = ResolutionKind.Attack;
            resolutionStatus = $"第 {planningRoundNumber} 轮进入攻击回合：{openingAttacks.Count} 次攻击依次结算，之后继续执行移动命令";
            MarkAttackUsage(openingMoveAttacks, lockMovementAfterAttack: false);
            MarkAttackUsage(openingEnemyAttacks, lockMovementAfterAttack: true);
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
            if (stepResult.SuccessfulMoves.Count == 0)
            {
                break;
            }

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

            var followUpAttacks = CollectReadyFollowUpAttacks();
            if (followUpAttacks.Count > 0)
            {
                lastResolutionKind = ResolutionKind.Attack;
                resolutionStatus = $"第 {planningRoundNumber} 轮移动第 {moveTurnIndex} 回合后，{followUpAttacks.Count} 名棋子在接敌后发起攻击";
                MarkAttackUsage(followUpAttacks, lockMovementAfterAttack: true);
                yield return ResolveAttackTurn(followUpAttacks);
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

    private List<AttackEvent> CollectOpeningMoveCommandAttacks()
    {
        var events = new List<AttackEvent>();
        foreach (var unit in units)
        {
            if (!IsMoveCommand(unit) || unit.AttackConsumed)
            {
                continue;
            }

            var target = ChooseCpuAttackTargetFromOrigin(unit, unit.Coord, unit.Team == Team.Blue ? Team.Red : Team.Blue);
            if (target == null)
            {
                continue;
            }

            unit.PlannedAttackTarget = target.Coord;
            events.Add(new AttackEvent(unit, target));
        }

        return events;
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

            if (!IsUnitAlive(unit.PlannedEnemyTargetUnit))
            {
                unit.MovementLocked = true;
                continue;
            }

            unit.PlannedAttackTarget = unit.PlannedEnemyTargetUnit.Coord;
            if (!AreAdjacent(unit.Coord, unit.PlannedEnemyTargetUnit.Coord))
            {
                continue;
            }

            events.Add(new AttackEvent(unit, unit.PlannedEnemyTargetUnit));
        }

        return events;
    }

    private List<AttackEvent> CollectReadyFollowUpAttacks()
    {
        var events = new List<AttackEvent>();
        foreach (var unit in units)
        {
            if (unit.AttackConsumed || !unit.HasPlannedMove)
            {
                continue;
            }

            HexUnit target = null;
            if (IsEnemyCommand(unit))
            {
                if (!IsUnitAlive(unit.PlannedEnemyTargetUnit))
                {
                    unit.MovementLocked = true;
                    continue;
                }

                if (AreAdjacent(unit.Coord, unit.PlannedEnemyTargetUnit.Coord))
                {
                    target = unit.PlannedEnemyTargetUnit;
                }
            }
            else if (IsMoveCommand(unit))
            {
                target = ChooseCpuAttackTargetFromOrigin(unit, unit.Coord, unit.Team == Team.Blue ? Team.Red : Team.Blue);
            }

            if (target == null)
            {
                continue;
            }

            unit.PlannedAttackTarget = target.Coord;
            events.Add(new AttackEvent(unit, target));
        }

        return events;
    }

    private void MarkAttackUsage(List<AttackEvent> attacks, bool lockMovementAfterAttack)
    {
        foreach (var attack in attacks)
        {
            attack.Attacker.AttackConsumed = true;
            if (lockMovementAfterAttack)
            {
                attack.Attacker.MovementLocked = true;
            }
        }
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

        var usesFastStride = unit.MoveRange >= 3;
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
        if (attacker?.Transform == null || defender?.Transform == null)
        {
            return;
        }

        activeCameraFocusOverride = new CameraFocusOverride(attacker, defender);
        ConfigureCamera();
    }

    private void ClearAttackCameraFocus()
    {
        if (activeCameraFocusOverride == null)
        {
            return;
        }

        activeCameraFocusOverride = null;
        ConfigureCamera();
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

        for (var i = 0; i < attacks.Count; i++)
        {
            var attack = attacks[i];
            if (!CanResolveAttackEvent(attack))
            {
                continue;
            }

            resolvedCount = CountResolvableAttacks(attacks, i);
            resolutionStatus = resolvedCount == 1
                ? $"{attack.Attacker.RoleName} 攻击 {attack.Defender.RoleName}"
                : $"依次结算：{attack.Attacker.RoleName} 攻击 {attack.Defender.RoleName}";

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
        var attackerStart = attacker.Transform.localPosition;
        var impactToken = attacker.AnimationEventRelay != null ? attacker.AnimationEventRelay.AttackImpactCount : -1;

        PlayUnitAttack(attacker, defender.Transform.position);
        yield return null;

        var attackClip = ResolveAttackClip(attacker);
        var impactDelay = ResolveAttackImpactDelay(attacker, attackClip);
        var swingDuration = ResolveAttackSwingDuration(attacker, impactDelay, attackClip);
        var elapsed = 0f;
        var hitApplied = false;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            var normalizedTime = swingDuration > 0.001f ? Mathf.Clamp01(elapsed / swingDuration) : 1f;
            attacker.Transform.localPosition = CalculateAttackLungePosition(attackerStart, defender, normalizedTime);

            if (!hitApplied && ShouldApplyAttackImpact(attacker, impactToken, elapsed, impactDelay))
            {
                hitApplied = true;
                ApplyAttackImpact(attacker, defender);
            }

            yield return null;
        }

        if (!hitApplied)
        {
            ApplyAttackImpact(attacker, defender);
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
               attack.Attacker.Transform != null &&
               attack.Defender.Transform != null &&
               AreAdjacent(attack.Attacker.Coord, attack.Defender.Coord);
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

    private void ApplyAttackImpact(HexUnit attacker, HexUnit defender)
    {
        if (!IsUnitAlive(attacker) || !IsUnitAlive(defender))
        {
            return;
        }

        FaceUnitTowards(defender, attacker.Transform.position, immediate: false);
        defender.CurrentHealth = Mathf.Max(0, defender.CurrentHealth - attacker.AttackPower);
        SpawnHitEffect(attacker, defender);
        if (defender.CurrentHealth > 0)
        {
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
            ? $"第 {planningRoundNumber} 轮移动第 {moveTurnIndex} 回合：{effectiveResult.ConflictCount} 处同阵营抢格已随机判定"
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

        var shouldStopAdjacent = IsEnemyCommand(unit);
        var path = FindShortestPath(
            unit,
            unit.Coord,
            moveGoal.Value,
            remainingSteps,
            shouldStopAdjacent,
            allowFriendlyWaitingOccupants: false);

        if (path.Count <= 1)
        {
            return false;
        }

        nextStep = path[1];
        return true;
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
        bool stopAdjacentToGoal,
        bool allowFriendlyWaitingOccupants)
    {
        var path = new List<HexCoord>();
        if (mover == null || maxSteps < 0 || !cells.ContainsKey(start) || !cells.ContainsKey(goal))
        {
            return path;
        }

        if (stopAdjacentToGoal)
        {
            if (start == goal)
            {
                return path;
            }

            if (AreAdjacent(start, goal))
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
        var bestGoalDistance = GetPathGoalDistance(start, goal, stopAdjacentToGoal);
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

            foreach (var neighbor in GetPathNeighbors(mover, start, current, goal, stopAdjacentToGoal, allowFriendlyWaitingOccupants))
            {
                if (distances.ContainsKey(neighbor))
                {
                    continue;
                }

                distances[neighbor] = distance + 1;
                parents[neighbor] = current;
                if (IsPathGoalReached(neighbor, goal, stopAdjacentToGoal))
                {
                    return ReconstructPath(start, neighbor, parents);
                }

                var goalDistance = GetPathGoalDistance(neighbor, goal, stopAdjacentToGoal);
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
        bool stopAdjacentToGoal,
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

            if (!CanTraversePathCell(mover, start, origin, candidateCell, goal, stopAdjacentToGoal, allowFriendlyWaitingOccupants))
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
        bool stopAdjacentToGoal,
        bool allowFriendlyWaitingOccupants)
    {
        if (candidateCell == null)
        {
            return false;
        }

        if (stopAdjacentToGoal && candidateCell.Coord == goal)
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

    private static bool IsPathGoalReached(HexCoord coord, HexCoord goal, bool stopAdjacentToGoal)
    {
        return stopAdjacentToGoal ? AreAdjacent(coord, goal) : coord == goal;
    }

    private static int GetPathGoalDistance(HexCoord coord, HexCoord goal, bool stopAdjacentToGoal)
    {
        var distance = HexDistance(coord, goal);
        return stopAdjacentToGoal ? Mathf.Max(0, distance - 1) : distance;
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
        return new MoveStepResolution(intents, successfulMoves, conflictCount, new Dictionary<HexUnit, HashSet<HexUnit>>());
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

                var score = candidate.AttackPower * 10 - candidate.CurrentHealth;
                if (candidate.CurrentHealth <= attacker.AttackPower)
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
                events.Add(new AttackEvent(attacker, bestTarget));
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

            var winner = pair.Value[Random.Range(0, pair.Value.Count)];
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
        unit.PlannedPath.Clear();
        unit.PlannedMoveProgress = 0;
        unit.AttackConsumed = false;
        unit.MovementLocked = false;
        unit.HasAssignedCommand = true;
        return true;
    }

    private void TryAssignAttackCommand(HexUnit unit, HexCoord target)
    {
        if (!cells.TryGetValue(target, out var targetCell) || targetCell.Occupant == null || targetCell.Occupant.Team == unit.Team)
        {
            return;
        }

        unit.HasPlannedMove = true;
        unit.HasPlannedAttack = true;
        unit.PlannedMoveTarget = unit.Coord;
        unit.PlannedEnemyTargetUnit = targetCell.Occupant;
        unit.PlannedAttackTarget = targetCell.Occupant.Coord;
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
            if (cell.Occupant != null && cell.Occupant.Team != unit.Team && CanSelectAttackTarget(unit, cell.Coord))
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

        if (targetCell.Occupant != null && targetCell.Occupant.Team != unit.Team)
        {
            return false;
        }

        return true;
    }

    private bool CanSelectAttackTarget(HexUnit unit, HexCoord target)
    {
        if (unit == null || !cells.TryGetValue(target, out var targetCell) || targetCell.Occupant == null)
        {
            return false;
        }

        if (targetCell.Occupant.Team == unit.Team)
        {
            return false;
        }

        return true;
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
            return IsUnitAlive(unit.PlannedEnemyTargetUnit) && HexDistance(unit.Coord, unit.PlannedEnemyTargetUnit.Coord) > 1;
        }

        return unit.Coord != unit.PlannedMoveTarget;
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
