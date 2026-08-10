using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class DroneCombatBehaviour : TowerCombatBehaviour
{
    [TitleGroup("Drone")]
    [Required]
    [SerializeField] private GameObject dronePrefab;
    private readonly int defaultMaximumDroneCount = 1;

    private readonly HashSet<DroneBehaviour> activeDrones =
        new HashSet<DroneBehaviour>();

    private MonsterBehaviour pendingDroneTarget;
    private bool hasLoggedMissingDronePrefab;
    private bool hasLoggedInvalidBlastRoundsEffect;
    private bool hasLoggedInvalidFinalDiveConfiguration;

    public override TowerFamily SupportedTowerFamily => TowerFamily.Drone;

    protected override TowerCombatBaseStats CreateBaseStats()
    {
        float batteryDuration = TowerRuntimeStatResolver.MinimumDroneBatteryDuration;
        float burstCooldown = TowerRuntimeStatResolver.MinimumDroneBurstCooldown;

        if (TryGetDronePrefabBehaviour(out DroneBehaviour droneBehaviour))
        {
            batteryDuration = droneBehaviour.BaseBatteryDuration;
            burstCooldown = droneBehaviour.BaseBurstCooldown;
        }

        return new TowerCombatBaseStats(
            BaseAttackDamage,
            BaseAttackRange,
            BaseAttackCycleDuration,
            droneBatteryDuration: batteryDuration,
            droneBurstCooldown: burstCooldown);
    }

    protected override bool IsSubtypeConfigurationValid()
    {
        if (dronePrefab == null)
        {
            Debug.LogWarning("Drone combat is invalid: Drone prefab is missing.", this);
            return false;
        }

        if (!TryGetDronePrefabBehaviour(out DroneBehaviour droneBehaviour))
        {
            Debug.LogWarning(
                "Drone combat is invalid: prefab root is missing DroneBehaviour.",
                this);
            return false;
        }

        if (!droneBehaviour.IsAuthoredConfigurationValid())
        {
            Debug.LogWarning(
                "Drone combat is invalid: prefab DroneBehaviour has invalid authored data.",
                this);
            return false;
        }

        if (defaultMaximumDroneCount < 1)
        {
            Debug.LogWarning(
                "Drone combat is invalid: default maximum Drone count must be at least 1.",
                this);
            return false;
        }

        return true;
    }

    protected override void OnCombatInitialized()
    {
        hasLoggedMissingDronePrefab = false;
        hasLoggedInvalidBlastRoundsEffect = false;
        hasLoggedInvalidFinalDiveConfiguration = false;
    }

    protected override void OnCombatCleanup()
    {
        StopAllCoroutines();
        ResetPendingAttack();
        ForceCleanupActiveDrones();
    }

    protected override void OnResolvedStatsChanged(
        ResolvedTowerCombatStats previousStats,
        ResolvedTowerCombatStats currentStats,
        TowerUpgradeDefinition sourceUpgrade)
    {
        bool refreshDamage = UpgradeIncludesBasicStat(
            sourceUpgrade,
            TowerUpgradeBasicStatType.DamageBonus);
        bool refreshAttackRange = UpgradeIncludesBasicStat(
            sourceUpgrade,
            TowerUpgradeBasicStatType.AttackRange);
        bool refreshBattery = UpgradeIncludesBasicStat(
            sourceUpgrade,
            TowerUpgradeBasicStatType.DroneBatteryDuration);
        bool refreshBurstCooldown = UpgradeIncludesBasicStat(
            sourceUpgrade,
            TowerUpgradeBasicStatType.DroneBurstCooldown);
        DroneStatRefresh refresh = new DroneStatRefresh(
            refreshDamage,
            currentStats.AttackDamage,
            refreshAttackRange,
            currentStats.AttackRange,
            refreshBattery
                ? currentStats.DroneBatteryDuration - previousStats.DroneBatteryDuration
                : 0f,
            refreshBurstCooldown,
            currentStats.DroneBurstCooldown);
        List<DroneBehaviour> droneSnapshot = GetActiveDroneSnapshot();

        for (int i = 0; i < droneSnapshot.Count; i++)
        {
            DroneBehaviour drone = droneSnapshot[i];

            if (drone != null)
            {
                drone.ApplyStatRefresh(refresh);
            }
        }
    }

    protected override void OnBehaviourPackageRecorded(TowerUpgradeDefinition upgradeDefinition)
    {
        if (upgradeDefinition == null)
        {
            return;
        }

        List<DroneBehaviour> droneSnapshot = GetActiveDroneSnapshot();

        switch (upgradeDefinition.BehaviourPackageType)
        {
            case TowerBehaviourPackageType.DroneBlastRounds:
                for (int i = 0; i < droneSnapshot.Count; i++)
                {
                    DroneBehaviour drone = droneSnapshot[i];

                    if (drone != null)
                    {
                        drone.RefreshBlastRounds(
                            upgradeDefinition,
                            upgradeDefinition.BlastRoundsEffect);
                    }
                }

                List<ProjectileBehaviour> projectileSnapshot = GetOwnedProjectileSnapshot();

                for (int i = 0; i < projectileSnapshot.Count; i++)
                {
                    ProjectileBehaviour projectile = projectileSnapshot[i];

                    if (projectile != null)
                    {
                        projectile.TryRefreshBlastRounds(
                            upgradeDefinition,
                            upgradeDefinition.BlastRoundsEffect);
                    }
                }
                break;
            case TowerBehaviourPackageType.DroneFinalDive:
                for (int i = 0; i < droneSnapshot.Count; i++)
                {
                    DroneBehaviour drone = droneSnapshot[i];

                    if (drone != null)
                    {
                        drone.RefreshFinalDive(
                            upgradeDefinition,
                            upgradeDefinition.FinalDiveHitThreshold,
                            upgradeDefinition.FinalDiveExplosionEffect);
                    }
                }
                break;
        }
    }

    protected override void OnCombatUpdate()
    {
        if (IsWaitingForAnimationRelease)
        {
            return;
        }

        if (!IsAttackCycleReady || !HasOpenDroneCapacity())
        {
            SetIdle();
            return;
        }

        MonsterBehaviour target = SelectTarget();
        SetCurrentTarget(target);

        if (!IsValidTarget(target))
        {
            SetIdle();
            return;
        }

        pendingDroneTarget = target;
        SetWaitingForAnimationRelease();

        if (!SetAttackAnimatorTrigger())
        {
            ReleasePendingAttack();
        }
    }

    protected override void OnAnimationRelease()
    {
        ReleasePendingAttack();
    }

    private void ReleasePendingAttack()
    {
        if (!IsWaitingForAnimationRelease)
        {
            return;
        }

        if (!HasOpenDroneCapacity())
        {
            ResetPendingAttack();
            return;
        }

        if (!IsValidTarget(pendingDroneTarget) || !IsInAttackRange(pendingDroneTarget))
        {
            ResetPendingAttack();
            return;
        }

        if (dronePrefab == null)
        {
            if (!hasLoggedMissingDronePrefab)
            {
                hasLoggedMissingDronePrefab = true;
                Debug.LogWarning("Drone combat cannot spawn: Drone prefab is not assigned.", this);
            }

            ResetPendingAttack();
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            ResetPendingAttack();
            return;
        }

        Vector3 releasePosition = origin.position;
        Quaternion releaseRotation = origin.rotation;
        MonsterBehaviour initialTarget = pendingDroneTarget;
        ResolvedTowerCombatStats resolvedStats = ResolveCombatStats();
        DroneRuntimeOptions runtimeOptions = CreateRuntimeOptions();
        DroneReleaseData releaseData = CreateReleaseData();

        if (!TryReleaseDrone(
                releasePosition,
                releaseRotation,
                initialTarget,
                resolvedStats,
                releaseData,
                runtimeOptions))
        {
            ResetPendingAttack();
            return;
        }

        StartAttackCycle(resolvedStats.AttackCycleDuration);
        ResetPendingAttack();
    }

    private bool TryReleaseDrone(
        Vector3 releasePosition,
        Quaternion releaseRotation,
        MonsterBehaviour initialTarget,
        ResolvedTowerCombatStats resolvedStats,
        DroneReleaseData releaseData,
        DroneRuntimeOptions runtimeOptions)
    {
        GameObject droneObject = Instantiate(dronePrefab, releasePosition, releaseRotation);

        if (!droneObject.TryGetComponent(out DroneBehaviour droneBehaviour))
        {
            Debug.LogWarning(
                "Drone instance root does not have DroneBehaviour. Release was rejected.",
                droneObject);
            Destroy(droneObject);
            return false;
        }

        droneBehaviour.Initialize(
            TowerInstance,
            MonsterManager,
            releaseData,
            runtimeOptions,
            resolvedStats,
            releasePosition,
            releaseRotation,
            initialTarget);

        if (!droneBehaviour.IsInitialized)
        {
            return false;
        }

        RegisterActiveDrone(droneBehaviour);
        return true;
    }

    private DroneReleaseData CreateReleaseData()
    {
        return new DroneReleaseData(
            TargetSelectionType,
            AttackReleaseVfxPrefab);
    }

    private bool TryGetDronePrefabBehaviour(out DroneBehaviour droneBehaviour)
    {
        droneBehaviour = null;
        return dronePrefab != null && dronePrefab.TryGetComponent(out droneBehaviour);
    }

    private DroneRuntimeOptions CreateRuntimeOptions()
    {
        TowerUpgradeDefinition blastRoundsSourceUpgrade = null;
        EffectDefinition blastRoundsEffect = null;
        TowerUpgradeDefinition finalDiveSourceUpgrade = null;
        float finalDiveHitThreshold = 0f;
        EffectDefinition finalDiveExplosionEffect = null;

        if (HasBehaviourPackage(TowerBehaviourPackageType.DroneBlastRounds) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.DroneBlastRounds,
                out TowerUpgradeDefinition resolvedBlastRoundsUpgrade))
        {
            blastRoundsSourceUpgrade = resolvedBlastRoundsUpgrade;
            blastRoundsEffect = resolvedBlastRoundsUpgrade.BlastRoundsEffect;

            if (blastRoundsEffect == null && !hasLoggedInvalidBlastRoundsEffect)
            {
                hasLoggedInvalidBlastRoundsEffect = true;
                Debug.LogWarning(
                    "Drone Blast Rounds is active, but its EffectDefinition is missing.",
                    resolvedBlastRoundsUpgrade);
            }
        }

        if (HasBehaviourPackage(TowerBehaviourPackageType.DroneFinalDive) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.DroneFinalDive,
                out TowerUpgradeDefinition resolvedFinalDiveUpgrade))
        {
            float resolvedHitThreshold = resolvedFinalDiveUpgrade.FinalDiveHitThreshold;
            EffectDefinition resolvedExplosionEffect =
                resolvedFinalDiveUpgrade.FinalDiveExplosionEffect;

            if (resolvedHitThreshold > 0f && resolvedExplosionEffect != null)
            {
                finalDiveSourceUpgrade = resolvedFinalDiveUpgrade;
                finalDiveHitThreshold = resolvedHitThreshold;
                finalDiveExplosionEffect = resolvedExplosionEffect;
            }
            else if (!hasLoggedInvalidFinalDiveConfiguration)
            {
                hasLoggedInvalidFinalDiveConfiguration = true;
                Debug.LogWarning(
                    "Drone Final Dive requires a positive hit threshold and explosion EffectDefinition.",
                    resolvedFinalDiveUpgrade);
            }
        }

        return new DroneRuntimeOptions(
            blastRoundsSourceUpgrade,
            blastRoundsEffect,
            finalDiveSourceUpgrade,
            finalDiveHitThreshold,
            finalDiveExplosionEffect);
    }

    private int ResolveMaximumDroneCount()
    {
        if (!HasBehaviourPackage(TowerBehaviourPackageType.DroneMultiDrones))
        {
            return Mathf.Max(1, defaultMaximumDroneCount);
        }

        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.DroneMultiDrones,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.OverrideMaximumDroneCount
            : Mathf.Max(1, defaultMaximumDroneCount);
    }

    private bool HasOpenDroneCapacity()
    {
        return activeDrones.Count < ResolveMaximumDroneCount();
    }

    private void RegisterActiveDrone(DroneBehaviour droneBehaviour)
    {
        if (droneBehaviour == null ||
            !droneBehaviour.IsInitialized ||
            !activeDrones.Add(droneBehaviour))
        {
            return;
        }

        droneBehaviour.OnEnded += HandleDroneEnded;
        droneBehaviour.OnProjectileReleased += HandleDroneProjectileReleased;
    }

    private List<DroneBehaviour> GetActiveDroneSnapshot()
    {
        return new List<DroneBehaviour>(activeDrones);
    }

    private void HandleDroneEnded(DroneBehaviour droneBehaviour)
    {
        if (droneBehaviour == null)
        {
            return;
        }

        droneBehaviour.OnEnded -= HandleDroneEnded;
        droneBehaviour.OnProjectileReleased -= HandleDroneProjectileReleased;
        activeDrones.Remove(droneBehaviour);
    }

    private void HandleDroneProjectileReleased(ProjectileBehaviour projectileBehaviour)
    {
        RegisterOwnedProjectile(projectileBehaviour);
    }

    private void ForceCleanupActiveDrones()
    {
        if (activeDrones.Count == 0)
        {
            return;
        }

        List<DroneBehaviour> snapshot = new List<DroneBehaviour>(activeDrones);

        for (int i = 0; i < snapshot.Count; i++)
        {
            DroneBehaviour droneBehaviour = snapshot[i];

            if (droneBehaviour == null)
            {
                continue;
            }

            droneBehaviour.ForceCleanup();
            droneBehaviour.OnEnded -= HandleDroneEnded;
            droneBehaviour.OnProjectileReleased -= HandleDroneProjectileReleased;
        }

        activeDrones.Clear();
    }

    private void ResetPendingAttack()
    {
        pendingDroneTarget = null;
        SetIdle();
    }
}
