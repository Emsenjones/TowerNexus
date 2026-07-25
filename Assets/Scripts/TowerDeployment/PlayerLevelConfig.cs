using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "PlayerLevelConfig",
    menuName = "Tower Nexus/Player Level Config"
)]
public class PlayerLevelConfig : ScriptableObject
{
    [Tooltip("Resolved progress required to level up from the current level to the next level.")]
    [FormerlySerializedAs("expRequiredPerLevel")]
    [SerializeField] private List<int> progressRequiredPerLevel = new List<int>();

    public IReadOnlyList<int> ProgressRequiredPerLevel => progressRequiredPerLevel;

    public int GetRequiredProgressForLevel(int currentLevel)
    {
        if (!TryGetRequiredProgressForLevel(currentLevel, out int requiredProgress))
        {
            return 0;
        }

        return requiredProgress;
    }

    public bool TryGetRequiredProgressForLevel(int currentLevel, out int requiredProgress)
    {
        requiredProgress = 0;

        if (currentLevel < 1)
        {
            Debug.LogWarning($"Invalid player level requested: {currentLevel}. Level must be 1 or higher.", this);
            return false;
        }

        if (progressRequiredPerLevel == null || progressRequiredPerLevel.Count == 0)
        {
            Debug.LogWarning("Player level config has no progress requirements configured.", this);
            return false;
        }

        int requirementIndex = currentLevel - 1;

        if (requirementIndex >= progressRequiredPerLevel.Count)
        {
            return false;
        }

        requiredProgress = progressRequiredPerLevel[requirementIndex];

        if (requiredProgress <= 0)
        {
            Debug.LogWarning($"Invalid progress requirement for level {currentLevel}: {requiredProgress}. Requirement must be greater than 0.", this);
            requiredProgress = 0;
            return false;
        }

        return true;
    }
}
