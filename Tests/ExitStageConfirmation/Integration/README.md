# Exit Stage production integration verification

Run from the repository root:

```sh
python3 Tests/ExitStageConfirmation/Integration/run.py
python3 Tests/ExitStageConfirmation/Integration/assets.py
python3 Tests/ExitStageConfirmation/Presentation/run.py
python3 Tests/ExitStageConfirmation/Transitions/run.py
python3 Tests/TowerInfoWindow/ContentRefinement/build.py
```

Integration passes 76 assertions. It executes the complete production Exit entry,
view/session, inspection, Game Flow, Stage, coordinator integration partial, and
exact coordinator permission/acceptance/terminal/failure/stop/release methods.
The actual HUD OnEnable body emits the actual notification into the actual
coordinator rebind helper. Subscription is enabled explicitly through its real
helper; the coordinator MonoBehaviour's full OnEnable is a source-wired native
boundary, not an executed full-scene lifecycle assertion.

Unlike the Task005 boundary suite, this suite does not install fake production
prepare/cleanup callbacks or a fake request delegate. The gameplay preparation
endpoint calls the production binder at the same seam used by the real coordinator.
Native hierarchy, Camera readiness, Map creation, gameplay preparation/start, and
simulation reset remain explicit doubles. Full runtime compilation separately
checks actual Unity references and the complete coordinator.

Cases cover hidden prepared readiness, stable modal ownership without self-blocking,
real Exit clicks and Cancel/Restart/Menu routes, current/fresh binding, same-Battle
HUD recovery, group partial rollback, prepared/active failure, terminal reentry,
owner loss, destroyed view, missing authoring, protected operation rejection,
Opening-before-pause exclusion, listener stability, and evidence publication with
reentrant release. Static `assets.py` checks saved Prefab identities/configuration;
it does not establish visual layout, native serialization or raycast behavior.

Related regression evidence: local Exit 138 assertions; transitions 156; inspection
109; whole Camera 138; Draft 560 Editor/509 player; Submission 34 contracts;
Effect/binding 27; Tower-state 14 plus five coordinator cases; modal pause 81 plus
five spawning/five attack cases. Assertions were retained in existing suites;
external boundary stubs and extraction dependencies were extended for new guards.

Device smoke test: on 2026-10-22 the user reported successful iOS export and
iPad mini 6 testing through Xcode with no issues observed. No per-case checklist
or device logs were supplied; see Task006 for the evidence boundary.

Individually unconfirmed: Unity Play Mode, genuine scene pause/resume and full same-Stage reset,
Initial Draft/Wave sequencing, root/component lifecycle, native EventSystem,
multi-touch on iPad mini 6, layout and user visual acceptance. None are inferred
from these managed assertions or from compilation.
