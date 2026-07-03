# Task004 - Elemental Upgrade Profile And Eligibility

## Objective

Connect Elemental Layer upgrade content to TowerUpgradeDefinition, TowerUpgradeState, TowerUpgradeSystem, and DraftSystem eligibility without implementing Elemental combat content yet.

This task makes Elemental upgrades selectable and applicable under the correct rules.

## System References

- `Docs/00_ProjectOverview.md`
- `Docs/05_DraftSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Prerequisites

- Task001 Effect trigger and binding foundation is complete.
- Task002 Effect definition and targeting foundation is complete.
- Task003 Monster Buff runtime foundation is complete.
- Tower Upgrade Draft flow is stable before adding Elemental candidates.

## Scope

### Elemental Profile Authoring

TowerUpgradeDefinition should support Elemental Layer upgrade content.

Elemental Layer content may reference or define:

- Element type
- Elemental debuff definition
- Direct hit stack rules
- Normal phase effect reference or binding
- Overload effect reference or binding
- Same-source apply cooldown
- Same-element stack immunity duration

This task should establish the authoring and eligibility path, not the full combat behavior for Fire, Cold, Electric, or Wind.

### One Element Per Tower

Each tower can have at most one Elemental Layer upgrade in the first version.

Fire, Cold, Electric, and Wind are mutually exclusive options per tower.

TowerUpgradeState should store applied upgrade facts and expose query support for whether the tower already owns an Elemental Layer upgrade.

TowerUpgradeSystem should own the application rule that rejects a second Elemental Layer upgrade.

### Draft Eligibility

Elemental Layer upgrade candidates should enter the Tower Upgrade Draft pool only when at least one deployed tower can legally receive them.

The typical first-version eligibility condition is:

- The tower satisfies the Elemental upgrade Required Tower Level.
- The tower TowerFamily matches the upgrade.
- The tower does not already own the same upgrade.
- The tower does not already own any Elemental Layer upgrade.

DraftSystem should use TowerUpgradeSystem eligibility support rather than duplicating Elemental rules locally.

### Trigger Rule Contract

Elemental tower hits use OnHit with Elemental stack eligibility context.

Do not introduce a separate OnElementalTowerHit trigger.

The runtime combat application of this trigger belongs to Task005 and later vertical slices.

## Out Of Scope

- Fire Burning runtime behavior.
- FlameBurst overload.
- Cold slow or Frozen.
- Electric extra damage or Overcharged.
- Windcut, WindVortex, or Storm Shift.
- EffectZone runtime.
- Visual status UI.
- Elemental replacement or reroll upgrades.
- Multi-element towers.
- Behaviour Layer Phase 2 upgrades.

## Acceptance Criteria

- TowerUpgradeDefinition can represent Elemental Layer content separately from Basic and Behaviour content.
- Elemental Layer upgrades are compatible with Required Tower Level eligibility without permanently deriving layer from level.
- TowerUpgradeState can answer whether a tower already owns an Elemental Layer upgrade.
- TowerUpgradeSystem rejects a second Elemental Layer upgrade on the same tower.
- DraftSystem includes Elemental candidates only when at least one deployed tower is eligible.
- DraftSystem does not hardcode Elemental exclusivity rules independently of TowerUpgradeSystem.
- OnHit remains the trigger used for direct Elemental tower hits.
- No separate OnElementalTowerHit trigger is introduced.
- Existing Basic and Behaviour upgrade application remains stable.
