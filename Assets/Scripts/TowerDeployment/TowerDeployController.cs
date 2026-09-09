using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerDeployController : MonoBehaviour
{
    [SerializeField] private Transform deployedTowerRoot;
    [SerializeField] private MonsterManager monsterManager;

    public bool IsConfiguredFor(MonsterManager activeMonsterManager)
    {
        return activeMonsterManager != null &&
               monsterManager == activeMonsterManager &&
               deployedTowerRoot != null;
    }

    public void Initialize(MonsterManager monsterManager)
    {
        this.monsterManager = monsterManager;
    }

    internal bool TryPrepareTower(
        TowerPlacementCandidate candidate,
        TowerPlacementTopologyPlan topologyPlan,
        out TowerBehaviour preparedTower,
        out string failureReason)
    {
        preparedTower = null;

        if (candidate == null)
        {
            failureReason = "the Tower placement candidate is missing.";
            return false;
        }

        if (topologyPlan == null || topologyPlan.Footprint.Count == 0)
        {
            failureReason = "the Tower placement topology plan is invalid.";
            return false;
        }

        if (deployedTowerRoot == null)
        {
            failureReason = "the Deployed Tower Root is not assigned.";
            return false;
        }

        if (monsterManager == null ||
            !monsterManager.isActiveAndEnabled ||
            !monsterManager.IsBattleActive)
        {
            failureReason = "the active Monster Manager is unavailable.";
            return false;
        }

        TowerDefinition towerDefinition = candidate.Definition;

        if (towerDefinition == null || !towerDefinition.IsValid())
        {
            failureReason = "the Tower definition is invalid.";
            return false;
        }

        TowerLevelConfig initialLevelConfig = towerDefinition.GetLevelConfig(1);

        if (initialLevelConfig == null || !initialLevelConfig.IsValid())
        {
            failureReason = "the Level 1 Tower configuration is invalid.";
            return false;
        }

        List<GridNodeBehaviour> occupiedNodes =
            new List<GridNodeBehaviour>(topologyPlan.Footprint.Count);

        for (int i = 0; i < topologyPlan.Footprint.Count; i++)
        {
            occupiedNodes.Add(topologyPlan.Footprint[i]);
        }

        GameObject towerObject = null;

        try
        {
            towerObject = Instantiate(
                towerDefinition.TowerPrefab,
                deployedTowerRoot);

            if (towerObject == null)
            {
                failureReason = "Unity did not create the Tower runtime instance.";
                return false;
            }

            towerObject.transform.SetPositionAndRotation(
                candidate.Position,
                candidate.Rotation);
            towerObject.transform.localScale = candidate.LocalScale;

            if (!towerObject.TryGetComponent(out TowerInstance towerInstance))
            {
                towerInstance = towerObject.AddComponent<TowerInstance>();
            }

            towerInstance.Initialize(towerDefinition, occupiedNodes);

            if (!towerObject.TryGetComponent(out TowerBehaviour towerBehaviour))
            {
                towerBehaviour = towerObject.AddComponent<TowerBehaviour>();
            }

            towerBehaviour.Initialize(towerInstance);

            if (!towerBehaviour.RefreshTowerVisual() ||
                towerBehaviour.VisualController == null ||
                towerBehaviour.VisualController.CurrentTowerModelInstance == null)
            {
                return FailPreparation(
                    towerObject,
                    "the Tower Level 1 visual could not reach ready state.",
                    out failureReason);
            }

            if (!towerObject.TryGetComponent(
                    out TowerCombatBehaviour towerCombatBehaviour))
            {
                return FailPreparation(
                    towerObject,
                    "the validated root combat component is missing from the " +
                    "Tower runtime instance.",
                    out failureReason);
            }

            towerCombatBehaviour.Initialize(towerInstance, monsterManager);

            if (!towerCombatBehaviour.TryPrepareBattleActivation(
                    out string combatFailureReason))
            {
                return FailPreparation(
                    towerObject,
                    $"the Tower combat runtime is not ready: " +
                    combatFailureReason,
                    out failureReason);
            }

            preparedTower = towerBehaviour;
            failureReason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return FailPreparation(
                towerObject,
                $"Tower readiness threw {exception.GetType().Name}.",
                out failureReason);
        }
    }

    private bool FailPreparation(
        GameObject towerObject,
        string reason,
        out string failureReason)
    {
        if (towerObject != null)
        {
            towerObject.SetActive(false);
            Destroy(towerObject);
        }

        failureReason = reason;
        return false;
    }
}
