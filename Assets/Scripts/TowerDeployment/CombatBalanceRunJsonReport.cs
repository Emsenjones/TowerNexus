#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
internal sealed class CombatBalanceRunJsonReport
{
    public int schemaVersion = 20;
    public string generatedAtLocal;
    public string runLabel;
    public string terminalState;
    public string failureReason;
    public CombatBalanceFixtureJson fixture = new CombatBalanceFixtureJson();
    public CombatBalanceCombatJson combat = new CombatBalanceCombatJson();
    public CombatBalanceDamageDiagnosticsJson damageDiagnostics =
        new CombatBalanceDamageDiagnosticsJson();
    public CombatBalanceTimingJson timing = new CombatBalanceTimingJson();
    public CombatBalancePlayerJson player = new CombatBalancePlayerJson();
    public CombatBalanceProgressionJson progression =
        new CombatBalanceProgressionJson();
    public CombatBalanceWaveRuntimeJson waveRuntime =
        new CombatBalanceWaveRuntimeJson();
    public CombatBalanceDraftRuntimeJson draftRuntime =
        new CombatBalanceDraftRuntimeJson();
    public CombatBalanceIntegrityJson integrity = new CombatBalanceIntegrityJson();
    public CombatBalanceMonsterRuntimeJson monsterRuntime =
        new CombatBalanceMonsterRuntimeJson();
    public List<CombatBalanceTowerJson> towers = new List<CombatBalanceTowerJson>();
    public List<CombatBalanceBuffJson> buffs = new List<CombatBalanceBuffJson>();
    public CombatBalanceElementalOpportunityDiagnosticsJson
        elementalOpportunityDiagnostics =
            new CombatBalanceElementalOpportunityDiagnosticsJson();
    public CombatBalanceElementalHitReactionDiagnosticsJson
        elementalHitReactionDiagnostics =
            new CombatBalanceElementalHitReactionDiagnosticsJson();
}

[Serializable]
internal sealed class CombatBalanceFixtureJson
{
    public string waveConfigName;
    public bool expectedWaveCountAvailable;
    public int expectedWaveCount;
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
internal sealed class CombatBalanceDamageDiagnosticsJson
{
    public int towerScaledResolutionCount;
    public int towerScaledRejectedCount;
    public int towerScaledSuccessfulApplicationCount;
    public int towerScaledAppliedDamageTotal;
    public int fixedBuffResolutionCount;
    public int fixedBuffSuccessfulApplicationCount;
    public int fixedBuffAppliedDamageTotal;
    public float maximumTowerScaledRelativeRoundingError;
    public List<CombatBalanceTowerScaledDamageJson> towerScaledSignatures =
        new List<CombatBalanceTowerScaledDamageJson>();
    public List<CombatBalanceTowerScaledRejectionJson> towerScaledRejections =
        new List<CombatBalanceTowerScaledRejectionJson>();
    public List<CombatBalanceFixedBuffDamageJson> fixedBuffSignatures =
        new List<CombatBalanceFixedBuffDamageJson>();
}

[Serializable]
internal sealed class CombatBalanceTowerScaledDamageJson
{
    public int towerInstanceId;
    public string towerDisplayName;
    public string towerFamily;
    public int level;
    public int levelBasicDamage;
    public float rawDamageBonus;
    public float resolvedBasicDamage;
    public string damageSourceType;
    public string effectDefinitionName;
    public int actionOrdinal;
    public float damageScale;
    public float rawProduct;
    public int finalDamage;
    public float relativeRoundingError;
    public int resolutionCount;
    public int successfulApplicationCount;
    public int appliedDamageTotal;
}

[Serializable]
internal sealed class CombatBalanceTowerScaledRejectionJson
{
    public int sourceTowerInstanceId;
    public string sourceTowerName;
    public string damageSourceType;
    public string effectDefinitionName;
    public int actionOrdinal;
    public float damageScale;
    public string failureReason;
    public int rejectionCount;
}

[Serializable]
internal sealed class CombatBalanceFixedBuffDamageJson
{
    public string effectDefinitionName;
    public int actionOrdinal;
    public int fixedDamage;
    public int sourceTowerInstanceCount;
    public int resolvedTargetCount;
    public int resolutionCount;
    public int successfulApplicationCount;
    public int appliedDamageTotal;
}

[Serializable]
internal sealed class CombatBalanceTimingJson
{
    public float runDurationSeconds;
    public float spawnSpanSeconds;
    public bool spawningCompleted;
    public float spawningCompletedAtSeconds;
    public float battleDurationSeconds;
    public float secondsAfterFinalDraft;
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
    public int sourceWaveNumber;
    public int sourceWaveSpawnOrdinal;
    public string runtimeTemplateName;
    public string displayName;
    public int maximumHealth;
    public float moveSpeedAtSpawn;
    public float spawnedAtSeconds;
    public float resolvedAtSeconds;
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
internal sealed class CombatBalanceProgressionJson
{
    public List<int> requirements = new List<int>();
    public int expectedLevelUpCount;
    public int expectedTotalDraftCount;
    public int expectedFinalDraftResolutionNode;
    public bool initialDraftCompleted;
    public int observedLevelUpCount;
    public int observedTotalDraftCount;
    public int finalLevel;
    public int finalProgress;
    public int finalRequiredProgress;
    public int observedFinalDraftResolutionNode;
    public int resolutionsAfterFinalDraft;
    public List<CombatBalanceProgressionEventJson> events =
        new List<CombatBalanceProgressionEventJson>();
}

[Serializable]
internal sealed class CombatBalanceProgressionEventJson
{
    public int ordinal;
    public string kind;
    public int resolvedMonsterCount;
    public int playerLevel;
    public int currentProgress;
    public int requiredProgress;
    public float activeTimeSeconds;
    public int spawned;
    public int resolved;
    public int killed;
    public int leaked;
    public int alive;
    public int playerHealth;
}

[Serializable]
internal sealed class CombatBalanceWaveRuntimeJson
{
    public int observedWaveStartCount;
    public int observedWaveCompletionCount;
    public List<CombatBalanceWaveEventJson> events =
        new List<CombatBalanceWaveEventJson>();
    public List<CombatBalanceWaveResolutionJson> resolutionSummaries =
        new List<CombatBalanceWaveResolutionJson>();
}

[Serializable]
internal sealed class CombatBalanceWaveResolutionJson
{
    public int waveNumber;
    public string runtimeTemplateName;
    public int configuredCount;
    public int spawned;
    public int resolved;
    public int killed;
    public int leaked;
    public int unresolvedAtReport;
    public int successfulDamageApplications;
    public int effectiveDamage;
    public int leakedRemainingHealth;
    public bool hasResolvedMonsters;
    public float firstResolutionAtSeconds;
    public float lastResolutionAtSeconds;
}

[Serializable]
internal sealed class CombatBalanceWaveEventJson
{
    public int ordinal;
    public string kind;
    public int waveNumber;
    public string runtimeTemplateName;
    public int configuredCount;
    public float configuredSpawnIntervalSeconds;
    public float configuredWaveDelaySeconds;
    public float activeTimeSeconds;
    public int spawned;
    public int resolved;
    public int killed;
    public int leaked;
    public int alive;
    public int playerHealth;
}

[Serializable]
internal sealed class CombatBalanceDraftRuntimeJson
{
    public string configuredGenerationMode;
    public int configuredFixedStepCount;
    public int observedAttemptCount;
    public int committedSelectionCount;
    public List<CombatBalanceDraftItemJson> towerDraftPool =
        new List<CombatBalanceDraftItemJson>();
    public List<CombatBalanceDraftItemJson> towerUpgradeDraftPool =
        new List<CombatBalanceDraftItemJson>();
    public List<CombatBalanceDraftAttemptJson> attempts =
        new List<CombatBalanceDraftAttemptJson>();
}

[Serializable]
internal sealed class CombatBalanceDraftAttemptJson
{
    public string attemptToken;
    public int ordinal;
    public string sessionKind;
    public string generationMode;
    public int resolvedMonsterCount;
    public int playerLevel;
    public int currentProgress;
    public int requiredProgress;
    public float activeTimeSeconds;
    public bool selectionCommitted;
    public List<CombatBalanceDraftItemJson> naturalCandidates =
        new List<CombatBalanceDraftItemJson>();
    public List<CombatBalanceDraftItemJson> displayedChoices =
        new List<CombatBalanceDraftItemJson>();
    public CombatBalanceDraftItemJson selectedChoice =
        new CombatBalanceDraftItemJson();
}

[Serializable]
internal sealed class CombatBalanceDraftItemJson
{
    public string resultType;
    public string assetName;
    public string displayName;
    public string towerFamily;
    public string upgradeLayer;
    public int requiredTowerLevel;
    public int multiplicity;
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
    public bool initialDraftCountMatches;
    public bool levelUpCountMatches;
    public bool levelUpResolutionNodesMatch;
    public bool finalPlayerLevelMatches;
    public bool postFinalDraftCombatObserved;
    public bool waveEventCountsMatch;
    public bool waveMonsterAttributionMatches;
    public bool draftAttemptSelectionsMatch;
    public bool draftAttemptCountMatchesProgression;
    public bool towerDeploymentCoverageMatches;
    public bool damageDiagnosticsCountsMatch;
    public bool droneBurstDiagnosticsConsistent;
    public bool elementalOpportunityDiagnosticsConsistent;
    public bool elementalHitReactionDiagnosticsConsistent;
    public bool buffDiagnosticsConsistent;
    public bool buffStackUnitAccountingConsistent;
    public bool buffWaveAttributionMatches;
}

[Serializable]
internal sealed class CombatBalanceTowerJson
{
    public int instanceId;
    public string displayName;
    public string family;
    public int level;
    public bool deploymentObserved;
    public int deploymentOrdinal;
    public float deployedAtSeconds;
    public CombatBalanceVector3Json deploymentWorldPosition =
        new CombatBalanceVector3Json();
    public List<CombatBalanceGridPositionJson> deploymentGridPositions =
        new List<CombatBalanceGridPositionJson>();
    public bool hasCombatRuntime;
    public int baseDamage;
    public float baseRange;
    public float baseCycleSeconds;
    public int resolvedDamage;
    public float resolvedRange;
    public float resolvedCycleSeconds;
    public CombatBalanceProjectileRuntimeJson projectileRuntime =
        new CombatBalanceProjectileRuntimeJson();
    public CombatBalanceDroneBurstRuntimeJson droneBurstRuntime =
        new CombatBalanceDroneBurstRuntimeJson();
    public List<CombatBalanceUpgradeJson> upgrades =
        new List<CombatBalanceUpgradeJson>();
}

[Serializable]
internal sealed class CombatBalanceDroneBurstRuntimeJson
{
    public bool diagnosticsConsistent = true;
    public List<CombatBalanceDroneRuntimeJson> drones =
        new List<CombatBalanceDroneRuntimeJson>();
}

[Serializable]
internal sealed class CombatBalanceDroneRuntimeJson
{
    public int sourceDroneInstanceId;
    public bool isAdditionalAttackEntity;
    public int burstsStarted;
    public int openingProjectilesReleased;
    public int elementalEligibleOpeningProjectilesReleased;
    public int openingProjectileDirectHits;
    public int elementalEligibleOpeningProjectileDirectHits;
    public int openingProjectilesEndedWithoutImpact;
    public int laterProjectilesReleased;
    public int laterProjectileDirectHits;
    public int laterProjectilesEndedWithoutImpact;
    public List<CombatBalanceDroneBurstJson> bursts =
        new List<CombatBalanceDroneBurstJson>();
}

[Serializable]
internal sealed class CombatBalanceDroneBurstJson
{
    public long burstId;
    public int started;
    public int openingProjectilesReleased;
    public int elementalEligibleOpeningProjectilesReleased;
    public int openingProjectileDirectHits;
    public int elementalEligibleOpeningProjectileDirectHits;
    public int openingProjectilesEndedWithoutImpact;
    public int laterProjectilesReleased;
    public int laterProjectileDirectHits;
    public int laterProjectilesEndedWithoutImpact;
}

[Serializable]
internal sealed class CombatBalanceElementalOpportunityDiagnosticsJson
{
    public int candidateResults;
    public int eligibleResults;
    public int dispatchedRequests;
    public bool diagnosticsConsistent;
    public List<CombatBalanceElementalOpportunityScopeJson> scopes =
        new List<CombatBalanceElementalOpportunityScopeJson>();
}

[Serializable]
internal sealed class CombatBalanceElementalOpportunityScopeJson
{
    public int sourceTowerInstanceId;
    public string towerFamily;
    public string elementalUpgradeName;
    public int waveNumber;
    public string provenance;
    public string memberIdentity;
    public string resultRole;
    public int minimumResultOrdinal;
    public int maximumResultOrdinal;
    public int candidateResults;
    public int eligibleResults;
    public int dispatchedRequests;
}

[Serializable]
internal sealed class CombatBalanceElementalHitReactionDiagnosticsJson
{
    public CombatBalanceElementalHitReactionCountsJson totals =
        new CombatBalanceElementalHitReactionCountsJson();
    public bool diagnosticsConsistent;
    public List<CombatBalanceElementalHitReactionBuffJson> buffs =
        new List<CombatBalanceElementalHitReactionBuffJson>();
}

[Serializable]
internal sealed class CombatBalanceElementalHitReactionBuffJson
{
    public string definitionName;
    public string displayName;
    public string element;
    public float cooldownSeconds;
    public string damageEffectDefinitionName;
    public int damageActionOrdinal;
    public int fixedDamage;
    public CombatBalanceElementalHitReactionCountsJson counts =
        new CombatBalanceElementalHitReactionCountsJson();
    public List<CombatBalanceElementalHitReactionSourceJson> sources =
        new List<CombatBalanceElementalHitReactionSourceJson>();
    public List<CombatBalanceElementalHitReactionWaveJson> waves =
        new List<CombatBalanceElementalHitReactionWaveJson>();
}

[Serializable]
internal sealed class CombatBalanceElementalHitReactionWaveJson
{
    public int waveNumber;
    public CombatBalanceElementalHitReactionCountsJson counts =
        new CombatBalanceElementalHitReactionCountsJson();
    public List<CombatBalanceElementalHitReactionSourceJson> sources =
        new List<CombatBalanceElementalHitReactionSourceJson>();
}

[Serializable]
internal sealed class CombatBalanceElementalHitReactionSourceJson
{
    public int sourceTowerInstanceId;
    public string towerFamily;
    public string triggeringElementalUpgradeName;
    public string sourceElementRelation;
    public string damageSourceType;
    public string effectDefinitionName;
    public int actionOrdinal;
    public string provenance;
    public string memberIdentity;
    public string resultRole;
    public int minimumResultOrdinal;
    public int maximumResultOrdinal;
    public CombatBalanceElementalHitReactionCountsJson counts =
        new CombatBalanceElementalHitReactionCountsJson();
}

[Serializable]
internal sealed class CombatBalanceElementalHitReactionCountsJson
{
    public int observedReactionOpportunities;
    public int evaluatedReactionOpportunities;
    public int triggeredReactions;
    public int blockedByReactionCooldown;
    public int noValidReactionTarget;
    public int invalidatedReactionOpportunities;
    public int ownerResolvedByEarlierElementalReaction;
    public int buffInstanceOrCycleChanged;
    public int reactionTargetInvalidBeforeCommit;
    public int reactionCommitRejected;
    public int successfulReactionDamageTargets;
    public int totalReactionFixedDamage;
    public int reactionGapSampleCount;
    public CombatBalanceMetricJson reactionGapSeconds =
        new CombatBalanceMetricJson();
}

[Serializable]
internal sealed class CombatBalanceVector3Json
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
internal sealed class CombatBalanceGridPositionJson
{
    public int x;
    public int z;
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
    public int elementalStackContribution;
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
    public int requestedStackUnits;
    public int appliedStackUnits;
    public int discardedStackUnits;
    public int maximumRequestedStackUnitsBySingleApplication;
    public int maximumAppliedStackUnitsBySingleApplication;
    public int stackingCyclesStarted;
    public int stackingCyclesOverloaded;
    public int stackingCyclesNaturallyExpired;
    public int distinctMonstersOverloaded;
    public int reentriesAfterProtection;
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
    public CombatBalanceMetricJson firstApplicationToOverloadSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson successfulApplicationGapSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson protectionExpiryToReapplicationSeconds =
        new CombatBalanceMetricJson();
    public List<CombatBalanceStackCountOccurrenceJson>
        naturalExpiryStackCounts =
            new List<CombatBalanceStackCountOccurrenceJson>();
    public List<CombatBalanceSourceCountOccurrenceJson>
        overloadSourceCounts =
            new List<CombatBalanceSourceCountOccurrenceJson>();
    public List<CombatBalanceBuffSourceJson> sources =
        new List<CombatBalanceBuffSourceJson>();
    public List<CombatBalanceBuffWaveJson> waveSummaries =
        new List<CombatBalanceBuffWaveJson>();
}

[Serializable]
internal sealed class CombatBalanceBuffWaveJson
{
    public int waveNumber;
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
    public int requestedStackUnits;
    public int appliedStackUnits;
    public int discardedStackUnits;
    public int maximumRequestedStackUnitsBySingleApplication;
    public int maximumAppliedStackUnitsBySingleApplication;
    public int stackingCyclesStarted;
    public int stackingCyclesOverloaded;
    public int stackingCyclesNaturallyExpired;
    public int distinctMonstersOverloaded;
    public int reentriesAfterProtection;
    public int overloads;
    public int enteredProtection;
    public int periodicTicks;
    public int naturalExpiriesBeforeOverload;
    public int protectionExpiries;
    public int monsterKilledRemovals;
    public int monsterLeakedRemovals;
    public CombatBalanceMetricJson firstApplicationToOverloadSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson successfulApplicationGapSeconds =
        new CombatBalanceMetricJson();
    public CombatBalanceMetricJson protectionExpiryToReapplicationSeconds =
        new CombatBalanceMetricJson();
    public List<CombatBalanceStackCountOccurrenceJson>
        naturalExpiryStackCounts =
            new List<CombatBalanceStackCountOccurrenceJson>();
    public List<CombatBalanceSourceCountOccurrenceJson>
        overloadSourceCounts =
            new List<CombatBalanceSourceCountOccurrenceJson>();
    public List<CombatBalanceBuffSourceJson> sources =
        new List<CombatBalanceBuffSourceJson>();
}

[Serializable]
internal sealed class CombatBalanceStackCountOccurrenceJson
{
    public int stackCount;
    public int occurrences;
}

[Serializable]
internal sealed class CombatBalanceSourceCountOccurrenceJson
{
    public int sourceCount;
    public int occurrences;
}

[Serializable]
internal sealed class CombatBalanceBuffParametersJson
{
    public bool usesStacks;
    public float activeDurationSeconds;
    public float periodicTickIntervalSeconds;
    public int maximumStacks;
    public float sourceApplyCooldownSeconds;
    public float towerHitReactionCooldownSeconds;
    public float overloadProtectionDurationSeconds;
    public bool moveSpeedMultiplierAvailable;
    public float moveSpeedMultiplier;
}

[Serializable]
internal sealed class CombatBalanceBuffSourceJson
{
    public int sourceTowerInstanceId;
    public string towerFamily;
    public string elementalUpgradeName;
    public int applicationAttempts;
    public int successfulApplications;
    public int applied;
    public int refreshed;
    public int stacked;
    public int requestedStackUnits;
    public int appliedStackUnits;
    public int discardedStackUnits;
    public int maximumRequestedStackUnitsBySingleApplication;
    public int maximumAppliedStackUnitsBySingleApplication;
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
