using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

internal static class IntegrationTests
{
    private static int checks;
    private static void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Get<T>(object target, string field) =>
        (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Call(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static GameObject Child(Transform root) { var go = new GameObject(); go.transform.SetParent(root, false); return go; }

    private sealed class Fixture
    {
        internal readonly GameFlowController Flow = new GameObject().AddComponent<GameFlowController>();
        internal readonly StageCompositionController Stage = new GameObject().AddComponent<StageCompositionController>();
        internal readonly BattleRuntimeCoordinator Runtime = new GameObject().AddComponent<BattleRuntimeCoordinator>();
        internal readonly CameraPanController Camera = new GameObject().AddComponent<CameraPanController>();
        internal readonly BattleHUDUI Hud = new GameObject().AddComponent<BattleHUDUI>();
        internal readonly ConfirmExitStageWindow Exit;
        internal readonly TowerInfoWindow Inspection;
        internal readonly Button Entry, Cancel, Restart, Menu;
        internal readonly List<StageDefinition> Stages = new List<StageDefinition>();
        internal readonly TMP_Text Title, Description;
        internal Fixture(bool intro = false, bool start = true)
        {
            GameObject.Activated = null; GameObject.BeforeDeactivate = null; UnityEngine.Object.BeforeInstantiate = null;
            Time.timeScale = 0.6f;
            Entry = Child(Hud.transform).AddComponent<Button>(); Set(Hud, "exitButton", Entry);
            var root = Child(Hud.transform); root.AddComponent<Image>(); Exit = root.AddComponent<ConfirmExitStageWindow>();
            var content = Child(root.transform).transform;
            Title = Child(content).AddComponent<TMP_Text>(); Title.text = "EXIT STAGE";
            Description = Child(content).AddComponent<TMP_Text>(); Description.text = "Restart or return to the main menu?";
            Cancel = Child(content).AddComponent<Button>(); Restart = Child(content).AddComponent<Button>(); Menu = Child(content).AddComponent<Button>();
            Set(Exit, "contentRoot", content); Set(Exit, "cancelButton", Cancel); Set(Exit, "restartButton", Restart); Set(Exit, "mainMenuButton", Menu);
            Set(Hud, "exitStageConfirmWindow", Exit);
            Inspection = Child(Hud.transform).AddComponent<TowerInfoWindow>();
            var infoContent = Child(Inspection.transform).transform;
            var grid = Child(infoContent).transform; grid.gameObject.AddComponent<GridLayoutGroup>();
            Set(Inspection, "contentRoot", infoContent); Set(Inspection, "upgradeContainer", grid);
            foreach (string field in new[] { "displayNameText", "killCountText", "levelText", "attackRangeText", "attackText", "attackCycleDurationText" })
                Set(Inspection, field, Child(infoContent).AddComponent<TMP_Text>());
            Set(Inspection, "towerIcon", Child(infoContent).AddComponent<Image>());
            Set(Inspection, "closeButton", Child(infoContent).AddComponent<Button>());
            var item = new GameObject().AddComponent<TowerUpgradeInfoItem>();
            Set(item, "iconImage", Child(item.transform).AddComponent<Image>());
            Set(item, "backgroundImage", Child(item.transform).AddComponent<Image>());
            Set(item, "nameText", Child(item.transform).AddComponent<TMP_Text>());
            foreach (string field in new[] { "basicUpgradeIconBackground", "behaviourUpgradeIconBackground", "elementalUpgradeIconBackground" }) Set(item, field, new Sprite());
            Set(Inspection, "upgradeItemPrefab", item); Set(Hud, "towerInfoWindow", Inspection);
            Set(Runtime, "gameFlowController", Flow); Set(Runtime, "battleHUDUI", Hud); Set(Runtime, "cameraPanController", Camera);
            Set(Stage, "stageRuntimeRoot", new GameObject().transform); Set(Stage, "battleRuntimeCoordinator", Runtime); Set(Stage, "cameraPanController", Camera);
            var first = new StageDefinition(); Stages.Add(first); Stages.Add(new StageDefinition());
            if (intro)
            {
                var tower = new TowerDefinition(); first.ShowStageIntroduction = true;
                first.TowerDraftPool.Add(tower); first.IntroducedTowers.Add(tower);
            }
            Set(Flow, "demoStages", Stages); Set(Flow, "stageCompositionController", Stage);
            Runtime.EnableProductionBindings();
            Call(Hud, "Awake"); Hud.gameObject.Dispatch("OnEnable"); Flow.gameObject.Dispatch("OnEnable");
            Check(!Exit.IsVisible && !Entry.gameObject.activeSelf, "authored active window initialized hidden before run");
            if (start) Check(Flow.RequestStartNewRun(), "production prepare/bind/start");
            Tick();
        }
        internal void Tick() { Call(Hud, "LateUpdate"); Flow.gameObject.Dispatch("Update"); }
        internal void Open()
        {
            Tick(); Entry.onClick.Invoke();
            Check(Hud.IsExitConfirmationOpen && Exit.IsVisible && Time.timeScale == 0f, "real Exit button enters production session");
        }
        internal TowerInstance Tower()
        {
            var go = new GameObject(); var tower = go.AddComponent<TowerInstance>();
            go.AddComponent<TowerCombatBehaviour>().Bind(tower, Runtime.Identity); Runtime.Submission.Members.Add(tower); return tower;
        }
        internal void End()
        {
            GameObject.Activated = null; GameObject.BeforeDeactivate = null; UnityEngine.Object.BeforeInstantiate = null; Camera.DuringBind = null;
            Flow.gameObject.SetActive(false); Hud.ClearExitConfirmationBinding(); Hud.ClearTowerInspectionBinding();
        }
    }

    private static void Main()
    {
        var f = new Fixture(true);
        Check(f.Flow.CurrentState == GameFlowState.StageIntroduction && !f.Runtime.IsBattleActive && !f.Entry.gameObject.activeSelf,
            "hidden window readiness permits preparation/Introduction without a started Battle");
        Check(f.Hud.HasExitConfirmationBinding(f.Runtime.Identity, f.Runtime.Submission, f.Runtime.ModalPause), "binding readiness independent from opening and root activation");
        Check(!f.Hud.TryOpenExitConfirmation(out _), "Introduction cannot open Exit");
        Check(f.Flow.RequestConfirmStageIntroduction(), "start after valid production readiness"); f.Open();
        f.Tick(); f.Tick();
        Check(f.Hud.IsExitConfirmationOpen && f.Exit.IsVisible && f.Runtime.IsGameplayModalBlocked,
            "Opening/Open guards do not reject their own stable session");
        Check(f.Title.text == "EXIT STAGE" && f.Description.text == "Restart or return to the main menu?", "preserve static text objects and copy");
        Check(!f.Hud.TryOpenTowerInfo(f.Tower(), out _), "Exit blocks inspection");
        f.Cancel.onClick.Invoke();
        Check(f.Runtime.IsBattleActive && f.Flow.CurrentState == GameFlowState.Battle && Time.timeScale == 0.6f && !f.Runtime.ModalPause.HasRetainedPause,
            "Cancel continues exact existing Battle at captured rate");
        Check(f.Hud.TryOpenTowerInfo(f.Tower(), out _), "inspection remains functional after Cancel"); f.Tick();
        Check(!f.Entry.interactable && !f.Hud.TryOpenExitConfirmation(out _), "inspection blocks Exit in reverse direction");
        f.Hud.CloseTowerInfo(); f.Open(); var old = f.Runtime.Identity;
        f.Restart.onClick.Invoke();
        Check(f.Flow.CurrentState == GameFlowState.StageIntroduction && f.Runtime.Identity != old && !old.IsOpenForRead &&
            !f.Exit.IsVisible && !f.Hud.IsExitConfirmationBusy && !f.Runtime.ModalPause.HasRetainedPause, "production clear and fresh bind on Restart without test cleanup callbacks");
        Check(f.Flow.RequestConfirmStageIntroduction(), "restart introduction confirmation"); f.Open(); f.Menu.onClick.Invoke(); f.Tick();
        Check(f.Flow.CurrentState == GameFlowState.MainMenu && f.Stage.IsRuntimeRetired && !f.Entry.gameObject.activeSelf && !f.Exit.IsVisible,
            "production Main Menu cleanup without test routing delegates"); f.End();

        f = new Fixture(); f.Open(); var current = f.Runtime.Identity; int preparations = f.Runtime.Preparations;
        f.Hud.gameObject.SetActive(false);
        Check(!f.Runtime.ModalPause.HasRetainedPause && !f.Exit.IsVisible && !f.Hud.OwnsExitConfirmationBinding(current), "HUD disable cleans regardless of active readiness");
        f.Hud.gameObject.SetActive(true); f.Tick();
        Check(f.Hud.IsHUDInputReady && f.Hud.HasExitConfirmationBinding(current, f.Runtime.Submission, f.Runtime.ModalPause) &&
            f.Hud.HasTowerInspectionBinding(current, f.Runtime.Submission, f.Runtime.ModalPause) && f.Camera.IsInspectionBindingReady(current, true), "actual HUD enabled event atomically rebinds all three consumers");
        Check(f.Runtime.Preparations == preparations && f.Runtime.Identity == current && !f.Exit.IsVisible, "rebind does not reinitialize Stage/Player/Draft or reopen session");
        f.Open(); f.Cancel.onClick.Invoke(); f.Open();
        f.Hud.gameObject.SetActive(false); Check(f.Runtime.TryReportResult(BattleResult.Defeat), "Battle ends while HUD disabled");
        f.Hud.gameObject.SetActive(true); f.Tick();
        Check(!f.Hud.OwnsExitConfirmationBinding(current) && !f.Hud.HasTowerInspectionBinding(current, f.Runtime.Submission, f.Runtime.ModalPause) &&
            !f.Entry.gameObject.activeSelf && !f.Exit.IsVisible, "HUD re-enable never rebinds ended Battle"); f.End();

        f = new Fixture(); f.Open(); current = f.Runtime.Identity; f.Hud.gameObject.SetActive(false); f.Camera.FailBind = true;
        f.Hud.gameObject.SetActive(true);
        Check(!f.Hud.IsHUDInputReady && !f.Hud.OwnsTowerInspectionBinding(current) && !f.Hud.OwnsExitConfirmationBinding(current),
            "partial active rebind failure rolls back all presentation ownership");
        Check(f.Flow.CurrentState == GameFlowState.MainMenu && f.Stage.IsRuntimeRetired && !f.Runtime.ModalPause.HasRetainedPause,
            "active rebind failure follows result-neutral technical failure route"); f.End();
        f = new Fixture(true); current = f.Runtime.Identity; f.Hud.gameObject.SetActive(false); f.Camera.FailBind = true;
        f.Hud.gameObject.SetActive(true);
        Check(f.Flow.CurrentState == GameFlowState.MainMenu && f.Stage.IsRuntimeRetired && !f.Runtime.IsBattleActive,
            "prepared rebind failure safely returns menu without starting Battle"); f.End();

        f = new Fixture(); var captured = f; f.Open(); current = f.Runtime.Identity;
        Set(f.Runtime, "gameFlowController", null); f.Hud.gameObject.SetActive(false);
        Check(!f.Runtime.ModalPause.HasRetainedPause && !f.Hud.OwnsExitConfirmationBinding(current) && !f.Hud.OwnsTowerInspectionBinding(current),
            "lost Flow owner cannot suppress outgoing cleanup"); f.End();

        f = new Fixture(); f.Hud.IsDraftOpen = true; f.Tick();
        Check(!f.Entry.interactable && !f.Hud.TryOpenExitConfirmation(out _) && !f.Runtime.ModalPause.HasRetainedPause,
            "Draft busy before pause acquisition blocks Exit without taking ownership");
        f.Hud.IsDraftOpen = false; f.Runtime.Submission.IsBusy = true; f.Tick();
        Check(!f.Entry.interactable && !f.Hud.TryOpenExitConfirmation(out _), "protected outer submission cleanup blocks Exit entry");
        f.Runtime.Submission.IsBusy = false; captured = f;
        bool checkedOpening = false;
        f.Hud.OnBattlefieldInputInvalidated += () =>
        {
            if (!captured.Hud.IsExitConfirmationBusy || checkedOpening) return;
            checkedOpening = true;
            Check(captured.Runtime.IsGameplayModalBlocked && !captured.Hud.TryOpenTowerInfo(captured.Tower(), out _),
                "exit Opening excludes inspection/investment before its pause is acquired");
        };
        f.Open(); Check(checkedOpening, "Opening exclusion observed through real pointer invalidation"); f.Cancel.onClick.Invoke();
        f.Hud.gameObject.Dispatch("OnEnable"); f.Hud.gameObject.Dispatch("OnEnable");
        ulong beforeClick = f.Hud.BattlefieldInputRevision; f.Open();
        Check(f.Hud.BattlefieldInputRevision == beforeClick + 1, "repeated enable/ready notifications do not duplicate button requests"); f.End();
        f = new Fixture(); f.Open(); current = f.Runtime.Identity; UnityEngine.Object.Destroy(f.Exit.gameObject);
        f.Runtime.StopBattle();
        Check(!f.Hud.OwnsExitConfirmationBinding(current) && !f.Runtime.ModalPause.HasRetainedPause, "destroyed view cannot suppress identity-based cleanup"); f.End();

        foreach (string missing in new[] { "exitButton", "exitStageConfirmWindow" })
        {
            f = new Fixture(start: false); Set(f.Hud, missing, null);
            Check(!f.Flow.RequestStartNewRun() && f.Flow.CurrentState == GameFlowState.MainMenu && !f.Runtime.IsBattleActive,
                "missing authored exit requirement prevents preparation: " + missing); f.End();
        }
        f = new Fixture(start: false); Set(f.Runtime, "gameFlowController", new GameObject().AddComponent<GameFlowController>());
        Check(!f.Flow.RequestStartNewRun() && !f.Runtime.IsBattleActive, "wrong Flow owner prevents preparation"); f.End();

        f = new Fixture(); captured = f; f.Hud.gameObject.SetActive(false); current = f.Runtime.Identity;
        f.Camera.DuringBind = () =>
        {
            Check(!captured.Hud.IsHUDInputReady && captured.Runtime.IsGameplayModalBlocked && !captured.Hud.TryOpenTowerInfo(captured.Tower(), out _),
                "partial group binding exposes no inspection or investment admission");
            captured.Runtime.StopBattle();
        };
        f.Hud.gameObject.SetActive(true);
        Check(!f.Hud.OwnsExitConfirmationBinding(current) && !f.Hud.OwnsTowerInspectionBinding(current),
            "reentrant stop during bind invalidates revision and rolls back partial bindings"); f.End();

        f = new Fixture(); captured = f; f.Hud.gameObject.SetActive(false);
        f.Camera.DuringBind = () => captured.Runtime.TryReportResult(BattleResult.Victory);
        f.Hud.gameObject.SetActive(true);
        Check(f.Flow.CurrentState == GameFlowState.StageVictory && f.Stage.IsCompositionReady &&
            !f.Hud.OwnsExitConfirmationBinding(f.Runtime.Identity), "terminal reentry during rebind preserves result runtime instead of running stale recovery"); f.End();

        f = new Fixture(); captured = f; int evidence = 0, published = 0; bool didReenter = false;
        f.Runtime.OnBattleResultEvidence += result =>
        {
            evidence++;
            Check(!captured.Runtime.Identity.IsOpenForRead && !captured.Runtime.ModalPause.IsBattleOpen(captured.Runtime.Identity), "terminal permissions closed before evidence");
            Check(!captured.Runtime.TryReportResult(BattleResult.Defeat) && !captured.Hud.TryOpenExitConfirmation(out _), "evidence cannot claim another result or exit");
            if (!didReenter) { didReenter = true; captured.Runtime.ReleasePreparedBattleRuntime(); }
        };
        f.Runtime.OnBattleResultPublished += _ => published++;
        Check(f.Runtime.TryReportResult(BattleResult.Victory), "terminal report accepted");
        Check(evidence == 1 && published == 1 && !f.Runtime.IsBattleActive && !f.Runtime.ModalPause.HasRetainedPause &&
            !Get<bool>(f.Runtime, "terminalObservationInProgress"), "evidence reentrant release finishes exactly one terminal publication"); f.End();
        f = new Fixture(); captured = f; f.Open(); evidence = 0;
        f.Runtime.OnBattleFailureEvidence += reason => { evidence++; captured.Runtime.ReleasePreparedBattleRuntime(); };
        f.Camera.FailBind = true; f.Hud.gameObject.SetActive(false); f.Hud.gameObject.SetActive(true);
        Check(evidence == 1 && f.Flow.CurrentState == GameFlowState.MainMenu && f.Stage.IsRuntimeRetired,
            "failure evidence retains ordering through deferred release and menu routing"); f.End();
        Console.WriteLine("PASS production Exit entry/bind/rebind/clear/Flow integration: " + checks + " assertions (native/gameplay/Camera endpoints doubled)");
    }
}
