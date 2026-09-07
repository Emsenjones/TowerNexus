using System.Collections.Generic;
using UnityEngine;

public class MonsterStatusUIManager : MonoBehaviour
{
    [SerializeField] private MonsterStatusUIItem statusUIItemPrefab;

    private readonly List<MonsterStatusUIItem> createdStatusItems = new List<MonsterStatusUIItem>();

    private Camera worldCamera;
    private RectTransform itemContainer;

    private void Awake()
    {
        itemContainer = transform as RectTransform;
    }

    private void OnDestroy()
    {
        ClearCreatedStatusItems();
    }

    public MonsterStatusUIItem CreateStatusUi(MonsterBehaviour monster, Vector3 offset)
    {
        PruneDestroyedStatusItems();

        if (monster == null)
        {
            return null;
        }

        if (statusUIItemPrefab == null)
        {
            Debug.LogWarning("Monster status UI manager cannot create status UI: status UI item prefab is not assigned.", this);
            return null;
        }

        if (!TryGetItemContainer(out RectTransform parent))
        {
            return null;
        }

        MonsterStatusUIItem statusUIItem = Instantiate(statusUIItemPrefab, parent);

        if (statusUIItem == null)
        {
            Debug.LogWarning("Monster status UI manager cannot create status UI: prefab is missing MonsterStatusUIItem.", this);
            return null;
        }

        createdStatusItems.Add(statusUIItem);
        statusUIItem.Initialize(monster, offset, ResolveWorldCamera());
        return statusUIItem;
    }

    private bool TryGetItemContainer(out RectTransform parent)
    {
        parent = itemContainer != null ? itemContainer : transform as RectTransform;

        if (parent != null)
        {
            itemContainer = parent;
            return true;
        }

        Debug.LogWarning("Monster status UI manager must be attached to a RectTransform so it can own its runtime status UI items.", this);
        return false;
    }

    private Camera ResolveWorldCamera()
    {
        return worldCamera != null ? worldCamera : Camera.main;
    }

    private void ClearCreatedStatusItems()
    {
        for (int i = 0; i < createdStatusItems.Count; i++)
        {
            MonsterStatusUIItem statusUIItem = createdStatusItems[i];

            if (statusUIItem != null)
            {
                Destroy(statusUIItem.gameObject);
            }
        }

        createdStatusItems.Clear();
    }

    private void PruneDestroyedStatusItems()
    {
        for (int i = createdStatusItems.Count - 1; i >= 0; i--)
        {
            if (createdStatusItems[i] == null)
            {
                createdStatusItems.RemoveAt(i);
            }
        }
    }
}
