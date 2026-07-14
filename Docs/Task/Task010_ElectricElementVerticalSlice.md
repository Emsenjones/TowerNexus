# Task010 - Electric Element Vertical Slice

## Objective

Implement Electric as a complete Elemental vertical slice using shared Electric Buff data, four tower-family Elemental upgrades, stack-driven extra damage, and the reviewed Overcharged lightning sequence.

## System References

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Task006 shared Elemental application entry is stable.
- Task007 MonsterStatusBar and Buff visual feedback are stable.

## Scope

### Shared Electric Data

- Create one shared ElectricShock BuffDefinition and shared Electric Effect data.
- Create the necessary Electric apply, stack, overload, and presentation Effect data.
- Electric gameplay after application is independent of source tower identity.

### Four-Tower Content

- Create one Electric Elemental TowerUpgradeDefinition for Archer, Cannon, Magic, and Drone.
- Each definition uses the shared Elemental apply pipeline and shared Electric Buff data.

### Electric Behavior

- A later successful Electric stack can trigger configured extra Electric damage through Buff lifecycle data.
- First application and pure refresh do not trigger stack-only behavior.
- Overcharged resolves valid nearby monsters, then ExecuteMultiTargetEffect immediately applies LightningStrike to a configured random, non-repeating target count.
- Overcharged is an instant overload effect, not a persistent Buff.

## Shared Constraints

- Implement only the reviewed ExecuteMultiTargetEffect action: no execution interval, temporary scene runner, target-selection mode, generic reaction, skill-sequencing, or chaining framework.
- LightningStrike and other Electric reaction damage do not apply ElectricShock stacks by default.
- Do not create generic ElementalBuff inheritance.

## Out Of Scope

- Cold movement behavior.
- WindVortex and Storm Shift.
- Generic EffectZone or moving-effect frameworks.
- Behaviour Layer Phase 2 upgrades.

## Acceptance Criteria

- All four Electric upgrades can be authored, drafted, and applied through the shared Elemental pipeline.
- Extra Electric damage occurs only after a successful later stack, never on first apply, pure refresh, or blocked application.
- Overcharged performs the configured immediate multi-target LightningStrike executions and ends cleanly.
- Electric stack, overload, Protection, UI, and VFX behavior are visible and correct.
- Electric reaction damage never recursively applies ElectricShock stacks by default.
