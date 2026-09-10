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

public enum DraftChoiceCategory
{
    Tower = 0,
    TowerUpgrade = 1
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
        int configuredChoiceCount,
        int draftSeed,
        float towerDraftSlotProbability,
        string generationContractVersion,
        string draftRandomAlgorithmVersion,
        IReadOnlyList<DraftChoiceCategory> requestedCategories,
        int requestedTowerCount,
        int requestedUpgradeCount,
        int availableDistinctTowerCount,
        int availableDistinctUpgradeCount,
        int realizedTowerCount,
        int realizedUpgradeCount,
        int towerSlotsBackfilledByUpgrade,
        int upgradeSlotsBackfilledByTower,
        string backfillReason,
        IReadOnlyList<DraftChoiceCandidateObservation> naturalCandidates,
        IReadOnlyList<DraftResult> displayedChoices,
        int choiceSetRevision = 1, int budgetBefore = 0, int budgetAfter = 0, long requestId = 0)
    {
        AttemptToken = attemptToken;
        DraftOrdinal = draftOrdinal;
        SessionKind = sessionKind;
        GenerationMode = generationMode;
        ConfiguredChoiceCount = configuredChoiceCount;
        DraftSeed = draftSeed;
        TowerDraftSlotProbability = towerDraftSlotProbability;
        GenerationContractVersion = generationContractVersion ?? string.Empty;
        DraftRandomAlgorithmVersion = draftRandomAlgorithmVersion ?? string.Empty;
        RequestedCategories = CopyCategories(requestedCategories);
        RequestedTowerCount = requestedTowerCount;
        RequestedUpgradeCount = requestedUpgradeCount;
        AvailableDistinctTowerCount = availableDistinctTowerCount;
        AvailableDistinctUpgradeCount = availableDistinctUpgradeCount;
        RealizedTowerCount = realizedTowerCount;
        RealizedUpgradeCount = realizedUpgradeCount;
        TowerSlotsBackfilledByUpgrade = towerSlotsBackfilledByUpgrade;
        UpgradeSlotsBackfilledByTower = upgradeSlotsBackfilledByTower;
        BackfillReason = backfillReason ?? string.Empty;
        NaturalCandidates = CopyCandidates(naturalCandidates);
        DisplayedChoices = CopyChoices(displayedChoices);
        RequestId = requestId;
        ChoiceSetRevision = choiceSetRevision; BudgetBefore = budgetBefore; BudgetAfter = budgetAfter;
    }

    public DraftAttemptToken AttemptToken { get; }
    public int DraftOrdinal { get; }
    public DraftChoiceSessionKind SessionKind { get; }
    public DraftChoiceGenerationMode GenerationMode { get; }
    public int ConfiguredChoiceCount { get; }
    public int DraftSeed { get; }
    public float TowerDraftSlotProbability { get; }
    public string GenerationContractVersion { get; }
    public string DraftRandomAlgorithmVersion { get; }
    public IReadOnlyList<DraftChoiceCategory> RequestedCategories { get; }
    public int RequestedTowerCount { get; }
    public int RequestedUpgradeCount { get; }
    public int AvailableDistinctTowerCount { get; }
    public int AvailableDistinctUpgradeCount { get; }
    public int RealizedTowerCount { get; }
    public int RealizedUpgradeCount { get; }
    public int TowerSlotsBackfilledByUpgrade { get; }
    public int UpgradeSlotsBackfilledByTower { get; }
    public string BackfillReason { get; }
    public IReadOnlyList<DraftChoiceCandidateObservation> NaturalCandidates
    {
        get;
    }
    public IReadOnlyList<DraftResult> DisplayedChoices { get; }
    public int ChoiceSetRevision { get; }
    public long RequestId { get; }
    public int BudgetBefore { get; }
    public int BudgetAfter { get; }

    private static IReadOnlyList<DraftChoiceCategory> CopyCategories(
        IReadOnlyList<DraftChoiceCategory> categories)
    {
        DraftChoiceCategory[] copy = categories != null
            ? new DraftChoiceCategory[categories.Count]
            : Array.Empty<DraftChoiceCategory>();

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = categories[i];
        }

        return Array.AsReadOnly(copy);
    }

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
        DraftResult selectedChoice,
        bool heldItemCreationSucceeded,
        string failureReason, int choiceSetRevision = 1)
    {
        ChoiceSetRevision = choiceSetRevision;
        AttemptToken = attemptToken;
        DraftOrdinal = draftOrdinal;
        SessionKind = sessionKind;
        GenerationMode = generationMode;
        SelectedChoice = selectedChoice;
        HeldItemCreationSucceeded = heldItemCreationSucceeded;
        FailureReason = failureReason ?? string.Empty;
    }

    public int ChoiceSetRevision { get; }
    public DraftAttemptToken AttemptToken { get; }
    public int DraftOrdinal { get; }
    public DraftChoiceSessionKind SessionKind { get; }
    public DraftChoiceGenerationMode GenerationMode { get; }
    public DraftResult SelectedChoice { get; }
    public bool HeldItemCreationSucceeded { get; }
    public string FailureReason { get; }
}
#endif

public enum DraftRerollPresentation { NotPresented, Completed, Cancelled, TechnicalFailure }
#if UNITY_EDITOR
public sealed class DraftRerollObservation
{
    public long RequestId { get; }
    public DraftAttemptToken AttemptToken { get; }
    public int RequestedRevision { get; }
    public DraftRerollResult Result { get; }
    public bool Committed { get; }
    public bool SamplingStarted { get; }
    public int CommittedRevision { get; }
    public int BudgetBefore { get; }
    public int BudgetAfter { get; }
    public DraftRerollPresentation Presentation { get; }
    public string FailureReason { get; }
    public DraftRerollObservation(long id, DraftAttemptToken token, int revision, DraftRerollResult result,
        bool committed, bool sampled, int committedRevision, int before, int after,
        DraftRerollPresentation presentation, string reason)
    {
        RequestId=id; AttemptToken=token; RequestedRevision=revision; Result=result;
        Committed=committed; SamplingStarted=sampled; CommittedRevision=committedRevision;
        BudgetBefore=before; BudgetAfter=after; Presentation=presentation; FailureReason=reason ?? string.Empty;
    }
}
#endif

public enum DraftRerollResult { Success, NoOtherCandidates, Unavailable, NoBudget, FixedSequence, PreparationFailed, Cancelled }

public class DraftSystem : MonoBehaviour
{
    public const string GenerationContractVersion = "CategorySlotV1RerollV1";
    public const string DraftRandomAlgorithmVersion = "SystemRandomV1";

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
        Cancelled = 6,
        Refreshing = 7
    }

    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private int draftChoiceCount = 3;

#if UNITY_EDITOR
    [Header("Editor Calibration")]
    [SerializeField] private bool useFixedDraftChoices;
    [SerializeField] private bool useFixedDraftSeed;
    [SerializeField] private int fixedDraftSeed = 1;
    [SerializeField] private List<FixedDraftChoiceStep>
        fixedDraftChoiceSteps = new List<FixedDraftChoiceStep>();
#endif

    private sealed class DraftChoiceSet
    {
        internal readonly List<DraftResult> draftChoices =
            new List<DraftResult>();
        internal readonly List<DraftResult> draftPool =
            new List<DraftResult>();
        internal readonly List<DraftResult> towerDraftCandidatePool =
            new List<DraftResult>();
        internal readonly List<DraftResult> upgradeDraftCandidatePool =
            new List<DraftResult>();
        internal readonly HashSet<UnityEngine.Object> displayedIdentities =
            new HashSet<UnityEngine.Object>();

    #if UNITY_EDITOR
        internal readonly List<DraftChoiceCandidateObservation>
            naturalCandidateObservations =
                new List<DraftChoiceCandidateObservation>();
        internal readonly List<DraftChoiceCategory> requestedCategoryObservations =
            new List<DraftChoiceCategory>();
        internal int requestedTowerCount;
        internal int requestedUpgradeCount;
        internal int availableDistinctTowerCount;
        internal int availableDistinctUpgradeCount;
        internal int realizedTowerCount;
        internal int realizedUpgradeCount;
        internal int towerSlotsBackfilledByUpgrade;
        internal int upgradeSlotsBackfilledByTower;
        internal string backfillReason = string.Empty;
    #endif

    }
    private DraftChoiceSet currentChoiceSet = new DraftChoiceSet();
    private object rerollOperation;
#if UNITY_EDITOR
    private int openedDraftCount;
    private int activeDraftOrdinal;
    private DraftChoiceGenerationMode activeGenerationMode;
    private DraftChoiceSessionKind activeObservationSessionKind;
    private long nextRerollRequestId;
#endif
    private BattleRuntimeCoordinator coordinator;
    internal void BindCoordinator(BattleRuntimeCoordinator owner) { coordinator = owner; }
    internal bool OwnsDraftSession(DraftAttemptToken token) => isBattleActive && activeToken == token;

    private IReadOnlyList<TowerDefinition> towerDefinitions;
    private IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions;
    private int configuredFreeRerollCount;
    private int freeRerollsRemaining;
    private int choiceSetRevision;
    public int ConfiguredFreeRerollCount => configuredFreeRerollCount;
    public int FreeRerollsRemaining => freeRerollsRemaining;
    public int ChoiceSetRevision => choiceSetRevision;
    private bool AllowsReroll
    {
        get
        {
#if UNITY_EDITOR
            if (useFixedDraftChoices) return false;
#endif
            return true;
        }
    }
    private float towerDraftSlotProbability;
    private bool hasStageDraftConfiguration;
    private System.Random draftRandom;
    private int activeDraftSeed;
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
    private PendingDraftEntry committedInitialHeldItem;
    internal PendingDraftCollection PendingOwner { get; } = new PendingDraftCollection();
    public IReadOnlyList<PendingDraftEntry> PendingDrafts => PendingOwner.Held;
    private bool isPreparingPendingViews;
    internal bool IsPendingMutationBusy => rerollOperation != null || isPreparingPendingViews ||
        sessionPhase == DraftSessionPhase.CommittingSelection ||
        sessionPhase == DraftSessionPhase.Refreshing;

    private TowerPlacementSubmission submission;
    internal void BindSubmission(TowerPlacementSubmission owner) { submission = owner; }

    public bool IsBattleActive => isBattleActive;
    public bool HasStagePools =>
        hasStageDraftConfiguration &&
        towerDefinitions != null &&
        upgradeDefinitions != null;

#if UNITY_EDITOR
    public bool UseFixedDraftChoices => useFixedDraftChoices;
    public int ConfiguredFixedDraftStepCount =>
        fixedDraftChoiceSteps != null ? fixedDraftChoiceSteps.Count : 0;
    public int ConfiguredDraftChoiceCount => draftChoiceCount;
    public float BoundTowerDraftSlotProbability => towerDraftSlotProbability;
    public int ActiveDraftSeed => activeDraftSeed;
    public bool UseFixedDraftSeed => useFixedDraftSeed;
    public IReadOnlyList<TowerDefinition> BoundTowerDefinitions =>
        towerDefinitions;
    public IReadOnlyList<TowerUpgradeDefinition> BoundUpgradeDefinitions =>
        upgradeDefinitions;
#endif

    public event Action<DraftAttemptToken> OnInitialDraftCompleted;
    public event Action<DraftAttemptToken, string> OnInitialDraftFailed;

#if UNITY_EDITOR
    public event Action<long> OnRerollRequestStarted;
    public event Action<DraftRerollObservation> OnRerollRequest;

    private static void PublishRerollObservation<T>(Action<T> handlers, T value, object identity)
    {
        if (handlers == null) return;
        foreach (Action<T> handler in handlers.GetInvocationList())
            try { handler(value); }
            catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }
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
        IReadOnlyList<TowerUpgradeDefinition> selectedUpgradeDefinitions,
        float selectedTowerDraftSlotProbability,
        int selectedFreeRerollCount)
    {
        if (selectedTowerDefinitions == null ||
            selectedUpgradeDefinitions == null ||
            float.IsNaN(selectedTowerDraftSlotProbability) ||
            float.IsInfinity(selectedTowerDraftSlotProbability) ||
            selectedTowerDraftSlotProbability < 0f ||
            selectedTowerDraftSlotProbability > 1f || selectedFreeRerollCount < 0)
        {
            Debug.LogError(
                "Draft system cannot bind Stage Draft configuration because " +
                "one or more values are missing or invalid.",
                this);
            return false;
        }

        towerDefinitions = selectedTowerDefinitions;
        upgradeDefinitions = selectedUpgradeDefinitions;
        towerDraftSlotProbability = selectedTowerDraftSlotProbability;
        configuredFreeRerollCount = selectedFreeRerollCount;
        hasStageDraftConfiguration = true;
        return true;
    }

    public bool CanBeginBattle(out string failureReason)
    {
        if (coordinator == null)
        {
            failureReason = "Battle Runtime Coordinator is not bound.";
            return false;
        }
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
        isBattleActive = false;
        PendingOwner.Stop();
        CancelActiveSession();
        battleHUDUI?.ClearStageRuntime();
        ClearCompletedInitialRecord();
#if UNITY_EDITOR
        ResetDraftObservationState();
#endif
        currentBattleGeneration = NextNonZero(
            ref battleGenerationCounter);
        PendingOwner.BeginBattle(currentBattleGeneration);
        battleHUDUI?.BindDraftOwner(this);
        InitializeDraftRandom();
        freeRerollsRemaining = configuredFreeRerollCount;
        choiceSetRevision = 0;
        sessionPhase = DraftSessionPhase.None;
        isBattleActive = true;
        battleHUDUI?.BeginBattle();
    }

    public void StopBattle()
    {
        PendingOwner.Stop();
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
        PendingOwner.Clear();
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
        towerDraftSlotProbability = 0f;
        configuredFreeRerollCount = 0;
        freeRerollsRemaining = 0;
        choiceSetRevision = 0;
        hasStageDraftConfiguration = false;
        draftRandom = null;
        activeDraftSeed = 0;
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
        out PendingDraftEntry committedItem)
    {
        committedItem = null;

        if (!attemptToken.IsValid ||
            completedInitialToken != attemptToken ||
            !PendingOwner.IsCurrent(committedInitialHeldItem))
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
        choiceSetRevision = 1;

        try
        {
            var initialSet = GatherDraftCandidates(requestedKind);
            if (!TryGenerateDraftChoices(initialSet,
                    requestedKind,
                    out failureReason))
            {
                RollBackOpening(provisionalToken);
                return false;
            }

            currentChoiceSet = initialSet;
            if (!battleHUDUI.TryOpenDraft(
                    currentChoiceSet.draftChoices,
                    selectedResult =>
                        HandleDraftSelected(
                            provisionalToken,
                            1, selectedResult),
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

            if (!isBattleActive || activeToken != provisionalToken)
            {
                failureReason = "Draft opening was cancelled.";
                return false;
            }
            BindRerollPresentation(provisionalToken);
#if UNITY_EDITOR
            PublishDraftChoicesOpened(provisionalToken, requestedKind);
#endif
            if (!isBattleActive || activeToken != provisionalToken)
            {
                failureReason = "Draft opening was cancelled.";
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

    private void BindRerollPresentation(DraftAttemptToken token)
    {
        int revision = choiceSetRevision;
        battleHUDUI?.BindReroll(freeRerollsRemaining, AllowsReroll,
            () => TryReroll(token, revision));
    }

    public DraftRerollResult TryReroll(DraftAttemptToken token, int revision)
    {
        // Calls made from protected gameplay/observation callbacks are rejected at ingress.
        // They do not recursively generate request notifications.
        if (rerollOperation != null) return DraftRerollResult.Unavailable;
        var operation = new object();
        rerollOperation = operation;
        var hud = battleHUDUI;
        DraftViewPreparation prepared = null;
        DraftRerollResult result = DraftRerollResult.Unavailable;
        DraftRerollPresentation presentation = DraftRerollPresentation.NotPresented;
        string reason = string.Empty;
        bool committed = false, claimed = false;
#if UNITY_EDITOR
        bool sampled = false;
#endif
        int before = freeRerollsRemaining, after = before, committedRevision = 0;
#if UNITY_EDITOR
        object observationIdentity = coordinator != null ? coordinator.DiagnosticIdentity : null;
        long requestId = ++nextRerollRequestId;
        var starts = OnRerollRequestStarted;
        var outcomes = OnRerollRequest;
        using (CombatDiagnosticScope.Enter(observationIdentity))
#endif
        {
            try
            {
#if UNITY_EDITOR
                PublishRerollObservation(starts, requestId, observationIdentity);
#endif
                if (!IsAwaitingDraft(token) || revision != choiceSetRevision || isPreparingPendingViews)
                    return result;
                if (!AllowsReroll) return result = DraftRerollResult.FixedSequence;
                if (before <= 0) return result = DraftRerollResult.NoBudget;
                sessionPhase = DraftSessionPhase.Refreshing;
                claimed = true;
                var kind = sessionKind;
                var set = GatherDraftCandidates(kind);
                bool canOfferTower = kind == DraftSessionKind.Initial || towerDraftSlotProbability > 0f ||
                    CountDistinctValidIdentities(set.upgradeDraftCandidatePool) < draftChoiceCount;
                bool canOfferUpgrade = kind == DraftSessionKind.LevelUp && (towerDraftSlotProbability < 1f ||
                    CountDistinctValidIdentities(set.towerDraftCandidatePool) < draftChoiceCount);
                bool hasOther = false;
                foreach (var candidate in set.draftPool)
                {
                    if (candidate == null || !candidate.IsValid || candidate.Identity == null) continue;
                    bool reachable = candidate.ResultType == DraftResultType.TowerDraft ? canOfferTower : canOfferUpgrade;
                    if (reachable && !currentChoiceSet.displayedIdentities.Contains(candidate.Identity)) { hasOther = true; break; }
                }
                if (!hasOther)
                {
                    result = DraftRerollResult.NoOtherCandidates;
                    hud.ShowNoOtherDraftChoices();
                    return result;
                }
#if UNITY_EDITOR
                sampled = true;
#endif
                if (!TryGenerateDraftChoices(set, kind, out reason)) return result = DraftRerollResult.PreparationFailed;
                int nextRevision = checked(revision + 1);
                if (!hud.TryPrepareDraftChoices(set.draftChoices,
                    selected => HandleDraftSelected(token, nextRevision, selected), out prepared, out reason))
                    return result = IsRefreshing(token, revision) ? DraftRerollResult.PreparationFailed : DraftRerollResult.Cancelled;
                if (!IsRefreshing(token, revision) || !OwnsPause(token) || freeRerollsRemaining != before ||
                    !hud.CanCommitDraftChoices(prepared)) return result = DraftRerollResult.Cancelled;

                // All allocations and validation precede this callback-free ownership transfer.
                hud.CommitDraftChoiceOwnership(prepared);
                currentChoiceSet = set;
                choiceSetRevision = nextRevision;
                freeRerollsRemaining = before - 1;
                committed = true; committedRevision = nextRevision; after = before - 1;
                result = DraftRerollResult.Success;
#if UNITY_EDITOR
                PublishDraftChoicesOpened(token, kind, true, requestId);
#endif
                presentation = hud.PresentDraftChoices(prepared, out reason);
                if (!OwnsDraftSession(token)) presentation = DraftRerollPresentation.Cancelled;
                else if (presentation == DraftRerollPresentation.Cancelled)
                {
                    presentation = DraftRerollPresentation.TechnicalFailure;
                    reason = "The current Draft lost its committed presentation.";
                }
                if (presentation == DraftRerollPresentation.TechnicalFailure)
                    coordinator?.FailDraftPresentation(this, token, reason);
                return result;
            }
            catch (Exception error)
            {
                reason = error.Message;
                Debug.LogException(error, this);
                if (committed)
                {
                    presentation = OwnsDraftSession(token) ? DraftRerollPresentation.TechnicalFailure : DraftRerollPresentation.Cancelled;
                    if (presentation == DraftRerollPresentation.TechnicalFailure)
                        coordinator?.FailDraftPresentation(this, token, reason);
                    return result = DraftRerollResult.Success;
                }
                return result = OwnsDraftSession(token) ? DraftRerollResult.PreparationFailed : DraftRerollResult.Cancelled;
            }
            finally
            {
                try
                {
                    prepared?.Dispose();
                    if (OwnsDraftSession(token)) BindRerollPresentation(token);
                }
                catch (Exception error)
                {
                    reason = error.Message;
                    if (committed)
                    {
                        presentation = OwnsDraftSession(token) ? DraftRerollPresentation.TechnicalFailure : DraftRerollPresentation.Cancelled;
                        if (presentation == DraftRerollPresentation.TechnicalFailure)
                            coordinator?.FailDraftPresentation(this, token, reason);
                    }
                    else Debug.LogException(error, this);
                }
                finally
                {
#if UNITY_EDITOR
                    PublishRerollObservation(outcomes, new DraftRerollObservation(requestId, token, revision, result,
                        committed, sampled, committedRevision, before, after, presentation, reason), observationIdentity);
#endif
                    if (ReferenceEquals(rerollOperation, operation))
                    {
                        if (claimed && OwnsDraftSession(token) && sessionPhase == DraftSessionPhase.Refreshing)
                            sessionPhase = DraftSessionPhase.AwaitingSelection;
                        rerollOperation = null;
                    }
                }
            }
        }
    }



    private bool IsRefreshing(DraftAttemptToken token, int revision) => isBattleActive &&
        activeToken == token && choiceSetRevision == revision && sessionPhase == DraftSessionPhase.Refreshing;

    private DraftChoiceSet GatherDraftCandidates(DraftSessionKind kind)
    {
        var set = new DraftChoiceSet();
        AddTowerDraftCandidates(set);
        if (kind == DraftSessionKind.LevelUp) AddTowerUpgradeDraftCandidates(set);
#if UNITY_EDITOR
        CaptureNaturalCandidateObservations(set);
        set.availableDistinctTowerCount = CountDistinctValidIdentities(set.towerDraftCandidatePool);
        set.availableDistinctUpgradeCount = CountDistinctValidIdentities(set.upgradeDraftCandidatePool);
#endif
        return set;
    }

    private bool TryGenerateDraftChoices(DraftChoiceSet set,
        DraftSessionKind requestedKind,
        out string failureReason)
    {
#if UNITY_EDITOR
        if (useFixedDraftChoices)
        {
            bool generatedFixedChoices = TryGenerateFixedDraftChoices(set,
                requestedKind,
                out failureReason);
            CaptureRealizedCategoryCounts(set);
            return generatedFixedChoices;
        }
#endif

        if (draftRandom == null)
        {
            failureReason = "the Draft-owned random source is not initialized.";
            return false;
        }

        if (requestedKind == DraftSessionKind.Initial)
        {
            SampleDistinctCandidates(set,
                set.towerDraftCandidatePool,
                draftChoiceCount);
        }
        else
        {
            GenerateLevelUpDraftChoices(set);
        }

        ShuffleDraftChoices(set);
#if UNITY_EDITOR
        CaptureRealizedCategoryCounts(set);
#endif

        if (set.draftChoices.Count == 0)
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

    private void AddTowerDraftCandidates(DraftChoiceSet set)
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
                set.draftPool.Add(
                    DraftResult.CreateTowerDraft(towerDefinition));
                set.towerDraftCandidatePool.Add(
                    DraftResult.CreateTowerDraft(towerDefinition));
            }
        }
    }

    private void AddTowerUpgradeDraftCandidates(DraftChoiceSet set)
    {
        if (upgradeDefinitions == null ||
            towerUpgradeSystem == null ||
            submission == null)
        {
            return;
        }

        IReadOnlyList<TowerInstance> deployedTowerInstances =
            submission?.DeployedTowerInstances;

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
                set.draftPool.Add(
                    DraftResult.CreateTowerUpgradeDraft(
                        upgradeDefinition));
                set.upgradeDraftCandidatePool.Add(
                    DraftResult.CreateTowerUpgradeDraft(
                        upgradeDefinition));
            }
        }
    }

    private void GenerateLevelUpDraftChoices(DraftChoiceSet set)
    {
        int towerRequestCount = 0;
        int upgradeRequestCount = 0;

        for (int slotIndex = 0; slotIndex < draftChoiceCount; slotIndex++)
        {
            bool requestsTower =
                draftRandom.NextDouble() < towerDraftSlotProbability;

            if (requestsTower)
            {
                towerRequestCount++;
#if UNITY_EDITOR
                set.requestedCategoryObservations.Add(DraftChoiceCategory.Tower);
#endif
            }
            else
            {
                upgradeRequestCount++;
#if UNITY_EDITOR
                set.requestedCategoryObservations.Add(
                    DraftChoiceCategory.TowerUpgrade);
#endif
            }
        }

#if UNITY_EDITOR
        set.requestedTowerCount = towerRequestCount;
        set.requestedUpgradeCount = upgradeRequestCount;
#endif

        int sampledTowerCount = SampleDistinctCandidates(set,
            set.towerDraftCandidatePool,
            towerRequestCount);
        int sampledUpgradeCount = SampleDistinctCandidates(set,
            set.upgradeDraftCandidatePool,
            upgradeRequestCount);
        int missingTowerSlots = towerRequestCount - sampledTowerCount;
        int missingUpgradeSlots = upgradeRequestCount - sampledUpgradeCount;

        int upgradeBackfillCount = SampleDistinctCandidates(set,
            set.upgradeDraftCandidatePool,
            missingTowerSlots);
        int towerBackfillCount = SampleDistinctCandidates(set,
            set.towerDraftCandidatePool,
            missingUpgradeSlots);

#if UNITY_EDITOR
        set.towerSlotsBackfilledByUpgrade = upgradeBackfillCount;
        set.upgradeSlotsBackfilledByTower = towerBackfillCount;

        if (upgradeBackfillCount > 0 || towerBackfillCount > 0)
        {
            set.backfillReason = "RequestedCategoryExhausted";
        }

        if (set.draftChoices.Count < draftChoiceCount)
        {
            set.backfillReason = "TotalDistinctIdentityExhaustion";
        }
#endif
    }

    private int SampleDistinctCandidates(DraftChoiceSet set,
        List<DraftResult> candidates,
        int requestedCount)
    {
        if (candidates == null || requestedCount <= 0)
        {
            return 0;
        }

        int initialChoiceCount = set.draftChoices.Count;

        while (set.draftChoices.Count - initialChoiceCount < requestedCount &&
               candidates.Count > 0)
        {
            int randomIndex = draftRandom.Next(0, candidates.Count);
            DraftResult selectedResult = candidates[randomIndex];
            candidates.RemoveAt(randomIndex);

            if (selectedResult == null ||
                !selectedResult.IsValid ||
                selectedResult.Identity == null ||
                !set.displayedIdentities.Add(selectedResult.Identity))
            {
                continue;
            }

            set.draftChoices.Add(selectedResult);
        }

        return set.draftChoices.Count - initialChoiceCount;
    }

    private void ShuffleDraftChoices(DraftChoiceSet set)
    {
        for (int i = set.draftChoices.Count - 1; i > 0; i--)
        {
            int swapIndex = draftRandom.Next(0, i + 1);
            DraftResult value = set.draftChoices[i];
            set.draftChoices[i] = set.draftChoices[swapIndex];
            set.draftChoices[swapIndex] = value;
        }
    }

    private static int CountDistinctValidIdentities(
        IReadOnlyList<DraftResult> candidates)
    {
        if (candidates == null)
        {
            return 0;
        }

        HashSet<UnityEngine.Object> identities =
            new HashSet<UnityEngine.Object>();

        for (int i = 0; i < candidates.Count; i++)
        {
            DraftResult candidate = candidates[i];

            if (candidate != null &&
                candidate.IsValid &&
                candidate.Identity != null)
            {
                identities.Add(candidate.Identity);
            }
        }

        return identities.Count;
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
        if (upgradeDefinition == null)
        {
            return 0;
        }

        IReadOnlyList<PendingDraftEntry> pendingDraftItems = PendingDrafts;

        if (pendingDraftItems == null)
        {
            return 0;
        }

        int pendingReservedCapacityCount = 0;

        for (int pendingIndex = 0;
             pendingIndex < pendingDraftItems.Count;
             pendingIndex++)
        {
            PendingDraftEntry pendingDraftItem =
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
        int callbackRevision,
        DraftResult selectedResult)
    {
        if (!isBattleActive ||
            activeToken != callbackToken ||
            choiceSetRevision != callbackRevision || rerollOperation != null ||
            battleHUDUI == null || !battleHUDUI.IsDraftOpen || !OwnsPause(callbackToken) ||
            sessionPhase != DraftSessionPhase.AwaitingSelection ||
            selectedResult == null ||
            !selectedResult.IsValid ||
            selectedResult.Identity == null ||
            !currentChoiceSet.displayedIdentities.Contains(selectedResult.Identity))
        {
            return;
        }

        sessionPhase = DraftSessionPhase.CommittingSelection;
        DraftSessionKind committingKind = sessionKind;

        if (!TryGrantPendingBatch(
                new[] { selectedResult }, new[] { callbackToken }, callbackToken,
                out PendingDraftEntry[] committedItems, out string failureReason))
        {
            if (!IsCommittingSelection(callbackToken)) return;
#if UNITY_EDITOR
            PublishDraftChoiceCommitted(
                callbackToken,
                selectedResult,
                false,
                failureReason);
#endif
            if (!IsCommittingSelection(callbackToken)) return;
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
            committedInitialHeldItem = committedItems[0];
        }

#if UNITY_EDITOR
        PublishDraftChoiceCommitted(
            callbackToken,
            selectedResult,
            true,
            string.Empty);
#endif

        if (!IsCommittingSelection(callbackToken)) return;
        sessionPhase = DraftSessionPhase.Completed;
        InvalidateActiveAuthority();
        battleHUDUI.CloseDraft();
        ReleasePause(callbackToken);

        if (committingKind == DraftSessionKind.Initial && isBattleActive &&
            completedInitialToken == callbackToken && PendingOwner.IsCurrent(committedInitialHeldItem))
        {
            OnInitialDraftCompleted?.Invoke(callbackToken);
        }
    }

    private bool IsCommittingSelection(DraftAttemptToken token) => isBattleActive &&
        activeToken == token && sessionPhase == DraftSessionPhase.CommittingSelection;

    private bool TryGrantPendingBatch(IReadOnlyList<DraftResult> results,
        IReadOnlyList<DraftAttemptToken> sources, DraftAttemptToken selectionToken,
        out PendingDraftEntry[] entries, out string failureReason)
    {
        entries = null;
        failureReason = "Pending preparation is unavailable or the Battle changed.";
        if (rerollOperation != null || isPreparingPendingViews || sessionPhase == DraftSessionPhase.Refreshing || !isBattleActive || battleHUDUI == null ||
            !PendingOwner.TryPrepareGrant(results, sources, out var grant)) return false;
        var views = new List<PendingDraftUIItem>(grant.Entries.Length);
        bool committed = false;
        isPreparingPendingViews = true;
        try
        {
            battleHUDUI.PrepareViewCapacity(grant.Entries.Length);
            foreach (var entry in grant.Entries)
            {
                if (!battleHUDUI.TryPreparePendingView(entry, out var item, out failureReason)) return false;
                views.Add(item);
            }
            failureReason = "Pending registration authority expired during view preparation.";
            if (selectionToken.IsValid && !IsCommittingSelection(selectionToken)) return false;
            if (battleHUDUI == null || !battleHUDUI.ArePreparedViewsUsable(views) || !PendingOwner.TryCommitGrant(grant)) return false;
            // Both lists have reserved capacity; registration contains no callbacks.
            battleHUDUI.RegisterPreparedViews(views);
            entries = grant.Entries;
            committed = true;
            failureReason = string.Empty;
            return true;
        }
        finally
        {
            if (!committed) battleHUDUI.DiscardPreparedViews(views);
            isPreparingPendingViews = false;
        }
    }

#if UNITY_EDITOR
    public bool TryGrantDebugPendingBatch(IReadOnlyList<DraftResult> results,
        IReadOnlyList<DraftAttemptToken> sources, out string failureReason)
    {
        failureReason = "Pending grant is blocked by an active Draft or investment operation.";
        if (sessionPhase == DraftSessionPhase.CommittingSelection ||
            sessionPhase == DraftSessionPhase.AwaitingSelection || towerPlacementController == null ||
            !towerPlacementController.CanStartDraftInteraction) return false;
        return TryGrantPendingBatch(results, sources, default, out _, out failureReason);
    }
#endif

    [ContextMenu("Rebuild Pending Draft Views")]
    private void RebuildPendingViewsForInspection()
    {
        if (!TryRebuildPendingViews(out string failureReason)) Debug.LogWarning(failureReason, this);
    }

    public bool TryRebuildPendingViews(out string failureReason)
    {
        failureReason = "Pending rebuild is unavailable during a Draft or investment operation.";
        if (!isBattleActive || rerollOperation != null || isPreparingPendingViews || battleHUDUI == null ||
            (sessionPhase == DraftSessionPhase.CommittingSelection || sessionPhase == DraftSessionPhase.Refreshing) ||
            towerPlacementController == null || !towerPlacementController.CanStartDraftInteraction) return false;
        isPreparingPendingViews = true;
        ulong generation = currentBattleGeneration;
        var entries = new List<PendingDraftEntry>(PendingDrafts);
        var views = new List<PendingDraftUIItem>(entries.Count);
        bool replaced = false;
        try
        {
            towerPlacementController.CancelPlacement();
            battleHUDUI.PrepareViewCapacity(entries.Count);
            foreach (var entry in entries)
            {
                if (!battleHUDUI.TryPreparePendingView(entry, out var item, out failureReason)) return false;
                views.Add(item);
            }
            failureReason = "Pending ownership changed during view preparation.";
            if (!isBattleActive || currentBattleGeneration != generation || entries.Count != PendingDrafts.Count) return false;
            for (int i = 0; i < entries.Count; i++)
                if (!ReferenceEquals(entries[i], PendingDrafts[i])) return false;
            if (battleHUDUI == null || !battleHUDUI.ArePreparedViewsUsable(views)) return false;
            battleHUDUI.ReplacePendingViews(views);
            replaced = true;
            failureReason = string.Empty;
            return true;
        }
        finally
        {
            if (!replaced) battleHUDUI.DiscardPreparedViews(views);
            isPreparingPendingViews = false;
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
        if (activeToken != attemptToken)
        {
            ReleasePause(attemptToken);
            return;
        }

        InvalidateActiveAuthority();
        sessionPhase = DraftSessionPhase.None;
        battleHUDUI?.CloseDraft();
        ReleasePause(attemptToken);
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
        currentChoiceSet = new DraftChoiceSet();
    }

    private void InitializeDraftRandom()
    {
#if UNITY_EDITOR
        activeDraftSeed = useFixedDraftSeed
            ? fixedDraftSeed
            : CreateFreshDraftSeed();
#else
        activeDraftSeed = CreateFreshDraftSeed();
#endif
        draftRandom = new System.Random(activeDraftSeed);
    }

    private int CreateFreshDraftSeed()
    {
        return unchecked(
            Guid.NewGuid().GetHashCode() ^
            GetInstanceID() ^
            (int)currentBattleGeneration);
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

    private bool TryGenerateFixedDraftChoices(DraftChoiceSet set,
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

            if (!TryGetNaturalCandidate(set,
                    configuredResult.Identity,
                    out DraftResult eligibleResult))
            {
                failureReason =
                    $"Fixed Draft Step {stepIndex + 1} Choice " +
                    $"'{configuredResult.DisplayName}' is not naturally " +
                    "eligible in the current Draft state.";
                return false;
            }

            if (!set.displayedIdentities.Add(eligibleResult.Identity))
            {
                failureReason =
                    $"Fixed Draft Step {stepIndex + 1} contains duplicate " +
                    $"Choice '{eligibleResult.DisplayName}'.";
                return false;
            }

            set.draftChoices.Add(eligibleResult);
        }

        failureReason = string.Empty;
        return set.draftChoices.Count > 0;
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

    private void CaptureNaturalCandidateObservations(DraftChoiceSet set)
    {
        set.naturalCandidateObservations.Clear();

        for (int poolIndex = 0;
             poolIndex < set.draftPool.Count;
             poolIndex++)
        {
            DraftResult result = set.draftPool[poolIndex];

            if (result == null || !result.IsValid || result.Identity == null)
            {
                continue;
            }

            DraftChoiceCandidateObservation existing = null;

            for (int candidateIndex = 0;
                 candidateIndex < set.naturalCandidateObservations.Count;
                 candidateIndex++)
            {
                DraftChoiceCandidateObservation candidate =
                    set.naturalCandidateObservations[candidateIndex];

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
                set.naturalCandidateObservations.Add(
                    new DraftChoiceCandidateObservation(result, 1));
            }
        }
    }

    private bool TryGetNaturalCandidate(DraftChoiceSet set,
        UnityEngine.Object identity,
        out DraftResult result)
    {
        for (int i = 0; i < set.naturalCandidateObservations.Count; i++)
        {
            DraftChoiceCandidateObservation candidate =
                set.naturalCandidateObservations[i];

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

    private void CaptureRealizedCategoryCounts(DraftChoiceSet set)
    {
        set.realizedTowerCount = 0;
        set.realizedUpgradeCount = 0;

        for (int i = 0; i < set.draftChoices.Count; i++)
        {
            DraftResult choice = set.draftChoices[i];

            if (choice == null)
            {
                continue;
            }

            if (choice.ResultType == DraftResultType.TowerDraft)
            {
                set.realizedTowerCount++;
            }
            else if (choice.ResultType == DraftResultType.TowerUpgradeDraft)
            {
                set.realizedUpgradeCount++;
            }
        }
    }

    private void PublishDraftChoicesOpened(
        DraftAttemptToken attemptToken,
        DraftSessionKind requestedKind, bool isReroll = false, long requestId = 0)
    {
        if (!isReroll)
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
        }

        DraftChoicesOpenedObservation observation =
            new DraftChoicesOpenedObservation(
                attemptToken,
                activeDraftOrdinal,
                activeObservationSessionKind,
                activeGenerationMode,
                draftChoiceCount,
                activeDraftSeed,
                towerDraftSlotProbability,
                GenerationContractVersion,
                DraftRandomAlgorithmVersion,
                currentChoiceSet.requestedCategoryObservations,
                currentChoiceSet.requestedTowerCount,
                currentChoiceSet.requestedUpgradeCount,
                currentChoiceSet.availableDistinctTowerCount,
                currentChoiceSet.availableDistinctUpgradeCount,
                currentChoiceSet.realizedTowerCount,
                currentChoiceSet.realizedUpgradeCount,
                currentChoiceSet.towerSlotsBackfilledByUpgrade,
                currentChoiceSet.upgradeSlotsBackfilledByTower,
                currentChoiceSet.backfillReason,
                currentChoiceSet.naturalCandidateObservations,
                currentChoiceSet.draftChoices, choiceSetRevision,
                freeRerollsRemaining + (isReroll ? 1 : 0), freeRerollsRemaining, requestId);
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
        DraftResult selectedResult,
        bool heldItemCreationSucceeded,
        string failureReason)
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
                selectedResult,
                heldItemCreationSucceeded,
                failureReason, choiceSetRevision);
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
