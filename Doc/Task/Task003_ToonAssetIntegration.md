# Task003 — Real Asset Integration And Visual Validation

Document Set: Task

Status: Ready for implementation-plan discussion; depends on Task001, Task002 and a representative asset.

## 1. Objective And Authority

Integrate an actual tower created from a Concept Reference into Unity. Combine it
with the completed two-tone Shader, dynamic outlines, and scene configuration to
validate a repeatable Tripo → Unity production workflow.

This task depends on [Task001](Task001_ToonShaderFoundation.md) and
[Task002](Task002_ToonOutline.md). See the [Task Index](README.md) for visual and
gameplay boundaries. Deliver one representative tower, ground, and minimal gameplay
integration; bulk art replacement and acceptance of the entire Vertical Slice are out of scope.

## 2. Asset Input Contract

| Input | Requirement |
|---|---|
| Reference | Identify the selected Concept Reference and record its most important silhouette, dominant colors, and line characteristics |
| Model | The FBX and associated textures are accessible; the silhouette and major form-defining planes are clear, with no obvious normal errors |
| UV / Textures | UVs match the base texture; remove fixed-direction shadows, reflections, and highlights as far as practical; static decorative lines may remain |
| Material Regions | Regions requiring independent palettes, such as the body, metal, and crystals, have separate material slots or parts |
| Size And Hierarchy | Define size, orientation, and Pivot; rotating parts have a usable hierarchy; scaling the model does not redefine its gameplay footprint |
| Type | The first acceptance asset is a static, opaque MeshRenderer asset; Normal Maps and separate lit/shadow texture sets are not required |

If Tripo produces only one material slot, organize the model according to its
palette requirements; do not switch to RGB masks as an ad hoc workaround.
Confirm that Unity resolves the FBX's embedded or externally referenced textures
before assigning custom materials. If substantial fixed lighting remains on the
model, first list the asset issues requiring regeneration or cleanup.

## 3. Scope And Deliverables

- The actual tower's Unity model/Prefab, separate materials, and texture references; retain the original imported assets for reimporting.
- A clear mapping table: model part → material → Base Texture → two-tone colors and outline parameters.
- A visual validation scene that can be reopened, using one ground surface, one directional light, and the actual orthographic viewing conditions.
- One representative gameplay integration and regression check following the existing Tower Framework.
- Reproducible instructions for asset import, material setup, lighting setup, and common issues.
- Reference images, Unity screenshots, user visual feedback, and results from the actual target device.

Without an actual asset, integration instructions and placeholder tests can proceed,
but actual-asset and final visual acceptance remain Pending.

## 4. Integration Requirements

1. Bind materials to the correct Renderer/material slots and share one Toon Shader instead of copying the Shader for each tower.
2. Confirm that the base-color texture's color space, filtering, and Mipmap settings preserve decorative lines at actual gameplay sizes.
   Close-up previews do not replace readability checks at a distance.
3. Adjust geometry, textures, lit/shadow colors, line width, and the main light against the reference; record whether each issue belongs to the asset or rendering.
4. Preserve existing `TowerModelRoot`, permanent-base, Attack Origin, and status-UI contracts.
   Do not change placement Anchors, collision/hit semantics, or combat values.
5. Check the existing `TowerVisualController` preview tinting, upgrade-target feedback, and model-replacement behavior,
   including per-material-slot MaterialPropertyBlock overrides and restoration. Do not contaminate materials shared by other towers.
6. If previews require transparency, explicitly choose between retaining existing preview materials and adding limited preview support.
   An Alpha property on the opaque Toon Shader does not establish working preview transparency.
   The implementation plan must confirm the actual requirement and minimum integration approach without silently dropping existing feedback.
7. Limit changes to the selected sample and necessary presentation integration; preserve dashed paths, range previews, HUD, and existing input behavior.

List the files and Prefabs involved in this integration in the implementation plan.
Record the origin of any existing functional issues found; keep unrelated fixes
separate from visual acceptance results.

## 5. Acceptance

- [ ] Imported assets, textures, materials, and Prefab references are complete, and results remain consistent after reimporting or reopening the scene.
- [ ] The original base texture remains readable, real-time lighting drives the two tones, and tinting does not erase decorative lines.
- [ ] From the actual gameplay view, the tower, ground, and shadows are clearly distinguishable, dominant colors remain restrained, and dynamic outlines work with texture lines.
- [ ] Model rotation, main-light changes, and overlapping objects produce no obvious fixed-lighting conflicts, outline cracks, or shadow artifacts.
- [ ] Representative tower placement, valid/invalid previews, upgrade-target feedback, and restoration after feedback all work correctly.
- [ ] References and material state remain correct after model replacement/destruction; status UI, Attack Origin, and gameplay behavior preserve their existing contracts.
- [ ] Compare the new rendering enabled and disabled in the same scene; record CPU/GPU frame times, draw calls/batches, and material counts.
- [ ] Validate a Player build on the project's target device; record device model, graphics API, resolution, Quality, visible-object count, and test duration.
- [ ] Define performance targets and test density before final acceptance; a single tower's Editor frame rate does not establish combat-performance acceptance.
- [ ] The user confirms the representative tower's visual direction; differences from the reference and deferred items are explicitly recorded.

## 6. Result Record

Complete the following records during development; all are currently Pending:

| Evidence Category | Required Record |
|---|---|
| Assets | FBX, texture, Prefab, and material paths; part mappings; model-cleanup steps |
| Rendering Environment | Unity/URP versions, Pipeline/Renderer, Quality, camera, main-light and shadow settings |
| Parameters | Base-texture tint, two-tone colors, threshold, boundary transition, outline color and width |
| Static / Import | Reference checks, Shader compilation, and reimport results |
| Play Mode | Lighting, occlusion, outline stability, gameplay integration, and state restoration |
| Player / Device | Actual build and runtime results, performance-sampling conditions and results; explicitly mark anything not performed |
| User Visual Acceptance | Date, reference images, actual screenshots, accepted differences, and unresolved issues |

On completion, synchronize stable asset-production and tuning contracts into
`Doc/Art/` and update the task status. Later additions of monsters, animations,
transparent effects, or new material categories require further sample acceptance;
this task's passing result does not automatically cover them.
