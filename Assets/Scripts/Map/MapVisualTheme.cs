using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum MapTileDirectionMask
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8
}

[Serializable]
public sealed class MapTileVisualEntry
{
    [SerializeField] private MapTileDirectionMask directionMask;
    [SerializeField] private GameObject prefab;

    public MapTileDirectionMask DirectionMask => directionMask;
    public GameObject Prefab => prefab;
}

[CreateAssetMenu(fileName = "MapVisualTheme", menuName = "Tower Nexus/Map Visual Theme")]
public sealed class MapVisualTheme : ScriptableObject
{
    private static readonly MapTileDirectionMask[] SupportedTileMasks =
    {
        MapTileDirectionMask.None,
        MapTileDirectionMask.Up | MapTileDirectionMask.Down,
        MapTileDirectionMask.Left | MapTileDirectionMask.Right,
        MapTileDirectionMask.Up | MapTileDirectionMask.Left,
        MapTileDirectionMask.Up | MapTileDirectionMask.Right,
        MapTileDirectionMask.Down | MapTileDirectionMask.Left,
        MapTileDirectionMask.Down | MapTileDirectionMask.Right,
        MapTileDirectionMask.Down | MapTileDirectionMask.Left | MapTileDirectionMask.Right,
        MapTileDirectionMask.Up | MapTileDirectionMask.Left | MapTileDirectionMask.Right,
        MapTileDirectionMask.Up | MapTileDirectionMask.Down | MapTileDirectionMask.Right,
        MapTileDirectionMask.Up | MapTileDirectionMask.Down | MapTileDirectionMask.Left,
        MapTileDirectionMask.Up | MapTileDirectionMask.Down | MapTileDirectionMask.Left | MapTileDirectionMask.Right
    };

    [SerializeField] private List<MapTileVisualEntry> tileVisualEntries = new List<MapTileVisualEntry>();
    [SerializeField] private List<GameObject> obstaclePrefabs = new List<GameObject>();
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private GameObject targetPrefab;

    public IReadOnlyList<MapTileVisualEntry> TileVisualEntries => tileVisualEntries;
    public IReadOnlyList<GameObject> ObstaclePrefabs => obstaclePrefabs;
    public GameObject SpawnPrefab => spawnPrefab;
    public GameObject TargetPrefab => targetPrefab;

    public static IReadOnlyList<MapTileDirectionMask> RequiredTileMasks => SupportedTileMasks;

    public bool TryGetTilePrefab(MapTileDirectionMask directionMask, out GameObject prefab)
    {
        prefab = null;

        if (tileVisualEntries == null)
        {
            return false;
        }

        for (int i = 0; i < tileVisualEntries.Count; i++)
        {
            MapTileVisualEntry entry = tileVisualEntries[i];

            if (entry == null || entry.DirectionMask != directionMask)
            {
                continue;
            }

            prefab = entry.Prefab;
            return prefab != null;
        }

        return false;
    }

    public GameObject GetDeterministicObstaclePrefab(int seed, Vector2Int gridPosition)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0)
        {
            return null;
        }

        int index = GetStableIndex(seed, gridPosition, 1, obstaclePrefabs.Count);
        return obstaclePrefabs[index];
    }

    public static bool IsSupportedTileMask(MapTileDirectionMask directionMask)
    {
        for (int i = 0; i < SupportedTileMasks.Length; i++)
        {
            if (SupportedTileMasks[i] == directionMask)
            {
                return true;
            }
        }

        return false;
    }

    public static bool UsesOnlyDirectionBits(MapTileDirectionMask directionMask)
    {
        const MapTileDirectionMask allDirectionBits =
            MapTileDirectionMask.Up |
            MapTileDirectionMask.Down |
            MapTileDirectionMask.Left |
            MapTileDirectionMask.Right;

        return (directionMask & ~allDirectionBits) == 0;
    }

    public static bool IsSingleDirectionMask(MapTileDirectionMask directionMask)
    {
        int value = (int)directionMask;
        return value != 0 && (value & (value - 1)) == 0;
    }

    private static int GetStableIndex(
        int seed,
        Vector2Int gridPosition,
        int visualCategory,
        int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0;
        }

        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)seed) * 16777619u;
            hash = (hash ^ (uint)gridPosition.x) * 16777619u;
            hash = (hash ^ (uint)gridPosition.y) * 16777619u;
            hash = (hash ^ (uint)visualCategory) * 16777619u;
            return (int)(hash % (uint)itemCount);
        }
    }
}
