using System.Collections.Generic;
using UnityEngine;

public class TowerDeployController : MonoBehaviour
{
    private TowerPlacementValidator placementValidator;
    [SerializeField] private Transform deployedTowerRoot;
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

    public bool TryDeployTower(TowerPlacementPreview preview, PendingTowerItemUI draftedTowerEntry)
    {
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

        if (placementValidator == null)
        {
            Debug.LogWarning("Tower deploy controller cannot deploy tower: placement validator is not assigned.", this);
            return false;
        }

        if (!placementValidator.CanPlaceTower(preview, out List<GridNodeBehaviour> occupiedNodes))
        {
            return false;
        }

        if (draftedTowerEntry != null && battleHUDUI == null)
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

        if (battleHUDUI != null && draftedTowerEntry != null)
        {
            battleHUDUI.RemovePendingTower(draftedTowerEntry);
        }

        return true;
    }
}
