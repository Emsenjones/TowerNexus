public readonly struct ResolvedTowerCombatStats
{
    public ResolvedTowerCombatStats(
        float attackRange,
        float attackCycleDuration,
        int attackDamage,
        float magicOrbRotationSpeed,
        float droneBurstCooldown,
        float droneProjectileBonusDamageChance)
    {
        AttackRange = attackRange;
        AttackCycleDuration = attackCycleDuration;
        AttackDamage = attackDamage;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        DroneBurstCooldown = droneBurstCooldown;
        DroneProjectileBonusDamageChance = droneProjectileBonusDamageChance;
    }

    public float AttackRange { get; }
    public float AttackCycleDuration { get; }
    public int AttackDamage { get; }
    public float MagicOrbRotationSpeed { get; }
    public float DroneBurstCooldown { get; }
    public float DroneProjectileBonusDamageChance { get; }
}
