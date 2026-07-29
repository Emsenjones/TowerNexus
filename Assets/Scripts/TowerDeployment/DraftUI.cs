using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DraftUI : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Transform draftItemContainer;
    [SerializeField] private GameObject towerDraftItemPrefab;

    private readonly List<TowerContentUIItem> draftItems =
        new List<TowerContentUIItem>();
    private Action<DraftResult> onDraftSelected;
    private bool isBattleActive;

    public bool IsOpen => rootObject != null && rootObject.activeSelf;

    private void Awake()
    {
        if (rootObject != null && rootObject != gameObject)
        {
            rootObject.SetActive(false);
        }
    }

    public bool TryValidateReferences(out string failureReason)
    {
        if (rootObject == null)
        {
            failureReason = "Root Object is not assigned.";
            return false;
        }

        Graphic modalSurface = rootObject.GetComponent<Graphic>();

        if (modalSurface == null || !modalSurface.raycastTarget)
        {
            failureReason =
                "Root Object requires a raycast-enabled modal Graphic.";
            return false;
        }

        if (draftItemContainer == null)
        {
            failureReason = "Draft Item Container is not assigned.";
            return false;
        }

        if (towerDraftItemPrefab == null)
        {
            failureReason = "Tower Draft Item Prefab is not assigned.";
            return false;
        }

        TowerContentUIItem[] rootItems =
            towerDraftItemPrefab.GetComponents<TowerContentUIItem>();
        TowerContentUIItem[] allItems =
            towerDraftItemPrefab.GetComponentsInChildren<TowerContentUIItem>(
                true);

        if (rootItems.Length != 1 || allItems.Length != 1)
        {
            failureReason =
                "Tower Draft Item Prefab requires exactly one " +
                "TowerContentUIItem on its root.";
            return false;
        }

        if (!rootItems[0].TryValidateReferences(
                out string itemFailureReason))
        {
            failureReason =
                $"Tower Draft Item Prefab is invalid: {itemFailureReason}";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryOpenDraft(
        IReadOnlyList<DraftResult> draftResults,
        Action<DraftResult> onSelected,
        out string failureReason)
    {
        if (!isBattleActive)
        {
            failureReason = "the Draft UI battle gate is closed.";
            return false;
        }

        ClearDraftItems();
        onDraftSelected = null;

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }

        if (!TryValidateReferences(out failureReason))
        {
            return false;
        }

        if (onSelected == null)
        {
            failureReason = "the selection callback is missing.";
            return false;
        }

        if (draftResults == null || draftResults.Count == 0)
        {
            rootObject.SetActive(false);
            failureReason = "the Draft result list is empty.";
            return false;
        }

        onDraftSelected = onSelected;
        int createdItemCount = 0;

        for (int i = 0; i < draftResults.Count; i++)
        {
            DraftResult draftResult = draftResults[i];

            if (draftResult == null || !draftResult.IsValid)
            {
                return FailOpen(
                    $"Draft result at index {i} is invalid.",
                    out failureReason);
            }

            if (draftResult.ResultType == DraftResultType.TowerDraft &&
                draftResult.TowerDefinition != null &&
                draftResult.TowerDefinition.TowerPrefab == null)
            {
                Debug.LogWarning($"Draft UI entry '{draftResult.DisplayName}' is missing a tower prefab reference.", draftResult.TowerDefinition);
            }

            GameObject itemObject = null;

            try
            {
                itemObject = Instantiate(
                    towerDraftItemPrefab,
                    draftItemContainer);

                if (!itemObject.TryGetComponent(
                        out TowerContentUIItem item))
                {
                    Destroy(itemObject);
                    return FailOpen(
                        "Tower Draft Item Prefab is missing " +
                        "TowerContentUIItem.",
                        out failureReason);
                }

                if (!item.TryInitializeSelectable(
                        draftResult,
                        HandleDraftSelected))
                {
                    Destroy(itemObject);
                    return FailOpen(
                        $"Draft item {i} failed initialization.",
                        out failureReason);
                }

                draftItems.Add(item);
                createdItemCount++;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                if (itemObject != null)
                {
                    itemObject.SetActive(false);
                    Destroy(itemObject);
                }

                return FailOpen(
                    $"Draft item {i} creation threw " +
                    $"{exception.GetType().Name}.",
                    out failureReason);
            }
        }

        if (createdItemCount == 0)
        {
            return FailOpen(
                "no selectable Draft items were created.",
                out failureReason);
        }

        rootObject.SetActive(true);
        failureReason = string.Empty;
        return true;
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

    public void BeginBattle()
    {
        isBattleActive = true;
    }

    public void StopBattle()
    {
        isBattleActive = false;
        CloseDraft();
    }

    private void HandleDraftSelected(DraftResult draftResult)
    {
        if (!isBattleActive)
        {
            CloseDraft();
            return;
        }

        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Draft UI cannot select draft result: draft result is invalid.", this);
            return;
        }

        onDraftSelected?.Invoke(draftResult);
    }

    private bool FailOpen(
        string reason,
        out string failureReason)
    {
        CloseDraft();
        failureReason = reason;
        return false;
    }

    private void ClearDraftItems()
    {
        for (int i = draftItems.Count - 1; i >= 0; i--)
        {
            TowerContentUIItem uiItem = draftItems[i];

            if (uiItem != null)
            {
                Destroy(uiItem.gameObject);
            }
        }

        draftItems.Clear();
    }
}
