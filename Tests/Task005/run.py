#!/usr/bin/env python3
from pathlib import Path
import subprocess,tempfile,re
def method(source,name):
 match=re.search(r"^    (?:private|public|internal) [^\n]*\b"+name+r"\(",source,re.M)
 begin=source.index("{",match.end());i=begin+1;depth=1
 while depth:
  depth+=(source[i]=="{")-(source[i]=="}");i+=1
 return source[match.start():i]
root=Path(__file__).resolve().parents[2]
sources=['TowerDeployment/BattleCombatBinding.cs','BuffAndEffect/BuffRemovalPermission.cs','BuffAndEffect/EffectTriggerContext.cs','BuffAndEffect/EffectTargetResolver.cs','BuffAndEffect/EffectExecutor.cs','BuffAndEffect/ElementalApplication.cs']
with tempfile.TemporaryDirectory(prefix='towernexus-task005-') as folder:
 exe=Path(folder)/'Contracts.exe'
 lifecycle=Path(folder)/'Lifecycle.cs'
 source=(root/'Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs').read_text()
 lifecycle.write_text('partial class LifecycleHarness {\n'+'\n'.join(method(source,n) for n in ['RevokeCombatAuthority','StopBattle','ReleasePreparedBattleRuntime'])+'\n}')
 subprocess.run(['csc','-nologo','-langversion:8.0',f'-out:{exe}',*[str(root/'Assets/Scripts'/s) for s in sources],str(root/'Tests/Task005/BoundaryDoubles.cs'),str(root/'Tests/Task005/BindingTests.cs'),str(lifecycle)],check=True)
 subprocess.run(['mono',str(exe)],check=True)
