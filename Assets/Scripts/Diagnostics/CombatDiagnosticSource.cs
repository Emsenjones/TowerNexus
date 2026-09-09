#if UNITY_EDITOR
// Captured at the producing boundary. No Unity object survives in this identity value.
internal readonly struct CombatDiagnosticSource
{
    internal CombatDiagnosticSource(TowerInstance tower, EffectDefinition effect)
    {
        Id = tower != null ? tower.GetInstanceID() : 0;
        Name = tower != null ? tower.name : string.Empty;
        var definition = tower != null ? tower.TowerDefinition : null;
        DisplayName = definition != null ?
            (string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.name : definition.DisplayName.Trim()) : string.Empty;
        WaveDisplayName = definition != null ? definition.DisplayName : Name;
        Family = definition != null ? definition.TowerFamily.ToString() : string.Empty;
        EffectName = effect != null ? effect.name : string.Empty;
    }
    internal int Id { get; }
    internal string Name { get; }
    internal string DisplayName { get; }
    internal string WaveDisplayName { get; }
    internal string Family { get; }
    internal string EffectName { get; }
}
#endif
