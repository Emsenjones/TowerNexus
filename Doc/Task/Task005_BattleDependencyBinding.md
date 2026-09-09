# Task005 - Battle Dependency Binding

Series: ArchitectureRefactor
Status: Completed - Remaining Targeted Acceptance Waived For This Iteration
Branch: `codex/architecture-refactor`
Depends on: Task004 implementation and confirmed Stage1/Recorder evidence; remaining targeted native acceptance is not implicitly waived. Preserve Task001-Task003 contracts.

## 1. Problem And Goal

EffectTargetResolver resolves radius targets by finding the first MonsterManager
in the scene. EffectExecutor does the same when spawning WindVortex. Combat and
placement also contain fallback discovery. Most Stage/Battle composition already
uses explicit bindings, so Effect execution is an inconsistent dependency boundary.

Make gameplay query dependencies belong to the Battle that produced the action.
An invalid reference must not silently reconnect to another Battle or test fixture.

## 2. Proposed Contract

- The existing composition/coordinator owns binding lifetime. Pass the minimum
  required Monster query dependency explicitly into runtime owners and Effect
  execution, using a small typed dependency slice where propagation requires it.
- Propagate the same dependency through Projectile, Magic, Drone, Buff lifecycle,
  nested/multi-target Effects, and WindVortex creation/ticks. Include target-owned
  and source-Tower-absent Buff paths; do not require a live Tower for fixed Buff
  damage or owner-bound removal cleanup.
- A released entity retains its originating Battle dependency. Losing a source
  Tower does not cause global lookup or inheritance of a new Stage's manager.
- A stopped/released Battle cannot supply new target acquisition, damage, spawning,
  or revived combat. If a manager is reused across retries, a generation/lease
  check or equivalent reviewed lifetime mechanism must reject old references.
- Preserve teardown-only Buff cleanup permissions after targetability closes.
  Closing gameplay authority must not prevent removal of owner-bound slow/lock
  state or safe visual/entity cleanup.
- Missing bindings fail preparation where possible. Runtime loss follows the
  existing owner's cancellation/technical-failure policy; explicitly map each
  path during review instead of inventing a global exception policy.
- No static singleton or globally cached first-found manager replaces discovery.

## 3. Scope And Source Pointers

- `Assets/Scripts/BuffAndEffect/EffectTargetResolver.cs`, `EffectExecutor.cs`,
  `EffectTriggerContext.cs`, Buff execution producers, and `WindVortexBehaviour.cs`
- Relevant initialization/Effect producers in Projectile and TowerRuntimeCombat
- Existing Stage/Battle composition and the owners extracted in Task004
- Gameplay fallback paths in TowerCombatBehaviour and placement initialization

Inventory gameplay `FindFirstObjectByType` calls and remove those addressed by
this contract. Recorder-only discovery belongs to Task006. Presentation hierarchy
lookups and optional component authoring are not a repository-wide target.

No DI container, generic service locator, multi-Battle feature, damage-formula
revision, Buff stacking change, or attack-entity state-machine rewrite.

## 4. Documentation And Review Decisions

Read [Stage](../System/02_StageSystem.md), [Monster](../System/07_MonsterSystem.md),
[Combat](../System/11_TowerRuntimeCombatSystem.md),
[Projectile](../System/12_ProjectileSystem.md),
[Effect](../System/14_EffectSystem.md), and [Buff](../System/15_BuffSystem.md).

Review the dependency propagation map, source-less/removed-owner behavior,
reused-manager lifetime protection, and per-owner failure routing. Update durable
scope/lifetime contracts after approval; keep injection mechanics in this Task.

## 5. Implementation Sequence

- Inventory every Effect call and runtime initialization chain before changing APIs.
- Add the narrow binding with a clear lifetime and wire composition explicitly.
- Migrate direct, nested, periodic, overload, and persistent Effect producers.
- Remove gameplay discovery fallback and verify intentional cleanup exceptions.
- Validate affected prefab/scene references and document Inspector handoff.

## 6. Acceptance

| Case | Required evidence |
|---|---|
| Radius Effect, nested Effect, WindVortex | Query only the bound Monster collection |
| Two managers in an isolated test | No accidental first-found selection; not a new product feature |
| Retry reusing manager with old entity/context | Old generation cannot output into the new Battle |
| Source Tower absent but valid Buff owner/Battle | Approved fixed Buff behavior preserved |
| Battle stop and Buff removal | No new combat; permitted owner-bound cleanup still completes |
| Invalid preparation/runtime binding | Reviewed rejection/cancellation/failure path; no fallback |
| Archer/Cannon/Magic/Drone and shared reactions | Damage, application eligibility, and targeting contracts preserved |

Run both reference-wiring/static checks and focused Play Mode lifecycle/Effect
coverage. Relevant shared-hit-reaction and Drone edge cases historically unrun
need new evidence if touched; historical waivers do not automatically transfer.

## 7. Completion

Record the complete migrated producer list, binding lifetime, failures exercised,
serialized-reference checks, and runtime evidence. Task006 must observe these
owners without obtaining gameplay authority. Implementation and managed evidence are recorded below; native acceptance remains separate.


## 8. Approved Implementation Boundaries

- One coordinator-created BattleCombatBinding per preparation; activation is explicit,
  closure irreversible. Runtime owners and children retain that exact identity.
- Terminal, Stop and Release acceptance revoke combat and Submission authority before
  evidence callbacks, including deferred Release. Physical cleanup remains deferred
  where required; closing an old identity cannot reactivate it or close a newer one.
- Migrate TowerOwnedHitTransaction as well as Effect contexts. Recheck after damage
  callbacks and before remaining application/reaction consequences. Preserve committed
  damage evidence and unconditional transaction finalization.
- Removed permission is synchronous and scoped to the exact Monster runtime identity.
  Only owner slow removal/unlock can execute through it. Dead-owner earned Overload
  still requires active originating gameplay authority.
- Closed/stale identities cancel quietly; a broken active Monster dependency reports
  technical failure once. Component recovery only uses the same valid binding.
- Producer inventory: TowerCombat, Projectile (including bounce children), Magic Orb
  group/Detonation/Field, Drone/projectile/Blast/Final Dive, ElementalApplication,
  TowerOwnedHitTransaction, MonsterBuffRuntime lifecycle/reactions/Overload, nested
  Effect and WindVortex creation/ticks. Recorder discovery stays in Task006.


## 9. Implementation And Managed Evidence — 2026-09-10

- The coordinator creates a new binding during preparation, opens it after Monster
  Begin, and revokes binding plus Submission before terminal/Stop/Release evidence
  callbacks. Deferred Release revokes immediately. Old bindings never reopen.
- Monster runtime reference binding precedes registration. Buff instances retain
  both Battle identity and Monster runtime identity; synchronous Removed permissions
  expire in finally and reject another owner or a reset lifetime.
- All producers listed in Section 8 now pass the required Effect context binding.
  Attack entity initializer parameters carry the binding itself, so bounce/Drone
  children cannot recapture a reused manager's current identity. Tower preparation
  receives the coordinator-bound manager's current binding once.
- Removed effects retain normal active-Battle behavior; after closure their permission
  permits only owner slow removal/unlock. Pending completion without active authority
  removes remaining state as technical cleanup when the owner is alive, rather than
  falsely reporting a Monster kill. Source-independent FixedBuff behavior is retained.
- Direct-hit finally preserves committed damage observations and releases the hit
  transaction even if a health callback throws. Effects recheck before target commits,
  including after Elemental dispatch observers. No damage formula or stacking value
  changed.
- All 5 scoped gameplay global lookups were removed. Placement receives its manager
  and pathfinding service explicitly before Map binding. Remaining global lookups in
  gameplay sources belong only to Recorder (Task006).
- No new Inspector component or reference is required. Existing serialized files and
  GUIDs were not changed by this implementation; new source files have new GUIDs.

Validation:

- `python3 Tests/Task005/run.py`: 27 checks through production binding, Effect context,
  resolver, executor and direct-hit transaction, plus extracted production Stop/Release
  routing. Native boundaries, Monster/Buff storage and Vortex runtime are doubles;
  see `Tests/Task005/README.md` for the precise coverage limit.
- Task001: 30 managed regressions passed.
- Task002: 32,192 baseline/current fixture observations match.
- Task003: 14 managed tests and 300 baseline Draft traces passed.
- Task004: 23 complete Submission managed contracts passed. Its routing harness
  substitutes the new revocation boundary; Task005 exercises that boundary directly.
- Runtime/Editor compilation: 0 warnings, 0 errors. Whitespace checks passed.

## 10. Native Acceptance Handoff — Partially Verified

1. Start a fresh Battle; exercise deployment, Level Up and Upgrade. Confirm target
   acquisition and attack release remain normal without missing-binding messages.
2. Exercise Archer/Cannon projectile and area/bounce paths, Magic Orb/Detonation/Field,
   and Drone opening/later shots, Holding, Blast and Final Dive as applicable to the
   authored build. Check normal eligibility and damage behavior with fresh Recorder data.
3. Exercise Fire ticks/Overload, Cold/Frozen removal, Electric/Wind shared reactions,
   nested Overload and persistent WindVortex. Source-less FixedBuff and dead-owner
   earned Overload require targeted evidence rather than inference from ordinary play.
4. Stop/end with live projectiles, Drone/Magic entities, Vortex and Buffs; retry and
   enter the next Stage. No outgoing entity may resume, acquire a new target or damage
   the new Battle. Old slow/lock state must not survive or clear a new owner's state.
5. Targeted lifecycle checks: disable/re-enable TowerCombat within the same valid Battle;
   break a required active Monster binding (one technical failure, no global recovery);
   Stop from a health callback and from pre-cleanup observers. Managed checks support
   these boundaries but are not Unity-native lifecycle acceptance.

Record actual scenarios and any explicit waiver before marking Completed. Fresh
Task005 Recorder coverage is recorded below. Task004's remaining native cases remain
separate, and no commit or push is part of this implementation handoff.


## 11. Native Recorder Review — 2026-09-10

All four schema-25 exports below were inspected under `Doc/GamePlayRecord/`.
Each reports all 32 integrity flags true. Independent reconciliation confirms every
Consumed selection token has exactly one investment with the matching selected asset;
all four terminal Pending lists are empty. All runs used Natural generation.

| Record | Outcome | Actual coverage |
|---|---|---|
| `Task005_Stage4_FourTower_DeploymentUpgrade_Acceptance_01.json` | Victory; 56 kills, no leaks; Health 5 to 5 | 3 deployments (Cannon, Magic, Drone), 3 Level Ups, 2 Magic Upgrades (Twin Orbs/Arcane Detonation); 10 successful Detonation applications |
| `Task005_Stage5_SingleElemental_Acceptance_01.json` | Defeat; 46 kills, 6 leaks, 24 unresolved at terminal | All 4 families deployed; 3 Level Ups, 2 Cannon Basic Upgrades; no Elemental offer/selection; 4 forced relocations; 1 Drone TechnicalCleanup |
| `Task005_Stage5_SingleElemental_Acceptance_02.json` | Defeat; 67 kills, 6 leaks, 3 unresolved at terminal | All 4 families deployed; 4 Level Ups, Magic Faster Orbit/Arcane Detonation; 11 successful Detonation applications; no Elemental offer/selection |
| `Task005_Stage5_SingleElemental_Acceptance_03.json` | Defeat; 62 kills, 6 leaks, 8 unresolved at terminal | Magic/Archer/Drone; 5 Level Ups; Archer Gale Arrows and Magic Arcane Field; 1 Drone TechnicalCleanup |

Stage5 Health is 6 to 0 in each run. Unresolved Monsters are terminal snapshots
at Defeat, not evidence of a stuck Monster. Stage5 run 01 ended before all expected
progression rewards; runs 02/03 report completed expected progression. Recorder's
final-build flag does not establish a Reference or Coherent Alternative build.

Wind coverage in Stage5 run 03:

- Windcut: 81 attempts = 24 Applied + 55 Stacked + 2 Protection-blocked; no Invalid.
  79 stack units applied, 1 Overload and 1 Protection entry.
- 123 reaction opportunities = 87 Triggered + 20 Cooldown-blocked + 14 without a
  valid secondary target + 2 invalidated by Buff instance/cycle changes.
- Wind reaction damage: 87 successful applications, 2,436 fixed damage.
  WindVortex: 5 successful tick applications, 25 fixed damage.
- Arcane Field: 890 successful applications, 2,670 damage.
- Drone Holding/reacquisition is observed in Stage4 and Stage5 runs 02/03.

These exports support normal three-intent execution, all four base Tower families
across the cohort, the observed Magic packages, Wind reaction/nested Effect and
Vortex tick paths, and observed Drone cleanup. They do not prove cross-Stage stale
entity isolation, Console cleanliness, Fire/Cold/Electric coverage, every Behaviour
package, source-less native Buff behavior, or native fault injection. Remaining
Section 10 cases have not been waived; Task005 is not marked Completed.

User feedback: constructing a Reference/Coherent Alternative build from Stage5
onward feels too difficult; the user intends to address this through design later.
Runs 01/02 contain no displayed Elemental option; run 03 offers Gale Arrows at draft
ordinal 9. This is concrete accessibility evidence, not a probability estimate or a
basis for changing balance values in this refactor. No runtime/assets were changed
as part of this record review.


## Iteration Closeout — 2026-09-10

The user explicitly elected to close the ArchitectureRefactor iteration after the
completed implementation and recorded managed/native evidence, and to investigate
further cases when a concrete bug or unexpected behavior is observed. Remaining
unexecuted acceptance checks are waived for this iteration, not recorded as passed.
This closeout supersedes earlier pending/no-waiver status statements in this file;
the earlier sections remain the historical record of actual coverage.

Task001–Task006 are complete within their approved refactor scope. No new gameplay,
balance tuning or speculative follow-up implementation is included. Known coverage
limits remain available for future diagnosis; completion is not a zero-bug claim.
