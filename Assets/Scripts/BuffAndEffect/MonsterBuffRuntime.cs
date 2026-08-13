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

    private int stateMutationDepth;
    private bool stateRefreshPending;
    private bool isPublishingObservations;

    public MonsterBuffRuntime(MonsterBehaviour owner)
    {
        this.owner = owner;
    }

    public event Action OnStateChanged;
    public event Action<BuffRuntimeObservation> OnRuntimeObserved;

    public IReadOnlyList<MonsterBuffStateSnapshot> ActiveSnapshots => activeSnapshots;

    public BuffApplyResult ApplyBuff(BuffApplyRequest request)
    {
        return ApplyBuffWithOutcome(request).Result;
    }

    public BuffApplyOutcome ApplyBuffWithOutcome(BuffApplyRequest request)
    {
        BuffDefinition buffDefinition = request.BuffDefinition;

        if (owner == null || buffDefinition == null || !buffDefinition.IsValid())
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
                false));
            if (stateMutationDepth == 0)
            {
                PublishPendingObservations();
            }
            return new BuffApplyOutcome(BuffApplyResult.Invalid, null, false, false);
        }

        BeginStateMutation();

        try
        {
            MonsterBuffInstance existingInstance = FindBuffInstance(buffDefinition);

            if (existingInstance == null)
            {
                MonsterBuffInstance buffInstance = new MonsterBuffInstance(request, owner);
                buffInstances.Add(buffInstance);
                QueueStateRefresh();
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
                    false));
                ExecuteLifecycleEffect(buffInstance, BuffEventType.Applied);
                return new BuffApplyOutcome(BuffApplyResult.Applied, buffInstance, false, false);
            }

            if (removalInProgress.Contains(existingInstance))
            {
                QueueApplicationObservation(
                    request,
                    existingInstance,
                    BuffApplyResult.Invalid,
                    existingInstance.StackCount,
                    existingInstance.Phase,
                    false);
                return new BuffApplyOutcome(BuffApplyResult.Invalid, existingInstance, true, false);
            }

            int previousStackCount = existingInstance.StackCount;
            BuffRuntimePhase previousPhase = existingInstance.Phase;
            BuffApplyResult result = existingInstance.TryReapply(request);

            if (!IsSuccessfulApplyResult(result))
            {
                QueueApplicationObservation(
                    request,
                    existingInstance,
                    result,
                    previousStackCount,
                    previousPhase,
                    false);
                return new BuffApplyOutcome(result, existingInstance, true, false);
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
                reachedMaxStacks);

            if (result == BuffApplyResult.Stacked)
            {
                ExecuteLifecycleEffect(existingInstance, BuffEventType.StackApplied);
            }

            if (reachedMaxStacks && IsActive(existingInstance))
            {
                ExecuteLifecycleEffect(existingInstance, BuffEventType.Overload);

                if (IsActive(existingInstance) && existingInstance.TryEnterProtectionPhase())
                {
                    QueueStateRefresh();
                    ExecuteLifecycleEffect(existingInstance, BuffEventType.EnteredProtection);
                }
                else if (IsActive(existingInstance))
                {
                    RemoveBuffInstance(
                        existingInstance,
                        BuffRemovalReason.OverloadWithoutProtection);
                }
            }

            return new BuffApplyOutcome(result, existingInstance, true, reachedMaxStacks);
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

    private void ExecuteLifecycleEffect(
        MonsterBuffInstance buffInstance,
        BuffEventType eventType,
        BuffRemovalReason removalReason = BuffRemovalReason.None,
        bool hasStateAfter = true)
    {
        QueueObservation(BuffRuntimeObservation.CreateLifecycleEvent(
            buffInstance,
            eventType,
            removalReason,
            hasStateAfter));

        BuffDefinition buffDefinition = buffInstance != null ? buffInstance.Definition : null;
        MonsterBehaviour buffOwner = buffInstance != null ? buffInstance.Owner : null;
        EffectDefinition effectDefinition = buffDefinition != null ? buffDefinition.GetEffectDefinition(eventType) : null;

        if (effectDefinition == null || buffOwner == null)
        {
            return;
        }

        Transform hitAnchor = buffOwner.HitAnchor;
        Vector3 triggerPosition = hitAnchor != null ? hitAnchor.position : buffOwner.transform.position;

        EffectExecutor.Execute(
            effectDefinition,
            new EffectTriggerContext(
                sourceTower: buffInstance.SourceTower,
                sourceUpgrade: buffInstance.SourceUpgrade,
                targetMonster: buffOwner,
                hasTriggerPosition: true,
                triggerPosition: triggerPosition,
                resolvedDamage: 0,
                allowsElementalApplication: false)
        );
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
        bool reachedMaximumStacks)
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
            reachedMaximumStacks));
    }

    private void QueueObservation(BuffRuntimeObservation observation)
    {
        pendingObservations.Add(observation);
    }

    private void PublishPendingObservations()
    {
        if (isPublishingObservations || pendingObservations.Count == 0)
        {
            return;
        }

        isPublishingObservations = true;

        try
        {
            while (pendingObservations.Count > 0)
            {
                BuffRuntimeObservation[] observations = pendingObservations.ToArray();
                pendingObservations.Clear();

                for (int i = 0; i < observations.Length; i++)
                {
                    PublishObservationSafely(observations[i]);
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
