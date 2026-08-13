using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MonsterBehaviour : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private string walkingBoolParameterName = "IsWalking";
    [SerializeField] private string dieTriggerName;
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private Vector3 statusUiOffset =
        new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector3 damageNumberOffset =
        new Vector3(0f, 1.5f, 0f);
    private MonsterManager monsterManager;
    private DamageNumberManager damageNumberManager;
    [ShowInInspector, ReadOnly] private int currentHealth;
    [ShowInInspector, ReadOnly] private float currentMoveSpeed;
    [TitleGroup("Movement Control")]
    [ShowInInspector, ReadOnly] private float moveSpeedMultiplier = 1f;
    [TitleGroup("Movement Control")]
    [ShowInInspector, ReadOnly] private bool isMovementLocked;
    [SerializeField] private Animator animator;
    [SerializeField] private MonsterHitFeedback hitFeedback;
    [SerializeField] private MonsterBuffVisualController buffVisualController;
    [SerializeField] private Transform hitAnchor;
    [SerializeField] private float arriveDistanceThreshold = 0.05f;

    [ShowInInspector, ReadOnly] private GridNodeBehaviour currentNode;
    [ShowInInspector, ReadOnly] private GridNodeBehaviour targetNode;
    [ShowInInspector, ReadOnly] private bool isMoving;
    [ShowInInspector, ReadOnly] private bool isDead;
    [ShowInInspector, ReadOnly] private bool isResolved;
    [ShowInInspector, ReadOnly] private bool isCleaningUp;

    private readonly List<GridNodeBehaviour> currentPath = new List<GridNodeBehaviour>();
    private MonsterBuffRuntime buffRuntime;
    private int pathIndex;

    private static readonly IReadOnlyList<MonsterBuffStateSnapshot> EmptyBuffSnapshots = Array.Empty<MonsterBuffStateSnapshot>();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float CurrentMoveSpeed => currentMoveSpeed;
    public Vector3 StatusUiOffset => statusUiOffset;
    public Vector3 DamageNumberOffset => damageNumberOffset;
    public Animator Animator => animator;
    public Transform HitAnchor => hitAnchor != null ? hitAnchor : transform;
    public GridNodeBehaviour CurrentNode => currentNode;
    public GridNodeBehaviour TargetNode => targetNode;
    public IReadOnlyList<GridNodeBehaviour> CurrentPath => currentPath;
    public int PathIndex => pathIndex;
    public bool IsMoving => isMoving;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public bool IsMovementLocked => isMovementLocked;
    public bool IsGameplayTargetable =>
        isActiveAndEnabled && !isResolved && !isCleaningUp;
    [TitleGroup("Buff Runtime")]
    [ShowInInspector, ReadOnly]
    public IReadOnlyList<MonsterBuffStateSnapshot> ActiveBuffSnapshots => buffRuntime != null ? buffRuntime.ActiveSnapshots : EmptyBuffSnapshots;

    public event Action<MonsterBehaviour> OnTargetReached;
    public event Action<MonsterBehaviour> OnDied;
    public event Action<MonsterBehaviour, bool> OnResolved;
    public event Action<MonsterBehaviour> OnDestroyed;
    public event Action<MonsterBehaviour, int, int> OnHealthChanged;
    public event Action<MonsterBehaviour> OnBuffStateChanged;
    public event Action<MonsterBehaviour, BuffRuntimeObservation> OnBuffRuntimeObserved;

    public bool TryInitializeRuntime(out string failureReason)
    {
        if (!TryValidateAuthoredConfiguration(out failureReason))
        {
            return false;
        }

        currentHealth = maxHealth;
        ClearMovementControls();
        EnsureBuffRuntime();
        buffRuntime.Clear(BuffRemovalReason.RuntimeReset);
        CacheBuffVisualController();
        isDead = false;
        isResolved = false;
        isCleaningUp = false;
        CacheAnimator();
        RefreshEffectiveMoveSpeed();
        CacheHitFeedback();

        if (!hitFeedback.TryInitialize(out failureReason))
        {
            return false;
        }

        NotifyHealthChanged();
        failureReason = string.Empty;
        return true;
    }

    public bool TryValidateAuthoredConfiguration(out string failureReason)
    {
        if (maxHealth <= 0)
        {
            failureReason =
                $"Maximum Health must be positive; found {maxHealth}.";
            return false;
        }

        if (moveSpeed < 0f)
        {
            failureReason =
                $"Move Speed cannot be negative; found {moveSpeed}.";
            return false;
        }

        if (deathDelay < 0f)
        {
            failureReason =
                $"Death Delay cannot be negative; found {deathDelay}.";
            return false;
        }

        Animator resolvedAnimator =
            animator != null ? animator : GetComponentInChildren<Animator>(true);

        if (resolvedAnimator == null)
        {
            failureReason = "an Animator is required.";
            return false;
        }

        MonsterHitFeedback resolvedHitFeedback =
            hitFeedback != null
                ? hitFeedback
                : GetComponentInChildren<MonsterHitFeedback>(true);

        if (resolvedHitFeedback == null)
        {
            failureReason = "MonsterHitFeedback is required.";
            return false;
        }

        if (!resolvedHitFeedback.TryValidateAuthoredConfiguration(
                out failureReason))
        {
            failureReason = $"MonsterHitFeedback is invalid: {failureReason}";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void SetRuntimeReferences(
        MonsterManager monsterManager,
        DamageNumberManager damageNumberManager = null)
    {
        this.monsterManager = monsterManager;
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
        if (isResolved || isCleaningUp)
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
        RefreshMovementAnimation();
    }

    public void StopMovement()
    {
        isMoving = false;
        pathIndex = 0;
        RefreshMovementAnimation();
    }

    public bool SetMoveSpeedMultiplier(float multiplier)
    {
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f || multiplier >= 1f)
        {
            Debug.LogWarning("Monster move speed multiplier must be greater than zero and less than one in the current reduction-only slot.", this);
            return false;
        }

        moveSpeedMultiplier = multiplier;
        RefreshEffectiveMoveSpeed();
        return true;
    }

    public void ClearMoveSpeedMultiplier()
    {
        moveSpeedMultiplier = 1f;
        RefreshEffectiveMoveSpeed();
    }

    public void SetMovementLock(bool isLocked)
    {
        isMovementLocked = isLocked;
        RefreshEffectiveMoveSpeed();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isResolved || isCleaningUp)
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
        TryResolve(reachedTarget: false);
    }

    public bool IsDead()
    {
        return isDead;
    }

    public BuffApplyResult ApplyBuff(BuffApplyRequest request)
    {
        return ApplyBuffWithOutcome(request).Result;
    }

    public BuffApplyOutcome ApplyBuffWithOutcome(BuffApplyRequest request)
    {
        if (isResolved || isCleaningUp)
        {
            return new BuffApplyOutcome(BuffApplyResult.Invalid, null, false, false);
        }

        EnsureBuffRuntime();
        return buffRuntime.ApplyBuffWithOutcome(request);
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

    private void Awake()
    {
        CacheAnimator();
        EnsureBuffRuntime();
        CacheBuffVisualController();
    }

    private void Update()
    {
        if (isResolved || isCleaningUp)
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
        monsterManager?.UnregisterMonster(this);
        buffRuntime?.Clear(BuffRemovalReason.TechnicalCleanup);
        ClearMovementControls();
        if (buffRuntime != null)
        {
            buffRuntime.OnStateChanged -= HandleBuffStateChanged;
            buffRuntime.OnRuntimeObserved -= HandleBuffRuntimeObserved;
        }
        OnDestroyed?.Invoke(this);
    }

    private void EnsureBuffRuntime()
    {
        if (buffRuntime == null)
        {
            buffRuntime = new MonsterBuffRuntime(this);
            buffRuntime.OnStateChanged += HandleBuffStateChanged;
            buffRuntime.OnRuntimeObserved += HandleBuffRuntimeObserved;
        }
    }

    private void CacheBuffVisualController()
    {
        if (buffVisualController == null)
        {
            buffVisualController = GetComponent<MonsterBuffVisualController>();
        }

        if (buffVisualController == null)
        {
            buffVisualController = gameObject.AddComponent<MonsterBuffVisualController>();
        }

        buffVisualController.Initialize(this);
    }

    private void HandleBuffStateChanged()
    {
        OnBuffStateChanged?.Invoke(this);
    }

    private void HandleBuffRuntimeObserved(BuffRuntimeObservation observation)
    {
        Action<MonsterBehaviour, BuffRuntimeObservation> handlers =
            OnBuffRuntimeObserved;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<MonsterBehaviour, BuffRuntimeObservation>)invocationList[i])
                    .Invoke(this, observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
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
            hitFeedback = GetComponentInChildren<MonsterHitFeedback>(true);
        }
    }

    private void ClearMovementControls()
    {
        moveSpeedMultiplier = 1f;
        isMovementLocked = false;
        RefreshEffectiveMoveSpeed();
    }

    private void RefreshEffectiveMoveSpeed()
    {
        float baseMoveSpeed = Mathf.Max(0f, moveSpeed);
        currentMoveSpeed = isMovementLocked ? 0f : baseMoveSpeed * moveSpeedMultiplier;
        RefreshMovementAnimation();
    }

    private void RefreshMovementAnimation()
    {
        SetWalkingAnimation(isMoving && !isMovementLocked);
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
        TryResolve(reachedTarget: true);
    }

    private void SetWalkingAnimation(bool walking)
    {
        if (animator == null || string.IsNullOrEmpty(walkingBoolParameterName))
        {
            return;
        }

        animator.SetBool(walkingBoolParameterName, walking);
    }

    private void PlayDeathAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(dieTriggerName))
        {
            return;
        }

        animator.SetTrigger(dieTriggerName);
    }

    public void ForceCleanup()
    {
        if (isCleaningUp)
        {
            return;
        }

        isCleaningUp = true;
        isResolved = true;
        StopGameplayState(BuffRemovalReason.TechnicalCleanup);
        monsterManager?.UnregisterMonster(this);
        Destroy(gameObject);
    }

    private void TryResolve(bool reachedTarget)
    {
        if (isResolved || isCleaningUp)
        {
            return;
        }

        isResolved = true;
        isDead = !reachedTarget;
        StopGameplayState(
            reachedTarget
                ? BuffRemovalReason.MonsterLeaked
                : BuffRemovalReason.MonsterKilled);

        OnResolved?.Invoke(this, reachedTarget);

        if (reachedTarget)
        {
            OnTargetReached?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        OnDied?.Invoke(this);
        PlayDeathAnimation();
        Destroy(gameObject, GetDeathDelay());
    }

    private void StopGameplayState(BuffRemovalReason buffRemovalReason)
    {
        buffRuntime?.Clear(buffRemovalReason);
        StopMovement();
        ClearMovementControls();
        currentPath.Clear();
        hitFeedback?.StopFeedback();
    }

    private float GetDeathDelay()
    {
        return Mathf.Max(0f, deathDelay);
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);
    }

    private void ShowDamageNumber(int damage)
    {
        if (damageNumberManager == null)
        {
            Debug.LogWarning("Monster behaviour cannot show damage number: damage number manager is not assigned.", this);
            return;
        }

        Vector3 damageNumberPosition = transform.position + damageNumberOffset;
        damageNumberManager.ShowDamage(damage, damageNumberPosition);
    }
}
