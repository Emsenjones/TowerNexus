#!/usr/bin/env python3
"""Compile without invoking Unity or editing its generated project files."""
from pathlib import Path
from xml.sax.saxutils import escape
import subprocess, tempfile, xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[3]
with tempfile.TemporaryDirectory(prefix='towernexus-animation-build-') as folder:
    overlay = Path(folder)/'Sources.targets'
    overlay.write_text('''<Project><Target Name="RerollAnimationSources" BeforeTargets="CoreCompile">
<ItemGroup Condition="'$(MSBuildProjectName)' == 'Assembly-CSharp'">
<Compile Remove="Assets/Scripts/Monster/DamageNumberAnimationStep.cs" />
<Compile Remove="Assets/Scripts/UI/*.cs" />
<Compile Include="'''+escape(str(root/'Assets/Scripts/UI/*.cs'))+'''" />
</ItemGroup></Target></Project>''')
    subprocess.run(['dotnet','build','Assembly-CSharp-Editor.csproj','--no-restore',
                    '-m:1','-nr:false','-p:LangVersion=8.0',
                    '-p:CustomAfterMicrosoftCommonTargets='+str(overlay)],cwd=root,check=True)
    ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
    project=ET.parse(root/'Assembly-CSharp.csproj')
    references=[p.text.replace('\\','/') for p in project.findall('.//m:Reference/m:HintPath',ns)]
    sources=[*list((root/'Assets/Scripts/UI').glob('*.cs')),
             root/'Assets/Scripts/Monster/DamageNumberUI.cs',root/'Assets/Scripts/Monster/DamageNumberManager.cs']
    for mode in ['player','editor']:
        command=['csc','-nologo','-langversion:8.0','-target:library','-nowarn:0649,0169',
                 '-out:'+str(Path(folder)/(mode+'.dll'))]
        if mode=='editor': command+=['-define:UNITY_EDITOR']
        subprocess.run(command+['-r:'+p for p in references]+list(map(str,sources)),cwd=root,check=True)
        print('PASS: actual Unity/DOTween/TMP references, '+mode+' conditional compilation')
