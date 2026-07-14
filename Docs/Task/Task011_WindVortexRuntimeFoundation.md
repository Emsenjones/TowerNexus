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

- Add a dedicated WindVortexConfig referenced by the narrow SpawnWindVortex Effect action.
- WindVortexConfig owns a complete runtime prefab with WindVortexBehaviour, lifetime, movement speed, target-search radius, damage radius, tick interval, arrival threshold, and on-tick EffectDefinition.
- Spawn at the supplied owner Transform position and retain only source context required for on-tick Effect execution.
- Do not yet wire the spawn action into Wind Elemental content.

### WindVortex Behavior

- Start lifetime at spawn, even when no target exists. Expiry ends the entity before any further movement or tick execution in that frame.
- Use the shared Effect target-validity contract: a target must be non-null, active, alive, and not otherwise resolved by Monster lifecycle cleanup.
- Search for valid monsters in authored target-search radius; randomly lock one and move directly toward its current Transform position. Initialization, target invalidation, and arrival reacquire immediately; an idle Vortex searches again on a small fixed internal interval.
- Do not use Monster pathfinding or move any monster.
- On arrival, immediately search again and prefer a new valid target when one exists. The reached target may be selected when it is the only valid candidate.
- If a locked target dies, arrives, is destroyed, or otherwise becomes invalid, clear it and immediately search again.
- If no target exists, remain at the current position, keep its visual alive, and continue checking for candidates. Do not choose an empty world position or drift.
- Independently, after each complete authored tick interval, resolve all valid monsters in authored damage radius and execute the required single-target on-tick Effect once per target. Preserve timer remainder and process every crossed interval safely. Targets are not one-shot: the same monster may be damaged on later ticks while it remains in range.
- Expire when lifetime ends regardless of movement or target state.

## Shared Constraints

- Do not create a generic moving EffectZone, target-selection, projectile, or Elemental-reaction framework.
- Do not create a generic ElementalBuff inheritance framework.
- Gameplay timing, target detection, movement, and damage radius remain WindVortex runtime authority; its complete runtime prefab may initially be test-only and does not drive gameplay.
- The on-tick Effect does not apply Windcut stacks or trigger Elemental reactions by default.

## Out Of Scope

- Wind BuffDefinition and Wind Elemental TowerUpgradeDefinitions.
- Generic area-zone behavior.
- Object pooling and final VFX polish.

## Acceptance Criteria

- WindVortex starts and expires correctly whether or not it ever finds a target.
- A resolved arriving or cleanup-state monster is rejected by the same validity contract used by Effect targeting.
- It directly tracks a valid monster, safely clears invalid targets, and remains stationary while no target is available.
- Arrival reacquisition prefers another target when available.
- The target-search radius and damage radius remain independent.
- Every damage tick affects all valid monsters in damage radius, with no per-vortex already-hit set.
- The first damage tick occurs only after one complete tick interval; timer remainder survives variable frame durations.
- Its on-tick Effect does not recursively apply Elemental stacks.
- WindVortex does not alter Monster movement, pathfinding, node state, or lifecycle.
