#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
internal sealed class CombatBalanceRunJsonReport
{
    public int schemaVersion = 27;
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
    public CombatBalanceInvestmentRuntimeJson investmentRuntime =
        new CombatBalanceInvestmentRuntimeJson();
    public CombatBalanceExecutionJson execution =
        new CombatBalanceExecutionJson();
    public CombatBalanceIntegrityJson integrity = new CombatBalanceIntegrityJson();
    public CombatBalanceMonsterRuntimeJson monsterRuntime =
        new CombatBalanceMonsterRuntimeJson();
    public CombatBalancePlacementRouteRuntimeJson placementRouteRuntime =
        new CombatBalancePlacementRouteRuntimeJson();
    public List<CombatBalanceTowerJson> towers = new List<CombatBalanceTowerJson>();
    public List<CombatBalanceTowerWaveJson> towerWaveSummaries =
        new List<CombatBalanceTowerWaveJson>();
    public CombatBalanceTowerRouteDamageCoverageRuntimeJson
        towerRouteDamageCoverage =
            new CombatBalanceTowerRouteDamageCoverageRuntimeJson();
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
    public string stageDefinitionName;
    public string stageDisplayName;
    public int configuredPlayerMaxHealth;
    public string mapTemplateName;
    public string runtimeMapName;
    public int mapWidth;
    public int mapLength;
    public float mapNodeSize;
    public List<CombatBalanceMapNodeFixtureJson> mapNodes =
        new List<CombatBalanceMapNodeFixtureJson>();
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
    public List<CombatBalanceWaveFixtureJson> waves =
        new List<CombatBalanceWaveFixtureJson>();
    public string placementRouteForcedRelocationExpectation;
}

[Serializable]
internal sealed class CombatBalancePlacementRouteRuntimeJson
{
    public string forcedRelocationExpectation;
    public int committedPlacementCount;
    public int observedMonsterRevisionCount;
    public int alreadyOnNewRouteCount;
    public int reachableRouteRejoinCount;
    public int forcedRelocationCount;
    public int lifecycleObservationCount;
    public List<CombatBalancePlacementRouteCommitJson> commits =
        new List<CombatBalancePlacementRouteCommitJson>();
    public List<CombatBalancePlacementRouteLifecycleJson> lifecycle =
        new List<CombatBalancePlacementRouteLifecycleJson>();
}

[Serializable]
internal sealed class CombatBalancePlacementRouteCommitJson
{
    public int ordinal;
    public int towerInstanceId;
    public float activeTimeSeconds;
    public List<CombatBalanceGridPositionJson> footprint =
        new List<CombatBalanceGridPositionJson>();
    public List<CombatBalanceGridPositionJson> authoritativeRoute =
        new List<CombatBalanceGridPositionJson>();
    public int playerHealthBefore;
    public int playerHealthAfter;
    public int playerProgressBefore;
    public int playerProgressAfter;
    public int aliveMonsterCountBefore;
    public int aliveMonsterCountAfter;
    public int resolvedMonsterCountBefore;
    public int resolvedMonsterCountAfter;
    public int alreadyOnNewRouteCount;
    public int reachableRouteRejoinCount;
    public int forcedRelocationCount;
    public string combatOwnershipFingerprintBefore;
    public string combatOwnershipFingerprintAfter;
    public List<CombatBalancePlacementRouteMonsterJson> monsters =
        new List<CombatBalancePlacementRouteMonsterJson>();
}

[Serializable]
internal sealed class CombatBalancePlacementRouteMonsterJson
{
    public long revisionId;
    public int monsterInstanceId;
    public int spawnOrdinal;
    public int sourceWaveNumber;
    public int sourceWaveSpawnOrdinal;
    public string mode;
    public string forcedRelocationReason;
    public CombatBalanceVector3Json capturedWorldPosition =
        new CombatBalanceVector3Json();
    public CombatBalanceVector3Json immediatePostCommitPosition =
        new CombatBalanceVector3Json();
    public CombatBalanceVector3Json preparedWorldPosition =
        new CombatBalanceVector3Json();
    public bool hasComparableCapturedPosition;
    public bool hasComparableImmediateDisplacement;
    public float immediateDisplacement;
    public bool hasComparableRelocationDistance;
    public float plannedRelocationDistance;
    public float plannedConnectorDistance;
    public bool requiresExactTargetApproach;
    public bool requiresConnector;
    public bool joinedAtCommit;
    public CombatBalanceGridPositionJson physicalGrid;
    public CombatBalanceGridPositionJson joinGrid;
    public CombatBalanceGridPositionJson recoveryGrid;
    public List<CombatBalanceGridPositionJson> connectorPath =
        new List<CombatBalanceGridPositionJson>();
    public List<CombatBalanceGridPositionJson> preparedContinuation =
        new List<CombatBalanceGridPositionJson>();
    public List<CombatBalanceGridPositionJson> routeSuffix =
        new List<CombatBalanceGridPositionJson>();
    public string preGameplayStateFingerprint;
    public string postGameplayStateFingerprint;
}

[Serializable]
internal sealed class CombatBalancePlacementRouteLifecycleJson
{
    public long revisionId;
    public long replacementRevisionId;
    public int monsterInstanceId;
    public int spawnOrdinal;
    public string kind;
    public string resolutionReason;
    public bool joinedAtCommit;
    public float activeTimeSeconds;
}

[Serializable]
internal sealed class CombatBalanceMapNodeFixtureJson
{
    public int x;
    public int z;
    public string nodeType;
    public bool baseWalkable;
}

[Serializable]
internal sealed class CombatBalanceWaveFixtureJson
{
    public int waveNumber;
    public string runtimeTemplateName;
    public string monsterDisplayName;
    public int maximumHealth;
    public float moveSpeed;
    public int count;
    public float spawnIntervalSeconds;
    public float waveDelaySeconds;
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
    public bool finalBuildCommitObserved;
    public float finalBuildCommittedAtSeconds;
    public float secondsAfterFinalBuildCommit;
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
    public int configuredChoiceCount;
    public float towerDraftSlotProbability;
    public int draftSeed;
    public bool fixedDraftSeedEnabled;
    public string generationContractVersion;
    public string draftRandomAlgorithmVersion;
    public int initialFreeRerolls;
    public int remainingFreeRerolls;
    public int rerollRequestStartCount;
    public List<long> startedRerollRequestIds = new List<long>();
    public int successfulRerollCount;
    public int initialRerollCount;
    public int levelUpRerollCount;
    public int initialOriginalChoiceExposureCount;
    public int levelUpOriginalChoiceExposureCount;
    public int initialRerollChoiceExposureCount;
    public int levelUpRerollChoiceExposureCount;
    public int originalChoiceExposureCount;
    public int rerollChoiceExposureCount;
    public List<CombatBalanceRerollRequestJson> rerollRequests = new List<CombatBalanceRerollRequestJson>();
    public int observedAttemptCount;
    public int committedSelectionCount;
    public List<CombatBalanceDraftItemJson> towerDraftPool =
        new List<CombatBalanceDraftItemJson>();
    public List<CombatBalanceDraftItemJson> towerUpgradeDraftPool =
        new List<CombatBalanceDraftItemJson>();
    public List<CombatBalanceDraftAttemptJson> attempts =
        new List<CombatBalanceDraftAttemptJson>();
    public List<CombatBalancePendingDraftJson> terminalPendingDrafts =
        new List<CombatBalancePendingDraftJson>();
}

[Serializable]
internal class CombatBalanceDraftChoiceSetJson
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
    public List<string> requestedCategories = new List<string>();
    public int requestedTowerCount;
    public int requestedUpgradeCount;
    public int availableDistinctTowerCount;
    public int availableDistinctUpgradeCount;
    public int realizedTowerCount;
    public int realizedUpgradeCount;
    public int towerSlotsBackfilledByUpgrade;
    public int upgradeSlotsBackfilledByTower;
    public string backfillReason;
    public List<CombatBalanceDraftItemJson> naturalCandidates =
        new List<CombatBalanceDraftItemJson>();
    public List<CombatBalanceDraftItemJson> displayedChoices =
        new List<CombatBalanceDraftItemJson>();
    public long requestId;
    public int revision;
    public int budgetBefore;
    public int budgetAfter;
}

[Serializable]
internal sealed class CombatBalanceDraftAttemptJson
{
    public string attemptToken;
    public int ordinal;
    public string sessionKind;
    public string generationMode;
    public List<CombatBalanceDraftChoiceSetJson> choiceSets = new List<CombatBalanceDraftChoiceSetJson>();
    public int selectedSetRevision;
    public bool selectionAttempted;
    public bool heldItemCreationSucceeded;
    public string heldItemCreationFailureReason;
    public bool selectionCommitted;
    public float selectionCommittedAtSeconds;
    public CombatBalanceDraftItemJson selectedChoice =
        new CombatBalanceDraftItemJson();
    public string consumptionStatus;
}

[Serializable]
internal sealed class CombatBalanceRerollRequestJson
{
    public long requestId;
    public bool committed;
    public bool samplingStarted;
    public int committedRevision;
    public int budgetBefore;
    public int budgetAfter;
    public string presentation;
    public string attemptToken;
    public int requestedRevision;
    public string result;
    public string failureReason;
    public float activeTimeSeconds;
}

[Serializable]
internal sealed class CombatBalancePendingDraftJson
{
    public string draftAttemptToken;
    public CombatBalanceDraftItemJson item = new CombatBalanceDraftItemJson();
}

[Serializable]
internal sealed class CombatBalanceInvestmentRuntimeJson
{
    public int committedInvestmentCount;
    public bool finalBuildCommitObserved;
    public int finalBuildCommitResolutionNode;
    public int resolutionsAfterFinalBuildCommit;
    public List<CombatBalanceInvestmentCommitJson> commits =
        new List<CombatBalanceInvestmentCommitJson>();
}

[Serializable]
internal sealed class CombatBalanceInvestmentCommitJson
{
    public int ordinal;
    public string kind;
    public string authoritySource;
    public string draftAttemptToken;
    public int draftOrdinal;
    public string draftResultType;
    public string draftAssetName;
    public int towerInstanceId;
    public string towerDisplayName;
    public string towerFamily;
    public int previousLevel;
    public int currentLevel;
    public string upgradeLayer;
    public float activeTimeSeconds;
    public int resolvedMonsterCount;
    public int spawned;
    public int resolved;
    public int killed;
    public int leaked;
    public int alive;
    public int playerHealth;
}

[Serializable]
internal sealed class CombatBalanceExecutionJson
{
    public bool initialDraftCompleted;
    public bool allExpectedLevelUpsObserved;
    public bool expectedFinalPlayerLevelReached;
    public bool allConfiguredWavesStarted;
    public bool allConfiguredWavesCompleted;
    public bool allConfiguredMonstersSpawned;
    public bool finalBuildCommitted;
    public bool postFinalBuildCombatObserved;
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
    public bool fixtureSnapshotComplete;
    public bool resolutionCountsMatch;
    public bool leakCountMatchesPlayerHealthLoss;
    public bool monsterRuntimeCountsMatch;
    public bool monsterRuntimeDamageMatches;
    public bool monsterRuntimeRegistrationCoverageMatch;
    public bool monsterRuntimeStartedAtFullHealth;
    public bool levelUpResolutionNodesMatch;
    public bool waveMonsterAttributionMatches;
    public bool draftAttemptSelectionsMatch;
    public bool draftAttemptCountMatchesProgression;
    public bool draftGenerationTraceConsistent;
    public bool draftConsumptionReconciled;
    public bool towerDeploymentCoverageMatches;
    public bool investmentCommitsMatchDraftSelections;
    public bool towerWaveAttributionMatches;
    public bool towerRouteDamageCoverageMatches;
    public bool damageDiagnosticsCountsMatch;
    public bool droneBurstDiagnosticsConsistent;
    public bool droneLifecycleDiagnosticsConsistent;
    public bool elementalOpportunityDiagnosticsConsistent;
    public bool elementalHitReactionDiagnosticsConsistent;
    public bool buffDiagnosticsConsistent;
    public bool buffStackUnitAccountingConsistent;
    public bool buffWaveAttributionMatches;
    public bool placementRouteBatchCountsMatch;
    public bool placementRouteCommitStatePreserved;
    public bool placementRouteTopologyValid;
    public bool placementRouteGameplayStatePreserved;
    public bool placementRouteCombatOwnershipPreserved;
    public bool placementRouteLifecycleConsistent;
    public bool placementRouteForcedRelocationUsageValid;
}

[Serializable]
internal sealed class CombatBalanceTowerWaveJson
{
    public int towerInstanceId;
    public string towerDisplayName;
    public string towerFamily;
    public int waveNumber;
    public int successfulDamageApplications;
    public int effectiveTowerScaledDamage;
    public int killingBlows;
}

[Serializable]
internal sealed class CombatBalanceTowerRouteDamageCoverageRuntimeJson
{
    public int observedTowerCount;
    public int successfulDamageApplications;
    public int locatedDamageApplications;
    public int unresolvedRouteCellApplicationCount;
    public int effectiveDamage;
    public int locatedEffectiveDamage;
    public int unresolvedRouteCellEffectiveDamage;
    public int killingBlows;
    public int locatedKillingBlows;
    public int unresolvedRouteCellKillingBlows;
    public List<CombatBalanceTowerRouteDamageCoverageJson> towers =
        new List<CombatBalanceTowerRouteDamageCoverageJson>();
    public List<CombatBalanceTowerRouteDamageOverlapJson> overlaps =
        new List<CombatBalanceTowerRouteDamageOverlapJson>();
}

[Serializable]
internal sealed class CombatBalanceTowerRouteDamageCoverageJson
{
    public int towerInstanceId;
    public int towerDeploymentOrdinal;
    public string towerDisplayName;
    public string towerFamily;
    public int successfulDamageApplications;
    public int locatedDamageApplications;
    public int unresolvedRouteCellApplicationCount;
    public int effectiveDamage;
    public int locatedEffectiveDamage;
    public int unresolvedRouteCellEffectiveDamage;
    public int killingBlows;
    public int locatedKillingBlows;
    public int unresolvedRouteCellKillingBlows;
    public int distinctDamagedMonsterCount;
    public int distinctDamageRouteCellCount;
    public List<CombatBalanceTowerRouteDamageCellJson> routeCells =
        new List<CombatBalanceTowerRouteDamageCellJson>();
}

[Serializable]
internal sealed class CombatBalanceTowerRouteDamageCellJson
{
    public int placementCommitOrdinal;
    public int x;
    public int z;
    public int successfulDamageApplications;
    public int effectiveDamage;
    public int killingBlows;
    public int distinctDamagedMonsterCount;
}

[Serializable]
internal sealed class CombatBalanceTowerRouteDamageOverlapJson
{
    public int towerAInstanceId;
    public int towerADeploymentOrdinal;
    public string towerADisplayName;
    public string towerAFamily;
    public int towerBInstanceId;
    public int towerBDeploymentOrdinal;
    public string towerBDisplayName;
    public string towerBFamily;
    public int sharedDamageRouteCellCount;
    public int sharedRoutePhaseCellCount;
    public int sharedDamagedMonsterCount;
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
    public CombatBalanceDroneLifecycleRuntimeJson droneLifecycleRuntime =
        new CombatBalanceDroneLifecycleRuntimeJson();
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
internal sealed class CombatBalanceDroneLifecycleRuntimeJson
{
    public bool diagnosticsConsistent = true;
    public List<CombatBalanceDroneLifecycleJson> drones =
        new List<CombatBalanceDroneLifecycleJson>();
}

[Serializable]
internal sealed class CombatBalanceDroneLifecycleJson
{
    public int sourceDroneInstanceId;
    public bool isAdditionalAttackEntity;
    public int initializedCount;
    public int launchCompletedCount;
    public int invalidTargetLossCount;
    public int outOfRangeTargetLossCount;
    public int immediateRetargetCount;
    public int holdingEntryCount;
    public int holdingExitCount;
    public int reacquisitionCount;
    public int orbitEntryCompletedCount;
    public int batteryDepletedCount;
    public int finalDiveEnteredCount;
    public int finalDiveCompletedCount;
    public int completionCount;
    public float activeTimeSeconds;
    public float holdingTimeSeconds;
    public string completionReason;
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
