using System;
using System.Collections.Generic;
using UnityEngine;

public class DraftSystem : MonoBehaviour
{
    private enum DraftSessionKind
    {
        None = 0,
        Initial = 1,
        LevelUp = 2
    }

    private enum DraftSessionPhase
    {
        None = 0,
        Opening = 1,
        AwaitingSelection = 2,
        CommittingSelection = 3,
        Completed = 4,
        Failed = 5,
        Cancelled = 6
    }

    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private int draftChoiceCount = 3;

    private readonly List<DraftResult> draftChoices =
        new List<DraftResult>();
    private readonly List<DraftResult> draftPool =
        new List<DraftResult>();
    private readonly HashSet<UnityEngine.Object> displayedIdentities =
        new HashSet<UnityEngine.Object>();

    private IReadOnlyList<TowerDefinition> towerDefinitions;
    private IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions;
    private bool isBattleActive;
    private ulong battleGenerationCounter;
    private ulong draftAttemptCounter;
    private ulong currentBattleGeneration;

    private DraftAttemptToken activeToken;
    private DraftSessionKind sessionKind;
    private DraftSessionPhase sessionPhase;

    private bool hasPauseLease;
    private DraftAttemptToken pauseOwnerToken;
    private float capturedTimeScale;

    private DraftAttemptToken completedInitialToken;
    private PendingDraftUIItem committedInitialHeldItem;

    public bool IsBattleActive => isBattleActive;
    public bool HasStagePools =>
        towerDefinitions != null && upgradeDefinitions != null;

    public event Action<DraftAttemptToken> OnInitialDraftCompleted;
    public event Action<DraftAttemptToken, string> OnInitialDraftFailed;

    private void OnEnable()
    {
        if (playerSystem != null)
        {
            playerSystem.OnLevelUp += HandleLevelUp;
        }
        else
        {
            Debug.LogWarning("Player System is not assigned.", this);
        }
    }

    private void OnDisable()
    {
        if (playerSystem != null)
        {
            playerSystem.OnLevelUp -= HandleLevelUp;
        }

        StopBattle();
    }

    public bool BindStagePools(
        IReadOnlyList<TowerDefinition> selectedTowerDefinitions,
        IReadOnlyList<TowerUpgradeDefinition> selectedUpgradeDefinitions)
    {
        if (selectedTowerDefinitions == null ||
            selectedUpgradeDefinitions == null)
        {
            Debug.LogError(
                "Draft system cannot bind Stage pools because one or both " +
                "pools are null.",
                this);
            return false;
        }

        towerDefinitions = selectedTowerDefinitions;
        upgradeDefinitions = selectedUpgradeDefinitions;
        return true;
    }

    public bool CanBeginBattle(out string failureReason)
    {
        if (playerSystem == null)
        {
            failureReason = "Player System is not assigned.";
            return false;
        }

        if (towerUpgradeSystem == null)
        {
            failureReason = "Tower Upgrade System is not assigned.";
            return false;
        }

        if (towerPlacementController == null)
        {
            failureReason = "Tower Placement Controller is not assigned.";
            return false;
        }

        if (battleHUDUI == null)
        {
            failureReason = "Battle HUD UI is not assigned.";
            return false;
        }

        if (draftChoiceCount <= 0)
        {
            failureReason = "Draft Choice Count must be positive.";
            return false;
        }

        if (!HasStagePools)
        {
            failureReason = "Stage Draft pools are not bound.";
            return false;
        }

        if (!HasValidInitialTowerCandidate())
        {
            failureReason =
                "the active Stage has no valid TowerDefinition for its " +
                "Initial Draft.";
            return false;
        }

        if (!battleHUDUI.TryValidateBattleReferences(
                out string hudFailureReason))
        {
            failureReason = $"Battle HUD UI is invalid: {hudFailureReason}";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void BeginBattle()
    {
        CancelActiveSession();
        ClearCompletedInitialRecord();
        currentBattleGeneration = NextNonZero(
            ref battleGenerationCounter);
        sessionPhase = DraftSessionPhase.None;
        isBattleActive = true;
        battleHUDUI?.BeginBattle();
    }

    public void StopBattle()
    {
        isBattleActive = false;
        CancelActiveSession();
        ClearTransientDraftCollections();
        battleHUDUI?.StopBattle();
    }

    public void ClearStageUi()
    {
        StopBattle();
        battleHUDUI?.ClearStageRuntime();
    }

    public void ClearStagePools()
    {
        ClearTransientDraftCollections();
        ClearCompletedInitialRecord();
        activeToken = default;
        sessionKind = DraftSessionKind.None;
        sessionPhase = DraftSessionPhase.None;
        currentBattleGeneration = 0;
        towerDefinitions = null;
        upgradeDefinitions = null;
    }

    public void ClearStageRuntime()
    {
        ClearStageUi();
        ClearStagePools();
    }

    public bool TryOpenInitialTowerDraft(
        out DraftAttemptToken attemptToken,
        out string failureReason)
    {
        return TryOpenDraft(
            DraftSessionKind.Initial,
            out attemptToken,
            out failureReason);
    }

    public bool IsAwaitingDraft(DraftAttemptToken attemptToken)
    {
        return attemptToken.IsValid &&
               activeToken == attemptToken &&
               sessionPhase == DraftSessionPhase.AwaitingSelection &&
               battleHUDUI != null &&
               battleHUDUI.IsDraftOpen &&
               OwnsPause(attemptToken);
    }

    public bool TryConfirmCommittedInitialDraft(
        DraftAttemptToken attemptToken,
        out PendingDraftUIItem committedItem)
    {
        committedItem = null;

        if (!attemptToken.IsValid ||
            completedInitialToken != attemptToken ||
            committedInitialHeldItem == null)
        {
            return false;
        }

        committedItem = committedInitialHeldItem;
        return true;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (!isBattleActive)
        {
            return;
        }

        if (!TryOpenDraft(
                DraftSessionKind.LevelUp,
                out _,
                out string failureReason))
        {
            Debug.LogWarning(
                $"Draft system rejected Level-Up Draft for level {newLevel}: " +
                $"{failureReason} Unsupported overlapping Level-Up Drafts are " +
                "not queued.",
                this);
        }
    }

    private bool TryOpenDraft(
        DraftSessionKind requestedKind,
        out DraftAttemptToken attemptToken,
        out string failureReason)
    {
        attemptToken = default;

        if (!isBattleActive || currentBattleGeneration == 0)
        {
            failureReason = "the Draft battle gate is closed.";
            return false;
        }

        if (activeToken.IsValid)
        {
            failureReason =
                $"Draft attempt {activeToken} is already active in phase " +
                $"{sessionPhase}.";
            return false;
        }

        DraftAttemptToken provisionalToken = new DraftAttemptToken(
            currentBattleGeneration,
            NextNonZero(ref draftAttemptCounter));
        activeToken = provisionalToken;
        sessionKind = requestedKind;
        sessionPhase = DraftSessionPhase.Opening;

        try
        {
            if (!TryGenerateDraftChoices(
                    requestedKind,
                    out failureReason))
            {
                RollBackOpening(provisionalToken);
                return false;
            }

            if (!battleHUDUI.TryOpenDraft(
                    draftChoices,
                    selectedResult =>
                        HandleDraftSelected(
                            provisionalToken,
                            selectedResult),
                    out failureReason))
            {
                RollBackOpening(provisionalToken);
                return false;
            }

            if (!TryAcquirePause(
                    provisionalToken,
                    out failureReason))
            {
                RollBackOpening(provisionalToken);
                return false;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            failureReason =
                $"Draft opening threw {exception.GetType().Name}.";
            RollBackOpening(provisionalToken);
            return false;
        }

        sessionPhase = DraftSessionPhase.AwaitingSelection;
        attemptToken = provisionalToken;
        failureReason = string.Empty;
        return true;
    }

    private bool TryGenerateDraftChoices(
        DraftSessionKind requestedKind,
        out string failureReason)
    {
        ClearTransientDraftCollections();
        AddTowerDraftCandidates();

        if (requestedKind == DraftSessionKind.LevelUp)
        {
            AddTowerUpgradeDraftCandidates();
        }

        while (draftChoices.Count < draftChoiceCount &&
               draftPool.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(
                0,
                draftPool.Count);
            DraftResult selectedResult = draftPool[randomIndex];
            draftPool.RemoveAt(randomIndex);

            if (selectedResult == null ||
                !selectedResult.IsValid ||
                selectedResult.Identity == null ||
                !displayedIdentities.Add(selectedResult.Identity))
            {
                continue;
            }

            draftChoices.Add(selectedResult);
        }

        if (draftChoices.Count == 0)
        {
            failureReason =
                requestedKind == DraftSessionKind.Initial
                    ? "no valid Initial Tower Draft candidates are available."
                    : "no valid Level-Up Draft candidates are available.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private void AddTowerDraftCandidates()
    {
        if (towerDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < towerDefinitions.Count; i++)
        {
            TowerDefinition towerDefinition = towerDefinitions[i];

            if (towerDefinition != null && towerDefinition.IsValid())
            {
                draftPool.Add(
                    DraftResult.CreateTowerDraft(towerDefinition));
            }
        }
    }

    private void AddTowerUpgradeDraftCandidates()
    {
        if (upgradeDefinitions == null ||
            towerUpgradeSystem == null ||
            towerPlacementController == null)
        {
            return;
        }

        IReadOnlyList<TowerInstance> deployedTowerInstances =
            towerPlacementController.DeployedTowerInstances;

        if (deployedTowerInstances == null)
        {
            return;
        }

        for (int upgradeIndex = 0;
             upgradeIndex < upgradeDefinitions.Count;
             upgradeIndex++)
        {
            TowerUpgradeDefinition upgradeDefinition =
                upgradeDefinitions[upgradeIndex];

            if (upgradeDefinition == null)
            {
                continue;
            }

            int eligibleTowerCount =
                CountEligibleTowerInstancesForUpgrade(
                    upgradeDefinition,
                    deployedTowerInstances);
            int pendingReservedCapacityCount =
                CountPendingReservedCapacityForUpgrade(
                    upgradeDefinition);
            int candidateCount = Mathf.Max(
                0,
                eligibleTowerCount - pendingReservedCapacityCount);

            for (int candidateIndex = 0;
                 candidateIndex < candidateCount;
                 candidateIndex++)
            {
                draftPool.Add(
                    DraftResult.CreateTowerUpgradeDraft(
                        upgradeDefinition));
            }
        }
    }

    private int CountEligibleTowerInstancesForUpgrade(
        TowerUpgradeDefinition upgradeDefinition,
        IReadOnlyList<TowerInstance> deployedTowerInstances)
    {
        if (upgradeDefinition == null ||
            deployedTowerInstances == null ||
            towerUpgradeSystem == null)
        {
            return 0;
        }

        int eligibleTowerCount = 0;

        for (int towerIndex = 0;
             towerIndex < deployedTowerInstances.Count;
             towerIndex++)
        {
            TowerInstance towerInstance =
                deployedTowerInstances[towerIndex];

            if (towerInstance != null &&
                towerUpgradeSystem.CanApplyUpgrade(
                    towerInstance,
                    upgradeDefinition,
                    out _))
            {
                eligibleTowerCount++;
            }
        }

        return eligibleTowerCount;
    }

    private int CountPendingReservedCapacityForUpgrade(
        TowerUpgradeDefinition upgradeDefinition)
    {
        if (upgradeDefinition == null || battleHUDUI == null)
        {
            return 0;
        }

        IReadOnlyList<PendingDraftUIItem> pendingDraftItems =
            battleHUDUI.PendingDraftItems;

        if (pendingDraftItems == null)
        {
            return 0;
        }

        int pendingReservedCapacityCount = 0;

        for (int pendingIndex = 0;
             pendingIndex < pendingDraftItems.Count;
             pendingIndex++)
        {
            PendingDraftUIItem pendingDraftItem =
                pendingDraftItems[pendingIndex];
            TowerUpgradeDefinition pendingUpgradeDefinition =
                pendingDraftItem != null
                    ? pendingDraftItem.TowerUpgradeDefinition
                    : null;

            if (DoesPendingUpgradeReserveCapacityForUpgrade(
                    pendingUpgradeDefinition,
                    upgradeDefinition))
            {
                pendingReservedCapacityCount++;
            }
        }

        return pendingReservedCapacityCount;
    }

    private static bool DoesPendingUpgradeReserveCapacityForUpgrade(
        TowerUpgradeDefinition pendingUpgradeDefinition,
        TowerUpgradeDefinition candidateUpgradeDefinition)
    {
        if (pendingUpgradeDefinition == null ||
            candidateUpgradeDefinition == null)
        {
            return false;
        }

        if (pendingUpgradeDefinition == candidateUpgradeDefinition)
        {
            return true;
        }

        return pendingUpgradeDefinition.UpgradeLayer ==
                   TowerUpgradeLayer.Elemental &&
               candidateUpgradeDefinition.UpgradeLayer ==
                   TowerUpgradeLayer.Elemental &&
               pendingUpgradeDefinition.TowerFamily ==
                   candidateUpgradeDefinition.TowerFamily;
    }

    private void HandleDraftSelected(
        DraftAttemptToken callbackToken,
        DraftResult selectedResult)
    {
        if (!isBattleActive ||
            activeToken != callbackToken ||
            sessionPhase != DraftSessionPhase.AwaitingSelection ||
            selectedResult == null ||
            !selectedResult.IsValid ||
            selectedResult.Identity == null ||
            !displayedIdentities.Contains(selectedResult.Identity))
        {
            return;
        }

        sessionPhase = DraftSessionPhase.CommittingSelection;
        DraftSessionKind committingKind = sessionKind;

        if (!battleHUDUI.TryAddPendingDraft(
                selectedResult,
                out PendingDraftUIItem committedItem,
                out string failureReason))
        {
            sessionPhase = DraftSessionPhase.Failed;
            InvalidateActiveAuthority();
            battleHUDUI.CloseDraft();
            ReleasePause(callbackToken);

            if (committingKind == DraftSessionKind.Initial)
            {
                OnInitialDraftFailed?.Invoke(
                    callbackToken,
                    failureReason);
                return;
            }

            Debug.LogError(
                $"Level-Up Draft {callbackToken} failed while committing its " +
                $"held item: {failureReason}",
                this);
            return;
        }

        if (committingKind == DraftSessionKind.Initial)
        {
            completedInitialToken = callbackToken;
            committedInitialHeldItem = committedItem;
        }

        sessionPhase = DraftSessionPhase.Completed;
        InvalidateActiveAuthority();
        battleHUDUI.CloseDraft();
        ReleasePause(callbackToken);

        if (committingKind == DraftSessionKind.Initial)
        {
            OnInitialDraftCompleted?.Invoke(callbackToken);
        }
    }

    private bool TryAcquirePause(
        DraftAttemptToken attemptToken,
        out string failureReason)
    {
        if (!attemptToken.IsValid || activeToken != attemptToken)
        {
            failureReason =
                "the Draft attempt lost active authority before pause acquisition.";
            return false;
        }

        if (hasPauseLease)
        {
            failureReason =
                $"Draft attempt {pauseOwnerToken} already owns the pause lease.";
            return false;
        }

        capturedTimeScale = Time.timeScale;
        pauseOwnerToken = attemptToken;
        hasPauseLease = true;
        Time.timeScale = 0f;
        failureReason = string.Empty;
        return true;
    }

    private bool OwnsPause(DraftAttemptToken attemptToken)
    {
        return hasPauseLease &&
               pauseOwnerToken == attemptToken;
    }

    private void ReleasePause(DraftAttemptToken attemptToken)
    {
        if (!OwnsPause(attemptToken))
        {
            return;
        }

        float timeScaleToRestore = capturedTimeScale;
        hasPauseLease = false;
        pauseOwnerToken = default;
        capturedTimeScale = 0f;
        Time.timeScale = timeScaleToRestore;
    }

    private void RollBackOpening(DraftAttemptToken attemptToken)
    {
        if (activeToken == attemptToken)
        {
            InvalidateActiveAuthority();
        }

        battleHUDUI?.CloseDraft();
        ReleasePause(attemptToken);
        sessionPhase = DraftSessionPhase.None;
    }

    private void CancelActiveSession()
    {
        DraftAttemptToken tokenToCancel = activeToken;

        if (tokenToCancel.IsValid)
        {
            sessionPhase = DraftSessionPhase.Cancelled;
            InvalidateActiveAuthority();
        }

        battleHUDUI?.CloseDraft();

        if (tokenToCancel.IsValid)
        {
            ReleasePause(tokenToCancel);
        }
        else if (hasPauseLease)
        {
            ReleasePause(pauseOwnerToken);
        }
    }

    private void InvalidateActiveAuthority()
    {
        activeToken = default;
        sessionKind = DraftSessionKind.None;
        ClearTransientDraftCollections();
    }

    private void ClearTransientDraftCollections()
    {
        draftChoices.Clear();
        draftPool.Clear();
        displayedIdentities.Clear();
    }

    private void ClearCompletedInitialRecord()
    {
        completedInitialToken = default;
        committedInitialHeldItem = null;
    }

    private bool HasValidInitialTowerCandidate()
    {
        if (towerDefinitions == null)
        {
            return false;
        }

        for (int i = 0; i < towerDefinitions.Count; i++)
        {
            TowerDefinition towerDefinition = towerDefinitions[i];

            if (towerDefinition != null && towerDefinition.IsValid())
            {
                return true;
            }
        }

        return false;
    }

    private static ulong NextNonZero(ref ulong counter)
    {
        counter++;

        if (counter == 0)
        {
            counter++;
        }

        return counter;
    }
}
