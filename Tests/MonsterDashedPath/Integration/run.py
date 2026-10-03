#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile
root=Path(__file__).resolve().parents[3]
sources=[root/p for p in (
    'Assets/Scripts/TowerDeployment/MonsterDashedPathSession.cs',
    'Assets/Scripts/TowerDeployment/MonsterDashedPathPresenter.cs',
    'Assets/Scripts/Pathfinding/MonsterMainRouteSnapshot.cs',
    'Assets/Scripts/TowerDeployment/TowerPlacementRoutePreviewResult.cs',
    'Tests/MonsterDashedPath/Presentation/Doubles.cs',
    'Tests/MonsterDashedPath/Integration/SessionTests.cs',
)]
with tempfile.TemporaryDirectory(prefix='towernexus-path-integration-') as folder:
    exe=Path(folder)/'SessionTests.exe'
    subprocess.run(['csc','-nologo','-nowarn:0649','-langversion:8.0',f'-out:{exe}',*map(str,sources)],check=True,cwd=root)
    subprocess.run(['mono',str(exe)],check=True,cwd=root)
