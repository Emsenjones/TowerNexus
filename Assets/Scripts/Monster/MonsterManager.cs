using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private AStarPathfindingService pathfindingService;

    private readonly List<MonsterBehaviour> aliveMonsters = new List<MonsterBehaviour>();

    public IReadOnlyList<MonsterBehaviour> GetAliveMonsters()
    {
        return aliveMonsters;
    }

    public void RegisterMonster(MonsterBehaviour monster)
    {
        if (monster == null || aliveMonsters.Contains(monster))
        {
            return;
        }

        aliveMonsters.Add(monster);
        monster.OnTargetReached += HandleMonsterTargetReached;
        monster.OnDied += HandleMonsterDied;
    }

    public void UnregisterMonster(MonsterBehaviour monster)
    {
        if (monster == null)
        {
            return;
        }

        monster.OnTargetReached -= HandleMonsterTargetReached;
        monster.OnDied -= HandleMonsterDied;
        aliveMonsters.Remove(monster);
    }

    public void RecalculateAllMonsterPaths()
    {
        for (int i = aliveMonsters.Count - 1; i >= 0; i--)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster == null || monster.IsDead())
            {
                aliveMonsters.RemoveAt(i);
                continue;
            }

            RequestPathRecalculation(monster);
        }
    }

    public void RequestPathRecalculation(MonsterBehaviour monster)
    {
        if (monster == null || monster.IsDead())
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
        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster != null)
            {
                monster.OnTargetReached -= HandleMonsterTargetReached;
                monster.OnDied -= HandleMonsterDied;
            }
        }

        aliveMonsters.Clear();
    }

    private void HandleMonsterTargetReached(MonsterBehaviour monster)
    {
        UnregisterMonster(monster);
    }

    private void HandleMonsterDied(MonsterBehaviour monster)
    {
        UnregisterMonster(monster);
    }
}
