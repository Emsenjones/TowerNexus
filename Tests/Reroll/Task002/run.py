#!/usr/bin/env python3
"""Full production DraftSystem and Pending/upgrade ownership; native HUD replaced with fault injection."""
from pathlib import Path
import sys, subprocess, tempfile
root=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(root/'Tests/Task004'))
from build import sources
from recorder import generate, block
with tempfile.TemporaryDirectory(prefix='towernexus-reroll-') as folder:
    folder=Path(folder)
    native=(root/'Tests/Task004/BoundaryDoubles.cs').read_text()
    a=native.index('public partial class DraftSystem');b=native.index('public class BattleRuntimeCoordinator',a)
    native=native[:a]+native[b:]
    native=native.replace('public static int frameCount;', 'public static int frameCount; public static float timeScale=1;')
    native=native.replace('public class SerializeField : Attribute {}','public class SerializeField : Attribute {} public class Header : Attribute {public Header(string s){}} public class ContextMenu : Attribute {public ContextMenu(string s){}}')
    native=native.replace('public class BattleRuntimeCoordinator : UnityEngine.MonoBehaviour','public partial class BattleRuntimeCoordinator : UnityEngine.MonoBehaviour')
    native=native.replace('    internal object DiagnosticIdentity => this;', '''    internal object DiagnosticIdentity => this;
    public DraftSystem Draft; public Action BeforeDraftFailureCleanup;
    private DraftSystem draftSystem => Draft;
    private enum BattleTerminalState {None, TechnicalFailure}
    private BattleTerminalState battleTerminalState;
    private void TryFailBattleRuntime(string reason) { FailureCount++;battleTerminalState=BattleTerminalState.TechnicalFailure; IsBattleActive=false;BeforeDraftFailureCleanup?.Invoke();Draft.StopBattle(); }
''')
    native+='\npartial class BattleRuntimeCoordinator {\n'+block((root/'Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs').read_text(),r'^    internal void FailDraftPresentation\(')+'\n}'
    native=native[:native.index('internal static class CombatDiagnosticScope')]+native[native.index('\npartial class BattleRuntimeCoordinator'):]
    native+='\ninternal struct CombatDiagnosticSource { internal CombatDiagnosticSource(TowerInstance tower,object unused){} }\npublic class MonsterBehaviour:UnityEngine.MonoBehaviour {public GridNodeBehaviour CurrentNode;}\n'

    boundary=folder/'BoundaryDoubles.cs';boundary.write_text(native)
    boundary.write_text(native)
    pending=(root/'Tests/Task003/PendingTests.cs').read_text().split('public partial class DraftSystem')[0]
    pending=pending.replace('    public bool IsDraftOpen = true;','')
    pending=pending.replace('public void CloseDraft() { Closed++; }','public void CloseDraft() { Closed++; IsDraftOpen=false; }')
    hud=folder/'PendingHud.cs';hud.write_text(pending)
    paths=[p for p in sources if p.name not in ['BoundaryDoubles.cs','SubmissionFixture.cs']]
    paths += [boundary,hud,root/'Assets/Scripts/TowerDeployment/DraftSystem.cs',root/'Tests/Reroll/Task002/RerollTests.cs']
    paths.append(root/'Assets/Scripts/Diagnostics/CombatDiagnosticScope.cs')
    paths.append(generate(root,folder))
    for mode in ['editor','player']:
        exe=folder/(mode+'.exe')
        subprocess.run(['csc','-nologo','-nowarn:0649,0067','-langversion:8.0',f'-out:{exe}',
                        *(['-define:UNITY_EDITOR'] if mode=='editor' else []),*map(str,paths)],cwd=root,check=True)
        subprocess.run(['mono',str(exe)],cwd=root,check=True)
