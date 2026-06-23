using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum DroneRuntimeState
{
    Resting,
    Launching,
    Hovering,
    Returning,
    Recharging
}

public class DroneBehaviour : MonoBehaviour
{
    [SerializeField] private Transform fireAnchor;
    [SerializeField] private Animator animator;
    [SerializeField] private string flyingAnimatorBoolName = "IsFlying";

    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private AttackConfig attackConfig;
    private Transform restAnchor;
    private MonsterBehaviour currentTarget;
    private Vector3 hoverPoint;
    private float batteryTimer;
    private float fireTimer;
    private float rechargeTimer;
    private bool isInitialized;
    private bool hasLoggedMissingFireAnchor;

    public event Action<DroneBehaviour, DroneRuntimeState> OnStateChanged;

    public TowerInstance SourceTower => sourceTower;
    public AttackConfig AttackConfig => attackConfig;
    public Transform FireAnchor => fireAnchor != null ? fireAnchor : transform;
    public MonsterBehaviour CurrentTarget => currentTarget;
    public DroneRuntimeState State { get; private set; } = DroneRuntimeState.Resting;
    public bool IsInitialized => isInitialized;

    public void Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        AttackConfig attackConfig,
        Transform restAnchor)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.attackConfig = attackConfig;
        this.restAnchor = restAnchor;

        CacheAnimator();
        currentTarget = null;
        batteryTimer = 0f;
        fireTimer = 0f;
        rechargeTimer = 0f;
        hasLoggedMissingFireAnchor = false;

        if (!CanInitialize())
        {
            Destroy(gameObject);
            return;
        }

        isInitialized = true;
        transform.position = GetRestPosition();
        AlignToRestPose();
        SetState(DroneRuntimeState.Resting);
    }

    private void Awake()
    {
        CacheAnimator();
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

        if (attackConfig.DroneProjectileConfig == null || attackConfig.DroneProjectileConfig.ProjectilePrefab == null)
        {
            Debug.LogWarning("Drone cannot initialize: drone projectile config or prefab is missing.", this);
            return false;
        }

        if (attackConfig.DroneMoveSpeed <= 0f)
        {
            Debug.LogWarning("Drone cannot initialize: drone move speed must be greater than zero.", this);
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
            case DroneRuntimeState.Resting:
                UpdateResting();
                break;
            case DroneRuntimeState.Launching:
                UpdateLaunching();
                break;
            case DroneRuntimeState.Hovering:
                UpdateHovering();
                break;
            case DroneRuntimeState.Returning:
                UpdateReturning();
                break;
            case DroneRuntimeState.Recharging:
                UpdateRecharging();
                break;
        }
    }

    private void UpdateResting()
    {
        transform.position = GetRestPosition();
        AlignToRestPose();

        if (SelectTarget() == null)
        {
            return;
        }

        Launch();
    }

    private void Launch()
    {
        currentTarget = null;
        batteryTimer = attackConfig.DroneBatteryDuration;
        fireTimer = 0f;
        SetState(DroneRuntimeState.Launching);
    }

    private void UpdateLaunching()
    {
        DrainBattery();

        if (batteryTimer <= 0f)
        {
            ReturnToRest();
            return;
        }

        MoveTowards(GetLaunchPosition());

        if (!IsAtPosition(GetLaunchPosition()))
        {
            return;
        }

        currentTarget = SelectTarget();

        if (!IsValidTargetInRange(currentTarget))
        {
            ReturnToRest();
            return;
        }

        hoverPoint = CalculateHoverPoint(currentTarget);
        SetState(DroneRuntimeState.Hovering);
    }

    private void UpdateHovering()
    {
        DrainBattery();

        if (batteryTimer <= 0f)
        {
            ReturnToRest();
            return;
        }

        if (!IsValidTargetInRange(currentTarget))
        {
            currentTarget = SelectTarget();

            if (!IsValidTargetInRange(currentTarget))
            {
                ReturnToRest();
                return;
            }

            hoverPoint = CalculateHoverPoint(currentTarget);
            fireTimer = 0f;
        }

        Vector3 desiredHoverPoint = CalculateHoverPoint(currentTarget);

        if ((desiredHoverPoint - hoverPoint).sqrMagnitude > 0.01f)
        {
            hoverPoint = desiredHoverPoint;
        }

        MoveTowards(hoverPoint);
        FaceTarget(currentTarget);

        if (!IsAtPosition(hoverPoint))
        {
            return;
        }

        fireTimer -= Time.deltaTime;

        if (fireTimer > 0f)
        {
            return;
        }

        FireProjectile(currentTarget);
        fireTimer = Mathf.Max(0f, attackConfig.AttackInterval);
    }

    private void UpdateReturning()
    {
        currentTarget = null;
        MoveTowards(GetRestPosition());

        if (!IsAtPosition(GetRestPosition()))
        {
            return;
        }

        AlignToRestPose();
        rechargeTimer = attackConfig.DroneRechargeDuration;
        SetState(DroneRuntimeState.Recharging);
    }

    private void UpdateRecharging()
    {
        transform.position = GetRestPosition();
        AlignToRestPose();
        rechargeTimer -= Time.deltaTime;

        if (rechargeTimer > 0f)
        {
            return;
        }

        SetState(DroneRuntimeState.Resting);
    }

    private void ReturnToRest()
    {
        currentTarget = null;
        SetState(DroneRuntimeState.Returning);
    }

    private void DrainBattery()
    {
        batteryTimer = Mathf.Max(0f, batteryTimer - Time.deltaTime);
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
            AttackArchetype.DirectionProjectile
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
            ? Quaternion.LookRotation(direction.normalized)
            : spawnAnchor.rotation;

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
        Vector3 originPosition = GetRestPosition();

        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterBehaviour monster = candidates[i];
            float distanceSqr = (GetMonsterHitPosition(monster) - originPosition).sqrMagnitude;

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

        float attackRangeSqr = attackConfig.AttackRange * attackConfig.AttackRange;
        return (GetMonsterHitPosition(monster) - GetRestPosition()).sqrMagnitude <= attackRangeSqr;
    }

    private Vector3 CalculateHoverPoint(MonsterBehaviour target)
    {
        Vector3 targetPosition = GetMonsterHitPosition(target);
        Vector3 awayDirection = targetPosition - GetRestPosition();
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude <= 0.0001f)
        {
            awayDirection = transform.position - targetPosition;
            awayDirection.y = 0f;
        }

        if (awayDirection.sqrMagnitude <= 0.0001f)
        {
            awayDirection = Vector3.forward;
        }

        Vector3 point = targetPosition + awayDirection.normalized * attackConfig.DroneHoverDistance;
        point.y = GetActiveFlightHeight();
        return point;
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            attackConfig.DroneMoveSpeed * Time.deltaTime
        );
    }

    private void FaceTarget(MonsterBehaviour target)
    {
        if (!IsValidTarget(target))
        {
            return;
        }

        Vector3 direction = GetMonsterHitPosition(target) - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void AlignToRestPose()
    {
        transform.rotation = GetRestRotation();
    }

    private bool IsAtPosition(Vector3 targetPosition)
    {
        return (transform.position - targetPosition).sqrMagnitude <= 0.0025f;
    }

    private Vector3 GetRestPosition()
    {
        return restAnchor != null ? restAnchor.position : transform.position;
    }

    private Quaternion GetRestRotation()
    {
        Quaternion baseRotation = restAnchor != null ? restAnchor.rotation : Quaternion.identity;
        return baseRotation * Quaternion.Euler(0f, 180f, 0f);
    }

    private Vector3 GetLaunchPosition()
    {
        Vector3 launchPosition = GetRestPosition();
        launchPosition.y = GetActiveFlightHeight();
        return launchPosition;
    }

    private float GetActiveFlightHeight()
    {
        return GetRestPosition().y + attackConfig.DroneLaunchHeight;
    }

    private void SetState(DroneRuntimeState state)
    {
        ApplyStatePresentation(state);

        if (State == state)
        {
            return;
        }

        State = state;
        OnStateChanged?.Invoke(this, State);
    }

    private void ApplyStatePresentation(DroneRuntimeState state)
    {
        bool isFlying = state != DroneRuntimeState.Resting &&
                        state != DroneRuntimeState.Recharging;

        SetFlyingAnimatorBool(isFlying);

        if (!isFlying)
        {
            AlignToRestPose();
        }
    }

    private void SetFlyingAnimatorBool(bool isFlying)
    {
        if (animator == null || string.IsNullOrEmpty(flyingAnimatorBoolName))
        {
            return;
        }

        animator.SetBool(flyingAnimatorBoolName, isFlying);
    }

    private void CacheAnimator()
    {
        if (animator != null)
        {
            return;
        }

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
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
