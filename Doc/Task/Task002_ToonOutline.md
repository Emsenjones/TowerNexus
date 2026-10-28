# Task002 — Dynamic Outline

Document Set: Task

Status: Ready for implementation-plan discussion; depends on Task001.

## 1. Objective And Authority

Add thin, stable dynamic outlines to the two-tone baseline from
[Task001](Task001_ToonShaderFoundation.md), keeping towers readable through the
orthographic gameplay camera while preserving broad flat-color surfaces.
Shared visual contracts are defined in the [Task Index](README.md).

Static engravings, brick seams, and decorative lines remain in the base texture.
Prioritize visible silhouettes and readable boundaries between overlapping objects.
Automatic rendering of every internal crease or triangle edge is not required.

## 2. Scope And Deliverables

- One outline implementation that meets the prototype requirements, with any necessary Shader Passes, materials, or Renderer Features.
- Configuration for outline color, width, and participation; specify whether width uses screen or world units.
- Add samples for multiple parts, hard edges, UV seams, thin surfaces, overlapping objects, and scaling to the Task001 scene.
- Document model requirements, limitations, rendering cost, and the integration procedure for Task003.

Choose one primary approach; the first version does not need a framework for
switching between multiple algorithms. Both inverted-hull outlines and screen-space
depth/normal outlines may be evaluated. Decide based on the actual camera view,
asset-cleanup effort, and device rendering cost. This task does not designate either
approach as the final solution in advance.

## 3. Required Behavior

| Situation | Requirement |
|---|---|
| Silhouette | Major forms remain continuous and readable; line width does not excessively obscure detail |
| Occlusion | Outlines hidden behind other objects do not show through; front-to-back relationships remain correct |
| Multiple Materials/Parts | Material boundaries do not automatically become thick black edges; avoid drawing the same outline repeatedly |
| Decorative Lines | Texture lines remain visible without being amplified again as normal/depth edges |
| Object Motion | Outlines update during movement and rotation without lingering trails or misalignment |
| Camera And Resolution | Remain stable at the actual orthographic scale, during camera panning, and at the target resolution; document the supported scale range |
| Lighting And Shadows | Outlines do not change Task001's tone classification; outline hulls do not unintentionally enlarge cast shadows |
| Participation | Allow designated 3D models to participate without applying outlines indiscriminately to HUD, text, dashed paths, or unrelated effects |

If the inverted-hull approach requires additional smoothed-normal data, keep it
separate from the Mesh normals used for two-tone lighting. Fixing outline seams
must not damage hard-edge lighting on the main surface.
If the screen-space approach requires depth/normal inputs, confirm that the custom
Toon Shader actually writes the required data. Validate it with the project's
effective URP Renderer and Render Graph/Compatibility mode.

## 4. Implementation Plan Must Resolve

1. The primary approach and its tradeoffs: outline coverage, hard edges and seams, orthographic line width, and rendering cost.
2. Integration with Task001's Shader, material slots, depth, and normals, including the minimum required additions.
3. Inclusion/exclusion rules, depth occlusion, culling, drawing order, and any additional Renderer configuration.
4. The scope of configuration assets. If a dedicated Renderer is needed, document how to assign it and restore the previous configuration.
5. Constraints on imported-model normals, scale, and parts, with explicit handling for unsupported cases.

Transparent objects, skinned animation, scene-wide structural-line extraction, and
selection-highlight systems are outside this task's first-version deliverables.
Artistic outlines must not replace or change the gameplay semantics of existing
selection and upgrade-target feedback.

## 5. Acceptance

- [ ] Task001's two-tone colors and shadow behavior remain valid; disabling outlines restores the original baseline.
- [ ] Hard edges, smooth surfaces, and UV seams show no obvious cracks or abnormal spikes.
- [ ] Samples with multiple materials or parts show no widespread double lines, black patches, or unintended internal edges.
- [ ] Outlines do not show through overlapping objects, and cast shadows are not incorrectly enlarged by outlines.
- [ ] Outlines remain stable with the actual gameplay camera at rest, during panning, and at the agreed display scales.
- [ ] Detail remains readable at the target resolution; outlines do not swallow distant or small objects.
- [ ] HUD, dashed paths, and unrelated objects remain unaffected, and participation settings are reproducible.
- [ ] Record rendering cost with outlines enabled and disabled in the same scene under the same settings, and explain the choice of approach.

## 6. Evidence And Handoff

All acceptance checks are currently unexecuted. Record the implementation, asset
references, effective Pipeline/Renderer, line-width settings, and seam-handling
requirements. Provide evidence from static and moving views, plus known failing samples.
Editor screenshots alone do not establish temporal stability or device performance;
Task003 consolidates final device verification.
