using System;
using UnityEngine;

internal enum BattleModalKind { Draft, TowerInspection }

internal sealed class BattleModalPauseHandle
{
    internal readonly BattleModalPauseAuthority Authority;
    internal readonly object Battle;
    internal readonly object Session;
    internal readonly BattleModalKind Kind;
    internal BattleModalPauseHandle(BattleModalPauseAuthority authority, object battle,
        object session, BattleModalKind kind)
    { Authority = authority; Battle = battle; Session = session; Kind = kind; }
}

// One coordinator owns this capability. Views never write simulation time.
internal sealed class BattleModalPauseAuthority
{
    private readonly Func<float> readRate;
    private readonly Action<float> writeRate;
    private object battle;
    private bool open;
    private bool activated;
    private BattleModalPauseHandle retained;
    private float capturedRate;

    internal BattleModalPauseAuthority() : this(() => Time.timeScale, rate => Time.timeScale = rate) { }
    internal BattleModalPauseAuthority(Func<float> readRate, Action<float> writeRate)
    { this.readRate = readRate; this.writeRate = writeRate; }

    internal bool HasRetainedPause => retained != null;
    internal bool CanAcquire => battle != null && open && retained == null;
    internal BattleModalKind? CurrentKind => retained?.Kind;
    internal bool IsBattleOpen(object identity) => open && ReferenceEquals(battle, identity);

    internal void BindBattle(object identity)
    {
        if (battle != null || retained != null) throw new InvalidOperationException("Outgoing modal pause was not cleared.");
        if (identity == null) throw new ArgumentNullException(nameof(identity));
        battle = identity;
        open = false;
        activated = false;
    }

    internal void OpenBattle(object identity)
    {
        if (!ReferenceEquals(battle, identity) || identity == null || activated)
            throw new InvalidOperationException("Invalid modal Battle activation.");
        open = true;
        activated = true;
    }

    internal bool TryAcquire(object identity, BattleModalKind kind, object session,
        out BattleModalPauseHandle handle, out string reason)
    {
        handle = null;
        reason = "Modal Battle/session is unavailable or another modal owns pause.";
        if (identity == null || session == null || !IsBattleOpen(identity) || retained != null ||
            (kind != BattleModalKind.Draft && kind != BattleModalKind.TowerInspection)) return false;
        float rate = readRate();
        if (float.IsNaN(rate) || float.IsInfinity(rate) || rate < 0f)
        { reason = "The current simulation rate is invalid."; return false; }
        var next = new BattleModalPauseHandle(this, identity, session, kind);
        capturedRate = rate;
        retained = next;
        try { writeRate(0f); }
        catch { retained = null; capturedRate = 0f; throw; }
        handle = next;
        reason = string.Empty;
        return true;
    }

    internal bool Owns(BattleModalPauseHandle handle) => handle != null && open &&
        ReferenceEquals(handle, retained) && ReferenceEquals(handle.Authority, this) &&
        ReferenceEquals(handle.Battle, battle);

    internal void Release(BattleModalPauseHandle handle)
    {
        // Retained cleanup ownership survives revocation; interactive ownership does not.
        if (handle == null || !ReferenceEquals(handle, retained) ||
            !ReferenceEquals(handle.Authority, this) || !ReferenceEquals(handle.Battle, battle)) return;
        float restore = capturedRate;
        retained = null;
        capturedRate = 0f;
        writeRate(restore);
    }

    internal void RevokeBattle(object identity)
    { if (identity != null && ReferenceEquals(identity, battle)) open = false; }

    internal void CancelBattle(object identity)
    {
        if (identity == null || !ReferenceEquals(identity, battle)) return;
        open = false;
        try { Release(retained); }
        finally { battle = null; }
    }
}
