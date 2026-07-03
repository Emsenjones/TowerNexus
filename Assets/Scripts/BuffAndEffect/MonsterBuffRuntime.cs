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
        BuffDefinition buffDefinition = request.BuffDefinition;

        if (owner == null || buffDefinition == null || !buffDefinition.IsValid())
        {
            return BuffApplyResult.Invalid;
        }

        if (!buffDefinition.IsElementalStackImmunity &&
            buffDefinition.ElementType != ElementType.None &&
            HasElementalStackImmunity(buffDefinition.ElementType))
        {
            return BuffApplyResult.BlockedByElementalStackImmunity;
        }

        MonsterBuffInstance existingInstance = FindBuffInstance(buffDefinition);

        if (existingInstance == null)
        {
            buffInstances.Add(new MonsterBuffInstance(request, owner));
            return BuffApplyResult.Applied;
        }

        return existingInstance.TryReapply(request);
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

    public bool HasElementalStackImmunity(ElementType elementType)
    {
        if (elementType == ElementType.None)
        {
            return false;
        }

        for (int i = 0; i < buffInstances.Count; i++)
        {
            MonsterBuffInstance buffInstance = buffInstances[i];

            if (buffInstance == null ||
                buffInstance.Definition == null ||
                !buffInstance.Definition.IsElementalStackImmunity ||
                buffInstance.Definition.ElementType != elementType)
            {
                continue;
            }

            return true;
        }

        return false;
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
