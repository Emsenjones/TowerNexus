#!/usr/bin/env python3
"""Actual pause authority; exact production attack methods with native/session boundaries."""
from pathlib import Path
import re
import subprocess
import tempfile

root = Path(__file__).resolve().parents[3]

def method(source, name):
    match = re.search(r'^    (?:private|protected|public|internal) [^\n]*\b'+name+r'\(', source, re.M)
    start = source.index('{', match.end())
    end, depth = start + 1, 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[match.start():end].replace('protected ', 'private ')

with tempfile.TemporaryDirectory(prefix='towernexus-modal-pause-') as folder:
    folder = Path(folder)
    exe = folder/'Pause.exe'
    subprocess.run(['csc', '-nologo', '-langversion:8.0', f'-out:{exe}',
                    str(root/'Assets/Scripts/TowerDeployment/BattleModalPauseAuthority.cs'),
                    str(Path(__file__).with_name('PauseTests.cs'))], check=True)
    subprocess.run(['mono', str(exe)], check=True)
    source = (root/'Assets/Scripts/Monster/MonsterSpawner.cs').read_text()
    extracted = folder/'SpawnerMethod.cs'
    extracted.write_text('using UnityEngine; using System.Collections; using System.Collections.Generic;\npartial class SpawnerHarness {\n'+
                         method(source, 'SpawnWavesRoutine')+'\n}')
    exe = folder/'Spawner.exe'
    subprocess.run(['csc', '-nologo', '-nowarn:0649', '-langversion:8.0', f'-out:{exe}',
                    str(extracted), str(Path(__file__).with_name('SpawnerTests.cs'))], check=True)
    subprocess.run(['mono', str(exe)], check=True)
    source = (root/'Assets/Scripts/TowerRuntimeCombat/TowerCombatBehaviour.cs').read_text()
    extracted = folder/'AttackMethods.cs'
    extracted.write_text('using UnityEngine;\npartial class AttackHarness {\n'+'\n'.join(method(source, name) for name in
        ('OnAttackAnimationRelease', 'OnTowerPresentationReplaced', 'Update', 'SetWaitingForAnimationRelease',
         'SetIdle', 'StopBattle', 'DeactivateRuntimeSession', 'CleanupOwnedCombatRuntime'))+'\n}')
    exe = folder/'Attack.exe'
    subprocess.run(['csc', '-nologo', '-nowarn:0169,0414', '-langversion:8.0', f'-out:{exe}',
                    str(extracted), str(Path(__file__).with_name('AttackTests.cs'))], check=True)
    subprocess.run(['mono', str(exe)], check=True)
