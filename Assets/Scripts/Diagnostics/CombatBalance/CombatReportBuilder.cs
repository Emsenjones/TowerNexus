#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using static CombatReportIntegrity;
internal sealed class CombatReportBuilder
{
    private readonly CombatRecordingSession state;
    internal CombatReportBuilder(CombatRecordingSession state) { this.state = state; }
    internal CombatBalanceRunJsonReport Build(
        string terminalState,
        string failureReason,
        DateTime generatedAt)
    {
        if (!state.IsFrozen || !state.terminalCaptured) throw new InvalidOperationException("Report input is not frozen.");
        int unresolvedCount = Mathf.Max(0, state.spawnedCount - state.resolvedCount);
        int notSpawnedCount = state.hasExpectedMonsterCount
            ? Mathf.Max(0, state.expectedMonsterCount - state.spawnedCount)
            : 0;
        float killRate = state.spawnedCount > 0
            ? (float)state.killedCount / state.spawnedCount
            : 0f;
        float averageObservedSpawnInterval = state.observedSpawnIntervalCount > 0
            ? state.observedSpawnIntervalTotal / state.observedSpawnIntervalCount
            : 0f;
        float damageCoverage = state.observedTotalMonsterMaxHealth > 0
            ? (float)state.effectiveDamage / state.observedTotalMonsterMaxHealth
            : 0f;
        float averageLeakedRemainingHealth = state.leakedCount > 0
            ? (float)state.leakedRemainingHealth / state.leakedCount
            : 0f;
        float battleDuration = state.hasObservedFirstSpawn
            ? Mathf.Max(
                0f,
                (state.lastResolutionTime > 0f ? state.lastResolutionTime : state.ObservedTime) -
                state.firstSpawnTime)
            : 0f;
        float spawnSpan = state.hasObservedFirstSpawn
            ? Mathf.Max(0f, state.lastSpawnTime - state.firstSpawnTime)
            : 0f;
        float spawningCompletionOffset =
            state.hasObservedFirstSpawn && state.hasObservedSpawningCompletion
                ? Mathf.Max(0f, state.spawningCompletedTime - state.firstSpawnTime)
                : 0f;
        int observedPlayerHealthLoss = (state.terminalCaptured || state.playerSystem != null)
            ? Mathf.Max(0, state.initialPlayerHealth - (state.terminalCaptured ? state.terminalHealth : state.playerSystem.CurrentHealth))
            : 0;

        CombatBalanceRunJsonReport report = new CombatBalanceRunJsonReport
        {
            generatedAtLocal = generatedAt.ToString(
                "yyyy-MM-dd'T'HH:mm:sszzz",
                CultureInfo.InvariantCulture),
            runLabel = state.ResolveRunName(generatedAt),
            terminalState = terminalState,
            failureReason = failureReason ?? string.Empty
        };

        report.fixture = state.fixtureSnapshot ?? new CombatBalanceFixtureJson();
        report.fixture.waveConfigName = string.IsNullOrWhiteSpace(
            report.fixture.waveConfigName)
                ? state.waveConfigName ?? string.Empty
                : report.fixture.waveConfigName;
        report.fixture.expectedWaveCountAvailable = state.hasExpectedWaveCount;
        report.fixture.expectedWaveCount = state.expectedWaveCount;
        report.fixture.expectedMonsterCountAvailable =
            state.hasExpectedMonsterCount;
        report.fixture.expectedMonsterCount = state.expectedMonsterCount;
        report.fixture.placementRouteForcedRelocationExpectation =
            state.placementRouteForcedRelocationExpectation.ToString();
        report.fixture.observedMinimumMonsterHealth =
            state.observedMinimumMonsterHealth == int.MaxValue
                ? 0
                : state.observedMinimumMonsterHealth;
        report.fixture.observedMaximumMonsterHealth = state.observedMaximumMonsterHealth;
        report.fixture.observedMinimumMonsterSpeed =
            float.IsPositiveInfinity(state.observedMinimumMonsterSpeed)
                ? 0f
                : state.observedMinimumMonsterSpeed;
        report.fixture.observedMaximumMonsterSpeed = state.observedMaximumMonsterSpeed;
        report.fixture.observedAverageSpawnIntervalSeconds =
            averageObservedSpawnInterval;

        report.combat.spawned = state.spawnedCount;
        report.combat.resolved = state.resolvedCount;
        report.combat.killed = state.killedCount;
        report.combat.leaked = state.leakedCount;
        report.combat.unresolved = unresolvedCount;
        report.combat.notSpawned = notSpawnedCount;
        report.combat.killRate = killRate;
        report.combat.effectiveDamage = state.effectiveDamage;
        report.combat.totalObservedHealth = state.observedTotalMonsterMaxHealth;
        report.combat.damageCoverage = damageCoverage;
        report.combat.leakedRemainingHealth = state.leakedRemainingHealth;
        report.combat.averageLeakedRemainingHealth = averageLeakedRemainingHealth;
        report.combat.peakAlive = state.peakAliveCount;

        report.timing.runDurationSeconds = state.GetRunActiveTimeSeconds();
        report.timing.spawnSpanSeconds = spawnSpan;
        report.timing.spawningCompleted = state.hasObservedSpawningCompletion;
        report.timing.spawningCompletedAtSeconds = spawningCompletionOffset;
        report.timing.battleDurationSeconds = battleDuration;

        report.player.initialHealth = state.initialPlayerHealth;
        report.player.finalHealth = (state.terminalCaptured || state.playerSystem != null)
            ? (state.terminalCaptured ? state.terminalHealth : state.playerSystem.CurrentHealth)
            : 0;
        report.player.maximumHealth = (state.terminalCaptured || state.playerSystem != null)
            ? (state.terminalCaptured ? state.terminalMaxHealth : state.playerSystem.MaxHealth)
            : 0;

        report.progression = state.CreateProgressionJson();
        report.timing.secondsAfterFinalDraft =
            CombatRecordingSession.ResolveSecondsAfterFinalDraft(
                report.progression,
                report.timing.runDurationSeconds);
        report.waveRuntime = state.CreateWaveRuntimeJson();
        report.draftRuntime = state.CreateDraftRuntimeJson();
        report.investmentRuntime = state.draftAccumulator.CreateInvestmentRuntimeJson(state.resolvedCount);
        report.timing.finalBuildCommitObserved =
            report.investmentRuntime.finalBuildCommitObserved;
        report.timing.finalBuildCommittedAtSeconds =
            state.draftAccumulator.ResolveFinalBuildCommitTime();
        report.timing.secondsAfterFinalBuildCommit =
            report.timing.finalBuildCommitObserved
                ? Mathf.Max(
                    0f,
                    report.timing.runDurationSeconds -
                    report.timing.finalBuildCommittedAtSeconds)
                : 0f;

        report.monsterRuntime = state.CreateMonsterRuntimeJson();
        report.placementRouteRuntime = state.routeAccumulator.CreatePlacementRouteRuntimeJson(state.placementRouteForcedRelocationExpectation, state.GetRunActiveTimeSeconds());
        report.damageDiagnostics = state.damageAccumulator.CreateDamageDiagnosticsJson();
        report.towers = state.CreateTowerJsonRecords();
        report.towerWaveSummaries = state.CreateTowerWaveSummaries();
        report.towerRouteDamageCoverage =
            state.routeAccumulator.CreateTowerRouteDamageCoverageRuntimeJson(report.towers);
        report.execution.initialDraftCompleted =
            state.initialDraftCompletionCount > 0;
        report.execution.allExpectedLevelUpsObserved =
            report.progression.observedLevelUpCount ==
            report.progression.expectedLevelUpCount;
        report.execution.expectedFinalPlayerLevelReached =
            report.progression.finalLevel ==
            report.progression.expectedLevelUpCount + 1;
        report.execution.allConfiguredWavesStarted =
            !state.hasExpectedWaveCount ||
            report.waveRuntime.observedWaveStartCount == state.expectedWaveCount;
        report.execution.allConfiguredWavesCompleted =
            !state.hasExpectedWaveCount ||
            report.waveRuntime.observedWaveCompletionCount == state.expectedWaveCount;
        report.execution.allConfiguredMonstersSpawned =
            !state.hasExpectedMonsterCount || state.spawnedCount == state.expectedMonsterCount;
        report.execution.finalBuildCommitted =
            report.investmentRuntime.committedInvestmentCount ==
            report.progression.expectedTotalDraftCount;
        report.execution.postFinalBuildCombatObserved =
            report.investmentRuntime.resolutionsAfterFinalBuildCommit > 0;
        report.integrity.fixtureSnapshotComplete =
            state.FixtureSnapshotIsComplete(report.fixture);
        report.integrity.resolutionCountsMatch =
            state.resolvedCount == state.killedCount + state.leakedCount;
        report.integrity.leakCountMatchesPlayerHealthLoss =
            (state.terminalCaptured || state.playerSystem != null) && state.leakedCount == observedPlayerHealthLoss;
        report.integrity.monsterRuntimeCountsMatch =
            state.MonsterRuntimeCountsMatch(report.monsterRuntime);
        report.integrity.monsterRuntimeDamageMatches =
            state.MonsterRuntimeDamageMatches(report.monsterRuntime);
        report.integrity.monsterRuntimeRegistrationCoverageMatch =
            state.MonsterRuntimeRegistrationCoverageMatches(report.monsterRuntime);
        report.integrity.monsterRuntimeStartedAtFullHealth =
            CombatRecordingSession.MonsterRuntimeStartedAtFullHealth(report.monsterRuntime);
        report.integrity.levelUpResolutionNodesMatch =
            CombatRecordingSession.LevelUpResolutionNodesMatch(report.progression);
        report.integrity.waveMonsterAttributionMatches =
            state.WaveMonsterAttributionMatches(report.waveRuntime);
        report.integrity.draftAttemptSelectionsMatch =
            report.draftRuntime.observedAttemptCount ==
            report.draftRuntime.committedSelectionCount;
        report.integrity.draftAttemptCountMatchesProgression =
            report.draftRuntime.observedAttemptCount ==
            report.progression.observedTotalDraftCount;
        report.integrity.draftGenerationTraceConsistent =
            CombatRecordingSession.DraftGenerationTraceIsConsistent(report.draftRuntime);
        report.integrity.draftConsumptionReconciled =
            CombatRecordingSession.DraftConsumptionIsReconciled(report.draftRuntime);
        report.integrity.towerDeploymentCoverageMatches =
            state.TowerDeploymentCoverageMatches(report.towers);
        report.integrity.investmentCommitsMatchDraftSelections =
            CombatRecordingSession.InvestmentCommitsMatchDraftSelections(
                report.investmentRuntime,
                report.draftRuntime);
        report.integrity.towerWaveAttributionMatches =
            CombatRecordingSession.TowerWaveAttributionMatches(
                report.towerWaveSummaries,
                report.damageDiagnostics);
        report.integrity.towerRouteDamageCoverageMatches =
            state.TowerRouteDamageCoverageMatches(
                report.towerRouteDamageCoverage,
                report.towers,
                report.towerWaveSummaries);
        report.integrity.damageDiagnosticsCountsMatch =
            DamageDiagnosticsCountsMatch(report.damageDiagnostics);
        report.integrity.droneBurstDiagnosticsConsistent =
            state.entityAccumulator.DroneBurstDiagnosticsAreConsistent();
        report.integrity.droneLifecycleDiagnosticsConsistent =
            state.entityAccumulator.DroneLifecycleDiagnosticsAreConsistent();
        report.elementalOpportunityDiagnostics =
            state.CreateElementalOpportunityDiagnosticsJson();
        report.integrity.elementalOpportunityDiagnosticsConsistent =
            report.elementalOpportunityDiagnostics.diagnosticsConsistent;
        report.elementalHitReactionDiagnostics =
            state.elementalHitReactionAccumulator.CreateJson();
        report.integrity.elementalHitReactionDiagnosticsConsistent =
            report.elementalHitReactionDiagnostics.diagnosticsConsistent &&
            state.ElementalHitReactionDamageReconciliationMatches();
        report.buffs = state.buffAccumulator.CreateJsonRecords();
        report.integrity.buffDiagnosticsConsistent =
            state.buffAccumulator.DiagnosticsConsistent;
        report.integrity.buffStackUnitAccountingConsistent =
            state.buffAccumulator.StackUnitAccountingConsistent;
        report.integrity.buffWaveAttributionMatches =
            state.buffAccumulator.WaveAttributionMatches;
        report.integrity.placementRouteBatchCountsMatch =
            PlacementRouteBatchCountsMatch(report.placementRouteRuntime);
        report.integrity.placementRouteCommitStatePreserved =
            PlacementRouteCommitStatePreserved(
                report.placementRouteRuntime);
        report.integrity.placementRouteTopologyValid =
            PlacementRouteTopologyValid(report.placementRouteRuntime);
        report.integrity.placementRouteGameplayStatePreserved =
            PlacementRouteGameplayStatePreserved(
                report.placementRouteRuntime);
        report.integrity.placementRouteCombatOwnershipPreserved =
            PlacementRouteCombatOwnershipPreserved(
                report.placementRouteRuntime);
        report.integrity.placementRouteLifecycleConsistent =
            PlacementRouteLifecycleConsistent(
                report.placementRouteRuntime);
        report.integrity.placementRouteForcedRelocationUsageValid =
            PlacementRouteForcedRelocationUsageValid(
                report.placementRouteRuntime);
        return report;
    }
}
#endif
