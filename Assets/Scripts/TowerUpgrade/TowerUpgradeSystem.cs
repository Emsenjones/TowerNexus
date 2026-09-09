using System.Collections.Generic;
using UnityEngine;

internal readonly struct PreparedTowerLevelUp
{
    internal PreparedTowerLevelUp(
        TowerInstance targetTower,
        int previousLevel,
        TowerLevelConfig nextLevelConfig)
    {
        TargetTower = targetTower;
        PreviousLevel = previousLevel;
        NextLevelConfig = nextLevelConfig;
    }

    internal TowerInstance TargetTower { get; }
    internal int PreviousLevel { get; }
    internal TowerLevelConfig NextLevelConfig { get; }
    internal int NextLevel => NextLevelConfig.Level;
}

public class TowerUpgradeSystem : MonoBehaviour
{
    public const int SupportedMaximumTowerLevel = 3;

    private readonly Dictionary<TowerFamily, int> stageMaximumTowerLevels =
        new Dictionary<TowerFamily, int>();

    public bool HasStageLevelRules { get; private set; }

    public bool TryBindStageLevelRules(
        IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions,
        out string failureReason)
    {
        ClearStageLevelRules();

        if (!TryResolveStageMaximumTowerLevels(
                upgradeDefinitions,
                stageMaximumTowerLevels,
                out failureReason))
        {
            return false;
        }

        HasStageLevelRules = true;
        failureReason = string.Empty;
        return true;
    }

    public void ClearStageLevelRules()
    {
        stageMaximumTowerLevels.Clear();
        HasStageLevelRules = false;
    }

    public int GetStageMaximumTowerLevel(TowerFamily towerFamily)
    {
        if (!HasStageLevelRules)
        {
            return 0;
        }

        return stageMaximumTowerLevels.TryGetValue(
            towerFamily,
            out int maximumTowerLevel)
            ? maximumTowerLevel
            : 1;
    }

    public static bool TryResolveStageMaximumTowerLevels(
        IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions,
        Dictionary<TowerFamily, int> resolvedMaximumLevels,
        out string failureReason)
    {
        if (resolvedMaximumLevels == null)
        {
            failureReason = "Resolved maximum-level output is missing.";
            return false;
        }

        resolvedMaximumLevels.Clear();

        if (upgradeDefinitions == null)
        {
            failureReason = "Tower Upgrade Draft pool is null.";
            return false;
        }

        Dictionary<TowerFamily, HashSet<int>> requiredLevelsByFamily =
            new Dictionary<TowerFamily, HashSet<int>>();

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[i];

            if (upgradeDefinition == null)
            {
                failureReason = $"Tower Upgrade Draft pool entry {i} is missing.";
                resolvedMaximumLevels.Clear();
                return false;
            }

            if (!upgradeDefinition.IsValid())
            {
                failureReason =
                    $"Tower Upgrade definition '{upgradeDefinition.name}' failed owner validation.";
                resolvedMaximumLevels.Clear();
                return false;
            }

            int requiredTowerLevel = upgradeDefinition.RequiredTowerLevel;

            if (!IsSupportedRequiredTowerLevel(requiredTowerLevel))
            {
                failureReason =
                    $"Tower Upgrade definition '{upgradeDefinition.name}' requires " +
                    $"Tower Level {requiredTowerLevel}, outside the supported range " +
                    $"1-{SupportedMaximumTowerLevel}.";
                resolvedMaximumLevels.Clear();
                return false;
            }

            TowerFamily towerFamily = upgradeDefinition.TowerFamily;

            if (!requiredLevelsByFamily.TryGetValue(
                    towerFamily,
                    out HashSet<int> requiredLevels))
            {
                requiredLevels = new HashSet<int>();
                requiredLevelsByFamily.Add(towerFamily, requiredLevels);
            }

            requiredLevels.Add(requiredTowerLevel);

            if (!resolvedMaximumLevels.TryGetValue(
                    towerFamily,
                    out int currentMaximum) ||
                requiredTowerLevel > currentMaximum)
            {
                resolvedMaximumLevels[towerFamily] = requiredTowerLevel;
            }
        }

        foreach (KeyValuePair<TowerFamily, int> pair in resolvedMaximumLevels)
        {
            HashSet<int> requiredLevels = requiredLevelsByFamily[pair.Key];

            for (int level = 2; level <= pair.Value; level++)
            {
                if (requiredLevels.Contains(level))
                {
                    continue;
                }

                failureReason =
                    $"TowerFamily '{pair.Key}' reaches Stage Tower Level {pair.Value} " +
                    $"but no Stage Upgrade first becomes eligible at Level {level}.";
                resolvedMaximumLevels.Clear();
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    public bool CanLevelUpTower(
        TowerInstance targetTower,
        TowerDefinition draftTowerDefinition,
        out int nextLevel)
    {
        nextLevel = 0;

        if (targetTower == null || draftTowerDefinition == null)
        {
            return false;
        }

        TowerDefinition targetDefinition = targetTower.TowerDefinition;

        if (targetDefinition == null)
        {
            return false;
        }

        if (targetDefinition.TowerFamily != draftTowerDefinition.TowerFamily)
        {
            return false;
        }

        if (!HasStageLevelRules)
        {
            return false;
        }

        int candidateNextLevel = targetTower.CurrentLevel + 1;
        TowerLevelConfig candidateNextConfig =
            targetDefinition.GetLevelConfig(candidateNextLevel);
        int maxAllowedLevel = Mathf.Min(
            targetTower.GetMaxConfiguredLevel(),
            SupportedMaximumTowerLevel,
            GetStageMaximumTowerLevel(targetDefinition.TowerFamily));

        if (maxAllowedLevel <= 0 ||
            candidateNextLevel > maxAllowedLevel ||
            !targetTower.CanCommitLevelConfig(candidateNextConfig))
        {
            return false;
        }

        nextLevel = candidateNextLevel;
        return true;
    }

    internal bool TryPrepareLevelUp(
        TowerInstance targetTower,
        TowerDefinition draftTowerDefinition,
        out PreparedTowerLevelUp preparedLevelUp,
        out string failureReason)
    {
        preparedLevelUp = default;

        if (!CanLevelUpTower(
                targetTower,
                draftTowerDefinition,
                out int nextLevel))
        {
            failureReason = "the Tower Level-Up request is not eligible.";
            return false;
        }

        TowerLevelConfig nextLevelConfig =
            targetTower.TowerDefinition.GetLevelConfig(nextLevel);

        if (!targetTower.CanCommitLevelConfig(nextLevelConfig))
        {
            failureReason = "the next Tower Level configuration is invalid.";
            return false;
        }

        preparedLevelUp = new PreparedTowerLevelUp(
            targetTower,
            targetTower.CurrentLevel,
            nextLevelConfig);
        failureReason = string.Empty;
        return true;
    }

    internal void CommitPreparedLevelUp(PreparedTowerLevelUp preparedLevelUp)
    {
        preparedLevelUp.TargetTower.CommitPreparedLevel(
            preparedLevelUp.NextLevelConfig);
    }

    internal void PublishPreparedLevelUp(PreparedTowerLevelUp preparedLevelUp)
    {
        preparedLevelUp.TargetTower.PublishLevelChangedSafely(
            preparedLevelUp.PreviousLevel,
            preparedLevelUp.NextLevel);
    }

    public bool CanApplyUpgrade(
        TowerInstance targetTower,
        TowerUpgradeDefinition upgradeDefinition,
        out string failureReason)
    {
        if (targetTower == null)
        {
            failureReason = "Target tower is missing.";
            return false;
        }

        if (upgradeDefinition == null)
        {
            failureReason = "Upgrade definition is missing.";
            return false;
        }

        int requiredTowerLevel = upgradeDefinition.RequiredTowerLevel;

        if (!IsSupportedRequiredTowerLevel(requiredTowerLevel))
        {
            failureReason = $"Required Tower Level {requiredTowerLevel} is not supported by v1 upgrade rules.";
            return false;
        }

        TowerDefinition targetDefinition = targetTower.TowerDefinition;

        if (targetDefinition == null)
        {
            failureReason = "Target tower definition is missing.";
            return false;
        }

        if (targetDefinition.TowerFamily != upgradeDefinition.TowerFamily)
        {
            failureReason = $"Upgrade TowerFamily '{upgradeDefinition.TowerFamily}' does not match target tower TowerFamily '{targetDefinition.TowerFamily}'.";
            return false;
        }

        if (!upgradeDefinition.IsValid())
        {
            failureReason = $"Upgrade definition '{upgradeDefinition.name}' failed validation.";
            return false;
        }

        if (targetTower.CurrentLevel < requiredTowerLevel)
        {
            failureReason = $"Target tower level {targetTower.CurrentLevel} does not satisfy Required Tower Level {requiredTowerLevel}.";
            return false;
        }

        if (targetTower.HasUpgrade(upgradeDefinition))
        {
            failureReason = $"Target tower already has upgrade '{upgradeDefinition.name}'.";
            return false;
        }

        if (upgradeDefinition.UpgradeLayer == TowerUpgradeLayer.Behaviour &&
            upgradeDefinition.BehaviourPackageType != TowerBehaviourPackageType.None &&
            targetTower.HasBehaviourPackage(upgradeDefinition.BehaviourPackageType))
        {
            failureReason = $"Target tower already has Behaviour package '{upgradeDefinition.BehaviourPackageType}'.";
            return false;
        }

        if (upgradeDefinition.UpgradeLayer == TowerUpgradeLayer.Elemental &&
            targetTower.HasElementalUpgrade())
        {
            failureReason = "Target tower already has an Elemental Layer upgrade.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private BattleRuntimeCoordinator battleRuntime;
    private TowerPlacementController placement;
    private TowerInvestmentCommitObservation committedInvestment;
    private bool hasUnpublishedInvestment;
    public bool IsApplyingUpgrade { get; private set; }

    internal void BindUpgradeRuntime(BattleRuntimeCoordinator coordinator,
        TowerPlacementController controller)
    {
        battleRuntime = coordinator;
        placement = controller;
    }

    internal void FlushCommittedInvestment()
    {
        if (!hasUnpublishedInvestment) return;
        hasUnpublishedInvestment = false;
        placement.PublishInvestmentEvidence(committedInvestment);
    }

    internal bool TryApplyHeldUpgrade(TowerInstance targetTower,
        TowerUpgradeDefinition upgradeDefinition, PendingDraftUIItem heldItem,
        DraftResult heldResult, BattleHUDUI hud, out string failureReason) =>
        TryApplyUpgradeCore(targetTower, upgradeDefinition, heldItem,
            heldResult, hud, false, out failureReason);

#if UNITY_EDITOR
    public bool TryApplyDebugUpgrade(TowerInstance targetTower,
        TowerUpgradeDefinition upgradeDefinition, out string failureReason) =>
        TryApplyUpgradeCore(targetTower, upgradeDefinition, null,
            null, null, true, out failureReason);
#endif

    private bool TryApplyUpgradeCore(TowerInstance targetTower,
        TowerUpgradeDefinition upgradeDefinition, PendingDraftUIItem heldItem,
        DraftResult heldResult, BattleHUDUI hud, bool isDebug,
        out string failureReason)
    {
        failureReason = "Upgrade operation is busy or the active Battle no longer owns the target.";
        if (IsApplyingUpgrade || battleRuntime == null || !battleRuntime.IsBattleActive ||
            placement == null || (isDebug && !placement.CanStartDraftInteraction) ||
            !placement.OwnsDeployedTower(targetTower)) return false;

        bool committed = false;
        IsApplyingUpgrade = true;
        try
        {
            if (!CanApplyUpgrade(targetTower, upgradeDefinition, out failureReason)) return false;
            PreparedPendingDraftConsumption consumption = default;
            if (!isDebug && (hud == null || heldItem == null ||
                !heldItem.DraftAttemptToken.IsValid || heldResult == null ||
                heldItem.DraftResult != heldResult ||
                heldResult.ResultType != DraftResultType.TowerUpgradeDraft ||
                heldResult.TowerUpgradeDefinition != upgradeDefinition ||
                !hud.TryPreparePendingDraftConsumption(heldItem, out consumption)))
            {
                failureReason = "The exact held Upgrade Draft is no longer consumable.";
                return false;
            }
            if (!targetTower.TryGetComponent(out TowerCombatBehaviour combat))
            {
                failureReason = "The target combat owner is missing.";
                return false;
            }
            if (!combat.TryPrepareUpgradeRevision(targetTower, upgradeDefinition,
                    out PreparedTowerCombatUpgradeRevision revision, out failureReason)) return false;
            targetTower.PrepareUpgradeCapacity();
            var observation = isDebug ? default : new TowerInvestmentCommitObservation(
                TowerInvestmentCommitKind.Upgrade, heldItem.DraftAttemptToken,
                heldResult, targetTower, targetTower.CurrentLevel, targetTower.CurrentLevel);

            // No callbacks or runtime entity creation in this semantic commit section.
            targetTower.CommitPreparedUpgrade(upgradeDefinition);
            combat.CommitPreparedUpgradeBaseline(revision);
            if (!isDebug) hud.CommitPreparedPendingDraftConsumption(consumption);
            committedInvestment = observation;
            hasUnpublishedInvestment = !isDebug;
            committed = true;

            RequiredUpgradeRefreshResult refresh;
            try { refresh = combat.RefreshCommittedUpgrade(upgradeDefinition, revision, out failureReason); }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
                failureReason = "Required upgrade refresh threw: " + exception.Message;
                refresh = RequiredUpgradeRefreshResult.TechnicalFailure;
            }

            // Also flushed by Stop/Release if synchronous runtime callbacks end the Stage.
            FlushCommittedInvestment();
            if (refresh == RequiredUpgradeRefreshResult.TechnicalFailure)
            {
                battleRuntime.FailCommittedUpgrade(this, failureReason);
                if (!isDebug) placement.PublishInvestmentNotification(observation);
                return false;
            }

            if (!isDebug) placement.PublishInvestmentNotification(observation);
            if (targetTower != null) targetTower.PublishUpgradeRecordedSafely(upgradeDefinition);
            if (battleRuntime.IsBattleActive && targetTower != null)
            {
                try
                {
                    if (targetTower.TryGetComponent(out TowerBehaviour behaviour))
                        behaviour.VisualController?.PlayUpgradeAppliedFeedback();
                }
                catch (System.Exception exception) { Debug.LogException(exception, this); }
            }
            failureReason = string.Empty;
            return true;
        }
        finally
        {
            try
            {
                FlushCommittedInvestment();
                if (committed && !isDebug && heldItem != null && hud != null)
                    hud.ReleaseConsumedPendingDraftView(heldItem);
            }
            finally { IsApplyingUpgrade = false; }
        }
    }

    public static bool IsSupportedRequiredTowerLevel(int requiredTowerLevel)
    {
        return requiredTowerLevel >= 1 &&
               requiredTowerLevel <= SupportedMaximumTowerLevel;
    }
}
