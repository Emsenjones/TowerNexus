#!/usr/bin/env python3
"""Serialized wiring/budget contract. Does not claim Unity import or layout validation."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[3]
for suffix,count in [('Default',0),('Lv1',1),('Lv2',1),('Lv3',2),('Lv4',3),('Lv5',4),('Lv6',5)]:
    s=(root/f'Assets/Configs/StageDefini/StageDefini_{suffix}.asset').read_text()
    assert re.findall(r'^  freeRerollCount: (\d+)$',s,re.M)==[str(count)],suffix
s=(root/'Assets/Art/Prefab/Main/Prefab_GameRuntime.prefab').read_text()
blocks={m[2]:m[3] for m in re.findall(r'(^|\n)--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
draft=blocks['6668126777542329585']
for field,fileid in [('rerollActiveButton','9109373452310609252'),('rerollInactiveButton','7175061882928073051'),('rerollCountText','468145514946939143'),('toastContainer','2591946000137446198')]:
    assert f'  {field}: {{fileID: {fileid}}}' in draft
    assert fileid in blocks
for fileid in ['9109373452310609252','7175061882928073051']:
    assert '  m_Interactable: 1' in blocks[fileid]
    assert '  m_Transition: 0' in blocks[fileid]
    assert 'm_Calls: []' in blocks[fileid]
for fileid in ['6034960126831365471','8195601797709766335']:
    assert 'ButtonPressFeedback' in blocks[fileid]
    assert 'pressedOffset: {x: 0, y: -30}' in blocks[fileid]
assert 'noOtherChoicesMessage: No other draft choices available.' in draft
assert 'toastPrefab: {fileID: 8466065171535225167, guid: 16348d0b7afb463b99b1579be05cc224, type: 3}' in draft
print('PASS serialized seven Stage budgets, dual buttons, existing feedback, child count and Toast references')
