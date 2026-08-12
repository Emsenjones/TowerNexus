public readonly struct ResolvedTowerCombatStats
{
    public ResolvedTowerCombatStats(
        float attackRange,
        float attackCycleDuration,
        int attackDamage,
        int damageBonus,
        float magicOrbRotationSpeed,
        float droneBurstCooldown)
    {
        AttackRange = attackRange;
        AttackCycleDuration = attackCycleDuration;
        AttackDamage = attackDamage;
        DamageBonus = damageBonus;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        DroneBurstCooldown = droneBurstCooldown;
    }

    public float AttackRange { get; }
    public float AttackCycleDuration { get; }
    public int AttackDamage { get; }
    public int DamageBonus { get; }
    public float MagicOrbRotationSpeed { get; }
    public float DroneBurstCooldown { get; }
}
