#!/usr/bin/env python3
"""Execute production query code with Unity hierarchy doubles; not a Unity profiler."""
from pathlib import Path
import re, subprocess, tempfile, argparse
root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--baseline-only', action='store_true')
args = parser.parse_args()

def method(source, signature):
    a = source.index(signature)
    begin = source.index('{', a)
    depth = 1
    end = begin + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[a:end] + '\n'

def old(path):
    return subprocess.run(['git','show','10eaf6f:'+path],cwd=root,text=True,capture_output=True,check=True).stdout

def generate_map(source, baseline):
    if baseline:
        a=source.index('    private readonly Dictionary<Vector2Int, GridNodeBehaviour> nodeDictionary')
        b=source.index('    public int Width',a)
        fields=source[a:b]
        lifecycle='''public bool TryEnsureNodeIndex() { EnsureNodeDictionaryValid(); return true; }
public ulong StructureRevision => 0; public ulong WalkabilityRevision => 0;
public string NodeIndexFailureReason => "";
'''
        signatures=['private void EnsureNodeDictionaryValid()', 'private void RebuildNodeDictionary()',
                    'private void RebuildNodeDictionary(GridNodeBehaviour[] hierarchyNodes)', 'private void OnEnable()']
    else:
        a=source.index('    private readonly MapRuntimeNodeIndex runtimeIndex')
        b=source.index('    public int Width',a)
        fields=source[a:b]
        a=source.index('    private void OnEnable()');b=source.index('    [Button("Generate Map")]',a)
        lifecycle=source[a:b]
        signatures=['private void ReleaseNodeOwners()', 'private void RebuildNodeDictionary()',
                    'private bool HasGridNodeAncestorInsideNodesRoot(GridNodeBehaviour node)',
                    'private bool IsNodesRootSafelyOwned()']
    shared=['public GridNodeBehaviour GetNode(Vector2Int gridPosition)',
            'public GridNodeBehaviour GetNode(int x, int y)',
            'public bool TryGetNodeByWorldPosition(Vector3 worldPosition, out GridNodeBehaviour node)',
            'private static bool TryResolvePhysicalGridCoordinate(', 'private static bool IsFinite(Vector3 value)',
            'public bool IsInsideBounds(Vector2Int gridPosition)',
            'public List<GridNodeBehaviour> GetNeighborNodes(Vector2Int gridPosition)',
            'private GridNodeBehaviour[] GetHierarchyNodes()']
    # Current endpoint methods use expression bodies.
    if baseline:
        shared += ['public GridNodeBehaviour GetSpawnNode()', 'public GridNodeBehaviour GetTargetNode()']
    else:
        for name in ['GetSpawnNode', 'GetTargetNode']:
            expression=re.search(r'public GridNodeBehaviour '+name+r'\(\)\s*=>[^;]+;',source).group(0)
            lifecycle += expression + '\n'
    return '''using System; using System.Collections.Generic; using UnityEngine;
public class MapGeneratorBehaviour : MonoBehaviour {
private int width, lengh; private float nodeSize = 1; private Transform nodesRoot;
public Transform NodesRoot => nodesRoot;
private static readonly Vector2Int[] OrthogonalDirections = {Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
private static bool IsStrictDescendant(Transform child, Transform parent) => child != parent && child.IsChildOf(parent);
private static string GetNodeLabel(GridNodeBehaviour node) => "fixture";
public void Configure(int w, int h, IReadOnlyList<GridNodeBehaviour> nodes) {
width=w; lengh=h; nodesRoot=new Transform(); nodesRoot.SetParent(transform);
foreach(var node in nodes) node.transform.SetParent(nodesRoot);
OnEnable();
}
''' + fields + lifecycle + ''.join(method(source,m) for m in signatures+shared) + '}\n'

def execute(folder, baseline, fixtures):
    mode='baseline' if baseline else 'current'
    paths=['Assets/Scripts/Map/GridNodeBehaviour.cs','Assets/Scripts/Pathfinding/AStarPathfindingService.cs',
           'Assets/Scripts/TowerDeployment/TowerPlacementValidator.cs']
    sources=[]
    for path in paths:
        text=old(path) if baseline else (root/path).read_text()
        if baseline and 'AStarPathfinding' in path:
            text=text.replace('    public bool HasActiveMap => mapGenerator != null;',
                              '    public bool HasActiveMap => mapGenerator != null;\n    public int SearchCount;')
            text=text.replace('        if (mapGenerator == null)\n        {\n            Debug.LogWarning',
                              '        SearchCount++;\n        if (mapGenerator == null)\n        {\n            Debug.LogWarning',1)
        target=folder/(mode+Path(path).name);target.write_text(text);sources.append(target)
    map_path='Assets/Scripts/Map/MapGeneratorBehaviour.cs'
    target=folder/(mode+'Map.cs')
    target.write_text(generate_map(old(map_path) if baseline else (root/map_path).read_text(),baseline));sources.append(target)
    if not baseline:sources.append(root/'Assets/Scripts/Map/MapRuntimeNodeIndex.cs')
    sources += [root/'Tests/Task002/UnityDoubles.cs',root/'Tests/Task002/QueryTests.cs']
    exe=folder/(mode+'.exe')
    subprocess.run(['csc','-nologo','-nowarn:0649','-langversion:8.0','-define:UNITY_EDITOR'+(';BASELINE' if baseline else ''),
                    f'-out:{exe}',*map(str,sources)],cwd=root,check=True)
    result=subprocess.run(['mono',str(exe),*map(str,fixtures)],cwd=root,text=True,capture_output=True)
    if result.returncode:
        print(result.stdout, result.stderr)
        result.check_returncode()
    print(mode.upper()+':\n'+'\n'.join(line for line in result.stdout.splitlines() if not line.startswith('CASE|')))
    return [line for line in result.stdout.splitlines() if line.startswith('CASE|')]

with tempfile.TemporaryDirectory(prefix='towernexus-task002-') as temp:
    folder=Path(temp);fixtures=[]
    for stage in [1,4]:
        path=root/f'Assets/Art/Prefab/Map/Prefab_Map_Stage{stage}.prefab'
        source=path.read_text()
        width=re.search(r'^  width: (\d+)$',source,re.M).group(1)
        height=re.search(r'^  lengh: (\d+)$',source,re.M).group(1)
        nodes=re.findall(r'  gridPosition: \{x: (-?\d+), y: (-?\d+)\}\n  baseWalkable: (\d+)\n  nodeType: (\d+)',source)
        assert len(nodes)==int(width)*int(height), 'Unsupported prefab serialization; do not silently omit nodes.'
        fixture=folder/f'Stage{stage}.txt';fixture.write_text(width+' '+height+'\n'+'\n'.join(' '.join(node) for node in nodes));fixtures.append(fixture)
    baseline=execute(folder,True,fixtures)
    if not args.baseline_only:
        current=execute(folder,False,fixtures)
        assert baseline==current, 'Exact route / preview / final placement results changed!'
        print(f'PASS {len(current)} baseline/current fixture observations match exactly.')
