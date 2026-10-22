using System;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleHUDUI
{
    [SerializeField] private Button exitButton;
    private Button boundExitButton;
    private bool hudConsumersManaged, hudConsumersReady;
    internal bool IsHUDInputReady => !hudConsumersManaged || hudConsumersReady;
    internal void BeginHUDConsumerBinding() { hudConsumersManaged = true; hudConsumersReady = false; }
    internal void CompleteHUDConsumerBinding() { hudConsumersReady = true; }
    internal void InvalidateHUDConsumerBinding() { if (hudConsumersManaged) hudConsumersReady = false; }
    internal event Action<BattleHUDUI> PresentationEnabled;
    internal bool IsBattleModalBusy => !IsHUDInputReady || IsDraftSessionBusy || IsTowerInspectionBusy || IsExitConfirmationBusy;

    private void Awake()
    {
        TryHudCleanup(() => exitStageConfirmWindow?.HideAndLock());
        RefreshExitEntry();
    }

    internal bool TryValidateExitReferences(out string reason)
    {
        reason = "Battle HUD requires its distinct Exit button and authored exit-confirmation child.";
        if (exitButton == null || !exitButton.enabled || !exitButton.transform.IsChildOf(transform) ||
            exitStageConfirmWindow == null || !exitStageConfirmWindow.transform.IsChildOf(transform) ||
            exitButton.transform.IsChildOf(exitStageConfirmWindow.transform)) return false;
        return exitStageConfirmWindow.TryValidateReferences(out reason);
    }

    // Cleanup ownership does not depend on active UI, current owner readiness or view survival.
    internal bool OwnsExitConfirmationBinding(BattleCombatBinding battle) => battle != null &&
        (ReferenceEquals(exitBattle, battle) || ReferenceEquals(exitSession?.Battle, battle));
    internal void ClearExitConfirmationBinding(BattleCombatBinding battle)
    { if (OwnsExitConfirmationBinding(battle)) ClearExitConfirmationBinding(); }

    internal bool HasExitConfirmationBinding(BattleCombatBinding battle, TowerPlacementSubmission members,
        BattleModalPauseAuthority pause) => isActiveAndEnabled && !exitClearing && battle != null &&
        ReferenceEquals(exitBattle, battle) && ReferenceEquals(exitMembers, members) && ReferenceEquals(exitPause, pause) &&
        ReferenceEquals(boundExitView, exitStageConfirmWindow) && boundExitView != null && boundExitView.enabled &&
        exitTransition != null && exitOwnerAvailable != null && exitOwnerAvailable() &&
        exitOpeningAllowed != null && TryValidateExitReferences(out _);

    private void EnableExitEntry()
    {
        if (boundExitButton != exitButton)
        {
            DisableExitEntry();
            boundExitButton = exitButton;
            if (boundExitButton != null) boundExitButton.onClick.AddListener(HandleExitClicked);
        }
        if (!IsExitConfirmationBusy) TryHudCleanup(() => exitStageConfirmWindow?.HideAndLock());
        foreach (Delegate callback in PresentationEnabled?.GetInvocationList() ?? Array.Empty<Delegate>())
            TryHudCleanup(() => ((Action<BattleHUDUI>)callback)(this));
        RefreshExitEntry();
    }

    private void DisableExitEntry()
    {
        if (boundExitButton != null) boundExitButton.onClick.RemoveListener(HandleExitClicked);
        boundExitButton = null;
        if (exitButton != null) { exitButton.interactable = false; exitButton.gameObject.SetActive(false); }
    }

    private void RefreshExitEntry()
    {
        if (exitButton == null) return;
        bool visible = isActiveAndEnabled && exitOwnerAvailable != null && exitOwnerAvailable() &&
            exitOpeningAllowed != null && exitOpeningAllowed();
        exitButton.interactable = visible && !IsExitConfirmationBusy && ExitBattleAvailable() && exitPause.CanAcquire;
        if (exitButton.gameObject.activeSelf != visible) exitButton.gameObject.SetActive(visible);
    }

    private void HandleExitClicked()
    {
        if (exitButton == null || !exitButton.isActiveAndEnabled || !exitButton.interactable) return;
        TryOpenExitConfirmation(out _); // Admission rechecks live identity and all protected work.
        RefreshExitEntry();
    }
}
