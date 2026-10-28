# TowerNexus Toon Rendering — Task Index

Document Set: Task

Status: Requirements prepared; implementation and visual acceptance pending.

These tasks capture the first-version visual implementation scope agreed on 2026-10-28
for subsequent development. The working branch is `codex/vertical-slice`.
Read the relevant Task before defining its implementation plan in the development session.
This document does not establish completion of shaders, materials, scenes, or device validation.

## 1. Visual Target And Approved Scope

- Follow a Sable-inspired line-and-flat-color direction: a restrained palette, broad clean color areas, and clearly defined major forms.
- Validate through the project's orthographic gameplay view. Concept images express the direction; pixel-perfect reproduction is not required.
- The first version has exactly two tones: lit and shadowed. A narrow transition may improve the boundary, but must not create broad realistic gradients.
- Base textures retain base colors, engraved patterns, and decorative lines. Dynamic rendering handles lighting changes and silhouettes.
- Multiple materials share one Toon Shader. Tower bodies, metal, crystals, and other regions each have their own lit and shadow colors.
- Define regions through material assignments on existing submeshes or separate parts; do not create RGB region masks.
- The first version does not require Normal Maps, but Mesh normals must be correct. Simplified models must retain the major form-defining planes.
- Initially support static, opaque towers and ground. Crystals can initially use opaque colors.
- Defer a third highlight tone, specular reflections, transparent refraction, Bloom, multiple-light accumulation, separate line-art masks, and bulk asset migration.
- Skinning, hit feedback, and outline compatibility for animated monsters require separate validation with actual samples; static-tower tests do not establish support.

The concept-generation prompt permits "two or three tones" for visual exploration.
The implementation scope of these tasks is explicitly limited to two tones.

## 2. Execution Order

| Task | Deliverable | Dependencies |
|---|---|---|
| [Task001 — Two-Tone Shader](Task001_ToonShaderFoundation.md) | Two-tone shading, cast and received shadows, independent material parameters, and a basic validation scene | This document's contracts; placeholder geometry is acceptable |
| [Task002 — Dynamic Outline](Task002_ToonOutline.md) | Adjustable dynamic outlines, correct occlusion, and stability in the orthographic view | Task001 material and scene baseline |
| [Task003 — Asset Integration And Validation](Task003_ToonAssetIntegration.md) | Integration workflow for one actual tower, gameplay-view acceptance, and device evidence | Task001, Task002, and an actual asset supplied by the user |

Task numbering restarts for this round. Earlier Task documents were retired at the
user's request and are preserved in Git commit `f54e8a17`. Acceptance of those earlier
features remains bounded by the evidence recorded in that historical version.

## 3. References And Ownership

- [Concept Prompt](../Art/00_SableInspiredConceptPrompts.md)
- [Reference 1](../Art/00_SableInspiredConceptPrompt_Ref1.png), [Reference 2](../Art/00_SableInspiredConceptPrompt_Ref2.png), [Reference 3](../Art/00_SableInspiredConceptPrompt_Ref3.png), [Reference 4](../Art/00_SableInspiredConceptPrompt_Ref4.png)
- [Tower Framework](../System/10_TowerFrameworkSystem.md): existing contracts for model mounting, level-model replacement, Anchors, and Tower status presentation.
- [Camera System](../System/06_CameraSystem.md): gameplay camera and input behavior.
- [Tower Placement](../System/09_TowerPlacementSystem.md): placement previews, footprints, and interaction semantics.

The user owns reference selection, Tripo model and texture generation, necessary
asset cleanup, and final visual judgment. Development tasks cover rendering,
material configuration, integration instructions, and validation support.
Both sides can iterate on the first tower's model, textures, and parameters based
on its appearance in Unity. The Shader is not responsible for repairing silhouettes,
reassigning material regions, or removing fixed lighting from textures.

These tasks concern visual presentation. They do not introduce gameplay systems or
change ownership of combat, pathfinding, placement, upgrades, or camera input.
Integrate actual assets through existing contracts such as TowerModelRoot;
visual requirements must not redefine gameplay coordinates or footprints.

## 4. Repository Baseline — Static Inspection, 2026-10-28

| Item | Verified Configuration On Disk |
|---|---|
| Unity | `6000.3.14f1`, from `ProjectSettings/ProjectVersion.txt` |
| URP | `17.3.0`, from `Packages/manifest.json` |
| Gameplay Camera Prefab | Orthographic with Size 6 in `Prefab_GameRuntime.prefab`; this is the current configuration, not a fixed art standard |
| Quality | Currently recorded as Medium; iPhone defaults to index 2 and Standalone to index 5 |
| 3D Renderer | The Medium and Ultra Pipeline Assets reference `Assets/URPDefaultResources/Default_Forward_Renderer.asset` |
| Graphics Default | References `Assets/Settings/UniversalRP.asset`, whose Renderer list differs from the Quality configurations above |

Before development, confirm the target platform's effective Quality, Pipeline Asset,
Camera Renderer, Render Graph/Compatibility mode, and shadow settings in Unity.
These static records do not replace runtime verification. Changing only the Graphics
default does not establish that gameplay and iOS use the new settings.

The existing `TowerVisualController` changes preview colors through `_BaseColor` /
`_Color` and uses MaterialPropertyBlock per material slot for upgrade-target feedback.
The new Shader parameters must account for this existing integration path;
see Task001 and Task003 for the requirements.

## 5. Completion And Evidence

- Start validation with one tower or placeholder model, one ground surface, one directional light, and an orthographic camera.
- For each task, separately record static checks, Unity import/Shader compilation, Play Mode, Player/device results, and user visual acceptance.
- Mark unexecuted checks Pending. Passing with placeholders does not establish acceptance of Tripo assets.
- Acceptance of the sample tower does not establish completion of all towers, animated monsters, or the entire Vertical Slice.
- Before the final merge, record actual assets, material parameters, the effective Renderer configuration, screenshots, device conditions, and known limitations.

Technical references: [Custom Lighting In URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-lighting.html),
[Custom Shadows In URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-shadows.html).
