# Task005 - Battle Dependency Binding

Series: ArchitectureRefactor
Status: Draft - Pending Review
Branch: `codex/architecture-refactor`
Depends on: Accepted Task004; preserve Task001-Task003 contracts.

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
owners without obtaining gameplay authority. No implementation or evidence exists yet.
