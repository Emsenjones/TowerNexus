using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
public enum DraftChoiceGenerationMode
{
    Natural = 0,
    Fixed = 1
}

public enum DraftChoiceSessionKind
{
    Initial = 0,
    LevelUp = 1
}

[Serializable]
public sealed class FixedDraftChoiceEntry
{
    [SerializeField] private TowerDefinition towerDefinition;
    [SerializeField] private TowerUpgradeDefinition towerUpgradeDefinition;

    public TowerDefinition TowerDefinition => towerDefinition;
    public TowerUpgradeDefinition TowerUpgradeDefinition =>
        towerUpgradeDefinition;
}

[Serializable]
public sealed class FixedDraftChoiceStep
{
    [SerializeField] private List<FixedDraftChoiceEntry> choices =
        new List<FixedDraftChoiceEntry>();

    public IReadOnlyList<FixedDraftChoiceEntry> Choices => choices;
}

public sealed class DraftChoiceCandidateObservation
{
    public DraftChoiceCandidateObservation(
        DraftResult draftResult,
        int multiplicity)
    {
        DraftResult = draftResult;
        Multiplicity = multiplicity;
    }

    public DraftResult DraftResult { get; }
    public int Multiplicity { get; private set; }

    internal void IncrementMultiplicity()
    {
        Multiplicity++;
    }
}

public sealed class DraftChoicesOpenedObservation
{
    public DraftChoicesOpenedObservation(
        DraftAttemptToken attemptToken,
        int draftOrdinal,
        DraftChoiceSessionKind sessionKind,
        DraftChoiceGenerationMode generationMode,
        IReadOnlyList<DraftChoiceCandidateObservation> naturalCandidates,
        IReadOnlyList<DraftResult> displayedChoices)
    {
        AttemptToken = attemptToken;
        DraftOrdinal = draftOrdinal;
        SessionKind = sessionKind;
        GenerationMode = generationMode;
        NaturalCandidates = CopyCandidates(naturalCandidates);
        DisplayedChoices = CopyChoices(displayedChoices);
    }

    public DraftAttemptToken AttemptToken { get; }
    public int DraftOrdinal { get; }
    public DraftChoiceSessionKind SessionKind { get; }
    public DraftChoiceGenerationMode GenerationMode { get; }
    public IReadOnlyList<DraftChoiceCandidateObservation> NaturalCandidates
    {
        get;
    }
    public IReadOnlyList<DraftResult> DisplayedChoices { get; }

    private static IReadOnlyList<DraftChoiceCandidateObservation>
        CopyCandidates(
            IReadOnlyList<DraftChoiceCandidateObservation> candidates)
    {
        DraftChoiceCandidateObservation[] copy = candidates != null
            ? new DraftChoiceCandidateObservation[candidates.Count]
            : Array.Empty<DraftChoiceCandidateObservation>();

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = candidates[i];
        }

        return Array.AsReadOnly(copy);
    }

    private static IReadOnlyList<DraftResult> CopyChoices(
        IReadOnlyList<DraftResult> choices)
    {
        DraftResult[] copy = choices != null
            ? new DraftResult[choices.Count]
            : Array.Empty<DraftResult>();

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = choices[i];
        }

        return Array.AsReadOnly(copy);
    }
}

public sealed class DraftChoiceCommittedObservation
{
    public DraftChoiceCommittedObservation(
        DraftAttemptToken attemptToken,
        int draftOrdinal,
        DraftChoiceSessionKind sessionKind,
        DraftChoiceGenerationMode generationMode,
        DraftResult selectedChoice)
    {
        AttemptToken = attemptToken;
        DraftOrdinal = draftOrdinal;
        SessionKind = sessionKind;
        GenerationMode = generationMode;
        SelectedChoice = selectedChoice;
    }

    public DraftAttemptToken AttemptToken { get; }
    public int DraftOrdinal { get; }
    public DraftChoiceSessionKind SessionKind { get; }
    public DraftChoiceGenerationMode GenerationMode { get; }
    public DraftResult SelectedChoice { get; }
}
#endif

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

#if UNITY_EDITOR
    [Header("Editor Calibration")]
    [SerializeField] private bool useFixedDraftChoices;
    [SerializeField] private List<FixedDraftChoiceStep>
        fixedDraftChoiceSteps = new List<FixedDraftChoiceStep>();
#endif

    private readonly List<DraftResult> draftChoices =
        new List<DraftResult>();
    private readonly List<DraftResult> draftPool =
        new List<DraftResult>();
    private readonly HashSet<UnityEngine.Object> displayedIdentities =
        new HashSet<UnityEngine.Object>();

#if UNITY_EDITOR
    private readonly List<DraftChoiceCandidateObservation>
        naturalCandidateObservations =
            new List<DraftChoiceCandidateObservation>();
    private int openedDraftCount;
    private int activeDraftOrdinal;
    private DraftChoiceGenerationMode activeGenerationMode;
    private DraftChoiceSessionKind activeObservationSessionKind;
#endif

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

#if UNITY_EDITOR
    public bool UseFixedDraftChoices => useFixedDraftChoices;
    public int ConfiguredFixedDraftStepCount =>
        fixedDraftChoiceSteps != null ? fixedDraftChoiceSteps.Count : 0;
    public IReadOnlyList<TowerDefinition> BoundTowerDefinitions =>
        towerDefinitions;
    public IReadOnlyList<TowerUpgradeDefinition> BoundUpgradeDefinitions =>
        upgradeDefinitions;
#endif

    public event Action<DraftAttemptToken> OnInitialDraftCompleted;
    public event Action<DraftAttemptToken, string> OnInitialDraftFailed;

#if UNITY_EDITOR
    public event Action<DraftChoicesOpenedObservation> OnDraftChoicesOpened;
    public event Action<DraftChoiceCommittedObservation>
        OnDraftChoiceCommitted;
#endif

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

#if UNITY_EDITOR
        if (useFixedDraftChoices &&
            !TryValidateFixedDraftConfiguration(out failureReason))
        {
            return false;
        }
#endif

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
#if UNITY_EDITOR
        ResetDraftObservationState();
#endif
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
#if UNITY_EDITOR
        ResetDraftObservationState();
#endif
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
#if UNITY_EDITOR
        ResetDraftObservationState();
#endif
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

#if UNITY_EDITOR
            PublishDraftChoicesOpened(
                provisionalToken,
                requestedKind);
#endif
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

#if UNITY_EDITOR
        CaptureNaturalCandidateObservations();

        if (useFixedDraftChoices)
        {
            return TryGenerateFixedDraftChoices(
                requestedKind,
                out failureReason);
        }
#endif

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

#if UNITY_EDITOR
        PublishDraftChoiceCommitted(callbackToken, selectedResult);
#endif

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
#if UNITY_EDITOR
        ClearActiveDraftObservation();
#endif
    }

    private void ClearTransientDraftCollections()
    {
        draftChoices.Clear();
        draftPool.Clear();
        displayedIdentities.Clear();
#if UNITY_EDITOR
        naturalCandidateObservations.Clear();
#endif
    }

#if UNITY_EDITOR
    private bool TryValidateFixedDraftConfiguration(
        out string failureReason)
    {
        int expectedDraftCount = playerSystem != null &&
                                 playerSystem.ProgressRequirements != null
            ? playerSystem.ProgressRequirements.Count + 1
            : 0;
        int configuredStepCount = fixedDraftChoiceSteps != null
            ? fixedDraftChoiceSteps.Count
            : 0;

        if (expectedDraftCount <= 0)
        {
            failureReason =
                "Fixed Draft Choices require Player Progress Requirements.";
            return false;
        }

        if (configuredStepCount != expectedDraftCount)
        {
            failureReason =
                $"Fixed Draft Choices require exactly {expectedDraftCount} " +
                $"Steps for this Stage; found {configuredStepCount}.";
            return false;
        }

        for (int stepIndex = 0;
             stepIndex < configuredStepCount;
             stepIndex++)
        {
            FixedDraftChoiceStep step = fixedDraftChoiceSteps[stepIndex];
            IReadOnlyList<FixedDraftChoiceEntry> choices =
                step != null ? step.Choices : null;

            if (choices == null || choices.Count == 0)
            {
                failureReason =
                    $"Fixed Draft Step {stepIndex + 1} has no Choices.";
                return false;
            }

            if (choices.Count > draftChoiceCount)
            {
                failureReason =
                    $"Fixed Draft Step {stepIndex + 1} has " +
                    $"{choices.Count} Choices, exceeding Draft Choice " +
                    $"Count {draftChoiceCount}.";
                return false;
            }

            HashSet<UnityEngine.Object> identities =
                new HashSet<UnityEngine.Object>();

            for (int choiceIndex = 0;
                 choiceIndex < choices.Count;
                 choiceIndex++)
            {
                if (!TryCreateFixedDraftResult(
                        choices[choiceIndex],
                        out DraftResult result,
                        out string entryFailureReason))
                {
                    failureReason =
                        $"Fixed Draft Step {stepIndex + 1} Choice " +
                        $"{choiceIndex + 1} is invalid: " +
                        entryFailureReason;
                    return false;
                }

                if (stepIndex == 0 &&
                    result.ResultType != DraftResultType.TowerDraft)
                {
                    failureReason =
                        "Fixed Draft Step 1 is the Initial Draft and may " +
                        "contain only Tower Definitions.";
                    return false;
                }

                if (!identities.Add(result.Identity))
                {
                    failureReason =
                        $"Fixed Draft Step {stepIndex + 1} contains " +
                        $"duplicate Choice '{result.DisplayName}'.";
                    return false;
                }
            }
        }

        failureReason = string.Empty;
        return true;
    }

    private bool TryGenerateFixedDraftChoices(
        DraftSessionKind requestedKind,
        out string failureReason)
    {
        int stepIndex = openedDraftCount;

        if (fixedDraftChoiceSteps == null ||
            stepIndex < 0 ||
            stepIndex >= fixedDraftChoiceSteps.Count)
        {
            failureReason =
                $"Fixed Draft Sequence has no Step {stepIndex + 1}.";
            return false;
        }

        FixedDraftChoiceStep step = fixedDraftChoiceSteps[stepIndex];
        IReadOnlyList<FixedDraftChoiceEntry> configuredChoices =
            step != null ? step.Choices : null;

        if (configuredChoices == null || configuredChoices.Count == 0)
        {
            failureReason =
                $"Fixed Draft Step {stepIndex + 1} has no Choices.";
            return false;
        }

        for (int choiceIndex = 0;
             choiceIndex < configuredChoices.Count;
             choiceIndex++)
        {
            if (!TryCreateFixedDraftResult(
                    configuredChoices[choiceIndex],
                    out DraftResult configuredResult,
                    out string entryFailureReason))
            {
                failureReason =
                    $"Fixed Draft Step {stepIndex + 1} Choice " +
                    $"{choiceIndex + 1} is invalid: {entryFailureReason}";
                return false;
            }

            if (requestedKind == DraftSessionKind.Initial &&
                configuredResult.ResultType != DraftResultType.TowerDraft)
            {
                failureReason =
                    "The Initial Fixed Draft may contain only Tower " +
                    "Definitions.";
                return false;
            }

            if (!TryGetNaturalCandidate(
                    configuredResult.Identity,
                    out DraftResult eligibleResult))
            {
                failureReason =
                    $"Fixed Draft Step {stepIndex + 1} Choice " +
                    $"'{configuredResult.DisplayName}' is not naturally " +
                    "eligible in the current Draft state.";
                return false;
            }

            if (!displayedIdentities.Add(eligibleResult.Identity))
            {
                failureReason =
                    $"Fixed Draft Step {stepIndex + 1} contains duplicate " +
                    $"Choice '{eligibleResult.DisplayName}'.";
                return false;
            }

            draftChoices.Add(eligibleResult);
        }

        failureReason = string.Empty;
        return draftChoices.Count > 0;
    }

    private bool TryCreateFixedDraftResult(
        FixedDraftChoiceEntry entry,
        out DraftResult result,
        out string failureReason)
    {
        result = null;

        if (entry == null)
        {
            failureReason = "the entry is missing.";
            return false;
        }

        bool hasTower = entry.TowerDefinition != null;
        bool hasUpgrade = entry.TowerUpgradeDefinition != null;

        if (hasTower == hasUpgrade)
        {
            failureReason =
                "assign exactly one Tower Definition or Tower Upgrade " +
                "Definition.";
            return false;
        }

        if (hasTower)
        {
            if (!ContainsReference(towerDefinitions, entry.TowerDefinition))
            {
                failureReason =
                    $"Tower '{entry.TowerDefinition.name}' is not in the " +
                    "bound Stage Tower Draft Pool.";
                return false;
            }

            if (!entry.TowerDefinition.IsValid())
            {
                failureReason =
                    $"Tower '{entry.TowerDefinition.name}' is invalid.";
                return false;
            }

            result = DraftResult.CreateTowerDraft(entry.TowerDefinition);
            failureReason = string.Empty;
            return true;
        }

        if (!ContainsReference(
                upgradeDefinitions,
                entry.TowerUpgradeDefinition))
        {
            failureReason =
                $"Upgrade '{entry.TowerUpgradeDefinition.name}' is not in " +
                "the bound Stage Tower Upgrade Draft Pool.";
            return false;
        }

        if (!entry.TowerUpgradeDefinition.IsValid())
        {
            failureReason =
                $"Upgrade '{entry.TowerUpgradeDefinition.name}' is invalid.";
            return false;
        }

        result = DraftResult.CreateTowerUpgradeDraft(
            entry.TowerUpgradeDefinition);
        failureReason = string.Empty;
        return true;
    }

    private static bool ContainsReference<T>(
        IReadOnlyList<T> collection,
        T candidate)
        where T : UnityEngine.Object
    {
        if (collection == null || candidate == null)
        {
            return false;
        }

        for (int i = 0; i < collection.Count; i++)
        {
            if (collection[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private void CaptureNaturalCandidateObservations()
    {
        naturalCandidateObservations.Clear();

        for (int poolIndex = 0;
             poolIndex < draftPool.Count;
             poolIndex++)
        {
            DraftResult result = draftPool[poolIndex];

            if (result == null || !result.IsValid || result.Identity == null)
            {
                continue;
            }

            DraftChoiceCandidateObservation existing = null;

            for (int candidateIndex = 0;
                 candidateIndex < naturalCandidateObservations.Count;
                 candidateIndex++)
            {
                DraftChoiceCandidateObservation candidate =
                    naturalCandidateObservations[candidateIndex];

                if (candidate.DraftResult.Identity == result.Identity)
                {
                    existing = candidate;
                    break;
                }
            }

            if (existing != null)
            {
                existing.IncrementMultiplicity();
            }
            else
            {
                naturalCandidateObservations.Add(
                    new DraftChoiceCandidateObservation(result, 1));
            }
        }
    }

    private bool TryGetNaturalCandidate(
        UnityEngine.Object identity,
        out DraftResult result)
    {
        for (int i = 0; i < naturalCandidateObservations.Count; i++)
        {
            DraftChoiceCandidateObservation candidate =
                naturalCandidateObservations[i];

            if (candidate.DraftResult.Identity == identity &&
                candidate.Multiplicity > 0)
            {
                result = candidate.DraftResult;
                return true;
            }
        }

        result = null;
        return false;
    }

    private void PublishDraftChoicesOpened(
        DraftAttemptToken attemptToken,
        DraftSessionKind requestedKind)
    {
        activeDraftOrdinal = openedDraftCount + 1;
        activeGenerationMode = useFixedDraftChoices
            ? DraftChoiceGenerationMode.Fixed
            : DraftChoiceGenerationMode.Natural;
        activeObservationSessionKind = requestedKind ==
                                       DraftSessionKind.Initial
            ? DraftChoiceSessionKind.Initial
            : DraftChoiceSessionKind.LevelUp;
        openedDraftCount++;

        DraftChoicesOpenedObservation observation =
            new DraftChoicesOpenedObservation(
                attemptToken,
                activeDraftOrdinal,
                activeObservationSessionKind,
                activeGenerationMode,
                naturalCandidateObservations,
                draftChoices);
        Action<DraftChoicesOpenedObservation> handlers =
            OnDraftChoicesOpened;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<DraftChoicesOpenedObservation>)invocationList[i])
                    .Invoke(observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }

    private void PublishDraftChoiceCommitted(
        DraftAttemptToken attemptToken,
        DraftResult selectedResult)
    {
        if (activeDraftOrdinal <= 0 || selectedResult == null)
        {
            return;
        }

        DraftChoiceCommittedObservation observation =
            new DraftChoiceCommittedObservation(
                attemptToken,
                activeDraftOrdinal,
                activeObservationSessionKind,
                activeGenerationMode,
                selectedResult);
        Action<DraftChoiceCommittedObservation> handlers =
            OnDraftChoiceCommitted;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<DraftChoiceCommittedObservation>)invocationList[i])
                    .Invoke(observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }

    private void ResetDraftObservationState()
    {
        openedDraftCount = 0;
        ClearActiveDraftObservation();
        naturalCandidateObservations.Clear();
    }

    private void ClearActiveDraftObservation()
    {
        activeDraftOrdinal = 0;
        activeGenerationMode = DraftChoiceGenerationMode.Natural;
        activeObservationSessionKind = DraftChoiceSessionKind.Initial;
    }
#endif

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
