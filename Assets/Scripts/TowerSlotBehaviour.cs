using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSlotBehaviour : MonoBehaviour,IBeginDragHandler,IEndDragHandler,IDragHandler
{
    Image image;
    [SerializeField] TextMeshProUGUI nameText;
    new Camera camera;
    GameObject relatedTowerPrefab;
    /// <summary>
    /// This gameObject is instantiated when the player begin dragging.
    /// </summary>
    TowerBehaviour instantiatedTower;
    public void Initialize(GameObject towerPrefab,Camera mainCamera)
    {
        image = GetComponent<Image>();
        camera = mainCamera;
        relatedTowerPrefab = towerPrefab;

        TowerBehaviour tower = relatedTowerPrefab.GetComponent<TowerBehaviour>();
        if (tower == null)
        {
            Debug.LogError($"{relatedTowerPrefab.name} is missing a TowerBehaviour!");
            return;
        }
        if (image is null)
        {
            Debug.LogError($"{gameObject.name} is missing a Image!");
            return;
        }
        image.sprite = tower.IconSprite;
        if (nameText == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing nameText!");
            return;
        }
        nameText.text = tower.TheName;
        
        //Set others ui info via tower.
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        instantiatedTower = DungeonManager.Instance.RecyclePoolController
            .GenerateOneObject(relatedTowerPrefab)
            .GetComponent<TowerBehaviour>();
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (instantiatedTower == null)
        {
            Debug.LogError($"{gameObject.name} is missing a instantiated tower!");
            return;
        }
        #region Snapping the center point of the instantiatedTower to the closest grid.
        if (camera == null)
        {
            Debug.LogError($"{gameObject.name} is missing a camera!");
            return;
        }
        //Getting the woldPosition of the instantiatedTower
        Vector2 instantiatedTowerPosition = camera.ScreenToWorldPoint(eventData.position);
        MapController mapController = DungeonManager.Instance.MapController;
        GridBehaviour snappedGrid = mapController.GetClosestGrid(instantiatedTowerPosition);
        if (snappedGrid == null)
        {
            Debug.LogError($"{gameObject.name} cannot get snapped grid!");
            return;
        }
        instantiatedTower.transform.position = snappedGrid.transform.position;
        #endregion
         bool isDeployable = mapController.CanDeployTower(instantiatedTower.GridTransformList);
         instantiatedTower.Deploying(isDeployable);

    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (instantiatedTower == null)
        {
            Debug.LogError($"{gameObject.name} is missing a instantiated tower!");
            return;
        }
        MapController mapController = DungeonManager.Instance.MapController;
        bool isDeployable = mapController.CanDeployTower(instantiatedTower.GridTransformList);
        if (isDeployable)
        {
            mapController.Deploy(instantiatedTower.GridTransformList);
            instantiatedTower.Initialize();
            DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
        }
        else
            DungeonManager.Instance.RecyclePoolController.RecycleOneObject(instantiatedTower.gameObject);
    }

}
