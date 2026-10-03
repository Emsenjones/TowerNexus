// Controlled renderer/spatial doubles. These do not execute Unity rendering or native transforms.
using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class Object { }
 public class MonoBehaviour : Object { public Transform transform = new Transform(); }
 public class SerializeField : Attribute { }
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} public static Vector2 one=>new Vector2(1,1); }
 public struct Vector2Int { public int x,y; public Vector2Int(int a,int b){x=a;y=b;} }
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public float sqrMagnitude => x*x+y*y+z*z;
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
  public Vector3 Normalized => this*(1f/(float)Math.Sqrt(sqrMagnitude));
 }
 public struct Quaternion {
  public Vector3 Forward,Up;
  public static Quaternion identity=>LookRotation(new Vector3(0,0,1),new Vector3(0,1,0));
  public static Quaternion LookRotation(Vector3 f,Vector3 u){f=f.Normalized;var right=Vector3.Cross(u,f).Normalized;return new Quaternion{Forward=f,Up=Vector3.Cross(f,right)};}
 }
 public class Transform {
  public Vector3 position; public Quaternion rotation=Quaternion.identity; public Transform parent;
  public Vector3 up=>rotation.Up; public Vector3 forward=>rotation.Forward;
  public Matrix4x4 localToWorldMatrix=>new Matrix4x4{Position=position,Forward=forward,Up=up};
  public bool IsChildOf(Transform other){for(var p=parent;p!=null;p=p.parent)if(p==other)return true;return false;}
 }
 public struct Matrix4x4 {public Vector3 Position,Forward,Up;}
 public struct Color {public float r,g,b,a;public Color(float red,float green,float blue,float alpha){r=red;g=green;b=blue;a=alpha;}public static Color red=>new Color(1,0,0,1);public static Color white=>new Color(1,1,1,1);}
 public struct GradientColorKey {public Color color;public GradientColorKey(Color c,float t){color=c;}}
 public struct GradientAlphaKey {public float alpha;public GradientAlphaKey(float a,float t){alpha=a;}}
 public class Gradient {public GradientColorKey[] colorKeys;public GradientAlphaKey[] alphaKeys;public void SetKeys(GradientColorKey[] c,GradientAlphaKey[] a){colorKeys=c;alphaKeys=a;}}
 public class AnimationCurve {public static AnimationCurve Linear(float a,float b,float c,float d)=>new AnimationCurve();}
 public enum LineAlignment {TransformZ}
 public enum LineTextureMode {Tile}
 public static class Shader {public static int PropertyToID(string name)=>name.GetHashCode();}
 public enum TextureWrapMode {Repeat,Clamp}
 public class Texture {public TextureWrapMode wrapMode=TextureWrapMode.Repeat;}
 public class Material {
  public bool SupportsContract=true;public Texture Texture=new Texture();public Vector2 Tiling=new Vector2(3,1),Offset=new Vector2(.1f,0);public float Speed=.5f;
  public bool HasProperty(int id)=>SupportsContract;
  public Texture GetTexture(int id)=>Texture;
  public Vector2 GetTextureScale(int id)=>Tiling;
  public Vector2 GetTextureOffset(int id)=>Offset;
  public float GetFloat(int id)=>Speed;
 }
 public static class Time {public static float deltaTime,timeScale=1;}
 public static class Debug {public static void LogWarning(string value,Object context){} public static void LogException(Exception e){}}
 public class MaterialPropertyBlock {public Dictionary<int,float> Values=new Dictionary<int,float>();public void Clear()=>Values.Clear();public void SetFloat(int id,float v)=>Values[id]=v;}
 public class LineRenderer : Object {
  public Transform transform=new Transform();public Material sharedMaterial=new Material();
  public bool enabled,useWorldSpace,loop,receiveShadows; public int positionCount,numCornerVertices,numCapVertices;
  public LineAlignment alignment;public LineTextureMode textureMode;public Vector2 textureScale;
  public AnimationCurve widthCurve;public float widthMultiplier;public Gradient colorGradient=DefaultGradient();
  static Gradient DefaultGradient(){var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,1)});return g;}
  public Rendering.ShadowCastingMode shadowCastingMode;public Vector3[] Positions;public int Writes;public float FlowOffset;
  public void SetPositions(Vector3[] v){Positions=(Vector3[])v.Clone();Writes++;}
  public void SetPropertyBlock(MaterialPropertyBlock block){FlowOffset=block==null?0:block.Values[Shader.PropertyToID("_FlowOffset")];}
 }
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}}
public enum GridNodeType {Normal,Spawn,Target}
public class GridNodeBehaviour {
 public MapGeneratorBehaviour MapOwner;public UnityEngine.Vector2Int GridPosition;public UnityEngine.Vector3 WorldPosition;public GridNodeType NodeType;
}
public class MapGeneratorBehaviour {
 public UnityEngine.Transform NodesRoot=new UnityEngine.Transform();public ulong StructureRevision,WalkabilityRevision;
 public bool TryEnsureNodeIndex()=>true;
}
public class AStarPathfindingService {
 public MapGeneratorBehaviour ActiveMap;public ulong BindingRevision;public int SearchCount;public bool FailQuery;
 public IReadOnlyList<GridNodeBehaviour> FormalNodes;
 internal bool TryQueryFormalMainRoute(out MonsterMainRouteSnapshot route,out string reason){SearchCount++;route=null;reason="formal query failure";if(FailQuery)return false;route=new MonsterMainRouteSnapshot(ActiveMap,this,FormalNodes);reason=null;return true;}
}
public class TowerPlacementValidator {
 internal MapGeneratorBehaviour ActiveMap;internal AStarPathfindingService ActivePathfinding;internal ulong BindingRevision;
 public bool IsConfiguredFor(MapGeneratorBehaviour m,AStarPathfindingService p)=>m!=null&&m==ActiveMap&&p==ActivePathfinding;
}
internal class TowerPlacementTopologyPlan {
 internal IReadOnlyList<GridNodeBehaviour> AuthoritativeRoute;
}


namespace UnityEngine.Serialization { public class FormerlySerializedAsAttribute : Attribute {public FormerlySerializedAsAttribute(string name){}} }
