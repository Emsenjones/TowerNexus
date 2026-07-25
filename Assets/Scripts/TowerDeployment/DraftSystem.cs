using System.Collections.Generic;
using UnityEngine;

public class DraftSystem : MonoBehaviour
{
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private int draftChoiceCount = 3;

    private readonly List<DraftResult> draftChoices = new List<DraftResult>();
    private readonly List<DraftResult> draftPool = new List<DraftResult>();
    private readonly HashSet<Object> displayedIdentities = new HashSet<Object>();
    private IReadOnlyList<TowerDefinition> towerDefinitions;
    private IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions;
    private bool isBattleActive;

    public bool IsBattleActive => isBattleActive;
    public bool HasStagePools => towerDefinitions != null && upgradeDefinitions != null;

    private void OnEnable()
    {
        if (playerSystem != null)
            playerSystem.OnLevelUp += HandleLevelUp;
        else
            Debug.LogWarning("playerSystem is not assigned.", this);
    }

    private void OnDisable()
    {
        if (playerSystem != null)
        {
            playerSystem.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        if (!isBattleActive)
        {
            return;
        }

        OpenTowerDraft();
    }

    public bool BindStagePools(
        IReadOnlyList<TowerDefinition> selectedTowerDefinitions,
        IReadOnlyList<TowerUpgradeDefinition> selectedUpgradeDefinitions)
    {
        if (selectedTowerDefinitions == null || selectedUpgradeDefinitions == null)
        {
            Debug.LogError(
                "Draft system cannot bind Stage pools because one or both pools are null.",
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

        if (!HasStagePools)
        {
            failureReason = "Stage Draft pools are not bound.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void BeginBattle()
    {
        isBattleActive = true;
        battleHUDUI?.BeginBattle();
    }

    public void StopBattle()
    {
        isBattleActive = false;
        draftChoices.Clear();
        draftPool.Clear();
        displayedIdentities.Clear();
        battleHUDUI?.StopBattle();
    }

    public void ClearStageUi()
    {
        StopBattle();
        battleHUDUI?.ClearStageRuntime();
    }

    public void ClearStagePools()
    {
        draftChoices.Clear();
        draftPool.Clear();
        displayedIdentities.Clear();
        towerDefinitions = null;
        upgradeDefinitions = null;
    }

    public void ClearStageRuntime()
    {
        ClearStageUi();
        ClearStagePools();
    }

    private void OpenTowerDraft()
    {
        if (!isBattleActive)
        {
            return;
        }

        if (battleHUDUI == null)
        {
            Debug.LogWarning("Draft system cannot open draft: battle HUD UI is not assigned.", this);
            return;
        }

        GenerateDraftChoices();

        if (draftChoices.Count == 0)
        {
            Debug.LogWarning("Draft system cannot open draft: no draft candidates are available.", this);
            return;
        }

        battleHUDUI.OpenDraft(draftChoices, HandleDraftSelected);
    }

    private void GenerateDraftChoices()
    {
        draftChoices.Clear();
        draftPool.Clear();
        displayedIdentities.Clear();

        AddTowerDraftCandidates();
        AddTowerUpgradeDraftCandidates();

        int choiceCount = Mathf.Max(1, draftChoiceCount);

        while (draftChoices.Count < choiceCount && draftPool.Count > 0)
        {
            int randomIndex = Random.Range(0, draftPool.Count);
            DraftResult selectedResult = draftPool[randomIndex];
            draftPool.RemoveAt(randomIndex);

            if (selectedResult == null || !selectedResult.IsValid || selectedResult.Identity == null)
            {
                continue;
            }

            if (!displayedIdentities.Add(selectedResult.Identity))
            {
                continue;
            }

            draftChoices.Add(selectedResult);
        }
    }

    private void AddTowerDraftCandidates()
    {
        if (towerDefinitions == null)
        {
            Debug.LogWarning(
                "Draft system cannot generate Tower Draft candidates: " +
                "the active Stage Tower pool is not bound.",
                this);
            return;
        }

        for (int i = 0; i < towerDefinitions.Count; i++)
        {
            TowerDefinition towerDefinition = towerDefinitions[i];

            if (towerDefinition != null)
            {
                draftPool.Add(DraftResult.CreateTowerDraft(towerDefinition));
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

        IReadOnlyList<TowerInstance> deployedTowerInstances = towerPlacementController.DeployedTowerInstances;

        if (upgradeDefinitions == null || deployedTowerInstances == null)
        {
            return;
        }

        for (int upgradeIndex = 0; upgradeIndex < upgradeDefinitions.Count; upgradeIndex++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[upgradeIndex];

            if (upgradeDefinition == null)
            {
                continue;
            }

            int eligibleTowerCount = CountEligibleTowerInstancesForUpgrade(upgradeDefinition, deployedTowerInstances);
            int pendingReservedCapacityCount = CountPendingReservedCapacityForUpgrade(upgradeDefinition);
            int candidateCount = Mathf.Max(0, eligibleTowerCount - pendingReservedCapacityCount);

            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                draftPool.Add(DraftResult.CreateTowerUpgradeDraft(upgradeDefinition));
            }
        }
    }

    private int CountEligibleTowerInstancesForUpgrade(
        TowerUpgradeDefinition upgradeDefinition,
        IReadOnlyList<TowerInstance> deployedTowerInstances)
    {
        if (upgradeDefinition == null || deployedTowerInstances == null || towerUpgradeSystem == null)
        {
            return 0;
        }

        int eligibleTowerCount = 0;

        for (int towerIndex = 0; towerIndex < deployedTowerInstances.Count; towerIndex++)
        {
            TowerInstance towerInstance = deployedTowerInstances[towerIndex];

            if (towerInstance != null &&
                towerUpgradeSystem.CanApplyUpgrade(towerInstance, upgradeDefinition, out _))
            {
                eligibleTowerCount++;
            }
        }

        return eligibleTowerCount;
    }

    private int CountPendingReservedCapacityForUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        if (upgradeDefinition == null || battleHUDUI == null)
        {
            return 0;
        }

        IReadOnlyList<PendingDraftUIItem> pendingDraftItems = battleHUDUI.PendingDraftItems;

        if (pendingDraftItems == null)
        {
            return 0;
        }

        int pendingReservedCapacityCount = 0;

        for (int pendingIndex = 0; pendingIndex < pendingDraftItems.Count; pendingIndex++)
        {
            PendingDraftUIItem pendingDraftItem = pendingDraftItems[pendingIndex];
            TowerUpgradeDefinition pendingUpgradeDefinition = pendingDraftItem != null
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

    private bool DoesPendingUpgradeReserveCapacityForUpgrade(
        TowerUpgradeDefinition pendingUpgradeDefinition,
        TowerUpgradeDefinition candidateUpgradeDefinition)
    {
        if (pendingUpgradeDefinition == null || candidateUpgradeDefinition == null)
        {
            return false;
        }

        if (pendingUpgradeDefinition == candidateUpgradeDefinition)
        {
            return true;
        }

        return pendingUpgradeDefinition.UpgradeLayer == TowerUpgradeLayer.Elemental &&
               candidateUpgradeDefinition.UpgradeLayer == TowerUpgradeLayer.Elemental &&
               pendingUpgradeDefinition.TowerFamily == candidateUpgradeDefinition.TowerFamily;
    }

    private void HandleDraftSelected(DraftResult draftResult)
    {
        if (!isBattleActive)
        {
            return;
        }

        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Draft system cannot handle draft selection: draft result is invalid.", this);
            return;
        }

        if (battleHUDUI == null)
        {
            Debug.LogWarning("Draft system cannot add selected result: battle HUD UI is not assigned.", this);
            return;
        }

        battleHUDUI.AddPendingDraft(draftResult);
    }
}
