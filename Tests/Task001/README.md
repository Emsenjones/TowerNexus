# ArchitectureRefactor Task001 managed checks

Run from the repository root with Python 3 and Mono (`csc`, `mono`) installed:

```sh
python3 Tests/Task001/run.py --baseline
python3 Tests/Task001/run.py
```

The baseline mode reads the original `TowerInstance` and `TowerUpgradeState` from
`10eaf6f` using `git show`, compiles them, and reproduces an Upgrade recorded before
a throwing observer prevents the subsequent consumption call. It does not change
the checkout. Keep that history reachable to rerun the baseline.

The current mode uses the actual Submission entry and compiles `TowerUpgradeSystem`, `TowerInstance`, and
`TowerUpgradeState`, `PendingDraftCollection`, `DraftResult`, `DraftAttemptToken`,
and the production investment observation directly from this checkout. It runs 30 assertions-based cases
for acceptance/rejection, exactly-once commitment, required refresh failure,
optional event/presentation exceptions, nested requests, cancellation flushing,
and Debug authority. Generated binaries live only in a temporary directory.

`Tests/Task004/BoundaryDoubles.cs` substitutes native Unity, combat, definitions,
Tower instantiation and Battle termination boundaries. Submission, membership,
Pending ownership and Upgrade commitment execute production code. Consequently these tests validate the transaction
core against the actual Pending model, including cross-owner rejection and Stop
during preparation. They do not prove native view cleanup, prefab validation, native entity creation, subtype refresh, timer ratios,
Recorder export, or Game Flow event ordering. They are managed checks, not Unity
Edit Mode or Play Mode evidence. The remaining native acceptance matrix is in
`Doc/Task/Task001_UpgradeCommitConsistency.md`. No production fault hooks are added.
