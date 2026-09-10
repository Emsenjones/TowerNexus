#!/usr/bin/env python3
"""Production DraftUI + TowerContentUIItem; explicit effective-activation/graphics boundaries."""
from pathlib import Path
import tempfile,subprocess
root=Path(__file__).resolve().parents[3]
s=(root/'Tests/Reroll/Task002/UITests.cs').read_text()
a=s.index('public class TowerContentUIItem:');b=s.index('public class ToastUI:',a);s=s[:a]+s[b:]
s=s.replace('public enum DraftResultType{TowerDraft}', 'public enum DraftResultType{TowerDraft,TowerUpgradeDraft}\npublic enum TowerUpgradeLayer{Basic,Behaviour,Elemental}\npublic class TowerUpgradeDefinition{public TowerUpgradeLayer UpgradeLayer;}')
s=s.replace('public class DraftResult{', 'public class DraftResult{public TowerUpgradeDefinition TowerUpgradeDefinition;public UnityEngine.Sprite Icon;public string Description="description";')
s=s.replace('public bool isActiveAndEnabled=>gameObject.activeSelf;', 'public bool isActiveAndEnabled=>gameObject.activeInHierarchy;')
s=s.replace('''            if(value==activeSelf)return;activeSelf=value;
            if(!value)foreach(var member in members.ToArray())
                member.GetType().GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(member,null);''','''            bool before=activeInHierarchy;activeSelf=value;
            if(before!=activeInHierarchy)foreach(var member in members.ToArray())
                member.GetType().GetMethod(activeInHierarchy?"OnEnable":"OnDisable",BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(member,null);''')
s=s.replace('public static void LogWarning(string s,Object context){}', 'public static void LogError(string s,Object context){} public static void LogWarning(string s,Object context){}')
s=s.replace('    static void Main()', '''    static TowerContentUIItem NewItem(bool valid=true)
    {
        var item=new TowerContentUIItem();
        Set(item,"iconImage",valid?new UnityEngine.UI.Image():null);Set(item,"iconBackgroundImage",new UnityEngine.UI.Image());
        Set(item,"nameText",new TMPro.TMP_Text());Set(item,"descriptionText",new TMPro.TMP_Text());
        foreach(var field in typeof(TowerContentUIItem).GetFields(BindingFlags.NonPublic|BindingFlags.Instance))
            if(field.FieldType==typeof(UnityEngine.Sprite))field.SetValue(item,new UnityEngine.Sprite());
        var press=new UnityEngine.RectTransform{gameObject=new UnityEngine.GameObject()};press.SetParent(item.transform,false);
        Set(item,"pressedContentRoot",press);return item;
    }
    static void Main()''')
s=s.replace('new TowerContentUIItem()', 'NewItem()', 1) if False else s
s=s.replace('var prefab=new TowerContentUIItem();', 'var prefab=NewItem();').replace('var item=new TowerContentUIItem{Valid=!fail};', 'var item=NewItem(!fail);')
s=s.replace('Check(!created[created.Count-1].transform.parent.gameObject.activeSelf,"prepared batch hidden");', '''Check(!created[created.Count-1].transform.parent.gameObject.activeSelf,"prepared batch hidden");
        created[created.Count-1].OnPointerClick(new UnityEngine.EventSystems.PointerEventData());
        Check(selected==0,"real hidden card rejects clicks");''')
s=s.replace('Check(commits==1&&created[0].gameObject.Destroyed,"single commit retires old views");', '''Check(commits==1&&created[0].gameObject.Destroyed,"single commit retires old views");
        created[created.Count-1].OnPointerClick(new UnityEngine.EventSystems.PointerEventData());
        Check(selected==10,"real activated card retains selection binding");
        created[0].OnPointerClick(new UnityEngine.EventSystems.PointerEventData());
        Check(selected==10,"real retired card clears selection binding");''')
s+='''
namespace UnityEngine
{
    public class Sprite:Object{}
    public class RectTransform:Transform { public Vector2 anchoredPosition; }
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);}
}
namespace UnityEngine.UI{public class Image:Graphic {public UnityEngine.Sprite sprite; public bool enabled=true;}}
namespace UnityEngine.Serialization{public class FormerlySerializedAs:Attribute{public FormerlySerializedAs(string name){}}}
namespace UnityEngine.EventSystems
{
    public class PointerEventData{public enum InputButton{Left,Right} public InputButton button;}
    public interface IPointerDownHandler{} public interface IPointerUpHandler{} public interface IPointerClickHandler{}
    public interface IPointerEnterHandler{} public interface IPointerExitHandler{}
}
'''
with tempfile.TemporaryDirectory(prefix='towernexus-real-draft-card-') as folder:
    source=Path(folder)/'Boundary.cs';source.write_text(s);exe=Path(folder)/'Cards.exe'
    subprocess.run(['csc','-nologo','-langversion:8.0','-nowarn:0649',f'-out:{exe}',str(source),
                    str(root/'Assets/Scripts/TowerDeployment/DraftUI.cs'),str(root/'Assets/Scripts/TowerDeployment/TowerContentUIItem.cs')],check=True)
    subprocess.run(['mono',str(exe)],check=True)
