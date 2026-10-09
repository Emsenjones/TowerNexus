#!/usr/bin/env python3
"""Whole production View/HUD inspection/Snapshot/Binding/Pause; exact combat query/commit methods."""
from pathlib import Path
import re, subprocess, tempfile
root=Path(__file__).resolve().parents[3]
def method(source,name):
    match=re.search(r'^    (?:private|internal|public) [^\n]*\b'+name+r'\(',source,re.M)
    start=match.start(); opening=source.index('{',match.end()); depth=1;end=opening+1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
def expression(source,name):
    match=re.search(r'^    (?:private|internal) [^\n]*\b'+name+r'\(',source,re.M)
    return source[match.start():source.index(';',match.end())+1]
with tempfile.TemporaryDirectory(prefix='towernexus-inspection-') as folder:
    folder=Path(folder)
    combat=(root/'Assets/Scripts/TowerRuntimeCombat/TowerCombatBehaviour.cs').read_text()
    query=folder/'Query.cs'
    query.write_text('using UnityEngine;using System;\npartial class TowerCombatBehaviour {\n'+
        '\n'.join(method(combat,n) for n in ('TryGetInspectionStats','ApplyPreparedLevelDamageRevision','CommitPreparedUpgradeBaseline'))+'\n'+
        '\n'.join(expression(combat,n) for n in ('IsInspectionRuntimeAvailable','IsFiniteNonNegative'))+'\n}')
    hud=folder/'HUDLifecycle.cs'
    hud.write_text('using System;\npartial class BattleHUDUI {\n'+method((root/'Assets/Scripts/TowerDeployment/BattleHUDUI.cs').read_text(),'OnDisable')+'\n}')
    paths=[root/'Assets/Scripts/TowerDeployment'/n for n in ('BattleModalPauseAuthority.cs','BattleCombatBinding.cs',
        'TowerInspectionSnapshot.cs','TowerInfoWindow.cs','TowerUpgradeInfoItem.cs','BattleHUDUI.TowerInspection.cs')]
    paths+=[root/'Assets/Scripts/TowerRuntimeCombat/ResolvedTowerCombatStats.cs',query,hud,
            Path(__file__).with_name('BoundaryDoubles.cs'),Path(__file__).with_name('PresentationTests.cs')]
    exe=folder/'Presentation.exe'
    subprocess.run(['csc','-nologo','-langversion:8.0','-nowarn:0649',f'-out:{exe}',*map(str,paths)],check=True,cwd=root)
    subprocess.run(['mono',str(exe)],check=True,cwd=root)
