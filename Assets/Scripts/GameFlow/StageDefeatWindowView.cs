using System;
using UnityEngine;
using UnityEngine.UI;

public class StageDefeatWindowView : MonoBehaviour
{
    [SerializeField] private Graphic modalBlocker;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnToMainMenuButton;

    private bool isInteractionEnabled;

    public event Action RetryRequested;
    public event Action ReturnToMainMenuRequested;

    public bool IsInteractionEnabled => isInteractionEnabled;

    private void Awake()
    {
        SetInteractionEnabled(false);
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
        SetInteractionEnabled(false);
    }

    public bool TryValidateReferences(out string failureReason)
    {
        if (modalBlocker == null || !modalBlocker.raycastTarget)
        {
            failureReason =
                "Defeat Window requires a raycast-enabled modal blocker Graphic.";
            return false;
        }

        if (retryButton == null)
        {
            failureReason = "Retry Button is not assigned.";
            return false;
        }

        if (returnToMainMenuButton == null)
        {
            failureReason = "Return To Main Menu Button is not assigned.";
            return false;
        }

        failureReason = null;
        return true;
    }

    public void Show()
    {
        SetInteractionEnabled(false);
        gameObject.SetActive(true);
        SetInteractionEnabled(true);
    }

    public void HideAndLock()
    {
        SetInteractionEnabled(false);
        gameObject.SetActive(false);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        isInteractionEnabled = enabled && isActiveAndEnabled;

        if (retryButton != null)
        {
            retryButton.interactable = isInteractionEnabled;
        }

        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.interactable = isInteractionEnabled;
        }
    }

    private void BindButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked);
            retryButton.onClick.AddListener(HandleRetryClicked);
        }

        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.RemoveListener(
                HandleReturnToMainMenuClicked);
            returnToMainMenuButton.onClick.AddListener(
                HandleReturnToMainMenuClicked);
        }
    }

    private void UnbindButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.RemoveListener(
                HandleReturnToMainMenuClicked);
        }
    }

    private void HandleRetryClicked()
    {
        if (!isInteractionEnabled)
        {
            return;
        }

        RetryRequested?.Invoke();
    }

    private void HandleReturnToMainMenuClicked()
    {
        if (!isInteractionEnabled)
        {
            return;
        }

        ReturnToMainMenuRequested?.Invoke();
    }
}
