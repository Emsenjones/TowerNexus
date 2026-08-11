public readonly struct TowerCombatBaseStats
{
    public TowerCombatBaseStats(
        int attackDamage,
        float attackRange,
        float attackCycleDuration,
        float magicOrbRotationSpeed = 0f,
        float droneBurstCooldown = 0f,
        float droneProjectileBonusDamageChance = 0f)
    {
        AttackDamage = attackDamage;
        AttackRange = attackRange;
        AttackCycleDuration = attackCycleDuration;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        DroneBurstCooldown = droneBurstCooldown;
        DroneProjectileBonusDamageChance = droneProjectileBonusDamageChance;
    }

    public int AttackDamage { get; }
    public float AttackRange { get; }
    public float AttackCycleDuration { get; }
    public float MagicOrbRotationSpeed { get; }
    public float DroneBurstCooldown { get; }
    public float DroneProjectileBonusDamageChance { get; }
}
