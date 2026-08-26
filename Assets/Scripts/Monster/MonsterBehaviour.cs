using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Sirenix.OdinInspector;

internal sealed class MonsterMovementSnapshot
{
    private readonly ReadOnlyCollection<GridNodeBehaviour> currentRoute;

    internal MonsterMovementSnapshot(
        GridNodeBehaviour reachedNode,
        GridNodeBehaviour activeNextNode,
        GridNodeBehaviour targetNode,
        IReadOnlyList<GridNodeBehaviour> currentRoute,
        int routeIndex,
        Vector3 worldPosition,
        float activeSegmentProgress,
        float remainingCenterlineDistance,
        bool hasComparableRemainingDistance,
        bool hasComparableWorldPosition,
        bool isValid)
    {
        ReachedNode = reachedNode;
        ActiveNextNode = activeNextNode;
        TargetNode = targetNode;
        RouteIndex = routeIndex;
        WorldPosition = worldPosition;
        ActiveSegmentProgress = activeSegmentProgress;
        RemainingCenterlineDistance = remainingCenterlineDistance;
        HasComparableRemainingDistance = hasComparableRemainingDistance;
        HasComparableWorldPosition = hasComparableWorldPosition;
        IsValid = isValid;

        GridNodeBehaviour[] routeCopy = currentRoute != null
            ? new GridNodeBehaviour[currentRoute.Count]
            : Array.Empty<GridNodeBehaviour>();

        for (int i = 0; i < routeCopy.Length; i++)
        {
            routeCopy[i] = currentRoute[i];
        }

        this.currentRoute = Array.AsReadOnly(routeCopy);
    }

    internal GridNodeBehaviour ReachedNode { get; }
    internal GridNodeBehaviour ActiveNextNode { get; }
    internal GridNodeBehaviour TargetNode { get; }
    internal IReadOnlyList<GridNodeBehaviour> CurrentRoute => currentRoute;
    internal int RouteIndex { get; }
    internal Vector3 WorldPosition { get; }
    internal float ActiveSegmentProgress { get; }
    internal float RemainingCenterlineDistance { get; }
    internal bool HasComparableRemainingDistance { get; }
    internal bool HasComparableWorldPosition { get; }
    internal bool IsValid { get; }
}

public class MonsterBehaviour : MonoBehaviour
{
    private const float MaximumLaneOffsetNodeFraction = 0.25f;

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
    [TitleGroup("Movement Lane")]
    [Tooltip(
        "Symmetric maximum Map-local XZ offset. Runtime targets resolve " +
        "within [-X,+X] and [-Y,+Y].")]
    [SerializeField] private Vector2 maximumLaneOffset =
        new Vector2(0.18f, 0.18f);

    [ShowInInspector, ReadOnly] private GridNodeBehaviour currentNode;
    [ShowInInspector, ReadOnly] private GridNodeBehaviour targetNode;
    [ShowInInspector, ReadOnly] private bool isMoving;
    [ShowInInspector, ReadOnly] private bool isDead;
    [ShowInInspector, ReadOnly] private bool isResolved;
    [ShowInInspector, ReadOnly] private bool isCleaningUp;
    [TitleGroup("Movement Lane")]
    [ShowInInspector, ReadOnly] private int laneIdentity;

    private readonly List<GridNodeBehaviour> currentPath = new List<GridNodeBehaviour>();
    private MonsterBuffRuntime buffRuntime;
    private int pathIndex;
    private int towerOwnedHitTransactionDepth;

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
    public GridNodeBehaviour ReachedNode => currentNode;
    public GridNodeBehaviour ActiveNextNode => GetActiveNextNode();
    public GridNodeBehaviour TargetNode => targetNode;
    public IReadOnlyList<GridNodeBehaviour> CurrentPath => currentPath;
    public IReadOnlyList<GridNodeBehaviour> CurrentRoute => currentPath;
    public int PathIndex => pathIndex;
    public int RouteIndex => pathIndex;
    public bool IsMoving => isMoving;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public bool IsMovementLocked => isMovementLocked;
    public int LaneIdentity => laneIdentity;
    public Vector2 MaximumLaneOffset => maximumLaneOffset;
    public bool IsGameplayTargetable =>
        isActiveAndEnabled &&
        !isResolved &&
        !isCleaningUp &&
        currentHealth > 0;
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
    public event Action<MonsterBehaviour, ElementalHitReactionObservation>
        OnElementalHitReactionObserved;

    public bool TryInitializeRuntime(out string failureReason)
    {
        if (!TryValidateAuthoredConfiguration(out failureReason))
        {
            return false;
        }

        laneIdentity = GetInstanceID();
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

        if (!IsFinite(maximumLaneOffset) ||
            maximumLaneOffset.x < 0f ||
            maximumLaneOffset.y < 0f)
        {
            failureReason =
                "Maximum Lane Offset must contain finite, non-negative XZ values.";
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

    public void SetPath(IReadOnlyList<GridNodeBehaviour> path)
    {
        if (isResolved || isCleaningUp)
        {
            return;
        }

        if (path == null || path.Count == 0)
        {
            ClearRouteAndStopMovement();
            return;
        }

        GridNodeBehaviour reachedNode = currentNode;

        if (reachedNode == null)
        {
            reachedNode = path[0];
        }

        if (!TryBuildCompleteRoute(
                path,
                reachedNode,
                out List<GridNodeBehaviour> completeRoute))
        {
            ClearRouteAndStopMovement();
            return;
        }

        currentPath.Clear();
        currentPath.AddRange(completeRoute);
        currentNode = reachedNode;

        if (currentPath.Count == 1)
        {
            if (currentNode == targetNode)
            {
                HandleTargetReached();
                return;
            }

            ClearRouteAndStopMovement();
            return;
        }

        pathIndex = 1;
        isMoving = true;
        RefreshMovementAnimation();
    }

    internal MonsterMovementSnapshot CaptureMovementSnapshot(
        MapGeneratorBehaviour activeMap)
    {
        GridNodeBehaviour activeNextNode = GetActiveNextNode();
        bool hasValidRouteInvariant = HasValidActiveRouteInvariant(activeNextNode);
        float activeSegmentProgress = 0f;
        float remainingCenterlineDistance = 0f;
        bool hasComparableRemainingDistance =
            hasValidRouteInvariant &&
            TryCalculateRemainingCenterlineDistance(
                activeMap,
                out activeSegmentProgress,
                out remainingCenterlineDistance);

        Vector3 worldPosition = transform.position;
        bool hasComparableWorldPosition = IsFinite(worldPosition);

        return new MonsterMovementSnapshot(
            currentNode,
            activeNextNode,
            targetNode,
            currentPath,
            pathIndex,
            worldPosition,
            activeSegmentProgress,
            remainingCenterlineDistance,
            hasComparableRemainingDistance,
            hasComparableWorldPosition,
            hasValidRouteInvariant);
    }

    internal void ApplyPreparedMovementRevision(
        MonsterRouteRevisionEntry revisionEntry)
    {
        transform.position = revisionEntry.ProjectedWorldPosition;
        currentNode = revisionEntry.ReachedNode;
        targetNode = revisionEntry.TargetNode;
        currentPath.Clear();

        for (int i = 0; i < revisionEntry.CurrentRoute.Count; i++)
        {
            currentPath.Add(revisionEntry.CurrentRoute[i]);
        }

        pathIndex = revisionEntry.RouteIndex;
        isMoving = true;
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

        if (currentHealth <= 0 && towerOwnedHitTransactionDepth == 0)
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
        return ApplyBuffWithOutcome(request, deferOverload: false);
    }

    internal BuffApplyOutcome ApplyBuffWithOutcome(
        BuffApplyRequest request,
        bool deferOverload)
    {
        if (isResolved || isCleaningUp)
        {
            return new BuffApplyOutcome(
                BuffApplyResult.Invalid,
                null,
                false,
                false,
                0,
                0,
                0);
        }

        EnsureBuffRuntime();
        return buffRuntime.ApplyBuffWithOutcome(request, deferOverload);
    }

    internal void BeginTowerOwnedHitTransaction()
    {
        EnsureBuffRuntime();
        towerOwnedHitTransactionDepth++;
        buffRuntime.BeginExternalMutation();
    }

    internal void ResolveElementalHitReactions(
        TowerInstance triggeringTower,
        TowerDamageSourceIdentity damageSourceIdentity,
        ElementalOpportunityDiagnosticContext diagnostics)
    {
        EnsureBuffRuntime();
        buffRuntime.ResolveElementalHitReactions(
            triggeringTower,
            damageSourceIdentity,
            diagnostics);
    }

    internal void EndTowerOwnedHitTransaction()
    {
        if (towerOwnedHitTransactionDepth <= 0)
        {
            return;
        }

        towerOwnedHitTransactionDepth--;

        if (towerOwnedHitTransactionDepth == 0 &&
            currentHealth <= 0 &&
            !isResolved &&
            !isCleaningUp)
        {
            Die();
        }

        buffRuntime.EndExternalMutation();
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
            buffRuntime.OnElementalHitReactionObserved -=
                HandleElementalHitReactionObserved;
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
            buffRuntime.OnElementalHitReactionObserved +=
                HandleElementalHitReactionObserved;
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

    private void HandleElementalHitReactionObserved(
        ElementalHitReactionObservation observation)
    {
        Action<MonsterBehaviour, ElementalHitReactionObservation> handlers =
            OnElementalHitReactionObserved;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<MonsterBehaviour, ElementalHitReactionObservation>)
                    invocationList[i]).Invoke(this, observation);
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

        Vector3 targetPosition = ResolveMovementTargetPosition(nextNode);

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

    private bool TryBuildCompleteRoute(
        IReadOnlyList<GridNodeBehaviour> path,
        GridNodeBehaviour reachedNode,
        out List<GridNodeBehaviour> completeRoute)
    {
        completeRoute = new List<GridNodeBehaviour>();

        if (reachedNode == null || targetNode == null)
        {
            return false;
        }

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] == null)
            {
                return false;
            }
        }

        if (path[path.Count - 1] != targetNode)
        {
            return false;
        }

        GridNodeBehaviour activeNextNode = GetActiveNextNode();

        if (path[0] == reachedNode)
        {
            for (int i = 0; i < path.Count; i++)
            {
                completeRoute.Add(path[i]);
            }

            return true;
        }

        if (activeNextNode == null || path[0] != activeNextNode)
        {
            return false;
        }

        completeRoute.Add(reachedNode);

        for (int i = 0; i < path.Count; i++)
        {
            completeRoute.Add(path[i]);
        }

        return true;
    }

    private GridNodeBehaviour GetActiveNextNode()
    {
        return isMoving && pathIndex >= 0 && pathIndex < currentPath.Count
            ? currentPath[pathIndex]
            : null;
    }

    private bool HasValidActiveRouteInvariant(
        GridNodeBehaviour activeNextNode)
    {
        if (!isMoving ||
            currentNode == null ||
            activeNextNode == null ||
            targetNode == null ||
            pathIndex <= 0 ||
            pathIndex >= currentPath.Count ||
            currentPath[pathIndex] != activeNextNode ||
            currentPath[pathIndex - 1] != currentNode ||
            currentPath[currentPath.Count - 1] != targetNode)
        {
            return false;
        }

        for (int i = 0; i < currentPath.Count; i++)
        {
            if (currentPath[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryCalculateRemainingCenterlineDistance(
        MapGeneratorBehaviour activeMap,
        out float activeSegmentProgress,
        out float remainingCenterlineDistance)
    {
        activeSegmentProgress = 0f;
        remainingCenterlineDistance = 0f;

        if (activeMap == null || activeMap.NodesRoot == null)
        {
            return false;
        }

        Vector3 reachedLocalPosition =
            activeMap.NodesRoot.InverseTransformPoint(currentNode.WorldPosition);
        Vector3 nextLocalPosition =
            activeMap.NodesRoot.InverseTransformPoint(currentPath[pathIndex].WorldPosition);
        Vector3 monsterLocalPosition =
            activeMap.NodesRoot.InverseTransformPoint(transform.position);

        if (!IsFinite(reachedLocalPosition) ||
            !IsFinite(nextLocalPosition) ||
            !IsFinite(monsterLocalPosition))
        {
            return false;
        }

        Vector2 reachedCenter =
            new Vector2(reachedLocalPosition.x, reachedLocalPosition.z);
        Vector2 nextCenter =
            new Vector2(nextLocalPosition.x, nextLocalPosition.z);
        Vector2 monsterPosition =
            new Vector2(monsterLocalPosition.x, monsterLocalPosition.z);
        Vector2 activeSegment = nextCenter - reachedCenter;
        float activeSegmentLengthSquared = activeSegment.sqrMagnitude;

        if (activeSegmentLengthSquared <= Mathf.Epsilon)
        {
            return false;
        }

        activeSegmentProgress = Mathf.Clamp01(
            Vector2.Dot(monsterPosition - reachedCenter, activeSegment) /
            activeSegmentLengthSquared);

        float activeSegmentLength = Mathf.Sqrt(activeSegmentLengthSquared);
        remainingCenterlineDistance =
            activeSegmentLength * (1f - activeSegmentProgress);

        for (int i = pathIndex; i < currentPath.Count - 1; i++)
        {
            Vector3 fromLocalPosition =
                activeMap.NodesRoot.InverseTransformPoint(currentPath[i].WorldPosition);
            Vector3 toLocalPosition =
                activeMap.NodesRoot.InverseTransformPoint(currentPath[i + 1].WorldPosition);

            if (!IsFinite(fromLocalPosition) || !IsFinite(toLocalPosition))
            {
                activeSegmentProgress = 0f;
                remainingCenterlineDistance = 0f;
                return false;
            }

            Vector2 fromCenter =
                new Vector2(fromLocalPosition.x, fromLocalPosition.z);
            Vector2 toCenter =
                new Vector2(toLocalPosition.x, toLocalPosition.z);
            remainingCenterlineDistance += Vector2.Distance(fromCenter, toCenter);
        }

        return !float.IsNaN(remainingCenterlineDistance) &&
               !float.IsInfinity(remainingCenterlineDistance);
    }

    internal Vector3 ResolveMovementTargetPosition(
        GridNodeBehaviour destinationNode)
    {
        Vector3 targetPosition = destinationNode.WorldPosition;
        targetPosition.y = transform.position.y;

        if (destinationNode == targetNode ||
            destinationNode.NodeType == GridNodeType.Spawn ||
            destinationNode.NodeType == GridNodeType.Target)
        {
            return targetPosition;
        }

        MapGeneratorBehaviour activeMap =
            monsterManager != null ? monsterManager.ActiveMap : null;

        if (activeMap == null ||
            activeMap.NodesRoot == null ||
            activeMap.NodeSize <= 0f)
        {
            return targetPosition;
        }

        float safeMaximum =
            activeMap.NodeSize * MaximumLaneOffsetNodeFraction;
        Vector2 safeLaneMaximum = new Vector2(
            Mathf.Min(maximumLaneOffset.x, safeMaximum),
            Mathf.Min(maximumLaneOffset.y, safeMaximum));
        Vector2 laneOffset = ResolveDeterministicLaneOffset(
            laneIdentity,
            destinationNode.GridPosition,
            safeLaneMaximum);
        Vector3 destinationLocalPosition =
            activeMap.NodesRoot.InverseTransformPoint(
                destinationNode.WorldPosition);
        destinationLocalPosition.x += laneOffset.x;
        destinationLocalPosition.z += laneOffset.y;
        targetPosition =
            activeMap.NodesRoot.TransformPoint(destinationLocalPosition);
        targetPosition.y = transform.position.y;
        return targetPosition;
    }

    private static Vector2 ResolveDeterministicLaneOffset(
        int instanceLaneIdentity,
        Vector2Int destinationGridPosition,
        Vector2 safeMaximum)
    {
        uint hash = 2166136261u;

        unchecked
        {
            hash = (hash ^ (uint)instanceLaneIdentity) * 16777619u;
            hash = (hash ^ (uint)destinationGridPosition.x) * 16777619u;
            hash = (hash ^ (uint)destinationGridPosition.y) * 16777619u;
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            hash ^= hash >> 16;
        }

        float normalizedX = (hash & 0xFFFFu) / 65535f;
        float normalizedZ = ((hash >> 16) & 0xFFFFu) / 65535f;
        return new Vector2(
            (normalizedX * 2f - 1f) * safeMaximum.x,
            (normalizedZ * 2f - 1f) * safeMaximum.y);
    }

    private void ClearRouteAndStopMovement()
    {
        currentPath.Clear();
        StopMovement();
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) &&
               !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) &&
               !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) &&
               !float.IsInfinity(value.z);
    }

    private static bool IsFinite(Vector2 value)
    {
        return !float.IsNaN(value.x) &&
               !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) &&
               !float.IsInfinity(value.y);
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
