using System.Collections.Generic;
using System;

public class MonsterBuffRuntime
{
    private readonly MonsterBehaviour owner;
    private readonly List<MonsterBuffInstance> buffInstances = new List<MonsterBuffInstance>();
    private readonly List<MonsterBuffStateSnapshot> activeSnapshots = new List<MonsterBuffStateSnapshot>();

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

        MonsterBuffInstance existingInstance = FindBuffInstance(buffDefinition);

        if (existingInstance == null)
        {
            MonsterBuffInstance buffInstance = new MonsterBuffInstance(request, owner);
            buffInstances.Add(buffInstance);
            RefreshSnapshotsAndNotify();
            return new BuffApplyOutcome(BuffApplyResult.Applied, buffInstance, false, false);
        }

        int previousStackCount = existingInstance.StackCount;
        BuffApplyResult result = existingInstance.TryReapply(request);
        bool reachedMaxStacks = result == BuffApplyResult.Stacked &&
                                previousStackCount < buffDefinition.MaxStacks &&
                                existingInstance.StackCount >= buffDefinition.MaxStacks;

        if (IsSuccessfulApplyResult(result))
        {
            RefreshSnapshotsAndNotify();
        }

        return new BuffApplyOutcome(result, existingInstance, true, reachedMaxStacks);
    }

    public bool RemoveBuff(BuffDefinition buffDefinition)
    {
        MonsterBuffInstance buffInstance = FindBuffInstance(buffDefinition);

        if (buffInstance == null)
        {
            return false;
        }

        buffInstances.Remove(buffInstance);
        RefreshSnapshotsAndNotify();
        return true;
    }

    public bool HasBuff(BuffDefinition buffDefinition)
    {
        return FindBuffInstance(buffDefinition) != null;
    }

    public void Tick(float deltaTime)
    {
        bool removedAnyInstance = false;

        for (int i = buffInstances.Count - 1; i >= 0; i--)
        {
            MonsterBuffInstance buffInstance = buffInstances[i];

            if (buffInstance == null || !buffInstance.Tick(deltaTime))
            {
                buffInstances.RemoveAt(i);
                removedAnyInstance = true;
            }
        }

        if (removedAnyInstance)
        {
            RefreshSnapshotsAndNotify();
        }
    }

    public void Clear()
    {
        if (buffInstances.Count == 0 && activeSnapshots.Count == 0)
        {
            return;
        }

        buffInstances.Clear();
        RefreshSnapshotsAndNotify();
    }

    public bool TryEnterProtectionPhase(BuffDefinition buffDefinition)
    {
        MonsterBuffInstance buffInstance = FindBuffInstance(buffDefinition);

        if (buffInstance == null || !buffInstance.TryEnterProtectionPhase())
        {
            return false;
        }

        RefreshSnapshotsAndNotify();
        return true;
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

    private static bool IsSuccessfulApplyResult(BuffApplyResult result)
    {
        return result == BuffApplyResult.Applied ||
               result == BuffApplyResult.Refreshed ||
               result == BuffApplyResult.Stacked;
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
