// Only Unity scene/authoring APIs are substituted. Query/index/pathfinding code is real.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class SerializeField : Attribute {}
    public class ExecuteAlways : Attribute {}
    public class MonoBehaviour
    {
        public string name = "fixture";
        public Transform transform;
        public MonoBehaviour() { transform = new Transform(this); Resources.All.Add(this); }
        public T GetComponentInParent<T>(bool inactive = false) where T : class => transform.GetComponentInParent<T>(inactive);
    }
    public class Transform
    {
        public static int HierarchyScans;
        public Vector3 position;
        public Transform parent;
        public readonly List<Transform> Children = new List<Transform>();
        public object Component;
        public Transform(object owner = null) { Component = owner; }
        public void SetParent(Transform next, bool world = true)
        { parent?.Children.Remove(this); parent = next; next?.Children.Add(this); }
        public bool IsChildOf(Transform ancestor) { for (var p = this; p != null; p = p.parent) if (p == ancestor) return true; return false; }
        public T GetComponent<T>() where T : class => Component as T;
        public bool TryGetComponent<T>(out T value) where T : class { value = Component as T; return value != null; }
        public T GetComponentInParent<T>(bool inactive = false) where T : class
        { for (var p = this; p != null; p = p.parent) if (p.Component is T value) return value; return null; }
        public T[] GetComponentsInChildren<T>(bool inactive) where T : class
        { HierarchyScans++; var result = new List<T>(); Collect(this, result); return result.ToArray(); }
        private static void Collect<T>(Transform item, List<T> result) where T : class
        { if (item.Component is T value) result.Add(value); foreach (var child in item.Children) Collect(child, result); }
        public Vector3 InverseTransformPoint(Vector3 value) => value;
    }
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x=x; this.y=y; }
        public static Vector2Int up => new Vector2Int(0,1);
        public static Vector2Int down => new Vector2Int(0,-1);
        public static Vector2Int left => new Vector2Int(-1,0);
        public static Vector2Int right => new Vector2Int(1,0);
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x+b.x,a.y+b.y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);
        public bool Equals(Vector2Int b) => x==b.x && y==b.y;
        public override bool Equals(object b) => b is Vector2Int p && Equals(p);
        public override int GetHashCode() => x * 397 ^ y;
        public override string ToString() => x + "," + y;
    }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;} }
    public static class Mathf
    { public static int Abs(int x)=>Math.Abs(x); public static int FloorToInt(float v)=>(int)Math.Floor(v); }
    public static class Debug { public static void LogWarning(string s, object context = null) {} public static void LogError(string s, object context = null) {} }
    public static class Application { public static bool isPlaying; }
    public static class Resources
    {
        public static List<object> All = new List<object>();
        public static T[] FindObjectsOfTypeAll<T>() where T : class => All.FindAll(o=>o is T).ConvertAll(o=>(T)o).ToArray();
    }
}
namespace UnityEditor
{
    public class InitializeOnLoadMethodAttribute : Attribute {}
    public static class EditorApplication { public static event Action hierarchyChanged; public static void Raise() => hierarchyChanged?.Invoke(); }
    public static class Undo { public static event Action undoRedoPerformed; public static void Raise() => undoRedoPerformed?.Invoke(); }
}
public class TowerAnchorSet
{
    public bool Valid = true;
    public IReadOnlyList<UnityEngine.Transform> OccupiedAnchors;
    public bool IsValid() => Valid;
}
public class TowerPlacementPreview
{
    public TowerAnchorSet TowerAnchorSet;
}

#if !BASELINE
// Task002 supplies resolved candidates to the real final validator. Candidate capture
// and geometry checks execute against production code in Task004's integration suite.
internal sealed class TowerPlacementCandidate
{
    private TowerPlacementValidator owner;
    internal System.Collections.Generic.IReadOnlyList<GridNodeBehaviour> Footprint;
    internal bool IsCurrent(TowerPlacementValidator value)=>ReferenceEquals(owner,value);
    internal static TowerPlacementCandidate FromPreview(TowerPlacementValidator owner,TowerPlacementPreview preview)
    {
        owner.TryGetOccupiedNodes(preview,out var nodes);
        return new TowerPlacementCandidate{owner=owner,Footprint=nodes.AsReadOnly()};
    }
}
#endif
