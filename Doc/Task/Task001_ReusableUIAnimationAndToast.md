# Task001 - Reusable UI Animation And Toast

Iteration: Re-roll System
Status: Completed for the approved Demo scope; original broader gates are subject to the closeout waivers.
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

The types are `UIAnimationStep`, `UIAnimationPlayer`, and `ToastUI`.
`UIAnimationPlayer` is a serializable ordinary class embedded in DamageNumberUI
and ToastUI, not an independently attached component. Each host owns runtime
updates, editor preview updates, and cancellation on disable/destruction.
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
- Validate raw authored values, including finite Delay + Duration. A new play
  request cancels the previous playback before validation; rejection leaves it
  stopped and sends no completion. An empty/all-disabled timeline is rejected.
- Zero-total-duration playback completes on the next effective update after
  TryPlay returns. Cancellation/replay can revoke that pending completion.
- DamageNumberManager clears instances on disable, and individual early
  disable/destruction unregisters its number without reporting normal completion.

## 4. Starting Points And Authoring Handoff

The migration baseline at `1a044e9` contains
`Assets/Scripts/Monster/DamageNumberAnimationStep.cs`. Its current replacement is
`Assets/Scripts/UI/UIAnimationStep.cs`, accompanied by `UIAnimationPlayer.cs`
and `ToastUI.cs`. Inspect `DamageNumberUI.cs`, `DamageNumberManager.cs`, and
`Assets/Art/Prefab/UI/UiPrefab_DamageNumberItem.prefab`.

The implementation plan must identify serialized migrations and how values,
GUIDs, and references will be preserved. Existing `Tests/TaskNNN` folders belong
to the archived ArchitectureRefactor iteration; do not overwrite them because
these new Task numbers happen to match.

Unity authoring checklist:

- [x] Existing DamageNumber prefab uses the shared player with its original data
  and explicit simulation-time mode.
- [x] Toast prefab provides text, opacity target, animated content, and a
  non-blocking presentation hierarchy.
- [x] Author Fade-in, hold, Fade-out, and concurrent position motion using steps.
  Example starting timing: Fade-in at 0s for 0.15s, motion at 0s for 1.2s,
  Fade-out at 0.95s for 0.25s. These are adjustable visual values.
- [x] Expose playback configuration and references needed by another UI caller.

The authoring checkmarks above are serialized-file evidence only. Unity import,
Inspector confirmation, appearance, and native interaction remain pending.

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

## 7. Implementation And Validation Evidence (2026-09-10)

Implemented the reviewed contract from the task "评审 Task001 Stage Re-roll 方案"
(`01a08a09-92a2-7a00-b388-cd3551df7ce2`).

- `UIAnimationStep` preserves the original data field names, enum values, and
  script meta GUID. Timing/Alpha getters expose raw values for validation.
- The inline `UIAnimationPlayer` samples one explicit timeline per host update using DOTween
  easing. It does not construct independent property Tweens whose callbacks can
  race at shared endpoints. Enabled steps and playback targets/time mode are
  captured per play. Completion clears state before invoking the owner.
- Host Update supplies scaled/unscaled deltas to `UIAnimationPlayer.Tick`; the
  player selects its captured clock and skips the starting frame. Each host owns
  its Editor Preview subscription and detaches it on cancel or completion.
- DamageNumber now delegates playback; Manager owns normal, failed, disabled,
  and early-removed instance cleanup. Old animation implementation was removed.
- Original DamageNumber prefab GUID, original component IDs, targets, text,
  random offset ranges, easing and full step payload are preserved. Its actual
  animation remains Position 0-to-30 over 0.4s, Scale 3-to-1 over 0.4s, and Fade
  0-to-1 over 0.2s. No Fade-out was added to that prefab.
- `UiPrefab_ToastItem.prefab` is standalone, uses the existing TMP font/material,
  has non-blocking text and CanvasGroup, and separates placement from animation.
  It is not yet connected to DraftWindow; Task002 owns that integration.

Validation performed:

| Evidence | Result | Boundary |
|---|---|---|
| `python3 Tests/Reroll/Task001/run.py` | 92 assertions passed | Whole production player, Toast and DamageNumber owner classes with explicit managed clock/UI/easing doubles; not native Unity/DOTween execution |
| `python3 Tests/Reroll/Task001/assets.py` | Passed | Exact migration payload, GUID/reference/hierarchy preservation and Toast serialized ownership/input flags |
| Runtime and Editor project build | 0 warnings, 0 errors | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -m:1 -nr:false -p:LangVersion=8.0` with a temporary source-list overlay for the moved/new files, since Unity project regeneration was not run |
| Changed production classes against actual Unity/DOTween/TMP references | Passed with and without `UNITY_EDITOR` | C# compilation, not an iOS build or native playback test |
| Focused diff checks | Passed | Code, metadata, prefab, test and documentation whitespace |

Managed coverage includes reversed-list shared endpoints, updates crossing
multiple boundaries, gap hold, explicit later start values, parallel channels,
raw invalid/overflow timing, zero-duration cancellation/replay, failed replay,
clock selection, completion reentrancy, playback snapshots, manager disable,
early instance disable/destruction, failed instance creation cleanup, and outer
spawn-position preservation. Tests use a separate Re-roll directory; archived
test harnesses and their oracles were not overwritten.

## 8. Unity Handoff And Outstanding Acceptance

1. Let Unity import the moved/new scripts and prefabs and regenerate project
   files. Confirm no missing script, compile, or serialized-reference errors.
2. Open `Assets/Art/Prefab/UI/UiPrefab_DamageNumberItem.prefab`. Confirm its
   DamageNumberUI contains an inline Animation Player foldout; its Animated Root is
   `RecTransform_AnimatedRoot`, its CanvasGroup remains the original root group,
   and Time Mode is Scaled. Verify the original three steps and random ranges.
3. Use DamageNumber Preview in Editor and Play Mode, then observe actual damage
   in battle. Verify text/world placement, random offset, motion, scale, and fade.
   Disable/re-enable the presentation owner and remove an individual number
   mid-animation; no frozen number should survive or remain registered.
4. Open `Assets/Art/Prefab/UI/UiPrefab_ToastItem.prefab` and temporarily place it under
   a test Canvas to use Preview. ToastUI references its text and embeds the player; the player
   targets the child and its CanvasGroup, with Unscaled time. The user has authored its background and animation parameters; preserve these
   settings during Draft integration.
5. Preview with normal, paused, and changed battle time scale. Replay during
   fade-in, hold, and fade-out; then disable/destroy during playback. Confirm no
   accumulated offset, blocked underlying button, or stale completion. Editor
   Preview keeps the instance; runtime callers remove it on completion.
6. Record native results here before marking Completed. Task002 must handle
   `ToastUI.TryPlay` failure and normal completion, own the single instance, and
   cancel/remove it on Draft closure.

The user reported that the initial component-based version imported into Unity
without errors. The subsequent user-requested inline-class revision preserves
both prefabs' current animation settings and the user's ToastItem asset rename;
it removes the standalone player component and nests its data in each host.
Managed coverage now also exercises both hosts' Update forwarding, host disable
and inactive rejection, and the serializable class/default clock configuration.

On 2026-09-10 the user reported that DamageNumber behaves correctly in Unity
and that a first Toast parameter calibration is authored. This is user-reported
native DamageNumber evidence, not an agent-run test or proof of every lifecycle
case above. The user explicitly requested proceeding with Task002 and deferring
Toast integration testing until the Draft UI extension is ready. Integrated
paused Toast/replay/cleanup and device acceptance remain unverified.

The Task002 integration check also found user-authored DamageNumber layout edits
in two existing RectTransforms (text anchors/size and animated-root size). Those
asset values were preserved. The migration test now recognizes exactly those
reviewed layout values while retaining the original animation payload, GUID,
hierarchy and reference oracles; it does not silently replace the baseline.

## Final Demo disposition (2026-09-11)

This disposition supersedes earlier pending-status notes and uncompleted broad
acceptance checklists above. See [durable closeout](../History/ReRollSystem_Closeout.md)
for accepted scope, native/manual evidence, report inventory, and explicit
waivers. Historical implementation and test details above are retained as such.
