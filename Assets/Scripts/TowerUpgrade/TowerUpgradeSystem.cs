using UnityEngine;

public class TowerUpgradeSystem : MonoBehaviour
{
    private const int CurrentMaxTowerLevel = 3;

    public bool CanLevelUpTower(
        TowerInstance targetTower,
        TowerDefinition draftTowerDefinition,
        out int nextLevel)
    {
        nextLevel = 0;

        if (targetTower == null || draftTowerDefinition == null)
        {
            return false;
        }

        TowerDefinition targetDefinition = targetTower.TowerDefinition;

        if (targetDefinition == null)
        {
            return false;
        }

        if (targetDefinition.TowerFamily != draftTowerDefinition.TowerFamily)
        {
            return false;
        }

        int candidateNextLevel = targetTower.CurrentLevel + 1;
        int maxAllowedLevel = Mathf.Min(targetTower.GetMaxConfiguredLevel(), CurrentMaxTowerLevel);

        if (maxAllowedLevel <= 0 ||
            candidateNextLevel > maxAllowedLevel ||
            !targetTower.CanSetLevel(candidateNextLevel))
        {
            return false;
        }

        nextLevel = candidateNextLevel;
        return true;
    }

    public bool TryLevelUpTower(
        TowerInstance targetTower,
        TowerDefinition draftTowerDefinition,
        out int nextLevel)
    {
        if (!CanLevelUpTower(targetTower, draftTowerDefinition, out nextLevel))
        {
            return false;
        }

        return targetTower.TrySetLevel(nextLevel);
    }
}
