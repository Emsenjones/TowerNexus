using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class PendingDraftEntry
{
    internal PendingDraftEntry(PendingDraftCollection owner, ulong generation, ulong session, ulong sequence,
        DraftResult result, DraftAttemptToken source)
    {
        Owner = owner; BattleGeneration = generation; Session = session; Sequence = sequence;
        DraftResult = result; DraftAttemptToken = source;
    }
    internal PendingDraftCollection Owner { get; }
    internal ulong Session { get; }
    internal bool Registered { get; set; }
    public ulong BattleGeneration { get; }
    public ulong Sequence { get; }
    public DraftResult DraftResult { get; }
    public DraftAttemptToken DraftAttemptToken { get; }
    public TowerDefinition TowerDefinition => DraftResult.TowerDefinition;
    public TowerUpgradeDefinition TowerUpgradeDefinition => DraftResult.TowerUpgradeDefinition;
    public bool IsConsumed { get; internal set; }
}

// Prepared operations are single-use, synchronous tickets. No Unity objects or callbacks
// participate in registration/consumption. Stop invalidates tickets but retains snapshots.
internal sealed class PreparedPendingDraftConsumption
{
    internal PendingDraftCollection Owner;
    internal ulong Revision;
    internal PendingDraftEntry Entry;
    internal bool Used;
}
internal sealed class PreparedPendingDraftGrant
{
    internal PendingDraftCollection Owner;
    internal ulong Revision;
    internal PendingDraftEntry[] Entries;
    internal bool Used;
}

internal sealed class PendingDraftCollection
{
    private readonly List<PendingDraftEntry> held = new List<PendingDraftEntry>();
    private readonly ReadOnlyCollection<PendingDraftEntry> view;
    private ulong generation, session, sequence, revision;
    private bool active;
    internal PendingDraftCollection() { view = held.AsReadOnly(); }
    internal IReadOnlyList<PendingDraftEntry> Held => view;
    internal void BeginBattle(ulong value)
    {
        Clear(); generation = value; active = value != 0;
    }
    internal void Stop() { active = false; revision++; }
    internal void Clear() { Stop(); held.Clear(); generation = 0; sequence = 0; session++; }
    internal bool IsCurrent(PendingDraftEntry entry) => entry != null &&
        ReferenceEquals(entry.Owner, this) && generation != 0 &&
        entry.BattleGeneration == generation && entry.Session == session && entry.Registered;
    internal bool CanConsume(PendingDraftEntry entry) => active && IsCurrent(entry) &&
        !entry.IsConsumed && held.Contains(entry);

    internal bool TryPrepareGrant(IReadOnlyList<DraftResult> results,
        IReadOnlyList<DraftAttemptToken> sources, out PreparedPendingDraftGrant grant)
    {
        grant = null;
        if (!active || results == null || sources == null || results.Count == 0 ||
            results.Count != sources.Count) return false;
        for (int i = 0; i < results.Count; i++)
            if (results[i] == null || !results[i].IsValid || !sources[i].IsValid) return false;
        var entries = new PendingDraftEntry[results.Count];
        for (int i = 0; i < entries.Length; i++)
            entries[i] = new PendingDraftEntry(this, generation, session, ++sequence, results[i], sources[i]);
        if (held.Capacity < held.Count + entries.Length) held.Capacity = held.Count + entries.Length;
        grant = new PreparedPendingDraftGrant { Owner = this, Revision = revision, Entries = entries };
        return true;
    }
    internal bool TryCommitGrant(PreparedPendingDraftGrant grant)
    {
        if (!active || grant == null || grant.Used || !ReferenceEquals(grant.Owner, this) ||
            grant.Revision != revision) return false;
        grant.Used = true;
        foreach (var entry in grant.Entries) { entry.Registered = true; held.Add(entry); }
        revision++;
        return true;
    }
    internal bool TryPrepareConsumption(PendingDraftEntry entry,
        out PreparedPendingDraftConsumption ticket)
    {
        ticket = null;
        if (!CanConsume(entry)) return false;
        ticket = new PreparedPendingDraftConsumption { Owner = this, Revision = revision, Entry = entry };
        return true;
    }
    internal bool TryCommitConsumption(PreparedPendingDraftConsumption ticket)
    {
        if (ticket == null || ticket.Used || !ReferenceEquals(ticket.Owner, this) ||
            ticket.Revision != revision || !CanConsume(ticket.Entry)) return false;
        ticket.Used = true; ticket.Entry.IsConsumed = true; held.Remove(ticket.Entry); revision++;
        return true;
    }
}
