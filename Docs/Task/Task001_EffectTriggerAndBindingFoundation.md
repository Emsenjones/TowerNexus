# Task001 - Effect Trigger And Binding Foundation

## Objective

Establish the first Buff And Effect System entry point by adding trigger context and Effect binding contracts without changing the base damage pipeline.

This task creates the authoring and runtime connection that lets Behaviour Layer and Elemental Layer upgrade content request reusable Effect execution later.

## System References

- `Docs/00_ProjectOverview.md`
- `Docs/07_TowerFrameworkSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Prerequisites

- Current System documents are synced to the Elemental Layer and Buff And Effect framework direction.
- Existing Basic and Behaviour upgrade behavior remains stable.
- Existing direct base damage behavior remains stable.

## Scope

### Upgrade Layer Authoring

TowerUpgradeDefinition should have an explicit design-facing Upgrade Layer concept.

Supported first-version layers:

- Basic
- Behaviour
- Elemental

Required Tower Level remains the unlock requirement.

Upgrade Layer describes what kind of upgrade the definition represents.

The first content set may still map Lv1 to Basic, Lv2 to Behaviour, and Lv3 to Elemental, but implementation should not permanently derive Upgrade Layer from Required Tower Level.

### Effect Binding Authoring

Effect bindings are authoring data that connect a gameplay trigger to an Effect definition.

Effect bindings may be configured only on:

- Behaviour Layer upgrades
- Elemental Layer upgrades

Basic Layer upgrades must not have Effect bindings in this first version.

This preserves Basic Layer as a pure numerical upgrade layer.

### Trigger Types

First-version trigger types:

- OnHit
- OnImpact
- OnBuffTick
- OnZoneTick
- OnMaxStack

Do not introduce a separate OnElementalTowerHit trigger.

Elemental tower hits should use OnHit with source tower, source upgrade, element, and stack eligibility context.

### Trigger Context

The runtime trigger context should carry enough information for future Effect execution without transferring ownership of upstream systems.

Context may include:

- Source tower
- Source upgrade
- Target monster
- Trigger position
- Impact position
- Zone position
- Zone radius
- Resolved damage value when relevant
- Attack Entity concept
- Trigger type
- Element type when relevant
- Whether this hit can apply Elemental stacks

The exact implementation shape may evolve during planning. The stable task contract is that Attack Entities, Projectile System, Buff runtime, and EffectZone runtime can provide context to Buff And Effect System without owning Buff or Elemental rules.

### Runtime Integration Points

Runtime Combat and Attack Entity behavior may provide trigger context when a hit, contact, or impact occurs.

Projectile System may provide OnHit or OnImpact context from projectile hit or arrival events.

This task should keep the current base damage path intact. Trigger context exists around that path and should not force DamageContext migration.

## Out Of Scope

- EffectDefinition execution.
- Effect action implementation.
- Radius target resolution.
- Buff runtime state.
- Elemental debuff application.
- Elemental overload.
- EffectZone runtime.
- Magic Orb Splash.
- Cannon Timed Shell.
- Cannon Burning Shell.
- Renaming or refactoring AttackArchetype into a new attack entity architecture.
- Migrating base attack damage into DamageContext.

## Acceptance Criteria

- TowerUpgradeDefinition can express Upgrade Layer separately from Required Tower Level.
- Basic Layer upgrades cannot author Effect bindings.
- Behaviour Layer and Elemental Layer upgrades can author Effect bindings or have a clearly reserved authoring path for them.
- Supported trigger types are limited to OnHit, OnImpact, OnBuffTick, OnZoneTick, and OnMaxStack.
- No separate OnElementalTowerHit trigger is introduced.
- Trigger context can carry source tower, source upgrade, target or position, trigger type, and stack eligibility data when relevant.
- Existing direct damage behavior remains unchanged.
- Existing Basic and Behaviour upgrade behavior remains unchanged.
- Invalid authoring combinations fail safely or warn clearly.
