# Task006 - Cannon Bouncing Shell

Status: Implementation complete; Unity Play Mode validation pending

Depends on: Task004, Task005

## 1. Goal

Implement Bouncing Shell as a finite Shell-local chain that selects and creates one bounce child in the same frame after every immediate result of the current landing has completed.

## 2. Source Documents

- `Doc/09_ProjectileSystem.md`
- `Doc/10_TowerUpgradeSystem.md`
- `Doc/11_EffectSystem.md`
- `Doc/12_BuffSystem.md`

## 3. In Scope

- Resolve `bounceSearchRadius`, `maxBounceCount`, `bounceArcHeight`, and `bounceTargetSelectionType` from the applied package.
- Give each initial Shell its own remaining bounce count and chain hit history.
- Add the resolved direct Monster to that chain's history.
- Continue from Position Impact even when no direct Monster Hit was resolved.
- Complete all synchronous landing results before the bounce query.
- Search around the actual impact position, exclude chain history, and apply the package-authored selection type to the surviving local candidates.
- Capture the selected Monster's current HitAnchor position.
- Create exactly one bounce child in the same frame.
- Carry the chain history, decremented remaining count, Explosive Shell option, and release-time damage into the child.
- Preserve the existing sourceTower-based Elemental lookup semantics; Elemental upgrade snapshotting is outside Task006.
- Mark the child as non-initial so Multi Shells is not consumed again.

## 4. Out of Scope

- Tower AttackRange or the source `AttackConfig.TargetSelectionType` filtering.
- `bounceSearchRadius` centered on the tower or original target.
- Coroutine, delay, next-frame scheduling, or animation for bounce creation.
- Multiple children from one landing.
- Generic ricochet or SpawnProjectile Effect frameworks.
- Snapshotting Elemental upgrade data into an already released Attack Entity.

## 5. Same-Frame Ordering Contract

```text
Position Impact
    -> optional direct Monster Hit
    -> direct damage
    -> direct Elemental application attempt
    -> Explosive Shell actions when active
    -> explosion-target Elemental attempts
    -> synchronous Buff / overload / death / target-state consequences
    -> query surviving candidates within bounceSearchRadius
    -> apply bounceTargetSelectionType
    -> create one bounce child in the same frame
```

No remaining bounce count or no candidate ends the chain. A direct Monster Hit is optional and direct damage success is not a bounce gate.

`maxBounceCount` counts bounce children after the initial Shell. A value of one permits exactly one child landing.

## 6. Ownership Contract

- Projectile runtime owns chain history, remaining count, search center, candidate selection, child creation, and ordering.
- Effect System may execute Explosive Shell but does not own bounce state.
- Tower Runtime resolves the source package composition before the initial Shell is released.
- Buff and Monster systems synchronously expose the post-landing valid-target state used by the bounce search.

## 7. Unity Authoring Checklist

- Create or configure a Cannon Bouncing Shell upgrade asset.
- Author a positive `bounceSearchRadius`, positive `maxBounceCount`, nonnegative `bounceArcHeight`, and `bounceTargetSelectionType`.
- Prepare Play Mode layouts with candidates inside/outside the local radius and with targets that die from direct or explosion damage.
- No bounce projectile prefab or generic bounce Effect asset is required; the child reuses the Cannon Shell prefab and immutable runtime data.

## 8. Acceptance Criteria

- Every initial Multi Shell owns an independent chain.
- One landing creates at most one child.
- Hit history prevents revisiting any direct target already resolved by the chain.
- Candidate eligibility is measured from impact position and is limited by `bounceSearchRadius` and chain history before selection.
- `Nearest`, `HighestHealth`, `LowestHealth`, and `Random` select only among eligible local candidates.
- Candidates may be outside source Tower AttackRange.
- The source `AttackConfig.TargetSelectionType` does not affect bounce selection; the Bouncing Shell package owns its own selector.
- Explosive Shell completes before candidate search.
- A target killed or invalidated by direct, Elemental, explosion, Buff, or overload consequences is not selected.
- The child is created in the same frame with a target-position snapshot.
- Initial Shell flight uses the Cannon `AttackConfig.ArcHeight`; bounce children use the package-authored `bounceArcHeight`.
- Bounce children inherit Explosive Shell and do not retrigger Multi Shells.
- Bounce children do not re-resolve Cannon Behaviour packages; existing Elemental application continues to query through sourceTower.
- Chain length has no off-by-one error relative to `maxBounceCount`.

## 9. Validation

- Test zero, one, and multiple local candidates at controlled distances.
- Test all four bounce selection modes, including stable manager-order ties for deterministic modes and local-candidate-only Random selection.
- Test visibly different initial-Shell and bounce-child arc heights.
- Test a nearer candidate dying during the current landing and confirm the next surviving candidate is selected.
- Test direct-only, Explosive-only composition, and all three Cannon upgrades together.
- Test no direct target with a surviving local candidate and confirm the chain continues.
- Test a resolved direct target with zero damage and confirm history recording and bounce creation still occur.
- Test candidates outside tower range and repeated-history exclusion.
- Confirm no coroutine or delayed child creation exists.
- Run `git diff --check` and inspect same-frame call ordering.

## 10. Review Note

Task014 owns final composition regression only. It must not replace this local chain with a generic combination framework.
