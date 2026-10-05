#!/usr/bin/env python3
"""Compile production UI scripts against managed boundary doubles, not the Unity runtime."""
from pathlib import Path
import subprocess
import tempfile
import re


def method(source, name):
    match = re.search(r'^    (?:private|public|internal) [^\n]*\b' + name + r'\(', source, re.M)
    begin = source.index('{', match.end())
    end, depth = begin + 1, 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[match.start():end]

root = Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory(prefix='tower-state-ui-') as folder:
    exe = Path(folder) / 'StateUI.exe'
    sources = [root / 'Assets/Scripts/TowerFramework' / name for name in
               ('TowerStateUIItem.cs', 'TowerStateUIManager.cs')]
    sources += [root / 'Tests/TowerStateUI' / name for name in
                ('BoundaryDoubles.cs', 'StateUITests.cs')]
    subprocess.run(['csc', '-nologo', '-nowarn:0649', '-langversion:8.0',
                    f'-out:{exe}', *map(str, sources)], check=True)
    subprocess.run(['mono', str(exe)], check=True)

    coordinator = (root / 'Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs').read_text()
    extracted = Path(folder) / 'CoordinatorMethods.cs'
    extracted.write_text('using System; using UnityEngine;\npartial class CoordinatorHarness {\n' +
                         '\n'.join(method(coordinator, name) for name in (
                             'BeginPreparedBattleCore', 'BeginTowerStateUI', 'RunCleanupSafely',
                             'ReleasePreparedBattleRuntimeCore', 'StopBattle')) + '\n}')
    integration = Path(folder) / 'Coordinator.exe'
    subprocess.run(['csc', '-nologo', '-nowarn:0649', '-langversion:8.0', f'-out:{integration}',
                    str(root / 'Tests/TowerStateUI/CoordinatorTests.cs'), str(extracted)], check=True)
    subprocess.run(['mono', str(integration)], check=True)
