# ArchitectureRefactor Closeout — 2026-09-10

Status: Completed, with remaining targeted acceptance explicitly waived by the user.
This index preserves evidence access; System and Balance remain design authority.

## Historical Snapshot

Pre-retirement commit: `9cc5572598ef27021f8841df7ec44b133c0c372d`.
It preserves code, Unity scene/assets, six Task documents plus their index, and
nine schema-25 reports together. Original paths: `Doc/Task/` and
`Doc/GamePlayRecord/`. Retirement removes those working-tree materials and changes
new Recorder output to `Doc/CombatReports/`.
The snapshot is retained in local Git history; this operation does not push it.

Implementation commits: Task001–002 `7bec5ca`; Task003–004 `c621362`;
Task005 `630fcad`; Task006 and overall acceptance `9cc5572`.

## Evidence And Limits

- Task001: upgrade commit contracts and Stage1-to-Stage3 smoke.
- Task002: query/cache checks and Stage1 deployment/Monster relocation smoke.
- Task003: Draft trace/ownership checks, two-Stage smoke and successful Tower and
  Upgrade consumption after Pending-view rebuild.
- Task004: Submission contracts and two Stage1 reports, including a retained
  Archer Tower Draft at Defeat with consumed/pending token reconciliation.
- Task005: four Stage4/Stage5 reports with combat and Wind evidence. Natural Draft
  build accessibility remains a separate design concern.
- Task006: Editor/non-Editor conditional C# compilation, inherited regressions,
  64 investment cases, 17 lifecycle/export assertions and 40 historical route/
  integrity checks. Three fresh reports cover Victory/Defeat, Wind/Cold and entity
  observations, and retained Piercing Arrow at terminal. All pass 32 integrity
  flags. User confirmed partial-run disable/re-enable yields no report and the
  next full Battle, with Recorder continuously enabled, records normally.

The user explicitly waived all remaining unexecuted acceptance for this iteration,
including controlled Recorder on/off gameplay/RNG comparison and Unity profiling,
unexercised native fault/lifecycle cases and the queued-export disable race.
These are not passed tests. No measured Unity performance gain, exhaustive lifecycle
coverage or device/IL2CPP acceptance is claimed. Future concrete bugs or unexpected
behavior trigger focused diagnosis and repair. Archived Tasks retain detailed
evidence, fixture conditions and coverage limits.

## Retrieval And Reproduction

```sh
git ls-tree -r --name-only 9cc5572598ef27021f8841df7ec44b133c0c372d -- Doc/Task Doc/GamePlayRecord
git show 9cc5572598ef27021f8841df7ec44b133c0c372d:Doc/Task/Task006_CombatDiagnosticsIsolation.md
git show 9cc5572598ef27021f8841df7ec44b133c0c372d:Doc/GamePlayRecord/Task006_Stage1_RecorderToggle_Recovery_01.json
```

Use archived scene/assets and report fixture fields when reproducing an old run;
do not infer exact fixtures from RunNames alone. `Tests/Task006/integrity.py` reads
its four pinned Task005 report fixtures from this snapshot into a temporary
directory, so working-tree cleanup preserves regression coverage. Other harnesses
remain under `Tests/`. New runs write to `Doc/CombatReports/`; `Doc/Task/` is empty
and ready for future bounded work. Both directories retain only `.gitkeep` files.
