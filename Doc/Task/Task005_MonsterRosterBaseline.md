# Task005 - Monster Roster Baseline

Status: Planned

Depends on: Task002 Base Combat baseline; Task003 and Task004 for regression coverage

## 1. Goal

Create the smallest useful Monster roster for Stage calibration while preserving one stable Reference Monster.

The first pass should use maximum health as the primary differentiation axis and introduce movement-speed differences only when they create a required tactical role.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/System/15_BuffSystem.md`

## 3. In Scope

- Reference Monster preservation
- Monster maximum-health tiers
- Shared or deliberately different movement speed
- Fragile, Normal, Tough, or Elite role candidates
- Tower TTK and attack-count breakpoints
- Route traversal time
- Elemental and Behaviour regression against each accepted Monster role

## 4. Out Of Scope

- MonsterWaveConfig
- Stage-specific Monster counts
- New armor, resistance, or broad crowd-control frameworks
- Player Progress Requirements
- Final Stage difficulty

## 5. Calibration Sequence

1. Preserve the Task002 Reference Monster.
2. Define target survival time for each proposed Monster role.
3. Estimate health from effective reference damage and target survival time.
4. Check discrete shot, contact, and burst breakpoints.
5. Keep movement speed common for the first pass.
6. Introduce a speed variant only if one Stage target requires it.
7. Regress Base, Behaviour, and Elemental interactions.

## 6. Required Measurements

- Maximum health
- Movement speed
- Route traversal time
- TTK by representative Tower states
- Number of hits or bursts to resolve
- Effect of slow, lock, and Elemental state
- Whether the variant creates a distinct tactical decision

## 7. Ownership

| Owner | Responsibility |
|---|---|
| Monster content | Authored health, speed, Prefab, and presentation |
| Monster System | Movement, lifecycle, and resolution behavior |
| Effect and Buff Systems | Existing interactions with valid Monster targets |
| Task005 | Role justification, comparative measurements, and accepted roster |

## 8. Required Proposal Table

Codex prepares the first-pass Monster table from the accepted Reference Monster and target survival experience:

| Monster Role | Proposed Max Health | Proposed Move Speed | Target Survival Window | Expected Tactical Meaning | Observed TTK | Decision |
|---|---:|---:|---|---|---|---|
| Reference or proposed role | First-pass estimate | Common speed unless justified | Expected hits, bursts, or seconds | Reason this role exists | Filled after Play Mode | Keep or revise |

## 9. Execution Collaboration

- The user describes how durable or urgent each Monster role should feel and owns Unity asset authoring plus Play Mode runs.
- Codex fills the initial health and movement table from accepted Tower output, checks shot and burst breakpoints, and revises the smallest necessary parameter set.
- Movement speed remains shared unless the requested experience and test evidence justify a tactical speed variant.

## 10. Unity Authoring Checklist

- Identify the canonical Reference Monster.
- Record every MonsterDefinition and Prefab used by the roster.
- Normalize unintended movement-speed differences.
- Author health tiers from measured survival targets.
- Validate all Monster runtime references.
- Test death and Target-arrival resolution.

## 11. Acceptance Criteria

- One stable Reference Monster remains available.
- Every additional Monster role has a clear reason to exist.
- Maximum health produces understandable survival tiers.
- Movement-speed differences are deliberate rather than incidental.
- No Monster requires a new systemic mechanic merely to justify its identity.
- All accepted Monsters remain compatible with existing Effect and Buff contracts.

## 12. Validation

- Fixed Tower-versus-Monster TTK runs
- Route traversal comparison
- Resolution and Player consequence checks
- Effect and Buff regression
- Static asset validation

## 13. Review Note

If health-only differentiation is sufficient, this Task should stop there. Simplicity is an accepted result.
