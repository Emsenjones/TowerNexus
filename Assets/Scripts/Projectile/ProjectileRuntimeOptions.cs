public readonly struct ProjectileRuntimeOptions
{
    public bool CanPierce { get; }
    public int MaxPierceHitCount { get; }
    public bool IsBounceChild { get; }
    public bool IsInitialRelease => !IsBounceChild;

    public ProjectileRuntimeOptions(bool canPierce, int maxPierceHitCount, bool isBounceChild = false)
    {
        CanPierce = canPierce;
        MaxPierceHitCount = maxPierceHitCount;
        IsBounceChild = isBounceChild;
    }
}
