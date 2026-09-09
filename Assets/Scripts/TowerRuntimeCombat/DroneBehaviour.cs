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
    FinalDiving,
    Holding
}

public enum DroneTargetLossReason
{
    None = 0,
    Invalid = 1,
    OutOfRange = 2
}

public enum DroneCompletionReason
{
    None = 0,
    BatteryAerialRetirement = 1,
    FinalDiveImpact = 2,
    TechnicalCleanup = 3
}

public enum DroneBurstPhase
{
    ReadyToStartBurst,
    BetweenShots,
    InterBurstCooldown
}

#if UNITY_EDITOR
public enum DroneLifecycleRuntimeObservationType
{
    Initialized = 0,
    LaunchCompleted = 1,
    TargetLost = 2,
    ImmediateRetargeted = 3,
    HoldingEntered = 4,
    HoldingExited = 5,
    OrbitEntryCompleted = 6,
    BatteryDepleted = 7,
    FinalDiveEntered = 8,
    FinalDiveCompleted = 9,
    Completed = 10
}

public readonly struct DroneLifecycleRuntimeObservation
{
    public DroneLifecycleRuntimeObservation(
        DroneLifecycleRuntimeObservationType observationType,
        TowerInstance sourceTower,
        int sourceDroneInstanceId,
        bool isAdditionalDrone,
        DroneRuntimeState state,
        DroneTargetLossReason targetLossReason,
        DroneCompletionReason completionReason,
        float batteryRemaining,
        float observedAtTime)
    {
        ObservationType = observationType;
        SourceTower = sourceTower;
        SourceDroneInstanceId = sourceDroneInstanceId;
        IsAdditionalDrone = isAdditionalDrone;
        State = state;
        TargetLossReason = targetLossReason;
        CompletionReason = completionReason;
        BatteryRemaining = Mathf.Max(0f, batteryRemaining);
        ObservedAtTime = observedAtTime;
    }

    public DroneLifecycleRuntimeObservationType ObservationType { get; }
    public TowerInstance SourceTower { get; }
    public int SourceDroneInstanceId { get; }
    public bool IsAdditionalDrone { get; }
    public DroneRuntimeState State { get; }
    public DroneTargetLossReason TargetLossReason { get; }
    public DroneCompletionReason CompletionReason { get; }
    public float BatteryRemaining { get; }
    public float ObservedAtTime { get; }
}

public static class DroneLifecycleRuntimeDiagnostics
{
    public static event Action<DroneLifecycleRuntimeObservation> OnObserved;

    public static void Publish(DroneLifecycleRuntimeObservation observation)
    {
        Action<DroneLifecycleRuntimeObservation> observers = OnObserved;

        if (observers == null)
        {
            return;
        }

        Delegate[] invocationList = observers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<DroneLifecycleRuntimeObservation>)invocationList[i])(
                    observation);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Drone lifecycle runtime observation subscriber failed: " +
                    exception.Message);
            }
        }
    }
}

public enum DroneBurstRuntimeObservationType
{
    BurstStarted = 0,
    ProjectileReleased = 1,
    ProjectileDirectHit = 2,
    ProjectileEndedWithoutImpact = 3
}

public readonly struct DroneBurstRuntimeObservation
{
    public DroneBurstRuntimeObservation(
        DroneBurstRuntimeObservationType observationType,
        TowerInstance sourceTower,
        int sourceDroneInstanceId,
        bool isAdditionalDrone,
        long burstId,
        bool isOpeningShotSlot,
        bool allowsElementalApplication,
        int count = 1)
    {
        ObservationType = observationType;
        SourceTower = sourceTower;
        SourceDroneInstanceId = sourceDroneInstanceId;
        IsAdditionalDrone = isAdditionalDrone;
        BurstId = burstId;
        IsOpeningShotSlot = isOpeningShotSlot;
        AllowsElementalApplication = allowsElementalApplication;
        Count = Mathf.Max(0, count);
    }

    public DroneBurstRuntimeObservationType ObservationType { get; }
    public TowerInstance SourceTower { get; }
    public int SourceDroneInstanceId { get; }
    public bool IsAdditionalDrone { get; }
    public long BurstId { get; }
    public bool IsOpeningShotSlot { get; }
    public bool AllowsElementalApplication { get; }
    public int Count { get; }
}

public static class DroneBurstRuntimeDiagnostics
{
    public static event Action<DroneBurstRuntimeObservation> OnObserved;

    public static void Publish(DroneBurstRuntimeObservation observation)
    {
        Action<DroneBurstRuntimeObservation> observers = OnObserved;

        if (observers == null)
        {
            return;
        }

        Delegate[] invocationList = observers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<DroneBurstRuntimeObservation>)invocationList[i])(
                    observation);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Drone Burst runtime observation subscriber failed: " +
                    exception.Message);
            }
        }
    }
}
#endif

public readonly struct DroneStatRefresh
{
    public DroneStatRefresh(
        bool refreshAttackRange,
        float newAttackRange,
        bool refreshBurstCooldown,
        float newBurstCooldown)
    {
        RefreshAttackRange = refreshAttackRange;
        NewAttackRange = newAttackRange;
        RefreshBurstCooldown = refreshBurstCooldown;
        NewBurstCooldown = newBurstCooldown;
    }

    public bool RefreshAttackRange { get; }
    public float NewAttackRange { get; }
    public bool RefreshBurstCooldown { get; }
    public float NewBurstCooldown { get; }
    public bool HasAnyChange =>
        RefreshAttackRange ||
        RefreshBurstCooldown;
}

public class DroneBehaviour : MonoBehaviour
{
    private enum DroneProjectileReleaseResult
    {
        Released,
        TargetUnavailable,
        TechnicalFailure
    }

    [TitleGroup("Projectile")]
    [Required]
    [SerializeField] private ProjectileBehaviour projectilePrefab;

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
    private float damageScale;
    private TowerDamageSourceIdentity damageSourceIdentity;
    private float attackRange;
    private float currentBurstCooldown;
    private float finalDiveHitThreshold;
    private float orbitAngleRadians;
    private int orbitDirection = 1;
    private float batteryTimer;
    private float burstTimer;
    private Vector3 lastValidLockedTargetPosition;
    private Vector3 lastValidFinalDiveHitPosition;
    private int burstShotsRemaining;
    private long nextBurstId;
    private long currentBurstId;
    private DroneBurstPhase burstPhase;
    private bool hasReachedOrbitPath;
    private bool hasLastValidLockedTargetPosition;
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
    public bool IsAdditionalAttackEntity { get; private set; }
    public float BaseBatteryDuration => batteryDuration;
    public float BaseBurstCooldown => burstCooldown;

    public bool IsAuthoredConfigurationValid()
    {
        return projectilePrefab != null &&
               projectilePrefab.IsValid() &&
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
        float initializedDamageScale,
        bool isAdditionalAttackEntity,
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
        damageScale = initializedDamageScale;
        damageSourceIdentity = isAdditionalAttackEntity
            ? TowerDamageSourceIdentity.AdditionalDirect
            : TowerDamageSourceIdentity.PrimaryDirect;
        IsAdditionalAttackEntity = isAdditionalAttackEntity;
        attackRange = resolvedStats.AttackRange;
        currentBurstCooldown = resolvedStats.DroneBurstCooldown;
        orbitAngleRadians = 0f;
        orbitDirection = 1;
        batteryTimer = batteryDuration;
        nextBurstId = 1;
        ResetBurstState();
        hasReachedOrbitPath = false;
        hasLastValidLockedTargetPosition = false;
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
        CaptureCurrentTargetPosition();
        SetState(DroneRuntimeState.Launching);
#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.Initialized);
#endif
    }

    private bool CanInitialize()
    {
        if (sourceTower == null || monsterManager == null)
        {
            Debug.LogWarning(
                "Drone cannot initialize: source Tower or monster manager is null.",
                this);
            return false;
        }

        if (!damageSourceIdentity.IsValid ||
            float.IsNaN(damageScale) ||
            float.IsInfinity(damageScale) ||
            damageScale <= 0f)
        {
            Debug.LogWarning(
                "Drone cannot initialize: direct Damage Scale and source identity must be valid.",
                this);
            return false;
        }

        if (!IsValidTargetInRange(currentTarget))
        {
            Debug.LogWarning("Drone cannot initialize: initial target is invalid or outside source tower attack range.", this);
            return false;
        }

        if (projectilePrefab == null || !projectilePrefab.IsValid())
        {
            Debug.LogWarning("Drone cannot initialize: projectile prefab or behaviour authoring is missing.", this);
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
            case DroneRuntimeState.Holding:
                UpdateHolding();
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
            CompleteDrone(
                DroneCompletionReason.TechnicalCleanup,
                playAerialRetirementVfx: false,
                destroyObject: false);
        }
    }

    public void ForceCleanup()
    {
        CompleteDrone(
            DroneCompletionReason.TechnicalCleanup,
            playAerialRetirementVfx: false,
            destroyObject: true);
    }

    public void ApplyStatRefresh(DroneStatRefresh refresh)
    {
        if (!isInitialized || hasEnded || !refresh.HasAnyChange)
        {
            return;
        }

        if (refresh.RefreshAttackRange)
        {
            attackRange = Mathf.Max(0f, refresh.NewAttackRange);
        }

        if (refresh.RefreshBurstCooldown)
        {
            RefreshBurstCooldown(refresh.NewBurstCooldown);
        }

    }

    public bool RefreshBlastRounds(
        TowerUpgradeDefinition sourceUpgrade,
        EffectDefinition effectDefinition)
    {
        if (!isInitialized || hasEnded)
        {
            return false;
        }

        blastRoundsSourceUpgrade = sourceUpgrade;
        blastRoundsEffect = effectDefinition;
        return true;
    }

    public bool RefreshFinalDive(
        TowerUpgradeDefinition sourceUpgrade,
        float hitThreshold,
        EffectDefinition explosionEffect)
    {
        if (!isInitialized || hasEnded || hasResolvedBatteryEnd)
        {
            return false;
        }

        finalDiveSourceUpgrade = sourceUpgrade;
        finalDiveHitThreshold = Mathf.Max(0f, hitThreshold);
        finalDiveExplosionEffect = explosionEffect;
        return true;
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
        CaptureCurrentTargetPosition();

        MoveTowards(GetLaunchPosition());

        if (!IsAtPosition(GetLaunchPosition()))
        {
            return;
        }

#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.LaunchCompleted);
#endif

        DroneTargetLossReason targetLossReason =
            GetTargetLossReason(currentTarget);

        if (targetLossReason != DroneTargetLossReason.None)
        {
            ResolveTargetLoss(targetLossReason);
            return;
        }

        BeginOrbitingTarget(currentTarget);
    }

    private void UpdateOrbiting()
    {
        CaptureCurrentTargetPosition();
        DrainBattery();

        if (batteryTimer <= 0f)
        {
            ResolveBatteryDepletion();
            return;
        }

        DroneTargetLossReason targetLossReason =
            GetTargetLossReason(currentTarget);

        if (targetLossReason != DroneTargetLossReason.None)
        {
            ResolveTargetLoss(targetLossReason);
            return;
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
#if UNITY_EDITOR
            PublishLifecycleObservation(
                DroneLifecycleRuntimeObservationType.OrbitEntryCompleted);
#endif
        }
        else
        {
            AdvanceOrbitAngle();
            MoveTowards(CalculateOrbitPosition(currentTarget));
        }

        UpdateBurstFire();
    }

    private void UpdateHolding()
    {
        DrainBattery();

        if (batteryTimer <= 0f)
        {
            ResolveBatteryDepletion();
            return;
        }

        MonsterBehaviour selectedTarget = SelectTarget();

        if (IsValidTargetInRange(selectedTarget))
        {
            BeginOrbitingTarget(selectedTarget);
            return;
        }

        if (!hasLastValidLockedTargetPosition)
        {
            CompleteDrone(
                DroneCompletionReason.TechnicalCleanup,
                playAerialRetirementVfx: false,
                destroyObject: true);
            return;
        }

        AdvanceOrbitAngle();
        MoveTowards(CalculateOrbitPosition(lastValidLockedTargetPosition));
    }

    private void ResolveTargetLoss(DroneTargetLossReason targetLossReason)
    {
        if (targetLossReason == DroneTargetLossReason.None)
        {
            return;
        }

#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.TargetLost,
            targetLossReason: targetLossReason);
#endif

        MonsterBehaviour selectedTarget = SelectTarget();

        if (IsValidTargetInRange(selectedTarget))
        {
#if UNITY_EDITOR
            PublishLifecycleObservation(
                DroneLifecycleRuntimeObservationType.ImmediateRetargeted,
                targetLossReason: targetLossReason);
#endif
            BeginOrbitingTarget(selectedTarget);
            return;
        }

        BeginHolding();
    }

    private void BeginHolding()
    {
        if (!hasLastValidLockedTargetPosition)
        {
            CompleteDrone(
                DroneCompletionReason.TechnicalCleanup,
                playAerialRetirementVfx: false,
                destroyObject: true);
            return;
        }

        currentTarget = null;
        hasReachedOrbitPath = true;
        InitializeOrbitAngle(lastValidLockedTargetPosition);
        orbitDirection = ChooseOrbitDirection();
        SetState(DroneRuntimeState.Holding);
#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.HoldingEntered);
#endif
    }

    private void ResolveBatteryDepletion()
    {
        if (hasResolvedBatteryEnd)
        {
            return;
        }

        hasResolvedBatteryEnd = true;
#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.BatteryDepleted);
#endif

        if (!IsFinalDiveEnabled() ||
            !IsValidTargetInRange(currentTarget))
        {
            AerialDespawn();
            return;
        }

        lastValidFinalDiveHitPosition = EffectTargetResolver.GetMonsterHitPosition(currentTarget);
        hasResolvedFinalDiveImpact = false;
        ResetBurstState();
        SetState(DroneRuntimeState.FinalDiving);
#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.FinalDiveEntered);
#endif
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

        if (!TowerRuntimeStatResolver.TryResolveTowerOwnedDamage(
                sourceTower,
                TowerDamageSourceIdentity.FinalDiveDirect,
                damageScale,
                out TowerOwnedDamageResolution damageResolution))
        {
            CompleteDrone(
                DroneCompletionReason.TechnicalCleanup,
                playAerialRetirementVfx: false,
                destroyObject: true);
            return;
        }

        if (TryResolveFinalDiveDirectTarget(impactPosition, out MonsterBehaviour directTarget))
        {
            ElementalApplication.ObserveCandidate(
                sourceTower,
                directTarget,
                new ElementalOpportunityDiagnosticContext(
                    ElementalOpportunityProvenance.DroneFinalDive,
                    IsAdditionalAttackEntity
                        ? ElementalOpportunityMemberIdentity.Additional
                        : ElementalOpportunityMemberIdentity.Primary,
                    ElementalOpportunityResultRole.FinalDiveDirect,
                    0,
                    topologyAuthorized: false));
            TowerOwnedHitTransaction.ApplyDamage(
                directTarget,
                damageResolution,
                impactPosition,
                new ElementalOpportunityDiagnosticContext(
                    ElementalOpportunityProvenance.DroneFinalDive,
                    IsAdditionalAttackEntity
                        ? ElementalOpportunityMemberIdentity.Additional
                        : ElementalOpportunityMemberIdentity.Primary,
                    ElementalOpportunityResultRole.FinalDiveDirect,
                    0,
                    topologyAuthorized: false),
                allowsElementalApplication: false);
        }

        EffectExecutor.ExecuteWithResolvedTargets(
            finalDiveExplosionEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: finalDiveSourceUpgrade,
                targetMonster: null,
                hasTriggerPosition: true,
                triggerPosition: impactPosition,
                allowsElementalApplication: false,
                elementalOpportunityDiagnostics:
                    new ElementalOpportunityDiagnosticContext(
                        ElementalOpportunityProvenance.DroneFinalDive,
                        IsAdditionalAttackEntity
                            ? ElementalOpportunityMemberIdentity.Additional
                            : ElementalOpportunityMemberIdentity.Primary,
                        ElementalOpportunityResultRole.FinalDiveExplosion,
                        0,
                        topologyAuthorized: false,
                        observeResolvedTargetsAsCandidates: true)),
            resolvedFinalDiveExplosionTargets);

#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.FinalDiveCompleted);
#endif
        CompleteDrone(
            DroneCompletionReason.FinalDiveImpact,
            playAerialRetirementVfx: false,
            destroyObject: true);
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
        CompleteDrone(
            DroneCompletionReason.BatteryAerialRetirement,
            playAerialRetirementVfx: true,
            destroyObject: true);
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

                ReselectTargetForNextBurst();
                burstPhase = DroneBurstPhase.ReadyToStartBurst;
                StartBurst();
                return;
            case DroneBurstPhase.ReadyToStartBurst:
            default:
                StartBurst();
                return;
        }
    }

    private void ReselectTargetForNextBurst()
    {
        MonsterBehaviour selectedTarget = SelectTarget();

        if (IsValidTargetInRange(selectedTarget))
        {
            currentTarget = selectedTarget;
        }
    }

    private void StartBurst()
    {
        if (burstShotsRemaining <= 0)
        {
            burstShotsRemaining = Mathf.Max(1, burstCount);
            currentBurstId = nextBurstId++;
#if UNITY_EDITOR
            PublishBurstObservation(
                DroneBurstRuntimeObservationType.BurstStarted,
                currentBurstId,
                isOpeningShotSlot: false,
                allowsElementalApplication: false);
#endif
        }

        FireNextBurstShot();
    }

    private void FireNextBurstShot()
    {
        bool isBurstOpener = currentBurstId > 0 &&
                             burstShotsRemaining == Mathf.Max(1, burstCount);
        int shotOrdinal = Mathf.Max(
            0,
            Mathf.Max(1, burstCount) - burstShotsRemaining);
        DroneProjectileReleaseResult releaseResult = TryFireProjectile(
            currentTarget,
            currentBurstId,
            isBurstOpener,
            shotOrdinal);

        if (releaseResult == DroneProjectileReleaseResult.TargetUnavailable)
        {
            ResolveTargetLoss(GetTargetLossReason(currentTarget));
            return;
        }

        if (releaseResult == DroneProjectileReleaseResult.TechnicalFailure)
        {
            CompleteDrone(
                DroneCompletionReason.TechnicalCleanup,
                playAerialRetirementVfx: false,
                destroyObject: true);
            return;
        }

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
        currentBurstId = 0;
        burstPhase = DroneBurstPhase.ReadyToStartBurst;
    }

    private DroneProjectileReleaseResult TryFireProjectile(
        MonsterBehaviour target,
        long burstId,
        bool isBurstOpener,
        int shotOrdinal)
    {
        if (!IsValidTargetInRange(target))
        {
            return DroneProjectileReleaseResult.TargetUnavailable;
        }

        Transform spawnAnchor = GetFireAnchor();
        Vector3 targetPosition = GetMonsterHitPosition(target);
        ProjectileBehaviour projectileBehaviour = Instantiate(
            projectilePrefab,
            spawnAnchor.position,
            Quaternion.identity);
        bool allowsElementalApplication =
            isBurstOpener && !IsAdditionalAttackEntity;
        projectileBehaviour.Initialize(
            sourceTower,
            monsterManager,
            projectilePrefab,
            target,
            targetPosition,
            damageScale,
            damageSourceIdentity,
            flightType: ProjectileFlightType.Direction,
            runtimeOptions: new ProjectileRuntimeOptions(
                allowsElementalApplication: allowsElementalApplication,
                canPierce: false,
                maxPierceHitCount: 1,
                blastRoundsSourceUpgrade: blastRoundsSourceUpgrade,
                blastRoundsEffect: blastRoundsEffect,
                sourceDroneInstanceId: GetInstanceID(),
                droneBurstId: burstId,
                isAdditionalDrone: IsAdditionalAttackEntity,
                isOpeningShotSlot: isBurstOpener,
                elementalOpportunityProvenance:
                    isBurstOpener
                        ? ElementalOpportunityProvenance.DroneOpeningProjectile
                        : ElementalOpportunityProvenance.DroneLaterProjectile,
                elementalOpportunityMemberIdentity: IsAdditionalAttackEntity
                    ? ElementalOpportunityMemberIdentity.Additional
                    : ElementalOpportunityMemberIdentity.Primary,
                elementalResultOrdinal: shotOrdinal)
        );

        if (!projectileBehaviour.IsInitialized)
        {
            return DroneProjectileReleaseResult.TechnicalFailure;
        }

#if UNITY_EDITOR
        PublishBurstObservation(
            DroneBurstRuntimeObservationType.ProjectileReleased,
            burstId,
            isBurstOpener,
            allowsElementalApplication);
#endif
        OnProjectileReleased?.Invoke(projectileBehaviour);
        PlayAttackReleaseVfx(spawnAnchor, targetPosition);
        return DroneProjectileReleaseResult.Released;
    }

#if UNITY_EDITOR
    private void PublishLifecycleObservation(
        DroneLifecycleRuntimeObservationType observationType,
        DroneTargetLossReason targetLossReason = DroneTargetLossReason.None,
        DroneCompletionReason completionReason = DroneCompletionReason.None)
    {
        DroneLifecycleRuntimeDiagnostics.Publish(
            new DroneLifecycleRuntimeObservation(
                observationType,
                sourceTower,
                GetInstanceID(),
                IsAdditionalAttackEntity,
                State,
                targetLossReason,
                completionReason,
                batteryTimer,
                Time.time));
    }

    private void PublishBurstObservation(
        DroneBurstRuntimeObservationType observationType,
        long burstId,
        bool isOpeningShotSlot,
        bool allowsElementalApplication,
        int count = 1)
    {
        DroneBurstRuntimeDiagnostics.Publish(
            new DroneBurstRuntimeObservation(
                observationType,
                sourceTower,
                GetInstanceID(),
                IsAdditionalAttackEntity,
                burstId,
                isOpeningShotSlot,
                allowsElementalApplication,
                count));
    }
#endif

    private void CompleteDrone(
        DroneCompletionReason completionReason,
        bool playAerialRetirementVfx,
        bool destroyObject)
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
#if UNITY_EDITOR
        PublishLifecycleObservation(
            DroneLifecycleRuntimeObservationType.Completed,
            completionReason: completionReason);
#endif

        if (playAerialRetirementVfx && aerialDespawnVfxPrefab != null)
        {
            Instantiate(
                aerialDespawnVfxPrefab,
                transform.position,
                Quaternion.identity);
        }

        isInitialized = false;
        currentTarget = null;
        ResetBurstState();
        OnEnded?.Invoke(this);

        if (destroyObject)
        {
            Destroy(gameObject);
        }
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

    private DroneTargetLossReason GetTargetLossReason(
        MonsterBehaviour monster)
    {
        if (!IsValidTarget(monster))
        {
            return DroneTargetLossReason.Invalid;
        }

        return IsValidTargetInRange(monster)
            ? DroneTargetLossReason.None
            : DroneTargetLossReason.OutOfRange;
    }

    private void CaptureCurrentTargetPosition()
    {
        if (!IsValidTargetInRange(currentTarget))
        {
            return;
        }

        lastValidLockedTargetPosition = GetMonsterHitPosition(currentTarget);
        hasLastValidLockedTargetPosition = true;
    }

    private void BeginOrbitingTarget(MonsterBehaviour target)
    {
        bool exitsHolding = State == DroneRuntimeState.Holding;
        currentTarget = target;
        CaptureCurrentTargetPosition();
        hasReachedOrbitPath = false;
        InitializeOrbitAngle(lastValidLockedTargetPosition);
        orbitDirection = ChooseOrbitDirection();

        SetState(DroneRuntimeState.Orbiting);
#if UNITY_EDITOR
        if (exitsHolding)
        {
            PublishLifecycleObservation(
                DroneLifecycleRuntimeObservationType.HoldingExited);
        }
#endif
    }

    private void InitializeOrbitAngle(Vector3 center)
    {
        Vector3 radialDirection = transform.position - center;
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
        return CalculateOrbitPosition(GetMonsterHitPosition(target));
    }

    private Vector3 CalculateOrbitPosition(Vector3 center)
    {
        float orbitRadius = Mathf.Max(this.orbitRadius, 0.01f);
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(orbitAngleRadians),
            0f,
            Mathf.Sin(orbitAngleRadians)
        ) * orbitRadius;
        Vector3 point = center + orbitOffset;
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
               state == DroneRuntimeState.FinalDiving ||
               state == DroneRuntimeState.Holding;
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
