# Task004 - Game Flow UI And Loop Integration

Status: Not started

Depends on: Task001, Task002, Task003

## 1. Goal

Implement the Game Flow presentation layer and connect it to Task003 so the Demo supports one complete interactive loop:

```text
Main Menu
    -> Stage Preparing
    -> Optional Stage Introduction
    -> Battle
    -> Victory Or Defeat
    -> Next Stage, Retry, Or Main Menu
```

UI must present Game Flow state and send semantic intent. It must not own Stage index, Battle outcome, Player state, Monster completion, or Stage composition.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_GameFlowSystem.md`
- `Doc/02_StageSystem.md`
- `Doc/04_BattleHUDUISystem.md`
- `Doc/Task/Task001_BattleResultAuthority.md`
- `Doc/Task/Task002_StagePreparationAndIntroduction.md`
- `Doc/Task/Task003_GameFlowRuntime.md`

## 3. Pre-Implementation State

- Task001 provides one authoritative Victory or Defeat result.
- Task002 provides a prepared Stage and optional Introduction data.
- Task003 provides the complete UI-independent Game Flow state machine and semantic intent API.
- The existing Canvas contains Battle HUD, Monster status, and damage-number presentation.
- There is no main menu, Stage Introduction window, Victory window, Defeat window, or Game Flow presentation root.
- Game Flow views are fixed Demo surfaces; only Introduction content items vary per Stage.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| GameFlowController | Flow state, current Stage, current index, result state, and accepted intents |
| GameFlowUIRoot | Observe Game Flow state and make exactly the matching presentation interactive |
| MainMenuView | Present start interaction and its local visual feedback |
| StageIntroductionView | Present current Stage Introduction content and confirmation intent |
| StageIntroductionUIItem | Present one referenced Tower or Tower Upgrade |
| StageVictoryWindowView | Present Victory and continue intent |
| StageDefeatWindowView | Present Defeat, retry intent, and return intent |
| BattleUIRoot | Continue to group battle-local HUD, Monster status, and damage-number presentation |

No gameplay system receives a generic UI manager dependency.

UI views never call StageCompositionController, BattleRuntimeCoordinator, PlayerSystem, MonsterManager, or MonsterSpawner directly.

## 5. Authored UI Composition

The first-version authored composition is:

```text
Canvas
    -> Battle Layer
        -> Battle HUD
        -> Monster Status Presentation
        -> Damage Number Presentation
    -> Game Flow Layer
        -> Main Menu View
        -> Stage Introduction View
        -> Stage Victory Window View
        -> Stage Defeat Window View
```

The exact hierarchy and visual styling remain user-authored, but the composition contract is:

- Fixed Game Flow views exist as authored references.
- Fixed views are not loaded dynamically at runtime.
- Only Stage Introduction UI items are created dynamically.
- Game Flow views render above battle-local interaction when active.
- Modal Introduction and result views prevent input from reaching Battle UI beneath them.

Do not add a generic UIManager, addressable loading layer, service locator, or window stack for this Demo scope.

## 6. GameFlowUIRoot

Create one `GameFlowUIRoot` with stable references to:

- GameFlowController
- MainMenuView
- StageIntroductionView
- StageVictoryWindowView
- StageDefeatWindowView

It observes Task003 state changes and applies one presentation state:

| GameFlowState | Active Game Flow Presentation |
|---|---|
| MainMenu | MainMenuView |
| StagePreparing | None; interaction remains blocked until preparation resolves |
| StageIntroduction | StageIntroductionView |
| Battle | None |
| StageVictory | StageVictoryWindowView |
| StageDefeat | StageDefeatWindowView |

Before activating a new view:

- Disable or hide every incompatible Game Flow view.
- Clear stale transient Introduction items when leaving Introduction.
- Configure the target view from current read-only Game Flow data.
- Enable target interaction only after configuration completes.

GameFlowUIRoot forwards view intent into the matching GameFlowController request. It does not duplicate state validation.

## 7. Main Menu View

The Main Menu is one full-screen interaction surface containing:

- One Image filling the Canvas
- One player-facing start message: `点击任意位置游戏开始`
- One interaction covering the full Image

The start message performs a slow repeating alpha pulse.

Presentation requirements:

- The full-screen Image is the interaction target.
- Decorative text must not intercept pointer input.
- The view emits one Start intent.
- Disable interaction immediately after the accepted click or while leaving MainMenu.
- Repeated pointer input cannot request multiple runs.
- Starting or stopping the pulse is owned by MainMenuView.
- Disabling the view stops its active tween and restores the authored initial alpha.
- Re-entering MainMenu creates at most one pulse tween.

Use the project's existing tween dependency rather than implementing a new animation framework.

## 8. Stage Introduction View

The Stage Introduction contains:

- Authored static title presentation
- Introduced Tower section and layout container
- Introduced Tower Upgrade section and layout container
- Confirm interaction

On entry:

```text
Receive Current StageDefinition
    -> Clear Prior Runtime Items
    -> Read Introduced Towers
    -> Create One Item Per Presentable Tower
    -> Read Introduced Tower Upgrades
    -> Create One Item Per Presentable Upgrade
    -> Hide Empty Section Roots
    -> Enable Confirm Interaction
```

The view must not compare the current Stage with the previous Stage.

The view must not read the full Draft pools as implied Introduction content.

Confirm emits one semantic confirmation intent. It does not begin Stage composition directly.

## 9. Stage Introduction UI Item

Create one reusable `StageIntroductionUIItem` presentation component containing references for:

- Name text
- Description text
- Icon image

Use the same item type for introduced Towers and introduced Tower Upgrades because both provide the same presentation data.

Initialization receives only resolved presentation values or one narrow presentation data value:

```text
Display Name
Description
Icon
```

Display-name fallback follows the existing definition presentation convention: use the authored DisplayName when present, otherwise use the referenced definition's asset name.

The item:

- Has no Button behavior.
- Does not create DraftResult.
- Does not own Tower or Upgrade eligibility.
- Does not use TowerDraftUIItem or PendingDraftUIItem.
- Does not write back to StageDefinition.

## 10. Introduction Item Cleanup

StageIntroductionView tracks only the runtime items it creates.

Cleanup occurs:

- Before populating another Stage
- When leaving StageIntroduction
- When the view is disabled or destroyed
- During Game Flow UI teardown

Cleanup must:

- Destroy or release only tracked Introduction items.
- Clear the tracking collection.
- Reset section visibility.
- Prevent stale Stage content from reappearing on next Stage or retry.

Do not delete authored container children or unrelated UI objects.

## 11. Stage Victory Window

Create a distinct `StageVictoryWindowView`.

It contains:

- Authored static Victory text
- One Continue button
- One Continue button label

When another Stage exists:

- Button label is `下一关`.
- Clicking emits Continue After Victory.

When the completed Stage is final:

- Button label is `返回主界面`.
- Clicking still emits the same Continue After Victory intent.
- Task003 decides that final Victory returns to MainMenu.

The view may read the GameFlowController `HasNextStage` value for presentation. It must not increment the Stage index or select the next Stage itself.

## 12. Stage Defeat Window

Create a distinct `StageDefeatWindowView`.

It contains:

- Authored static Defeat text
- Retry button
- Return-to-main-menu button

Retry emits the Task003 Retry intent.

Return emits the Task003 Return To Main Menu intent.

The view does not reset Player health or recompose the Stage directly.

## 13. Button And Subscription Lifecycle

- Bind each authored Button exactly once per active subscription lifecycle.
- Remove listeners symmetrically.
- Do not add listeners every time a view is shown without removing the prior binding.
- Disable relevant buttons immediately after forwarding an accepted intent.
- Re-enable buttons only when the corresponding view is configured for a fresh valid state.
- A hidden or inactive view cannot forward intent.
- Re-enabling GameFlowUIRoot must reconcile to the current GameFlowState without duplicating listeners or transient items.

## 14. Battle UI Boundary

Battle HUD UI remains battle-local.

Game Flow UI:

- May share one Canvas with Battle UI.
- May visually cover Battle UI.
- May block Battle UI input while modal.
- Does not become the owner of Battle HUD, Monster status, or damage-number state.

Battle UI:

- Does not open or close Game Flow windows.
- Does not decide Victory or Defeat.
- Does not advance, retry, or select Stages.

During StageVictory and StageDefeat, Battle authority has already closed through Task001 before the result window becomes interactive.

## 15. Flow Integration

The final integrated flow is:

```text
Application Entry
    -> GameFlowController Enters MainMenu
    -> GameFlowUIRoot Shows MainMenuView
    -> Player Clicks Full-Screen Start
    -> Task003 Prepares Stage Zero
    -> Optional Introduction Is Populated
    -> Confirm Begins Prepared Battle
    -> Task001 Publishes Victory Or Defeat After Stop
    -> GameFlowController Enters Result State
    -> GameFlowUIRoot Shows Matching Result Window
    -> Player Continues, Retries, Or Returns
```

At no point may UI visibility become the source of truth for Game Flow state.

## 16. Validation

Report at minimum:

- Missing GameFlowController reference
- Missing required Game Flow view
- Missing required Button, label, section, container, or item prefab reference
- Introduction item prefab missing StageIntroductionUIItem
- Introduction item presentation references missing
- Fixed Game Flow view configured for runtime dynamic loading
- Multiple active modal Game Flow views
- Active modal view not blocking Battle UI input
- Duplicate Button subscriptions
- Stale Introduction items after Stage change or retry
- Victory label inconsistent with `HasNextStage`
- Hidden view still accepting input

Validation reports authoring problems without creating substitute UI, choosing a Stage, or starting Battle.

## 17. Out Of Scope

- Generic UIManager
- Generic modal/window framework
- Runtime loading of fixed Game Flow windows
- Addressables or asset bundles
- Scene transitions
- Pause menu
- Settings menu
- Stage Selection UI
- Save/load UI
- Localization system
- Controller/keyboard navigation beyond existing project input scope
- Audio, cinematic, transition, or loading-screen systems
- New BattleResult, Stage preparation, or Game Flow rules
- Reworking Battle HUD, Monster status, or damage-number behavior

## 18. Acceptance Criteria

- Main Menu fills the Canvas and accepts one click anywhere on its Image.
- Main Menu start text pulses slowly and does not block pointer input.
- Main Menu tween stops and resets cleanly when hidden.
- Start intent begins one run at Stage index zero.
- A configured Stage Introduction displays the current Stage's explicit introduced Towers and Upgrades.
- Empty Tower or Upgrade sections are hidden.
- Introduction items show name, description, and icon.
- Confirm starts the already prepared Stage exactly once.
- A Stage without presentable Introduction content begins without an empty window.
- Victory displays only StageVictoryWindowView.
- Non-final Victory label is `下一关`.
- Final Victory label is `返回主界面`.
- Defeat displays only StageDefeatWindowView.
- Retry prepares the same Stage with fresh Player health.
- Return releases current Stage runtime and shows MainMenuView.
- Starting again from MainMenu begins at index zero.
- Modal views block Battle UI interaction beneath them.
- Fixed views are authored references; only Introduction items are dynamically instantiated.
- Rapid repeated clicks do not duplicate Stage preparation, begin, continue, retry, or return.
- No prior Stage Introduction items or button listeners leak into another Stage or retry.

## 19. Static Validation

Run at minimum:

- Main Unity assembly compilation
- `git diff --check`
- Search for UI references inside GameFlowController
- Search for Game Flow views directly calling Stage, Player, Monster, or Battle owners
- Search for fixed Game Flow view runtime instantiation
- Search for duplicate listener registration patterns
- Review tween stop/reset behavior
- Review transient Introduction item ownership and cleanup
- Review GameFlowState-to-view mapping completeness

Static validation does not prove Canvas ordering, pointer blocking, serialized Button references, visual layout, or Play Mode flow.

## 20. Unity Authoring Checklist

User authoring:

- Create MainMenuView prefab and full-screen Image interaction.
- Create StageIntroductionView prefab with authored title, two sections, containers, and Confirm button.
- Create StageIntroductionUIItem prefab with name, description, and icon presentation.
- Create separate StageVictoryWindowView and StageDefeatWindowView prefabs.
- Place fixed Game Flow prefab instances under the Canvas Game Flow layer.
- Assign GameFlowUIRoot references.
- Configure Canvas ordering and modal raycast blocking.
- Assign the ordered Demo StageDefinition list.
- Author at least five StageDefinitions and their Introduction content.
- Keep EventSystem and the project's current UI input module active.

Do not duplicate placeholder Stage content only to satisfy the target count.

## 21. Unity Play Mode Handoff

Validate the complete loop:

- Application opens at Main Menu with no active Battle.
- Click anywhere on Main Menu starts exactly one run.
- Stage zero resets Player current and maximum health from its StageDefinition.
- Introduction Stage shows correct Tower and Upgrade items.
- No-Introduction Stage starts directly.
- No Monsters spawn before Introduction confirmation.
- Victory appears only after all spawning completes and no Monsters remain.
- Final Target arrival at one Player health shows Defeat only.
- Non-final Victory advances to the next Stage and resets all Stage-local runtime.
- Final Victory returns to Main Menu.
- Defeat retry keeps the same Stage index and resets Player health.
- Defeat return releases the Stage and returns to Main Menu.
- Starting again after return begins from Stage zero.
- Rapid double-clicks on every flow button remain single-transition.
- No stale Map, Monster, Tower, Attack Entity, Draft item, status UI, damage number, Introduction item, or tween survives into an invalid flow state.

Unless explicitly handed over, Codex owns scripts and static checks; the user owns UI prefab creation, Canvas and GameManager wiring, Stage content authoring, visual acceptance, and Unity Play Mode acceptance.
