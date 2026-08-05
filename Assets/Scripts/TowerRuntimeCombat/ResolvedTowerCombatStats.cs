public readonly struct ResolvedTowerCombatStats
{
    public ResolvedTowerCombatStats(
        float attackRange,
        float attackCycleDuration,
        int attackDamage,
        float magicOrbRotationSpeed,
        int magicOrbMaxHitCount,
        float droneBatteryDuration,
        float droneBurstCooldown)
    {
        AttackRange = attackRange;
        AttackCycleDuration = attackCycleDuration;
        AttackDamage = attackDamage;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        MagicOrbMaxHitCount = magicOrbMaxHitCount;
        DroneBatteryDuration = droneBatteryDuration;
        DroneBurstCooldown = droneBurstCooldown;
    }

    public float AttackRange { get; }
    public float AttackCycleDuration { get; }
    public int AttackDamage { get; }
    public float MagicOrbRotationSpeed { get; }
    public int MagicOrbMaxHitCount { get; }
    public float DroneBatteryDuration { get; }
    public float DroneBurstCooldown { get; }
}
