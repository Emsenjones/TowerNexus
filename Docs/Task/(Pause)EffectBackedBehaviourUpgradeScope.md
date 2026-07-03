# Task005 - Phase 2 Effect-Backed Behaviour Upgrade Scope

## Objective

Define the Phase 2 Behaviour Layer upgrade scope that should wait for Buff And Effect System foundation work.

This is a scope and staging document only. It should not prescribe the final Buff And Effect System implementation shape before that foundation is built and reviewed.

## System References

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_BuffAndEffectSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

## Prerequisites

- Phase 1 Behaviour Layer tasks are complete or intentionally paused at a stable point.
- Buff And Effect System foundation has been reviewed before any Phase 2 implementation task begins.
- Area damage, delayed area damage, and repeated area damage contracts are aligned with `Docs/11_BuffAndEffectSystem.md`.

## Phase 2 Upgrade Items

Phase 2 should cover effect-backed Behaviour Layer upgrades:

- Magic Orb Splash
- Cannon Timed Shell
- Cannon Burning Shell

These upgrades are grouped together because they need reusable effect resolution rather than tower-local duplicate area query or timed damage logic.

## Expected Effect Dependencies

Magic Orb Splash:

- Natural candidate for instant area damage.
- Should eventually map cleanly to shared area damage resolution.
- Should not attach persistent buff state to monsters.

Cannon Timed Shell:

- Natural candidate for delayed area damage if the reviewed gameplay definition is delayed explosion damage.
- Should wait until delayed area damage is supported or explicitly re-scoped.
- Should not be implemented as tower-local duplicate radius query logic if it remains an area damage behaviour.

Cannon Burning Shell:

- Natural candidate for repeated area damage over duration.
- Should wait until repeated area damage over duration is supported or explicitly re-scoped.
- Should not be implemented as tower-local duplicate tick or radius query logic.

## Scope Rules

- Task005 does not implement Buff And Effect System foundation.
- Task005 does not implement any Phase 2 upgrade item.
- Concrete Phase 2 implementation tasks should start from Task006 after Buff And Effect System foundation is available.
- Concrete Task006+ documents may revise the exact implementation order based on the final Buff And Effect System foundation.
- Phase 2 upgrade implementation should not duplicate shared area-query, delayed-damage, repeated-damage, buff, or effect execution logic inside individual tower runtimes.

## Out Of Scope

- Archer Hunting Arrow.
- Cannon Bouncing Shell.
- Magic Resonance Orb.
- Drone Missile Drone.
- Drone Final Dive.
- Any Phase 3 advanced behaviour package.
- Final Buff And Effect System internal implementation design.
- Concrete Task006+ implementation breakdown.

## Acceptance Criteria

- Phase 2 scope explicitly includes Magic Orb Splash, Cannon Timed Shell, and Cannon Burning Shell.
- Task005 clearly states that these upgrades wait for Buff And Effect System foundation review.
- Task005 does not lock the final implementation approach for Buff And Effect System.
- Task005 preserves the system boundary that reusable effect execution belongs to Buff And Effect System.
- Phase 3 upgrade ideas remain deferred for later review after combat runtime and Buff And Effect System contracts are more stable.
