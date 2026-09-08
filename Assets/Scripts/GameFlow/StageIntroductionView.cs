using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageIntroductionView : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Transform introducedItemContainer;
    [SerializeField] private TowerContentUIItem introductionItemPrefab;
    [SerializeField] private Button confirmButton;

    private readonly List<TowerContentUIItem> runtimeItems =
        new List<TowerContentUIItem>();
    private bool isInteractionEnabled;

    public event Action ConfirmRequested;

    public bool IsInteractionEnabled => isInteractionEnabled;
    public bool IsVisible =>
        rootObject != null && rootObject.activeInHierarchy;

    private void Awake()
    {
        // GameFlowUIRoot owns visibility; Awake may run during the first Show().
        SetInteractionEnabled(false);
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
        SetInteractionEnabled(false);
        ClearRuntimeItems();
    }

    private void OnDestroy()
    {
        ClearRuntimeItems();
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

        if (introducedItemContainer == null)
        {
            failureReason = "Introduced Item Container is not assigned.";
            return false;
        }

        if (!IsUnderRoot(introducedItemContainer))
        {
            failureReason =
                "Introduced Item Container must be under Root Object.";
            return false;
        }

        if (introductionItemPrefab == null)
        {
            failureReason = "Introduction Item Prefab is not assigned.";
            return false;
        }

        if (!introductionItemPrefab.TryValidateReferences(
                out string itemFailureReason))
        {
            failureReason =
                $"Introduction Item Prefab is invalid: {itemFailureReason}";
            return false;
        }

        if (confirmButton == null)
        {
            failureReason = "Confirm Button is not assigned.";
            return false;
        }

        if (!IsUnderRoot(confirmButton.transform))
        {
            failureReason = "Confirm Button must be under Root Object.";
            return false;
        }

        if (!confirmButton.TryGetComponent(
                out ButtonPressFeedback pressFeedback))
        {
            failureReason =
                "Confirm Button is missing ButtonPressFeedback.";
            return false;
        }

        if (!pressFeedback.TryValidateReferences(
                out string feedbackFailureReason))
        {
            failureReason =
                $"Confirm Button press feedback is invalid: " +
                feedbackFailureReason;
            return false;
        }

        failureReason = null;
        return true;
    }

    public bool TryConfigure(StageDefinition stageDefinition)
    {
        SetInteractionEnabled(false);
        ClearRuntimeItems();

        if (stageDefinition == null)
        {
            Debug.LogError(
                "Stage Introduction cannot configure a null Stage Definition.",
                this);
            return false;
        }

        int createdItemCount =
            PopulateTowers(stageDefinition.IntroducedTowers) +
            PopulateUpgrades(stageDefinition.IntroducedTowerUpgrades);

        if (createdItemCount == 0)
        {
            Debug.LogError(
                $"Stage Introduction for '{GetStageName(stageDefinition)}' " +
                "contains no presentable items.",
                this);
            return false;
        }

        return true;
    }

    public void Show()
    {
        rootObject.SetActive(true);
        SetInteractionEnabled(true);
    }

    public void HideAndClear()
    {
        SetInteractionEnabled(false);

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }

        ClearRuntimeItems();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        isInteractionEnabled =
            enabled &&
            isActiveAndEnabled &&
            IsVisible;

        if (confirmButton != null)
        {
            confirmButton.interactable = isInteractionEnabled;
        }
    }

    private int PopulateTowers(
        IReadOnlyList<TowerDefinition> towerDefinitions)
    {
        if (towerDefinitions == null)
        {
            return 0;
        }

        int createdItemCount = 0;

        for (int i = 0; i < towerDefinitions.Count; i++)
        {
            TowerDefinition towerDefinition = towerDefinitions[i];

            if (towerDefinition == null)
            {
                Debug.LogWarning(
                    $"Stage Introduction skipped null Tower entry {i}.",
                    this);
                continue;
            }

            if (TryCreateItem(
                    DraftResult.CreateTowerDraft(towerDefinition)))
            {
                createdItemCount++;
            }
        }

        return createdItemCount;
    }

    private int PopulateUpgrades(
        IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions)
    {
        if (upgradeDefinitions == null)
        {
            return 0;
        }

        int createdItemCount = 0;

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition =
                upgradeDefinitions[i];

            if (upgradeDefinition == null)
            {
                Debug.LogWarning(
                    $"Stage Introduction skipped null Tower Upgrade entry {i}.",
                    this);
                continue;
            }

            if (TryCreateItem(
                    DraftResult.CreateTowerUpgradeDraft(upgradeDefinition)))
            {
                createdItemCount++;
            }
        }

        return createdItemCount;
    }

    private bool TryCreateItem(DraftResult content)
    {
        TowerContentUIItem item = Instantiate(
            introductionItemPrefab,
            introducedItemContainer,
            false);

        if (!item.TryInitializeReadOnly(content))
        {
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
            return false;
        }

        item.gameObject.SetActive(true);
        runtimeItems.Add(item);
        return true;
    }

    private void ClearRuntimeItems()
    {
        for (int i = runtimeItems.Count - 1; i >= 0; i--)
        {
            TowerContentUIItem item = runtimeItems[i];

            if (item == null)
            {
                continue;
            }

            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }

        runtimeItems.Clear();
    }

    private void BindButton()
    {
        if (confirmButton == null)
        {
            return;
        }

        confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        confirmButton.onClick.AddListener(HandleConfirmClicked);
    }

    private void UnbindButton()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }
    }

    private void HandleConfirmClicked()
    {
        if (!isInteractionEnabled)
        {
            return;
        }

        ConfirmRequested?.Invoke();
    }

    private static string GetStageName(
        StageDefinition stageDefinition)
    {
        return string.IsNullOrEmpty(stageDefinition.DisplayName)
            ? stageDefinition.name
            : stageDefinition.DisplayName;
    }

    private bool IsUnderRoot(Transform candidate)
    {
        Transform rootTransform = rootObject.transform;
        return candidate == rootTransform ||
               candidate.IsChildOf(rootTransform);
    }
}
