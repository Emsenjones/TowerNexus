#!/usr/bin/env python3
"""Production exit view/session/pause and independent HUD lifetime dispatcher."""
from pathlib import Path
import runpy
import subprocess
import tempfile

root = Path(__file__).resolve().parents[3]
shared = runpy.run_path(str(root / 'Tests/TowerInfoWindow/Presentation/run.py'))
with tempfile.TemporaryDirectory(prefix='towernexus-exit-confirmation-') as folder:
    folder = Path(folder)
    paths = shared['production_sources'](folder)
    paths.append(Path(__file__).with_name('PresentationTests.cs'))
    exe = folder / 'ExitConfirmation.exe'
    subprocess.run(['csc', '-nologo', '-langversion:8.0', '-nowarn:0649',
                    f'-out:{exe}', *map(str, paths)], cwd=root, check=True)
    subprocess.run(['mono', str(exe)], cwd=root, check=True)
