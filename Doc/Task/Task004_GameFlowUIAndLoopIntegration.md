# Task004 - Game Flow UI And Loop Integration

Status: Runtime implementation complete; Unity Play Mode acceptance pending

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
- There is no main menu, Stage Introduction window, Stage Result window, or Game Flow presentation root.
- Game Flow views are fixed Demo surfaces; only Introduction content items vary per Stage.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| GameFlowController | Flow state, current Stage, current index, result state, and accepted intents |
| GameFlowUIRoot | Observe Game Flow state and make exactly the matching presentation interactive |
| MainMenuView | Present start interaction and its local visual feedback |
| StageIntroductionView | Present current Stage Introduction content and confirmation intent |
| TowerContentUIItem | Present one referenced Tower or Tower Upgrade in selectable Draft mode or read-only Introduction mode |
| StageResultWindowView | Present Victory or Defeat copy and the actions valid for that result |

No gameplay system receives a generic UI manager dependency.

UI views never call StageCompositionController, BattleRuntimeCoordinator, PlayerSystem, MonsterManager, or MonsterSpawner directly.

The existing authored Battle Layer continues to group battle-local HUD, Monster status, and damage-number presentation without becoming a runtime owner.

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
        -> Stage Result Window View
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
- StageResultWindowView

GameFlowUIRoot remains active for the complete application lifecycle. A state
with no fixed Game Flow presentation deactivates all three child views without
deactivating the integration root or removing its state subscription.

On `Awake`, GameFlowUIRoot hides and locks every referenced presentation
regardless of its authored active state. Every later `OnEnable` repeats that
safe reset before reference validation, subscription, and reconciliation.
Prefab activation state is therefore not a presentation authority; current
Game Flow state is the only runtime visibility authority.

It observes Task003 state changes and applies one presentation state:

| GameFlowState | Active Game Flow Presentation |
|---|---|
| MainMenu | MainMenuView |
| StagePreparing | None; interaction remains blocked until preparation resolves |
| StageIntroduction | StageIntroductionView |
| Battle | None |
| StageVictory | StageResultWindowView configured for Victory |
| StageDefeat | StageResultWindowView configured for Defeat |

Before activating a new view:

- Disable or hide every incompatible Game Flow view.
- Clear stale transient Introduction items when leaving Introduction.
- Configure the target view from current read-only Game Flow data.
- Enable target interaction only after configuration completes.

State reconciliation is a presentation exception boundary. If hiding,
Introduction population, target configuration, or activation throws:

- Catch and log the presentation failure.
- Hide and lock every fixed Game Flow view.
- Do not let the exception escape into GameFlowController's guarded transition.
- Do not mutate Game Flow state or manufacture another intent.

GameFlowUIRoot forwards view intent into the matching GameFlowController request. It does not duplicate state validation.

## 7. Main Menu View

The Main Menu is one full-screen interaction surface containing:

- One Image filling the Canvas
- One player-facing start message: `Tap anywhere to start`
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
- One unified introduced-item layout container
- Confirm interaction

On entry:

```text
Receive Current StageDefinition
    -> Clear Prior Runtime Items
    -> Read Introduced Towers
    -> Create One Read-Only Item Per Presentable Tower
    -> Read Introduced Tower Upgrades
    -> Append One Read-Only Item Per Presentable Upgrade
    -> Enable Confirm Interaction
```

Tower items are appended before Tower Upgrade items. The shared item's icon
background treatment distinguishes their categories without separate section
roots.

The view must not compare the current Stage with the previous Stage.

The view must not read the full Draft pools as implied Introduction content.

Confirm emits one semantic confirmation intent. It does not begin Stage composition directly.

The Confirm button keeps Unity Button as the sole click and interactable
authority. A separate reusable `ButtonPressFeedback` component swaps its
authored normal/pressed sprites and offsets only a child content root while a
valid pointer press is held. The Button Transition is `None`, so built-in
transition visuals do not compete with this feedback.

## 9. Reusable Tower Content UI Item

Use one reusable `TowerContentUIItem` presentation component containing references for:

- Name text
- Description text
- Icon image
- Icon background image and authored idle/pressed sprite pair for every category
- One child content root for pressed-position feedback

`TowerContentUIItem` is a pointer-driven presentation component, not a Button.
The prefab does not add Button navigation, Transition, target-Graphic, or
click-event configuration. Its icon background receives the item UI raycasts,
and the component owns idle/pressed sprite changes, content-root offset, and
selection publication directly.

Initialization assigns both the idle and pressed background sprites for Tower,
Basic Upgrade, Behaviour Upgrade, or Elemental Upgrade content. Category
presentation uses authored sprites rather than runtime color tinting.

The same authored item prefab is shared by Draft selection and Stage
Introduction. It has two explicit initialization modes:

- Selectable mode retains exactly one Draft selection callback and accepts
  pointer press and click handling.
- Read-only mode removes any prior selection callback, ignores pointer
  interaction, and restores idle press feedback.

Both modes receive one valid Tower or Tower Upgrade content value and present
the same display name, description, icon, and category background. Display-name
fallback follows the existing definition presentation convention: use the
authored DisplayName when present, otherwise use the referenced definition's
asset name.

The item:

- Does not own Tower or Upgrade eligibility.
- Does not infer whether it should be selectable from a nullable callback.
- Does not retain a selection callback after disable or read-only
  initialization.
- Applies a small authored offset only to its pressed-content child root, never
  to the layout-controlled item root.
- Restores the content root to its captured idle position on release, pointer
  exit, disable, and read-only initialization.
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
- Prevent stale Stage content from reappearing on next Stage or retry.

Do not delete authored container children or unrelated UI objects.

## 11. Stage Result Window

Create one `StageResultWindowView` shared by StageVictory and StageDefeat.

It contains:

- One presentation `Root Object`
- One title text
- One description text
- One authored `Next Stage` button
- One authored `Retry` button
- One authored `Main Menu` button

The view component remains active while `Root Object` is the visibility
boundary for the complete result presentation. The title, description, and all
three buttons must be under that root.

The three buttons retain fixed semantic meanings. The view changes button
visibility, never a button's label or click meaning.

Before showing a result, the view disables interaction, hides every result
button, assigns the result copy, activates only the valid buttons, and then
enables interaction.

## 12. Result Presentation Modes

When another Stage exists, Victory presents:

- Title: `VICTORY`
- Description: `All enemy waves have been defeated.`
- `Next Stage` button only
- Clicking emits Continue After Victory

When the completed Stage is final, Victory presents:

- Title: `VICTORY`
- Description: `All enemy waves have been defeated.`
- `Main Menu` button only
- Clicking emits Return To Main Menu

Defeat presents:

- Title: `DEFEAT`
- Description: `The enemy broke through your defenses.`
- `Retry` and `Main Menu` buttons

Retry emits the Task003 Retry intent.

Main Menu emits the Task003 Return To Main Menu intent.

The view receives `HasNextStage` only to choose the valid Victory button set.
It does not increment the Stage index, reset Player health, select the next
Stage, or recompose a Stage directly.

## 13. Button And Subscription Lifecycle

- Bind each authored Button exactly once per active subscription lifecycle.
- Remove listeners symmetrically.
- Do not add listeners every time a view is shown without removing the prior binding.
- Disable relevant buttons immediately after forwarding an accepted intent.
- Re-enable buttons only when the corresponding view is configured for a fresh valid state.
- Keep click publication in Button listeners; `ButtonPressFeedback` never
  invokes the Button or publishes a semantic intent.
- Apply Confirm press feedback only while its Button is active and interactable.
  Pointer release, pointer exit, and component disable restore the normal
  sprite and the captured idle content position.
- A hidden or inactive view cannot forward intent.
- Re-enabling GameFlowUIRoot must reconcile to the current GameFlowState without duplicating listeners or transient items.
- StagePreparing and Battle hide all three fixed child views while GameFlowUIRoot remains active and subscribed.

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
- Missing required Button, label, unified container, or item prefab reference
- Confirm Button missing valid `ButtonPressFeedback`, authored sprite pair, or
  child pressed-content-root reference
- Introduction item prefab missing TowerContentUIItem
- Tower content item presentation, raycast target, category sprite, or
  pressed-content-root references missing
- Fixed Game Flow view configured for runtime dynamic loading
- Multiple active modal Game Flow views
- Active modal view not blocking Battle UI input
- Duplicate Button subscriptions
- Stale Introduction items after Stage change or retry
- Result copy or visible button set inconsistent with the current result and `HasNextStage`
- Hidden view still accepting input

Validation reports authoring problems without creating substitute UI, choosing a Stage, or starting Battle.

Reference validation returns success or failure before Game Flow UI subscribes
or reconciles. Missing core wiring leaves all available views hidden and locked,
and GameFlowUIRoot creates no controller or view subscriptions. Introduction
validation checks that its item prefab contains TowerContentUIItem and that the
item has assigned name, description, icon, raycast-enabled icon background,
idle/pressed category sprites, and pressed-content-root references before any
Stage content is populated.

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
- Towers and Upgrades share one ordered Introduction container, with Towers presented first.
- Introduction items show name, description, icon, and category background without accepting Draft selection.
- Selectable Tower content uses pointer-driven pressed sprites and content-root
  offset, publishes selection only from a valid pointer click, and always
  restores both visuals to idle after the press ends.
- Read-only Introduction content never enters pressed feedback.
- An interactable Confirm button swaps to its pressed sprite and offsets only
  its child content while pressed, then restores both on release or exit.
- Confirm visual feedback never invokes Confirm independently of Button
  `onClick`.
- Confirm starts the already prepared Stage exactly once.
- A Stage without presentable Introduction content begins without an empty window.
- Victory and Defeat display only StageResultWindowView.
- Victory displays `VICTORY` and `All enemy waves have been defeated.`
- Non-final Victory displays only the `Next Stage` button.
- Final Victory displays only the `Main Menu` button.
- Defeat displays `DEFEAT` and `The enemy broke through your defenses.`
- Defeat displays the `Retry` and `Main Menu` buttons.
- Retry prepares the same Stage with fresh Player health.
- Return releases current Stage runtime and shows MainMenuView.
- Starting again from MainMenu begins at index zero.
- Modal views block Battle UI interaction beneath them.
- Fixed views are authored references; only Introduction items are dynamically instantiated.
- Startup and re-enable normalize every fixed view to hidden and locked before
  reconciling the current Game Flow state.
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
- Create StageIntroductionView prefab with authored title, one introduced-item container, and Confirm button.
- Add `ButtonPressFeedback` beside the Confirm Button, set Button Transition to
  `None`, and assign its Button, Image, normal/pressed sprites, child content
  root, and pressed offset.
- Reuse the TowerContentUIItem prefab for Introduction content and assign its unified container and prefab references.
- Create one StageResultWindowView with an assigned presentation `Root Object`;
  place its title, description, and authored `Next Stage`, `Retry`, and
  `Main Menu` buttons under that root.
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
