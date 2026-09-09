using System;
using System.Collections.Generic;
using UnityEngine;

// Only capture can construct a candidate. It takes geometry and pose from the same
// final preview resolution; the submission core never retains or dereferences Preview.
internal sealed class TowerPlacementCandidate
{
    private readonly TowerPlacementValidator validator;
    private readonly ulong validatorRevision, structureRevision, walkabilityRevision, pathRevision;
    private readonly Vector3[] anchorPositions;
    private readonly Vector3 centerPosition;
    private readonly int frame;
    private bool claimed;
    private TowerPlacementCandidate(TowerPlacementValidator validator, TowerPlacementPreview preview,
        IReadOnlyList<GridNodeBehaviour> footprint)
    {
        this.validator = validator; validatorRevision = validator.BindingRevision;
        Map = validator.ActiveMap; structureRevision = Map.StructureRevision;
        walkabilityRevision = Map.WalkabilityRevision; pathRevision = validator.PathBindingRevision;
        Definition = preview.TowerDefinition;
        Position = preview.transform.position; Rotation = preview.transform.rotation;
        LocalScale = preview.transform.localScale; frame = Time.frameCount;
        Footprint = new List<GridNodeBehaviour>(footprint).AsReadOnly();
        var anchors = preview.TowerAnchorSet.OccupiedAnchors;
        anchorPositions = new Vector3[anchors.Count];
        for (int i = 0; i < anchors.Count; i++) anchorPositions[i] = anchors[i].position;
        centerPosition = preview.TowerAnchorSet.CenterAnchor.position;
    }
    internal MapGeneratorBehaviour Map { get; }
    internal TowerDefinition Definition { get; }
    internal Vector3 Position { get; }
    internal Quaternion Rotation { get; }
    internal Vector3 LocalScale { get; }
    internal IReadOnlyList<GridNodeBehaviour> Footprint { get; }
    internal static bool TryCapture(TowerPlacementValidator validator, TowerPlacementPreview preview,
        out TowerPlacementCandidate candidate, out string reason)
    {
        candidate = null; reason = "The final Tower footprint or preview is unavailable.";
        if (validator == null || preview == null || preview.TowerDefinition == null ||
            !preview.TowerDefinition.IsValid() || validator.ActiveMap == null ||
            !validator.ActiveMap.TryEnsureNodeIndex() ||
            !validator.TryGetOccupiedNodes(preview, out var nodes) || nodes.Count == 0) return false;
        candidate = new TowerPlacementCandidate(validator, preview, nodes);
        reason = string.Empty; return true;
    }
    internal bool IsCurrent(TowerPlacementValidator owner) => owner == validator &&
        validator != null && validator.ActiveMap == Map && Map != null &&
        validator.BindingRevision == validatorRevision && validator.PathBindingRevision == pathRevision &&
        Map.StructureRevision == structureRevision && Map.WalkabilityRevision == walkabilityRevision &&
        frame == Time.frameCount;
    internal bool TryClaim(TowerPlacementValidator owner)
    {
        if (claimed || !IsCurrent(owner)) return false;
        claimed = true; return true;
    }
    internal bool MatchesPreparedTower(TowerBehaviour tower)
    {
        if (tower == null || tower.TowerInstance == null || tower.TowerInstance.TowerDefinition != Definition ||
            (tower.transform.position - Position).sqrMagnitude > 0.000001f ||
            Quaternion.Angle(tower.transform.rotation, Rotation) > 0.001f ||
            (tower.transform.localScale - LocalScale).sqrMagnitude > 0.000001f ||
            !tower.TryGetComponent(out TowerAnchorSet anchors) || !anchors.IsValid() ||
            anchors.OccupiedAnchors.Count != anchorPositions.Length ||
            (anchors.CenterAnchor.position - centerPosition).sqrMagnitude > 0.000001f) return false;
        for (int i = 0; i < anchorPositions.Length; i++)
            if (anchors.OccupiedAnchors[i] == null ||
                (anchors.OccupiedAnchors[i].position - anchorPositions[i]).sqrMagnitude > 0.000001f) return false;
        return true;
    }
}
