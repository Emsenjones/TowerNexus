// Managed transaction boundary doubles. These do not simulate Unity lifecycle or physics.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class MonoBehaviour
    {
        public string name = "test";
        public readonly Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public bool TryGetComponent<T>(out T value) where T : class
        { value = Components.TryGetValue(typeof(T), out object found) ? found as T : null; return value != null; }
    }
    public static class Debug { public static void LogException(Exception e, object context) {} }
    public static class Mathf { public static int Min(params int[] values) { return Math.Min(values[0], Math.Min(values[1], values[2])); } }
}
public class GridNodeBehaviour {}
public enum TowerFamily { Archer, Magic }
public enum TowerBehaviourPackageType { None, Example }
public enum TowerUpgradeLayer { Basic, Behaviour, Elemental }
public class TowerUpgradeDefinition
{
    public string name = "Upgrade";
    public TowerFamily TowerFamily;
    public TowerUpgradeLayer UpgradeLayer;
    public TowerBehaviourPackageType BehaviourPackageType;
    public int RequiredTowerLevel = 1;
    public bool Valid = true;
    public bool IsValid() => Valid;
}
public class TowerDefinition
{
    public TowerFamily TowerFamily;
    private readonly TowerLevelConfig level = new TowerLevelConfig { Level = 1 };
    public TowerLevelConfig GetLevelConfig(int n) => n == 1 ? level : null;
    public int GetMaxConfiguredLevel() => 3;
}
public class TowerLevelConfig { public int Level; public bool IsValid() => true; }
public enum DraftResultType { TowerDraft, TowerUpgradeDraft }
public class DraftResult
{
    public DraftResultType ResultType = DraftResultType.TowerUpgradeDraft;
    public TowerUpgradeDefinition TowerUpgradeDefinition;
}
public struct DraftAttemptToken { public bool IsValid; }
public class PendingDraftUIItem
{
    public DraftAttemptToken DraftAttemptToken = new DraftAttemptToken { IsValid = true };
    public DraftResult DraftResult;
    public bool IsConsumed;
}
internal struct PreparedPendingDraftConsumption { internal PendingDraftUIItem Item; }
public class BattleHUDUI
{
    public readonly List<PendingDraftUIItem> Pending = new List<PendingDraftUIItem>();
    public int ConsumptionCount, ReleasedViews;
    internal bool TryPreparePendingDraftConsumption(PendingDraftUIItem item, out PreparedPendingDraftConsumption prepared)
    { prepared = new PreparedPendingDraftConsumption { Item = item }; return Pending.Contains(item) && !item.IsConsumed; }
    internal void CommitPreparedPendingDraftConsumption(PreparedPendingDraftConsumption prepared)
    { Pending.Remove(prepared.Item); prepared.Item.IsConsumed = true; ConsumptionCount++; }
    internal void ReleaseConsumedPendingDraftView(PendingDraftUIItem item) { ReleasedViews++; }
}
public enum TowerInvestmentCommitKind { Upgrade }
public struct TowerInvestmentCommitObservation
{
    public TowerInvestmentCommitObservation(TowerInvestmentCommitKind kind, DraftAttemptToken token,
        DraftResult result, TowerInstance tower, int previous, int current) { Tower = tower; }
    public TowerInstance Tower;
}
public class TowerPlacementController
{
    public bool CanStartDraftInteraction = true;
    public readonly List<TowerInstance> Towers = new List<TowerInstance>();
    public int EvidenceCount, NotificationCount;
    public Action OnEvidence, OnNotification;
    public bool OwnsDeployedTower(TowerInstance tower) => Towers.Contains(tower);
    public void PublishInvestmentEvidence(TowerInvestmentCommitObservation observation) { EvidenceCount++; OnEvidence?.Invoke(); }
    public void PublishInvestmentNotification(TowerInvestmentCommitObservation observation) { NotificationCount++; OnNotification?.Invoke(); }
}
public class BattleRuntimeCoordinator
{
    public bool IsBattleActive = true;
    public int FailureCount;
    public Action OnFailure;
    internal void FailCommittedUpgrade(TowerUpgradeSystem owner, string reason)
    { owner.FlushCommittedInvestment(); FailureCount++; IsBattleActive = false; OnFailure?.Invoke(); }
}
public class TowerBehaviour { public Visual VisualController = new Visual(); }
public class Visual { public Action OnPlay; public void PlayUpgradeAppliedFeedback() { OnPlay?.Invoke(); } }
public enum RequiredUpgradeRefreshResult { Applied, NotRequired, TechnicalFailure }
internal struct PreparedTowerCombatUpgradeRevision {}
public class TowerCombatBehaviour
{
    public bool Ready = true;
    public int Baselines, Refreshes;
    public Action OnRefresh;
    public RequiredUpgradeRefreshResult Result = RequiredUpgradeRefreshResult.Applied;
    internal bool TryPrepareUpgradeRevision(TowerInstance tower, TowerUpgradeDefinition upgrade,
        out PreparedTowerCombatUpgradeRevision revision, out string reason)
    { revision = default; reason = "Injected readiness result"; return Ready; }
    internal void CommitPreparedUpgradeBaseline(PreparedTowerCombatUpgradeRevision revision) { Baselines++; }
    internal RequiredUpgradeRefreshResult RefreshCommittedUpgrade(TowerUpgradeDefinition upgrade,
        PreparedTowerCombatUpgradeRevision revision, out string reason)
    { Refreshes++; reason = "Injected refresh result"; OnRefresh?.Invoke(); return Result; }
}
