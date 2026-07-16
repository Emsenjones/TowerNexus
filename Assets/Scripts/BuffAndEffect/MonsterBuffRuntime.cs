using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterBuffRuntime
{
    private readonly MonsterBehaviour owner;
    private readonly List<MonsterBuffInstance> buffInstances = new List<MonsterBuffInstance>();
    private readonly List<MonsterBuffStateSnapshot> activeSnapshots = new List<MonsterBuffStateSnapshot>();
    private readonly HashSet<MonsterBuffInstance> removalInProgress = new HashSet<MonsterBuffInstance>();

    private int stateMutationDepth;
    private bool stateRefreshPending;

    public MonsterBuffRuntime(MonsterBehaviour owner)
    {
        this.owner = owner;
    }

    public event Action OnStateChanged;

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
                ExecuteLifecycleEffect(buffInstance, BuffEventType.Applied);
                return new BuffApplyOutcome(BuffApplyResult.Applied, buffInstance, false, false);
            }

            if (removalInProgress.Contains(existingInstance))
            {
                return new BuffApplyOutcome(BuffApplyResult.Invalid, existingInstance, true, false);
            }

            int previousStackCount = existingInstance.StackCount;
            BuffApplyResult result = existingInstance.TryReapply(request);

            if (!IsSuccessfulApplyResult(result))
            {
                return new BuffApplyOutcome(result, existingInstance, true, false);
            }

            QueueStateRefresh();

            bool reachedMaxStacks = result == BuffApplyResult.Stacked &&
                                    previousStackCount < buffDefinition.MaxStacks &&
                                    existingInstance.StackCount >= buffDefinition.MaxStacks;

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
                    RemoveBuffInstance(existingInstance);
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
            return RemoveBuffInstance(FindBuffInstance(buffDefinition));
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
                    RemoveBuffInstance(buffInstance);
                }
            }
        }
        finally
        {
            EndStateMutation();
        }
    }

    public void Clear()
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
                RemoveBuffInstance(clearSnapshot[i]);
            }
        }
        finally
        {
            EndStateMutation();
        }
    }

    private bool RemoveBuffInstance(MonsterBuffInstance buffInstance)
    {
        if (buffInstance == null || !IsActive(buffInstance) || !removalInProgress.Add(buffInstance))
        {
            return false;
        }

        try
        {
            ExecuteLifecycleEffect(buffInstance, BuffEventType.Removed);

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

    private void ExecuteLifecycleEffect(MonsterBuffInstance buffInstance, BuffEventType eventType)
    {
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

        if (stateMutationDepth == 0 && stateRefreshPending)
        {
            stateRefreshPending = false;
            RefreshSnapshotsAndNotify();
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
