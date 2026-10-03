# Task002 - Monster Dashed Path Presentation

Document Set: Task
Status: Functional presenter implemented; static/managed validation recorded below.
Shader source and authored assets are available; Unity visual acceptance remains
pending. Task003 implements live wiring; its scene binding and native acceptance
are recorded separately.
Dependencies: [Task001 - Monster Path Route Queries](Task001_MonsterPathRouteQueries.md).
Next: [Task003 - Monster Dashed Path Integration](Task003_MonsterDashedPathIntegration.md).

## 1. Goal

Render a supplied Monster main route as one readable world-space dashed
line using an authored Prefab with a world-space LineRenderer, with Normal
and Blocked presentation. The renderer
consumes an ordered route and requested state; it does not choose paths,
validate placement, or own the drag state machine.

## 2. Source Documents

- [Battle HUD UI System](../System/04_BattleHUDUISystem.md), Section 6.1: dashed-line visual and input contracts.
- [Monster System](../System/07_MonsterSystem.md), Section 6.3: main route and Grid-center interpretation.
- [Tower Placement System](../System/09_TowerPlacementSystem.md), Section 7.5: supplied display states and retained-route rules.
- [Map System](../System/05_MapSystem.md), Sections 2 and 9: coordinate frame and center positions.

## 3. Scope

- Add a Map-space presentation component owned by Battle HUD UI presentation,
  using a Prefab with a world-space LineRenderer rather than screen-space Canvas
  geometry.
- Render supplied ordered route geometry from Spawn center to Target center,
  following intermediate Grid centers and orthogonal corners.
- Apply the two requested visual states without deriving gameplay validity.
- Leave width curve/multiplier to LineRenderer authoring and dash/gap pattern
  and repetition density to Material/Texture authoring. Presenter configuration
  retains only the explicit renderer reference, Normal/Blocked RGBA colors, and surface height.
- Animate texture flow from Spawn toward Target using Material-authored speed
  and scaled battle time, preserving phase across route/state updates and pause.
- Support explicit route replacement, state-only changes, and complete clearing.
- Define the explicit Prefab/material references and setup instructions needed
  for the actual project render pipeline and target builds. Complete functional
  code first; create/configure the dashed-line Material and LineRenderer Prefab
  afterward, before native visual acceptance.

## 4. Out Of Scope

Route computation, preview-cache ownership, drag-state decisions, committed
route selection, Stage orchestration, lane/connector/relocation visualization,
animation beyond the agreed texture flow, arrows, smoothing curves, physics interaction, global VFX
frameworks, and unrelated HUD or Tower visual refactoring.

## 5. Ownership And Integration Boundary

Battle HUD UI System owns presentation responsibility; this does not require
a screen-space Canvas or rendering inside the HUD hierarchy. The line is a
separate Map-space Prefab instance, not a child of the Camera or screen-space
Canvas. Tower Placement System supplies the selected route and state through a narrow
presentation boundary. Task003 connects that boundary to gameplay interaction.

The route input comes from Task001's reviewed data contract. The renderer must
not discover a Map, call A*, inspect deployment legality, or change state based
on Tower color. Map spatial context and required assets are supplied explicitly.
An isolated supplied-route fixture may exercise the renderer before live
interaction wiring is completed.

## 6. Required Implementation Contract

| Requested State | RGB Color | Alpha |
|---|---|---|
| Normal | `normalColor` RGB; default white | `normalColor.a`; default 0.5 |
| Blocked | `blockedColor` RGB; default red | `blockedColor.a`; default 0.5 |

1. A state-only transition to Blocked preserves the exact supplied
   route geometry. Consecutive blocked-state requests do not create a new
   route or replace its shape.
2. The line follows Grid centers in the Map coordinate frame; visual height
   does not change node-center topology. Translated or rotated Map/NodesRoot
   transforms and authored Node Size remain supported. Use world-space
   LineRenderer positions (`useWorldSpace = true`), set `positionCount` to the
   supplied route node count, and update positions through `SetPositions` or
   equivalent explicit node updates. Camera pan alone requires no route query
   or node-position update; Camera projection keeps the line aligned with Map
   geometry.
3. Dashes remain readable along straight segments and around corners. Corners
   cannot cut across cells outside the supplied route. Dash spacing and
   endpoint treatment are reviewed visually, without assuming one Grid equals
   a fixed world-unit distance. A repeating transparent Material/texture or
   Shader supplies the dash pattern; LineRenderer geometry alone is not dashed.
   Texture alpha defines dash/gap proportions; Material tiling defines density.
   The authored pattern remains stable in world units when route length changes.
4. Authored width multiplier and texture tiling must be finite and positive;
   both state colors have finite RGBA channels and Alpha in [0, 1]; surface height is finite.
   Material flow speed is finite and nonnegative. Report unusable configuration
   or assets without silently rewriting authoring. Do not overwrite authored
   LineRenderer width curve/multiplier.
5. Rendering introduces no collider, occupancy, or input-blocking surface.
   It does not intercept Tower dragging, Camera pan, or modal Draft input.
6. Route replacement replaces previous geometry. Clearing removes visible
   geometry and retained presentation references; resources created by the
   renderer have explicit cleanup ownership. Task003 owns explicit Prefab
   instantiation/binding for the current Battle and destruction on release;
   the presenter owns only its rendering resources and supplied presentation data.
7. An unchanged route/state does not require recreating presentation objects
   every frame. No pooling framework is required for the single displayed line.
8. Flow changes only an instance Shader offset, never route geometry or the
   shared Material. All states flow toward Target. Draft pause freezes phase;
   resume continues it. Route/state updates preserve phase. Clear/new binding
   resets phase; speed zero disables motion.
9. Presentation failure is diagnosable and does not change deployment legality,
   mutate Monsters, or roll back an accepted gameplay result.

## 7. Unity Authoring Checklist

- The approved renderer is a Prefab with a world-space LineRenderer and a
  dedicated transparent textured Material. The Shader must follow the explicit
  URP property/UV contract below; texture shape and visual tuning remain authored.
- Implement functional route/state/clear behavior first. Afterward, create the
  Material and LineRenderer Prefab, configure their references and visual
  settings, and perform native visual acceptance. Asset authoring is not
  automatically delegated by completing the functional code.
- Store the renderer reference, Normal/Blocked RGBA colors, and visual height on the
  Presenter. Configure Width on LineRenderer; configure texture alpha shape,
  repeat density, and flow speed on Material/Texture. Save these in the Prefab
  and referenced assets.
- Supply the Prefab reference explicitly to the current Battle lifetime owner;
  instantiate once for that Battle under the Map presentation root. Do not
  search the scene for a replacement or reload/recreate it on each route update.
- Establish the presentation owner and explicit binding to the current Map's
  supplied spatial data; avoid hierarchy-search fallback for required owners.
- Ensure the material supports configured state tint and actual transparency under
  the project's render pipeline and is included in target builds.
- Configure positive width/tiling, a Repeat Texture with transparent gaps,
  finite nonnegative flow speed, Normal/Blocked RGBA colors, and visual height.
- Check visibility against campaign Tile surfaces, Spawn/Target features,
  Towers, and the normal battle Camera angles.
- Confirm the layer and renderer introduce no raycast or collision authority.
- Preserve asset metadata and list every required scene/prefab reference.

Inspector wiring, Unity import/reserialization, and Play Mode remain user-owned
unless explicitly delegated. Missing wiring is an explicit pending acceptance
item, not a reason to claim that the visual is complete.

## 8. Acceptance Criteria

- A supplied straight, cornered, and multi-turn route is displayed faithfully,
  with exact endpoint alignment and readable dashes.
- Normal applies Normal Color RGBA for formal and valid candidate routes;
  Blocked applies Blocked Color RGBA. Neither reads the LineRenderer's authored
  color gradient or uses a separate Preview Alpha/Opacity field.
- Switching a visible route from valid to blocked changes only its visual state
  and leaves geometry unchanged across repeated blocked updates.
- Supplying another route replaces the old geometry; clear and recreation
  leave no duplicate or stale line.
- Map transforms and Node Size do not detach the line from the route. Camera
  pan preserves alignment without screen-coordinate synchronization or
  Camera-triggered route recomputation.
- Route length changes preserve the authored dash/gap pattern and density.
- All states flow Spawn-to-Target at frame-rate-independent speed, without
  geometry writes. Pause/resume preserves phase; clear/new binding resets it;
  speed zero stops flow. Width and shared Material/Texture settings remain intact.
- Tower drag, Camera pan, and modal input remain unaffected.
- Actual material/shader behavior is verified in Unity, including target-build
  asset availability where required; C# compilation alone cannot accept it.

## 9. Validation And Evidence

- Compile the runtime assembly with the established static gate from Task001.
  Compile the Editor assembly too if Editor authoring support is changed.
- Verify deterministic geometry and state-only update behavior with appropriate
  focused fixtures where meaningful.
- Perform native visual inspection of both states, cornering, transformed
  Maps, surface visibility, clearing, and input behavior.
- Check shader/material import and target-build inclusion through Unity checks;
  retain any unexecuted build/device check as a named acceptance limit.
- Run `git diff --check` on changed files.
- Report static, fixture, Editor/import, Play Mode, and build/device evidence
  separately. Do not describe supplied-route fixture success as completed live
  gameplay integration.

## 10. Review And Status

Functional presenter code is implemented in
`Assets/Scripts/TowerDeployment/MonsterDashedPathPresenter.cs`. Prefab +
world-space LineRenderer is the approved rendering approach. Functional code
precedes Material/Prefab authoring; until
those assets are authored, wired, and visually verified, visual acceptance is
pending. Task003 owns live route/state wiring, Prefab instance creation and
cleanup, and Battle lifetime.
Completion requires the native visual evidence above, not merely a working
presentation API.

### Implemented Presentation Boundary

- `TryInitialize` clears previous display/cache and revokes the old binding
  before validation. Failed initialization leaves the presenter uninitialized.
- `TrySetRoute` checks supplied Map/node identities and finite ordered geometry,
  copies final world positions, and commits the complete line. Topology freshness
  and Battle authority remain Task003 responsibilities; no A* or walkability
  query is introduced.
- `TrySetState` changes color/Alpha only. Invalid requests clear the display and
  return a reason. `Clear` retains the Map binding/configuration but removes
  geometry, state, and deduplication cache; it cannot be undone by state alone.
- The ribbon uses `TransformZ`, with renderer world Z facing `NodesRoot.up` and
  its in-plane orientation derived from `NodesRoot.forward`. Final positions use
  `node.WorldPosition + NodesRoot.up * surfaceHeight`. Map spatial changes require
  Task003 to resubmit the retained route/state; Camera pan does not.
- LineRenderer uses Tile UVs with `textureScale = (1, 1)`. Presenter never
  overwrites authored width curve/multiplier. Material/Texture alone owns dash
  shape and tiling. Required assets/parameters and finite coordinates are checked.
- `Update` advances a bounded instance phase using scaled `Time.deltaTime`,
  with an explicit `Time.timeScale == 0` freeze for the frame Draft acquires
  pause. It never sets timeScale or repositions geometry for animation.
- The dedicated Shader must expose `_BaseMap` (Texture), `_FlowSpeed` (float,
  texture cycles per simulation second), and `_FlowOffset` (float, instance phase).
  `_BaseMap` must use Repeat wrapping and contain transparent gaps. Sampling is
  `uv = UV0 * _BaseMap_ST.xy + _BaseMap_ST.zw`, then `uv.x -= _FlowOffset`.
  Multiply sampled RGBA by vertex RGBA. Do not add Shader `_Time` animation or
  another period scaling. Increasing phase therefore moves the pattern along
  ordered nodes toward Target. Material `HasProperty` cannot verify this behavior;
  native visual acceptance is mandatory.
- `_FlowOffset` is supplied by a MaterialPropertyBlock; shared assets are never
  mutated. Clear/reinitialize resets phase, while route/state updates preserve it.

### Validation Evidence

- `Tests/MonsterDashedPath/Presentation/run.py`: 53 managed contract cases passed
  against the production presenter/snapshot with controlled Unity doubles.
- Runtime assembly compilation passed with zero warnings/errors, using the
  current generated project, confirmed to include the new production sources.
- Changed-file whitespace checks passed.
- These checks do not execute native Unity rendering/transforms, URP, Camera
  projection, input, or Battle lifecycle. Editor/import, Play Mode, shader and
  Prefab acceptance, live integration, and build/device evidence remain pending.
- Deferred asset setup and harness limitations:
  [Presentation README](../../Tests/MonsterDashedPath/Presentation/README.md).


### Configurable State Colors

`MonsterDashedPathState` contains only `Normal` and `Blocked`. Presenter exposes
`normalColor` (Normal Color) and `blockedColor` (Blocked Color); each controls
uniform RGB and Alpha along the route. Defaults are white and red respectively,
both at Alpha 0.5. Configure each Color's Alpha directly in the Inspector.
`previewAlpha`/`previewOpacity` and the authored-gradient cache are removed.
Existing Prefabs require checking these new Color fields after Unity imports;
old Preview Alpha and LineRenderer gradient values are not migrated into them.
LineRenderer still owns width; Material/Texture still owns pattern and flow speed.
Same-state Color edits are applied on the next route/state submission without
rewriting geometry or resetting flow. Clear/rebind uses the configured colors.
Normal does not distinguish formal from hypothetical route ownership; Session
retains the drag token, formal route, and last valid candidate independently.
