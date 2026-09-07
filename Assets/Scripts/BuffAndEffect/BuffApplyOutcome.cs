public readonly struct BuffApplyOutcome
{
    internal BuffApplyOutcome(
        BuffApplyResult result,
        MonsterBuffInstance buffInstance,
        bool wasExistingBuff,
        bool reachedMaxStacks,
        int eligibleRequestedStackUnits,
        int appliedStackUnits,
        int discardedStackUnits,
        PendingBuffOverload pendingOverload = null)
    {
        Result = result;
        BuffInstance = buffInstance;
        WasExistingBuff = wasExistingBuff;
        ReachedMaxStacks = reachedMaxStacks;
        EligibleRequestedStackUnits = eligibleRequestedStackUnits;
        AppliedStackUnits = appliedStackUnits;
        DiscardedStackUnits = discardedStackUnits;
        PendingOverload = pendingOverload;
    }

    public BuffApplyResult Result { get; }
    public MonsterBuffInstance BuffInstance { get; }
    public bool WasExistingBuff { get; }
    public bool ReachedMaxStacks { get; }
    public int EligibleRequestedStackUnits { get; }
    public int AppliedStackUnits { get; }
    public int DiscardedStackUnits { get; }
    internal PendingBuffOverload PendingOverload { get; }
}

internal sealed class PendingBuffOverload
{
    private bool isConsumed;

    internal PendingBuffOverload(
        MonsterBuffRuntime runtime,
        MonsterBuffInstance buffInstance,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        UnityEngine.Vector3 triggerPosition,
        bool wasExistingBuff)
    {
        Runtime = runtime;
        BuffInstance = buffInstance;
        BuffDefinition = buffInstance != null
            ? buffInstance.Definition
            : null;
        OverloadEffect = BuffDefinition != null
            ? BuffDefinition.GetEffectDefinition(BuffEventType.Overload)
            : null;
        StackingCycleIdentity = buffInstance != null
            ? buffInstance.StackingCycleIdentity
            : 0;
        SourceTower = sourceTower;
        SourceUpgrade = sourceUpgrade;
        TriggerPosition = triggerPosition;
        WasExistingBuff = wasExistingBuff;
    }

    internal MonsterBuffRuntime Runtime { get; }
    internal MonsterBuffInstance BuffInstance { get; }
    internal BuffDefinition BuffDefinition { get; }
    internal EffectDefinition OverloadEffect { get; }
    internal int StackingCycleIdentity { get; }
    internal TowerInstance SourceTower { get; }
    internal TowerUpgradeDefinition SourceUpgrade { get; }
    internal UnityEngine.Vector3 TriggerPosition { get; }
    internal bool WasExistingBuff { get; }
    internal bool IsPending => !isConsumed;

    internal bool TryConsume()
    {
        if (isConsumed)
        {
            return false;
        }

        isConsumed = true;
        return true;
    }
}
