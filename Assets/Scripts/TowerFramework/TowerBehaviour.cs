using UnityEngine;

[RequireComponent(typeof(TowerInstance))]
public class TowerBehaviour : MonoBehaviour
{
    [SerializeField] private TowerInstance towerInstance;
    [SerializeField] private TowerVisualController visualController;

    public TowerInstance TowerInstance => towerInstance;
    public TowerVisualController VisualController => visualController;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Initialize(TowerInstance towerInstance)
    {
        EnsureReferences();

        if (towerInstance != null)
        {
            this.towerInstance = towerInstance;
        }

        EnsureReferences();
    }

    public bool RefreshTowerVisual()
    {
        EnsureReferences();

        if (towerInstance == null)
        {
            Debug.LogWarning("Tower behaviour cannot refresh tower visual: TowerInstance is missing.", this);
            return false;
        }

        if (visualController == null)
        {
            Debug.LogWarning("Tower behaviour cannot refresh tower visual: TowerVisualController is missing.", this);
            return false;
        }

        TowerLevelConfig levelConfig = towerInstance.CurrentLevelConfig;

        if (levelConfig == null)
        {
            Debug.LogWarning($"Tower behaviour cannot refresh tower visual: level config for level {towerInstance.CurrentLevel} is missing.", this);
            return false;
        }

        if (!visualController.SetTowerVisual(levelConfig.TowerModelPrefab))
        {
            return false;
        }

        if (TryGetComponent(out TowerCombatBehaviour combatBehaviour))
        {
            combatBehaviour.OnTowerPresentationReplaced();
        }

        return true;
    }

    internal bool TryPrepareLevelVisualRefresh(
        TowerLevelConfig nextLevelConfig,
        out string failureReason)
    {
        if (towerInstance == null)
        {
            failureReason = "TowerInstance is missing.";
            return false;
        }

        if (visualController == null ||
            visualController.TowerPrefabSpawnPoint == null)
        {
            failureReason =
                "TowerVisualController or TowerPrefabSpawnPoint is missing.";
            return false;
        }

        if (nextLevelConfig == null || !nextLevelConfig.IsValid())
        {
            failureReason = "the next Tower Level configuration is invalid.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private void EnsureReferences()
    {
        if (towerInstance == null)
        {
            towerInstance = GetComponent<TowerInstance>();
        }

        if (visualController == null && !TryGetComponent(out visualController))
        {
            visualController = gameObject.AddComponent<TowerVisualController>();
        }
    }
}
