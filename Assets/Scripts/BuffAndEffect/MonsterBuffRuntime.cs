using System.Collections.Generic;

public class MonsterBuffRuntime
{
    private readonly MonsterBehaviour owner;
    private readonly List<MonsterBuffInstance> buffInstances = new List<MonsterBuffInstance>();

    public MonsterBuffRuntime(MonsterBehaviour owner)
    {
        this.owner = owner;
    }

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
            return new BuffApplyOutcome(BuffApplyResult.Applied, buffInstance, false, false);
        }

        int previousStackCount = existingInstance.StackCount;
        BuffApplyResult result = existingInstance.TryReapply(request);
        bool reachedMaxStacks = result == BuffApplyResult.Stacked &&
                                previousStackCount < buffDefinition.MaxStacks &&
                                existingInstance.StackCount >= buffDefinition.MaxStacks;
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
        return true;
    }

    public bool HasBuff(BuffDefinition buffDefinition)
    {
        return FindBuffInstance(buffDefinition) != null;
    }

    public void Tick(float deltaTime)
    {
        for (int i = buffInstances.Count - 1; i >= 0; i--)
        {
            MonsterBuffInstance buffInstance = buffInstances[i];

            if (buffInstance == null || !buffInstance.Tick(deltaTime))
            {
                buffInstances.RemoveAt(i);
            }
        }
    }

    public void Clear()
    {
        buffInstances.Clear();
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
}
