using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TowerCombatBehaviour : MonoBehaviour
{
    private const int DefaultPiercingArrowMaxHitCount = 3;
    private const float DefaultScatterArrowAngleOffset = 15f;
    private const int DefaultMultiOrbsCount = 2;
    private const int DefaultTwinDronesCount = 2;
    private const float DefaultTwinDronesTakeOffDelay = 0.6f;
    private const int HuntingArrowSlotCount = 3;

    [SerializeField] private TowerInstance towerInstance;
    private MonsterManager monsterManager;
    private TowerBehaviour towerBehaviour;

    private readonly List<MonsterBehaviour> detectedEnemies = new List<MonsterBehaviour>();
    private readonly List<Vector3> pendingCannonInitialShellTargetPositions = new List<Vector3>();
    private readonly MonsterBehaviour[] pendingHuntingTargets = new MonsterBehaviour[HuntingArrowSlotCount];
    private readonly HashSet<TowerBehaviourPackageType> missingBehaviourPackageWarnings = new HashSet<TowerBehaviourPackageType>();
    private readonly HashSet<MagicOrbBehaviour> activeMagicOrbs = new HashSet<MagicOrbBehaviour>();

    private TowerInstance subscribedUpgradeTowerInstance;
    private MagicArcaneFieldBehaviour activeMagicArcaneField;
    private TowerDefinition towerDefinition;
    private AttackConfig attackConfig;
    private MonsterBehaviour currentTarget;
    private MonsterBehaviour pendingProjectileTarget;
    private Vector3 pendingProjectileTargetPosition;
    private MonsterBehaviour pendingDroneTarget;
    private AttackArchetype pendingAttackArchetype;
    private float cooldownTimer;
    private TowerAttackState attackState = TowerAttackState.Idle;
    private bool hasLoggedUnsupportedAttackEntity;
    private bool hasLoggedMissingMagicOrbPrefab;
    private bool hasLoggedMissingDronePrefab;
    private bool hasLoggedMissingAttackOrigin;
    private bool hasLoggedInvalidArcHitDistanceThreshold;
    private bool hasLoggedInvalidExplosiveShellEffect;
    private bool hasLoggedInvalidArcaneDetonationEffect;
    private bool hasLoggedMissingMagicArcaneFieldVfxPrefab;
    private bool hasLoggedInvalidMagicArcaneFieldVfxPrefab;

    public event Action<TowerCombatBehaviour, MonsterBehaviour> OnProjectileReleased;
    public event Action<TowerCombatBehaviour, AttackArchetype> OnUnsupportedAttackEntity;

    public MonsterBehaviour CurrentTarget => currentTarget;
    public IReadOnlyList<MonsterBehaviour> DetectedEnemies => detectedEnemies;
    public float CooldownTimer => cooldownTimer;
    public TowerAttackState AttackState => attackState;
    public bool IsAttacking => attackState != TowerAttackState.Idle;

    public void Initialize(TowerInstance towerInstance, MonsterManager monsterManager)
    {
        UnsubscribeFromUpgradeNotifications();
        CleanupMagicArcaneField();
        ForceCleanupTrackedMagicOrbs();

        this.towerInstance = towerInstance;
        this.monsterManager = monsterManager;
        towerDefinition = towerInstance != null ? towerInstance.TowerDefinition : null;
        attackConfig = towerDefinition != null ? towerDefinition.AttackConfig : null;

        CacheOptionalReferences();

        cooldownTimer = 0f;
        currentTarget = null;
        ResetPendingAttackState();
        hasLoggedUnsupportedAttackEntity = false;
        hasLoggedMissingMagicOrbPrefab = false;
        hasLoggedMissingDronePrefab = false;
        hasLoggedMissingAttackOrigin = false;
        hasLoggedInvalidArcHitDistanceThreshold = false;
        hasLoggedInvalidExplosiveShellEffect = false;
        hasLoggedInvalidArcaneDetonationEffect = false;
        hasLoggedMissingMagicArcaneFieldVfxPrefab = false;
        hasLoggedInvalidMagicArcaneFieldVfxPrefab = false;
        missingBehaviourPackageWarnings.Clear();

        SubscribeToUpgradeNotifications();
        EnsureArcaneFieldExists();
    }

    public void OnAttackAnimationRelease()
    {
        if (attackState != TowerAttackState.WaitingForAnimationRelease)
        {
            return;
        }

        switch (pendingAttackArchetype)
        {
            case AttackArchetype.DirectionProjectile:
            case AttackArchetype.ArcProjectile:
                ReleasePendingProjectileAttack();
                break;
            case AttackArchetype.MagicOrb:
                ReleasePendingMagicOrbAttack();
                break;
            case AttackArchetype.Drone:
                ReleasePendingDroneAttack();
                break;
            default:
                ResetPendingAttackState();
                break;
        }
    }

    private void Awake()
    {
        CacheOptionalReferences();
    }

    private void OnEnable()
    {
        SubscribeToUpgradeNotifications();
        EnsureArcaneFieldExists();
    }

    private void OnDisable()
    {
        UnsubscribeFromUpgradeNotifications();
        CleanupMagicArcaneField();
        ForceCleanupTrackedMagicOrbs();
    }

    private void OnDestroy()
    {
        UnsubscribeFromUpgradeNotifications();
        CleanupMagicArcaneField();
        ForceCleanupTrackedMagicOrbs();
    }

    private void Update()
    {
        if (!CanRunCombat())
        {
            return;
        }

        UpdateCooldown();
        DetectEnemies();
        CleanupVfxOutsideCurrentArchetype();

        UpdateAttackEntity();
    }

    private void UpdateAttackEntity()
    {
        switch (attackConfig.AttackArchetype)
        {
            case AttackArchetype.DirectionProjectile:
            case AttackArchetype.ArcProjectile:
                UpdateProjectileAttack();
                break;
            case AttackArchetype.MagicOrb:
                UpdateMagicOrbAttackEntity();
                break;
            case AttackArchetype.Drone:
                UpdateDroneAttackEntity();
                break;
            default:
                UpdateUnsupportedAttackEntity();
                break;
        }
    }

    private bool CanRunCombat()
    {
        if (towerInstance == null)
        {
            towerInstance = GetComponent<TowerInstance>();
        }

        if (towerDefinition == null && towerInstance != null)
        {
            towerDefinition = towerInstance.TowerDefinition;
        }

        if (attackConfig == null && towerDefinition != null)
        {
            attackConfig = towerDefinition.AttackConfig;
        }

        if (monsterManager == null)
        {
            monsterManager = FindFirstObjectByType<MonsterManager>();
        }

        return attackConfig != null && monsterManager != null && GetAttackOrigin() != null;
    }

    private void CacheOptionalReferences()
    {
        if (towerInstance == null)
        {
            towerInstance = GetComponent<TowerInstance>();
        }

        if (towerBehaviour == null)
        {
            towerBehaviour = GetComponent<TowerBehaviour>();
        }
    }

    private void UpdateCooldown()
    {
        if (cooldownTimer <= 0f)
        {
            return;
        }

        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
    }

    private void DetectEnemies()
    {
        detectedEnemies.Clear();

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!IsValidTarget(monster))
            {
                continue;
            }

            if (!IsInAttackRange(monster))
            {
                continue;
            }

            detectedEnemies.Add(monster);
        }
    }

    private void UpdateProjectileAttack()
    {
        if (attackState == TowerAttackState.WaitingForAnimationRelease)
        {
            if (pendingAttackArchetype != AttackArchetype.ArcProjectile &&
                (!IsValidTarget(pendingProjectileTarget) || !IsInAttackRange(pendingProjectileTarget)))
            {
                ResetPendingAttackState();
            }

            return;
        }

        if (cooldownTimer > 0f)
        {
            return;
        }

        ProjectileConfig configuredProjectile = attackConfig.ProjectileConfig;

        if (attackConfig.AttackArchetype == AttackArchetype.ArcProjectile &&
            configuredProjectile != null &&
            !IsArcHitDistanceThresholdValid(configuredProjectile))
        {
            return;
        }

        currentTarget = SelectTarget();

        if (!IsValidTarget(currentTarget))
        {
            return;
        }

        pendingProjectileTarget = currentTarget;
        CapturePendingProjectileTargetPositions(currentTarget);
        pendingAttackArchetype = attackConfig.AttackArchetype;
        attackState = TowerAttackState.WaitingForAnimationRelease;

        if (SetAttackAnimatorTrigger()) return;

        ReleasePendingProjectileAttack();
    }

    private void ReleasePendingProjectileAttack()
    {
        if (attackState != TowerAttackState.WaitingForAnimationRelease)
        {
            return;
        }

        bool isArcProjectile = pendingAttackArchetype == AttackArchetype.ArcProjectile;

        if (isArcProjectile && pendingCannonInitialShellTargetPositions.Count == 0)
        {
            ResetPendingAttackState();
            return;
        }

        ProjectileConfig projectileConfig = attackConfig.ProjectileConfig;

        if (projectileConfig == null || projectileConfig.ProjectilePrefab == null)
        {
            Debug.LogWarning("Tower combat cannot release projectile: projectile config or prefab is missing.", this);
            ResetPendingAttackState();
            return;
        }

        if (isArcProjectile && !IsArcHitDistanceThresholdValid(projectileConfig))
        {
            ResetPendingAttackState();
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            ResetPendingAttackState();
            return;
        }

        ResolvedTowerCombatStats resolvedStats = ResolveCombatStats();

        if (!isArcProjectile &&
            (!IsRegisteredGameplayTarget(pendingProjectileTarget) ||
             !IsInRange(
                 origin.position,
                 GetMonsterHitPosition(pendingProjectileTarget),
                 resolvedStats.AttackRange)))
        {
            ResetPendingAttackState();
            return;
        }

        int attackDamage = resolvedStats.AttackDamage;
        ProjectileRuntimeOptions projectileRuntimeOptions = CreateProjectileRuntimeOptions(
            origin.position,
            resolvedStats.AttackRange);
        int releasedProjectileCount;

        if (isArcProjectile)
        {
            releasedProjectileCount = ReleasePendingArcProjectiles(
                projectileConfig,
                origin,
                attackDamage,
                projectileRuntimeOptions);
        }
        else
        {
            bool releasedProjectile;

            if (IsArcherScatterArrowActive())
            {
                releasedProjectile = TryReleaseScatterProjectiles(
                    projectileConfig,
                    origin,
                    attackDamage,
                    resolvedStats.AttackRange,
                    projectileRuntimeOptions);
            }
            else
            {
                bool isHuntingArrow = IsArcherHuntingArrowActive();
                releasedProjectile = TryReleaseProjectile(
                    projectileConfig,
                    origin,
                    pendingProjectileTargetPosition,
                    pendingProjectileTarget,
                    attackDamage,
                    projectileRuntimeOptions,
                    isHuntingArrow
                        ? (AttackArchetype?)AttackArchetype.TrackingProjectile
                        : null);
            }

            releasedProjectileCount = releasedProjectile ? 1 : 0;
        }

        if (releasedProjectileCount == 0)
        {
            ResetPendingAttackState();
            return;
        }

        StartAttackCooldown(resolvedStats.AttackInterval);
        PlayAttackReleaseVfx();
        OnProjectileReleased?.Invoke(this, pendingProjectileTarget);
        ResetPendingAttackState();
    }

    private void CapturePendingProjectileTargetPositions(MonsterBehaviour firstTarget)
    {
        pendingCannonInitialShellTargetPositions.Clear();
        pendingProjectileTargetPosition = GetMonsterHitPosition(firstTarget);
        CapturePendingHuntingTargets(firstTarget);

        if (attackConfig.AttackArchetype != AttackArchetype.ArcProjectile)
        {
            return;
        }

        int maxInitialShellCount = ResolveCannonMaxInitialShellCount();
        HashSet<MonsterBehaviour> selectedTargets = new HashSet<MonsterBehaviour>
        {
            firstTarget
        };
        pendingCannonInitialShellTargetPositions.Add(pendingProjectileTargetPosition);

        while (pendingCannonInitialShellTargetPositions.Count < maxInitialShellCount)
        {
            MonsterBehaviour additionalTarget = SelectTarget(selectedTargets);

            if (!IsValidTarget(additionalTarget))
            {
                break;
            }

            selectedTargets.Add(additionalTarget);
            pendingCannonInitialShellTargetPositions.Add(GetMonsterHitPosition(additionalTarget));
        }
    }

    private void CapturePendingHuntingTargets(MonsterBehaviour firstTarget)
    {
        Array.Clear(pendingHuntingTargets, 0, pendingHuntingTargets.Length);

        if (!IsArcherHuntingArrowActive())
        {
            return;
        }

        pendingHuntingTargets[0] = firstTarget;

        if (!IsArcherScatterArrowActive())
        {
            return;
        }

        HashSet<MonsterBehaviour> selectedTargets = new HashSet<MonsterBehaviour>
        {
            firstTarget
        };

        for (int slotIndex = 1; slotIndex < pendingHuntingTargets.Length; slotIndex++)
        {
            MonsterBehaviour additionalTarget = SelectTarget(selectedTargets);

            if (!IsValidTarget(additionalTarget))
            {
                break;
            }

            pendingHuntingTargets[slotIndex] = additionalTarget;
            selectedTargets.Add(additionalTarget);
        }
    }

    private int ReleasePendingArcProjectiles(
        ProjectileConfig projectileConfig,
        Transform origin,
        int attackDamage,
        ProjectileRuntimeOptions projectileRuntimeOptions)
    {
        int releasedCount = 0;

        for (int i = 0; i < pendingCannonInitialShellTargetPositions.Count; i++)
        {
            if (TryReleaseProjectile(
                    projectileConfig,
                    origin,
                    pendingCannonInitialShellTargetPositions[i],
                    null,
                    attackDamage,
                    projectileRuntimeOptions))
            {
                releasedCount++;
            }
        }

        return releasedCount;
    }

    private bool TryReleaseScatterProjectiles(
        ProjectileConfig projectileConfig,
        Transform origin,
        int attackDamage,
        float trackingRange,
        ProjectileRuntimeOptions projectileRuntimeOptions)
    {
        Vector3 centerDirection = pendingProjectileTargetPosition - origin.position;

        if (centerDirection.sqrMagnitude <= 0.0001f)
        {
            centerDirection = origin.forward;
        }

        centerDirection.Normalize();

        Vector3 leftDirection =
            Quaternion.AngleAxis(-GetScatterArrowAngleOffset(), Vector3.up) * centerDirection;
        Vector3 rightDirection =
            Quaternion.AngleAxis(GetScatterArrowAngleOffset(), Vector3.up) * centerDirection;
        bool isHuntingArrow = IsArcherHuntingArrowActive();

        bool releasedCenter = isHuntingArrow
            ? TryReleaseHuntingScatterSlot(
                0,
                centerDirection,
                projectileConfig,
                origin,
                attackDamage,
                trackingRange,
                projectileRuntimeOptions)
            : TryReleaseProjectile(
                projectileConfig,
                origin,
                pendingProjectileTargetPosition,
                pendingProjectileTarget,
                attackDamage,
                projectileRuntimeOptions);

        bool releasedLeft = isHuntingArrow
            ? TryReleaseHuntingScatterSlot(
                1,
                leftDirection,
                projectileConfig,
                origin,
                attackDamage,
                trackingRange,
                projectileRuntimeOptions)
            : TryReleaseProjectileInDirection(
                projectileConfig,
                origin,
                leftDirection,
                attackDamage,
                projectileRuntimeOptions);

        bool releasedRight = isHuntingArrow
            ? TryReleaseHuntingScatterSlot(
                2,
                rightDirection,
                projectileConfig,
                origin,
                attackDamage,
                trackingRange,
                projectileRuntimeOptions)
            : TryReleaseProjectileInDirection(
                projectileConfig,
                origin,
                rightDirection,
                attackDamage,
                projectileRuntimeOptions);

        return releasedCenter || releasedLeft || releasedRight;
    }

    private bool TryReleaseHuntingScatterSlot(
        int slotIndex,
        Vector3 fallbackDirection,
        ProjectileConfig projectileConfig,
        Transform origin,
        int attackDamage,
        float trackingRange,
        ProjectileRuntimeOptions projectileRuntimeOptions)
    {
        MonsterBehaviour lockedTarget = pendingHuntingTargets[slotIndex];

        if (IsRegisteredGameplayTarget(lockedTarget) &&
            IsInRange(origin.position, GetMonsterHitPosition(lockedTarget), trackingRange))
        {
            return TryReleaseProjectile(
                projectileConfig,
                origin,
                GetMonsterHitPosition(lockedTarget),
                lockedTarget,
                attackDamage,
                projectileRuntimeOptions,
                AttackArchetype.TrackingProjectile);
        }

        return TryReleaseProjectileInDirection(
            projectileConfig,
            origin,
            fallbackDirection,
            attackDamage,
            projectileRuntimeOptions);
    }

    private bool TryReleaseProjectileInDirection(
        ProjectileConfig projectileConfig,
        Transform origin,
        Vector3 direction,
        int attackDamage,
        ProjectileRuntimeOptions projectileRuntimeOptions)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 targetPosition = origin.position + direction.normalized;
        return TryReleaseProjectile(
            projectileConfig,
            origin,
            targetPosition,
            pendingProjectileTarget,
            attackDamage,
            projectileRuntimeOptions);
    }

    private bool TryReleaseProjectile(
        ProjectileConfig projectileConfig,
        Transform origin,
        Vector3 targetPosition,
        MonsterBehaviour target,
        int attackDamage,
        ProjectileRuntimeOptions projectileRuntimeOptions,
        AttackArchetype? flightArchetypeOverride = null)
    {
        GameObject projectileObject = Instantiate(projectileConfig.ProjectilePrefab, origin.position, Quaternion.identity);

        if (!projectileObject.TryGetComponent(out ProjectileBehaviour projectileBehaviour))
        {
            projectileBehaviour = projectileObject.AddComponent<ProjectileBehaviour>();
        }

        projectileBehaviour.Initialize(
            towerInstance,
            monsterManager,
            projectileConfig,
            attackConfig,
            target,
            targetPosition,
            attackDamage,
            flightArchetypeOverride: flightArchetypeOverride,
            runtimeOptions: projectileRuntimeOptions
        );

        return projectileBehaviour.IsInitialized;
    }

    private ProjectileRuntimeOptions CreateProjectileRuntimeOptions(
        Vector3 trackingRangeOrigin,
        float trackingRange)
    {
        bool canPierce = IsArcherPiercingArrowActive();
        TowerUpgradeDefinition explosiveShellSourceUpgrade = null;
        EffectDefinition explosiveShellEffect = null;
        float bounceSearchRadius = 0f;
        int remainingBounceCount = 0;
        float bounceArcHeight = 0f;
        TargetSelectionType bounceTargetSelectionType = TargetSelectionType.Nearest;

        if (IsCannonArcProjectileRelease() &&
            HasBehaviourPackage(TowerBehaviourPackageType.CannonExplosiveShell) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.CannonExplosiveShell,
                out TowerUpgradeDefinition resolvedExplosiveShellUpgrade))
        {
            explosiveShellSourceUpgrade = resolvedExplosiveShellUpgrade;
            explosiveShellEffect = resolvedExplosiveShellUpgrade.ExplosiveShellEffect;

            if (explosiveShellEffect == null && !hasLoggedInvalidExplosiveShellEffect)
            {
                hasLoggedInvalidExplosiveShellEffect = true;
                Debug.LogWarning(
                    "Tower combat resolved Cannon Explosive Shell, but its EffectDefinition is missing. Explosion gameplay is disabled for this release.",
                    resolvedExplosiveShellUpgrade);
            }
        }

        if (IsCannonArcProjectileRelease() &&
            HasBehaviourPackage(TowerBehaviourPackageType.CannonBouncingShell) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.CannonBouncingShell,
                out TowerUpgradeDefinition resolvedBouncingShellUpgrade))
        {
            bounceSearchRadius = resolvedBouncingShellUpgrade.BounceSearchRadius;
            remainingBounceCount = resolvedBouncingShellUpgrade.MaxBounceCount;
            bounceArcHeight = resolvedBouncingShellUpgrade.BounceArcHeight;
            bounceTargetSelectionType = resolvedBouncingShellUpgrade.BounceTargetSelectionType;
        }

        return new ProjectileRuntimeOptions(
            canPierce,
            canPierce ? GetPiercingArrowMaxHitCount() : 1,
            isBounceChild: false,
            explosiveShellSourceUpgrade: explosiveShellSourceUpgrade,
            explosiveShellEffect: explosiveShellEffect,
            bounceSearchRadius: bounceSearchRadius,
            remainingBounceCount: remainingBounceCount,
            bounceArcHeight: bounceArcHeight,
            bounceTargetSelectionType: bounceTargetSelectionType,
            trackingRangeOrigin: trackingRangeOrigin,
            trackingRange: trackingRange
        );
    }

    private bool IsArcherPiercingArrowActive()
    {
        return IsArcherProjectileRelease() &&
               HasBehaviourPackage(TowerBehaviourPackageType.ArcherPiercingArrow);
    }

    private bool IsArcherScatterArrowActive()
    {
        return IsArcherProjectileRelease() &&
               HasBehaviourPackage(TowerBehaviourPackageType.ArcherScatterArrow);
    }

    private bool IsArcherHuntingArrowActive()
    {
        return IsArcherProjectileRelease() &&
               HasBehaviourPackage(TowerBehaviourPackageType.ArcherHuntingArrow);
    }

    private bool IsArcherProjectileRelease()
    {
        return towerDefinition != null &&
               towerDefinition.TowerFamily == TowerFamily.Archer &&
               attackConfig != null &&
               attackConfig.AttackArchetype == AttackArchetype.DirectionProjectile;
    }

    private bool IsArcHitDistanceThresholdValid(ProjectileConfig projectileConfig)
    {
        if (projectileConfig != null && projectileConfig.HitDistanceThreshold > 0f)
        {
            return true;
        }

        if (!hasLoggedInvalidArcHitDistanceThreshold)
        {
            hasLoggedInvalidArcHitDistanceThreshold = true;
            UnityEngine.Object warningContext = projectileConfig != null
                ? (UnityEngine.Object)projectileConfig
                : this;
            Debug.LogWarning(
                "Tower combat cannot release Arc projectile: hit distance threshold must be greater than zero.",
                warningContext);
        }

        return false;
    }

    private void StartAttackCooldown()
    {
        cooldownTimer = ResolveCombatStats().AttackInterval;
    }

    private void StartAttackCooldown(float attackInterval)
    {
        cooldownTimer = Mathf.Max(0f, attackInterval);
    }

    private void ResetPendingAttackState()
    {
        pendingProjectileTarget = null;
        pendingProjectileTargetPosition = Vector3.zero;
        pendingCannonInitialShellTargetPositions.Clear();
        Array.Clear(pendingHuntingTargets, 0, pendingHuntingTargets.Length);
        pendingDroneTarget = null;
        pendingAttackArchetype = default;
        attackState = TowerAttackState.Idle;
    }

    private void UpdateMagicOrbAttackEntity()
    {
        hasLoggedUnsupportedAttackEntity = false;

        if (attackState == TowerAttackState.WaitingForAnimationRelease)
        {
            return;
        }

        if (cooldownTimer > 0f || detectedEnemies.Count == 0)
        {
            attackState = TowerAttackState.Idle;
            return;
        }

        pendingAttackArchetype = AttackArchetype.MagicOrb;
        attackState = TowerAttackState.WaitingForAnimationRelease;

        if (SetAttackAnimatorTrigger()) return;

        ReleasePendingMagicOrbAttack();
    }

    private void ReleasePendingMagicOrbAttack()
    {
        if (attackState != TowerAttackState.WaitingForAnimationRelease ||
            pendingAttackArchetype != AttackArchetype.MagicOrb)
        {
            return;
        }

        if (attackConfig.MagicOrbPrefab == null)
        {
            if (!hasLoggedMissingMagicOrbPrefab)
            {
                hasLoggedMissingMagicOrbPrefab = true;
                Debug.LogWarning("Tower combat cannot spawn Magic Orb: magic orb prefab is not assigned.", this);
            }

            ResetPendingAttackState();
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            ResetPendingAttackState();
            return;
        }

        ResolvedTowerCombatStats resolvedStats = ResolveCombatStats();
        MagicOrbRuntimeOptions runtimeOptions = CreateMagicOrbRuntimeOptions();
        bool releasedMagicOrb = IsMagicMultiOrbsActive()
            ? TryReleaseMultiMagicOrbs(origin, resolvedStats, runtimeOptions)
            : TryReleaseMagicOrb(origin, resolvedStats, runtimeOptions);

        if (!releasedMagicOrb)
        {
            ResetPendingAttackState();
            return;
        }

        StartAttackCooldown();
        PlayAttackReleaseVfx();
        ResetPendingAttackState();
    }

    private bool TryReleaseMultiMagicOrbs(
        Transform origin,
        ResolvedTowerCombatStats resolvedStats,
        MagicOrbRuntimeOptions runtimeOptions)
    {
        float baseStartingOrbitAngle = UnityEngine.Random.Range(0f, 360f);
        int orbCount = GetMultiOrbsCount();
        float orbitAngleStep = 360f / orbCount;
        bool releasedAnyOrb = false;

        for (int i = 0; i < orbCount; i++)
        {
            float startingOrbitAngle = baseStartingOrbitAngle + orbitAngleStep * i;
            releasedAnyOrb |= TryReleaseMagicOrb(origin, resolvedStats, runtimeOptions, startingOrbitAngle);
        }

        return releasedAnyOrb;
    }

    private bool TryReleaseMagicOrb(
        Transform origin,
        ResolvedTowerCombatStats resolvedStats,
        MagicOrbRuntimeOptions runtimeOptions,
        float? startingOrbitAngle = null)
    {
        GameObject magicOrbObject = Instantiate(attackConfig.MagicOrbPrefab, origin.position, Quaternion.identity);

        if (!magicOrbObject.TryGetComponent(out MagicOrbBehaviour magicOrbBehaviour))
        {
            Debug.LogWarning("Magic Orb prefab does not have MagicOrbBehaviour. Adding it at runtime as a fallback.", magicOrbObject);
            magicOrbBehaviour = magicOrbObject.AddComponent<MagicOrbBehaviour>();
        }

        magicOrbBehaviour.Initialize(
            towerInstance,
            monsterManager,
            attackConfig,
            resolvedStats,
            runtimeOptions,
            origin,
            startingOrbitAngle
        );

        if (!magicOrbBehaviour.IsInitialized)
        {
            return false;
        }

        magicOrbBehaviour.OnEnded += HandleMagicOrbEnded;
        activeMagicOrbs.Add(magicOrbBehaviour);
        return true;
    }

    private MagicOrbRuntimeOptions CreateMagicOrbRuntimeOptions()
    {
        TowerUpgradeDefinition arcaneDetonationSourceUpgrade = null;
        EffectDefinition arcaneDetonationEffect = null;

        if (IsMagicOrbRelease() &&
            HasBehaviourPackage(TowerBehaviourPackageType.MagicArcaneDetonation) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.MagicArcaneDetonation,
                out TowerUpgradeDefinition resolvedArcaneDetonationUpgrade))
        {
            arcaneDetonationSourceUpgrade = resolvedArcaneDetonationUpgrade;
            arcaneDetonationEffect = resolvedArcaneDetonationUpgrade.ArcaneDetonationEffect;

            if (arcaneDetonationEffect == null && !hasLoggedInvalidArcaneDetonationEffect)
            {
                hasLoggedInvalidArcaneDetonationEffect = true;
                Debug.LogWarning(
                    "Tower combat resolved Magic Arcane Detonation, but its EffectDefinition is missing. Detonation gameplay is disabled for this release.",
                    resolvedArcaneDetonationUpgrade);
            }
        }

        return new MagicOrbRuntimeOptions(
            arcaneDetonationSourceUpgrade,
            arcaneDetonationEffect);
    }

    private void HandleMagicOrbEnded(MagicOrbBehaviour magicOrbBehaviour)
    {
        if (magicOrbBehaviour == null)
        {
            return;
        }

        magicOrbBehaviour.OnEnded -= HandleMagicOrbEnded;
        activeMagicOrbs.Remove(magicOrbBehaviour);
    }

    private void ForceCleanupTrackedMagicOrbs()
    {
        if (activeMagicOrbs.Count == 0)
        {
            return;
        }

        List<MagicOrbBehaviour> magicOrbSnapshot = new List<MagicOrbBehaviour>(activeMagicOrbs);

        for (int i = 0; i < magicOrbSnapshot.Count; i++)
        {
            MagicOrbBehaviour magicOrbBehaviour = magicOrbSnapshot[i];

            if (magicOrbBehaviour == null)
            {
                continue;
            }

            magicOrbBehaviour.ForceCleanup();
            magicOrbBehaviour.OnEnded -= HandleMagicOrbEnded;
        }

        activeMagicOrbs.Clear();
    }

    private void SubscribeToUpgradeNotifications()
    {
        if (towerInstance == null)
        {
            towerInstance = GetComponent<TowerInstance>();
        }

        if (subscribedUpgradeTowerInstance == towerInstance)
        {
            return;
        }

        UnsubscribeFromUpgradeNotifications();

        if (towerInstance == null)
        {
            return;
        }

        subscribedUpgradeTowerInstance = towerInstance;
        subscribedUpgradeTowerInstance.OnUpgradeRecorded += HandleUpgradeRecorded;
    }

    private void UnsubscribeFromUpgradeNotifications()
    {
        if (subscribedUpgradeTowerInstance == null)
        {
            return;
        }

        subscribedUpgradeTowerInstance.OnUpgradeRecorded -= HandleUpgradeRecorded;
        subscribedUpgradeTowerInstance = null;
    }

    private void HandleUpgradeRecorded(TowerUpgradeDefinition _)
    {
        EnsureArcaneFieldExists();
    }

    private void EnsureArcaneFieldExists()
    {
        if (IsCurrentArcaneField(activeMagicArcaneField))
        {
            return;
        }

        if (activeMagicArcaneField != null)
        {
            activeMagicArcaneField.Cleanup();
            activeMagicArcaneField = null;
        }

        MagicArcaneFieldBehaviour existingField = FindAttachedMagicArcaneField();

        if (IsCurrentArcaneField(existingField))
        {
            activeMagicArcaneField = existingField;
            return;
        }

        if (towerInstance == null ||
            !towerInstance.TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.MagicArcaneField,
                out TowerUpgradeDefinition arcaneFieldUpgrade))
        {
            if (existingField != null)
            {
                existingField.Cleanup();
            }

            return;
        }

        if (monsterManager == null)
        {
            monsterManager = FindFirstObjectByType<MonsterManager>();
        }

        MagicArcaneFieldRuntimeOptions runtimeOptions = new MagicArcaneFieldRuntimeOptions(
            arcaneFieldUpgrade,
            arcaneFieldUpgrade.ArcaneFieldRadius,
            arcaneFieldUpgrade.ArcaneFieldTickInterval,
            arcaneFieldUpgrade.ArcaneFieldTickEffect,
            arcaneFieldUpgrade.MagicArcaneFieldVfxPrefab);

        MagicArcaneFieldBehaviour field = existingField;

        if (field == null && !TryInstantiateMagicArcaneField(runtimeOptions.VfxPrefab, out field))
        {
            return;
        }

        field.Cleanup();

        if (!field.Initialize(towerInstance, monsterManager, runtimeOptions))
        {
            field.Cleanup();
            activeMagicArcaneField = null;
            return;
        }

        activeMagicArcaneField = field;
    }

    private bool IsCurrentArcaneField(MagicArcaneFieldBehaviour field)
    {
        return field != null &&
               field.IsInitialized &&
               field.SourceTower == towerInstance &&
               field.SourceUpgrade != null &&
               field.SourceUpgrade.BehaviourPackageType == TowerBehaviourPackageType.MagicArcaneField &&
               towerInstance != null &&
               field.transform != transform &&
               field.transform.IsChildOf(transform) &&
               towerInstance.HasUpgrade(field.SourceUpgrade);
    }

    private void CleanupMagicArcaneField()
    {
        MagicArcaneFieldBehaviour field = activeMagicArcaneField != null
            ? activeMagicArcaneField
            : FindAttachedMagicArcaneField();

        if (field != null)
        {
            field.Cleanup();
        }

        activeMagicArcaneField = null;
    }

    private MagicArcaneFieldBehaviour FindAttachedMagicArcaneField()
    {
        MagicArcaneFieldBehaviour[] fields = GetComponentsInChildren<MagicArcaneFieldBehaviour>(true);
        MagicArcaneFieldBehaviour selectedField = null;

        for (int i = 0; i < fields.Length; i++)
        {
            MagicArcaneFieldBehaviour field = fields[i];

            if (field == null || field.transform == transform)
            {
                continue;
            }

            if (selectedField == null)
            {
                selectedField = field;
                continue;
            }

            field.Cleanup();
        }

        return selectedField;
    }

    private bool TryInstantiateMagicArcaneField(
        GameObject fieldVfxPrefab,
        out MagicArcaneFieldBehaviour field)
    {
        field = null;

        if (fieldVfxPrefab == null)
        {
            if (!hasLoggedMissingMagicArcaneFieldVfxPrefab)
            {
                hasLoggedMissingMagicArcaneFieldVfxPrefab = true;
                Debug.LogWarning(
                    "Tower combat cannot activate Magic Arcane Field: the applied TowerUpgradeDefinition has no Magic Arcane Field VFX prefab.",
                    this);
            }

            return false;
        }

        GameObject fieldObject = Instantiate(fieldVfxPrefab, transform);
        fieldObject.transform.localPosition = Vector3.zero;
        fieldObject.transform.localRotation = Quaternion.identity;

        if (!fieldObject.TryGetComponent(out field))
        {
            if (!hasLoggedInvalidMagicArcaneFieldVfxPrefab)
            {
                hasLoggedInvalidMagicArcaneFieldVfxPrefab = true;
                Debug.LogWarning(
                    "Tower combat cannot activate Magic Arcane Field: its VFX prefab root requires MagicArcaneFieldBehaviour.",
                    fieldVfxPrefab);
            }

            fieldObject.SetActive(false);
            Destroy(fieldObject);
            return false;
        }

        return true;
    }

    private bool IsMagicMultiOrbsActive()
    {
        return IsMagicOrbRelease() &&
               HasBehaviourPackage(TowerBehaviourPackageType.MagicMultiOrbs);
    }

    private bool IsMagicOrbRelease()
    {
        return towerDefinition != null &&
               towerDefinition.TowerFamily == TowerFamily.Magic &&
               attackConfig != null &&
               attackConfig.AttackArchetype == AttackArchetype.MagicOrb;
    }

    private void UpdateDroneAttackEntity()
    {
        hasLoggedUnsupportedAttackEntity = false;

        if (attackState == TowerAttackState.WaitingForAnimationRelease)
        {
            return;
        }

        if (cooldownTimer > 0f)
        {
            attackState = TowerAttackState.Idle;
            return;
        }

        currentTarget = SelectTarget();

        if (!IsValidTarget(currentTarget))
        {
            attackState = TowerAttackState.Idle;
            return;
        }

        pendingDroneTarget = currentTarget;
        pendingAttackArchetype = AttackArchetype.Drone;
        attackState = TowerAttackState.WaitingForAnimationRelease;

        if (SetAttackAnimatorTrigger()) return;

        ReleasePendingDroneAttack();
    }

    private void ReleasePendingDroneAttack()
    {
        if (attackState != TowerAttackState.WaitingForAnimationRelease ||
            pendingAttackArchetype != AttackArchetype.Drone)
        {
            return;
        }

        if (!IsValidTarget(pendingDroneTarget) || !IsInAttackRange(pendingDroneTarget))
        {
            ResetPendingAttackState();
            return;
        }

        if (attackConfig.DronePrefab == null)
        {
            if (!hasLoggedMissingDronePrefab)
            {
                hasLoggedMissingDronePrefab = true;
                Debug.LogWarning("Tower combat cannot spawn Drone: drone prefab is not assigned.", this);
            }

            ResetPendingAttackState();
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            ResetPendingAttackState();
            return;
        }

        Vector3 releasePosition = origin.position;
        Quaternion releaseRotation = origin.rotation;
        MonsterBehaviour initialTarget = pendingDroneTarget;
        ResolvedTowerCombatStats resolvedStats = ResolveCombatStats();

        if (!TryReleaseDrone(releasePosition, releaseRotation, initialTarget, resolvedStats))
        {
            ResetPendingAttackState();
            return;
        }

        ScheduleDelayedTwinDrones(releasePosition, releaseRotation, initialTarget, resolvedStats);
        StartAttackCooldown();
        ResetPendingAttackState();
    }

    private void ScheduleDelayedTwinDrones(
        Vector3 releasePosition,
        Quaternion releaseRotation,
        MonsterBehaviour initialTarget,
        ResolvedTowerCombatStats resolvedStats)
    {
        if (!IsDroneTwinDronesActive())
        {
            return;
        }

        int droneCount = GetTwinDronesCount();
        float takeOffDelay = GetTwinDronesTakeOffDelay();

        for (int i = 1; i < droneCount; i++)
        {
            StartCoroutine(ReleaseDelayedDrone(releasePosition, releaseRotation, initialTarget, resolvedStats, takeOffDelay * i));
        }
    }

    private IEnumerator ReleaseDelayedDrone(
        Vector3 releasePosition,
        Quaternion releaseRotation,
        MonsterBehaviour initialTarget,
        ResolvedTowerCombatStats resolvedStats,
        float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        TryReleaseDrone(releasePosition, releaseRotation, initialTarget, resolvedStats);
    }

    private bool TryReleaseDrone(
        Vector3 releasePosition,
        Quaternion releaseRotation,
        MonsterBehaviour initialTarget,
        ResolvedTowerCombatStats resolvedStats)
    {
        GameObject droneObject = Instantiate(attackConfig.DronePrefab, releasePosition, releaseRotation);

        if (!droneObject.TryGetComponent(out DroneBehaviour droneBehaviour))
        {
            Debug.LogWarning("Drone prefab does not have DroneBehaviour. Adding it at runtime as a fallback.", droneObject);
            droneBehaviour = droneObject.AddComponent<DroneBehaviour>();
        }

        droneBehaviour.Initialize(
            towerInstance,
            monsterManager,
            attackConfig,
            resolvedStats,
            releasePosition,
            releaseRotation,
            initialTarget
        );

        return droneBehaviour.IsInitialized;
    }

    private bool IsDroneTwinDronesActive()
    {
        return IsDroneRelease() &&
               HasBehaviourPackage(TowerBehaviourPackageType.DroneTwinDrones);
    }

    private bool IsDroneRelease()
    {
        return towerDefinition != null &&
               towerDefinition.TowerFamily == TowerFamily.Drone &&
               attackConfig != null &&
               attackConfig.AttackArchetype == AttackArchetype.Drone;
    }

    private void UpdateUnsupportedAttackEntity()
    {
        attackState = TowerAttackState.Idle;

        if (hasLoggedUnsupportedAttackEntity)
        {
            return;
        }

        hasLoggedUnsupportedAttackEntity = true;
        Debug.LogWarning(
            $"Tower combat does not implement attack archetype '{attackConfig.AttackArchetype}' yet. Runtime behavior belongs to a later task.",
            this
        );
        OnUnsupportedAttackEntity?.Invoke(this, attackConfig.AttackArchetype);
    }

    private MonsterBehaviour SelectTarget(ISet<MonsterBehaviour> excludedTargets = null)
    {
        if (detectedEnemies.Count == 0 ||
            (excludedTargets != null && excludedTargets.Count >= detectedEnemies.Count))
        {
            return null;
        }

        switch (attackConfig.TargetSelectionType)
        {
            case TargetSelectionType.HighestHealth:
                return SelectHighestHealthTarget(excludedTargets);
            case TargetSelectionType.LowestHealth:
                return SelectLowestHealthTarget(excludedTargets);
            case TargetSelectionType.Random:
                return SelectRandomTarget(excludedTargets);
            case TargetSelectionType.Nearest:
            default:
                return SelectNearestTarget(excludedTargets);
        }
    }

    private MonsterBehaviour SelectNearestTarget(ISet<MonsterBehaviour> excludedTargets)
    {
        MonsterBehaviour selectedTarget = null;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];

            if (excludedTargets != null && excludedTargets.Contains(monster))
            {
                continue;
            }

            Transform origin = GetAttackOrigin();

            if (origin == null)
            {
                return selectedTarget;
            }

            float distanceSqr = (GetMonsterHitPosition(monster) - origin.position).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                selectedTarget = monster;
                bestDistanceSqr = distanceSqr;
            }
        }

        return selectedTarget;
    }

    private MonsterBehaviour SelectHighestHealthTarget(ISet<MonsterBehaviour> excludedTargets)
    {
        MonsterBehaviour selectedTarget = null;
        int bestHealth = int.MinValue;

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];

            if (excludedTargets != null && excludedTargets.Contains(monster))
            {
                continue;
            }

            if (monster.CurrentHealth > bestHealth)
            {
                selectedTarget = monster;
                bestHealth = monster.CurrentHealth;
            }
        }

        return selectedTarget;
    }

    private MonsterBehaviour SelectLowestHealthTarget(ISet<MonsterBehaviour> excludedTargets)
    {
        MonsterBehaviour selectedTarget = null;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];

            if (excludedTargets != null && excludedTargets.Contains(monster))
            {
                continue;
            }

            if (monster.CurrentHealth < bestHealth)
            {
                selectedTarget = monster;
                bestHealth = monster.CurrentHealth;
            }
        }

        return selectedTarget;
    }

    private MonsterBehaviour SelectRandomTarget(ISet<MonsterBehaviour> excludedTargets)
    {
        int availableTargetCount = detectedEnemies.Count - (excludedTargets?.Count ?? 0);

        if (availableTargetCount <= 0)
        {
            return null;
        }

        int selectedAvailableIndex = Random.Range(0, availableTargetCount);

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];

            if (excludedTargets != null && excludedTargets.Contains(monster))
            {
                continue;
            }

            if (selectedAvailableIndex == 0)
            {
                return monster;
            }

            selectedAvailableIndex--;
        }

        return null;
    }

    private bool IsInAttackRange(MonsterBehaviour monster)
    {
        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            return false;
        }

        float attackRange = ResolveCombatStats().AttackRange;
        return IsInRange(origin.position, GetMonsterHitPosition(monster), attackRange);
    }

    private bool IsRegisteredGameplayTarget(MonsterBehaviour monster)
    {
        if (monster == null || !monster.IsGameplayTargetable || monsterManager == null)
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

    private static bool IsInRange(Vector3 origin, Vector3 targetPosition, float range)
    {
        if (range <= 0f)
        {
            return false;
        }

        float rangeSqr = range * range;
        return (targetPosition - origin).sqrMagnitude <= rangeSqr;
    }

    private ResolvedTowerCombatStats ResolveCombatStats()
    {
        return TowerRuntimeStatResolver.Resolve(towerInstance, attackConfig);
    }

    private int GetPiercingArrowMaxHitCount()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.ArcherPiercingArrow,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.PiercingMaxHitCount
            : DefaultPiercingArrowMaxHitCount;
    }

    private float GetScatterArrowAngleOffset()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.ArcherScatterArrow,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.ScatterAngleOffset
            : DefaultScatterArrowAngleOffset;
    }

    private int ResolveCannonMaxInitialShellCount()
    {
        if (!IsCannonArcProjectileRelease() ||
            !HasBehaviourPackage(TowerBehaviourPackageType.CannonMultiShells))
        {
            return 1;
        }

        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.CannonMultiShells,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.MultiShellsMaxInitialShellCount
            : 1;
    }

    private bool IsCannonArcProjectileRelease()
    {
        return towerDefinition != null &&
               towerDefinition.TowerFamily == TowerFamily.Cannon &&
               attackConfig != null &&
               attackConfig.AttackArchetype == AttackArchetype.ArcProjectile;
    }

    private int GetMultiOrbsCount()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.MagicMultiOrbs,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.MultiOrbsCount
            : DefaultMultiOrbsCount;
    }

    private int GetTwinDronesCount()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.DroneTwinDrones,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.TwinDronesCount
            : DefaultTwinDronesCount;
    }

    private float GetTwinDronesTakeOffDelay()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.DroneTwinDrones,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.TwinDronesTakeOffDelay
            : DefaultTwinDronesTakeOffDelay;
    }

    private bool HasBehaviourPackage(TowerBehaviourPackageType packageType)
    {
        return towerInstance != null && towerInstance.HasBehaviourPackage(packageType);
    }

    private bool TryGetBehaviourPackageUpgrade(
        TowerBehaviourPackageType packageType,
        out TowerUpgradeDefinition upgradeDefinition)
    {
        if (towerInstance != null &&
            towerInstance.TryGetBehaviourPackageUpgrade(packageType, out upgradeDefinition))
        {
            return true;
        }

        if (missingBehaviourPackageWarnings.Add(packageType))
        {
            Debug.LogWarning(
                $"Tower combat could not resolve applied Behaviour package upgrade '{packageType}'. Using fallback runtime defaults.",
                this
            );
        }

        upgradeDefinition = null;
        return false;
    }

    private Transform GetAttackOrigin()
    {
        if (towerBehaviour == null)
        {
            towerBehaviour = GetComponent<TowerBehaviour>();
        }

        Transform origin = towerBehaviour != null && towerBehaviour.VisualController != null
            ? towerBehaviour.VisualController.GetCurrentAttackOrigin()
            : null;

        if (origin == null && !hasLoggedMissingAttackOrigin)
        {
            Debug.LogWarning("Tower combat cannot resolve current active AttackOrigin from TowerVisualController.", this);
            hasLoggedMissingAttackOrigin = true;
        }

        return origin;
    }

    private static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
    }

    private void PlayAttackReleaseVfx()
    {
        if (attackConfig == null || attackConfig.AttackReleaseVfxPrefab == null)
        {
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            return;
        }

        Quaternion rotation = GetAttackReleaseVfxRotation(origin);
        Instantiate(attackConfig.AttackReleaseVfxPrefab, origin.position, rotation);
    }

    private Quaternion GetAttackReleaseVfxRotation(Transform origin)
    {
        if (origin == null)
        {
            return Quaternion.identity;
        }

        switch (attackConfig.AttackArchetype)
        {
            case AttackArchetype.DirectionProjectile:
                Vector3 direction = pendingProjectileTargetPosition - origin.position;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    return Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                return Quaternion.identity;
            default:
                return Quaternion.identity;
        }
    }

    private void CleanupVfxOutsideCurrentArchetype()
    {
    }

    private bool SetAttackAnimatorTrigger()
    {
        TowerModelPresentation presentation = GetTowerModelPresentation();
        return presentation != null && presentation.RequestAttackTrigger();
    }

    private TowerModelPresentation GetTowerModelPresentation()
    {
        if (towerBehaviour == null)
        {
            towerBehaviour = GetComponent<TowerBehaviour>();
        }

        return towerBehaviour != null && towerBehaviour.VisualController != null
            ? towerBehaviour.VisualController.GetCurrentTowerModelPresentation()
            : null;
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }
}
