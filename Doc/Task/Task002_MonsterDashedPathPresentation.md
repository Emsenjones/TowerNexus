# Task002 - Monster Dashed Path Presentation

Document Set: Task
Status: Planned; implementation and runtime acceptance have not begun.
Dependencies: [Task001 - Monster Path Route Queries](Task001_MonsterPathRouteQueries.md).
Next: [Task003 - Monster Dashed Path Integration](Task003_MonsterDashedPathIntegration.md).

## 1. Goal

Render a supplied Monster main route as one readable static world-space dashed
line with Solid, Valid Preview, and Blocked Preview presentation. The renderer
consumes an ordered route and requested state; it does not choose paths,
validate placement, or own the drag state machine.

## 2. Source Documents

- [Battle HUD UI System](../System/04_BattleHUDUISystem.md), Section 6.1: dashed-line visual and input contracts.
- [Monster System](../System/07_MonsterSystem.md), Section 6.3: main route and Grid-center interpretation.
- [Tower Placement System](../System/09_TowerPlacementSystem.md), Section 7.5: supplied display states and retained-route rules.
- [Map System](../System/05_MapSystem.md), Sections 2 and 9: coordinate frame and center positions.

## 3. Scope

- Add the display capability owned by Battle HUD UI presentation.
- Render supplied ordered route geometry from Spawn center to Target center,
  following intermediate Grid centers and orthogonal corners.
- Apply the three requested visual states without deriving gameplay validity.
- Supply narrowly scoped configurable line width, dash length, gap length,
  preview opacity, and surface-relative height.
- Support explicit route replacement, state-only changes, and complete clearing.
- Provide the authoring assets and setup instructions needed for the actual
  project render pipeline and target builds.

## 4. Out Of Scope

Route computation, preview-cache ownership, drag-state decisions, committed
route selection, Stage orchestration, lane/connector/relocation visualization,
flowing animation, arrows, smoothing curves, physics interaction, global VFX
frameworks, and unrelated HUD or Tower visual refactoring.

## 5. Ownership And Integration Boundary

Battle HUD UI System owns rendering even though the line is world-space and
may use a separate presentation object from the screen-space HUD. Tower
Placement System supplies the selected route and state through a narrow
presentation boundary. Task003 connects that boundary to gameplay interaction.

The route input comes from Task001's reviewed data contract. The renderer must
not discover a Map, call A*, inspect deployment legality, or change state based
on Tower color. Map spatial context and required assets are supplied explicitly.
An isolated supplied-route fixture may exercise the renderer before live
interaction wiring is completed.

## 6. Required Implementation Contract

| Requested State | RGB Color | Alpha |
|---|---|---|
| Solid | White | 1 |
| Valid Preview | White | Configured semi-transparent value; default 0.5 |
| Blocked Preview | Red | The same value as Valid Preview |

1. A state-only transition to Blocked Preview preserves the exact supplied
   route geometry. Consecutive blocked-state requests do not create a new
   route or replace its shape.
2. The line follows Grid centers in the Map coordinate frame; visual height
   does not change node-center topology. Translated or rotated Map/NodesRoot
   transforms and authored Node Size remain supported.
3. Dashes remain readable along straight segments and around corners. Corners
   cannot cut across cells outside the supplied route. Dash spacing and
   endpoint treatment are reviewed visually, without assuming one Grid equals
   a fixed world-unit distance.
4. Width, dash length, and gap length are positive; preview Alpha is strictly
   between 0 and 1. Report unusable configuration or assets without silently
   rewriting authoring.
5. Rendering introduces no collider, occupancy, or input-blocking surface.
   It does not intercept Tower dragging, Camera pan, or modal Draft input.
6. Route replacement replaces previous geometry. Clearing removes visible
   geometry and retained presentation references; resources created by the
   renderer have explicit cleanup ownership.
7. An unchanged route/state does not require recreating presentation objects
   every frame. No pooling framework is required for the single displayed line.
8. Presentation failure is diagnosable and does not change deployment legality,
   mutate Monsters, or roll back an accepted gameplay result.

## 7. Unity Authoring Checklist

- Review the concrete renderer and material approach before implementation;
  the design does not prescribe LineRenderer, a mesh, or a particular Shader.
- Establish the presentation owner and explicit binding to the current Map's
  supplied spatial data; avoid hierarchy-search fallback for required owners.
- Ensure the material supports white/red tint and actual transparency under
  the project's render pipeline and is included in target builds.
- Configure width, dash length, gap length, preview opacity, and visual height.
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
- Solid is white and opaque; both preview states are equally semi-transparent,
  and Blocked Preview is red.
- Switching a visible route from valid to blocked changes only its visual state
  and leaves geometry unchanged across repeated blocked updates.
- Supplying another route replaces the old geometry; clear and recreation
  leave no duplicate or stale line.
- Map transforms and Node Size do not detach the line from the route.
- Tower drag, Camera pan, and modal input remain unaffected.
- Actual material/shader behavior is verified in Unity, including target-build
  asset availability where required; C# compilation alone cannot accept it.

## 9. Validation And Evidence

- Compile the runtime assembly with the established static gate from Task001.
  Compile the Editor assembly too if Editor authoring support is changed.
- Verify deterministic geometry and state-only update behavior with appropriate
  focused fixtures where meaningful.
- Perform native visual inspection of all three states, cornering, transformed
  Maps, surface visibility, clearing, and input behavior.
- Check shader/material import and target-build inclusion through Unity checks;
  retain any unexecuted build/device check as a named acceptance limit.
- Run `git diff --check` on changed files.
- Report static, fixture, Editor/import, Play Mode, and build/device evidence
  separately. Do not describe supplied-route fixture success as completed live
  gameplay integration.

## 10. Review And Status

Planned contract only. The concrete rendering, asset, and setup plan is reviewed
before coding. Task003 owns live route/state wiring and Battle lifetime.
Completion requires the native visual evidence above, not merely a working
presentation API.
