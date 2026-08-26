# Tower Nexus - Tower Runtime Combat System

Document Set: System

---

# 1. Purpose And Ownership

Tower Runtime Combat System turns one placed Tower's authored data and current upgrade state into live attack orchestration.

It owns:

- Binding one combat session to one placed Tower instance
- Resolving current Tower combat values
- Monster detection and target selection
- Attack readiness and Attack Cycle timing
- Windup admission, release confirmation, and presentation-gated release
- Attack Entity release and ownership registration
- Coordination of approved Live Refresh
- Technical invalidation and cleanup
- Tower-side combat presentation requests

It does not own Tower placement, Tower Upgrade eligibility, Projectile flight, Monster lifecycle, reusable Effect execution, persistent Buff state, or Attack Entity presentation implementation.

Tower Runtime Combat decides when an Attack Entity is released. After release, the entity's runtime domain decides how it moves, hits, and completes.

---

# 2. Combat Session Contract

One active combat session is explicitly bound to one placed Tower instance, its TowerDefinition identity, compatible TowerFamily, active Map/Monster source, current level model presentation, and active Attack Origin.

A combat session becomes valid only after required gameplay references and the initial resolved-value baseline exist. A runtime must not observe Tower state-change notifications while only partially initialized.

New-Tower deployment may prepare a valid combat session before the Tower is committed to the battlefield. This ready state is combat-inactive: it performs no detection, windup, release, or Attack Entity work. After placement commits occupancy and affected-Monster movement revisions, the same transaction registers the deployed Tower and activates its prepared combat session before consuming the held Draft. Readiness and activation use the existing Tower runtime rather than a second combat validator or parallel service.

The following invalidate the session:

- Bound Tower instance is destroyed or unavailable
- Bound TowerDefinition identity changes
- Bound TowerFamily becomes incompatible with the authored archetype
- Required Monster source is unavailable

A missing Attack Origin does not invalidate the complete session. It blocks new detection or release work that needs the origin while already released entities continue independently.

Recovery may reacquire external services such as the Monster source, but it must not silently bind a different Tower instance or accept a changed TowerDefinition identity. Those changes require a new explicit combat session.

---

# 3. Runtime State

Common per-Tower runtime state includes:

- Current resolved combat values
- Remaining Attack Cycle time
- Detected valid Monsters
- Current selected target when required
- Optional pending attack topology
- Active released-entity registries
- Archetype-specific scheduler state

None of this state belongs in reusable definitions or authored templates.

The shared attack state is conceptually:

| State | Meaning |
|---|---|
| Ready | No admitted attack is waiting for presentation release |
| Waiting For Release | Windup topology is locked while awaiting release-time target confirmation |

Long-lived Magic Orb and Drone entities have their own runtime state after release.

---

# 4. Detection And Target Selection

Tower Runtime Combat queries alive, gameplay-targetable Monsters and filters them by current resolved Attack Range measured from the active Attack Origin to the Monster Hit Reference.

A valid candidate must be active, alive, registered with Monster System, and inside the relevant range.

Archer, Cannon, and Drone apply their authored target-selection category. Magic Orb does not require a release target for baseline contact behavior.

Detection and selection never spawn, move, damage, or change Monster state.

---

# 5. Windup Admission, Release Confirmation, And Release

For presentation-driven Archer and Cannon attacks, Windup admission is distinct from release confirmation and Attack Entity release.

Windup admission requires at least one current valid target. That target only proves that the Tower may begin its attack presentation; it is not the locked target of the future Projectile. Windup freezes only the already-started attack's structural identity:

- Release-group identity
- Member or slot topology such as Center, Left, and Right Arrow slots
- Initial Shell count
- Package-owned additional-member identity required by that topology

At the presentation Release Moment, Archer and Cannon select again from current valid in-range candidates. This release confirmation freezes the target identity, direction, and target-position snapshot required by the actual Projectile. Package identity and DamageScale are resolved at their reviewed Windup or Release boundary. The final Tower-owned integer damage is not frozen merely because release occurred; it resolves from the source Tower's current BasicDamage at the actual future damage boundary.

```text
Ready And Valid Target
    -> Admit Windup
    -> Lock Attack Topology
    -> Request Attack Presentation
    -> Receive Release Moment Or Use Fallback
    -> Select Current Release Target Or Targets
    -> Lock Release Identity, Direction, And Position
    -> Release Attack Entity Or Cancel
```

If no valid primary target exists at the Release Moment, the pending attack is cancelled, no Attack Entity is created, and no Attack Cycle begins. A successfully released Projectile receives immutable launch direction or landing position and never tracks later Monster movement.

Pending attacks are not released Attack Entities. They are not registered as active entities and do not receive Live Refresh commands intended for released runtime state.

## 5.1 Presentation Replacement During A Pending Attack

If a Tower level change replaces the active level model while an attack waits for release:

- The pending Windup topology remains unchanged.
- The new model presentation receives the same attack presentation request.
- The current Attack Origin is resolved from the new model.
- If the new presentation cannot accept the request, the attack releases immediately through the approved fallback.
- A late release signal from the removed model cannot release the same attack twice.

Pending data is cleared only by cancellation, successful release, or technical cleanup.

---

# 6. Attack Cycle Contract

Attack Cycle Duration begins when the approved Attack Entity is successfully released:

- Archer: after an Arrow group is successfully released
- Cannon: after a Shell group is successfully released
- Drone: after one Drone is successfully launched
- Magic: after one complete Magic Orb group is successfully created and activated

Failed Windup admission, failed release confirmation, missing required release data, or failed entity creation does not start an Attack Cycle.

Projectile completion does not delay Archer or Cannon readiness. Drone additionally requires active Drone count below current capacity.

Magic Attack Cycle time runs concurrently with its active Orb group. Shared lifetime expiry finishes all synchronous contact, Arcane Detonation, and member-completion results before exact ownership is cleared. Completion does not restart or extend the running Cycle. Magic may release again only when the Cycle is ready and no active group remains.

When a Cycle becomes ready while another gate remains blocked, it stays ready at zero. Clearing the gate allows the next normal detection and release pass; completion and upgrade notifications never create a new Attack Entity directly.

---

# 7. Runtime Data Timing

Every combat value belongs to one timing category:

| Category | Contract |
|---|---|
| Static Authoring | Reusable data that does not change during the battle |
| Windup Snapshot | Release-group and member topology locked when attack presentation begins |
| Release Snapshot | Value resolved when an Attack Entity is actually released |
| Live Refresh | Approved future behavior of an already released owned entity may change |
| Entity State | Consumed history, elapsed time, progress, and completed results that never reset |

Live Refresh changes only future unresolved behavior. It never rewrites:

- Elapsed lifetime
- Movement progress
- Captured positions or origins
- Hit or bounce history
- Consumed counts
- Already resolved damage or Effects
- Completed state transitions

Elemental identity remains a live lookup at each explicitly eligible attack boundary. Earlier attack results are never replayed.

Every Tower-owned damage result uses:

```text
TowerOwnedDamage
    = RoundToInt(CurrentResolvedBasicDamage At Damage Boundary * Stable DamageScale)
```

The source Tower identity, damage-source identity, and DamageScale are stable inputs of the released or persistent result. Damage-source identity distinguishes Primary, Additional, Bounce, and Behaviour Effect results; a Behaviour Effect additionally identifies its EffectDefinition and authored action ordinal. CurrentResolvedBasicDamage is read when that result actually damages a Monster. A Level or Basic Damage Bonus change may therefore change later unresolved hits, contacts, explosions, field ticks, or dive impacts without changing topology, targets, captured positions, histories, package identity, or already completed results.

One immutable damage-resolution fact is produced at each actual damage boundary. It contains the source Tower family and Level, Level BasicDamage, raw Damage Bonus, resolved BasicDamage, damage-source identity, DamageScale, raw product, and final integer. Gameplay, result context, and read-only diagnostics consume that same fact; none recalculates it. DamageScale, resolved BasicDamage, and raw product must be finite and positive. Invalid source or numeric state rejects the complete Tower-owned result rather than clamping it or partially continuing its Behaviour or Elemental consequences.

Resolution and application observations are exception-isolated and read-only.
A resolution observation records either the immutable successful fact or one
explicit rejection reason. A later application observation references that
same fact and records the number of successful target applications. Diagnostic
failure cannot reject, repeat, or modify gameplay damage. Combat-balance JSON
aggregates these observations by complete TowerScaled signature instead of
emitting one record per hit.

---

# 8. Accepted Combat-Value Change Transaction

An accepted Level Up arrives with its next combat baseline already prepared. Its semantic commit applies that baseline through pure cache assignment after the Tower Level changes and before the exact held Draft is consumed. Baseline apply performs no validation, virtual callback, active-entity traversal, event publication, or diagnostics. `OnLevelChanged` is an exception-isolated post-commit notification and never owns gameplay refresh.

An accepted Basic Upgrade that changes non-damage scheduler or package values performs one ordered runtime refresh transaction:

```text
Read Previous Resolved Baseline
    -> Resolve New Values From Updated Tower State
    -> Commit New Baseline
    -> Adjust Tower Scheduler
    -> Refresh Eligible Owned Entities
    -> Reconcile Matching Behaviour Package
```

Committing the new baseline before entity dispatch ensures multiple accepted changes in the same frame compose from the immediately preceding state.

Rejected changes create no refresh transaction. A Level-only BasicDamage change needs no active-entity damage rewrite because every unresolved Tower-owned result reads the committed baseline at its actual damage boundary. Model presentation, notification, and Upgrade eligibility remain separate post-commit consequences.

Before dispatch, active registries are treated as stable snapshots so entity completion during refresh cannot skip another entity or mutate the iteration source.

Each released entity receives only relevant typed values or package commands, never the source Tower's complete upgrade state.

## 8.1 Scheduler Refresh

Attack Range becomes the current detection range and may refresh active entities whose future behavior explicitly depends on it. Captured target positions and release-time range origins remain immutable.

Attack Cycle Duration preserves current Cycle completion ratio:

```text
Ready Cycle                     -> Remains Ready
Positive Old Duration           -> Remaining *= New / Old
Non-Positive Old Duration       -> Remaining Becomes Ready
```

The result is non-negative. Completing an Attack Cycle through refresh does not release an entity until the next normal scheduler pass. Magic applies the same ratio rule while its Orb group is active because the Cycle began at successful group release.

## 8.2 Entity Refresh

- Future unresolved Tower-owned damage observes the committed current resolved BasicDamage through live resolution while retaining its existing stable DamageScale and package timing; no entity receives a replacement integer-damage snapshot or damage-refresh payload.
- Additive capacities such as Piercing change remaining state by the resolved delta rather than resetting from a new maximum.
- Atomic multi-field changes are applied together before completion is evaluated.
- Ended, disabled, impacted, or otherwise terminal entities reject refresh.
- Package-specific immutable boundaries override generic refresh eligibility.

Detailed projectile refresh behavior belongs to Projectile System. Detailed package eligibility belongs to Tower Upgrade System.

---

# 9. Archer Runtime

Archer Windup admission creates one stable release group with Center and optional Left/Right slots.

- The Center slot is the authoritative main attack.
- Scatter Arrow fixes the slot topology and side-member authoring identity at Windup admission.
- An Upgrade during the presentation wait does not add slots.
- At release, Archer selects one current valid in-range Center target and freezes its current direction. Side directions are derived from that release direction and the Windup-frozen Scatter shape.
- Current Piercing and Explosive Arrow package values are resolved while Windup topology remains unchanged. The Center uses the Tower's Arrow template and DamageScale `1`; side members use Scatter Arrow's additional-entity template and package DamageScale. Every later direct or explosion result resolves current BasicDamage at its own damage boundary.
- If no valid Center target exists at release, the group is cancelled without starting an Attack Cycle.
- Released directions are immutable and do not follow later target movement.

Successful release starts one Attack Cycle and transfers Arrow movement, hit, Piercing, Explosive Arrow, and completion behavior to Projectile System.

---

# 10. Cannon Runtime

Cannon Windup admission freezes only the initial Shell count and additional-member authoring identity.

- Baseline Cannon admits one initial Shell.
- Multi Shells may admit one primary Shell plus its authored positive additional-member count.
- One attack uses one presentation sequence and one Attack Cycle.
- At release, Cannon selects one current valid in-range primary target and as many distinct additional targets as the frozen Shell topology permits. Each released Shell freezes that intended Monster identity and its current Hit Reference as the immutable landing position.
- If no valid primary target exists at release, the group is cancelled without starting an Attack Cycle. Missing additional targets reduce the released member count but do not redirect multiple Shells to one target implicitly.
- Intended Monster movement or invalidation after release does not cancel or redirect a captured position. Projectile System may use the reference only to prioritize the optional direct Monster Hit at that position.
- An Upgrade during the presentation wait does not add Shells.
- Each successful initial Shell release receives one stable direct DamageScale plus Explosive Shell and eligible pre-impact Bouncing Shell data. The primary Shell uses the Tower's Shell template and DamageScale `1`; additional Shells use Multi Shells' additional-entity template and package DamageScale.
- A BasicDamage refresh does not rewrite Shell topology, landing position, chain history, or stable scale. Initial and bounce direct results read current resolved BasicDamage when each impact damage result occurs.

Successful release starts one Attack Cycle. Arc movement, Position Impact, direct arrival query, bounce-chain behavior, and completion belong to Projectile System.

---

# 11. Magic Orb Runtime

Magic Tower owns at most one active synchronized Orb group.

Release requires Attack Cycle readiness and no active group. The group captures one orbit center from the current Attack Origin. Successful group activation begins both the active-group phase and one Attack Cycle.

Group contract:

- One shared orbit phase, lifetime, and completion reason
- Evenly distributed member angles
- Independent contact history per member
- Shared lifetime completion ends the whole group
- Group completion is idempotent
- The primary member uses the Tower's Orb template and DamageScale `1`; additional members use Multi Orbs' additional-entity template and authored DamageScale.
- Every contact resolves damage from the current shared resolved BasicDamage and that member's stable DamageScale.

Approved active-group refresh may update orbit speed and reviewed Behaviour packages. Unresolved damage is not refreshed or rewritten: every later contact or Effect execution reads the source Tower's current resolved BasicDamage at that boundary. No gameplay hit capacity, hit-exhaustion completion branch, or maximum-hit refresh exists.

Multi Orbs reconciliation is atomic:

- Determine missing membership.
- Prepare every missing member without making it active.
- If any candidate fails, discard only candidates and preserve the original group.
- Revalidate the group and commit all missing members together.
- Existing members keep lifetime and contact history.
- Newly committed members use the active package's configured template and DamageScale.

Arcane Detonation is eligible only on normal group completion and executes once at each active member's current position before the group disappears. Technical cleanup never triggers it.

After all normal completion results finish, exact group ownership is cleared without modifying the running Attack Cycle. The completion notification does not create the next group. Technical cleanup clears both the group and scheduler state; a later valid combat-session recovery begins from ready scheduler state.

## 11.1 Arcane Field

Arcane Field is one Tower-owned persistent field created when its Upgrade becomes active.

- It follows the owning Tower.
- It does not duplicate on unrelated changes.
- Its complete runtime prefab comes from the Upgrade definition and owns its presentation. The prefab root's MagicArcaneFieldBehaviour owns its authored radius, interval, and tick Effect.
- Each tick resolves valid Monsters inside its radius and deals its authored
  Behaviour damage without granting an ordinary Elemental opportunity.
- Each tick's Behaviour damage is Tower-owned and uses the current resolved BasicDamage with the field Effect's stable DamageScale.
- It ends when the owning combat session or Tower ends.

It is not an Attack Entity released by the ordinary Magic scheduler.

---

# 12. Drone Runtime

Drone Tower launches one Drone per successful scheduler pass while active count is below current capacity.

The primary active slot uses the Tower's Drone template and DamageScale `1`. Multi Drones adds the package-authored number of secondary slots; each secondary slot uses its configured additional-entity template and DamageScale. Capacity changes do not batch-fill slots. If the primary slot becomes vacant while secondary Drones remain active, the next eligible scheduler pass restores the primary slot.

Each Drone owns:

- Launch movement and release position
- Current target and allowed retargeting
- Orbit movement
- Burst state and projectile releases
- Battery state
- Optional Final Dive branch
- Completion and unregistering

Launching does not consume battery. Battery begins during active combat flight.

If a normal Drone target becomes invalid, it may select another valid Monster inside the current refreshed range. If none exists, it ends through aerial despawn without ordinary impact gameplay.

Normal retargeting preserves the current Burst phase, remaining shot count, timer, and already assigned Elemental-opener state. It never reloads a Burst, bypasses Inter-Burst Cooldown, or grants another Elemental contribution.

Burst cadence has three semantic phases:

| Phase | Contract |
|---|---|
| Ready To Start Burst | A new burst may begin during normal Drone update |
| Between Shots | Remaining shots use the current burst's authored spacing |
| Inter-Burst Cooldown | Timer before a future burst; approved cooldown refresh preserves its ratio |

Each Drone instance grants ordinary Elemental eligibility only to the opening
Projectile of a genuinely new Burst. Successful release freezes the eligibility
onto that Projectile. Later shots in the same Burst remain ineligible; an opener
miss or technical cleanup does not transfer eligibility, and retargeting does
not re-arm it. Primary and additional Drones own independent Burst state but
remain one Buff contribution source through their owning Tower instance.

Battery Duration is static Drone entity authoring for the battle. Applying a Tower Upgrade does not refresh remaining battery or rewrite the battery-end boundary.

High-Caliber Rounds is an ordinary deterministic Basic Damage Bonus. It updates current resolved BasicDamage and therefore affects future unresolved Drone direct, Blast Rounds, and Final Dive damage. It does not alter shared-state FixedBuff lifecycle, Overload, or Elemental hit-reaction damage.

Final Dive, when active at battery end, locks one target and becomes one-way:

- Ordinary firing stops.
- No new target is selected.
- The Drone pursues the target's current Hit Reference while valid.
- A last valid position is retained if the target becomes invalid.
- Reaching the active destination produces Position Impact.
- A local nearest-Monster query may additionally produce one direct Monster Hit.
- The package explosion executes after that optional direct result.
- The Drone completes after all synchronous impact results.

Detailed Drone-fired projectile behavior belongs to Projectile System.

---

# 13. Elemental Opportunity Boundary

Runtime Combat and Attack Entities grant ordinary Elemental application only
through the baseline primary attack path. Behaviour-added or Behaviour-extended
results may preserve their authored damage, targets, state, and completion, but
they have no ordinary Elemental authorization.

| TowerFamily | Authorized baseline primary boundary | Unauthorized Behaviour results |
|---|---|---|
| Archer | Center primary Arrow's first valid Monster Hit | Side Arrows, later Piercing hits, Explosive Arrow area targets |
| Cannon | Primary initial Shell's baseline direct Monster result | Additional initial Shells, bounce children, Explosive Shell area targets |
| Magic | Each valid primary Orb contact | Additional Orb contacts, Arcane Detonation targets, Arcane Field tick targets |
| Drone | Primary Drone's Burst-opening Projectile direct hit | Later shots, every additional-Drone Projectile, Blast Rounds targets, Final Dive direct and explosion targets |

Eligibility is explicit topology authorization. It is not inferred from being a
Projectile, Effect, positive-damage result, Attack Entity, or from the equipped
Elemental Upgrade. A target-specific candidate is classified before its damage
result. If that damage removes the Monster, the candidate remains historically
eligible but no application request is dispatched. Miss, invalidation, and
technical cleanup never transfer authorization to a later result.

Archer primary authorization is immutable release identity plus one-way consumed
state. The first valid center-Arrow impact consumes it before damage, even when
that damage prevents application; later Piercing hits cannot regain it. Cannon
bounce children are always unauthorized. Magic primary Orb authorization may be
used at every valid contact. Drone opening-slot identity is separate from
Elemental authorization: an additional Drone's opener is still an opener, but it
is unauthorized.

Every dispatched Elemental application carries the owning source Tower identity
and the current Elemental Upgrade's positive contribution. Attack Entity identity
does not become a Buff cooldown source. Different Tower instances remain
independent sources even when they share TowerFamily and ElementType.

Effect System owns authorization forwarding and request dispatch. Buff System
owns cooldown, stacking, Protection, overload, and lifecycle after receiving the
request; it never infers attack-result provenance.

## 13.1 Tower-Owned Elemental Hit Fact

Every successfully committed positive Tower-owned damage result produces one
target-specific hit fact after damage and before presentation publication. That
fact is available to an already-active ElectricShock or Windcut Buff even when
the result came from an additional member, continuation, bounce, area,
persistent tick, completion result, Blast area, or Final Dive result.

The hit fact does not authorize Elemental application. Baseline-primary
application remains governed by the matrix above. Behaviour results therefore
may trigger normal shared Electric/Wind value on a Buff applied by another hit,
but they never create, refresh, stack, or overload that Buff. FixedBuff damage,
Buff lifecycle damage, Overload results, WindVortex ticks, and Elemental hit-
reaction damage never produce a Tower-owned hit fact.

When the same hit applies ElectricShock or Windcut, the committed new Buff joins
that hit's reaction set. Electric is evaluated before Wind when both coexist.
Runtime gameplay owns eligibility, ordering, and target-specific committed
outcomes; Recorder observations are read-only.

---

# 14. Technical Cleanup

Technical cleanup applies when a combat session is replaced, invalidated, disabled, or removed.

It:

- Cancels pending Windup, release-confirmation, and release work
- Removes persistent Tower-owned runtime such as Arcane Field
- Force-completes every still-owned Attack Entity
- Clears registries and scheduler state
- Detaches state-change observation

Technical cleanup produces no gameplay completion results: no damage, Impact, Elemental attempt, Arcane Detonation, Final Dive, or ordinary completion Effect.

Technical cleanup does not preserve or restart an Attack Cycle. A later valid combat-session recovery establishes fresh scheduler state from the preserved placed-Tower identity and current resolved values.

Cleanup and unregister operations are idempotent.

---

# 15. Validation

Runtime validation should reject or report at minimum:

- Missing or incompatible bound Tower identity
- Missing Monster source
- Missing required Attack Entity configuration
- Missing active Attack Origin at release time
- Invalid Level-authored BasicDamage, range, Attack Cycle duration, capacity, or package values
- Duplicate active group identity or invalid slot membership
- Release data that cannot satisfy its archetype contract

Presentation-only failure uses approved fallback behavior and must not silently change Windup topology.

---

# 16. Approved Scope And Deferred Topics

Current scope includes Archer, Cannon, Magic, and Drone orchestration; presentation-gated release; Attack Cycles; target selection; active entity ownership; selective Live Refresh; Arcane Field; and technical cleanup.

Object pooling, Tower demolition, Monster-driven Tower destruction, new attack archetypes, and unreviewed package behavior are deferred.
