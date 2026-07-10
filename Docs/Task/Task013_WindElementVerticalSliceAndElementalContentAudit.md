# Task013 - Wind Element Vertical Slice And Elemental Content Audit

## Objective

Complete Wind as the final first-version Elemental vertical slice, then audit the full Elemental content matrix across Fire, Cold, Electric, Wind and Archer, Cannon, Magic, Drone.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Prerequisites

- Task006 shared Elemental application entry is stable.
- Task007 MonsterStatusBar and Buff visual feedback are stable.
- Task011 safe Monster relocation is stable.
- Task012 WindVortex runtime foundation is stable.

## Scope

### Shared Wind Data

- Create one shared Windcut BuffDefinition and shared Wind Effect data.
- Create the necessary Wind apply, stack, overload, and presentation Effect data.
- Wind gameplay after application is independent of source tower identity.

### Four-Tower Wind Content

- Create one Wind Elemental TowerUpgradeDefinition for Archer, Cannon, Magic, and Drone.
- Each definition uses the shared Elemental apply pipeline and shared Wind Buff data.
- Cannon applies Wind to every valid monster resolved by its explosion.

### Wind Behavior

- First Wind application applies Windcut only.
- Only a later successful stack creates WindVortex through Task012's dedicated spawn action.
- Pure refresh, blocked application, Buff tick, Vortex damage, and overload do not create WindVortex.
- Storm Shift overload uses Task011 safe relocation and fails safely when no valid nearby destination exists.

### Elemental Content Audit

- Audit Fire, Cold, Electric, and Wind across Archer, Cannon, Magic, and Drone.
- Confirm the complete matrix contains 16 Elemental TowerUpgradeDefinitions.
- Confirm each definition matches its TowerFamily and ElementType and routes through shared Elemental Buff data.
- Confirm each element is visible through Task007 feedback during gameplay validation.

## Shared Constraints

- Do not create generic Elemental reaction, ElementalBuff inheritance, or moving EffectZone frameworks.
- Keep Elemental behavior data-driven through BuffDefinition and EffectDefinition.
- Do not add multi-element towers, replacement, reroll, global upgrades, or rarity systems.

## Out Of Scope

- Behaviour Layer Phase 2 upgrades.
- Generic area-zone, crowd-control, or displacement systems.
- Object pooling and final VFX polish.

## Acceptance Criteria

- All four Wind upgrades can be authored, drafted, and applied through the shared Elemental pipeline.
- WindVortex is created only by a later successful Wind stack and obeys its approved runtime contract.
- Storm Shift uses safe Monster relocation and preserves path validity.
- Wind reaction damage does not recursively apply Windcut stacks.
- The Elemental Content Audit confirms all 16 Elemental TowerUpgradeDefinitions are present, correctly matched, and validated through their intended tower-family attack path.
