# Task012 - Wind Element Vertical Slice And Elemental Content Audit

## Objective

Complete Wind as the final first-version Elemental vertical slice, then audit the full Elemental content matrix across Fire, Cold, Electric, Wind and Archer, Cannon, Magic, Drone.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Task006 shared Elemental application entry is stable.
- Task007 MonsterStatusBar and Buff visual feedback are stable.
- Task011 WindVortex runtime foundation is stable.

## Scope

### Shared Wind Data

- Create one shared Windcut BuffDefinition and shared Wind Effect data.
- Create the Wind apply Effect, StackApplied parent Effect, single-target directional Wind attack Effect, no-target origin feedback when authored, WindVortex Overload Effect, and Wind presentation data.
- Wind gameplay after application is independent of source tower identity.

### Wind Behavior

- First Wind application creates the initial Windcut stack only.
- Each later successful stack refreshes duration, adds one stack, and invokes StackApplied.
- StackApplied resolves nearby valid monsters from its authored radius, excludes the Windcut owner, randomly selects up to one remaining monster, and deals authored reaction damage through the single-target Wind attack Effect.
- The Wind attack visual begins at the owner anchor and faces the selected target using local +Z forward. When no secondary target exists, origin feedback may play but no damage occurs and the owner is never a fallback target.
- Cooldown-blocked application, pure refresh, Buff tick, and Protection-phase application neither add a stack nor run the Wind attack.
- Reaching max stacks invokes Overload, which spawns WindVortex at the owner anchor; the same Windcut Buff then enters its existing Protection phase.
- Wind attack and WindVortex damage do not recursively apply Windcut or other Elemental stacks by default.

### Four-Tower Wind Content

- Create one Wind Elemental TowerUpgradeDefinition for Archer, Cannon, Magic, and Drone.
- Each definition uses the shared Elemental apply pipeline and shared Wind Buff data.
- Cannon applies Wind to every valid monster resolved by its explosion.

### Elemental Content Audit

- Audit Fire, Cold, Electric, and Wind across Archer, Cannon, Magic, and Drone.
- Confirm the complete matrix contains 16 Elemental TowerUpgradeDefinitions.
- Confirm each definition matches its TowerFamily and ElementType and routes through shared Elemental Buff data.
- Confirm each element is visible through Task007 feedback during gameplay validation.

## Shared Constraints

- Do not create generic Elemental reaction, ElementalBuff inheritance, or moving EffectZone frameworks.
- Keep Elemental behavior data-driven through BuffDefinition, EffectDefinition, and the dedicated WindVortexConfig.
- Do not add multi-element towers, replacement, reroll, global upgrades, or rarity systems.

## Out Of Scope

- Behaviour Layer Phase 2 upgrades.
- Generic area-zone, crowd-control, or moving-zone frameworks.
- Object pooling and final VFX polish.

## Acceptance Criteria

- All four Wind upgrades can be authored, drafted, and applied through the shared Elemental pipeline.
- Windcut's first application is stack-only; only successful later stack applications run its one-secondary-target attack.
- The Windcut owner is excluded from the secondary-target set and is never a fallback damage target.
- WindVortex is created only by successful Windcut Overload and obeys the Task011 contract.
- Wind reaction damage does not recursively apply Windcut stacks.
- The Elemental Content Audit confirms all 16 Elemental TowerUpgradeDefinitions are present, correctly matched, and validated through their intended tower-family attack path.
