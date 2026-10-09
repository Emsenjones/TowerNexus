"""Static serialization/reference checks, not native Unity import/visual acceptance."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[3]
count=0
def check(value,label):
 global count
 assert value,label
 count+=1
def blocks(s):
 pairs=[(m[2],m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^---|\Z)',s,re.M|re.S)]
 check(len({id for id,b in pairs})==len(pairs),'unique local object IDs')
 return dict(pairs)
s=(root/'Assets/Art/Prefab/Main/Prefab_GameRuntime.prefab').read_text();b=blocks(s)
view=next(x for x in b.values() if 'Assembly-CSharp::TowerInfoWindow\n' in x)
refs={m[1]:m[2] for m in re.finditer(r'^  (\w+Text): \{fileID: (-?\d+)\}',view,re.M)}
check(len(refs)==6 and len(set(refs.values()))==6,'all six stat text refs distinct')
check(all(id in b for id in refs.values()),'local text IDs resolve')
coordinator=next(x for x in b.values() if 'Assembly-CSharp::BattleRuntimeCoordinator\n' in x)
camera=next(x for x in b.values() if 'Assembly-CSharp::CameraPanController\n' in x)
hud=next(x for x in b.values() if 'Assembly-CSharp::BattleHUDUI\n' in x)
check('towerInfoWindow: {fileID: 7833152137912921819}' in hud,'HUD assigned view')
check('battleHUDUI: {fileID: 4430054556091105857}' in coordinator and 'cameraPanController: {fileID: 232661449897972293}' in coordinator,'production coordinator references')
check('m_Bits: 8192' in camera and 'tapMovementThresholdPixels: 10' in camera,'selection configuration')
item=(root/'Assets/Art/Prefab/UI/UiPrefab_TowerUpgradeInfoItem.prefab').read_text()
icon=re.search(r'iconImage: \{fileID: (\d+)',item)[1];background=re.search(r'backgroundImage: \{fileID: (\d+)',item)[1]
check(icon!=background,'icon and background separate')
check('TowerSelection' in (root/'ProjectSettings/TagManager.asset').read_text(),'dedicated layer authored')
for p in (root/'Assets/Art/Prefab/TowerBase').glob('*.prefab'):
 data=p.read_text();bb=blocks(data)
 check('m_Name: TowerSelection' in bb['9011203001001'] and 'm_Layer: 13' in bb['9011203001001'],'proxy GameObject/layer '+p.name)
 check('m_IsTrigger: 1' in bb['9011203001003'] and 'm_Enabled: 1' in bb['9011203001003'],'trigger selection '+p.name)
 parent=re.search(r'm_Father: \{fileID: (-?\d+)',bb['9011203001002'])[1]
 check(parent in bb and '{fileID: 9011203001002}' in bb[parent],'stable hierarchy '+p.name)
check('QueryTriggerInteraction.Ignore' in (root/'Assets/Scripts/TowerDeployment/TowerPlacementController.cs').read_text(),'placement ignores selection triggers')
print(f'PASS serialization and stable selection assets: {count} checks (no native import/physics acceptance)')
