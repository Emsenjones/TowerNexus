using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TowerDefinition",
    menuName = "Tower Nexus/Tower Definition"
)]
public class TowerDefinition : ScriptableObject
{
    [TitleGroup("Identity")]
    [Required]
    [SerializeField] private string towerId;
    [TitleGroup("Identity")]
    [SerializeField] private string displayName;
    [TitleGroup("Identity")]
    [TextArea]
    [SerializeField] private string description;

    [TitleGroup("Visuals")]
    [SerializeField] private Sprite icon;

    [TitleGroup("Prefab")]
    [Required]
    [SerializeField] private GameObject towerPrefab;

    [TitleGroup("Combat Configuration")]
    [Required]
    [SerializeField] private AttackConfig attackConfig;

    public string TowerId => towerId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject TowerPrefab => towerPrefab;
    public AttackConfig AttackConfig => attackConfig;

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

        if (attackConfig == null)
        {
            Debug.LogWarning($"Tower definition '{towerId}' is invalid: attack config is not assigned.", this);
            return false;
        }

        if (!attackConfig.IsValid())
        {
            Debug.LogWarning($"Tower definition '{towerId}' is invalid: attack config is invalid.", attackConfig);
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
