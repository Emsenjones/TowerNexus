#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
internal sealed class CombatBalanceRunJsonReport
{
    public int schemaVersion = 8;
    public string generatedAtLocal;
    public string runLabel;
    public string terminalState;
    public string failureReason;
    public CombatBalanceFixtureJson fixture = new CombatBalanceFixtureJson();
    public CombatBalanceCombatJson combat = new CombatBalanceCombatJson();
    public CombatBalanceTimingJson timing = new CombatBalanceTimingJson();
    public CombatBalancePlayerJson player = new CombatBalancePlayerJson();
    public CombatBalanceIntegrityJson integrity = new CombatBalanceIntegrityJson();
    public CombatBalanceMonsterRuntimeJson monsterRuntime =
        new CombatBalanceMonsterRuntimeJson();
    public List<CombatBalanceTowerJson> towers = new List<CombatBalanceTowerJson>();
    public List<CombatBalanceBuffJson> buffs = new List<CombatBalanceBuffJson>();
}

[Serializable]
internal sealed class CombatBalanceFixtureJson
{
    public string waveConfigName;
    public bool expectedMonsterCountAvailable;
    public int expectedMonsterCount;
    public int observedMinimumMonsterHealth;
    public int observedMaximumMonsterHealth;
    public float observedMinimumMonsterSpeed;
    public float observedMaximumMonsterSpeed;
    public float observedAverageSpawnIntervalSeconds;
}

[Serializable]
internal sealed class CombatBalanceCombatJson
{
    public int spawned;
    public int resolved;
    public int killed;
    public int leaked;
    public int unresolved;
    public int notSpawned;
    public float killRate;
    public int effectiveDamage;
    public int totalObservedHealth;
    public float damageCoverage;
    public int leakedRemainingHealth;
    public float averageLeakedRemainingHealth;
    public int peakAlive;
}

[Serializable]
internal sealed class CombatBalanceTimingJson
{
    public float spawnSpanSeconds;
    public bool spawningCompleted;
    public float spawningCompletedAtSeconds;
    public float battleDurationSeconds;
}

[Serializable]
internal sealed class CombatBalanceMonsterRuntimeJson
{
    public int observedTypes;
    public int instanceSamples;
    public int registrationObservedInstances;
    public int fallbackObservedInstances;
    public int instancesObservedAtFullHealth;
    public int instancesObservedAfterDamage;
    public int unobservedDamageAtObservationStart;
    public int successfulDamageApplications;
    public List<CombatBalanceMonsterTypeJson> types =
        new List<CombatBalanceMonsterTypeJson>();
    public List<CombatBalanceMonsterInstanceJson> instances =
        new List<CombatBalanceMonsterInstanceJson>();
}

[Serializable]
internal sealed class CombatBalanceMonsterTypeJson
{
    public string runtimeTemplateName;
    public string displayName;
    public int maximumHealth;
    public float moveSpeedAtSpawn;
    public int spawned;
    public int resolved;
    public int killed;
    public int leaked;
    public int unresolvedAtReport;
    public int registrationObservedInstances;
    public int fallbackObservedInstances;
    public int instancesObservedAtFullHealth;
    public int instancesObservedAfterDamage;
    public int unobservedDamageAtObservationStart;
    public int successfulDamageApplications;
    public int effectiveDamage;
    public int leakedRemainingHealth;
    public CombatBalanceMetricJson resolutionLifetimeSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson killedLifetimeSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson leakedLifetimeSeconds =
        new CombatBalanceMetricJson();
}

[Serializable]
internal sealed class CombatBalanceMonsterInstanceJson
{
    public int spawnOrdinal;
    public string runtimeTemplateName;
    public string displayName;
    public int maximumHealth;
    public float moveSpeedAtSpawn;
    public bool observedThroughRegistrationEvent;
    public int healthAtObservationStart;
    public int unobservedDamageAtObservationStart;
    public string resolutionType;
    public int finalHealth;
    public int successfulDamageApplications;
    public int effectiveDamage;
    public float lifetimeSeconds;
}

[Serializable]
internal sealed class CombatBalancePlayerJson
{
    public int initialHealth;
    public int finalHealth;
    public int maximumHealth;
}

[Serializable]
internal sealed class CombatBalanceIntegrityJson
{
    public bool resolutionCountsMatch;
    public bool leakCountMatchesPlayerHealthLoss;
    public bool monsterRuntimeCountsMatch;
    public bool monsterRuntimeDamageMatches;
    public bool monsterRuntimeRegistrationCoverageMatch;
    public bool monsterRuntimeStartedAtFullHealth;
}

[Serializable]
internal sealed class CombatBalanceTowerJson
{
    public int instanceId;
    public string displayName;
    public string family;
    public int level;
    public bool hasCombatRuntime;
    public int baseDamage;
    public float baseRange;
    public float baseCycleSeconds;
    public int resolvedDamage;
    public float resolvedRange;
    public float resolvedCycleSeconds;
    public CombatBalanceProjectileRuntimeJson projectileRuntime =
        new CombatBalanceProjectileRuntimeJson();
    public List<CombatBalanceUpgradeJson> upgrades =
        new List<CombatBalanceUpgradeJson>();
}

[Serializable]
internal sealed class CombatBalanceProjectileRuntimeJson
{
    public int projectilesReleased;
    public int initialProjectilesReleased;
    public int childProjectilesReleased;
    public int arcProjectilesReleased;
    public int arcTargetResolvedImpacts;
    public int arcIntendedTargetImpacts;
    public int arcFallbackTargetImpacts;
    public int arcPositionOnlyImpacts;
    public int arcPositionOnlyIntendedInvalid;
    public int arcPositionOnlyIntendedOutOfRange;
    public int arcPositionOnlyWithoutIntendedTarget;
    public int arcEndedWithoutImpact;
    public int arcUnresolvedAtReport;
    public float arcTargetResolutionRate;
    public CombatBalanceArcTargetRelationJson arcTargetRelation =
        new CombatBalanceArcTargetRelationJson();
}

[Serializable]
internal sealed class CombatBalanceArcTargetRelationJson
{
    public int sampledInitialImpacts;
    public int primaryIntendedTargetImpacts;
    public int primaryFallbackTargetImpacts;
    public int primaryPositionOnlyImpacts;
    public int additionalIntendedTargetImpacts;
    public int additionalFallbackTargetImpacts;
    public int additionalPositionOnlyImpacts;
    public int fallbackImmediateLaterSpawnImpacts;
    public int fallbackImmediateEarlierSpawnImpacts;
    public int fallbackNonAdjacentImpacts;
    public int fallbackUnknownRelationImpacts;
    public int positionOnlyNearestOtherAvailable;
    public int positionOnlyNearestOtherImmediateLaterSpawn;
    public int positionOnlyNearestOtherImmediateEarlierSpawn;
    public int positionOnlyNearestOtherNonAdjacent;
    public int positionOnlyNearestOtherUnknownRelation;
    public int positionOnlyWithoutNearestOther;
    public float observedMinimumHitDistanceThreshold;
    public float observedMaximumHitDistanceThreshold;
    public CombatBalanceMetricJson confirmationToReleaseSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson releaseToImpactSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson plannedTravelTimeSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson landingToIntendedDistanceAtRelease =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson intendedMoveSpeedAtRelease =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson landingToIntendedDistanceAtImpact =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson intendedMoveSpeedAtImpact =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson landingToFallbackDistanceAtImpact =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson fallbackMoveSpeedAtImpact =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson landingToPositionOnlyNearestOtherDistance =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson positionOnlyNearestOtherMoveSpeedAtImpact =
        new CombatBalanceMetricJson();
    public List<CombatBalanceArcTargetRelationSampleJson> samples =
        new List<CombatBalanceArcTargetRelationSampleJson>();
}

[Serializable]
internal sealed class CombatBalanceMetricJson
{
    public int samples;
    public float minimum;
    public float average;
    public float maximum;
}

[Serializable]
internal sealed class CombatBalanceArcTargetRelationSampleJson
{
    public string memberType;
    public string resolutionType;
    public int intendedSpawnOrdinal;
    public int resolvedSpawnOrdinal;
    public int nearestOtherSpawnOrdinal;
    public int resolvedOrdinalDelta;
    public int nearestOtherOrdinalDelta;
    public float confirmationToReleaseSeconds;
    public float releaseToImpactSeconds;
    public float plannedTravelTimeSeconds;
    public float hitDistanceThreshold;
    public bool hasIntendedAtRelease;
    public float landingToIntendedDistanceAtRelease;
    public float intendedMoveSpeedAtRelease;
    public bool hasIntendedAtImpact;
    public float landingToIntendedDistanceAtImpact;
    public float intendedMoveSpeedAtImpact;
    public bool hasResolvedTargetAtImpact;
    public float landingToResolvedTargetDistanceAtImpact;
    public float resolvedTargetMoveSpeedAtImpact;
    public bool hasNearestOtherTargetAtImpact;
    public float landingToNearestOtherTargetDistanceAtImpact;
    public float nearestOtherTargetMoveSpeedAtImpact;
}

[Serializable]
internal sealed class CombatBalanceUpgradeJson
{
    public string displayName;
    public string layer;
    public int requiredLevel;
    public string behaviourPackage;
    public string element;
}

[Serializable]
internal sealed class CombatBalanceBuffJson
{
    public string definitionName;
    public string displayName;
    public string element;
    public CombatBalanceBuffParametersJson parameters =
        new CombatBalanceBuffParametersJson();
    public List<string> sourceElements = new List<string>();
    public int applicationAttempts;
    public int applied;
    public int refreshed;
    public int stacked;
    public int invalid;
    public int blockedBySourceCooldown;
    public int blockedByProtection;
    public int distinctMonsters;
    public int distinctSourceTowers;
    public int maximumObservedStacks;
    public int overloads;
    public int enteredProtection;
    public int periodicTicks;
    public int naturalExpiriesBeforeOverload;
    public int protectionExpiries;
    public int explicitRemovals;
    public int monsterKilledRemovals;
    public int monsterLeakedRemovals;
    public int technicalCleanupRemovals;
    public int runtimeResetRemovals;
    public int overloadWithoutProtectionRemovals;
    public float averageFirstApplicationToOverloadSeconds;
    public List<CombatBalanceBuffSourceJson> sources =
        new List<CombatBalanceBuffSourceJson>();
}

[Serializable]
internal sealed class CombatBalanceBuffParametersJson
{
    public bool usesStacks;
    public float activeDurationSeconds;
    public float periodicTickIntervalSeconds;
    public int maximumStacks;
    public float sourceApplyCooldownSeconds;
    public float overloadProtectionDurationSeconds;
    public bool moveSpeedMultiplierAvailable;
    public float moveSpeedMultiplier;
}

[Serializable]
internal sealed class CombatBalanceBuffSourceJson
{
    public string towerFamily;
    public int applicationAttempts;
    public int successfulApplications;
    public int applied;
    public int refreshed;
    public int stacked;
    public int invalid;
    public int blockedBySourceCooldown;
    public int blockedByProtection;
    public int overloads;
    public int enteredProtection;
    public int periodicTicks;
    public int naturalExpiriesBeforeOverload;
    public float averageFirstApplicationToOverloadSeconds;
}
#endif
