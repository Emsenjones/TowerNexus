using UnityEngine;

internal sealed class TowerInspectionSnapshot
{
    internal readonly TowerInstance Target;
    internal readonly TowerCombatBehaviour Combat;
    internal readonly TowerDefinition Definition;
    internal readonly int Level;
    internal readonly ulong BaselineRevision;
    internal readonly ResolvedTowerCombatStats Stats;
    internal readonly string DisplayName, Description;
    internal readonly Sprite Icon;
    internal readonly TowerUpgradeDefinition[] Upgrades;
    internal readonly Sprite[] UpgradeIcons;

    private TowerInspectionSnapshot(TowerInstance target, TowerCombatBehaviour combat,
        ResolvedTowerCombatStats stats, ulong revision)
    {
        Target = target; Combat = combat; Definition = target.TowerDefinition;
        Level = target.CurrentLevel; Stats = stats; BaselineRevision = revision;
        DisplayName = Definition.DisplayName; Description = Definition.Description; Icon = Definition.Icon;
        Upgrades = new TowerUpgradeDefinition[target.AppliedUpgrades.Count];
        UpgradeIcons = new Sprite[Upgrades.Length];
        for (int i = 0; i < Upgrades.Length; i++)
        {
            Upgrades[i] = target.AppliedUpgrades[i];
            UpgradeIcons[i] = Upgrades[i] != null ? Upgrades[i].Icon : null;
        }
    }

    internal static bool TryCapture(TowerInstance target, BattleCombatBinding battle,
        TowerPlacementSubmission members, out TowerInspectionSnapshot snapshot, out string reason)
    {
        snapshot = null;
        reason = "Target is not a current deployed Tower.";
        if (target == null || members == null || !members.OwnsDeployedTower(target) ||
            target.TowerDefinition == null || !target.TryGetComponent(out TowerCombatBehaviour combat)) return false;
        if (!combat.TryGetInspectionStats(target, battle, out var stats, out var revision, out reason)) return false;
        snapshot = new TowerInspectionSnapshot(target, combat, stats, revision);
        if (!snapshot.IsCurrent(battle, members)) { snapshot = null; reason = "Tower data changed during capture."; return false; }
        return true;
    }

    internal bool IsCurrent(BattleCombatBinding battle, TowerPlacementSubmission members)
    {
        if (Target == null || Combat == null || Definition == null || members == null || !members.OwnsDeployedTower(Target) ||
            Target.TowerDefinition != Definition || Target.CurrentLevel != Level ||
            Definition.DisplayName != DisplayName || Definition.Description != Description || Definition.Icon != Icon ||
            !Combat.TryGetInspectionStats(Target, battle, out _, out var revision, out _) || revision != BaselineRevision ||
            Target.AppliedUpgrades.Count != Upgrades.Length) return false;
        for (int i = 0; i < Upgrades.Length; i++)
            if (Upgrades[i] == null || Target.AppliedUpgrades[i] != Upgrades[i] || Upgrades[i].Icon != UpgradeIcons[i]) return false;
        return true;
    }

    internal bool IsTargetAvailable(BattleCombatBinding battle, TowerPlacementSubmission members) =>
        Target != null && Target.isActiveAndEnabled && Combat != null && Combat.isActiveAndEnabled &&
        Combat.IsInspectionRuntimeAvailable(Target, battle) && members != null && members.OwnsDeployedTower(Target);
}
