# Task005 - Tower State UI

Document Set: Task
Status: Complete — runtime code and managed/static validation finished; user completed Unity setup, confirmed the displayed result, and accepted the task on 2026-10-05.
Dependencies: [Task004 - Tower Model Root Naming](Task004_TowerModelRootNaming.md).
Next: None for this task; preserve the validation evidence and authoring reference below.

## 1. Goal

Display the authoritative current level of each deployed Tower near its stable
model root, with one lightweight status item per Tower and complete battle-local
creation/removal/clear behavior.

## 2. Source Documents

- [Tower Framework System](../System/10_TowerFrameworkSystem.md), Sections 3 and 6.1: anchor, display, and lifecycle contract.
- [Battle HUD UI System](../System/04_BattleHUDUISystem.md), Section 2: composition and input boundary.
- [Tower Placement System](../System/09_TowerPlacementSystem.md), Section 4: post-commit presentation.
- [Tower Upgrade System](../System/13_TowerUpgradeSystem.md): authoritative level state and accepted changes.
- [Task004](Task004_TowerModelRootNaming.md): stable-root naming migration.

## 3. Scope

- `TowerStateUIItem` binds one Tower and its stable root, displays its level,
  follows its screen position, and releases its state subscription.
- `TowerStateUIManager` owns Tower-to-item tracking, duplicate-safe creation,
  single-item removal, and idempotent clear for the exact battle runtime.
- Author a status item Prefab and a dedicated container beneath
  `Prefab_GameRuntime/Canvas/BattleUIRoot`.
- Integrate committed deployment, accepted level updates, Tower removal,
  result-screen retention, and runtime release/retry/Stage replacement.

## 4. Out Of Scope

Object pooling, per-level UI anchors, renderer-bounds positioning, additional
Tower information, UI animations, gameplay/upgrade-rule changes, Monster status
refactoring, and a generalized shared status framework.

## 5. Ownership And Planning Inputs

Tower Framework owns status behavior; Battle UI supplies the container. Placement
owns deployed membership; Upgrade owns level changes; battle coordination owns
the runtime release boundary. UI failure cannot reverse committed gameplay.

Inspect these current integration points before proposing code changes:

- `TowerInstance.CurrentLevel` and `OnLevelChanged`: initial value and committed updates.
- `TowerVisualController.TowerModelRoot` after Task004: stable positioning reference.
- `TowerPlacementSubmission`: committed membership and post-commit presentation.
- `TowerDeployController`: existing Tower creation boundary, distinct from successful commit.
- `DeployedTowerCollection` and `BattleRuntimeCoordinator`: stop versus release.
- `MonsterStatusUIItem` and `MonsterStatusUIManager`: local pattern references,
  not automatic authority for Tower lifecycle or coordinate behavior.

The plan must select concrete creation/removal/clear integration points, dependency
wiring, camera source, and update timing relative to Camera movement. It must also
settle how explicit removal and Item-side destruction detection keep Manager
records consistent, prevent outgoing creation after clear/rebinding, and dispose
subscriptions even when UI is hidden. Avoid adding a generic event framework.

## 6. Display And Position Contract

Proposed authored hierarchy:

```text
UiPrefab_TowerStateItem [TowerStateUIItem]
└── LevelDisplay
    ├── FixedText  "Level."
    └── LevelText  "1"
```

Only `LevelText` is dynamically updated. Bind immediately from current state;
then respond to accepted level changes. Fixed text, font, spacing, shared offset,
and multi-digit layout remain Prefab authoring. Model replacement retains the Item.

Project `TowerModelRoot.position` using the battlefield Camera, then convert to
the parent RectTransform coordinate space. The current runtime Canvas is Screen
Space Overlay with Scale With Screen Size and a 1080 × 1920 reference resolution;
do not assign pixel coordinates directly to scaled `anchoredPosition`.

Position updates follow Camera movement independently of simulation time. Hide
when the Tower is inactive, the anchor is behind/outside the camera viewport, or
the camera is unavailable; resume when valid. No edge clamping. Offset uses UI
units, not world units; it does not track model height or scale with camera zoom.

## 7. Lifecycle And Failure Contract

- No Item for previews, readiness-only Towers, failed deployment, or cancellation.
- Successful committed deployment creates at most one Item per Tower; repeated
  requests do not create duplicates. UI observes the committed level immediately.
- Level-up updates only the existing numeric display; UI errors do not undo it.
- Draft pause preserves status and does not transfer pause or input ownership.
- Temporary Tower/UI hiding retains or safely restores the binding and current
  value; it must not produce duplicate subscriptions or stale text on return.
- Tower removal disposes its Item and tracking record. Clear invalidates outgoing
  bindings, disposes all Items/subscriptions, and empties Manager tracking safely
  even when called repeatedly or after individual destruction.
- Battle stop retains status while deployed Towers remain for results. Runtime
  release, retry, or Stage replacement clears it before a new runtime can bind.
- Missing Prefab, numeric text, container, or anchor references produce actionable
  diagnostics and no partial visible Item. UI failure does not fail gameplay commit.
- Manager destruction also releases owned presentation. UI cleanup never destroys
  the Tower or alters authoritative level/membership.

## 8. Unity Authoring Checklist

- Configure the Item script and explicit numeric TMP reference.
- Group both text objects under `LevelDisplay` for shared offset and layout;
  accommodate multi-digit levels and disable text raycast interception.
- Configure the dedicated container and Manager's item Prefab reference.
- Preserve existing Battle UI sibling ordering and modal input authority; the
  container must not acquire unwanted layout, clipping, or input-blocking behavior.
- Verify all supported TowerFamilies/level models share the migrated root contract.
- Tune offset/font/spacing in the real scene; numerical visual values are not yet
  approved. Begin with the stable-root approach before proposing extra anchors.
- User owns Inspector configuration, import/reserialization, and Play Mode unless
  explicitly delegated. An unwired asset is pending work, not completed acceptance.

## 9. Acceptance Criteria And Validation

Verify deployment success/failure, duplicate creation, initial level, accepted and
rejected level-up, model replacement, single removal, repeated clear, result-screen
retention, retry/next Stage, and stale outgoing requests. Confirm cleanup removes
both Item subscriptions and Manager records. Exercise missing configuration without
changing gameplay outcomes.

Native visual checks cover each TowerFamily, supported levels, multi-digit layout,
Camera pan/zoom, viewport edges and behind-camera behavior, resolution changes,
Draft pause, disable/re-enable, and unobstructed drag/click input. Confirm alignment
without a frame of Camera-follow lag and no residual status after runtime release.

Use focused managed tests for lifecycle/event integration where feasible and run
relevant deployment/level-up regression checks. Compile affected assemblies when
the environment supports it. Record managed/static, Unity import/Inspector, and
Play Mode evidence separately; never claim visual acceptance from static checks.

## 10. Review And Status

The implementation plan was presented in chat, reviewed in the referenced
"Review Task003 TowerState UI plan" chat, and authorized for implementation on
2026-10-05. That conversation's Task003 label refers to this Task005 contract.

Implemented:

- Added TowerStateUIItem and TowerStateUIManager, including script metadata.
  Initial level and accepted level changes affect only LevelText. Position uses
  the stable TowerModelRoot and the supplied placement output Camera.
- BattleRuntimeCoordinator binds the Manager after Submission begins and before
  Initial Draft opens, using the existing deployment-committed event. Binding
  exceptions and partial-binding cleanup are isolated from battle startup.
- Stop preserves existing displays. Retention and enable-time reconciliation use
  runtime membership without the active gate; deployment callbacks additionally
  check deployment authority. Release clears UI before Tower destruction.
- Disable preserves deployment binding but removes the static Canvas callback;
  hidden deployments are deferred. Enable prunes, backfills, and refreshes current
  members, including a stopped result-screen runtime. Clear while hidden removes
  all subscriptions and prevents later restoration.
- Binding revisions reject captured outgoing callbacks. Exact Item identity
  prevents delayed destruction from removing a replacement record.
- Canvas pre-render refresh converts screen coordinates into container-local
  coordinates; the display group hides for unavailable cameras, inactive Towers,
  and anchors behind/outside the viewport. No camera/main lookup or edge clamping.

Validation recorded:

- `python3 Tests/TowerStateUI/run.py`: 14 production UI managed cases plus 5
  extracted-production Coordinator cases passed. Boundary doubles model Unity
  objects/events; they do not establish native rendering or lifecycle ordering.
- `python3 Tests/Task004/run.py`: 31 existing deployment/upgrade contracts passed.
- Runtime assembly build passed with 0 warnings and 0 errors. The current
  Unity-generated project did not yet list the new files, so a temporary MSBuild
  target explicitly included both sources without editing the generated project.
- `Tests/Task005/run.py` (older combat-binding suite, unrelated numbering) could
  not compile because its LifecycleHarness lacks DashedPathSession. All three
  production methods extracted by that suite are unchanged from HEAD; this
  existing harness mismatch was not repaired as part of Tower status UI.
- Scoped whitespace checks passed. No Editor scripts changed.

After the code step, the user authored
`Assets/Art/Prefab/UI/UiPrefab_TowerStatusUIItem.prefab`, connected its numeric
text and display-group references, and wired the Manager under BattleUIRoot to
BattleRuntimeCoordinator in Prefab_GameRuntime. The authored fixed prefix is
`Lv.`. On 2026-10-05 the user reported that the Unity result looked good and
explicitly declared the Tower status UI task complete.

This is user-reported native visual acceptance. The conversation does not contain
individual results for every scenario in the planned native matrix; those results
are not inferred from the overall acceptance. The older combat-binding harness
limitation above remains recorded.

## 11. Unity Setup And Native Validation Reference

The original authoring/validation checklist is retained below as a maintenance
reference. The accepted asset uses `UiPrefab_TowerStatusUIItem` and the container
name `TowerStatusUIManager`; these names do not change script binding behavior.

1. Import the new scripts and confirm Task004's TowerModelRoot references on all
   four Tower base Prefabs. Let Unity regenerate IDE project files as needed.
2. Create `UiPrefab_TowerStateItem` using the Section 6 hierarchy. Root: UI layer,
   RectTransform, TowerStateUIItem; no input-blocking graphic. Use centered fixed
   anchors and pivot, unit scale and no rotation for its root.
3. Under the root, create LevelDisplay with two TextMeshProUGUI children. Set
   FixedText to `Level.` and LevelText to `1`; disable Raycast Target on both.
   Use a horizontal layout with preferred text widths and content sizing so
   multi-digit values fit. Adjust the shared UI offset on LevelDisplay only.
4. Assign the Item's `Level Text` and `Level Display` fields. The display group
   must be a child of the Item and contain the numeric text, not the Item root.
5. Under `Prefab_GameRuntime/Canvas/BattleUIRoot`, create `TowerStateUIRoot` with
   a full-stretch RectTransform (zero offsets, centered pivot) and
   TowerStateUIManager. Assign `State UI Item Prefab` to the new Item Prefab.
   Do not add layout, masking, or input interception to this container. Keep it
   behind interactive/modal HUD content and preserve existing sibling ordering.
6. Assign this Manager to BattleRuntimeCoordinator's `Tower State UI Manager`.
   Camera is supplied in code from TowerPlacementController.PlacementCamera;
   no additional Camera field needs to be wired on the Manager or Item.
7. Exercise Section 9's native matrix, including Stop -> UI hide/restore ->
   Release, hidden deployment/upgrade/removal, and release while hidden. Confirm
   fonts/offset, multi-digit readability, no Camera-follow frame lag, no input
   interception, and no residual labels on retry or next Stage. Current rendering
   support is Screen Space Overlay; other Canvas modes are not silently inferred.

For subsequent changes, record import/Inspector and Play Mode results separately.
Task005 is closed by the user's acceptance above; no additional per-scenario
native results or separate Task004 acceptance are claimed here.
