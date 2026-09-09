from pathlib import Path
import subprocess,tempfile,re,json
root=Path(__file__).resolve().parents[2]
def method(s,name):
 m=re.search(r'^    (?:private|public|internal) [^\n]*\b'+name+r'\(',s,re.M);a=s.index('{',m.end());i=a+1;depth=1
 while depth:depth+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[m.start():i]
def replay(source,dto):
 with tempfile.TemporaryDirectory(prefix='task006-replay-') as f:
  p=Path(f);(p/'Report.cs').write_text(dto)
  body='\n'.join(method(source,n) for n in ['ResolveDraftConsumptionStatus','CreateInvestmentRuntimeJson','ResolveFinalBuildCommitTime'])
  body=body.replace('CreateInvestmentRuntimeJson(int resolvedCount)', 'CreateInvestmentRuntimeJson()')
  (p/'Replay.cs').write_text('''using System;using System.Collections.Generic;using System.Web.Script.Serialization;
static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);}
class Replay {
 List<CombatBalanceInvestmentCommitJson> investmentCommits=new List<CombatBalanceInvestmentCommitJson>();
 List<CombatBalancePendingDraftJson> terminalPendingDraftSnapshot=new List<CombatBalancePendingDraftJson>();int resolvedCount=12;
'''+body+'''
 static void Main(){var x=new Replay();var output=new List<object>();
 for(int i=0;i<64;i++){
 x.investmentCommits.Clear();x.terminalPendingDraftSnapshot.Clear();
 var a=new CombatBalanceDraftAttemptJson{attemptToken="1:1",selectionAttempted=(i&32)!=0,selectionCommitted=(i&1)!=0};
 if((i&2)!=0)x.investmentCommits.Add(new CombatBalanceInvestmentCommitJson{draftAttemptToken="1:1",activeTimeSeconds=3,resolvedMonsterCount=5});
 if((i&4)!=0)x.terminalPendingDraftSnapshot.Add(new CombatBalancePendingDraftJson{draftAttemptToken="1:1"});
 if((i&8)!=0)x.investmentCommits.Add(new CombatBalanceInvestmentCommitJson{draftAttemptToken="1:2",activeTimeSeconds=9,resolvedMonsterCount=10});
 x.resolvedCount=(i&16)!=0?4:12;
 output.Add(new {status=x.ResolveDraftConsumptionStatus(a),runtime=x.CreateInvestmentRuntimeJson(),time=x.ResolveFinalBuildCommitTime()});
 }Console.Write(new JavaScriptSerializer().Serialize(output));}
}''')
  subprocess.run(['csc','-nologo','-nowarn:0649','-define:UNITY_EDITOR','-r:System.Web.Extensions.dll',f'-out:{p}/Replay.exe',str(p/'Replay.cs'),str(p/'Report.cs')],check=True,stdout=subprocess.PIPE)
  return json.loads(subprocess.check_output(['mono',str(p/'Replay.exe')]))
if __name__=='__main__':
 import sys
 if '--capture' in sys.argv:
  s=subprocess.check_output(['git','show','630fcad:Assets/Scripts/TowerDeployment/CombatBalanceRunRecorder.cs'],cwd=root,text=True)
  dto=subprocess.check_output(['git','show','630fcad:Assets/Scripts/TowerDeployment/CombatBalanceRunJsonReport.cs'],cwd=root,text=True)
  (root/'Tests/Task006/baseline-investment.json').write_text(json.dumps(replay(s,dto),indent=2)+'\n');print('Captured 64 old-implementation investment fixtures from 630fcad.')

 else:
  s=(root/'Assets/Scripts/Diagnostics/CombatBalance/CombatDraftAccumulator.cs').read_text()
  dto=(root/'Assets/Scripts/Diagnostics/CombatBalance/CombatBalanceRunJsonReport.cs').read_text()
  actual=replay(s,dto)
  expected=json.loads((root/'Tests/Task006/baseline-investment.json').read_text())
  assert actual==expected, 'Investment reconciliation differs from pre-extraction baseline'
  print('PASS: 64 investment reconciliation fixtures against 630fcad baseline')
