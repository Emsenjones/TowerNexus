using System;
using System.Collections.Generic;
using UnityEngine;

// One immutable Battle identity. Closing is permanent, including when the manager is reused.
public sealed class BattleCombatBinding
{
    private readonly MonsterManager manager;
    private readonly Action<string> reportFailure;
    private bool opened;
    private bool closed;
    private bool reportedFailure;
    private static readonly MonsterBehaviour[] Empty = new MonsterBehaviour[0];

    internal BattleCombatBinding(MonsterManager manager, Action<string> reportFailure)
    {
        this.manager = manager;
        this.reportFailure = reportFailure;
    }

    internal void Open() { if (!closed) opened = true; }
    internal void Close() { closed = true; }
    public bool IsUsable
    {
        get
        {
            if (!opened || closed) return false;
            if (manager != null && manager.isActiveAndEnabled &&
                ReferenceEquals(manager.CombatBinding, this) && manager.IsBattleActive) return true;
            if (!reportedFailure)
            {
                reportedFailure = true;
                Close();
                reportFailure?.Invoke("The active Battle lost its bound Monster query dependency.");
            }
            return false;
        }
    }
    public bool Owns(MonsterBehaviour target) => target != null &&
        ReferenceEquals(target.CombatBinding, this);
    public bool CanTarget(MonsterBehaviour target) => IsUsable && Owns(target) && target.IsGameplayTargetable;
    public IReadOnlyList<MonsterBehaviour> GetAliveMonsters() => IsUsable ? manager.GetAliveMonsters() : Empty;
}
