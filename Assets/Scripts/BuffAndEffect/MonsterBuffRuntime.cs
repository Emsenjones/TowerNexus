using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterBuffRuntime
{
    private readonly MonsterBehaviour owner;
    private readonly List<MonsterBuffInstance> buffInstances = new List<MonsterBuffInstance>();
    private readonly List<MonsterBuffStateSnapshot> activeSnapshots = new List<MonsterBuffStateSnapshot>();
    private readonly HashSet<MonsterBuffInstance> removalInProgress = new HashSet<MonsterBuffInstance>();
    private readonly List<BuffRuntimeObservation> pendingObservations =
        new List<BuffRuntimeObservation>();
    private readonly List<ElementalHitReactionObservation>
        pendingElementalHitReactionObservations =
            new List<ElementalHitReactionObservation>();

    private int stateMutationDepth;
    private bool stateRefreshPending;
    private bool isPublishingObservations;
    private int nextStackingCycleIdentity = 1;

    public MonsterBuffRuntime(MonsterBehaviour owner)
    {
        this.owner = owner;
    }

    public event Action OnStateChanged;
    public event Action<BuffRuntimeObservation> OnRuntimeObserved;
    public event Action<ElementalHitReactionObservation>
        OnElementalHitReactionObserved;

    public IReadOnlyList<MonsterBuffStateSnapshot> ActiveSnapshots => activeSnapshots;

    public BuffApplyResult ApplyBuff(BuffApplyRequest request)
    {
        return ApplyBuffWithOutcome(request).Result;
    }

    public BuffApplyOutcome ApplyBuffWithOutcome(BuffApplyRequest request)
    {
        return ApplyBuffWithOutcome(request, deferOverload: false);
    }

    internal BuffApplyOutcome ApplyBuffWithOutcome(
        BuffApplyRequest request,
        bool deferOverload)
    {
        BuffDefinition buffDefinition = request.BuffDefinition;

        if (owner == null ||
            buffDefinition == null ||
            !buffDefinition.IsValid() ||
            (buffDefinition.UsesStacks && request.RequestedStackUnits <= 0))
        {
            QueueObservation(BuffRuntimeObservation.CreateApplicationAttempt(
                request,
                owner,
                BuffApplyResult.Invalid,
                false,
                0,
                BuffRuntimePhase.Stacking,
                false,
                0,
                BuffRuntimePhase.Stacking,
                false,
                0,
                0,
                0));
            if (stateMutationDepth == 0)
            {
                PublishPendingObservations();
            }
            return new BuffApplyOutcome(
                BuffApplyResult.Invalid,
                null,
                false,
                false,
                0,
                0,
                0);
        }

        BeginStateMutation();

        try
        {
            MonsterBuffInstance existingInstance = FindBuffInstance(buffDefinition);

            if (existingInstance == null)
            {
                MonsterBuffInstance buffInstance = new MonsterBuffInstance(
                    request,
                    owner,
                    nextStackingCycleIdentity++);
                buffInstances.Add(buffInstance);
                QueueStateRefresh();
                int initialEligibleRequestedStackUnits = buffDefinition.UsesStacks
                    ? request.RequestedStackUnits
                    : 0;
                int initialAppliedStackUnits = buffDefinition.UsesStacks
                    ? buffInstance.StackCount
                    : 0;
                int initialDiscardedStackUnits =
                    initialEligibleRequestedStackUnits - initialAppliedStackUnits;
                bool initiallyReachedMaxStacks = buffDefinition.UsesStacks &&
                    buffInstance.StackCount >= buffDefinition.MaximumStacks;
                QueueObservation(BuffRuntimeObservation.CreateApplicationAttempt(
                    request,
                    owner,
                    BuffApplyResult.Applied,
                    false,
                    0,
                    BuffRuntimePhase.Stacking,
                    true,
                    buffInstance.StackCount,
                    buffInstance.Phase,
                    initiallyReachedMaxStacks,
                    initialEligibleRequestedStackUnits,
                    initialAppliedStackUnits,
                    initialDiscardedStackUnits));
                ExecuteLifecycleEffect(buffInstance, BuffEventType.Applied);

                PendingBuffOverload initialPendingOverload = initiallyReachedMaxStacks
                    ? CreatePendingOverload(
                        request,
                        buffInstance,
                        wasExistingBuff: false)
                    : null;

                if (!deferOverload && initialPendingOverload != null)
                {
                    FinalizePendingOverload(initialPendingOverload);
                }

                return new BuffApplyOutcome(
                    BuffApplyResult.Applied,
                    buffInstance,
                    false,
                    initiallyReachedMaxStacks,
                    initialEligibleRequestedStackUnits,
                    initialAppliedStackUnits,
                    initialDiscardedStackUnits,
                    initialPendingOverload);
            }

            if (removalInProgress.Contains(existingInstance))
            {
                QueueApplicationObservation(
                    request,
                    existingInstance,
                    BuffApplyResult.Invalid,
                    existingInstance.StackCount,
                    existingInstance.Phase,
                    false,
                    0,
                    0,
                    0);
                return new BuffApplyOutcome(
                    BuffApplyResult.Invalid,
                    existingInstance,
                    true,
                    false,
                    0,
                    0,
                    0);
            }

            int previousStackCount = existingInstance.StackCount;
            BuffRuntimePhase previousPhase = existingInstance.Phase;
            BuffApplyResult result = existingInstance.TryReapply(
                request,
                out int eligibleRequestedStackUnits,
                out int appliedStackUnits,
                out int discardedStackUnits);

            if (!IsSuccessfulApplyResult(result))
            {
                QueueApplicationObservation(
                    request,
                    existingInstance,
                    result,
                    previousStackCount,
                    previousPhase,
                    false,
                    0,
                    0,
                    0);
                return new BuffApplyOutcome(
                    result,
                    existingInstance,
                    true,
                    false,
                    0,
                    0,
                    0);
            }

            QueueStateRefresh();

            bool reachedMaxStacks = result == BuffApplyResult.Stacked &&
                                    previousStackCount < buffDefinition.MaximumStacks &&
                                    existingInstance.StackCount >= buffDefinition.MaximumStacks;

            QueueApplicationObservation(
                request,
                existingInstance,
                result,
                previousStackCount,
                previousPhase,
                reachedMaxStacks,
                eligibleRequestedStackUnits,
                appliedStackUnits,
                discardedStackUnits);

            if (result == BuffApplyResult.Stacked && appliedStackUnits > 0)
            {
                ExecuteLifecycleEffect(existingInstance, BuffEventType.StackApplied);
            }

            PendingBuffOverload reapplyPendingOverload = reachedMaxStacks
                ? CreatePendingOverload(
                    request,
                    existingInstance,
                    wasExistingBuff: true)
                : null;

            if (!deferOverload && reapplyPendingOverload != null)
            {
                FinalizePendingOverload(reapplyPendingOverload);
            }

            return new BuffApplyOutcome(
                result,
                existingInstance,
                true,
                reachedMaxStacks,
                eligibleRequestedStackUnits,
                appliedStackUnits,
                discardedStackUnits,
                reapplyPendingOverload);
        }
        finally
        {
            EndStateMutation();
        }
    }

    public bool RemoveBuff(BuffDefinition buffDefinition)
    {
        BeginStateMutation();

        try
        {
            return RemoveBuffInstance(
                FindBuffInstance(buffDefinition),
                BuffRemovalReason.ExplicitRemoval);
        }
        finally
        {
            EndStateMutation();
        }
    }

    public bool HasBuff(BuffDefinition buffDefinition)
    {
        return FindBuffInstance(buffDefinition) != null;
    }

    internal void BeginExternalMutation()
    {
        BeginStateMutation();
    }

    internal void EndExternalMutation()
    {
        EndStateMutation();
    }

    internal void ResolveElementalHitReactions(
        TowerInstance triggeringTower,
        TowerDamageSourceIdentity damageSourceIdentity,
        ElementalOpportunityDiagnosticContext diagnostics)
    {
        BeginStateMutation();

        try
        {
            List<MonsterBuffInstance> reactionInstances =
                new List<MonsterBuffInstance>();

            for (int i = 0; i < buffInstances.Count; i++)
            {
                MonsterBuffInstance buffInstance = buffInstances[i];
                BuffDefinition definition =
                    buffInstance != null ? buffInstance.Definition : null;

                if (definition == null ||
                    definition.GetEffectDefinition(
                        BuffEventType.TowerHitReceived) == null)
                {
                    continue;
                }

                reactionInstances.Add(buffInstance);
            }

            reactionInstances.Sort(CompareElementalReactionOrder);
            bool earlierReactionResolvedOwner = false;

            for (int i = 0; i < reactionInstances.Count; i++)
            {
                MonsterBuffInstance buffInstance = reactionInstances[i];

                if (owner == null || owner.CurrentHealth <= 0)
                {
                    QueueElementalHitReactionObservation(
                        buffInstance,
                        triggeringTower,
                        damageSourceIdentity,
                        diagnostics,
                        ElementalHitReactionResult.Invalidated,
                        earlierReactionResolvedOwner
                            ? ElementalHitReactionInvalidationReason
                                .OwnerResolvedByEarlierElementalReaction
                            : ElementalHitReactionInvalidationReason
                                .ReactionTargetInvalidBeforeCommit,
                        successfulDamageTargetCount: 0,
                        committedFixedDamage: 0);
                    continue;
                }

                if (!IsActive(buffInstance) ||
                    buffInstance.Phase != BuffRuntimePhase.Stacking)
                {
                    QueueElementalHitReactionObservation(
                        buffInstance,
                        triggeringTower,
                        damageSourceIdentity,
                        diagnostics,
                        ElementalHitReactionResult.Invalidated,
                        ElementalHitReactionInvalidationReason
                            .BuffInstanceOrCycleChanged,
                        successfulDamageTargetCount: 0,
                        committedFixedDamage: 0);
                    continue;
                }

                if (buffInstance.IsElementalHitReactionCooldownActive)
                {
                    QueueElementalHitReactionObservation(
                        buffInstance,
                        triggeringTower,
                        damageSourceIdentity,
                        diagnostics,
                        ElementalHitReactionResult.CooldownBlocked,
                        ElementalHitReactionInvalidationReason.None,
                        successfulDamageTargetCount: 0,
                        committedFixedDamage: 0);
                    continue;
                }

                BuffDefinition definition = buffInstance.Definition;
                EffectDefinition reactionEffect = definition.GetEffectDefinition(
                    BuffEventType.TowerHitReceived);
                bool reactionCommitted = ExecuteLifecycleEffect(
                    buffInstance,
                    BuffEventType.TowerHitReceived,
                    sourceTowerOverride: triggeringTower,
                    sourceUpgradeOverride:
                        ResolveTriggeringElementalUpgrade(triggeringTower),
                    hasSourceContextOverride: true,
                    triggerPositionOverride:
                        EffectTargetResolver.GetMonsterHitPosition(owner),
                    requiresCommittedActionForExecutionVfx: true,
                    queueObservationOnCommittedActionOnly: true);

                if (!reactionCommitted)
                {
                    QueueElementalHitReactionObservation(
                        buffInstance,
                        triggeringTower,
                        damageSourceIdentity,
                        diagnostics,
                        definition.ElementType == ElementType.Wind
                            ? ElementalHitReactionResult.NoValidTarget
                            : ElementalHitReactionResult.Invalidated,
                        definition.ElementType == ElementType.Wind
                            ? ElementalHitReactionInvalidationReason.None
                            : ElementalHitReactionInvalidationReason
                                .ReactionCommitRejected,
                        successfulDamageTargetCount: 0,
                        committedFixedDamage: 0);
                    continue;
                }

                buffInstance.RecordElementalHitReactionCooldown();
                int fixedDamage = 0;
                reactionEffect?.TryGetElementalHitReactionFixedDamage(
                    definition.ElementType,
                    out fixedDamage);
                QueueElementalHitReactionObservation(
                    buffInstance,
                    triggeringTower,
                    damageSourceIdentity,
                    diagnostics,
                    ElementalHitReactionResult.Triggered,
                    ElementalHitReactionInvalidationReason.None,
                    successfulDamageTargetCount: 1,
                    committedFixedDamage: fixedDamage);

                if (owner.CurrentHealth <= 0)
                {
                    earlierReactionResolvedOwner = true;
                }
            }
        }
        finally
        {
            EndStateMutation();
        }
    }

    public void Tick(float deltaTime)
    {
        BeginStateMutation();

        try
        {
            List<MonsterBuffInstance> tickSnapshot = new List<MonsterBuffInstance>(buffInstances);

            for (int i = 0; i < tickSnapshot.Count; i++)
            {
                MonsterBuffInstance buffInstance = tickSnapshot[i];

                if (buffInstance == null)
                {
                    if (buffInstances.Remove(buffInstance))
                    {
                        QueueStateRefresh();
                    }

                    continue;
                }

                if (!IsActive(buffInstance) || removalInProgress.Contains(buffInstance))
                {
                    continue;
                }

                bool remainsActive = buffInstance.Tick(deltaTime, out int periodicTickCount);

                for (int tickIndex = 0; tickIndex < periodicTickCount && IsActive(buffInstance); tickIndex++)
                {
                    ExecuteLifecycleEffect(buffInstance, BuffEventType.PeriodicTick);
                }

                if (!remainsActive && IsActive(buffInstance))
                {
                    BuffRemovalReason removalReason = buffInstance.IsInProtectionPhase
                        ? BuffRemovalReason.ProtectionExpired
                        : BuffRemovalReason.ActiveDurationExpired;
                    RemoveBuffInstance(buffInstance, removalReason);
                }
            }
        }
        finally
        {
            EndStateMutation();
        }
    }

    public void Clear(BuffRemovalReason removalReason)
    {
        if (buffInstances.Count == 0 && activeSnapshots.Count == 0)
        {
            return;
        }

        BeginStateMutation();

        try
        {
            List<MonsterBuffInstance> clearSnapshot = new List<MonsterBuffInstance>(buffInstances);

            if (clearSnapshot.Count == 0 && activeSnapshots.Count > 0)
            {
                QueueStateRefresh();
            }

            for (int i = 0; i < clearSnapshot.Count; i++)
            {
                RemoveBuffInstance(clearSnapshot[i], removalReason);
            }
        }
        finally
        {
            EndStateMutation();
        }
    }

    private bool RemoveBuffInstance(
        MonsterBuffInstance buffInstance,
        BuffRemovalReason removalReason)
    {
        if (buffInstance == null || !IsActive(buffInstance) || !removalInProgress.Add(buffInstance))
        {
            return false;
        }

        try
        {
            ExecuteLifecycleEffect(
                buffInstance,
                BuffEventType.Removed,
                removalReason,
                hasStateAfter: false);

            if (!buffInstances.Remove(buffInstance))
            {
                return false;
            }

            QueueStateRefresh();
            return true;
        }
        finally
        {
            removalInProgress.Remove(buffInstance);
        }
    }

    private bool IsActive(MonsterBuffInstance buffInstance)
    {
        return buffInstance != null && buffInstances.Contains(buffInstance);
    }

    private MonsterBuffInstance FindBuffInstance(BuffDefinition buffDefinition)
    {
        if (buffDefinition == null)
        {
            return null;
        }

        for (int i = 0; i < buffInstances.Count; i++)
        {
            MonsterBuffInstance buffInstance = buffInstances[i];

            if (buffInstance != null && buffInstance.Definition == buffDefinition)
            {
                return buffInstance;
            }
        }

        return null;
    }

    private bool ExecuteLifecycleEffect(
        MonsterBuffInstance buffInstance,
        BuffEventType eventType,
        BuffRemovalReason removalReason = BuffRemovalReason.None,
        bool hasStateAfter = true,
        TowerInstance sourceTowerOverride = null,
        TowerUpgradeDefinition sourceUpgradeOverride = null,
        bool hasSourceContextOverride = false,
        Vector3? triggerPositionOverride = null,
        bool requiresCommittedActionForExecutionVfx = false,
        EffectDefinition effectDefinitionOverride = null,
        bool queueObservationOnCommittedActionOnly = false)
    {
        if (!queueObservationOnCommittedActionOnly)
        {
            QueueObservation(BuffRuntimeObservation.CreateLifecycleEvent(
                buffInstance,
                eventType,
                removalReason,
                hasStateAfter));
        }

        BuffDefinition buffDefinition = buffInstance != null ? buffInstance.Definition : null;
        MonsterBehaviour buffOwner = buffInstance != null ? buffInstance.Owner : null;
        EffectDefinition effectDefinition = effectDefinitionOverride != null
            ? effectDefinitionOverride
            : buffDefinition != null
                ? buffDefinition.GetEffectDefinition(eventType)
                : null;

        if (effectDefinition == null || buffOwner == null)
        {
            return false;
        }

        Transform hitAnchor = buffOwner.HitAnchor;
        Vector3 triggerPosition = triggerPositionOverride ??
            (hitAnchor != null ? hitAnchor.position : buffOwner.transform.position);

        bool effectCommitted = EffectExecutor.Execute(
            effectDefinition,
            new EffectTriggerContext(
                sourceTower: hasSourceContextOverride
                    ? sourceTowerOverride
                    : buffInstance.SourceTower,
                sourceUpgrade: hasSourceContextOverride
                    ? sourceUpgradeOverride
                    : buffInstance.SourceUpgrade,
                targetMonster: buffOwner,
                hasTriggerPosition: true,
                triggerPosition: triggerPosition,
                allowsElementalApplication: false,
                allowsLifecycleOwnerTarget:
                    eventType == BuffEventType.Removed ||
                    (eventType == BuffEventType.Overload &&
                     buffOwner.CurrentHealth <= 0),
                requiresCommittedActionForExecutionVfx:
                    requiresCommittedActionForExecutionVfx)
        );

        if (queueObservationOnCommittedActionOnly && effectCommitted)
        {
            QueueObservation(BuffRuntimeObservation.CreateLifecycleEvent(
                buffInstance,
                eventType,
                removalReason,
                hasStateAfter));
        }

        return effectCommitted;
    }

    private static bool IsSuccessfulApplyResult(BuffApplyResult result)
    {
        return result == BuffApplyResult.Applied ||
               result == BuffApplyResult.Refreshed ||
               result == BuffApplyResult.Stacked;
    }

    private void BeginStateMutation()
    {
        stateMutationDepth++;
    }

    private void EndStateMutation()
    {
        stateMutationDepth--;

        if (stateMutationDepth != 0)
        {
            return;
        }

        if (stateRefreshPending)
        {
            stateRefreshPending = false;
            RefreshSnapshotsAndNotify();
        }

        PublishPendingObservations();
    }

    private void QueueApplicationObservation(
        BuffApplyRequest request,
        MonsterBuffInstance buffInstance,
        BuffApplyResult result,
        int stackCountBefore,
        BuffRuntimePhase phaseBefore,
        bool reachedMaximumStacks,
        int eligibleRequestedStackUnits,
        int appliedStackUnits,
        int discardedStackUnits)
    {
        QueueObservation(BuffRuntimeObservation.CreateApplicationAttempt(
            request,
            owner,
            result,
            true,
            stackCountBefore,
            phaseBefore,
            buffInstance != null && IsActive(buffInstance),
            buffInstance != null ? buffInstance.StackCount : 0,
            buffInstance != null ? buffInstance.Phase : phaseBefore,
            reachedMaximumStacks,
            eligibleRequestedStackUnits,
            appliedStackUnits,
            discardedStackUnits));
    }

    private PendingBuffOverload CreatePendingOverload(
        BuffApplyRequest request,
        MonsterBuffInstance buffInstance,
        bool wasExistingBuff)
    {
        Vector3 triggerPosition = request.HasTriggerPosition
            ? request.TriggerPosition
            : EffectTargetResolver.GetMonsterHitPosition(owner);

        return new PendingBuffOverload(
            this,
            buffInstance,
            request.SourceTower,
            request.SourceUpgrade,
            triggerPosition,
            wasExistingBuff);
    }

    internal bool FinalizePendingOverload(
        PendingBuffOverload pendingOverload)
    {
        if (pendingOverload == null ||
            pendingOverload.Runtime != this ||
            !pendingOverload.TryConsume())
        {
            return false;
        }

        BeginStateMutation();

        try
        {
            MonsterBuffInstance buffInstance = pendingOverload.BuffInstance;

            if (!IsActive(buffInstance) ||
                buffInstance.Definition != pendingOverload.BuffDefinition ||
                buffInstance.StackingCycleIdentity !=
                    pendingOverload.StackingCycleIdentity)
            {
                return false;
            }

            try
            {
                ExecuteLifecycleEffect(
                    buffInstance,
                    BuffEventType.Overload,
                    sourceTowerOverride: pendingOverload.SourceTower,
                    sourceUpgradeOverride: pendingOverload.SourceUpgrade,
                    hasSourceContextOverride: true,
                    triggerPositionOverride: pendingOverload.TriggerPosition,
                    effectDefinitionOverride: pendingOverload.OverloadEffect);
            }
            finally
            {
                if (IsActive(buffInstance))
                {
                    if (owner == null || owner.CurrentHealth <= 0)
                    {
                        RemoveBuffInstance(
                            buffInstance,
                            BuffRemovalReason.MonsterKilled);
                    }
                    else if (buffInstance.TryEnterProtectionPhase())
                    {
                        QueueStateRefresh();
                        ExecuteLifecycleEffect(
                            buffInstance,
                            BuffEventType.EnteredProtection);
                    }
                    else
                    {
                        RemoveBuffInstance(
                            buffInstance,
                            BuffRemovalReason.OverloadWithoutProtection);
                    }
                }
            }

            return true;
        }
        finally
        {
            EndStateMutation();
        }
    }

    private void QueueElementalHitReactionObservation(
        MonsterBuffInstance buffInstance,
        TowerInstance triggeringTower,
        TowerDamageSourceIdentity damageSourceIdentity,
        ElementalOpportunityDiagnosticContext diagnostics,
        ElementalHitReactionResult result,
        ElementalHitReactionInvalidationReason invalidationReason,
        int successfulDamageTargetCount,
        int committedFixedDamage)
    {
        BuffDefinition definition =
            buffInstance != null ? buffInstance.Definition : null;
        pendingElementalHitReactionObservations.Add(
            new ElementalHitReactionObservation(
                Time.time,
                owner,
                definition,
                buffInstance,
                buffInstance != null
                    ? buffInstance.StackingCycleIdentity
                    : 0,
                triggeringTower,
                ResolveTriggeringElementalUpgrade(triggeringTower),
                damageSourceIdentity,
                diagnostics,
                result,
                invalidationReason,
                definition != null
                    ? definition.GetEffectDefinition(
                        BuffEventType.TowerHitReceived)
                    : null,
                successfulDamageTargetCount,
                committedFixedDamage));
    }

    private static int CompareElementalReactionOrder(
        MonsterBuffInstance left,
        MonsterBuffInstance right)
    {
        ElementType leftElement = left != null && left.Definition != null
            ? left.Definition.ElementType
            : ElementType.None;
        ElementType rightElement = right != null && right.Definition != null
            ? right.Definition.ElementType
            : ElementType.None;
        return leftElement.CompareTo(rightElement);
    }

    private static TowerUpgradeDefinition ResolveTriggeringElementalUpgrade(
        TowerInstance triggeringTower)
    {
        return triggeringTower != null &&
               triggeringTower.TryGetElementalUpgrade(
                   out TowerUpgradeDefinition elementalUpgrade)
            ? elementalUpgrade
            : null;
    }

    private void QueueObservation(BuffRuntimeObservation observation)
    {
        pendingObservations.Add(observation);
    }

    private void PublishPendingObservations()
    {
        if (isPublishingObservations ||
            (pendingObservations.Count == 0 &&
             pendingElementalHitReactionObservations.Count == 0))
        {
            return;
        }

        isPublishingObservations = true;

        try
        {
            while (pendingObservations.Count > 0 ||
                   pendingElementalHitReactionObservations.Count > 0)
            {
                BuffRuntimeObservation[] observations = pendingObservations.ToArray();
                ElementalHitReactionObservation[] reactionObservations =
                    pendingElementalHitReactionObservations.ToArray();
                pendingObservations.Clear();
                pendingElementalHitReactionObservations.Clear();

                for (int i = 0; i < observations.Length; i++)
                {
                    PublishObservationSafely(observations[i]);
                }

                for (int i = 0; i < reactionObservations.Length; i++)
                {
                    PublishElementalHitReactionObservationSafely(
                        reactionObservations[i]);
                }
            }
        }
        finally
        {
            isPublishingObservations = false;
        }
    }

    private void PublishObservationSafely(BuffRuntimeObservation observation)
    {
        Action<BuffRuntimeObservation> handlers = OnRuntimeObserved;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<BuffRuntimeObservation>)invocationList[i]).Invoke(observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, owner);
            }
        }
    }

    private void PublishElementalHitReactionObservationSafely(
        ElementalHitReactionObservation observation)
    {
        Action<ElementalHitReactionObservation> handlers =
            OnElementalHitReactionObserved;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<ElementalHitReactionObservation>)invocationList[i])
                    .Invoke(observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, owner);
            }
        }
    }

    private void QueueStateRefresh()
    {
        stateRefreshPending = true;
    }

    private void RefreshSnapshotsAndNotify()
    {
        activeSnapshots.Clear();

        for (int i = 0; i < buffInstances.Count; i++)
        {
            MonsterBuffInstance buffInstance = buffInstances[i];

            if (buffInstance != null)
            {
                activeSnapshots.Add(new MonsterBuffStateSnapshot(buffInstance));
            }
        }

        OnStateChanged?.Invoke();
    }
}
