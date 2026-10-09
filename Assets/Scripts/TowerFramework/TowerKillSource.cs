using System;

// Immutable provenance captured at release/application, never reconstructed at impact.
public readonly struct TowerKillSource
{
    public TowerKillSource(BattleCombatBinding battle, TowerInstance tower)
    {
        Battle = battle;
        Tower = tower;
        RuntimeIdentity = tower != null ? tower.RuntimeIdentity : null;
    }

    public BattleCombatBinding Battle { get; }
    public TowerInstance Tower { get; }
    internal object RuntimeIdentity { get; }
    internal bool IsValidFor(BattleCombatBinding battle) =>
        ReferenceEquals(Battle, battle) && battle != null && battle.IsOpenForRead &&
        Tower != null && Tower.CanReceiveKill(battle, RuntimeIdentity);

    internal void Commit(BattleCombatBinding battle)
    {
        if (IsValidFor(battle)) Tower.RecordKill(RuntimeIdentity);
    }
}
