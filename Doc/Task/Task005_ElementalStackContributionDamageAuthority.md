# Task005 - Elemental Stack Contribution Damage Authority

Status: Completed; implementation accepted on 2026-08-24 with static validation
and user-confirmed Unity Play Mode evidence

Depends on: Completed Task001 Combat Damage Formula Refactor; accepted Task004 Non-Elemental Upgrade Baseline

Blocks: Task006-Task007 and Task010-Task017

## Downstream Supersession

This document preserves the Task005 implementation and evidence that were true
at its checkpoint. Completed Task007B later restricted ordinary Elemental
application to baseline-primary opportunities. Task007C's accepted
implementation checkpoint now supersedes Electric/Wind normal value: active
ElectricShock and Windcut content no longer uses contributor-owned
`StackApplied` TowerScaled damage and instead uses shared FixedBuff Elemental
hit reactions. Task007C's remaining edge-transaction smoke is explicitly
deferred; this does not restore the historical Task005 execution path.

## 1. Goal

Refine CombatMathV2 damage ownership so shared Buff state remains independent
from Tower values while the immediate result of one successful Elemental stack
contribution may derive from the Tower that contributed that stack.

This Task preserves the existing `EffectDamageMode` schema. Every DealDamage
EffectAction continues to author exactly one explicit mode:

| DamageMode | Formula | Required authority |
|---|---|---|
| TowerScaled | `RoundToInt(CurrentResolvedBasicDamage * DamageScale)` | one valid source Tower and one positive finite DamageScale |
| FixedBuff | authored positive integer FixedDamage | shared Buff or Elemental-reaction state |

`TowerScaled` and `FixedBuff` remain the stable names. This Task does not add a
fallback, a third damage mode, or an independently authored base-damage value.

## 2. Ownership Revision

Buff events are separated by what caused their gameplay result:

| Event result | Damage authority |
|---|---|
| Periodic, Overload, Protection, removal, and persistent reaction results owned by shared Buff state | FixedBuff |
| Immediate damage produced because one Tower successfully added a stack | TowerScaled from that contributing Tower |

The first application creates one stack but does not run StackApplied. A
cooldown-blocked, Protection-blocked, invalid, or pure-refresh result also does
not run StackApplied.

When a successful reapplication adds a stack, the source Tower and source
Elemental Upgrade from that exact application request become the contributor
context before StackApplied executes. The original Tower that created the Buff
has no continuing priority. A later contributing Tower may therefore supply a
different current resolved BasicDamage to the shared StackApplied Effect.

## 3. First Content Migration

- Electrified StackApplied extra damage becomes TowerScaled.
- Windcut StackApplied secondary-target damage becomes TowerScaled.
- Burning PeriodicTick and FlameBurst remain FixedBuff.
- Electric Overload LightningStrike remains FixedBuff.
- Windcut Overload and WindVortex ticks remain FixedBuff.
- Cold and Frozen movement state remains independent from source-Tower damage.

Provisional migration anchors may preserve the already observed Archer L3
per-stack results with Electrified DamageScale `0.15` and Windcut DamageScale
`0.30`. Task007 owns final numeric acceptance across all TowerFamilies; these
anchors are not accepted balance values.

## 4. Execution Contract

1. The producing Tower-owned attack boundary validates its TowerScaled numeric
   context before emitting an Elemental application opportunity.
2. Buff System evaluates definition identity, Protection, and the applying
   source Tower's cooldown entry.
3. A successful reapplication commits the refreshed duration, new shared stack,
   and exact contributor context.
4. StackApplied executes with that contributor context.
5. When the new stack reaches Maximum Stacks, Overload executes afterward from
   shared Buff state and then enters Protection when configured.

StackApplied TowerScaled damage never applies Elemental state recursively. A
missing or invalid source Tower, non-positive or non-finite resolved BasicDamage,
or non-positive or non-finite DamageScale rejects that TowerScaled damage result;
it is never reinterpreted as FixedBuff. Ordinary lifecycle cleanup does not
invent a replacement contributor.

## 5. Validation And Diagnostics

- Buff event validation permits TowerScaled nested DealDamage only for the
  approved contributor-owned StackApplied path.
- Other Buff lifecycle and reaction owners continue to require FixedBuff for
  every nested DealDamage action.
- Recursive Effect validation preserves the expected mode through wrapper and
  child Effects.
- TowerScaled Recorder signatures identify the contributing Tower instance,
  family, Level, BasicDamage inputs, Effect/action identity, DamageScale, final
  damage, and application count.
- FixedBuff Recorder signatures continue to exclude Tower Level, BasicDamage,
  Damage Bonus, and DamageScale.
- Buff source diagnostics identify the exact Tower and Elemental Upgrade that
  produced every successful stack, blocked request, and overload transition.

The existing Recorder schema may remain unchanged when its TowerScaled and Buff
source records already prove these facts. Any exported-shape change must bump the
schema in the same implementation checkpoint.

## 6. Required Evidence

Use one shared Elemental Buff with two different Tower instances:

- Tower A creates the Buff;
- Tower B successfully contributes the next stack;
- StackApplied TowerScaled damage resolves from Tower B;
- a later Tower A stack resolves from Tower A;
- blocked attempts produce no StackApplied damage;
- the max-stack contribution runs StackApplied before source-independent
  Overload and Protection;
- no StackApplied or reaction damage creates an Elemental application attempt.

Static acceptance additionally requires runtime and Editor builds, Combat
Damage Authoring validation, and path-scoped `git diff --check`.

## 7. Out Of Scope

- final Elemental DamageScale, FixedDamage, cooldown, duration, radius, or
  target-count balance;
- per-Tower BuffDefinition copies;
- a generic proc, reaction, or damage-payload framework;
- changing Tower attack cadence or Elemental application opportunities;
- Drone Burst opportunity timing, which belongs to Task006;
- Task007 fixture execution or final Elemental acceptance.

## 8. Completion Record

- StackApplied bindings recursively require TowerScaled DealDamage; other Buff
  lifecycle bindings continue to require FixedBuff DealDamage.
- Electrified StackApplied uses provisional DamageScale `0.15`, and Windcut
  StackApplied secondary damage uses provisional DamageScale `0.30`. Task007
  continues to own final Elemental balance.
- The existing successful-reapplication transaction supplies the exact
  contributor Tower and Elemental Upgrade before StackApplied executes; no new
  contributor or damage-payload abstraction was required.
- Runtime and Editor builds completed with zero warnings and zero errors, and
  path-scoped diff validation passed.
- In user-confirmed Play Mode evidence, a naked Level 3 Cannon resolved Basic
  Damage `204`, and its Electrified StackApplied result resolved
  `RoundToInt(204 * 0.15) = 31` damage.
- Recorder remains schema `15`. Task007 may reopen this Task if its broader
  Elemental fixtures expose a contributor-attribution, shared-state, or
  regression defect.
