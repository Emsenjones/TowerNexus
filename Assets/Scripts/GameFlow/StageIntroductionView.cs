using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageIntroductionView : MonoBehaviour
{
    [SerializeField] private Graphic modalBlocker;
    [SerializeField] private GameObject introducedTowerSectionRoot;
    [SerializeField] private Transform introducedTowerContainer;
    [SerializeField] private GameObject introducedUpgradeSectionRoot;
    [SerializeField] private Transform introducedUpgradeContainer;
    [SerializeField] private GameObject introductionItemPrefab;
    [SerializeField] private Button confirmButton;

    private readonly List<StageIntroductionUIItem> runtimeItems =
        new List<StageIntroductionUIItem>();
    private bool isInteractionEnabled;

    public event Action ConfirmRequested;

    public bool IsInteractionEnabled => isInteractionEnabled;

    private void Awake()
    {
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
        if (modalBlocker == null || !modalBlocker.raycastTarget)
        {
            failureReason =
                "Stage Introduction requires a raycast-enabled modal blocker Graphic.";
            return false;
        }

        if (introducedTowerSectionRoot == null)
        {
            failureReason = "Introduced Tower Section Root is not assigned.";
            return false;
        }

        if (introducedTowerContainer == null)
        {
            failureReason = "Introduced Tower Container is not assigned.";
            return false;
        }

        if (introducedUpgradeSectionRoot == null)
        {
            failureReason = "Introduced Upgrade Section Root is not assigned.";
            return false;
        }

        if (introducedUpgradeContainer == null)
        {
            failureReason = "Introduced Upgrade Container is not assigned.";
            return false;
        }

        if (introductionItemPrefab == null)
        {
            failureReason = "Stage Introduction Item Prefab is not assigned.";
            return false;
        }

        if (!introductionItemPrefab.TryGetComponent(
                out StageIntroductionUIItem item))
        {
            failureReason =
                "Stage Introduction Item Prefab is missing StageIntroductionUIItem.";
            return false;
        }

        if (!item.TryValidateReferences(out string itemFailureReason))
        {
            failureReason =
                $"Stage Introduction Item Prefab is invalid: {itemFailureReason}";
            return false;
        }

        if (introductionItemPrefab
                .GetComponentInChildren<Button>(true) != null)
        {
            failureReason =
                "Stage Introduction Item Prefab must not contain Button behavior.";
            return false;
        }

        if (confirmButton == null)
        {
            failureReason = "Confirm Button is not assigned.";
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

        int towerItemCount = PopulateTowers(
            stageDefinition.IntroducedTowers);
        int upgradeItemCount = PopulateUpgrades(
            stageDefinition.IntroducedTowerUpgrades);

        introducedTowerSectionRoot.SetActive(towerItemCount > 0);
        introducedUpgradeSectionRoot.SetActive(upgradeItemCount > 0);

        if (towerItemCount == 0 && upgradeItemCount == 0)
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
        gameObject.SetActive(true);
        SetInteractionEnabled(true);
    }

    public void HideAndClear()
    {
        SetInteractionEnabled(false);

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        ClearRuntimeItems();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        isInteractionEnabled = enabled && isActiveAndEnabled;

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
                    introducedTowerContainer,
                    GetDisplayName(
                        towerDefinition.DisplayName,
                        towerDefinition.name),
                    towerDefinition.Description,
                    towerDefinition.Icon))
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
                    introducedUpgradeContainer,
                    GetDisplayName(
                        upgradeDefinition.DisplayName,
                        upgradeDefinition.name),
                    upgradeDefinition.Description,
                    upgradeDefinition.Icon))
            {
                createdItemCount++;
            }
        }

        return createdItemCount;
    }

    private bool TryCreateItem(
        Transform container,
        string displayName,
        string description,
        Sprite icon)
    {
        GameObject itemObject = Instantiate(
            introductionItemPrefab,
            container,
            false);

        if (!itemObject.TryGetComponent(
                out StageIntroductionUIItem item))
        {
            Debug.LogError(
                "Created Stage Introduction item is missing " +
                "StageIntroductionUIItem.",
                itemObject);
            itemObject.SetActive(false);
            Destroy(itemObject);
            return false;
        }

        if (!item.TryInitialize(displayName, description, icon))
        {
            itemObject.SetActive(false);
            Destroy(itemObject);
            return false;
        }

        itemObject.SetActive(true);
        runtimeItems.Add(item);
        return true;
    }

    private void ClearRuntimeItems()
    {
        for (int i = runtimeItems.Count - 1; i >= 0; i--)
        {
            StageIntroductionUIItem item = runtimeItems[i];

            if (item == null)
            {
                continue;
            }

            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }

        runtimeItems.Clear();
        ResetSectionVisibility();
    }

    private void ResetSectionVisibility()
    {
        introducedTowerSectionRoot?.SetActive(false);
        introducedUpgradeSectionRoot?.SetActive(false);
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

    private static string GetDisplayName(
        string authoredDisplayName,
        string assetName)
    {
        return string.IsNullOrEmpty(authoredDisplayName)
            ? assetName
            : authoredDisplayName;
    }

    private static string GetStageName(
        StageDefinition stageDefinition)
    {
        return GetDisplayName(
            stageDefinition.DisplayName,
            stageDefinition.name);
    }
}
