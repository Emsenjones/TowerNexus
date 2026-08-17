# Tower Nexus - Projectile System

Document Set: System

---

# 1. Purpose And Ownership

Projectile System owns projectile-style Attack Entities after they are successfully created and supplied with complete release data.

It owns:

- Projectile runtime state
- Direction and Arc flight
- Hit and arrival detection
- Lifetime and hit history
- Position Impact and Monster Hit fact generation
- Simple direct-hit dispatch
- Projectile-specific Behaviour state
- Impact presentation requests
- Completion and cleanup

It does not own Tower targeting or cooldowns, damage formulas, reusable area Effect execution, Buff rules, Monster health state, or Tower Upgrade eligibility.

Projectile behavior is selected by flight identity and typed runtime data rather than source Tower type.

---

# 2. Projectile Lifecycle

```text
Create With Valid Release Data
    -> Move
    -> Resolve Zero Or More Monster Hits
    -> Resolve Position Impact When Applicable
    -> Execute Approved Projectile Results
    -> Present Impact
    -> Complete
```

Every projectile has a finite lifetime safety boundary. Lifetime prevents abandoned entities but does not replace behavior-specific limits such as Piercing hit count or Bouncing Shell count.

Normal completion may emit reviewed gameplay results. Technical cleanup removes the projectile without hit, Impact, damage, Effect, Elemental opportunity, or ordinary completion presentation.

Completion is idempotent.

---

# 3. Projectile Entity Authoring

Each Projectile entity template carries its reusable projectile-specific authoring on its Projectile Behaviour:

| Data | Contract |
|---|---|
| Movement Speed | Base movement rate |
| Hit Distance Threshold | Monster-contact threshold or Arc-arrival query radius |
| Maximum Lifetime | Safety lifetime |
| Optional Impact Effect | Reusable one-shot gameplay Effect executed at the approved impact boundary |
| Optional Impact Presentation | Presentation requested on impact |

Every released Projectile is created from a template that already contains a valid Projectile Behaviour and complete base authoring. Runtime code does not add a missing Projectile Behaviour or synthesize default authoring.

Projectile entity authoring does not contain Tower damage, Attack Range, Attack Interval, Tower archetype, Tower Upgrade state, initial Arc height, or flight identity.

Impact presentation and gameplay Effect references are independent. A projectile may present an impact without executing a gameplay Effect.

## 3.1 Orientation Contract

Projectile template roots use:

- Local +Z as forward
- Local +Y as up

The root faces its current movement direction. Imported model differences are corrected inside the visual hierarchy rather than through projectile-specific runtime offsets.

---

# 4. Runtime Data Categories

A projectile receives only data relevant to its own execution, such as source context, flight identity, launch direction or target snapshot, resolved damage, Elemental context, and approved package options.

Immutable Entity State includes:

- Captured landing position
- Launch direction
- Flight progress
- Elapsed lifetime
- Monster hit history
- Bounce history
- Started bounce-chain contract
- Completed results

Approved Live Refresh may replace future unresolved values such as damage, remaining Piercing capacity by delta, pre-impact Explosive or Bouncing capability, and Blast Rounds.

Live Refresh never resets immutable Entity State or replays completed results.

---

# 5. Direction Flight

Direction flight moves along one launch direction and may resolve Monsters encountered within its hit threshold.

- The selected target may define the initial direction.
- The projectile is not locked to that target after release.
- If multiple valid Monsters are simultaneously in threshold, the nearest valid Monster is resolved first using stable source order for ties.
- A Monster already recorded by the same projectile cannot be hit again when the active behavior requires unique-hit history.
- The projectile completes when its hit capacity is exhausted or lifetime ends.

## 5.1 Piercing Arrow

Piercing Arrow gives one Arrow a finite remaining Monster-hit capacity and a set of Monsters already hit.

Each new Monster Hit:

1. Records that Monster.
2. Consumes one remaining hit.
3. Dispatches unresolved direct damage.
4. Emits the reviewed Elemental opportunity.
5. Completes the Arrow when remaining capacity reaches zero.

Applying or increasing Piercing on an active eligible Arrow changes remaining capacity by the resolved maximum delta. It does not clear hit history or restore consumed hits.

## 5.2 Scatter Arrow

Scatter members are independent projectiles inside one stable release group. Each owns its own movement, hit history, remaining Piercing capacity, lifetime, damage, and Elemental results.

The Center member uses the Tower-authored Arrow template and resolved Attack Damage. Side members use Scatter Arrow's additional-entity template and immutable release damage equal to its authored Basic Damage plus the release-time resolved Damage Bonus.

Scatter topology is fixed when the Archer enters Windup and never adds projectiles to that pending or already released group. The Center target and launch direction are selected and frozen at the Release Moment.

---

# 6. Explosive Arrow

Explosive Arrow is a direct-hit Archer Behaviour package, not a flight identity.

- Arrow flight remains Direction flight.
- Each new Arrow Monster Hit first resolves its baseline direct damage and reviewed direct Elemental opportunity.
- When Explosive Arrow is active for that released Arrow, the same hit then executes one package-authored area Effect centered on the hit Monster's current Hit Reference.
- The directly hit Monster remains eligible for the area target set when it is still gameplay-targetable after baseline direct damage.
- A miss, lifetime expiry, or technical cleanup produces no Explosive Arrow Effect.
- Piercing may produce one Explosive Arrow Effect for each new unique Monster Hit.
- Scatter members resolve their own hits and explosions independently.
- Applying Explosive Arrow affects future Arrow releases only and never retrofits an active Arrow.

Explosive Arrow is intentionally a small, frequent direct-hit splash. Cannon Explosive Shell remains a Position Impact Effect and may execute even when no direct Monster Hit exists.

---

# 7. Arc Flight

Arc flight consumes an explicitly present release-time immutable target-position snapshot and initial Arc height.

- Any world position, including the origin, may be valid; absence must be represented explicitly rather than through a sentinel coordinate.
- Source Monster movement or invalidation after release does not cancel or redirect the Shell.
- Arc travel completes at normalized progress one and resolves exactly at the captured position.
- Arrival always produces Position Impact.
- Initial Shell and bounce-child Arc heights come from their respective authoring owners.

After Position Impact, one local query uses `Hit Distance Threshold` as its radius and the impact position as its center. An Arc release may also retain its intended Monster identity without changing the captured landing position or becoming a tracking projectile.

- Candidate distance uses Monster Hit References.
- The threshold boundary is inclusive.
- If the intended Monster remains gameplay-targetable inside the threshold, it is selected first.
- Otherwise, at most one nearest gameplay-targetable Monster is selected as fallback.
- Equal-distance candidates use stable source order.
- A selected Monster additionally produces Monster Hit and may receive direct damage.
- No selected Monster leaves Position Impact valid but produces no Monster-targeted direct result.

Position Impact Effects remain centered on the actual landing position even when a direct Monster is also resolved.

---

# 8. Impact Facts And Result Order

Position Impact and Monster Hit are independent semantic facts:

- Direction contact normally produces Monster Hit.
- Arc arrival always produces Position Impact and may also produce Monster Hit.

For a projectile that has both direct and additional package results, ordering is:

```text
Resolve Optional Direct Monster Hit
    -> Direct Damage
    -> Direct Elemental Opportunity When Authorized
    -> Execute Additive Impact Behaviour Effect
    -> Complete Its Synchronous Damage And State Changes
    -> Continue Package-Specific Follow-Up
    -> Complete Projectile
```

Impact presentation is requested in the same impact-resolution step. Its ordering relative to synchronous gameplay results within that frame is not a gameplay contract. Presentation cannot change result ordering, target eligibility, or completion.

Explosive Arrow, Explosive Shell, and Blast Rounds are additive to their baseline direct result. Their surviving direct target may also be included in the area Effect and may therefore receive two independent damage results and two explicitly authorized Elemental opportunities.

Positive damage or successful damage application is not a universal gate for Elemental opportunity. However, a Monster removed by the preceding damage is no longer a valid target at the following boundary.

Projectile System emits trigger context; Effect System owns reusable Effect execution and Buff System owns persistent outcomes.

---

# 9. Bouncing Shell

Bouncing Shell is a specific Cannon Behaviour package, not a generic ricochet or projectile-spawn framework.

After every Shell Position Impact:

1. Resolve optional direct Monster Hit.
2. Execute Explosive Shell when active.
3. Complete all synchronous damage, death, Buff, and target-state consequences.
4. If bounce capacity remains, search inside the package's local radius around the impact position.
5. Exclude Monsters already resolved by this bounce chain.
6. Apply the package's local selection category to remaining candidates.
7. Capture the selected Monster's current Hit Reference position.
8. Create one bounce child in the same gameplay step.

Direct Monster Hit and positive direct damage are not required to continue the chain. No remaining count or no candidate ends it.

The local selector does not use the source Tower's full Attack Range or normal target-selection value. It operates only after radius and chain-history filtering.

Before an initial Shell's first Position Impact, approved refresh may change unresolved primary damage, Explosive Shell, or whether Bouncing Shell is available. The first Position Impact fixes the chain's remaining count, resolved-target history, fixed Bounce Damage, bounce Arc height, search radius, and local selector. Later refresh cannot extend or rewrite that active chain.

Bounce children:

- Inherit only relevant typed source and package data.
- Use the package-authored positive integer Bounce Damage rather than inheriting parent direct damage.
- Use package-authored bounce Arc height.
- Do not consume Multi Shells again.
- Do not inspect the source Tower's complete Upgrade state.
- Retain live Elemental lookup at each eligible result boundary.

Primary initial Shells use the Tower-authored Shell template and current resolved Cannon Attack Damage. Additional initial Shells use the Multi Shells package's additional-entity template and immutable release damage equal to its positive integer Basic Damage plus the release-time resolved Damage Bonus. Bounce children always use the Bouncing Shell package's positive integer Bounce Damage, including when the parent was an additional Shell or another bounce child. Additional and bounce direct damage is immutable against later refresh, while an unresolved primary initial Shell remains eligible for its reviewed live Damage refresh. Explosive Shell remains an independently authored Effect and uses the same authored Effect damage at every eligible impact. No Cannon Behaviour composes a general damage multiplier.

---

# 10. Drone-Fired Projectiles

A Drone may release projectile-style Attack Entities from its own Fire Anchor.

Drone runtime owns target choice, burst timing, and creation request. Projectile System owns the projectile after release.

Blast Rounds may be refreshed for already airborne unresolved Drone projectiles. A resolved hit is never replayed after refresh.

Drone movement, battery, orbit, and Final Dive do not belong to Projectile System.

---

# 11. Presentation Contract

Projectile travel and impact presentation communicate projectile state only.

- Travel presentation follows projectile motion.
- Impact presentation occurs at the actual Monster Hit or Position Impact boundary.
- Presentation cannot create damage, target queries, Effects, Buffs, or additional projectiles.
- Presentation failure does not alter hit or completion rules.

Particle collision or visual contact is never an independent gameplay authority.

---

# 12. Validation

Projectile authoring and release validation should report or reject at minimum:

- Missing projectile template
- Non-positive speed, lifetime, or required hit threshold
- Missing required launch direction, target, or target-position presence
- Incompatible flight identity and release data
- Invalid Piercing capacity
- Invalid bounce count, radius, Arc height, or selector
- Missing source context required by an authorized Elemental or package result

Invalid data must not be repaired by changing flight identity or inventing a target.

---

# 13. Approved Scope And Deferred Topics

Current scope includes Direction and Arc flight; direct Monster Hits; Position Impact; finite Piercing; Scatter independence; Explosive Arrow; Bouncing Shell; Drone-fired projectiles; and optional impact presentation. Tracking flight is not part of the current projectile schema or runtime contract.

Deferred topics include generic chain, split, boomerang, ricochet, and missile frameworks. New behaviors require explicit contracts rather than Tower-specific branching inside the shared projectile lifecycle.
