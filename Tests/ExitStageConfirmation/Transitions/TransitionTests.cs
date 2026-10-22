using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

internal static class TransitionTests
{
    private static int checks;
    private static void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Get<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Phase(object target, string field, string value)
    { var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic); f.SetValue(target, Enum.Parse(f.FieldType, value)); }
    private static GameObject Child(Transform root) { var go = new GameObject(); go.transform.SetParent(root, false); return go; }

    private sealed class Fixture
    {
        internal readonly GameFlowController Flow = new GameObject().AddComponent<GameFlowController>();
        internal readonly StageCompositionController Stage = new GameObject().AddComponent<StageCompositionController>();
        internal readonly BattleRuntimeCoordinator Runtime = new GameObject().AddComponent<BattleRuntimeCoordinator>();
        internal readonly CameraPanController Camera = new GameObject().AddComponent<CameraPanController>();
        internal readonly BattleHUDUI Hud = new GameObject().AddComponent<BattleHUDUI>();
        internal readonly ConfirmExitStageWindow View;
        internal readonly Button Restart, MainMenu, Cancel;
        internal readonly List<StageDefinition> Stages = new List<StageDefinition>();
        internal ExitStageTransitionRequest LastRequest;
        internal ExitStageRequestDisposition LastDisposition;
        internal Func<ExitStageTransitionRequest, ExitStageRequestDisposition> Forward;
        internal bool OpenOnBegin;
        internal Fixture(bool intro = false)
        {
            GameObject.Activated = null; GameObject.BeforeDeactivate = null;
            UnityEngine.Object.BeforeInstantiate = null; Time.timeScale = 0.7f;
            for (int i = 0; i < 3; i++) Stages.Add(new StageDefinition { DisplayName = "Stage" + i, PlayerMaxHealth = 50 + i, FreeRerollCount = i + 1 });
            if (intro)
            {
                var tower = new TowerDefinition(); Stages[0].ShowStageIntroduction = true;
                Stages[0].TowerDraftPool.Add(tower); Stages[0].IntroducedTowers.Add(tower);
            }
            var root = Child(Hud.transform); root.AddComponent<Image>(); View = root.AddComponent<ConfirmExitStageWindow>();
            var content = Child(root.transform).transform;
            Cancel = Child(content).AddComponent<Button>(); Restart = Child(content).AddComponent<Button>(); MainMenu = Child(content).AddComponent<Button>();
            Set(View, "contentRoot", content); Set(View, "cancelButton", Cancel); Set(View, "restartButton", Restart); Set(View, "mainMenuButton", MainMenu);
            Set(Hud, "exitStageConfirmWindow", View);
            Set(Runtime, "battleHUDUI", Hud); Set(Runtime, "cameraPanController", Camera);
            Set(Stage, "stageRuntimeRoot", new GameObject().transform); Set(Stage, "battleRuntimeCoordinator", Runtime); Set(Stage, "cameraPanController", Camera);
            Set(Flow, "demoStages", Stages); Set(Flow, "stageCompositionController", Stage);
            Forward = request => Flow.RequestExitStageTransition(Runtime, request);
            // Explicit test assembly of Task006 boundaries. No production wiring is implied.
            Runtime.OnBeforeBattleCleanup += () => Hud.ClearExitConfirmationBinding();
            Runtime.OnPrepared = () => Check(Hud.BindExitConfirmation(Runtime.Identity, Runtime.Submission, Runtime.ModalPause,
                request => { LastRequest = request; LastDisposition = Forward(request); return LastDisposition; }, out _), "fresh HUD binding");
            Runtime.OnBegan = () => { if (OpenOnBegin) Check(Hud.TryOpenExitConfirmation(out _), "new Battle opens own session during startup"); };
            Flow.gameObject.Dispatch("OnEnable");
            Check(Flow.RequestStartNewRun(), "start run");
            if (Flow.CurrentState == GameFlowState.StageIntroduction) Check(Flow.RequestConfirmStageIntroduction(), "initial introduction confirm");
            Check(Hud.TryOpenExitConfirmation(out _), "open actual exit session");
        }
        internal void Tick() { Flow.gameObject.Dispatch("Update"); }
        internal void End()
        {
            GameObject.Activated = null; GameObject.BeforeDeactivate = null; UnityEngine.Object.BeforeInstantiate = null;
            Camera.DuringClear = null; Set(Runtime, "isLifecycleOperationInProgress", false); Set(Runtime, "releaseRequested", false);
            Flow.gameObject.SetActive(false); Hud.ClearExitConfirmationBinding();
        }
    }

    private static void Main()
    {
        var f = new Fixture(); var oldBattle = f.Runtime.Identity; var oldMap = f.Stage.ActiveMap;
        f.OpenOnBegin = true;
        f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.LastRequest.Accepted, "real HUD -> real Flow acceptance handshake");
        Check(f.Flow.CurrentState == GameFlowState.Battle && f.Flow.CurrentStageIndex == 0 && f.Runtime.Identity != oldBattle && !oldBattle.IsOpenForRead, "same-index fresh Battle");
        Check(oldMap.gameObject.Destroyed && !oldMap.gameObject.activeInHierarchy, "old Map retired before fresh preparation");
        Check(f.Runtime.HealthSlice == 50 && f.Runtime.RerollSlice == 1, "Stage health and Re-roll configuration supplied afresh");
        Check(f.Hud.IsExitConfirmationOpen && f.View.IsInteractionEnabled && f.Runtime.ModalPause.HasRetainedPause && Time.timeScale == 0f,
            "old synchronous return cannot hide/release new session on reused HUD and coordinator");
        Check(f.Flow.RequestExitStageTransition(f.Runtime, f.LastRequest) == ExitStageRequestDisposition.Rejected, "stale request rejects on reused coordinator"); f.End();

        f = new Fixture(true); int priorBegins = f.Runtime.Begins;
        f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.Flow.CurrentState == GameFlowState.StageIntroduction,
            "Restart honors configured Introduction");
        Check(f.Runtime.Begins == priorBegins && !f.Runtime.IsBattleActive && !f.View.IsVisible && !f.Runtime.ModalPause.HasRetainedPause, "Introduction retains prepared inactive runtime without old window/pause");
        Check(f.Flow.RequestConfirmStageIntroduction() && f.Runtime.Begins == priorBegins + 1, "confirmed Introduction starts fresh Battle"); f.End();

        f = new Fixture(); int starts = 0; var captured = f; bool nestedStart = true;
        f.Flow.OnStateChanged += state => { if (state == GameFlowState.MainMenu) { starts++; nestedStart = captured.Flow.RequestStartNewRun(); } };
        f.MainMenu.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.Flow.CurrentState == GameFlowState.MainMenu && f.Flow.CurrentStageIndex == -1, "Main Menu clears run");
        Check(starts == 1 && !nestedStart && !f.View.IsVisible && f.Stage.IsRuntimeRetired && Time.timeScale == 0.7f, "menu notification protected until retirement, exact pause restoration");
        Check(f.Flow.RequestStartNewRun() && f.Flow.CurrentStageIndex == 0, "new run admitted only after notification returns"); f.End();

        foreach (string field in new[] { "isLifecycleOperationInProgress", "isReleasingStage", "releaseRequested" })
        {
            f = new Fixture(); oldMap = f.Stage.ActiveMap; Set(f.Stage, field, true); f.Restart.onClick.Invoke();
            Check(f.LastDisposition == ExitStageRequestDisposition.Rejected && f.Hud.IsExitConfirmationOpen && f.View.IsInteractionEnabled &&
                f.Runtime.ModalPause.HasRetainedPause && f.Stage.ActiveMap == oldMap, "Stage busy rejection restores live window: " + field);
            Set(f.Stage, field, false); f.End();
        }
        foreach (string field in new[] { "isLifecycleOperationInProgress", "isReleasing", "releaseRequested" })
        {
            f = new Fixture(); Set(f.Runtime, field, true); f.MainMenu.onClick.Invoke();
            Check(f.LastDisposition == ExitStageRequestDisposition.Rejected && f.View.IsInteractionEnabled && !f.LastRequest.Accepted,
                "coordinator lifecycle rejection: " + field); Set(f.Runtime, field, false); f.End();
        }
        f = new Fixture(); Set(f.Flow, "isTransitionInProgress", true); f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Rejected && f.View.IsInteractionEnabled, "competing Flow transition rejects");
        Set(f.Flow, "isTransitionInProgress", false); f.End();
        f = new Fixture(); Phase(f.Runtime, "battleTerminalState", "Victory"); f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Rejected && !f.LastRequest.Accepted, "terminal claim wins admission"); f.End();
        f = new Fixture(); var wrong = new GameObject().AddComponent<BattleRuntimeCoordinator>(); captured = f;
        f.Forward = request => captured.Flow.RequestExitStageTransition(wrong, request); f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Rejected && f.View.IsInteractionEnabled, "wrong coordinator rejected"); f.End();
        f = new Fixture(); var wrongHud = new GameObject().AddComponent<BattleHUDUI>(); Set(f.Runtime, "battleHUDUI", wrongHud); f.MainMenu.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Rejected && f.View.IsInteractionEnabled, "wrong bound HUD rejected"); f.End();

        f = new Fixture(); captured = f; bool reentryChecked = false; int results = 0;
        f.Runtime.OnBattleResultPublished += _ => results++;
        f.Runtime.OnBeforeBattleCleanup += () =>
        {
            if (reentryChecked) return; reentryChecked = true;
            Check(!captured.Runtime.IsBattleActive && !captured.LastRequest.Battle.IsOpenForRead && captured.Runtime.Submission.Closed &&
                !captured.Runtime.ModalPause.IsBattleOpen(captured.LastRequest.Battle), "all admission closes before first cleanup observer");
            Check(!captured.Runtime.TryReportResult(BattleResult.Victory) && !captured.Runtime.TryReportResult(BattleResult.Defeat), "cleanup reentry cannot claim a terminal result");
            Check(captured.Flow.RequestExitStageTransition(captured.Runtime, captured.LastRequest) == ExitStageRequestDisposition.Rejected &&
                !captured.Flow.RequestStartNewRun(), "cleanup reentry cannot exit or Start again");
            Check(!captured.Stage.IsRuntimeRetired && !captured.Stage.CanStartFlowTransition && !captured.Stage.TryPrepareStage(captured.Stages[1]),
                "release-in-progress is not a retired Stage or valid preparation entry");
            captured.Stage.ReleaseStage(); // Idempotent reentrant release must not recurse.
        };
        f.MainMenu.onClick.Invoke();
        Check(reentryChecked && results == 0 && f.Flow.LastBattleResult == null && f.Flow.CurrentState == GameFlowState.MainMenu,
            "voluntary cancellation publishes no result"); f.End();

        foreach (bool restart in new[] { false, true })
        {
            f = new Fixture(); captured = f; int prep = f.Runtime.Preparations;
            f.Camera.DuringClear = () => { captured.Camera.DuringClear = null; Set(captured.Runtime, "isLifecycleOperationInProgress", true); };
            (restart ? f.Restart : f.MainMenu).onClick.Invoke();
            Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && !f.Stage.IsCompositionReady && !f.Stage.HasBattleBegun &&
                !f.Stage.IsRuntimeRetired && Get<bool>(f.Flow, "isTransitionInProgress"), "deferred coordinator cleanup is not mistaken for retirement");
            f.Tick(); f.Tick();
            Check(f.Runtime.Preparations == prep && !f.Flow.RequestStartNewRun(), "waiting never prepares or admits a new run");
            Check(!f.Runtime.FinishDeferredLifecycle(), "actual lifecycle finalizer drains requested release"); f.Tick();
            Check(!Get<bool>(f.Flow, "isTransitionInProgress") && f.Flow.CurrentState == (restart ? GameFlowState.Battle : GameFlowState.MainMenu),
                "release owner completes, Flow observes and continues once"); f.End();
        }

        foreach (bool throws in new[] { false, true })
        {
            f = new Fixture(); captured = f;
            f.Runtime.FailPrepare = !throws; f.Runtime.ThrowPrepare = throws;
            int menu = 0; bool startInRecovery = true;
            f.Flow.OnStateChanged += state =>
            { if (state == GameFlowState.MainMenu) { menu++; startInRecovery = captured.Flow.RequestStartNewRun(); } };
            f.Restart.onClick.Invoke();
            Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.LastRequest.Accepted && f.Flow.CurrentState == GameFlowState.MainMenu,
                "failed preparation stays Accepted and publishes Main Menu");
            Check(menu == 1 && !startInRecovery && f.Stage.IsRuntimeRetired && !f.View.IsVisible && !f.Runtime.ModalPause.HasRetainedPause,
                "failed incoming runtime retired before protected recovery notification"); f.End();
        }
        f = new Fixture(); int heard = 0;
        f.Flow.OnStateChanged += state => { if (state == GameFlowState.MainMenu) throw new Exception("menu subscriber failure"); };
        f.Flow.OnStateChanged += state => { if (state == GameFlowState.MainMenu) heard++; };
        f.MainMenu.onClick.Invoke();
        Check(heard == 1 && f.LastDisposition == ExitStageRequestDisposition.Accepted && !Get<bool>(f.Flow, "isTransitionInProgress"), "menu subscriber fault does not skip later UI or strand guard"); f.End();
        f = new Fixture(); captured = f;
        f.Runtime.OnBeforeBattleCleanup += () => captured.Flow.gameObject.SetActive(false);
        f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && !f.Runtime.IsBattleActive && f.Stage.IsRuntimeRetired &&
            f.Flow.CurrentState == GameFlowState.MainMenu && !Get<bool>(f.Flow, "isTransitionInProgress"), "disable in cleanup completes silent protected retirement"); f.End();

        f = new Fixture(); captured = f;
        f.Flow.OnStateChanged += state => { if (state == GameFlowState.StagePreparing) throw new Exception("incoming state observer fault"); };
        f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.Stage.IsRuntimeRetired && f.Flow.CurrentState == GameFlowState.MainMenu,
            "callback exception outside Stage preparation recovers under the exit guard"); f.End();
        f = new Fixture(); captured = f; int releaseAttempts = 0;
        f.Camera.DuringClear = () => { if (++releaseAttempts == 1) throw new Exception("first release fails"); };
        f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.Flow.CurrentState == GameFlowState.MainMenu && f.Stage.IsRuntimeRetired,
            "one bounded recovery retries an interrupted release and abandons Restart"); f.End();
        f = new Fixture(); captured = f; releaseAttempts = 0;
        f.Camera.DuringClear = () => { releaseAttempts++; throw new Exception("persistent release fault"); };
        f.MainMenu.onClick.Invoke(); int attemptsAfterDispatch = releaseAttempts;
        f.Tick(); f.Tick();
        Check(releaseAttempts == attemptsAfterDispatch && !f.Stage.IsRuntimeRetired && Get<bool>(f.Flow, "isTransitionInProgress") &&
            !f.Flow.RequestStartNewRun(), "failed retirement never spins/restarts or publishes an unsafe menu");
        f.Camera.DuringClear = null; f.Stage.ReleaseStage(); f.Tick();
        Check(f.Stage.IsRuntimeRetired && f.Flow.CurrentState == GameFlowState.MainMenu && !Get<bool>(f.Flow, "isTransitionInProgress"),
            "external lifecycle completion resumes a failed-retirement wait without duplicate release"); f.End();
        f = new Fixture(); captured = f;
        f.Camera.DuringClear = () => { captured.Camera.DuringClear = null; Set(captured.Runtime, "isLifecycleOperationInProgress", true); };
        f.Restart.onClick.Invoke(); f.Flow.gameObject.SetActive(false);
        Check(!f.Runtime.FinishDeferredLifecycle() && f.Stage.IsRuntimeRetired && !f.Runtime.IsBattleActive,
            "release owner completes retirement even while flow is disabled");
        f.Flow.gameObject.SetActive(true);
        Check(f.Flow.CurrentState == GameFlowState.MainMenu && !Get<bool>(f.Flow, "isTransitionInProgress") && !f.View.IsVisible,
            "re-enabled Flow drains cancelled work before accepting any new run"); f.End();

        f = new Fixture(); f.Cancel.onClick.Invoke();
        Check(f.Runtime.TryReportResult(BattleResult.Victory) && f.Flow.RequestContinueAfterVictory(), "advance before voluntary restart");
        Check(f.Hud.TryOpenExitConfirmation(out _), "open on Stage index one"); f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.Flow.CurrentStageIndex == 1 &&
            f.Stage.ActiveStage == f.Stages[1] && f.Runtime.HealthSlice == 51 && f.Runtime.RerollSlice == 2, "Restart uses current Stage, not index zero"); f.End();

        f = new Fixture(); captured = f;
        f.Runtime.OnBegan = () => captured.Runtime.TryReportResult(BattleResult.Defeat);
        f.Restart.onClick.Invoke();
        Check(f.LastDisposition == ExitStageRequestDisposition.Accepted && f.Flow.CurrentState == GameFlowState.StageDefeat &&
            f.Flow.LastBattleResult == BattleResult.Defeat, "new Battle synchronous startup result retains existing buffering contract"); f.End();

        // Existing result-screen paths retain their public admission and sequence behavior.
        f = new Fixture(); f.Cancel.onClick.Invoke();
        Check(!f.Flow.RequestRetryCurrentStage() && !f.Flow.RequestReturnToMainMenu(), "result-only entry points still reject live Battle");
        Check(f.Runtime.TryReportResult(BattleResult.Defeat) && f.Flow.CurrentState == GameFlowState.StageDefeat, "Defeat result");
        Check(f.Flow.RequestRetryCurrentStage() && f.Flow.CurrentStageIndex == 0 && f.Flow.CurrentState == GameFlowState.Battle, "Defeat Retry unchanged");
        for (int i = 0; i < 3; i++)
        {
            Check(f.Runtime.TryReportResult(BattleResult.Victory) && f.Flow.CurrentState == GameFlowState.StageVictory, "Victory result");
            Check(f.Flow.RequestContinueAfterVictory(), "Victory forward");
            Check(f.Flow.CurrentState == (i == 2 ? GameFlowState.MainMenu : GameFlowState.Battle) &&
                f.Flow.CurrentStageIndex == (i == 2 ? -1 : i + 1), "sequence including final Stage unchanged");
        }
        f.End();
        Console.WriteLine("PASS real HUD + GameFlow + Stage, production coordinator acceptance/release: " + checks + " assertions (native/gameplay preparation boundaries doubled)");
    }
}
