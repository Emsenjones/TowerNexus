public readonly struct TowerCombatBaseStats
{
    public TowerCombatBaseStats(
        float attackRange,
        float attackInterval,
        float magicOrbRotationSpeed = 0f,
        int magicOrbMaxHitCount = 1,
        float droneBatteryDuration = TowerRuntimeStatResolver.MinimumDroneBatteryDuration,
        float droneBurstCooldown = 0f)
    {
        AttackRange = attackRange;
        AttackInterval = attackInterval;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        MagicOrbMaxHitCount = magicOrbMaxHitCount;
        DroneBatteryDuration = droneBatteryDuration;
        DroneBurstCooldown = droneBurstCooldown;
    }

    public float AttackRange { get; }
    public float AttackInterval { get; }
    public float MagicOrbRotationSpeed { get; }
    public int MagicOrbMaxHitCount { get; }
    public float DroneBatteryDuration { get; }
    public float DroneBurstCooldown { get; }
}
