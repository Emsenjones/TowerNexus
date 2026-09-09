using System;
class CommitTests
{
    class Fixture
    {
        public TowerUpgradeSystem System = new TowerUpgradeSystem();
        public TowerInstance Tower = new TowerInstance();
        public TowerUpgradeDefinition Upgrade = new TowerUpgradeDefinition();
        public TowerCombatBehaviour Combat = new TowerCombatBehaviour();
        public TowerPlacementController Placement = new TowerPlacementController();
        public BattleRuntimeCoordinator Battle = new BattleRuntimeCoordinator();
        public BattleHUDUI Hud = new BattleHUDUI();
        public PendingDraftUIItem Item = new PendingDraftUIItem();
        public Fixture()
        {
            Tower.Initialize(new TowerDefinition(), null);
            Tower.Components.Add(typeof(TowerCombatBehaviour), Combat);
            Tower.Components.Add(typeof(TowerBehaviour), new TowerBehaviour());
            Placement.Towers.Add(Tower);
            System.BindUpgradeRuntime(Battle, Placement);
            Item.DraftResult = new DraftResult { TowerUpgradeDefinition = Upgrade };
            Hud.Pending.Add(Item);
        }
        public bool Apply() => System.TryApplyHeldUpgrade(Tower, Upgrade, Item, Item.DraftResult, Hud, out _);
        public void Committed()
        {
            Check(Tower.AppliedUpgrades.Count == 1 && Tower.HasUpgrade(Upgrade), "one upgrade");
            Check(Item.IsConsumed && Hud.Pending.Count == 0 && Hud.ConsumptionCount == 1, "one reward consumed");
            Check(Combat.Baselines == 1 && Combat.Refreshes == 1, "one baseline and refresh");
            Check(Placement.EvidenceCount == 1, "one investment evidence");
            Check(Hud.ReleasedViews == 1, "only accepted operation releases its view");
            Check(!System.IsApplyingUpgrade, "operation guard released");
        }
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static int count;
    static void Run(string name, Action test) { test(); count++; Console.WriteLine("PASS " + name); }
    static void Reject(Action<Fixture> change)
    {
        var f = new Fixture(); change(f); int before = f.Tower.AppliedUpgrades.Count;
        int pendingBefore = f.Hud.Pending.Count;
        Check(!f.Apply(), "reject request");
        Check(f.Tower.AppliedUpgrades.Count == before && f.Hud.Pending.Count == pendingBefore &&
            f.Hud.ConsumptionCount == 0 && f.Combat.Baselines == 0 && f.Combat.Refreshes == 0 &&
            f.Placement.EvidenceCount == 0 && f.Hud.ReleasedViews == 0, "rejection preserves state");
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
            f.Battle.OnFailure = () => Check(f.Placement.EvidenceCount == 1 && f.Item.IsConsumed, "facts before terminal callback");
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
            f.Combat.OnRefresh = () => Check(!f.System.TryApplyDebugUpgrade(f.Tower, second, out _), "refresh guard blocks a different eligible upgrade");
            f.Tower.OnUpgradeRecorded += (t,u) => Check(!f.System.TryApplyDebugUpgrade(f.Tower, second, out _), "notification guard blocks a different eligible upgrade");
            Check(f.Apply(), "first accepted"); f.Committed(); Check(!f.Apply(), "repeat rejected"); f.Committed();
        });
        Run("synchronous release flushes evidence only once", () => {
            var f = new Fixture(); f.Combat.OnRefresh = () => {
                f.System.FlushCommittedInvestment();
                Check(f.Placement.EvidenceCount == 1 && f.Item.IsConsumed, "facts before release");
                f.Battle.IsBattleActive = false; f.Placement.Towers.Clear(); f.Hud.Pending.Clear();
            };
            Check(f.Apply(), "committed before cancellation"); f.Committed();
        });
        Run("debug has no synthetic reward or investment", () => {
            var f = new Fixture(); Check(f.System.TryApplyDebugUpgrade(f.Tower, f.Upgrade, out _), "debug accepted");
            Check(f.Tower.HasUpgrade(f.Upgrade) && !f.Item.IsConsumed && f.Hud.ConsumptionCount == 0 && f.Placement.EvidenceCount == 0, "debug authority");
        });
        Run("debug respects outer placement guard", () => {
            var f = new Fixture(); f.Placement.CanStartDraftInteraction = false;
            Check(!f.System.TryApplyDebugUpgrade(f.Tower, f.Upgrade, out _), "outer guard");
        });
        Run("same upgrade but different held result identity", () => {
            var f = new Fixture(); var otherResult = new DraftResult { TowerUpgradeDefinition = f.Upgrade };
            Check(!f.System.TryApplyHeldUpgrade(f.Tower, f.Upgrade, f.Item, otherResult, f.Hud, out _), "exact result identity required");
            Check(!f.Item.IsConsumed && f.Tower.AppliedUpgrades.Count == 0 && f.Hud.ReleasedViews == 0, "identity rejection is inert");
        });
        Run("Debug failure blocks later batch entry without reward evidence", () => {
            var f = new Fixture(); f.Combat.Result = RequiredUpgradeRefreshResult.TechnicalFailure;
            Check(!f.System.TryApplyDebugUpgrade(f.Tower, f.Upgrade, out _) && f.Battle.FailureCount == 1, "Debug technical failure");
            Check(!f.System.TryApplyDebugUpgrade(f.Tower, new TowerUpgradeDefinition(), out _), "later entry rejected");
            Check(f.Tower.AppliedUpgrades.Count == 1 && f.Hud.ConsumptionCount == 0 && f.Placement.EvidenceCount == 0, "no synthetic reward");
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
        Run("foreign target", () => Reject(f => f.Placement.Towers.Clear()));
        Run("Battle stopped", () => Reject(f => f.Battle.IsBattleActive = false));
        Run("previous or foreign reward", () => Reject(f => f.Hud.Pending.Clear()));
        Run("already consumed reward", () => Reject(f => f.Item.IsConsumed = true));
        Run("invalid token", () => Reject(f => f.Item.DraftAttemptToken = default));
        Run("wrong result", () => Reject(f => f.Item.DraftResult.TowerUpgradeDefinition = new TowerUpgradeDefinition()));
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
