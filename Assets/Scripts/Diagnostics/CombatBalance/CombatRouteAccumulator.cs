#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using static CombatReportIntegrity;
internal sealed class CombatRouteAccumulator
{
    internal readonly List<CombatBalancePlacementRouteCommitJson>
        placementRouteCommits =
            new List<CombatBalancePlacementRouteCommitJson>();
    internal readonly List<CombatBalancePlacementRouteLifecycleJson>
        placementRouteLifecycle =
            new List<CombatBalancePlacementRouteLifecycleJson>();
    internal readonly Dictionary<int, TowerRouteDamageCoverageAggregate>
        towerRouteDamageCoverageByTowerInstanceId =
            new Dictionary<int, TowerRouteDamageCoverageAggregate>();
    internal sealed class TowerRouteDamageCoverageAggregate
    {
        public int TowerInstanceId { get; set; }
        public string TowerDisplayName { get; set; }
        public string TowerFamily { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int LocatedDamageApplications { get; set; }
        public int UnresolvedRouteCellApplicationCount { get; set; }
        public int EffectiveDamage { get; set; }
        public int LocatedEffectiveDamage { get; set; }
        public int UnresolvedRouteCellEffectiveDamage { get; set; }
        public int KillingBlows { get; set; }
        public int LocatedKillingBlows { get; set; }
        public int UnresolvedRouteCellKillingBlows { get; set; }
        public HashSet<int> DamagedMonsterInstanceIds { get; } =
            new HashSet<int>();
        public Dictionary<string, TowerRouteDamageCellAggregate> Cells { get; } =
            new Dictionary<string, TowerRouteDamageCellAggregate>();
    }
    internal sealed class TowerRouteDamageCellAggregate
    {
        public int PlacementCommitOrdinal { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int EffectiveDamage { get; set; }
        public int KillingBlows { get; set; }
        public HashSet<int> DamagedMonsterInstanceIds { get; } =
            new HashSet<int>();
    }
    internal CombatBalancePlacementRouteRuntimeJson
        CreatePlacementRouteRuntimeJson(PlacementRouteForcedRelocationExpectation placementRouteForcedRelocationExpectation, float activeTime)
    {
        CombatBalancePlacementRouteRuntimeJson runtime =
            new CombatBalancePlacementRouteRuntimeJson
            {
                forcedRelocationExpectation =
                    placementRouteForcedRelocationExpectation.ToString(),
                committedPlacementCount = placementRouteCommits.Count
            };
        HashSet<long> started = new HashSet<long>();
        HashSet<long> terminated = new HashSet<long>();

        for (int i = 0; i < placementRouteCommits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit =
                placementRouteCommits[i];
            runtime.commits.Add(commit);
            runtime.observedMonsterRevisionCount += commit.monsters.Count;

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                switch (commit.monsters[monsterIndex].mode)
                {
                    case nameof(
                        MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute):
                        runtime.alreadyOnNewRouteCount++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ReachableRouteRejoin):
                        runtime.reachableRouteRejoinCount++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ForcedRelocation):
                        runtime.forcedRelocationCount++;
                        break;
                }
            }
        }

        for (int i = 0; i < placementRouteLifecycle.Count; i++)
        {
            CombatBalancePlacementRouteLifecycleJson observation =
                placementRouteLifecycle[i];
            runtime.lifecycle.Add(observation);

            if (observation.kind ==
                nameof(MonsterPlacementRouteLifecycleKind.Started))
            {
                started.Add(observation.revisionId);
            }
            else if (IsPlacementConnectorTerminal(observation.kind))
            {
                terminated.Add(observation.revisionId);
            }
        }

        foreach (long revisionId in started)
        {
            if (terminated.Contains(revisionId))
            {
                continue;
            }

            CombatBalancePlacementRouteMonsterJson revision =
                FindPlacementRouteMonster(runtime, revisionId);
            runtime.lifecycle.Add(
                new CombatBalancePlacementRouteLifecycleJson
                {
                    revisionId = revisionId,
                    monsterInstanceId = revision != null
                        ? revision.monsterInstanceId
                        : 0,
                    spawnOrdinal = revision != null
                        ? revision.spawnOrdinal
                        : 0,
                    kind = nameof(
                        MonsterPlacementRouteLifecycleKind.ActiveAtRunEnd),
                    resolutionReason = nameof(
                        MonsterPlacementRouteResolutionReason.None),
                    activeTimeSeconds = activeTime
                });
        }

        runtime.lifecycleObservationCount = runtime.lifecycle.Count;
        return runtime;
    }
    internal CombatBalanceTowerRouteDamageCoverageRuntimeJson
        CreateTowerRouteDamageCoverageRuntimeJson(
            IReadOnlyList<CombatBalanceTowerJson> towers)
    {
        CombatBalanceTowerRouteDamageCoverageRuntimeJson runtime =
            new CombatBalanceTowerRouteDamageCoverageRuntimeJson();

        if (towers == null)
        {
            return runtime;
        }

        for (int i = 0; i < towers.Count; i++)
        {
            CombatBalanceTowerJson tower = towers[i];

            if (tower == null)
            {
                continue;
            }

            towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                tower.instanceId,
                out TowerRouteDamageCoverageAggregate aggregate);
            CombatBalanceTowerRouteDamageCoverageJson coverage =
                CreateTowerRouteDamageCoverageJson(tower, aggregate);
            runtime.towers.Add(coverage);
            runtime.successfulDamageApplications +=
                coverage.successfulDamageApplications;
            runtime.locatedDamageApplications +=
                coverage.locatedDamageApplications;
            runtime.unresolvedRouteCellApplicationCount +=
                coverage.unresolvedRouteCellApplicationCount;
            runtime.effectiveDamage += coverage.effectiveDamage;
            runtime.locatedEffectiveDamage += coverage.locatedEffectiveDamage;
            runtime.unresolvedRouteCellEffectiveDamage +=
                coverage.unresolvedRouteCellEffectiveDamage;
            runtime.killingBlows += coverage.killingBlows;
            runtime.locatedKillingBlows += coverage.locatedKillingBlows;
            runtime.unresolvedRouteCellKillingBlows +=
                coverage.unresolvedRouteCellKillingBlows;
        }

        runtime.observedTowerCount = runtime.towers.Count;

        for (int leftIndex = 0;
             leftIndex < runtime.towers.Count;
             leftIndex++)
        {
            CombatBalanceTowerRouteDamageCoverageJson left =
                runtime.towers[leftIndex];

            for (int rightIndex = leftIndex + 1;
                 rightIndex < runtime.towers.Count;
                 rightIndex++)
            {
                CombatBalanceTowerRouteDamageCoverageJson right =
                    runtime.towers[rightIndex];
                runtime.overlaps.Add(
                    CreateTowerRouteDamageOverlapJson(left, right));
            }
        }

        return runtime;
    }

    internal static CombatBalanceTowerRouteDamageCoverageJson
        CreateTowerRouteDamageCoverageJson(
            CombatBalanceTowerJson tower,
            TowerRouteDamageCoverageAggregate aggregate)
    {
        CombatBalanceTowerRouteDamageCoverageJson coverage =
            new CombatBalanceTowerRouteDamageCoverageJson
            {
                towerInstanceId = tower.instanceId,
                towerDeploymentOrdinal = tower.deploymentOrdinal,
                towerDisplayName = tower.displayName ?? string.Empty,
                towerFamily = tower.family ?? string.Empty
            };

        if (aggregate == null)
        {
            return coverage;
        }

        coverage.successfulDamageApplications =
            aggregate.SuccessfulDamageApplications;
        coverage.locatedDamageApplications =
            aggregate.LocatedDamageApplications;
        coverage.unresolvedRouteCellApplicationCount =
            aggregate.UnresolvedRouteCellApplicationCount;
        coverage.effectiveDamage = aggregate.EffectiveDamage;
        coverage.locatedEffectiveDamage = aggregate.LocatedEffectiveDamage;
        coverage.unresolvedRouteCellEffectiveDamage =
            aggregate.UnresolvedRouteCellEffectiveDamage;
        coverage.killingBlows = aggregate.KillingBlows;
        coverage.locatedKillingBlows = aggregate.LocatedKillingBlows;
        coverage.unresolvedRouteCellKillingBlows =
            aggregate.UnresolvedRouteCellKillingBlows;
        coverage.distinctDamagedMonsterCount =
            aggregate.DamagedMonsterInstanceIds.Count;

        List<TowerRouteDamageCellAggregate> cells =
            new List<TowerRouteDamageCellAggregate>(aggregate.Cells.Values);
        cells.Sort(CompareTowerRouteDamageCells);
        HashSet<string> distinctGridCells = new HashSet<string>();

        for (int i = 0; i < cells.Count; i++)
        {
            TowerRouteDamageCellAggregate cell = cells[i];
            distinctGridCells.Add(CreateRouteGridCellKey(cell.X, cell.Z));
            coverage.routeCells.Add(
                new CombatBalanceTowerRouteDamageCellJson
                {
                    placementCommitOrdinal = cell.PlacementCommitOrdinal,
                    x = cell.X,
                    z = cell.Z,
                    successfulDamageApplications =
                        cell.SuccessfulDamageApplications,
                    effectiveDamage = cell.EffectiveDamage,
                    killingBlows = cell.KillingBlows,
                    distinctDamagedMonsterCount =
                        cell.DamagedMonsterInstanceIds.Count
                });
        }

        coverage.distinctDamageRouteCellCount = distinctGridCells.Count;
        return coverage;
    }

    internal CombatBalanceTowerRouteDamageOverlapJson
        CreateTowerRouteDamageOverlapJson(
            CombatBalanceTowerRouteDamageCoverageJson left,
            CombatBalanceTowerRouteDamageCoverageJson right)
    {
        towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
            left.towerInstanceId,
            out TowerRouteDamageCoverageAggregate leftAggregate);
        towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
            right.towerInstanceId,
            out TowerRouteDamageCoverageAggregate rightAggregate);

        return new CombatBalanceTowerRouteDamageOverlapJson
        {
            towerAInstanceId = left.towerInstanceId,
            towerADeploymentOrdinal = left.towerDeploymentOrdinal,
            towerADisplayName = left.towerDisplayName ?? string.Empty,
            towerAFamily = left.towerFamily ?? string.Empty,
            towerBInstanceId = right.towerInstanceId,
            towerBDeploymentOrdinal = right.towerDeploymentOrdinal,
            towerBDisplayName = right.towerDisplayName ?? string.Empty,
            towerBFamily = right.towerFamily ?? string.Empty,
            sharedDamageRouteCellCount = CountSharedRouteGridCells(
                leftAggregate,
                rightAggregate),
            sharedRoutePhaseCellCount = CountSharedRoutePhaseCells(
                leftAggregate,
                rightAggregate),
            sharedDamagedMonsterCount = CountSharedMonsterInstances(
                leftAggregate,
                rightAggregate)
        };
    }

    internal static int CompareTowerRouteDamageCells(
        TowerRouteDamageCellAggregate left,
        TowerRouteDamageCellAggregate right)
    {
        int phaseComparison = left.PlacementCommitOrdinal.CompareTo(
            right.PlacementCommitOrdinal);

        if (phaseComparison != 0)
        {
            return phaseComparison;
        }

        int xComparison = left.X.CompareTo(right.X);
        return xComparison != 0 ? xComparison : left.Z.CompareTo(right.Z);
    }

    internal static int CountSharedRouteGridCells(
        TowerRouteDamageCoverageAggregate left,
        TowerRouteDamageCoverageAggregate right)
    {
        return CountSharedStrings(
            CreateRouteGridCellKeys(left),
            CreateRouteGridCellKeys(right));
    }

    internal static int CountSharedRoutePhaseCells(
        TowerRouteDamageCoverageAggregate left,
        TowerRouteDamageCoverageAggregate right)
    {
        HashSet<string> leftKeys = left != null
            ? new HashSet<string>(left.Cells.Keys)
            : new HashSet<string>();
        HashSet<string> rightKeys = right != null
            ? new HashSet<string>(right.Cells.Keys)
            : new HashSet<string>();
        return CountSharedStrings(leftKeys, rightKeys);
    }

    internal static int CountSharedMonsterInstances(
        TowerRouteDamageCoverageAggregate left,
        TowerRouteDamageCoverageAggregate right)
    {
        HashSet<int> leftIds = left != null
            ? left.DamagedMonsterInstanceIds
            : new HashSet<int>();
        HashSet<int> rightIds = right != null
            ? right.DamagedMonsterInstanceIds
            : new HashSet<int>();
        HashSet<int> smaller = leftIds.Count <= rightIds.Count
            ? leftIds
            : rightIds;
        HashSet<int> larger = ReferenceEquals(smaller, leftIds)
            ? rightIds
            : leftIds;
        int sharedCount = 0;

        foreach (int value in smaller)
        {
            if (larger.Contains(value))
            {
                sharedCount++;
            }
        }

        return sharedCount;
    }

    internal static HashSet<string> CreateRouteGridCellKeys(
        TowerRouteDamageCoverageAggregate aggregate)
    {
        HashSet<string> keys = new HashSet<string>();

        if (aggregate == null)
        {
            return keys;
        }

        foreach (TowerRouteDamageCellAggregate cell in aggregate.Cells.Values)
        {
            keys.Add(CreateRouteGridCellKey(cell.X, cell.Z));
        }

        return keys;
    }

    internal static string CreateRouteGridCellKey(int x, int z)
    {
        return x.ToString(CultureInfo.InvariantCulture) + ":" +
               z.ToString(CultureInfo.InvariantCulture);
    }

    internal static int CountSharedStrings(
        HashSet<string> left,
        HashSet<string> right)
    {
        HashSet<string> smaller = left.Count <= right.Count ? left : right;
        HashSet<string> larger = ReferenceEquals(smaller, left) ? right : left;
        int sharedCount = 0;

        foreach (string value in smaller)
        {
            if (larger.Contains(value))
            {
                sharedCount++;
            }
        }

        return sharedCount;
    }

}
#endif
