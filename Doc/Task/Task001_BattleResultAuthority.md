# Task001 - Battle Result Authority

Status: Not started

Depends on: Existing Monster resolution, Player defeat, Battle stop, and Stage Composition runtime foundations

## 1. Goal

Implement one authoritative semantic result boundary for each active Battle.

The Battle runtime must distinguish:

- Victory
- Defeat
- Technical stop or cleanup with no semantic result

Victory is valid only after every configured Monster has been spawned normally, no alive unresolved Monster remains, and Player System is not defeated. Defeat is valid when Player System enters its terminal defeated state. One Battle may publish at most one result.

This task establishes Battle outcome facts only. It does not choose the next Stage, retry the current Stage, return to the main menu, or present result UI.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_GameFlowSystem.md`
- `Doc/03_PlayerSystem.md`
- `Doc/04_BattleHUDUISystem.md`
- `Doc/06_MonsterSystem.md`
- `Doc/07_DraftSystem.md`
- `Doc/08_TowerPlacementSystem.md`
- `Doc/10_TowerRuntimeCombatSystem.md`

## 3. Pre-Implementation State

- Player System already publishes one terminal Defeat notification after health reaches zero.
- BattleRuntimeCoordinator consumes Defeat and performs technical Battle stop.
- MonsterSpawner clears its spawn routine after normal completion, stop, disable, or failure, but does not expose a semantic distinction among those cases.
- MonsterManager unregisters a resolved Monster before sending its resolution fact into the Player transaction.
- There is no post-Player-resolution Monster completion notification.
- There is no Victory detection.
- There is no common BattleResult value or exactly-once result publication boundary.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| MonsterSpawner | Report normal completion of all configured spawning |
| MonsterManager | Report that one Monster resolution and its Player transaction have fully completed |
| PlayerSystem | Remain the sole authority for Player health and Defeat |
| BattleRuntimeCoordinator | Aggregate completion facts, establish one BattleResult, close Battle authority, perform technical stop, and publish the result |
| Game Flow | Future consumer of the result; no implementation in this task |

Monster System supplies facts. It does not decide Stage Victory presentation or navigation.

Player System supplies Defeat. It does not decide retry or return-to-main-menu behavior.

BattleRuntimeCoordinator owns result aggregation because it already owns the Battle-active authority and technical stop boundary. It must not absorb Stage sequence or UI responsibilities.

## 5. Battle Result Model

Add one explicit Battle result value with exactly these first-version outcomes:

```text
Victory
Defeat
```

Do not use `bool` because the caller must not infer which value means which result.

Do not add values for:

- Stopped
- Cancelled
- Released
- PreparationFailed
- ReturnedToMenu

Those are technical lifecycle outcomes, not semantic Battle results.

BattleRuntimeCoordinator exposes one read-only result publication event for future Game Flow consumption. The result is published only after Battle gameplay authority has closed and required technical stop work has completed.

## 6. Normal Spawning Completion

MonsterSpawner must distinguish normal Wave completion from every other way spawning can end.

Normal completion means:

- The selected MonsterWaveConfig was valid when execution began.
- Every configured Wave was processed in order.
- Every configured Spawn Entry was processed in order.
- Every configured Monster count was successfully processed.
- The routine was not stopped, cancelled, disabled, or aborted by invalid runtime content.

Only that path publishes the all-spawning-completed fact.

The completion fact must not publish when:

- Battle stop cancels spawning.
- Stage release clears the Stage binding.
- The spawner is disabled.
- The selected Wave or Spawn Entry becomes invalid.
- A required Monster cannot be created or registered.
- Preparation rollback occurs.

The spawning-completed state resets before every prepared Battle and during Stage-runtime release.

## 7. Post-Resolution Completion

MonsterManager keeps the approved Monster-resolution order:

```text
Monster Enters Terminal State
    -> MonsterManager Unregisters Monster
    -> PlayerSystem Resolves Progress, Health, And Defeat Atomically
    -> MonsterManager Reports Resolution Completed
```

The post-resolution notification occurs only after PlayerSystem has completed the full accepted transaction.

It must not occur for:

- ForceCleanup
- Stage release
- Battle stop cleanup
- Duplicate terminal callbacks
- A resolution rejected because Battle authority is already closed

The notification does not carry or mutate Player state. BattleRuntimeCoordinator may query the authoritative Player defeated state and current Monster alive set after receiving it.

## 8. Victory Evaluation

BattleRuntimeCoordinator evaluates Victory after either:

- Normal spawning completion
- A post-Player-resolution Monster completion

Victory requires all conditions at the same evaluation boundary:

```text
Battle Is Active
AND Normal Spawning Has Completed
AND Alive Unresolved Monster Count Is Zero
AND Player Is Not Defeated
AND No Result Has Already Been Established
```

An temporarily empty battlefield between Waves is not Victory.

An empty battlefield caused by technical cleanup is not Victory.

An invalid or aborted spawn sequence is not Victory.

## 9. Defeat Precedence

PlayerSystem remains the sole Defeat authority.

When the final Monster reaches the Target and reduces Player health to zero:

1. MonsterManager has already removed that Monster from the alive set.
2. PlayerSystem completes progress, health, and Defeat mutation.
3. Player Defeat is published.
4. BattleRuntimeCoordinator establishes Defeat and closes Battle authority.
5. Any later post-resolution completion notification observes an inactive or already-completed Battle and cannot establish Victory.

This ordering makes Defeat authoritative even when the same resolution leaves zero alive Monsters after all spawning has completed.

Do not implement result precedence by timing assumptions, deferred destruction, frame delay, or UI state.

## 10. Result Completion Transaction

Use one guarded result-completion transaction:

```text
Receive Candidate Result
    -> Reject If Battle Is Inactive
    -> Reject If A Result Already Exists
    -> Record Result Guard
    -> Close Battle Authority And Consumer Gates
    -> Stop Spawning And New Runtime Output
    -> Force-Clean Remaining Monsters When Required
    -> Stop Tracked Tower Combat And Attack Entities
    -> Stop Draft And Placement Interaction
    -> Publish One Semantic BattleResult
```

The result guard must be established before cleanup begins so nested callbacks cannot complete the Battle again.

The semantic result publishes after technical stop. Consumers must never receive a result while Towers, spawning, Draft interaction, or Player resolution can continue producing gameplay.

Technical stop methods remain idempotent and result-neutral.

## 11. Reset And Subscription Lifecycle

- Subscribe to spawning-completion, post-resolution-completion, and Player Defeat facts symmetrically.
- Re-enabling a coordinator must not duplicate subscriptions.
- Disabling or destroying runtime owners must not publish a result.
- Preparing a fresh Battle resets all prior result and spawning-completion guards.
- Releasing Stage runtime clears result-tracking state after gameplay authority is closed.
- A result from a prior Battle cannot enter a later Battle session.

## 12. Failure Handling

If Battle startup or Wave execution fails before a semantic result:

- Keep or return Battle authority to stopped.
- Perform existing rollback or technical cleanup.
- Do not publish Victory or Defeat.
- Log the concrete failing owner and reason.
- Leave Game Flow free to handle preparation failure separately in Task003.

This task must not silently convert invalid authoring or startup failure into Defeat.

## 13. Out Of Scope

- Stage sequence and current Stage index
- Main menu
- Next-Stage, retry, or return-to-menu transitions
- Stage Introduction content
- GameFlowController
- Victory or Defeat windows
- Save data or unlock rules
- Pause flow
- Scene transitions
- Changes to Monster progress or Player damage formulas
- Changes to Wave ordering or Spawn Interval semantics
- New generic event bus or service locator

## 14. Acceptance Criteria

- One explicit BattleResult type represents Victory and Defeat.
- MonsterSpawner reports normal spawning completion exactly once per successful Battle execution.
- Stop, disable, release, invalid content, and spawn failure do not report normal completion.
- MonsterManager reports resolution completion only after the Player transaction completes.
- ForceCleanup and technical teardown do not report resolution completion.
- Victory cannot occur before normal spawning completion.
- Victory cannot occur while an alive unresolved Monster remains.
- Victory cannot occur after Player Defeat.
- The final Target arrival at one remaining Player health produces Defeat only.
- One Battle publishes at most one result under nested or repeated callbacks.
- Battle authority closes before the result is published.
- Technical StopBattle and Stage release remain result-neutral.
- A fresh Battle does not retain the prior Battle result or spawning-completion state.

## 15. Static Validation

Run at minimum:

- Main Unity assembly compilation
- `git diff --check`
- Search for result publication outside the authoritative completion transaction
- Search for spawning-completion publication from stop, disable, or cleanup paths
- Search for Monster post-resolution publication before the Player transaction
- Review subscription symmetry and exactly-once guards

Static validation does not prove event timing or Unity lifecycle behavior.

## 16. Unity Play Mode Handoff

Validate at minimum:

- All Monsters die after all spawning completes: one Victory
- All Monsters resolve before a later Wave begins: no early Victory
- Final Monster dies before all configured spawning completes: no early Victory
- Final Monster reaches Target without causing Defeat: Victory only after Player resolution
- Final Monster reaches Target at one Player health: one Defeat and no Victory
- Player Defeat while other Monsters remain: one Defeat and remaining runtime stops
- Manual technical stop: no result
- Stage release: no result
- Stop during Wave delay or Spawn Interval: no spawning-completion fact
- Fresh Battle after Victory or Defeat: no retained result state

Unless explicitly handed over, Codex owns runtime scripts and static checks; the user owns final prefab/scene wiring and Unity Play Mode acceptance.
