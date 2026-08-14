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
- Attack confirmation and presentation-gated release
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
- Optional pending attack confirmation
- Active released-entity registries
- Archetype-specific scheduler state

None of this state belongs in reusable definitions or authored templates.

The shared attack state is conceptually:

| State | Meaning |
|---|---|
| Ready | No confirmed attack is waiting for presentation release |
| Waiting For Release | Attack topology and confirmation snapshots are locked while awaiting the authored release moment |

Long-lived Magic Orb and Drone entities have their own runtime state after release.

---

# 4. Detection And Target Selection

Tower Runtime Combat queries alive, gameplay-targetable Monsters and filters them by current resolved Attack Range measured from the active Attack Origin to the Monster Hit Reference.

A valid candidate must be active, alive, registered with Monster System, and inside the relevant range.

Archer, Cannon, and Drone apply their authored target-selection category. Magic Orb does not require a release target for baseline contact behavior.

Detection and selection never spawn, move, damage, or change Monster state.

---

# 5. Attack Confirmation And Release

An attack confirmation is distinct from Attack Entity release.

Confirmation locks only information that represents the already-made attack decision:

- Main target identity where required
- Captured target positions
- Release-group identity
- Member or slot topology such as Center, Left, and Right Arrow slots
- Initial Shell count
- Confirmation-time fallback directions

Values that define the entity at actual release, such as unresolved damage and approved current package options, are resolved at release unless their contract explicitly says they are confirmation snapshots.

```text
Ready And Valid Target
    -> Confirm Attack
    -> Lock Confirmation Topology And Snapshots
    -> Request Attack Presentation
    -> Receive Release Moment Or Use Fallback
    -> Revalidate Required Confirmation Data
    -> Release Attack Entity Or Cancel
```

Pending attacks are not released Attack Entities. They are not registered as active entities and do not receive Live Refresh commands intended for released runtime state.

## 5.1 Presentation Replacement During A Pending Attack

If a Tower level change replaces the active level model while an attack waits for release:

- The pending confirmation remains unchanged.
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

Failed confirmation, missing required release data, or failed entity creation does not start an Attack Cycle.

Projectile completion does not delay Archer or Cannon readiness. Drone additionally requires active Drone count below current capacity.

Magic Attack Cycle time runs concurrently with its active Orb group. Shared lifetime expiry finishes all synchronous contact, Arcane Detonation, and member-completion results before exact ownership is cleared. Completion does not restart or extend the running Cycle. Magic may release again only when the Cycle is ready and no active group remains.

When a Cycle becomes ready while another gate remains blocked, it stays ready at zero. Clearing the gate allows the next normal detection and release pass; completion and upgrade notifications never create a new Attack Entity directly.

---

# 7. Runtime Data Timing

Every combat value belongs to one timing category:

| Category | Contract |
|---|---|
| Static Authoring | Reusable data that does not change during the battle |
| Confirmation Snapshot | Identity, topology, or position locked when the attack is confirmed |
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

---

# 8. Accepted Combat-Value Change Transaction

After an accepted Basic Upgrade changes resolved combat values, the bound runtime performs one ordered semantic transaction:

```text
Read Previous Resolved Baseline
    -> Resolve New Values From Updated Tower State
    -> Commit New Baseline
    -> Adjust Tower Scheduler
    -> Refresh Eligible Owned Entities
    -> Reconcile Matching Behaviour Package
```

Committing the new baseline before entity dispatch ensures multiple accepted changes in the same frame compose from the immediately preceding state.

Rejected changes create no refresh transaction. Tower Level changes replace level presentation and eligibility state without creating a combat-value refresh transaction in the v0.1 growth model.

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

- Unresolved damage may refresh for eligible active entities.
- Additive capacities such as Piercing change remaining state by the resolved delta rather than resetting from a new maximum.
- Atomic multi-field changes are applied together before completion is evaluated.
- Ended, disabled, impacted, or otherwise terminal entities reject refresh.
- Package-specific immutable boundaries override generic refresh eligibility.

Detailed projectile refresh behavior belongs to Projectile System. Detailed package eligibility belongs to Tower Upgrade System.

---

# 9. Archer Runtime

Archer confirmation creates one stable release group with Center and optional Left/Right slots.

- The Center slot is the authoritative main attack.
- Each confirmed slot captures a distinct target candidate when available and a fallback direction.
- Scatter Arrow fixes the slot topology at confirmation.
- An Upgrade during the presentation wait does not add slots.
- At release, current Damage, Piercing, and Explosive Arrow values are resolved while confirmed topology remains unchanged. The Center uses the Tower's Arrow template and resolved Attack Damage; side members use Scatter Arrow's additional-entity template and `Additional Basic Damage + resolved Damage Bonus`.
- If the Center release requirement is invalid, the group is cancelled.
- An invalid secondary target uses its confirmation-time fallback direction rather than free retargeting.

Successful release starts one Attack Cycle and transfers Arrow movement, hit, Piercing, Explosive Arrow, and completion behavior to Projectile System.

---

# 10. Cannon Runtime

Cannon confirmation captures one or more immutable target-position snapshots and the corresponding intended Monster references.

- Baseline Cannon confirms one position.
- Multi Shells may confirm one primary position plus its authored positive additional-member count.
- One attack uses one presentation sequence and one Attack Cycle.
- Intended Monster invalidation after confirmation does not cancel or redirect a captured position. Projectile System may use the reference only to prioritize the optional direct Monster Hit at that position.
- An Upgrade during the presentation wait does not add Shells or recapture positions.
- Each successful initial Shell release receives one immutable integer direct-damage value plus Explosive Shell and eligible pre-impact Bouncing Shell data. The primary Shell uses the Tower's Shell template and current resolved Cannon Attack Damage; additional Shells use Multi Shells' additional-entity template and `Additional Basic Damage + resolved Damage Bonus`.
- Damage Bonus live refresh may update an unresolved primary initial Shell, but it cannot overwrite an additional initial Shell's composed release-time direct damage or any bounce child's fixed Bounce Damage.

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
- The primary member uses the Tower's Orb template and base damage; additional members use Multi Orbs' additional-entity template and authored Basic Damage.
- Every member composes its own Basic Damage with the current shared resolved Damage Bonus.

Approved active-group refresh may update unresolved damage, orbit speed, and reviewed Behaviour packages. No gameplay hit capacity, hit-exhaustion completion branch, or maximum-hit refresh exists.

Multi Orbs reconciliation is atomic:

- Determine missing membership.
- Prepare every missing member without making it active.
- If any candidate fails, discard only candidates and preserve the original group.
- Revalidate the group and commit all missing members together.
- Existing members keep lifetime and contact history.
- Newly committed members use the active package's configured template and Basic Damage.

Arcane Detonation is eligible only on normal group completion and executes once at each active member's current position before the group disappears. Technical cleanup never triggers it.

After all normal completion results finish, exact group ownership is cleared without modifying the running Attack Cycle. The completion notification does not create the next group. Technical cleanup clears both the group and scheduler state; a later valid combat-session recovery begins from ready scheduler state.

## 11.1 Arcane Field

Arcane Field is one Tower-owned persistent field created when its Upgrade becomes active.

- It follows the owning Tower.
- It does not duplicate on unrelated changes.
- Its complete runtime prefab comes from the Upgrade definition and owns its presentation. The prefab root's MagicArcaneFieldBehaviour owns its authored radius, interval, and tick Effect.
- Each tick resolves valid Monsters inside its radius and grants the reviewed Elemental opportunity.
- It ends when the owning combat session or Tower ends.

It is not an Attack Entity released by the ordinary Magic scheduler.

---

# 12. Drone Runtime

Drone Tower launches one Drone per successful scheduler pass while active count is below current capacity.

The primary active slot uses the Tower's Drone template and base damage. Multi Drones adds the package-authored number of secondary slots; each secondary slot uses its configured additional-entity template and `Additional Basic Damage + resolved Damage Bonus`. Capacity changes do not batch-fill slots. If the primary slot becomes vacant while secondary Drones remain active, the next eligible scheduler pass restores the primary slot.

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

Normal retargeting preserves the current Burst phase, remaining shot count, and timer. It never reloads a Burst or bypasses Inter-Burst Cooldown.

Burst cadence has three semantic phases:

| Phase | Contract |
|---|---|
| Ready To Start Burst | A new burst may begin during normal Drone update |
| Between Shots | Remaining shots use the current burst's authored spacing |
| Inter-Burst Cooldown | Timer before a future burst; approved cooldown refresh preserves its ratio |

Battery Duration is static Drone entity authoring for the battle. Applying a Tower Upgrade does not refresh remaining battery or rewrite the battery-end boundary.

High-Caliber Rounds is an ordinary deterministic Basic Damage Bonus. It updates eligible active Drones through the same Damage Bonus refresh path and affects future Drone projectile direct damage. It does not alter Blast Rounds or other independently authored Effect damage.

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

Runtime Combat and Attack Entities grant Elemental application only at explicitly reviewed attack boundaries.

Eligibility is not inferred from being a Projectile, Effect, positive-damage result, or Attack Entity. Damage resolves first; if that result removes the Monster, the following Elemental attempt has no valid target.

Every Elemental application opportunity carries the owning source Tower identity. Primary attacks, additional Attack Entities, area results, bounce children, contacts, and persistent attack entities produced by one Tower remain one contribution source for Source Apply Cooldown. Attack Entity identity never becomes an independent Buff cooldown source. Different Tower instances remain independent sources even when they share TowerFamily and ElementType.

Effect System and Buff System own the application, cooldown, stacking, Protection, overload, and lifecycle result after an eligible opportunity is emitted.

---

# 14. Technical Cleanup

Technical cleanup applies when a combat session is replaced, invalidated, disabled, or removed.

It:

- Cancels pending confirmation and release work
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
- Invalid base damage, range, Attack Cycle duration, capacity, or package values
- Duplicate active group identity or invalid slot membership
- Release data that cannot satisfy its archetype contract

Presentation-only failure uses approved fallback behavior and must not silently change confirmation topology.

---

# 16. Approved Scope And Deferred Topics

Current scope includes Archer, Cannon, Magic, and Drone orchestration; presentation-gated release; Attack Cycles; target selection; active entity ownership; selective Live Refresh; Arcane Field; and technical cleanup.

Object pooling, Tower demolition, Monster-driven Tower destruction, new attack archetypes, and unreviewed package behavior are deferred.
