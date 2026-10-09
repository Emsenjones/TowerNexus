#!/usr/bin/env python3
"""Compile the complete runtime with real installed Unity references in both modes."""
from pathlib import Path
import subprocess,tempfile,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3]
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
project=ET.parse(root/'Assembly-CSharp.csproj')
refs=[p.text.replace('\\','/') for p in project.findall('.//m:Reference/m:HintPath',ns)]
refs.append(str(root/'Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll'))
sources={root/p.attrib['Include'].replace('\\','/') for p in project.findall('.//m:Compile',ns)}
sources={p for p in sources if p.exists()}|set((root/'Assets/Scripts').rglob('*.cs'))
sources={p for p in sources if 'Editor' not in p.parts}
with tempfile.TemporaryDirectory(prefix='towernexus-content-build-') as folder:
 for mode in ['player','editor']:
  args=['csc','-nologo','-langversion:8.0','-target:library','-nowarn:0649,0169','-out:'+str(Path(folder)/(mode+'.dll'))]
  if mode=='editor':args+=['-define:UNITY_EDITOR']
  subprocess.run(args+['-r:'+r for r in refs]+list(map(str,sorted(sources))),cwd=root,check=True)
  print(f'PASS complete runtime {mode}: {len(sources)} sources, actual Unity references')
