# Task002 - Attack Result And Elemental Opportunity Foundation

Status: Ready for implementation

Depends on: Task001

## 1. Goal

Provide the minimum shared runtime semantics needed by the new Behaviour packages: independent Position Impact and Monster Hit facts, Effect target resolution independent from action success, explicit Elemental opportunities, and narrow immutable option transport to released Attack Entities.

This is compatibility work, not an architecture rewrite.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/08_TowerRuntimeCombatSystem.md`
- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. Current State

- Direct Arrow, Drone projectile, and Magic Orb hits already call the shared Elemental entry explicitly.
- `EffectExecutor` already distinguishes resolved targets from executed actions internally.
- Projectile runtime options currently carry only Piercing state.
- Position Impact and Monster Hit are not yet consistently represented by all attack flows.

## 4. In Scope

- Preserve Position Impact when an entity reaches its gameplay destination even if no Monster is resolved.
- Preserve Monster Hit only when a valid Monster is resolved.
- Allow an eligible Elemental apply request when damage is zero, nonpositive, or `DealDamage` performs no successful action.
- Allow reviewed area Effects to expose their resolved target set without treating damage success as the eligibility gate.
- Ensure each authorized target receives an independent Elemental request.
- Preserve source tower, source upgrade, trigger position, and explicit eligibility context.
- Pass immutable options through existing concrete initialization paths.

## 5. Out of Scope

- Universal `AttackResult` or `DamageContext` migration.
- A common AttackEntity base class or framework.
- One monolithic options structure containing every current and future package.
- New serialized Effect trigger types solely for Position Impact or Monster Hit.
- Changing BuffApplyCooldown, Protection, stack, overload, or reaction rules.
- Granting Elemental eligibility to child Effects, Buff ticks, reactions, overloads, or zones by default.

## 6. Runtime Contract

```text
Reviewed attack boundary resolves a valid target
    -> optional damage result
    -> explicit Elemental application opportunity
    -> Buff runtime decides Applied / Refreshed / Stacked / Blocked / Invalid
```

Damage and Elemental application are sibling results. One must not be implemented as a prerequisite for the other.

```text
Position Impact
    -> destination was reached

Monster Hit
    -> valid Monster was resolved
```

An attack may produce Position Impact only, Monster Hit only where appropriate, or both. Neither semantic fact globally implies damage; each owning attack contract decides its results.

## 7. Ownership Contract

- Tower Runtime resolves the source tower's complete applied Behaviour composition.
- Projectile, Magic Orb, Field, and Drone Tasks add only their own concrete immutable runtime options.
- Effect System resolves and executes reusable Effects.
- Buff System evaluates application requests without inspecting associated damage.
- Secondary Effects do not inherit Elemental eligibility unless a reviewed Behaviour explicitly grants it.

## 8. Unity Authoring Checklist

Prepare temporary Play Mode configurations that can produce zero resolved attack damage while still providing a valid Elemental apply Effect. Reuse existing Buff/Effect assets where possible; this task does not require new gameplay prefabs.

## 9. Acceptance Criteria

- A reviewed valid target can receive an Elemental attempt when associated damage is zero.
- An unsuccessful `DealDamage` action does not suppress a separately authorized `ApplyBuff` action.
- Effect target resolution can be observed independently from whether any action succeeded.
- Multiple explicitly authorized targets each receive their own request.
- BuffApplyCooldown and Protection remain the final application gates.
- Child Effects and lifecycle/periodic Effects remain ineligible by default.
- Existing Piercing, Scatter, Twin Orbs, Twin Drones, Buff, and Effect behavior does not regress.
- No universal attack-result or AttackEntity abstraction is introduced.

## 10. Validation

- Exercise direct, area, zero-damage, blocked-by-cooldown, and blocked-by-Protection scenarios.
- Confirm no recursive Elemental application from reaction, overload, or Buff tick damage.
- Inspect released entity initialization to ensure it receives no unrelated package state.
- Run `git diff --check` and targeted runtime searches for positive-damage gates.

## 11. Review Note

Later Tasks may extend `ProjectileRuntimeOptions` or introduce equally narrow Magic/Drone option values. They should reuse the semantics established here without centralizing all Behaviour execution into Task002.
