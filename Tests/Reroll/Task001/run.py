#!/usr/bin/env python3
"""Execute whole production classes against explicit clock/UI/easing boundaries."""
from pathlib import Path
import subprocess, tempfile
root = Path(__file__).resolve().parents[3]
local = Path(__file__).resolve().parent
sources = ['UI/UIAnimationStep.cs', 'UI/UIAnimationPlayer.cs', 'UI/ToastUI.cs',
           'Monster/DamageNumberUI.cs', 'Monster/DamageNumberManager.cs']
with tempfile.TemporaryDirectory(prefix='towernexus-reroll-animation-') as folder:
    exe = Path(folder) / 'PlaybackTests.exe'
    subprocess.run(['csc', '-nologo', '-langversion:8.0', '-nowarn:0649,0169', '-out:' + str(exe),
                    *[str(root/'Assets/Scripts'/p) for p in sources],
                    str(local/'BoundaryDoubles.cs'), str(local/'PlaybackTests.cs')], check=True)
    subprocess.run(['mono', str(exe)], check=True)
