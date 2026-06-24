using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TowerCombatBehaviour : MonoBehaviour
{
    [SerializeField] private TowerInstance towerInstance;
    private MonsterManager monsterManager;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackOrigin;
    //[SerializeField] private Transform projectileSpawnPoint;

    private readonly List<MonsterBehaviour> detectedEnemies = new List<MonsterBehaviour>();

    private TowerDefinition towerDefinition;
    private AttackConfig attackConfig;
    private MonsterBehaviour currentTarget;
    private MonsterBehaviour pendingProjectileTarget;
    private Vector3 pendingProjectileTargetPosition;
    private MagicOrbBehaviour activeMagicOrb;
    private DroneBehaviour activeDrone;
    private float cooldownTimer;
    private TowerAttackState attackState = TowerAttackState.Idle;
    private bool hasLoggedUnsupportedAttackEntity;
    private bool hasLoggedMissingMagicOrbPrefab;
    private bool hasLoggedMissingDronePrefab;

    public event Action<TowerCombatBehaviour, MonsterBehaviour> OnProjectileReleased;
    public event Action<TowerCombatBehaviour, AttackArchetype> OnUnsupportedAttackEntity;

    public MonsterBehaviour CurrentTarget => currentTarget;
    public IReadOnlyList<MonsterBehaviour> DetectedEnemies => detectedEnemies;
    public float CooldownTimer => cooldownTimer;
    public TowerAttackState AttackState => attackState;
    public bool IsAttacking => attackState != TowerAttackState.Idle;

    public void Initialize(TowerInstance towerInstance, MonsterManager monsterManager)
    {
        CleanupActiveMagicOrb();
        CleanupActiveDrone();
        CleanupActiveVfx();

        this.towerInstance = towerInstance;
        this.monsterManager = monsterManager;
        towerDefinition = towerInstance != null ? towerInstance.TowerDefinition : null;
        attackConfig = towerDefinition != null ? towerDefinition.AttackConfig : null;

        CacheOptionalReferences();

        cooldownTimer = 0f;
        currentTarget = null;
        pendingProjectileTarget = null;
        activeMagicOrb = null;
        activeDrone = null;
        hasLoggedUnsupportedAttackEntity = false;
        hasLoggedMissingMagicOrbPrefab = false;
        hasLoggedMissingDronePrefab = false;
        attackState = TowerAttackState.Idle;
        ResetAttackingAnimatorBoolIfUsed();
    }

    public void OnAttackAnimationRelease()
    {
        if (attackState != TowerAttackState.WaitingForAnimationRelease)
        {
            return;
        }

        ReleasePendingProjectileAttack();
    }

    private void Awake()
    {
        CacheOptionalReferences();
    }

    private void OnDisable()
    {
        CleanupActiveMagicOrb();
        CleanupActiveDrone();
        CleanupActiveVfx();
    }

    private void OnDestroy()
    {
        CleanupActiveMagicOrb();
        CleanupActiveDrone();
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

        return attackConfig != null && monsterManager != null;
    }

    private void CacheOptionalReferences()
    {
        if (towerInstance == null)
        {
            towerInstance = GetComponent<TowerInstance>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (attackOrigin == null)
        {
            attackOrigin = transform;
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
                ResetProjectileAttackState();
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
        attackState = TowerAttackState.WaitingForAnimationRelease;
        SetAttackingAnimatorBool(true);

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
            ResetProjectileAttackState();
            return;
        }

        ProjectileConfig projectileConfig = attackConfig.ProjectileConfig;

        if (projectileConfig == null || projectileConfig.ProjectilePrefab == null)
        {
            Debug.LogWarning("Tower combat cannot release projectile: projectile config or prefab is missing.", this);
            ResetProjectileAttackState();
            return;
        }

        Vector3 spawnPosition = attackOrigin != null ? attackOrigin.position : transform.position;
        GameObject projectileObject = Instantiate(projectileConfig.ProjectilePrefab, spawnPosition, Quaternion.identity);

        if (!projectileObject.TryGetComponent(out ProjectileBehaviour projectileBehaviour))
        {
            projectileBehaviour = projectileObject.AddComponent<ProjectileBehaviour>();
        }

        projectileBehaviour.Initialize(
            towerInstance,
            monsterManager,
            projectileConfig,
            attackConfig,
            pendingProjectileTarget,
            pendingProjectileTargetPosition
        );

        if (!projectileBehaviour.IsInitialized)
        {
            ResetProjectileAttackState();
            return;
        }

        StartAttackCooldown();
        PlayAttackReleaseVfx();
        OnProjectileReleased?.Invoke(this, pendingProjectileTarget);
        ResetProjectileAttackState();
    }

    private void StartAttackCooldown()
    {
        cooldownTimer = Mathf.Max(0f, attackConfig.AttackInterval);
    }

    private void ResetProjectileAttackState()
    {
        pendingProjectileTarget = null;
        pendingProjectileTargetPosition = Vector3.zero;
        SetAttackingAnimatorBool(false);
        attackState = TowerAttackState.Idle;
    }

    private void UpdateMagicOrbAttackEntity()
    {
        hasLoggedUnsupportedAttackEntity = false;

        if (activeMagicOrb != null)
        {
            attackState = TowerAttackState.MagicOrbActive;
            SetAttackingAnimatorBool(true);
            return;
        }

        if (cooldownTimer > 0f || detectedEnemies.Count == 0)
        {
            attackState = TowerAttackState.Idle;
            SetAttackingAnimatorBool(false);
            return;
        }

        SpawnMagicOrbAttackEntity();
    }

    private void SpawnMagicOrbAttackEntity()
    {
        if (attackConfig.MagicOrbPrefab == null)
        {
            attackState = TowerAttackState.Idle;
            SetAttackingAnimatorBool(false);

            if (!hasLoggedMissingMagicOrbPrefab)
            {
                hasLoggedMissingMagicOrbPrefab = true;
                Debug.LogWarning("Tower combat cannot spawn Magic Orb: magic orb prefab is not assigned.", this);
            }

            return;
        }

        Transform origin = GetAttackOrigin();
        GameObject magicOrbObject = Instantiate(attackConfig.MagicOrbPrefab, origin.position, Quaternion.identity);

        if (!magicOrbObject.TryGetComponent(out MagicOrbBehaviour magicOrbBehaviour))
        {
            Debug.LogWarning("Magic Orb prefab does not have MagicOrbBehaviour. Adding it at runtime as a fallback.", magicOrbObject);
            magicOrbBehaviour = magicOrbObject.AddComponent<MagicOrbBehaviour>();
        }

        activeMagicOrb = magicOrbBehaviour;
        activeMagicOrb.OnEnded += HandleMagicOrbEnded;
        magicOrbBehaviour.Initialize(towerInstance, monsterManager, attackConfig, origin);

        if (!magicOrbBehaviour.IsInitialized)
        {
            CleanupActiveMagicOrbReference(magicOrbBehaviour);
            return;
        }

        attackState = TowerAttackState.MagicOrbActive;
        SetAttackingAnimatorBool(true);
        PlayAttackReleaseVfx();
    }

    private void HandleMagicOrbEnded(MagicOrbBehaviour magicOrb)
    {
        if (magicOrb != activeMagicOrb)
        {
            return;
        }

        CleanupActiveMagicOrbReference(magicOrb);
        StartAttackCooldown();
        SetAttackingAnimatorBool(false);
        attackState = TowerAttackState.Idle;
    }

    private void UpdateDroneAttackEntity()
    {
        hasLoggedUnsupportedAttackEntity = false;

        if (activeDrone != null)
        {
            SyncDroneAttackState(activeDrone.State);
            return;
        }

        if (detectedEnemies.Count == 0)
        {
            attackState = TowerAttackState.Idle;
            SetAttackingAnimatorBool(false);
            return;
        }

        SpawnDroneAttackEntity();
    }

    private void SpawnDroneAttackEntity()
    {
        if (attackConfig.DronePrefab == null)
        {
            attackState = TowerAttackState.Idle;
            SetAttackingAnimatorBool(false);

            if (!hasLoggedMissingDronePrefab)
            {
                hasLoggedMissingDronePrefab = true;
                Debug.LogWarning("Tower combat cannot spawn Drone: drone prefab is not assigned.", this);
            }

            return;
        }

        Transform origin = GetAttackOrigin();
        GameObject droneObject = Instantiate(attackConfig.DronePrefab, origin.position, origin.rotation);

        if (!droneObject.TryGetComponent(out DroneBehaviour droneBehaviour))
        {
            Debug.LogWarning("Drone prefab does not have DroneBehaviour. Adding it at runtime as a fallback.", droneObject);
            droneBehaviour = droneObject.AddComponent<DroneBehaviour>();
        }

        activeDrone = droneBehaviour;
        activeDrone.OnStateChanged += HandleDroneStateChanged;
        droneBehaviour.Initialize(towerInstance, monsterManager, attackConfig, origin);

        if (!droneBehaviour.IsInitialized)
        {
            CleanupActiveDroneReference(droneBehaviour);
            return;
        }

        SyncDroneAttackState(droneBehaviour.State);
    }

    private void HandleDroneStateChanged(DroneBehaviour drone, DroneRuntimeState state)
    {
        if (drone != activeDrone)
        {
            return;
        }

        SyncDroneAttackState(state);
    }

    private void SyncDroneAttackState(DroneRuntimeState state)
    {
        switch (state)
        {
            case DroneRuntimeState.Launching:
            case DroneRuntimeState.Hovering:
            case DroneRuntimeState.Returning:
                attackState = TowerAttackState.DroneLaunched;
                SetAttackingAnimatorBool(true);
                break;
            case DroneRuntimeState.Recharging:
                attackState = TowerAttackState.DroneRecharging;
                SetAttackingAnimatorBool(false);
                break;
            case DroneRuntimeState.Resting:
            default:
                attackState = TowerAttackState.DroneResting;
                SetAttackingAnimatorBool(false);
                break;
        }
    }

    private void UpdateUnsupportedAttackEntity()
    {
        attackState = TowerAttackState.Idle;
        SetAttackingAnimatorBool(false);

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
            float distanceSqr = (GetMonsterHitPosition(monster) - GetAttackOrigin().position).sqrMagnitude;

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
        float attackRange = attackConfig.AttackRange;
        Vector3 originPosition = GetAttackOrigin().position;
        return Vector3.Distance(originPosition, GetMonsterHitPosition(monster)) <= attackRange;
    }

    private Transform GetAttackOrigin()
    {
        return attackOrigin != null ? attackOrigin : transform;
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

    private void CleanupActiveMagicOrb()
    {
        if (activeMagicOrb == null)
        {
            return;
        }

        MagicOrbBehaviour magicOrb = activeMagicOrb;
        CleanupActiveMagicOrbReference(magicOrb);

        if (magicOrb != null)
        {
            Destroy(magicOrb.gameObject);
        }
    }

    private void CleanupActiveMagicOrbReference(MagicOrbBehaviour magicOrb)
    {
        if (magicOrb != null)
        {
            magicOrb.OnEnded -= HandleMagicOrbEnded;
        }

        if (activeMagicOrb == magicOrb)
        {
            activeMagicOrb = null;
        }
    }

    private void CleanupActiveDrone()
    {
        if (activeDrone == null)
        {
            return;
        }

        DroneBehaviour drone = activeDrone;
        CleanupActiveDroneReference(drone);

        if (drone != null)
        {
            Destroy(drone.gameObject);
        }
    }

    private void CleanupActiveDroneReference(DroneBehaviour drone)
    {
        if (drone != null)
        {
            drone.OnStateChanged -= HandleDroneStateChanged;
        }

        if (activeDrone == drone)
        {
            activeDrone = null;
        }
    }

    private void CleanupVfxOutsideCurrentArchetype()
    {
    }

    private void ResetAttackingAnimatorBoolIfUsed()
    {
        SetAttackingAnimatorBool(false);
    }

    private void SetAttackingAnimatorBool(bool isAttacking)
    {
        if (animator == null || attackConfig == null || string.IsNullOrEmpty(attackConfig.AttackingAnimatorBoolName))
        {
            return;
        }

        animator.SetBool(attackConfig.AttackingAnimatorBoolName, isAttacking);
    }

    private bool SetAttackAnimatorTrigger()
    {
        if (animator == null || attackConfig == null || string.IsNullOrEmpty(attackConfig.AttackAnimatorTriggerName))
        {
            return false;
        }
        
        animator.SetTrigger(attackConfig.AttackAnimatorTriggerName);
        return true;
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }
}
