#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
internal static class CombatReportIntegrity
{
    internal static bool PlacementRouteBatchCountsMatch(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        int revisions = 0;

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (commit == null ||
                commit.monsters.Count != commit.aliveMonsterCountBefore ||
                commit.aliveMonsterCountBefore !=
                    commit.aliveMonsterCountAfter ||
                commit.resolvedMonsterCountBefore !=
                    commit.resolvedMonsterCountAfter)
            {
                return false;
            }

            int alreadyOnRoute = 0;
            int reachableRejoin = 0;
            int forcedRelocation = 0;

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                switch (commit.monsters[monsterIndex].mode)
                {
                    case nameof(
                        MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute):
                        alreadyOnRoute++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ReachableRouteRejoin):
                        reachableRejoin++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ForcedRelocation):
                        forcedRelocation++;
                        break;
                }
            }

            if (alreadyOnRoute != commit.alreadyOnNewRouteCount ||
                reachableRejoin != commit.reachableRouteRejoinCount ||
                forcedRelocation != commit.forcedRelocationCount)
            {
                return false;
            }

            revisions += commit.monsters.Count;
        }

        return revisions == runtime.observedMonsterRevisionCount &&
               revisions ==
                   runtime.alreadyOnNewRouteCount +
                   runtime.reachableRouteRejoinCount +
                   runtime.forcedRelocationCount;
    }

    internal static bool PlacementRouteCommitStatePreserved(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        const float epsilon = 0.0001f;

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (commit.playerHealthBefore != commit.playerHealthAfter ||
                commit.playerProgressBefore != commit.playerProgressAfter)
            {
                return false;
            }

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    commit.monsters[monsterIndex];
                bool forced = monster.mode == nameof(
                    MonsterPlacementRouteRevisionMode.ForcedRelocation);
                CombatBalanceVector3Json expected = forced
                    ? monster.preparedWorldPosition
                    : monster.capturedWorldPosition;

                if ((!forced && !monster.hasComparableCapturedPosition) ||
                    !PositionsApproximatelyEqual(
                        expected,
                        monster.immediatePostCommitPosition,
                        epsilon))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool PlacementRouteGameplayStatePreserved(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                if (!string.Equals(
                        monsters[monsterIndex].preGameplayStateFingerprint,
                        monsters[monsterIndex].postGameplayStateFingerprint,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool PlacementRouteTopologyValid(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (commit.authoritativeRoute.Count < 2 ||
                !IsOrthogonalGridPath(commit.authoritativeRoute))
            {
                return false;
            }

            for (int routeIndex = 0;
                 routeIndex < commit.authoritativeRoute.Count;
                 routeIndex++)
            {
                if (ContainsGridPosition(
                        commit.footprint,
                        commit.authoritativeRoute[routeIndex]))
                {
                    return false;
                }
            }

            CombatBalanceGridPositionJson target =
                commit.authoritativeRoute[
                    commit.authoritativeRoute.Count - 1];

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    commit.monsters[monsterIndex];

                if (monster.requiresExactTargetApproach)
                {
                    if (monster.preparedContinuation.Count != 0 ||
                        monster.joinGrid == null ||
                        !GridPositionsEqual(monster.joinGrid, target))
                    {
                        return false;
                    }

                    continue;
                }

                if (monster.preparedContinuation.Count == 0 ||
                    !GridPositionsEqual(
                        monster.preparedContinuation[
                            monster.preparedContinuation.Count - 1],
                        target) ||
                    !IsOrthogonalGridPath(monster.preparedContinuation))
                {
                    return false;
                }

                if (monster.routeSuffix.Count == 0 ||
                    monster.joinGrid == null ||
                    !GridPositionsEqual(
                        monster.routeSuffix[0],
                        monster.joinGrid) ||
                    !GridPositionsEqual(
                        monster.routeSuffix[
                            monster.routeSuffix.Count - 1],
                        target) ||
                    !IsOrthogonalGridPath(monster.routeSuffix))
                {
                    return false;
                }

                if (monster.connectorPath.Count > 0)
                {
                    if (!IsOrthogonalGridPath(monster.connectorPath) ||
                        monster.joinGrid == null ||
                        !GridPositionsEqual(
                            monster.connectorPath[
                                monster.connectorPath.Count - 1],
                            monster.joinGrid))
                    {
                        return false;
                    }

                    for (int connectorIndex = 0;
                         connectorIndex < monster.connectorPath.Count;
                         connectorIndex++)
                    {
                        if (ContainsGridPosition(
                                commit.footprint,
                                monster.connectorPath[connectorIndex]))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    internal static bool PlacementRouteCombatOwnershipPreserved(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (!string.Equals(
                    commit.combatOwnershipFingerprintBefore,
                    commit.combatOwnershipFingerprintAfter,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool PlacementRouteLifecycleConsistent(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        Dictionary<int, long> activeRevisionByMonster =
            new Dictionary<int, long>();

        for (int i = 0; i < runtime.lifecycle.Count; i++)
        {
            CombatBalancePlacementRouteLifecycleJson lifecycle =
                runtime.lifecycle[i];

            if (lifecycle.kind == nameof(
                    MonsterPlacementRouteLifecycleKind.Started))
            {
                if (activeRevisionByMonster.ContainsKey(
                        lifecycle.monsterInstanceId))
                {
                    return false;
                }

                activeRevisionByMonster[lifecycle.monsterInstanceId] =
                    lifecycle.revisionId;
            }
            else if (IsPlacementConnectorTerminal(lifecycle.kind) &&
                     lifecycle.kind != nameof(
                         MonsterPlacementRouteLifecycleKind.ActiveAtRunEnd))
            {
                if (activeRevisionByMonster.TryGetValue(
                        lifecycle.monsterInstanceId,
                        out long activeRevision) &&
                    activeRevision == lifecycle.revisionId)
                {
                    activeRevisionByMonster.Remove(
                        lifecycle.monsterInstanceId);
                }
            }
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    monsters[monsterIndex];
                int startedCount = 0;
                int terminalCount = 0;
                int relocationCount = 0;

                for (int lifecycleIndex = 0;
                     lifecycleIndex < runtime.lifecycle.Count;
                     lifecycleIndex++)
                {
                    CombatBalancePlacementRouteLifecycleJson lifecycle =
                        runtime.lifecycle[lifecycleIndex];

                    if (lifecycle.revisionId != monster.revisionId)
                    {
                        continue;
                    }

                    if (lifecycle.kind == nameof(
                        MonsterPlacementRouteLifecycleKind.Started))
                    {
                        startedCount++;
                    }
                    else if (lifecycle.kind == nameof(
                        MonsterPlacementRouteLifecycleKind.RelocationApplied))
                    {
                        relocationCount++;
                    }
                    else if (IsPlacementConnectorTerminal(lifecycle.kind))
                    {
                        terminalCount++;
                    }
                }

                if (monster.requiresConnector)
                {
                    if (startedCount != 1 || terminalCount != 1)
                    {
                        return false;
                    }
                }
                else if (monster.joinedAtCommit)
                {
                    if (startedCount != 0 || terminalCount != 1)
                    {
                        return false;
                    }
                }
                else if (startedCount != 0 || terminalCount != 0)
                {
                    return false;
                }

                bool forced = monster.mode == nameof(
                    MonsterPlacementRouteRevisionMode.ForcedRelocation);

                if ((forced && relocationCount != 1) ||
                    (!forced && relocationCount != 0))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool PlacementRouteForcedRelocationUsageValid(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null ||
            !Enum.TryParse(
                runtime.forcedRelocationExpectation,
                out PlacementRouteForcedRelocationExpectation expectation))
        {
            return false;
        }

        switch (expectation)
        {
            case PlacementRouteForcedRelocationExpectation.RequireZero:
                return runtime.forcedRelocationCount == 0;
            case PlacementRouteForcedRelocationExpectation.RequireDiagnosed:
                return runtime.forcedRelocationCount > 0 &&
                       AllForcedRelocationsDiagnosed(runtime);
            case PlacementRouteForcedRelocationExpectation.Ignore:
                return AllForcedRelocationsDiagnosed(runtime);
            default:
                return false;
        }
    }

    internal static bool AllForcedRelocationsDiagnosed(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    monsters[monsterIndex];

                if (monster.mode == nameof(
                        MonsterPlacementRouteRevisionMode.ForcedRelocation) &&
                    (monster.forcedRelocationReason == nameof(
                         MonsterForcedRelocationReason.None) ||
                     monster.recoveryGrid == null))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static CombatBalancePlacementRouteMonsterJson
        FindPlacementRouteMonster(
            CombatBalancePlacementRouteRuntimeJson runtime,
            long revisionId)
    {
        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                if (monsters[monsterIndex].revisionId == revisionId)
                {
                    return monsters[monsterIndex];
                }
            }
        }

        return null;
    }

    internal static bool IsPlacementConnectorTerminal(string kind)
    {
        return kind == nameof(MonsterPlacementRouteLifecycleKind.Joined) ||
               kind == nameof(
                   MonsterPlacementRouteLifecycleKind.Superseded) ||
               kind == nameof(
                   MonsterPlacementRouteLifecycleKind.MonsterResolvedBeforeJoin) ||
               kind == nameof(
                   MonsterPlacementRouteLifecycleKind.ActiveAtRunEnd);
    }

    internal static bool PositionsApproximatelyEqual(
        CombatBalanceVector3Json left,
        CombatBalanceVector3Json right,
        float epsilon)
    {
        return left != null &&
               right != null &&
               Mathf.Abs(left.x - right.x) <= epsilon &&
               Mathf.Abs(left.y - right.y) <= epsilon &&
               Mathf.Abs(left.z - right.z) <= epsilon;
    }

    internal static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) &&
               !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) &&
               !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) &&
               !float.IsInfinity(value.z);
    }

    internal static bool IsOrthogonalGridPath(
        IReadOnlyList<CombatBalanceGridPositionJson> path)
    {
        if (path == null || path.Count == 0)
        {
            return false;
        }

        for (int i = 1; i < path.Count; i++)
        {
            if (path[i - 1] == null ||
                path[i] == null ||
                Mathf.Abs(path[i].x - path[i - 1].x) +
                Mathf.Abs(path[i].z - path[i - 1].z) != 1)
            {
                return false;
            }
        }

        return path[0] != null;
    }

    internal static bool ContainsGridPosition(
        IReadOnlyList<CombatBalanceGridPositionJson> positions,
        CombatBalanceGridPositionJson candidate)
    {
        if (positions == null || candidate == null)
        {
            return false;
        }

        for (int i = 0; i < positions.Count; i++)
        {
            if (GridPositionsEqual(positions[i], candidate))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool GridPositionsEqual(
        CombatBalanceGridPositionJson left,
        CombatBalanceGridPositionJson right)
    {
        return left != null &&
               right != null &&
               left.x == right.x &&
               left.z == right.z;
    }

    internal static bool DamageDiagnosticsCountsMatch(
        CombatBalanceDamageDiagnosticsJson diagnostics)
    {
        if (diagnostics == null)
        {
            return false;
        }

        int towerResolutionCount = 0;
        int towerApplicationCount = 0;
        int towerAppliedDamage = 0;
        int towerRejectionCount = 0;
        int fixedResolutionCount = 0;
        int fixedApplicationCount = 0;
        int fixedAppliedDamage = 0;

        for (int i = 0; i < diagnostics.towerScaledSignatures.Count; i++)
        {
            CombatBalanceTowerScaledDamageJson record =
                diagnostics.towerScaledSignatures[i];
            towerResolutionCount += record.resolutionCount;
            towerApplicationCount += record.successfulApplicationCount;
            towerAppliedDamage += record.appliedDamageTotal;
        }

        for (int i = 0; i < diagnostics.towerScaledRejections.Count; i++)
        {
            towerRejectionCount +=
                diagnostics.towerScaledRejections[i].rejectionCount;
        }

        for (int i = 0; i < diagnostics.fixedBuffSignatures.Count; i++)
        {
            CombatBalanceFixedBuffDamageJson record =
                diagnostics.fixedBuffSignatures[i];
            fixedResolutionCount += record.resolutionCount;
            fixedApplicationCount += record.successfulApplicationCount;
            fixedAppliedDamage += record.appliedDamageTotal;
        }

        return towerResolutionCount == diagnostics.towerScaledResolutionCount &&
               towerApplicationCount ==
               diagnostics.towerScaledSuccessfulApplicationCount &&
               towerAppliedDamage == diagnostics.towerScaledAppliedDamageTotal &&
               towerRejectionCount == diagnostics.towerScaledRejectedCount &&
               fixedResolutionCount == diagnostics.fixedBuffResolutionCount &&
               fixedApplicationCount ==
               diagnostics.fixedBuffSuccessfulApplicationCount &&
               fixedAppliedDamage == diagnostics.fixedBuffAppliedDamageTotal;
    }

}
#endif
