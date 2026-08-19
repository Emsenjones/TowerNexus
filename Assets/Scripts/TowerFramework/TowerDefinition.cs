using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "TowerDefinition",
    menuName = "Tower Nexus/Tower Definition"
)]
public class TowerDefinition : ScriptableObject
{
    [TitleGroup("Identity")]
    [FormerlySerializedAs("towerCategory")]
    [SerializeField] private TowerFamily towerFamily;
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

    [TitleGroup("Level Configuration")]
    [SerializeField] private List<TowerLevelConfig> towerLevelConfigs = new List<TowerLevelConfig>();

    public TowerFamily TowerFamily => towerFamily;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject TowerPrefab => towerPrefab;
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

    public int GetMaxConfiguredLevel()
    {
        if (towerLevelConfigs == null)
        {
            return 0;
        }

        int maxLevel = 0;

        for (int i = 0; i < towerLevelConfigs.Count; i++)
        {
            TowerLevelConfig levelConfig = towerLevelConfigs[i];

            if (levelConfig != null && levelConfig.Level > maxLevel)
            {
                maxLevel = levelConfig.Level;
            }
        }

        return maxLevel;
    }

    public bool IsValid()
    {
        if (towerPrefab == null)
        {
            Debug.LogWarning($"Tower definition '{GetDebugName()}' is invalid: tower prefab is not assigned.", this);
            return false;
        }

        if (!TryGetCombatBehaviour(out _, out string combatFailureReason))
        {
            Debug.LogWarning(
                $"Tower definition '{GetDebugName()}' is invalid: {combatFailureReason}",
                this);
            return false;
        }

        TowerAnchorSet anchorSet = towerPrefab.GetComponent<TowerAnchorSet>();

        if (anchorSet == null)
        {
            Debug.LogWarning($"Tower definition '{GetDebugName()}' is invalid: tower prefab root is missing a TowerAnchorSet component.", towerPrefab);
            return false;
        }

        if (!anchorSet.IsValid())
        {
            Debug.LogWarning($"Tower definition '{GetDebugName()}' is invalid: tower prefab anchor set is invalid.", towerPrefab);
            return false;
        }

        if (!AreTowerLevelConfigsValid())
        {
            return false;
        }

        return true;
    }

    public bool TryGetCombatBehaviour(
        out TowerCombatBehaviour combatBehaviour,
        out string failureReason)
    {
        combatBehaviour = null;
        failureReason = string.Empty;

        if (towerPrefab == null)
        {
            failureReason = "tower prefab is not assigned.";
            return false;
        }

        TowerCombatBehaviour[] rootCombatBehaviours =
            towerPrefab.GetComponents<TowerCombatBehaviour>();
        TowerCombatBehaviour[] allCombatBehaviours =
            towerPrefab.GetComponentsInChildren<TowerCombatBehaviour>(true);

        if (rootCombatBehaviours.Length == 0)
        {
            failureReason = allCombatBehaviours.Length > 0
                ? "combat component must be on the Tower Base Prefab root, not a child."
                : "Tower Base Prefab root is missing a concrete combat component.";
            return false;
        }

        if (rootCombatBehaviours.Length != 1 || allCombatBehaviours.Length != 1)
        {
            failureReason = "Tower Base Prefab must contain exactly one combat component on its root.";
            return false;
        }

        combatBehaviour = rootCombatBehaviours[0];

        if (combatBehaviour == null)
        {
            failureReason = "Tower Base Prefab combat component is missing its script.";
            return false;
        }

        if (combatBehaviour.SupportedTowerFamily != towerFamily)
        {
            failureReason =
                $"combat component '{combatBehaviour.GetType().Name}' supports " +
                $"'{combatBehaviour.SupportedTowerFamily}', not '{towerFamily}'.";
            combatBehaviour = null;
            return false;
        }

        if (!combatBehaviour.IsAuthoredConfigurationValid())
        {
            failureReason =
                $"combat component '{combatBehaviour.GetType().Name}' has invalid authored data.";
            combatBehaviour = null;
            return false;
        }

        return true;
    }

    private bool AreTowerLevelConfigsValid()
    {
        if (towerLevelConfigs == null || towerLevelConfigs.Count == 0)
        {
            Debug.LogWarning(
                $"Tower definition '{GetDebugName()}' is invalid: at least " +
                "one tower level configuration is required.",
                this);
            return false;
        }

        HashSet<int> configuredLevels = new HashSet<int>();
        bool hasInitialLevel = false;

        for (int i = 0; i < towerLevelConfigs.Count; i++)
        {
            TowerLevelConfig levelConfig = towerLevelConfigs[i];

            if (levelConfig == null)
            {
                Debug.LogWarning($"Tower definition '{GetDebugName()}' is invalid: tower level config at index {i} is missing.", this);
                return false;
            }

            if (!levelConfig.IsValid())
            {
                Debug.LogWarning(
                    $"Tower definition '{GetDebugName()}' is invalid: tower " +
                    $"level configuration at index {i} is invalid.",
                    this);
                return false;
            }

            if (!configuredLevels.Add(levelConfig.Level))
            {
                Debug.LogWarning($"Tower definition '{GetDebugName()}' is invalid: duplicate tower level config for level {levelConfig.Level}.", this);
                return false;
            }

            if (levelConfig.Level == 1)
            {
                hasInitialLevel = true;
            }
        }

        if (!hasInitialLevel)
        {
            Debug.LogWarning(
                $"Tower definition '{GetDebugName()}' is invalid: a Level 1 " +
                "tower configuration is required for deployment.",
                this);
            return false;
        }

        return true;
    }

    private string GetDebugName()
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        return name;
    }
}
