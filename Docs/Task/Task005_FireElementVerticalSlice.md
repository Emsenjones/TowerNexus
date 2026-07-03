# Task005 - Fire Element Vertical Slice

## Objective

Implement the first complete Elemental Layer vertical slice using Fire, Burning, and FlameBurst.

This task validates the framework loop:

```text
Elemental upgrade eligibility
    -> OnHit trigger context
    -> Buff runtime
    -> normal phase rules
    -> max stack overload
    -> ElementalStackImmunity
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

Direct hits from that tower may apply Burning through the OnHit trigger context.

Elemental stack application should remain tied to direct Elemental tower attacks.

Reaction-generated damage should not apply Elemental stacks by default.

### Burning Normal Phase

Burning is the Fire elemental debuff.

First successful direct Fire hit:

- Applies Burning.
- Adds initial stacks.
- Does not execute additional normal phase behavior beyond applying the debuff.

Later successful direct Fire hits against a monster that already has Burning:

- Must not be blocked by same-source apply cooldown.
- Must not be blocked by Fire ElementalStackImmunity.
- Refresh or add Burning stacks according to authored rules.
- Allow Burning runtime tick behavior to continue through Buff runtime.

Burning tick damage is handled by Buff runtime and shared Effect execution.

Burning tick damage does not apply Burning stacks and does not trigger Elemental reactions by default.

### Same-Source Apply Cooldown

Same-source apply cooldown limits pre-overload stack frequency from the same Fire tower to the same monster.

When same-source cooldown blocks a Fire application:

- No stack is added.
- Duration is not refreshed.
- Normal phase behavior does not trigger.

This is separate from ElementalStackImmunity.

### FlameBurst Overload

When Burning reaches max stack, Burning overload triggers FlameBurst.

FlameBurst deals area damage around the monster that reached max Burning stacks.

FlameBurst should use shared EffectDefinition and radius targeting from Task002.

FlameBurst damage does not apply Burning stacks and does not trigger Elemental reactions by default.

After overload:

- Burning is removed when configured to do so.
- Fire ElementalStackImmunity is applied.
- Fire stacks cannot be added or refreshed during Fire ElementalStackImmunity.
- Other elements are not blocked by Fire ElementalStackImmunity.

### Base Damage Boundary

Base attack damage should remain in the existing direct damage path.

Fire Elemental behavior runs around that path through OnHit context and Buff And Effect System rules.

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
- Direct Fire tower hits use OnHit with Elemental stack eligibility context.
- First successful Fire hit applies Burning but does not execute an extra normal phase effect.
- Later successful Fire hits against an already Burning monster refresh or stack Burning according to authored rules.
- Same-source apply cooldown blocks stack, refresh, and normal phase behavior from the same Fire tower to the same monster.
- Fire ElementalStackImmunity blocks post-overload Fire restacking from all sources.
- Burning tick damage is handled by Buff runtime and does not apply Burning stacks.
- FlameBurst triggers when Burning reaches max stack.
- FlameBurst uses shared EffectDefinition and radius targeting.
- FlameBurst damage does not apply Burning stacks or trigger Elemental reactions.
- Existing base attack damage behavior remains stable and does not migrate into DamageContext.
