public readonly struct BuffApplyOutcome
{
    public BuffApplyOutcome(
        BuffApplyResult result,
        MonsterBuffInstance buffInstance,
        bool wasExistingBuff,
        bool reachedMaxStacks)
    {
        Result = result;
        BuffInstance = buffInstance;
        WasExistingBuff = wasExistingBuff;
        ReachedMaxStacks = reachedMaxStacks;
    }

    public BuffApplyResult Result { get; }
    public MonsterBuffInstance BuffInstance { get; }
    public bool WasExistingBuff { get; }
    public bool ReachedMaxStacks { get; }
}
