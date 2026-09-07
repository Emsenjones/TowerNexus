public readonly struct TowerCombatBaseStats
{
    public TowerCombatBaseStats(
        float attackRange,
        float attackCycleDuration,
        float magicOrbRotationSpeed = 0f,
        float droneBurstCooldown = 0f)
    {
        AttackRange = attackRange;
        AttackCycleDuration = attackCycleDuration;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        DroneBurstCooldown = droneBurstCooldown;
    }

    public float AttackRange { get; }
    public float AttackCycleDuration { get; }
    public float MagicOrbRotationSpeed { get; }
    public float DroneBurstCooldown { get; }
}
