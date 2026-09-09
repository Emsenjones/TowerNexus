# Task002 query correctness and managed baseline

Run from the repository root with Python 3 and Mono (`csc`, `mono`):

```sh
python3 Tests/Task002/run.py
```

`--baseline-only` runs the original query path from Git revision `10eaf6f`.
The script reads the current Stage1 (8x8) and Stage4 (14x14) prefab node records,
requires the complete expected node count, and uses identical footprints in both
versions. It does not change the checkout, run Unity, or persist generated binaries.

The actual GridNodeBehaviour, AStarPathfindingService, TowerPlacementValidator,
and current MapRuntimeNodeIndex are compiled. The Map adapter extracts production
query/index/lifecycle/Editor-reconciliation methods directly from each version of
MapGeneratorBehaviour. It substitutes fixture configuration and omits unrelated
Camera, theme, visual generation and destruction code. Unity hierarchy/transform
and Editor event APIs are doubles; the algorithm is not a Python reimplementation.

The comparison checks complete path sequences, preview/final placement decisions,
and every available connector's complete path-to-join. Current-code checks cover
revision stability, occupancy/reset, role changes, duplicate rejection and repair,
exact current footprint validity, rebinding, authoring notification reconciliation,
visual-only child changes, all-Normal scaffolds, ownership transfer, and release.

The stationary benchmark runs one cold preview query and 99 identical repeats
following fixture warm-up. It reports scan/search counts, managed elapsed time,
and per-thread allocated bytes. `GC.GetAllocatedBytesForCurrentThread` includes
fixture-double hierarchy traversal allocations. These bytes and timings are NOT
Unity Profiler measurements, standalone/player performance, or device improvements.
No Recorder runs in this harness. Cache hits still allocate footprint List/HashSet
storage; zero-GC is not claimed.

Native Unity acceptance remains in Task002 Section 8: actual inactive-node/editor
callback behavior and Undo/Redo, Clear/Generate with deferred destruction, genuine
Tile/Feature refresh, moving-Monster placement, Stage lifecycle and native profiling.
The doubles cannot establish those runtime outcomes or replace their acceptance.

Task004 adaptation: final-query fixtures provide resolved candidate snapshots to the
production validator. Baseline source still uses Preview. Actual candidate capture,
owner/Map checks and prepared geometry execute in Task004's integration suite.
