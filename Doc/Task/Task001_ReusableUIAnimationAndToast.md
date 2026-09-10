# Task001 - Reusable UI Animation And Toast

Iteration: Re-roll System
Status: Planned; implementation and native acceptance have not started.
Dependencies: None.
Next: [Task002 - Stage Free Re-roll System](Task002_StageFreeRerollSystem.md).

## 1. Goal And Sources

Extract the existing damage-number animation into reusable UI playback and
provide a Toast presentation that Draft can use without a global notification
service. Preserve damage-number authoring and ownership during migration.

Sources:

- [Battle HUD UI System](../System/04_BattleHUDUISystem.md), Sections 6.3-6.4.
- [Monster System](../System/07_MonsterSystem.md), Section 9.3.
- [Documentation authority](../README.md).

## 2. Scope And Ownership

| Owner | Responsibility |
|---|---|
| Reusable animation data | Enabled Position, Scale, and Fade steps with start/target values, duration, delay, and easing |
| Reusable animation player | Explicit targets, timeline validation, time mode, start/reset, replay, cancellation, and completion notification |
| DamageNumberUI and its manager | Damage text, world placement/random offset, runtime instances, and removal |
| ToastUI | Toast text and playback on an authored reusable prefab |
| Calling presentation | Instance/container ownership and cleanup; Task002 connects this to DraftWindow |

Candidate names are `UIAnimationStep`, `UIAnimationPlayer`, and `ToastUI`.
Review the live code before fixing APIs. Do not introduce a general UI framework,
global Toast manager, queue, new pooling system, or changes to Draft generation.

## 3. Implementation Contract

- Delay is relative to the whole playback start. Different properties may run
  concurrently; same-property steps must not overlap. Timing must be finite and
  non-negative, and required animation targets must be present.
- Initialize each property from its earliest enabled step by time, not list
  order. Apply subsequent start values only at their scheduled start; hold the
  preceding state during gaps. Fade-out must not override Fade-in at startup.
- Normal completion occurs after the last enabled step ends. Replay cancels the
  old playback and restores authored start values; cancellation must not invoke
  normal completion or leave stale callbacks affecting a later playback.
- UI playback defaults to unscaled presentation time. Toast uses that mode;
  DamageNumber explicitly retains simulation-time behavior. Neither writes the
  battle time scale.
- The player reports completion; its caller decides destruction or reuse. Keep
  content and lifetime decisions outside shared animation code.
- Separate Toast placement from its animated child so motion cannot accumulate
  on replay or overwrite its layout position. Toast graphics do not block input.
- Retain useful damage-number preview behavior and migrate its existing steps,
  values, target references, and prefab identity. Remove superseded animation
  code after migration rather than retaining parallel implementations.
- Define safe handling of empty/disabled timelines and zero-duration steps in
  the implementation plan; they must not leak instances or repeat completion.

## 4. Starting Points And Authoring Handoff

Inspect `Assets/Scripts/Monster/DamageNumberAnimationStep.cs`,
`DamageNumberUI.cs`, `DamageNumberManager.cs`, and
`Assets/Art/Prefab/UI/UiPrefab_DamageNumberItem.prefab`.

The implementation plan must identify serialized migrations and how values,
GUIDs, and references will be preserved. Existing `Tests/TaskNNN` folders belong
to the archived ArchitectureRefactor iteration; do not overwrite them because
these new Task numbers happen to match.

Unity authoring checklist:

- [ ] Existing DamageNumber prefab uses the shared player with its original data
  and explicit simulation-time mode.
- [ ] Toast prefab provides text, opacity target, animated content, and a
  non-blocking presentation hierarchy.
- [ ] Author Fade-in, hold, Fade-out, and concurrent position motion using steps.
  Example starting timing: Fade-in at 0s for 0.15s, motion at 0s for 1.2s,
  Fade-out at 0.95s for 0.25s. These are adjustable visual values.
- [ ] Expose playback configuration and references needed by another UI caller.

The user owns final layout, Inspector wiring, Unity import/reserialization, and
Play Mode unless separately delegated. Provide exact handoff instructions and
report any unfinished migration instead of treating compilation as acceptance.

## 5. Acceptance And Validation

- [ ] Fade-in begins at its configured Alpha even when Fade-out is later in the
  list; reordered steps still follow timeline time.
- [ ] Position, Scale, and Fade run concurrently when valid; conflicting
  same-property steps are rejected with a useful configuration error.
- [ ] Replay during Fade-in, hold, and Fade-out resets cleanly with one eventual
  completion. Cancellation/destruction stops work and stale completion.
- [ ] Toast continues during battle pause and speed changes; DamageNumber
  retains simulation-time behavior and its intended visual appearance.
- [ ] Toast does not block underlying controls. Its owner can remove it during
  playback without leaving a running animation or callback.
- [ ] Damage values, world placement/random offset, prefab references, and
  preview remain usable after migration.
- [ ] Relevant runtime/Editor compilation and focused timing/lifecycle checks
  pass. Native visual acceptance is recorded separately from managed checks.

## 6. Review And Completion Evidence

Before implementation, present a scoped plan covering migration, playback
semantics, invalid authoring, and validation. On completion record changed
files, commands/results, serialized migration evidence, Unity visual evidence,
and unresolved or explicitly waived checks. Task002 may consume a stable
reviewed API; this Task is not Completed merely because its code compiles.

Current evidence: Design contract only; no implementation or tests performed.
