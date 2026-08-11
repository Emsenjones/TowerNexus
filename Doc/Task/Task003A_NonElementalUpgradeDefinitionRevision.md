# Task003A - Non-Elemental Upgrade Definition Revision

Status: Completed and accepted; source, serialized authoring, static validation, Unity Play Mode inspection, and the post-change Task002 `Base Combat v0.3` regression are complete

Depends on: Completed Task002 `Base Combat v0.2`; approved Task003 Upgrade review

Unblocks: Task003 Upgrade calibration

## 1. Goal

Revise the current Basic and Behaviour Upgrade set before numerical calibration so every retained Upgrade has independent gameplay value, coherent combination behavior, and a parameter surface that can be tuned without changing unrelated base-Tower values.

Task003A is a definition, schema, runtime, and asset-migration slice. It does not choose final Upgrade balance values. Its structural implementation is accepted, and Task002 has established the required naked-Tower `Base Combat v0.3` baseline after removal of Magic Orb maximum-hit exhaustion. Task003 calibration may now proceed from that baseline.

## 2. Source Documents

- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`
- `Doc/System/12_ProjectileSystem.md`
- `Doc/System/13_TowerUpgradeSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/Task/Task002_BaseTowerAndReferenceMonsterBaseline.md`
- `Doc/Task/Task003_TowerGrowthAndNonElementalUpgrades.md`

## 3. Design Decisions

### 3.1 Archer

| Current Upgrade | Task003A Decision | Stable Contract |
|---|---|---|
| Eagle Sight | Keep | Attack Range remains a coverage and placement-tolerance Upgrade. Its value may vary when the Tower is already continuously firing. |
| Quick Draw | Keep | Reduces Attack Cycle Duration. |
| Sharpened Arrows | Keep | Adds direct attack damage. |
| Hunting Arrow | Replace with `Explosive Arrow` | A direct Arrow Monster Hit produces a small area explosion. Tracking and active-Arrow Hunting retrofit are removed. |
| Piercing Arrow | Keep | One Arrow may resolve additional unique Monster Hits. |
| Scatter Arrow | Keep | One attack confirms additional Arrow slots. |

Explosive Arrow differs from Cannon Explosive Shell by trigger and role:

- Explosive Arrow requires a direct Arrow Monster Hit and produces no result when the Arrow expires or misses.
- Its area Effect includes every valid Monster in range, including the directly hit Monster when that Monster remains gameplay-targetable after direct damage.
- It is a small, frequent splash extension. Cannon remains the larger, slower Position Impact explosion owner.
- Final radius and damage values belong to Task003.
- Explosive Arrow affects future releases only; it does not retrofit already released Arrows.

### 3.2 Cannon

| Upgrade | Task003A Decision | New Authoring Surface |
|---|---|---|
| Extended Barrel | Keep | Existing Attack Range delta |
| Faster Reload | Keep | Existing Attack Cycle Duration delta |
| Reinforced Shells | Keep | Existing Damage Bonus delta |
| Bouncing Shell | Keep | Add a positive integer `Bounce Damage` |
| Explosive Shell | Keep | Existing Effect remains independently tunable by radius and damage |
| Twin Shells | Keep | Add a positive integer `Additional Shell Damage` |

Every Cannon Shell resolves one explicit integer direct-damage budget:

- The primary initial Shell uses the current resolved Cannon Attack Damage.
- Each additional initial Shell uses the Multi Shells package's authored Additional Shell Damage.
- Every bounce child uses the Bouncing Shell package's authored Bounce Damage, regardless of whether its parent was primary, additional, or another bounce child.
- Explosive Shell uses its EffectDefinition's independently authored damage and does not inherit direct damage from primary, additional, or bounced Shells.
- Reinforced Shells changes the primary initial Shell's direct damage only.
- Additional initial Shells and bounce children lock their fixed authored direct damage against later generic Damage Bonus refresh; an unresolved primary initial Shell may still receive the reviewed live Damage refresh.

This fixed-integer contract keeps each Upgrade's damage budget independent and introduces no general damage-scale formula. Exact accepted damage values remain Task003 outputs; Task003A may author provisional positive values only for structural validation.

### 3.3 Magic

| Current Upgrade Or Parameter | Task003A Decision | Stable Contract |
|---|---|---|
| Magic Orb Maximum Hit Count | Remove completely | It is no longer base authoring, resolved stat data, runtime member state, refresh data, completion authority, or Upgrade data. |
| Faster Orbit | Keep | Changes orbit speed. |
| Lingering Orbit | Replace with `Arcane Recovery` | Reduces Magic Attack Cycle Duration instead of adding hit capacity. |
| Arcane Charge | Keep | Adds direct contact damage. |
| Arcane Detonation | Keep | Executes on normal group lifetime completion. |
| Twin Orbs | Keep | Adds synchronized Orb members. |
| Arcane Field | Keep | Remains a permanent Tower-owned field while the Upgrade and Tower combat session are active. |

A normal Magic Orb group ends when its shared maximum lifetime expires. Same-target contact cooldown, per-member contact history, shared lifetime, and technical cleanup remain. If a future dense scenario requires a technical safety limit, it must be explicitly named as a non-gameplay protection and must not reintroduce a draftable Maximum Hit Count.

Removing Maximum Hit Count is an intentional clean break. No compatibility field, sentinel value such as `999`, or alternate hit-exhaustion branch remains.

### 3.4 Drone

| Current Upgrade Or Parameter | Task003A Decision | Stable Contract |
|---|---|---|
| Battery Duration | Keep as base Drone authoring | It is no longer an upgradable resolved stat or Live Refresh surface. |
| Extended Battery | Replace with `Expanded Patrol Radius` | Adds Drone Tower Attack Range. |
| High-Caliber Rounds | Redesign | Adds an authored chance for each released Drone projectile to carry a fixed `+1` bonus direct damage result. |
| Optimized Burst Module | Keep | Reduces Inter-Burst Cooldown. |
| Blast Rounds | Keep | Existing hit-triggered area Effect. |
| Double Drones | Keep | Existing active-Drone capacity increase. |
| Final Dive | Keep | Existing battery-end branch. |

The High-Caliber chance is finite and authored in the inclusive range `[0, 1]`. It is rolled once for each projectile at release and the result remains immutable for that projectile. Exact probability belongs to Task003. The fixed bonus remains `+1`, so the authored chance is the Upgrade's single balance parameter. Blast Rounds damage is not increased by the High-Caliber direct-hit bonus.

## 4. In Scope

- Remove Hunting Arrow package identity, tracking behavior, and live retrofit
- Add Explosive Arrow Behaviour package and direct-hit explosion execution
- Remove Magic Orb Maximum Hit Count across definitions, resolved values, runtime state, refresh, validation, and authoring
- Replace Lingering Orbit with Arcane Recovery
- Remove Drone Battery Duration from Upgrade stat resolution and refresh
- Replace Extended Battery with Expanded Patrol Radius
- Redesign High-Caliber Rounds as a release-snapshot bonus-damage chance
- Add independent positive integer Cannon bounce and additional-Shell direct damage
- Preserve existing compatible Upgrade asset references through deliberate asset rename or replacement
- Static validation, Unity import inspection, and focused Play Mode behavior checks

## 5. Out Of Scope

- Final Basic or Behaviour numerical balance
- Task003 Straight/L/U Upgrade matrices
- Changes to the Task002 fixture, Reference Monster, or unchanged naked-Tower base values
- Elemental Layer balance or reaction behavior
- New generic tracking, critical-hit, chain-projectile, or persistent-zone frameworks
- Arcane Field redesign
- Player-facing VFX polish beyond sufficient feedback to identify the revised behavior
- Stage pool or Draft probability changes

## 6. Ownership And Runtime Boundaries

| Owner | Responsibility |
|---|---|
| Tower Upgrade System | Revised Upgrade schema, family compatibility, validation, accepted Upgrade state, and package timing identity |
| Tower Runtime Combat | Resolved values, attack release snapshots, Magic group lifetime, Drone bonus roll, and package refresh coordination |
| Projectile System | Explosive Arrow direct-hit trigger, Cannon primary/additional/bounce direct-damage ownership, impact order, and projectile completion |
| Effect System | Area target resolution and independently authored Effect damage |
| Tower content | Revised Upgrade definitions, package references, Effect definitions, descriptions, and authored parameters |
| Task002 revision | Post-implementation naked-Tower baseline acceptance |
| Task003 | Final Upgrade values and comparative route acceptance |

## 7. Implementation Sequence

1. Add the revised schema and package identities while keeping the project compilable.
2. Implement fixed integer direct-damage ownership for Cannon primary, additional, and bounce Shells.
3. Replace Hunting runtime with Explosive Arrow and remove Tracking-only state that no longer has an owner.
4. Remove Magic Orb Maximum Hit Count from schema through runtime as one clean break.
5. Replace Lingering Orbit and Extended Battery definitions with their approved Basic stat roles.
6. Redesign High-Caliber Rounds using one immutable projectile release roll.
7. Migrate the affected Upgrade assets and preserve serialized references where a rename represents the same Draft slot.
8. Run static validation and focused package-combination smoke checks.
9. Hand off Unity import, serialized asset inspection, and Play Mode acceptance.
10. Return to Task002 and run the complete `Base Combat v0.3` route suite before Task003 tuning. Completed on 2026-08-12.

## 8. Asset Migration Contract

- Reuse the existing Draft slots for Hunting Arrow, Lingering Orbit, and Extended Battery rather than adding extra definitions to Stage pools.
- When an existing asset is renamed into its replacement, move the asset and its metadata together so serialized references retain identity.
- Remove obsolete package or stat identities after migration; do not keep compatibility aliases or dual interpretation paths.
- Update display name, description, package/stat data, and referenced Effect authoring as one coherent migration.
- Validation must reject any surviving definition that uses removed Hunting, Magic Orb Maximum Hit Count, or Drone Battery Duration Upgrade identities.

## 9. Unity Authoring Checklist

- Confirm the three replacement Draft assets still appear in every intended Stage Upgrade pool.
- Confirm no affected prefab, Stage, or Upgrade definition reports a missing reference after import.
- Confirm Magic Orb authoring no longer displays or serializes Maximum Hit Count.
- Confirm Battery Duration remains on Drone entity authoring and is absent from Basic Upgrade stat choices.
- Author Explosive Arrow area Effect and package reference.
- Author positive integer Cannon Bounce Damage and Additional Shell Damage.
- Author a valid High-Caliber bonus chance and verify the fixed bonus is represented once.
- Verify revised display names and descriptions in the Draft debug flow.

## 10. Acceptance Criteria

- Hunting Arrow and projectile Tracking are absent from the current package and flight schemas; Explosive Arrow works only after a direct Monster Hit.
- Explosive Arrow, Piercing Arrow, and Scatter Arrow compose without duplicate completion, lost direct hits, or implicit target reacquisition.
- Primary, additional, and bounced Cannon Shells use their independently owned integer direct-damage values without multiplier composition or runtime rounding.
- Explosive Shell damage remains independently authored and identical for primary, additional, and bounced Shell impacts.
- Damage Bonus refresh cannot overwrite fixed Additional Shell Damage or Bounce Damage on active projectiles.
- Explosive Arrow and Explosive Shell both include a surviving direct target in their area Effect while preserving their different direct-hit and Position Impact trigger rules.
- Magic Orb normal completion is governed by shared lifetime rather than hit exhaustion, and Arcane Detonation still resolves exactly once per active member on normal completion.
- Arcane Recovery changes Attack Cycle Duration without changing Orb lifetime.
- Arcane Field remains one persistent Tower-owned field independent of active Orb-group presence.
- Drone Battery Duration does not change when Expanded Patrol Radius or another Basic Upgrade is applied.
- Each High-Caliber projectile receives at most one immutable bonus roll and at most one fixed bonus direct-damage result.
- Removed stat and package identities produce no compatibility or silent fallback behavior.
- Existing unaffected Upgrades retain their prior semantic behavior.
- Static checks pass, and Unity import plus focused Play Mode checks report no missing scripts or serialized references.

## 11. Validation

- Path-scoped compile and diff checks
- Focused stale-identity scan for Hunting, Tracking, Magic Orb Maximum Hit Count, Lingering Orbit, Drone Battery Duration Upgrade, Extended Battery, their runtime fields, and their serialized enum values
- Upgrade-definition and Stage-pool validation
- Archer direct-hit, miss, Scatter, Piercing, and Explosive composition checks: a surviving direct target and every valid nearby Monster receive the explosion result; a target removed by direct damage is no longer eligible for the following explosion.
- Cannon checks with known Damage and Monster HP must assert exact integer results for primary, additional, bounce, and additional-then-bounce paths. Reinforced Shells changes only primary direct damage; Explosive Shell damage remains the independently authored Effect amount on every impact.
- Magic lifetime checks must show an Orb exceeding the removed historical hit limit while remaining active until lifetime completion. Arcane Detonation resolves on normal lifetime completion and resolves zero times when technical cleanup ends the group.
- Drone checks must cover chance `0` and `1`, Active Drone chance refresh, immutable already released projectiles, unchanged Blast Rounds damage, unchanged Battery Duration, Expanded Patrol Radius, Double Drones, and Final Dive.
- Mid-flight checks must show Bouncing Shell applied before an initial Shell's first impact captures its fixed Bounce Damage, while later Upgrade changes cannot rewrite an already active chain.
- Unity import and missing-reference inspection
- Task002 `Base Combat v0.3` L, Straight, and U runs for all four naked Towers after implementation

The post-change Task002 regression is accepted. The unchanged Archer, Cannon, and Drone controls remained close to v0.2. Removing Magic hit exhaustion made Rotation Speed `180` structurally dominant on L, so Task002 isolated Rotation Speed `90`, repeated the accepted Magic L and U observations, and froze the full v0.3 suite before returning calibration authority to Task003.

The clean-break scan must return no active code or serialized authoring matches for `TryConvertToTracking`, `TrackingRange`, `huntingOpportunityConsumed`, `maxHitCount`, `BaseMaxHitCount`, `resolvedMaxHitCount`, `RemainingHitCountDelta`, `TryConsumeHit`, `BatteryDurationDelta`, `statType: 101`, `statType: 200`, or `behaviourPackageType: 102`. Unity import alone is insufficient; the affected assets and Magic Orb prefab must be explicitly saved or force-reserialized before this gate.

## 12. Review Note

The first L-route pass across the current twenty-four Basic and Behaviour Upgrades is exploratory design evidence. It successfully revealed definition and composition problems, but it is not final Task003 balance acceptance because Task003A intentionally changes several tested definitions and the naked Magic baseline.

The revised runtime and authored content are accepted, and Task002 now owns the frozen v0.3 naked-Tower baseline. Task003 owns all final Upgrade-value decisions and must retest revised definitions from those controls; the pre-Task003A Upgrade results remain exploratory evidence only.
