using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerContentUIItem :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [Header("Icon Background")]
    [SerializeField] private Image iconBackgroundImage;
    [FormerlySerializedAs("towerDraftIconBackground")]
    [SerializeField] private Sprite towerIconBackground;
    [SerializeField] private Sprite towerPressedIconBackground;
    [SerializeField] private Sprite basicUpgradeIconBackground;
    [SerializeField] private Sprite basicUpgradePressedIconBackground;
    [SerializeField] private Sprite behaviourUpgradeIconBackground;
    [SerializeField] private Sprite behaviourUpgradePressedIconBackground;
    [SerializeField] private Sprite elementalUpgradeIconBackground;
    [SerializeField] private Sprite elementalUpgradePressedIconBackground;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [Header("Press Feedback")]
    [SerializeField] private RectTransform pressedContentRoot;
    [SerializeField] private Vector2 pressedOffset =
        new Vector2(0f, -4f);

    private DraftResult content;
    private Action<DraftResult> onSelected;
    private Sprite idleBackground;
    private Sprite pressedBackground;
    private Vector2 idleContentPosition;
    private bool hasIdleContentPosition;
    private bool isSelectableMode;
    private bool isPointerDown;

    private void Awake()
    {
        CaptureIdleContentPosition();
        ResetPressFeedback();
    }

    private void OnEnable()
    {
        CaptureIdleContentPosition();
        ResetPressFeedback();
    }

    private void OnDisable()
    {
        ClearSelection();
        content = null;
    }

    public bool TryValidateReferences(out string failureReason)
    {
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

        if (!iconBackgroundImage.raycastTarget)
        {
            failureReason =
                "Icon Background Image must receive UI raycasts.";
            return false;
        }

        if (nameText == null)
        {
            failureReason = "Name Text is not assigned.";
            return false;
        }

        if (descriptionText == null)
        {
            failureReason = "Description Text is not assigned.";
            return false;
        }

        if (towerIconBackground == null ||
            towerPressedIconBackground == null ||
            basicUpgradeIconBackground == null ||
            basicUpgradePressedIconBackground == null ||
            behaviourUpgradeIconBackground == null ||
            behaviourUpgradePressedIconBackground == null ||
            elementalUpgradeIconBackground == null ||
            elementalUpgradePressedIconBackground == null)
        {
            failureReason =
                "Idle and pressed category background sprites must be assigned.";
            return false;
        }

        if (pressedContentRoot == null)
        {
            failureReason = "Pressed Content Root is not assigned.";
            return false;
        }

        if (pressedContentRoot == transform ||
            !pressedContentRoot.IsChildOf(transform))
        {
            failureReason =
                "Pressed Content Root must be a child of the item root.";
            return false;
        }

        failureReason = null;
        return true;
    }

    public bool TryInitializeReadOnly(DraftResult content)
    {
        return TryInitialize(content, null, false);
    }

    public bool TryInitializeSelectable(
        DraftResult content,
        Action<DraftResult> onSelected)
    {
        if (onSelected == null)
        {
            ClearSelection();
            content = null;
            Debug.LogWarning(
                "Selectable Tower content requires a selection callback.",
                this);
            return false;
        }

        return TryInitialize(content, onSelected, true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            !CanInteract())
        {
            return;
        }

        isPointerDown = true;
        ApplyPressFeedback();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerDown = false;
        ResetPressFeedback();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            !CanInteract())
        {
            return;
        }

        onSelected.Invoke(content);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPointerDown && CanInteract())
        {
            ApplyPressFeedback();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPressFeedback();
    }

    private bool TryInitialize(
        DraftResult content,
        Action<DraftResult> selectionCallback,
        bool isSelectable)
    {
        ClearSelection();
        this.content = null;

        if (!TryValidateReferences(out string failureReason))
        {
            Debug.LogError(
                $"Tower content item cannot initialize: {failureReason}",
                this);
            return false;
        }

        if (content == null || !content.IsValid)
        {
            Debug.LogWarning(
                "Tower content item cannot initialize: content is invalid.",
                this);
            return false;
        }

        this.content = content;
        UpdateIcon(content.Icon);
        UpdateIconBackground(content);
        nameText.text = content.DisplayName;
        descriptionText.text = content.Description;
        isSelectableMode = isSelectable;
        onSelected = selectionCallback;
        return true;
    }

    private void UpdateIcon(Sprite icon)
    {
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        if (icon == null)
        {
            Debug.LogWarning(
                $"Tower content item has no icon for '{GetContentName()}'.",
                this);
        }
    }

    private void UpdateIconBackground(DraftResult content)
    {
        GetIconBackgrounds(
            content,
            out idleBackground,
            out pressedBackground);

        iconBackgroundImage.sprite = idleBackground;
        iconBackgroundImage.enabled = idleBackground != null;

        if (idleBackground == null || pressedBackground == null)
        {
            Debug.LogWarning(
                $"Tower content item has incomplete icon backgrounds for " +
                $"'{GetContentName()}'.",
                this);
        }
    }

    private void GetIconBackgrounds(
        DraftResult content,
        out Sprite idleSprite,
        out Sprite pressedSprite)
    {
        switch (content.ResultType)
        {
            case DraftResultType.TowerDraft:
                idleSprite = towerIconBackground;
                pressedSprite = towerPressedIconBackground;
                return;
            case DraftResultType.TowerUpgradeDraft:
                GetUpgradeIconBackgrounds(
                    content.TowerUpgradeDefinition,
                    out idleSprite,
                    out pressedSprite);
                return;
            default:
                idleSprite = null;
                pressedSprite = null;
                return;
        }
    }

    private void GetUpgradeIconBackgrounds(
        TowerUpgradeDefinition upgradeDefinition,
        out Sprite idleSprite,
        out Sprite pressedSprite)
    {
        if (upgradeDefinition == null)
        {
            idleSprite = null;
            pressedSprite = null;
            return;
        }

        switch (upgradeDefinition.UpgradeLayer)
        {
            case TowerUpgradeLayer.Basic:
                idleSprite = basicUpgradeIconBackground;
                pressedSprite = basicUpgradePressedIconBackground;
                return;
            case TowerUpgradeLayer.Behaviour:
                idleSprite = behaviourUpgradeIconBackground;
                pressedSprite =
                    behaviourUpgradePressedIconBackground;
                return;
            case TowerUpgradeLayer.Elemental:
                idleSprite = elementalUpgradeIconBackground;
                pressedSprite =
                    elementalUpgradePressedIconBackground;
                return;
            default:
                idleSprite = null;
                pressedSprite = null;
                return;
        }
    }

    private bool CanInteract()
    {
        return isSelectableMode &&
               isActiveAndEnabled &&
               content != null &&
               content.IsValid &&
               onSelected != null;
    }

    private void ClearSelection()
    {
        isSelectableMode = false;
        isPointerDown = false;
        onSelected = null;
        ResetPressFeedback();
    }

    private string GetContentName()
    {
        return content != null ? content.DisplayName : string.Empty;
    }

    private void CaptureIdleContentPosition()
    {
        if (pressedContentRoot == null ||
            hasIdleContentPosition)
        {
            return;
        }

        idleContentPosition = pressedContentRoot.anchoredPosition;
        hasIdleContentPosition = true;
    }

    private void ApplyPressFeedback()
    {
        CaptureIdleContentPosition();

        if (pressedBackground != null)
        {
            iconBackgroundImage.sprite = pressedBackground;
        }

        if (!hasIdleContentPosition)
        {
            return;
        }

        pressedContentRoot.anchoredPosition =
            idleContentPosition + pressedOffset;
    }

    private void ResetPressFeedback()
    {
        if (iconBackgroundImage != null &&
            idleBackground != null)
        {
            iconBackgroundImage.sprite = idleBackground;
        }

        if (pressedContentRoot == null ||
            !hasIdleContentPosition)
        {
            return;
        }

        pressedContentRoot.anchoredPosition = idleContentPosition;
    }
}
