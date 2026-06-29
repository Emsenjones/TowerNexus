# Task006 - Magic Behaviour Packages

## Status

Planned.

## Goal

Implement Magic Tower Behaviour Layer upgrades.

After this task, Magic Tower can use the prioritized Magic behaviour packages without moving behaviour execution into TowerUpgradeSystem.

## Source Documents

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Priority Scope

This task should prioritize:

- Twin Orbs
- Orb Splash

Resonance Orb may be postponed unless explicitly included after review.

## Twin Orbs Requirements

- Spawn one additional Magic Orb.
- The two orbs rotate 180 degrees apart.
- Magic runtime owns the orb spawning and orbit behaviour.
- TowerUpgradeSystem only records that the tower owns the behaviour package.

## Orb Splash Requirements

- Magic Orb impact also damages nearby enemies.
- Splash radius is behaviour package data.
- Splash behavior stays inside Magic runtime or Magic Orb runtime.
- Do not introduce a generic Buff Framework for Orb Splash.

## Resonance Orb Future Note

If Resonance Orb is included later:

- Use local stack tracking inside MagicOrbBehaviour or the Magic runtime path.
- Do not introduce a generic Buff Framework.
- Use integer additive damage:

```text
FinalDamage = BaseDamage + Min(HitCount * DamageIncreasePerHit, MaxBonusDamage)
```

## Out Of Scope

- Archer, Cannon, or Drone behaviour packages.
- Generic Buff Framework.
- Generic status effect framework.
- Cross-tower Synergy gameplay.

## Acceptance Criteria

- Applying Twin Orbs causes Magic Tower to spawn two Magic Orbs.
- Twin Orbs are positioned 180 degrees apart in orbit.
- Applying Orb Splash causes Magic Orb hits to damage nearby valid enemies.
- Orb Splash does not attach persistent buff state to monsters.
- Magic runtime consumes active Behaviour packages without TowerUpgradeSystem executing Magic behaviour.
- Existing Magic behavior is preserved when no Magic Behaviour package is applied.

## Implementation Review Note

Before implementation begins, present the concrete implementation plan in chat for review.
