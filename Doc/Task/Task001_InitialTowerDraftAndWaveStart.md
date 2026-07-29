# Task001 - Initial Tower Draft And Wave Start

Status: Not started

Depends on: Existing Stage preparation, Battle runtime, Draft, Battle HUD UI, and Monster Wave runtime foundations

## 1. Goal

Implement exactly one Initial Tower Draft at the start of every fresh Stage battle.

After Game Flow permits the prepared Stage to enter Battle, the player must select one Stage-allowed TowerDefinition and receive one held Tower Draft item before the first Monster Wave Delay begins.

The Initial Draft is an explicit Battle-start transaction. It does not grant Player progress, trigger Player level-up, or require the selected Tower to be deployed before Wave timing begins.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_GameFlowSystem.md`
- `Doc/02_StageSystem.md`
- `Doc/03_PlayerSystem.md`
- `Doc/04_BattleHUDUISystem.md`
- `Doc/07_MonsterSystem.md`
- `Doc/08_DraftSystem.md`
- `Doc/09_TowerPlacementSystem.md`

## 3. Pre-Implementation State

- `BattleRuntimeCoordinator.BeginPreparedBattleCore()` opens the Player, Monster, Placement, Draft, and Spawner battle gates and then immediately calls `MonsterSpawner.StartSpawning()`.
- `DraftSystem` opens a Draft only in response to `PlayerSystem.OnLevelUp`.
- `DraftSystem.OpenTowerDraft()` combines Tower and currently eligible Tower Upgrade candidates.
- Draft selection asks `BattleHUDUI` to create a held `PendingDraftUIItem`, but that creation currently has no success result.
- `DraftUI` closes after forwarding a valid selection regardless of whether the held item was created.
- Fresh Player state begins at Level 1 and zero Progress.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| BattleRuntimeCoordinator | Order Battle gate opening, Initial Draft request, and first Wave start |
| DraftSystem | Own the one-per-battle Initial Draft attempt, Tower-only candidate generation, selection validation, and completion/failure facts |
| BattleHUDUI | Create and track one held Tower Draft item and report whether creation succeeded |
| DraftUI | Present the supplied candidates, emit one selection intent, and close only through an accepted or terminated Draft flow |
| MonsterSpawner | Keep Wave execution stopped until explicitly authorized after Initial Draft completion |
| PlayerSystem | Preserve the normal Level 1 and zero-Progress fresh-battle state |
| Game Flow | Permit the prepared Stage to begin; it does not open the Draft or start Waves directly |

## 5. Battle-Start Sequence

`BattleRuntimeCoordinator.BeginPreparedBattleCore()` changes to the following sequence:

```text
Validate Prepared Battle
    -> Open All Battle Consumer Gates
    -> Request One Initial Tower Draft
    -> Confirm Initial Draft Window Opened With At Least One Valid Choice
    -> Return A Successfully Begun Battle
    -> Wait For Player Selection
    -> Create One Held Tower Draft Item
    -> Publish Initial Draft Completion
    -> Start First Monster Wave Delay
```

`MonsterSpawner.StartSpawning()` must not be called in the initial synchronous Battle-start transaction.

Successfully opening the Initial Draft is enough for `BeginPreparedBattle()` to succeed. The Battle remains active while it waits for the asynchronous player selection.

Entering the Battle Game Flow state, opening the Draft Window, closing it technically, or beginning a selection callback does not authorize Wave execution.

## 6. Initial Draft Candidate Contract

Add an explicit Initial Draft request owned by `DraftSystem`. It must not be implemented by mutating Player progress or invoking Player level-up.

The Initial Draft:

- Samples only valid TowerDefinitions from the active Stage Tower Draft Pool.
- Does not include TowerUpgradeDefinitions.
- Uses the configured display-count limit.
- Shows one or two choices when only one or two distinct valid TowerDefinitions are available.
- Rejects an empty valid candidate set as a technical Battle-start failure.
- Does not change the later Player level-up Draft candidate rules.

Stage order may intentionally produce one choice in Stage 1, two choices in Stage 2, and a wider pool in later Stages.

## 7. Attempt And Reentrancy Guards

Draft System tracks the Initial Draft independently from Player level-up Drafts.

For one fresh Battle it must distinguish:

```text
Not Requested
Open And Awaiting Selection
Completed
Failed Or Cancelled By Battle Stop
```

Rules:

- At most one Initial Draft request is accepted per fresh Battle.
- Repeated Battle-start callbacks cannot open duplicate Initial Draft Windows.
- A selection is accepted only for the currently open Initial Draft and one of its displayed identities.
- The completion guard is established before publishing completion.
- Nested callbacks cannot create a second held item or start Waves twice.
- Preparing or retrying a fresh Stage resets the Initial Draft state.
- Battle stop, Stage release, disable, or technical rollback invalidates any pending selection without reporting completion.

Player level-up Drafts remain unavailable while the Initial Draft is unresolved. The first version does not queue overlapping Draft Windows because no Monster progress can occur before Wave execution starts.

## 8. Selection Commit Boundary

Initial Draft completion requires a committed held item:

```text
Receive Selection Intent
    -> Verify Active Initial Draft And Displayed Identity
    -> Create And Initialize One Pending Tower Draft Item
    -> Register It In The Battle HUD Held-Item Collection
    -> Confirm Held-Item Creation
    -> Mark Initial Draft Completed
    -> Close The Draft Window
    -> Notify BattleRuntimeCoordinator
```

`BattleHUDUI` must provide an explicit success/failure result for held-item creation. A `void` request is not sufficient for the Initial Draft transaction.

If held-item creation fails:

- Do not report Initial Draft completion.
- Do not start Monster spawning.
- Do not consume or silently lose the selection.
- Report a technical startup failure to the Battle authority and close the Battle through the existing rollback/stop path.

The selected Tower does not have to be placed before completion. Placement consumes the held item later through normal Tower Placement rules.

## 9. Wave Authorization

`BattleRuntimeCoordinator` consumes exactly one Initial Draft completion fact for the current active Battle.

It then calls `MonsterSpawner.StartSpawning()` exactly once. The first configured Wave Delay begins at that point.

Before accepting completion, the coordinator verifies:

- The Battle is still active.
- No semantic Battle result has been established.
- The Initial Draft has not already authorized Wave execution.
- Draft System confirms one held Tower Draft item was committed for this attempt.

A synchronous `StartSpawning()` failure is a technical Battle failure, not Victory or Defeat. It closes Battle authority and performs the existing technical cleanup without publishing a semantic result.

## 10. UI And Input Contract

- The Initial Draft uses the existing Draft Window and Tower content item presentation.
- While the Draft Window is open, it is modal to battlefield input.
- Camera pan, Tower placement, and other battlefield pointer actions cannot begin through the Draft Window.
- After selection creates the held item and closes the window, normal Battle interaction resumes.
- The held item's drag gesture continues to own its pointer until release or cancellation.

This task does not implement time-scale changes. A future slow-motion design may alter time scale while the Draft Window or held-item drag is active without changing the Initial-Draft-gated Wave-start contract.

## 11. Failure And Cleanup

The following are technical failures or cancellations and must not authorize Waves:

- No valid Initial Tower Draft candidate
- Missing or unusable Draft presentation
- Failure to create a selectable Draft item
- Failure to create or register the selected held item
- Battle stop or Stage release while waiting for selection
- Draft System or UI disable while waiting for selection
- Late selection from an earlier Stage or retry

Cleanup is idempotent. It closes the Draft Window, invalidates callbacks, clears transient choice items, and leaves no Initial Draft completion callback able to enter a later Battle.

## 12. Out Of Scope

- Starting Player progress above zero
- Triggering a synthetic Player level-up
- Requiring Tower deployment before the first Wave Delay
- Changing PlayerLevelConfig
- Changing later level-up Draft weighting or eligibility
- Draft reroll, skip, ban, or replacement rewards
- Time-scale changes or pause behavior
- Camera Pan implementation
- New TowerDefinition content

## 13. Unity Authoring Checklist

- Confirm `DraftSystem`, `BattleHUDUI`, `DraftUI`, and `MonsterSpawner` references remain assigned on the active Game runtime prefab.
- Confirm every playable Stage has at least one valid TowerDefinition in its Tower Draft Pool.
- Confirm the Draft item prefab can create a valid selectable Tower item.
- Confirm the Pending Draft item prefab contains `PendingDraftUIItem` and can be registered under the authored held-item container.
- Do not add placeholder TowerDefinitions merely to force three visible choices.

## 14. Acceptance Criteria

- Every initial Stage, next Stage, and retry opens exactly one Initial Tower Draft after Battle start permission.
- Stage 1 may open with one valid choice and Stage 2 may open with two.
- The Initial Draft contains Tower choices only.
- Player remains Level 1 with zero Progress before and after the Initial Draft.
- No first-Wave Delay or Monster spawning begins while the Initial Draft is open.
- One accepted selection creates exactly one held Tower Draft item.
- The first-Wave Delay begins only after that held item exists.
- The player may deploy the held Tower while the first-Wave Delay advances.
- Repeated or late callbacks cannot create another held item or start spawning twice.
- Stop, release, retry, and technical failure do not leak callbacks or Draft state across Battles.
- Later Player level-ups continue to open the normal combined Draft.

## 15. Static Validation

- Build `Assembly-CSharp.csproj` without restoring packages.
- Run path-scoped `git diff --check` for touched scripts and documents.
- Search the Battle-start path to confirm `StartSpawning()` is reachable only through the Initial Draft completion authorization.
- Search for any synthetic Player progress or level-up call added for Initial Draft and reject it.
- Confirm all Initial Draft callbacks have symmetric reset or unsubscription paths.

## 16. Unity Play Mode Handoff

Verify in Unity:

- Initial Stage, next Stage, and retry behavior
- One-choice, two-choice, and three-choice Initial Draft presentation
- No Wave Delay consumed while thinking in the Initial Draft
- Held-item creation before first-Wave authorization
- Deployment during the first-Wave Delay
- Stop or return-to-main-menu while the Initial Draft is open
- Repeated selection input and late callback rejection
- Normal later level-up Draft behavior

Static build and document validation do not prove these Play Mode behaviors.
