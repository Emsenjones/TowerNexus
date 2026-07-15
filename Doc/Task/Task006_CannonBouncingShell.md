# Task006 - Cannon Bouncing Shell

Status: Ready for implementation

Depends on: Task004, Task005

## 1. Goal

Implement Bouncing Shell as a finite Shell-local chain that selects and creates one bounce child in the same frame after every immediate result of the current landing has completed.

## 2. Source Documents

- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. In Scope

- Resolve `bounceSearchRadius` and `maxBounceCount` from the applied package.
- Give each initial Shell its own remaining bounce count and chain hit history.
- Add the resolved direct Monster to that chain's history.
- Continue only after a valid direct Monster Hit.
- Complete all synchronous landing results before the bounce query.
- Search around the actual impact position, exclude chain history, and choose the nearest surviving valid Monster.
- Capture the selected Monster's current HitAnchor position.
- Create exactly one bounce child in the same frame.
- Carry the chain history, decremented remaining count, Explosive Shell option, damage, and Elemental context into the child.
- Mark the child as non-initial so Twin Shells is not consumed again.

## 4. Out of Scope

- Tower AttackRange or TargetSelectionType filtering.
- `bounceSearchRadius` centered on the tower or original target.
- Coroutine, delay, next-frame scheduling, or animation for bounce creation.
- Multiple children from one landing.
- Generic ricochet or SpawnProjectile Effect frameworks.
- Bouncing after Position Impact with no direct Monster Hit.

## 5. Same-Frame Ordering Contract

```text
Position Impact
    -> optional direct Monster Hit
    -> direct damage
    -> direct Elemental application attempt
    -> Explosive Shell actions when active
    -> explosion-target Elemental attempts
    -> synchronous Buff / overload / death / target-state consequences
    -> query nearest surviving candidate within bounceSearchRadius
    -> create one bounce child in the same frame
```

No direct Monster Hit, no remaining bounce count, or no candidate ends the chain.

`maxBounceCount` counts bounce children after the initial Shell. A value of one permits exactly one child landing.

## 6. Ownership Contract

- Projectile runtime owns chain history, remaining count, search center, candidate selection, child creation, and ordering.
- Effect System may execute Explosive Shell but does not own bounce state.
- Tower Runtime resolves the source package composition before the initial Shell is released.
- Buff and Monster systems synchronously expose the post-landing valid-target state used by the bounce search.

## 7. Unity Authoring Checklist

- Create or configure a Cannon Bouncing Shell upgrade asset.
- Author a positive `bounceSearchRadius` and positive `maxBounceCount`.
- Prepare Play Mode layouts with candidates inside/outside the local radius and with targets that die from direct or explosion damage.
- No bounce projectile prefab or generic bounce Effect asset is required; the child reuses the Cannon Shell prefab and immutable runtime data.

## 8. Acceptance Criteria

- Every initial Twin Shell owns an independent chain.
- One landing creates at most one child.
- Hit history prevents revisiting any direct target already resolved by the chain.
- The nearest surviving candidate is measured from impact position.
- Candidates may be outside source Tower AttackRange.
- TargetSelectionType does not affect bounce selection.
- Explosive Shell completes before candidate search.
- A target killed or invalidated by direct, Elemental, explosion, Buff, or overload consequences is not selected.
- The child is created in the same frame with a target-position snapshot.
- Bounce children inherit Explosive Shell and do not retrigger Twin Shells.
- Chain length has no off-by-one error relative to `maxBounceCount`.

## 9. Validation

- Test zero, one, and multiple local candidates at controlled distances.
- Test a nearer candidate dying during the current landing and confirm the next surviving candidate is selected.
- Test direct-only, Explosive-only composition, and all three Cannon upgrades together.
- Test candidates outside tower range and repeated-history exclusion.
- Confirm no coroutine or delayed child creation exists.
- Run `git diff --check` and inspect same-frame call ordering.

## 10. Review Note

Task012 owns final composition regression only. It must not replace this local chain with a generic combination framework.
