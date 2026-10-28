# Task001 — Two-Tone Shader Foundation

Document Set: Task

Status: Ready for implementation-plan discussion; not implemented.

## 1. Objective And Authority

Create a shared TowerNexus Toon Shader that uses one model and one UV/Base Texture
set to produce clear, adjustable lit and shadow tones under real-time lighting,
while preserving the texture's decorative lines and base colors.

The [Task Index](README.md) defines this round's scope and visual contracts.
This task delivers the material and lighting baseline. [Task002](Task002_ToonOutline.md)
owns dynamic outlines, and [Task003](Task003_ToonAssetIntegration.md) owns actual-asset
and gameplay integration.

## 2. Inputs And Deliverables

Inputs may be placeholder models with UVs and correct Mesh normals; Tripo assets
are not a prerequisite.

Deliverables:

- A shared Toon Shader, or equivalent Shader Graph, compatible with the project's URP version.
- A small set of sample materials demonstrating separate two-tone palettes for the tower body, metal, and crystals; all use the same Shader.
- A dedicated validation scene with an orthographic camera, directional light, ground, planar/curved/hard-edge samples, and occlusion test objects.
- Brief instructions covering parameter meanings, defaults, adjustment order, required Renderer settings, and known limitations.
- Required Unity-generated `.meta` files and explicit asset references so the result is reproducible after reopening the project.

Work primarily in the dedicated validation scene. This task does not replace the
main level, runtime Prefab, or all existing materials.

## 3. Required Material Behavior

| Item | Requirement |
|---|---|
| Base Texture | Use one Base Texture, defaulting to white. Lit and shadowed regions share one UV set; do not author separate lit and shadow textures |
| Two-Tone Palette | Each material independently controls its lit and shadow colors; base-color variation and static decorative lines remain readable in both tones |
| Lighting Classification | Use surface normals and the main-light direction in the correct coordinate space, together with real-time shadow occlusion; surface orientation alone is insufficient |
| Boundary Control | Provide an adjustable lighting threshold and a controlled narrow transition; broad regions retain flat two-tone shading |
| Cast Shadows | Test objects cast shadows onto the ground and receive shadows from other test objects; avoid applying darkening twice |
| Shared Code | Different materials and models reuse the Shader; changing one material's palette does not affect other materials |
| Runtime Tint | Provide overall tinting compatible with the existing `_BaseColor` path, affecting both lit and shadowed regions; the neutral default leaves colors unchanged |
| Normals | Do not sample a Normal Map in the first version; preserve the Mesh's major hard edges and appropriate smoothing |

Document exactly how the base texture combines with the two-tone colors. For example,
if using multiplication, identify the parameters as tint multipliers; do not claim
that they can precisely remap any complex texture to a target palette.
Validate with clean textures first. Record lighting already painted into textures
as an asset issue.

Existing color controls also write Alpha. Exposing Alpha does not automatically
make the base opaque Shader support transparency. Task003 must explicitly address
preview transparency rather than assume compatibility.

## 4. Scope Boundaries

The first version uses one main directional light. Define how ambient light,
light color, and post-processing affect the final palette. Prevent default PBR
highlights, environment reflections, or duplicate lighting from disrupting the
two-tone result. Determine the control strategy in the implementation plan and
include it in the tuning instructions.

Defer dynamic outlines, a third highlight tone, PBR metallic reflections,
transparent crystals, additional-light blending, baked lighting, Normal Maps,
and RGB material-region masks. Do not modify existing dashed-path, UI, particle,
or other third-party Shaders.

## 5. Implementation Plan Must Resolve

1. HLSL versus Shader Graph, and the integration of the main light, shadows, and Player Shader variants.
2. Explicit combination semantics and defaults for the Base Texture, two-tone palette, and `_BaseColor`.
3. How shadow occlusion combines with orientation-based tone selection, including threshold behavior and the fallback when no valid main light exists.
4. Required visible-surface, shadow-casting, depth, and other Passes for the current URP version; do not commit to a fixed file structure in advance.
5. The prototype scene's Pipeline/Renderer configuration and its scope of effect, avoiding unintended changes across all Quality levels.
6. Depth/normal capabilities supplied to Task002, and material/runtime-tint contracts handed to Task003.

## 6. Acceptance

- [ ] Unity import and Shader compilation produce no related errors; material references remain valid after reopening the scene.
- [ ] Missing textures, solid-color textures, and textures with decorative lines all render correctly.
- [ ] Rotating the main light moves the same surface appropriately between the two tones; rotating the model produces the correct normal response.
- [ ] High-frequency texture detail does not fragment planar surfaces into numerous lighting patches; curved and hard-edge models retain readable volume.
- [ ] Shadows update when occluders move; surfaces facing the light can enter the shadow tone when occluded.
- [ ] Objects cast correct shadows onto the ground; repeated darkening does not make shadows unreadable.
- [ ] Two material slots on one model support independent palettes, and texture lines remain readable in both tones.
- [ ] Neutral and colored `_BaseColor` values, including per-material-slot overrides, follow the contract.
- [ ] Default parameters produce two-tone shading without undeclared specular highlights or broad gradients.

## 7. Evidence And Handoff

All acceptance checks are currently unexecuted. On completion, record the actual
Shader/material/scene paths, Renderer configuration, parameter screenshots, and
screenshots before and after rotating the main light. Record static checks,
Unity compilation, Play Mode, and Player validation separately.
Task003 consolidates final Player/device validation. Passing this task does not
establish final visual or device acceptance.
