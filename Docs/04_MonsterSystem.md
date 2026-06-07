
# 04 - Monster System

## 1. Overview

Monster System is one of the core runtime systems in TowerNexus.

Monsters are generated from map spawn nodes and automatically move toward the target node using Monster System pathfinding functionality and Map System data.

Players must strategically deploy and upgrade towers to eliminate monsters before they reach the target point.

This system focuses on the following core gameplay loop and current Monster-related implementation direction:

- Monster wave spawning
- Monster movement and pathfinding
- Dynamic path recalculation
- Monster death handling
- Monster health bar display through current Task implementation scope
- Monster hit feedback through current Task implementation scope
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

Health bar and hit feedback fields are current Task design targets. They may be added to code during the corresponding Task implementation.

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
| hitAnimatorTriggerName | Animator trigger parameter name used to play hit reaction animation |
| deathAnimationName | Death animation state name |
| deathDelay | Delay before monster object is destroyed |
| healthBarOffset | World-space offset between monster transform and monster health bar position |
| hitFlashColor | Temporary color used by monster hit flash effect, recommended red |
| hitFlashDuration | Total duration of monster hit flash effect |
| hitFlashRestoreDuration | Duration used to restore monster color back to normal |
| hitFlashRendererRoot | Optional root transform used to collect monster renderers for hit flash |

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

Hit reaction should provide immediate feedback when a monster receives damage.

For the current Task implementation scope, monster hit feedback contains two parts:

1. Play hit animation
2. Play hit flash visual feedback

Hit reaction should not become a standalone monster state in the first version.

Monster should continue using the existing movement/death state flow.

Recommended Animator parameter:

```text
GetHit
```

Runtime behavior:

```text
Monster receives damage
→ Animator.SetTrigger("GetHit")
→ Play hit flash
→ Continue current movement/death flow according to monster state
```

The exact hit trigger parameter name should be configurable through MonsterDefinition.

If the monster Animator does not contain a hit animation yet, the system may still play hit flash only.

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

## 6.4 Hit Flash

The first version of the Monster System should support a simple hit flash effect.

Recommended runtime behavior:

```text
Monster receives damage
→ Briefly change monster model color to hitFlashColor
→ Restore original material color after hitFlashDuration / hitFlashRestoreDuration
```

Default visual direction:

```text
hitFlashColor = Red
```

The following values should be configurable through MonsterDefinition or a dedicated monster visual configuration structure:

| Field | Description |
|---|---|
| hitFlashColor | Temporary color used when the monster is hit |
| hitFlashDuration | How long the monster stays in the flash color |
| hitFlashRestoreDuration | How long the monster takes to restore its original color |
| hitFlashRendererRoot | Optional renderer root used to find monster model renderers |

Implementation notes:

- Hit flash should be lightweight and should not interrupt movement.
- Hit flash should be safe to replay when the monster is hit repeatedly.
- Runtime should cache original renderer material colors during initialization.
- If a monster dies, hit flash should not block the death flow.
- Shared materials should not be modified directly. Runtime should use instance materials or MaterialPropertyBlock.

Possible future implementations:

- Material color lerp
- Shader flash parameter
- Emission intensity flash
- Renderer overlay effect

---

# 7. Monster Health Bar System

Monster Health Bar is a runtime UI feedback feature owned by Monster System.

When a monster is instantiated into the scene, the system should automatically create a corresponding health bar UI item.

The health bar UI item should follow the monster's transform position during runtime.

## 7.1 Health Bar Creation Flow

Recommended flow:

```text
Monster instantiated
→ MonsterBehaviour.Initialize(...)
→ Create health bar UI item
→ Bind health bar to monster transform
→ Apply healthBarOffset
→ Update health bar value when monster HP changes
→ Destroy or recycle health bar when monster dies / arrives / is removed
```

## 7.2 Health Bar Position Binding

Health bar position should be calculated from monster world position plus a configurable offset.

```text
healthBarWorldPosition = monster.transform.position + healthBarOffset
```

The health bar UI system should convert this world position into screen/UI position.

The exact offset should be configurable because different monster models may have different heights and visual centers.

Recommended configurable field:

| Field | Description |
|---|---|
| healthBarOffset | World-space offset from monster transform to health bar anchor position |

Example:

```text
healthBarOffset = (0, 2.0, 0)
```

## 7.3 Health Bar Ownership Rules

Monster System owns:

- Creating the monster health bar item
- Binding the health bar to the monster
- Updating health bar value when monster HP changes
- Removing the health bar when the monster leaves the battlefield

Battle HUD UI System should not own individual monster health bars.

Battle HUD UI System is responsible for global battle UI, such as player HP, player EXP, and battle failure UI.

Monster health bars are battlefield unit UI and belong to Monster System.

## 7.4 Health Bar Update Rules

Recommended update behavior:

```text
Monster takes damage
→ MonsterBehaviour updates currentHealth
→ Monster health bar updates currentHealth / maxHealth
```

Health bar should be hidden or removed when:

- Monster dies
- Monster reaches target
- Monster is destroyed or recycled

For the first version, health bar can always be visible after monster spawn.

Future versions may support:

- Only show health bar after monster takes damage
- Hide health bar after no damage for several seconds
- Boss health bar
- Elite monster health bar style
---

# 8. Monster Pathfinding

Monster pathfinding is one of the core systems of TowerNexus.

The first version should use A* pathfinding.

Monster System owns runtime pathfinding behavior, while Map System only provides spatial and walkability data.

```markdown
Tower Placement System may reuse Monster System pathfinding functionality when validating whether a placement would completely block all monster routes.
```

## 8.1 Pathfinding Rules

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

# 9. Dynamic Path Recalculation

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

# 10. Tower Placement Path Validation

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

# 11. Monster Death Flow

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

# 12. Monster Target Arrival Flow

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

# 13. Current Scope

The current and upcoming implementation scope of the Monster System focuses only on:

- Wave spawning
- Monster movement
- A* pathfinding
- Dynamic path recalculation
- Death handling
- Monster health bar display
- Monster hit animation trigger
- Monster hit flash feedback
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
- Boss health bar
- Floating damage numbers

---

# 14. Related Systems

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

## 2026-06-07 (Monster Visual Feedback Sync)

- Added Monster Health Bar System.
- Added health bar creation, binding, offset, update, and removal rules.
- Added configurable `healthBarOffset`.
- Updated Hit Reaction to include hit animation trigger and hit flash feedback.
- Added configurable hit flash fields: `hitFlashColor`, `hitFlashDuration`, `hitFlashRestoreDuration`, and `hitFlashRendererRoot`.
- Clarified Monster System ownership of battlefield unit UI.
- Clarified that Battle HUD UI System should not own individual monster health bars.

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
