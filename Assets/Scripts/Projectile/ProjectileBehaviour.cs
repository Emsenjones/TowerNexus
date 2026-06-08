using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private ProjectileConfig projectileConfig;
    private AttackConfig attackConfig;
    private MonsterBehaviour targetMonster;
    private Vector3 targetPosition;
    private Vector3 launchDirection;
    private Vector3 startPosition;
    private float elapsedLifetime;
    private float arcTravelTime;
    private bool isInitialized;
    private bool hasImpacted;

    public event Action<ProjectileImpactContext> OnImpact;

    public TowerInstance SourceTower => sourceTower;
    public ProjectileConfig ProjectileConfig => projectileConfig;
    public AttackConfig AttackConfig => attackConfig;
    public MonsterBehaviour TargetMonster => targetMonster;
    public Vector3 TargetPosition => targetPosition;
    public bool IsInitialized => isInitialized;

    public void Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        ProjectileConfig projectileConfig,
        AttackConfig attackConfig,
        MonsterBehaviour targetMonster,
        Vector3 targetPosition)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.projectileConfig = projectileConfig;
        this.attackConfig = attackConfig;
        this.targetMonster = targetMonster;
        this.targetPosition = targetPosition;

        startPosition = transform.position;
        elapsedLifetime = 0f;
        hasImpacted = false;

        if (!CanInitialize())
        {
            DestroyProjectile();
            return;
        }

        if (attackConfig.AttackArchetype == AttackArchetype.StraightProjectile)
        {
            launchDirection = CalculateLaunchDirection(targetMonster.transform.position);
        }

        arcTravelTime = CalculateArcTravelTime();
        isInitialized = true;
    }

    private bool CanInitialize()
    {
        if (projectileConfig == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: projectile config is null.", this);
            return false;
        }

        if (attackConfig == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: attack config is null.", this);
            return false;
        }

        if (projectileConfig.ProjectileSpeed <= 0f)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: projectile speed must be greater than zero.", projectileConfig);
            return false;
        }

        if (attackConfig.AttackArchetype == AttackArchetype.StraightProjectile)
        {
            if (monsterManager == null)
            {
                Debug.LogWarning("Projectile behaviour cannot initialize straight projectile: monster manager is null.", this);
                return false;
            }

            if (!IsValidTarget(targetMonster))
            {
                Debug.LogWarning("Projectile behaviour cannot initialize straight projectile: target monster is invalid.", this);
                return false;
            }

            return true;
        }

        if (attackConfig.AttackArchetype == AttackArchetype.ArcProjectile)
        {
            return true;
        }

        Debug.LogWarning($"Projectile behaviour cannot initialize: unsupported attack archetype '{attackConfig.AttackArchetype}'.", this);
        return false;
    }

    private void Update()
    {
        if (!isInitialized || hasImpacted)
        {
            return;
        }

        elapsedLifetime += Time.deltaTime;

        if (elapsedLifetime >= projectileConfig.MaxLifetime)
        {
            DestroyProjectile();
            return;
        }

        switch (attackConfig.AttackArchetype)
        {
            case AttackArchetype.StraightProjectile:
                UpdateStraightProjectile();
                break;
            case AttackArchetype.ArcProjectile:
                UpdateArcProjectile();
                break;
            default:
                DestroyProjectile();
                break;
        }
    }

    private void UpdateStraightProjectile()
    {
        transform.position += launchDirection * projectileConfig.ProjectileSpeed * Time.deltaTime;

        FaceMoveDirection(launchDirection);

        if (!TryGetStraightProjectileHit(out MonsterBehaviour hitMonster))
        {
            return;
        }

        ImpactStraightProjectile(hitMonster);
    }

    private void UpdateArcProjectile()
    {
        float progress = Mathf.Clamp01(elapsedLifetime / arcTravelTime);
        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        nextPosition.y += Mathf.Sin(progress * Mathf.PI) * attackConfig.ArcHeight;
        transform.position = nextPosition;

        FaceMoveDirection(targetPosition - transform.position);

        if (progress < 1f && Vector3.Distance(transform.position, targetPosition) > projectileConfig.HitDistanceThreshold)
        {
            return;
        }

        ImpactArcProjectile();
    }

    private float CalculateArcTravelTime()
    {
        float distance = Vector3.Distance(startPosition, targetPosition);
        return Mathf.Max(distance / projectileConfig.ProjectileSpeed, 0.01f);
    }

    private void FaceMoveDirection(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.FromToRotation(Vector3.down, moveDirection.normalized);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleMonsterCollision(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryHandleMonsterCollision(collision.collider);
    }

    private void TryHandleMonsterCollision(Collider hitCollider)
    {
        if (!isInitialized ||
            hasImpacted ||
            attackConfig.AttackArchetype != AttackArchetype.StraightProjectile ||
            hitCollider == null)
        {
            return;
        }

        MonsterBehaviour hitMonster = hitCollider.GetComponentInParent<MonsterBehaviour>();

        if (!IsTrackedValidTarget(hitMonster))
        {
            return;
        }

        ImpactStraightProjectile(hitMonster);
    }

    private void ImpactStraightProjectile(MonsterBehaviour hitMonster)
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        hitMonster.TakeDamage(attackConfig.Damage);
        RaiseImpact(hitMonster, transform.position);
        DestroyProjectile();
    }

    private void ImpactArcProjectile()
    {
        if (hasImpacted)
        {
            return;
        }
        hasImpacted = true;
        RaiseImpact(null, targetPosition);
        DestroyProjectile();
    }

    private void RaiseImpact(MonsterBehaviour hitMonster, Vector3 impactPosition)
    {
        ProjectileImpactContext impactContext = new ProjectileImpactContext(
            sourceTower,
            projectileConfig,
            attackConfig,
            hitMonster,
            impactPosition,
            attackConfig.Damage,
            projectileConfig.ImpactEffectConfig
        );

        OnImpact?.Invoke(impactContext);
        AreaDamageEffectExecutor.Execute(impactContext);
    }

    private Vector3 CalculateLaunchDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    private bool TryGetStraightProjectileHit(out MonsterBehaviour hitMonster)
    {
        hitMonster = null;

        if (monsterManager == null)
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float hitDistanceThresholdSqr = projectileConfig.HitDistanceThreshold * projectileConfig.HitDistanceThreshold;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!IsValidTarget(monster))
            {
                continue;
            }

            float distanceSqr = (monster.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr > hitDistanceThresholdSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            hitMonster = monster;
            nearestDistanceSqr = distanceSqr;
        }

        return hitMonster != null;
    }

    private bool IsTrackedValidTarget(MonsterBehaviour monster)
    {
        if (!IsValidTarget(monster) || monsterManager == null)
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            if (aliveMonsters[i] == monster)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
