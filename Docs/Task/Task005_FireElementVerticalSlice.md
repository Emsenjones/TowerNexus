# Task005 - Fire Element Vertical Slice

## Objective

Implement the first complete Elemental Layer vertical slice using Fire, Burning, and FlameBurst.

This task validates the framework loop:

```text
Elemental upgrade eligibility
    -> tower-owned attack event with Elemental stack eligibility
    -> Buff runtime
    -> Buff event bindings for periodic tick, stack, and overload effects
    -> max stack overload
    -> same Buff enters post-overload Protection phase
```

## System References

- `Docs/04_MonsterSystem.md`
- `Docs/05_DraftSystem.md`
- `Docs/08_TowerRuntimeCombatSystem.md`
- `Docs/09_ProjectileSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/11_BuffAndEffectSystem.md`

## Prerequisites

- Task001 Effect trigger and binding foundation is complete.
- Task002 Effect definition, action, and targeting foundation is complete.
- Task003 Monster Buff runtime foundation is complete.
- Task004 Elemental upgrade profile and eligibility is complete.
- Fire Elemental upgrade content can be authored and selected through the Tower Upgrade Draft flow.

## Scope

### Fire Elemental Upgrade

Applying the Fire Elemental Layer upgrade converts a tower into a Fire tower.

Tower-owned attack events from that tower may apply Burning when their runtime context explicitly allows Elemental stack application.

Elemental stack application should remain tied to tower-owned attack events, not reaction-generated effects.

Reaction-generated damage should not apply Elemental stacks by default.

### Burning Normal Phase

Burning is the Fire elemental debuff.

Burning has two runtime phases:

- Stacking: Burning is active, can tick, refresh, and gain stacks.
- Protection: Burning has overloaded and temporarily blocks further Fire stack application.

First successful Fire stack application:

- Applies Burning.
- Adds initial stacks.
- Does not execute additional stack effect behavior beyond applying the debuff.

Later successful Fire stack applications against a monster that already has Burning:

- Must not be blocked by shared Buff apply cooldown.
- Must not be in Burning Protection phase.
- Refresh Burning duration.
- Add one Burning stack when the monster is below max stacks.
- Allow Burning runtime tick behavior to continue through Buff runtime.

Burning periodic damage is handled by Buff runtime through a periodic tick Buff event binding and shared Effect execution.

Burning periodic damage does not apply Burning stacks and does not trigger Elemental reactions by default.

### Shared Buff Apply Cooldown

Shared Buff apply cooldown limits pre-overload stack frequency for the same monster and the same BuffDefinition.

When shared Buff apply cooldown blocks a Fire application:

- No stack is added.
- Duration is not refreshed.
- Stack effect behavior does not trigger.

This is separate from post-overload Protection phase.

Multiple Fire towers share this cooldown when they try to apply the same Burning BuffDefinition to the same monster.

### FlameBurst Overload

When Burning reaches max stack, Burning overload triggers FlameBurst.

FlameBurst deals area damage around the monster that reached max Burning stacks.

FlameBurst should use shared EffectDefinition and radius targeting from Task002.

FlameBurst damage does not apply Burning stacks and does not trigger Elemental reactions by default.

After overload:

- Burning enters Protection phase for its configured protection duration.
- Fire stacks cannot be added or refreshed while Burning is in Protection phase.
- Other elements are not blocked by Burning's Fire Protection phase.

Protection phase belongs to the same BuffDefinition as Burning. It is not authored as a separate post-overload Buff asset in Task005.

### Elemental Stack Eligibility

Elemental stack application is controlled by runtime context.

Tower-owned primary attack events may set Elemental stack eligibility when they hit a monster and the source tower owns an active Elemental Layer upgrade.

Examples of future eligible tower-owned attack events may include:

- Archer direct arrow hit
- Drone direct projectile hit
- Cannon shell area damage to each damaged monster
- Magic Orb hit or tick when explicitly treated as a tower-owned attack event

Reaction-generated effects must not inherit Elemental stack eligibility by default.

The following should keep Elemental stack eligibility disabled:

- Burning periodic damage
- FlameBurst damage
- Future Electric stack damage
- Future WindVortex damage
- Buff tick damage
- Overload damage

### Base Damage Boundary

Base attack damage should remain in the existing direct damage path.

Fire Elemental behavior runs around that path through attack context and Buff And Effect System rules.

This task should not migrate base attack damage into DamageContext.

## Out Of Scope

- Cold Elemental content.
- Electric Elemental content.
- Wind Elemental content.
- Overcharged.
- WindVortex.
- Storm Shift.
- EffectZone foundation beyond what FlameBurst needs for instant area damage.
- Magic Orb Splash.
- Cannon Timed Shell.
- Cannon Burning Shell.
- Visual status UI beyond any minimal debug visibility needed for verification.
- Multi-element towers.
- Element replacement or reroll upgrades.

## Acceptance Criteria

- Fire Elemental upgrade can be authored, drafted when eligible, and applied to an eligible tower.
- A tower that already owns an Elemental Layer upgrade cannot receive Fire as a second Elemental upgrade.
- Tower-owned Fire attack events can use OnHit with Elemental stack eligibility context.
- First successful Fire stack application applies Burning but does not execute a stack effect.
- Later successful Fire stack applications against an already Burning monster refresh Burning and add one stack when below max stacks.
- Stack effect behavior triggers only when Burning successfully gains one stack.
- Shared Buff apply cooldown blocks stack, refresh, and stack effect behavior for the same monster and Burning BuffDefinition.
- Burning Protection phase blocks post-overload Fire restacking from all sources.
- Burning periodic damage is handled by Buff runtime and does not apply Burning stacks.
- FlameBurst triggers when Burning reaches max stack.
- FlameBurst uses shared EffectDefinition and radius targeting.
- FlameBurst damage does not apply Burning stacks or trigger Elemental reactions.
- Burning enters Protection phase after FlameBurst as a fixed first-version rule.
- Burning Protection phase uses an authored protection duration.
- Existing base attack damage behavior remains stable and does not migrate into DamageContext.
