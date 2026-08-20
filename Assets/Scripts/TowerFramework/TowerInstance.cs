using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerInstance : MonoBehaviour
{
    private const int DefaultLevel = 1;

    private TowerDefinition towerDefinition;
    private int currentLevel = DefaultLevel;
    private readonly TowerUpgradeState upgradeState = new TowerUpgradeState();

    private List<GridNodeBehaviour> occupiedNodes = new List<GridNodeBehaviour>();

    public TowerDefinition TowerDefinition => towerDefinition;
    public int CurrentLevel => currentLevel;
    public TowerLevelConfig CurrentLevelConfig => towerDefinition != null ? towerDefinition.GetLevelConfig(currentLevel) : null;
    public IReadOnlyList<GridNodeBehaviour> OccupiedNodes => occupiedNodes;
    public IReadOnlyList<TowerUpgradeDefinition> AppliedUpgrades => upgradeState.AppliedUpgrades;

    public event Action<TowerInstance, TowerUpgradeDefinition> OnUpgradeRecorded;
    public event Action<TowerInstance, int, int> OnLevelChanged;

    public void Initialize(TowerDefinition towerDefinition, List<GridNodeBehaviour> occupiedNodes)
    {
        this.towerDefinition = towerDefinition;
        currentLevel = DefaultLevel;
        upgradeState.Reset();
        this.occupiedNodes.Clear();

        if (occupiedNodes == null)
        {
            return;
        }

        HashSet<GridNodeBehaviour> uniqueNodes = new HashSet<GridNodeBehaviour>();

        for (int i = 0; i < occupiedNodes.Count; i++)
        {
            GridNodeBehaviour node = occupiedNodes[i];

            if (node != null && uniqueNodes.Add(node))
            {
                this.occupiedNodes.Add(node);
            }
        }
    }

    public int GetMaxConfiguredLevel()
    {
        return towerDefinition != null
            ? towerDefinition.GetMaxConfiguredLevel()
            : 0;
    }

    internal bool CanCommitLevelConfig(TowerLevelConfig levelConfig)
    {
        return towerDefinition != null &&
               levelConfig != null &&
               levelConfig.IsValid() &&
               towerDefinition.GetLevelConfig(levelConfig.Level) == levelConfig;
    }

    internal bool TrySetPreviewLevel(int level)
    {
        TowerLevelConfig levelConfig = towerDefinition != null
            ? towerDefinition.GetLevelConfig(level)
            : null;

        if (!CanCommitLevelConfig(levelConfig))
        {
            return false;
        }

        currentLevel = level;
        return true;
    }

    internal void CommitPreparedLevel(TowerLevelConfig levelConfig)
    {
        currentLevel = levelConfig.Level;
    }

    internal void PublishLevelChangedSafely(int previousLevel, int currentLevel)
    {
        Action<TowerInstance, int, int> handlers = OnLevelChanged;

        if (handlers == null)
        {
            return;
        }

        Delegate[] subscribers = handlers.GetInvocationList();

        for (int i = 0; i < subscribers.Length; i++)
        {
            try
            {
                ((Action<TowerInstance, int, int>)subscribers[i]).Invoke(
                    this,
                    previousLevel,
                    currentLevel);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }

    public IReadOnlyList<GridNodeBehaviour> GetOccupiedNodes()
    {
        return occupiedNodes;
    }

    public bool HasUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        return upgradeState.HasUpgrade(upgradeDefinition);
    }

    public bool HasBehaviourPackage(TowerBehaviourPackageType packageType)
    {
        return upgradeState.HasBehaviourPackage(packageType);
    }

    public bool HasElementalUpgrade()
    {
        return upgradeState.HasElementalUpgrade();
    }

    public bool TryGetElementalUpgrade(out TowerUpgradeDefinition upgradeDefinition)
    {
        return upgradeState.TryGetElementalUpgrade(out upgradeDefinition);
    }

    public bool TryGetBehaviourPackageUpgrade(
        TowerBehaviourPackageType packageType,
        out TowerUpgradeDefinition upgradeDefinition)
    {
        return upgradeState.TryGetBehaviourPackageUpgrade(packageType, out upgradeDefinition);
    }

    public IReadOnlyList<TowerBehaviourPackageType> GetActiveBehaviourPackageTypes()
    {
        return upgradeState.GetActiveBehaviourPackageTypes();
    }

    public bool TryRecordUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        if (!upgradeState.TryRecordUpgrade(upgradeDefinition))
        {
            return false;
        }

        OnUpgradeRecorded?.Invoke(this, upgradeDefinition);
        return true;
    }
}
