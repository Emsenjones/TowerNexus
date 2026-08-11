# Task004 - Elemental, Effect, And Buff Baseline

Status: Planned; Task004 owns the Elemental power budget and explicit single-source, maximum single-Tower, matching-source, and Overload tests, while exact values remain pending

Depends on: Task003 Tower growth and non-Elemental Upgrade baseline

## 1. Goal

Calibrate the four ElementTypes, their Tower-family application opportunities, lifecycle Effects, shared stacking, Overload, and Protection on top of the accepted Tower baseline.

The result must support Stage5 single-core learning and Stage6 matching-Element cooperation.

Task004 is the sole calibration owner for Elemental power targets. Task003 supplies accepted non-Elemental control builds but does not test Elemental Cores, matching sources, or Overload.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/System/15_BuffSystem.md`

## 3. Preconditions

- Stage5 and Stage6 Elemental pool authoring may remain unresolved during isolated content tests.
- Final Stage acceptance cannot proceed until Task006 authors and validates those pools.
- Task003 must first accept the Basic-plus-Behaviour control state used beneath an Elemental Upgrade.

## 4. In Scope

- Tower-family Elemental UpgradeDefinitions
- Elemental application opportunities
- Burning, Cold, ElectricShock, and Windcut
- Stack maximum
- Buff duration
- Buff apply cooldown
- StackApplied lifecycle output
- Overload Effect output
- Protection duration and blocked applications
- Single-source and dual-source stacking comparisons
- Cross-TowerFamily contribution to one shared Elemental Buff
- One-Basic plus one-Behaviour non-Elemental Reference control
- Single-source Elemental Reference Core
- Maximum single-Tower Elemental stress build
- Matching-source pair, mismatched-source control, and non-overlap control

## 5. Out Of Scope

- Final Stage5 or Stage6 Wave pressure
- Final Elemental Draft pool decision
- Player Progress Requirements
- Non-Elemental base or Behaviour redesign

## 6. Calibration Sequence

1. Fix one accepted Task003 Reference non-Elemental Core with exactly one named Basic and one named Behaviour Upgrade, plus one Monster, Map, placement, and application opportunity.
2. Measure that non-Elemental control before adding one Elemental Upgrade.
3. Add exactly one Elemental Upgrade to the same Tower and measure the single-source Elemental Reference Core.
4. Validate first application, refresh, stacking, Overload, and Protection.
5. Apply the complete Task003 combined Basic and combined Behaviour stress sets plus one Elemental Upgrade to one Tower and measure the maximum single-Tower Elemental stress build.
6. Measure two matching single-source Elemental Reference Cores with overlapping coverage against shared targets.
7. Run mismatched-Element and non-overlapping controls against the same fixture.
8. Calibrate lifecycle Effect output only after stack cadence is stable.
9. Repeat across all four ElementTypes and regress all TowerFamilies that may apply each Element.

### 6.1 Required Elemental Test Builds

| Test Build | Exact Construction | Purpose |
|---|---|---|
| Non-Elemental Reference control | One Tower with exactly one accepted Task003 Basic plus one accepted Task003 Behaviour Upgrade | Starting line before Elemental value |
| Single-source Elemental Reference Core | The same control Tower plus exactly one Elemental Upgrade; no second Tower applies that ElementType | Normal Stage5-style Elemental value |
| Maximum single-Tower Elemental stress build | The accepted Task003 combined Basic and combined Behaviour stress sets on one Tower, plus exactly one Elemental Upgrade | Cumulative single-Tower ceiling |
| Matching-source pair | Two single-source Elemental Reference Cores using the same ElementType with overlapping effective coverage and shared targets | Shared stacks and Overload |
| Mismatched-source control | Two otherwise comparable Elemental Reference Cores using different ElementTypes | Prove matching identity matters |
| Non-overlap control | Two matching Elemental Reference Cores without shared-target opportunity | Prove effective cooperation matters |

The isolated Task004 fixture may differ from Task003 when Elemental lifetime or shared-target observation requires it, but every comparison keeps its own control and Elemental states identical except for the tested Elemental content. Prefer `40` Monsters and a readable `2.5s` Spawn Interval; raise fixture health rather than compressing spawn presentation when a `40 / 40` ceiling hides the gain.

The maximum single-Tower Elemental stress build has the proposed `4.0x` realized-value ceiling relative to its naked same-family control. Task004 derives the narrower single-source Elemental Reference Core target from the accepted Task003 control before value authoring. Matching sources and Overload may exceed the normal single-Tower ceiling only while two sources, matching ElementType, overlapping effective coverage, and shared-target opportunity are present.

## 7. Required Measurements

- Time from first application to Overload
- Successful stack additions
- Cooldown-blocked applications
- Expiry before maximum stacks
- Overloads per fixed run
- Protection duration and blocked applications
- Overload Effect result
- Difference between single-source and dual-source stacking
- Non-Elemental Reference control, single-source Elemental Reference Core result, and realized-gain ratio
- Fully stacked single-Tower result against its naked same-family control
- Matching, mismatched, and non-overlap pair results
- Whether the fixed result reaches the measurement ceiling

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Tower Upgrade System | Elemental eligibility and accepted Tower state |
| Tower Runtime Combat | Elemental application opportunity |
| Effect System | One-shot lifecycle result execution |
| Buff System | Shared stacks, cooldown, Overload, and Protection |
| Task004 | Elemental power budget, named fixed comparisons, and accepted Elemental baseline |

## 9. Required Proposal Table

Codex prepares the first-pass table from accepted Tower and Upgrade baselines:

| ElementType | Test Build | Application Opportunity | Stack/Cooldown Proposal | Overload Proposal | Control Result | Test Result | Realized Gain / Lifecycle Result | Decision |
|---|---|---|---|---|---|---|---|---|
| Each supported ElementType | One named required build from Section 6.1 | Record owning attack opportunity | First-pass estimate | First-pass Effect result | Named fixed control | Filled after Play Mode | Calculated gain, stacks, or Overloads | Keep or revise |

## 10. Execution Collaboration

- The user supplies the intended Elemental feeling and owns Unity asset authoring plus lifecycle observation.
- Codex fills the initial Element, Effect, and Buff table, calculates expected stack timing, and proposes the smallest revisions from single-source and dual-source results.
- Exact Stage5 and Stage6 pool composition remains a Task006 output rather than a prerequisite for isolated Task004 tests.

## 11. Unity Authoring Checklist

- Validate all Elemental UpgradeDefinitions.
- Validate shared BuffDefinition references.
- Test two different TowerFamilies using the same ElementType.
- Test mismatched ElementTypes independently.
- Test every named build in Section 6.1 without substituting an unnamed extra condition.
- Validate StackApplied, Overload, EnteredProtection, and removal results.
- Record accepted Effect and Buff values.

## 12. Acceptance Criteria

- Every ElementType has a readable normal phase and Overload result.
- Shared stacks work across matching TowerFamilies.
- Stage5 can demonstrate Elemental application and stacking.
- Stage6 can use two matching Elements to reach Overload more reliably.
- A single-source Elemental Reference Core has measurable normal value before a second matching source exists.
- The maximum single-Tower Elemental stress build respects the single-Tower power ceiling accepted through this Task.
- Any gain above that ceiling is attributable to matching-source cooperation rather than the Elemental Layer name alone.
- Protection is observable and prevents immediate reapplication for the same definition.
- No Elemental value change silently rewrites Base Tower identity.

## 13. Validation

- Fixed-condition single-source runs
- Maximum single-Tower Elemental stress runs
- Fixed-condition matching, mismatched, and non-overlap pair runs
- Lifecycle ordering checks
- Cross-family shared-stack checks
- Static asset validation

## 14. Review Note

Task004 owns matching-source and Overload calibration, including accepted Required Levels, authored parameters, candidate targets, and test evidence. `01_TowerGrowthAndUpgradeIdentity.md` remains the qualitative growth contract. Final Stage5 and Stage6 Elemental pool composition is derived and authored by Task006 from the Blueprint's required capabilities.
