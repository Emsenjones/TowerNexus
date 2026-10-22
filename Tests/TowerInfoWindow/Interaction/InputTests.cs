using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
namespace UnityEngine {
 public class Object {static int n;int id=++n;public int GetInstanceID()=>id;}
 public class MonoBehaviour:Object {public bool isActiveAndEnabled=true;public GameObject gameObject=new GameObject();public Transform transform=new Transform();}
 public class GameObject {public bool activeInHierarchy=true;}
 public class Transform {public Vector3 position,up=new Vector3(0,1,0);public Quaternion rotation;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 ProjectOnPlane(Vector3 a,Vector3 n)=>new Vector3(a.x,0,a.z);}
 public struct Quaternion {}
 public struct LayerMask {public int value;public static implicit operator LayerMask(int n)=>new LayerMask{value=n};}
 public static class Debug {public static void LogWarning(string message,object context){}}
 public class SerializeField:Attribute {} public class HeaderAttribute:Attribute {public HeaderAttribute(string title){}}
 public class Camera:MonoBehaviour {public Ray ScreenPointToRay(Vector2 p)=>new Ray{Position=p};}
 public struct Ray {public Vector2 Position;public Vector3 GetPoint(float e)=>new Vector3(Position.x,0,Position.y);}
 public struct Plane {public Plane(Vector3 a,Vector3 b){}public bool Raycast(Ray r,out float enter){enter=1;return true;}}
 public class Collider:MonoBehaviour {public bool enabled=true;public TowerInstance Owner;public T GetComponentInParent<T>() where T:class=>Owner as T;}
 public class BoxCollider:Collider {}
 public enum QueryTriggerInteraction {Collide}
 public struct RaycastHit {public Collider collider;public float distance;}
 public static class Physics {public static Func<Vector2,RaycastHit[]> Hits;public static RaycastHit[] RaycastAll(Ray r,float distance,LayerMask mask,QueryTriggerInteraction triggers)=>Hits(r.Position);}
 public static class Mathf {public static bool Approximately(float a,float b)=>Math.Abs(a-b)<0.0001f;}

}
namespace UnityEngine.UI {public class GraphicRaycaster {}}
namespace UnityEngine.EventSystems {
 public class EventSystem:MonoBehaviour {public static EventSystem current;public bool UI;public void RaycastAll(PointerEventData p,List<RaycastResult> results){if(UI)results.Add(new RaycastResult{module=new UnityEngine.UI.GraphicRaycaster()});}}
 public class PointerEventData {public PointerEventData(EventSystem s){}public int pointerId;public Vector2 position;}
 public struct RaycastResult {public object module;}
}
namespace Unity.Cinemachine {
 public struct LensSettings {public float OrthographicSize;}
 public class CinemachineCamera:MonoBehaviour {public LensSettings Lens=new LensSettings{OrthographicSize=5};public void ForceCameraPosition(Vector3 p,Quaternion q){transform.position=p;}}
 public class CinemachineBrain:MonoBehaviour {public Camera OutputCamera;public object ActiveVirtualCamera;}
 public class CinemachineConfiner3D:MonoBehaviour {public float SlowingDistance;public BoxCollider BoundingVolume;public bool enabled=true,IsValid=true;}
 public static class CinemachineCore {public class Event {public void AddListener(Action<CinemachineBrain> a){}public void RemoveListener(Action<CinemachineBrain> a){}}public static Event CameraUpdatedEvent=new Event();}
}
public enum GameFlowState {Battle,Other}
public class GameFlowController {public event Action<GameFlowState> OnStateChanged;public GameFlowState CurrentState=GameFlowState.Battle;}
public class MapGeneratorBehaviour {public MapCameraBoundary CameraBoundary;public Transform CameraDefaultPose=new Transform(),NodesRoot=new Transform();}
public class MapCameraBoundary:MonoBehaviour {public BoxCollider BoundaryCollider=new BoxCollider();public bool ContainsPoint(Vector3 p)=>true;}
public class BattleCombatBinding {public bool IsOpenForRead=true;}
public class TowerInstance:MonoBehaviour {public object RuntimeIdentity=new object();}
internal class TowerPlacementSubmission {public bool CanStartOperation=true;public HashSet<TowerInstance> Members=new HashSet<TowerInstance>();public bool OwnsDeployedTower(TowerInstance t)=>Members.Contains(t);}
internal class BattleModalPauseAuthority {public bool CanAcquire=true;}
public class TowerPlacementController {public Camera PlacementCamera;public bool IsAvailableForInspection=true;public void BindBattlefieldInput(CameraPanController c){}}
public class BattleHUDUI:MonoBehaviour {internal BattleCombatBinding BoundBattle;internal bool HasTowerInspectionBinding(BattleCombatBinding b,TowerPlacementSubmission m,BattleModalPauseAuthority p)=>Clears==0&&(BoundBattle==null||ReferenceEquals(BoundBattle,b));public bool IsDraftOpen,IsTowerInspectionBusy,IsExitConfirmationBusy;internal bool IsBattleModalBusy=>IsDraftOpen||IsTowerInspectionBusy||IsExitConfirmationBusy;internal bool IsDraftSessionBusy=>IsDraftOpen;internal ulong BattlefieldInputRevision;internal event Action OnBattlefieldInputInvalidated;public int Opens,Clears;public TowerInstance LastOpened;public bool TryOpenTowerInfo(TowerInstance t,out string r){r="";LastOpened=t;Opens++;IsTowerInspectionBusy=true;Invalidate();return true;}public void Invalidate(){BattlefieldInputRevision++;OnBattlefieldInputInvalidated?.Invoke();}public void CancelTowerInspection(){IsTowerInspectionBusy=false;}public void ClearTowerInspectionBinding(){Clears++;CancelTowerInspection();}}
class InputTests {
 static int checks;static void Check(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static void Call(object o,string n,params object[] v)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,v);
 class F {
  internal CameraPanController C=new CameraPanController();internal BattleHUDUI H=new BattleHUDUI();internal TowerInstance A=new TowerInstance(),B=new TowerInstance();internal BattleCombatBinding Battle=new BattleCombatBinding();internal MapGeneratorBehaviour Map=new MapGeneratorBehaviour();internal UnityEngine.EventSystems.EventSystem E=new UnityEngine.EventSystems.EventSystem();internal TowerPlacementController P=new TowerPlacementController();internal BattleModalPauseAuthority Pause=new BattleModalPauseAuthority();internal Unity.Cinemachine.CinemachineCamera View=new Unity.Cinemachine.CinemachineCamera();internal Unity.Cinemachine.CinemachineConfiner3D Confiner=new Unity.Cinemachine.CinemachineConfiner3D();
  internal F(bool commit=true){Mouse.current=new Mouse();Touch.activeTouches=new Touch[0];Mouse.current.leftButton.wasPressedThisFrame=Mouse.current.leftButton.wasReleasedThisFrame=Mouse.current.leftButton.isPressed=false;Map.CameraBoundary=new MapCameraBoundary();var camera=new Camera();P.PlacementCamera=camera;Confiner.gameObject=View.gameObject;var brain=new Unity.Cinemachine.CinemachineBrain{OutputCamera=camera,ActiveVirtualCamera=View};Set(C,"outputCamera",camera);Set(C,"cinemachineBrain",brain);Set(C,"cinemachineGameplayCamera",View);Set(C,"cinemachineConfiner",Confiner);Set(C,"gameFlowController",new GameFlowController());Set(C,"battleHUDUI",H);Set(C,"towerPlacementController",P);Set(C,"eventSystem",E);UnityEngine.EventSystems.EventSystem.current=E;var members=new TowerPlacementSubmission();members.Members.Add(A);members.Members.Add(B);Physics.Hits=p=>new[]{new RaycastHit{collider=new Collider{Owner=p.x>100?B:A},distance=1}};
   Check(C.TryStageMapBinding(Map,Map.CameraBoundary,Map.CameraDefaultPose,out _),"stage Map");Check(C.TryBindInspection(Battle,Map,members,Pause,out _),"bind against staged Map");Check(C.ActiveMap==null&&C.IsInspectionBindingReady(Battle,false)&&!C.IsInspectionBindingReady(Battle,true),"preparation accepts staged but begin requires committed");if(commit)Check(C.TryCommitStagedMapBinding(Map,Map.CameraBoundary,Map.CameraDefaultPose,out _),"commit Camera");}
  internal void Press(float x=0){Mouse.current.leftButton.wasPressedThisFrame=true;Mouse.current.leftButton.wasReleasedThisFrame=false;Mouse.current.leftButton.isPressed=true;Mouse.current.position.value=new Vector3(x,0,0);Call(C,"Update");Mouse.current.leftButton.wasPressedThisFrame=false;}
  internal void Move(float x){Mouse.current.position.value=new Vector3(x,0,0);Call(C,"Update");}
  internal void Release(float x=0){Mouse.current.leftButton.wasReleasedThisFrame=true;Mouse.current.leftButton.isPressed=false;Mouse.current.position.value=new Vector3(x,0,0);Call(C,"Update");Mouse.current.leftButton.wasReleasedThisFrame=false;}
 }
 static void Main(){
  var exitCase=new F();exitCase.H.IsExitConfirmationBusy=true;exitCase.Press();exitCase.Move(20);exitCase.Release();
  Check(!exitCase.C.IsPointerGestureOwned&&!exitCase.C.IsPanning&&exitCase.H.Opens==0,"exit busy blocks Camera press/pan/inspection before pause acquisition");
  exitCase=new F();exitCase.Press();exitCase.H.Invalidate();exitCase.H.IsExitConfirmationBusy=true;exitCase.Move(20);exitCase.H.IsExitConfirmationBusy=false;exitCase.Release();
  Check(exitCase.H.Opens==0&&!exitCase.C.IsPointerGestureOwned,"exit opening and closing retire the original pointer without replay");
  var f=new F();f.Press();Check(f.C.IsPointerGestureOwned&&!f.C.IsPanning,"press candidate no pan");f.Move(10);Check(!f.C.IsPanning&&f.View.transform.position.x==0,"threshold boundary holds Camera");f.Release(10);Check(f.H.Opens==1&&!f.C.IsPointerGestureOwned,"tap release opens once");
  f=new F();f.Press();f.Move(11);Check(f.C.IsPanning&&f.View.transform.position.x==0,"crossing sets baseline with no jump");f.Move(12);Check(f.View.transform.position.x==-1,"pan preserves direct manipulation");f.Move(0);f.Release();Check(f.H.Opens==0,"drag return never becomes tap");
  f=new F();f.Press();f.Release(11);Check(f.H.Opens==0,"last sample beyond threshold rejects");
  f=new F();f.Press();f.E.UI=true;f.Release();Check(f.H.Opens==0,"release over UI");
  f=new F();f.E.UI=true;f.Press();f.E.UI=false;f.Release();Check(f.H.Opens==0,"press over UI never owns release");
  f=new F();Set(f.C,"tapMovementThresholdPixels",200f);f.Press();f.Release(101);Check(f.H.Opens==0,"another Tower at release");
  f=new F();f.Press();f.H.Invalidate();f.H.IsTowerInspectionBusy=true;f.H.CancelTowerInspection();f.Release();Check(f.H.Opens==0,"modal opens/closes between updates synchronously retires candidate");
  f=new F();f.Press();f.H.Invalidate();f.Release();Check(f.H.Opens==0,"failed modal opening also invalidates gesture");
  f=new F();f.Press();f.A.RuntimeIdentity=new object();f.Release();Check(f.H.Opens==0,"same Tower new lifecycle rejected");
  f=new F();f.P.IsAvailableForInspection=false;f.Press();Check(!f.C.IsPointerGestureOwned,"pending outer placement cleanup rejects press");
  f=new F();f.Press();Call(f.C,"OnApplicationFocus",false);f.Release();Check(f.H.Opens==0,"focus cancellation");
  f=new F();f.Press();f.Battle.IsOpenForRead=false;f.Release();Check(f.H.Opens==0,"outgoing Battle rejects release");
  f=new F(false);f.Confiner.IsValid=false;Check(!f.C.TryCommitStagedMapBinding(f.Map,f.Map.CameraBoundary,f.Map.CameraDefaultPose,out _),"Camera commit failure after Battle preparation");f.C.ClearMapBinding(f.Map,f.Map.CameraBoundary,f.Map.CameraDefaultPose);Check(!f.C.IsInspectionBindingReady(f.Battle,false)&&f.H.Clears==1,"rollback clears staged input/HUD bindings");
  f=new F();Touch.activeTouches=new[]{new Touch{touchId=1,phase=TouchPhase.Began,screenPosition=new Vector3(0,0,0)}};Call(f.C,"Update");Touch.activeTouches=new[]{new Touch{touchId=1,phase=TouchPhase.Stationary,screenPosition=new Vector3(0,0,0)},new Touch{touchId=2,phase=TouchPhase.Ended,screenPosition=new Vector3(0,0,0)}};Call(f.C,"Update");Check(f.H.Opens==0&&f.C.IsPointerGestureOwned,"extra touch ignored");Touch.activeTouches=new[]{new Touch{touchId=1,phase=TouchPhase.Ended,screenPosition=new Vector3(0,0,0)}};Call(f.C,"Update");Check(f.H.Opens==1,"owned touch release opens");f.H.CancelTowerInspection();Touch.activeTouches=new Touch[0];Mouse.current.leftButton.wasPressedThisFrame=Mouse.current.leftButton.isPressed=true;Call(f.C,"Update");Check(!f.C.IsPointerGestureOwned,"touch-emulated Mouse suppressed");
  f=new F();f.Press();f.C.ClearInspectionBinding();f.Release();Check(f.H.Opens==0&&!f.C.IsInspectionBindingReady(f.Battle,false),"explicit revoke cancels pointer");
  f=new F();f.Press();f.Move(11);f.Move(12);var brain=(Unity.Cinemachine.CinemachineBrain)typeof(CameraPanController).GetField("cinemachineBrain",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(f.C);brain.OutputCamera.transform.position=new Vector3(0,0,0);Call(f.C,"HandleCinemachineCameraUpdated",brain);f.Move(11);Check(f.View.transform.position.x==1,"rendered clamp rebuilds baseline and permits immediate reversal");f.Release(11);
  f=new F();Physics.Hits=p=>new[]{new RaycastHit{collider=new Collider{Owner=new TowerInstance()},distance=0},new RaycastHit{collider=new Collider{Owner=f.B},distance=2},new RaycastHit{collider=new Collider{Owner=f.A},distance=1}};f.Press();f.Release();Check(f.H.LastOpened==f.A,"nearest deployed Tower wins; uncommitted collider ignored");
  f=new F();Set(f.C,"tapMovementThresholdPixels",float.NaN);Check(!f.C.TryValidateStableReferences(out _),"nonfinite threshold rejected");Set(f.C,"tapMovementThresholdPixels",10f);Set(f.C,"towerSelectionMask",(LayerMask)0);Check(!f.C.TryValidateStableReferences(out _),"empty selection mask rejected");
  f=new F();f.H.IsTowerInspectionBusy=true;f.H.BoundBattle=new BattleCombatBinding();Call(f.C,"OnDisable");Check(f.H.IsTowerInspectionBusy,"outgoing Camera disable cannot cancel a newer HUD Battle");
  f=new F();Mouse.current=null;Call(f.C,"Update");Check(!f.C.IsPointerGestureOwned,"no mouse is safe");
  f=new F();f.Press();Mouse.current.added=false;Call(f.C,"Update");Check(!f.C.IsPointerGestureOwned&&f.H.Opens==0,"removed mouse cancels without tap");
  f=new F();f.Press();var ownedMouse=Mouse.current;Mouse.current=new Mouse();ownedMouse.leftButton.wasReleasedThisFrame=true;ownedMouse.leftButton.isPressed=false;Call(f.C,"Update");Check(f.H.Opens==1,"release follows captured mouse not current device");
  f=new F();Touch.activeTouches=new[]{new Touch{touchId=5,phase=TouchPhase.Began}};Call(f.C,"Update");Touch.activeTouches=new[]{new Touch{touchId=5,phase=TouchPhase.Canceled}};Call(f.C,"Update");Check(!f.C.IsPointerGestureOwned&&f.H.Opens==0,"touch cancellation never taps");
  f=new F();Touch.activeTouches=new[]{new Touch{touchId=5,phase=TouchPhase.Began}};Call(f.C,"Update");Touch.activeTouches=new Touch[0];Call(f.C,"Update");Check(!f.C.IsPointerGestureOwned&&f.H.Opens==0,"missing owned touch cancels");
  int leases=UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.leases;Call(f.C,"OnEnable");Call(f.C,"OnDisable");Check(UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.leases==leases,"EnhancedTouch lifecycle balanced");
  Console.WriteLine("PASS whole production Camera pointer/binding integration: "+checks+" assertions (native input, physics, Cinemachine and HUD endpoints doubled)");
 }
}
