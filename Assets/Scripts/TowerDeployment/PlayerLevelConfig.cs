using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerLevelConfig",
    menuName = "Tower Nexus/Player Level Config"
)]
public class PlayerLevelConfig : ScriptableObject
{
    [Tooltip("EXP required to level up from current level to next level.")]
    [SerializeField] private List<int> expRequiredPerLevel = new List<int>();

    public IReadOnlyList<int> ExpRequiredPerLevel => expRequiredPerLevel;

    public int GetRequiredExpForLevel(int currentLevel)
    {
        if (!TryGetRequiredExpForLevel(currentLevel, out int requiredExp))
        {
            return 0;
        }

        return requiredExp;
    }

    public bool TryGetRequiredExpForLevel(int currentLevel, out int requiredExp)
    {
        requiredExp = 0;

        if (currentLevel < 1)
        {
            Debug.LogWarning($"Invalid player level requested: {currentLevel}. Level must be 1 or higher.", this);
            return false;
        }

        if (expRequiredPerLevel == null || expRequiredPerLevel.Count == 0)
        {
            Debug.LogWarning("Player level config has no EXP requirements configured.", this);
            return false;
        }

        int requirementIndex = currentLevel - 1;

        if (requirementIndex >= expRequiredPerLevel.Count)
        {
            return false;
        }

        requiredExp = expRequiredPerLevel[requirementIndex];

        if (requiredExp <= 0)
        {
            Debug.LogWarning($"Invalid EXP requirement for level {currentLevel}: {requiredExp}. Requirement must be greater than 0.", this);
            requiredExp = 0;
            return false;
        }

        return true;
    }
}
