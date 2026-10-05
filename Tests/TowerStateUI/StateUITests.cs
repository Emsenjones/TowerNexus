using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using UObject = UnityEngine.Object;

class StateUITests
{
    static int count;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Run(string name, Action test)
    { test();UObject.Flush();Check(Canvas.Subscribers==0,"leaked Canvas handler");count++;Console.WriteLine("PASS "+name); }
    internal static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    static T Get<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    sealed class Fixture : IDisposable
    {
        public TowerStateUIManager Manager = new TowerStateUIManager();
        public TowerPlacementSubmission Source = new TowerPlacementSubmission();
        public Camera Camera = new Camera();
        public List<TowerStateUIItem> Created = new List<TowerStateUIItem>();
        public bool InvalidText;
        public Action DuringInstantiate;
        public Fixture(bool bind=true)
        {
            var canvas=new Canvas();Manager.transform.parent=canvas.transform;
            Set(Manager,"stateUIItemPrefab",new TowerStateUIItem());
            UObject.Factory=(original,parent)=>
            {
                var item=new TowerStateUIItem();item.transform.parent=parent;
                var display=new GameObject();display.transform.parent=item.transform;
                var text=new TMP_Text();text.transform.parent=display.transform;
                Set(item,"levelDisplay",display.transform);Set(item,"levelText",InvalidText ? null : text);
                Created.Add(item);DuringInstantiate?.Invoke();return item;
            };
            if(bind) Manager.Bind(Source,Camera);
        }
        public int Count => Get<IDictionary>(Manager,"items").Count;
        public TowerInstance Tower()
        {
            var tower=new TowerInstance();tower.gameObject.Attach(new TowerBehaviour{VisualController=new TowerVisualController{TowerModelRoot=new GameObject().transform}});return tower;
        }
        public TMP_Text Text(int i=0)=>Get<TMP_Text>(Created[i],"levelText");
        public bool Visible(int i=0)=>Get<RectTransform>(Created[i],"levelDisplay").gameObject.activeSelf;
        public void Disable() { Manager.enabled=false;UObject.Invoke(Manager,"OnDisable"); }
        public void Enable() { Manager.enabled=true;UObject.Invoke(Manager,"OnEnable"); }
        public void Dispose(){Manager.Clear();Check(Source.Subscribers==0,"leaked deployment handler");}
    }
    static void Main()
    {
        Run("initial level, duplicate deployment, accepted update and stable item",()=>{
            using(var f=new Fixture()) {var tower=f.Tower();tower.CurrentLevel=7;f.Source.Deploy(tower);f.Source.Publish(tower);
                Check(f.Count==1&&f.Created.Count==1&&f.Text().text=="7"&&tower.Subscribers==1,"duplicate/initial");
                tower.Level(10);Check(f.Text().text=="10"&&f.Count==1,"level event");f.Manager.Clear();Check(tower.Subscribers==0,"level subscription cleanup");}
        });
        Run("uncommitted, foreign and stopped deployment notifications rejected",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Publish(tower);Check(f.Count==0,"uncommitted");f.Source.Active=false;f.Source.Deploy(tower);Check(f.Count==0,"stopped event");}
        });
        Run("Stop then UI hide/restore preserves labels until Release clear",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Deploy(tower);f.Source.Active=false;Canvas.Render();Check(f.Count==1&&f.Visible(),"stop pruned");
                f.Disable();Check(!f.Visible()&&Canvas.Subscribers==0,"hide");f.Enable();Check(f.Count==1&&f.Visible(),"restore");f.Manager.Clear();f.Enable();Check(f.Count==0&&Canvas.Subscribers==0,"clear resurrected");}
        });
        Run("hidden deployment, upgrade and destruction reconcile at enable",()=>{
            using(var f=new Fixture()){var a=f.Tower();f.Source.Deploy(a);f.Disable();var b=f.Tower();f.Source.Deploy(b);b.Level(12);UObject.Destroy(a.gameObject);UObject.Flush();
                Check(f.Created.Count==1,"spawned while hidden");f.Source.Active=false;f.Enable();Check(f.Count==1&&f.Created.Count==2&&f.Text(1).text=="12"&&a.Subscribers==0,"reconcile stopped runtime");}
        });
        Run("hidden Release clears every subscription and prevents restoration",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Deploy(tower);f.Disable();var b=f.Tower();f.Source.Deploy(b);f.Manager.Clear();f.Manager.Clear();f.Enable();
                Check(f.Count==0&&tower.Subscribers==0&&f.Source.Subscribers==0&&Canvas.Subscribers==0,"hidden clear");}
        });
        Run("captured old notification cannot create after clear or same-source rebind",()=>{
            using(var f=new Fixture()){var tower=f.Tower();var old=f.Source.Capture();f.Manager.Clear();f.Source.Members.Add(tower);old(tower);Check(f.Count==0,"after clear");
                f.Manager.Bind(f.Source,f.Camera);f.Manager.Remove(tower);old(tower);Check(f.Count==0,"old callback after rebind");f.Source.Publish(tower);Check(f.Count==1,"fresh callback");}
        });
        Run("clear during instantiation disposes unregistered partial item",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.DuringInstantiate=()=>f.Manager.Clear();f.Source.Deploy(tower);Check(f.Count==0&&tower.Subscribers==0&&!f.Visible(),"partial item");}
        });
        Run("delayed old destroy does not remove replacement",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Deploy(tower);f.Manager.Remove(tower);f.Source.Publish(tower);UObject.Flush();Check(f.Count==1&&tower.Subscribers==1,"replacement lost");}
        });
        Run("external item destruction removes record and subscription",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Deploy(tower);UObject.Destroy(f.Created[0].gameObject);UObject.Flush();Check(f.Count==0&&tower.Subscribers==0,"external item");}
        });
        Run("destroyed tower is pruned even with Unity null equality",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Deploy(tower);UObject.Destroy(tower.gameObject);UObject.Flush();Canvas.Render();Check(f.Count==0&&tower.Subscribers==0,"destroyed tower leak");}
        });
        Run("viewport, camera availability, inactive tower and scaled coordinates",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Deploy(tower);Check(f.Visible(),"initial visible");
                var pos=f.Created[0].transform.localPosition;Check(pos.x==350&&pos.y==225,"screen pixels assigned directly");
                f.Camera.Viewport=new Vector3(1.1f,.5f,1);Canvas.Render();Check(!f.Visible(),"offscreen");
                f.Camera.Viewport=new Vector3(.5f,.5f,-1);Canvas.Render();Check(!f.Visible(),"behind");
                f.Camera.Viewport=new Vector3(.5f,.5f,1);f.Camera.enabled=false;Canvas.Render();Check(!f.Visible(),"camera disabled");
                f.Camera.enabled=true;tower.gameObject.SetActive(false);Canvas.Render();Check(!f.Visible(),"tower disabled");
                tower.gameObject.SetActive(true);Canvas.Render();Check(f.Visible()&&f.Count==1,"visibility recovery");}
        });
        Run("invalid numeric reference destroys partial item without changing membership",()=>{
            using(var f=new Fixture()){f.InvalidText=true;var tower=f.Tower();f.Source.Deploy(tower);Check(f.Count==0&&tower.Subscribers==0&&f.Source.Members.Count==1,"invalid item");UObject.Flush();Check(f.Created[0]==null,"partial not destroyed");}
        });
        Run("missing manager configuration subscribes nothing",()=>{
            using(var f=new Fixture(false)){Set(f.Manager,"stateUIItemPrefab",null);f.Manager.Bind(f.Source,f.Camera);Check(f.Source.Subscribers==0&&Canvas.Subscribers==0,"invalid manager");}
        });
        Run("Manager destruction while hidden clears all observers",()=>{
            using(var f=new Fixture()){var tower=f.Tower();f.Source.Deploy(tower);f.Disable();UObject.Destroy(f.Manager.gameObject);UObject.Flush();Check(tower.Subscribers==0&&f.Source.Subscribers==0&&f.Count==0,"manager destruction");}
        });
        Console.WriteLine(count+" Tower state UI managed cases passed.");
    }
}
