# Task006 diagnostics isolation checks

Run `python3 Tests/Task006/run.py` from the repository root with Python and Mono
(`csc`, `mono`). No Unity project or scene is changed by this harness.

- `replay.py` compiles the actual extracted investment reconciliation methods and
  schema-25 DTO. Its 64 fixed inputs compare with `baseline-investment.json`,
  generated only from `git show 630fcad:...` old methods/DTO, never the refactor.
  The initial 32 cases were captured before extraction; the other 32 extend
  selection-attempt coverage from the pinned old oracle. `--capture` explicitly
  regenerates this pinned baseline and is not part of routine tests.
- `LifecycleTests.cs` compiles production scope, captured source and exporter with
  minimal Unity/Editor doubles. It checks nested draining, synchronous Retry
  identity restoration, source destruction, capture failure, disabled gates,
  duplicate acquisition, detached export, collision handling and I/O exceptions.
  The printed 100,000-iteration gate timing is a Mono microbenchmark only.
- `integrity.py` compiles production route accumulator/integrity methods and DTO.
  It replays the four preserved Task005 JSON route inputs and compares the entire
  resulting route subtree. Seven route checks and damage checks also run on each
  report, including a deliberately corrupted damage count that must be rejected.

This is scoped replay, not a complete old/new Recorder event-stream comparison.
It does not establish native destruction timing, real MonoBehaviour subscription
ordering, final-hit/Elemental full-report equivalence, fresh Recorder exports,
Unity allocation/frame-time savings, on/off gameplay parity or iOS compilation.
Archived Task006 Section 9 lists the original native acceptance steps. Remaining
checks were explicitly waived at iteration closeout. Task001–005 suites
retain their separate documented boundaries; old reports do not become new runs.

Historical JSON fixtures are loaded using `git show` from commit
`9cc5572598ef27021f8841df7ec44b133c0c372d` into the test temporary directory.
The harness does not depend on live `Doc/CombatReports` output. A clone needs this
commit in its Git history. See `Doc/History/ArchitectureRefactor_Closeout.md`.
