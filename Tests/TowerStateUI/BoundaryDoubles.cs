// Managed boundary doubles: these do not validate Unity rendering or native lifecycle order.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public class Object
    {
        public bool Dead;
        public static Func<Object, Transform, Object> Factory;
        private static readonly List<GameObject> pending = new List<GameObject>();
        public static T Instantiate<T>(T original, Transform parent) where T : Object => (T)Factory(original, parent);
        public static void Destroy(Object target) { if (target is GameObject go && !pending.Contains(go)) pending.Add(go); }
        public static void Flush()
        {
            var snapshot = pending.ToArray(); pending.Clear();
            foreach (var go in snapshot)
            {
                go.SetActive(false);
                foreach (var component in go.Components.ToArray())
                {
                    Invoke(component, "OnDestroy"); component.Dead = true;
                }
                go.Dead = true;
            }
        }
        public static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
        public static bool operator ==(Object a, Object b)
        {
            bool na = ReferenceEquals(a, null) || a.Dead, nb = ReferenceEquals(b, null) || b.Dead;
            return na || nb ? na == nb : ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object o) => ReferenceEquals(this, o);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
    public class Component : Object
    {
        internal GameObject Owner;
        public GameObject gameObject => Owner ?? (Owner = new GameObject(this));
        public Transform transform => gameObject.transform;
        public bool TryGetComponent<T>(out T value) where T : class
        { value = gameObject.Components.OfType<T>().FirstOrDefault(); return value != null; }
        public T GetComponentInParent<T>() where T : class
        {
            for (Transform p = transform; p != null; p = p.parent)
            { var found = p.gameObject.Components.OfType<T>().FirstOrDefault(); if (found != null) return found; }
            return null;
        }
    }
    public class MonoBehaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled && !Dead && gameObject.activeInHierarchy;
    }
    public class GameObject : Object
    {
        public readonly List<Component> Components = new List<Component>();
        public RectTransform transform;
        public bool activeSelf = true;
        public bool activeInHierarchy => !Dead && activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy);
        public GameObject(Component component = null)
        {
            transform = new RectTransform { Owner = this }; Components.Add(transform);
            if (component != null) Attach(component);
        }
        public void Attach(Component component) { component.Owner = this; Components.Add(component); }
        public void SetActive(bool value)
        {
            if (activeSelf == value) return;
            activeSelf = value;
            foreach (var c in Components.ToArray()) Object.Invoke(c, value ? "OnEnable" : "OnDisable");
        }
    }
    public class Transform : Component
    {
        public Transform parent;
        public Vector3 position, localPosition;
        public bool IsChildOf(Transform other)
        { for (Transform p = this; p != null; p = p.parent) if (ReferenceEquals(p, other)) return true; return false; }
    }
    public class RectTransform : Transform { }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x,v.y);
    }
    public class Camera : MonoBehaviour
    {
        public Vector3 Viewport = new Vector3(.5f,.5f,1), Screen = new Vector3(800,500,1);
        public Vector3 WorldToViewportPoint(Vector3 value) => Viewport;
        public Vector3 WorldToScreenPoint(Vector3 value) => Screen;
    }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera }
    public class Canvas : MonoBehaviour
    {
        public Canvas rootCanvas => this;
        public RenderMode renderMode;
        public static event Action preWillRenderCanvases;
        public static int Subscribers => preWillRenderCanvases?.GetInvocationList().Length ?? 0;
        public static void Render() => preWillRenderCanvases?.Invoke();
    }
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform parent, Vector2 screen, Camera camera, out Vector2 local)
        { if (camera != null) throw new Exception("Overlay must use null UI camera"); local=new Vector2((screen.x-100)/2,(screen.y-50)/2);return true; }
    }
    public static class Debug
    {
        public static int Warnings, Exceptions;
        public static void LogWarning(string message, Object context=null) { Warnings++; }
        public static void LogException(Exception exception, Object context=null) { Exceptions++; }
    }
}
namespace TMPro
{
    public class TMP_Text : UnityEngine.MonoBehaviour
    {
        public bool raycastTarget=true;
        public string text;
        public void SetText(string format, float value) { text=string.Format(System.Globalization.CultureInfo.InvariantCulture,format,value); }
    }
}
public class TowerInstance : UnityEngine.MonoBehaviour
{
    public int CurrentLevel=1;
    public event Action<TowerInstance,int,int> OnLevelChanged;
    public int Subscribers => OnLevelChanged?.GetInvocationList().Length ?? 0;
    public void Level(int value) { int before=CurrentLevel;CurrentLevel=value;OnLevelChanged?.Invoke(this,before,value); }
}
public class TowerBehaviour : UnityEngine.MonoBehaviour { public TowerVisualController VisualController; }
public class TowerVisualController { public UnityEngine.Transform TowerModelRoot; }
internal class TowerPlacementSubmission
{
    public readonly List<TowerInstance> Members=new List<TowerInstance>();
    public IReadOnlyList<TowerInstance> DeployedTowerInstances => Members;
    public bool Active=true;
    public event Action<TowerInstance> OnTowerDeploymentCommitted;
    public int Subscribers => OnTowerDeploymentCommitted?.GetInvocationList().Length ?? 0;
    public Action<TowerInstance> Capture() => OnTowerDeploymentCommitted;
    public bool OwnsDeployedTower(TowerInstance tower) => Active && Members.Contains(tower);
    public void Deploy(TowerInstance tower) { Members.Add(tower);OnTowerDeploymentCommitted?.Invoke(tower); }
    public void Publish(TowerInstance tower) => OnTowerDeploymentCommitted?.Invoke(tower);
}
