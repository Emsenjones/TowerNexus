using System;
using UnityEngine;

public partial class BattleRuntimeCoordinator
{
    [SerializeField] private GameFlowController gameFlowController;
    private BattleHUDUI subscribedHUD;
    private bool hudBindingOperation, hudConsumersReady;
    private ulong hudBindingRevision;
    private MapGeneratorBehaviour hudMap;

    private bool TryValidateExitIntegration(out string reason)
    {
        reason = "Battle coordinator requires its exact Game Flow owner, Exit entry and confirmation view.";
        return gameFlowController != null && gameFlowController.isActiveAndEnabled &&
            gameFlowController.OwnsBattleCoordinator(this) && battleHUDUI != null &&
            battleHUDUI.TryValidateExitReferences(out reason);
    }

    private void SubscribeBattleHUD()
    {
        UnsubscribeBattleHUD();
        subscribedHUD = battleHUDUI;
        if (subscribedHUD != null) subscribedHUD.PresentationEnabled += HandleHUDEnabled;
    }

    private void UnsubscribeBattleHUD()
    {
        if (subscribedHUD != null) subscribedHUD.PresentationEnabled -= HandleHUDEnabled;
        subscribedHUD = null;
    }

    private void InvalidateBattleHUDReadiness()
    { hudConsumersReady = false; hudBindingRevision++; battleHUDUI?.InvalidateHUDConsumerBinding(); }

    private bool TryBindBattleHUDConsumers(MapGeneratorBehaviour map, out string reason)
    {
        reason = "Battle HUD consumers cannot bind during conflicting lifecycle work.";
        if (hudBindingOperation || isReleasing || releaseRequested || combatBinding == null || map == null ||
            battleTerminalState != BattleTerminalState.None || !TryValidateExitIntegration(out reason)) return false;
        hudConsumersReady = false;
        battleHUDUI.BeginHUDConsumerBinding();
        hudBindingOperation = true;
        var identity = combatBinding;
        var owner = gameFlowController;
        ulong revision = hudBindingRevision;
        bool succeeded = false;
        try
        {
            ClearBattleHUDConsumers(identity);
            if (!battleHUDUI.BindTowerInspection(identity, Submission, ModalPause, out reason) ||
                !cameraPanController.TryBindInspection(identity, map, Submission, ModalPause, out reason)) return false;
            if (revision != hudBindingRevision || !ReferenceEquals(identity, combatBinding) ||
                isReleasing || releaseRequested) return false;
            if (!battleHUDUI.BindExitConfirmation(identity, Submission, ModalPause,
                request => IsBoundOwnerCurrent(owner, identity)
                    ? owner.RequestExitStageTransition(this, request) : ExitStageRequestDisposition.Rejected,
                out reason,
                () => IsBoundOwnerCurrent(owner, identity),
                () => hudConsumersReady && IsBattleActive && IsBoundOwnerCurrent(owner, identity) && owner.AllowsBattleHUDInput(this))) return false;
            if (revision != hudBindingRevision || !ReferenceEquals(identity, combatBinding) ||
                !battleHUDUI.HasTowerInspectionBinding(identity, Submission, ModalPause) ||
                !cameraPanController.IsInspectionBindingReady(identity, false) ||
                !battleHUDUI.HasExitConfirmationBinding(identity, Submission, ModalPause)) return false;
            hudMap = map;
            hudConsumersReady = true;
            battleHUDUI.CompleteHUDConsumerBinding();
            succeeded = true;
            reason = string.Empty;
            return true;
        }
        catch (Exception error) { reason = error.Message; Debug.LogException(error, this); return false; }
        finally
        {
            try { if (!succeeded) ClearBattleHUDConsumers(identity); }
            finally { hudBindingOperation = false; }
        }
    }

    private bool IsBoundOwnerCurrent(GameFlowController owner, BattleCombatBinding identity) =>
        owner != null && owner.isActiveAndEnabled && ReferenceEquals(owner, gameFlowController) &&
        owner.OwnsBattleCoordinator(this) && ReferenceEquals(identity, combatBinding) && !isReleasing && !releaseRequested;

    private bool AreBattleHUDConsumersReady() => hudConsumersReady && battleHUDUI != null &&
        battleHUDUI.HasTowerInspectionBinding(combatBinding, Submission, ModalPause) &&
        battleHUDUI.HasExitConfirmationBinding(combatBinding, Submission, ModalPause) &&
        cameraPanController != null && cameraPanController.IsInspectionBindingReady(combatBinding, false);

    private void ClearBattleHUDConsumers(BattleCombatBinding identity)
    {
        RunCleanupSafely(() => cameraPanController?.ClearInspectionBinding(identity));
        RunCleanupSafely(() => battleHUDUI?.ClearExitConfirmationBinding(identity));
        RunCleanupSafely(() => battleHUDUI?.ClearTowerInspectionBinding(identity));
    }

    private void HandleHUDEnabled(BattleHUDUI hud)
    {
        if (!ReferenceEquals(hud, battleHUDUI) || hudBindingOperation || IsLifecycleBusy ||
            (!IsBattleActive && !isBattlePrepared) || battleTerminalState != BattleTerminalState.None ||
            gameFlowController == null || !gameFlowController.AllowsBattleHUDRebind(this)) return;
        if (AreBattleHUDConsumersReady()) return;
        var identity = combatBinding;
        if (TryBindBattleHUDConsumers(hudMap, out string reason)) return;
        // Binding callbacks can synchronously finish/replace the Battle. Never recover
        // a completed result as though it were the original failed rebind request.
        if (!ReferenceEquals(identity, combatBinding) || battleTerminalState != BattleTerminalState.None ||
            (!IsBattleActive && !isBattlePrepared)) return;
        if (IsBattleActive) { TryFailBattleRuntime("Battle HUD rebind failed: " + reason); return; }
        if (gameFlowController.RecoverPreparedHUD(this, reason)) return;
        Debug.LogError("Prepared Battle HUD rebind failed: " + reason, this);
        ReleasePreparedBattleRuntime(); // Invalid prepared UI cannot later begin Battle.
    }
}
