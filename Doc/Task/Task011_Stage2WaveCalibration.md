# Task011 - Stage2 Wave Calibration

Status: Completed on 2026-08-28; Stage2 V3 Progress, fixed-speed Wave,
Player Health, Build envelope, and Fixed Draft evidence are accepted

Depends on: Accepted Task010 Stage1 Wave Calibration; implemented Task010A
Monster Placement Route Continuity Refactor with schema-22 fixture integrity
reviewed per accepted Stage2 run

Unblocks: Task012 Stage3 Wave Calibration

Task010A's dedicated ten-fixture movement acceptance remains separately pending.
Task011's schema-22 records prove the movement cases encountered during Stage2
calibration; they do not claim that the complete Task010A fixture suite passed.

## 1. Goal And Accepted Interpretation

Calibrate Stage2 around a developed Cannon Core and useful Archer support while
rejecting Builds that spend all six Drafts without creating effective vertical
growth or a coherent combat package.

The Reference Build is a reproducible positive control, not a perfect or unique
answer. Final leak count also depends on Upgrade order and normal placement
timing. Stage2 therefore accepts a coherent Build envelope rather than requiring
the Reference sequence to produce zero leaks.

Player Health provides Build-maturation tolerance. It permits coherent Builds
with different formation timing to absorb a reviewed leak margin, but it must
not let structurally incoherent Builds clear merely because the measurement
ceiling is generous.

## 2. Accepted Stage2 V3 Configuration

| Field | Accepted value |
|---|---|
| Player Max Health | `6` |
| Progress Requirements | `[3, 3, 4, 4, 4]` |
| Post-initial Draft nodes | `3 / 6 / 10 / 14 / 18` |
| Total Draft opportunities | `6` |
| Total Monsters | `32` |
| Resolutions after final Draft node | `14` |
| Standard Spawn Interval | `2.5s` |
| Wave Delays | `[4s, 6s, 5s, 4s, 4s, 4s]` |
| New Monster Profiles | None |

The authored Stage pools remain cumulative Archer/Cannon pools. Both families
retain continuous Level 2 eligibility and every unlocked Archer/Cannon Basic
and Behaviour Upgrade remains available. Fixed sequences prove Build efficacy;
Task017 owns natural offer accessibility and probability.

## 3. Accepted Monster Wave

Task011 reuses three Task010-accepted fixed-speed Profiles without changing HP
or Move Speed:

| Wave | Profile | HP | Move Speed | Count | Spawn Interval | Wave Delay | Cumulative count |
|---:|---|---:|---:|---:|---:|---:|---:|
| 1 | Slime Lv1 | `60` | `0.25` | `3` | `2.5s` | `4s` | `3` |
| 2 | Slime Lv1 | `60` | `0.25` | `3` | `2.5s` | `6s` | `6` |
| 3 | Monster Plant Lv3 | `180` | `0.25` | `4` | `2.5s` | `5s` | `10` |
| 4 | Monster Plant Lv3 | `180` | `0.25` | `4` | `2.5s` | `4s` | `14` |
| 5 | Turtule Shell Lv4 | `400` | `0.25` | `8` | `2.5s` | `4s` | `22` |
| 6 | Turtule Shell Lv4 | `400` | `0.25` | `10` | `2.5s` | `4s` | `32` |

Waves 1-4 provide readable Cannon Level and support-formation feedback. The 18
HP400 Monsters in Waves 5-6 provide the vertical-growth and package-efficiency
boundary. Task011 adds no new HP tier; Bat Lv2 and Orc Lv5 remain outside this
fixed-speed Wave and retain their Task016 movement-identity ownership.

## 4. Accepted Reference And Placement Fixture

Reference final Build:

| Role | Count | TowerFamily | Final state |
|---|---:|---|---|
| Core | `1` | Cannon | L2, Faster Reload, Explosive Shell |
| Support | `2` | Archer | L1, no Upgrades |

Reference Fixed Draft sequence:

| Draft | Resolution node | Fixed result | Commit target |
|---:|---:|---|---|
| 1 | `0` | Cannon Tower | Deploy Core |
| 2 | `3` | Cannon Tower | Level Core to L2 |
| 3 | `6` | Archer Tower | Deploy Support A |
| 4 | `10` | Archer Tower | Deploy Support B |
| 5 | `14` | Faster Reload | Apply to Cannon Core |
| 6 | `18` | Explosive Shell | Apply to Cannon Core |

Accepted comparison placement cells:

| Tower | Footprint cells |
|---|---|
| Cannon Core | `(2,6) / (3,5) / (3,6)` |
| Archer Support A | `(2,1) / (2,2)` |
| Archer Support B | `(4,1) / (4,2)` |

`(2,1) / (2,2)` is the strongest reviewed Archer footprint and must not be
replaced with a weaker position merely to force zero placement-route recovery.
If a normal Stage-calibration deployment covers a live Monster, the legal,
diagnosed Task010A relocation remains gameplay behavior rather than a balance
penalty. Dedicated Task010A fixtures, not ordinary balance fixtures, own strict
`RequireZero` or `RequireDiagnosed` expectations.

## 5. Build And Sequence Envelope

The accepted evidence separates final Build structure from Upgrade order:

- Reference control: Cannon L2, two Archer L1 Supports, Faster Reload, then
  Explosive Shell.
- Early-Explosive sequence control: the same final Build, but Explosive Shell is
  applied at node `6`, the two Supports at nodes `10 / 14`, and Faster Reload at
  node `18`.
- Reinforced-Explosive coherent floor: replace Faster Reload with Reinforced
  Shells.
- Reload-Bouncing coherent alternative: replace Explosive Shell with Bouncing
  Shell.
- Support-concentration coherent alternative: replace two Archer L1 Supports
  with one Archer L2 Support while retaining the Reference Cannon package.

The early-Explosive sequence confirms that Upgrade order can change immediate
damage efficiency without making the final Reference composition the unique or
perfect Build. Coherent alternatives consume `0-4` Health under the measurement
ceiling and remain inside the accepted positive envelope.

## 6. Anti-pattern And Edge Fixtures

| Fixture | Final allocation | Purpose |
|---|---|---|
| Horizontal Sprawl | Cannon L1 x3 + Archer L1 x3; no Levels or Upgrades | Spend every Draft without vertical growth |
| Fragmented Investment | Cannon L2 + Extended Barrel; Archer L2 + Eagle Sight; no Behaviour | Split investment into two incomplete, range-only packages |
| Archer-only Edge | Archer L2 + Quick Draw + Scatter Arrow; second Archer L2; no Cannon | Measure a coherent single-family role-rejection edge |

Range-only Upgrades can be useful when coverage is missing, but Extended Barrel
and Eagle Sight do not improve damage or attack cycle. At already strong
placements, their opportunity value does not replace a coherent damage or
multi-target package.

Archer-only is retained as an edge Build rather than treated as the primary
Anti-pattern. Quick Draw and Scatter Arrow form a real package, but rejecting
Cannon leaves only the exact Player-Health margin.

## 7. Final Stage2 V3 Evidence

All listed runs used schema 22, Player Health `6`, Progress Requirements
`[3,3,4,4,4]`, and the accepted Wave table.

| Fixture | Result | Killed | Leaked | Unresolved | Final Health | Damage coverage | Decision |
|---|---|---:|---:|---:|---:|---:|---|
| Reference Control | Victory | `31` | `1` | `0` | `5 / 6` | `96.29%` | Accepted positive control |
| Early Explosive Sequence | Victory | `31` | `1` | `0` | `5 / 6` | `98.62%` | Accepted sequencing alternative |
| Reinforced-Explosive Coherent Floor | Victory | `28` | `4` | `0` | `2 / 6` | `90.49%` | Accepted weakest coherent sample |
| Archer-only Edge | Victory | `27` | `5` | `0` | `1 / 6` | `91.59%` | Accepted narrow edge |
| Horizontal Sprawl | Defeat | `24` | `6` | `2` | `0 / 6` | `69.78%` at terminal | Accepted primary Anti-pattern failure |
| Fragmented Investment | Defeat | `25` | `6` | `1` | `0 / 6` | `72.04%` at terminal | Accepted primary Anti-pattern failure |

The two expected-Defeat runs spawned all `32` Monsters, committed all six Draft
investments, and observed post-final-Build combat before Player Health reached
zero. Their unresolved Monsters at terminal Defeat are expected execution state,
not missing combat evidence.

Reference, Early Explosive, Coherent Floor, Archer-only, and Horizontal Sprawl
passed all schema-22 integrity checks. Fragmented Investment recorded one legal
`CoveredByNewFootprint` forced relocation while its fixture expectation remained
`RequireZero`; every other topology, gameplay-state, combat-ownership, lifecycle,
and accounting check passed. Its combat result is accepted together with the
earlier full measurement-ceiling run, while the false expectation mismatch is
retained explicitly rather than reported as a clean Task010A fixture pass.

## 8. Acceptance And Downstream Handoff

- The accepted Stage assets match Player Health `6`, Progress Requirements
  `[3,3,4,4,4]`, and the six-Wave table in this document.
- The Reference and multiple coherent alternatives clear without requiring one
  exact Upgrade package or Upgrade order.
- Player Health provides reviewed Build-maturation tolerance: coherent and edge
  Builds with `1-5` leaks clear, while the primary incoherent Builds fail on the
  sixth leak.
- Cannon development produces meaningful late-wave value against HP400 bodies;
  pure undeveloped spread cannot compensate through coverage alone.
- Every accepted positive fixture completes all `32` Monster resolutions.
- Expected Defeat is separated from Recorder integrity failure.
- Natural Draft offer frequency and accessibility remain Task017 work.
- Task012 may begin Stage3 calibration using the accepted Task010-Task011
  fixed-speed Profiles and Stage-calibration workflow.

Task011 is complete. Any later revision to Stage2 Player Health, Progress,
Monster counts, Wave delays, or accepted upstream Tower/Profile power explicitly
reopens this Task and its dependent Stage calibration evidence.
