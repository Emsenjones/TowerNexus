# Tower Nexus - Monster System

---

# 1. Purpose And Ownership

Monster System owns the complete runtime lifecycle of battlefield Monsters:

- Execution of the Stage-selected MonsterWaveConfig
- Monster spawning
- Normal completion reporting for all configured spawning
- Health and damage reception
- Movement and pathfinding
- Dynamic path recalculation
- Death and Target arrival
- Exactly-once Monster resolution reporting
- Monster-local animation and hit feedback requests
- Monster status presentation data and damage-number requests
- Safe gameplay endpoints used by Effects and Buffs

Stage System supplies the active Map and MonsterWaveConfig before Wave execution begins. Map System supplies spatial and walkability data. Player System owns progress, health, level-up, and defeat consequences reported by Monster System.

Monster System does not select the Stage, modify Player state directly, decide Tower placement, determine Victory or Defeat, execute reusable Effect rules, or own persistent Buff definitions.

---

# 2. Monster Runtime Template

The demo uses one directly referenced Monster runtime template for each unique
Monster type. The template owns that type's reusable authored baseline and
presentation configuration. A spawned Monster instance owns current health,
path state, movement state, Buff state, and resolution state.

| Data | Contract |
|---|---|
| Display Name | Player-facing Monster name |
| Move Speed | Base movement speed |
| Maximum Health | Initial health capacity |
| Hit Reference | Optional authored reference point for targeting and presentation |
| Movement And Death Presentation | Animation identities and death-presentation timing required by this Monster type |
| Hit Feedback Configuration | Hit-reaction and flash authoring owned by the template's hit-feedback behavior |
| Status UI Offset | Monster-specific placement offset for the combined health and active-Buff status display |
| Damage Number Offset | Monster-specific placement offset for transient damage-number presentation |

Spawn Entries reference the runtime template directly. A separate Monster data
definition is not required while one template represents one unique demo
Monster type. Shared visual templates with multiple independent stat profiles
are deferred until a concrete variant requirement exists.

A stable Monster identifier is not required while content is referenced
directly. One should be added only when persistence, external data,
localization, or cross-session lookup requires it.

Runtime values must not be stored back into the reusable template or its
authored component configuration.

---

# 3. Wave Configuration

Each StageDefinition selects one MonsterWaveConfig. Monster System executes that configuration without owning Stage composition.

A MonsterWaveConfig contains an ordered list of Waves. Each Wave contains:

| Data | Contract |
|---|---|
| Wave Delay | Time before the Wave begins |
| Spawn Entries | Ordered Monster groups spawned by the Wave |

Each Spawn Entry contains:

| Data | Contract |
|---|---|
| Monster Runtime Template | Unique Monster type to spawn |
| Count | Number of instances |
| Spawn Interval | Time between adjacent instances inside this entry; it adds no delay after the entry's final instance |

The current Map contract provides one Spawn node and one Target node. Multiple Spawn Routes and route-specific Wave entries are deferred.

Wave execution begins only after Stage composition has established the active Map, supplied a valid MonsterWaveConfig, and the required Initial Tower Draft has produced one held Tower Draft item. Entering the Battle state or merely opening the Initial Draft Window does not begin Wave timing.

The first Wave Delay starts when Monster System receives authorization after the Initial Draft selection is accepted. Tower deployment is not an additional prerequisite; the player may deploy the held Tower Draft item while the first Wave Delay advances.

Wave timing, spawning, Monster movement, and Monster-driven gameplay output do not advance while an Initial or Player level-up Draft Window holds the approved battle-simulation pause. Draft presentation remains interactive outside Monster System. The pause is released before Initial Draft completion authorizes the first Wave Delay.

Monster System reports normal spawning completion only after every configured Monster instance has been created through the complete ordered Wave sequence. Stopping, cancelling, disabling, or aborting invalid Wave execution does not report normal completion.

---

# 4. Monster Lifecycle

The first-version Monster lifecycle contains four states.

| State | Meaning |
|---|---|
| Spawn | Initialize runtime state, resolve Spawn and Target data, and request the initial path |
| Walk | Follow the current path, receive combat results, and update the current Grid Node |
| Arrived | Stop movement and report Target arrival and Monster resolution |
| Dead | Stop movement and pathfinding, present death feedback, and report Monster resolution |

Valid primary transitions are:

```text
Spawn -> Walk
Walk -> Arrived
Walk -> Dead
```

Arrived and Dead are terminal gameplay states. A terminal Monster cannot receive new gameplay effects, deal Target damage again, or report resolution again.

## 4.1 Spawn

Spawn behavior is:

```text
Resolve Required Spawn And Target Data
    -> Establish A Usable Initial Route
    -> Create The Authored Monster Template At Spawn
    -> Initialize Runtime State From Its Authored Baseline
    -> Register The Monster As Alive And Unresolved
    -> Enter Walk
```

Failure to establish required Map data, a usable initial route, a valid runtime
instance, or alive-Monster registration is invalid runtime composition. Any
partially created or registered Monster is cleaned up, Wave execution stops
without normal-completion reporting, and Battle coordination receives a
result-neutral technical failure.

## 4.2 Death

When health reaches zero:

```text
Enter Dead
    -> Stop Movement And Pathfinding
    -> Report Monster Resolved
    -> Present Death Feedback
    -> Remove Monster After Presentation Delay
```

Death feedback cannot postpone or duplicate the gameplay resolution boundary.

## 4.3 Target Arrival

When the Monster reaches the Target node:

```text
Enter Arrived
    -> Stop Movement
    -> Report Monster Resolved With Target-Arrival Fact
    -> Remove Monster From Battlefield
```

Monster System detects Target arrival and includes that fact in the Monster-resolution report. Player System reduces health by one and decides the resulting defeat state in the same atomic transaction that advances progress.

---

# 5. Resolution Contract

A Monster resolves exactly once when it dies or reaches the Target. Every accepted resolution contributes exactly one point of Player progress.

While the battle run is active, Monster System reports one resolution fact to Player System. The report contains whether the Monster reached the Target. Player System uses that single report to update progress, level, health, and defeat coherently.

Player System owns:

- Accumulating resolved progress
- Player level checks
- Player health changes
- Defeat state

After the battle run has stopped, later Monster cleanup must not advance Player progress or reduce Player health.

## 5.1 Battle Completion Facts

Monster System exposes two semantic facts consumed by battle result coordination:

- Whether all configured spawning completed normally
- Whether any alive unresolved Monster remains after one Monster resolution finishes

For one resolved Monster, removal from the alive set occurs before the Player resolution transaction. The post-resolution alive-Monster fact becomes eligible for battle-result evaluation only after Player System has completed progress, health, and Defeat resolution.

This ordering prevents the final Target arrival from appearing victorious before its Player-health consequence is known. Monster System supplies completion facts but does not decide the final Battle result.

Victory requires normal spawning completion and no alive unresolved Monsters, while Player System remains not defeated. Game Flow System owns the transition that follows the authoritative result.

---

# 6. Pathfinding And Movement

Monster System owns runtime pathfinding and movement. Map System owns the graph data: Grid Nodes, effective walkability, neighbor queries, Spawn identity, and Target identity.

The first version uses A* over an orthogonal grid:

- Movement starts from the Monster's current Grid Node.
- The destination is the active Target node.
- Diagonal traversal is disabled.
- Missing or effectively unwalkable nodes are not traversable.
- Movement follows Grid Node center positions.
- The Monster tracks its current node while moving.

When effective walkability changes, every alive non-terminal Monster requests a new path from its current node. Recalculation never restarts from the original Spawn node.

Movement modifiers and movement locks are owned by Monster runtime state. Effects and Buffs request changes through Monster System rather than directly moving the Monster, changing its current node, or bypassing pathfinding.

## 6.1 Placement Path Validation

Tower Placement System owns the decision to accept or reject placement. It uses the same pathfinding rules to simulate the candidate occupied nodes and verifies both the authored Spawn-to-Target route and every alive Monster's current-node-to-Target route.

```text
Simulate Candidate Occupancy
    -> Query Spawn-To-Target Route
    -> Query Each Alive Monster's Current-Node-To-Target Route
    -> All Required Routes Exist: Continue Placement Validation
    -> Any Required Route Missing: Reject Placement
```

Candidate occupancy must not include an alive Monster's current Grid Node. Validation queries must not mutate the active Map or active Monster paths before placement is committed.

---

# 7. Hit Reference Contract

Each Monster may expose a Hit Reference representing a stable, readable position on its body. The Monster root is the safe fallback when no dedicated reference is authored.

The Hit Reference may be used for:

- Tower range and distance evaluation
- Target-position snapshots
- Projectile and Magic Orb hit checks
- Drone orbit or pursuit targeting
- Area inclusion checks
- Hit and persistent Buff presentation attachment
- Status and damage-number positioning

It does not own collision shape, target validity, movement, health, or damage processing.

---

# 8. Effect And Buff Boundary

Monster System provides safe operations for Monster-owned state, including:

- Damage reception
- Effective movement-speed resolution
- Movement lock changes
- Current position and Hit Reference queries
- Path recalculation requests
- Health and lifecycle validity queries

Buff runtime state is attached to one Monster and is cleared when that Monster dies, arrives, despawns, or is reset for reuse. Buff System owns Buff duration, stacking, Protection, and lifecycle rules; Monster System owns the affected Monster capabilities.

Effects and Buffs must not bypass Monster ownership to change world position, current Grid Nodes, death, arrival, or path state.

---

# 9. Monster Presentation Contract

Monster presentation communicates runtime state but does not decide gameplay results.

The current presentation contract includes:

- Idle and walking state presentation
- Death presentation
- Non-blocking hit reaction
- Repeatable hit flash
- Status display for health and active Buffs
- Transient damage-number display
- Persistent Buff presentation attached to the Monster

Presentation failure must not block movement, damage, death, arrival, resolution, or cleanup.

## 9.1 Animation And Hit Feedback

- Walking presentation reflects whether the Monster is currently allowed and able to move.
- A movement lock pauses walking presentation without discarding the current path or node state.
- Death presentation begins after the Dead transition.
- Hit reaction and hit flash may play when damage is received without becoming a separate gameplay state.
- Repeated hits safely replace or compose presentation without altering shared authored visual resources.

Concrete animation controllers, shader techniques, tween libraries, and renderer APIs are implementation choices.

## 9.2 Monster Status Display

One status display follows each active Monster and shows current health plus active Buff state.

Status rules:

- One icon slot represents one active Buff definition, not one icon per stack.
- Stack count may be displayed when greater than one.
- During Protection, the same icon remains visible, stack count is hidden, and the icon uses a repeating Protection emphasis.
- The display is removed when the Monster dies, arrives, despawns, or is reset.
- Persistent Buff world presentation may follow the same Buff state but does not own that state.

The status display is Monster-owned battlefield UI even when composed inside a shared battle UI surface.

## 9.3 Damage Numbers

When a Monster receives a displayed damage result, Monster System requests one transient damage-number presentation containing:

- Displayed value
- Monster reference position
- Optional Monster-specific presentation offset or style

The presentation appears near the Monster, performs its authored visual motion, and then removes or recycles itself. It remains valid even if the Monster dies immediately after the request.

Exact typography, easing, animation channels, preview tools, and pooling strategy are presentation implementation or authoring-guide concerns rather than Monster System rules.

---

# 10. Validation

Monster and Wave authoring validation should report at minimum:

- Missing or invalid Monster runtime template in a Spawn Entry
- Non-positive maximum health
- Negative move speed, count, delay, or interval where invalid
- Empty or invalid Wave content
- Wave execution or the first Wave Delay beginning before the Initial Tower Draft is accepted
- Wave timing, spawning, or Monster movement advancing while a Draft Window holds the battle-simulation pause
- Normal spawning completion reported after cancellation, stop, or invalid Wave execution
- Post-resolution alive-Monster state reported before Player resolution completes
- Missing active Spawn or Target node
- Missing initial Spawn-to-Target route
- Presentation references that are configured but unusable

Validation reports authored errors without silently rewriting content.

---

# 11. Approved Scope And Deferred Topics

Current scope includes:

- One Spawn and one Target
- One directly referenced runtime template per unique demo Monster type
- Stage-selected Wave execution
- Initial-Draft-gated start of the first Wave Delay
- Normal spawning-completion and post-resolution alive-Monster facts
- A* pathfinding and dynamic recalculation
- Health, death, arrival, and exactly-once resolution
- One-point Player progress and one-damage Target-arrival reporting
- Hit Reference
- Monster status and damage-number presentation contracts
- Monster-owned movement endpoints for Effect and Buff requests

Deferred topics include:

- Multiple independent stat profiles sharing one visual Monster template
- Multiple Spawn Routes and multiple Targets
- Route identifiers and route-specific Wave entries
- Boss and elite behavior
- Flying or non-grid movement
- Threat and advanced AI systems
- Monster skills
- Boss-specific global UI

These topics require separate design review before changing the current ownership boundary.
