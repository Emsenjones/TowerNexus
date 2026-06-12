
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
- Monster damage number display through current Task implementation scope
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

Health bar, hit feedback, and damage number fields are current Task design targets. They may be added to code during the corresponding Task implementation.

| Field | Description |
|---|---|
| monsterId | Unique monster id |
| displayName | Monster display name |
| monsterPrefab | Runtime monster prefab |
| moveSpeed | Monster movement speed |
| maxHealth | Monster maximum health |
| expReward | EXP rewarded to player after death |
| damageToPlayer | Damage dealt to player when reaching target |
| hitAnchor | Optional transform used as the monster hit/reference anchor for combat targeting, hit checks, effects, and presentation binding |
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
| damageNumberOffset | World-space offset between monster transform and damage number spawn position |
| damageNumberPrefab | Optional damage number UI prefab override used by this monster type |

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

# 6. Monster Visual Feedback

Monster Visual Feedback contains all monster-related runtime presentation feedback.

The first version of Monster Visual Feedback focuses on:

- Idle and Walk animation switching
- Death animation
- Hit animation trigger
- Hit flash feedback
- Health bar display
- Damage number display

Monster Visual Feedback should remain lightweight and should not take ownership of monster combat calculation, pathfinding, or Player System logic.

### 6.1 Monster Hit Reference Anchor

MonsterBehaviour may expose optional anchors that provide stable reference points for other runtime systems.

The first shared anchor is:

```text
HitAnchor
```

HitAnchor is an optional Transform reference on MonsterBehaviour.

HitAnchor represents the monster-side reference point used when another system needs a readable target, hit, or attachment position for that monster.

HitAnchor may be consumed by:

- Tower attack range and target distance evaluation
- Projectile hit checks and target position snapshots
- Area effect inclusion checks
- ChannelBeam VFX target binding
- Monster hit VFX
- Damage number spawn positioning if needed
- Future status effect attachment points

HitAnchor should usually be placed around the monster's chest, body center, or visually readable hit point.

If HitAnchor is not configured, runtime systems should still have a safe monster-root fallback.

HitAnchor does not own monster movement, pathfinding, collision shape, damage application, or target validity. It only provides the monster-side reference position consumed by those systems.

---

## 6.2 Monster Animation Rules

The first version of the Monster System uses a simple Animator setup.

### Idle and Walk

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

### Death

Death animation should be triggered when the monster enters the Dead state.

The first version may use a simple Animator trigger for death animation.

Recommended parameter:

```text
Dead
```

Death animation behavior is handled by the monster death flow.

### Hit Reaction

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

## 6.3 Hit Flash

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

## 6.4 Monster Health Bar System

Monster Health Bar is a runtime UI feedback feature owned by Monster System.

When a monster is instantiated into the scene, the system should automatically create a corresponding health bar UI item.

The health bar UI item should follow the monster's transform position during runtime.

### Health Bar Creation Flow

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

### Health Bar Position Binding

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

### Health Bar Ownership Rules

Monster System owns:

- Creating the monster health bar item
- Binding the health bar to the monster
- Updating health bar value when monster HP changes
- Removing the health bar when the monster leaves the battlefield

Battle HUD UI System should not own individual monster health bars.

Battle HUD UI System is responsible for global battle UI, such as player HP, player EXP, and battle failure UI.

Monster health bars are battlefield unit UI and belong to Monster System.

### Health Bar Update Rules

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

## 6.5 Monster Damage Number System

Monster Damage Number is a runtime UI feedback feature owned by Monster System.

When a monster receives damage, the system should spawn a damage number UI item near the monster and display the damage amount.

Damage number should be implemented as a UI prefab using TextMeshProUGUI and DOTween-based animation.

Recommended runtime flow:

```text
Monster takes damage
→ MonsterBehaviour updates currentHealth
→ Monster hit feedback plays if available
→ DamageNumberManager creates damage number UI item
→ DamageNumberUI displays damage value
→ DamageNumberUI plays configured tween steps
→ DamageNumberUI is destroyed or recycled after animation completes
```

### Damage Number Position Binding

Damage number spawn position should be calculated from monster world position plus a configurable offset.

```text
damageNumberWorldPosition = monster.transform.position + damageNumberOffset
```

The damage number UI system should convert this world position into screen/UI position.

Recommended configurable fields:

| Field | Description |
|---|---|
| damageNumberOffset | World-space offset from monster transform to damage number spawn position |
| damageNumberPrefab | Optional UI prefab override used by this monster type |

If no monster-specific prefab override is configured, DamageNumberManager should use its default damage number prefab.

### Damage Number Ownership Rules

Monster System owns:

- Creating damage number UI items
- Passing damage amount and world position to the damage number UI item
- Removing or recycling damage number UI items after animation completion

MonsterBehaviour should not directly own DOTween animation details.

MonsterBehaviour should only report:

```text
Damage amount + monster world position + optional visual offset
```

DamageNumberManager and DamageNumberUI should own visual presentation.

### Damage Number Tween Step List

DamageNumberUI should use an Inspector-configurable tween step list.

Each tween step represents one visual animation channel.

All enabled tween steps should be played when DamageNumberUI.Play(...) is called.

The first implementation should play all tween steps in parallel.

Recommended base fields shared by all tween steps:

| Field | Description |
|---|---|
| enabled | Whether this tween step is active |
| tweenType | Type of tween behavior |
| duration | Tween duration |
| delay | Delay before this tween starts |
| easeType | DOTween ease type |

Recommended first-version tween types:

| Tween Type | Purpose |
|---|---|
| Position | Move the damage number from current position to target offset or target position |
| Scale | Scale the damage number from start scale to target scale |
| Fade | Fade the damage number through CanvasGroup alpha |

Future tween types may include:

- Color
- Rotation
- ShakePosition
- ShakeScale
- PunchScale

### Position Tween Step

Position tween should support runtime spawn positions.

Recommended fields:

| Field | Description |
|---|---|
| startAnchoredPosition | Explicit start anchored position relative to the spawned damage number UI item |
| targetAnchoredPosition | Optional explicit target anchored position |
| targetOffset | Offset added to startAnchoredPosition |
| useTargetOffset | If true, target position is startAnchoredPosition plus targetOffset |
| duration | Tween duration |
| delay | Delay before tween starts |
| easeType | DOTween ease type |

For damage number usage, the recommended default is:

```text
startAnchoredPosition = (0, 80)
useTargetOffset = true
targetOffset = (0, 80)
duration = 0.6
easeType = OutQuad
```

In this default setup, the damage number UI item is spawned at the monster's converted UI position, then the text starts above the monster through `startAnchoredPosition` instead of appearing from the monster's feet.

### Scale Tween Step

Scale tween should support simple scale transition.

Recommended fields:

| Field | Description |
|---|---|
| startScale | Initial local scale |
| targetScale | Target local scale |
| duration | Tween duration |
| delay | Delay before tween starts |
| easeType | DOTween ease type |

For the first version, DamageNumberUI should not implement PunchScale as a separate tween type.

A punch-like visual effect can be created by combining multiple Scale tween steps with proper delay and ease type.

Example punch-like setup:

```text
Scale Step A
startScale = (0.8, 0.8, 0.8)
targetScale = (1.2, 1.2, 1.2)
duration = 0.12
delay = 0
easeType = OutBack

Scale Step B
startScale = (1.2, 1.2, 1.2)
targetScale = (1.0, 1.0, 1.0)
duration = 0.12
delay = 0.12
easeType = OutQuad
```

Future versions may add PunchScale as an independent tween type if critical damage or stronger hit feedback requires more elastic animation.

### Fade Tween Step

Fade tween should use CanvasGroup alpha.

Recommended fields:

| Field | Description |
|---|---|
| startAlpha | Initial CanvasGroup alpha |
| targetAlpha | Target CanvasGroup alpha |
| duration | Tween duration |
| delay | Delay before tween starts |
| easeType | DOTween ease type |

Recommended default fade behavior:

```text
startAlpha = 1
targetAlpha = 0
duration = 0.4
delay = 0.2
```


### Recommended Default Damage Number Tween Setup

Recommended first-version setup:

```text
Tween Steps
  [0] Type: Position
      startAnchoredPosition = (0, 80)
      useTargetOffset = true
      targetOffset = (0, 80)
      duration = 0.6
      delay = 0
      easeType = OutQuad

  [1] Type: Scale
      startScale = (0.8, 0.8, 0.8)
      targetScale = (1.2, 1.2, 1.2)
      duration = 0.12
      delay = 0
      easeType = OutBack

  [2] Type: Scale
      startScale = (1.2, 1.2, 1.2)
      targetScale = (1.0, 1.0, 1.0)
      duration = 0.12
      delay = 0.12
      easeType = OutQuad

  [3] Type: Fade
      startAlpha = 1
      targetAlpha = 0
      duration = 0.4
      delay = 0.2
      easeType = InQuad
```

### Damage Number Preview

DamageNumberUI should support an Inspector preview function.

Recommended preview behavior:

```text
Designer selects DamageNumberUI prefab or scene instance
→ Click Preview button in Inspector
→ DamageNumberUI resets preview state
→ DamageNumberUI plays the configured tween step list
```

Preview should allow visual tuning without entering full combat flow.

Recommended preview requirements:

- Preview should work in Play Mode.
- Preview may optionally support Edit Mode if implementation remains simple and safe.
- Preview should reset RectTransform position, scale, alpha, and text value before playing.
- Preview should kill any existing preview tween before replaying.
- Preview should use a configurable preview damage value.
- Preview should not require MonsterBehaviour or DamageNumberManager.

If Odin Inspector is available, DamageNumberUI should prefer Odin Inspector for editor usability.

Recommended Odin usage:

- Use `[Button]` to expose Preview in Inspector.
- Use `[ShowIf]` or `[HideIf]` to show only fields related to the selected tween type.
- Use `[LabelText]`, `[TitleGroup]`, or `[FoldoutGroup]` to keep tween step configuration readable.
- Use `[ListDrawerSettings]` to make the tween step list easier to edit.

If Odin Inspector is not used, Preview can be exposed through a custom editor or a ContextMenu method.

### Damage Number Implementation Notes

- DamageNumberUI should use TextMeshProUGUI for text display.
- DamageNumberUI should use CanvasGroup for fade control.
- DOTween should drive animation playback.
- Tween values should be configurable in Inspector whenever possible.
- First-version DamageNumberUI should keep tween types simple and only implement Position, Scale, and Fade.
- Odin Inspector may be used to improve tween step list editing, conditional field display, and preview buttons.
- Runtime should kill existing tweens before replaying the same UI item.
- Animation completion should destroy or recycle the UI item.
- The first version may destroy the item after completion.
- Future Object Pool implementation may replace Destroy with return-to-pool behavior.
- Damage numbers should not block monster death or target arrival flow.
- Damage number display should still be safe if the monster dies immediately after taking damage.

Future versions may support:

- Critical damage number style
- Healing number style
- Shield damage number style
- Elemental damage number style
- Damage number pooling
- Multiple damage number layout rules when many numbers appear at the same time
---

# 7. Monster Pathfinding

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

The current and upcoming implementation scope of the Monster System focuses only on:

- Wave spawning
- Monster movement
- A* pathfinding
- Dynamic path recalculation
- Death handling
- Monster health bar display
- Monster hit animation trigger
- Monster hit flash feedback
- Monster damage number display
- EXP reward flow
- Monster target arrival notification to Player System
- Pathfinding functionality that can be reused by tower placement validation
- Optional Monster HitAnchor for shared combat, effect, and presentation reference positioning

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

### 2026-06-12 (Monster Hit Reference Anchor Sync)

- Clarified that Monster HitAnchor is the monster-side hit/reference anchor consumed by combat, projectile, effect, and presentation systems.
- Clarified that HitAnchor provides a reference position but does not own movement, pathfinding, collision, damage application, or target validity.

### 2026-06-11 (Monster HitAnchor Shared Anchor Sync)

- Added optional Monster HitAnchor visual anchor.
- Clarified that HitAnchor is a shared Transform reference used for beam target binding, hit VFX, damage number positioning, and future visual attachment points.
- Clarified that runtime systems should fall back to monster transform when HitAnchor is not configured.
- Clarified that HitAnchor must not own monster position, pathfinding, collision, damage, or target validity logic.

# Change Log

## 2026-06-08 (Damage Number Visual Feedback Sync)

- Reorganized Monster animation, hit flash, health bar, and damage number content under Monster Visual Feedback.
- Added Monster Damage Number System.
- Added damage number creation, position binding, ownership, tween step list, and preview rules.
- Added configurable `damageNumberOffset` and `damageNumberPrefab` fields.
- Clarified that DamageNumberUI should use TextMeshProUGUI, CanvasGroup, and DOTween.
- Clarified that damage number tween parameters should be Inspector-configurable.
- Clarified that DamageNumberUI should support preview playback for tuning animation without full combat flow.
- Simplified first-version tween types to Position, Scale, and Fade.
- Clarified that punch-like scale feedback should be created through multiple Scale tween steps with delay and ease type.
- Added Odin Inspector recommendation for preview buttons and conditional tween step field display.

- Removed `useCurrentAsStart` from Position tween and clarified explicit startAnchoredPosition usage.

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
