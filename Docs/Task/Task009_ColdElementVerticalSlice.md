# Task009 - Cold Element Vertical Slice

## Objective

Implement Cold as the second complete Elemental vertical slice using shared Cold Buff data, four tower-family Elemental upgrades, active slow, Frozen overload, and visible Buff feedback.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Task006 shared Elemental application entry is stable.
- Task007 MonsterStatusBar and Buff visual feedback are stable.
- Task008 Slow and Frozen control API is stable.
- Task009-1 Buff lifecycle Effect binding refactor is stable.

## Scope

### Shared Cold Data

- Create one shared stackable Cold BuffDefinition and one shared non-Elemental, non-stackable Frozen BuffDefinition.
- Create the necessary Cold apply, lifecycle, overload, and presentation Effect data.
- Cold gameplay after application is independent of source tower identity.

### Four-Tower Content

- Create one Cold Elemental TowerUpgradeDefinition for Archer, Cannon, Magic, and Drone.
- Each definition uses the shared Elemental apply pipeline and the shared Cold Buff data.
- Cannon applies Cold to every valid monster resolved by its explosion.

### Cold Behavior

- Cold Applied sets the reduction-only move-speed multiplier through Task008's safe API.
- Cold Overload executes an Apply Frozen Effect whose ApplyBuff action applies Frozen.
- Frozen Applied sets movement lock to true through Task008's safe API; Frozen Removed sets it to false.
- Cold EnteredProtection and Removed clear the move-speed multiplier through Task008's safe API.
- First Cold apply does not trigger stack-only behavior; a Frozen refresh does not create a second lock.
- Cold Protection blocks only Cold restacking and does not block damage by default.

## Shared Constraints

- Do not create generic crowd-control, Elemental reaction, or ElementalBuff inheritance frameworks.
- Do not add multi-element towers, replacement, or reroll rules.
- Keep base attack damage in its existing direct path.

## Out Of Scope

- Electric, Wind, Overcharged, and WindVortex.
- New generic movement controls beyond Task008.
- Behaviour Layer Phase 2 upgrades.

## Acceptance Criteria

- All four Cold upgrades can be authored, drafted, and applied through the shared Elemental pipeline.
- Cold slow is visible and ends safely when the Buff leaves Stacking state.
- Frozen overload locks movement temporarily without bypassing Monster System ownership.
- Cold stack, cooldown, overload, Protection, UI, and VFX behavior are visible and correct.
- Cold reaction damage and overload do not recursively apply Elemental stacks.
