# Task009 - Magic Arcane Field

Status: Runtime implementation complete; Unity prefab authoring and Play Mode validation pending

Depends on: Task002

## 1. Goal

Implement Arcane Field as one tower-owned persistent field that activates immediately when the upgrade is successfully applied, follows its tower, ticks every valid Monster in its radius, and cleans up with the tower or battle.

## 2. Source Documents

- `Doc/07_TowerFrameworkSystem.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. In Scope

- Detect successful Arcane Field application on an already deployed tower.
- Ensure the field exists exactly once immediately after application.
- Keep activation/reconciliation idempotent when other upgrades are applied later.
- Resolve package radius, tick interval, and tick Effect from the applied definition.
- Resolve the Magic Arcane Field VFX prefab from the applied TowerUpgradeDefinition.
- Instantiate or reuse that prefab as a child of the tower and require MagicArcaneFieldBehaviour on its root.
- Set the field prefab root's local X/Z scale to the applied field radius while preserving its authored local Y scale.
- Follow the owning tower position.
- Begin the first tick after one complete authored tick interval.
- At each tick, resolve every valid Monster within the package-owned radius.
- Execute the authored tick Effect once per resolved target.
- Give every resolved target one explicit 100% Elemental application opportunity per tick.
- Remove the field on tower destruction/removal, battle cleanup, or reset.

## 4. Out of Scope

- Creating a second field on later attacks or upgrade applications.
- Independent duration or expiry.
- Per-target chance authoring.
- Reusing generic EffectZone ownership for the tower-attached field.
- Adding MagicArcaneFieldBehaviour dynamically to the tower or using a runtime AddComponent fallback when the VFX prefab is invalid.
- Granting Elemental eligibility to ordinary OnZoneTick or periodic Effects.
- A generic upgrade-activation event framework beyond the smallest notification/reconciliation needed here.

## 5. Runtime Contract

```text
Arcane Field upgrade successfully applied
    -> EnsureArcaneFieldExists()
    -> instantiate or reuse the applied package's Magic Arcane Field VFX prefab
    -> require MagicArcaneFieldBehaviour on the prefab root
    -> one tower-owned field runtime child
    -> follow owner
    -> every tick interval:
        -> collect every valid Monster inside field radius
        -> execute tick Effect once per target
        -> one explicit Elemental attempt per target
```

Calling the ensure path repeatedly must return the same active field instead of creating another.

## 6. Ownership Contract

- The Arcane Field TowerUpgradeDefinition owns radius, tick interval, tick Effect, and the Magic Arcane Field VFX prefab reference.
- Tower runtime owns field instance identity, prefab instantiation/reuse, activation, follow behavior, timer, uniqueness, and cleanup.
- MagicArcaneFieldBehaviour on the VFX prefab root owns tick execution; nested VFX content is presentation-only.
- Effect System owns reusable tick actions and execution feedback.
- Effect target-resolution utilities may be reused for the package radius.
- Buff System owns the outcome of each application attempt.

## 7. Unity Authoring Checklist

- Create or configure a Magic Arcane Field upgrade asset.
- Author a positive field radius and tick interval.
- Assign a valid single-target tick EffectDefinition; the field runtime supplies the multi-target radius and invokes the Effect once per target.
- Create the Magic Arcane Field VFX prefab and attach MagicArcaneFieldBehaviour to its root.
- Author the VFX at local X/Z scale `1` for radius `1`; runtime applies the configured radius scale.
- Assign that prefab to the Magic Arcane Field TowerUpgradeDefinition.
- Configure the desired tick damage and execution VFX on the Effect.
- No independent duration, chance field, or generic EffectZone prefab is required.

## 8. Acceptance Criteria

- Applying Arcane Field to a deployed tower activates it immediately without waiting for an attack.
- Exactly one field exists per owning tower.
- The field instance comes from the applied TowerUpgradeDefinition VFX prefab and retains MagicArcaneFieldBehaviour on its root.
- Applying any later upgrade does not create another field.
- The field remains centered on the moving/placed tower.
- The field VFX local X/Z scale matches ArcaneFieldRadius and its local Y scale remains prefab-authored.
- The first tick occurs after one full interval and subsequent ticks follow the authored cadence.
- Every valid Monster inside radius is processed once per tick.
- Monsters outside radius are not processed.
- Every resolved target receives one Elemental attempt even when tick damage is zero.
- BuffApplyCooldown and Protection remain authoritative.
- Tower/battle cleanup removes the field and stops future ticks.
- Arcane Field absence leaves Magic Tower runtime unchanged.

## 9. Validation

- Apply Arcane Field before and during combat, then apply other upgrades.
- Count field instances after repeated reconciliation.
- Confirm the instantiated field is a child of the owning tower and comes from the configured TowerUpgradeDefinition VFX prefab.
- Disable and re-enable the tower, then verify the same inactive field child is reinitialized instead of duplicated.
- Move or replace the tower and verify ownership/cleanup behavior.
- Exercise zero, one, and multiple Monsters crossing the radius between ticks.
- Test zero damage, cooldown, Protection, death during a tick, and battle cleanup.
- Run `git diff --check` and verify no generic EffectZone or chance framework was introduced.

## 10. Review Note

Arcane Field's 100% per-target eligibility is an explicit Behaviour exception and must not broaden periodic Elemental eligibility elsewhere.
