# Task002 - Camera Pan And Authored Map Boundary

Status: Not started

Depends on: Task001 for final Draft-modal interaction acceptance; Cinemachine installed through Unity Package Manager

## 1. Goal

Implement press-and-drag Camera pan over the Active Map with direct-manipulation movement:

```text
Pointer Drags Screen Content
    -> Battlefield Content Follows The Pointer
    -> Camera Translates In The Opposite World Direction
```

Movement must be derived from pointer rays intersecting the Active Map plane so the perceived drag distance remains consistent across resolution and Camera framing.

Each Map Prefab authors one 2D or 3D Collider for its Cinemachine Confiner. Stage composition must ensure that the Collider from the newly created Map is bound to the current matching Cinemachine Confiner before the Stage becomes ready, and must clear that binding safely during rollback, replacement, and release.

Every initial Stage, next Stage, and retry must also reset the Camera to its authored default pose. Runtime Pan displacement never carries into another Stage composition.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/01_GameFlowSystem.md`
- `Doc/02_StageSystem.md`
- `Doc/04_BattleHUDUISystem.md`
- `Doc/05_MapSystem.md`
- `Doc/06_CameraSystem.md`
- `Doc/09_TowerPlacementSystem.md`
- `Doc/Task/Task001_InitialTowerDraftAndWaveStart.md`

## 3. Pre-Implementation State

- The project has one tilted Orthographic gameplay Camera.
- No project-owned Camera Pan controller currently exists.
- Tower placement reads the gameplay Camera for pointer-to-world interaction.
- Stage composition creates and validates one candidate Map through `StageCompositionController.TryCreateCandidateMap(selectedStage)`.
- Camera bounds are not currently bound as part of Stage preparation or release.
- Cinemachine is not present in the current package manifest at document-generation time; the user will install it before implementation.

After installation, implementation must confirm the actual Cinemachine package version and compile-time Confiner APIs rather than assuming an older package API.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| Map Prefab | Author one valid Camera boundary Collider and any deliberate decorative margin |
| Map boundary adapter | Expose exactly one supported 2D Collider or 3D Collider from the instantiated Map |
| StageCompositionController | Orchestrate validation, binding, commit, replacement, default-pose reset, and clearing of the Map boundary handoff |
| Camera Pan controller | Own gesture eligibility, ray-plane drag conversion, and logical pan position |
| Cinemachine Camera and Confiner | Produce the final Camera view and enforce the currently bound Map boundary |
| Game Flow | Supply whether battle-local Camera interaction is allowed |
| Draft and Battle HUD UI | Own modal Draft input and held-item drag gestures |
| Tower Placement | Own an accepted placement gesture until release or cancellation |

Stage composition orchestrates the boundary handoff but does not calculate Camera movement. Camera runtime enforces the Collider but does not own Map creation or destruction.

## 5. Cinemachine Runtime Contract

Use the Cinemachine 3.x version resolved by Unity Package Manager for the project's Unity editor.

The gameplay Camera rig must provide:

- One active Cinemachine Camera view
- One Camera Pan target or equivalent logical movement owner
- A Confiner configuration capable of consuming the supported 2D boundary
- A Confiner configuration capable of consuming the supported 3D boundary when 3D Map boundaries are authored
- A single authoritative output Camera used by both Camera Pan raycasts and existing battlefield interaction

Only the Confiner matching the current Map boundary type may control the view. The unused Confiner type must not retain an active Collider or apply a second constraint.

Do not dynamically search the scene every frame. Stable Camera and Confiner references are authored once and validated before Stage preparation.

## 6. Map Prefab Boundary Contract

Every playable Map Prefab uses this conceptual hierarchy:

```text
Map Root
├── NodesRoot
└── CameraBoundaryRoot
    └── CameraMovementBoundary
        -> One Collider2D
        OR
        -> One Collider
```

Add one small Map-owned boundary adapter or equivalent explicit reference contract. It exposes either:

- One enabled, valid 2D Collider; or
- One enabled, valid 3D Collider.

It is invalid to expose neither, both types simultaneously, multiple candidates, a Collider outside the instantiated Map hierarchy, or a degenerate boundary.

The boundary:

- Is presentation-only and must not affect Monster collision, pathfinding, Tower placement, or runtime occupancy.
- Uses a dedicated non-gameplay Layer or equivalent collision exclusion.
- May extend outside the Grid gameplay footprint to show surrounding decoration.
- Is saved in the Map Prefab and instantiated with that Map.
- Must be usable by the installed Cinemachine Confiner type and the current Camera orientation.

For the current XZ gameplay plane and tilted Camera, validate the chosen Collider type in Play Mode. A 2D Collider is accepted only when its plane and Cinemachine Confiner behavior correctly match this Camera rig; otherwise author a 3D volume.

## 7. Stage Composition To Confiner Handoff

The current candidate Map is created through:

```text
StageCompositionController.TryCreateCandidateMap(selectedStage)
```

This method is the creation anchor, not a required location for the Confiner assignment. Implementation may bind the boundary:

- During candidate creation after Map validation;
- In a separate preparation step after `TryCreateCandidateMap()` succeeds; or
- During the guarded candidate-to-Active-Map commit.

The selected location must preserve the preparation transaction, complete before Stage composition reports ready, and allow all failure paths to clear the correct owned binding.

After the candidate Map instance and its `MapGeneratorBehaviour` pass Map validation, Stage composition must ensure this sequence completes:

```text
Resolve Candidate Map Boundary Adapter
    -> Validate Exactly One 2D Or 3D Collider
    -> Pass Candidate Map, Collider, And Boundary Type To Camera Runtime
    -> Clear The Inactive Confiner Type
    -> Assign Collider To The Matching Current Cinemachine Confiner
    -> Invalidate Any Cached Confiner Boundary Data Required By The Installed Version
    -> Confirm The Confiner Accepted The Candidate Boundary
    -> Keep Camera Input Disabled Until The Stage Is Committed And Battle Allows It
```

Stage preparation succeeds only when this boundary handoff succeeds. A valid gameplay Grid with an unusable Camera boundary is not a prepared Stage.

Do not let `StageCompositionController` perform pan math. It invokes a narrow Camera-runtime binding operation and treats its success as part of candidate preparation.

## 8. Transactional Binding And Cleanup

The Confiner binding participates in the same preparation transaction as the candidate Map.

### 8.1 Candidate Failure Or Deferred Release

If any later Battle-runtime preparation step fails, throws, or receives a deferred release:

1. Disable Camera interaction.
2. Clear the matching Confiner reference when it still points to the candidate Collider.
3. Clear the inactive Confiner type as well.
4. Reset Camera gesture and logical pan state.
5. Destroy the candidate Map.

The Confiner must never retain a reference to a destroyed candidate Collider.

### 8.2 Successful Commit

When `CommitCandidateStage()` succeeds:

- The staged boundary becomes the Active Map boundary.
- Camera Pan state resets to the authored default pose relative to the new Map Root/framing origin.
- Camera input remains governed by Game Flow and interaction-owner eligibility.
- No outgoing Map boundary remains assigned.

### 8.3 Stage Replacement Or Release

`ReleaseStageRuntimeCore()` clears the Camera binding before destroying the Active or candidate Map object.

Clear is conditional on owned identity: a late cleanup from an older candidate must not remove a newer committed boundary.

Release and repeated clear calls are idempotent. After release, no Camera pan is accepted until another valid Active Map boundary is committed.

## 9. Initial Framing

When a candidate becomes the Active Map:

- Restore the Camera rig and logical Pan target from one stable authored default pose captured or configured independently from runtime Pan.
- Resolve that default pose relative to the new Map's authored framing origin.
- Preserve its authored Camera rotation, projection, height, and framing offset unless a later design explicitly changes them.
- Allow Cinemachine and the matching Confiner to resolve the final valid Camera pose.
- Do not translate or recenter Map gameplay data.

This reset is mandatory for:

- The first Stage of a new run
- Loading the next Stage after Victory
- Retrying the current Stage after Defeat

The reset occurs before Camera input becomes eligible and without retaining the outgoing Stage's Pan offset. Do not capture the currently panned Camera position during Stage release and reuse it as the next default.

The Camera viewport may show content outside the Grid gameplay footprint. The authored Confiner boundary, not the rectangular Grid size, determines permitted movement.

## 10. Direct-Manipulation Pan

For each accepted pointer movement sample:

```text
Previous Map Point
    = Previous Pointer Ray Intersected With Active Map Plane

Current Map Point
    = Current Pointer Ray Intersected With Active Map Plane

Requested Camera Translation
    = Previous Map Point - Current Map Point
```

Apply the translation on the Active Map plane to the logical Camera Pan position. This makes battlefield content follow the pointer and works for the tilted Orthographic Camera without a fixed world-units-per-pixel speed scalar.

If either ray cannot intersect the Map plane, ignore that sample and reset the sample baseline safely.

The ray source must be the authoritative final gameplay Camera so the calculation matches what the player sees.

## 11. Confiner And Logical Position Reconciliation

Cinemachine may constrain the rendered Camera after a requested Pan target moves.

The logical Pan position must not continue accumulating movement beyond the actual confined pose. After the Confiner resolves a clamped result, reconcile the logical position with the accepted Camera position or otherwise clamp the logical target to the same boundary.

This guarantees:

- No hidden overscroll distance builds up beyond the boundary.
- Reversing drag direction moves immediately.
- Changing Maps does not reuse an unreachable target position.
- The Confiner remains the final boundary authority without degrading direct manipulation.

Elastic overscroll and spring-back are outside this task.

## 12. Gesture Eligibility And Ownership

Camera Pan begins only when:

- Game Flow is in Battle.
- One valid Active Map and Confiner boundary are committed.
- The primary pointer begins on an eligible battlefield region.
- No modal Draft or Game Flow UI owns the interaction.
- No held Draft item or Tower Placement operation owns that pointer.
- No other Camera Pan gesture is active.

Ownership is fixed at pointer press:

- A press beginning over interactive UI never becomes Camera Pan after leaving UI.
- A press owned by a held Draft item remains owned by it through release or cancellation.
- A Camera Pan entering a UI region remains Camera-owned until release unless the gesture is cancelled by Battle or Stage lifecycle.

Only one primary-pointer Pan is in scope. Multi-touch, pinch zoom, and simultaneous gesture transfer are deferred.

## 13. Lifecycle And Failure Handling

Cancel the active Pan and clear its pointer sample when:

- Game Flow leaves Battle.
- A modal surface invalidates Camera interaction.
- The Active Map is replaced or released.
- The Camera controller or relevant Camera is disabled.
- The active pointer is cancelled.

Reject Stage preparation when:

- The candidate Map has no valid boundary adapter.
- Boundary selection is ambiguous.
- The matching Cinemachine Confiner is missing.
- The Collider type is unsupported by the installed Cinemachine version.
- Confiner assignment or required cache invalidation fails.

Failure is technical and result-neutral. It must not start Battle, publish Victory or Defeat, or silently continue with an unconstrained Camera.

## 14. Out Of Scope

- Cinemachine package installation itself
- Pinch or wheel zoom
- Camera rotation
- Inertial movement
- Elastic overscroll or spring-back
- Edge scrolling
- Camera shake
- Cinematic paths or follow targets
- Minimap navigation
- Changing Map gameplay coordinates
- Changing Tower placement or pathfinding rules
- Time-scale changes during Draft

## 15. Unity Authoring Checklist

- Install a Unity-6-compatible Cinemachine 3.x package through Package Manager.
- Reopen and compile the project before implementing against its concrete APIs.
- Configure one authoritative Cinemachine gameplay Camera and output Camera.
- Author and assign the required 2D and/or 3D Confiner components on the Camera rig.
- Assign stable Camera runtime references on the Game runtime prefab.
- Add one `CameraBoundaryRoot` and exactly one supported boundary Collider to every playable Map Prefab.
- Put every boundary on a non-gameplay Layer and exclude it from gameplay physics queries.
- Size each boundary with the desired decorative margin outside the gameplay Grid.
- Confirm all five Stage Map Prefabs satisfy the same boundary adapter contract.
- Preserve existing Camera projection, rotation, and Tower-placement Camera references.

## 16. Acceptance Criteria

- Every playable Map Prefab exposes exactly one valid 2D or 3D Camera boundary.
- Stage composition binds the new Map Collider to the matching current Cinemachine Confiner before Stage preparation can report ready; the exact helper or lifecycle method is not prescribed.
- The inactive Confiner type has no active stale boundary.
- Candidate failure, cancellation, Stage replacement, and release clear Confiner references before destroying their Map objects.
- No destroyed Collider remains referenced by Cinemachine.
- The initial Stage, next Stage, and retry each restore the authored default Camera pose and use their Map's own boundary.
- No runtime Pan displacement carries from one Stage battle into another.
- Dragging makes battlefield content follow the pointer with no authored speed scalar.
- Behavior remains subjectively consistent across supported screen resolutions.
- The Camera cannot pan beyond the Map-authored permitted region.
- The viewport may reveal the authored margin outside the gameplay Grid.
- Reversing at a boundary responds immediately with no hidden overscroll.
- Draft modal input and held-item drag never also move the Camera.
- Stage transition and retry replace the boundary and framing without stale state.
- Missing or invalid boundary authoring fails Stage preparation instead of running unconstrained.

## 17. Static Validation

- Build `Assembly-CSharp.csproj` without restoring packages after Cinemachine installation has completed and Unity has regenerated project files.
- Run path-scoped `git diff --check` for touched scripts, prefabs, package files, and documents.
- Search for per-frame scene-wide Camera, Map, or Collider discovery.
- Search every Camera binding path for a corresponding identity-safe clear path.
- Confirm both 2D and 3D branches clear the inactive Confiner type.
- Confirm Camera Pan does not use a fixed world-units-per-pixel speed scalar.
- Confirm gameplay collision queries exclude the Camera boundary Layer.

## 18. Unity Play Mode Handoff

Verify in Unity:

- Each of the five Maps binds its own Collider and confines the Camera.
- At least one representative 2D boundary and one representative 3D boundary if both modes will remain supported.
- Initial Stage, next Stage, retry, and return-to-main-menu lifecycle.
- Exact default Camera pose restoration after panning far from center before next Stage and before retry.
- Candidate preparation failure and deferred release without stale Collider references.
- Drag directions and perceived point-under-pointer matching at the center and edges.
- Immediate reversal after reaching every boundary edge.
- Intended decorative margin visible outside the Grid.
- Draft Window modal exclusion.
- Pending Draft item drag exclusion.
- Tower placement interaction exclusion.
- Common mobile aspect ratios and editor Game-view sizes.

Static build and document validation do not prove Cinemachine setup, Collider-plane compatibility, Camera feel, or Play Mode lifecycle behavior.
