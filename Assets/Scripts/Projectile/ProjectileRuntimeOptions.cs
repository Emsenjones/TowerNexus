public struct ProjectileRuntimeOptions
{
    public bool CanPierce { get; }
    public int MaxPierceHitCount { get; }

    public ProjectileRuntimeOptions(bool canPierce, int maxPierceHitCount)
    {
        CanPierce = canPierce;
        MaxPierceHitCount = maxPierceHitCount;
    }
}
