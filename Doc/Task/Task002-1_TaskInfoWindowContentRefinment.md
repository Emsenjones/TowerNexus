# Task002-1 - TaskInfoWindow Content Refinment

Document Set: Task

Status: Implemented after chat plan review; managed/compilation evidence recorded;
manual UI assembly and native Unity/device acceptance pending.

## 1. Objective And Authority

Refine TowerInfoWindow's compact presentation: remove Description, show the
target Tower's cumulative Kill count, and display each acquired Upgrade's name,
icon and layer-specific background. Establish reliable gameplay kill attribution
before connecting production map input in Task003.

Authoritative contracts:

- [Battle HUD Section 4.2](../System/04_BattleHUDUISystem.md#42-towerinfowindow)
- [Tower Framework Section 6.2](../System/10_TowerFrameworkSystem.md#62-tower-inspection-data)
- [Monster System](../System/07_MonsterSystem.md)
- [Tower Runtime Combat](../System/11_TowerRuntimeCombatSystem.md)
- [Effect System](../System/14_EffectSystem.md)
- [Buff System](../System/15_BuffSystem.md)

Dependencies: [Task001](Task001_BattleModalPause.md) and
[Task002](Task002_TowerInfoWindowPresentation.md). Complete this task before
[Task003](Task003_TowerInfoWindowInteractionIntegration.md).

## 2. Scope And Deliverables

- Remove Description text binding, population and inspection snapshot comparison;
  retain TowerDefinition.Description for other consumers.
- Runtime-owned cumulative TowerInstance.KillCount with explicit reset semantics.
- Gameplay lethal-source transport and exactly-once death credit across damage paths.
- Read-only Kill count capture and freshness validation in the inspection snapshot.
- A dedicated TowerUpgradeInfoItem presentation component and typed Prefab reference.
- Updated manual UI assembly instructions and bounded regression evidence.

Keep the existing HUD session ownership, shared pause, read-only combat queries,
generated-item cleanup and opening rollback. Do not restore basicStatsContainer or
backgroundMask script references. The manually authored full-screen blocking mask
remains required. Map input and production lifecycle wiring remain Task003.

No damage-formula changes, assist scoring, damage totals, persistent player-profile
statistics, Camera movement, upgrade details or secondary popups are included.

## 3. Kill Count And Attribution Contract

TowerInstance owns a read-only nonnegative cumulative KillCount. Fresh instance
initialization resets it to zero. Accepted level/Upgrade changes, combat-model
replacement, pause and combat-component rebinding do not reset it. A retained
Tower keeps its count after Battle stop; retry/new Stage initialization starts
fresh counts and cannot receive outgoing damage credit.

Credit the Tower explicitly associated with the damage application that first
changes a living Monster to lethal health, and commit that credit exactly once
when the Monster's actual death is resolved. Arrival, forced cleanup, repeated
damage to a resolved target, and damage without a valid source Tower do not count.
Death credit must be committed before external death/resolution callbacks can
stop or release the Battle. Nested hit transactions must preserve the first
lethal application's source rather than crediting the outer transaction too.

Direct attacks, Projectiles, Drone/Orb damage, Behaviour Effects, elemental
reactions, Overload and periodic/persistent damage follow the same credit rule.
Preserve each released entity's source Tower and exact Battle identity. Reject
foreign/outgoing source attribution; do not discover a replacement source by
distance, current target selection, Upgrade identity or a new Tower registry.

FixedBuff damage remains source-independent for damage calculation. Its existing
execution context's nullable SourceTower may supply kill-credit metadata. Shared
Buff lifecycle execution uses the existing latest successful source context;
individual triggering attacks do not silently replace it. TowerHitReceived is an
explicit existing exception: that reaction execution uses the triggering Tower's
captured source without rewriting the Buff's stored lifecycle source. Reapplication does not
retroactively change an already captured execution or lethal source. If no valid
source exists, execute otherwise-valid FixedBuff damage without Tower kill credit.
Do not split a shared Buff into per-Tower stacks or award every contributor a kill.

Audit every health-changing entry, including direct TakeDamage callers, deferred
death, reaction/Overload reentrancy and source-less damage. Existing diagnostic
damage observations are not the authoritative gameplay counter: listener presence
and diagnostics configuration must not affect KillCount. Preserve Monster rewards,
player progress, resolution ordering and outgoing Battle evidence semantics.

## 4. Presentation And Authoring Contract

Basic information is DisplayName, Icon, Level, AttackRange, Attack,
AttackCycleDuration and Kill count. AttackCycleDuration uses the current committed
resolved value including Upgrades, formatted with invariant `0.##` plus ` s`.
It is a full cycle duration, not the remaining timer; zero is valid and invalid
non-finite/negative values reject opening through the read-only stats query.
Title remains fixed authored text. Kill count is integer text, including zero.
Every opening captures the latest committed count and independently copies the
acquired Upgrade presentation sequence. Compare count, Upgrade identity, name,
icon and layer alongside existing level/combat revision checks before pause and
after activation. Stale preparation rolls back without automatic retries.
No per-frame displayed-data polling is added while simulation is paused.

TowerUpgradeInfoItem owns author-assigned references:

- Icon Image, background Image and name TMP_Text.
- BasicUpgradeIconBackground, BehaviourUpgradeIconBackground and
  ElementalUpgradeIconBackground Sprites.

Initialize each generated item with the captured display name, icon and
TowerUpgradeLayer. Select exactly the corresponding layer background. The window
references the TowerUpgradeInfoItem component on a Project Prefab rather than a
bare Image. This component only presents acquired Upgrade data; it owns no Draft,
reward, drag, investment, click-detail or modal authority.

Generate one item per acquired Upgrade, in acquisition order, under the existing
Grid Layout container. Preserve authored children and remove only generated items.
Missing Upgrade icons retain their slots with diagnostics; missing required item
references/background configuration or unsupported layers reject preparation
without substituting another layer's visual. Empty optional names remain blank.
Validate configuration during hidden preparation and preserve existing rollback
and immediate close/reopen cleanup semantics.

The user removes the Description row, authors/assigns Kill count text, builds the
Upgrade item Prefab and assigns all three layer backgrounds manually. Do not
automatically redesign or regenerate the Canvas or reuse PendingDraftUIItem's
interaction responsibilities.

## 5. Implementation Plan Review Requirements

Generate the implementation plan in the conversation and review it before coding.
Identify all damage/death seams, exact Battle/source validity rules, nested and
exception paths, credit publication order, reset lifetime and overflow policy.
Explain how FixedBuff attribution metadata remains separate from damage authority.
Specify the typed Prefab API, preparation validation, snapshot freshness, item
ownership and manual-reference migration. Verify these against live code rather
than assuming all damage currently carries a Tower source.

## 6. Acceptance And Evidence

Automated coverage must distinguish nonlethal damage from a kill; two Towers
damaging the same target; repeated hits/resolution; source-less lethal damage;
arrival/cleanup; direct and released-entity kills; FixedBuff periodic/reaction/
Overload deaths; nested lethal effects; and callback reentrancy/exception paths.
Verify source changes in shared Buffs, source loss, foreign/stale Battle rejection,
preserved counts through level/model changes and Stop, and fresh-instance reset.
Existing damage totals, reward/progress and terminal contracts must remain valid.

Presentation checks cover Description removal, zero/nonzero/latest Kill count,
stale-count rejection, all three background layers, ordered names/icons, empty
Upgrades, missing icons/configuration, halfway generation failure and repeated
close/reopen with authored children retained. No failure leaves a pause or items.

Native Unity acceptance verifies authored references, compact layout, name/icon/
background correctness, visible Kill counts after real combat and model changes,
Grid Layout and Close responsiveness during pause. Task003 verifies actual
mouse/touch entry, modal input and full Battle lifecycle with this final content.
Record managed/build, Play Mode/device and user layout evidence separately;
unexecuted acceptance remains pending. Document generation is not implementation.

## 7. Implemented Interfaces And Review Resolution

- `TowerInstance.KillCount` is read-only; initialization resets the counter and
  runtime identity. Internal counting saturates at int.MaxValue. Formal deployment
  commit binds kill ownership to the existing Submission membership and exact Battle,
  before combat activation. Model changes and combat rebinding do not reset it.
- `TowerKillSource` is immutable Battle/Tower/runtime provenance. Projectile,
  Drone, Orb group and persistent Field capture it at release/initialization;
  child Projectiles, WindVortex, Effect contexts, Buff requests/instances and
  pending Overload forward the original stamp instead of recapturing at impact.
- Monster records the first lethal source before health feedback. Death consumes
  it once before path/Buff/death/resolution notifications. Credit checks exact
  runtime identity, existing deployed membership and pure Battle IsOpenForRead;
  it does not require the now-dead Monster to satisfy CanTarget.
- Damage commit facts are returned before callback-capable feedback so a reset or
  throwing callback cannot erase committed application evidence. Old target-life
  damage observations are not published against a newly initialized Monster.
- Hit transactions retain Monster identity and their original Buff runtime.
  End is single-use; Buff mutation End runs in finally even when death throws.
  Monster reset revokes identity before old Buff cleanup, detaches outgoing
  observers and establishes a separate Buff runtime. Old finally cannot decrement
  new-life depths or kill/destroy that new life. Death notifications isolate
  subscriber exceptions while preserving publication order and progress delivery.
- `TowerUpgradeInfoItem.Initialize(name, icon, layer)` is presentation only.
  Required local references and all three layer backgrounds are validated; an
  unsupported layer rejects hidden preparation. The View references the typed
  Prefab and owns each returned item before initialization/explicit activation.
- Snapshot captures runtime identity, Kill count and independent Upgrade
  name/icon/layer arrays; existing pre-pause/post-activation freshness checks apply.
  Description is removed. HUD session/pause interfaces remain unchanged.
- The View requires attackCycleDurationText and reads the existing snapshot's
  Stats.AttackCycleDuration. The authored base field is renamed to
  baseAttackCycleDuration; FormerlySerializedAs preserves both attackCycleDuration
  and older attackInterval serialized values. BaseAttackCycleDuration remains the
  public base-value API. upgradeContainer is retained as the generated-item Parent.

## 8. Verification Evidence And Manual Handoff

- ContentRefinement/run.py: 33 managed assertions using exact production health,
  death, hit-transaction, Tower initialization/counter and Buff lifecycle execution
  methods, whole provenance/context/request/BuffInstance/pending Overload classes.
  Native feedback, Buff mutation boundary and Effect damage execution are doubled.
  Includes A/B reaction/lifecycle attribution, reapplication, delayed Overload,
  source-less damage, identity reuse, nested death, exception/reset and progress.
- Presentation/run.py: 105 assertions using the production View, Item, Snapshot,
  HUD session, Battle binding and pause authority. Native rendering/instantiation
  and deployment membership are doubled; these do not establish Unity visuals.
- Existing Effect/binding/hit regression: 27 checks; Submission: 31 contracts;
  modal pause: 81 assertions plus five attack and five spawning boundary cases.
- ContentRefinement/build.py compiles the complete 139-source runtime against
  actual installed Unity/TMP references in player and UNITY_EDITOR modes. This is
  compilation evidence, not a Unity player build or Play Mode acceptance.

Manual migration: remove Description row; create/assign Kill count and Attack
Cycle Duration TMP_Text references;
replace the old bare Image template with a TowerUpgradeInfoItem Prefab; assign
icon/background/name and Basic/Behaviour/Elemental backgrounds. The old Image
reference is not automatically convertible and must be reassigned. Keep the
existing Canvas, common root, Grid, Close and full-screen raycast mask.

Pending: native real-combat kill attribution (including released entities and
Buff/Effect deaths), model-change preservation, actual hierarchy/Prefab references,
names/backgrounds/layout and deferred destruction. Production mouse/touch entry
and full Battle lifecycle remain Task003. No native checks are marked passed.
