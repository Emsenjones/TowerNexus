# Task008 - Monster Slow And Frozen Control API

## Objective

Provide the smallest Monster System-owned control API required by Cold: an active slow and a temporary Frozen movement lock.

This is a Cold-specific safety foundation, not a generic movement modifier framework.

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/11_EffectSystem.md`
- `Docs/12_BuffSystem.md`

## Prerequisites

- Task007 Buff visual and debugging foundation is stable.
- Existing Monster movement, death, arrival, and path lifecycle behavior is stable.

## Scope

- Monster System owns the actual move-speed calculation and movement lock behavior.
- Buff and Effect execution can request one active Cold slow and one temporary Frozen lock through safe Monster-owned APIs.
- Frozen takes precedence over slow while active.
- Requests are removed safely when the owning Buff or overload state ends, expires, or the monster is cleaned up.
- Existing path ownership, current-node state, death flow, and arrival flow remain intact.

## Shared Constraints

- Do not create a generic movement modifier, crowd-control, or status-effect framework.
- Do not create a generic ElementalBuff inheritance framework.
- Do not let Effect System or Buff System directly modify monster Transform or path state.
- Do not implement Cold Buff assets or Elemental upgrade assets in this task.

## Out Of Scope

- Knockback, pull, stun, fear, or other movement controls.
- Cold Elemental content.
- Generic Elemental reaction framework.

## Acceptance Criteria

- A valid slow request changes monster movement through Monster System ownership.
- A valid Frozen request temporarily prevents movement without corrupting path state.
- Frozen and slow requests clean up correctly on expiry, Buff removal, death, target arrival, reset, and destroy.
- When no control is active, the monster returns to its normal movement behavior.
- No direct Transform movement or path mutation is introduced outside Monster System ownership.
