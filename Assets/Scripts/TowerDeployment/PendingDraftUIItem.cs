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
    private TowerPlacementController placementController;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private bool originalParentUsesLayoutGroup;
    private bool hasStoredPendingPosition;
    private bool isDragVisualActive;
    private bool isBattleActive;

    public DraftResult DraftResult => draftResult;
    public TowerDefinition TowerDefinition => draftResult != null ? draftResult.TowerDefinition : null;
    public TowerUpgradeDefinition TowerUpgradeDefinition => draftResult != null ? draftResult.TowerUpgradeDefinition : null;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
    }

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
        UpdateIconBackground(draftResult);
        UpdateName(draftResult);
    }

    public void SetPlacementController(TowerPlacementController placementController)
    {
        this.placementController = placementController;
    }

    public void BeginBattle()
    {
        isBattleActive = true;
    }

    public void StopBattle()
    {
        isBattleActive = false;
        RestorePendingPosition();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isBattleActive)
        {
            return;
        }

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
        if (!isBattleActive)
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
        if (!isBattleActive)
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
        RestorePendingPosition();
    }

    public void RestorePendingPosition()
    {
        if (!hasStoredPendingPosition || originalParent == null || rectTransform == null)
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

        if (originalParent is RectTransform originalParentRect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(originalParentRect);
        }

        hasStoredPendingPosition = false;
        isDragVisualActive = false;
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

        if (rootCanvas == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            rootCanvas = canvas != null ? canvas.rootCanvas : null;
        }

        if (!hasStoredPendingPosition)
        {
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalLocalScale = rectTransform.localScale;
            originalParentUsesLayoutGroup = originalParent != null && originalParent.GetComponent<LayoutGroup>() != null;
            hasStoredPendingPosition = true;
        }

        if (!isDragVisualActive && rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
        }

        isDragVisualActive = true;
        SetDragVisualScreenPosition(screenPosition, eventCamera);
    }

    private void SetDragVisualScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        if (!isDragVisualActive || rectTransform == null)
        {
            return;
        }

        RectTransform canvasRectTransform = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
        Camera dragCamera = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventCamera;

        if (canvasRectTransform != null &&
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRectTransform,
                screenPosition,
                dragCamera,
                out Vector3 worldPoint))
        {
            rectTransform.position = worldPoint;
            return;
        }

        rectTransform.position = screenPosition;
    }

    private bool ShouldShowDragVisual()
    {
        return draftResult != null && draftResult.ResultType == DraftResultType.TowerUpgradeDraft;
    }
}
