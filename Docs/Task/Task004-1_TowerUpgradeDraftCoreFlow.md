# Task004-1 - Tower Upgrade Draft Core Flow

## Status

Implemented.

## Goal

Implement the playable core Tower Upgrade Draft flow without valid-target highlight or presentation motion work.

After this task, the existing Draft flow should support both Tower Draft results and Tower Upgrade Draft results through one unified DraftSystem, one pending Draft item UI shell, and one TowerPlacementController drag lifecycle.

## Relationship To Task004

Task004 describes the full Tower Upgrade Draft Flow Integration target, including valid-target highlight and possible presentation motion extraction.

Task004-1 extracts the core gameplay flow only so it can be reviewed and implemented first.

Task004-1 should not implement:

- Valid target tower highlight.
- Material BaseMap color changes.
- Presentation motion or pulse animation.
- AttackRangePreviewMotion abstraction or migration.
- Advanced target feedback UX.

Those remain in Task004 or a later follow-up task.

## Source Documents

- `Docs/05_DraftSystem.md`
- `Docs/06_TowerPlacementSystem.md`
- `Docs/10_TowerUpgradeSystem.md`
- `Docs/02_BattleHUDUISystem.md`
- `Docs/Task/Task004_TowerUpgradeDraftFlowIntegration.md`

## Required Core Flow

Task004-1 should implement this formal Tower Upgrade Draft core flow:

```text
PlayerSystem raises OnPlayerLevelUp
    ->
DraftSystem generates Draft Choices
    ->
BattleHUD opens Draft Window
    ->
Player selects Tower Upgrade Draft choice
    ->
DraftSystem processes selected Draft result
    ->
BattleHUD creates draggable PendingDraftUI with Tower Upgrade payload
    ->
Player drags PendingDraftUI onto target tower
    ->
TowerPlacementController detects target tower intent
    ->
TowerPlacementController routes target intent to TowerUpgradeSystem
    ->
TowerUpgradeSystem validates and applies upgrade
    ->
Successful apply consumes the pending Draft item
```

Rejected apply must not consume the pending Draft item.

Cancelling the drag or returning the item to the Draft Item Interaction Area must not consume the pending Draft item.

## Unified DraftSystem Requirements

Task004-1 should upgrade the existing TowerDraftSystem concept into one unified DraftSystem.

DraftSystem should generate both:

- Tower Draft candidates from TowerDefinitionDatabase.
- Tower Upgrade Draft candidates from TowerUpgradeDatabase and current deployed tower state.

Do not create two separate systems that each own similar candidate generation, sampling, displayed-choice deduplication, and result workflow.

TowerDefinitionDatabase and TowerUpgradeDatabase are content sources only. They should meet inside DraftSystem's combined candidate pipeline:

```text
Tower Draft candidates
    +
Tower Upgrade Draft candidates
    ->
Combined Draft candidate pool
    ->
Equal-weight candidate entry sampling
    ->
Displayed identity dedupe
```

DraftSystem should explicitly distinguish Draft result types:

- Tower Draft result carries a TowerDefinition.
- Tower Upgrade Draft result carries a TowerUpgradeDefinition.

DraftSystem result handling should route by DraftResultType:

- Tower Draft result creates a pending Draft item with TowerDefinition payload.
- Tower Upgrade Draft result creates a pending Draft item with TowerUpgradeDefinition payload.

Selecting any Draft choice should close or finish the current Draft selection flow.

## Candidate Generation Rules

Tower Draft candidates:

- Come from TowerDefinitionDatabase.
- Use TowerDefinition as displayed identity.
- Should be available when TowerDefinitionDatabase contains at least one valid TowerDefinition.

Tower Upgrade Draft candidates:

- Come from TowerUpgradeDatabase.
- Are generated against current deployed tower instances.
- Use TowerUpgradeDefinition as displayed identity.
- May be generated per eligible tower instance, so repeated internal candidate entries can naturally weight upgrades toward tower families with more eligible deployed instances.

For Tower Upgrade candidate generation, DraftSystem may use TowerUpgradeSystem.CanApplyUpgrade against deployed tower instances to check whether at least one current tower can receive an upgrade. This is candidate filtering only.

Final validation and mutation must still happen through TowerUpgradeSystem.TryApplyUpgrade when the pending Draft item is dropped.

If no valid Tower Upgrade Draft candidates exist, DraftSystem should not force an upgrade choice. It should continue with other available Draft choice types, such as Tower Draft choices.

More advanced weighting rules can be added later. V1 should keep the combined candidate pool simple:

- Each candidate entry has equal sampling weight.
- Tower Draft identity is TowerDefinition.
- Tower Upgrade Draft identity is TowerUpgradeDefinition.
- Final displayed choices should not show duplicate identities in the same Draft window.

## PendingDraftUI Requirements

Reuse the existing pending Draft item UI shell.

Do not create a separate PendingTowerUpgradeItemUI unless implementation proves it is absolutely necessary.

PendingDraftUI should carry a DraftResult or DraftPayload that can represent either:

- Tower Draft with TowerDefinition.
- Tower Upgrade Draft with TowerUpgradeDefinition.

The visual shell and drag interaction should be shared. The payload and drop behavior should branch by DraftResultType.

Task004-1 should keep pending item behavior simple:

- Selecting one DraftChoice creates one pending Draft item.
- Successful placement, level-up, or upgrade application consumes that item.
- Invalid drop, cancel, or failed apply restores or keeps that item.

Task004-1 should not introduce a new pending-item queue, pause/resume system, or broader pending-item accumulation UX.

BattleHUD should remove the pending Draft item only after the corresponding drop action succeeds:

- Tower Draft success means successful tower placement or accepted tower level-up.
- Tower Upgrade Draft success means TowerUpgradeSystem.TryApplyUpgrade succeeds.

If a drag is cancelled, released back into the Draft Item Interaction Area, dropped on an invalid target, or fails application, the PendingDraftUI should return to the Draft Item Interaction Area or its previous pending-item position.

BattleHUD should not decide target eligibility.

## TowerPlacementController Requirements

Keep one TowerPlacementController.

Do not create a separate TowerUpgradePlacementController.

TowerPlacementController should add an internal drag mode or result-type branch for:

- Tower Draft payload.
- Tower Upgrade Draft payload.

Shared outer lifecycle should include:

- Mouse world position handling.
- Grid node lookup.
- Occupied tower lookup.
- Release back to Draft Item Interaction Area handling.
- Cancel handling.
- Restore handling.
- Consume handling.

The actual success logic should branch by DraftResultType:

- Tower Draft uses the existing tower placement and tower level-up flow.
- Tower Upgrade Draft calls TowerUpgradeSystem.TryApplyUpgrade.

Tower Upgrade Draft drag mode should not create a tower preview prefab.

Tower Upgrade Draft drag mode should only:

- Track the drag lifecycle.
- Use grid node lookup and occupied tower lookup to detect target tower intent.
- Route final validation and mutation to TowerUpgradeSystem.TryApplyUpgrade.

Task004-1 must preserve existing Tower Draft placement and tower level-up behavior.

## Drop Handling Contract

All Draft result types should share the same outer drop lifecycle:

- Release back to Draft Item Interaction Area -> restore, no consume.
- Invalid target -> restore, no consume.
- Failed apply -> restore, no consume.
- Success -> consume.

Tower Draft success path:

- Valid deployment tile -> place tower and consume pending Draft item.
- Existing same-TowerFamily tower target -> route level-up request to TowerUpgradeSystem; consume only if accepted.

Tower Upgrade Draft success path:

- Existing tower target -> call TowerUpgradeSystem.TryApplyUpgrade with the dragged TowerUpgradeDefinition.
- Consume only if TryApplyUpgrade succeeds.

TowerPlacementController should not decide TowerFamily, RequiredTowerLevel, duplicate, or upgrade definition validation rules for Tower Upgrade Drafts.

## TowerUpgradeSystem Requirements

Task004-1 should use the Task002 application API:

- CanApplyUpgrade
- TryApplyUpgrade

The expected v1 eligibility rules are:

- Target tower exists.
- Upgrade definition exists.
- Target tower has a TowerDefinition.
- Target tower TowerFamily matches upgrade TowerFamily.
- Upgrade definition is valid.
- Target tower CurrentLevel satisfies RequiredTowerLevel.
- Target tower does not already have the same TowerUpgradeDefinition.

There is no upgrade slot quantity limit in v1.

## Out Of Scope

- Valid target highlight.
- Highlight cleanup.
- Material BaseMap color changes.
- MaterialPropertyBlock or runtime material instance work.
- Pulse animation.
- Presentation motion extraction.
- AttackRangePreviewMotion refactor.
- Separate Tower Upgrade Draft UI shell.
- Separate TowerUpgradePlacementController.
- Separate Tower Upgrade Draft generation system.
- Reroll.
- Rarity.
- Global Buff Draft.
- Temporary Buff Draft.
- Synergy Draft gameplay.
- Concrete behaviour package execution.
- Advanced Tower Upgrade Draft UX.

## Acceptance Criteria

- Existing Tower Draft placement behavior is preserved.
- Existing Tower Draft tower level-up behavior is preserved.
- The existing TowerDraftSystem concept is upgraded into one unified DraftSystem.
- DraftSystem generates Tower Draft candidates from TowerDefinitionDatabase.
- DraftSystem generates Tower Upgrade Draft candidates from TowerUpgradeDatabase and current deployed tower state.
- DraftSystem merges Tower Draft candidates and Tower Upgrade Draft candidates into one combined candidate pool.
- Combined candidate sampling uses equal-weight candidate entries in v1.
- Displayed choices are deduplicated by Draft item identity.
- Tower Draft identity is TowerDefinition.
- Tower Upgrade Draft identity is TowerUpgradeDefinition.
- If no valid Tower Upgrade Draft candidates exist, DraftSystem continues with other available Draft choice types instead of forcing an upgrade choice.
- DraftSystem does not apply upgrades directly.
- TowerUpgradeSystem does not generate, weight, sample, or display Draft choices.
- Tower Draft results and Tower Upgrade Draft results are distinct result types.
- Tower Draft results create pending Draft items with TowerDefinition payload.
- Tower Upgrade Draft results create pending Draft items with TowerUpgradeDefinition payload.
- PendingDraftUI is reused for both Tower Draft and Tower Upgrade Draft payloads.
- The existing pending Draft item prefab is reused where possible.
- Selecting one DraftChoice creates one pending Draft item.
- Task004-1 does not add a new pending-item queue or broader pending-item accumulation UX.
- TowerPlacementController remains the single drag/drop controller.
- TowerPlacementController branches success logic by DraftResultType.
- Tower Upgrade Draft drag mode does not create a tower preview prefab.
- Dragging a Tower Upgrade Draft item onto a valid target tower applies the upgrade.
- Dragging a Tower Upgrade Draft item onto an invalid target tower does not apply the upgrade.
- Invalid target rejection returns the pending Draft item to the Draft Item Interaction Area or its previous pending-item position and does not consume it.
- Releasing the item back into the Draft Item Interaction Area restores the pending Draft item and does not consume it.
- Successful Tower Upgrade Draft apply consumes the pending Draft item.
- Candidate generation may use CanApplyUpgrade for filtering, but final drop handling uses TryApplyUpgrade for validation and mutation.
- A failed TryApplyUpgrade does not mutate tower upgrade state.
- Task004-1 does not implement valid-target highlight or presentation motion.

## Implementation Review Note

Before implementation begins, present the concrete Task004-1 implementation plan in chat for review.
