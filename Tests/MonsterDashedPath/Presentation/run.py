#!/usr/bin/env python3
"""Production presenter + snapshot with controlled Unity doubles; no native visual evidence."""
from pathlib import Path
import subprocess
import tempfile
root = Path(__file__).resolve().parents[3]
sources = [root / path for path in (
    'Assets/Scripts/TowerDeployment/MonsterDashedPathPresenter.cs',
    'Assets/Scripts/Pathfinding/MonsterMainRouteSnapshot.cs',
    'Tests/MonsterDashedPath/Presentation/Doubles.cs',
    'Tests/MonsterDashedPath/Presentation/PresenterTests.cs',
)]
with tempfile.TemporaryDirectory(prefix='towernexus-path-presentation-') as directory:
    exe = Path(directory) / 'PresenterTests.exe'
    subprocess.run(['csc', '-nologo', '-nowarn:0649', '-langversion:8.0',
                    f'-out:{exe}', *map(str, sources)], check=True, cwd=root)
    subprocess.run(['mono', str(exe)], check=True, cwd=root)
