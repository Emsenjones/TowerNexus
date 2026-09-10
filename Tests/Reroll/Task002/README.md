# Stage Free Re-roll validation

Run from the repository root:

```sh
python3 Tests/Reroll/Task002/run.py
python3 Tests/Reroll/Task002/ui.py
python3 Tests/Reroll/Task002/real_cards.py
python3 Tests/Reroll/Task002/assets.py
python3 Tests/Reroll/Task002/build.py
python3 Tests/Task003/run.py
python3 Tests/Task004/run.py
```

`run.py` compiles the complete production DraftSystem, real Pending collection,
upgrade eligibility and submission ownership with explicit native boundaries.
Editor and diagnostics-disabled builds cover shared/reset/zero budgets,
source-preserving refresh, Pending reservations, zero-probability categories,
stale overlapping rewards, repeated requests, preparation failures, RNG failure
semantics, cancellation/restart, exact set selection and identical replacements.
The Editor variant also extracts production Recorder handlers, JSON DTOs,
accumulator, summaries and integrity methods. Combat timing/context are doubles;
set capture, opportunity aggregation, budget and selected-set reconciliation run
from production code. Terminal capture is injected at the observation callback; the production
CombatDiagnosticScope performs actual depth/drain/identity routing. Coordinator failure
authority is extracted from production; its terminal implementation is a test boundary.
This is not a real combat export or Unity serialization test.

`ui.py` executes the complete production DraftUI with explicit native boundaries:
dual-button visibility, active callback rebinding, exhausted no-op, one replayed
Toast, inactive prepared views, rollback, retirement, single-use commit and
closure/cancellation. It does not emulate EventSystem press ordering, layout,
Unity deferred destruction, actual TowerContentUIItem lifecycle or tween playback.
Task001 separately exercises real animation/Toast owner code with clock/ease doubles.

`assets.py` checks all seven authored Stage budgets and real prefab local references,
existing ButtonPressFeedback values and empty persistent click actions. It does
not modify layouts or claim native Inspector acceptance.

`build.py` invokes the existing temporary-project Editor build and also compiles
the complete runtime assembly without UNITY_EDITOR against installed Unity,
TMP and DOTween references, including the project's built firstpass assembly.
No Unity process or generated project is modified. `--player-only` reuses the
already-built firstpass assembly when only that conditional check is needed.

Archived Task003's 300 fixed-seed sampling traces retain their original baseline
oracle. Its Pending fixture changed only to supply the new current-set and
open-view/pause callback contract; assertions were not removed. Task004 retains
complete deployment/level/upgrade consumption and cancellation coverage.

Native handoff: inspect Window_DraftUI's assigned buttons/count/Toast references;
test presses/releases and exhausted feedback, final-count switching, repeated
Toast during pause, retry and Stage transitions. Export schema-27 zero/one/multiple
Re-roll battles and verify generation/consumption/investment integrity separately.

Optimization assertions also cover isolated preparation, notification reentry through
final cleanup, Initial/Level-Up committed presentation failure, stale failure isolation,
independent request starts, complete final results, request/set joins, Natural membership
and weights, and outgoing/incoming Battle observation isolation. The start boundary
excludes recursive calls rejected at the held gameplay guard.

`real_cards.py` combines whole production DraftUI and TowerContentUIItem. It exercises
hidden-card rejection, post-activation selection binding and retired-card cleanup with
explicit managed effective-activation/graphics boundaries. Native Unity EventSystem,
full hierarchy callback ordering, rendering and deferred destruction remain unverified.

Current evidence: 535 Editor / 484 player model/Recorder assertions; 19 DraftUI checks;
22 production-card integration checks. See Task002 Section 11 for full acceptance status.
