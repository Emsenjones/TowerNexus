using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PendingDraftUIItem : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [Header("Icon Background")]
    [SerializeField] private Image iconBackgroundImage;
    [SerializeField] private Sprite towerDraftIconBackground;
    [SerializeField] private Sprite basicUpgradeIconBackground;
    [SerializeField] private Sprite behaviourUpgradeIconBackground;
    [SerializeField] private Sprite elementalUpgradeIconBackground;
    [SerializeField] private TMP_Text nameText;

    private DraftResult draftResult;
    private DraftAttemptToken draftAttemptToken;
    private TowerPlacementController placementController;
    private RectTransform rectTransform;
    private RectTransform pendingItemContainer;
    private RectTransform dragVisualRoot;
    private RectTransform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private bool originalParentUsesLayoutGroup;
    private bool hasStoredPendingPosition;
    private bool isDragVisualActive;
    private bool isBattleActive;
    private bool isConsumed;

    public DraftResult DraftResult => draftResult;
    public DraftAttemptToken DraftAttemptToken => draftAttemptToken;
    public TowerDefinition TowerDefinition => draftResult != null ? draftResult.TowerDefinition : null;
    public TowerUpgradeDefinition TowerUpgradeDefinition => draftResult != null ? draftResult.TowerUpgradeDefinition : null;
    public bool IsConsumed => isConsumed;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    private void OnDisable()
    {
        isBattleActive = false;
        RestorePendingPosition();
    }

    public bool TryValidateReferences(out string failureReason)
    {
        if (!(transform is RectTransform))
        {
            failureReason = "the item root must be a RectTransform.";
            return false;
        }

        if (iconImage == null)
        {
            failureReason = "Icon Image is not assigned.";
            return false;
        }

        if (iconBackgroundImage == null)
        {
            failureReason = "Icon Background Image is not assigned.";
            return false;
        }

        if (nameText == null)
        {
            failureReason = "Name Text is not assigned.";
            return false;
        }

        if (towerDraftIconBackground == null ||
            basicUpgradeIconBackground == null ||
            behaviourUpgradeIconBackground == null ||
            elementalUpgradeIconBackground == null)
        {
            failureReason =
                "all Pending Draft category background sprites must be assigned.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryInitialize(
        DraftResult selectedDraftResult,
        DraftAttemptToken selectedDraftAttemptToken,
        TowerPlacementController selectedPlacementController,
        RectTransform selectedPendingItemContainer,
        RectTransform selectedDragVisualRoot,
        out string failureReason)
    {
        draftResult = null;
        draftAttemptToken = default;
        placementController = null;
        pendingItemContainer = null;
        dragVisualRoot = null;
        isConsumed = false;

        if (!TryValidateReferences(out failureReason))
        {
            return false;
        }

        if (selectedDraftResult == null || !selectedDraftResult.IsValid)
        {
            failureReason = "the Draft result is invalid.";
            return false;
        }

        if (!selectedDraftAttemptToken.IsValid)
        {
            failureReason = "the Draft attempt token is invalid.";
            return false;
        }

        if (selectedPlacementController == null)
        {
            failureReason = "Tower Placement Controller is not assigned.";
            return false;
        }

        if (selectedPendingItemContainer == null ||
            selectedDragVisualRoot == null)
        {
            failureReason =
                "the Pending Item Container or Drag Visual Root is missing.";
            return false;
        }

        if (selectedPendingItemContainer == selectedDragVisualRoot)
        {
            failureReason =
                "the Pending Item Container and Drag Visual Root must be distinct.";
            return false;
        }

        if (transform.parent != selectedPendingItemContainer)
        {
            failureReason =
                "the Pending Draft Item was not instantiated directly under " +
                "the supplied Pending Item Container.";
            return false;
        }

        draftResult = selectedDraftResult;
        draftAttemptToken = selectedDraftAttemptToken;
        placementController = selectedPlacementController;
        pendingItemContainer = selectedPendingItemContainer;
        dragVisualRoot = selectedDragVisualRoot;
        UpdateIcon(draftResult.Icon);
        UpdateIconBackground(draftResult);
        UpdateName(draftResult);
        failureReason = string.Empty;
        return true;
    }

    public void BeginBattle()
    {
        if (!isConsumed)
        {
            isBattleActive = true;
        }
    }

    public void StopBattle()
    {
        isBattleActive = false;
        RestorePendingPosition();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract() || eventData == null)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
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

        if (ShouldShowDragVisual())
        {
            BeginDragVisual(eventData.position, eventData.pressEventCamera);
        }

        placementController.BeginDraftDrag(draftResult, this);

        if (!placementController.IsDragging)
        {
            RestorePendingPosition();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!ShouldShowDragVisual())
        {
            return;
        }

        BeginDragVisual(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!ShouldShowDragVisual())
        {
            return;
        }

        SetDragVisualScreenPosition(eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        RestorePendingPosition();
    }

    public void RestorePendingPosition()
    {
        if (isConsumed)
        {
            return;
        }

        if (!hasStoredPendingPosition ||
            originalParent == null ||
            rectTransform == null)
        {
            isDragVisualActive = false;
            return;
        }

        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1));

        if (!originalParentUsesLayoutGroup)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }

        rectTransform.localScale = originalLocalScale;

        LayoutRebuilder.MarkLayoutForRebuild(originalParent);

        hasStoredPendingPosition = false;
        isDragVisualActive = false;
    }

    internal void MarkConsumed()
    {
        isConsumed = true;
        isBattleActive = false;
    }

    private bool CanInteract()
    {
        return isBattleActive && !isConsumed &&
               (placementController == null || placementController.CanStartDraftInteraction);
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

    private void UpdateIconBackground(DraftResult draftResult)
    {
        if (iconBackgroundImage == null)
        {
            Debug.LogWarning("Pending draft UI cannot update icon background: icon background image is not assigned.", this);
            return;
        }

        Sprite background = GetIconBackground(draftResult);
        iconBackgroundImage.sprite = background;
        iconBackgroundImage.enabled = background != null;

        if (background == null)
        {
            Debug.LogWarning($"Pending draft UI has no icon background for '{GetDraftName(draftResult)}'.", this);
        }
    }

    private Sprite GetIconBackground(DraftResult draftResult)
    {
        switch (draftResult.ResultType)
        {
            case DraftResultType.TowerDraft:
                return towerDraftIconBackground;
            case DraftResultType.TowerUpgradeDraft:
                return GetUpgradeIconBackground(draftResult.TowerUpgradeDefinition);
            default:
                return null;
        }
    }

    private Sprite GetUpgradeIconBackground(TowerUpgradeDefinition upgradeDefinition)
    {
        if (upgradeDefinition == null)
        {
            return null;
        }

        switch (upgradeDefinition.UpgradeLayer)
        {
            case TowerUpgradeLayer.Basic:
                return basicUpgradeIconBackground;
            case TowerUpgradeLayer.Behaviour:
                return behaviourUpgradeIconBackground;
            case TowerUpgradeLayer.Elemental:
                return elementalUpgradeIconBackground;
            default:
                return null;
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

    private void BeginDragVisual(Vector2 screenPosition, Camera eventCamera)
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform == null)
        {
            return;
        }

        if (!hasStoredPendingPosition)
        {
            originalParent = transform.parent as RectTransform;

            if (originalParent == null ||
                originalParent != pendingItemContainer ||
                dragVisualRoot == null)
            {
                return;
            }

            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalLocalScale = rectTransform.localScale;
            originalParentUsesLayoutGroup =
                originalParent.GetComponent<LayoutGroup>() != null;
            hasStoredPendingPosition = true;
        }

        if (!isDragVisualActive)
        {
            transform.SetParent(dragVisualRoot, true);
            transform.SetAsLastSibling();
        }

        isDragVisualActive = true;

        if (!TrySetDragVisualScreenPosition(screenPosition, eventCamera))
        {
            RestorePendingPosition();
        }
    }

    private void SetDragVisualScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        TrySetDragVisualScreenPosition(screenPosition, eventCamera);
    }

    private bool TrySetDragVisualScreenPosition(
        Vector2 screenPosition,
        Camera eventCamera)
    {
        if (!isDragVisualActive ||
            rectTransform == null ||
            dragVisualRoot == null)
        {
            return false;
        }

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragVisualRoot,
                screenPosition,
                eventCamera,
                out Vector3 worldPoint))
        {
            rectTransform.position = worldPoint;
            return true;
        }

        return false;
    }

    private bool ShouldShowDragVisual()
    {
        return draftResult != null && draftResult.ResultType == DraftResultType.TowerUpgradeDraft;
    }
}
