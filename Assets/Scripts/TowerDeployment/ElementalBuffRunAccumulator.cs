#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

internal sealed class ElementalBuffRunAccumulator
{
    private sealed class SourceAggregate
    {
        public SourceAggregate(
            int sourceTowerInstanceId,
            string towerFamily,
            string elementalUpgradeName)
        {
            SourceTowerInstanceId = sourceTowerInstanceId;
            TowerFamily = towerFamily;
            ElementalUpgradeName = elementalUpgradeName;
        }

        public int SourceTowerInstanceId { get; }
        public string TowerFamily { get; }
        public string ElementalUpgradeName { get; private set; }
        public int ApplicationAttempts { get; set; }
        public int SuccessfulApplications { get; set; }
        public int Applied { get; set; }
        public int Refreshed { get; set; }
        public int Stacked { get; set; }
        public int StackUnitsAdded { get; set; }
        public int MaximumStackUnitsAddedBySingleApplication { get; set; }
        public int Invalid { get; set; }
        public int BlockedBySourceCooldown { get; set; }
        public int BlockedByProtection { get; set; }
        public int Overloads { get; set; }
        public int EnteredProtection { get; set; }
        public int PeriodicTicks { get; set; }
        public int NaturalExpiriesBeforeOverload { get; set; }
        public float FirstApplicationToOverloadTotal { get; set; }
        public int TimedOverloadCount { get; set; }

        public void RefreshElementalUpgradeName(string elementalUpgradeName)
        {
            if (string.IsNullOrEmpty(ElementalUpgradeName) &&
                !string.IsNullOrEmpty(elementalUpgradeName))
            {
                ElementalUpgradeName = elementalUpgradeName;
            }
        }
    }

    private sealed class MetricAggregate
    {
        public int Samples { get; private set; }
        public float Total { get; private set; }
        public float Minimum { get; private set; }
        public float Maximum { get; private set; }

        public void Record(float value)
        {
            float clampedValue = Mathf.Max(0f, value);

            if (Samples == 0)
            {
                Minimum = clampedValue;
                Maximum = clampedValue;
            }
            else
            {
                Minimum = Mathf.Min(Minimum, clampedValue);
                Maximum = Mathf.Max(Maximum, clampedValue);
            }

            Samples++;
            Total += clampedValue;
        }

        public CombatBalanceMetricJson CreateJson()
        {
            return new CombatBalanceMetricJson
            {
                samples = Samples,
                minimum = Samples > 0 ? Minimum : 0f,
                average = Samples > 0 ? Total / Samples : 0f,
                maximum = Samples > 0 ? Maximum : 0f
            };
        }
    }

    private class BuffScopeAggregate
    {
        public HashSet<int> DistinctMonsterIds { get; } = new HashSet<int>();
        public HashSet<int> DistinctSourceTowerIds { get; } = new HashSet<int>();
        public HashSet<int> OverloadedMonsterIds { get; } = new HashSet<int>();
        public HashSet<string> SourceElements { get; } = new HashSet<string>();
        public Dictionary<int, SourceAggregate> Sources { get; } =
            new Dictionary<int, SourceAggregate>();
        public Dictionary<int, float> FirstApplicationTimesByMonster { get; } =
            new Dictionary<int, float>();
        public Dictionary<int, float> LastSuccessfulApplicationTimesByMonster { get; } =
            new Dictionary<int, float>();
        public Dictionary<int, float> ProtectionExpiryTimesByMonster { get; } =
            new Dictionary<int, float>();
        public Dictionary<int, HashSet<int>> CycleSourceTowerIdsByMonster { get; } =
            new Dictionary<int, HashSet<int>>();
        public Dictionary<int, int> NaturalExpiryStackCounts { get; } =
            new Dictionary<int, int>();
        public Dictionary<int, int> OverloadSourceCounts { get; } =
            new Dictionary<int, int>();
        public MetricAggregate FirstApplicationToOverloadMetric { get; } =
            new MetricAggregate();
        public MetricAggregate SuccessfulApplicationGapMetric { get; } =
            new MetricAggregate();
        public MetricAggregate ProtectionExpiryToReapplicationMetric { get; } =
            new MetricAggregate();
        public int ApplicationAttempts { get; set; }
        public int Applied { get; set; }
        public int Refreshed { get; set; }
        public int Stacked { get; set; }
        public int StackUnitsAdded { get; set; }
        public int MaximumStackUnitsAddedBySingleApplication { get; set; }
        public int Invalid { get; set; }
        public int BlockedBySourceCooldown { get; set; }
        public int BlockedByProtection { get; set; }
        public int MaximumObservedStacks { get; set; }
        public int StackingCyclesStarted { get; set; }
        public int StackingCyclesOverloaded { get; set; }
        public int StackingCyclesNaturallyExpired { get; set; }
        public int ReentriesAfterProtection { get; set; }
        public int Overloads { get; set; }
        public int EnteredProtection { get; set; }
        public int PeriodicTicks { get; set; }
        public int NaturalExpiriesBeforeOverload { get; set; }
        public int ProtectionExpiries { get; set; }
        public int ExplicitRemovals { get; set; }
        public int MonsterKilledRemovals { get; set; }
        public int MonsterLeakedRemovals { get; set; }
        public int TechnicalCleanupRemovals { get; set; }
        public int RuntimeResetRemovals { get; set; }
        public int OverloadWithoutProtectionRemovals { get; set; }
        public float FirstApplicationToOverloadTotal { get; set; }
        public int TimedOverloadCount { get; set; }
    }

    private sealed class BuffWaveAggregate : BuffScopeAggregate
    {
        public BuffWaveAggregate(int waveNumber)
        {
            WaveNumber = waveNumber;
        }

        public int WaveNumber { get; }
    }

    private sealed class BuffAggregate : BuffScopeAggregate
    {
        public BuffAggregate(BuffDefinition definition)
        {
            Definition = definition;
            Parameters = CreateParameterSnapshot(definition);
        }

        public BuffDefinition Definition { get; }
        public CombatBalanceBuffParametersJson Parameters { get; }
        public Dictionary<int, BuffWaveAggregate> Waves { get; } =
            new Dictionary<int, BuffWaveAggregate>();
    }

    private readonly Dictionary<BuffDefinition, BuffAggregate> aggregates =
        new Dictionary<BuffDefinition, BuffAggregate>();
    private bool waveAttributionMatches = true;

    public bool DiagnosticsConsistent => ValidateAllAggregates();
    public bool WaveAttributionMatches => waveAttributionMatches;

    public void Reset()
    {
        aggregates.Clear();
        waveAttributionMatches = true;
    }

    public void Consume(BuffRuntimeObservation observation, int sourceWaveNumber)
    {
        BuffDefinition definition = observation.BuffDefinition;

        if (definition == null)
        {
            return;
        }

        BuffAggregate aggregate = GetOrCreateAggregate(definition);
        ConsumeScope(aggregate, definition, observation);

        if (observation.OwnerMonster == null)
        {
            return;
        }

        if (sourceWaveNumber <= 0)
        {
            waveAttributionMatches = false;
            return;
        }

        ConsumeScope(
            GetOrCreateWaveAggregate(aggregate, sourceWaveNumber),
            definition,
            observation);
    }

    public void AppendSummary(StringBuilder builder)
    {
        List<BuffAggregate> sortedAggregates = GetSortedAggregates();
        builder.AppendLine("Buff Runtime:");

        if (sortedAggregates.Count == 0)
        {
            builder.AppendLine("- None Observed");
            return;
        }

        for (int i = 0; i < sortedAggregates.Count; i++)
        {
            BuffAggregate aggregate = sortedAggregates[i];
            builder.Append("- ")
                .Append(GetBuffDisplayName(aggregate.Definition))
                .Append(": Element=")
                .Append(aggregate.Definition.ElementType)
                .Append(", Attempts=")
                .Append(aggregate.ApplicationAttempts)
                .Append(", Applied=")
                .Append(aggregate.Applied)
                .Append(", Refreshed=")
                .Append(aggregate.Refreshed)
                .Append(", Stacked=")
                .Append(aggregate.Stacked)
                .Append(", Blocked=[SourceCooldown=")
                .Append(aggregate.BlockedBySourceCooldown)
                .Append(", Protection=")
                .Append(aggregate.BlockedByProtection)
                .Append("], DistinctMonsters=")
                .Append(aggregate.DistinctMonsterIds.Count)
                .Append(", DistinctSources=")
                .Append(aggregate.DistinctSourceTowerIds.Count)
                .Append(", MaxObservedStacks=")
                .Append(aggregate.MaximumObservedStacks)
                .Append(", StackUnitsAdded=")
                .Append(aggregate.StackUnitsAdded)
                .Append(", Cycles=[Started=")
                .Append(aggregate.StackingCyclesStarted)
                .Append(", Overloaded=")
                .Append(aggregate.StackingCyclesOverloaded)
                .Append(", NaturalExpiry=")
                .Append(aggregate.StackingCyclesNaturallyExpired)
                .Append(", Reentry=")
                .Append(aggregate.ReentriesAfterProtection)
                .Append(']')
                .Append(", Overloads=")
                .Append(aggregate.Overloads)
                .Append(", EnteredProtection=")
                .Append(aggregate.EnteredProtection)
                .Append(", AvgTimeToOverload=")
                .Append(aggregate.TimedOverloadCount > 0
                    ? FormatSeconds(GetAverageTimeToOverload(aggregate))
                    : "NotObserved")
                .Append(", PeriodicTicks=")
                .Append(aggregate.PeriodicTicks)
                .Append(", NaturalExpiryBeforeOverload=")
                .Append(aggregate.NaturalExpiriesBeforeOverload)
                .Append(", Invalid=")
                .Append(aggregate.Invalid)
                .AppendLine();

            List<SourceAggregate> sortedSources = GetSortedSources(aggregate);

            for (int sourceIndex = 0; sourceIndex < sortedSources.Count; sourceIndex++)
            {
                SourceAggregate source = sortedSources[sourceIndex];
                builder.Append("  - ")
                    .Append(source.TowerFamily)
                    .Append('#')
                    .Append(source.SourceTowerInstanceId)
                    .Append(" / ElementalUpgrade=")
                    .Append(string.IsNullOrEmpty(source.ElementalUpgradeName)
                        ? "None"
                        : source.ElementalUpgradeName)
                    .Append(": Attempts=")
                    .Append(source.ApplicationAttempts)
                    .Append(", Successful=")
                    .Append(source.SuccessfulApplications)
                    .Append(", Applied=")
                    .Append(source.Applied)
                    .Append(", Refreshed=")
                    .Append(source.Refreshed)
                    .Append(", Stacked=")
                    .Append(source.Stacked)
                    .Append(", StackUnitsAdded=")
                    .Append(source.StackUnitsAdded)
                    .Append(", Blocked=[SourceCooldown=")
                    .Append(source.BlockedBySourceCooldown)
                    .Append(", Protection=")
                    .Append(source.BlockedByProtection)
                    .Append("], Invalid=")
                    .Append(source.Invalid)
                    .Append(", Overloads=")
                    .Append(source.Overloads)
                    .Append(", EnteredProtection=")
                    .Append(source.EnteredProtection)
                    .Append(", AvgTimeToOverload=")
                    .Append(source.TimedOverloadCount > 0
                        ? FormatSeconds(GetAverageTimeToOverload(source))
                        : "NotObserved")
                    .Append(", PeriodicTicks=")
                    .Append(source.PeriodicTicks)
                    .Append(", NaturalExpiryBeforeOverload=")
                    .Append(source.NaturalExpiriesBeforeOverload)
                    .AppendLine();
            }
        }
    }

    public List<CombatBalanceBuffJson> CreateJsonRecords()
    {
        List<CombatBalanceBuffJson> records = new List<CombatBalanceBuffJson>();
        List<BuffAggregate> sortedAggregates = GetSortedAggregates();

        for (int i = 0; i < sortedAggregates.Count; i++)
        {
            BuffAggregate aggregate = sortedAggregates[i];
            CombatBalanceBuffJson record = new CombatBalanceBuffJson
            {
                definitionName = aggregate.Definition.name,
                displayName = GetBuffDisplayName(aggregate.Definition),
                element = aggregate.Definition.ElementType.ToString(),
                parameters = aggregate.Parameters,
                applicationAttempts = aggregate.ApplicationAttempts,
                applied = aggregate.Applied,
                refreshed = aggregate.Refreshed,
                stacked = aggregate.Stacked,
                invalid = aggregate.Invalid,
                blockedBySourceCooldown = aggregate.BlockedBySourceCooldown,
                blockedByProtection = aggregate.BlockedByProtection,
                distinctMonsters = aggregate.DistinctMonsterIds.Count,
                distinctSourceTowers = aggregate.DistinctSourceTowerIds.Count,
                maximumObservedStacks = aggregate.MaximumObservedStacks,
                stackUnitsAdded = aggregate.StackUnitsAdded,
                maximumStackUnitsAddedBySingleApplication =
                    aggregate.MaximumStackUnitsAddedBySingleApplication,
                stackingCyclesStarted = aggregate.StackingCyclesStarted,
                stackingCyclesOverloaded = aggregate.StackingCyclesOverloaded,
                stackingCyclesNaturallyExpired =
                    aggregate.StackingCyclesNaturallyExpired,
                distinctMonstersOverloaded =
                    aggregate.OverloadedMonsterIds.Count,
                reentriesAfterProtection = aggregate.ReentriesAfterProtection,
                overloads = aggregate.Overloads,
                enteredProtection = aggregate.EnteredProtection,
                periodicTicks = aggregate.PeriodicTicks,
                naturalExpiriesBeforeOverload =
                    aggregate.NaturalExpiriesBeforeOverload,
                protectionExpiries = aggregate.ProtectionExpiries,
                explicitRemovals = aggregate.ExplicitRemovals,
                monsterKilledRemovals = aggregate.MonsterKilledRemovals,
                monsterLeakedRemovals = aggregate.MonsterLeakedRemovals,
                technicalCleanupRemovals = aggregate.TechnicalCleanupRemovals,
                runtimeResetRemovals = aggregate.RuntimeResetRemovals,
                overloadWithoutProtectionRemovals =
                    aggregate.OverloadWithoutProtectionRemovals,
                averageFirstApplicationToOverloadSeconds =
                    GetAverageTimeToOverload(aggregate),
                firstApplicationToOverloadSeconds =
                    aggregate.FirstApplicationToOverloadMetric.CreateJson(),
                successfulApplicationGapSeconds =
                    aggregate.SuccessfulApplicationGapMetric.CreateJson(),
                protectionExpiryToReapplicationSeconds =
                    aggregate.ProtectionExpiryToReapplicationMetric.CreateJson()
            };

            AppendStackCountOccurrences(
                record.naturalExpiryStackCounts,
                aggregate.NaturalExpiryStackCounts);
            AppendSourceCountOccurrences(
                record.overloadSourceCounts,
                aggregate.OverloadSourceCounts);

            record.sourceElements.AddRange(aggregate.SourceElements);
            record.sourceElements.Sort(StringComparer.Ordinal);

            List<SourceAggregate> sortedSources = GetSortedSources(aggregate);

            for (int sourceIndex = 0; sourceIndex < sortedSources.Count; sourceIndex++)
            {
                record.sources.Add(CreateSourceJson(sortedSources[sourceIndex]));
            }

            List<int> waveNumbers = new List<int>(aggregate.Waves.Keys);
            waveNumbers.Sort();

            for (int waveIndex = 0; waveIndex < waveNumbers.Count; waveIndex++)
            {
                record.waveSummaries.Add(CreateWaveJson(
                    aggregate.Waves[waveNumbers[waveIndex]]));
            }

            records.Add(record);
        }

        return records;
    }

    private static void ConsumeScope(
        BuffScopeAggregate aggregate,
        BuffDefinition definition,
        BuffRuntimeObservation observation)
    {
        aggregate.MaximumObservedStacks = Mathf.Max(
            aggregate.MaximumObservedStacks,
            observation.StackCountBefore,
            observation.StackCountAfter);

        if (observation.ObservationType ==
            BuffRuntimeObservationType.ApplicationAttempt)
        {
            ConsumeApplication(aggregate, definition, observation);
            return;
        }

        ConsumeLifecycle(aggregate, definition, observation);
    }

    private static void ConsumeApplication(
        BuffScopeAggregate aggregate,
        BuffDefinition definition,
        BuffRuntimeObservation observation)
    {
        aggregate.ApplicationAttempts++;
        SourceAggregate source = GetOrCreateSource(
            aggregate,
            observation.SourceTower,
            observation.SourceUpgrade);
        source.ApplicationAttempts++;

        switch (observation.ApplyResult)
        {
            case BuffApplyResult.Applied:
                aggregate.Applied++;
                source.Applied++;
                source.SuccessfulApplications++;
                RecordSuccessfulSource(aggregate, observation);
                RecordFirstApplicationTime(aggregate, observation);
                RecordCycleSource(aggregate, observation);
                RecordProtectionReentry(aggregate, observation);
                aggregate.StackingCyclesStarted += definition.UsesStacks ? 1 : 0;
                RecordStackUnitsAdded(aggregate, source, definition, observation);
                break;
            case BuffApplyResult.Refreshed:
                aggregate.Refreshed++;
                source.Refreshed++;
                source.SuccessfulApplications++;
                RecordSuccessfulSource(aggregate, observation);
                RecordSuccessfulApplicationGap(aggregate, observation);
                RecordCycleSource(aggregate, observation);
                RecordStackUnitsAdded(aggregate, source, definition, observation);
                break;
            case BuffApplyResult.Stacked:
                aggregate.Stacked++;
                source.Stacked++;
                source.SuccessfulApplications++;
                RecordSuccessfulSource(aggregate, observation);
                RecordSuccessfulApplicationGap(aggregate, observation);
                RecordCycleSource(aggregate, observation);
                RecordStackUnitsAdded(aggregate, source, definition, observation);
                break;
            case BuffApplyResult.BlockedBySourceApplyCooldown:
                aggregate.BlockedBySourceCooldown++;
                source.BlockedBySourceCooldown++;
                break;
            case BuffApplyResult.BlockedByProtectionPhase:
                aggregate.BlockedByProtection++;
                source.BlockedByProtection++;
                break;
            case BuffApplyResult.Invalid:
            default:
                aggregate.Invalid++;
                source.Invalid++;
                break;
        }
    }

    private static void ConsumeLifecycle(
        BuffScopeAggregate aggregate,
        BuffDefinition definition,
        BuffRuntimeObservation observation)
    {
        SourceAggregate source = GetOrCreateSource(
            aggregate,
            observation.SourceTower,
            observation.SourceUpgrade);

        switch (observation.LifecycleEvent)
        {
            case BuffEventType.Overload:
                aggregate.Overloads++;
                aggregate.StackingCyclesOverloaded++;
                RecordOverloadedMonster(aggregate, observation);
                RecordOverloadSourceCount(aggregate, observation);
                source.Overloads++;
                RecordOverloadTime(aggregate, source, observation);
                ClearActiveCycle(aggregate, observation.OwnerMonster);
                break;
            case BuffEventType.EnteredProtection:
                aggregate.EnteredProtection++;
                source.EnteredProtection++;
                break;
            case BuffEventType.PeriodicTick:
                aggregate.PeriodicTicks++;
                source.PeriodicTicks++;
                break;
            case BuffEventType.Removed:
                RecordRemoval(aggregate, source, definition, observation);
                break;
        }
    }

    private BuffAggregate GetOrCreateAggregate(BuffDefinition definition)
    {
        if (!aggregates.TryGetValue(definition, out BuffAggregate aggregate))
        {
            aggregate = new BuffAggregate(definition);
            aggregates.Add(definition, aggregate);
        }

        return aggregate;
    }

    private static BuffWaveAggregate GetOrCreateWaveAggregate(
        BuffAggregate aggregate,
        int waveNumber)
    {
        if (!aggregate.Waves.TryGetValue(
                waveNumber,
                out BuffWaveAggregate waveAggregate))
        {
            waveAggregate = new BuffWaveAggregate(waveNumber);
            aggregate.Waves.Add(waveNumber, waveAggregate);
        }

        return waveAggregate;
    }

    private static SourceAggregate GetOrCreateSource(
        BuffScopeAggregate aggregate,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade)
    {
        int sourceTowerInstanceId = GetSourceTowerInstanceId(sourceTower);
        string towerFamily = GetTowerFamily(sourceTower);
        string elementalUpgradeName = GetElementalUpgradeName(sourceUpgrade);

        if (!aggregate.Sources.TryGetValue(
                sourceTowerInstanceId,
                out SourceAggregate source))
        {
            source = new SourceAggregate(
                sourceTowerInstanceId,
                towerFamily,
                elementalUpgradeName);
            aggregate.Sources.Add(sourceTowerInstanceId, source);
        }
        else
        {
            source.RefreshElementalUpgradeName(elementalUpgradeName);
        }

        return source;
    }

    private static void RecordSuccessfulSource(
        BuffScopeAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster != null)
        {
            aggregate.DistinctMonsterIds.Add(
                observation.OwnerMonster.GetInstanceID());
        }

        if (observation.SourceTower != null)
        {
            aggregate.DistinctSourceTowerIds.Add(
                observation.SourceTower.GetInstanceID());
        }

        if (observation.SourceUpgrade != null &&
            observation.SourceUpgrade.UpgradeLayer == TowerUpgradeLayer.Elemental)
        {
            aggregate.SourceElements.Add(
                observation.SourceUpgrade.ElementType.ToString());
        }
    }

    private static void RecordFirstApplicationTime(
        BuffScopeAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster == null)
        {
            return;
        }

        int monsterId = observation.OwnerMonster.GetInstanceID();
        aggregate.FirstApplicationTimesByMonster[monsterId] =
            observation.ObservationTime;
        aggregate.LastSuccessfulApplicationTimesByMonster[monsterId] =
            observation.ObservationTime;
    }

    private static void RecordOverloadTime(
        BuffScopeAggregate aggregate,
        SourceAggregate source,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster == null)
        {
            return;
        }

        int monsterId = observation.OwnerMonster.GetInstanceID();

        if (aggregate.FirstApplicationTimesByMonster.TryGetValue(
                monsterId,
                out float firstApplicationTime))
        {
            float elapsedSeconds = Mathf.Max(
                0f,
                observation.ObservationTime - firstApplicationTime);
            aggregate.FirstApplicationToOverloadTotal += elapsedSeconds;
            aggregate.TimedOverloadCount++;
            aggregate.FirstApplicationToOverloadMetric.Record(elapsedSeconds);
            source.FirstApplicationToOverloadTotal += elapsedSeconds;
            source.TimedOverloadCount++;
        }
    }

    private static void RecordRemoval(
        BuffScopeAggregate aggregate,
        SourceAggregate source,
        BuffDefinition definition,
        BuffRuntimeObservation observation)
    {
        int monsterId = observation.OwnerMonster != null
            ? observation.OwnerMonster.GetInstanceID()
            : 0;

        switch (observation.RemovalReason)
        {
            case BuffRemovalReason.ActiveDurationExpired:
                if (definition != null &&
                    definition.UsesStacks &&
                    monsterId != 0 &&
                    aggregate.FirstApplicationTimesByMonster.ContainsKey(monsterId))
                {
                    aggregate.NaturalExpiriesBeforeOverload++;
                    aggregate.StackingCyclesNaturallyExpired++;
                    IncrementCount(
                        aggregate.NaturalExpiryStackCounts,
                        Mathf.Max(0, observation.StackCountBefore));
                    source.NaturalExpiriesBeforeOverload++;
                }
                break;
            case BuffRemovalReason.ProtectionExpired:
                aggregate.ProtectionExpiries++;

                if (monsterId != 0)
                {
                    aggregate.ProtectionExpiryTimesByMonster[monsterId] =
                        observation.ObservationTime;
                }
                break;
            case BuffRemovalReason.ExplicitRemoval:
                aggregate.ExplicitRemovals++;
                break;
            case BuffRemovalReason.MonsterKilled:
                aggregate.MonsterKilledRemovals++;
                break;
            case BuffRemovalReason.MonsterLeaked:
                aggregate.MonsterLeakedRemovals++;
                break;
            case BuffRemovalReason.TechnicalCleanup:
                aggregate.TechnicalCleanupRemovals++;
                break;
            case BuffRemovalReason.RuntimeReset:
                aggregate.RuntimeResetRemovals++;
                break;
            case BuffRemovalReason.OverloadWithoutProtection:
                aggregate.OverloadWithoutProtectionRemovals++;
                break;
        }

        ClearActiveCycle(aggregate, observation.OwnerMonster);
    }

    private static void RecordSuccessfulApplicationGap(
        BuffScopeAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster == null)
        {
            return;
        }

        int monsterId = observation.OwnerMonster.GetInstanceID();

        if (aggregate.LastSuccessfulApplicationTimesByMonster.TryGetValue(
                monsterId,
                out float lastSuccessfulTime))
        {
            aggregate.SuccessfulApplicationGapMetric.Record(
                observation.ObservationTime - lastSuccessfulTime);
        }

        aggregate.LastSuccessfulApplicationTimesByMonster[monsterId] =
            observation.ObservationTime;
    }

    private static void RecordCycleSource(
        BuffScopeAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster == null)
        {
            return;
        }

        int monsterId = observation.OwnerMonster.GetInstanceID();

        if (!aggregate.CycleSourceTowerIdsByMonster.TryGetValue(
                monsterId,
                out HashSet<int> sourceTowerIds))
        {
            sourceTowerIds = new HashSet<int>();
            aggregate.CycleSourceTowerIdsByMonster.Add(
                monsterId,
                sourceTowerIds);
        }

        int sourceTowerInstanceId =
            GetSourceTowerInstanceId(observation.SourceTower);

        if (sourceTowerInstanceId != 0)
        {
            sourceTowerIds.Add(sourceTowerInstanceId);
        }
    }

    private static void RecordProtectionReentry(
        BuffScopeAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster == null)
        {
            return;
        }

        int monsterId = observation.OwnerMonster.GetInstanceID();

        if (!aggregate.ProtectionExpiryTimesByMonster.TryGetValue(
                monsterId,
                out float protectionExpiryTime))
        {
            return;
        }

        aggregate.ReentriesAfterProtection++;
        aggregate.ProtectionExpiryToReapplicationMetric.Record(
            observation.ObservationTime - protectionExpiryTime);
        aggregate.ProtectionExpiryTimesByMonster.Remove(monsterId);
    }

    private static void RecordStackUnitsAdded(
        BuffScopeAggregate aggregate,
        SourceAggregate source,
        BuffDefinition definition,
        BuffRuntimeObservation observation)
    {
        if (definition == null || !definition.UsesStacks)
        {
            return;
        }

        int stackUnitsAdded = observation.ApplyResult == BuffApplyResult.Applied
            ? Mathf.Max(0, observation.StackCountAfter)
            : Mathf.Max(
                0,
                observation.StackCountAfter - observation.StackCountBefore);

        aggregate.StackUnitsAdded += stackUnitsAdded;
        aggregate.MaximumStackUnitsAddedBySingleApplication = Mathf.Max(
            aggregate.MaximumStackUnitsAddedBySingleApplication,
            stackUnitsAdded);
        source.StackUnitsAdded += stackUnitsAdded;
        source.MaximumStackUnitsAddedBySingleApplication = Mathf.Max(
            source.MaximumStackUnitsAddedBySingleApplication,
            stackUnitsAdded);
    }

    private static void RecordOverloadedMonster(
        BuffScopeAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster != null)
        {
            aggregate.OverloadedMonsterIds.Add(
                observation.OwnerMonster.GetInstanceID());
        }
    }

    private static void RecordOverloadSourceCount(
        BuffScopeAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        int sourceCount = 0;

        if (observation.OwnerMonster != null &&
            aggregate.CycleSourceTowerIdsByMonster.TryGetValue(
                observation.OwnerMonster.GetInstanceID(),
                out HashSet<int> sourceTowerIds))
        {
            sourceCount = sourceTowerIds.Count;
        }

        IncrementCount(aggregate.OverloadSourceCounts, sourceCount);
    }

    private static void ClearActiveCycle(
        BuffScopeAggregate aggregate,
        MonsterBehaviour ownerMonster)
    {
        if (ownerMonster == null)
        {
            return;
        }

        int monsterId = ownerMonster.GetInstanceID();
        aggregate.FirstApplicationTimesByMonster.Remove(monsterId);
        aggregate.LastSuccessfulApplicationTimesByMonster.Remove(monsterId);
        aggregate.CycleSourceTowerIdsByMonster.Remove(monsterId);
    }

    private static void IncrementCount(
        Dictionary<int, int> counts,
        int value)
    {
        counts.TryGetValue(value, out int currentCount);
        counts[value] = currentCount + 1;
    }

    private static CombatBalanceBuffWaveJson CreateWaveJson(
        BuffWaveAggregate aggregate)
    {
        CombatBalanceBuffWaveJson record = new CombatBalanceBuffWaveJson
        {
            waveNumber = aggregate.WaveNumber,
            applicationAttempts = aggregate.ApplicationAttempts,
            applied = aggregate.Applied,
            refreshed = aggregate.Refreshed,
            stacked = aggregate.Stacked,
            invalid = aggregate.Invalid,
            blockedBySourceCooldown = aggregate.BlockedBySourceCooldown,
            blockedByProtection = aggregate.BlockedByProtection,
            distinctMonsters = aggregate.DistinctMonsterIds.Count,
            distinctSourceTowers = aggregate.DistinctSourceTowerIds.Count,
            maximumObservedStacks = aggregate.MaximumObservedStacks,
            stackUnitsAdded = aggregate.StackUnitsAdded,
            maximumStackUnitsAddedBySingleApplication =
                aggregate.MaximumStackUnitsAddedBySingleApplication,
            stackingCyclesStarted = aggregate.StackingCyclesStarted,
            stackingCyclesOverloaded = aggregate.StackingCyclesOverloaded,
            stackingCyclesNaturallyExpired =
                aggregate.StackingCyclesNaturallyExpired,
            distinctMonstersOverloaded =
                aggregate.OverloadedMonsterIds.Count,
            reentriesAfterProtection = aggregate.ReentriesAfterProtection,
            overloads = aggregate.Overloads,
            enteredProtection = aggregate.EnteredProtection,
            periodicTicks = aggregate.PeriodicTicks,
            naturalExpiriesBeforeOverload =
                aggregate.NaturalExpiriesBeforeOverload,
            protectionExpiries = aggregate.ProtectionExpiries,
            monsterKilledRemovals = aggregate.MonsterKilledRemovals,
            monsterLeakedRemovals = aggregate.MonsterLeakedRemovals,
            firstApplicationToOverloadSeconds =
                aggregate.FirstApplicationToOverloadMetric.CreateJson(),
            successfulApplicationGapSeconds =
                aggregate.SuccessfulApplicationGapMetric.CreateJson(),
            protectionExpiryToReapplicationSeconds =
                aggregate.ProtectionExpiryToReapplicationMetric.CreateJson()
        };

        AppendStackCountOccurrences(
            record.naturalExpiryStackCounts,
            aggregate.NaturalExpiryStackCounts);
        AppendSourceCountOccurrences(
            record.overloadSourceCounts,
            aggregate.OverloadSourceCounts);

        List<SourceAggregate> sources = GetSortedSources(aggregate);

        for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
        {
            record.sources.Add(CreateSourceJson(sources[sourceIndex]));
        }

        return record;
    }

    private static CombatBalanceBuffSourceJson CreateSourceJson(
        SourceAggregate source)
    {
        return new CombatBalanceBuffSourceJson
        {
            sourceTowerInstanceId = source.SourceTowerInstanceId,
            towerFamily = source.TowerFamily,
            elementalUpgradeName = source.ElementalUpgradeName,
            applicationAttempts = source.ApplicationAttempts,
            successfulApplications = source.SuccessfulApplications,
            applied = source.Applied,
            refreshed = source.Refreshed,
            stacked = source.Stacked,
            stackUnitsAdded = source.StackUnitsAdded,
            maximumStackUnitsAddedBySingleApplication =
                source.MaximumStackUnitsAddedBySingleApplication,
            invalid = source.Invalid,
            blockedBySourceCooldown = source.BlockedBySourceCooldown,
            blockedByProtection = source.BlockedByProtection,
            overloads = source.Overloads,
            enteredProtection = source.EnteredProtection,
            periodicTicks = source.PeriodicTicks,
            naturalExpiriesBeforeOverload =
                source.NaturalExpiriesBeforeOverload,
            averageFirstApplicationToOverloadSeconds =
                GetAverageTimeToOverload(source)
        };
    }

    private static void AppendStackCountOccurrences(
        List<CombatBalanceStackCountOccurrenceJson> records,
        Dictionary<int, int> counts)
    {
        List<int> values = new List<int>(counts.Keys);
        values.Sort();

        for (int i = 0; i < values.Count; i++)
        {
            int value = values[i];
            records.Add(new CombatBalanceStackCountOccurrenceJson
            {
                stackCount = value,
                occurrences = counts[value]
            });
        }
    }

    private static void AppendSourceCountOccurrences(
        List<CombatBalanceSourceCountOccurrenceJson> records,
        Dictionary<int, int> counts)
    {
        List<int> values = new List<int>(counts.Keys);
        values.Sort();

        for (int i = 0; i < values.Count; i++)
        {
            int value = values[i];
            records.Add(new CombatBalanceSourceCountOccurrenceJson
            {
                sourceCount = value,
                occurrences = counts[value]
            });
        }
    }

    private bool ValidateAllAggregates()
    {
        foreach (BuffAggregate aggregate in aggregates.Values)
        {
            if (!ValidateScopeAggregate(aggregate))
            {
                return false;
            }

            foreach (BuffWaveAggregate waveAggregate in aggregate.Waves.Values)
            {
                if (!ValidateScopeAggregate(waveAggregate))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ValidateScopeAggregate(BuffScopeAggregate aggregate)
    {
        int applicationAttempts = 0;
        int successfulApplications = 0;
        int applied = 0;
        int refreshed = 0;
        int stacked = 0;
        int stackUnitsAdded = 0;
        int invalid = 0;
        int blockedBySourceCooldown = 0;
        int blockedByProtection = 0;
        int overloads = 0;
        int enteredProtection = 0;
        int periodicTicks = 0;
        int naturalExpiriesBeforeOverload = 0;

        foreach (SourceAggregate source in aggregate.Sources.Values)
        {
            if (source.SuccessfulApplications !=
                source.Applied + source.Refreshed + source.Stacked)
            {
                return false;
            }

            applicationAttempts += source.ApplicationAttempts;
            successfulApplications += source.SuccessfulApplications;
            applied += source.Applied;
            refreshed += source.Refreshed;
            stacked += source.Stacked;
            stackUnitsAdded += source.StackUnitsAdded;
            invalid += source.Invalid;
            blockedBySourceCooldown += source.BlockedBySourceCooldown;
            blockedByProtection += source.BlockedByProtection;
            overloads += source.Overloads;
            enteredProtection += source.EnteredProtection;
            periodicTicks += source.PeriodicTicks;
            naturalExpiriesBeforeOverload +=
                source.NaturalExpiriesBeforeOverload;
        }

        int classifiedAttempts = aggregate.Applied + aggregate.Refreshed +
            aggregate.Stacked + aggregate.Invalid +
            aggregate.BlockedBySourceCooldown +
            aggregate.BlockedByProtection;

        return aggregate.ApplicationAttempts == classifiedAttempts &&
               applicationAttempts == aggregate.ApplicationAttempts &&
               successfulApplications ==
               aggregate.Applied + aggregate.Refreshed + aggregate.Stacked &&
               applied == aggregate.Applied &&
               refreshed == aggregate.Refreshed &&
               stacked == aggregate.Stacked &&
               stackUnitsAdded == aggregate.StackUnitsAdded &&
               invalid == aggregate.Invalid &&
               blockedBySourceCooldown ==
               aggregate.BlockedBySourceCooldown &&
               blockedByProtection == aggregate.BlockedByProtection &&
               overloads == aggregate.Overloads &&
               enteredProtection == aggregate.EnteredProtection &&
               periodicTicks == aggregate.PeriodicTicks &&
               naturalExpiriesBeforeOverload ==
               aggregate.NaturalExpiriesBeforeOverload &&
               aggregate.StackingCyclesOverloaded == aggregate.Overloads &&
               aggregate.StackingCyclesNaturallyExpired ==
               aggregate.NaturalExpiriesBeforeOverload &&
               SumCounts(aggregate.NaturalExpiryStackCounts) ==
               aggregate.StackingCyclesNaturallyExpired &&
               SumCounts(aggregate.OverloadSourceCounts) ==
               aggregate.Overloads &&
               aggregate.FirstApplicationToOverloadMetric.Samples ==
               aggregate.TimedOverloadCount &&
               aggregate.ReentriesAfterProtection <=
               aggregate.ProtectionExpiries;
    }

    private static int SumCounts(Dictionary<int, int> counts)
    {
        int total = 0;

        foreach (int count in counts.Values)
        {
            total += count;
        }

        return total;
    }

    private List<BuffAggregate> GetSortedAggregates()
    {
        List<BuffAggregate> result = new List<BuffAggregate>(aggregates.Values);
        result.Sort((left, right) => string.CompareOrdinal(
            GetBuffSortKey(left.Definition),
            GetBuffSortKey(right.Definition)));
        return result;
    }

    private static List<SourceAggregate> GetSortedSources(
        BuffScopeAggregate aggregate)
    {
        List<SourceAggregate> result =
            new List<SourceAggregate>(aggregate.Sources.Values);
        result.Sort((left, right) =>
        {
            int familyComparison = string.CompareOrdinal(
                left.TowerFamily,
                right.TowerFamily);

            return familyComparison != 0
                ? familyComparison
                : left.SourceTowerInstanceId.CompareTo(
                    right.SourceTowerInstanceId);
        });
        return result;
    }

    private static int GetSourceTowerInstanceId(TowerInstance sourceTower)
    {
        return sourceTower != null ? sourceTower.GetInstanceID() : 0;
    }

    private static string GetTowerFamily(TowerInstance sourceTower)
    {
        return sourceTower != null && sourceTower.TowerDefinition != null
            ? sourceTower.TowerDefinition.TowerFamily.ToString()
            : "Unattributed";
    }

    private static string GetElementalUpgradeName(
        TowerUpgradeDefinition sourceUpgrade)
    {
        return sourceUpgrade != null &&
               sourceUpgrade.UpgradeLayer == TowerUpgradeLayer.Elemental
            ? sourceUpgrade.name
            : string.Empty;
    }

    private static string GetBuffDisplayName(BuffDefinition definition)
    {
        if (definition == null)
        {
            return "Missing Buff";
        }

        return string.IsNullOrWhiteSpace(definition.DisplayName)
            ? definition.name
            : definition.DisplayName.Trim();
    }

    private static CombatBalanceBuffParametersJson CreateParameterSnapshot(
        BuffDefinition definition)
    {
        CombatBalanceBuffParametersJson snapshot =
            new CombatBalanceBuffParametersJson
        {
            usesStacks = definition.UsesStacks,
            activeDurationSeconds = definition.ActiveDuration,
            periodicTickIntervalSeconds = definition.PeriodicTickInterval,
            maximumStacks = definition.MaximumStacks,
            sourceApplyCooldownSeconds = definition.SourceApplyCooldown,
            overloadProtectionDurationSeconds =
                definition.OverloadProtectionDuration
        };

        snapshot.moveSpeedMultiplierAvailable =
            TryGetMoveSpeedMultiplier(definition, out float multiplier);
        snapshot.moveSpeedMultiplier = multiplier;
        return snapshot;
    }

    private static bool TryGetMoveSpeedMultiplier(
        BuffDefinition definition,
        out float multiplier)
    {
        multiplier = 0f;
        EffectDefinition appliedEffect =
            definition.GetEffectDefinition(BuffEventType.Applied);

        if (appliedEffect == null || appliedEffect.Actions == null)
        {
            return false;
        }

        IReadOnlyList<EffectAction> actions = appliedEffect.Actions;

        for (int i = 0; i < actions.Count; i++)
        {
            EffectAction action = actions[i];

            if (action == null ||
                action.ActionType != EffectActionType.SetMoveSpeedMultiplier)
            {
                continue;
            }

            multiplier = action.MoveSpeedMultiplier;
            return true;
        }

        return false;
    }

    private static string GetBuffSortKey(BuffDefinition definition)
    {
        return definition == null
            ? string.Empty
            : definition.ElementType + ":" + GetBuffDisplayName(definition);
    }

    private static float GetAverageTimeToOverload(BuffAggregate aggregate)
    {
        return aggregate.TimedOverloadCount > 0
            ? aggregate.FirstApplicationToOverloadTotal /
              aggregate.TimedOverloadCount
            : 0f;
    }

    private static float GetAverageTimeToOverload(SourceAggregate aggregate)
    {
        return aggregate.TimedOverloadCount > 0
            ? aggregate.FirstApplicationToOverloadTotal /
              aggregate.TimedOverloadCount
            : 0f;
    }

    private static string FormatSeconds(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture) + "s";
    }
}
#endif
