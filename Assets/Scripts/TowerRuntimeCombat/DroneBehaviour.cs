using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum DroneRuntimeState
{
    Launching,
    Orbiting,
    FinalDiving
}

public enum DroneBurstPhase
{
    ReadyToStartBurst,
    BetweenShots,
    InterBurstCooldown
}

public readonly struct DroneStatRefresh
{
    public DroneStatRefresh(
        bool refreshDamage,
        int newDamage,
        bool refreshAttackRange,
        float newAttackRange,
        float batteryDurationDelta,
        bool refreshBurstCooldown,
        float newBurstCooldown)
    {
        RefreshDamage = refreshDamage;
        NewDamage = newDamage;
        RefreshAttackRange = refreshAttackRange;
        NewAttackRange = newAttackRange;
        BatteryDurationDelta = batteryDurationDelta;
        RefreshBurstCooldown = refreshBurstCooldown;
        NewBurstCooldown = newBurstCooldown;
    }

    public bool RefreshDamage { get; }
    public int NewDamage { get; }
    public bool RefreshAttackRange { get; }
    public float NewAttackRange { get; }
    public float BatteryDurationDelta { get; }
    public bool RefreshBurstCooldown { get; }
    public float NewBurstCooldown { get; }
    public bool HasAnyChange =>
        RefreshDamage ||
        RefreshAttackRange ||
        !Mathf.Approximately(BatteryDurationDelta, 0f) ||
        RefreshBurstCooldown;
}

public class DroneBehaviour : MonoBehaviour
{
    [TitleGroup("Projectile")]
    [Required]
    [SerializeField] private ProjectileConfig projectileConfig;

    [TitleGroup("Lifetime")]
    [MinValue(0.01f)]
    [SerializeField] private float batteryDuration = 5f;

    [TitleGroup("Flight")]
    [MinValue(0.01f)]
    [SerializeField] private float orbitRadius = 1.5f;
    [TitleGroup("Flight")]
    [MinValue(0.01f)]
    [SerializeField] private float flightSpeed = 3f;
    [TitleGroup("Flight")]
    [MinValue(0f)]
    [SerializeField] private float flightHeight = 1f;

    [TitleGroup("Burst Fire")]
    [MinValue(1)]
    [SerializeField] private int burstCount = 3;
    [TitleGroup("Burst Fire")]
    [MinValue(0f)]
    [SerializeField] private float burstInterval = 0.1f;
    [TitleGroup("Burst Fire")]
    [MinValue(0f)]
    [SerializeField] private float burstCooldown = 0.5f;

    [TitleGroup("Anchors")]
    [SerializeField] private Transform fireAnchor;

    [TitleGroup("Presentation")]
    [SerializeField] private Transform propellerTransform;
    [TitleGroup("Presentation")]
    [SerializeField] private Vector3 propellerSpinAxis = Vector3.forward;
    [TitleGroup("Presentation")]
    [SerializeField, Min(0f)] private float propellerSpinSpeedDegreesPerSecond = 1080f;
    [TitleGroup("Presentation")]
    [SerializeField] private GameObject aerialDespawnVfxPrefab;

    private readonly List<MonsterBehaviour> resolvedFinalDiveExplosionTargets = new List<MonsterBehaviour>();

    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private DroneReleaseData releaseData;
    private TowerUpgradeDefinition blastRoundsSourceUpgrade;
    private TowerUpgradeDefinition finalDiveSourceUpgrade;
    private EffectDefinition blastRoundsEffect;
    private EffectDefinition finalDiveExplosionEffect;
    private Vector3 releasePosition;
    private MonsterBehaviour currentTarget;
    private int attackDamage;
    private float attackRange;
    private float currentBurstCooldown;
    private float finalDiveHitThreshold;
    private float orbitAngleRadians;
    private int orbitDirection = 1;
    private float batteryTimer;
    private float burstTimer;
    private Vector3 lastValidFinalDiveHitPosition;
    private int burstShotsRemaining;
    private DroneBurstPhase burstPhase;
    private bool hasReachedOrbitPath;
    private bool hasResolvedFinalDiveImpact;
    private bool hasResolvedBatteryEnd;
    private bool isInitialized;
    private bool hasEnded;
    private bool hasLoggedMissingFireAnchor;

    public event Action<DroneBehaviour, DroneRuntimeState> OnStateChanged;
    public event Action<ProjectileBehaviour> OnProjectileReleased;
    public event Action<DroneBehaviour> OnEnded;

    public TowerInstance SourceTower => sourceTower;
    public DroneReleaseData ReleaseData => releaseData;
    public Transform FireAnchor => fireAnchor != null ? fireAnchor : transform;
    public MonsterBehaviour CurrentTarget => currentTarget;
    public DroneRuntimeState State { get; private set; } = DroneRuntimeState.Launching;
    public DroneBurstPhase BurstPhase => burstPhase;
    public bool IsInitialized => isInitialized;
    public float BaseBatteryDuration => batteryDuration;
    public float BaseBurstCooldown => burstCooldown;

    public bool IsAuthoredConfigurationValid()
    {
        return projectileConfig != null &&
               projectileConfig.IsValid() &&
               batteryDuration > 0f &&
               orbitRadius > 0f &&
               flightSpeed > 0f &&
               flightHeight >= 0f &&
               burstCount > 0 &&
               burstInterval >= 0f &&
               burstCooldown >= 0f;
    }

    public void Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        DroneReleaseData releaseData,
        DroneRuntimeOptions runtimeOptions,
        ResolvedTowerCombatStats resolvedStats,
        Vector3 releasePosition,
        Quaternion releaseRotation,
        MonsterBehaviour initialTarget)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.releaseData = releaseData;
        blastRoundsSourceUpgrade = runtimeOptions.BlastRoundsSourceUpgrade;
        blastRoundsEffect = runtimeOptions.BlastRoundsEffect;
        finalDiveSourceUpgrade = runtimeOptions.FinalDiveSourceUpgrade;
        finalDiveHitThreshold = runtimeOptions.FinalDiveHitThreshold;
        finalDiveExplosionEffect = runtimeOptions.FinalDiveExplosionEffect;
        this.releasePosition = releasePosition;

        currentTarget = initialTarget;
        attackDamage = resolvedStats.AttackDamage;
        attackRange = resolvedStats.AttackRange;
        currentBurstCooldown = resolvedStats.DroneBurstCooldown;
        orbitAngleRadians = 0f;
        orbitDirection = 1;
        batteryTimer = resolvedStats.DroneBatteryDuration;
        ResetBurstState();
        hasReachedOrbitPath = false;
        hasResolvedFinalDiveImpact = false;
        hasResolvedBatteryEnd = false;
        hasEnded = false;
        hasLoggedMissingFireAnchor = false;
        resolvedFinalDiveExplosionTargets.Clear();

        if (!CanInitialize())
        {
            Destroy(gameObject);
            return;
        }

        isInitialized = true;
        transform.position = releasePosition;
        transform.rotation = releaseRotation;
        SetState(DroneRuntimeState.Launching);
    }

    private bool CanInitialize()
    {
        if (monsterManager == null)
        {
            Debug.LogWarning("Drone cannot initialize: monster manager is null.", this);
            return false;
        }

        if (!IsValidTargetInRange(currentTarget))
        {
            Debug.LogWarning("Drone cannot initialize: initial target is invalid or outside source tower attack range.", this);
            return false;
        }

        if (projectileConfig == null || projectileConfig.ProjectilePrefab == null)
        {
            Debug.LogWarning("Drone cannot initialize: drone projectile config or prefab is missing.", this);
            return false;
        }

        if (batteryTimer <= 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone battery duration must be greater than zero.", this);
            return false;
        }

        if (flightSpeed <= 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone flight speed must be greater than zero.", this);
            return false;
        }

        if (orbitRadius <= 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone orbit radius must be greater than zero.", this);
            return false;
        }

        if (burstCount <= 0)
        {
            Debug.LogWarning("Drone cannot initialize: drone burst count must be greater than zero.", this);
            return false;
        }

        if (burstInterval < 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone burst interval cannot be negative.", this);
            return false;
        }

        if (currentBurstCooldown < 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone burst cooldown cannot be negative.", this);
            return false;
        }

        return true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        switch (State)
        {
            case DroneRuntimeState.Launching:
                UpdateLaunching();
                break;
            case DroneRuntimeState.Orbiting:
                UpdateOrbiting();
                break;
            case DroneRuntimeState.FinalDiving:
                UpdateFinalDiving();
                break;
        }

        UpdatePropellerSpin();
    }

    private void OnDisable()
    {
        if (isInitialized && !hasEnded)
        {
            ForceCleanup();
        }
    }

    private void OnDestroy()
    {
        if (isInitialized && !hasEnded)
        {
            NotifyEndedWithoutDestroy();
        }
    }

    public void ForceCleanup()
    {
        EndDrone();
    }

    public void ApplyStatRefresh(DroneStatRefresh refresh)
    {
        if (!isInitialized || hasEnded || !refresh.HasAnyChange)
        {
            return;
        }

        if (refresh.RefreshDamage)
        {
            attackDamage = Mathf.Max(0, refresh.NewDamage);
        }

        if (refresh.RefreshAttackRange)
        {
            attackRange = Mathf.Max(0f, refresh.NewAttackRange);
        }

        if (!hasResolvedBatteryEnd &&
            (State == DroneRuntimeState.Launching || State == DroneRuntimeState.Orbiting) &&
            !Mathf.Approximately(refresh.BatteryDurationDelta, 0f))
        {
            batteryTimer = Mathf.Max(0f, batteryTimer + refresh.BatteryDurationDelta);
        }

        if (refresh.RefreshBurstCooldown)
        {
            RefreshBurstCooldown(refresh.NewBurstCooldown);
        }
    }

    public void RefreshBlastRounds(
        TowerUpgradeDefinition sourceUpgrade,
        EffectDefinition effectDefinition)
    {
        if (!isInitialized || hasEnded)
        {
            return;
        }

        blastRoundsSourceUpgrade = sourceUpgrade;
        blastRoundsEffect = effectDefinition;
    }

    public void RefreshFinalDive(
        TowerUpgradeDefinition sourceUpgrade,
        float hitThreshold,
        EffectDefinition explosionEffect)
    {
        if (!isInitialized || hasEnded || hasResolvedBatteryEnd)
        {
            return;
        }

        finalDiveSourceUpgrade = sourceUpgrade;
        finalDiveHitThreshold = Mathf.Max(0f, hitThreshold);
        finalDiveExplosionEffect = explosionEffect;
    }

    private void RefreshBurstCooldown(float resolvedBurstCooldown)
    {
        float previousBurstCooldown = currentBurstCooldown;
        currentBurstCooldown = Mathf.Max(0f, resolvedBurstCooldown);

        if (burstPhase != DroneBurstPhase.InterBurstCooldown)
        {
            return;
        }

        if (burstTimer <= 0f)
        {
            burstTimer = 0f;
            return;
        }

        burstTimer = previousBurstCooldown > 0f
            ? Mathf.Max(0f, burstTimer * currentBurstCooldown / previousBurstCooldown)
            : 0f;
    }

    private void UpdateLaunching()
    {
        if (batteryTimer <= 0f)
        {
            hasResolvedBatteryEnd = true;
            AerialDespawn();
            return;
        }

        MoveTowards(GetLaunchPosition());

        if (!IsAtPosition(GetLaunchPosition()))
        {
            return;
        }

        if (!IsValidTargetInRange(currentTarget))
        {
            currentTarget = SelectTarget();

            if (!IsValidTargetInRange(currentTarget))
            {
                AerialDespawn();
                return;
            }
        }

        BeginOrbitingTarget(currentTarget, true);
    }

    private void UpdateOrbiting()
    {
        DrainBattery();

        if (batteryTimer <= 0f)
        {
            ResolveBatteryDepletion();
            return;
        }

        if (!IsValidTargetInRange(currentTarget))
        {
            currentTarget = SelectTarget();

            if (!IsValidTargetInRange(currentTarget))
            {
                AerialDespawn();
                return;
            }

            BeginOrbitingTarget(currentTarget, true);
        }

        if (!hasReachedOrbitPath)
        {
            Vector3 entryPosition = CalculateOrbitPosition(currentTarget);
            MoveTowards(entryPosition);

            if (!IsAtPosition(entryPosition))
            {
                return;
            }

            hasReachedOrbitPath = true;
        }
        else
        {
            AdvanceOrbitAngle();
            MoveTowards(CalculateOrbitPosition(currentTarget));
        }

        UpdateBurstFire();
    }

    private void ResolveBatteryDepletion()
    {
        if (hasResolvedBatteryEnd)
        {
            return;
        }

        hasResolvedBatteryEnd = true;

        if (!IsFinalDiveEnabled() ||
            !EffectTargetResolver.IsValidMonsterTarget(currentTarget))
        {
            AerialDespawn();
            return;
        }

        lastValidFinalDiveHitPosition = EffectTargetResolver.GetMonsterHitPosition(currentTarget);
        hasResolvedFinalDiveImpact = false;
        ResetBurstState();
        SetState(DroneRuntimeState.FinalDiving);
    }

    private bool IsFinalDiveEnabled()
    {
        return finalDiveSourceUpgrade != null &&
               finalDiveHitThreshold > 0f &&
               finalDiveExplosionEffect != null;
    }

    private void UpdateFinalDiving()
    {
        Vector3 destination = lastValidFinalDiveHitPosition;

        if (EffectTargetResolver.IsValidMonsterTarget(currentTarget))
        {
            destination = EffectTargetResolver.GetMonsterHitPosition(currentTarget);
            lastValidFinalDiveHitPosition = destination;
        }

        MoveTowards(destination);

        float hitThreshold = finalDiveHitThreshold;

        if ((transform.position - destination).sqrMagnitude <= hitThreshold * hitThreshold)
        {
            ResolveFinalDiveImpact();
        }
    }

    private void ResolveFinalDiveImpact()
    {
        if (hasResolvedFinalDiveImpact)
        {
            return;
        }

        hasResolvedFinalDiveImpact = true;
        Vector3 impactPosition = transform.position;

        if (TryResolveFinalDiveDirectTarget(impactPosition, out MonsterBehaviour directTarget))
        {
            directTarget.TakeDamage(attackDamage);
            ElementalApplication.TryApplyFromTowerAttack(
                sourceTower,
                directTarget,
                impactPosition);
        }

        EffectExecutor.ExecuteWithResolvedTargets(
            finalDiveExplosionEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: finalDiveSourceUpgrade,
                targetMonster: null,
                hasTriggerPosition: true,
                triggerPosition: impactPosition,
                resolvedDamage: attackDamage,
                allowsElementalApplication: false),
            resolvedFinalDiveExplosionTargets);

        for (int i = 0; i < resolvedFinalDiveExplosionTargets.Count; i++)
        {
            ElementalApplication.TryApplyFromTowerAttack(
                sourceTower,
                resolvedFinalDiveExplosionTargets[i],
                impactPosition);
        }

        Despawn();
    }

    private bool TryResolveFinalDiveDirectTarget(
        Vector3 impactPosition,
        out MonsterBehaviour directTarget)
    {
        directTarget = null;

        if (monsterManager == null || finalDiveHitThreshold <= 0f)
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float hitThresholdSqr = finalDiveHitThreshold * finalDiveHitThreshold;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(monster))
            {
                continue;
            }

            float distanceSqr =
                (EffectTargetResolver.GetMonsterHitPosition(monster) - impactPosition).sqrMagnitude;

            if (distanceSqr > hitThresholdSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            directTarget = monster;
            nearestDistanceSqr = distanceSqr;
        }

        return directTarget != null;
    }

    private void AerialDespawn()
    {
        if (aerialDespawnVfxPrefab != null)
        {
            Instantiate(aerialDespawnVfxPrefab, transform.position, Quaternion.identity);
        }

        Despawn();
    }

    private void Despawn()
    {
        EndDrone();
    }

    private void DrainBattery()
    {
        batteryTimer = Mathf.Max(0f, batteryTimer - Time.deltaTime);
    }

    private void UpdateBurstFire()
    {
        switch (burstPhase)
        {
            case DroneBurstPhase.BetweenShots:
                burstTimer = Mathf.Max(0f, burstTimer - Time.deltaTime);

                if (burstTimer > 0f)
                {
                    return;
                }

                FireNextBurstShot();
                return;
            case DroneBurstPhase.InterBurstCooldown:
                burstTimer = Mathf.Max(0f, burstTimer - Time.deltaTime);

                if (burstTimer > 0f)
                {
                    return;
                }

                burstPhase = DroneBurstPhase.ReadyToStartBurst;
                StartBurst();
                return;
            case DroneBurstPhase.ReadyToStartBurst:
            default:
                StartBurst();
                return;
        }
    }

    private void StartBurst()
    {
        burstShotsRemaining = Mathf.Max(1, burstCount);
        FireNextBurstShot();
    }

    private void FireNextBurstShot()
    {
        if (burstShotsRemaining <= 0)
        {
            burstShotsRemaining = Mathf.Max(1, burstCount);
        }

        FireProjectile(currentTarget);
        burstShotsRemaining--;

        if (burstShotsRemaining > 0)
        {
            burstPhase = DroneBurstPhase.BetweenShots;
            burstTimer = Mathf.Max(0f, burstInterval);
            return;
        }

        burstPhase = DroneBurstPhase.InterBurstCooldown;
        burstTimer = currentBurstCooldown;
    }

    private void ResetBurstState()
    {
        burstTimer = 0f;
        burstShotsRemaining = 0;
        burstPhase = DroneBurstPhase.ReadyToStartBurst;
    }

    private void FireProjectile(MonsterBehaviour target)
    {
        if (!IsValidTargetInRange(target))
        {
            return;
        }

        Transform spawnAnchor = GetFireAnchor();
        Vector3 targetPosition = GetMonsterHitPosition(target);
        GameObject projectileObject = Instantiate(projectileConfig.ProjectilePrefab, spawnAnchor.position, Quaternion.identity);

        if (!projectileObject.TryGetComponent(out ProjectileBehaviour projectileBehaviour))
        {
            projectileBehaviour = projectileObject.AddComponent<ProjectileBehaviour>();
        }

        projectileBehaviour.Initialize(
            sourceTower,
            monsterManager,
            projectileConfig,
            target,
            targetPosition,
            attackDamage,
            flightType: ProjectileFlightType.Direction,
            runtimeOptions: new ProjectileRuntimeOptions(
                canPierce: false,
                maxPierceHitCount: 1,
                blastRoundsSourceUpgrade: blastRoundsSourceUpgrade,
                blastRoundsEffect: blastRoundsEffect)
        );

        if (!projectileBehaviour.IsInitialized)
        {
            return;
        }

        OnProjectileReleased?.Invoke(projectileBehaviour);
        PlayAttackReleaseVfx(spawnAnchor, targetPosition);
    }

    private void EndDrone()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        isInitialized = false;
        currentTarget = null;
        ResetBurstState();
        OnEnded?.Invoke(this);
        Destroy(gameObject);
    }

    private void NotifyEndedWithoutDestroy()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        isInitialized = false;
        currentTarget = null;
        ResetBurstState();
        OnEnded?.Invoke(this);
    }

    private Transform GetFireAnchor()
    {
        if (fireAnchor != null)
        {
            return fireAnchor;
        }

        if (!hasLoggedMissingFireAnchor)
        {
            hasLoggedMissingFireAnchor = true;
            Debug.LogWarning("Drone fireAnchor is not assigned. Using Drone transform as a fallback.", this);
        }

        return transform;
    }

    private void PlayAttackReleaseVfx(Transform spawnAnchor, Vector3 targetPosition)
    {
        if (releaseData.AttackReleaseVfxPrefab == null)
        {
            return;
        }

        Vector3 direction = targetPosition - spawnAnchor.position;
        Quaternion rotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;

        Instantiate(releaseData.AttackReleaseVfxPrefab, spawnAnchor.position, rotation);
    }

    private MonsterBehaviour SelectTarget()
    {
        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        List<MonsterBehaviour> candidates = new List<MonsterBehaviour>();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (IsValidTargetInRange(monster))
            {
                candidates.Add(monster);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        switch (releaseData.TargetSelectionType)
        {
            case TargetSelectionType.HighestHealth:
                return SelectHighestHealthTarget(candidates);
            case TargetSelectionType.LowestHealth:
                return SelectLowestHealthTarget(candidates);
            case TargetSelectionType.Random:
                return candidates[Random.Range(0, candidates.Count)];
            case TargetSelectionType.Nearest:
            default:
                return SelectNearestTarget(candidates);
        }
    }

    private MonsterBehaviour SelectNearestTarget(IReadOnlyList<MonsterBehaviour> candidates)
    {
        MonsterBehaviour selectedTarget = null;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterBehaviour monster = candidates[i];
            float distanceSqr = (GetMonsterHitPosition(monster) - releasePosition).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                selectedTarget = monster;
                bestDistanceSqr = distanceSqr;
            }
        }

        return selectedTarget;
    }

    private static MonsterBehaviour SelectHighestHealthTarget(IReadOnlyList<MonsterBehaviour> candidates)
    {
        MonsterBehaviour selectedTarget = null;
        int bestHealth = int.MinValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterBehaviour monster = candidates[i];

            if (monster.CurrentHealth > bestHealth)
            {
                selectedTarget = monster;
                bestHealth = monster.CurrentHealth;
            }
        }

        return selectedTarget;
    }

    private static MonsterBehaviour SelectLowestHealthTarget(IReadOnlyList<MonsterBehaviour> candidates)
    {
        MonsterBehaviour selectedTarget = null;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterBehaviour monster = candidates[i];

            if (monster.CurrentHealth < bestHealth)
            {
                selectedTarget = monster;
                bestHealth = monster.CurrentHealth;
            }
        }

        return selectedTarget;
    }

    private bool IsValidTargetInRange(MonsterBehaviour monster)
    {
        if (!IsValidTarget(monster))
        {
            return false;
        }

        float attackRangeSqr = attackRange * attackRange;
        return (GetMonsterHitPosition(monster) - releasePosition).sqrMagnitude <= attackRangeSqr;
    }

    private void BeginOrbitingTarget(MonsterBehaviour target, bool resetBurstState)
    {
        currentTarget = target;
        hasReachedOrbitPath = false;
        InitializeOrbitAngle(target);
        orbitDirection = ChooseOrbitDirection();

        if (resetBurstState)
        {
            ResetBurstState();
        }

        SetState(DroneRuntimeState.Orbiting);
    }

    private void InitializeOrbitAngle(MonsterBehaviour target)
    {
        Vector3 targetPosition = GetMonsterHitPosition(target);
        Vector3 radialDirection = transform.position - targetPosition;
        radialDirection.y = 0f;

        if (radialDirection.sqrMagnitude <= 0.0001f)
        {
            radialDirection = Vector3.Cross(Vector3.up, GetPlanarForward());
        }

        if (radialDirection.sqrMagnitude <= 0.0001f)
        {
            radialDirection = Vector3.forward;
        }

        radialDirection.Normalize();
        orbitAngleRadians = Mathf.Atan2(radialDirection.z, radialDirection.x);
    }

    private int ChooseOrbitDirection()
    {
        Vector3 forward = GetPlanarForward();
        Vector3 positiveTangent = CalculateOrbitTangent(orbitAngleRadians);
        Vector3 negativeTangent = -positiveTangent;

        return Vector3.Dot(forward, positiveTangent) >= Vector3.Dot(forward, negativeTangent) ? 1 : -1;
    }

    private Vector3 CalculateOrbitPosition(MonsterBehaviour target)
    {
        Vector3 targetPosition = GetMonsterHitPosition(target);
        float orbitRadius = Mathf.Max(this.orbitRadius, 0.01f);
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngleRadians),
            0f,
            Mathf.Sin(orbitAngleRadians)
        ) * orbitRadius;
        Vector3 point = targetPosition + orbitOffset;
        point.y = GetActiveFlightHeight();
        return point;
    }

    private void AdvanceOrbitAngle()
    {
        float orbitRadius = Mathf.Max(this.orbitRadius, 0.01f);
        float angularSpeed = flightSpeed / orbitRadius;
        orbitAngleRadians += orbitDirection * angularSpeed * Time.deltaTime;
    }

    private static Vector3 CalculateOrbitTangent(float angleRadians)
    {
        return new Vector3(
            -Mathf.Sin(angleRadians),
            0f,
            Mathf.Cos(angleRadians)
        ).normalized;
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        FaceMovementDirection(targetPosition);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            flightSpeed * Time.deltaTime
        );
    }

    private void FaceMovementDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private Vector3 GetPlanarForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    private bool IsAtPosition(Vector3 targetPosition)
    {
        return (transform.position - targetPosition).sqrMagnitude <= 0.0025f;
    }

    private Vector3 GetLaunchPosition()
    {
        Vector3 launchPosition = releasePosition;
        launchPosition.y = GetActiveFlightHeight();
        return launchPosition;
    }

    private float GetActiveFlightHeight()
    {
        return releasePosition.y + flightHeight;
    }

    private void SetState(DroneRuntimeState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        OnStateChanged?.Invoke(this, State);
    }

    private void UpdatePropellerSpin()
    {
        if (!ShouldSpinPropeller(State) ||
            propellerTransform == null ||
            propellerSpinSpeedDegreesPerSecond <= 0f)
        {
            return;
        }

        Vector3 spinAxis = propellerSpinAxis.sqrMagnitude > 0.0001f
            ? propellerSpinAxis.normalized
            : Vector3.forward;

        propellerTransform.Rotate(
            spinAxis,
            propellerSpinSpeedDegreesPerSecond * Time.deltaTime,
            Space.Self
        );
    }

    private static bool ShouldSpinPropeller(DroneRuntimeState state)
    {
        return state == DroneRuntimeState.Launching ||
               state == DroneRuntimeState.Orbiting ||
               state == DroneRuntimeState.FinalDiving;
    }

    private static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }
}
