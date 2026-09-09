using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class DeployedTowerCollection
{
    private bool isStopping;
    private readonly List<TowerBehaviour> towers = new List<TowerBehaviour>();
    private readonly List<TowerInstance> instances = new List<TowerInstance>();
    internal DeployedTowerCollection()
    { Towers = towers.AsReadOnly(); Instances = instances.AsReadOnly(); }
    internal IReadOnlyList<TowerBehaviour> Towers { get; }
    internal IReadOnlyList<TowerInstance> Instances { get; }
    internal bool Contains(TowerInstance tower) => tower != null && instances.Contains(tower);
    internal TowerBehaviour Find(TowerInstance tower)
    {
        int index = instances.IndexOf(tower);
        return index < 0 ? null : towers[index];
    }
    internal void PrepareCapacity()
    {
        if (towers.Capacity <= towers.Count) towers.Capacity = towers.Count + 1;
        if (instances.Capacity <= instances.Count) instances.Capacity = instances.Count + 1;
    }
    internal void CommitAdd(TowerBehaviour tower)
    { towers.Add(tower); instances.Add(tower.TowerInstance); }
    internal void StopCombat()
    {
        // Stop callbacks may synchronously request release, so never iterate the live list.
        if (isStopping) return;
        isStopping = true;
        try { foreach (var tower in towers.ToArray()) StopTower(tower); }
        finally { isStopping = false; }
    }
    internal void Release()
    {
        var snapshot = towers.ToArray();
        towers.Clear(); instances.Clear();
        foreach (var tower in snapshot)
        {
            try { DestroyTower(tower); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
    private static void StopTower(TowerBehaviour tower)
    {
        try
        {
            if (tower != null && tower.TryGetComponent(out TowerCombatBehaviour combat)) combat.StopBattle();
        }
        catch (Exception exception) { Debug.LogException(exception); }
    }
    internal static void DestroyTower(TowerBehaviour tower)
    {
        if (tower == null) return;
        // A failed Stop/OnDisable must not prevent destruction or later snapshot entries.
        GameObject instance = tower.gameObject;
        StopTower(tower);
        try { if (instance != null) instance.SetActive(false); }
        catch (Exception exception) { Debug.LogException(exception); }
        try { if (instance != null) UnityEngine.Object.Destroy(instance); }
        catch (Exception exception) { Debug.LogException(exception); }
    }
}
