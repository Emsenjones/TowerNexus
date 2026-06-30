using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PendingDraftUI : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    private DraftResult draftResult;
    private TowerPlacementController placementController;

    public DraftResult DraftResult => draftResult;
    public TowerDefinition TowerDefinition => draftResult != null ? draftResult.TowerDefinition : null;

    public void Initialize(TowerDefinition towerDefinition)
    {
        Initialize(DraftResult.CreateTowerDraft(towerDefinition), placementController);
    }

    public void Initialize(TowerDefinition towerDefinition, TowerPlacementController placementController)
    {
        Initialize(DraftResult.CreateTowerDraft(towerDefinition), placementController);
    }

    public void Initialize(DraftResult draftResult, TowerPlacementController placementController)
    {
        this.draftResult = draftResult;
        this.placementController = placementController;

        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Pending draft UI cannot initialize: draft result is invalid.", this);
            return;
        }

        UpdateIcon(draftResult.Icon);
        UpdateName(draftResult);
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
            Debug.LogWarning("Pending draft UI cannot begin drag: placement controller is not assigned.", this);
            return;
        }

        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Pending draft UI cannot begin drag: draft result is invalid.", this);
            return;
        }

        placementController.BeginDraftDrag(draftResult, this);
    }

    private void UpdateIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            Debug.LogWarning("Pending draft UI cannot update icon: icon image is not assigned.", this);
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        if (icon == null)
        {
            Debug.LogWarning($"Pending draft UI has no icon for '{GetDraftName(draftResult)}'.", this);
        }
    }

    private void UpdateName(DraftResult draftResult)
    {
        if (nameText == null)
        {
            Debug.LogWarning("Pending draft UI cannot update name: name text is not assigned.", this);
            return;
        }

        nameText.text = GetDraftName(draftResult);
    }

    private static string GetDraftName(DraftResult draftResult)
    {
        if (draftResult == null)
        {
            return string.Empty;
        }

        return draftResult.DisplayName;
    }
}
