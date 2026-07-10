# Task006 - Elemental Shared Entry Foundation And Fire Validation

## Objective

Establish the one shared Elemental application entry used by every tower family, then migrate Fire into that entry and validate Fire as the first four-tower vertical slice.

Fire is validation content for the shared pipeline. It must not retain a separate Fire-only runtime path.

## System References

- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Prerequisites

- Task001 through Task005 are complete or at a stable reviewable point.
- Elemental Layer eligibility and one-Elemental-upgrade-per-tower rules are stable.

## Scope

### Elemental Upgrade Authoring

- Elemental Layer upgrade content uses `ElementType` and one direct Elemental apply effect.
- Generic `EffectBindings` and their `TriggerType` remain available for Behaviour Layer content.
- Elemental Layer authoring does not expose a designer-selected TriggerType.

### Shared Runtime Entry

- Tower-owned attack runtime determines the real attack boundary and affected targets.
- Archer projectile hit, Drone-fired projectile hit, and Magic Orb contact provide single-target Elemental application.
- Cannon impact applies the Elemental apply effect to every valid monster resolved by the explosion.
- Runtime contexts may still carry actual event facts such as hit or impact, but those facts do not filter Elemental authoring.

### Fire Migration And Validation

- Fire uses one shared Burning BuffDefinition and shared Fire Effect data across all tower families.
- Burning tick, FlameBurst overload, Protection, and Fire presentation do not vary by the tower that applied Burning.
- Validate Fire application, stack, periodic damage, overload, Protection, and cooldown behavior through Archer, Cannon, Magic, and Drone.

## Shared Constraints

- Do not create a generic Elemental reaction framework.
- Do not create a generic ElementalBuff inheritance framework.
- Keep persistent Elemental behavior data-driven through BuffDefinition and EffectDefinition.
- Do not migrate base attack damage into a new damage framework.

## Out Of Scope

- MonsterStatusBar and Buff visual feedback.
- Cold, Electric, and Wind gameplay content.
- Generic moving EffectZone work.
- Behaviour Layer Phase 2 upgrades.

## Acceptance Criteria

- Each Elemental TowerUpgradeDefinition exposes ElementType plus a direct Elemental apply effect.
- Behaviour Layer generic EffectBindings remain usable without becoming Elemental authoring controls.
- All four Fire tower families use the same shared Fire Buff data.
- Cannon Elemental application reaches every valid monster resolved by the shell explosion.
- Burning gameplay after application is independent of source tower identity.
- Fire passes four-tower validation for apply, stack, tick, FlameBurst, cooldown, and Protection behavior.
- No separate Fire-only application path remains.
