from pathlib import Path
import subprocess,tempfile,re
root=Path(__file__).resolve().parents[2]
base=root/'Assets/Scripts/Diagnostics/CombatBalance'
source=(root/'Assets/Scripts/Monster/MonsterManager.cs').read_text()
enums='\n'.join(re.findall(r'public enum Monster(?:PlacementRoute\w+|ForcedRelocationReason)\s*\{[^}]+\}',source))
rec=(root/'Assets/Scripts/TowerDeployment/CombatBalanceRunRecorder.cs').read_text()
enums+='\n'+re.search(r'public enum PlacementRouteForcedRelocationExpectation\s*\{[^}]+\}',rec).group()
checks=['PlacementRouteBatchCountsMatch','PlacementRouteCommitStatePreserved','PlacementRouteGameplayStatePreserved','PlacementRouteTopologyValid','PlacementRouteCombatOwnershipPreserved','PlacementRouteLifecycleConsistent','PlacementRouteForcedRelocationUsageValid']
with tempfile.TemporaryDirectory(prefix='task006-integrity-') as folder:
 p=Path(folder);h=p/'Harness.cs'
 h.write_text('''using System;using System.IO;using System.Linq;using System.Web.Script.Serialization;
namespace UnityEngine { public struct Vector3 { public float x,y,z; } public static class Mathf { public static float Abs(float x)=>Math.Abs(x); public static int Abs(int x)=>Math.Abs(x); } }
'''+enums+'''
class Harness { static void Main(string[] paths) {int n=0; foreach(var path in paths) {
 var report=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<CombatBalanceRunJsonReport>(File.ReadAllText(path));
'''+''.join('if(!CombatReportIntegrity.'+name+'(report.placementRouteRuntime)) throw new Exception("'+name+' failed: "+path);n++;\n' for name in checks)+'''
 if(!CombatReportIntegrity.DamageDiagnosticsCountsMatch(report.damageDiagnostics)) throw new Exception("damage mismatch");n++;
 var accumulator = new CombatRouteAccumulator();
 accumulator.placementRouteCommits.AddRange(report.placementRouteRuntime.commits);
 accumulator.placementRouteLifecycle.AddRange(report.placementRouteRuntime.lifecycle.Where(x => x.kind != "ActiveAtRunEnd"));
 var actual = accumulator.CreatePlacementRouteRuntimeJson((PlacementRouteForcedRelocationExpectation)Enum.Parse(typeof(PlacementRouteForcedRelocationExpectation),report.fixture.placementRouteForcedRelocationExpectation),report.timing.runDurationSeconds);
 var serializer = new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
 if(serializer.Serialize(actual)!=serializer.Serialize(report.placementRouteRuntime)) throw new Exception("route replay mismatch: "+path);n++;
 // Corrupt the accepted report to verify negative evidence remains negative.
 report.damageDiagnostics.towerScaledResolutionCount++;
 if(CombatReportIntegrity.DamageDiagnosticsCountsMatch(report.damageDiagnostics)) throw new Exception("corruption accepted");n++;
 } Console.WriteLine(n+" production integrity checks on four preserved Task005 reports passed (historical evidence)."); } }
''')
 exe=p/'Integrity.exe'
 subprocess.run(['csc','-nologo','-nowarn:0649','-langversion:8.0','-define:UNITY_EDITOR','-r:System.Web.Extensions.dll',f'-out:{exe}',str(h),str(base/'CombatReportIntegrity.cs'),str(base/'CombatRouteAccumulator.cs'),str(root/'Assets/Scripts/Diagnostics/CombatBalance/CombatBalanceRunJsonReport.cs')],check=True)
 reports=[root/'Doc/GamePlayRecord'/name for name in ['Task005_Stage4_FourTower_DeploymentUpgrade_Acceptance_01.json',*[f'Task005_Stage5_SingleElemental_Acceptance_{i:02}.json' for i in range(1,4)]]]
 assert len(reports)==4,len(reports)
 subprocess.run(['mono',str(exe),*map(str,reports)],check=True)
