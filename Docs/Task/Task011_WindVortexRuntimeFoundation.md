# Task011 - WindVortex Runtime Foundation

## Objective

Implement the reviewed persistent, moving WindVortex gameplay entity and its narrow spawn action. Do not create a generic moving EffectZone framework or wire it into the Wind Elemental vertical slice yet.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Shared Effect execution and target-validity rules are stable.
- Task007 Buff visual and debugging foundation is stable.

## Scope

### Authoring And Spawn Contract

- Add a dedicated WindVortexDefinition referenced by the narrow SpawnWindVortex Effect action.
- WindVortexDefinition owns lifetime, movement speed, target-search radius, damage radius, tick interval, arrival threshold, on-tick EffectDefinition, and visual prefab.
- Spawn at the supplied owner hit/reference position and retain source context for on-tick Effect execution.
- Do not yet wire the spawn action into Wind Elemental content.

### WindVortex Behavior

- Start lifetime at spawn, even when no target exists.
- Search for valid monsters in authored target-search radius; randomly lock one and move directly toward its current hit/reference position.
- Do not use Monster pathfinding or move any monster.
- On arrival, immediately search again and prefer a new valid target when one exists. The reached target may be selected when it is the only valid candidate.
- If a locked target dies, arrives, is destroyed, or otherwise becomes invalid, clear it and immediately search again.
- If no target exists, remain at the current position, keep its visual alive, and continue checking for candidates. Do not choose an empty world position or drift.
- Independently, at every authored tick, resolve all valid monsters in authored damage radius and execute the on-tick Effect. Targets are not one-shot: the same monster may be damaged on later ticks while it remains in range.
- Expire when lifetime ends regardless of movement or target state.

## Shared Constraints

- Do not create a generic moving EffectZone, target-selection, projectile, or Elemental-reaction framework.
- Do not create a generic ElementalBuff inheritance framework.
- Gameplay timing, target detection, movement, and damage radius remain WindVortex runtime authority; the visual prefab follows that entity and does not drive gameplay.
- The on-tick Effect does not apply Windcut stacks or trigger Elemental reactions by default.

## Out Of Scope

- Wind BuffDefinition and Wind Elemental TowerUpgradeDefinitions.
- Generic area-zone behavior.
- Object pooling and final VFX polish.

## Acceptance Criteria

- WindVortex starts and expires correctly whether or not it ever finds a target.
- It directly tracks a valid monster, safely clears invalid targets, and remains stationary while no target is available.
- Arrival reacquisition prefers another target when available.
- The target-search radius and damage radius remain independent.
- Every damage tick affects all valid monsters in damage radius, with no per-vortex already-hit set.
- Its on-tick Effect does not recursively apply Elemental stacks.
- WindVortex does not alter Monster movement, pathfinding, node state, or lifecycle.
