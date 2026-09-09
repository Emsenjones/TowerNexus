using System;
class CommitTests
{
    class Fixture : SubmissionFixture
    {
        public void Committed()
        {
            Check(Tower.AppliedUpgrades.Count == 1 && Tower.HasUpgrade(Upgrade), "one upgrade");
            Check(Item.IsConsumed && Owner.Held.Count == 0, "one reward consumed");
            Check(Combat.Baselines == 1 && Combat.Refreshes == 1, "one baseline and refresh");
            Check(EvidenceCount == 1, "one investment evidence");
            Check(!System.IsApplyingUpgrade && !Submission.IsBusy, "operation guards released");
        }
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static int count;
    static void Run(string name, Action test) { test(); count++; Console.WriteLine("PASS " + name); }
    static void Reject(Action<Fixture> change)
    {
        var f = new Fixture(); change(f); int before = f.Tower.AppliedUpgrades.Count;
        int pendingBefore = f.Owner.Held.Count;
        Check(!f.Apply(), "reject request");
        Check(f.Tower.AppliedUpgrades.Count == before && f.Owner.Held.Count == pendingBefore &&
            f.Combat.Baselines == 0 && f.Combat.Refreshes == 0 &&
            f.EvidenceCount == 0, "rejection preserves state");
    }
    static void Main()
    {
        foreach (TowerUpgradeLayer layer in Enum.GetValues(typeof(TowerUpgradeLayer)))
            Run("valid " + layer, () => { var f = new Fixture(); f.Upgrade.UpgradeLayer = layer; Check(f.Apply(), "accepted"); f.Committed(); });
        Run("observer exception cannot strand reward or suppress later observer", () => {
            var f = new Fixture(); bool later = false;
            f.Tower.OnUpgradeRecorded += (t,u) => { Check(f.Item.IsConsumed, "consume before callback"); throw new Exception("fault"); };
            f.Tower.OnUpgradeRecorded += (t,u) => later = true;
            Check(f.Apply() && later, "isolated notification"); f.Committed();
        });
        Run("presentation exception preserves commit", () => {
            var f = new Fixture(); f.Tower.TryGetComponent(out TowerBehaviour behaviour);
            behaviour.VisualController.OnPlay = () => { throw new Exception("VFX fault"); };
            Check(f.Apply(), "accepted despite VFX"); f.Committed();
        });
        Run("required refresh exception terminates after evidence", () => {
            var f = new Fixture(); f.Combat.OnRefresh = () => { throw new Exception("required fault"); };
            f.Battle.OnFailure = () => Check(f.EvidenceCount == 1 && f.Item.IsConsumed, "facts before terminal callback");
            Check(!f.Apply() && f.Battle.FailureCount == 1, "technical failure"); f.Committed();
            Check(!f.Apply(), "no replay"); f.Committed();
        });
        Run("explicit creation failure uses same terminal route", () => {
            var f = new Fixture(); f.Combat.Result = RequiredUpgradeRefreshResult.TechnicalFailure;
            Check(!f.Apply() && f.Battle.FailureCount == 1, "explicit failure"); f.Committed();
        });
        Run("no active entity is normal", () => {
            var f = new Fixture(); f.Combat.Result = RequiredUpgradeRefreshResult.NotRequired;
            Check(f.Apply() && f.Battle.FailureCount == 0, "not required"); f.Committed();
        });
        Run("reentrant acceptance rejected during refresh and notifications", () => {
            var f = new Fixture(); var second = new TowerUpgradeDefinition();
            f.Combat.OnRefresh = () => Check(!(f.System.ApplyDebugUpgrade(f.Tower, second).Outcome == TowerSubmissionOutcome.Committed), "refresh guard blocks a different eligible upgrade");
            f.Tower.OnUpgradeRecorded += (t,u) => Check(!(f.System.ApplyDebugUpgrade(f.Tower, second).Outcome == TowerSubmissionOutcome.Committed), "notification guard blocks a different eligible upgrade");
            Check(f.Apply(), "first accepted"); f.Committed(); Check(!f.Apply(), "repeat rejected"); f.Committed();
        });
        Run("synchronous release flushes evidence only once", () => {
            var f = new Fixture(); f.Combat.OnRefresh = () => {
                f.System.FlushCommittedInvestment();
                Check(f.EvidenceCount == 1 && f.Item.IsConsumed, "facts before release");
                f.Battle.IsBattleActive = false; f.Submission.DestroyTrackedTowers(); f.Owner.Clear();
            };
            Check(f.Apply(), "committed before cancellation"); f.Committed();
        });
        Run("debug has no synthetic reward or investment", () => {
            var f = new Fixture(); Check((f.System.ApplyDebugUpgrade(f.Tower, f.Upgrade).Outcome == TowerSubmissionOutcome.Committed), "debug accepted");
            Check(f.Tower.HasUpgrade(f.Upgrade) && !f.Item.IsConsumed && f.EvidenceCount == 0, "debug authority");
        });
        Run("debug respects outer placement guard", () => {
            var f = new Fixture(); Check(f.Submission.TryBeginInteraction(out _), "outer lease");
            Check(!(f.System.ApplyDebugUpgrade(f.Tower, f.Upgrade).Outcome == TowerSubmissionOutcome.Committed), "outer guard");
        });
        Run("wrong immutable reward type", () => {
            var f = new Fixture(); var other = f.Grant(DraftResult.CreateTowerDraft(f.Definition));
            Check(!f.Submission.SubmitUpgrade(other, f.Tower).IsCommitted && !other.IsConsumed && f.Tower.AppliedUpgrades.Count == 0, "type rejection is inert");
        });
        Run("Debug failure blocks later batch entry without reward evidence", () => {
            var f = new Fixture(); f.Combat.Result = RequiredUpgradeRefreshResult.TechnicalFailure;
            Check(!(f.System.ApplyDebugUpgrade(f.Tower, f.Upgrade).Outcome == TowerSubmissionOutcome.Committed) && f.Battle.FailureCount == 1, "Debug technical failure");
            Check(!(f.System.ApplyDebugUpgrade(f.Tower, new TowerUpgradeDefinition()).Outcome == TowerSubmissionOutcome.Committed), "later entry rejected");
            Check(f.Tower.AppliedUpgrades.Count == 1 && f.EvidenceCount == 0, "no synthetic reward");
        });
        Run("observer stop skips presentation and retains evidence", () => {
            var f = new Fixture(); f.Tower.TryGetComponent(out TowerBehaviour behaviour); bool played = false;
            behaviour.VisualController.OnPlay = () => played = true;
            f.Tower.OnUpgradeRecorded += (t,u) => f.Battle.IsBattleActive = false;
            Check(f.Apply() && !played, "no presentation after stop"); f.Committed();
        });
        Run("wrong family", () => Reject(f => f.Upgrade.TowerFamily = TowerFamily.Magic));
        Run("required level", () => Reject(f => f.Upgrade.RequiredTowerLevel = 2));
        Run("invalid config", () => Reject(f => f.Upgrade.Valid = false));
        Run("combat not ready", () => Reject(f => f.Combat.Ready = false));
        Run("missing combat", () => Reject(f => f.Tower.Components.Remove(typeof(TowerCombatBehaviour))));
        Run("foreign target", () => Reject(f => f.Submission.DestroyTrackedTowers()));
        Run("Battle stopped", () => Reject(f => f.Battle.IsBattleActive = false));
        Run("previous or foreign reward", () => Reject(f => f.Owner.Clear()));
        Run("already consumed reward", () => Reject(f => { f.Owner.TryPrepareConsumption(f.Item, out var ticket); f.Owner.TryCommitConsumption(ticket); }));
        Run("stop during preparation invalidates actual model ticket", () => {
            var f = new Fixture(); f.Combat.OnPrepare = () => f.Owner.Stop();
            Check(!f.Apply() && !f.Item.IsConsumed && f.Owner.Held.Count == 1 &&
                f.Tower.AppliedUpgrades.Count == 0 && f.Combat.Baselines == 0, "precommit stop is inert");
        });
        Run("same numeric identity from different owner rejected", () => {
            var f = new Fixture(); var other = new Fixture();
            Check(f.Item.BattleGeneration == other.Item.BattleGeneration && f.Item.Sequence == other.Item.Sequence, "same numbers");
            Check(!other.Submission.SubmitUpgrade(f.Item, other.Tower).IsCommitted, "exact owner");
            Check(!f.Item.IsConsumed && f.Tower.AppliedUpgrades.Count == 0, "no mutation");
        });
        Run("mismatched reward family", () => Reject(f => f.Item.TowerUpgradeDefinition.TowerFamily = TowerFamily.Magic));
        Run("duplicate upgrade", () => Reject(f => f.Tower.CommitPreparedUpgrade(f.Upgrade)));
        Run("duplicate package", () => Reject(f => {
            f.Upgrade.UpgradeLayer = TowerUpgradeLayer.Behaviour; f.Upgrade.BehaviourPackageType = TowerBehaviourPackageType.Example;
            f.Tower.CommitPreparedUpgrade(new TowerUpgradeDefinition { UpgradeLayer = TowerUpgradeLayer.Behaviour, BehaviourPackageType = TowerBehaviourPackageType.Example });
        }));
        Run("Elemental capacity", () => Reject(f => {
            f.Upgrade.UpgradeLayer = TowerUpgradeLayer.Elemental;
            f.Tower.CommitPreparedUpgrade(new TowerUpgradeDefinition { UpgradeLayer = TowerUpgradeLayer.Elemental });
        }));
        Console.WriteLine(count + " managed tests passed; Unity runtime acceptance remains separate.");
    }
}
