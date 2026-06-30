public readonly struct ResolvedTowerCombatStats
{
    public ResolvedTowerCombatStats(
        float attackRange,
        float attackInterval,
        int attackDamage,
        float magicOrbRotationSpeed,
        int magicOrbMaxHitCount,
        float droneBatteryDuration,
        float droneBurstCooldown)
    {
        AttackRange = attackRange;
        AttackInterval = attackInterval;
        AttackDamage = attackDamage;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        MagicOrbMaxHitCount = magicOrbMaxHitCount;
        DroneBatteryDuration = droneBatteryDuration;
        DroneBurstCooldown = droneBurstCooldown;
    }

    public float AttackRange { get; }
    public float AttackInterval { get; }
    public int AttackDamage { get; }
    public float MagicOrbRotationSpeed { get; }
    public int MagicOrbMaxHitCount { get; }
    public float DroneBatteryDuration { get; }
    public float DroneBurstCooldown { get; }
}
