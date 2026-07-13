# Task011 - Monster Safe Relocation Foundation

## Objective

Provide the smallest Monster System-owned relocation capability required by Wind Storm Shift.

Storm Shift changes the monster's valid grid position and path state. It is intentionally separate from WindVortex, which moves only itself.

## System References

- `Docs/03_MapSystem.md`
- `Docs/04_MonsterSystem.md`
- `Docs/11_EffectSystem.md`

## Prerequisites

- Monster current-node tracking and path recalculation are stable.
- Task007 Buff visual and debugging foundation is stable.

## Scope

- Monster System provides a safe relocation request for a target monster.
- Relocation selects a random valid nearby grid node under reviewed radius or distance rules.
- A selected node must be walkable and have a valid route to the goal.
- On success, Monster System updates position and current node consistently, then recalculates the path.
- On failure to find a valid destination, the monster remains unchanged and the request fails safely.

## Shared Constraints

- Do not create generic knockback, pull, teleport, or displacement frameworks.
- Do not create a generic ElementalBuff inheritance framework.
- Effect System must not directly mutate monster Transform, current node, or path data.
- Do not implement Wind Buff content or WindVortex in this task.

## Out Of Scope

- Wind Elemental TowerUpgradeDefinitions.
- WindVortex runtime.
- Generic movement modifier framework.
- Other Elemental reactions.

## Acceptance Criteria

- A successful relocation chooses only a valid nearby node and preserves a valid path to the goal.
- Monster position, current node, and path state remain consistent after relocation.
- An invalid relocation request leaves the monster unchanged.
- Death, target arrival, and cleanup remain safe during or after relocation.
- No non-Monster System code directly changes monster path ownership state.
