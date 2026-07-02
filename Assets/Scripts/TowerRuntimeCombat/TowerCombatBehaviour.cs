using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TowerCombatBehaviour : MonoBehaviour
{
    private const int DefaultPiercingArrowMaxHitCount = 3;
    private const float DefaultScatterArrowAngleOffset = 15f;
    private const int DefaultTwinOrbsCount = 2;
    private const float DefaultTwinOrbsStartingAngleOffset = 180f;
    private const int DefaultTwinDronesCount = 2;
    private const float DefaultTwinDronesTakeOffDelay = 0.6f;

    [SerializeField] private TowerInstance towerInstance;
    private MonsterManager monsterManager;
    private TowerBehaviour towerBehaviour;

    private readonly List<MonsterBehaviour> detectedEnemies = new List<MonsterBehaviour>();
    private readonly HashSet<TowerBehaviourPackageType> missingBehaviourPackageWarnings = new HashSet<TowerBehaviourPackageType>();

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

    public event Action<TowerCombatBehaviour, MonsterBehaviour> OnProjectileReleased;
    public event Action<TowerCombatBehaviour, AttackArchetype> OnUnsupportedAttackEntity;

    public MonsterBehaviour CurrentTarget => currentTarget;
    public IReadOnlyList<MonsterBehaviour> DetectedEnemies => detectedEnemies;
    public float CooldownTimer => cooldownTimer;
    public TowerAttackState AttackState => attackState;
    public bool IsAttacking => attackState != TowerAttackState.Idle;

    public void Initialize(TowerInstance towerInstance, MonsterManager monsterManager)
    {
        CleanupActiveVfx();

        this.towerInstance = towerInstance;
        this.monsterManager = monsterManager;
        towerDefinition = towerInstance != null ? towerInstance.TowerDefinition : null;
        attackConfig = towerDefinition != null ? towerDefinition.AttackConfig : null;

        CacheOptionalReferences();

        cooldownTimer = 0f;
        currentTarget = null;
        pendingProjectileTarget = null;
        pendingProjectileTargetPosition = Vector3.zero;
        pendingDroneTarget = null;
        pendingAttackArchetype = default;
        hasLoggedUnsupportedAttackEntity = false;
        hasLoggedMissingMagicOrbPrefab = false;
        hasLoggedMissingDronePrefab = false;
        hasLoggedMissingAttackOrigin = false;
        missingBehaviourPackageWarnings.Clear();
        attackState = TowerAttackState.Idle;
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

    private void OnDisable()
    {
        CleanupActiveVfx();
    }

    private void OnDestroy()
    {
        CleanupActiveVfx();
    }

    private void Update()
    {
        if (!CanRunCombat())
        {
            CleanupActiveVfx();
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
            if (!IsValidTarget(pendingProjectileTarget) || !IsInAttackRange(pendingProjectileTarget))
            {
                ResetPendingAttackState();
            }

            return;
        }

        if (cooldownTimer > 0f)
        {
            return;
        }

        currentTarget = SelectTarget();

        if (!IsValidTarget(currentTarget))
        {
            return;
        }

        pendingProjectileTarget = currentTarget;
        pendingProjectileTargetPosition = GetMonsterHitPosition(currentTarget);
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

        if (!IsValidTarget(pendingProjectileTarget))
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

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            ResetPendingAttackState();
            return;
        }

        ProjectileRuntimeOptions projectileRuntimeOptions = CreateProjectileRuntimeOptions();
        bool releasedProjectile = IsArcherScatterArrowActive()
            ? TryReleaseScatterProjectiles(projectileConfig, origin, projectileRuntimeOptions)
            : TryReleaseProjectile(projectileConfig, origin, pendingProjectileTargetPosition, pendingProjectileTarget, projectileRuntimeOptions);

        if (!releasedProjectile)
        {
            ResetPendingAttackState();
            return;
        }

        StartAttackCooldown();
        PlayAttackReleaseVfx();
        OnProjectileReleased?.Invoke(this, pendingProjectileTarget);
        ResetPendingAttackState();
    }

    private bool TryReleaseScatterProjectiles(
        ProjectileConfig projectileConfig,
        Transform origin,
        ProjectileRuntimeOptions projectileRuntimeOptions)
    {
        Vector3 centerDirection = pendingProjectileTargetPosition - origin.position;

        if (centerDirection.sqrMagnitude <= 0.0001f)
        {
            centerDirection = origin.forward;
        }

        centerDirection.Normalize();

        bool releasedCenter = TryReleaseProjectile(
            projectileConfig,
            origin,
            pendingProjectileTargetPosition,
            pendingProjectileTarget,
            projectileRuntimeOptions);

        bool releasedLeft = TryReleaseProjectileInDirection(
            projectileConfig,
            origin,
            Quaternion.AngleAxis(-GetScatterArrowAngleOffset(), Vector3.up) * centerDirection,
            projectileRuntimeOptions);

        bool releasedRight = TryReleaseProjectileInDirection(
            projectileConfig,
            origin,
            Quaternion.AngleAxis(GetScatterArrowAngleOffset(), Vector3.up) * centerDirection,
            projectileRuntimeOptions);

        return releasedCenter || releasedLeft || releasedRight;
    }

    private bool TryReleaseProjectileInDirection(
        ProjectileConfig projectileConfig,
        Transform origin,
        Vector3 direction,
        ProjectileRuntimeOptions projectileRuntimeOptions)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 targetPosition = origin.position + direction.normalized;
        return TryReleaseProjectile(projectileConfig, origin, targetPosition, pendingProjectileTarget, projectileRuntimeOptions);
    }

    private bool TryReleaseProjectile(
        ProjectileConfig projectileConfig,
        Transform origin,
        Vector3 targetPosition,
        MonsterBehaviour target,
        ProjectileRuntimeOptions projectileRuntimeOptions)
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
            ResolveCombatStats().AttackDamage,
            runtimeOptions: projectileRuntimeOptions
        );

        return projectileBehaviour.IsInitialized;
    }

    private ProjectileRuntimeOptions CreateProjectileRuntimeOptions()
    {
        bool canPierce = IsArcherPiercingArrowActive();

        return new ProjectileRuntimeOptions(
            canPierce,
            canPierce ? GetPiercingArrowMaxHitCount() : 1
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

    private bool IsArcherProjectileRelease()
    {
        return towerDefinition != null &&
               towerDefinition.TowerFamily == TowerFamily.Archer &&
               attackConfig != null &&
               attackConfig.AttackArchetype == AttackArchetype.DirectionProjectile;
    }

    private void StartAttackCooldown()
    {
        cooldownTimer = ResolveCombatStats().AttackInterval;
    }

    private void ResetPendingAttackState()
    {
        pendingProjectileTarget = null;
        pendingProjectileTargetPosition = Vector3.zero;
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
        bool releasedMagicOrb = IsMagicTwinOrbsActive()
            ? TryReleaseTwinMagicOrbs(origin, resolvedStats)
            : TryReleaseMagicOrb(origin, resolvedStats);

        if (!releasedMagicOrb)
        {
            ResetPendingAttackState();
            return;
        }

        StartAttackCooldown();
        PlayAttackReleaseVfx();
        ResetPendingAttackState();
    }

    private bool TryReleaseTwinMagicOrbs(Transform origin, ResolvedTowerCombatStats resolvedStats)
    {
        float baseStartingOrbitAngle = UnityEngine.Random.Range(0f, 360f);
        int orbCount = GetTwinOrbsCount();
        bool releasedAnyOrb = false;

        for (int i = 0; i < orbCount; i++)
        {
            float startingOrbitAngle = baseStartingOrbitAngle + GetTwinOrbsStartingAngleOffset() * i;
            releasedAnyOrb |= TryReleaseMagicOrb(origin, resolvedStats, startingOrbitAngle);
        }

        return releasedAnyOrb;
    }

    private bool TryReleaseMagicOrb(
        Transform origin,
        ResolvedTowerCombatStats resolvedStats,
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
            origin,
            startingOrbitAngle
        );

        return magicOrbBehaviour.IsInitialized;
    }

    private bool IsMagicTwinOrbsActive()
    {
        return IsMagicOrbRelease() &&
               HasBehaviourPackage(TowerBehaviourPackageType.MagicTwinOrbs);
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

    private MonsterBehaviour SelectTarget()
    {
        if (detectedEnemies.Count == 0)
        {
            return null;
        }

        switch (attackConfig.TargetSelectionType)
        {
            case TargetSelectionType.HighestHealth:
                return SelectHighestHealthTarget();
            case TargetSelectionType.LowestHealth:
                return SelectLowestHealthTarget();
            case TargetSelectionType.Random:
                return detectedEnemies[Random.Range(0, detectedEnemies.Count)];
            case TargetSelectionType.Nearest:
            default:
                return SelectNearestTarget();
        }
    }

    private MonsterBehaviour SelectNearestTarget()
    {
        MonsterBehaviour selectedTarget = null;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];
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

    private MonsterBehaviour SelectHighestHealthTarget()
    {
        MonsterBehaviour selectedTarget = null;
        int bestHealth = int.MinValue;

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];

            if (monster.CurrentHealth > bestHealth)
            {
                selectedTarget = monster;
                bestHealth = monster.CurrentHealth;
            }
        }

        return selectedTarget;
    }

    private MonsterBehaviour SelectLowestHealthTarget()
    {
        MonsterBehaviour selectedTarget = null;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];

            if (monster.CurrentHealth < bestHealth)
            {
                selectedTarget = monster;
                bestHealth = monster.CurrentHealth;
            }
        }

        return selectedTarget;
    }

    private bool IsInAttackRange(MonsterBehaviour monster)
    {
        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            return false;
        }

        float attackRange = ResolveCombatStats().AttackRange;
        return Vector3.Distance(origin.position, GetMonsterHitPosition(monster)) <= attackRange;
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

    private int GetTwinOrbsCount()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.MagicTwinOrbs,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.TwinOrbsCount
            : DefaultTwinOrbsCount;
    }

    private float GetTwinOrbsStartingAngleOffset()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.MagicTwinOrbs,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.TwinOrbsStartingAngleOffset
            : DefaultTwinOrbsStartingAngleOffset;
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

    private void CleanupActiveVfx()
    {
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
