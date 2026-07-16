# Task002 - Attack Result And Elemental Opportunity Foundation

Status: Implementation complete; Unity Play Mode validation pending

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
- `ExecuteWithResolvedTargets` does not clear a caller-provided target list before null or invalid Effect early returns, so stale targets can survive from a previous execution.
- Projectile runtime options currently carry only Piercing state.
- Position Impact and Monster Hit are not yet consistently represented by all attack flows.

## 4. In Scope

- Preserve Position Impact when an entity reaches its gameplay destination even if no Monster is resolved.
- Preserve Monster Hit only when a valid Monster is resolved.
- Allow an eligible Elemental apply request when damage is zero, nonpositive, or `DealDamage` performs no successful action.
- Allow reviewed area Effects to expose their resolved target set without treating damage success as the eligibility gate.
- Clear caller-provided resolved-target output before Effect validation or target resolution; false leaves an empty list and true exposes the complete snapshot for that execution.
- Execute every authored sibling action once in authored order without short-circuiting after either success or failure.
- Ensure each authorized target that remains gameplay-targetable at the reviewed Elemental boundary receives an independent request.
- Treat a Monster killed or removed before that boundary as lifecycle-invalid, not as an Elemental opportunity suppressed by damage success.
- Preserve source tower, source upgrade, trigger position, and explicit eligibility context.
- Make `EffectTriggerContext` and `ProjectileRuntimeOptions` explicitly immutable and keep option transport narrow through existing concrete initialization paths.

## 5. Out of Scope

- Universal `AttackResult` or `DamageContext` migration.
- A common AttackEntity base class or framework.
- One monolithic options structure containing every current and future package.
- New serialized Effect trigger types solely for Position Impact or Monster Hit.
- Changing BuffApplyCooldown, Protection, stack, overload, or reaction rules.
- Granting Elemental eligibility to child Effects, Buff ticks, reactions, overloads, or zones by default.
- Reordering Elemental Buff application before direct damage merely to keep lethally damaged targets eligible.
- Broad cosmetic `readonly struct` conversion outside `EffectTriggerContext` and `ProjectileRuntimeOptions`.

## 6. Runtime Contract

```text
Reviewed attack boundary resolves a valid target
    -> optional damage result
    -> explicit Elemental application opportunity
    -> Buff runtime decides Applied / Refreshed / Stacked / Blocked / Invalid
```

Damage and Elemental application are sibling results. One must not be implemented as a prerequisite for the other.

If damage kills or removes the Monster before the Elemental application call, that Monster is no longer gameplay-targetable and receives no Buff request. This is target lifecycle invalidation, not damage-result gating. Zero or nonpositive damage leaves an otherwise valid target eligible.

Effect execution preserves authored sequencing:

```text
resolve target snapshot
    -> clear caller output before validation/resolution
    -> execute every authored action once in authored order
    -> aggregate action success without short-circuiting
```

`ExecuteWithResolvedTargets` returns false with an empty output list. On true, the output contains the complete resolved-target snapshot even when no action succeeds.

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
- Non-elemental `ApplyBuff` actions are not gated by `AllowsElementalApplication`; every `ApplyBuff` whose `BuffDefinition.ElementType` is non-None requires explicit eligibility.
- Secondary Effects do not inherit Elemental eligibility unless a reviewed Behaviour explicitly grants it.

## 8. Unity Authoring Checklist

Prepare temporary Play Mode configurations that can produce zero resolved attack damage while still providing a valid Elemental apply Effect. Reuse existing Buff/Effect assets where possible; this task does not require new gameplay prefabs.

## 9. Acceptance Criteria

- A reviewed valid target can receive an Elemental attempt when associated damage is zero.
- An unsuccessful `DealDamage` action does not suppress a separately authorized `ApplyBuff` action.
- A successful action does not suppress any later sibling action; all actions execute once in authored order.
- Effect target resolution can be observed independently from whether any action succeeded, with no stale output after null, invalid, or no-target execution.
- Multiple explicitly authorized targets that remain gameplay-targetable each receive their own request.
- A lethally damaged or otherwise removed target receives no later Buff request because it is lifecycle-invalid.
- BuffApplyCooldown and Protection remain the final application gates.
- Child Effects and lifecycle/periodic Effects remain ineligible by default.
- Existing Piercing, Scatter, Twin Orbs, Twin Drones, Buff, and Effect behavior does not regress.
- No universal attack-result or AttackEntity abstraction is introduced.

## 10. Validation

- Exercise direct positive nonlethal, zero-damage, lethal-damage, area, blocked-by-cooldown, and blocked-by-Protection scenarios.
- Verify both failed-then-successful and successful-then-later sibling action sequences preserve authored order.
- Seed the resolved-target output with stale data, then verify null, invalid, and no-target executions return false with an empty list.
- Verify a valid area Effect returns true with its complete resolved-target snapshot even when its actions report no success.
- Audit `ProjectileImpactContext` and the current impact events to confirm Position Impact and Monster Hit remain independent without adding a shared result type.
- Confirm no recursive Elemental application from reaction, overload, or Buff tick damage.
- Inspect released entity initialization to ensure it receives no unrelated package state.
- Run `git diff --check` and targeted runtime searches for positive-damage gates.

## 11. Review Note

Later Tasks may extend `ProjectileRuntimeOptions` or introduce equally narrow Magic/Drone option values. They should reuse the semantics established here without centralizing all Behaviour execution into Task002.
