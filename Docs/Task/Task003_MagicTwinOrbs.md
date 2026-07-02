# Task003 - Phase 1 Magic Twin Orbs

## Objective

Implement the Phase 1 Magic Behaviour Layer upgrade:

- Twin Orbs

Twin Orbs should change the number and starting arrangement of released Magic Orb attack entities. It should not introduce area damage, persistent status effects, or Buff And Effect System dependencies.

## System References

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/07_TowerFrameworkSystem.md`

## Prerequisites

- Task001 Phase 1 Behaviour Runtime Activation is complete.
- Magic Tower can release Magic Orb attack entities without Behaviour Layer upgrades.
- Magic Orb runtime already owns orbit movement, contact detection, hit count consumption, same-target hit cooldown, and lifetime.

## Scope

Task003 should implement Twin Orbs as a low-dependency attack entity count change.

Twin Orbs:

- Causes Magic Tower to release two Magic Orb attack entities from one attack release.
- The two Magic Orbs should orbit around the same release-time orbit center.
- The two Magic Orbs should start 180 degrees apart.
- Each Magic Orb owns its own hit count, lifetime, contact checks, and same-target hit cooldown tracking.
- Tower cooldown rules remain based on Magic Orb release timing, not on later Magic Orb despawn timing.

## Requirements

- Twin Orbs uses existing Magic Orb runtime rules.
- Twin Orbs does not change Magic Orb damage formula.
- Twin Orbs does not make older Magic Orbs block later releases.
- Twin Orbs does not require target selection for first-version Magic Orb behavior.
- Twin Orbs does not attach persistent buff state to monsters.
- Twin Orbs does not trigger area damage.

## Out Of Scope

- Orb Splash.
- Resonance Orb.
- Buff And Effect System integration.
- AreaDamageEffect.
- Persistent status effects.
- New Magic Orb lifetime or hit-count rules beyond the two-orb release arrangement.

## Acceptance Criteria

- Applying Twin Orbs causes Magic Tower to release two Magic Orbs from one attack release.
- The two Magic Orbs are positioned 180 degrees apart in orbit at release.
- Each released Magic Orb independently tracks lifetime, hit count, and same-target hit cooldown.
- Existing single-orb Magic behavior is preserved when Twin Orbs is not active.
- Orb Splash and Resonance Orb are not partially implemented.
- Before implementation begins, present the concrete Task003 implementation plan in chat for review.
