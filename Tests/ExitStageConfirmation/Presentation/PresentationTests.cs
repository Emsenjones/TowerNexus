using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

internal static class ExitPresentationTests
{
    private static int checks;
    private static void Check(bool value, string reason)
    { checks++; if (!value) throw new Exception(reason); }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Get<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Frame(BattleHUDUI hud) => hud.GetType().GetMethod("LateUpdate",
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
    private static GameObject Child(Transform parent)
    { var go = new GameObject(); go.transform.SetParent(parent, false); return go; }

    private sealed class Fixture
    {
        internal readonly BattleHUDUI Hud;
        internal readonly ConfirmExitStageWindow View;
        internal readonly Button Cancel, Restart, MainMenu;
        internal readonly TMP_Text Title, Description;
        internal readonly RectTransform Content;
        internal BattleCombatBinding Battle;
        internal readonly BattleModalPauseAuthority Pause = new BattleModalPauseAuthority();
        internal readonly TowerPlacementSubmission Members = new TowerPlacementSubmission();
        internal Func<ExitStageTransitionRequest, ExitStageRequestDisposition> Handler;
        internal int Failures;
        internal Fixture(bool initiallyActive = false)
        {
            GameObject.Activated = null;
            GameObject.BeforeDeactivate = null;
            Time.timeScale = 0.65f;
            Hud = new GameObject().AddComponent<BattleHUDUI>();
            var root = new GameObject();
            root.AddComponent<Image>();
            View = root.AddComponent<ConfirmExitStageWindow>();
            Content = Child(root.transform).transform;
            Title = Child(Content).AddComponent<TMP_Text>(); Title.text = "EXIT STAGE";
            Description = Child(Content).AddComponent<TMP_Text>(); Description.text = "Restart or return to the main menu?";
            Cancel = Child(Content).AddComponent<Button>();
            Restart = Child(Content).AddComponent<Button>();
            MainMenu = Child(Content).AddComponent<Button>();
            Set(View, "contentRoot", Content);
            Set(View, "cancelButton", Cancel); Set(View, "restartButton", Restart); Set(View, "mainMenuButton", MainMenu);
            root.SetActive(initiallyActive);
            Set(Hud, "exitStageConfirmWindow", View);
            BindFreshBattle();
        }
        internal void BindFreshBattle()
        {
            var manager = new GameObject().AddComponent<MonsterManager>();
            BattleCombatBinding battle = null;
            battle = new BattleCombatBinding(manager, reason =>
            { Failures++; Pause.RevokeBattle(battle); battle.Close(); });
            Battle = battle; manager.CombatBinding = Battle; Battle.Open();
            Pause.BindBattle(Battle); Pause.OpenBattle(Battle);
            Check(Hud.BindExitConfirmation(Battle, Members, Pause,
                request => Handler == null ? ExitStageRequestDisposition.Rejected : Handler(request), out var reason), "bind: " + reason);
        }
        internal bool Open() => Hud.TryOpenExitConfirmation(out _);
        internal void RevokeAndClear()
        {
            var outgoing = Battle;
            Pause.RevokeBattle(outgoing); outgoing.Close();
            Hud.ClearExitConfirmationBinding(); Pause.CancelBattle(outgoing);
        }
        internal void End()
        {
            GameObject.Activated = null; GameObject.BeforeDeactivate = null;
            Hud.ClearExitConfirmationBinding(); Pause.CancelBattle(Battle);
        }
    }

    private static void Main()
    {
        var f = new Fixture(true);
        Check(!f.View.gameObject.activeSelf && !f.View.IsInteractionEnabled, "bind hides an authored active root");
        Check(f.View.TryValidateReferences(out _), "our own button lock is not invalid authoring");
        int activated = 0; var captured = f;
        GameObject.Activated = go =>
        {
            if (go != captured.View.gameObject) return;
            activated++;
            Check(Time.timeScale == 0f && captured.Pause.CurrentKind == BattleModalKind.ExitConfirmation, "pause before exposure");
            var group = go.GetComponent<CanvasGroup>();
            Check(captured.View.IsVisible && !captured.View.IsInteractionEnabled && group.blocksRaycasts,
                "Opening is visible and blocking but non-interactive");
            captured.Restart.onClick.Invoke();
        };
        Check(f.Open() && activated == 1 && f.Hud.IsExitConfirmationOpen, "open");
        Check(f.View.IsInteractionEnabled && f.Cancel.interactable && f.Restart.interactable && f.MainMenu.interactable, "all actions enabled after ownership validation");
        Check(f.Title.text == "EXIT STAGE" && f.Description.text == "Restart or return to the main menu?", "preserve author copy");
        Check(!f.Open(), "repeat opening rejects");
        f.Cancel.onClick.Invoke();
        Check(!f.View.IsVisible && !f.Hud.IsExitConfirmationBusy && !f.Pause.HasRetainedPause && Time.timeScale == 0.65f, "Cancel restores exact captured rate");
        Check(f.Battle.IsOpenForRead && f.Failures == 0, "Cancel retains live Battle");
        GameObject.Activated = null;
        f.Title.text = "Author-edited title"; f.Description.text = "Author-edited description";
        Check(f.Open(), "reopen"); f.Hud.CancelExitConfirmation(); f.Hud.CancelExitConfirmation();
        Check(f.Title.text == "Author-edited title" && f.Description.text == "Author-edited description",
            "reopening and cleanup preserve independent author text without view bindings");
        Check(Time.timeScale == 0.65f && !f.Hud.IsExitConfirmationBusy, "idempotent local cancellation"); f.End();

        foreach (var kind in new[] { BattleModalKind.Draft, BattleModalKind.TowerInspection, BattleModalKind.ExitConfirmation })
        {
            f = new Fixture();
            Check(f.Pause.TryAcquire(f.Battle, kind, new object(), out var handle, out _), "existing modal acquire");
            Check(!f.Open() && f.Pause.Owns(handle) && Time.timeScale == 0f, "reject retained modal without mutation");
            f.Pause.Release(handle); f.End();
        }
        f = new Fixture(); Check(f.Open(), "reverse overlap setup");
        foreach (var kind in new[] { BattleModalKind.Draft, BattleModalKind.TowerInspection })
            Check(!f.Pause.TryAcquire(f.Battle, kind, new object(), out _, out _), "exit prevents other pause ownership");
        f.End();

        foreach (var field in new[] { "contentRoot", "cancelButton", "restartButton", "mainMenuButton" })
        {
            f = new Fixture(); Set(f.View, field, null);
            Check(!f.Open() && !f.Pause.HasRetainedPause && !f.Hud.IsExitConfirmationBusy, "missing reference rollback: " + field); f.End();
        }
        f = new Fixture(); Set(f.View, "restartButton", f.Cancel);
        Check(!f.Open(), "distinct buttons required"); f.End();
        f = new Fixture(); f.View.GetComponent<Image>().raycastTarget = false;
        Check(!f.Open() && !f.Pause.HasRetainedPause, "root must block input"); f.End();
        f = new Fixture(); f.Members.IsBusy = true;
        Check(!f.Open(), "protected submission/outer cleanup rejects"); f.End();
        f = new Fixture(); f.Members.CanStartOperation = false;
        Check(!f.Open(), "pending mutation/Upgrade admission rejects"); f.End();
        f = new Fixture(); f.Hud.IsDraftOpen = true;
        Check(!f.Open(), "Draft busy rejects"); f.End();
        f = new Fixture(); var phase = typeof(BattleHUDUI).GetField("inspectionPhase", BindingFlags.Instance | BindingFlags.NonPublic);
        phase.SetValue(f.Hud, Enum.Parse(phase.FieldType, "Opening"));
        Check(!f.Open(), "inspection before pause acquisition rejects"); f.End();
        f = new Fixture(); Get<TowerPlacementController>(f.Hud, "towerPlacementController").IsDragging = true;
        Check(!f.Open(), "drag rejects"); f.End();
        f = new Fixture(); Get<TowerPlacementController>(f.Hud, "towerPlacementController").CanStartDraftInteraction = false;
        Check(!f.Open(), "placement protected cleanup rejects"); f.End();

        f = new Fixture(); captured = f; int invalidations = 0;
        f.Hud.OnBattlefieldInputInvalidated += () =>
        {
            invalidations++;
            captured.Hud.CancelExitConfirmation();
            Check(!captured.Hud.BindExitConfirmation(captured.Battle, captured.Members, captured.Pause, null, out _), "cannot rebind inside Opening rollback");
        };
        Check(!f.Open() && invalidations == 1 && !f.Pause.HasRetainedPause && !f.View.IsVisible, "opening invalidation cancellation");
        f.End();
        f = new Fixture(); captured = f;
        GameObject.Activated = go => { if (go == captured.View.gameObject) throw new Exception("show failure"); };
        Check(!f.Open() && !f.Pause.HasRetainedPause && !f.View.IsVisible && Time.timeScale == 0.65f, "failed activation rolls back pause"); f.End();
        f = new Fixture(); captured = f;
        GameObject.Activated = go => { if (go == captured.View.gameObject) captured.View.gameObject.SetActive(false); };
        Check(!f.Open() && !f.Pause.HasRetainedPause, "activation-time external disable"); f.End();
        f = new Fixture(); captured = f;
        GameObject.Activated = go => { if (go == captured.View.gameObject) Set(captured.View, "restartButton", null); };
        Check(!f.Open() && !f.Pause.HasRetainedPause && !f.View.IsInteractionEnabled, "activation-time reference mutation revalidates before input"); f.End();

        f = new Fixture(); Check(f.Open(), "external disable setup"); f.View.gameObject.SetActive(false);
        Check(!f.Pause.HasRetainedPause && !f.Hud.IsExitConfirmationBusy && f.Battle.IsOpenForRead && f.Failures == 0, "external presentation loss is local cancellation"); f.End();
        f = new Fixture(); Check(f.Open(), "component disable setup"); f.View.enabled = false; Frame(f.Hud);
        Check(!f.Pause.HasRetainedPause, "exit-only lifetime checker detects component loss without inspection session"); f.End();
        f = new Fixture(); Check(f.Open(), "sibling frame-fault setup");
        // Fault-inject a lingering sibling cleanup whose hide and failure reporter throw.
        var throwingManager = new GameObject().AddComponent<MonsterManager>();
        var throwingBattle = new BattleCombatBinding(throwingManager,
            reason => { throw new Exception("sibling cleanup reporter"); });
        throwingManager.CombatBinding = throwingBattle; throwingBattle.Open();
        var siblingView = new GameObject().AddComponent<TowerInfoWindow>();
        var inspectionType = typeof(BattleHUDUI).GetNestedType("InspectionSession", BindingFlags.NonPublic);
        var brokenInspection = Activator.CreateInstance(inspectionType, BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { throwingBattle, siblingView }, null);
        Set(f.Hud, "inspectionSession", brokenInspection);
        var inspectionPhase = typeof(BattleHUDUI).GetField("inspectionPhase", BindingFlags.Instance | BindingFlags.NonPublic);
        inspectionPhase.SetValue(f.Hud, Enum.Parse(inspectionPhase.FieldType, "Open"));
        GameObject.BeforeDeactivate = go => { if (go == siblingView.gameObject) throw new Exception("sibling hide fault"); };
        f.View.enabled = false;
        Frame(f.Hud);
        Check(!f.Pause.HasRetainedPause && !f.Hud.IsExitConfirmationBusy, "sibling lifetime exception cannot skip exit cleanup"); f.End();
        f = new Fixture(); Check(f.Open(), "content loss setup"); f.Content.gameObject.SetActive(false); Frame(f.Hud);
        Check(!f.Pause.HasRetainedPause && !f.View.IsVisible, "exit-only frame detects content loss"); f.End();
        f = new Fixture(); Check(f.Open(), "revocation setup"); f.Pause.RevokeBattle(f.Battle); Frame(f.Hud);
        Check(!f.Pause.HasRetainedPause && Time.timeScale == 0.65f && !f.Hud.IsExitConfirmationBusy, "frame cleans revoked retained pause"); f.End();
        f = new Fixture(); Check(f.Open(), "HUD disable setup");
        Get<DraftWindow>(f.Hud, "draftWindow").ThrowNotify = true;
        f.Hud.ThrowUnsubscribe = true;
        f.Hud.gameObject.SetActive(false);
        Check(!f.Pause.HasRetainedPause && !f.View.IsVisible, "independent HUD disable cleanup survives sibling faults"); f.End();

        f = new Fixture(); ExitStageTransitionRequest oldRequest = null; captured = f; int calls = 0;
        f.Handler = request =>
        {
            calls++; oldRequest = request;
            Check(!captured.Hud.IsExitConfirmationOpen && captured.Hud.IsExitConfirmationBusy, "submitting is not Open but remains busy");
            Check(captured.Hud.IsCurrentExitStageRequest(request), "acceptor validates exact AwaitingTransition request");
            Check(request.Source == captured.Hud && request.Battle == captured.Battle && request.Action == ExitStageAction.Restart, "request immutable source and action");
            var forged = new ExitStageTransitionRequest(request.Source, request.Battle, request.Session, request.BindingRevision, ExitStageAction.MainMenu);
            Check(!captured.Hud.IsCurrentExitStageRequest(forged) && !captured.Hud.TryAcceptExitStageRequest(forged), "forged/action-mismatched request rejects");
            Check(captured.View.IsVisible && !captured.View.IsInteractionEnabled && captured.View.GetComponent<CanvasGroup>().blocksRaycasts,
                "AwaitingTransition keeps mask while locking buttons");
            Frame(captured.Hud);
            Check(captured.Pause.HasRetainedPause && captured.View.IsVisible, "frame does not interpret button lock as presentation loss");
            captured.Restart.onClick.Invoke(); captured.MainMenu.onClick.Invoke(); captured.Cancel.onClick.Invoke();
            return ExitStageRequestDisposition.Rejected;
        };
        Check(f.Open(), "rejected dispatch setup"); f.Restart.onClick.Invoke();
        Check(calls == 1 && f.Hud.IsExitConfirmationOpen && f.View.IsInteractionEnabled && f.Pause.HasRetainedPause, "rejection restores only current live choices; duplicate buttons suppressed");
        Check(!f.Hud.IsCurrentExitStageRequest(oldRequest) && !f.Hud.TryAcceptExitStageRequest(oldRequest), "request expires after rejection");
        f.Handler = request => { throw new Exception("before acceptance"); };
        f.MainMenu.onClick.Invoke();
        Check(f.Hud.IsExitConfirmationOpen && f.View.IsInteractionEnabled && f.Pause.HasRetainedPause, "pre-acceptance exception safely restores unchanged session"); f.End();

        f = new Fixture(); captured = f;
        f.Handler = request =>
        {
            Check(captured.Hud.TryAcceptExitStageRequest(request), "deferred accepted ownership");
            captured.Pause.RevokeBattle(request.Battle); request.Battle.Close();
            return ExitStageRequestDisposition.Accepted;
        };
        Check(f.Open(), "accepted deferred cleanup setup"); f.Restart.onClick.Invoke();
        Check(!f.Hud.IsExitConfirmationOpen && f.Hud.IsExitConfirmationBusy && f.View.IsVisible &&
            !f.View.IsInteractionEnabled && f.Pause.HasRetainedPause, "accepted revoked session stays locked and blocking until cleanup");
        Frame(f.Hud);
        Check(!f.Pause.HasRetainedPause && !f.Hud.IsExitConfirmationBusy && !f.Battle.IsOpenForRead,
            "independent lifetime helper cleans deferred revoked session without reopening gameplay"); f.End();

        foreach (bool throwsAfter in new[] { false, true })
        {
            f = new Fixture(); captured = f;
            var outgoing = f.Battle;
            f.Handler = request =>
            {
                oldRequest = request;
                Check(captured.Hud.TryAcceptExitStageRequest(request) && request.Accepted, "explicit acceptance transfers ownership");
                Check(!captured.Hud.TryAcceptExitStageRequest(request), "accept once");
                captured.RevokeAndClear();
                captured.BindFreshBattle(); Check(captured.Open(), "synchronous replacement opens a fresh session on same HUD/view");
                if (throwsAfter) throw new Exception("after accepted cleanup/replacement");
                // Even a later preparation failure must not be reinterpreted as admission rejection.
                return ExitStageRequestDisposition.Rejected;
            };
            Check(f.Open(), "accepted dispatch setup"); f.MainMenu.onClick.Invoke();
            Check(f.Battle != outgoing && f.Hud.IsExitConfirmationOpen && f.View.IsInteractionEnabled && f.Pause.HasRetainedPause,
                "accepted request never restores/closes newly bound session after return/exception");
            Check(!f.Hud.IsCurrentExitStageRequest(oldRequest) && !f.Hud.TryAcceptExitStageRequest(oldRequest), "stale accepted request rejects on reused HUD"); f.End();
        }
        f = new Fixture(); captured = f;
        f.Handler = request => { captured.RevokeAndClear(); throw new Exception("revoked without acceptance"); };
        Check(f.Open(), "revocation exception setup"); f.Restart.onClick.Invoke();
        Check(!f.Hud.IsExitConfirmationBusy && !f.Pause.HasRetainedPause && !f.View.IsVisible, "exception after revocation does not restore old interaction"); f.End();
        f = new Fixture(); captured = f;
        f.Handler = request => { captured.Hud.TryAcceptExitStageRequest(request); throw new Exception("accepted without revoke"); };
        Check(f.Open(), "broken accepted contract setup"); f.Restart.onClick.Invoke();
        Check(f.Failures == 1 && !f.Hud.IsExitConfirmationBusy && !f.Pause.HasRetainedPause && !f.Battle.IsOpenForRead,
            "accepted handler must revoke before return/exception; otherwise exact Battle failure cleanup"); f.End();
        f = new Fixture(); captured = f;
        f.Handler = request =>
        {
            captured.Hud.TryAcceptExitStageRequest(request);
            captured.Pause.RevokeBattle(request.Battle);
            return ExitStageRequestDisposition.Accepted;
        };
        Check(f.Open(), "partial revocation setup"); f.Restart.onClick.Invoke();
        Check(f.Failures == 1 && !f.Battle.IsOpenForRead && !f.Pause.HasRetainedPause,
            "revoking only modal admission is insufficient; outgoing gameplay must close too"); f.End();
        f = new Fixture(); f.Handler = request => ExitStageRequestDisposition.Accepted;
        Check(f.Open(), "unacknowledged acceptance setup"); f.MainMenu.onClick.Invoke();
        Check(f.Failures == 1 && !f.Pause.HasRetainedPause && !f.Hud.IsExitConfirmationOpen, "Accepted result without ownership handoff fails safely"); f.End();
        f = new Fixture(); Check(f.Open(), "missing receiver setup");
        Set(f.Hud, "exitTransition", null); f.MainMenu.onClick.Invoke();
        Check(f.View.IsInteractionEnabled && f.Hud.IsExitConfirmationOpen, "no receiver rejects without stranding pause"); f.End();

        f = new Fixture(); Check(f.Open(), "stale callbacks setup");
        var staleCancel = Get<Action>(f.View, "cancelRequested"); var staleRestart = Get<Action>(f.View, "restartRequested");
        f.Hud.CancelExitConfirmation(); Check(f.Open(), "replacement session");
        staleCancel(); staleRestart();
        Check(f.Hud.IsExitConfirmationOpen && f.Pause.HasRetainedPause, "old callbacks cannot affect new session");
        int externalClicks = 0; f.Cancel.onClick.AddListener(() => externalClicks++);
        f.Cancel.onClick.Invoke(); f.Hud.ClearExitConfirmationBinding();
        Check(f.Hud.BindExitConfirmation(f.Battle, f.Members, f.Pause, null, out _) && f.Open(), "detach and reattach");
        f.Cancel.onClick.Invoke();
        Check(externalClicks == 2 && !f.Pause.HasRetainedPause, "preserve external listeners without accumulating own bindings"); f.End();

        f = new Fixture(); Check(f.Open(), "hide failure setup"); captured = f;
        GameObject.BeforeDeactivate = go => { if (go == captured.View.gameObject) throw new Exception("native hide fault"); };
        f.Hud.CancelExitConfirmation();
        Check(!f.Pause.HasRetainedPause && !f.View.GetComponent<CanvasGroup>().blocksRaycasts && f.Failures == 1,
            "hide failure removes blocker, restores owned pause once, reports exact Battle"); f.End();
        f = new Fixture(); Check(f.Open(), "destroy setup"); UnityEngine.Object.Destroy(f.View.gameObject);
        Check(!f.Pause.HasRetainedPause && !f.Hud.IsExitConfirmationBusy, "destroy view releases only owned session"); f.End();
        Console.WriteLine("PASS production ExitConfirmation View/HUD/Binding/Pause: " + checks + " assertions (native/transition boundaries doubled)");
    }
}
