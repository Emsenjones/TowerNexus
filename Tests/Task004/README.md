# Task004 complete submission contracts

```sh
python3 Tests/Task004/run.py
python3 Tests/Task001/run.py
python3 Tests/Task003/run.py
python3 Tests/Task002/run.py
```

Python 3 and Mono (`csc`, `mono`) are required. Compilation outputs live in temporary
directories. The production sources compiled by `build.py` include complete
Submission, collection, candidate capture, final validator, Pending, Upgrade core,
Tower instance/state and immutable investment observation.

The 23 Task004 cases cover full Deployment/Level Up/Upgrade acceptance and rejection,
read-only membership, truthful technical failure, final path evaluation after preview,
geometry/owner/Map/frame invalidation, multi-cell offset/rotation/scale fixtures,
readiness failure or Stop/rebind, authorized vs nested submission, standalone Stop,
reentrant/throwing release, and immutable evidence before each deployment callback.

The suite also extracts the production Controller's full `CompletePlacement` and
`CompletePlacementCore` methods and interaction predicate. The native view-cleanup
boundary injects nested requests and a destruction exception to verify guard lifetime
and guaranteed cancellation. Production Recorder subscription/unsubscription blocks
are executed against the stable real Submission event source across Stage resets,
re-enable and a changed runtime reference. This tests binding, not Recorder export.

`BoundaryDoubles.cs` supplies Unity objects, authored definitions, Map/path search,
Monster preparation and Tower instantiation/combat. Candidate capture and validation
execute real code; the boundary returns a prepared Tower using the captured pose.
These controlled fixtures are not native transform/physics, actual Monster relocation,
subtype combat refresh, Play Mode or fresh JSON acceptance. Task002 separately runs
real Map index/pathfinding and final topology logic against its native hierarchy doubles.

Task001's 30 Upgrade fault/regression cases now run through the same complete Submission
entry using these shared boundaries. Task003 retains 14 Pending/orchestration checks
and 300 baseline Draft traces; its former post-preflight investment segments have been
replaced by the stronger complete-entry lifecycle tests here.
