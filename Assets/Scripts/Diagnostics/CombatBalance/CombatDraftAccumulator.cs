#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
internal sealed class CombatDraftAccumulator
{
    internal readonly List<CombatBalanceDraftAttemptJson> draftAttempts =
        new List<CombatBalanceDraftAttemptJson>();
    internal readonly List<CombatBalancePendingDraftJson>
        terminalPendingDraftSnapshot =
            new List<CombatBalancePendingDraftJson>();
    internal readonly List<CombatBalanceInvestmentCommitJson> investmentCommits =
        new List<CombatBalanceInvestmentCommitJson>();
    internal string ResolveDraftConsumptionStatus(
        CombatBalanceDraftAttemptJson attempt)
    {
        if (attempt == null || !attempt.selectionAttempted)
        {
            return "NoSelection";
        }

        if (!attempt.heldItemCreationSucceeded)
        {
            return "NotCommitted";
        }

        for (int i = 0; i < investmentCommits.Count; i++)
        {
            if (string.Equals(
                    investmentCommits[i].draftAttemptToken,
                    attempt.attemptToken,
                    StringComparison.Ordinal))
            {
                return "Consumed";
            }
        }

        for (int i = 0; i < terminalPendingDraftSnapshot.Count; i++)
        {
            if (string.Equals(
                    terminalPendingDraftSnapshot[i].draftAttemptToken,
                    attempt.attemptToken,
                    StringComparison.Ordinal))
            {
                return "StillPending";
            }
        }

        return "MissingInvestmentCommit";
    }
    internal float ResolveFinalBuildCommitTime()
    {
        return investmentCommits.Count > 0
            ? investmentCommits[investmentCommits.Count - 1]
                .activeTimeSeconds
            : 0f;
    }
    internal int ResolveDraftOrdinal(DraftAttemptToken attemptToken)
    {
        string token = attemptToken.ToString();

        for (int i = draftAttempts.Count - 1; i >= 0; i--)
        {
            if (string.Equals(
                    draftAttempts[i].attemptToken,
                    token,
                    StringComparison.Ordinal))
            {
                return draftAttempts[i].ordinal;
            }
        }

        return 0;
    }
    internal CombatBalanceInvestmentRuntimeJson CreateInvestmentRuntimeJson(int resolvedCount)
    {
        CombatBalanceInvestmentRuntimeJson runtime =
            new CombatBalanceInvestmentRuntimeJson
            {
                commits = new List<CombatBalanceInvestmentCommitJson>(
                    investmentCommits)
            };
        runtime.committedInvestmentCount = runtime.commits.Count;

        if (runtime.commits.Count > 0)
        {
            CombatBalanceInvestmentCommitJson finalCommit =
                runtime.commits[runtime.commits.Count - 1];
            runtime.finalBuildCommitObserved = true;
            runtime.finalBuildCommitResolutionNode =
                finalCommit.resolvedMonsterCount;
            runtime.resolutionsAfterFinalBuildCommit = Mathf.Max(
                0,
                resolvedCount - finalCommit.resolvedMonsterCount);
        }

        return runtime;
    }
}
#endif
