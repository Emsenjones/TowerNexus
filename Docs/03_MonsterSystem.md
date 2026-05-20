

# 03 - Monster System

## 1. Overview

Monster System is one of the core runtime systems in TowerNexus.

Monsters are generated from map spawn nodes and automatically move toward the target node through the map pathfinding system.

Players must strategically deploy and upgrade towers to eliminate monsters before they reach the target point.

This system currently focuses on the following core gameplay loop:

- Monster wave spawning
- Monster movement and pathfinding
- Dynamic path recalculation
- Monster death handling
- Rewarding player EXP after monster elimination
- Preventing invalid tower placements that fully block monster paths

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
| walkAnimationName | Walk animation state name |
| hitAnimationName | Hit animation state name |
| deathAnimationName | Death animation state name |
| deathDelay | Delay before monster object is destroyed |

---

# 3. Spawn Node and Target Node

Monster spawning and movement are integrated into the existing map node system.

The current map structure already contains:

- MapGeneratorBehaviour
- GridNodeBehaviour
- GridNodeBehaviour.IsWalkable

Therefore, monster spawn points and target points should directly use GridNodeBehaviour.

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

### Dead

Monster has died.

Behavior:

- Stop movement immediately
- Stop all pathfinding behavior
- Play death animation
- Reward player EXP
- Destroy monster after delay

---

# 6. Hit Reaction

Hit reactions should not be treated as a standalone monster state in the first version.

Instead:

- Monsters can continue moving while receiving damage
- Hit animations can be triggered independently through Animator
- Visual feedback such as hit flash, floating damage numbers, or additive hit animation layers can be added later

---

# 7. Monster Pathfinding

Monster pathfinding is one of the core systems of TowerNexus.

The first version should use A* pathfinding.

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

Before a tower is successfully deployed:

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

---

# 11. Current Scope

The first implementation phase of the Monster System focuses only on:

- Wave spawning
- Monster movement
- A* pathfinding
- Dynamic path recalculation
- Death handling
- EXP reward flow
- Tower placement path validation

The following features are intentionally postponed:

- Boss mechanics
- Elite monsters
- Flying monsters
- Crowd control effects
- Special AI behaviors
- Threat systems
- Skill systems
- Advanced combat logic