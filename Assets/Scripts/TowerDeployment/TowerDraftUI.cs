using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerDraftUI : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Transform draftItemContainer;
    [SerializeField] private GameObject towerDraftItemPrefab;

    private readonly List<TowerDraftItemUI> draftItems = new List<TowerDraftItemUI>();
    private Action<TowerDefinition> onTowerSelected;

    public bool IsOpen => rootObject != null && rootObject.activeSelf;

    private void Awake()
    {
        CloseDraft();
    }

    public void OpenDraft(List<TowerDefinition> towerDefinitions)
    {
        OpenDraft(towerDefinitions, null);
    }

    public void OpenDraft(List<TowerDefinition> towerDefinitions, Action<TowerDefinition> onSelected)
    {
        ClearDraftItems();
        onTowerSelected = onSelected;

        if (rootObject == null)
        {
            Debug.LogWarning("Tower draft UI cannot open: root object is not assigned.", this);
            return;
        }

        if (draftItemContainer == null)
        {
            Debug.LogWarning("Tower draft UI cannot open: draft item container is not assigned.", this);
            return;
        }

        if (towerDraftItemPrefab == null)
        {
            Debug.LogWarning("Tower draft UI cannot open: tower draft item prefab is not assigned.", this);
            return;
        }

        if (towerDefinitions == null || towerDefinitions.Count == 0)
        {
            Debug.LogWarning("Tower draft UI cannot open: draft tower list is empty.", this);
            rootObject.SetActive(false);
            return;
        }

        int createdItemCount = 0;

        for (int i = 0; i < towerDefinitions.Count && createdItemCount < 3; i++)
        {
            TowerDefinition towerDefinition = towerDefinitions[i];

            if (towerDefinition == null)
            {
                Debug.LogWarning($"Tower draft UI skipped draft entry at index {i}: tower definition is null.", this);
                continue;
            }

            if (towerDefinition.TowerPrefab == null)
            {
                Debug.LogWarning($"Tower draft UI entry '{GetTowerName(towerDefinition)}' is missing a tower prefab reference.", towerDefinition);
            }

            GameObject itemObject = Instantiate(towerDraftItemPrefab, draftItemContainer);

            if (!itemObject.TryGetComponent(out TowerDraftItemUI item))
            {
                Debug.LogWarning("Tower draft UI skipped draft item: tower draft item prefab is missing TowerDraftItemUI.", itemObject);
                Destroy(itemObject);
                continue;
            }

            item.Initialize(towerDefinition, HandleTowerSelected);
            draftItems.Add(item);
            createdItemCount++;
        }

        if (createdItemCount == 0)
        {
            Debug.LogWarning("Tower draft UI cannot open: no valid draft items were generated.", this);
            rootObject.SetActive(false);
            return;
        }

        if (createdItemCount < 3)
        {
            Debug.LogWarning($"Tower draft UI opened with {createdItemCount} draft item(s). Expected 3.", this);
        }

        rootObject.SetActive(true);
    }

    public void CloseDraft()
    {
        ClearDraftItems();
        onTowerSelected = null;

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    private void HandleTowerSelected(TowerDefinition towerDefinition)
    {
        if (towerDefinition == null)
        {
            Debug.LogWarning("Tower draft UI cannot select tower: tower definition is null.", this);
            return;
        }

        onTowerSelected?.Invoke(towerDefinition);
        CloseDraft();
    }

    private void ClearDraftItems()
    {
        for (int i = draftItems.Count - 1; i >= 0; i--)
        {
            TowerDraftItemUI item = draftItems[i];

            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        draftItems.Clear();

        if (draftItemContainer == null)
        {
            return;
        }

        for (int i = draftItemContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(draftItemContainer.GetChild(i).gameObject);
        }
    }

    private static string GetTowerName(TowerDefinition towerDefinition)
    {
        if (towerDefinition == null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(towerDefinition.DisplayName) ? towerDefinition.TowerId : towerDefinition.DisplayName;
    }
}
