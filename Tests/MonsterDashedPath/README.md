# Monster dashed-line path Task001 validation

Run from the repository root with Python 3 and Mono (`csc`, `mono`):

```sh
python3 Tests/Task002/run.py
python3 Tests/Task004/run.py
```

The directory names refer to historical architecture tasks. This feature's
`RouteQueryTests.cs` is included only in the current-code build of Task002's
existing query harness. It does not change the historical Git baseline.

## Query evidence

The query harness compiles the actual Map index, extracted production Map
query/lifecycle methods, Grid nodes, A*, Validator, and both new result types.
Only Unity hierarchy/authoring APIs are doubles. It verifies:

- 29 feature cases: formal route availability without Monsters, deterministic
  equal-length route selection, full multi-cell footprints, all four outcomes,
  route/footprint immutability, shared positive and negative caches, and old
  Available/Blocked/Unavailable results invalidating on structure, walkability,
  Map replacement, service replacement, same-Map rebind, Validator rebind,
  and Map release.
- Partial out-of-Map or malformed anchors and technical failures clear old
  caches; a disconnected committed graph cannot report candidate blocking.
- 32,192 complete historical/current observations match for authored Stage1
  and Stage4 Maps. Rich available-preview routes additionally match fresh
  final topology plans node-for-node.
- A cold available query searches once; equivalent stationary candidates reuse
  it. A cold blocked query searches the candidate and confirms the formal
  route; repeated blocked candidates reuse the result without either search.

## Submission evidence

The submission harness passes 26 cases with actual Candidate, Validator,
Submission, Pending, and upgrade transaction code. Map/pathfinding/native-runtime
behavior is controlled by boundary doubles. Three feature-specific cases prove:

- Unchanged rich-preview results can remain current across frames, while the
  production final Candidate rejects an old frame.
- Cached previews do not bypass final Candidate topology/binding revisions.
- A rich-preview cache hit cannot bypass fresh final topology evaluation.

These tests do not prove native Unity transforms, Editor callbacks, import,
shader rendering, live Monster behavior, Recorder export, or Play Mode visual
acceptance. Inspector/import/Play Mode remain user-owned unless delegated.

## Static compilation

Use the established runtime assembly gate:

```sh
dotnet build Assembly-CSharp.csproj --no-restore -m:1 -nr:false -p:LangVersion=8.0
```

If the Unity-generated project has not yet included the two new source files,
include them through a temporary MSBuild import scoped only to Assembly-CSharp
for static compilation, or regenerate the project when Unity work is delegated.
The recorded run used that temporary import and passed with 0 warnings/errors.
No temporary project edits or generated build outputs belong in source control.

Task002's renderer and Task003's interaction/lifecycle integration are separate
feature tasks. These checks do not claim that the dashed line is visible yet.
