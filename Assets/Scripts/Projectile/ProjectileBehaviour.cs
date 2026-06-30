using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private ProjectileConfig projectileConfig;
    private AttackConfig attackConfig;
    private AttackArchetype flightArchetype;
    private MonsterBehaviour targetMonster;
    private Vector3 targetPosition;
    private Vector3 launchDirection;
    private Vector3 startPosition;
    private int attackDamage;
    private float elapsedLifetime;
    private float arcTravelTime;
    private bool isInitialized;
    private bool hasImpacted;
    private bool hasLoggedUnsupportedTrackingFlight;

    public event Action<ProjectileImpactContext> OnImpact;

    public TowerInstance SourceTower => sourceTower;
    public ProjectileConfig ProjectileConfig => projectileConfig;
    public AttackConfig AttackConfig => attackConfig;
    public AttackArchetype FlightArchetype => flightArchetype;
    public MonsterBehaviour TargetMonster => targetMonster;
    public Vector3 TargetPosition => targetPosition;
    public bool IsInitialized => isInitialized;

    public void Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        ProjectileConfig projectileConfig,
        AttackConfig attackConfig,
        MonsterBehaviour targetMonster,
        Vector3 targetPosition,
        int attackDamage,
        AttackArchetype? flightArchetypeOverride = null)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.projectileConfig = projectileConfig;
        this.attackConfig = attackConfig;
        flightArchetype = flightArchetypeOverride ?? (attackConfig != null ? attackConfig.AttackArchetype : default);
        this.targetMonster = targetMonster;
        this.targetPosition = targetPosition;
        this.attackDamage = Mathf.Max(0, attackDamage);

        startPosition = transform.position;
        elapsedLifetime = 0f;
        hasImpacted = false;
        hasLoggedUnsupportedTrackingFlight = false;

        if (!CanInitialize())
        {
            DestroyProjectile();
            return;
        }

        isInitialized = InitializeFlight();

        if (!isInitialized)
        {
            DestroyProjectile();
        }
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

        switch (flightArchetype)
        {
            case AttackArchetype.DirectionProjectile:
                return CanInitializeDirectionFlight();
            case AttackArchetype.ArcProjectile:
                return CanInitializeArcFlight();
            case AttackArchetype.TrackingProjectile:
                return CanInitializeTrackingFlight();
            default:
                Debug.LogWarning($"Projectile behaviour cannot initialize: unsupported attack archetype '{flightArchetype}'.", this);
                return false;
        }
    }

    private bool CanInitializeDirectionFlight()
    {
        if (monsterManager == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize direction flight: monster manager is null.", this);
            return false;
        }

        if (!IsValidTarget(targetMonster))
        {
            Debug.LogWarning("Projectile behaviour cannot initialize direction flight: target monster is invalid.", this);
            return false;
        }

        return true;
    }

    private bool CanInitializeArcFlight()
    {
        return true;
    }

    private bool CanInitializeTrackingFlight()
    {
        return true;
    }

    private bool InitializeFlight()
    {
        switch (flightArchetype)
        {
            case AttackArchetype.DirectionProjectile:
                return InitializeDirectionFlight();
            case AttackArchetype.ArcProjectile:
                return InitializeArcFlight();
            case AttackArchetype.TrackingProjectile:
                return InitializeTrackingFlight();
            default:
                return false;
        }
    }

    private bool InitializeDirectionFlight()
    {
        launchDirection = CalculateLaunchDirection(GetMonsterHitPosition(targetMonster));
        return true;
    }

    private bool InitializeArcFlight()
    {
        arcTravelTime = CalculateArcTravelTime();
        return true;
    }

    private bool InitializeTrackingFlight()
    {
        LogUnsupportedTrackingFlight();
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

        switch (flightArchetype)
        {
            case AttackArchetype.DirectionProjectile:
                UpdateDirectionFlight();
                break;
            case AttackArchetype.ArcProjectile:
                UpdateArcFlight();
                break;
            case AttackArchetype.TrackingProjectile:
                UpdateTrackingFlight();
                break;
            default:
                DestroyProjectile();
                break;
        }
    }

    private void UpdateDirectionFlight()
    {
        transform.position += launchDirection * projectileConfig.ProjectileSpeed * Time.deltaTime;

        FaceMoveDirection(launchDirection);

        if (!TryGetDirectionProjectileHit(out MonsterBehaviour hitMonster))
        {
            return;
        }

        ImpactDirectionProjectile(hitMonster);
    }

    private void UpdateArcFlight()
    {
        float progress = Mathf.Clamp01(elapsedLifetime / arcTravelTime);
        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        nextPosition.y += Mathf.Sin(progress * Mathf.PI) * attackConfig.ArcHeight;
        Vector3 moveDirection = nextPosition - transform.position;
        transform.position = nextPosition;

        FaceMoveDirection(moveDirection);

        if (progress < 1f && Vector3.Distance(transform.position, targetPosition) > projectileConfig.HitDistanceThreshold)
        {
            return;
        }

        ImpactArcProjectile();
    }

    private void UpdateTrackingFlight()
    {
        LogUnsupportedTrackingFlight();
        DestroyProjectile();
    }

    private void LogUnsupportedTrackingFlight()
    {
        if (hasLoggedUnsupportedTrackingFlight)
        {
            return;
        }

        hasLoggedUnsupportedTrackingFlight = true;
        Debug.LogWarning("Projectile tracking flight is not implemented yet. Tracking projectiles are reserved for a later runtime task.", this);
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

        transform.rotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
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
            flightArchetype != AttackArchetype.DirectionProjectile ||
            hitCollider == null)
        {
            return;
        }

        MonsterBehaviour hitMonster = hitCollider.GetComponentInParent<MonsterBehaviour>();

        if (!IsTrackedValidTarget(hitMonster))
        {
            return;
        }

        if (!IsWithinHitDistance(hitMonster))
        {
            return;
        }

        ImpactDirectionProjectile(hitMonster);
    }

    private void ImpactDirectionProjectile(MonsterBehaviour hitMonster)
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        hitMonster.TakeDamage(attackDamage);
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
            attackDamage,
            projectileConfig.ImpactEffectConfig
        );

        PlayImpactVfx(impactContext.ImpactPosition);
        OnImpact?.Invoke(impactContext);
        AreaDamageEffectExecutor.Execute(impactContext);
    }

    private void PlayImpactVfx(Vector3 impactPosition)
    {
        if (projectileConfig == null || projectileConfig.ImpactVfxPrefab == null)
        {
            return;
        }

        Instantiate(projectileConfig.ImpactVfxPrefab, impactPosition, Quaternion.identity);
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

    private bool TryGetDirectionProjectileHit(out MonsterBehaviour hitMonster)
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

            float distanceSqr = GetHitDistanceSqr(monster);

            if (distanceSqr > hitDistanceThresholdSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            hitMonster = monster;
            nearestDistanceSqr = distanceSqr;
        }

        return hitMonster != null;
    }

    private bool IsWithinHitDistance(MonsterBehaviour monster)
    {
        float hitDistanceThresholdSqr = projectileConfig.HitDistanceThreshold * projectileConfig.HitDistanceThreshold;
        return GetHitDistanceSqr(monster) <= hitDistanceThresholdSqr;
    }

    private float GetHitDistanceSqr(MonsterBehaviour monster)
    {
        return (GetMonsterHitPosition(monster) - transform.position).sqrMagnitude;
    }

    private static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
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
