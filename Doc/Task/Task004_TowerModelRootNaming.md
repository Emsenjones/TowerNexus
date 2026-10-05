# Task004 - Tower Model Root Naming

Document Set: Task
Status: Naming migration implemented; static validation passed. Native Unity import and Play Mode acceptance pending.
Dependencies: None; independent of the existing Monster dashed-path Task001–Task003.
Next: Verify imported references and preview/deploy/level-up behavior in Unity.

## 1. Goal

Rename `TowerPrefabSpawnPoint` to `TowerModelRoot`, as approved in the design
review, and preserve the existing stable model-parent behavior. Establish the
named position reference consumed by Task005 without changing model placement.

## 2. Source Documents

- [Tower Framework System](../System/10_TowerFrameworkSystem.md), Sections 3, 5, and 6.1.
- [Project Overview](../System/00_ProjectOverview.md), Section 4.10.
- User-approved design: the model parent survives level-model replacement and
  is reused for Tower status positioning; no bounds-derived or extra UI anchor.

## 3. Scope

- Migrate the public property, serialized backing field, hierarchy node names,
  lookup strings, diagnostics, affected callers, and active fixture references.
- Preserve authored references, transforms, model-parent identity, level-model
  initialization, and existing model-dependent feedback behavior.
- Audit Tower base Prefabs, variants/overrides, scenes, editor helpers, and tests
  for affected references. Record the exact asset inventory in the plan.
- Keep the System naming contract and active task references consistent.

## 4. Out Of Scope

Status scripts or status Prefab creation, new UI anchors, model-center/bounds
queries, combat changes, placement geometry changes, and unrelated renames.
Historical task/evidence records need not be rewritten as current code.

## 5. Ownership And Migration Contract

Tower Framework owns the stable `TowerModelRoot` under `VisualRoot`. Level models
are mounted at its local origin; replacing a level model does not replace this
root. Its position is a reference point, not a promise of geometric center or top.

Inspect `TowerVisualController`, `TowerBehaviour`, all serialized uses of
`towerPrefabSpawnPoint`, and exact-name lookups. The implementation plan must
settle serialized-field compatibility and asset/override migration together;
renaming a C# property alone is insufficient. Old terminology is permitted only
where deliberately retained for migration compatibility or historical evidence.
No silent fallback that conceals a broken authored reference is acceptable.

## 6. Unity Authoring Checklist

- Identify each affected `Prefab_TowerBase_*` and dependent authored instance.
- Rename the existing node; retain its identity and all local transform values.
- Confirm the renamed serialized reference resolves after import/reload.
- Preserve level-model, Attack Origin, spawn/upgrade feedback, and preview wiring.
- Unity Inspector work, import/reserialization, and Play Mode validation remain
  user-owned unless explicitly delegated. The plan must separate code work from
  authoring steps and report any pending asset migration honestly.

## 7. Acceptance Criteria

- Active production callers and hierarchy lookups use the approved name.
- Every affected Tower base resolves the same stable parent before and after
  level-model replacement, without changed model pose or placement footprint.
- Serialized references and overrides survive migration; no new missing-reference
  diagnostics occur when creating, previewing, deploying, or leveling Towers.
- Existing visual feedback still resolves its intended anchor.
- Remaining legacy-name matches are classified, not blindly deleted.

## 8. Validation

Perform a focused reference/asset scan and diff review. Compile affected runtime
code with the repository's available static gate; include Editor compilation if
Editor code changes. Run relevant existing harnesses where affected.
Native Unity import/reference checks and preview/deploy/level-up smoke checks
are separate required evidence. Static checks do not establish native acceptance.

## 9. Review And Status

The implementation plan was presented in chat and the user authorized execution
on 2026-10-05 without a further review round. Implementation follows that plan.

Completed:

- Renamed the serialized field, public property, hierarchy lookup, internal uses,
  and diagnostics in TowerVisualController; updated TowerBehaviour's caller.
- Retained `[FormerlySerializedAs("towerPrefabSpawnPoint")]` for serialized-data
  compatibility. No old public-property alias or old-name hierarchy fallback.
- Renamed the existing node and serialized field key in Archer, Cannon, Magic,
  and Drone base Prefabs. An exact comparison against the pre-change assets
  confirmed that only those two names changed in each Prefab; file IDs,
  transforms, hierarchy, and other configuration were preserved.
- Scanned Assets and Tests: the only remaining legacy-name occurrence is the
  intentional serialization compatibility attribute.
- Runtime compilation passed with 0 warnings and 0 errors using
  `dotnet build Assembly-CSharp.csproj --no-restore -m:1 -nr:false -p:LangVersion=8.0`.
  The initial sandboxed attempt could not write build caches; the authorized
  rerun succeeded. No Editor code changed.
- Scoped whitespace validation passed. No additional test harness was added for
  this naming-only change.

Pending user-owned native validation: Unity import/reload and serialized-reference
inspection, then preview, deployment, level-up/model replacement, and feedback
smoke checks for all four TowerFamilies. Static results do not establish visual
or Play Mode acceptance. Task005 implementation has not started.
