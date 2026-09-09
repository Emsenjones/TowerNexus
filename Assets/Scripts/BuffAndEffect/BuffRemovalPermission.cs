// Only the synchronous Removed execution owns this permission; it cannot survive reuse.
public sealed class BuffRemovalPermission
{
    private readonly MonsterBehaviour owner;
    private readonly BattleCombatBinding binding;
    private readonly object identity;
    private bool active = true;
    internal BuffRemovalPermission(MonsterBehaviour owner, BattleCombatBinding binding, object identity)
    { this.owner = owner; this.binding = binding; this.identity = identity; }
    internal bool Allows(MonsterBehaviour target) => active && binding != null &&
        target == owner && binding.Owns(target) && ReferenceEquals(target.RuntimeIdentity, identity);
    internal void Close() { active = false; }
}
