using System;
using System.Collections.Generic;
using UnityEngine;

// Map-owned lookup storage. Full Stage/visual readiness remains in ValidateMap.
internal sealed class MapRuntimeNodeIndex
{
    private readonly Dictionary<Vector2Int, GridNodeBehaviour> nodes =
        new Dictionary<Vector2Int, GridNodeBehaviour>();
    internal bool IsDirty { get; private set; } = true;
    internal bool IsValid { get; private set; }
    internal ulong StructureRevision { get; private set; } = 1;
    internal ulong WalkabilityRevision { get; private set; } = 1;
    internal GridNodeBehaviour Spawn { get; private set; }
    internal GridNodeBehaviour Target { get; private set; }
    internal string FailureReason { get; private set; }
#if UNITY_EDITOR
    internal int BuildCount { get; private set; }
#endif

    internal void InvalidateStructure()
    {
        StructureRevision++;
        IsDirty = true;
        IsValid = false;
    }

    internal void InvalidateWalkability() => WalkabilityRevision++;

    internal void Release()
    {
        InvalidateStructure();
        nodes.Clear();
        Spawn = Target = null;
    }

    internal bool Rebuild(IReadOnlyList<GridNodeBehaviour> hierarchy, int width,
        int height, Func<GridNodeBehaviour, bool> ownsNode)
    {
#if UNITY_EDITOR
        BuildCount++;
#endif
        nodes.Clear();
        Spawn = Target = null;
        IsDirty = false;
        IsValid = false;
        FailureReason = string.Empty;
        if (width <= 0 || height <= 0 || hierarchy == null ||
            (long)width * height != hierarchy.Count)
            return Reject("The node index does not contain the complete rectangular grid.");

        int spawnCount = 0, targetCount = 0;
        for (int i = 0; i < hierarchy.Count; i++)
        {
            GridNodeBehaviour node = hierarchy[i];
            if (node == null || !ownsNode(node))
                return Reject("The node index contains a missing or incorrectly owned node.");
            Vector2Int position = node.GridPosition;
            if (position.x < 0 || position.y < 0 || position.x >= width ||
                position.y >= height || nodes.ContainsKey(position))
                return Reject("The node index contains an out-of-bounds or duplicate coordinate.");
            nodes.Add(position, node);
            if (node.NodeType == GridNodeType.Spawn) { Spawn = node; spawnCount++; }
            if (node.NodeType == GridNodeType.Target) { Target = node; targetCount++; }
        }
        // Authoring without endpoints is queryable. Ambiguous endpoints are not selected.
        if (spawnCount != 1) Spawn = null;
        if (targetCount != 1) Target = null;
        IsValid = true;
        return true;
    }

    private bool Reject(string reason)
    {
        nodes.Clear();
        Spawn = Target = null;
        FailureReason = reason;
        return false;
    }

    internal GridNodeBehaviour GetNode(Vector2Int position) =>
        IsValid && nodes.TryGetValue(position, out GridNodeBehaviour node) ? node : null;
}
