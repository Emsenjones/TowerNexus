#!/usr/bin/env python3
"""Production HUD entry, coordinator binder/rebind/clear and Game Flow route."""
from pathlib import Path
import runpy
import re
import subprocess
import tempfile

root = Path(__file__).resolve().parents[3]
shared = runpy.run_path(str(root / 'Tests/ExitStageConfirmation/Transitions/run.py'))
with tempfile.TemporaryDirectory(prefix='towernexus-exit-integration-') as folder:
    folder = Path(folder)
    paths = shared['production_sources'](folder)
    coordinator = folder / 'CoordinatorMethods.cs'
    source = (root / 'Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs').read_text()
    coordinator.write_text(coordinator.read_text() + '\npublic partial class BattleRuntimeCoordinator {\n' +
        re.search(r'^    internal bool IsGameplayModalBlocked =>.*?;', source, re.M | re.S)[0] + '\n}')
    hud = folder / 'HUDEnable.cs'
    hud.write_text('using System; using System.Collections.Generic;\npublic partial class BattleHUDUI {\n' +
        shared['method']((root / 'Assets/Scripts/TowerDeployment/BattleHUDUI.cs').read_text(), 'OnEnable') + '\n' +
        'private void SubscribeToPlayerSystem(){} private void InitializeFromPlayerSystem(){}\n' +
        'public DraftSystem DraftOwner; private List<PendingDraftUIItem> pendingDraftItems=new List<PendingDraftUIItem>();\n}\n' +
        'public class PendingDraftUIItem {public void BeginBattle(){}}\n')
    # Add only the external endpoints used by the production OnEnable and shared modal query.
    native = folder / 'NativeDoubles.cs'
    native.write_text(native.read_text().replace('public bool ThrowNotify;', 'public bool ThrowNotify; public void CloseDraft(){}'))
    domain = root / 'Tests/ExitStageConfirmation/Transitions/BoundaryDoubles.cs'
    paths.remove(domain)
    domain_copy = folder / 'DomainDoubles.cs'
    domain_copy.write_text(domain.read_text().replace('public class DraftSystem {',
        'public class DraftSystem { public bool IsBattleActive,IsModalSessionBusy;'))
    paths += [domain_copy, hud, Path(__file__).with_name('IntegrationTests.cs')]
    exe = folder / 'Integration.exe'
    subprocess.run(['csc', '-nologo', '-langversion:8.0', '-nowarn:0649,0067,0414', f'-out:{exe}',
                    *map(str, paths)], cwd=root, check=True)
    subprocess.run(['mono', str(exe)], cwd=root, check=True)
