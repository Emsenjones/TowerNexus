using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public static Action BeforeInstantiate;
        public static List<GameObject> Created=new List<GameObject>();
        public bool Destroyed;
        public static bool operator ==(Object a,Object b)
        { bool an=ReferenceEquals(a,null)||a.Dead, bn=ReferenceEquals(b,null)||b.Dead;return an&&bn || !an&&!bn&&ReferenceEquals(a,b); }
        public static bool operator !=(Object a,Object b)=>!(a==b);
        bool Dead=>Destroyed || this is Component c && c.gameObject.Destroyed;
        public override bool Equals(object other)=>ReferenceEquals(this,other);
        public override int GetHashCode()=>base.GetHashCode();
        public static T Instantiate<T>(T source,Transform parent) where T:Object
        {
            BeforeInstantiate?.Invoke();
            var go=new GameObject("icon");go.transform.SetParent(parent,false);
            var item=go.AddComponent<UnityEngine.UI.Image>(); Created.Add(go);return item as T;
        }
        public static void Destroy(Object value)
        {
            if(value is GameObject go){go.SetActive(false);go.Dispatch("OnDestroy");go.Destroyed=true;go.transform.SetParent(null,false);}
            else value.Destroyed=true;
        }
    }
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public bool TryGetComponent<T>(out T result) where T:class {result=GetComponent<T>();return result!=null;}
    }
    public class MonoBehaviour:Component
    {
        public bool enabled=true;
        public bool isActiveAndEnabled=>enabled&&gameObject.activeInHierarchy;
    }
    public class Transform:Component
    {
        Transform owner;
        public Transform parent=>owner;
        public List<Transform> children=new List<Transform>();
        public void SetParent(Transform next,bool world){owner?.children.Remove(this);owner=next;next?.children.Add(this);}
        public bool IsChildOf(Transform next)=>this==next||parent!=null&&parent.IsChildOf(next);
        public int GetSiblingIndex()=>parent==null?0:parent.children.IndexOf(this);
    }
    public class RectTransform:Transform
    {
        public Vector2 anchorMin=Vector2.zero,anchorMax=Vector2.one,offsetMin,offsetMax;
    }
    public struct Vector2
    {
        float x,y;
        public static Vector2 zero=>default;
        public static Vector2 one=>new Vector2{x=1,y=1};
        public static bool operator ==(Vector2 a,Vector2 b)=>a.x==b.x&&a.y==b.y;
        public static bool operator !=(Vector2 a,Vector2 b)=>!(a==b);
        public override bool Equals(object o)=>o is Vector2 v&&this==v;
        public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode();
    }
    public class GameObject:Object
    {
        public static Action<GameObject> Activated;
        public static Action<GameObject> BeforeDeactivate;
        public bool activeSelf=true;
        public bool activeInHierarchy=>activeSelf&&!Destroyed&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public RectTransform transform;
        List<Component> components=new List<Component>();
        public GameObject(string name=""){transform=new RectTransform{gameObject=this};components.Add(transform);}
        public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
        public T GetComponent<T>() where T:class=>components.Find(x=>x is T) as T;
        public void Dispatch(string method)
        {foreach(var c in components.ToArray()) c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);}
        public void SetActive(bool next)
        {
            if (!next) BeforeDeactivate?.Invoke(this);
            bool before=activeInHierarchy;activeSelf=next;
            if(before!=activeInHierarchy)Dispatch(activeInHierarchy?"OnEnable":"OnDisable");
            if(next&&activeInHierarchy)Activated?.Invoke(this);
        }
    }
    public class CanvasGroup:MonoBehaviour{public float alpha=1;public bool blocksRaycasts=true,interactable=true;}
    public class Sprite:Object{}
    public class SerializeField:Attribute{}
    public static class Time {public static float timeScale=1;}
    public static class Debug
    {
        public static void LogWarning(string value,Object context=null){}
        public static void LogException(Exception error,Object context=null){}
    }
}
namespace UnityEngine.UI
{
    public class Image:UnityEngine.MonoBehaviour{public UnityEngine.Sprite sprite;public bool raycastTarget=true;}
    public class GridLayoutGroup:UnityEngine.MonoBehaviour{}
    public class Button:UnityEngine.MonoBehaviour
    {
        public bool interactable=true;
        public Event onClick=new Event();
        public class Event
        {
            Action handlers;
            public void AddListener(Action callback){handlers+=callback;}
            public void RemoveListener(Action callback){handlers-=callback;}
            public void Invoke(){handlers?.Invoke();}
        }
    }
}
namespace TMPro {public class TMP_Text:UnityEngine.MonoBehaviour{public string text;}}

public enum TowerFamily{Archer}
public class EffectDefinition{}
public class TowerLevelConfig{public int BasicDamage=10;}
public class TowerDefinition
{
    public string DisplayName="Archer", Description="Description";
    public UnityEngine.Sprite Icon=new UnityEngine.Sprite();
    public TowerFamily TowerFamily;
}
public class TowerUpgradeDefinition{public UnityEngine.Sprite Icon=new UnityEngine.Sprite();}
public class TowerInstance:UnityEngine.MonoBehaviour
{
    public TowerDefinition TowerDefinition=new TowerDefinition();
    public int CurrentLevel=1;
    public TowerLevelConfig CurrentLevelConfig=new TowerLevelConfig();
    public List<TowerUpgradeDefinition> AppliedUpgrades=new List<TowerUpgradeDefinition>();
}
public class MonsterBehaviour:UnityEngine.MonoBehaviour
{
    public BattleCombatBinding CombatBinding;
    public bool IsGameplayTargetable=true;
}
public class MonsterManager:UnityEngine.MonoBehaviour
{
    public BattleCombatBinding CombatBinding;
    public bool IsBattleActive=true;
    public IReadOnlyList<MonsterBehaviour> GetAliveMonsters()=>new MonsterBehaviour[0];
}
internal sealed class TowerPlacementSubmission
{
    internal bool IsBusy, CanStartOperation=true;
    internal HashSet<TowerInstance> Members=new HashSet<TowerInstance>();
    internal bool OwnsDeployedTower(TowerInstance tower)=>tower!=null&&Members.Contains(tower);
}
public class TowerPlacementController
{
    public bool IsDragging, CanStartDraftInteraction=true, ThrowCancel;
    public void CancelPlacement(){if(ThrowCancel)throw new Exception("placement cleanup");}
}
public class DraftWindow
{
    public bool ThrowNotify;
    public void NotifyPresentationLost(){if(ThrowNotify)throw new Exception("Draft cleanup");}
}
public partial class BattleHUDUI:UnityEngine.MonoBehaviour
{
    private TowerPlacementController towerPlacementController=new TowerPlacementController();
    private DraftWindow draftWindow=new DraftWindow();
    public bool IsDraftOpen;
    public bool ThrowUnsubscribe;
    private void UnsubscribeFromPlayerSystem(){if(ThrowUnsubscribe)throw new Exception("subscription cleanup");}
}
internal struct PreparedTowerCombatLevelRevision
{
    internal ResolvedTowerCombatStats ResolvedStats;
    internal PreparedTowerCombatLevelRevision(ResolvedTowerCombatStats stats){ResolvedStats=stats;}
}
internal struct PreparedTowerCombatUpgradeRevision
{
    internal ResolvedTowerCombatStats Current;
}
public partial class TowerCombatBehaviour:UnityEngine.MonoBehaviour
{
    private TowerInstance towerInstance;
    private TowerDefinition towerDefinition;
    private BattleCombatBinding battleBinding;
    private ResolvedTowerCombatStats cachedResolvedStats;
    private ulong resolvedBaselineRevision;
    private bool hasExplicitInitialization=true,hasResolvedStatsCache=true,isBattleActive=true,isRuntimeSessionActive=true;
    public TowerFamily SupportedTowerFamily=>TowerFamily.Archer;
    internal void Bind(TowerInstance tower,BattleCombatBinding battle)
    {
        towerInstance=tower;towerDefinition=tower.TowerDefinition;battleBinding=battle;
        cachedResolvedStats=new ResolvedTowerCombatStats(3.25f,1,10,15.5f,5,0,0);resolvedBaselineRevision=1;
    }
    internal void UpgradeBaseline(){CommitPreparedUpgradeBaseline(new PreparedTowerCombatUpgradeRevision{Current=new ResolvedTowerCombatStats(4.5f,1,10,20.5f,10,0,0)});}
    internal void LevelBaseline(){towerInstance.CurrentLevel++;towerInstance.CurrentLevelConfig.BasicDamage=20;ApplyPreparedLevelDamageRevision(new PreparedTowerCombatLevelRevision(new ResolvedTowerCombatStats(3.25f,1,20,25.5f,5,0,0)));}
    internal void InvalidateCache(){hasResolvedStatsCache=false;}
}
