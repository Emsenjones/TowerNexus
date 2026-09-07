using UnityEngine;

public readonly struct DroneReleaseData
{
    public DroneReleaseData(
        TargetSelectionType targetSelectionType,
        GameObject attackReleaseVfxPrefab)
    {
        TargetSelectionType = targetSelectionType;
        AttackReleaseVfxPrefab = attackReleaseVfxPrefab;
    }

    public TargetSelectionType TargetSelectionType { get; }
    public GameObject AttackReleaseVfxPrefab { get; }
}
