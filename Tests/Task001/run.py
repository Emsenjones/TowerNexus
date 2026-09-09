#!/usr/bin/env python3
"""Compile real transaction core against boundary doubles; no Unity execution."""
from pathlib import Path
import subprocess
import tempfile
import sys
root = Path(__file__).resolve().parents[2]
if '--baseline' not in sys.argv:
    sys.path.insert(0, str(root / 'Tests/Task004'))
    from build import build
    with tempfile.TemporaryDirectory(prefix='towernexus-upgrade-') as folder:
        build(folder, [root / 'Tests/Task001/CommitTests.cs'], 'CommitTests')
    sys.exit(0)
with tempfile.TemporaryDirectory(prefix='towernexus-task001-') as folder:
    if '--baseline' in sys.argv:
        sources = [root / 'Tests/Task001/Baseline.cs']
        for name, repo_path in (
            ('TowerInstance.cs', 'Assets/Scripts/TowerFramework/TowerInstance.cs'),
            ('TowerUpgradeState.cs', 'Assets/Scripts/TowerUpgrade/TowerUpgradeState.cs'),
        ):
            original = subprocess.run(['git', 'show', '10eaf6f:' + repo_path],
                                      cwd=root, text=True, capture_output=True, check=True)
            target = Path(folder) / name
            target.write_text(original.stdout)
            sources.append(target)
    exe = Path(folder) / 'CommitTests.exe'
    subprocess.run(['csc', '-nologo', '-langversion:8.0', '-define:UNITY_EDITOR',
                    f'-out:{exe}', *map(str, sources)], check=True, cwd=root)
    subprocess.run(['mono', str(exe)], check=True, cwd=root)
