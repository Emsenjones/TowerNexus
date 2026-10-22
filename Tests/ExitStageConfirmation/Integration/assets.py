#!/usr/bin/env python3
"""Static authoring references; does not claim Unity raycast/rendering acceptance."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[3]
source = (root / 'Assets/Art/Prefab/Main/Prefab_GameRuntime.prefab').read_text()
records = {m[2]: (m[1], m[3]) for m in re.finditer(
    r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)', source, re.M | re.S)}

def component(script):
    guid = re.search(r'guid: ([a-f0-9]{32})', (root / script).with_suffix('.cs.meta').read_text())[1]
    matches = [(key, body) for key, (kind, body) in records.items() if kind == '114' and 'guid: ' + guid in body]
    assert len(matches) == 1, script
    return matches[0]

def ref(body, key):
    match = re.search(r'^  ' + key + r': \{fileID: (-?\d+)\}', body, re.M)
    assert match and match[1] in records, key
    return match[1]

flow_id, flow = component('Assets/Scripts/GameFlow/GameFlowController.cs')
runtime_id, runtime = component('Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs')
hud_id, hud = component('Assets/Scripts/TowerDeployment/BattleHUDUI.cs')
view_id, view = component('Assets/Scripts/TowerDeployment/ConfirmExitStageWindow.cs')
stage_id, stage = component('Assets/Scripts/Stage/StageCompositionController.cs')
assert ref(flow, 'stageCompositionController') == stage_id
assert ref(stage, 'battleRuntimeCoordinator') == runtime_id
assert ref(runtime, 'gameFlowController') == flow_id and ref(runtime, 'battleHUDUI') == hud_id
assert ref(hud, 'exitStageConfirmWindow') == view_id
entry_id = ref(hud, 'exitButton'); entry = records[entry_id][1]
assert 'UnityEngine.UI::UnityEngine.UI.Button' in entry and '  m_Interactable: 0' in entry
entry_go = records[ref(entry, 'm_GameObject')][1]
assert '  m_Name: Button_Exit\n' in entry_go
view_go = records[ref(view, 'm_GameObject')][1]
assert '  m_Name: Window_ConfirmExitStage\n' in view_go and '  m_IsActive: 0' in view_go
assert 'titleText:' not in view and 'descriptionText:' not in view
buttons = [ref(view, name) for name in ('cancelButton', 'restartButton', 'mainMenuButton')]
assert len(set(buttons)) == 3 and entry_id not in buttons
for key in [entry_id, *buttons]:
    assert re.search(r'm_OnClick:\s+m_PersistentCalls:\s+m_Calls: \[\]', records[key][1])
assert all(copy in source for copy in ('m_text: EXIT STAGE', 'm_text: RESTART', 'm_text: CANCEL',
                                     'm_text: MAIN MENU', 'm_text: Restart or return to the main menu?'))
print('PASS exit Prefab: exact Flow/Stage/coordinator/HUD/view/entry references, initial lock, distinct controls and static copy')
