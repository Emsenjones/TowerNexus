# Monster dashed-path integration evidence

```sh
python3 Tests/MonsterDashedPath/Integration/run.py
python3 Tests/MonsterDashedPath/Presentation/run.py
python3 Tests/Task004/run.py
python3 Tests/Task002/run.py
```

The integration harness executes the production Session, Presenter, immutable
snapshot, and preview-result freshness checks. Its 18 cases use controlled Map,
A*, Validator, transform, clock, and renderer doubles. It proves the selection,
retention, failure recovery, origin/drag rejection, revision invalidation, and
coordinate refresh contracts. It does not execute actual A*, scene creation,
Camera projection, native input, or Unity renderer/shader behavior.

The Task004 suite executes actual Submission code with a display boundary double
that throws during snapshot construction/publication or replaces the runtime
session during capture. It checks committed reward/accounting and existing
notification continuation. Production Controller Update/completion and the earliest
Coordinator revocation method are extracted and run against controlled input/UI
and lifecycle boundaries. These are managed contract checks, not Play Mode.

The historical Task002 query harness executes real A* against campaign fixtures
and compares baseline/current routes. Its Candidate double does not prove final
submission freshness; that evidence remains in the actual Candidate/Submission
Task004 cases.

## Manual Unity setup and acceptance

Assign `Monster Dashed Path Prefab` on the scene's BattleRuntimeCoordinator to
`Assets/Art/Prefab/DashedLinePath/Prefab_DashedLinePath.prefab`. The reference is the
GameObject Prefab asset; its root must contain MonsterDashedPathPresenter. Its explicit
LineRenderer and Material/Texture references must follow the Presentation README.
No separate scene line or Camera/Canvas parenting is required; Battle creates a
single presentation root under the Active Map (outside NodesRoot) and releases it.

Native checks remain user-owned:

- Import/Console shader and C# diagnostics, one line at Battle begin before spawning.
- Formal Normal, unchanged Normal appearance on pickup, full-footprint candidate,
  first/repeated blocked red retaining geometry, recovery, and all fallback cases.
- Eligible/ineligible level-up targeting uses formal route in Normal; Upgrade Draft stays Normal.
- Success uses post-commit route; cancellation/rejection returns Normal without occupancy
  or reward changes. Check multi-cell Towers and preview/formal node-order equality.
- Draft modal freezes movement/submission and flow. Release inside modal cancels on
  close; keeping the button held resumes dragging. No click-through deployment.
- Technical failure hides/diagnoses once per repeated reason; same-topology blocked
  recovery restores retained geometry; a new topology establishes formal baseline.
- Camera pan does not detach the line or trigger query refresh. Map spatial changes
  refresh the retained line, including blocked state. Flow survives state/route changes.
- Stop, Victory, Defeat, failure, Retry, and next Stage leave no old line, requests,
  retained geometry, red state, or phase; no duplicate line across repeated lifecycle.
- Actual appearance and target-build/device behavior are separate acceptance evidence.
