using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

internal sealed class TowerPlacementSubmission
{
    private readonly DeployedTowerCollection members = new DeployedTowerCollection();
    private BattleRuntimeCoordinator battle;
    private DraftSystem draft;
    private TowerPlacementValidator placementValidator;
    private TowerDeployController deployController;
    private TowerUpgradeSystem towerUpgradeSystem;
    private MonsterManager monsterManager;
    private MapGeneratorBehaviour mapGenerator;
    private ulong bindingRevision;
    private bool active;
    private Operation operation;
    private Interaction interaction;
    internal IReadOnlyList<TowerInstance> DeployedTowerInstances => members.Instances;
    internal IReadOnlyList<TowerBehaviour> DeployedTowers => members.Towers;
    private IReadOnlyList<TowerBehaviour> deployedTowers => members.Towers;
    internal bool IsBusy => operation != null || interaction != null;
    internal bool CanStartOperation => active && battle != null && battle.IsBattleActive &&
        !IsBusy && draft != null && !draft.IsPendingMutationBusy &&
        towerUpgradeSystem != null && !towerUpgradeSystem.IsApplyingUpgrade;
    internal bool OwnsDeployedTower(TowerInstance tower) => active && members.Contains(tower);

    internal event Action<TowerInstance> OnTowerDeploymentCommitted;
    internal event Action<TowerInstance, TowerPlacementTopologyPlan, MonsterRouteRevisionBatch> OnPlacementRouteRevisionCommitted;
    internal event Action<TowerInvestmentCommitObservation> OnTowerInvestmentCommitted;
    internal event Action<TowerInvestmentCommitObservation> OnInvestmentEvidenceCommitted;

    internal void Bind(BattleRuntimeCoordinator runtime, DraftSystem drafts,
        TowerPlacementValidator validator, TowerDeployController deployer,
        TowerUpgradeSystem upgrades, MonsterManager monsters, MapGeneratorBehaviour map)
    {
        CloseBattleGate(); battle = runtime; draft = drafts; placementValidator = validator;
        deployController = deployer; towerUpgradeSystem = upgrades; monsterManager = monsters; mapGenerator = map;
    }
    internal void BeginBattle() { bindingRevision++; active = true; }
    internal void CloseBattleGate() { active = false; bindingRevision++; }
    internal void StopTrackedTowerCombat() { members.StopCombat(); }
    internal void DestroyTrackedTowers() { CloseBattleGate(); members.Release(); }

    // A private-current identity authorizes one submission inside the UI's longer lifetime.
    internal sealed class Interaction : IDisposable
    {
        internal readonly TowerPlacementSubmission Owner;
        internal bool Submitted;
        internal Interaction(TowerPlacementSubmission owner) { Owner = owner; }
        public void Dispose() { if (ReferenceEquals(Owner.interaction, this)) Owner.interaction = null; }
    }
    internal bool TryBeginInteraction(out Interaction lease)
    {
        lease = null;
        if (!CanStartOperation) return false;
        interaction = lease = new Interaction(this); return true;
    }
    internal sealed class Operation
    {
        internal readonly ulong Revision, ValidatorRevision, StructureRevision, WalkabilityRevision, PathRevision;
        internal readonly PendingDraftEntry Entry;
        internal readonly TowerInstance Target;
        internal readonly TowerUpgradeDefinition Upgrade;
        internal readonly bool Debug;
        internal Operation(TowerPlacementSubmission owner, PendingDraftEntry entry,
            TowerInstance target, TowerUpgradeDefinition upgrade, bool debug)
        {
            Revision = owner.bindingRevision; Entry = entry; Target = target; Upgrade = upgrade; Debug = debug;
            ValidatorRevision = owner.placementValidator.BindingRevision;
            StructureRevision = owner.mapGenerator.StructureRevision;
            WalkabilityRevision = owner.mapGenerator.WalkabilityRevision;
            PathRevision = owner.placementValidator.PathBindingRevision;
        }
    }
    private bool TryEnter(PendingDraftEntry entry, TowerInstance target, TowerUpgradeDefinition upgrade,
        bool debug, Interaction lease, out Operation current)
    {
        current = null;
        if (!active || battle == null || !battle.IsBattleActive || operation != null ||
            draft == null || draft.IsPendingMutationBusy || mapGenerator == null || placementValidator == null ||
            towerUpgradeSystem == null || towerUpgradeSystem.IsApplyingUpgrade ||
            (interaction != null && (!ReferenceEquals(interaction, lease) || lease.Submitted)) ||
            (lease != null && !ReferenceEquals(interaction, lease))) return false;
        if (!debug && !draft.PendingOwner.CanConsume(entry)) return false;
        if (lease != null) lease.Submitted = true;
        operation = current = new Operation(this, entry, target, upgrade, debug);
        return true;
    }
    internal bool IsCurrent(Operation current) => current != null && ReferenceEquals(current, operation) &&
        active && battle != null && battle.IsBattleActive && current.Revision == bindingRevision &&
        mapGenerator != null && placementValidator != null && placementValidator.ActiveMap == mapGenerator &&
        current.ValidatorRevision == placementValidator.BindingRevision &&
        current.StructureRevision == mapGenerator.StructureRevision &&
        current.WalkabilityRevision == mapGenerator.WalkabilityRevision &&
        current.PathRevision == placementValidator.PathBindingRevision;
    internal bool AuthorizesUpgrade(Operation current, TowerInstance target,
        TowerUpgradeDefinition upgrade, PendingDraftEntry entry, bool debug) =>
        IsCurrent(current) && current.Target == target && current.Upgrade == upgrade &&
        ReferenceEquals(current.Entry, entry) && current.Debug == debug && OwnsDeployedTower(target);

    internal TowerSubmissionResult SubmitDeployment(PendingDraftEntry entry, TowerPlacementCandidate candidate,
        Interaction lease = null)
    {
        if (!TryEnter(entry, null, null, false, lease, out var current))
            return TowerSubmissionResult.Reject("Deployment authority is unavailable or busy.");
        TowerBehaviour preparedTower = null;
        bool committed = false;
        try
        {
            if (entry.DraftResult.ResultType != DraftResultType.TowerDraft || candidate == null ||
                candidate.Definition != entry.TowerDefinition || candidate.Map != mapGenerator ||
                !candidate.TryClaim(placementValidator)) return TowerSubmissionResult.Reject("Invalid or expired deployment candidate.");
            if (!placementValidator.TryCreateTopologyPlan(candidate, out var plan, out var footprint,
                    out var routeExists, out string reason))
            { LogPlacementRejected("TopologyPlan", reason, footprint, routeExists); return TowerSubmissionResult.Reject(reason); }
            if (monsterManager == null || !monsterManager.TryPrepareTopologyRevision(plan, out var batch, out reason))
                return TowerSubmissionResult.Reject("Monster revision preparation failed: " + reason);
            if (deployController == null || !deployController.TryPrepareTower(candidate, plan, out preparedTower, out reason))
                return TowerSubmissionResult.Reject("Tower readiness failed: " + reason);
            if (!IsCurrent(current) || !candidate.IsCurrent(placementValidator) ||
                !candidate.MatchesPreparedTower(preparedTower) ||
                !TryValidatePreparedTowerForCommit(preparedTower, plan, out var combat, out reason) ||
                !draft.PendingOwner.TryPrepareConsumption(entry, out var consumption))
                return TowerSubmissionResult.Reject("Deployment authority or prepared geometry changed during readiness.");
            var observation = new TowerInvestmentCommitObservation(TowerInvestmentCommitKind.Deployment,
                entry.DraftAttemptToken, entry.DraftResult, preparedTower.TowerInstance, 0, preparedTower.TowerInstance.CurrentLevel);
            members.PrepareCapacity();
            batch.CombatOwnershipFingerprintBefore = CaptureExistingCombatOwnershipFingerprint();
            if (!IsCurrent(current) || !draft.PendingOwner.TryCommitConsumption(consumption))
                return TowerSubmissionResult.Reject("Deployment authority expired before commit.");
            CommitPreparedPlacement(plan, batch, preparedTower, combat);
            committed = true;
            batch.CombatOwnershipFingerprintAfter = CaptureExistingCombatOwnershipFingerprint(preparedTower.TowerInstance);
            PublishInvestmentEvidence(observation);
            PublishTowerDeploymentCommitted(preparedTower.TowerInstance, plan, batch);
            PublishInvestmentNotification(observation);
            bool warning = active && !TryRunPlacementPresentation(preparedTower);
            try { LogPlacementAccepted(plan, batch, warning ? "AcceptedWithPresentationWarning" : "Accepted"); }
            catch (Exception exception) { Debug.LogException(exception, battle); }
            return new TowerSubmissionResult(TowerSubmissionOutcome.Committed);
        }
        finally
        {
            try { if (!committed) DeployedTowerCollection.DestroyTower(preparedTower); }
            finally { operation = null; }
        }
    }

    internal TowerSubmissionResult SubmitLevelUp(PendingDraftEntry entry, TowerInstance target, Interaction lease = null)
    {
        if (!TryEnter(entry, target, null, false, lease, out var current))
            return TowerSubmissionResult.Reject("Level Up authority is unavailable or busy.");
        try
        {
            string reason = "The exact Tower Draft or target is not owned by this Battle.";
            var behaviour = members.Find(target);
            if (entry.DraftResult.ResultType != DraftResultType.TowerDraft || !OwnsDeployedTower(target) || behaviour == null)
                return TowerSubmissionResult.Reject(reason);
            if (!draft.PendingOwner.TryPrepareConsumption(entry, out var consumption) ||
                !towerUpgradeSystem.TryPrepareLevelUp(target, entry.TowerDefinition, out var level, out reason) ||
                !behaviour.TryPrepareLevelVisualRefresh(level.NextLevelConfig, out reason)) return TowerSubmissionResult.Reject(reason);
            if (!behaviour.TryGetComponent(out TowerCombatBehaviour combat)) return TowerSubmissionResult.Reject("Combat owner is missing.");
            if (!combat.TryPrepareLevelDamageRevision(level.NextLevelConfig, out var revision, out reason)) return TowerSubmissionResult.Reject(reason);
            var observation = new TowerInvestmentCommitObservation(TowerInvestmentCommitKind.LevelUp,
                entry.DraftAttemptToken, entry.DraftResult, target, level.PreviousLevel, level.NextLevel);
            if (!IsCurrent(current) || !OwnsDeployedTower(target) || !draft.PendingOwner.TryCommitConsumption(consumption))
                return TowerSubmissionResult.Reject("Level Up authority expired during preparation.");
            towerUpgradeSystem.CommitPreparedLevelUp(level);
            combat.ApplyPreparedLevelDamageRevision(revision);
            PublishInvestmentEvidence(observation);
            towerUpgradeSystem.PublishPreparedLevelUp(level);
            PublishInvestmentNotification(observation);
            if (active) RunLevelUpPresentation(behaviour);
            return new TowerSubmissionResult(TowerSubmissionOutcome.Committed);
        }
        finally { operation = null; }
    }

    internal TowerSubmissionResult SubmitUpgrade(PendingDraftEntry entry, TowerInstance target, Interaction lease = null)
    {
        var upgrade = entry?.TowerUpgradeDefinition;
        if (!TryEnter(entry, target, upgrade, false, lease, out var current))
            return TowerSubmissionResult.Reject("Upgrade authority is unavailable or busy.");
        try { return towerUpgradeSystem.ApplyAuthorizedUpgrade(current, target, upgrade, entry, draft.PendingOwner, false); }
        finally { operation = null; }
    }
    internal TowerSubmissionResult SubmitDebugUpgrade(TowerInstance target, TowerUpgradeDefinition upgrade)
    {
        if (!TryEnter(null, target, upgrade, true, null, out var current))
            return TowerSubmissionResult.Reject("Debug Upgrade authority is unavailable or busy.");
        try { return towerUpgradeSystem.ApplyAuthorizedUpgrade(current, target, upgrade, null, null, true); }
        finally { operation = null; }
    }

    private void RunLevelUpPresentation(
        TowerBehaviour levelledTower)
    {
        bool didRefreshVisual = false;

        try
        {
            didRefreshVisual = levelledTower != null &&
                               levelledTower.RefreshTowerVisual();

            if (!didRefreshVisual)
            {
                Debug.LogWarning(
                    "Tower Level Up committed with a model-refresh presentation warning.",
                    battle);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, battle);
        }

        if (didRefreshVisual)
        {
            try
            {
                levelledTower.VisualController?.PlayTowerSpawnRefreshFeedback();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, battle);
            }
        }

    }
    private void PublishTowerDeploymentCommitted(
        TowerInstance towerInstance,
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch)
    {
        System.Action<TowerInstance> handlers = OnTowerDeploymentCommitted;

        if (towerInstance == null)
        {
            return;
        }

        if (handlers != null)
        {
            System.Delegate[] subscribers = handlers.GetInvocationList();

            for (int i = 0; i < subscribers.Length; i++)
            {
                try
                {
                    ((System.Action<TowerInstance>)subscribers[i]).Invoke(
                        towerInstance);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception, battle);
                }
            }
        }

        PublishPlacementRouteRevisionCommitted(
            towerInstance,
            topologyPlan,
            revisionBatch);
        monsterManager.PublishCommittedPlacementRouteLifecycle(revisionBatch);
    }

    private void PublishPlacementRouteRevisionCommitted(
        TowerInstance towerInstance,
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch)
    {
        System.Action<
            TowerInstance,
            TowerPlacementTopologyPlan,
            MonsterRouteRevisionBatch> handlers =
                OnPlacementRouteRevisionCommitted;

        if (handlers == null)
        {
            return;
        }

        System.Delegate[] subscribers = handlers.GetInvocationList();

        for (int i = 0; i < subscribers.Length; i++)
        {
            try
            {
                ((System.Action<
                    TowerInstance,
                    TowerPlacementTopologyPlan,
                    MonsterRouteRevisionBatch>)subscribers[i]).Invoke(
                        towerInstance,
                        topologyPlan,
                        revisionBatch);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, battle);
            }
        }
    }

    private string CaptureExistingCombatOwnershipFingerprint(
        TowerInstance excludedTower = null)
    {
        List<TowerCombatBehaviour> combatRuntimes =
            new List<TowerCombatBehaviour>();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            TowerBehaviour tower = deployedTowers[i];

            if (tower == null ||
                tower.TowerInstance == excludedTower ||
                !tower.TryGetComponent(out TowerCombatBehaviour combat))
            {
                continue;
            }

            combatRuntimes.Add(combat);
        }

        combatRuntimes.Sort((left, right) =>
            left.GetInstanceID().CompareTo(right.GetInstanceID()));
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < combatRuntimes.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('|');
            }

            builder.Append(
                combatRuntimes[i].CapturePlacementOwnershipFingerprint());
        }

        return builder.ToString();
    }

    private void PublishInvestmentHandlers(
        System.Action<TowerInvestmentCommitObservation> handlers,
        TowerInvestmentCommitObservation observation)
    {
        if (handlers == null) return;
        foreach (System.Delegate subscriber in handlers.GetInvocationList())
        {
            try { ((System.Action<TowerInvestmentCommitObservation>)subscriber)(observation); }
            catch (System.Exception exception) { Debug.LogException(exception, battle); }
        }
    }

    private bool TryValidatePreparedTowerForCommit(
        TowerBehaviour preparedTower,
        TowerPlacementTopologyPlan topologyPlan,
        out TowerCombatBehaviour preparedCombat,
        out string failureReason)
    {
        preparedCombat = null;

        if (preparedTower == null ||
            preparedTower.TowerInstance == null ||
            preparedTower.VisualController == null ||
            preparedTower.VisualController.CurrentTowerModelInstance == null)
        {
            failureReason = "the prepared Tower is missing required runtime state.";
            return false;
        }

        IReadOnlyList<GridNodeBehaviour> preparedFootprint =
            preparedTower.TowerInstance.OccupiedNodes;

        if (preparedFootprint.Count != topologyPlan.Footprint.Count)
        {
            failureReason =
                "the prepared Tower footprint differs from the topology plan.";
            return false;
        }

        for (int i = 0; i < topologyPlan.Footprint.Count; i++)
        {
            if (!ContainsNode(preparedFootprint, topologyPlan.Footprint[i]))
            {
                failureReason =
                    "the prepared Tower footprint differs from the topology plan.";
                return false;
            }
        }

        if (members.Contains(preparedTower.TowerInstance) ||
            !preparedTower.TryGetComponent(out preparedCombat) ||
            !preparedCombat.IsPreparedForBattleActivation)
        {
            failureReason =
                "the prepared Tower combat runtime is not ready for activation.";
            preparedCombat = null;
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private void CommitPreparedPlacement(
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch,
        TowerBehaviour preparedTower,
        TowerCombatBehaviour preparedCombat)
    {
        for (int i = 0; i < topologyPlan.Footprint.Count; i++)
        {
            topologyPlan.Footprint[i].SetRuntimeOccupied(true);
        }

        monsterManager.ApplyPreparedMovementRevisionBatch(revisionBatch);
        members.CommitAdd(preparedTower);
        preparedCombat.ActivatePreparedBattleRuntime();
    }

    private bool TryRunPlacementPresentation(TowerBehaviour deployedTower)
    {
        bool succeeded = true;

        try
        {
            if (mapGenerator == null ||
                !mapGenerator.RefreshRuntimeTileVisuals())
            {
                succeeded = false;
            }
        }
        catch (System.Exception exception)
        {
            succeeded = false;
            Debug.LogException(exception, battle);
        }

        try
        {
            deployedTower.VisualController?.PlayTowerSpawnRefreshFeedback();
        }
        catch (System.Exception exception)
        {
            succeeded = false;
            Debug.LogException(exception, battle);
        }

        return succeeded;
    }

    private void LogPlacementRejected(
        string failureStage,
        string failureReason,
        IReadOnlyList<GridNodeBehaviour> footprint = null,
        bool? routeExists = null)
    {
        Debug.LogWarning(
            $"Tower placement transaction: Outcome=Rejected; " +
            $"FootprintGridPositions={FormatGridPositions(footprint)}; " +
            $"RouteExists={FormatRouteExists(routeExists)}; " +
            $"FailureStage={failureStage}; FailureReason={failureReason}",
            battle);
    }

    private void LogPlacementAccepted(
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch,
        string outcome)
    {
        Debug.Log(
            $"Tower placement transaction: Outcome={outcome}; " +
            $"FootprintNodes={topologyPlan.Footprint.Count}; " +
            $"FootprintGridPositions=" +
            $"{FormatGridPositions(topologyPlan.Footprint)}; " +
            $"RouteExists=True; " +
            $"AuthoritativeRouteNodes={topologyPlan.AuthoritativeRoute.Count}; " +
            $"LivingMonsters={revisionBatch.LivingMonsterCount}; " +
            $"AlreadyOnNewRoute={revisionBatch.AlreadyOnNewRouteCount}; " +
            $"ReachableRouteRejoin={revisionBatch.ReachableRouteRejoinCount}; " +
            $"ForcedRelocation={revisionBatch.ForcedRelocationCount}",
            battle);

        for (int i = 0; i < revisionBatch.Entries.Count; i++)
        {
            MonsterRouteRevisionEntry entry = revisionBatch.Entries[i];
            Debug.Log(
                $"Tower placement Monster revision: " +
                $"Monster={entry.Monster.name}; " +
                $"RevisionId={entry.RevisionId}; " +
                $"Mode={entry.Mode}; " +
                $"RelocationReason={entry.RelocationReason}; " +
                $"PrePosition={entry.CapturedWorldPosition}; " +
                $"PhysicalGrid=" +
                $"{entry.PhysicalCurrentGrid?.GridPosition.ToString() ?? "Unresolved"}; " +
                $"JoinGrid=" +
                $"{entry.JoinGrid?.GridPosition.ToString() ?? "None"}; " +
                $"RecoveryGrid=" +
                $"{entry.RecoveryGrid?.GridPosition.ToString() ?? "None"}; " +
                $"RelocationDistance=" +
                $"{(entry.HasComparableRelocationDistance ? entry.RelocationDistance.ToString() : "Uncompared")}; " +
                $"RequiresExactTargetApproach=" +
                $"{entry.RequiresExactTargetApproach}",
                entry.Monster);
        }
    }

    private static string FormatGridPositions(
        IReadOnlyList<GridNodeBehaviour> footprint)
    {
        if (footprint == null)
        {
            return "Unresolved";
        }

        StringBuilder builder = new StringBuilder("[");

        for (int i = 0; i < footprint.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            GridNodeBehaviour node = footprint[i];

            if (node == null)
            {
                builder.Append("Null");
                continue;
            }

            Vector2Int gridPosition = node.GridPosition;
            builder.Append('(');
            builder.Append(gridPosition.x);
            builder.Append(',');
            builder.Append(gridPosition.y);
            builder.Append(')');
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string FormatRouteExists(bool? routeExists)
    {
        return routeExists.HasValue
            ? routeExists.Value ? "True" : "False"
            : "Unresolved";
    }

    private static bool ContainsNode(
        IReadOnlyList<GridNodeBehaviour> nodes,
        GridNodeBehaviour targetNode)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == targetNode)
            {
                return true;
            }
        }

        return false;
    }
    internal void PublishInvestmentEvidence(TowerInvestmentCommitObservation value) => PublishInvestmentHandlers(OnInvestmentEvidenceCommitted, value);
    internal void PublishInvestmentNotification(TowerInvestmentCommitObservation value) => PublishInvestmentHandlers(OnTowerInvestmentCommitted, value);
}
