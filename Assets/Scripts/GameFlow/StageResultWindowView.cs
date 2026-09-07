using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageResultWindowView : MonoBehaviour
{
    private const string VictoryTitleText = "VICTORY";
    private const string VictoryDescriptionText =
        "All enemy waves have been defeated.";
    private const string DefeatTitleText = "DEFEAT";
    private const string DefeatDescriptionText =
        "The enemy broke through your defenses.";

    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    private bool isInteractionEnabled;

    public event Action NextStageRequested;
    public event Action RetryRequested;
    public event Action ReturnToMainMenuRequested;

    public bool IsInteractionEnabled => isInteractionEnabled;
    public bool IsVisible =>
        rootObject != null && rootObject.activeInHierarchy;

    private void Awake()
    {
        SetInteractionEnabled(false);

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
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
        if (rootObject == null)
        {
            failureReason = "Root Object is not assigned.";
            return false;
        }

        if (rootObject != gameObject &&
            !rootObject.transform.IsChildOf(transform))
        {
            failureReason =
                "Root Object must be the view object or one of its children.";
            return false;
        }

        if (titleText == null)
        {
            failureReason = "Title Text is not assigned.";
            return false;
        }

        if (!IsUnderRoot(titleText.transform))
        {
            failureReason = "Title Text must be under Root Object.";
            return false;
        }

        if (descriptionText == null)
        {
            failureReason = "Description Text is not assigned.";
            return false;
        }

        if (!IsUnderRoot(descriptionText.transform))
        {
            failureReason = "Description Text must be under Root Object.";
            return false;
        }

        if (nextStageButton == null)
        {
            failureReason = "Next Stage Button is not assigned.";
            return false;
        }

        if (!IsUnderRoot(nextStageButton.transform))
        {
            failureReason = "Next Stage Button must be under Root Object.";
            return false;
        }

        if (retryButton == null)
        {
            failureReason = "Retry Button is not assigned.";
            return false;
        }

        if (!IsUnderRoot(retryButton.transform))
        {
            failureReason = "Retry Button must be under Root Object.";
            return false;
        }

        if (mainMenuButton == null)
        {
            failureReason = "Main Menu Button is not assigned.";
            return false;
        }

        if (!IsUnderRoot(mainMenuButton.transform))
        {
            failureReason = "Main Menu Button must be under Root Object.";
            return false;
        }

        if (nextStageButton == retryButton ||
            nextStageButton == mainMenuButton ||
            retryButton == mainMenuButton)
        {
            failureReason =
                "Next Stage, Retry, and Main Menu require distinct Buttons.";
            return false;
        }

        failureReason = null;
        return true;
    }

    public void ShowVictory(bool hasNextStage)
    {
        SetInteractionEnabled(false);
        titleText.text = VictoryTitleText;
        descriptionText.text = VictoryDescriptionText;
        SetButtonVisibility(
            showNextStage: hasNextStage,
            showRetry: false,
            showMainMenu: !hasNextStage);
        rootObject.SetActive(true);
        SetInteractionEnabled(true);
    }

    public void ShowDefeat()
    {
        SetInteractionEnabled(false);
        titleText.text = DefeatTitleText;
        descriptionText.text = DefeatDescriptionText;
        SetButtonVisibility(
            showNextStage: false,
            showRetry: true,
            showMainMenu: true);
        rootObject.SetActive(true);
        SetInteractionEnabled(true);
    }

    public void HideAndLock()
    {
        SetInteractionEnabled(false);

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    public void SetInteractionEnabled(bool enabled)
    {
        isInteractionEnabled =
            enabled &&
            isActiveAndEnabled &&
            IsVisible;

        SetButtonInteraction(nextStageButton, isInteractionEnabled);
        SetButtonInteraction(retryButton, isInteractionEnabled);
        SetButtonInteraction(mainMenuButton, isInteractionEnabled);
    }

    private void SetButtonVisibility(
        bool showNextStage,
        bool showRetry,
        bool showMainMenu)
    {
        nextStageButton.gameObject.SetActive(showNextStage);
        retryButton.gameObject.SetActive(showRetry);
        mainMenuButton.gameObject.SetActive(showMainMenu);
    }

    private static void SetButtonInteraction(
        Button button,
        bool interactionEnabled)
    {
        if (button != null)
        {
            button.interactable =
                interactionEnabled && button.gameObject.activeSelf;
        }
    }

    private void BindButtons()
    {
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveListener(HandleNextStageClicked);
            nextStageButton.onClick.AddListener(HandleNextStageClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked);
            retryButton.onClick.AddListener(HandleRetryClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }
    }

    private void UnbindButtons()
    {
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveListener(HandleNextStageClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }
    }

    private void HandleNextStageClicked()
    {
        if (!CanAcceptButton(nextStageButton))
        {
            return;
        }

        NextStageRequested?.Invoke();
    }

    private void HandleRetryClicked()
    {
        if (!CanAcceptButton(retryButton))
        {
            return;
        }

        RetryRequested?.Invoke();
    }

    private void HandleMainMenuClicked()
    {
        if (!CanAcceptButton(mainMenuButton))
        {
            return;
        }

        ReturnToMainMenuRequested?.Invoke();
    }

    private bool CanAcceptButton(Button button)
    {
        return isInteractionEnabled &&
               button != null &&
               button.isActiveAndEnabled &&
               button.interactable;
    }

    private bool IsUnderRoot(Transform candidate)
    {
        Transform rootTransform = rootObject.transform;
        return candidate == rootTransform ||
               candidate.IsChildOf(rootTransform);
    }
}
