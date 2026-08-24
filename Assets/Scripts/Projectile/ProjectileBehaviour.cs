using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    [TitleGroup("Movement")]
    [MinValue(0f)]
    [SerializeField] private float projectileSpeed = 1f;
    [TitleGroup("Movement")]
    [MinValue(0f)]
    [SerializeField] private float hitDistanceThreshold = 0.1f;
    [TitleGroup("Movement")]
    [MinValue(0f)]
    [SerializeField] private float maxLifetime = 10f;

    [TitleGroup("Impact Gameplay")]
    [SerializeField] private EffectDefinition impactEffectDefinition;
    [TitleGroup("Impact VFX")]
    [SerializeField] private GameObject impactVfxPrefab;

    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private ProjectileBehaviour projectileTemplate;
    private ProjectileFlightType flightType;
    private MonsterBehaviour targetMonster;
    private Vector3 targetPosition;
    private Vector3 launchDirection;
    private Vector3 startPosition;
    private readonly List<MonsterBehaviour> piercedMonsters = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> resolvedExplosiveArrowTargets = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> resolvedExplosiveShellTargets = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> resolvedBlastRoundsTargets = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> bounceCandidates = new List<MonsterBehaviour>();
    private readonly HashSet<MonsterBehaviour> bounceHitHistory = new HashSet<MonsterBehaviour>();
    private ArcherProjectileReleaseIdentity archerReleaseIdentity;
    private TowerUpgradeDefinition explosiveArrowSourceUpgrade;
    private TowerUpgradeDefinition explosiveShellSourceUpgrade;
    private TowerUpgradeDefinition blastRoundsSourceUpgrade;
    private EffectDefinition explosiveArrowEffect;
    private EffectDefinition explosiveShellEffect;
    private EffectDefinition blastRoundsEffect;
    private bool allowsElementalApplication;
    private int sourceDroneInstanceId;
    private long droneBurstId;
    private bool isAdditionalDrone;
    private float damageScale;
    private TowerDamageSourceIdentity damageSourceIdentity;
    private int remainingPiercingHitCount;
    private int remainingBounceCount;
    private float elapsedLifetime;
    private float arcTravelTime;
    private float initialArcHeight;
    private float bounceSearchRadius;
    private float bounceArcHeight;
    private float bounceDamageScale;
    private TargetSelectionType bounceTargetSelectionType;
    private bool canPierce;
    private bool isBounceChild;
    private bool isInitialized;
    private bool hasImpacted;
    private bool hasEnded;
#if UNITY_EDITOR
    private bool hasIntendedTargetSnapshot;
    private ProjectileArcImpactResolutionType arcImpactResolutionType;
    private ProjectileArcMemberType arcMemberType;
    private float arcConfirmationToReleaseSeconds;
    private bool hasArcIntendedTargetAtRelease;
    private float arcLandingToIntendedDistanceAtRelease;
    private float arcIntendedMoveSpeedAtRelease;
    private MonsterBehaviour arcNearestOtherTargetAtImpact;
    private float arcLandingToNearestOtherDistanceAtImpact;
    private ProjectileArcTargetRelationObservation
        arcTargetRelationObservationAtResolution;
#endif

    public event Action<ProjectileImpactContext> OnImpact;
    public event Action<EffectTriggerContext> OnEffectTriggerContextCreated;
    public event Action<ProjectileBehaviour> OnChildReleased;
    public event Action<ProjectileBehaviour> OnEnded;
#if UNITY_EDITOR
    public static event Action<ProjectileRuntimeObservation> OnRuntimeObserved;
#endif

    public TowerInstance SourceTower => sourceTower;
    public float ProjectileSpeed => projectileSpeed;
    public float HitDistanceThreshold => hitDistanceThreshold;
    public float MaxLifetime => maxLifetime;
    public EffectDefinition ImpactEffectDefinition => impactEffectDefinition;
    public GameObject ImpactVfxPrefab => impactVfxPrefab;
    public ProjectileFlightType FlightType => flightType;
    public MonsterBehaviour TargetMonster => targetMonster;
    public Vector3 TargetPosition => targetPosition;
    public bool IsInitialized => isInitialized;
    public bool IsActiveForRefresh =>
        isInitialized && !hasImpacted && !hasEnded && gameObject.activeInHierarchy;
    public ArcherProjectileReleaseIdentity ArcherReleaseIdentity => archerReleaseIdentity;

#if UNITY_EDITOR
    public void ConfigureArcRuntimeObservation(
        ProjectileArcMemberType memberType,
        float confirmationToReleaseSeconds)
    {
        if (flightType != ProjectileFlightType.Arc)
        {
            return;
        }

        arcMemberType = memberType;
        arcConfirmationToReleaseSeconds =
            Mathf.Max(0f, confirmationToReleaseSeconds);
    }
#endif

    public bool IsValid()
    {
        if (projectileSpeed <= 0f)
        {
            Debug.LogWarning("Projectile behaviour is invalid: projectile speed must be greater than zero.", this);
            return false;
        }

        if (hitDistanceThreshold <= 0f)
        {
            Debug.LogWarning("Projectile behaviour is invalid: hit distance threshold must be greater than zero.", this);
            return false;
        }

        if (maxLifetime <= 0f)
        {
            Debug.LogWarning("Projectile behaviour is invalid: max lifetime must be greater than zero.", this);
            return false;
        }

        return true;
    }

    public void Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        ProjectileBehaviour projectileTemplate,
        MonsterBehaviour targetMonster,
        Vector3 targetPosition,
        float damageScale,
        TowerDamageSourceIdentity damageSourceIdentity,
        ProjectileFlightType flightType,
        float initialArcHeight = 0f,
        ProjectileRuntimeOptions runtimeOptions = default,
        IReadOnlyCollection<MonsterBehaviour> inheritedBounceHitHistory = null,
        ArcherProjectileReleaseIdentity archerReleaseIdentity = default)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.projectileTemplate = projectileTemplate;
        this.flightType = flightType;
        this.targetMonster = targetMonster;
        this.targetPosition = targetPosition;
        this.archerReleaseIdentity = archerReleaseIdentity;
        this.damageScale = damageScale;
        this.damageSourceIdentity = damageSourceIdentity;
        this.initialArcHeight = Mathf.Max(0f, initialArcHeight);
        canPierce = runtimeOptions.CanPierce;
        allowsElementalApplication = runtimeOptions.AllowsElementalApplication;
        remainingPiercingHitCount = canPierce
            ? Mathf.Max(1, runtimeOptions.MaxPierceHitCount)
            : 1;
        isBounceChild = runtimeOptions.IsBounceChild;
        explosiveArrowSourceUpgrade = runtimeOptions.ExplosiveArrowSourceUpgrade;
        explosiveArrowEffect = runtimeOptions.ExplosiveArrowEffect;
        explosiveShellSourceUpgrade = runtimeOptions.ExplosiveShellSourceUpgrade;
        explosiveShellEffect = runtimeOptions.ExplosiveShellEffect;
        bounceSearchRadius = runtimeOptions.BounceSearchRadius;
        remainingBounceCount = runtimeOptions.RemainingBounceCount;
        bounceArcHeight = runtimeOptions.BounceArcHeight;
        bounceTargetSelectionType = runtimeOptions.BounceTargetSelectionType;
        bounceDamageScale = runtimeOptions.BounceDamageScale;
        blastRoundsSourceUpgrade = runtimeOptions.BlastRoundsSourceUpgrade;
        blastRoundsEffect = runtimeOptions.BlastRoundsEffect;
        sourceDroneInstanceId = runtimeOptions.SourceDroneInstanceId;
        droneBurstId = runtimeOptions.DroneBurstId;
        isAdditionalDrone = runtimeOptions.IsAdditionalDrone;

        startPosition = transform.position;
        piercedMonsters.Clear();
        resolvedExplosiveArrowTargets.Clear();
        resolvedExplosiveShellTargets.Clear();
        resolvedBlastRoundsTargets.Clear();
        bounceCandidates.Clear();
        CopyBounceHitHistory(inheritedBounceHitHistory);
        elapsedLifetime = 0f;
        hasImpacted = false;
        hasEnded = false;
#if UNITY_EDITOR
        hasIntendedTargetSnapshot = targetMonster != null;
        arcImpactResolutionType =
            ProjectileArcImpactResolutionType.NotApplicable;
        arcMemberType = flightType != ProjectileFlightType.Arc
            ? ProjectileArcMemberType.NotApplicable
            : isBounceChild
                ? ProjectileArcMemberType.BounceChild
                : damageSourceIdentity.SourceType ==
                  TowerDamageSourceType.AdditionalDirect
                    ? ProjectileArcMemberType.AdditionalInitial
                    : ProjectileArcMemberType.PrimaryInitial;
        arcConfirmationToReleaseSeconds = 0f;
        hasArcIntendedTargetAtRelease =
            flightType == ProjectileFlightType.Arc &&
            IsTrackedValidTarget(targetMonster);
        arcLandingToIntendedDistanceAtRelease =
            hasArcIntendedTargetAtRelease
                ? Mathf.Sqrt(GetHitDistanceSqrFromPosition(
                    targetMonster,
                    targetPosition))
                : 0f;
        arcIntendedMoveSpeedAtRelease =
            hasArcIntendedTargetAtRelease
                ? targetMonster.CurrentMoveSpeed
                : 0f;
        arcNearestOtherTargetAtImpact = null;
        arcLandingToNearestOtherDistanceAtImpact = 0f;
        arcTargetRelationObservationAtResolution = default;
#endif

        if (!CanInitialize())
        {
            DestroyProjectile();
            return;
        }

        isInitialized = InitializeFlight();

        if (!isInitialized)
        {
            DestroyProjectile();
            return;
        }

#if UNITY_EDITOR
        PublishRuntimeObservationSafely(
            ProjectileRuntimeObservation.CreateReleased(
                sourceTower,
                flightType,
                isBounceChild));
#endif
    }

    private bool CanInitialize()
    {
        if (projectileTemplate == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: projectile template is null.", this);
            return false;
        }

        if (projectileSpeed <= 0f)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: projectile speed must be greater than zero.", this);
            return false;
        }

        if (maxLifetime <= 0f)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: max lifetime must be greater than zero.", this);
            return false;
        }

        if (!damageSourceIdentity.IsValid ||
            float.IsNaN(damageScale) ||
            float.IsInfinity(damageScale) ||
            damageScale <= 0f)
        {
            Debug.LogWarning(
                "Projectile behaviour cannot initialize: direct Damage Scale and source identity must be valid.",
                this);
            return false;
        }

        switch (flightType)
        {
            case ProjectileFlightType.Direction:
                return CanInitializeDirectionFlight();
            case ProjectileFlightType.Arc:
                return CanInitializeArcFlight();
            default:
                Debug.LogWarning($"Projectile behaviour cannot initialize: unsupported flight type '{flightType}'.", this);
                return false;
        }
    }

    private bool CanInitializeDirectionFlight()
    {
        if (monsterManager == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize direction flight: monster manager is null.", this);
            return false;
        }

        if (!IsValidTarget(targetMonster))
        {
            Debug.LogWarning("Projectile behaviour cannot initialize direction flight: target monster is invalid.", this);
            return false;
        }

        return true;
    }

    private bool CanInitializeArcFlight()
    {
        if (monsterManager == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize Arc flight: monster manager is null.", this);
            return false;
        }

        if (hitDistanceThreshold <= 0f)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize Arc flight: hit distance threshold must be greater than zero.", this);
            return false;
        }

        return true;
    }

    private bool InitializeFlight()
    {
        switch (flightType)
        {
            case ProjectileFlightType.Direction:
                return InitializeDirectionFlight();
            case ProjectileFlightType.Arc:
                return InitializeArcFlight();
            default:
                return false;
        }
    }

    private bool InitializeDirectionFlight()
    {
        launchDirection = CalculateLaunchDirection(targetPosition);
        return true;
    }

    private bool InitializeArcFlight()
    {
        arcTravelTime = CalculateArcTravelTime();
        return true;
    }

    private void Update()
    {
        if (!isInitialized || hasImpacted)
        {
            return;
        }

        elapsedLifetime += Time.deltaTime;

        if (elapsedLifetime >= maxLifetime)
        {
            DestroyProjectile();
            return;
        }

        switch (flightType)
        {
            case ProjectileFlightType.Direction:
                UpdateDirectionFlight();
                break;
            case ProjectileFlightType.Arc:
                UpdateArcFlight();
                break;
            default:
                DestroyProjectile();
                break;
        }
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
        if (hasEnded)
        {
            return;
        }

#if UNITY_EDITOR
        PublishEndedWithoutImpactIfNeeded();
#endif
        hasImpacted = true;
        EndProjectile();
    }

    public bool TryAddPiercingCapacity(int capacityDelta)
    {
        if (!IsActiveForRefresh ||
            !archerReleaseIdentity.IsValid ||
            capacityDelta == 0)
        {
            return false;
        }

        canPierce = true;
        remainingPiercingHitCount = Mathf.Max(
            0,
            remainingPiercingHitCount + capacityDelta);

        if (remainingPiercingHitCount <= 0)
        {
            FinishDirectionProjectileAfterImpact();
        }

        return true;
    }

    public bool TryRefreshExplosiveShell(
        TowerUpgradeDefinition sourceUpgrade,
        EffectDefinition effectDefinition)
    {
        if (!IsActiveForRefresh || flightType != ProjectileFlightType.Arc)
        {
            return false;
        }

        explosiveShellSourceUpgrade = sourceUpgrade;
        explosiveShellEffect = effectDefinition;
        return true;
    }

    public bool TryEnableInitialBouncingShell(
        float searchRadius,
        int maxBounceCount,
        float arcHeight,
        TargetSelectionType selectionType,
        float resolvedBounceDamageScale)
    {
        if (!IsActiveForRefresh ||
            flightType != ProjectileFlightType.Arc ||
            isBounceChild ||
            float.IsNaN(resolvedBounceDamageScale) ||
            float.IsInfinity(resolvedBounceDamageScale) ||
            resolvedBounceDamageScale <= 0f)
        {
            return false;
        }

        bounceSearchRadius = Mathf.Max(0f, searchRadius);
        remainingBounceCount = Mathf.Max(0, maxBounceCount);
        bounceArcHeight = Mathf.Max(0f, arcHeight);
        bounceTargetSelectionType = selectionType;
        bounceDamageScale = resolvedBounceDamageScale;
        return true;
    }

    public bool TryRefreshBlastRounds(
        TowerUpgradeDefinition sourceUpgrade,
        EffectDefinition effectDefinition)
    {
        if (!IsActiveForRefresh || flightType != ProjectileFlightType.Direction)
        {
            return false;
        }

        blastRoundsSourceUpgrade = sourceUpgrade;
        blastRoundsEffect = effectDefinition;
        return true;
    }

    private void UpdateDirectionFlight()
    {
        transform.position += launchDirection * projectileSpeed * Time.deltaTime;

        FaceMoveDirection(launchDirection);

        if (!TryGetDirectionProjectileHit(out MonsterBehaviour hitMonster))
        {
            return;
        }

        if (canPierce && piercedMonsters.Contains(hitMonster))
        {
            return;
        }

        ImpactDirectionProjectile(hitMonster);
    }

    private void UpdateArcFlight()
    {
        float progress = Mathf.Clamp01(elapsedLifetime / arcTravelTime);
        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        float arcHeight = isBounceChild
            ? bounceArcHeight
            : initialArcHeight;
        nextPosition.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
        Vector3 moveDirection = nextPosition - transform.position;
        transform.position = nextPosition;

        FaceMoveDirection(moveDirection);

        if (progress < 1f)
        {
            return;
        }

        transform.position = targetPosition;
        ImpactArcProjectile();
    }

    private float CalculateArcTravelTime()
    {
        float distance = Vector3.Distance(startPosition, targetPosition);
        return Mathf.Max(distance / projectileSpeed, 0.01f);
    }

    private void FaceMoveDirection(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleMonsterCollision(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryHandleMonsterCollision(collision.collider);
    }

    private void TryHandleMonsterCollision(Collider hitCollider)
    {
        if (!isInitialized ||
            hasImpacted ||
            hitCollider == null)
        {
            return;
        }

        MonsterBehaviour hitMonster = hitCollider.GetComponentInParent<MonsterBehaviour>();

        if (flightType != ProjectileFlightType.Direction)
        {
            return;
        }

        if (!IsDirectHitEnabled())
        {
            return;
        }

        if (!IsTrackedValidTarget(hitMonster))
        {
            return;
        }

        if (!IsWithinHitDistance(hitMonster))
        {
            return;
        }

        ImpactDirectionProjectile(hitMonster);
    }

    private void ImpactDirectionProjectile(MonsterBehaviour hitMonster)
    {
        if (hasImpacted || hitMonster == null)
        {
            return;
        }

        if (canPierce)
        {
            if (piercedMonsters.Contains(hitMonster))
            {
                return;
            }

            piercedMonsters.Add(hitMonster);
            ApplyDirectionProjectileImpact(hitMonster);
            remainingPiercingHitCount--;

            if (remainingPiercingHitCount <= 0)
            {
                FinishDirectionProjectileAfterImpact();
            }

            return;
        }

        ApplyDirectionProjectileImpact(hitMonster);
        FinishDirectionProjectileAfterImpact();
    }

    private void ApplyDirectionProjectileImpact(MonsterBehaviour hitMonster)
    {
        Vector3 impactPosition = transform.position;

        if (!TryResolveDirectDamage(out TowerOwnedDamageResolution damageResolution))
        {
            return;
        }

        hitMonster.TakeDamage(damageResolution.FinalDamage);
        TowerRuntimeStatResolver.PublishTowerOwnedDamageApplication(
            damageResolution,
            1);
        bool directElementalOpportunity =
            TryApplyElementalOpportunity(hitMonster, impactPosition);
#if UNITY_EDITOR
        PublishDroneBurstObservation(
            DroneBurstRuntimeObservationType.ProjectileDirectHit,
            count: 1);

        if (directElementalOpportunity)
        {
            PublishDroneBurstObservation(
                DroneBurstRuntimeObservationType.DirectElementalOpportunity,
                count: 1);
        }
#endif
        RaiseImpact(hitMonster, impactPosition, damageResolution);
        ExecuteExplosiveArrowImpact(
            hitMonster,
            impactPosition);
        ExecuteBlastRoundsImpact(impactPosition);
    }

    private void ExecuteExplosiveArrowImpact(
        MonsterBehaviour directTarget,
        Vector3 impactPosition)
    {
        if (explosiveArrowEffect == null)
        {
            return;
        }

        bool executed = EffectExecutor.ExecuteWithResolvedTargets(
            explosiveArrowEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: explosiveArrowSourceUpgrade,
                targetMonster: directTarget,
                hasTriggerPosition: true,
                triggerPosition: impactPosition,
                allowsElementalApplication: false),
            resolvedExplosiveArrowTargets);

        if (!executed)
        {
            return;
        }

        for (int i = 0; i < resolvedExplosiveArrowTargets.Count; i++)
        {
            TryApplyElementalOpportunity(
                resolvedExplosiveArrowTargets[i],
                impactPosition);
        }
    }

    private void ExecuteBlastRoundsImpact(Vector3 impactPosition)
    {
        if (blastRoundsEffect == null)
        {
            return;
        }

        bool executed = EffectExecutor.ExecuteWithResolvedTargets(
            blastRoundsEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: blastRoundsSourceUpgrade,
                targetMonster: null,
                hasTriggerPosition: true,
                triggerPosition: impactPosition,
                // Elemental Buff actions stay gated here; Blast Rounds grants explicit opportunities below.
                allowsElementalApplication: false),
            resolvedBlastRoundsTargets);

        if (!executed)
        {
            return;
        }

        int elementalOpportunityCount = 0;

        for (int i = 0; i < resolvedBlastRoundsTargets.Count; i++)
        {
            if (TryApplyElementalOpportunity(
                    resolvedBlastRoundsTargets[i],
                    impactPosition))
            {
                elementalOpportunityCount++;
            }
        }

#if UNITY_EDITOR
        PublishDroneBurstObservation(
            DroneBurstRuntimeObservationType.BlastTargetElementalOpportunity,
            elementalOpportunityCount);
#endif
    }

    private void FinishDirectionProjectileAfterImpact()
    {
        hasImpacted = true;
        DestroyProjectile();
    }

    private void ImpactArcProjectile()
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        Vector3 impactPosition = transform.position;
        MonsterBehaviour hitMonster = null;
        bool hasResolvedMonster = TryResolveArcImpactTarget(
            impactPosition,
            out MonsterBehaviour resolvedMonster);

        if (hasResolvedMonster)
        {
            hitMonster = resolvedMonster;
        }

        if (!TryResolveDirectDamage(out TowerOwnedDamageResolution damageResolution))
        {
            DestroyProjectile();
            return;
        }

#if UNITY_EDITOR
        arcTargetRelationObservationAtResolution =
            CreateArcTargetRelationObservation(hitMonster);
#endif

        if (hasResolvedMonster)
        {
            bounceHitHistory.Add(hitMonster);
            hitMonster.TakeDamage(damageResolution.FinalDamage);
            TowerRuntimeStatResolver.PublishTowerOwnedDamageApplication(
                damageResolution,
                1);
            TryApplyElementalOpportunity(hitMonster, impactPosition);
        }

        RaiseImpact(hitMonster, impactPosition, damageResolution);
        ExecuteExplosiveShellImpact(impactPosition);
        TryReleaseBounceChild(impactPosition);
        DestroyProjectile();
    }

    private void CopyBounceHitHistory(IReadOnlyCollection<MonsterBehaviour> inheritedBounceHitHistory)
    {
        bounceHitHistory.Clear();

        if (inheritedBounceHitHistory == null)
        {
            return;
        }

        foreach (MonsterBehaviour monster in inheritedBounceHitHistory)
        {
            if (monster != null)
            {
                bounceHitHistory.Add(monster);
            }
        }
    }

    private void ExecuteExplosiveShellImpact(Vector3 impactPosition)
    {
        if (explosiveShellEffect == null)
        {
            return;
        }

        bool executed = EffectExecutor.ExecuteWithResolvedTargets(
            explosiveShellEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: explosiveShellSourceUpgrade,
                targetMonster: null,
                hasTriggerPosition: true,
                triggerPosition: impactPosition,
                // Elemental Buff actions stay gated here; non-elemental authored Buff actions may still execute.
                allowsElementalApplication: false),
            resolvedExplosiveShellTargets);

        if (!executed)
        {
            return;
        }

        for (int i = 0; i < resolvedExplosiveShellTargets.Count; i++)
        {
            TryApplyElementalOpportunity(
                resolvedExplosiveShellTargets[i],
                impactPosition);
        }
    }

    private bool TryReleaseBounceChild(Vector3 impactPosition)
    {
        if (remainingBounceCount <= 0 ||
            bounceSearchRadius <= 0f ||
            !TryResolveBounceTarget(impactPosition, out MonsterBehaviour bounceTarget))
        {
            return false;
        }

        Vector3 bounceTargetPosition = EffectTargetResolver.GetMonsterHitPosition(bounceTarget);
        return TryCreateBounceChild(impactPosition, bounceTargetPosition);
    }

    private bool TryResolveBounceTarget(
        Vector3 impactPosition,
        out MonsterBehaviour bounceTarget)
    {
        bounceTarget = null;

        if (monsterManager == null)
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float searchRadiusSqr = bounceSearchRadius * bounceSearchRadius;
        bounceCandidates.Clear();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(monster) ||
                bounceHitHistory.Contains(monster))
            {
                continue;
            }

            float distanceSqr =
                (EffectTargetResolver.GetMonsterHitPosition(monster) - impactPosition).sqrMagnitude;

            if (distanceSqr > searchRadiusSqr)
            {
                continue;
            }

            bounceCandidates.Add(monster);
        }

        if (bounceCandidates.Count == 0)
        {
            return false;
        }

        bounceTarget = SelectBounceTarget(impactPosition);
        return bounceTarget != null;
    }

    private MonsterBehaviour SelectBounceTarget(Vector3 impactPosition)
    {
        switch (bounceTargetSelectionType)
        {
            case TargetSelectionType.HighestHealth:
                return SelectBounceTargetByHealth(selectHighest: true);
            case TargetSelectionType.LowestHealth:
                return SelectBounceTargetByHealth(selectHighest: false);
            case TargetSelectionType.Random:
                return bounceCandidates[UnityEngine.Random.Range(0, bounceCandidates.Count)];
            case TargetSelectionType.Nearest:
            default:
                return SelectNearestBounceTarget(impactPosition);
        }
    }

    private MonsterBehaviour SelectNearestBounceTarget(Vector3 impactPosition)
    {
        MonsterBehaviour selectedTarget = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < bounceCandidates.Count; i++)
        {
            MonsterBehaviour candidate = bounceCandidates[i];
            float distanceSqr =
                (EffectTargetResolver.GetMonsterHitPosition(candidate) - impactPosition).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            selectedTarget = candidate;
            nearestDistanceSqr = distanceSqr;
        }

        return selectedTarget;
    }

    private MonsterBehaviour SelectBounceTargetByHealth(bool selectHighest)
    {
        MonsterBehaviour selectedTarget = bounceCandidates[0];

        for (int i = 1; i < bounceCandidates.Count; i++)
        {
            MonsterBehaviour candidate = bounceCandidates[i];
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

    private bool TryCreateBounceChild(
        Vector3 impactPosition,
        Vector3 bounceTargetPosition)
    {
        if (projectileTemplate == null)
        {
            return false;
        }

        ProjectileBehaviour childProjectile = Instantiate(
            projectileTemplate,
            impactPosition,
            Quaternion.identity);

        childProjectile.Initialize(
            sourceTower,
            monsterManager,
            projectileTemplate,
            targetMonster: null,
            targetPosition: bounceTargetPosition,
            damageScale: bounceDamageScale,
            damageSourceIdentity: TowerDamageSourceIdentity.BounceDirect,
            flightType: ProjectileFlightType.Arc,
            initialArcHeight: bounceArcHeight,
            runtimeOptions: CreateBounceChildRuntimeOptions(),
            inheritedBounceHitHistory: bounceHitHistory);

        if (!childProjectile.IsInitialized)
        {
            return false;
        }

        OnChildReleased?.Invoke(childProjectile);
        return true;
    }

    private ProjectileRuntimeOptions CreateBounceChildRuntimeOptions()
    {
        return new ProjectileRuntimeOptions(
            allowsElementalApplication: allowsElementalApplication,
            canPierce: false,
            maxPierceHitCount: 1,
            isBounceChild: true,
            explosiveShellSourceUpgrade: explosiveShellSourceUpgrade,
            explosiveShellEffect: explosiveShellEffect,
            bounceSearchRadius: bounceSearchRadius,
            remainingBounceCount: remainingBounceCount - 1,
            bounceArcHeight: bounceArcHeight,
            bounceTargetSelectionType: bounceTargetSelectionType,
            bounceDamageScale: bounceDamageScale);
    }

    private bool TryApplyElementalOpportunity(
        MonsterBehaviour target,
        Vector3 applicationPosition)
    {
        if (!allowsElementalApplication ||
            !EffectTargetResolver.IsValidMonsterTarget(target))
        {
            return false;
        }

        return ElementalApplication.TryApplyFromTowerAttack(
            sourceTower,
            target,
            applicationPosition);
    }

#if UNITY_EDITOR
    private void PublishDroneBurstObservation(
        DroneBurstRuntimeObservationType observationType,
        int count)
    {
        if (sourceDroneInstanceId == 0 || count <= 0)
        {
            return;
        }

        DroneBurstRuntimeDiagnostics.Publish(
            new DroneBurstRuntimeObservation(
                observationType,
                sourceTower,
                sourceDroneInstanceId,
                isAdditionalDrone,
                droneBurstId,
                allowsElementalApplication,
                count));
    }
#endif

    private void RaiseImpact(
        MonsterBehaviour hitMonster,
        Vector3 impactPosition,
        TowerOwnedDamageResolution damageResolution)
    {
        bool isArcPositionImpact = flightType == ProjectileFlightType.Arc;
        EffectDefinition impactEffectDefinition = isArcPositionImpact
            ? null
            : this.impactEffectDefinition;
        ProjectileImpactContext impactContext = new ProjectileImpactContext(
            sourceTower,
            hitMonster,
            impactPosition,
            damageResolution,
            impactEffectDefinition
        );

        PlayImpactVfx(impactContext.ImpactPosition);
#if UNITY_EDITOR
        PublishRuntimeObservationSafely(
            ProjectileRuntimeObservation.CreateImpact(
                sourceTower,
                flightType,
                isBounceChild,
                hitMonster != null,
                arcImpactResolutionType,
                flightType == ProjectileFlightType.Arc
                    ? arcTargetRelationObservationAtResolution
                    : default));
#endif
        OnImpact?.Invoke(impactContext);
        EffectTriggerContext effectTriggerContext = CreateEffectTriggerContext(
            hitMonster,
            impactPosition);
        OnEffectTriggerContextCreated?.Invoke(effectTriggerContext);

        if (isArcPositionImpact)
        {
            return;
        }

        EffectExecutor.Execute(impactEffectDefinition, effectTriggerContext);
    }

    private EffectTriggerContext CreateEffectTriggerContext(
        MonsterBehaviour hitMonster,
        Vector3 triggerPosition)
    {
        return new EffectTriggerContext(
            sourceTower: sourceTower,
            sourceUpgrade: null,
            targetMonster: flightType == ProjectileFlightType.Arc ? null : hitMonster,
            hasTriggerPosition: true,
            triggerPosition: triggerPosition,
            allowsElementalApplication: false
        );
    }

    private void PlayImpactVfx(Vector3 impactPosition)
    {
        if (impactVfxPrefab == null)
        {
            return;
        }

        Instantiate(impactVfxPrefab, impactPosition, Quaternion.identity);
    }

    private Vector3 CalculateLaunchDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    private bool TryResolveArcImpactTarget(
        Vector3 impactPosition,
        out MonsterBehaviour hitMonster)
    {
        hitMonster = null;
#if UNITY_EDITOR
        arcNearestOtherTargetAtImpact = null;
        arcLandingToNearestOtherDistanceAtImpact = 0f;
#endif

        if (monsterManager == null || hitDistanceThreshold <= 0f)
        {
#if UNITY_EDITOR
            arcImpactResolutionType = hasIntendedTargetSnapshot
                ? ProjectileArcImpactResolutionType.PositionOnlyIntendedInvalid
                : ProjectileArcImpactResolutionType.PositionOnlyWithoutIntendedTarget;
#endif
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float hitDistanceThresholdSqr = hitDistanceThreshold * hitDistanceThreshold;

        if (IsTrackedValidTarget(targetMonster) &&
            GetHitDistanceSqrFromPosition(targetMonster, impactPosition) <=
            hitDistanceThresholdSqr)
        {
            hitMonster = targetMonster;
#if UNITY_EDITOR
            arcImpactResolutionType =
                ProjectileArcImpactResolutionType.IntendedTarget;
#endif
            return true;
        }

        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster == null || !monster.IsGameplayTargetable)
            {
                continue;
            }

            float distanceSqr = GetHitDistanceSqrFromPosition(
                monster,
                impactPosition);

#if UNITY_EDITOR
            if (monster != targetMonster &&
                (arcNearestOtherTargetAtImpact == null ||
                 distanceSqr <
                 arcLandingToNearestOtherDistanceAtImpact *
                 arcLandingToNearestOtherDistanceAtImpact))
            {
                arcNearestOtherTargetAtImpact = monster;
                arcLandingToNearestOtherDistanceAtImpact =
                    Mathf.Sqrt(distanceSqr);
            }
#endif

            if (distanceSqr > hitDistanceThresholdSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            hitMonster = monster;
            nearestDistanceSqr = distanceSqr;
        }

        if (hitMonster != null)
        {
#if UNITY_EDITOR
            arcImpactResolutionType =
                ProjectileArcImpactResolutionType.FallbackTarget;
#endif
            return true;
        }

#if UNITY_EDITOR
        arcImpactResolutionType = !hasIntendedTargetSnapshot
            ? ProjectileArcImpactResolutionType.PositionOnlyWithoutIntendedTarget
            : IsTrackedValidTarget(targetMonster)
                ? ProjectileArcImpactResolutionType.PositionOnlyIntendedOutOfRange
                : ProjectileArcImpactResolutionType.PositionOnlyIntendedInvalid;
#endif
        return false;
    }

#if UNITY_EDITOR
    private ProjectileArcTargetRelationObservation
        CreateArcTargetRelationObservation(MonsterBehaviour resolvedTarget)
    {
        if (flightType != ProjectileFlightType.Arc)
        {
            return default;
        }

        bool hasIntendedAtImpact = IsTrackedValidTarget(targetMonster);
        bool hasResolvedAtImpact = IsTrackedValidTarget(resolvedTarget);
        bool hasNearestOtherAtImpact =
            IsTrackedValidTarget(arcNearestOtherTargetAtImpact);

        return new ProjectileArcTargetRelationObservation(
            arcMemberType,
            targetMonster,
            resolvedTarget,
            arcNearestOtherTargetAtImpact,
            arcConfirmationToReleaseSeconds,
            elapsedLifetime,
            arcTravelTime,
            hitDistanceThreshold,
            hasArcIntendedTargetAtRelease,
            arcLandingToIntendedDistanceAtRelease,
            arcIntendedMoveSpeedAtRelease,
            hasIntendedAtImpact,
            hasIntendedAtImpact
                ? Mathf.Sqrt(GetHitDistanceSqrFromPosition(
                    targetMonster,
                    targetPosition))
                : 0f,
            hasIntendedAtImpact ? targetMonster.CurrentMoveSpeed : 0f,
            hasResolvedAtImpact,
            hasResolvedAtImpact
                ? Mathf.Sqrt(GetHitDistanceSqrFromPosition(
                    resolvedTarget,
                    targetPosition))
                : 0f,
            hasResolvedAtImpact ? resolvedTarget.CurrentMoveSpeed : 0f,
            hasNearestOtherAtImpact,
            hasNearestOtherAtImpact
                ? arcLandingToNearestOtherDistanceAtImpact
                : 0f,
            hasNearestOtherAtImpact
                ? arcNearestOtherTargetAtImpact.CurrentMoveSpeed
                : 0f);
    }
#endif

    private static float GetHitDistanceSqrFromPosition(
        MonsterBehaviour monster,
        Vector3 position)
    {
        return (EffectTargetResolver.GetMonsterHitPosition(monster) - position)
            .sqrMagnitude;
    }

    private bool TryGetDirectionProjectileHit(out MonsterBehaviour hitMonster)
    {
        hitMonster = null;

        if (monsterManager == null || !IsDirectHitEnabled())
        {
            return false;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float hitDistanceThresholdSqr = hitDistanceThreshold * hitDistanceThreshold;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!IsValidTarget(monster) ||
                (canPierce && piercedMonsters.Contains(monster)))
            {
                continue;
            }

            float distanceSqr = GetHitDistanceSqr(monster);

            if (distanceSqr > hitDistanceThresholdSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            hitMonster = monster;
            nearestDistanceSqr = distanceSqr;
        }

        return hitMonster != null;
    }

    private bool IsWithinHitDistance(MonsterBehaviour monster)
    {
        if (!IsDirectHitEnabled())
        {
            return false;
        }

        float hitDistanceThresholdSqr = hitDistanceThreshold * hitDistanceThreshold;
        return GetHitDistanceSqr(monster) <= hitDistanceThresholdSqr;
    }

    private bool IsDirectHitEnabled()
    {
        return hitDistanceThreshold > 0f;
    }

    private float GetHitDistanceSqr(MonsterBehaviour monster)
    {
        return (GetMonsterHitPosition(monster) - transform.position).sqrMagnitude;
    }

    private static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
    }

    private bool IsTrackedValidTarget(MonsterBehaviour monster)
    {
        if (!IsValidTarget(monster) || monsterManager == null)
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

    private bool TryResolveDirectDamage(
        out TowerOwnedDamageResolution damageResolution)
    {
        return TowerRuntimeStatResolver.TryResolveTowerOwnedDamage(
            sourceTower,
            damageSourceIdentity,
            damageScale,
            out damageResolution);
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }

    private void DestroyProjectile()
    {
        EndProjectile();
    }

    private void EndProjectile()
    {
        if (hasEnded)
        {
            return;
        }

#if UNITY_EDITOR
        PublishEndedWithoutImpactIfNeeded();
#endif
        hasEnded = true;
        isInitialized = false;
        OnEnded?.Invoke(this);
        Destroy(gameObject);
    }

    private void NotifyEndedWithoutDestroy()
    {
        if (hasEnded)
        {
            return;
        }

#if UNITY_EDITOR
        PublishEndedWithoutImpactIfNeeded();
#endif
        hasEnded = true;
        isInitialized = false;
        hasImpacted = true;
        OnEnded?.Invoke(this);
    }

#if UNITY_EDITOR
    private void PublishEndedWithoutImpactIfNeeded()
    {
        if (!isInitialized || hasImpacted)
        {
            return;
        }

        PublishRuntimeObservationSafely(
            ProjectileRuntimeObservation.CreateEndedWithoutImpact(
                sourceTower,
                flightType,
                isBounceChild));
        PublishDroneBurstObservation(
            DroneBurstRuntimeObservationType.ProjectileEndedWithoutImpact,
            count: 1);
    }

    private void PublishRuntimeObservationSafely(
        ProjectileRuntimeObservation observation)
    {
        Action<ProjectileRuntimeObservation> handlers = OnRuntimeObserved;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<ProjectileRuntimeObservation>)invocationList[i])
                    .Invoke(observation);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Projectile runtime observation subscriber failed: " +
                    exception.Message,
                    this);
            }
        }
    }
#endif
}
