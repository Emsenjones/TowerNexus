# Task002 - Camera Pan And Authored Map Boundary

Status: Runtime implementation complete; Unity authoring and Play Mode acceptance pending

Depends on: Task001 for final Draft-modal interaction acceptance; Cinemachine installed through Unity Package Manager

## 1. Goal

Implement press-and-drag Camera pan over the Active Map with direct-manipulation movement:

```text
Pointer Drags Screen Content
    -> Battlefield Content Follows The Pointer
    -> Camera Translates In The Opposite World Direction
```

Movement must be derived from pointer rays intersecting the Active Map plane so the perceived drag distance remains consistent across resolution and Camera framing.

Each Map Prefab authors one child GameObject named `MapCameraBoundary`. That same GameObject owns one `MapCameraBoundary` adapter and one 3D BoxCollider used by Cinemachine Confiner 3D. Stage composition must ensure that the Collider from the newly created Map is bound before the Stage becomes ready, and must clear that binding safely during rollback, replacement, and release.

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

## 3. Implementation State

- Cinemachine 3.1.7 is installed through Unity Package Manager and referenced by the generated C# project.
- Runtime code now provides Map Camera authoring validation and scaffold creation, transactional Stage-to-Camera binding, identity-safe cleanup, direct-manipulation mouse/touch Pan, Confiner reconciliation, and explicit Tower Placement Camera validation.
- The dedicated `CameraBoundary` Layer is defined.
- `Prefab_GameRuntime`, the Main scene, and playable Map Prefabs still require the Unity authoring checklist in this task.
- The current repository has one playable StageDefinition using `Prefab_Map_Lv1`; the five-Stage content target remains pending.

## 4. Ownership

| Owner | Responsibility In This Task |
|---|---|
| Map Prefab | Own one `MapCameraBoundary` child GameObject with its adapter, BoxCollider, and deliberate Camera margin |
| MapGeneratorBehaviour | Hold the explicit adapter reference, create its default authoring scaffold when Inspector Generate finds it absent, and aggregate boundary validation |
| MapCameraBoundary | Validate and expose exactly one enabled 3D BoxCollider owned by its GameObject |
| StageCompositionController | Orchestrate validation, binding, commit, replacement, default-pose reset, and clearing of the Map boundary handoff |
| Camera Pan controller | Own gesture eligibility, ray-plane drag conversion, and logical pan position |
| Main Camera, Cinemachine Camera, and Confiner 3D | Produce the final Camera view and enforce the currently bound Map boundary |
| Game Flow | Supply whether battle-local Camera interaction is allowed |
| Draft and Battle HUD UI | Own modal Draft input and held-item drag gestures |
| Tower Placement | Own an accepted placement gesture until release or cancellation |

Stage composition orchestrates the boundary handoff but does not calculate Camera movement. Camera runtime enforces the Collider but does not own Map creation or destruction.

## 5. Cinemachine Runtime Contract

Use the installed Cinemachine 3.1.7 APIs in the `Unity.Cinemachine` namespace.

`Prefab_GameRuntime` owns the complete gameplay Camera rig:

- One Main Camera tagged `MainCamera`
- One Cinemachine Brain on that output Camera
- One active Cinemachine Camera view
- One Cinemachine Confiner 3D on that Cinemachine Camera
- The Cinemachine Camera transform as the sole logical Pan transform, with no separate Pan target
- A single authoritative output Camera used by both Camera Pan raycasts and existing battlefield interaction

Remove the old Main-scene Camera after the prefab-owned Main Camera is authored. The runtime must contain exactly one active Main Camera, Audio Listener, and Cinemachine Brain.

The Camera Pan controller, Stage composition, and Tower Placement use stable serialized references to the same output Camera. Do not dynamically search the scene every frame.

The Cinemachine Camera's Orthographic Size must be greater than zero. Tower Placement exposes one serialized `placementCamera`, has no `Camera.main` fallback, validates that reference before Battle, and uses the same output Camera as Camera Pan.

## 6. Map Prefab Boundary Contract

Every playable Map Prefab uses this conceptual hierarchy:

```text
Map Root
├── NodesRoot
├── MapCameraBoundary
│   -> MapCameraBoundary adapter
│   -> BoxCollider
└── CameraDefaultPose
```

`MapGeneratorBehaviour` holds explicit serialized references to the child `MapCameraBoundary` adapter and direct-child `CameraDefaultPose`. Both references are exposed read-only to Stage composition. The adapter uses `RequireComponent(BoxCollider)` and exposes the sole BoxCollider on its own GameObject.

When the Inspector `Generate Map` action finds that explicit adapter reference missing, it first checks direct children without changing them:

1. Exactly one valid direct-child `MapCameraBoundary` adapter: assign that existing adapter.
2. More than one candidate or one invalid candidate: report the ambiguity or invalid authoring and stop without creating another.
3. No candidate: create one direct child GameObject named `MapCameraBoundary`.
4. Attach `MapCameraBoundary`; its required BoxCollider is retrieved rather than added a second time.
5. Assign that sole BoxCollider to the adapter.
6. Configure the BoxCollider as a trigger on the dedicated Camera-boundary Layer.
7. Assign the new adapter to `MapGeneratorBehaviour`.
8. Create or adopt one unambiguous direct-child `CameraDefaultPose` when its reference is absent.
9. Continue complete Camera preflight and normal Grid generation.

The default pose starts at local position `(0, 10, -6)` and local rotation `(60, 0, 0)`. It stays independent from runtime Camera movement and must lie inside the boundary.

Automatic scaffold creation occurs only through the explicit Inspector Generate action. The reusable programmatic `GenerateMap()` path and `ValidateMap()` never silently create, assign, or repair Camera authoring. Complete Camera validation occurs before Grid clearing. Generate, Clear, and visual refresh preserve the boundary and default pose.

The designer remains responsible for sizing and positioning the generated BoxCollider. Grid dimensions cannot determine its final volume because the intended Camera reference-position region may include authored decorative margin.

Map-level validation owns explicit-reference and candidate cardinality. It scans inactive descendants and rejects a reserved-name child without its adapter, a wrong-name or non-direct adapter, multiple adapters or reserved-name children, missing or external references, and an invalid default pose. Adapter-local validation owns the same-GameObject BoxCollider structure and rejects missing or multiple Colliders, disabled or inactive state, degenerate volume, non-trigger configuration, and the wrong Layer.

The boundary:

- Is presentation-only and must not affect Monster collision, pathfinding, Tower placement, or runtime occupancy.
- Is a trigger on a dedicated non-gameplay Layer excluded from gameplay physics queries.
- May extend outside the Grid gameplay footprint to show surrounding decoration.
- Is saved in the Map Prefab and instantiated with that Map.
- Defines permitted Camera reference positions rather than the rectangular ground footprint.
- Must contain the authored default Camera position and support the complete intended Pan region.

## 7. Stage Composition To Confiner Handoff

The current candidate Map is created through:

```text
StageCompositionController.TryCreateCandidateMap(selectedStage)
```

After `TryCreateCandidateMap()` succeeds and before remaining Battle-runtime preparation begins, Stage composition must stage the validated candidate boundary. Camera commit then occurs only after the remaining Battle runtime is prepared and before Stage composition publishes readiness. This ordering keeps the binding inside the existing preparation transaction and gives every failure path the correct owned boundary to clear.

After the candidate Map instance and its `MapGeneratorBehaviour` pass Map validation, Stage composition obtains `candidateMap.CameraBoundary` and `candidateMap.CameraDefaultPose` and pushes the exact triple into Camera runtime:

```text
MapGeneratorBehaviour
    -> Serialized MapCameraBoundary Reference
    -> Serialized CameraDefaultPose Reference
    -> StageCompositionController Creates Candidate Map
    -> Candidate Map ValidateMap
    -> Candidate Map CameraBoundary And CameraDefaultPose
    -> CameraPanController TryStageMapBinding
    -> Assign BoxCollider To CinemachineConfiner3D BoundingVolume
    -> Confirm CinemachineConfiner3D IsValid
    -> Prepare Remaining Battle Runtime
    -> Deferred-Release Check
    -> CameraPanController TryCommitStagedMapBinding
    -> Reset Authored Default Pose
    -> Stage Composition Reports Ready
    -> Keep Camera Input Disabled Until The Stage Is Committed And Battle Allows It
```

Stage preparation succeeds only when this boundary handoff succeeds. A valid gameplay Grid with an unusable Camera boundary is not a prepared Stage.

Camera runtime never searches for the current Map or boundary. Stage composition is the only handoff orchestrator. Do not let `StageCompositionController` perform pan math.

## 8. Transactional Binding And Cleanup

The Confiner binding participates in the same preparation transaction as the candidate Map.

### 8.1 Candidate Failure Or Deferred Release

If any later Battle-runtime preparation step fails, throws, or receives a deferred release:

1. Disable Camera interaction and clear the exact candidate Map-boundary-default-pose identity.
2. Clear `CinemachineConfiner3D.BoundingVolume` only when the staged or active owner still matches the candidate boundary.
3. Disable the Confiner.
4. Reset Camera gesture, logical pan state, and default-pose references.
5. Destroy the candidate Map.

The Confiner must never retain a reference to a destroyed candidate Collider.

### 8.2 Successful Commit

When `CommitCandidateStage()` succeeds:

- The staged boundary becomes the Active Map boundary.
- Camera Pan state resets to the authored default pose relative to the new Map Root/framing origin.
- Camera input remains governed by Game Flow and interaction-owner eligibility.
- No outgoing Map boundary remains assigned.

### 8.3 Stage Replacement Or Release

`ReleaseStageRuntimeCore()` clears candidate and active Camera bindings before Battle runtime cleanup and before disabling or destroying either Map object.

Clear is conditional on owned identity: a late cleanup from an older candidate must not remove a newer committed boundary.

Release and repeated clear calls are idempotent. After release, no Camera pan is accepted until another valid Active Map boundary is committed.

## 9. Initial Framing

When a candidate becomes the Active Map:

- Restore the Cinemachine Camera transform from the instantiated Map's exact `CameraDefaultPose.position` and `.rotation`.
- Preserve its authored Camera rotation, projection, height, and framing offset unless a later design explicitly changes them.
- Allow Cinemachine Confiner 3D to resolve the final valid Camera pose.
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

The Active Map plane passes through `candidateMap.NodesRoot.position` with normal `candidateMap.NodesRoot.up`. Apply translation on that plane directly to `CinemachineGameplayCamera.transform`, the sole logical Pan transform. This makes battlefield content follow the pointer without a fixed world-units-per-pixel speed scalar.

If either ray cannot intersect the Map plane, ignore that sample and reset the sample baseline safely.

The ray source must be the authoritative final gameplay Camera so the calculation matches what the player sees.

## 11. Confiner And Logical Position Reconciliation

Cinemachine may constrain the rendered Camera after the logical Cinemachine Camera transform moves.

Set Confiner 3D Slowing Distance to zero. After the exact serialized Cinemachine Brain updates, read the confined output Camera position, reconcile the Cinemachine Camera transform to that accepted position, and rebuild the active pointer-to-Map-plane baseline. Ignore updates from every other Brain.

Use `BoxCollider.ClosestPoint(defaultPose.position)` with a small tolerance for authored default-pose containment. `CinemachineConfiner3D.IsValid` is only the final Cinemachine acceptance check; the Map adapter remains responsible for structural validation.

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
- The configured EventSystem exists and is the active UI event-routing authority.

Ownership is fixed at pointer press:

- A press beginning over interactive UI never becomes Camera Pan after leaving UI.
- A press owned by a held Draft item remains owned by it through release or cancellation.
- A Camera Pan entering a UI region remains Camera-owned until release unless the gesture is cancelled by Battle or Stage lifecycle.

The internal primary-pointer model supports the mouse pointer and one touch `fingerId`, including press, movement, release, and cancellation. The exact accepted identity owns the gesture and supplies UI rejection; all other pointers are ignored until that gesture ends. A missing EventSystem rejects Camera Pan and is invalid runtime authoring. Multi-touch, pinch zoom, and simultaneous gesture transfer are deferred.

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
- The Camera-boundary adapter or its BoxCollider is missing, ambiguous, externally owned, disabled, inactive, or degenerate.
- The BoxCollider is not a trigger on the dedicated Camera-boundary Layer.
- Cinemachine Confiner 3D is missing or rejects the assigned Bounding Volume.
- The authored default Camera position lies outside the assigned volume.

Failure is technical and result-neutral. It must not start Battle, publish Victory or Defeat, or silently continue with an unconstrained Camera.

## 14. Out Of Scope

- Cinemachine package installation or version changes
- 2D Camera boundaries and Cinemachine Confiner 2D
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

- Keep Cinemachine 3.1.7 installed and compile against its concrete APIs.
- Move ownership of the Main Camera into `Prefab_GameRuntime`.
- Add Cinemachine Brain to the prefab-owned Main Camera.
- Configure one Cinemachine Camera and one Cinemachine Confiner 3D.
- Author one direct-child `CameraDefaultPose` on every Map outside the moving Camera hierarchy.
- Assign stable Main Camera, Cinemachine, Stage, Game Flow, Battle HUD, and Tower Placement references on the Game runtime prefab.
- Remove the previous Main-scene Camera and confirm exactly one Main Camera and Audio Listener remain.
- Confirm Inspector Generate creates the default `MapCameraBoundary` plus BoxCollider scaffold for new Map authoring when absent.
- Put every boundary on a non-gameplay Layer and exclude it from gameplay physics queries.
- Size and position each BoxCollider for the legal Camera reference-position region and desired decorative margin.
- Add the combined boundary GameObject directly to the current reusable default Map and handcrafted `Prefab_Map_Lv1` without regenerating their Grid content; require every future playable Map to satisfy the same contract.
- Configure a positive Cinemachine Camera Orthographic Size.
- Bind Tower Placement explicitly to the same output Camera.

## 16. Acceptance Criteria

- Inspector Generate adopts one valid direct-child adapter when its explicit reference is absent, creates the combined adapter-plus-BoxCollider GameObject only when no candidate exists, and rejects ambiguous or invalid candidates without creating duplicates.
- Validate Map reports missing, ambiguous, invalid, externally owned, or misconfigured boundary authoring without silently repairing it.
- Every playable Map exposes exactly one direct-child `CameraDefaultPose` whose position lies inside its boundary.
- Every playable Map Prefab exposes exactly one valid 3D BoxCollider Camera boundary.
- Stage composition obtains the explicit boundary through `candidateMap.CameraBoundary` and pushes it into Camera runtime.
- Stage composition binds the new Map Collider to Cinemachine Confiner 3D before Stage preparation can report ready.
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

- Build `Assembly-CSharp.csproj` without restoring packages after Unity has regenerated project files for Cinemachine 3.1.7.
- Run path-scoped `git diff --check` for touched scripts, prefabs, package files, and documents.
- Search for per-frame scene-wide Camera, Map, or Collider discovery.
- Search every Camera binding path for a corresponding identity-safe clear path.
- Confirm only the approved 3D Confiner path exists.
- Confirm Camera Pan does not use a fixed world-units-per-pixel speed scalar.
- Confirm gameplay collision queries exclude the Camera boundary Layer.

## 18. Unity Play Mode Handoff

Verify in Unity:

- The current playable Map binds its own BoxCollider and confines the Camera; repeat this acceptance for each future playable Map.
- On a disposable or newly authored Map, Generate Map creates the combined adapter-plus-BoxCollider scaffold when absent and preserves an existing authored boundary on later generation. Do not regenerate `Prefab_Map_Lv1` merely to add the boundary.
- Validate Map reports null, missing, duplicated, external, disabled, degenerate, non-trigger, and wrong-Layer boundary cases.
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

Static build and document validation do not prove Cinemachine setup, BoxCollider sizing, Camera feel, or Play Mode lifecycle behavior.
