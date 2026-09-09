#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

// Editor-only observation identity and draining. Never consults combat permissions.
internal static class CombatDiagnosticScope
{
    internal sealed class Lease : IDisposable
    {
        internal readonly object Identity;
        internal readonly Dictionary<TowerInstance, CombatDiagnosticSource> Sources = new Dictionary<TowerInstance, CombatDiagnosticSource>();
        internal readonly Dictionary<MonsterBehaviour, TargetSnapshot> Targets = new Dictionary<MonsterBehaviour, TargetSnapshot>();
        internal int Depth;
        internal bool Closing, Failed;
        internal Action Drained;
        internal Lease(object identity) { Identity = identity; }
        public void Dispose() { leases.Remove(Identity); Drained = null; }
    }
    private static readonly Dictionary<object, Lease> leases = new Dictionary<object, Lease>();
    [ThreadStatic] internal static object CurrentIdentity;
    internal static Lease Acquire(object identity)
    {
        if (identity == null || leases.ContainsKey(identity)) return null;
        var lease = new Lease(identity); leases.Add(identity, lease); return lease;
    }
    internal static bool Enabled(object identity) => identity != null && leases.ContainsKey(identity);
    internal static CombatDiagnosticSource Source(TowerInstance tower)
    {
        if (ReferenceEquals(tower, null) || CurrentIdentity == null || !leases.TryGetValue(CurrentIdentity, out var lease)) return default;
        if (lease.Sources.TryGetValue(tower, out var source)) return source;
        source = Capture(CurrentIdentity, () => new CombatDiagnosticSource(tower, null));
        lease.Sources[tower] = source;
        return source;
    }
    internal readonly struct TargetSnapshot
    {
        internal readonly int Id;
        internal readonly bool HasNode;
        internal readonly Vector2Int Position;
        internal TargetSnapshot(MonsterBehaviour target)
        {
            Id = target.GetInstanceID();
            var node = target.CurrentNode;
            HasNode = node != null;
            Position = HasNode ? node.GridPosition : default;
        }
    }
    internal static void CaptureTarget(MonsterBehaviour target)
    {
        if (CurrentIdentity != null && leases.TryGetValue(CurrentIdentity, out var lease))
            lease.Targets[target] = Capture(CurrentIdentity, () => new TargetSnapshot(target));
    }
    internal static TargetSnapshot Target(MonsterBehaviour target)
    {
        if (CurrentIdentity != null && leases.TryGetValue(CurrentIdentity, out var lease) &&
            lease.Targets.TryGetValue(target, out var snapshot)) return snapshot;
        return default;
    }
    internal static void Fail(object identity, Exception error)
    {
        if (identity != null && leases.TryGetValue(identity, out var lease)) lease.Failed = true;
        Debug.LogException(error);
    }
    internal static T Capture<T>(object identity, Func<T> capture, T missing = default)
    {
        try { return capture(); }
        catch (Exception error) { Fail(identity, error); return missing; }
    }
    internal struct Scope : IDisposable
    {
        private readonly Lease lease;
        private readonly object previous;
        internal Scope(object identity)
        {
            previous = CurrentIdentity;
            CurrentIdentity = identity;
            lease = null;
            if (identity != null && leases.TryGetValue(identity, out lease)) lease.Depth++;
        }
        public void Dispose()
        {
            CurrentIdentity = previous;
            if (lease == null) return;
            lease.Depth--;
            if (lease.Depth == 0 && lease.Closing)
            {
                try { lease.Drained?.Invoke(); }
                catch (Exception error) { Fail(lease.Identity, error); }
            }
        }
    }
    internal static Scope Enter(object identity) => new Scope(identity);
}
#endif
