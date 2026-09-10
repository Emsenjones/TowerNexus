// Explicit managed boundaries, not Unity or DOTween runtime emulation.
using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    public class Object
    {
        public static Func<Object> Factory;
        public static T Instantiate<T>(T template, Transform parent) where T : Object => (T)Factory();
        public static void Destroy(Object value) { if (value is GameObject go) go.SetActive(false); }
    }
    public class GameObject : Object
    {
        public bool active = true;
        public RectTransform transform = new RectTransform();
        public List<object> members = new List<object>();
        public void SetActive(bool active)
        {
            if (this.active == active) return;
            this.active = active;
            if (!active) foreach (object member in members.ToArray())
                member.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(member, null);
        }
    }
    public class MonoBehaviour : Object
    {
        public GameObject gameObject = new GameObject();
        public MonoBehaviour() { gameObject.members.Add(this); }
        public Transform transform => gameObject.transform;
        public bool isActiveAndEnabled => gameObject.active;
        public T[] GetComponentsInChildren<T>(bool all) => gameObject.members.FindAll(x => x is T).ConvertAll(x => (T)x).ToArray();
    }
    public class Transform : Object { public Vector3 position; public Vector3 localScale; }
    public class RectTransform : Transform { public Vector2 anchoredPosition; }
    public class CanvasGroup : Object { public float alpha; public bool interactable = true, blocksRaycasts = true; }
    public class Camera : Object { public static Camera main = new Camera(); public Vector3 WorldToScreenPoint(Vector3 value) => value; }
    public struct Vector2
    {
        public float x,y; public Vector2(float x,float y) { this.x=x;this.y=y; }
        public static Vector2 operator +(Vector2 a,Vector2 b) => new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b,float t) => new Vector2(Mathf.LerpUnclamped(a.x,b.x,t),Mathf.LerpUnclamped(a.y,b.y,t));
    }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
        public static Vector3 one => new Vector3(1,1,1);
        public static Vector3 LerpUnclamped(Vector3 a,Vector3 b,float t) => new Vector3(Mathf.LerpUnclamped(a.x,b.x,t),Mathf.LerpUnclamped(a.y,b.y,t),Mathf.LerpUnclamped(a.z,b.z,t));
    }
    public static class Time { public static int frameCount; public static float deltaTime, unscaledDeltaTime, timeScale=1; }
    public static class Application { public static bool isPlaying = true; }
    public static class Mathf
    {
        public static float Max(float a,float b) => Math.Max(a,b);
        public static float Clamp01(float t) => Math.Max(0,Math.Min(1,t));
        public static float LerpUnclamped(float a,float b,float t) => a+(b-a)*t;
    }
    public static class Random { public static float Range(float a,float b) => a; }
    public static class Debug { public static void LogWarning(object s, Object context) {} }
    public class SerializeField : Attribute {}
    public class ContextMenu : Attribute { public ContextMenu(string s) {} }
}
namespace UnityEngine.UI { public class Graphic : UnityEngine.Object { public bool raycastTarget = true; } }
namespace TMPro { public class TMP_Text : UnityEngine.UI.Graphic { public string text; } public class TextMeshProUGUI : TMP_Text {} }
namespace Sirenix.OdinInspector
{
    public class Button : Attribute { public Button(string s) {} }
    public class ShowIf : Attribute { public ShowIf(string s) {} }
    public class LabelText : Attribute { public LabelText(string s) {} }
}
namespace DG.Tweening
{
    public enum Ease { Unset, Linear, InQuad, OutQuad, OutBack, INTERNAL_Custom }
    public static class DOVirtual
    {
        public static float EasedValue(float a,float b,float t,Ease ease)
        {
            if (ease == Ease.OutQuad) t=1-(1-t)*(1-t);
            else if (ease == Ease.InQuad) t=t*t;
            else if (ease == Ease.OutBack) { float x=t-1; t=1+2.70158f*x*x*x+1.70158f*x*x; }
            return a+(b-a)*t;
        }
    }
}
