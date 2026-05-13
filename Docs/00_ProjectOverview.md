# Tower Nexus - Project Overview

---

# 1. Project Introduction

Tower Nexus is a strategy tower defense game focused on terrain manipulation and random build creation.

The core gameplay inspiration comes from games such as Underdark: Defence and Wittle Defender. The game combines traditional tower defense mechanics with roguelike drafting systems and terrain-based strategic decision making.

Unlike traditional tower defense games where towers are only used for combat, towers in Tower Nexus also function as physical obstacles that directly affect monster pathfinding and battlefield structure.

Players are expected to continuously make strategic decisions during gameplay, including:

- Tower placement positioning
- Path manipulation
- Build drafting
- Tower upgrading
- Synergy construction
- Resource distribution

The core experience of the game is to allow players to gradually reshape the battlefield through their own decisions and ultimately create unique combat builds during each run.

---

# 2. Core Gameplay Loop

The gameplay loop of Tower Nexus is structured as follows:

1. The level starts with:
    - A Monster Spawn Point
    - A Monster Target Point
    - A grid-based map composed of multiple tiles

2. Monsters continuously spawn from the spawn point based on wave configuration.

3. Monsters use pathfinding logic similar to A* Pathfinding and move toward the target point using horizontal and vertical movement directions.

4. Players deploy towers onto the grid map to:
    - Attack and eliminate monsters
    - Occupy grid nodes
    - Block and alter monster movement paths

5. Eliminating monsters grants experience points to the player.

6. When enough experience is accumulated:
    - The player levels up
    - A new Draft selection is triggered

7. Through repeated drafting, deployment, repositioning, upgrading, and path manipulation, the player gradually strengthens their build and survives increasingly difficult monster waves.

8. If monsters reach the target point:
    - Player HP is reduced

9. If player HP reaches 0:
    - The run fails

10. If all monster waves are eliminated:
    - The run succeeds

---

# 3. Core Gameplay Features

## 3.1 Terrain Manipulation Tower Defense

One of the core mechanics of Tower Nexus is that towers physically occupy map tiles and influence monster movement routes.

Each tower has a unique footprint shape similar to Tetromino shapes from Tetris.

Different towers may occupy different tile patterns, such as:

- Line Shapes
- L Shapes
- T Shapes
- Square Shapes
- Cross Shapes

This creates a gameplay experience where players are not only building offensive structures, but are also dynamically reshaping the battlefield itself.

To prevent invalid gameplay states, the system prevents players from deploying towers in positions that would completely block all valid paths between the monster spawn point and target point.

Tower placement is validated based on tower footprint anchors. Each tower prefab defines a center anchor and multiple occupied anchors. During placement, the center anchor snaps to a target grid node, while all occupied anchors must match valid walkable grid nodes. If any occupied anchor cannot find a corresponding grid node, overlaps an unwalkable node, or causes the monster path to become fully blocked, the tower cannot be deployed.

---

## 3.2 Draft-Based Build System

The Draft System is the core progression mechanic of Tower Nexus.

At the beginning of the game, the player receives an initial 3-choice tower draft.

Each time the player levels up, another draft selection becomes available.

Draft options are currently divided into two major categories:

### Tower Draft

Allows players to:

- Obtain a new tower from a configured Tower Pool
- Deploy the selected tower onto the battlefield
- Reposition deployed towers through a recycle or redeployment flow
- Merge the tower with an existing identical tower for upgrading

### Upgrade Draft

Allows players to enhance existing tower types through stat or mechanic modifications.

Examples include:

- Increased attack speed
- Multi-projectile attacks
- Range enhancement
- Additional attack effects
- Mechanic transformations

The goal of the Draft System is to create deep build diversity and encourage players to adapt their strategy during each run.

---

## 3.3 Synergy System (Planned)

Future versions of the game will include a Synergy System.

Specific tower combinations placed simultaneously on the battlefield may activate special bonuses or mechanic changes.

Example:

- If Tower A + Tower B + Tower C all exist on the battlefield:
    - Additional passive bonuses may activate
    - Unique mechanics may unlock
    - Towers may gain special interactions

This system is intended to further deepen strategic build creation.

---

## 3.4 Monster Counter System (Planned)

Future monster and boss designs may contain counter relationships against specific tower types or build strategies.

Examples may include:

- High armor enemies
- Fast movement enemies
- Flying enemies
- Anti-projectile enemies
- Split-type enemies
- Path-breaking bosses

The purpose of this system is to continuously encourage strategic adaptation and prevent static gameplay solutions.

---

# 4. Core Design Philosophy

The core design philosophy of Tower Nexus is:

"Meaningful decisions create unique battlefield stories."

The game emphasizes:

- Strategic thinking
- Terrain control
- Dynamic adaptation
- Build experimentation
- Spatial planning
- Risk and reward balancing

Players are expected to constantly evaluate:

- Where to place towers
- How to manipulate monster paths
- Which draft option provides the highest value
- When to merge towers
- How to construct synergies
- How to adapt against different enemy types

The ultimate goal is to create a tower defense experience where every run feels strategically unique.

---

# 5. Core Systems Overview

## 5.1 Map System

The Map System provides the grid-based battlefield foundation of Tower Nexus.

It manages:

- Grid nodes
- Walkable and unwalkable states
- Runtime battlefield modification
- Map visual refresh
- Prefab-based map creation workflow

The Map System does not directly handle tower placement rules, monster AI, combat logic, or draft logic. Other systems interact with the Map System through node query and walkability update APIs.

---

## 5.2 Tower Deployment System

The Tower Deployment System manages the process of selecting, previewing, validating, deploying, and later repositioning towers on the grid map.

It is responsible for:

- Tower draft selection flow
- Tower pool based draft generation
- Tower prefab footprint definition
- Tower drag and snap placement
- Placement validity preview
- Grid node walkability occupation and release
- Path-blocking validation before final deployment
- Future tower recycle and redeployment flow

The first implementation focuses on the core deployment loop:

1. The player gains experience.
2. The player levels up when enough experience is accumulated.
3. A 3-choice tower draft UI is opened.
4. The player selects one tower from the draft options.
5. The selected tower enters drag placement mode.
6. The tower preview snaps to grid nodes based on its center anchor.
7. The system validates all occupied anchors and monster path availability.
8. If placement is valid, the tower is deployed and occupied grid nodes become unwalkable.
9. If placement is invalid, the tower returns to the draft selection flow.

Tower recycling and redeployment are planned as future extensions. When implemented, removed towers should release their occupied grid nodes and enter a UI-based recycle area, allowing players to drag them back onto the battlefield later.

---