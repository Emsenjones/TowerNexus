using System.Collections.Generic;
using UnityEngine;

public class TowerDraftSystem : MonoBehaviour
{
    [SerializeField] private PlayerLevelSystem playerLevelSystem;
    [SerializeField] private TowerDefinitionDatabase towerDefinitionDatabase;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private int draftChoiceCount = 3;

    private readonly List<TowerDefinition> draftChoices = new List<TowerDefinition>();
    private readonly List<TowerDefinition> draftPool = new List<TowerDefinition>();

    private void OnEnable()
    {
        if (playerLevelSystem != null)
        {
            playerLevelSystem.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (playerLevelSystem != null)
        {
            playerLevelSystem.OnLevelUp -= HandleLevelUp;
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
            Debug.LogWarning("Tower draft system cannot open draft: battle HUD UI is not assigned.", this);
            return;
        }

        GenerateDraftChoices();

        if (draftChoices.Count == 0)
        {
            Debug.LogWarning("Tower draft system cannot open draft: no tower definitions are available.", this);
            return;
        }

        battleHUDUI.OpenDraft(draftChoices, HandleTowerSelected);
    }

    private void GenerateDraftChoices()
    {
        draftChoices.Clear();
        draftPool.Clear();

        if (towerDefinitionDatabase == null)
        {
            Debug.LogWarning("Tower draft system cannot generate draft choices: tower definition database is not assigned.", this);
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
                draftPool.Add(towerDefinition);
            }
        }

        int choiceCount = Mathf.Min(Mathf.Max(1, draftChoiceCount), draftPool.Count);

        for (int i = 0; i < choiceCount; i++)
        {
            int randomIndex = Random.Range(0, draftPool.Count);
            TowerDefinition selectedTower = draftPool[randomIndex];
            draftPool.RemoveAt(randomIndex);
            draftChoices.Add(selectedTower);
        }
    }

    private void HandleTowerSelected(TowerDefinition towerDefinition)
    {
        if (towerDefinition == null)
        {
            Debug.LogWarning("Tower draft system cannot handle draft selection: tower definition is null.", this);
            return;
        }

        if (battleHUDUI == null)
        {
            Debug.LogWarning("Tower draft system cannot add selected tower: battle HUD UI is not assigned.", this);
            return;
        }

        battleHUDUI.AddPendingTower(towerDefinition);
    }
}
