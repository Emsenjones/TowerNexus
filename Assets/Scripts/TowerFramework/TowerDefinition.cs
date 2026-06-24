using System.Collections.Generic;
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
    [TitleGroup("Identity")]
    [SerializeField] private TowerCategory towerCategory;

    [TitleGroup("Visuals")]
    [SerializeField] private Sprite icon;

    [TitleGroup("Prefab")]
    [Required]
    [SerializeField] private GameObject towerPrefab;

    [TitleGroup("Combat Configuration")]
    [Required]
    [SerializeField] private AttackConfig attackConfig;

    [TitleGroup("Level Configuration")]
    [SerializeField] private List<TowerLevelConfig> towerLevelConfigs = new List<TowerLevelConfig>();

    public string TowerId => towerId;
    public string DisplayName => displayName;
    public string Description => description;
    public TowerCategory TowerCategory => towerCategory;
    public Sprite Icon => icon;
    public GameObject TowerPrefab => towerPrefab;
    public AttackConfig AttackConfig => attackConfig;
    public IReadOnlyList<TowerLevelConfig> TowerLevelConfigs => towerLevelConfigs;

    public TowerLevelConfig GetLevelConfig(int level)
    {
        if (towerLevelConfigs == null)
        {
            return null;
        }

        for (int i = 0; i < towerLevelConfigs.Count; i++)
        {
            TowerLevelConfig levelConfig = towerLevelConfigs[i];

            if (levelConfig != null && levelConfig.Level == level)
            {
                return levelConfig;
            }
        }

        return null;
    }

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

        if (!AreTowerLevelConfigsValid())
        {
            return false;
        }

        return true;
    }

    private bool AreTowerLevelConfigsValid()
    {
        if (towerLevelConfigs == null)
        {
            return true;
        }

        HashSet<int> configuredLevels = new HashSet<int>();

        for (int i = 0; i < towerLevelConfigs.Count; i++)
        {
            TowerLevelConfig levelConfig = towerLevelConfigs[i];

            if (levelConfig == null)
            {
                Debug.LogWarning($"Tower definition '{towerId}' is invalid: tower level config at index {i} is missing.", this);
                return false;
            }

            if (!levelConfig.IsValid())
            {
                Debug.LogWarning($"Tower definition '{towerId}' is invalid: tower level config at index {i} has an invalid level.", this);
                return false;
            }

            if (!configuredLevels.Add(levelConfig.Level))
            {
                Debug.LogWarning($"Tower definition '{towerId}' is invalid: duplicate tower level config for level {levelConfig.Level}.", this);
                return false;
            }
        }

        return true;
    }
}
