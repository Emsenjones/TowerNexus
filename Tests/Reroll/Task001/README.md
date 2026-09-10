# Re-roll Task001 animation validation

Run from the repository root:

```sh
python3 Tests/Reroll/Task001/run.py
python3 Tests/Reroll/Task001/assets.py
python3 Tests/Reroll/Task001/build.py
```

`run.py` requires Python, Mono csc, and mono. It compiles the complete current
UIAnimationStep, UIAnimationPlayer, ToastUI, DamageNumberUI, and DamageNumberManager
classes. Reflection supplies serialized configuration and calls the real host Update/lifecycle methods and plain player Tick. Controlled Unity clock/property/lifecycle boundaries and a
small easing double are explicit in BoundaryDoubles.cs; this is not native Unity
or DOTween playback. The 92 assertions include actual player execution, not only
validation/sorting, and exercise completion reentrancy, failed replay, clock
selection, reversed endpoint steps, multi-boundary updates, zero-duration
completion/cancellation, and instance owner cleanup.

`assets.py` pins migration input to `1a044e9`. It checks the exact authored damage
step payload, old prefab and script GUIDs, unchanged existing component blocks,
local reference resolution and GameObject ownership, plus Toast input flags.
Keep that baseline reachable. This does not replace Unity's importer.

`build.py` uses the current generated Unity project and installed dependencies.
A temporary source-list overlay includes the new files and removes the old path
until Unity regenerates its csproj. It builds runtime/Editor assemblies, then
compiles the changed classes against actual Unity/DOTween/TMP references with
and without UNITY_EDITOR. It never launches Unity or edits generated projects.
Generated managed test binaries and source overlays live only in temp directories.

Pending native evidence: Unity import and Inspector references; Editor Preview;
real battle damage and placement; Toast fade/motion while paused and at changed
speed; repeated replay; early disable/destruction; no UI raycast interception;
no native lifecycle or completion residue. These checks belong to
[Task001](../../../Doc/Task/Task001_ReusableUIAnimationAndToast.md), not the managed
assertion count. No iOS build, device test, or visual test is claimed.

UIAnimationPlayer is a serializable plain class. The hosts forward clocks and
cancel on disable; tests exercise both host paths. Serialized checks verify that
no standalone player component remains. The Toast asset uses the user-selected
name `UiPrefab_ToastItem.prefab`.
