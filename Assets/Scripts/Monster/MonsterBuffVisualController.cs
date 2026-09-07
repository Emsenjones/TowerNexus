using System.Collections.Generic;
using UnityEngine;

public class MonsterBuffVisualController : MonoBehaviour
{
    private readonly Dictionary<BuffDefinition, GameObject> persistentVfxInstances = new Dictionary<BuffDefinition, GameObject>();
    private readonly HashSet<BuffDefinition> activeDefinitions = new HashSet<BuffDefinition>();
    private readonly List<BuffDefinition> definitionsToRemove = new List<BuffDefinition>();

    private MonsterBehaviour boundMonster;

    public void Initialize(MonsterBehaviour monster)
    {
        if (boundMonster == monster)
        {
            RefreshPersistentVfx();
            return;
        }

        CleanupSubscriptions();
        ClearPersistentVfx();

        boundMonster = monster;

        if (boundMonster == null)
        {
            return;
        }

        boundMonster.OnBuffStateChanged += HandleBuffStateChanged;
        boundMonster.OnDied += HandleMonsterRemoved;
        boundMonster.OnTargetReached += HandleMonsterRemoved;
        boundMonster.OnDestroyed += HandleMonsterRemoved;
        RefreshPersistentVfx();
    }

    private void OnDestroy()
    {
        CleanupSubscriptions();
        ClearPersistentVfx();
    }

    private void HandleBuffStateChanged(MonsterBehaviour monster)
    {
        RefreshPersistentVfx();
    }

    private void HandleMonsterRemoved(MonsterBehaviour monster)
    {
        ClearPersistentVfx();
    }

    private void RefreshPersistentVfx()
    {
        activeDefinitions.Clear();

        if (boundMonster != null)
        {
            IReadOnlyList<MonsterBuffStateSnapshot> snapshots = boundMonster.ActiveBuffSnapshots;

            for (int i = 0; i < snapshots.Count; i++)
            {
                BuffDefinition definition = snapshots[i].Definition;

                if (definition == null)
                {
                    continue;
                }

                activeDefinitions.Add(definition);

                if (definition.PersistentBuffVfxPrefab != null)
                {
                    EnsurePersistentVfx(definition);
                }
                else
                {
                    RemovePersistentVfx(definition);
                }
            }
        }

        definitionsToRemove.Clear();

        foreach (KeyValuePair<BuffDefinition, GameObject> pair in persistentVfxInstances)
        {
            if (!activeDefinitions.Contains(pair.Key))
            {
                definitionsToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < definitionsToRemove.Count; i++)
        {
            RemovePersistentVfx(definitionsToRemove[i]);
        }
    }

    private void EnsurePersistentVfx(BuffDefinition definition)
    {
        if (persistentVfxInstances.TryGetValue(definition, out GameObject existingInstance) && existingInstance != null)
        {
            return;
        }

        Transform anchor = boundMonster != null ? boundMonster.HitAnchor : null;

        if (anchor == null)
        {
            return;
        }

        GameObject instance = Instantiate(
            definition.PersistentBuffVfxPrefab,
            anchor.position,
            Quaternion.identity,
            anchor);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        persistentVfxInstances[definition] = instance;
    }

    private void RemovePersistentVfx(BuffDefinition definition)
    {
        if (!persistentVfxInstances.TryGetValue(definition, out GameObject instance))
        {
            return;
        }

        persistentVfxInstances.Remove(definition);

        if (instance != null)
        {
            Destroy(instance);
        }
    }

    private void ClearPersistentVfx()
    {
        foreach (KeyValuePair<BuffDefinition, GameObject> pair in persistentVfxInstances)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value);
            }
        }

        persistentVfxInstances.Clear();
        activeDefinitions.Clear();
        definitionsToRemove.Clear();
    }

    private void CleanupSubscriptions()
    {
        if (boundMonster == null)
        {
            return;
        }

        boundMonster.OnBuffStateChanged -= HandleBuffStateChanged;
        boundMonster.OnDied -= HandleMonsterRemoved;
        boundMonster.OnTargetReached -= HandleMonsterRemoved;
        boundMonster.OnDestroyed -= HandleMonsterRemoved;
        boundMonster = null;
    }
}
