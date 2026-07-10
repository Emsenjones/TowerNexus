using System;
using System.Collections.Generic;
using UnityEngine;

public class DraftUI : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Transform draftItemContainer;
    [SerializeField] private GameObject towerDraftItemPrefab;

    private readonly List<TowerDraftItemUI> draftItems = new List<TowerDraftItemUI>();
    private Action<DraftResult> onDraftSelected;

    public bool IsOpen => rootObject != null && rootObject.activeSelf;

    private void Awake()
    {
        if (rootObject != null && rootObject != gameObject)
        {
            rootObject.SetActive(false);
        }
    }

    public void OpenDraft(List<DraftResult> draftResults)
    {
        OpenDraft(draftResults, null);
    }

    public void OpenDraft(List<DraftResult> draftResults, Action<DraftResult> onSelected)
    {
        ClearDraftItems();
        onDraftSelected = onSelected;

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

        if (draftResults == null || draftResults.Count == 0)
        {
            Debug.LogWarning("Draft UI cannot open: draft result list is empty.", this);
            rootObject.SetActive(false);
            return;
        }

        int createdItemCount = 0;

        for (int i = 0; i < draftResults.Count && createdItemCount < 3; i++)
        {
            DraftResult draftResult = draftResults[i];

            if (draftResult == null || !draftResult.IsValid)
            {
                Debug.LogWarning($"Draft UI skipped draft entry at index {i}: draft result is invalid.", this);
                continue;
            }

            if (draftResult.ResultType == DraftResultType.TowerDraft &&
                draftResult.TowerDefinition != null &&
                draftResult.TowerDefinition.TowerPrefab == null)
            {
                Debug.LogWarning($"Draft UI entry '{draftResult.DisplayName}' is missing a tower prefab reference.", draftResult.TowerDefinition);
            }

            GameObject itemObject = Instantiate(towerDraftItemPrefab, draftItemContainer);

            if (!itemObject.TryGetComponent(out TowerDraftItemUI item))
            {
                Debug.LogWarning("Tower draft UI skipped draft item: tower draft item prefab is missing TowerDraftItemUI.", itemObject);
                Destroy(itemObject);
                continue;
            }

            item.Initialize(draftResult, HandleDraftSelected);
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
        onDraftSelected = null;

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    private void HandleDraftSelected(DraftResult draftResult)
    {
        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Draft UI cannot select draft result: draft result is invalid.", this);
            return;
        }

        onDraftSelected?.Invoke(draftResult);
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
    }
}
