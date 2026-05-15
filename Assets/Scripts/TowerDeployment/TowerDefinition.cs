using UnityEngine;

[CreateAssetMenu(
    fileName = "TowerDefinition",
    menuName = "Tower Nexus/Tower Definition"
)]
public class TowerDefinition : ScriptableObject
{
    [SerializeField] private string towerId;
    [SerializeField] private string displayName;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject towerPrefab;

    public string TowerId => towerId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject TowerPrefab => towerPrefab;

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(towerId))
        {
            Debug.LogWarning("Tower definition is invalid: tower id is missing.", this);
            return false;
        }

        if (towerPrefab == null)
        {
            Debug.LogWarning($"Tower definition '{towerId}' is invalid: tower prefab is not assigned.", this);
            return false;
        }

        TowerAnchorSet anchorSet = towerPrefab.GetComponent<TowerAnchorSet>();

        if (anchorSet == null)
        {
            Debug.LogWarning($"Tower definition '{towerId}' is invalid: tower prefab root is missing a TowerAnchorSet component.", towerPrefab);
            return false;
        }

        if (!anchorSet.IsValid())
        {
            Debug.LogWarning($"Tower definition '{towerId}' is invalid: tower prefab anchor set is invalid.", towerPrefab);
            return false;
        }

        return true;
    }
}
