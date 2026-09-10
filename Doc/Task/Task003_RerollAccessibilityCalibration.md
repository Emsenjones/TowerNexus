# Task003 - Re-roll Accessibility Calibration

Iteration: Re-roll System
Status: Planned; calibration has not started.
Dependencies: [Task002 - Stage Free Re-roll System](Task002_StageFreeRerollSystem.md)
with functional, authoring, and Recorder acceptance; Task001 transitively.

## 1. Goal And Sources

Evaluate whether limited free Re-rolls help players form a Reference or coherent
alternative Build in time, especially in Stage4-Stage6. Decide Keep or Revise for
each Stage's budget using real offer, investment, and combat evidence.

Sources:

- [Stage Blueprint](../Balance/00_StageDesignBlueprint.md), Section 2.1 and Stage lessons.
- [Tower Growth And Upgrade Identity](../Balance/01_TowerGrowthAndUpgradeIdentity.md).
- [Draft System](../System/08_DraftSystem.md), rules and per-set observation.
- [Stage System](../System/02_StageSystem.md), calibration interpretation.
- [CombatMathV2 closeout](../History/CombatMathV2_Closeout.md), historical evidence limits.

## 2. Scope And Ownership

This Task owns representative Natural Draft calibration, budget decisions,
named reports, and synchronization of approved values into Stage assets and the
Blueprint. Begin with Stage1-Stage6 counts `1 / 1 / 2 / 3 / 4 / 5`.

Keep category probabilities, pools, eligibility, progression, Monster/Wave
values, health, Tower/Upgrade power, Maps, and placement rules unchanged during
the initial budget comparison. Use strategically reasonable legal placement.
Changing these other variables requires a separately reviewed scope; do not
silently compensate for a sampling, application, or placement defect with budget.

Task002 owns functional and Recorder defects. A broken run is diagnostic, not
balance evidence. Fixed Draft remains without Re-rolls and measures conditional
Build efficacy; Natural runs measure accessibility and player decisions.

## 3. Pre-run Review Gate

Before collecting acceptance cohorts, present a calibration plan that states:

- Exact current Stage configuration, build revision, report schema, seeds, and
  budget for each fixture; historical labels are not current executable proof.
- Reference-seeking and coherent-alternative-seeking build/placement plans for
  Stage4-Stage6, including core/support ownership and upgrade targets.
- A practical Re-roll decision policy: desired eligible rewards, when to keep
  a useful alternative, when to save/spend budget, and how deviations are logged.
- Exact cohort size, run names, and reviewed success/build-timing margins before
  collection. Include repeated independent seeds per intended path rather than
  selecting one successful seed after the fact.
- Any unchanged Fixed control or zero-budget comparison needed to resolve
  uncertainty. Same seed with different Re-roll history is not an identical
  future-offer sequence or an isolated per-click causal comparison.

No numerical win-rate guarantee or universal Reference completion target is
established by this document. Review the cohort boundary before native runs;
do not infer approval to automate Unity or device testing from this Task.

## 4. Execution Phases

### Phase A - Stage4-Stage6 Initial Cohorts

Run the reviewed Reference/alternative policies at budgets `3 / 4 / 5`.
After each run inspect its actual Recorder report before changing a value.
Record the relevant offered sets, Re-roll decisions, chosen and consumed rewards,
build completion/progress node, placement and coverage, leaks, and final result.

Separate causes:

| Finding | Interpretation |
|---|---|
| Desired identity not eligible | Level, family, layer, or Pending capacity boundary; more rolls cannot unlock it |
| Eligible but not offered before its useful node | Potential accessibility/budget limitation |
| Useful offers rejected or investment fragmented | Selection/allocation issue |
| Useful reward held too long or never consumed | Application/timing issue |
| Coherent timely Build still fails | Inspect placement, coverage, and combat before changing budget |
| Invalid event/count/terminal evidence | Diagnose and repair before using the run for calibration |

### Phase B - Focused Budget Revisions

Choose Keep or Revise based on repeated attributable evidence. Change one
Stage's budget at a time and rerun the affected reviewed cohort. Preserve the
previous configuration and reports for comparison. More budget is not by itself
proof of better play; inspect whether it changes useful offer timing and coherent
investment. Repeated identical sets are valid, not sampler failures.

### Phase C - Early Stages And Closeout

Check Stage1-Stage3 at `1 / 1 / 2` for usability and appropriate challenge.
Their Initial Draft shows all Tower candidates, so confirm that early Toast
feedback preserves the budget for later Drafts. Check budget reset across Retry
and Stage progression as an integration smoke, without substituting it for
Task002's functional acceptance.

Record final per-Stage decisions, remaining limitations, and the evidence needed
to revisit them. Synchronize approved values and document changes to the initial
hypothesis without presenting old reports as new acceptance.

## 5. Authoring And Evidence Handoff

- [ ] User-confirmed native run plan and actual Stage assets match each fixture.
- [ ] Re-roll and Toast references from Task002 are wired and visually usable.
- [ ] Fixed Draft override is off for Natural cohorts; record seed settings.
- [ ] Reports use unique descriptive RunNames under `Doc/CombatReports/` and
  identify actual Stage, budget, policy, revision, and seed in report or run notes.
- [ ] Inspect gameplay records after each run: Draft order, usage, consumption,
  investment, placement/route changes, leaks, and integrity.

Unity layout, Inspector changes, import/reserialization, Play Mode, and device
runs remain user-owned unless explicitly delegated. Any Codex-authored value
change must be distinguished from the subsequent imported/native result.

## 6. Acceptance And Validation

- [ ] Task002 feature/report acceptance is complete before balance conclusions.
- [ ] Reviewed Stage4-Stage6 cohorts are completed with report integrity and
  build/placement interpretation, including failures rather than only successes.
- [ ] Every tested Stage has an evidence-backed Keep/Revise decision, and every
  revised budget has affected-cohort follow-up.
- [ ] Stage1-Stage3 checks and retry/progression integration evidence are recorded.
- [ ] Candidate availability, choice, consumption, timely Build formation, and
  combat outcome remain separate; superseded sets are not counted as rewards.
- [ ] Final Stage asset values and Blueprint agree, with explicit remaining
  accessibility limitations and no unsupported completion-rate claim.
- [ ] JSON parsing/integrity and focused document/asset diff checks pass. Native
  evidence is named separately from static checks; waivers are explicit.

## 7. Review And Completion Evidence

Completion requires a reviewed conclusion for the collected cohort, synchronized
values, and named evidence. If the intended accessibility is still not met,
record that limitation and the next decision rather than marking the objective
achieved because more Re-rolls were added. Broader balance changes remain separate.

Current evidence: Initial budget hypothesis only; no calibration runs performed.
