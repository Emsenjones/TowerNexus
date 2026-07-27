using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private PlayerSystem playerSystem;

    private readonly List<MonsterBehaviour> aliveMonsters = new List<MonsterBehaviour>();
    private bool isBattleActive;

    public bool IsBattleActive => isBattleActive;
    public int AliveMonsterCount => aliveMonsters.Count;

    public event Action OnMonsterResolutionCompleted;

    public IReadOnlyList<MonsterBehaviour> GetAliveMonsters()
    {
        return aliveMonsters;
    }

    public void BeginBattle()
    {
        isBattleActive = true;
    }

    public bool CanBeginBattle(out string failureReason)
    {
        if (pathfindingService == null)
        {
            failureReason = "A* pathfinding service is not assigned.";
            return false;
        }

        if (!pathfindingService.HasActiveMap)
        {
            failureReason = "A* pathfinding has no Active Map.";
            return false;
        }

        if (playerSystem == null)
        {
            failureReason = "Player System is not assigned.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void CloseBattleGate()
    {
        isBattleActive = false;
    }

    public void ForceCleanupAllMonsters()
    {
        List<MonsterBehaviour> snapshot = new List<MonsterBehaviour>(aliveMonsters);

        for (int i = 0; i < snapshot.Count; i++)
        {
            MonsterBehaviour monster = snapshot[i];

            if (monster != null)
            {
                monster.ForceCleanup();
            }
        }

        aliveMonsters.Clear();
    }

    public void StopBattle()
    {
        CloseBattleGate();
        ForceCleanupAllMonsters();
    }

    public bool RegisterMonster(MonsterBehaviour monster)
    {
        if (!isBattleActive || monster == null || aliveMonsters.Contains(monster))
        {
            return false;
        }

        aliveMonsters.Add(monster);
        monster.OnResolved += HandleMonsterResolved;
        return true;
    }

    public void UnregisterMonster(MonsterBehaviour monster)
    {
        if (monster == null)
        {
            return;
        }

        monster.OnResolved -= HandleMonsterResolved;
        aliveMonsters.Remove(monster);
    }

    public void RecalculateAllMonsterPaths()
    {
        if (!isBattleActive)
        {
            return;
        }

        for (int i = aliveMonsters.Count - 1; i >= 0; i--)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster == null)
            {
                aliveMonsters.RemoveAt(i);
                continue;
            }

            if (monster.IsDead())
            {
                UnregisterMonster(monster);
                continue;
            }

            RequestPathRecalculation(monster);
        }
    }

    public void RequestPathRecalculation(MonsterBehaviour monster)
    {
        if (!isBattleActive || monster == null || monster.IsDead())
        {
            return;
        }

        if (!aliveMonsters.Contains(monster))
        {
            return;
        }

        if (pathfindingService == null)
        {
            Debug.LogWarning("Monster manager cannot recalculate path: pathfinding service is not assigned.", this);
            monster.StopMovement();
            return;
        }

        GridNodeBehaviour currentNode = monster.GetCurrentNode();
        GridNodeBehaviour targetNode = monster.GetTargetNode();

        if (currentNode == null || targetNode == null)
        {
            monster.StopMovement();
            return;
        }

        List<GridNodeBehaviour> path = pathfindingService.FindPath(currentNode, targetNode);

        if (path == null || path.Count == 0)
        {
            monster.StopMovement();
            return;
        }

        monster.SetPath(path);
    }

    private void OnDisable()
    {
        isBattleActive = false;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster != null)
            {
                monster.OnResolved -= HandleMonsterResolved;
            }
        }

        aliveMonsters.Clear();
    }

    private void HandleMonsterResolved(MonsterBehaviour monster, bool reachedTarget)
    {
        UnregisterMonster(monster);

        if (!isBattleActive || playerSystem == null)
        {
            return;
        }

        if (playerSystem.TryResolveMonster(reachedTarget))
        {
            OnMonsterResolutionCompleted?.Invoke();
        }
    }
}
