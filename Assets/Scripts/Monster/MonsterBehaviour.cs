using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MonsterBehaviour : MonoBehaviour
{
    [SerializeField] private MonsterDefinition definition;
    private MonsterManager monsterManager;
    private PlayerSystem playerSystem;
    private DamageNumberManager damageNumberManager;
    [ShowInInspector, ReadOnly] private int currentHealth;
    [ShowInInspector, ReadOnly] private float currentMoveSpeed;
    [SerializeField] private Animator animator;
    [SerializeField] private MonsterHitFeedback hitFeedback;
    [SerializeField] private Transform hitAnchor;
    [SerializeField] private float arriveDistanceThreshold = 0.05f;

    [ShowInInspector, ReadOnly] private GridNodeBehaviour currentNode;
    [ShowInInspector, ReadOnly] private GridNodeBehaviour targetNode;
    [ShowInInspector, ReadOnly] private bool isMoving;
    [ShowInInspector, ReadOnly] private bool isDead;
    [ShowInInspector, ReadOnly] private bool isCleaningUp;

    private readonly List<GridNodeBehaviour> currentPath = new List<GridNodeBehaviour>();
    private MonsterBuffRuntime buffRuntime;
    private int pathIndex;

    public MonsterDefinition Definition => definition;
    public int CurrentHealth => currentHealth;
    public float CurrentMoveSpeed => currentMoveSpeed;
    public Animator Animator => animator;
    public Transform HitAnchor => hitAnchor != null ? hitAnchor : transform;
    public GridNodeBehaviour CurrentNode => currentNode;
    public GridNodeBehaviour TargetNode => targetNode;
    public IReadOnlyList<GridNodeBehaviour> CurrentPath => currentPath;
    public int PathIndex => pathIndex;
    public bool IsMoving => isMoving;

    public event Action<MonsterBehaviour> OnTargetReached;
    public event Action<MonsterBehaviour> OnDied;
    public event Action<MonsterBehaviour> OnDestroyed;
    public event Action<MonsterBehaviour, int, int> OnHealthChanged;

    public void Initialize(MonsterDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("Monster behaviour cannot initialize: monster definition is null.", this);
            return;
        }

        this.definition = definition;
        currentHealth = definition.MaxHealth;
        currentMoveSpeed = definition.MoveSpeed;
        EnsureBuffRuntime();
        buffRuntime.Clear();
        isDead = false;
        isCleaningUp = false;
        CacheAnimator();
        CacheHitFeedback();
        hitFeedback?.Initialize(definition);
        NotifyHealthChanged();
    }

    public void SetRuntimeReferences(
        MonsterManager monsterManager,
        PlayerSystem playerSystem,
        DamageNumberManager damageNumberManager = null)
    {
        this.monsterManager = monsterManager;
        this.playerSystem = playerSystem;
        this.damageNumberManager = damageNumberManager;
    }

    public void SetCurrentNode(GridNodeBehaviour currentNode)
    {
        this.currentNode = currentNode;
    }

    public void SetTargetNode(GridNodeBehaviour targetNode)
    {
        this.targetNode = targetNode;
    }

    public GridNodeBehaviour GetCurrentNode()
    {
        return currentNode;
    }

    public GridNodeBehaviour GetTargetNode()
    {
        return targetNode;
    }

    public void SetPath(List<GridNodeBehaviour> path)
    {
        if (isDead || isCleaningUp)
        {
            return;
        }

        currentPath.Clear();

        if (path == null || path.Count == 0)
        {
            StopMovement();
            return;
        }

        for (int i = 0; i < path.Count; i++)
        {
            GridNodeBehaviour pathNode = path[i];

            if (pathNode != null)
            {
                currentPath.Add(pathNode);
            }
        }

        if (currentPath.Count == 0)
        {
            StopMovement();
            return;
        }

        if (currentNode == null)
        {
            currentNode = currentPath[0];
        }

        pathIndex = currentPath[0] == currentNode && currentPath.Count > 1 ? 1 : 0;

        if (currentPath.Count == 1 && currentPath[0] == targetNode)
        {
            HandleTargetReached();
            return;
        }

        isMoving = pathIndex < currentPath.Count;
        SetWalkingAnimation(isMoving);
    }

    public void StopMovement()
    {
        isMoving = false;
        pathIndex = 0;
        SetWalkingAnimation(false);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || isCleaningUp)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        NotifyHealthChanged();
        hitFeedback?.PlayHitFeedback();
        ShowDamageNumber(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead || isCleaningUp)
        {
            return;
        }

        isDead = true;
        buffRuntime?.Clear();
        StopMovement();
        currentPath.Clear();
        hitFeedback?.StopFeedback();
        OnDied?.Invoke(this);

        if (monsterManager != null)
        {
            monsterManager.UnregisterMonster(this);
        }

        PlayDeathAnimation();
        RewardExp();
        Destroy(gameObject, GetDeathDelay());
    }

    public bool IsDead()
    {
        return isDead;
    }

    public BuffApplyResult ApplyBuff(BuffApplyRequest request)
    {
        if (isDead || isCleaningUp)
        {
            return BuffApplyResult.Invalid;
        }

        EnsureBuffRuntime();
        return buffRuntime.ApplyBuff(request);
    }

    public bool RemoveBuff(BuffDefinition buffDefinition)
    {
        EnsureBuffRuntime();
        return buffRuntime.RemoveBuff(buffDefinition);
    }

    public bool HasBuff(BuffDefinition buffDefinition)
    {
        EnsureBuffRuntime();
        return buffRuntime.HasBuff(buffDefinition);
    }

    public bool HasElementalStackImmunity(ElementType elementType)
    {
        EnsureBuffRuntime();
        return buffRuntime.HasElementalStackImmunity(elementType);
    }

    private void Awake()
    {
        CacheAnimator();
        EnsureBuffRuntime();
    }

    private void Update()
    {
        if (isDead || isCleaningUp)
        {
            return;
        }

        buffRuntime?.Tick(Time.deltaTime);

        if (isMoving)
        {
            MoveAlongPath();
        }
    }

    private void OnDestroy()
    {
        buffRuntime?.Clear();
        OnDestroyed?.Invoke(this);
    }

    private void EnsureBuffRuntime()
    {
        if (buffRuntime == null)
        {
            buffRuntime = new MonsterBuffRuntime(this);
        }
    }

    private void CacheAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void CacheHitFeedback()
    {
        if (hitFeedback == null)
        {
            hitFeedback = GetComponentInChildren<MonsterHitFeedback>();
        }

        if (hitFeedback == null)
        {
            hitFeedback = gameObject.AddComponent<MonsterHitFeedback>();
        }
    }

    private void MoveAlongPath()
    {
        if (pathIndex < 0 || pathIndex >= currentPath.Count)
        {
            StopMovement();
            return;
        }

        GridNodeBehaviour nextNode = currentPath[pathIndex];

        if (nextNode == null)
        {
            StopMovement();
            return;
        }

        Vector3 targetPosition = nextNode.WorldPosition;
        targetPosition.y = transform.position.y;

        Vector3 moveDirection = targetPosition - transform.position;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            currentMoveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) > arriveDistanceThreshold)
        {
            return;
        }

        transform.position = targetPosition;
        currentNode = nextNode;
        pathIndex++;

        if (currentNode == targetNode || pathIndex >= currentPath.Count) HandleTargetReached();
    }

    private void HandleTargetReached()
    {
        if (isDead || isCleaningUp)
        {
            return;
        }

        isCleaningUp = true;
        buffRuntime?.Clear();
        StopMovement();
        currentPath.Clear();
        hitFeedback?.StopFeedback();
        OnTargetReached?.Invoke(this);

        if (monsterManager != null)
        {
            monsterManager.UnregisterMonster(this);
        }

        Destroy(gameObject);
    }

    private void SetWalkingAnimation(bool walking)
    {
        if (animator == null || definition == null || string.IsNullOrEmpty(definition.WalkingBoolParameterName))
        {
            return;
        }

        animator.SetBool(definition.WalkingBoolParameterName, walking);
    }

    private void PlayDeathAnimation()
    {
        if (animator == null || definition == null || string.IsNullOrEmpty(definition.DieTriggerName))
        {
            return;
        }

        animator.SetTrigger(definition.DieTriggerName);
    }

    private void RewardExp()
    {
        if (playerSystem == null || definition == null)
        {
            return;
        }

        playerSystem.AddExp(definition.ExpReward);
    }

    private float GetDeathDelay()
    {
        return definition != null ? Mathf.Max(0f, definition.DeathDelay) : 0f;
    }

    private void NotifyHealthChanged()
    {
        if (definition == null)
        {
            return;
        }

        OnHealthChanged?.Invoke(this, currentHealth, definition.MaxHealth);
    }

    private void ShowDamageNumber(int damage)
    {
        if (definition == null)
        {
            return;
        }

        if (damageNumberManager == null)
        {
            Debug.LogWarning("Monster behaviour cannot show damage number: damage number manager is not assigned.", this);
            return;
        }

        Vector3 damageNumberPosition = transform.position + definition.DamageNumberOffset;
        damageNumberManager.ShowDamage(damage, damageNumberPosition);
    }
}
