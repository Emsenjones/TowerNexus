#!/usr/bin/env python3
"""Whole Game Flow/Stage/HUD, production coordinator acceptance and release methods."""
from pathlib import Path
import re
import runpy
import subprocess
import tempfile

root = Path(__file__).resolve().parents[3]
shared = runpy.run_path(str(root / 'Tests/TowerInfoWindow/Presentation/run.py'))
method = shared['method']
def production_sources(folder):
    paths = shared['production_sources'](folder)
    original = root / 'Tests/TowerInfoWindow/Presentation/BoundaryDoubles.cs'
    paths.remove(original)
    doubles = original.read_text()
    doubles = doubles.replace('public class TowerDefinition\n', 'public class TowerDefinition:UnityEngine.Object\n')
    doubles = doubles.replace('public class TowerUpgradeDefinition{', 'public class TowerUpgradeDefinition:UnityEngine.Object{')
    doubles = doubles.replace('public bool Destroyed;', 'public bool Destroyed; public string name;')
    doubles = doubles.replace('public class MonsterManager:UnityEngine.MonoBehaviour', 'public partial class MonsterManager:UnityEngine.MonoBehaviour')
    doubles = doubles.replace('internal sealed class TowerPlacementSubmission', 'internal sealed partial class TowerPlacementSubmission')
    doubles = doubles.replace('public class TowerPlacementController\n', 'public partial class TowerPlacementController\n')
    doubles = doubles.replace('public static void LogWarning(string value,Object context=null){}',
        'public static void LogWarning(string value,Object context=null){} public static void LogError(string value,Object context=null){}')
    doubles = doubles.replace('public static void Destroy(Object value)', '''public static GameObject Instantiate(GameObject template, Transform parent, bool world)
        {
            BeforeInstantiate?.Invoke();
            var go = new GameObject(); go.transform.SetParent(parent, false);
            go.AddComponent<MapGeneratorBehaviour>(); Created.Add(go); return go;
        }
        public static void Destroy(Object value)''')
    doubles = doubles.replace('public void Dispatch(string method)', '''public T[] GetComponents<T>() where T:class => components.FindAll(c => c is T).ConvertAll(c => c as T).ToArray();
        public T[] GetComponentsInChildren<T>(bool inactive) where T:class
        { var result=new List<T>(GetComponents<T>()); foreach(var child in transform.children) result.AddRange(child.gameObject.GetComponentsInChildren<T>(inactive));return result.ToArray(); }
        public void Dispatch(string method)''')
    native = folder / 'NativeDoubles.cs'; native.write_text(doubles); paths.append(native)
    source = (root / 'Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs').read_text()
    coordinator = folder / 'CoordinatorMethods.cs'
    coordinator.write_text('using System;using UnityEngine;\npublic partial class BattleRuntimeCoordinator {\n' +
        '\n'.join(method(source, n) for n in ('TryAcceptExitStageTransition', 'CaptureBeforeCleanup',
            'RevokeCombatAuthority', 'CloseBattleAuthorityAndGates', 'StopBattle', 'StopBattleCore',
            'ReleasePreparedBattleRuntime', 'ReleasePreparedBattleRuntimeCore', 'RunCleanupSafely',
            'TryClaimBattleTerminalState', 'FinalizeLifecycleOperation', 'RevokeBattlePermissions', 'CompleteTerminalObservation', 'TryCompleteBattleResult', 'TryFailBattleRuntime')) + '\n' +
        method(source.replace('PublishSafely<T>(', 'PublishSafely('), 'PublishSafely').replace('void PublishSafely(', 'void PublishSafely<T>(')+'\n'+
        shared['expression'](source, 'CanAcceptExitStageRequest') + '\n' +
        '\n'.join(re.search(r'^    internal bool '+name+r' =>.*?;', source, re.M | re.S)[0]
            for name in ('IsLifecycleBusy', 'IsRuntimeRetired')) + '\n}')
    paths += [coordinator, root / 'Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.ExitStageIntegration.cs', Path(__file__).with_name('BoundaryDoubles.cs'),
              root / 'Assets/Scripts/Stage/StageCompositionController.cs',
              root / 'Assets/Scripts/GameFlow/GameFlowController.cs',
              root / 'Assets/Scripts/GameFlow/GameFlowController.ExitStageTransitions.cs',
              root / 'Assets/Scripts/GameFlow/GameFlowState.cs']
    return paths

if __name__ == '__main__':
  with tempfile.TemporaryDirectory(prefix='towernexus-exit-transitions-') as folder:
    folder = Path(folder)
    paths = production_sources(folder) + [Path(__file__).with_name('TransitionTests.cs')]
    exe = folder / 'Transitions.exe'
    subprocess.run(['csc', '-nologo', '-langversion:8.0', '-nowarn:0649,0067,0414', f'-out:{exe}',
                    *map(str, paths)], cwd=root, check=True)
    subprocess.run(['mono', str(exe)], cwd=root, check=True)
