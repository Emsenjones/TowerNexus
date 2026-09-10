#!/usr/bin/env python3
"""Static serialized migration checks; Unity import/visual behavior is a separate gate."""
from pathlib import Path
import re, subprocess
root = Path(__file__).resolve().parents[3]
baseline = '1a044e9'
asset = 'Assets/Art/Prefab/UI/UiPrefab_DamageNumberItem.prefab'
def old(path):
    return subprocess.check_output(['git','show',baseline+':'+path],cwd=root,text=True)
def blocks(text):
    return {m.group(2): m.group(0) for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)}
def payload(text):
    match=re.search(r'^( +)animationSteps:\n(?P<data>.*?)(?=^--- !u!|^  [a-zA-Z]|\Z)',text,re.M|re.S)
    indentation=len(match.group(1))
    return '  animationSteps:\n'+'\n'.join(line[indentation-2:] for line in match.group('data').rstrip('\n').splitlines())+'\n'
before=old(asset); after=(root/asset).read_text()
assert payload(before)==payload(after), 'Authored animation payload changed'
assert old(asset+'.meta')==(root/(asset+'.meta')).read_text(), 'Prefab GUID/importer changed'
assert old('Assets/Scripts/Monster/DamageNumberAnimationStep.cs.meta')==(root/'Assets/Scripts/UI/UIAnimationStep.cs.meta').read_text()
assert old('Assets/Scripts/Monster/DamageNumberUI.cs.meta')==(root/'Assets/Scripts/Monster/DamageNumberUI.cs.meta').read_text()
bb,ab=blocks(before),blocks(after)
# User-authored Unity layout after the migration and successful DamageNumber visual test.
# Keep migration identity/animation oracles; permit only these reviewed layout values.
layout_updates = {
    '1901699748267368699': {
        'm_AnchorMin': '{x: 0, y: 1}', 'm_AnchorMax': '{x: 0, y: 1}',
        'm_AnchoredPosition': '{x: 0, y: -34.295}', 'm_SizeDelta': '{x: 101.58, y: 68.59}'},
    '4721239818144917664': {'m_SizeDelta': '{x: 0, y: 0}'}}
for identity, value in bb.items():
    for field, authored in layout_updates.get(identity, {}).items():
        value = re.sub(r'^  '+field+r':.*$', '  '+field+': '+authored, value, flags=re.M)
    if identity not in ('362574065864791292','8466065171535225167'):
        assert ab[identity]==value, 'Existing hierarchy/component changed: '+identity
for field in ['damageText','previewDamageValue','randomOffsetRangeX','randomOffsetRangeY']:
    pattern=r'^  '+field+r':.*$'
    assert re.search(pattern,before,re.M).group()==re.search(pattern,after,re.M).group()
assert '  timeMode: 1' in after
assert '  animationPlayer:\n' in ab['8466065171535225167']
assert len(ab)==len(bb), 'No extra player component remains'
playerguid=re.search(r'guid: (\w+)',(root/'Assets/Scripts/UI/UIAnimationPlayer.cs.meta').read_text()).group(1)
toastguid=re.search(r'guid: (\w+)',(root/'Assets/Scripts/UI/ToastUI.cs.meta').read_text()).group(1)
for path in [asset,'Assets/Art/Prefab/UI/UiPrefab_ToastItem.prefab']:
    text=(root/path).read_text(); table=blocks(text)
    ids=re.findall(r'^--- !u!\d+ &(\d+)',text,re.M)
    assert len(ids)==len(set(ids)), 'Duplicate object fileID'
    for identity in re.findall(r'\{fileID: (\d+)\}',text):
        assert identity=='0' or identity in table, 'Unresolved local fileID '+identity
    for goid,value in table.items():
        if '\nGameObject:\n' not in value: continue
        for component in re.findall(r'component: \{fileID: (\d+)\}',value):
            assert 'm_GameObject: {fileID: '+goid+'}' in table[component], 'Component owner mismatch'
    assert playerguid not in text, 'Player must be serialized inline, not a component'
    if 'Toast' in path:
        assert toastguid in text and '  timeMode: 0' in text
        assert 'm_RaycastTarget: 1' not in text and 'm_BlocksRaycasts: 1' not in text
        assert 'messageText: {fileID: 2902563685988067440}' in text
        assert '  damageText:' not in text
print('PASS: exact damage animation payload, existing GUIDs/hierarchy/references with reviewed user layout, and Toast prefab ownership/input flags.')
