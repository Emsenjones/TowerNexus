#!/usr/bin/env python3
"""Exact production death/health/transaction/counter methods; native dependencies doubled."""
from pathlib import Path
import re,subprocess,tempfile
root=Path(__file__).resolve().parents[3]
def block(s,needle):
 a=s.index(needle);o=s.index('{',a);d=1;i=o+1
 while d:d+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[a:i]
monster=(root/'Assets/Scripts/Monster/MonsterBehaviour.cs').read_text()
tower=(root/'Assets/Scripts/TowerFramework/TowerInstance.cs').read_text()
with tempfile.TemporaryDirectory(prefix='towernexus-kills-') as folder:
 folder=Path(folder)
 m=folder/'Monster.cs';m.write_text('using System;using UnityEngine;partial class MonsterBehaviour {\n internal void TakeDamage(int n,TowerKillSource s)=>TakeDamage(n,s,out _);\n'+ '\n'.join(block(monster,n) for n in ['    internal void TakeDamage(int damage, TowerKillSource source, out', '    public void Die()', '    internal sealed class HitTransaction','    internal HitTransaction BeginTowerOwnedHitTransaction(', '    internal void EndTowerOwnedHitTransaction(', '    private void TryResolve(', '    public bool TryInitializeRuntime(', '    private void StopGameplayState('])+'\n}')
 t=folder/'Tower.cs';start=tower.index('    private int killCount;');end=tower.index('    private TowerDefinition towerDefinition;',start)
 t.write_text('using System;using System.Collections.Generic;using UnityEngine;partial class TowerInstance {\n'+tower[start:end]+ '\n'+block(tower,'    public void Initialize(')+'\n}')
 buff=folder/'Buff.cs'
 buff.write_text('using System;using UnityEngine;public partial class MonsterBuffRuntime {\n'+block((root/'Assets/Scripts/BuffAndEffect/MonsterBuffRuntime.cs').read_text(),'    private bool ExecuteLifecycleEffect(')+'\n}')
 hit=folder/'Hit.cs'
 hit.write_text('using System;using UnityEngine;public static class TowerOwnedHitTransaction {\n'+block((root/'Assets/Scripts/BuffAndEffect/ElementalApplication.cs').read_text(),'    public static bool ApplyDamage(')+'\n}')
 paths=[hit,root/'Assets/Scripts/BuffAndEffect/MonsterBuffInstance.cs',root/'Assets/Scripts/BuffAndEffect/BuffApplyOutcome.cs',buff,root/'Assets/Scripts/TowerFramework/TowerKillSource.cs',root/'Assets/Scripts/TowerDeployment/BattleCombatBinding.cs',root/'Assets/Scripts/BuffAndEffect/EffectTriggerContext.cs',root/'Assets/Scripts/BuffAndEffect/BuffApplyRequest.cs',m,t,Path(__file__).with_name('KillTests.cs')]
 exe=folder/'Kills.exe'
 subprocess.run(['csc','-nologo','-langversion:8.0','-nowarn:0067,0649,0414',f'-out:{exe}',*map(str,paths)],check=True,cwd=root)
 subprocess.run(['mono',str(exe)],check=True,cwd=root)
