#!/usr/bin/env python3
from pathlib import Path
import tempfile,subprocess
root=Path(__file__).resolve().parents[3]
with tempfile.TemporaryDirectory(prefix='towernexus-draft-ui-') as folder:
    exe=Path(folder)/'UI.exe'
    subprocess.run(['csc','-nologo','-langversion:8.0','-nowarn:0649',f'-out:{exe}',
                    str(root/'Assets/Scripts/TowerDeployment/DraftUI.cs'),str(root/'Tests/Reroll/Task002/UITests.cs')],check=True)
    subprocess.run(['mono',str(exe)],check=True)
