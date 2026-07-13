# Task012 - WindVortex Runtime Foundation

## Objective

Implement the reviewed WindVortex gameplay entity and its narrow spawn action without implementing a generic moving EffectZone framework or the full Wind Elemental content slice.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Shared Effect execution and target validity rules are stable.
- Task007 Buff visual and debugging foundation is stable.

## Scope

### Spawn Contract

- Provide the dedicated WindVortex spawn action used later by Wind Buff StackApplied behavior.
- Spawn at a supplied monster hit/reference position.
- Do not yet wire the action into Wind Elemental upgrade content.

### WindVortex Behavior

- At spawn, randomly lock one alive monster inside the authored search radius; if none exists, choose a random world position inside that radius.
- Before the first monster hit, move toward the locked monster or chosen position.
- The first hit on any monster stops tracking, locks current movement direction, and begins straight-line travel.
- Each WindVortex hits each monster at most once.
- It expires after its authored short movement lifetime.
- Its hit effect is a normal Effect execution and does not apply Windcut stacks or trigger Elemental reactions by default.

## Shared Constraints

- Do not create generic moving EffectZone, target-selection, projectile, or Elemental reaction frameworks.
- Do not create a generic ElementalBuff inheritance framework.
- WindVortex moves only itself; it does not move monsters or use Monster pathfinding.
- Keep its behavior and data limited to the approved WindVortex contract.

## Out Of Scope

- Wind BuffDefinition and Wind Elemental TowerUpgradeDefinitions.
- Storm Shift relocation.
- Generic area-zone behavior.
- Full final VFX polish or pooling.

## Acceptance Criteria

- WindVortex follows the reviewed random-lock, first-hit transition, straight-line, hit-once, and expiry rules.
- A missing target produces safe random-position travel rather than failure.
- A first hit on a non-locked monster still transitions the Vortex to straight-line travel.
- Vortex hit effects do not recursively apply Elemental stacks.
- WindVortex does not alter Monster movement, pathfinding, node state, or lifecycle.
