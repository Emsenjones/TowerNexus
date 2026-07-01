# Task001 - Phase 1 Behaviour Runtime Activation

## Objective

Prepare the runtime path for Phase 1 Behaviour Layer upgrades.

After this task, placed tower runtime should be able to observe which Behaviour Layer packages are active on its tower instance and route tower-specific behaviour decisions without moving gameplay execution into TowerUpgradeSystem.

## System References

- `Docs/10_TowerUpgradeSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`

## Prerequisites

The completed TowerUpgrade foundation should already provide:

- TowerUpgradeDefinition and TowerUpgradeDatabase authoring data
- Per-tower upgrade state
- Upgrade eligibility and application rules
- Runtime stat resolution
- Tower Upgrade Draft flow integration
- Upgrade application visual feedback foundation

This task should consume those foundations instead of redefining them.

## Scope

Task001 should establish the runtime access pattern for active Behaviour Layer packages.

The runtime contract is:

- TowerUpgradeSystem applies upgrades and records ownership on the tower instance.
- TowerUpgradeSystem does not execute attack behaviour.
- Tower Runtime Combat may coordinate behaviour package usage.
- The corresponding tower runtime module owns the actual behaviour execution.
- Runtime combat should consume resolved runtime stats and active behaviour packages, not raw draft UI state.

Phase 1 behaviour package consumers:

- Archer Piercing Arrow
- Archer Scatter Arrow
- Magic Twin Orbs
- Drone Twin Drones

## Requirements

- Placed tower runtime can determine whether a tower owns a specific Behaviour Layer package.
- Invalid or missing behaviour package data fails safely with clear diagnostic output.
- Behaviour package checks use tower instance upgrade state as the source of truth.
- Behaviour package checks do not require DraftSystem or pending draft UI state.
- Behaviour package checks do not bypass TowerUpgradeSystem application rules.
- Behaviour package activation remains compatible with multiple active Behaviour packages on the same tower.

## Out Of Scope

- Implementing Piercing Arrow.
- Implementing Scatter Arrow.
- Implementing Twin Orbs.
- Implementing Twin Drones.
- Implementing Orb Splash, Timed Shell, or Burning Shell.
- Implementing Hunting Arrow, Bouncing Shell, Resonance Orb, Missile Drone, or Final Dive.
- Adding upgrade exclusion rules.
- Adding a Buff And Effect System implementation.

## Acceptance Criteria

- A placed tower with no Behaviour Layer upgrades keeps existing attack behaviour.
- A placed tower with one active Behaviour Layer upgrade exposes that package to its tower runtime path.
- A placed tower with multiple active Behaviour Layer upgrades exposes all active packages without treating them as mutually exclusive.
- TowerUpgradeSystem remains the application and recording authority only.
- DraftSystem remains unrelated to runtime behaviour execution.
- Runtime stat resolution remains unchanged by this task.
- No Phase 1 tower-specific behaviour package is half-implemented in Task001.
- Before implementation begins, present the concrete Task001 implementation plan in chat for review.
