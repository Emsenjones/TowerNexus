# Task006 - Monster Movement Identity And Wave Composition

Status: Planned; qualitative fast-assault and slow-durable identities proposed, exact Move Speed values pending controlled Play Mode evidence

Depends on: Task005 Monster Roster Baseline

Blocks: Task007 MonsterWaveConfig skeleton authoring

## 1. Goal

Decide whether the accepted HP120 Normal, HP240 Tough, and HP480 Elite roster should also carry deliberate movement-speed identities, and define the campaign Wave-composition convention that consumes those identities.

The current experience hypothesis is:

- each campaign Wave presents one Monster type;
- different Waves may present different Monster types;
- a faster Monster creates assault pressure;
- a slower high-health Monster creates a durable, readable tank window.

This Task isolates those decisions before Task007 creates six Stage Wave skeletons. It does not reopen Task005 Maximum Health unless movement evidence demonstrates that a speed revision destroys an accepted role.

## 2. Source Documents

- `Doc/Task/Task005_MonsterRosterBaseline.md`
- `Doc/Task/Task004_ElementalEffectAndBuffBaseline.md`
- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/07_MonsterSystem.md`

## 3. Baseline And Candidate Identities

| Role | Runtime Prefab | Accepted HP | Task005 Control Speed | Task006 Movement Hypothesis |
|---|---|---:|---:|---|
| Normal | Bat | `120` | `0.25` | Preserve as the stable Reference speed |
| Tough | Dragon | `240` | `0.25` | Faster assault candidate; exact value TBD |
| Elite | Golem | `480` | `0.25` | Slower durable candidate; exact value TBD |

The first candidate scan should use a narrow step before a stronger identity:

- Dragon: test `0.275`; test `0.30` only if the first change is not readable.
- Golem: test `0.225`; test `0.20` only if the first change is not readable.

These are diagnostic candidates, not accepted values. Faster movement shortens Tower exposure and increases threat; slower movement lengthens exposure and may partially cancel the Elite's HP advantage. The accepted result must preserve the Task005 Normal/Tough/Elite ordering rather than judging speed in isolation.

## 4. Homogeneous-Wave Authoring Hypothesis

For the six campaign Stages, one authored Wave should contain one unique Monster runtime template. A Wave may use multiple Spawn Entries only when those entries reference the same template to express timing groups. The next Wave may select another accepted Monster type.

This is initially a campaign content convention, not a universal Monster System restriction. `MonsterWaveConfig` remains structurally capable of mixed-type Spawn Entries unless this Task later proves that an explicit validation rule is useful and approves that change separately.

Homogeneous Waves make one movement identity readable at a time and keep Stage-local Count, Spawn Interval, and Wave Delay as independent calibration levers. Transitions between Waves must still be observed because a fast later Wave can catch a slow earlier Wave when their active windows overlap.

## 5. In Scope

- Per-role Move Speed candidates
- Route traversal time and Grid-per-second confirmation
- HP-role preservation after speed changes
- Homogeneous-Wave readability
- Cross-Wave catch-up, separation, and clustering
- Cannon projectile interception regression
- Cold/Slow interaction regression
- Buff continuity and Active Duration exposure
- Campaign Wave-composition convention

## 6. Out Of Scope

- New Monster mechanics, armor, resistance, collision, or blocking
- Final Stage counts, Spawn Intervals, Wave Delays, or difficulty
- Final Stage-specific Monster order
- Tower, Projectile, Buff, or Effect rebalance unrelated to a demonstrated movement regression
- Adding all ten visual Prefabs to the roster

## 7. Calibration Sequence

### Phase A - Traversal Identity

Use the Straight diagnostic route and one Monster per run with no effective Tower coverage. Compare the accepted `0.25` control against each candidate speed. Record complete registration coverage, full-health observation, leaked lifetime, and approximate Grid traversal.

Accept a candidate only when the difference is visually readable and the measured traversal ratio agrees with the authored speed direction.

### Phase B - HP Role Preservation

Repeat the smallest representative Tower comparisons for changed Dragon and Golem candidates. Verify that:

- Dragon remains Tough rather than becoming an unintended Elite rush unit;
- Golem remains Elite rather than becoming easier than Dragon because of excessive exposure;
- Bat remains the unchanged Reference control.

### Phase C - Projectile And Slow Regression

Run targeted Cannon Control and Cannon + Cold comparisons using the changed-speed candidates. Observe releases, intended/fallback/position-only Arc outcomes, successful Damage Applications, Slow snapshots, and final combat efficiency.

The purpose is regression protection: movement identity must not recreate the previously corrected negative Cannon + Cold interaction or rely on incidental interception of a following Monster.

### Phase D - Homogeneous-Wave Formation

Run a small same-type Wave for each accepted role using one fixed Count and Spawn Interval. Confirm that the Wave reads as one coherent movement identity and that spacing remains stable enough for the intended Tower, AoE, Piercing, and Buff interactions.

Then run a short sequence of different homogeneous Waves. Observe whether a faster later Wave catches a slower earlier Wave before the prior Wave resolves. Any resulting clustering must be an intentional Stage authoring option rather than an unnoticed global side effect.

## 8. Required Measurements

- Authored and observed Move Speed
- Route traversal lifetime
- Kill/leak result and final Health
- Successful Damage Applications
- Projectile releases and Arc target-relation outcomes where applicable
- Cold movement multiplier and Source Apply Cooldown snapshot
- Buff applications, natural expiries, maximum Stacks, and Overloads
- Same-Wave spacing and cross-Wave catch-up observation
- All schema-v8 completeness and integrity flags

## 9. Ownership

| Owner | Responsibility |
|---|---|
| Task005 | Accepted HP120/HP240/HP480 role baseline |
| Task006 | Movement identities and campaign homogeneous-Wave convention |
| Monster runtime template | Accepted per-type Move Speed |
| MonsterWaveConfig | Authored Wave template, Count, interval, and delay structure |
| Task007 | Six structural Wave skeletons consuming accepted identities |
| Task008-Task013 | Final Stage-local Monster order, Count, timing, and difficulty |

## 10. Acceptance Criteria

- Every changed Move Speed communicates a distinct tactical role in motion.
- Bat remains the stable `0.25` Reference unless a separate baseline revision is explicitly approved.
- Dragon and Golem retain the accepted Tough and Elite survival ordering.
- Faster movement creates pressure without silently combining excessive HP and insufficient exposure.
- Slower movement creates a durable tank window without erasing Elite threat through excessive Tower exposure.
- Cannon and Cannon + Cold remain functionally non-negative under accepted movement identities.
- Buff duration, stacking, and Overload behavior remain explainable from exposure rather than a runtime defect.
- The homogeneous-Wave convention is clear enough for Task007 to author six skeletons.
- Cross-Wave catch-up is either intentionally used or prevented through Stage-local timing.

## 11. Handoff

Task007 begins only after Task006 either accepts distinct Move Speeds or explicitly keeps all three roles at `0.25`. Task007 authors rough homogeneous-Wave skeletons from that result. Task008-Task013 then tune Stage-local Monster order, Count, Spawn Interval, Wave Delay, and difficulty without redefining global movement identities.
