using UnityEngine;

public struct ProjectileImpactContext
{
    public ProjectileImpactContext(
        TowerInstance sourceTower,
        ProjectileConfig projectileConfig,
        MonsterBehaviour targetMonster,
        Vector3 impactPosition,
        int attackDamage,
        EffectDefinition impactEffectDefinition)
    {
        SourceTower = sourceTower;
        ProjectileConfig = projectileConfig;
        TargetMonster = targetMonster;
        ImpactPosition = impactPosition;
        AttackDamage = attackDamage;
        ImpactEffectDefinition = impactEffectDefinition;
    }

    public TowerInstance SourceTower { get; }
    public ProjectileConfig ProjectileConfig { get; }
    public MonsterBehaviour TargetMonster { get; }
    public Vector3 ImpactPosition { get; }
    public int AttackDamage { get; }
    public EffectDefinition ImpactEffectDefinition { get; }
}
