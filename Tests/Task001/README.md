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

The current mode compiles the actual `TowerUpgradeSystem`, `TowerInstance`, and
`TowerUpgradeState` directly from this checkout. It runs 29 assertions-based cases
for acceptance/rejection, exactly-once commitment, required refresh failure,
optional event/presentation exceptions, nested requests, cancellation flushing,
and Debug authority. Generated binaries live only in a temporary directory.

`ContractDoubles.cs` substitutes Unity, HUD, placement, combat, definitions, and
Battle termination boundaries. Consequently these tests validate the transaction
core's behavior against those contracts; they do not prove real HUD collection
mutation, prefab validation, native entity creation, subtype refresh, timer ratios,
Recorder export, or Game Flow event ordering. They are managed checks, not Unity
Edit Mode or Play Mode evidence. The remaining native acceptance matrix is in
`Doc/Task/Task001_UpgradeCommitConsistency.md`. No production fault hooks are added.
