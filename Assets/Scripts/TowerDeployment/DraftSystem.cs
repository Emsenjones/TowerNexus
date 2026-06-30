using System.Collections.Generic;
using UnityEngine;

public class DraftSystem : MonoBehaviour
{
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private TowerDefinitionDatabase towerDefinitionDatabase;
    [SerializeField] private TowerUpgradeDatabase towerUpgradeDatabase;
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private int draftChoiceCount = 3;

    private readonly List<DraftResult> draftChoices = new List<DraftResult>();
    private readonly List<DraftResult> draftPool = new List<DraftResult>();
    private readonly HashSet<Object> displayedIdentities = new HashSet<Object>();

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
        OpenTowerDraft();
    }

    private void OpenTowerDraft()
    {
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
        if (towerDefinitionDatabase == null)
        {
            Debug.LogWarning("Draft system cannot generate tower draft candidates: tower definition database is not assigned.", this);
            return;
        }

        IReadOnlyList<TowerDefinition> towerDefinitions = towerDefinitionDatabase.GetAllTowers();

        if (towerDefinitions == null)
        {
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
        if (towerUpgradeDatabase == null || towerUpgradeSystem == null || towerPlacementController == null)
        {
            return;
        }

        IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions = towerUpgradeDatabase.GetAllUpgrades();
        IReadOnlyList<TowerInstance> deployedTowerInstances = towerPlacementController.DeployedTowerInstances;

        if (upgradeDefinitions == null || deployedTowerInstances == null)
        {
            return;
        }

        for (int towerIndex = 0; towerIndex < deployedTowerInstances.Count; towerIndex++)
        {
            TowerInstance towerInstance = deployedTowerInstances[towerIndex];

            if (towerInstance == null)
            {
                continue;
            }

            for (int upgradeIndex = 0; upgradeIndex < upgradeDefinitions.Count; upgradeIndex++)
            {
                TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[upgradeIndex];

                if (upgradeDefinition == null)
                {
                    continue;
                }

                if (towerUpgradeSystem.CanApplyUpgrade(towerInstance, upgradeDefinition, out _))
                {
                    draftPool.Add(DraftResult.CreateTowerUpgradeDraft(upgradeDefinition));
                }
            }
        }
    }

    private void HandleDraftSelected(DraftResult draftResult)
    {
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
