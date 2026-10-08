# Task002 - TowerInfoWindow Presentation

Document Set: Task

Status: Task contract generated; implementation plan and review pending.

## 1. Objective And Authority

Present the latest committed information for one deployed Tower in the authored
TowerInfoWindow, pause through Task001, and clean the window safely on close or
cancellation. Provide an explicit opening capability for Task003.

Authoritative contracts:

- [Battle HUD Section 4.2](../System/04_BattleHUDUISystem.md#42-towerinfowindow)
- [Tower Framework Section 6.2](../System/10_TowerFrameworkSystem.md#62-tower-inspection-data)
- [Tower Runtime Combat](../System/11_TowerRuntimeCombatSystem.md)
- [Battle Modal Pause](../System/02_StageSystem.md#32-battle-modal-pause-lifetime)

Dependency: [Task001](Task001_BattleModalPause.md). Task003 integrates player entry
and production Battle lifecycle. This task owns local opening rollback, close,
disable, target binding, and generated-content cleanup.

## 2. Scope And Deliverables

- Window script with explicit author-assigned presentation references.
- Read-only current resolved combat-stat query and a coherent inspection snapshot.
- Target/session ownership, pause acquisition/release, and reliable opening rollback.
- Upgrade icon generation in acquisition order and local content cleanup.
- Close-button integration and configuration diagnostics.
- A documented UI assembly checklist for the user's manual layout and binding.

Map hit detection and tap-versus-pan classification belong to Task003. Camera
focus, zoom, upgrade details, unacquired icons, secondary popups, per-frame polling,
Tower editing, and a generic window framework are outside this task.

## 3. Data Contract

| Display | Source |
|---|---|
| Title | Fixed author-edited text; opening does not overwrite it |
| DisplayName | `TowerInstance.TowerDefinition.DisplayName` |
| Description | `TowerInstance.TowerDefinition.Description` |
| Icon | `TowerInstance.TowerDefinition.Icon` |
| Level | `TowerInstance.CurrentLevel` |
| AttackRange | Current committed resolved Attack Range from `TowerCombatBehaviour` |
| Attack | Current committed `ResolvedBasicDamage` from `TowerCombatBehaviour` |
| Upgrade icons | `TowerInstance.AppliedUpgrades`, each `TowerUpgradeDefinition.Icon` |

Attack includes level BasicDamage and applied Basic Damage Bonus deltas, before
source-specific DamageScale. Do not substitute `LevelBasicDamage`, final damage
for one damage source, or DPS. Range includes applied Upgrade changes.

The existing combat cache is private and resolved-stat access is protected.
Provide a read-only capability that verifies the target binding and an established
committed baseline; reading UI data cannot initialize, mutate, or refresh combat.
Unavailable data rejects opening with diagnostics instead of fabricated zero values.
Use the current Battle's authoritative deployed membership, not component existence.

Capture level, identity, resolved values, and an independent ordered Upgrade list
for one opening. Revalidate target/Battle/session after work that can reenter
lifecycle code. Reopening after accepted level or Upgrade changes displays new values.
No regular per-frame stats polling is needed while simulation is paused.

Numeric formatting must be defined in the plan and applied consistently. Keep
finite valid values accurate; do not convert a float to an integer merely for layout.
Blank optional identity text may remain blank. Specify missing-icon diagnostics and
presentation behavior without dropping an acquired Upgrade or reusing another icon.

## 4. Authored UI Assembly Contract

The user creates and positions the UI and assigns references manually:

- Existing UI Canvas, initially inactive TowerInfoWindow content root.
- Fixed Title text and Basic Stats parent containing individual text/Image rows.
- References for DisplayName, Description, Icon, Level, AttackRange, and Attack.
- Upgrade Info parent using Grid Layout and an icon UI prefab containing an Image.
- Close button.
- Full-screen semitransparent black Image mask below window content, above underlying
  HUD surfaces, with Raycast Target enabled.

The script must support opening an initially inactive window through an explicitly
bound active session owner. Do not require the hidden view's Update or OnEnable to
discover dependencies or accept the opening request. Bind Close once and avoid
duplicate listeners across activation cycles.

Generate one icon per acquired Upgrade, in acquisition order. Empty upgrades leave
an empty container. Own and remove only generated icon instances, preserving authored
children/templates. Deferred destruction must not leave old items visible or counted
by layout during immediate close/reopen. Icons provide no click action.

Provide assembly instructions and diagnostics; do not automatically generate or
redesign the user's Canvas layout. Native checks become complete after user assembly.

## 5. Session And Close Contract

Validate eligibility, references, snapshot, and prepared content; acquire Task001's
exclusive pause; then activate the populated window. No partial visible content
or acquired pause survives an opening failure or cancellation during activation.
Only one target/session is current. A second open cannot replace an existing target.

Close hides content, retires generated icons, clears target/session state, and
releases only the exact current pause handle. A mask click has no action. Explicit
Close, disable, disposal, and target invalidation converge on idempotent cleanup;
reentrant OnDisable cannot release twice or clear a newer opening.

Expose current inspection/modal availability to Task003. Window visibility alone
is insufficient authority. Production Battle end, stop, release, retry, and Stage
replacement wiring belongs to Task003 and calls this task's cancellation capability.
Neither opening nor closing moves the Camera or mutates Tower/reward state.

## 6. Plan Review Requirements

Specify view versus active-session owner, the inactive-root opening path, snapshot
and combat-query API, deployed-membership validation, reference failure behavior,
numeric formatting, icon instance ownership, listener lifetime, and reentrant
opening/closing rollback. Describe how target removal and external disable notify
or invalidate the session even while simulation is paused.

Define the Task003-facing opening/cancellation interfaces and which session owns
the Task001 handle. Avoid introducing a second pause writer or duplicate Tower registry.

## 7. Acceptance And Evidence

Automated checks cover a base Tower, changed level, range/damage Upgrades, no
Upgrades, several acquired Upgrades in order, switching targets after close,
repeated open/close, missing required references, invalid/uncommitted target data,
content-generation failure, pause rejection, activation cancellation, and disable.
Every rejected/closed case leaves no generated items, target binding, or pause.

Native Unity checks verify manual references, Title preservation, text/image
correctness, Grid Layout, immediate reopen without duplicate icons, full-screen
mask ordering, Close responsiveness during pause, and unchanged Camera view.
Task003 verifies production input blocking and lifecycle end to end.

Record automated/build evidence separately from Play Mode/device and user-confirmed
layout acceptance. Completion requires the data/view/session capabilities and
assembly checklist; pending manual assembly is reported explicitly, not marked passed.
