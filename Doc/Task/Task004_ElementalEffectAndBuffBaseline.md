# Task004 - Elemental, Effect, And Buff Baseline

Status: Planned

Depends on: Task003 Tower growth and non-Elemental Upgrade baseline

## 1. Goal

Calibrate the four ElementTypes, their Tower-family application opportunities, lifecycle Effects, shared stacking, Overload, and Protection on top of the accepted Tower baseline.

The result must support Stage5 single-core learning and Stage6 matching-Element cooperation.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/System/15_BuffSystem.md`

## 3. Preconditions

- Stage5 and Stage6 Elemental pool authoring may remain unresolved during isolated content tests.
- Final Stage acceptance cannot proceed until Task006 authors and validates those pools.

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

## 5. Out Of Scope

- Final Stage5 or Stage6 Wave pressure
- Final Elemental Draft pool decision
- Player Progress Requirements
- Non-Elemental base or Behaviour redesign

## 6. Calibration Sequence

1. Fix one Tower, Monster, Map, and application opportunity.
2. Validate first application, refresh, stacking, Overload, and Protection.
3. Measure one source against the shared Elemental Buff.
4. Measure two matching Elemental sources with overlapping coverage.
5. Calibrate lifecycle Effect output only after stack cadence is stable.
6. Repeat across all four ElementTypes.
7. Regress all TowerFamilies that may apply each Element.

## 7. Required Measurements

- Time from first application to Overload
- Successful stack additions
- Cooldown-blocked applications
- Expiry before maximum stacks
- Overloads per fixed run
- Protection duration and blocked applications
- Overload Effect result
- Difference between single-source and dual-source stacking

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Tower Upgrade System | Elemental eligibility and accepted Tower state |
| Tower Runtime Combat | Elemental application opportunity |
| Effect System | One-shot lifecycle result execution |
| Buff System | Shared stacks, cooldown, Overload, and Protection |
| Task004 | Fixed comparisons and accepted Elemental baseline |

## 9. Required Proposal Table

Codex prepares the first-pass table from accepted Tower and Upgrade baselines:

| ElementType | Application Opportunity | Stack/Cooldown Proposal | Overload Proposal | Expected Single-Source Result | Expected Dual-Source Result | Play Mode Decision |
|---|---|---|---|---|---|---|
| Each supported ElementType | Record owning attack opportunity | First-pass estimate | First-pass Effect result | Observable but slower | More reliable matching-Element stacking | Keep or revise |

## 10. Execution Collaboration

- The user supplies the intended Elemental feeling and owns Unity asset authoring plus lifecycle observation.
- Codex fills the initial Element, Effect, and Buff table, calculates expected stack timing, and proposes the smallest revisions from single-source and dual-source results.
- Exact Stage5 and Stage6 pool composition remains a Task006 output rather than a prerequisite for isolated Task004 tests.

## 11. Unity Authoring Checklist

- Validate all Elemental UpgradeDefinitions.
- Validate shared BuffDefinition references.
- Test two different TowerFamilies using the same ElementType.
- Test mismatched ElementTypes independently.
- Validate StackApplied, Overload, EnteredProtection, and removal results.
- Record accepted Effect and Buff values.

## 12. Acceptance Criteria

- Every ElementType has a readable normal phase and Overload result.
- Shared stacks work across matching TowerFamilies.
- Stage5 can demonstrate Elemental application and stacking.
- Stage6 can use two matching Elements to reach Overload more reliably.
- Protection is observable and prevents immediate reapplication for the same definition.
- No Elemental value change silently rewrites Base Tower identity.

## 13. Validation

- Fixed-condition single-source runs
- Fixed-condition dual-source runs
- Lifecycle ordering checks
- Cross-family shared-stack checks
- Static asset validation

## 14. Review Note

Final Stage5 and Stage6 Elemental pool composition is derived and authored by Task006 from the Blueprint's required capabilities.
