using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public abstract class TowerCombatBehaviour : MonoBehaviour
{
    [TitleGroup("Core")]
    [MinValue(0f)]
    [SerializeField] private float attackRange = 1f;
    [TitleGroup("Core")]
    [MinValue(0f)]
    [FormerlySerializedAs("attackInterval")]
    [SerializeField] private float attackCycleDuration = 1f;
    [TitleGroup("Core")]
    [SerializeField] private TargetSelectionType targetSelectionType;
    [TitleGroup("Attack VFX")]
    [SerializeField] private GameObject attackReleaseVfxPrefab;

    private readonly List<MonsterBehaviour> detectedEnemies = new List<MonsterBehaviour>();
    private readonly HashSet<ProjectileBehaviour> activeProjectiles =
        new HashSet<ProjectileBehaviour>();
    private readonly HashSet<TowerBehaviourPackageType> missingBehaviourPackageWarnings =
        new HashSet<TowerBehaviourPackageType>();

    private TowerInstance towerInstance;
    private TowerInstance subscribedUpgradeTowerInstance;
    private MonsterManager monsterManager;
    private TowerBehaviour towerBehaviour;
    private TowerDefinition towerDefinition;
    private ResolvedTowerCombatStats cachedResolvedStats;
    private MonsterBehaviour currentTarget;
    private float attackCycleTimer;
    private TowerAttackState attackState = TowerAttackState.Idle;
    private bool hasExplicitInitialization;
    private bool hasCompletedSubtypeInitialization;
    private bool hasResolvedStatsCache;
    private bool isRuntimeSessionActive;
    private bool isBattleActive;
    private bool hasLoggedMissingAttackOrigin;

    public event Action<TowerCombatBehaviour, MonsterBehaviour> OnProjectileReleased;

    public abstract TowerFamily SupportedTowerFamily { get; }
    public MonsterBehaviour CurrentTarget => currentTarget;
    public IReadOnlyList<MonsterBehaviour> DetectedEnemies => detectedEnemies;
    public float AttackCycleTimer => attackCycleTimer;
    public TowerAttackState AttackState => attackState;
    public bool IsAttacking => attackState != TowerAttackState.Idle;
    public bool IsBattleActive => isBattleActive;
    public float BaseAttackRange => attackRange;
    public float BaseAttackCycleDuration => attackCycleDuration;
    public float CurrentResolvedAttackRange => ResolveCombatStats().AttackRange;

    protected TowerInstance TowerInstance => towerInstance;
    protected TowerDefinition TowerDefinition => towerDefinition;
    protected MonsterManager MonsterManager => monsterManager;
    protected TargetSelectionType TargetSelectionType => targetSelectionType;
    protected GameObject AttackReleaseVfxPrefab => attackReleaseVfxPrefab;
    protected bool IsWaitingForAnimationRelease =>
        attackState == TowerAttackState.WaitingForAnimationRelease;
    protected bool IsAttackCycleReady => attackCycleTimer <= 0f;

    public void Initialize(TowerInstance initializedTowerInstance, MonsterManager initializedMonsterManager)
    {
        DeactivateRuntimeSession(clearExplicitOwner: true);

        towerInstance = initializedTowerInstance;
        monsterManager = initializedMonsterManager;
        towerDefinition = towerInstance != null ? towerInstance.TowerDefinition : null;
        hasExplicitInitialization = towerInstance != null && towerDefinition != null;
        hasCompletedSubtypeInitialization = false;
        CacheOptionalReferences();

        attackCycleTimer = 0f;
        currentTarget = null;
        attackState = TowerAttackState.Idle;
        hasLoggedMissingAttackOrigin = false;
        missingBehaviourPackageWarnings.Clear();

        if (!isBattleActive || !isActiveAndEnabled)
        {
            hasResolvedStatsCache = false;
            return;
        }

        if (!TryValidateExplicitOwner() || !TryResolveMonsterManager())
        {
            hasResolvedStatsCache = false;
            return;
        }

        EstablishResolvedBaseline();
        OnCombatInitialized();
        hasCompletedSubtypeInitialization = true;
        isRuntimeSessionActive = true;
        SubscribeToRuntimeNotifications();
    }

    public void BeginBattle()
    {
        isBattleActive = true;
        TryRecoverRuntimeSession();
    }

    public void StopBattle()
    {
        if (!isBattleActive && !isRuntimeSessionActive)
        {
            return;
        }

        isBattleActive = false;
        DeactivateRuntimeSession(clearExplicitOwner: false);
    }

    public void OnAttackAnimationRelease()
    {
        if (!isBattleActive ||
            !isRuntimeSessionActive ||
            !TryValidateExplicitOwner() ||
            monsterManager == null ||
            !monsterManager.isActiveAndEnabled ||
            !IsWaitingForAnimationRelease)
        {
            return;
        }

        OnAnimationRelease();
    }

    public void OnTowerPresentationReplaced()
    {
        if (!isBattleActive ||
            !isRuntimeSessionActive ||
            !TryValidateExplicitOwner() ||
            !IsWaitingForAnimationRelease)
        {
            return;
        }

        if (!SetAttackAnimatorTrigger())
        {
            OnAnimationRelease();
        }
    }

    public bool IsAuthoredConfigurationValid()
    {
        if (attackRange < 0f)
        {
            Debug.LogWarning($"{GetType().Name} on '{name}' is invalid: attack range cannot be negative.", this);
            return false;
        }

        if (attackCycleDuration < 0f)
        {
            Debug.LogWarning(
                $"{GetType().Name} on '{name}' is invalid: attack cycle duration cannot be negative.",
                this);
            return false;
        }

        return IsSubtypeConfigurationValid();
    }

    protected void Awake()
    {
        CacheOptionalReferences();
    }

    protected void OnEnable()
    {
        if (isBattleActive)
        {
            TryRecoverRuntimeSession();
        }
    }

    protected void OnDisable()
    {
        DeactivateRuntimeSession(clearExplicitOwner: false);
    }

    protected void OnDestroy()
    {
        DeactivateRuntimeSession(clearExplicitOwner: true);
    }

    protected void Update()
    {
        if (!EnsureRuntimeSession())
        {
            return;
        }

        UpdateAttackCycle();
        OnOwnedRuntimeUpdate();

        if (!CanScheduleCombat())
        {
            return;
        }

        DetectEnemies();
        OnCombatUpdate();
    }

    protected abstract TowerCombatBaseStats CreateBaseStats();
    protected abstract bool IsSubtypeConfigurationValid();
    protected abstract void OnCombatUpdate();
    protected abstract void OnAnimationRelease();

    protected virtual void OnCombatInitialized()
    {
    }

    protected virtual void OnCombatEnabled()
    {
    }

    protected virtual void OnResolvedBaselineEstablished(ResolvedTowerCombatStats resolvedStats)
    {
    }

    protected virtual void OnOwnedRuntimeUpdate()
    {
    }

    protected virtual void OnCombatCleanup()
    {
    }

    protected virtual void OnResolvedStatsChanged(
        ResolvedTowerCombatStats previousStats,
        ResolvedTowerCombatStats currentStats,
        TowerUpgradeDefinition sourceUpgrade,
        bool isLevelChange)
    {
    }

    protected virtual void OnBehaviourPackageRecorded(TowerUpgradeDefinition upgradeDefinition)
    {
    }

    protected void SetCurrentTarget(MonsterBehaviour target)
    {
        currentTarget = target;
    }

    protected void SetWaitingForAnimationRelease()
    {
        attackState = TowerAttackState.WaitingForAnimationRelease;
    }

    protected void SetIdle()
    {
        attackState = TowerAttackState.Idle;
    }

    protected void StartAttackCycle()
    {
        StartAttackCycle(ResolveCombatStats().AttackCycleDuration);
    }

    protected void StartAttackCycle(float resolvedAttackCycleDuration)
    {
        attackCycleTimer = Mathf.Max(0f, resolvedAttackCycleDuration);
    }

    protected void ClearAttackCycle()
    {
        attackCycleTimer = 0f;
    }

    protected ResolvedTowerCombatStats ResolveCombatStats()
    {
        if (hasResolvedStatsCache)
        {
            return cachedResolvedStats;
        }

        return TowerRuntimeStatResolver.Resolve(towerInstance, CreateBaseStats());
    }

    protected MonsterBehaviour SelectTarget(ISet<MonsterBehaviour> excludedTargets = null)
    {
        if (detectedEnemies.Count == 0 ||
            (excludedTargets != null && excludedTargets.Count >= detectedEnemies.Count))
        {
            return null;
        }

        switch (targetSelectionType)
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

    protected bool IsInAttackRange(MonsterBehaviour monster)
    {
        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            return false;
        }

        return IsInRange(
            origin.position,
            GetMonsterHitPosition(monster),
            ResolveCombatStats().AttackRange);
    }

    protected bool IsRegisteredGameplayTarget(MonsterBehaviour monster)
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

    protected bool TryReleaseProjectile(
        ProjectileBehaviour projectilePrefab,
        Transform origin,
        Vector3 targetPosition,
        MonsterBehaviour target,
        int attackDamage,
        ProjectileFlightType flightType,
        float initialArcHeight,
        ProjectileRuntimeOptions runtimeOptions,
        ArcherProjectileReleaseIdentity archerReleaseIdentity = default,
        bool huntingOpportunityConsumed = false)
    {
        if (projectilePrefab == null || origin == null)
        {
            return false;
        }

        ProjectileBehaviour projectileBehaviour = Instantiate(
            projectilePrefab,
            origin.position,
            Quaternion.identity);

        projectileBehaviour.Initialize(
            towerInstance,
            monsterManager,
            projectilePrefab,
            target,
            targetPosition,
            attackDamage,
            flightType,
            initialArcHeight,
            runtimeOptions,
            inheritedBounceHitHistory: null,
            archerReleaseIdentity: archerReleaseIdentity,
            huntingOpportunityConsumed: huntingOpportunityConsumed);

        if (!projectileBehaviour.IsInitialized)
        {
            return false;
        }

        RegisterOwnedProjectile(projectileBehaviour);
        return true;
    }

    protected void RegisterOwnedProjectile(ProjectileBehaviour projectileBehaviour)
    {
        if (projectileBehaviour == null ||
            !projectileBehaviour.IsInitialized ||
            !activeProjectiles.Add(projectileBehaviour))
        {
            return;
        }

        projectileBehaviour.OnEnded += HandleOwnedProjectileEnded;
        projectileBehaviour.OnChildReleased += HandleOwnedProjectileChildReleased;
    }

    protected List<ProjectileBehaviour> GetOwnedProjectileSnapshot()
    {
        return new List<ProjectileBehaviour>(activeProjectiles);
    }

    protected void CollectTargetCandidates(
        List<MonsterBehaviour> results,
        Vector3 rangeOrigin,
        float attackRange)
    {
        results.Clear();

        if (!isRuntimeSessionActive || monsterManager == null)
        {
            return;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (IsValidTarget(monster) &&
                IsRegisteredGameplayTarget(monster) &&
                IsInRange(rangeOrigin, GetMonsterHitPosition(monster), attackRange))
            {
                results.Add(monster);
            }
        }
    }

    protected MonsterBehaviour SelectTargetFromCandidates(
        IReadOnlyList<MonsterBehaviour> candidates,
        Vector3 selectionOrigin,
        ISet<MonsterBehaviour> excludedTargets = null)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        List<MonsterBehaviour> availableCandidates = new List<MonsterBehaviour>();

        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterBehaviour candidate = candidates[i];

            if (candidate != null &&
                (excludedTargets == null || !excludedTargets.Contains(candidate)))
            {
                availableCandidates.Add(candidate);
            }
        }

        if (availableCandidates.Count == 0)
        {
            return null;
        }

        switch (targetSelectionType)
        {
            case TargetSelectionType.HighestHealth:
                return SelectCandidateByHealth(availableCandidates, selectHighest: true);
            case TargetSelectionType.LowestHealth:
                return SelectCandidateByHealth(availableCandidates, selectHighest: false);
            case TargetSelectionType.Random:
                return availableCandidates[Random.Range(0, availableCandidates.Count)];
            case TargetSelectionType.Nearest:
            default:
                return SelectNearestCandidate(availableCandidates, selectionOrigin);
        }
    }

    protected static bool UpgradeIncludesBasicStat(
        TowerUpgradeDefinition upgradeDefinition,
        TowerUpgradeBasicStatType statType)
    {
        if (upgradeDefinition == null ||
            upgradeDefinition.UpgradeLayer != TowerUpgradeLayer.Basic ||
            upgradeDefinition.BasicStatDeltas == null)
        {
            return false;
        }

        IReadOnlyList<TowerUpgradeStatDelta> deltas = upgradeDefinition.BasicStatDeltas;

        for (int i = 0; i < deltas.Count; i++)
        {
            TowerUpgradeStatDelta delta = deltas[i];

            if (delta != null && delta.StatType == statType)
            {
                return true;
            }
        }

        return false;
    }

    protected bool HasBehaviourPackage(TowerBehaviourPackageType packageType)
    {
        return towerInstance != null && towerInstance.HasBehaviourPackage(packageType);
    }

    protected bool TryGetBehaviourPackageUpgrade(
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
                this);
        }

        upgradeDefinition = null;
        return false;
    }

    protected Transform GetAttackOrigin()
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
            Debug.LogWarning(
                "Tower combat cannot resolve current active AttackOrigin from TowerVisualController.",
                this);
            hasLoggedMissingAttackOrigin = true;
        }

        return origin;
    }

    protected bool SetAttackAnimatorTrigger()
    {
        TowerModelPresentation presentation = GetTowerModelPresentation();
        return presentation != null && presentation.RequestAttackTrigger();
    }

    protected void PlayAttackReleaseVfx(Quaternion rotation)
    {
        if (attackReleaseVfxPrefab == null)
        {
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin != null)
        {
            Instantiate(attackReleaseVfxPrefab, origin.position, rotation);
        }
    }

    protected void RaiseProjectileReleased(MonsterBehaviour target)
    {
        OnProjectileReleased?.Invoke(this, target);
    }

    protected static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }

    protected static bool IsInRange(Vector3 origin, Vector3 targetPosition, float range)
    {
        if (range <= 0f)
        {
            return false;
        }

        float rangeSqr = range * range;
        return (targetPosition - origin).sqrMagnitude <= rangeSqr;
    }

    protected static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
    }

    private bool EnsureRuntimeSession()
    {
        if (!isBattleActive || !hasExplicitInitialization)
        {
            return false;
        }

        if (!isRuntimeSessionActive)
        {
            return TryRecoverRuntimeSession();
        }

        if (!TryValidateExplicitOwner() ||
            monsterManager == null ||
            !monsterManager.isActiveAndEnabled)
        {
            DeactivateRuntimeSession(clearExplicitOwner: false);
            return false;
        }

        return true;
    }

    private bool CanScheduleCombat()
    {
        return GetAttackOrigin() != null;
    }

    private void CacheOptionalReferences()
    {
        if (towerBehaviour == null)
        {
            towerBehaviour = GetComponent<TowerBehaviour>();
        }
    }

    private void UpdateAttackCycle()
    {
        if (attackCycleTimer > 0f)
        {
            attackCycleTimer = Mathf.Max(0f, attackCycleTimer - Time.deltaTime);
        }
    }

    private void DetectEnemies()
    {
        detectedEnemies.Clear();
        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (IsValidTarget(monster) && IsInAttackRange(monster))
            {
                detectedEnemies.Add(monster);
            }
        }
    }

    private MonsterBehaviour SelectNearestTarget(ISet<MonsterBehaviour> excludedTargets)
    {
        MonsterBehaviour selectedTarget = null;
        float bestDistanceSqr = float.MaxValue;
        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            return null;
        }

        for (int i = 0; i < detectedEnemies.Count; i++)
        {
            MonsterBehaviour monster = detectedEnemies[i];

            if (excludedTargets != null && excludedTargets.Contains(monster))
            {
                continue;
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

    private static MonsterBehaviour SelectNearestCandidate(
        IReadOnlyList<MonsterBehaviour> candidates,
        Vector3 selectionOrigin)
    {
        MonsterBehaviour selectedTarget = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterBehaviour candidate = candidates[i];
            float distanceSqr =
                (GetMonsterHitPosition(candidate) - selectionOrigin).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            selectedTarget = candidate;
            nearestDistanceSqr = distanceSqr;
        }

        return selectedTarget;
    }

    private static MonsterBehaviour SelectCandidateByHealth(
        IReadOnlyList<MonsterBehaviour> candidates,
        bool selectHighest)
    {
        MonsterBehaviour selectedTarget = candidates[0];

        for (int i = 1; i < candidates.Count; i++)
        {
            MonsterBehaviour candidate = candidates[i];
            bool isBetter = selectHighest
                ? candidate.CurrentHealth > selectedTarget.CurrentHealth
                : candidate.CurrentHealth < selectedTarget.CurrentHealth;

            if (isBetter)
            {
                selectedTarget = candidate;
            }
        }

        return selectedTarget;
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

    private bool TryValidateExplicitOwner()
    {
        return hasExplicitInitialization &&
               towerInstance != null &&
               towerDefinition != null &&
               towerInstance.TowerDefinition == towerDefinition &&
               towerDefinition.TowerFamily == SupportedTowerFamily;
    }

    private bool TryResolveMonsterManager()
    {
        if (monsterManager != null && monsterManager.isActiveAndEnabled)
        {
            return true;
        }

        monsterManager = FindFirstObjectByType<MonsterManager>();
        return monsterManager != null && monsterManager.isActiveAndEnabled;
    }

    private bool TryRecoverRuntimeSession()
    {
        if (!isBattleActive ||
            !isActiveAndEnabled ||
            isRuntimeSessionActive ||
            !TryValidateExplicitOwner() ||
            !TryResolveMonsterManager())
        {
            return false;
        }

        attackCycleTimer = 0f;
        currentTarget = null;
        attackState = TowerAttackState.Idle;
        hasLoggedMissingAttackOrigin = false;
        EstablishResolvedBaseline();

        if (hasCompletedSubtypeInitialization)
        {
            OnCombatEnabled();
        }
        else
        {
            OnCombatInitialized();
            hasCompletedSubtypeInitialization = true;
        }

        isRuntimeSessionActive = true;
        SubscribeToRuntimeNotifications();
        return true;
    }

    private void EstablishResolvedBaseline()
    {
        cachedResolvedStats = TowerRuntimeStatResolver.Resolve(towerInstance, CreateBaseStats());
        hasResolvedStatsCache = true;
        OnResolvedBaselineEstablished(cachedResolvedStats);
    }

    private void SubscribeToRuntimeNotifications()
    {
        if (!isRuntimeSessionActive || towerInstance == null)
        {
            return;
        }

        if (subscribedUpgradeTowerInstance == towerInstance)
        {
            return;
        }

        UnsubscribeFromRuntimeNotifications();
        subscribedUpgradeTowerInstance = towerInstance;
        subscribedUpgradeTowerInstance.OnUpgradeRecorded += HandleUpgradeRecorded;
        subscribedUpgradeTowerInstance.OnLevelChanged += HandleLevelChanged;
    }

    private void UnsubscribeFromRuntimeNotifications()
    {
        if (subscribedUpgradeTowerInstance != null)
        {
            subscribedUpgradeTowerInstance.OnUpgradeRecorded -= HandleUpgradeRecorded;
            subscribedUpgradeTowerInstance.OnLevelChanged -= HandleLevelChanged;
        }

        subscribedUpgradeTowerInstance = null;
    }

    private bool CanHandleNotification(TowerInstance sourceTower)
    {
        return isBattleActive &&
               isRuntimeSessionActive &&
               hasResolvedStatsCache &&
               sourceTower != null &&
               sourceTower == towerInstance &&
               sourceTower == subscribedUpgradeTowerInstance &&
               monsterManager != null &&
               monsterManager.isActiveAndEnabled &&
               TryValidateExplicitOwner();
    }

    private void HandleLevelChanged(
        TowerInstance sourceTower,
        int previousLevel,
        int currentLevel)
    {
        if (!CanHandleNotification(sourceTower) || previousLevel == currentLevel)
        {
            return;
        }

        RefreshResolvedStats(sourceUpgrade: null, isLevelChange: true);
    }

    private void HandleUpgradeRecorded(
        TowerInstance sourceTower,
        TowerUpgradeDefinition upgradeDefinition)
    {
        if (!CanHandleNotification(sourceTower) || upgradeDefinition == null)
        {
            return;
        }

        switch (upgradeDefinition.UpgradeLayer)
        {
            case TowerUpgradeLayer.Basic:
                RefreshResolvedStats(upgradeDefinition, isLevelChange: false);
                break;
            case TowerUpgradeLayer.Behaviour:
                OnBehaviourPackageRecorded(upgradeDefinition);
                break;
            case TowerUpgradeLayer.Elemental:
                break;
        }
    }

    private void RefreshResolvedStats(
        TowerUpgradeDefinition sourceUpgrade,
        bool isLevelChange)
    {
        ResolvedTowerCombatStats previousStats = cachedResolvedStats;
        ResolvedTowerCombatStats currentStats =
            TowerRuntimeStatResolver.Resolve(towerInstance, CreateBaseStats());
        cachedResolvedStats = currentStats;

        bool refreshAttackRange = isLevelChange
            ? !Mathf.Approximately(previousStats.AttackRange, currentStats.AttackRange)
            : UpgradeIncludesBasicStat(sourceUpgrade, TowerUpgradeBasicStatType.AttackRange);
        bool refreshAttackCycleDuration = isLevelChange
            ? !Mathf.Approximately(
                previousStats.AttackCycleDuration,
                currentStats.AttackCycleDuration)
            : UpgradeIncludesBasicStat(
                sourceUpgrade,
                TowerUpgradeBasicStatType.AttackCycleDuration);
        bool refreshDamage = isLevelChange
            ? previousStats.AttackDamage != currentStats.AttackDamage
            : UpgradeIncludesBasicStat(sourceUpgrade, TowerUpgradeBasicStatType.DamageBonus);

        if (refreshAttackCycleDuration)
        {
            RefreshAttackCycleRatio(
                previousStats.AttackCycleDuration,
                currentStats.AttackCycleDuration);
        }

        if (refreshAttackRange || refreshDamage)
        {
            List<ProjectileBehaviour> projectileSnapshot = GetOwnedProjectileSnapshot();

            for (int i = 0; i < projectileSnapshot.Count; i++)
            {
                ProjectileBehaviour projectile = projectileSnapshot[i];

                if (projectile == null)
                {
                    continue;
                }

                if (refreshDamage)
                {
                    projectile.TryRefreshDamage(currentStats.AttackDamage);
                }

                if (refreshAttackRange)
                {
                    projectile.TryRefreshTrackingRange(currentStats.AttackRange);
                }
            }
        }

        OnResolvedStatsChanged(
            previousStats,
            currentStats,
            sourceUpgrade,
            isLevelChange);
    }

    private void RefreshAttackCycleRatio(
        float previousCycleDuration,
        float currentCycleDuration)
    {
        if (attackCycleTimer <= 0f)
        {
            attackCycleTimer = 0f;
            return;
        }

        attackCycleTimer = previousCycleDuration > 0f
            ? Mathf.Max(
                0f,
                attackCycleTimer * currentCycleDuration / previousCycleDuration)
            : 0f;
    }

    private void DeactivateRuntimeSession(bool clearExplicitOwner)
    {
        UnsubscribeFromRuntimeNotifications();

        if (isRuntimeSessionActive || hasResolvedStatsCache)
        {
            CleanupOwnedCombatRuntime();
        }

        isRuntimeSessionActive = false;
        hasResolvedStatsCache = false;
        attackCycleTimer = 0f;
        currentTarget = null;
        attackState = TowerAttackState.Idle;

        if (!clearExplicitOwner)
        {
            return;
        }

        hasExplicitInitialization = false;
        hasCompletedSubtypeInitialization = false;
        towerInstance = null;
        towerDefinition = null;
        monsterManager = null;
    }

    private void CleanupOwnedCombatRuntime()
    {
        OnCombatCleanup();
        ForceCleanupOwnedProjectiles();
        currentTarget = null;
        attackState = TowerAttackState.Idle;
    }

    private void HandleOwnedProjectileChildReleased(ProjectileBehaviour childProjectile)
    {
        RegisterOwnedProjectile(childProjectile);
    }

    private void HandleOwnedProjectileEnded(ProjectileBehaviour projectileBehaviour)
    {
        if (projectileBehaviour == null)
        {
            return;
        }

        projectileBehaviour.OnEnded -= HandleOwnedProjectileEnded;
        projectileBehaviour.OnChildReleased -= HandleOwnedProjectileChildReleased;
        activeProjectiles.Remove(projectileBehaviour);
    }

    private void ForceCleanupOwnedProjectiles()
    {
        if (activeProjectiles.Count == 0)
        {
            return;
        }

        List<ProjectileBehaviour> snapshot =
            new List<ProjectileBehaviour>(activeProjectiles);

        for (int i = 0; i < snapshot.Count; i++)
        {
            ProjectileBehaviour projectileBehaviour = snapshot[i];

            if (projectileBehaviour == null)
            {
                continue;
            }

            projectileBehaviour.ForceCleanup();
            projectileBehaviour.OnEnded -= HandleOwnedProjectileEnded;
            projectileBehaviour.OnChildReleased -= HandleOwnedProjectileChildReleased;
        }

        activeProjectiles.Clear();
    }
}
