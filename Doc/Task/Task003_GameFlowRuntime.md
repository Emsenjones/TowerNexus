# Task003 - Game Flow Runtime

Status: Not started

Depends on: Task001, Task002

## 1. Goal

Implement one authoritative Game Flow runtime that owns the ordered Demo Stage sequence and all legal transitions among:

- Main Menu
- Stage Preparing
- Stage Introduction
- Battle
- Stage Victory
- Stage Defeat

The runtime must drive Stage preparation, begin, release, next Stage, retry, and return-to-main-menu behavior without depending on concrete UI components.

UI added in Task004 will send semantic intent into this runtime and present its read-only state.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_GameFlowSystem.md`
- `Doc/02_StageSystem.md`
- `Doc/03_PlayerSystem.md`
- `Doc/Task/Task001_BattleResultAuthority.md`
- `Doc/Task/Task002_StagePreparationAndIntroduction.md`

## 3. Pre-Implementation State

- Task001 provides one authoritative BattleResult after Battle authority closes.
- Task002 provides separate Stage prepare, begin, and release boundaries.
- StageDefinition provides optional Introduction content.
- No runtime owner holds the ordered Demo Stage list or current index.
- No explicit Game Flow state guards exist.
- There is no single path for new run, next Stage, retry, or return to main menu.
- UI does not yet have a semantic Game Flow API.

## 4. Ownership

Create one `GameFlowController` as the only runtime owner of:

- Ordered Demo StageDefinition list
- Current Stage index
- Current GameFlowState
- Current selected StageDefinition
- New-run initialization
- Stage prepare and begin requests
- BattleResult consumption
- Next-Stage, retry, and return-to-main-menu transitions
- Flow-transition reentrancy guard
- Read-only state notifications consumed by Task004 UI

GameFlowController must not:

- Validate Map internals
- Mutate Player state directly
- Execute Wave spawning
- Count alive Monsters
- Determine Victory or Defeat from low-level facts
- Generate Drafts
- Own Tower placement or combat
- Own concrete UI objects

## 5. Game Flow State Model

Add one explicit `GameFlowState` value with:

```text
MainMenu
StagePreparing
StageIntroduction
Battle
StageVictory
StageDefeat
```

Do not infer Game Flow state from:

- Active UI object
- Stage map existence
- Coroutine state
- Player health display
- BattleResult window visibility
- Current Stage index alone

GameFlowController exposes the current state read-only and publishes one state-change notification after each accepted transition commits.

## 6. Stable References And Runtime State

GameFlowController requires stable references to:

- Ordered Demo `List<StageDefinition>`
- StageCompositionController
- BattleRuntimeCoordinator

It owns runtime state:

- Current Stage index
- Current GameFlowState
- Whether a transition is in progress
- Last accepted BattleResult when in a result state

The current index is invalid while no game run is active. Main Menu does not implicitly retain a completed or failed run.

Do not copy StageDefinition content into GameFlowController. It retains only the ordered direct references.

## 7. Demo Stage Sequence Validation

Validate at minimum:

- Stage list is not null or empty.
- No Stage entry is null.
- No StageDefinition reference appears more than once.
- Current index is either inactive or inside the list.
- Every selected Stage passes owner Stage validation during preparation.

The current Demo target is at least five StageDefinitions, but runtime flow must use the authored list count rather than hard-code five.

A shorter non-empty list may support content production. Report the Demo target as a warning rather than making all intermediate authoring unplayable.

Do not silently remove duplicates, reorder Stages, skip invalid entries, or fall back to another Stage.

## 8. Application Entry And Main Menu

Application entry establishes Main Menu:

```text
Close Or Confirm Closed Battle Authority
    -> Release Any Residual Stage Runtime
    -> Clear Current Run Index
    -> Clear Prior Result State
    -> Enter MainMenu
```

Entering Main Menu must be safe from:

- Initial application startup
- Final Stage Victory
- Victory return action
- Defeat return action
- Preparation failure fallback

Main Menu entry is technical cleanup and does not manufacture a Battle result.

## 9. Start New Run

Accept Start New Run only from MainMenu.

```text
Receive Start Intent
    -> Validate Stage Sequence
    -> Set Current Index To Zero
    -> Enter StagePreparing
    -> Prepare Current Stage
```

Repeated Start intents while preparation is in progress or after leaving MainMenu are ignored or rejected without producing a second Stage.

## 10. Prepare Current Stage

Use one internal transition for initial Stage, next Stage, and retry:

```text
Enter StagePreparing
    -> Ask StageCompositionController To Prepare Current StageDefinition
    -> Preparation Fails
        -> Release Partial Runtime
        -> Clear Active Run
        -> Return To MainMenu
    -> Preparation Succeeds
        -> Introduction Requested And Presentable
            -> Enter StageIntroduction
        -> Otherwise
            -> Begin Prepared Stage
            -> Enter Battle
```

GameFlowController does not reset Player fields itself. It relies on Task002 Stage preparation to establish:

- Initial Player level
- Zero progress
- Current Stage maximum health
- Current health equal to that maximum
- Non-defeated state

## 11. Stage Introduction Confirmation

Accept Introduction confirmation only from StageIntroduction.

```text
Receive Confirmation
    -> Ask StageCompositionController To Begin Prepared Stage
    -> Begin Succeeds
        -> Enter Battle
    -> Begin Fails
        -> Release Stage Runtime
        -> Clear Active Run
        -> Return To MainMenu
```

Confirmation does not:

- Prepare the Stage again
- Increment the Stage index
- Reset Player state again
- Mutate Introduction configuration

If the Stage requested an Introduction but contains no presentable items, the authoring warning from Task002 is preserved and Game Flow begins the prepared Stage directly.

## 12. Battle Result Consumption

Subscribe to Task001 BattleResult publication symmetrically.

Accept a result only when:

- Current state is Battle.
- No flow transition is already in progress.
- The result belongs to the current active Battle.

Map results:

```text
Victory -> StageVictory
Defeat  -> StageDefeat
```

The result arrives after Battle authority and runtime output have stopped. GameFlowController must not call technical stop again as a substitute for result validation, though idempotent cleanup remains safe.

Late, duplicate, or prior-session results are ignored and reported where appropriate.

## 13. Victory Continuation

Accept Continue After Victory only from StageVictory.

If another Stage exists:

```text
Release Completed Stage Runtime
    -> Increment Current Index Once
    -> Clear Prior Result State
    -> Prepare Current Stage
```

If the completed Stage is the final Stage:

```text
Release Completed Stage Runtime
    -> Clear Current Run Index
    -> Clear Prior Result State
    -> Enter MainMenu
```

The final-Stage condition is derived from:

```text
Current Index == Stage List Count - 1
```

Do not author a separate final-Stage flag on StageDefinition.

## 14. Defeat Actions

Accept Retry only from StageDefeat:

```text
Release Failed Stage Runtime
    -> Keep Current Index
    -> Clear Prior Result State
    -> Prepare Current Stage
```

Accept Return To Main Menu only from StageVictory or StageDefeat:

```text
Release Current Stage Runtime
    -> Clear Current Run Index
    -> Clear Prior Result State
    -> Enter MainMenu
```

Retry always executes the full Task002 preparation transaction. It cannot reuse Player, Map, Wave, Draft, Tower, Monster, or Battle-result state.

## 15. Intent API For Presentation

Expose narrow semantic intent operations for Task004:

- Request Start New Run
- Request Confirm Stage Introduction
- Request Continue After Victory
- Request Retry Current Stage
- Request Return To Main Menu

Each request validates the current GameFlowState internally.

Do not expose setters for:

- Current state
- Current index
- Current Stage
- Last result
- Transition guard

Expose read-only information needed by Task004:

- Current state
- Current StageDefinition
- Current Stage index
- Whether another Stage follows the current Stage
- Last BattleResult while in a result state

## 16. Transition And Reentrancy Contract

Use one transition-in-progress guard.

For every accepted intent:

1. Validate current state and required references.
2. Establish the guard before calling Stage or Battle owners.
3. Commit index and result changes in the documented order.
4. Publish the new state only after that state is valid.
5. Clear the guard in a guaranteed completion boundary.

Nested UI intent, Battle result callback, disable callback, or preparation failure must not:

- Increment the index twice
- Prepare two Maps
- Begin the same Stage twice
- Retry after returning to Main Menu
- Replace a Victory state with Defeat
- Publish a state whose required runtime is absent

## 17. Disable And Cleanup

When GameFlowController is disabled or destroyed:

- Unsubscribe from Battle results.
- Close or confirm closed Battle authority.
- Release the currently composed Stage runtime.
- Clear active run state.
- Do not publish Victory or Defeat.

Re-enabling must not duplicate subscriptions or resume an old Battle implicitly.

## 18. Out Of Scope

- Main menu, Introduction, Victory, or Defeat UI components
- Button binding and UI animation
- Dynamic UI loading
- Stage unlock rules
- Save data or persistent Stage progress
- Stage Selection UI
- Chapters or branching sequences
- Pause and resume
- Scene transitions
- Asynchronous loading
- Changes to BattleResult rules from Task001
- Changes to Stage preparation rules from Task002
- Generic state-machine framework, event bus, service locator, or dependency-injection container

## 19. Acceptance Criteria

- One GameFlowController owns the Demo Stage sequence and current index.
- One explicit GameFlowState is the state authority.
- Application entry establishes MainMenu with no active Stage Battle.
- Start New Run selects index zero.
- Initial Stage, next Stage, and retry share one preparation path.
- Stage Introduction confirmation begins the already prepared Stage.
- A Stage without presentable Introduction content begins directly.
- BattleResult is accepted only during Battle.
- Victory enters StageVictory; Defeat enters StageDefeat.
- Continue after non-final Victory increments the index exactly once.
- Continue after final Victory returns to MainMenu.
- Retry keeps the current index and establishes fresh Stage and Player state.
- Return from Victory or Defeat clears the current run and returns to MainMenu.
- Starting again after MainMenu begins from index zero.
- Duplicate or late intents cannot cause duplicate composition, begin, result, or release.
- Preparation or begin failure returns to a safe MainMenu state with no partial runtime.
- No concrete UI reference is required by GameFlowController.

## 20. Static Validation

Run at minimum:

- Main Unity assembly compilation
- `git diff --check`
- Search for Game Flow state writes outside GameFlowController
- Search for direct Player state mutation from GameFlowController
- Search for Stage index ownership outside GameFlowController
- Search for duplicate Stage preparation entry paths
- Review subscription symmetry and transition guards
- Review all valid and invalid state/intent combinations

Static validation does not prove Unity lifecycle ordering or rapid-interaction behavior.

## 21. Unity Play Mode Handoff

Before Task004 UI is available, exercise semantic intent operations through controlled test or debug entry points:

- Application entry reaches MainMenu.
- Start prepares Stage zero.
- Configured Introduction reaches StageIntroduction without spawning.
- Confirmation begins Battle.
- No-Introduction Stage begins directly.
- Victory reaches StageVictory.
- Non-final continue advances one Stage.
- Final continue returns to MainMenu.
- Defeat reaches StageDefeat.
- Retry keeps index and resets Player health.
- Return clears the run.
- Rapid duplicate intents do not duplicate runtime state.
- Preparation and begin failures return safely to MainMenu.

Unless explicitly handed over, Codex owns runtime scripts and static checks; the user owns final serialized Stage sequence wiring and Unity Play Mode acceptance.
