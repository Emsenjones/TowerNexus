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
    private float cooldownTimer;
    private TowerAttackState attackState = TowerAttackState.Idle;
    private bool hasLoggedUnsupportedAttackEntity;

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
        hasLoggedUnsupportedAttackEntity = false;
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

        switch (attackConfig.AttackArchetype)
        {
            case AttackArchetype.DirectionProjectile:
            case AttackArchetype.ArcProjectile:
                UpdateProjectileAttack();
                break;
            case AttackArchetype.MagicOrb:
            case AttackArchetype.Drone:
                UpdateUnsupportedAttackEntity();
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
        pendingProjectileTargetPosition = currentTarget.HitAnchor.position;
        attackState = TowerAttackState.WaitingForAnimationRelease;
        cooldownTimer = Mathf.Max(0f, attackConfig.AttackInterval);
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

        if (projectileBehaviour.IsInitialized)
        {
            PlayProjectileReleaseVfx();
        }

        OnProjectileReleased?.Invoke(this, pendingProjectileTarget);
        ResetProjectileAttackState();
    }

    private void ResetProjectileAttackState()
    {
        pendingProjectileTarget = null;
        pendingProjectileTargetPosition = Vector3.zero;
        SetAttackingAnimatorBool(false);
        attackState = TowerAttackState.Idle;
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
            float distanceSqr = (monster.HitAnchor.position - GetAttackOrigin().position).sqrMagnitude;

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
        return Vector3.Distance(originPosition, monster.HitAnchor.position) <= attackRange;
    }

    private Transform GetAttackOrigin()
    {
        return attackOrigin != null ? attackOrigin : transform;
    }

    private void PlayProjectileReleaseVfx()
    {
        if (attackConfig == null || attackConfig.ProjectileReleaseVfxPrefab == null)
        {
            return;
        }

        Transform origin = GetAttackOrigin();
        Quaternion rotation = GetProjectileReleaseVfxRotation(origin);
        Instantiate(attackConfig.ProjectileReleaseVfxPrefab, origin.position, rotation);
    }

    private Quaternion GetProjectileReleaseVfxRotation(Transform origin)
    {
        if (origin == null)
        {
            return transform.rotation;
        }

        switch (attackConfig.AttackArchetype)
        {
            case AttackArchetype.DirectionProjectile:
                Vector3 direction = pendingProjectileTargetPosition - origin.position;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    return Quaternion.LookRotation(direction.normalized);
                }

                return origin.rotation;
            case AttackArchetype.ArcProjectile:
                return Quaternion.LookRotation(Vector3.up);
            default:
                return origin.rotation;
        }
    }

    private void CleanupActiveVfx()
    {
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
