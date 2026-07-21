using System.Collections.Generic;
using UnityEngine;

public class TowerDeployController : MonoBehaviour
{
    private TowerPlacementValidator placementValidator;
    [SerializeField] private Transform deployedTowerRoot;
    [SerializeField] private MonsterManager monsterManager;
    private BattleHUDUI battleHUDUI;
    private MapGeneratorBehaviour mapGenerator;

    public void Initialize(
        TowerPlacementValidator placementValidator,
        MapGeneratorBehaviour mapGenerator,
        BattleHUDUI battleHUDUI)
    {
        this.placementValidator = placementValidator;
        this.mapGenerator = mapGenerator;
        this.battleHUDUI = battleHUDUI;
    }

    public void Initialize(
        TowerPlacementValidator placementValidator,
        MapGeneratorBehaviour mapGenerator,
        BattleHUDUI battleHUDUI,
        MonsterManager monsterManager)
    {
        this.placementValidator = placementValidator;
        this.mapGenerator = mapGenerator;
        this.battleHUDUI = battleHUDUI;
        this.monsterManager = monsterManager;
    }

    public bool TryDeployTower(TowerPlacementPreview preview, PendingDraftUIItem draftedDraftEntry)
    {
        return TryDeployTower(preview, draftedDraftEntry, out _);
    }

    public bool TryDeployTower(
        TowerPlacementPreview preview,
        PendingDraftUIItem draftedDraftEntry,
        out TowerBehaviour deployedTower)
    {
        deployedTower = null;

        if (preview == null)
        {
            Debug.LogWarning("Tower deploy controller cannot deploy tower: placement preview is null.", this);
            return false;
        }

        TowerDefinition towerDefinition = preview.TowerDefinition;

        if (towerDefinition == null)
        {
            Debug.LogWarning("Tower deploy controller cannot deploy tower: tower definition is null.", preview);
            return false;
        }

        if (towerDefinition.TowerPrefab == null)
        {
            Debug.LogWarning("Tower deploy controller cannot deploy tower: tower prefab is not assigned.", towerDefinition);
            return false;
        }

        if (!towerDefinition.TryGetCombatBehaviour(out _, out string combatFailureReason))
        {
            Debug.LogWarning(
                $"Tower deploy controller cannot deploy tower: {combatFailureReason}",
                towerDefinition);
            return false;
        }

        if (placementValidator == null)
        {
            Debug.LogWarning("Tower deploy controller cannot deploy tower: placement validator is not assigned.", this);
            return false;
        }

        if (!placementValidator.CanPlaceTower(preview, out List<GridNodeBehaviour> occupiedNodes))
        {
            return false;
        }

        if (draftedDraftEntry != null && battleHUDUI == null)
        {
            Debug.LogWarning("Tower deploy controller cannot deploy tower: battle HUD UI is not assigned for drafted tower entry removal.", this);
            return false;
        }

        GameObject towerObject = Instantiate(towerDefinition.TowerPrefab, deployedTowerRoot);
        towerObject.transform.SetPositionAndRotation(preview.transform.position, preview.transform.rotation);
        towerObject.transform.localScale = preview.transform.localScale;

        if (!towerObject.TryGetComponent(out TowerInstance towerInstance))
        {
            towerInstance = towerObject.AddComponent<TowerInstance>();
        }

        towerInstance.Initialize(towerDefinition, occupiedNodes);

        if (!towerObject.TryGetComponent(out TowerBehaviour towerBehaviour))
        {
            towerBehaviour = towerObject.AddComponent<TowerBehaviour>();
        }

        towerBehaviour.Initialize(towerInstance);
        towerBehaviour.RefreshTowerVisual();

        if (!towerObject.TryGetComponent(out TowerCombatBehaviour towerCombatBehaviour))
        {
            Debug.LogWarning(
                "Tower deploy controller rejected instantiated tower: validated root combat component is missing.",
                towerObject);
            Destroy(towerObject);
            return false;
        }

        towerCombatBehaviour.Initialize(towerInstance, monsterManager);

        for (int i = 0; i < occupiedNodes.Count; i++)
        {
            GridNodeBehaviour node = occupiedNodes[i];

            if (node != null)
            {
                node.SetWalkable(false);
            }
        }

        if (mapGenerator != null)
        {
            mapGenerator.RefreshMapVisual();
        }

        if (monsterManager != null)
        {
            monsterManager.RecalculateAllMonsterPaths();
        }

        if (battleHUDUI != null && draftedDraftEntry != null)
        {
            battleHUDUI.RemovePendingDraft(draftedDraftEntry);
        }

        deployedTower = towerBehaviour;
        return true;
    }
}
