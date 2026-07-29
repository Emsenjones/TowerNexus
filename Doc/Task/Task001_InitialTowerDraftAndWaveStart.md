# Task001 - Initial Tower Draft And Wave Start

Status: Runtime implemented; Unity authoring and Play Mode acceptance pending

Depends on: Existing Stage preparation, Battle runtime, Draft, Battle HUD UI, Game Flow, and Monster Wave runtime foundations

## 1. Goal

Implement exactly one Initial Tower Draft at the start of every fresh Stage battle.

After Game Flow permits the prepared Stage to enter Battle, the player selects one Stage-allowed TowerDefinition and receives one committed held Tower Draft item before the first Monster Wave Delay begins.

Every successfully opened Initial or Player level-up Draft Window pauses gameplay simulation while remaining fully interactive. This prevents ordinary Monster resolution from producing another level-up opportunity while the current Draft is unresolved.

The Initial Draft does not grant Player progress, trigger Player level-up, or require the selected Tower to be deployed before Wave timing begins.

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

- `BattleRuntimeCoordinator.BeginPreparedBattleCore()` opens the Player, Monster, Placement, Draft, and Spawner battle gates and immediately calls `MonsterSpawner.StartSpawning()`.
- `DraftSystem` opens a Draft only in response to `PlayerSystem.OnLevelUp`.
- `DraftSystem.OpenTowerDraft()` combines Tower and currently eligible Tower Upgrade candidates.
- Draft selection asks `BattleHUDUI` to create a held `PendingDraftUIItem`, but that creation has no success result.
- `DraftUI` closes after forwarding a valid selection regardless of whether the held item was created.
- `DraftUI` hard-caps presentation at three items and warns when fewer than three are shown.
- Draft opening, held-item creation, and complete UI capability are not reusable preflight contracts.
- Draft attempts have no Battle-generation identity and stale callbacks are not distinguishable across retries.
- Draft Window visibility has no gameplay-simulation pause ownership.
- `PendingDraftUIItem` discovers a root Canvas through its parent hierarchy even though `BattleHUDUI` already owns the pending-item container used to instantiate it.
- Battle HUD has no dedicated authored root for the free-moving Pending Draft drag visual.
- Draft completion has no durable attempt-scoped record separate from active selection authority.
- `BattleRuntimeCoordinator` exposes semantic Victory or Defeat but no result-neutral asynchronous runtime-failure event for Game Flow.
- Fresh Player state begins at Level 1 and zero Progress.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| BattleRuntimeCoordinator | Order Battle gate opening, Initial Draft request, first Wave authorization, and one shared terminal claim across Victory, Defeat, and Technical Failure |
| DraftSystem | Own Draft workflow phase and session identity, Initial/level-up source rules, candidate generation, pause lifetime, selection validation, and the committed Initial Draft record |
| BattleHUDUI | Own the authored pending-item container and drag-visual root, validate both roots, atomically create and register held items, and report success or failure |
| DraftUI | Present supplied candidates, remain interactive while simulation is paused, block battlefield input, and emit selection intent |
| PendingDraftUIItem | Validate and complete its own presentation and interaction initialization before registration |
| MonsterSpawner | Keep Wave execution stopped until Initial Draft completion authorization |
| PlayerSystem | Preserve normal Level 1 and zero-Progress fresh state and publish later level-up opportunities |
| GameFlowController | Accept one current result-neutral asynchronous runtime failure, release the Stage, and return directly to Main Menu |

Draft UI does not own gameplay pause. Game Flow does not open Drafts, start Waves, or manufacture Victory or Defeat for technical failure.

## 5. Battle And Draft Attempt Identity

Add one immutable attempt token containing:

```text
DraftAttemptToken
    BattleGeneration
    AttemptIdentity
```

Rules:

- `BattleGeneration` changes for every fresh Battle, including retry and next Stage.
- `AttemptIdentity` changes for every Initial or level-up Draft attempt.
- The monotonic counters backing these values never reset in `BeginBattle()`.
- Draft session kind and phase reset for each fresh Battle.
- Stop, release, retry, replacement, and disable invalidate the active token.
- Completion and asynchronous failure callbacks carry the exact token.
- Draft System and BattleRuntimeCoordinator both compare complete token equality.
- A callback from a previous Battle cannot collide with a current attempt even if it references the same TowerDefinition.

Every Initial or level-up Draft session uses:

```text
DraftSessionKind
    None
    Initial
    LevelUp

DraftSessionPhase
    None
    Opening
    AwaitingSelection
    CommittingSelection
    Completed
    Failed
    Cancelled
```

The token is created and recorded provisionally before it is passed to any UI callback:

```text
Create Token
    -> Enter Opening
    -> Try Open Window
    -> Acquire Attempt-Owned Pause
    -> Enter AwaitingSelection
```

Opening failure rolls the provisional session back to `None` synchronously and publishes no asynchronous failure event. At most one Draft session and one Draft-owned pause may be active.

Keep active authority separate from committed Initial Draft evidence:

```text
Active Draft Session
    activeToken
    sessionKind
    sessionPhase
    displayedIdentities
    pause ownership

Committed Initial Draft Record
    completedInitialToken
    committedInitialHeldItem
```

Invalidating active selection authority does not erase the committed record.

## 6. Complete Preflight

Before Battle consumer gates open, validate the complete capabilities required by both Initial and later level-up Drafts.

### 6.1 Draft System

- `draftChoiceCount` is positive.
- Stage Tower and Tower Upgrade pools are bound.
- At least one distinct, owner-valid TowerDefinition exists for the Initial Draft.
- Required Player, Tower Upgrade, Tower Placement, and Battle HUD references exist.

### 6.2 Draft Window

- Draft UI reference exists.
- Draft root and choice container exist.
- An explicit modal raycast surface exists and blocks battlefield pointer input.
- Draft choice prefab exists and has one root `TowerContentUIItem`.
- `TowerContentUIItem.TryValidateReferences()` succeeds for the prefab contract.
- Presentation can remain interactive while gameplay simulation is paused.

### 6.3 Held Item

- Existing serialized `pendingDraftContainer` is a `RectTransform` inside the current Battle HUD Canvas hierarchy.
- Serialized `pendingDraftDragVisualRoot` is a distinct `RectTransform` inside the Battle HUD presentation root.
- Pending item prefab exists and is instantiated directly under `pendingDraftContainer`.
- Both roots use the same UI Canvas space.
- `pendingDraftDragVisualRoot` has no `HorizontalLayoutGroup`, `VerticalLayoutGroup`, `GridLayoutGroup`, or `ContentSizeFitter`.
- `pendingDraftDragVisualRoot` is not under a `Mask` or `RectMask2D` that clips battlefield dragging.
- `pendingDraftDragVisualRoot` covers the required drag coordinate space and can render the dragged item above ordinary Battle HUD content.
- The drag-visual root has no raycastable `Graphic` and introduces no additional input-blocking surface.
- Pending prefab has one root `PendingDraftUIItem`.
- `PendingDraftUIItem.TryValidateReferences()` validates icon, name, category backgrounds, its own RectTransform, and every required drag reference.
- TowerPlacementController is assigned.

`BattleHUDUI.OnEnable()` must enter a safe closed presentation state without throwing when DraftUI is missing. Preflight, not an early null-reference exception, reports the missing capability.

Known configuration failures must be discovered before asynchronous player selection whenever possible.

## 7. Battle-Start Sequence

`BattleRuntimeCoordinator.BeginPreparedBattleCore()` becomes:

```text
Validate Prepared Battle
    -> Open All Battle Consumer Gates
    -> Verify Every Gate Is Open
    -> Create Provisional Initial Draft Token In Opening
    -> Request One Initial Tower Draft With That Token
    -> Draft Window Opens With At Least One Selectable Choice
    -> DraftSystem Acquires Its Attempt-Owned Pause
    -> DraftSystem Enters AwaitingSelection
    -> Revalidate Battle Authority, Gates, Release State, Token, And Window
    -> Consume Prepared-State Markers
    -> Return A Successfully Begun Battle
    -> Wait For Player Selection
```

`MonsterSpawner.StartSpawning()` is removed from the synchronous Battle-start transaction.

Before returning success after Draft opening, the coordinator verifies:

- Battle authority remains active.
- Every consumer gate remains open.
- No deferred release was requested.
- DraftSystem is awaiting the exact returned attempt token.
- Draft Window remains open with selectable choices.
- The exact attempt owns the active Draft pause.

Failure before `TryOpenInitialTowerDraft(...)` returns is synchronous:

- Return `false` with a concrete failure reason.
- Publish no Draft failed event.
- Let existing Battle-begin rollback close opened gates and clean Stage runtime.

Successfully opening and revalidating the Initial Draft is enough for Battle begin to return success.

## 8. Candidate Generation

Use separate Draft sources while retaining shared sampling and distinct-identity logic.

### 8.1 Initial Tower Draft

- Add candidates only from the active Stage Tower Draft Pool.
- Do not add TowerUpgradeDefinition candidates.
- Do not change Player level or progress.
- Sample up to `draftChoiceCount`.
- One, two, and three displayed choices are valid according to available distinct content.
- No valid candidate is a synchronous Battle-start failure.

### 8.2 Player Level-Up Draft

- Merge Stage Tower candidates with currently eligible Tower Upgrade candidates.
- Preserve existing Tower-instance weighting and pending Upgrade reservation.
- Do not let pending Tower Draft items reduce Tower candidate capacity.

DraftUI presents every supplied sampled choice and does not impose its own hard-coded count.

## 9. Draft Window Pause Contract

`DraftSystem`, not `DraftUI`, owns the Unity time-scale lease.

After a Draft Window opens successfully:

```text
Capture Current Time.timeScale
    -> Record Exact Owning DraftAttemptToken
    -> Set Time.timeScale = 0
    -> Keep Draft Presentation And Input Interactive
```

Rules:

- Do not assume the prior value was `1`.
- Restore the captured value exactly.
- While a Draft pause lease is active, `DraftSystem` is the only permitted writer of `Time.timeScale`.
- Another pause or slow-motion owner must not change `Time.timeScale` until the Draft lease is released.
- Release only when the current token owns the pause.
- Never restore another system's or another Draft attempt's pause.
- Synchronous opening failure acquires no pause.
- Successful selection releases pause before publishing completion.
- Asynchronous technical failure releases pause before publishing failure.
- Stop, release, retry, replacement, and disable release pause as cancellation and publish neither completion nor technical failure.
- Repeated cleanup is idempotent.
- Draft UI animation or timing required for interaction uses unscaled time.
- Modal pointer blocking remains mandatory because time-scale pause does not stop input polling.

For the Initial Draft, pause is released before completion authorizes the first Wave Delay.

Slow motion while dragging a PendingDraftItem is deferred. It will have a separate ownership lifetime and is not implemented in Task001.

The attempt token prevents another Draft from releasing the lease; it cannot protect against an unrelated time owner. If another runtime time owner is introduced, replace the direct lease with a shared pause/time-control service rather than layering another writer onto `Time.timeScale`.

## 10. Atomic Held Item Commit

Keep the existing serialized `pendingDraftContainer` and change its declared type from `Transform` to `RectTransform`. Add one distinct serialized `RectTransform pendingDraftDragVisualRoot`. Do not add a Canvas reference.

`BattleHUDUI.TryAddPendingDraft(...)` instantiates the prefab directly under `pendingDraftContainer`, returns the committed item or an explicit failure result, and passes both authored roots explicitly into:

```text
PendingDraftUIItem.TryInitialize(
    DraftResult,
    TowerPlacementController,
    RectTransform pendingItemContainer,
    RectTransform dragVisualRoot,
    out failureReason)
```

The transaction is:

```text
Instantiate Pending Item
    -> Resolve PendingDraftUIItem
    -> Validate Complete References
    -> Verify Direct Parent Is The Supplied Pending-Item Container
    -> Initialize Draft Result, Placement Owner, Container, And Drag-Visual Root
    -> Enable Battle Interaction
    -> Register In PendingDraftItems
    -> Return Committed Item
```

Until registration succeeds, the item is provisional.

Failure:

- Disables and destroys the partial object.
- Leaves `PendingDraftItems` unchanged.
- Does not consume the selected Draft identity.
- Does not report Initial Draft completion.

Coordinator must not infer Initial Draft completion from general pending-item count. DraftSystem records the exact committed Initial held item against the exact attempt token.

`PendingDraftUIItem`:

- Caches only its own `RectTransform` in `Awake()`.
- Validates the supplied container and drag-visual root during `TryInitialize(...)`.
- Verifies that it was instantiated directly under the supplied container.
- Stores its original parent and sibling index when an Upgrade Draft drag begins.
- Reparents to `pendingDraftDragVisualRoot`, becomes its last sibling, and converts pointer position in that root's coordinate space.
- On end, cancellation, stop, or release, reparents to the original container and restores its sibling index.
- Lets the original `HorizontalLayoutGroup` rebuild position after return; it does not manually restore an authored anchored position when the original parent owns layout.
- Performs no `GetComponentInParent<Canvas>()`, parent-hierarchy Canvas lookup, root-Canvas cache, or fallback root lookup.

Both pending-item roots remain Battle HUD presentation ownership. They do not move into DraftSystem, TowerPlacementController, or a revived `BattleUIRoot` service component.

## 11. Selection Transaction

For one valid selection:

```text
Receive Selection Intent With Attempt Token
    -> Verify Battle Active
    -> Verify Current Session And Exact Token
    -> Verify Displayed Identity
    -> Atomically Transition AwaitingSelection To CommittingSelection
    -> Try Commit Held Item
    -> Success: Record Completed Initial Token And Exact Item When Applicable
    -> Success: Establish Completed
    -> Failure: Establish Failed
    -> Invalidate Active Attempt
    -> Close And Clear Draft Window
    -> Release Attempt-Owned Pause
    -> Publish Completion Or Failure
```

The `CommittingSelection` guard is established before `TryAddPendingDraft(...)`, including before `Instantiate`, `Awake`, `OnEnable`, or initialization can re-enter. Duplicate callbacks therefore cannot enter a second held-item creation transaction.

For Initial Draft success, write `completedInitialToken` and `committedInitialHeldItem` before invalidating active selection authority. Active invalidation clears the active token, kind, displayed identities, and selection callback authority, but not the completed Initial record.

Invalid, duplicate, stale, or mismatched selection does not close the current valid Draft, create an item, release its pause, or publish completion.

## 12. Synchronous And Asynchronous Failure

### 12.1 Synchronous Opening Failure

Failure before the Initial Draft open operation returns:

- Rolls provisional `Opening` state back to `None`.
- Returns `false` with a failure reason.
- Publishes no `OnInitialDraftFailed`.
- Uses existing `BeginPreparedBattle()` rollback.

### 12.2 Asynchronous Draft Failure

Failure after the Draft Window has successfully opened:

```text
Establish Failed From The Current Attempt Phase
    -> Invalidate Attempt
    -> Close And Clear Draft Window
    -> Release Attempt-Owned Pause
    -> Publish OnInitialDraftFailed(attemptToken, reason) Exactly Once
```

Held-item initialization failure uses this path.

### 12.3 Cancellation

Stop, release, retry, replacement, and disable:

- Establish `Cancelled` when an active session exists.
- Invalidate the active attempt.
- Close and clear Draft presentation.
- Release the owned pause.
- Publish neither completion nor technical failure.

## 13. Wave Authorization

BattleRuntimeCoordinator stores the expected Initial Draft token and exact completion guard.

On `OnInitialDraftCompleted(token)`:

1. Verify Battle is active.
2. Verify `BattleTerminalState == None`.
3. Verify token equals the expected Initial token.
4. Call `TryConfirmCommittedInitialDraft(token, out committedItem)`.
5. Verify the returned exact Initial held item remains valid.
6. Set `hasInitialDraftAuthorizedSpawning` before calling the spawner.
7. Call `MonsterSpawner.StartSpawning()` exactly once.

`TryConfirmCommittedInitialDraft(...)` validates only the preserved committed Initial record. It does not require the token to remain the active session token.

The restored pre-Draft time scale is already active when the first Wave Delay begins.

`StartSpawning()` may both return `false` and synchronously publish a failure callback. Both reports converge through one guarded `TryClaimBattleTerminalState(TechnicalFailure)` transaction.

## 14. Level-Up Draft Overlap

Gameplay pause prevents ordinary later Monster simulation and Player progress while a Draft Window is open, so Task001 does not add a Draft queue.

`DebugAddProgress()` or a future batch-progression transaction may still publish multiple `OnLevelUp` callbacks synchronously. Only the first callback may establish the active Draft session. Later same-transaction callbacks are rejected and logged as unsupported overlap. They cannot replace the active Draft, create a reward, close its window, or release its pause.

Task001 does not guarantee one Draft reward per callback emitted by multi-level batch progression. If that behavior becomes required, implement Draft queueing as a separate task.

Rapid duplicate selection and late callbacks from a stopped, replaced, retried, or previous-Stage attempt remain rejected by token and phase guards.

## 15. Result-Neutral Battle Runtime Failure

Add one authoritative terminal value:

```text
BattleTerminalState
    None
    Victory
    Defeat
    TechnicalFailure
```

Victory, Defeat, and Technical Failure all enter through the same guarded `TryClaimBattleTerminalState(...)`. Competing independent booleans must not authorize separate terminal paths. Lifecycle cancellation through stop or release does not claim a terminal state.

Add:

```text
BattleRuntimeCoordinator.OnBattleRuntimeFailed(reason)
```

Contract:

- Synchronous failure before `BeginPreparedBattle()` returns uses `false` and existing rollback; it publishes no event.
- Asynchronous failure after begin success first claims `TechnicalFailure`.
- Coordinator closes Battle authority and consumer gates first.
- Coordinator stops runtime output.
- Coordinator performs technical cleanup once.
- Coordinator publishes one result-neutral failure as its final operation and returns immediately.
- Coordinator does not access its own state or any Stage object after event publication because Game Flow may synchronously release and destroy them.
- The failure publishes neither Victory nor Defeat.

GameFlowController subscribes symmetrically and accepts the event only from the exact current committed Battle.

Accepted failure:

```text
Release Current Stage
    -> Clear Current Run State
    -> Publish MainMenu
```

No technical-error window is added.

## 16. Lifecycle And Reentrancy

- Subscribe and unsubscribe completion, failure, spawning, and Game Flow facts symmetrically.
- Guards are established before cleanup or callback publication.
- Deferred release wins over begin success.
- A prior Battle token cannot affect a new run, next Stage, or retry.
- Stop and release are idempotent when Battle is already inactive.
- Draft cleanup cannot remove a newer session's item, callback, or pause.
- Semantic Battle result and result-neutral technical failure are mutually exclusive.
- Reset `BattleTerminalState`, expected Initial Draft token, Initial spawning-authorization guard, normal-spawning-completed state, and any separately retained runtime-failure publication state only during fresh Battle preparation or release.
- Clear `completedInitialToken` and `committedInitialHeldItem` only during fresh Battle reset or Stage cleanup, not during completion publication or active-session invalidation.

## 17. Out Of Scope

- Starting Player progress above zero
- Triggering a synthetic Player level-up
- Requiring Tower deployment before the first Wave Delay
- Changing Player progression requirements
- Changing later level-up Draft weighting or eligibility
- Draft reroll, skip, ban, or replacement rewards
- Slow motion while dragging PendingDraftItem
- Generic pause-stack or time-control framework
- Draft queueing for multi-level batch progression
- Technical-error presentation
- Camera Pan implementation
- New TowerDefinition content

## 18. Unity Authoring Checklist

- Confirm DraftSystem, BattleHUDUI, DraftUI, PendingDraftUIItem, MonsterSpawner, and Game Flow references remain assigned.
- Author one modal Draft raycast surface that blocks battlefield input.
- Confirm the Draft choice prefab passes `TowerContentUIItem.TryValidateReferences()`.
- Confirm the Pending Draft prefab passes `PendingDraftUIItem.TryValidateReferences()`.
- Keep the existing `BattleHUDUI.pendingDraftContainer` assigned to the intended child `RectTransform` inside the current HUD Canvas.
- Create and assign `BattleHUDUI.pendingDraftDragVisualRoot` as a distinct child `RectTransform` inside the Battle HUD presentation root.
- Confirm Pending Draft items are initially instantiated directly under `pendingDraftContainer`.
- Confirm both roots use the same Canvas space.
- Confirm the drag-visual root has no layout group, content-size fitter, clipping mask, or raycastable Graphic.
- Size and order the drag-visual root so it covers the battlefield drag area and renders the dragged item above ordinary HUD content.
- Do not add a Canvas reference or rely on parent-hierarchy/fallback Canvas discovery.
- Confirm Draft UI remains interactive at `Time.timeScale == 0`.
- Configure any Draft UI tween or timed animation to use unscaled time.
- Confirm every playable Stage has at least one distinct owner-valid TowerDefinition.
- Do not add placeholder TowerDefinitions merely to force three choices.

## 19. Acceptance Criteria

- Every initial Stage, next Stage, and retry receives a unique non-colliding Battle generation and Initial Draft attempt token.
- Every fresh Battle opens exactly one Initial Tower Draft after Battle start permission.
- One-, two-, and three-choice Initial Drafts are valid.
- Initial Draft contains Tower choices only.
- Player remains Level 1 with zero Progress before and after Initial Draft.
- Any open Initial or level-up Draft Window pauses gameplay simulation.
- Draft selection remains interactive while time scale is zero.
- Previous time scale restores exactly after selection, asynchronous failure, stop, release, retry, replacement, and disable.
- No first-Wave Delay or Monster spawning begins while Initial Draft is open.
- One accepted selection atomically commits exactly one held Tower Draft item.
- The committed item is instantiated directly under the authored `pendingDraftContainer`.
- Upgrade Draft drag temporarily reparents the item to `pendingDraftDragVisualRoot` and restores it to its original container and sibling index.
- Returning to a layout-owned container lets its layout rebuild position rather than manually restoring an authored anchored position.
- Selection enters `CommittingSelection` before any held-item object is created or initialized.
- Failed held-item initialization leaves no partial item and returns Game Flow to Main Menu.
- Initial completion releases pause before starting the first Wave Delay.
- Player may deploy the held Tower while the first Wave Delay advances.
- Repeated selection and same-transaction Level-Up callbacks cannot replace the active session, create duplicate rewards, close its window, or release its pause.
- Retry and next-Stage tokens reject old callbacks.
- Coordinator can confirm the committed Initial Draft after active selection authority has been invalidated.
- Fresh Battle reset and Stage cleanup clear the prior committed Initial Draft record.
- Synchronous and asynchronous reports of one spawning failure clean up once.
- Technical runtime failure publishes no Victory or Defeat and returns directly to Main Menu.
- Later normal Level-Up Drafts still combine Tower and eligible Upgrade candidates.

## 20. Static Validation

- Build `Assembly-CSharp.csproj` without restoring packages.
- Add `DraftAttemptToken.cs` and its Unity `.meta` file.
- Add an explicit `<Compile Include>` entry for `DraftAttemptToken.cs` in `Assembly-CSharp.csproj`.
- Confirm the CLI build compiles `DraftAttemptToken` without depending on later Unity project-file regeneration.
- Run path-scoped `git diff --check` for touched scripts and documents.
- Confirm `StartSpawning()` is reachable only through guarded Initial Draft completion.
- Confirm Initial Draft code does not mutate Player progress or invoke level-up.
- Confirm Initial Draft generation never adds Tower Upgrade candidates.
- Confirm all Draft completion/failure callbacks carry complete tokens.
- Confirm monotonic attempt counters are not reset by `BeginBattle()`.
- Confirm all pause acquisition paths have token-safe release paths.
- Confirm `Time.timeScale` is restored from captured state rather than hard-coded to `1`.
- Confirm no other runtime owner writes `Time.timeScale` while a Draft pause lease is active.
- Confirm `PendingDraftUIItem` performs no Canvas hierarchy, root-Canvas cache, or fallback root lookup.
- Confirm preflight validates both pending-item roots, their shared Canvas space, and drag-root layout, clipping, ordering, and raycast constraints.
- Confirm active-session invalidation does not clear the committed Initial Draft record.
- Confirm only fresh Battle reset or Stage cleanup clears the committed Initial Draft record.
- Confirm all result and technical-failure subscriptions are symmetric.
- Confirm `StartSpawning()` return failure and callback failure converge on one transaction.
- Confirm all Victory, Defeat, and Technical Failure paths use the same terminal-state claim.
- Confirm asynchronous runtime-failure publication is the coordinator's final operation.

## 21. Unity Play Mode Handoff

Verify:

- Initial Stage, next Stage, and retry Initial Draft behavior
- Retry token cannot collide with a late old callback
- One-, two-, and three-choice Initial Drafts
- Gameplay and Wave timing remain paused while any Draft is open
- Draft UI remains interactive at time scale zero
- Previous time scale restores after selection, failure, stop, release, and disable
- Held-item initialization failure leaves no partial item and returns to Main Menu
- Upgrade Draft drag renders under the dedicated drag-visual root and restores to the layout container on end, cancellation, stop, and release
- Drag-visual root neither clips the item nor blocks additional pointer input
- Reentrant selection during held-item construction is rejected by `CommittingSelection`
- Initial selection commits Held Item before first-Wave authorization
- Coordinator confirms Initial completion through the preserved attempt-scoped record after active authority invalidation
- Deployment during the first Wave Delay
- Synchronous and asynchronous `StartSpawning()` failure cleanup exactly once
- Repeated selection and same-transaction Level-Up callback rejection without affecting the active Draft
- Normal later Level-Up Draft candidate composition

Static validation does not prove Unity callback timing, prefab capability, time-scale behavior, UI interactivity, or Play Mode cleanup.
