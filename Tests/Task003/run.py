#!/usr/bin/env python3
"""Real Pending collection + extracted production orchestration/sampling, native boundaries doubled."""
from pathlib import Path
import subprocess, tempfile
root = Path(__file__).resolve().parents[2]
draft_path = 'Assets/Scripts/TowerDeployment/DraftSystem.cs'
current = (root / draft_path).read_text()
baseline = subprocess.run(['git','show','7bec5ca:'+draft_path], cwd=root,
                          capture_output=True,text=True,check=True).stdout

def method(source, name):
    import re
    match = re.search(r'^    (?:private|public|internal) (?:static )?[^\n]*\b'+name+r'\(', source, re.M)
    if not match: raise ValueError(name)
    start = match.start(); opening = source.index('{', match.end())
    depth = 1; end = opening + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}'); end += 1
    result = source[start:end]
    # Observation instrumentation is tested through native Recorder acceptance separately.
    lines=[]; level=0
    for line in result.splitlines():
        if line.strip().startswith('#if'): level+=1
        elif line.strip().startswith('#endif'): level-=1
        elif not level: lines.append(line)
    return '\n'.join(lines)

with tempfile.TemporaryDirectory(prefix='towernexus-task003-') as folder:
    folder=Path(folder)
    actual = folder/'DraftSystem.cs'
    actual.write_text('partial class DraftSystem {\n'+'\n'.join(method(current,n) for n in
        ['TryGrantPendingBatch','TryGrantDebugPendingBatch','TryRebuildPendingViews','HandleDraftSelected'])+'\n}')
    hud_source = (root/'Assets/Scripts/TowerDeployment/BattleHUDUI.cs').read_text()
    start = hud_source.index('    internal bool IsCurrentPendingView(')
    predicate = hud_source[start:hud_source.index(';',start)+1]
    hud = folder/'BattleHUDUI.cs'
    hud.write_text('partial class BattleHUDUI {\n'+predicate+'\n}')
    samplers=[]
    for label, source in [('CurrentSampler', current), ('BaselineSampler', baseline)]:
        methods='\n'.join(method(source,n) for n in ['AddTowerUpgradeDraftCandidates',
            'CountPendingReservedCapacityForUpgrade','DoesPendingUpgradeReserveCapacityForUpgrade',
            'GenerateLevelUpDraftChoices','SampleDistinctCandidates','ShuffleDraftChoices'])
        header=(root/'Tests/Task003/Sampler.cs').read_text().replace('SAMPLER',label)
        path=folder/(label+'.cs');path.write_text(header+'\n'+methods+'\n}');samplers.append(path)
    # Full investment entry coverage moved to Task004; no obsolete post-preflight extraction.
    import sys
    sys.path.insert(0,str(root/'Tests/Task004'))
    from build import sources
    native=(root/'Tests/Task004/BoundaryDoubles.cs').read_text()
    a=native.index('public partial class DraftSystem');b=native.index('public class BattleRuntimeCoordinator',a)
    native=native[:a]+native[b:]
    boundary=folder/'BoundaryDoubles.cs';boundary.write_text(native)
    paths=[p for p in sources if p.name not in ['BoundaryDoubles.cs']]
    paths += [boundary,root/'Tests/Task003/PendingTests.cs']
    exe=folder/'PendingTests.exe'
    # Add namespace imports to extracted production partial.
    actual.write_text('using System; using System.Collections.Generic; using UnityEngine;\n'+actual.read_text())
    subprocess.run(['csc','-nologo','-langversion:8.0',f'-out:{exe}',
        *map(str,paths),str(actual),str(hud),*map(str,samplers)],check=True,cwd=root)
    subprocess.run(['mono',str(exe)],check=True,cwd=root)
