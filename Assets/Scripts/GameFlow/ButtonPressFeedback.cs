using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonPressFeedback :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    [SerializeField] private RectTransform pressedContentRoot;
    [SerializeField] private Vector2 pressedOffset =
        new Vector2(0f, -4f);

    private Vector2 idleContentPosition;
    private bool hasIdleContentPosition;
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
        isPointerDown = false;
        ResetPressFeedback();
    }

    public bool TryValidateReferences(out string failureReason)
    {
        if (button == null)
        {
            failureReason = "Button is not assigned.";
            return false;
        }

        if (button.gameObject != gameObject)
        {
            failureReason =
                "Button must be on the same GameObject as its press feedback.";
            return false;
        }

        if (button.transition != Selectable.Transition.None)
        {
            failureReason =
                "Button Transition must be None so it does not compete with " +
                "the custom press feedback.";
            return false;
        }

        if (buttonImage == null)
        {
            failureReason = "Button Image is not assigned.";
            return false;
        }

        if (normalSprite == null || pressedSprite == null)
        {
            failureReason =
                "Normal and Pressed sprites must both be assigned.";
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
                "Pressed Content Root must be a child of the Button.";
            return false;
        }

        failureReason = null;
        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            !CanShowPressedState())
        {
            return;
        }

        isPointerDown = true;
        ApplyPressFeedback();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerDown = false;
        ResetPressFeedback();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPointerDown && CanShowPressedState())
        {
            ApplyPressFeedback();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPressFeedback();
    }

    private bool CanShowPressedState()
    {
        return isActiveAndEnabled &&
               button != null &&
               button.IsActive() &&
               button.IsInteractable();
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

        if (buttonImage != null &&
            pressedSprite != null)
        {
            buttonImage.sprite = pressedSprite;
        }

        if (pressedContentRoot == null ||
            !hasIdleContentPosition)
        {
            return;
        }

        pressedContentRoot.anchoredPosition =
            idleContentPosition + pressedOffset;
    }

    private void ResetPressFeedback()
    {
        if (buttonImage != null &&
            normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }

        if (pressedContentRoot == null ||
            !hasIdleContentPosition)
        {
            return;
        }

        pressedContentRoot.anchoredPosition = idleContentPosition;
    }
}
