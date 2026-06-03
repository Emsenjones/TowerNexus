using UnityEngine;

public struct ProjectileImpactContext
{
    public ProjectileImpactContext(
        TowerInstance sourceTower,
        ProjectileConfig projectileConfig,
        AttackConfig attackConfig,
        MonsterBehaviour targetMonster,
        Vector3 impactPosition,
        int attackDamage,
        EffectConfig impactEffectConfig)
    {
        SourceTower = sourceTower;
        ProjectileConfig = projectileConfig;
        AttackConfig = attackConfig;
        TargetMonster = targetMonster;
        ImpactPosition = impactPosition;
        AttackDamage = attackDamage;
        ImpactEffectConfig = impactEffectConfig;
    }

    public TowerInstance SourceTower { get; }
    public ProjectileConfig ProjectileConfig { get; }
    public AttackConfig AttackConfig { get; }
    public MonsterBehaviour TargetMonster { get; }
    public Vector3 ImpactPosition { get; }
    public int AttackDamage { get; }
    public EffectConfig ImpactEffectConfig { get; }
}
