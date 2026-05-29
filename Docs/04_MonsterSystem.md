
# 04 - Monster System

## 1. Overview

Monster System is one of the core runtime systems in TowerNexus.

Monsters are generated from map spawn nodes and automatically move toward the target node using Monster System pathfinding functionality and Map System data.

Players must strategically deploy and upgrade towers to eliminate monsters before they reach the target point.

This system currently focuses on the following core gameplay loop:

- Monster wave spawning
- Monster movement and pathfinding
- Dynamic path recalculation
- Monster death handling
- Rewarding player EXP after monster elimination
- Notifying Player System when monsters reach the target node
- Providing path validation functionality used by Tower Placement System

The first version of the Monster System is intentionally kept simple and extensible.
Future features such as Boss mechanics, elite monsters, flying enemies, abnormal states, and advanced AI behaviors will be added in later phases.

---

# 2. Monster Definition

Monster runtime logic and monster configuration should be separated.

Monster static data should be configured through ScriptableObject assets.

Recommended structure:

- MonsterDefinition: static monster configuration
- MonsterBehaviour: runtime monster logic

## 2.1 Monster Definition Fields

Each monster should support the following configurable fields:

| Field | Description |
|---|---|
| monsterId | Unique monster id |
| displayName | Monster display name |
| monsterPrefab | Runtime monster prefab |
| moveSpeed | Monster movement speed |
| maxHealth | Monster maximum health |
| expReward | EXP rewarded to player after death |
| damageToPlayer | Damage dealt to player when reaching target |
| isWalkingParameterName | Animator bool parameter name used to switch between Idle and Walk |
| hitAnimationName | Hit animation state name, reserved for future hit reaction implementation |
| deathAnimationName | Death animation state name |
| deathDelay | Delay before monster object is destroyed |

---

# 3. Spawn Node and Target Node

Monster spawning and movement are integrated into the existing map node system.

The current map structure already contains:

- MapGeneratorBehaviour
- GridNodeBehaviour
- GridNodeBehaviour.IsWalkable

Therefore, monster spawn points and target points should reference GridNodeBehaviour data provided by Map System.

The Target Node only defines the monster destination position.

Monster System is responsible for detecting when a monster reaches the Target Node.

After target arrival, Monster System should notify Player System so Player System can apply player HP damage and handle battle failure if needed.

Monster System should not directly modify player HP or decide battle failure.

## 3.1 Node Types

Current node types:

- Normal
- Spawn
- Target

## 3.2 Monster Spawn Flow

Monster spawn logic:

1. Find Spawn Node
2. Instantiate monster at Spawn Node position
3. Monster immediately requests a path toward Target Node
4. Monster begins movement

---

# 4. Monster Wave System

Monster spawning should use a wave-based spawning structure.

The wave system should support multiple monster entries within a single wave.

## 4.1 MonsterWaveConfig

Each wave should contain:

| Field | Description |
|---|---|
| waveDelay | Delay before this wave begins |
| spawnEntries | Monster spawn entries inside the wave |

## 4.2 MonsterSpawnEntry

Each spawn entry should contain:

| Field | Description |
|---|---|
| monsterDefinition | Monster configuration reference |
| count | Spawn count |
| spawnInterval | Interval between each spawned monster |

Example:

```text
Wave 1
- Small Monster x10, interval 0.5s
- Fast Monster x3, interval 1s
```

---

# 5. Monster State Machine

The first version of the monster state machine should remain lightweight.

## 5.1 Monster States

### Spawn

Used for:

- Runtime initialization
- Requesting movement path
- Entering movement state

### Walk

Monster is moving toward the target.

Behavior:

- Move along current path
- Play walk animation
- Receive damage from towers

### Arrived

Monster has reached the target node.

Behavior:

- Stop movement immediately
- Notify Player System that this monster has reached the target
- Pass target arrival damage information if required
- Remove or destroy the monster after arrival handling

Monster System should not directly reduce player HP.

Player HP calculation and battle failure logic belong to Player System.

### Dead

Monster has died.

Behavior:

- Stop movement immediately
- Stop all pathfinding behavior
- Play death animation
- Reward player EXP
- Destroy monster after delay

---

# 6. Monster Animation Rules

The first version of the Monster System uses a simple Animator setup.

## 6.1 Idle and Walk

Monster Idle and Walk animations should be controlled by an Animator bool parameter.

Recommended parameter:

```text
IsWalking
```

Runtime behavior:

```text
Monster starts moving
→ Animator.SetBool("IsWalking", true)

Monster stops moving
→ Animator.SetBool("IsWalking", false)
```

The exact Animator parameter name should be configurable through MonsterDefinition.

This avoids hardcoding Animator parameter names inside MonsterBehaviour.

## 6.2 Death

Death animation should be triggered when the monster enters the Dead state.

The first version may use a simple Animator trigger for death animation.

Recommended parameter:

```text
Dead
```

Death animation behavior is handled by the monster death flow.

## 6.3 Hit Reaction

Hit reactions should not be treated as a standalone monster state in the first version.

The current implementation does not need to play GetHit animation yet.

Future versions may support hit reaction through a separated Animator layer:

```text
Base Layer
- Idle
- Walk
- Death

Hit Layer
- GetHit
```

In that future setup, GetHit can be triggered independently while the Base Layer continues controlling Idle or Walk.

This allows monsters to keep moving while showing hit reaction feedback.

For the current version, hit reaction can be postponed or replaced with simple visual feedback such as:

- Hit flash (recommended first-version solution)
- Floating damage numbers
- Sound effect

## 6.4 Hit Flash

The first version of the Monster System may use a simple hit flash effect instead of a full GetHit animation.

Recommended runtime behavior:

```text
Monster receives damage
→ Briefly change monster material color to white
→ Restore original material color after a short delay
```

Advantages of hit flash:

- Lightweight implementation
- Easy to combine with movement animation
- Does not interrupt Walk animation
- Does not require Animator layer setup
- Provides immediate visual hit feedback

The exact implementation method is not restricted yet.

Possible future implementations:

- Material color lerp
- Shader flash parameter
- Emission intensity flash
- Renderer overlay effect

---

# 7. Monster Pathfinding

Monster pathfinding is one of the core systems of TowerNexus.

The first version should use A* pathfinding.

Monster System owns runtime pathfinding behavior, while Map System only provides spatial and walkability data.

```markdown
Tower Placement System may reuse Monster System pathfinding functionality when validating whether a placement would completely block all monster routes.
```

## 7.1 Pathfinding Rules

### Rule 1

Monsters should always calculate a valid path between:

- Current monster node
- Target node

### Rule 2

Monsters should immediately calculate a path after spawning.

### Rule 3

When GridNodeBehaviour.IsWalkable changes because of tower placement or tower removal:

- All alive monsters should recalculate their paths

### Rule 4

Monsters should move along node center positions.

---

# 8. Dynamic Path Recalculation

When map walkability changes:

```text
Map walkability changes
→ MonsterManager notifies all alive monsters
→ Monsters recalculate paths
```

Monsters should not recalculate from the original spawn node.

Instead:

- Monsters should track their current node during movement
- Recalculation should start from the current node

---

# 9. Tower Placement Path Validation

Tower placement must never completely block all valid monster paths.

This feature is owned by Tower Placement System.

Monster System only provides pathfinding functionality that may be reused by placement validation.

Before a tower is successfully placed:

1. Temporarily evaluate the nodes occupied by the tower
2. Simulate those nodes as blocked
3. Verify whether a valid path still exists between Spawn Node and Target Node
4. If no valid path exists:
   - Tower placement must be rejected

This validation should reuse the same pathfinding system used by monsters.

---

# 10. Monster Death Flow

When monster HP reaches 0:

```text
Monster HP <= 0
→ Enter Dead state
→ Stop movement
→ Play death animation
→ Reward player EXP
→ Delay
→ Destroy monster object
```

EXP reward should be sent to Player System.

Monster System may provide the reward value from MonsterDefinition, but Player System should own EXP accumulation and level-up logic.

---

# 11. Monster Target Arrival Flow

When a monster reaches the Target Node:

```text
Monster reaches Target Node
→ Monster enters Arrived state
→ Monster System notifies Player System
→ Player System applies HP damage
→ BattleHUDUISystem updates HP display through Player System events
→ Monster is removed from battlefield
```

Monster target arrival should follow these ownership rules:

- Monster System detects arrival.
- Monster System reports arrival damage information.
- Player System owns player HP damage calculation.
- Player System owns battle failure state.
- Battle HUD UI System displays updated HP and battle failure UI.
- Map System only provides the Target Node spatial reference.

---

# 12. Current Scope

The first implementation phase of the Monster System focuses only on:

- Wave spawning
- Monster movement
- A* pathfinding
- Dynamic path recalculation
- Death handling
- EXP reward flow
- Monster target arrival notification to Player System
- Pathfinding functionality that can be reused by tower placement validation

The following features are intentionally postponed:

- Boss mechanics
- Elite monsters
- Flying monsters
- Crowd control effects
- Special AI behaviors
- Threat systems
- Skill systems
- Advanced combat logic

---

# 13. Related Systems

## Player System

Player System owns player EXP, level-up logic, player HP, and battle failure state.

Monster System may notify Player System when:

- A monster dies and provides EXP reward
- A monster reaches the target node and provides player damage information

Monster System should not directly own player progression or player HP.

## Battle HUD UI System

Battle HUD UI System displays player EXP, player HP, and battle failure UI through Player System events.

Monster System should not directly control Battle HUD UI.

## Map System

Map System provides Spawn Nodes, Target Node, walkability state, node queries, and pathfinding-related map data.

Map System does not handle monster arrival consequences.

## Tower Placement System

Tower Placement System owns:

- Placement validation
- Occupancy simulation
- Path blocking validation decisions

Monster System may provide pathfinding functionality used during placement validation.
  
---

# Change Log

## 2026-05-24 (Naming Sync)

- Updated deployment terminology to placement terminology.
- Updated BattleHUDUI references to BattleHUDUISystem.
- Clarified ownership boundary between Monster System pathfinding behavior and Map System spatial data.
- Clarified ownership boundary between Monster System pathfinding functionality and Tower Placement System path validation.

## 2026-05-24

- Clarified Monster System relationship with Player System.
- Added monster target arrival flow.
- Clarified that Monster System detects target arrival but does not directly modify player HP.
- Clarified that EXP reward should be sent to Player System.
- Added Battle HUD UI System and Map System ownership boundaries.