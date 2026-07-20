using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum DroneRuntimeState
{
    Launching,
    Orbiting
}

public class DroneBehaviour : MonoBehaviour
{
    [SerializeField] private Transform fireAnchor;
    [SerializeField] private Transform propellerTransform;
    [SerializeField] private Vector3 propellerSpinAxis = Vector3.forward;
    [SerializeField, Min(0f)] private float propellerSpinSpeedDegreesPerSecond = 1080f;

    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private AttackConfig attackConfig;
    private DroneRuntimeOptions runtimeOptions;
    private Vector3 releasePosition;
    private MonsterBehaviour currentTarget;
    private int attackDamage;
    private float attackRange;
    private float droneBurstCooldown;
    private float orbitAngleRadians;
    private int orbitDirection = 1;
    private float batteryTimer;
    private float burstTimer;
    private int burstShotsRemaining;
    private bool hasReachedOrbitPath;
    private bool isInitialized;
    private bool hasLoggedMissingFireAnchor;

    public event Action<DroneBehaviour, DroneRuntimeState> OnStateChanged;

    public TowerInstance SourceTower => sourceTower;
    public AttackConfig AttackConfig => attackConfig;
    public Transform FireAnchor => fireAnchor != null ? fireAnchor : transform;
    public MonsterBehaviour CurrentTarget => currentTarget;
    public DroneRuntimeState State { get; private set; } = DroneRuntimeState.Launching;
    public bool IsInitialized => isInitialized;

    public void Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        AttackConfig attackConfig,
        DroneRuntimeOptions runtimeOptions,
        ResolvedTowerCombatStats resolvedStats,
        Vector3 releasePosition,
        Quaternion releaseRotation,
        MonsterBehaviour initialTarget)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.attackConfig = attackConfig;
        this.runtimeOptions = runtimeOptions;
        this.releasePosition = releasePosition;

        currentTarget = initialTarget;
        attackDamage = resolvedStats.AttackDamage;
        attackRange = resolvedStats.AttackRange;
        droneBurstCooldown = resolvedStats.DroneBurstCooldown;
        orbitAngleRadians = 0f;
        orbitDirection = 1;
        batteryTimer = resolvedStats.DroneBatteryDuration;
        ResetBurstState();
        hasReachedOrbitPath = false;
        hasLoggedMissingFireAnchor = false;

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

        if (attackConfig == null)
        {
            Debug.LogWarning("Drone cannot initialize: attack config is null.", this);
            return false;
        }

        if (!IsValidTargetInRange(currentTarget))
        {
            Debug.LogWarning("Drone cannot initialize: initial target is invalid or outside source tower attack range.", this);
            return false;
        }

        if (attackConfig.DroneProjectileConfig == null || attackConfig.DroneProjectileConfig.ProjectilePrefab == null)
        {
            Debug.LogWarning("Drone cannot initialize: drone projectile config or prefab is missing.", this);
            return false;
        }

        if (batteryTimer <= 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone battery duration must be greater than zero.", this);
            return false;
        }

        if (attackConfig.DroneFlightSpeed <= 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone flight speed must be greater than zero.", this);
            return false;
        }

        if (attackConfig.DroneOrbitRadius <= 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone orbit radius must be greater than zero.", this);
            return false;
        }

        if (attackConfig.DroneBurstCount <= 0)
        {
            Debug.LogWarning("Drone cannot initialize: drone burst count must be greater than zero.", this);
            return false;
        }

        if (attackConfig.DroneBurstInterval < 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone burst interval cannot be negative.", this);
            return false;
        }

        if (droneBurstCooldown < 0f)
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
        }

        UpdatePropellerSpin();
    }

    private void UpdateLaunching()
    {
        DrainBattery();

        if (batteryTimer <= 0f)
        {
            Despawn();
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
                Despawn();
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
            Despawn();
            return;
        }

        if (!IsValidTargetInRange(currentTarget))
        {
            currentTarget = SelectTarget();

            if (!IsValidTargetInRange(currentTarget))
            {
                Despawn();
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

    private void Despawn()
    {
        currentTarget = null;
        isInitialized = false;
        ResetBurstState();
        Destroy(gameObject);
    }

    private void DrainBattery()
    {
        batteryTimer = Mathf.Max(0f, batteryTimer - Time.deltaTime);
    }

    private void UpdateBurstFire()
    {
        burstTimer = Mathf.Max(0f, burstTimer - Time.deltaTime);

        if (burstTimer > 0f)
        {
            return;
        }

        if (burstShotsRemaining <= 0)
        {
            burstShotsRemaining = Mathf.Max(1, attackConfig.DroneBurstCount);
        }

        FireProjectile(currentTarget);
        burstShotsRemaining--;

        burstTimer = burstShotsRemaining > 0
            ? Mathf.Max(0f, attackConfig.DroneBurstInterval)
            : droneBurstCooldown;
    }

    private void ResetBurstState()
    {
        burstTimer = 0f;
        burstShotsRemaining = 0;
    }

    private void FireProjectile(MonsterBehaviour target)
    {
        if (!IsValidTargetInRange(target))
        {
            return;
        }

        ProjectileConfig droneProjectileConfig = attackConfig.DroneProjectileConfig;
        Transform spawnAnchor = GetFireAnchor();
        Vector3 targetPosition = GetMonsterHitPosition(target);
        GameObject projectileObject = Instantiate(droneProjectileConfig.ProjectilePrefab, spawnAnchor.position, Quaternion.identity);

        if (!projectileObject.TryGetComponent(out ProjectileBehaviour projectileBehaviour))
        {
            projectileBehaviour = projectileObject.AddComponent<ProjectileBehaviour>();
        }

        projectileBehaviour.Initialize(
            sourceTower,
            monsterManager,
            droneProjectileConfig,
            attackConfig,
            target,
            targetPosition,
            attackDamage,
            AttackArchetype.DirectionProjectile,
            new ProjectileRuntimeOptions(
                canPierce: false,
                maxPierceHitCount: 1,
                blastRoundsSourceUpgrade: runtimeOptions.BlastRoundsSourceUpgrade,
                blastRoundsEffect: runtimeOptions.BlastRoundsEffect)
        );

        if (!projectileBehaviour.IsInitialized)
        {
            return;
        }

        PlayAttackReleaseVfx(spawnAnchor, targetPosition);
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
        if (attackConfig.AttackReleaseVfxPrefab == null)
        {
            return;
        }

        Vector3 direction = targetPosition - spawnAnchor.position;
        Quaternion rotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;

        Instantiate(attackConfig.AttackReleaseVfxPrefab, spawnAnchor.position, rotation);
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

        switch (attackConfig.TargetSelectionType)
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
        float orbitRadius = Mathf.Max(attackConfig.DroneOrbitRadius, 0.01f);
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
        float orbitRadius = Mathf.Max(attackConfig.DroneOrbitRadius, 0.01f);
        float angularSpeed = attackConfig.DroneFlightSpeed / orbitRadius;
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
            attackConfig.DroneFlightSpeed * Time.deltaTime
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
        return releasePosition.y + attackConfig.DroneFlightHeight;
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
               state == DroneRuntimeState.Orbiting;
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
