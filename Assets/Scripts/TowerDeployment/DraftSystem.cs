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
        IReadOnlyList<DraftResult> displayedChoices)
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
        string failureReason)
    {
        AttemptToken = attemptToken;
        DraftOrdinal = draftOrdinal;
        SessionKind = sessionKind;
        GenerationMode = generationMode;
        SelectedChoice = selectedChoice;
        HeldItemCreationSucceeded = heldItemCreationSucceeded;
        FailureReason = failureReason ?? string.Empty;
    }

    public DraftAttemptToken AttemptToken { get; }
    public int DraftOrdinal { get; }
    public DraftChoiceSessionKind SessionKind { get; }
    public DraftChoiceGenerationMode GenerationMode { get; }
    public DraftResult SelectedChoice { get; }
    public bool HeldItemCreationSucceeded { get; }
    public string FailureReason { get; }
}
#endif

public class DraftSystem : MonoBehaviour
{
    public const string GenerationContractVersion = "CategorySlotV1";
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
    [SerializeField] private bool useFixedDraftSeed;
    [SerializeField] private int fixedDraftSeed = 1;
    [SerializeField] private List<FixedDraftChoiceStep>
        fixedDraftChoiceSteps = new List<FixedDraftChoiceStep>();
#endif

    private readonly List<DraftResult> draftChoices =
        new List<DraftResult>();
    private readonly List<DraftResult> draftPool =
        new List<DraftResult>();
    private readonly List<DraftResult> towerDraftCandidatePool =
        new List<DraftResult>();
    private readonly List<DraftResult> upgradeDraftCandidatePool =
        new List<DraftResult>();
    private readonly HashSet<UnityEngine.Object> displayedIdentities =
        new HashSet<UnityEngine.Object>();

#if UNITY_EDITOR
    private readonly List<DraftChoiceCandidateObservation>
        naturalCandidateObservations =
            new List<DraftChoiceCandidateObservation>();
    private readonly List<DraftChoiceCategory> requestedCategoryObservations =
        new List<DraftChoiceCategory>();
    private int openedDraftCount;
    private int activeDraftOrdinal;
    private DraftChoiceGenerationMode activeGenerationMode;
    private DraftChoiceSessionKind activeObservationSessionKind;
    private int requestedTowerCount;
    private int requestedUpgradeCount;
    private int availableDistinctTowerCount;
    private int availableDistinctUpgradeCount;
    private int realizedTowerCount;
    private int realizedUpgradeCount;
    private int towerSlotsBackfilledByUpgrade;
    private int upgradeSlotsBackfilledByTower;
    private string backfillReason = string.Empty;
#endif

    private IReadOnlyList<TowerDefinition> towerDefinitions;
    private IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions;
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
    internal bool IsPendingMutationBusy => isPreparingPendingViews ||
        sessionPhase == DraftSessionPhase.CommittingSelection;

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
        float selectedTowerDraftSlotProbability)
    {
        if (selectedTowerDefinitions == null ||
            selectedUpgradeDefinitions == null ||
            float.IsNaN(selectedTowerDraftSlotProbability) ||
            float.IsInfinity(selectedTowerDraftSlotProbability) ||
            selectedTowerDraftSlotProbability < 0f ||
            selectedTowerDraftSlotProbability > 1f)
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
        hasStageDraftConfiguration = true;
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
        availableDistinctTowerCount = CountDistinctValidIdentities(
            towerDraftCandidatePool);
        availableDistinctUpgradeCount = CountDistinctValidIdentities(
            upgradeDraftCandidatePool);

        if (useFixedDraftChoices)
        {
            bool generatedFixedChoices = TryGenerateFixedDraftChoices(
                requestedKind,
                out failureReason);
            CaptureRealizedCategoryCounts();
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
            SampleDistinctCandidates(
                towerDraftCandidatePool,
                draftChoiceCount);
        }
        else
        {
            GenerateLevelUpDraftChoices();
        }

        ShuffleDraftChoices();
#if UNITY_EDITOR
        CaptureRealizedCategoryCounts();
#endif

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
                towerDraftCandidatePool.Add(
                    DraftResult.CreateTowerDraft(towerDefinition));
            }
        }
    }

    private void AddTowerUpgradeDraftCandidates()
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
                draftPool.Add(
                    DraftResult.CreateTowerUpgradeDraft(
                        upgradeDefinition));
                upgradeDraftCandidatePool.Add(
                    DraftResult.CreateTowerUpgradeDraft(
                        upgradeDefinition));
            }
        }
    }

    private void GenerateLevelUpDraftChoices()
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
                requestedCategoryObservations.Add(DraftChoiceCategory.Tower);
#endif
            }
            else
            {
                upgradeRequestCount++;
#if UNITY_EDITOR
                requestedCategoryObservations.Add(
                    DraftChoiceCategory.TowerUpgrade);
#endif
            }
        }

#if UNITY_EDITOR
        requestedTowerCount = towerRequestCount;
        requestedUpgradeCount = upgradeRequestCount;
#endif

        int sampledTowerCount = SampleDistinctCandidates(
            towerDraftCandidatePool,
            towerRequestCount);
        int sampledUpgradeCount = SampleDistinctCandidates(
            upgradeDraftCandidatePool,
            upgradeRequestCount);
        int missingTowerSlots = towerRequestCount - sampledTowerCount;
        int missingUpgradeSlots = upgradeRequestCount - sampledUpgradeCount;

        int upgradeBackfillCount = SampleDistinctCandidates(
            upgradeDraftCandidatePool,
            missingTowerSlots);
        int towerBackfillCount = SampleDistinctCandidates(
            towerDraftCandidatePool,
            missingUpgradeSlots);

#if UNITY_EDITOR
        towerSlotsBackfilledByUpgrade = upgradeBackfillCount;
        upgradeSlotsBackfilledByTower = towerBackfillCount;

        if (upgradeBackfillCount > 0 || towerBackfillCount > 0)
        {
            backfillReason = "RequestedCategoryExhausted";
        }

        if (draftChoices.Count < draftChoiceCount)
        {
            backfillReason = "TotalDistinctIdentityExhaustion";
        }
#endif
    }

    private int SampleDistinctCandidates(
        List<DraftResult> candidates,
        int requestedCount)
    {
        if (candidates == null || requestedCount <= 0)
        {
            return 0;
        }

        int initialChoiceCount = draftChoices.Count;

        while (draftChoices.Count - initialChoiceCount < requestedCount &&
               candidates.Count > 0)
        {
            int randomIndex = draftRandom.Next(0, candidates.Count);
            DraftResult selectedResult = candidates[randomIndex];
            candidates.RemoveAt(randomIndex);

            if (selectedResult == null ||
                !selectedResult.IsValid ||
                selectedResult.Identity == null ||
                !displayedIdentities.Add(selectedResult.Identity))
            {
                continue;
            }

            draftChoices.Add(selectedResult);
        }

        return draftChoices.Count - initialChoiceCount;
    }

    private void ShuffleDraftChoices()
    {
        for (int i = draftChoices.Count - 1; i > 0; i--)
        {
            int swapIndex = draftRandom.Next(0, i + 1);
            DraftResult value = draftChoices[i];
            draftChoices[i] = draftChoices[swapIndex];
            draftChoices[swapIndex] = value;
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
        if (isPreparingPendingViews || !isBattleActive || battleHUDUI == null ||
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
        if (!isBattleActive || isPreparingPendingViews || battleHUDUI == null ||
            sessionPhase == DraftSessionPhase.CommittingSelection ||
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
        towerDraftCandidatePool.Clear();
        upgradeDraftCandidatePool.Clear();
        displayedIdentities.Clear();
#if UNITY_EDITOR
        naturalCandidateObservations.Clear();
        requestedCategoryObservations.Clear();
        requestedTowerCount = 0;
        requestedUpgradeCount = 0;
        availableDistinctTowerCount = 0;
        availableDistinctUpgradeCount = 0;
        realizedTowerCount = 0;
        realizedUpgradeCount = 0;
        towerSlotsBackfilledByUpgrade = 0;
        upgradeSlotsBackfilledByTower = 0;
        backfillReason = string.Empty;
#endif
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

    private void CaptureRealizedCategoryCounts()
    {
        realizedTowerCount = 0;
        realizedUpgradeCount = 0;

        for (int i = 0; i < draftChoices.Count; i++)
        {
            DraftResult choice = draftChoices[i];

            if (choice == null)
            {
                continue;
            }

            if (choice.ResultType == DraftResultType.TowerDraft)
            {
                realizedTowerCount++;
            }
            else if (choice.ResultType == DraftResultType.TowerUpgradeDraft)
            {
                realizedUpgradeCount++;
            }
        }
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
                draftChoiceCount,
                activeDraftSeed,
                towerDraftSlotProbability,
                GenerationContractVersion,
                DraftRandomAlgorithmVersion,
                requestedCategoryObservations,
                requestedTowerCount,
                requestedUpgradeCount,
                availableDistinctTowerCount,
                availableDistinctUpgradeCount,
                realizedTowerCount,
                realizedUpgradeCount,
                towerSlotsBackfilledByUpgrade,
                upgradeSlotsBackfilledByTower,
                backfillReason,
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
                failureReason);
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
