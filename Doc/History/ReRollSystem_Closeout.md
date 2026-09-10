# Re-roll System Demo Closeout

Date: 2026-09-11
Status: Completed for the user-approved Demo scope; broader balance validation deferred.

Pre-retirement snapshot: Git commit `ae50bd2` preserves the final Task001-Task003
documents together with code, assets, and reports. The Task documents were then
removed from the active workspace at the user's request.

## Accepted scope and limits

Each Stage has an observed successful Build and placement route, with repeated
successes for some paths. This is not a measured stable win rate, guaranteed
Natural offer accessibility, or proof that every Tower family can serve as a core.
The user explicitly accepted this narrower Demo scope instead of further balance
iterations. Stage5 non-Magic core diagnosis is deferred; Stage6 need not realize
two same-Element cores to pass this Demo gate. Historical learning goals and Fixed
controls are retained, not presented as fresh universal acceptance.

## Final authored decisions

| Stage | Free Re-rolls | Tower slot probability | Decision and evidence |
|---|---:|---:|---|
| 1 | 1 | 0.500 | Keep; 02 wins, 01 fails; budget need not be exhausted |
| 2 | 1 | 0.600 | Keep; 01 wins |
| 3 | 2 | 0.667 | Keep; 01, 02, 04, 07, 08 win; 07/08 share final Build but differ in timing/placement |
| 4 | 3 | 0.600 | Revise probability from 0.714; 03, 03_01, 05 win; budget unchanged |
| 5 | 4 | 0.667 | Keep; normal-budget 01 and 04_01 win with Magic cores |
| 6 | 5 | 0.571 | Keep; 01 and 02 win with single Fire and mixed Cold/Fire alternatives |

Stage5 exploratory 07-10 used 99 configured rolls and are excluded from normal
budget acceptance. The live Stage5 asset is restored to 4. Stage4 03_01 and Stage5
04_01 are actual filenames with repeated RunNames; they are not silently renamed
as 04 and 05. Report fixtures take precedence over labels. Stage4 before/after
runs differ in seeds and decisions and do not establish isolated causality.

## Feature and validation evidence

- Task001: reusable animation and Toast implemented. User confirmed native
  DamageNumber visuals, Toast playback, and tuned UI presentation.
- Task002: user confirmed Initial Re-roll, exhausted-count interaction, no-other-
  candidates Toast, Retry reset, and corrected button child layout. Some of these
  manual checks have no standalone JSON; they are accepted user-reported evidence.
- Schema-27 reports include successful multi-roll transactions, consumption, and
  preserved Stage budgets. Stage1/2 no-other-candidate requests preserve budget.
  Task002_FreeReroll_01.json provides the earlier single-LevelUp-roll record.
- Earlier managed validation passed: Task001 92 assertions; Task002 535 Editor /
  484 player, UI 19, real-card managed harness 22; runtime/Editor compilation and
  historical Pending/submission regressions. These are prior results, not tests
  rerun during this documentation closeout, and not native lifecycle proof.
- All listed reports were parsed again at closeout and their exported integrity
  flags are true. Per-run Draft reconciliation was performed during report reviews.
- No new iOS/device, exhaustive native fault injection, or controlled per-policy
  seed cohort is claimed. Original broader Task gates are waived/deferred for this
  Demo closeout, not silently marked as executed. Runtime rules remain unchanged.

## Named native reports

Counts are observations, not population win-rate estimates. Budget column is
configured / successfully consumed; 99-roll runs are exploratory.

| Report | Result | Final Health | Tower probability | Budget / used |
|---|---|---:|---:|---:|
| [Task003_Stage1_Reroll_01](../CombatReports/Task003_Stage1_Reroll_01.json) | Defeat | 0 | 0.500 | 1 / 0 |
| [Task003_Stage1_Reroll_02](../CombatReports/Task003_Stage1_Reroll_02.json) | Victory | 3 | 0.500 | 1 / 0 |
| [Task003_Stage2_Reroll_01](../CombatReports/Task003_Stage2_Reroll_01.json) | Victory | 2 | 0.600 | 1 / 0 |
| [Task003_Stage3_Reroll_01](../CombatReports/Task003_Stage3_Reroll_01.json) | Victory | 4 | 0.667 | 2 / 2 |
| [Task003_Stage3_Reroll_02](../CombatReports/Task003_Stage3_Reroll_02.json) | Victory | 6 | 0.667 | 2 / 2 |
| [Task003_Stage3_Reroll_03](../CombatReports/Task003_Stage3_Reroll_03.json) | Defeat | 0 | 0.667 | 2 / 0 |
| [Task003_Stage3_Reroll_04](../CombatReports/Task003_Stage3_Reroll_04.json) | Victory | 1 | 0.667 | 2 / 0 |
| [Task003_Stage3_Reroll_05](../CombatReports/Task003_Stage3_Reroll_05.json) | Defeat | 0 | 0.667 | 2 / 0 |
| [Task003_Stage3_Reroll_06](../CombatReports/Task003_Stage3_Reroll_06.json) | Defeat | 0 | 0.667 | 2 / 1 |
| [Task003_Stage3_Reroll_07](../CombatReports/Task003_Stage3_Reroll_07.json) | Victory | 1 | 0.667 | 2 / 1 |
| [Task003_Stage3_Reroll_08](../CombatReports/Task003_Stage3_Reroll_08.json) | Victory | 5 | 0.667 | 2 / 0 |
| [Task003_Stage4_Reroll_01](../CombatReports/Task003_Stage4_Reroll_01.json) | Defeat | 0 | 0.714 | 3 / 3 |
| [Task003_Stage4_Reroll_02](../CombatReports/Task003_Stage4_Reroll_02.json) | Defeat | 0 | 0.714 | 3 / 3 |
| [Task003_Stage4_Reroll_03](../CombatReports/Task003_Stage4_Reroll_03.json) | Victory | 3 | 0.600 | 3 / 3 |
| [Task003_Stage4_Reroll_03_01](../CombatReports/Task003_Stage4_Reroll_03_01.json) | Victory | 5 | 0.600 | 3 / 3 |
| [Task003_Stage4_Reroll_05](../CombatReports/Task003_Stage4_Reroll_05.json) | Victory | 1 | 0.600 | 3 / 3 |
| [Task003_Stage5_Reroll_01](../CombatReports/Task003_Stage5_Reroll_01.json) | Victory | 6 | 0.667 | 4 / 3 |
| [Task003_Stage5_Reroll_02](../CombatReports/Task003_Stage5_Reroll_02.json) | Defeat | 0 | 0.667 | 4 / 4 |
| [Task003_Stage5_Reroll_03](../CombatReports/Task003_Stage5_Reroll_03.json) | Defeat | 0 | 0.667 | 4 / 4 |
| [Task003_Stage5_Reroll_04](../CombatReports/Task003_Stage5_Reroll_04.json) | Defeat | 0 | 0.667 | 4 / 2 |
| [Task003_Stage5_Reroll_04_01](../CombatReports/Task003_Stage5_Reroll_04_01.json) | Victory | 6 | 0.667 | 4 / 4 |
| [Task003_Stage5_Reroll_06](../CombatReports/Task003_Stage5_Reroll_06.json) | Defeat | 0 | 0.667 | 4 / 4 |
| [Task003_Stage5_Reroll_07](../CombatReports/Task003_Stage5_Reroll_07.json) | Defeat | 0 | 0.667 | 99 / 12 |
| [Task003_Stage5_Reroll_08](../CombatReports/Task003_Stage5_Reroll_08.json) | Defeat | 0 | 0.667 | 99 / 21 |
| [Task003_Stage5_Reroll_09](../CombatReports/Task003_Stage5_Reroll_09.json) | Victory | 6 | 0.667 | 99 / 24 |
| [Task003_Stage5_Reroll_10](../CombatReports/Task003_Stage5_Reroll_10.json) | Defeat | 0 | 0.667 | 99 / 13 |
| [Task003_Stage6_Reroll_01](../CombatReports/Task003_Stage6_Reroll_01.json) | Victory | 2 | 0.571 | 5 / 5 |
| [Task003_Stage6_Reroll_02](../CombatReports/Task003_Stage6_Reroll_02.json) | Victory | 3 | 0.571 | 5 / 5 |
