# Buff And Effect System

## 1. System Overview

The Buff And Effect System is responsible for reusable gameplay effects, persistent Buff state, Elemental debuff stacking, overload rules, EffectZone-style gameplay entities, and reviewed specialized elemental gameplay entities.

It is not only a Buff system.

It is the gameplay rule framework that can consume trigger events, build runtime context, resolve targets, execute actions, manage persistent states, and coordinate elemental stack and overload behavior.

Core direction:

```text
Attack Entity / Projectile / EffectZone / Buff
    -> emits trigger context
Buff And Effect System
    -> resolves target or radius targets
    -> executes Effect actions
    -> applies or updates Buff runtime state when needed
```

Direct base attack damage does not need to migrate into this system immediately.

The current direct damage path may remain simple while Buff And Effect System handles additional gameplay results such as area damage, buff application, buff ticks, EffectZone ticks, elemental normal phase effects, and overload effects.

Projectile impact visual effects are separate from gameplay Effects. Projectile-specific impact VFX belongs to ProjectileConfig and Projectile System.

Buff status and persistent Buff VFX may be authored with BuffDefinition. An EffectDefinition may optionally author one-shot feedback for the gameplay Effect it executes. These references do not move projectile impact VFX into Buff And Effect System ownership.

EffectDefinition is the gameplay Effect source of truth for reusable effect data. EffectBinding references and projectile gameplay impact effects should point to EffectDefinition.

---

## 2. Responsibility Boundary

### Owns

The Buff And Effect System owns:

- Effect trigger context consumption
- Effect target resolution
- Effect action execution
- Area damage and other reusable gameplay effect execution
- Buff application and lifecycle
- Buff stacking, refresh, tick, and removal rules
- Elemental debuff stacking
- Elemental overload and post-overload Protection phase rules
- EffectZone duration, tick, and effect execution
- Reviewed specialized elemental gameplay entity behavior

### Does Not Own

The Buff And Effect System does not own:

- Tower target selection
- Tower attack cooldown
- Attack Entity release timing
- Projectile movement
- Projectile collision detection
- Projectile lifetime
- Projectile impact VFX spawning
- Projectile impact VFX cleanup
- Monster pathfinding implementation
- Monster death rewards
- Draft generation
- Tower upgrade application rules

These responsibilities belong to their respective systems.

Movement or path effects may request safe Monster System operations, but Buff And Effect System should not directly manipulate monster transforms or bypass pathfinding ownership.

---

## 3. Core Design Philosophy

## 3.1 Effect, Buff, Damage, And EffectZone Are Different Concepts

An Effect is one rule execution.

Examples:

- Deal damage
- Apply buff
- Spawn EffectZone
- Apply movement effect

A Buff is persistent runtime state attached to a unit.

Examples:

- Burning
- Cold
- ElectricShock
- Windcut
- Elemental Buff Protection phase

Damage is an action result. It may be direct base attack damage, effect damage, buff tick damage, zone tick damage, overload damage, or future reaction damage.

EffectZone is a gameplay entity that exists in the world for a duration, checks targets in an area, and executes effects over time.

Examples:

- FireZone
- SlowField
- DelayedExplosionZone

EffectZone is not a Buff and is not pure VFX.

Some reviewed gameplay entities may be more specific than a generic EffectZone. WindVortex is one such moving Wind entity: it owns an explicit pursuit-then-straight-line behavior rather than defining a generic moving-zone contract for every future effect.

---

## 3.2 Keep Simple Damage Simple

Direct single-target base attack damage may remain in the existing Projectile, Attack Entity, and Monster damage path.

Example:

```text
Arrow Hits Monster
    -> Direct Damage
    -> Optional effect trigger context
```

Buff And Effect System should be used when a combat event needs reusable gameplay rule execution.

Examples:

- Area damage
- Apply buff
- Spawn persistent field
- Delayed damage
- Repeated damage
- Elemental stack
- Elemental overload
- Movement or path effect request

This keeps early implementation scope controlled while preserving clean extension points.

---

## 4. Trigger Context

Effect trigger context is the runtime data package created when a gameplay event wants Buff And Effect System execution.

Typical trigger producers:

- Projectile impact
- Projectile or Attack Entity hit
- Magic Orb contact
- Drone-fired projectile hit
- EffectZone tick
- Buff tick
- Elemental max stack event

First-version trigger types:

| Trigger Type | Meaning |
|---|---|
| OnHit | An attack entity hits a specific monster |
| OnImpact | An attack entity reaches a resolve point or impact position |
| OnBuffTick | A persistent Buff performs a periodic tick |
| OnZoneTick | An EffectZone performs a periodic tick |
| OnMaxStack | An elemental debuff reaches max stack and triggers overload |

Elemental tower attacks do not need a separate trigger type. They are regular trigger events with source tower and explicit stack eligibility context.

Trigger context may carry:

- Source tower
- Source upgrade
- Target monster
- Trigger position
- Impact position
- Zone position
- Zone radius
- Base damage or resolved damage value when relevant
- Attack Entity concept
- Trigger type
- Element type when relevant
- Whether this event can apply Elemental stacks

When a trigger position is carried, the context must expose explicit validity such as HasTriggerPosition. Systems should not use Vector3.zero as an implicit "no position" sentinel.

The exact runtime fields may evolve during implementation. The stable contract is that Attack Entities, projectiles, zones, and buffs provide enough source, target, and position context for Buff And Effect System to resolve effects without owning their upstream behavior.

---

## 5. Effect Targeting

First-version Effect targeting should stay simple.

The trigger context should provide one of:

- Target monster
- Trigger position with explicit validity

Effect targeting uses:

- Target monster
- Valid trigger position
- Radius

Targeting rule:

```text
If radius <= 0 and target monster exists:
    target = target monster

If radius <= 0 and target monster is missing:
    execute nothing and log a warning

If radius > 0:
    center = target monster position if target monster exists
    otherwise center = context trigger position only when HasTriggerPosition is true
    targets = valid monsters inside radius around center

If radius > 0 and neither target monster nor valid trigger position exists:
    execute nothing and log a warning
```

Monster inclusion in radius should use the monster-side hit/reference anchor provided by Monster System.

This avoids a large first-version targeting enum while supporting direct effects, area effects, buff application, and overload effects.

---

## 6. Effect Actions

Effect actions define what happens after targets are resolved.

Core action types:

| Action | Responsibility |
|---|---|
| DealDamage | Apply configured gameplay damage to resolved targets |
| ApplyBuff | Add, refresh, or stack a Buff runtime instance on resolved targets |
| SpawnEffectZone | Create a gameplay zone that owns duration and tick timing |
| SpawnWindVortex | Create the reviewed specialized moving Wind gameplay entity |
| ApplyMovementEffect | Request a safe Monster System movement or path effect |

### 6.1 DealDamage

DealDamage is an instant action.

Examples:

- Area impact damage
- Burning tick damage
- FlameBurst overload damage
- Electric extra damage
- LightningStrike damage
- WindVortex hit damage
- EffectZone tick damage

First implementation can route Effect, Buff, Zone, and Overload damage through Effect actions while keeping base attack damage unchanged.

DealDamage may use an authored action damage amount when configured. If no positive authored damage amount is provided, it can fall back to the resolved damage carried by the EffectTriggerContext.

Reaction-generated damage should not apply Elemental stacks by default.

### 6.2 ApplyBuff

ApplyBuff creates, refreshes, or stacks a Buff runtime instance.

ApplyBuff is an Effect action. When executed through EffectExecutor, it applies the configured Buff definition to each resolved target monster while preserving source tower and source upgrade context from the EffectTriggerContext when available.

Examples:

- Apply Burning
- Apply Cold
- Apply ElectricShock
- Apply Windcut
- Enter Elemental Buff Protection phase

### 6.3 SpawnEffectZone

SpawnEffectZone creates a gameplay entity with duration, radius, tick interval, source context, and an on-tick Effect.

Examples:

- Spawn FireZone
- Spawn SlowField

EffectZone gameplay timing should be authoritative. VFX may follow gameplay timing, but gameplay should not depend on animation timing.

### 6.4 SpawnWindVortex

SpawnWindVortex is a deliberately narrow first-version action for the Wind Buff's StackApplied event. It creates the specialized entity described in the WindVortex contract; it is not a requirement to introduce a generic moving EffectZone implementation.

### 6.5 ApplyMovementEffect

ApplyMovementEffect requests monster movement or path changes through safe Monster System APIs.

Examples:

- Frozen movement lock
- Storm Shift
- Future knockback
- Future pull

Buff And Effect System should not directly modify monster transforms for path logic.

---

## 7. Buff Definition And Runtime State

Buff definitions are static configuration.

Buff runtime instances are runtime state.

Buff definitions should be referenced through Inspector-assigned ScriptableObject references. They should not depend on hand-authored string ids unless a future persistence, external-data, or lookup requirement needs stable ids.

Buff definition data may include:

- Display name and description
- Element type when relevant
- Duration
- Tick interval when relevant
- Max stack
- Buff apply cooldown
- Buff event bindings for lifecycle-driven effects
- Protection duration when an Elemental Buff should block restacking after overload
- Status icon and optional Protection-phase icon
- Persistent Buff VFX prefab and optional Protection-phase VFX prefab

Buff runtime state should include:

- Definition reference
- Owner monster
- Remaining duration
- Tick timer
- Stack count
- Buff phase when an Elemental Buff can enter post-overload Protection
- Buff apply cooldown tracking when required

Runtime state must not be stored in definition assets.

For Elemental Buffs, the source tower and source upgrade are relevant only while a tower-owned attack applies the Buff. Once the Buff exists on a monster, its ticking, stack behavior, overload, damage, and visual feedback are determined solely by the shared BuffDefinition and the owner monster. Source information may be retained for diagnostics, but it must not change those gameplay results.

Persistent Elemental Buff effects and overload effects must use their own authored gameplay values. They do not fall back to the source tower's resolved damage.

MonsterBehaviour owns attached Buff runtime state through an internal plain C# runtime container:

```text
MonsterBehaviour
    -> MonsterBuffRuntime
        -> List<MonsterBuffInstance>
```

Buff runtime instances are not MonoBehaviour components.

First-version successful apply always adds one stack, up to max stack, and refreshes duration to the Buff definition duration.

Buff apply attempts should return explicit results such as Applied, Refreshed, Stacked, BlockedByBuffApplyCooldown, BlockedByProtectionPhase, or Invalid.

Buff ticks may execute periodic tick Buff event bindings through EffectExecutor using OnBuffTick context.

Buff event bindings express Buff-owned lifecycle events:

| Buff Event | Meaning |
|---|---|
| PeriodicTick | The Buff reaches a configured tick interval |
| StackApplied | An existing Buff successfully gains one stack |
| Overload | The Buff reaches max stack and triggers overload |

Behaviour Layer EffectBindings remain responsible for generic external tower events such as OnHit. Elemental Layer uses its dedicated Elemental apply effect at the runtime-selected attack boundary. BuffDefinition event bindings are responsible for what the Buff does after it already exists on a monster.

### 7.1 First-Version Buff Feedback

MonsterStatusBar owns UI display of health and active Buff state. It shows one icon slot per active BuffDefinition, may show stack count when it exceeds one, and uses the authored Protection-phase icon when configured. If the Protection icon is absent, the normal status icon remains the fallback.

MonsterBuffVisualController owns persistent world-space Buff VFX lifecycle. It attaches authored Buff VFX at the monster hit/reference anchor, keeps one instance alive across stack and refresh changes, optionally swaps it for Protection-phase VFX, and destroys it when the Buff leaves runtime state.

MonsterBuffRuntime does not operate UI or ParticleSystem behavior. It exposes state-change notifications and read-only Buff state snapshots so these presentation consumers can refresh after apply, refresh, stack, Protection transition, removal, expiry, or cleanup. First version may rebuild all status-icon slots on each state change.

EffectDefinition may own an optional one-shot effect VFX prefab for gameplay feedback such as overload, tick, or special effect execution. It is spawned once at the trigger position or target reference position for that execution, and its prefab owns its own short lifetime.

---

## 8. Elemental Debuff Rules

Elemental Layer upgrades convert towers into elemental towers.

Each ElementType has one shared BuffDefinition and shared Elemental Buff data. Tower-family-specific Elemental upgrades only decide which Elemental apply effect their tower-owned attack may execute; after application, the resulting Buff behavior is independent of the source tower.

Tower-owned attack events from elemental towers can apply elemental debuff stacks through Buff And Effect System when their runtime context explicitly allows Elemental stack application. Elemental Layer authoring does not expose a designer-selected TriggerType in v1. Runtime combat determines the real attack boundary and affected targets, then executes the tower's Elemental apply effect.

First-version elemental debuffs:

- Burning
- Cold
- ElectricShock
- Windcut

Each elemental debuff has two runtime phases:

| Phase | Meaning |
|---|---|
| Stacking | Persistent stackable debuff behavior before overload |
| Protection | Temporary post-overload state that blocks restacking for that same BuffDefinition |

General stacking rule:

1. If the monster does not have this elemental debuff, apply it with initial stacks.
2. If the monster already has this elemental debuff and the application is not blocked, refresh duration and add one stack when below max stacks.
3. Multiple towers with the same ElementType can add stacks to the same shared elemental debuff on the same monster.
4. The same elemental debuff on the same monster may have an apply cooldown that blocks applications from any tower while active.
5. If the elemental debuff expires before max stack is reached, the debuff is removed and stacks are lost.
6. When max stack is reached, trigger overload.
7. After overload triggers, the elemental debuff enters Protection phase when it has a protection duration.

First application of an elemental debuff should apply the debuff only. StackApplied Buff event bindings, such as Electric extra damage or WindVortex spawn, trigger only when an existing elemental debuff successfully gains one stack. A pure refresh does not trigger stack effects.

Buff apply cooldown prevents the same elemental debuff from stacking too quickly on the same monster, regardless of which tower attempts the application. When this cooldown blocks an application, no stack is added, duration is not refreshed, and stack effects do not trigger.

After a successful stack increase, the system checks whether max stacks have been reached. If max stacks are reached, overload executes and the Buff enters Protection phase when it has a protection duration. While in Protection phase, the same BuffDefinition cannot be applied, refreshed, or stacked.

---

## 9. Elemental Buff Protection Phase

Protection phase is a temporary state on the same Buff instance after elemental overload.

It prevents the monster from gaining stacks for that same Elemental BuffDefinition for a short duration.

It does not block damage by default.

Example:

```text
Monster triggers Cold overload
    -> Cold Buff enters Protection phase
    -> Cold stacks cannot be added or refreshed while Protection is active
    -> Fire, Electric, and Wind stacks can still be applied normally
```

Protection behavior is authored directly on the same BuffDefinition with a protection duration.

Example Fire authoring:

```text
Burning:
    Element Type = Fire
    Max Stacks = 3
    Protection Duration = 2s
```

Protection phase should be treated as status on the same Buff runtime instance, not as a separate elemental debuff reaction chain.

---

## 10. Elemental First-Version Design Contracts

The first Elemental content set should preserve clear identity for each element without locking numeric tuning or concrete implementation details into the system contract.

| Element | Normal Phase | Overload | Important Boundary |
|---|---|---|---|
| Fire | Burning creates persistent damage pressure while active | FlameBurst deals area damage around the target monster | Burning tick damage and FlameBurst damage do not apply Burning stacks by default |
| Cold | Cold slows the monster while active | Frozen is an instant overload effect that applies a temporary movement lock | Movement lock must go through safe Monster movement APIs |
| Electric | ElectricShock causes later direct Electric tower hits to trigger extra electric damage | Overcharged is an instant lightning sequence that selects random monsters near the target and strikes them one by one | Overcharged is not a persistent Buff, and lightning strikes do not apply ElectricShock stacks by default |
| Wind | A later successful Wind stack creates WindVortex | Storm Shift is an instant overload effect that moves the target monster to a valid nearby grid node | WindVortex damage and Storm Shift do not apply Windcut stacks by default |

### 10.1 Burning Overload

Burning overload triggers FlameBurst.

FlameBurst deals area damage around the monster that reached max Burning stacks.

FlameBurst damage is Effect damage and should not apply Burning stacks or trigger Elemental reactions by default.

### 10.2 Cold Overload

Cold overload triggers Frozen.

Frozen is an instant overload effect that applies a temporary movement lock to the target monster.

Frozen should request movement control through safe Monster movement APIs. Buff And Effect System should not directly stop movement by bypassing Monster System ownership.

### 10.3 Electric Overload

ElectricShock overload triggers Overcharged.

Overcharged is an instant overload effect, not a long-lived Buff state.

Overcharged selects random monsters within a configured radius around the target monster, then drops sequential single-target lightning strikes with a configured strike interval.

LightningStrike damage is Electric Effect damage and should not apply ElectricShock stacks or trigger Elemental reactions by default.

### 10.4 Wind Overload

Windcut overload triggers Storm Shift.

Storm Shift is an instant overload effect that moves the target monster to a random valid nearby grid node.

The selected node must be walkable and reachable to the goal. After relocation, the monster should recalculate its path through the pathfinding system.

Buff And Effect System should request safe Monster movement and pathfinding APIs for Storm Shift. It must not directly modify the monster Transform, current grid node, or path state.

---

## 11. Recursion Rule

Only tower-owned attack events whose runtime context explicitly allows Elemental stack application can apply elemental stacks by default.

The following should not apply elemental stacks by default:

- Burning tick damage
- FlameBurst damage
- Electric extra damage
- LightningStrike damage
- WindVortex damage
- EffectZone tick damage
- Overload damage
- Buff tick damage

Future upgrades may explicitly override this rule, but that should be reviewed as separate upgrade content rather than assumed by the baseline Elemental Layer.

The baseline rule is that tower attacks can carry Elemental stack application, while Elemental reactions do not recursively carry Elemental stack application.

---

## 12. EffectZone And WindVortex

EffectZone is a zone-like gameplay entity for reviewed effects that need duration, position, radius, tick interval, target detection, and on-tick Effect execution. Static examples include FireZone, SlowField, PoisonCloud, and DelayedExplosionZone.

EffectZone gameplay timing is authoritative. Its VFX may follow gameplay timing, but gameplay does not depend on animation timing.

WindVortex is a separate moving Wind gameplay entity with a deliberately specific first-version contract. It does not require a generic moving EffectZone framework.

WindVortex is created only by the Wind Buff's StackApplied event: the target monster must already have Windcut and successfully receive one additional stack. Initial application, pure refresh, blocked application, Buff tick, and overload do not create WindVortex.

WindVortex behavior:

1. Spawn at the Wind Buff owner's current hit/reference position.
2. At spawn, randomly lock one alive monster inside its authored search radius. If none exists, randomly choose a world position inside that radius instead.
3. Before its first monster hit, move toward the locked monster or chosen position. A locked monster may be tracked while it remains valid.
4. The first time it hits any monster, whether or not that monster was the locked target, stop tracking, lock the current movement direction, and continue in a straight line.
5. Each WindVortex keeps its own set of monsters it has hit; a monster can receive its hit effect at most once from that Vortex.
6. After it starts moving, it expires after its authored short lifetime.

WindVortex owns its target choice, movement, first-hit transition, already-hit set, and lifetime. Its hit effect resolves through Buff And Effect System rules and does not apply Windcut stacks or trigger Elemental reactions by default. It does not move monsters, use Monster pathfinding, or require Storm Shift's safe relocation capability.

---

## 13. Relationship With Other Systems

### Tower Upgrade System

Tower Upgrade System owns upgrade definitions, eligibility, and application rules.

Behaviour upgrades may reference generic Effect bindings. Elemental upgrades reference an ElementType and Elemental apply effect; their shared BuffDefinition and lifecycle-driven Effects belong to the Elemental Buff data.

Tower Upgrade System should not execute Buff, Effect, Elemental stack, or overload behavior.

### Tower Runtime Combat System

Tower Runtime Combat decides when towers attack, resolves combat stats, and releases Attack Entities.

Runtime Combat determines the actual elemental attack timing and affected target scope, then provides the current tower's Elemental apply effect and context to Buff And Effect System. It does not own Buff, Elemental stack, or overload rules.

### Projectile System

Projectile System owns projectile movement, hit detection, impact event generation, simple single-target projectile damage dispatch, and projectile-specific impact VFX.

Projectile System may emit trigger context for complex gameplay results.

Projectile System should not directly apply Buffs or handle Elemental stack logic.

### Monster System

Monster System owns monster health, movement, pathfinding, death, arrival, hit/reference anchors, and safe movement/path operations.

Buff And Effect System may request damage or movement/path effects through Monster System-owned APIs, but it should not bypass Monster System ownership.

---

## 14. Implementation Scope Direction

The next implementation should be split into Task Documents rather than implemented as one large change.

The next scope should complete Elemental trigger handoff and Fire coverage across all four tower families, then add Buff runtime feedback, Cold, Electric, and Wind in dependency order. WindVortex can proceed as its reviewed specialized entity; Storm Shift remains dependent on safe Monster relocation and path recalculation support.

Effect-backed Behaviour Layer upgrades such as Magic Orb Splash, Cannon Timed Shell, and Cannon Burning Shell should wait until the required Effect binding and Buff And Effect System foundation exists.

---

## 15. Summary

The Buff And Effect System handles reusable gameplay rule execution beyond simple direct attack damage.

It should grow from a small Effect foundation into Buff runtime, Elemental debuff stacking, overload, and EffectZone support through staged Task Documents.

Direct base attack damage can remain in the existing runtime path until a later DamageContext migration is explicitly reviewed.
