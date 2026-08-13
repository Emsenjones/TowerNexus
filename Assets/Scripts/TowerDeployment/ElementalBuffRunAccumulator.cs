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
        public SourceAggregate(string towerFamily)
        {
            TowerFamily = towerFamily;
        }

        public string TowerFamily { get; }
        public int ApplicationAttempts { get; set; }
        public int SuccessfulApplications { get; set; }
        public int Applied { get; set; }
        public int Refreshed { get; set; }
        public int Stacked { get; set; }
        public int Invalid { get; set; }
        public int BlockedBySourceCooldown { get; set; }
        public int BlockedByProtection { get; set; }
        public int Overloads { get; set; }
        public int EnteredProtection { get; set; }
        public int PeriodicTicks { get; set; }
        public int NaturalExpiriesBeforeOverload { get; set; }
        public float FirstApplicationToOverloadTotal { get; set; }
        public int TimedOverloadCount { get; set; }
    }

    private sealed class BuffAggregate
    {
        public BuffAggregate(BuffDefinition definition)
        {
            Definition = definition;
        }

        public BuffDefinition Definition { get; }
        public HashSet<int> DistinctMonsterIds { get; } = new HashSet<int>();
        public HashSet<int> DistinctSourceTowerIds { get; } = new HashSet<int>();
        public HashSet<string> SourceElements { get; } = new HashSet<string>();
        public Dictionary<string, SourceAggregate> Sources { get; } =
            new Dictionary<string, SourceAggregate>();
        public Dictionary<int, float> FirstApplicationTimesByMonster { get; } =
            new Dictionary<int, float>();
        public int ApplicationAttempts { get; set; }
        public int Applied { get; set; }
        public int Refreshed { get; set; }
        public int Stacked { get; set; }
        public int Invalid { get; set; }
        public int BlockedBySourceCooldown { get; set; }
        public int BlockedByProtection { get; set; }
        public int MaximumObservedStacks { get; set; }
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

    private readonly Dictionary<BuffDefinition, BuffAggregate> aggregates =
        new Dictionary<BuffDefinition, BuffAggregate>();

    public void Reset()
    {
        aggregates.Clear();
    }

    public void Consume(BuffRuntimeObservation observation)
    {
        BuffDefinition definition = observation.BuffDefinition;

        if (definition == null)
        {
            return;
        }

        BuffAggregate aggregate = GetOrCreateAggregate(definition);
        aggregate.MaximumObservedStacks = Mathf.Max(
            aggregate.MaximumObservedStacks,
            observation.StackCountBefore,
            observation.StackCountAfter);

        if (observation.ObservationType ==
            BuffRuntimeObservationType.ApplicationAttempt)
        {
            ConsumeApplication(aggregate, observation);
            return;
        }

        ConsumeLifecycle(aggregate, observation);
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
                displayName = GetBuffDisplayName(aggregate.Definition),
                element = aggregate.Definition.ElementType.ToString(),
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
                    GetAverageTimeToOverload(aggregate)
            };

            record.sourceElements.AddRange(aggregate.SourceElements);
            record.sourceElements.Sort(StringComparer.Ordinal);

            List<SourceAggregate> sortedSources = GetSortedSources(aggregate);

            for (int sourceIndex = 0; sourceIndex < sortedSources.Count; sourceIndex++)
            {
                SourceAggregate source = sortedSources[sourceIndex];
                record.sources.Add(new CombatBalanceBuffSourceJson
                {
                    towerFamily = source.TowerFamily,
                    applicationAttempts = source.ApplicationAttempts,
                    successfulApplications = source.SuccessfulApplications,
                    applied = source.Applied,
                    refreshed = source.Refreshed,
                    stacked = source.Stacked,
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
                    });
            }

            records.Add(record);
        }

        return records;
    }

    private static void ConsumeApplication(
        BuffAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        aggregate.ApplicationAttempts++;
        SourceAggregate source = GetOrCreateSource(aggregate, observation.SourceTower);
        source.ApplicationAttempts++;

        switch (observation.ApplyResult)
        {
            case BuffApplyResult.Applied:
                aggregate.Applied++;
                source.Applied++;
                source.SuccessfulApplications++;
                RecordSuccessfulSource(aggregate, observation);
                RecordFirstApplicationTime(aggregate, observation);
                break;
            case BuffApplyResult.Refreshed:
                aggregate.Refreshed++;
                source.Refreshed++;
                source.SuccessfulApplications++;
                RecordSuccessfulSource(aggregate, observation);
                break;
            case BuffApplyResult.Stacked:
                aggregate.Stacked++;
                source.Stacked++;
                source.SuccessfulApplications++;
                RecordSuccessfulSource(aggregate, observation);
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
        BuffAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        SourceAggregate source = GetOrCreateSource(
            aggregate,
            observation.SourceTower);

        switch (observation.LifecycleEvent)
        {
            case BuffEventType.Overload:
                aggregate.Overloads++;
                source.Overloads++;
                RecordOverloadTime(aggregate, source, observation);
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
                RecordRemoval(aggregate, source, observation);
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

    private static SourceAggregate GetOrCreateSource(
        BuffAggregate aggregate,
        TowerInstance sourceTower)
    {
        string towerFamily = GetTowerFamily(sourceTower);

        if (!aggregate.Sources.TryGetValue(towerFamily, out SourceAggregate source))
        {
            source = new SourceAggregate(towerFamily);
            aggregate.Sources.Add(towerFamily, source);
        }

        return source;
    }

    private static void RecordSuccessfulSource(
        BuffAggregate aggregate,
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
        BuffAggregate aggregate,
        BuffRuntimeObservation observation)
    {
        if (observation.OwnerMonster == null)
        {
            return;
        }

        aggregate.FirstApplicationTimesByMonster[
            observation.OwnerMonster.GetInstanceID()] = observation.ObservationTime;
    }

    private static void RecordOverloadTime(
        BuffAggregate aggregate,
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
            aggregate.FirstApplicationToOverloadTotal += Mathf.Max(
                0f,
                observation.ObservationTime - firstApplicationTime);
            aggregate.TimedOverloadCount++;
            source.FirstApplicationToOverloadTotal += Mathf.Max(
                0f,
                observation.ObservationTime - firstApplicationTime);
            source.TimedOverloadCount++;
            aggregate.FirstApplicationTimesByMonster.Remove(monsterId);
        }
    }

    private static void RecordRemoval(
        BuffAggregate aggregate,
        SourceAggregate source,
        BuffRuntimeObservation observation)
    {
        int monsterId = observation.OwnerMonster != null
            ? observation.OwnerMonster.GetInstanceID()
            : 0;

        switch (observation.RemovalReason)
        {
            case BuffRemovalReason.ActiveDurationExpired:
                if (aggregate.Definition != null &&
                    aggregate.Definition.UsesStacks &&
                    monsterId != 0 &&
                    aggregate.FirstApplicationTimesByMonster.ContainsKey(monsterId))
                {
                    aggregate.NaturalExpiriesBeforeOverload++;
                    source.NaturalExpiriesBeforeOverload++;
                }
                break;
            case BuffRemovalReason.ProtectionExpired:
                aggregate.ProtectionExpiries++;
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

        if (monsterId != 0)
        {
            aggregate.FirstApplicationTimesByMonster.Remove(monsterId);
        }
    }

    private List<BuffAggregate> GetSortedAggregates()
    {
        List<BuffAggregate> result = new List<BuffAggregate>(aggregates.Values);
        result.Sort((left, right) => string.CompareOrdinal(
            GetBuffSortKey(left.Definition),
            GetBuffSortKey(right.Definition)));
        return result;
    }

    private static List<SourceAggregate> GetSortedSources(BuffAggregate aggregate)
    {
        List<SourceAggregate> result =
            new List<SourceAggregate>(aggregate.Sources.Values);
        result.Sort((left, right) => string.CompareOrdinal(
            left.TowerFamily,
            right.TowerFamily));
        return result;
    }

    private static string GetTowerFamily(TowerInstance sourceTower)
    {
        return sourceTower != null && sourceTower.TowerDefinition != null
            ? sourceTower.TowerDefinition.TowerFamily.ToString()
            : "Unattributed";
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
