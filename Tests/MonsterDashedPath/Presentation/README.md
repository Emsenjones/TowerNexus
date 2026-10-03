# Monster dashed-path presentation contracts

Run from the repository root:

```sh
python3 Tests/MonsterDashedPath/Presentation/run.py
```

The harness compiles the actual presenter and snapshot against controlled Unity
renderer, transform, Map, node, material, and clock doubles. It checks geometry,
state, clear/rebind, world-coordinate resubmission, authored width preservation,
flow phase, pause/resume, frame subdivisions, invalid input, and asset ownership.
It does not execute native Unity transforms, mesh generation, URP, Shader import,
Camera rendering, actual Draft pause, input, Battle lifecycle, or device behavior.

## Deferred asset setup

Create the Material/Shader and Prefab after functional code completion:

- Put `MonsterDashedPathPresenter` on the Prefab root. Assign its LineRenderer
  on that root or a child explicitly. Configure Width directly on LineRenderer;
  the presenter preserves its width curve/multiplier. The presenter controls
  world-space positions, TransformZ orientation toward `NodesRoot.up`, Tile UVs
  with neutral `textureScale = (1, 1)`, sharp corners, no caps, state Color/Alpha,
  and shadows. Configure Normal Color and Blocked Color on the Presenter; each
  includes its own Alpha, defaulting to 0.5 (white Normal / red Blocked).
  The old Preview Alpha/Opacity field and LineRenderer color-gradient baseline
  are removed. Check the new fields on existing Prefabs after Unity imports.
- Use a transparent URP Shader exposing `_BaseMap` (Texture), `_FlowSpeed`
  (float), and `_FlowOffset` (float). Supply a Repeat Texture with opaque dash
  and transparent gap regions. Texture alpha proportions define dash/gap shape;
  Material `_BaseMap` Tiling defines repeat density in world space.
- Shader sampling contract: apply `_BaseMap_ST` once to UV0, then subtract
  `_FlowOffset` from the resulting U coordinate; sample `_BaseMap` and multiply
  by vertex RGBA. Do not add `_Time` animation or further period scaling.
- `_FlowSpeed` is finite/nonnegative, in texture cycles per simulation second;
  zero disables flow. `_FlowOffset` defaults to zero in the Material and is
  overridden per instance by the presenter. Increasing offset makes the pattern
  move from the first route node (Spawn) toward the last (Target).
- Flow uses scaled delta time and explicitly freezes at timeScale zero, including
  the frame Draft acquired pause. It resumes at the same phase; ordinary route
  and state updates preserve phase, while Clear and new binding reset it.
- State Color RGBA must be finite, with Alpha in [0, 1]. Height is a finite
  world-unit offset along the Map normal. Presenter owns these, not duplicate
  width or dash/gap fields.
- Do not add colliders or Canvas/raycast interaction. Check corner joins,
  endpoints, ground visibility, transparency, pattern density, flow direction,
  both states, real Draft pause/resume, Camera pan, and target builds natively.
- Asset/property validation cannot verify transparent gaps or correct UV/vertex
  RGBA consumption. Those remain mandatory visual checks. Missing required assets
  or properties are failures, not a solid-line fallback.

Task003 will provide the Prefab reference to the Battle owner and create one
instance under the Map presentation root. It owns topology freshness and Battle
publication authority. Presenter checks Map/node identity, not walkability.
Resubmit the retained route/state on Map spatial changes, even for the same
snapshot. Do nothing for Camera pan. Clear on stop; destroy on release.
A cleared presenter keeps its Map binding; failed initialization revokes it.

Asset creation, Inspector wiring, import/Play Mode, and live integration remain
pending. Managed tests cannot accept these items.
