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
    private readonly List<MonsterBehaviour> piercedMonsters = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> resolvedExplosiveShellTargets = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> bounceCandidates = new List<MonsterBehaviour>();
    private readonly HashSet<MonsterBehaviour> bounceHitHistory = new HashSet<MonsterBehaviour>();
    private ProjectileRuntimeOptions runtimeOptions;
    private int attackDamage;
    private float elapsedLifetime;
    private float arcTravelTime;
    private bool isInitialized;
    private bool hasImpacted;
    private bool hasLoggedUnsupportedTrackingFlight;

    public event Action<ProjectileImpactContext> OnImpact;
    public event Action<EffectTriggerContext> OnEffectTriggerContextCreated;

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
        AttackArchetype? flightArchetypeOverride = null,
        ProjectileRuntimeOptions runtimeOptions = default,
        IReadOnlyCollection<MonsterBehaviour> inheritedBounceHitHistory = null)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.projectileConfig = projectileConfig;
        this.attackConfig = attackConfig;
        flightArchetype = flightArchetypeOverride ?? (attackConfig != null ? attackConfig.AttackArchetype : default);
        this.targetMonster = targetMonster;
        this.targetPosition = targetPosition;
        this.runtimeOptions = runtimeOptions;
        this.attackDamage = Mathf.Max(0, attackDamage);

        startPosition = transform.position;
        piercedMonsters.Clear();
        resolvedExplosiveShellTargets.Clear();
        bounceCandidates.Clear();
        CopyBounceHitHistory(inheritedBounceHitHistory);
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
        if (monsterManager == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize Arc flight: monster manager is null.", this);
            return false;
        }

        if (projectileConfig.HitDistanceThreshold <= 0f)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize Arc flight: hit distance threshold must be greater than zero.", projectileConfig);
            return false;
        }

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
        launchDirection = CalculateLaunchDirection(targetPosition);
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

        if (runtimeOptions.CanPierce && piercedMonsters.Contains(hitMonster))
        {
            return;
        }

        ImpactDirectionProjectile(hitMonster);
    }

    private void UpdateArcFlight()
    {
        float progress = Mathf.Clamp01(elapsedLifetime / arcTravelTime);
        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        float arcHeight = runtimeOptions.IsBounceChild
            ? runtimeOptions.BounceArcHeight
            : attackConfig.ArcHeight;
        nextPosition.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
        Vector3 moveDirection = nextPosition - transform.position;
        transform.position = nextPosition;

        FaceMoveDirection(moveDirection);

        if (progress < 1f)
        {
            return;
        }

        transform.position = targetPosition;
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

        if (!IsDirectHitEnabled())
        {
            return;
        }

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
        if (hasImpacted || hitMonster == null)
        {
            return;
        }

        if (runtimeOptions.CanPierce)
        {
            if (piercedMonsters.Contains(hitMonster))
            {
                return;
            }

            piercedMonsters.Add(hitMonster);
            ApplyDirectionProjectileImpact(hitMonster);

            if (piercedMonsters.Count >= runtimeOptions.MaxPierceHitCount)
            {
                FinishDirectionProjectileAfterImpact();
            }

            return;
        }

        ApplyDirectionProjectileImpact(hitMonster);
        FinishDirectionProjectileAfterImpact();
    }

    private void ApplyDirectionProjectileImpact(MonsterBehaviour hitMonster)
    {
        hitMonster.TakeDamage(attackDamage);
        ElementalApplication.TryApplyFromTowerAttack(
            sourceTower,
            hitMonster,
            transform.position);
        RaiseImpact(hitMonster, transform.position);
    }

    private void FinishDirectionProjectileAfterImpact()
    {
        hasImpacted = true;
        DestroyProjectile();
    }

    private void ImpactArcProjectile()
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        Vector3 impactPosition = transform.position;
        MonsterBehaviour hitMonster = null;

        if (TryResolveArcImpactTarget(impactPosition, out MonsterBehaviour resolvedMonster))
        {
            hitMonster = resolvedMonster;
            bounceHitHistory.Add(hitMonster);
            hitMonster.TakeDamage(attackDamage);
            ElementalApplication.TryApplyFromTowerAttack(
                sourceTower,
                hitMonster,
                impactPosition);
        }

        RaiseImpact(hitMonster, impactPosition);
        ExecuteExplosiveShellImpact(impactPosition);
        TryReleaseBounceChild(impactPosition);
        DestroyProjectile();
    }

    private void CopyBounceHitHistory(IReadOnlyCollection<MonsterBehaviour> inheritedBounceHitHistory)
    {
        bounceHitHistory.Clear();

        if (inheritedBounceHitHistory == null)
        {
            return;
        }

        foreach (MonsterBehaviour monster in inheritedBounceHitHistory)
        {
            if (monster != null)
            {
                bounceHitHistory.Add(monster);
            }
        }
    }

    private void ExecuteExplosiveShellImpact(Vector3 impactPosition)
    {
        EffectDefinition explosiveShellEffect = runtimeOptions.ExplosiveShellEffect;

        if (explosiveShellEffect == null)
        {
            return;
        }

        EffectExecutor.ExecuteWithResolvedTargets(
            explosiveShellEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: runtimeOptions.ExplosiveShellSourceUpgrade,
                targetMonster: null,
                hasTriggerPosition: true,
                triggerPosition: impactPosition,
                resolvedDamage: attackDamage,
                // Elemental Buff actions stay gated here; non-elemental authored Buff actions may still execute.
                allowsElementalApplication: false),
            resolvedExplosiveShellTargets);

        for (int i = 0; i < resolvedExplosiveShellTargets.Count; i++)
        {
            ElementalApplication.TryApplyFromTowerAttack(
                sourceTower,
                resolvedExplosiveShellTargets[i],
                impactPosition);
        }
    }

    private bool TryReleaseBounceChild(Vector3 impactPosition)
    {
        if (runtimeOptions.RemainingBounceCount <= 0 ||
            runtimeOptions.BounceSearchRadius <= 0f ||
            !TryResolveBounceTarget(impactPosition, out MonsterBehaviour bounceTarget))
        {
            return false;
        }

        Vector3 bounceTargetPosition = EffectTargetResolver.GetMonsterHitPosition(bounceTarget);
        return TryCreateBounceChild(impactPosition, bounceTargetPosition);
    }

    private bool TryResolveBounceTarget(
        Vector3 impactPosition,
        out MonsterBehaviour bounceTarget)
    {
        bounceTarget = null;

        if (monsterManager == null)
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float searchRadiusSqr = runtimeOptions.BounceSearchRadius * runtimeOptions.BounceSearchRadius;
        bounceCandidates.Clear();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(monster) ||
                bounceHitHistory.Contains(monster))
            {
                continue;
            }

            float distanceSqr =
                (EffectTargetResolver.GetMonsterHitPosition(monster) - impactPosition).sqrMagnitude;

            if (distanceSqr > searchRadiusSqr)
            {
                continue;
            }

            bounceCandidates.Add(monster);
        }

        if (bounceCandidates.Count == 0)
        {
            return false;
        }

        bounceTarget = SelectBounceTarget(impactPosition);
        return bounceTarget != null;
    }

    private MonsterBehaviour SelectBounceTarget(Vector3 impactPosition)
    {
        switch (runtimeOptions.BounceTargetSelectionType)
        {
            case TargetSelectionType.HighestHealth:
                return SelectBounceTargetByHealth(selectHighest: true);
            case TargetSelectionType.LowestHealth:
                return SelectBounceTargetByHealth(selectHighest: false);
            case TargetSelectionType.Random:
                return bounceCandidates[UnityEngine.Random.Range(0, bounceCandidates.Count)];
            case TargetSelectionType.Nearest:
            default:
                return SelectNearestBounceTarget(impactPosition);
        }
    }

    private MonsterBehaviour SelectNearestBounceTarget(Vector3 impactPosition)
    {
        MonsterBehaviour selectedTarget = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < bounceCandidates.Count; i++)
        {
            MonsterBehaviour candidate = bounceCandidates[i];
            float distanceSqr =
                (EffectTargetResolver.GetMonsterHitPosition(candidate) - impactPosition).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            selectedTarget = candidate;
            nearestDistanceSqr = distanceSqr;
        }

        return selectedTarget;
    }

    private MonsterBehaviour SelectBounceTargetByHealth(bool selectHighest)
    {
        MonsterBehaviour selectedTarget = bounceCandidates[0];

        for (int i = 1; i < bounceCandidates.Count; i++)
        {
            MonsterBehaviour candidate = bounceCandidates[i];
            bool isBetter = selectHighest
                ? candidate.CurrentHealth > selectedTarget.CurrentHealth
                : candidate.CurrentHealth < selectedTarget.CurrentHealth;

            if (isBetter)
            {
                selectedTarget = candidate;
            }
        }

        return selectedTarget;
    }

    private bool TryCreateBounceChild(
        Vector3 impactPosition,
        Vector3 bounceTargetPosition)
    {
        if (projectileConfig == null || projectileConfig.ProjectilePrefab == null)
        {
            return false;
        }

        GameObject childObject = Instantiate(
            projectileConfig.ProjectilePrefab,
            impactPosition,
            Quaternion.identity);

        if (!childObject.TryGetComponent(out ProjectileBehaviour childProjectile))
        {
            childProjectile = childObject.AddComponent<ProjectileBehaviour>();
        }

        childProjectile.Initialize(
            sourceTower,
            monsterManager,
            projectileConfig,
            attackConfig,
            targetMonster: null,
            targetPosition: bounceTargetPosition,
            attackDamage: attackDamage,
            flightArchetypeOverride: AttackArchetype.ArcProjectile,
            runtimeOptions: CreateBounceChildRuntimeOptions(),
            inheritedBounceHitHistory: bounceHitHistory);

        return childProjectile.IsInitialized;
    }

    private ProjectileRuntimeOptions CreateBounceChildRuntimeOptions()
    {
        return new ProjectileRuntimeOptions(
            canPierce: false,
            maxPierceHitCount: 1,
            isBounceChild: true,
            explosiveShellSourceUpgrade: runtimeOptions.ExplosiveShellSourceUpgrade,
            explosiveShellEffect: runtimeOptions.ExplosiveShellEffect,
            bounceSearchRadius: runtimeOptions.BounceSearchRadius,
            remainingBounceCount: runtimeOptions.RemainingBounceCount - 1,
            bounceArcHeight: runtimeOptions.BounceArcHeight,
            bounceTargetSelectionType: runtimeOptions.BounceTargetSelectionType);
    }

    private void RaiseImpact(MonsterBehaviour hitMonster, Vector3 impactPosition)
    {
        bool isArcPositionImpact = flightArchetype == AttackArchetype.ArcProjectile;
        EffectDefinition impactEffectDefinition = isArcPositionImpact
            ? null
            : projectileConfig.ImpactEffectDefinition;
        ProjectileImpactContext impactContext = new ProjectileImpactContext(
            sourceTower,
            projectileConfig,
            attackConfig,
            hitMonster,
            impactPosition,
            attackDamage,
            impactEffectDefinition
        );

        PlayImpactVfx(impactContext.ImpactPosition);
        OnImpact?.Invoke(impactContext);
        EffectTriggerContext effectTriggerContext = CreateEffectTriggerContext(hitMonster, impactPosition);
        OnEffectTriggerContextCreated?.Invoke(effectTriggerContext);

        if (isArcPositionImpact)
        {
            return;
        }

        EffectExecutor.Execute(impactEffectDefinition, effectTriggerContext);
    }

    private EffectTriggerContext CreateEffectTriggerContext(
        MonsterBehaviour hitMonster,
        Vector3 triggerPosition)
    {
        return new EffectTriggerContext(
            sourceTower: sourceTower,
            sourceUpgrade: null,
            targetMonster: flightArchetype == AttackArchetype.ArcProjectile ? null : hitMonster,
            hasTriggerPosition: true,
            triggerPosition: triggerPosition,
            resolvedDamage: attackDamage,
            allowsElementalApplication: false
        );
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

    private bool TryResolveArcImpactTarget(
        Vector3 impactPosition,
        out MonsterBehaviour hitMonster)
    {
        hitMonster = null;

        if (monsterManager == null || projectileConfig == null || projectileConfig.HitDistanceThreshold <= 0f)
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float hitDistanceThresholdSqr = projectileConfig.HitDistanceThreshold * projectileConfig.HitDistanceThreshold;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster == null || !monster.IsGameplayTargetable)
            {
                continue;
            }

            float distanceSqr =
                (EffectTargetResolver.GetMonsterHitPosition(monster) - impactPosition).sqrMagnitude;

            if (distanceSqr > hitDistanceThresholdSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            hitMonster = monster;
            nearestDistanceSqr = distanceSqr;
        }

        return hitMonster != null;
    }

    private bool TryGetDirectionProjectileHit(out MonsterBehaviour hitMonster)
    {
        hitMonster = null;

        if (monsterManager == null || !IsDirectHitEnabled())
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float hitDistanceThresholdSqr = projectileConfig.HitDistanceThreshold * projectileConfig.HitDistanceThreshold;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!IsValidTarget(monster) ||
                (runtimeOptions.CanPierce && piercedMonsters.Contains(monster)))
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
        if (!IsDirectHitEnabled())
        {
            return false;
        }

        float hitDistanceThresholdSqr = projectileConfig.HitDistanceThreshold * projectileConfig.HitDistanceThreshold;
        return GetHitDistanceSqr(monster) <= hitDistanceThresholdSqr;
    }

    private bool IsDirectHitEnabled()
    {
        return projectileConfig != null && projectileConfig.HitDistanceThreshold > 0f;
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
