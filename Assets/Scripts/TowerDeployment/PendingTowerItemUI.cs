using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PendingTowerItemUI : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    private TowerDefinition towerDefinition;
    private TowerPlacementController placementController;

    public TowerDefinition TowerDefinition => towerDefinition;

    public void Initialize(TowerDefinition towerDefinition)
    {
        Initialize(towerDefinition, placementController);
    }

    public void Initialize(TowerDefinition towerDefinition, TowerPlacementController placementController)
    {
        this.towerDefinition = towerDefinition;
        this.placementController = placementController;

        if (towerDefinition == null)
        {
            Debug.LogWarning("Pending tower item UI cannot initialize: tower definition is null.", this);
            return;
        }

        UpdateIcon(towerDefinition.Icon);
        UpdateName(towerDefinition);
    }

    public void SetPlacementController(TowerPlacementController placementController)
    {
        this.placementController = placementController;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (placementController == null)
        {
            Debug.LogWarning("Pending tower item UI cannot begin placement: placement controller is not assigned.", this);
            return;
        }

        if (towerDefinition == null)
        {
            Debug.LogWarning("Pending tower item UI cannot begin placement: tower definition is null.", this);
            return;
        }

        placementController.BeginPlacement(towerDefinition);
    }

    private void UpdateIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            Debug.LogWarning("Pending tower item UI cannot update icon: icon image is not assigned.", this);
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        if (icon == null)
        {
            Debug.LogWarning($"Pending tower item UI has no icon for tower '{GetTowerName(towerDefinition)}'.", this);
        }
    }

    private void UpdateName(TowerDefinition towerDefinition)
    {
        if (nameText == null)
        {
            Debug.LogWarning("Pending tower item UI cannot update name: name text is not assigned.", this);
            return;
        }

        nameText.text = GetTowerName(towerDefinition);
    }

    private static string GetTowerName(TowerDefinition towerDefinition)
    {
        if (towerDefinition == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(towerDefinition.DisplayName))
        {
            return towerDefinition.DisplayName;
        }

        if (!string.IsNullOrEmpty(towerDefinition.TowerId))
        {
            return towerDefinition.TowerId;
        }

        return "Unnamed Tower";
    }
}
