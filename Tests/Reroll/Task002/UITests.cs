using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    public class Object
    {
        public static Func<Object,Transform,Object> Factory;
        public static T Instantiate<T>(T prefab,Transform parent) where T:Object => (T)Factory(prefab,parent);
        public static void Destroy(Object o){if(o is GameObject g){g.SetActive(false);g.Destroyed=true;}}
    }
    public class SerializeField:Attribute{}
    public class Header:Attribute{public Header(string value){}}
    public class Transform:Object
    {
        public GameObject gameObject;public Transform parent;
        public void SetParent(Transform p,bool world){parent=p;}
        public bool IsChildOf(Transform p)=>this==p || parent!=null&&parent.IsChildOf(p);
        public void SetAsLastSibling(){}
    }
    public class GameObject:Object
    {
        public bool activeSelf=true,Destroyed; public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);public Transform transform;
        public List<MonoBehaviour> members=new List<MonoBehaviour>();
        public GameObject(string name=""){transform=new Transform{gameObject=this};}
        public T GetComponent<T>() where T:class=>members.Find(x=>x is T) as T;
        public T[] GetComponents<T>()=>members.FindAll(x=>x is T).ConvertAll(x=>(T)(object)x).ToArray();
        public T[] GetComponentsInChildren<T>(bool all)=>GetComponents<T>();
        public bool TryGetComponent<T>(out T result) where T:class{result=GetComponent<T>();return result!=null;}
        public void SetActive(bool value)
        {
            if(value==activeSelf)return;activeSelf=value;
            if(!value)foreach(var member in members.ToArray())
                member.GetType().GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(member,null);
        }
    }
    public class MonoBehaviour:Object
    {
        public GameObject gameObject;public Transform transform=>gameObject.transform;
        public MonoBehaviour(){gameObject=new GameObject();gameObject.members.Add(this);}
        public bool isActiveAndEnabled=>gameObject.activeSelf;
    }
    public static class Debug{public static void LogWarning(string s,Object context){} public static void LogException(Exception e,Object context=null){}}
}
namespace UnityEngine.UI
{
    public class Graphic:UnityEngine.MonoBehaviour{public bool raycastTarget=true;}
    public class Button:UnityEngine.MonoBehaviour
    {
        public bool interactable=true;public ClickEvent onClick=new ClickEvent();
        public class ClickEvent
        {
            public event Action Click;
            public void AddListener(Action action){Click+=action;}
            public void RemoveListener(Action action){Click-=action;}
            public void Invoke(){Click?.Invoke();}
        }
    }
}
namespace TMPro{public class TMP_Text:UnityEngine.MonoBehaviour{public string text;}}
public enum DraftRerollPresentation{NotPresented,Completed,Cancelled,TechnicalFailure}
public enum DraftResultType{TowerDraft}
public class TowerDefinition:UnityEngine.Object{public UnityEngine.GameObject TowerPrefab=new UnityEngine.GameObject();}
public class DraftResult{public bool IsValid=true;public DraftResultType ResultType;public TowerDefinition TowerDefinition=new TowerDefinition();public string DisplayName="test";}
public class TowerContentUIItem:UnityEngine.MonoBehaviour
{
    public bool Valid=true;public Action<DraftResult> Selection;
    public bool TryValidateReferences(out string reason){reason="";return Valid;}
    public bool TryInitializeSelectable(DraftResult result,Action<DraftResult> selected){Selection=selected;return Valid&&result.IsValid;}
}
public class ToastUI:UnityEngine.MonoBehaviour
{
    public int Plays,Cancels;public string Message;public Action<ToastUI> Complete;
    public bool TryPlay(string message,Action<ToastUI> complete,out string reason){Message=message;Complete=complete;Plays++;reason="";return true;}
    public void Cancel(){Cancels++;}
}
class UITests
{
    static int checks;
    static void Check(bool condition,string reason){checks++;if(!condition)throw new Exception(reason);}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Main()
    {
        var ui=new DraftUI();var root=ui.gameObject;root.members.Add(new UnityEngine.UI.Graphic());
        var active=new UnityEngine.UI.Button();var inactive=new UnityEngine.UI.Button();var text=new TMPro.TMP_Text();
        text.transform.SetParent(active.transform,false);
        var prefab=new TowerContentUIItem();var container=new UnityEngine.GameObject();var toast=new ToastUI();
        Set(ui,"rootObject",root);Set(ui,"draftItemContainer",container.transform);Set(ui,"towerDraftItemPrefab",prefab.gameObject);
        Set(ui,"rerollActiveButton",active);Set(ui,"rerollInactiveButton",inactive);Set(ui,"rerollCountText",text);
        Set(ui,"toastPrefab",toast);Set(ui,"toastContainer",root.transform);
        var created=new List<TowerContentUIItem>();bool fail=false;
        UnityEngine.Object.Factory=(template,parent)=>
        {
            if(template is ToastUI){var t=new ToastUI();t.transform.SetParent(parent,false);return t;}
            var item=new TowerContentUIItem{Valid=!fail};item.transform.SetParent(parent,false);created.Add(item);return item.gameObject;
        };
        ui.BeginBattle();var choices=new[]{new DraftResult(),new DraftResult(),new DraftResult()};int selected=0,rolls=0;
        Check(ui.TryOpenDraft(choices,x=>selected++,out _),"open actual DraftUI");
        ui.BindReroll(2,true,()=>rolls++);
        Check(active.gameObject.activeSelf&&!inactive.gameObject.activeSelf&&text.text=="2","positive balance dual buttons");
        active.onClick.Invoke();Check(rolls==1,"active bound once");
        ui.BindReroll(1,true,()=>rolls++);active.onClick.Invoke();Check(rolls==2,"rebinding no duplicate callback");
        ui.BindReroll(0,true,()=>rolls++);inactive.onClick.Invoke();
        Check(!active.gameObject.activeSelf&&inactive.gameObject.activeSelf&&inactive.interactable&&rolls==2,"exhausted feedback without action");
        ui.ShowNoOtherChoices();var instance=Get<ToastUI>(ui,"toastInstance");ui.ShowNoOtherChoices();
        Check(ReferenceEquals(instance,Get<ToastUI>(ui,"toastInstance"))&&instance.Plays==2,"one replayed toast");
        Check(instance.Message=="No other draft choices available.","Draft owns message");
        fail=true;Check(!ui.TryPrepareChoices(choices,x=>{},out _,out _)&&ui.IsOpen,"failed preparation preserves window");
        Check(!created[0].gameObject.Destroyed,"old views retained");fail=false;
        Check(ui.TryPrepareChoices(choices,x=>selected+=10,out var prepared,out _),"prepare new views");
        Check(!created[created.Count-1].transform.parent.gameObject.activeSelf,"prepared batch hidden");
        int commits=0;Check(ui.CanCommitChoices(prepared),"commit validation");ui.CommitChoiceOwnership(prepared);commits++;Check(ui.PresentChoices(prepared,out _)==DraftRerollPresentation.Completed,"commit prepared set");prepared.Dispose();
        Check(commits==1&&created[0].gameObject.Destroyed,"single commit retires old views");
        Check(!ui.CanCommitChoices(prepared),"batch single use");
        Check(ui.TryPrepareChoices(choices,x=>{},out prepared,out _),"prepare before cancellation");ui.CloseDraft();
        Check(!ui.CanCommitChoices(prepared)&&commits==1,"close invalidates prepared batch");prepared.Dispose();
        Check(instance.Cancels==1&&instance.gameObject.Destroyed,"close cancels and removes toast");
        ui.BeginBattle();ui.TryOpenDraft(choices,x=>{},out _);ui.TryPrepareChoices(choices,x=>{},out prepared,out _);
        ui.CommitChoiceOwnership(prepared);ui.StopBattle();Check(ui.PresentChoices(prepared,out _)==DraftRerollPresentation.Cancelled&&!ui.IsOpen,"cancellation during commit cannot reopen");prepared.Dispose();
        Console.WriteLine("PASS actual DraftUI with native boundaries: "+checks+" assertions");
    }
}
