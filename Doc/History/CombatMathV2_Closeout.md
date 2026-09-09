# CombatMathV2 Historical Evidence Index

Retired from the working tree: 2026-09-09.

This index locates historical evidence and records its limits. Current System
and Balance documents own current design; current Unity assets contain executable
parameters. Historical Tasks and reports do not override those authorities.

## Preserved Version

- Annotated tag: `milestone/combatmath-v2-2026-09-09`
- Commit: `b9fc1a9e5bc8aabfabfd4b17e6c622d323a4a0f3`
- [Complete source and assets](https://github.com/Emsenjones/TowerNexus/tree/b9fc1a9e5bc8aabfabfd4b17e6c622d323a4a0f3)
- [Historical Task directory](https://github.com/Emsenjones/TowerNexus/tree/b9fc1a9e5bc8aabfabfd4b17e6c622d323a4a0f3/Doc/Task)
- [Historical GamePlayRecord directory](https://github.com/Emsenjones/TowerNexus/tree/b9fc1a9e5bc8aabfabfd4b17e6c622d323a4a0f3/Doc/GamePlayRecord)

The tag was pushed to origin and its peeled commit verified before cleanup.
The snapshot contains 22 Task-directory files and 521 GamePlayRecord-directory
files (520 JSON reports and one metadata file). The reports include experiments,
superseded fixtures, and historical failures as well as
accepted evidence. The old Task README is not a reliable final status summary:
for example, it still describes Task017 as in progress. Read the owning Task's
closeout and the actual report fixture together.

## Evidence Lookup

| Question | Historical source |
|---|---|
| Damage formula and naked Level/Upgrade comparisons | Task001-Task004 and their named reports |
| Elemental normal value, matching cooperation, and shared reactions | Task005-Task007, Task007A/B/C; Task007 Phase F/G/H acceptance |
| Stage Reference, coherent alternatives, Anti-patterns, placement, and Fixed Draft sequences | Task010-Task015, each with its own accepted run list |
| Placement route continuity | Task010A and subsequent per-run route integrity; focused acceptance remains incomplete |
| Drone Holding and cleanup | Task015A closeout and its named schema-24 regression matrix |
| Natural Draft offers, selection, Pending, consumption, and outcomes | Task017 Section 11 and 44 schema-25 Phase A reports |

Task010-Task015 accepted the fixed-speed Stage campaign. Task017 accepted the
Natural Draft runtime and representative integration evidence, not a statistical
guarantee of Reference Build availability or completion. Stage6's accepted Fixed
Fire Reference uses Magic and Cannon Cores; matching Cold and complementary
Cold-plus-Fire Builds also passed. Unmatched Elements are not universally an
Anti-pattern.

## Known Limits And Unexecuted Checks

- Task016 Fast-Monster/Wave substitution was deferred, not implemented or
  accepted. Its document is retired by the user's decision. If resumed, create
  a new task from current design and revalidate affected combat and Natural
  Draft fixtures. Its omission does not block the fixed-speed campaign.
- Task017 Stage5/Stage6 natural coherent paths remain narrow. The Stage6 cohort
  contains two Defeats and does not establish a reliable completion rate.
  Rerolls, pity, and other accessibility assistance remain future experience work.
- Stage4 historical Fixed and Natural evidence used six Health; the preserved
  asset uses five. That evidence must not be described as acceptance at five.
  The first four Task017 Stage5 runs used temporary five Health and are diagnostic
  only; the accepted restored fixture uses six.
- Task004 Phase E additional manual eligibility and mid-entity refresh scenarios
  were explicitly waived. Changes to eligibility, Pending consumption, package
  capacity, or live refresh require relevant new validation.
- Task006's remaining standalone retarget, opener-miss, Blast Rounds,
  multiple-Drone, Final Dive, and four-Element matrix was waived in favor of
  downstream calibration. This is not proof of each individual edge case.
- Task007B/C inherited requirement-99 fixture exceptions for
  `levelUpCountMatches`, `levelUpResolutionNodesMatch`, and
  `finalPlayerLevelMatches`. They are not blanket integrity passes. Task007C
  ordinary gameplay smoke was accepted; its remaining focused edge transactions
  were deferred rather than claimed as executed.
- Task010A reports implementation complete but its dedicated ten-fixture route
  matrix and Stage1 Reference movement regression remain pending. Later Stage
  records cover encountered cases, not the entire focused matrix. Retirement
  does not resolve or waive that acceptance gap.
- Task015A was accepted through live Stage6 Holding observation and named
  regressions. Its original deterministic L1-L11 cases were not individually
  executed and remain an explicitly waived diagnostic catalog.

## Reuse

Use the historical Task to select an accepted fixture and named reports, then
inspect the report's actual parameters, schema, Draft sequence, placement,
integrity, and outcome. Filenames alone do not establish comparability. Re-run
on current code to accept a refactor; changed balance goals require new fixtures
and evidence derived from current Balance and System documents.

For read-only local inspection after fetching the tag:

```sh
git fetch origin tag milestone/combatmath-v2-2026-09-09
git show milestone/combatmath-v2-2026-09-09:Doc/Task/Task017_NaturalDraftOfferProbabilityCalibration.md
git ls-tree -r --name-only milestone/combatmath-v2-2026-09-09 -- Doc/GamePlayRecord
```
