#!/usr/bin/env python3
"""Editor build plus the complete runtime assembly without UNITY_EDITOR, using installed Unity references."""
from pathlib import Path
import subprocess,tempfile,sys,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3]
if '--player-only' not in sys.argv:
    subprocess.run(['python3',str(root/'Tests/Reroll/Task001/build.py')],cwd=root,check=True)
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
project=ET.parse(root/'Assembly-CSharp.csproj')
refs=[p.text.replace('\\','/') for p in project.findall('.//m:Reference/m:HintPath',ns)]
refs.append(str(root/'Temp/Bin/Debug/Assembly-CSharp-firstpass/Assembly-CSharp-firstpass.dll'))
sources=[root/p.attrib['Include'].replace('\\','/') for p in project.findall('.//m:Compile',ns)]
sources=[p for p in sources if p.exists()]
sources=sorted(set(sources)|set((root/'Assets/Scripts/UI').glob('*.cs')))
with tempfile.TemporaryDirectory(prefix='towernexus-reroll-player-') as folder:
    subprocess.run(['csc','-nologo','-langversion:8.0','-target:library','-nowarn:0649,0169',
                    '-out:'+str(Path(folder)/'Player.dll'),*['-r:'+r for r in refs],*map(str,sources)],cwd=root,check=True)
    print('PASS complete runtime assembly without UNITY_EDITOR: '+str(len(sources))+' sources')
