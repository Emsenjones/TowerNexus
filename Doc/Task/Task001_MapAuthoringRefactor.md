# Task001 - Map Authoring Refactor

Status: Ready for review

Depends on: Current completed combat and placement baseline

## 1. Goal

Refactor the existing handcrafted Map authoring path so one reusable Map template owns authored Grid Node data and obtains all generated presentation from one interchangeable `MapVisualTheme` asset.

The task must preserve runtime topology changes: Tower occupancy refreshes Tile connectivity but never creates authored Obstacles or replaces Spawn and Target presentation.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/04_MapSystem.md`
- `Doc/05_MonsterSystem.md`
- `Doc/07_TowerPlacementSystem.md`

The current System documents are authoritative for Base Walkable versus Runtime Occupied state, the approved generated hierarchy, deterministic presentation, and validation.

## 3. Current State

- `GridNodeBehaviour` stores one mutable `isWalkable` field and one `GridNodeType`.
- Tower placement changes that same field, so authored terrain and runtime occupancy are not distinguishable.
- `MapGeneratorBehaviour` stores twelve direct Tile prefab references.
- Tile instances are generated directly below each Grid Node.
- There is no theme asset, feature presentation, deterministic obstacle selection, authored/runtime refresh split, or Map validation command.
- Node-dictionary rebuilding is exposed as a designer button.

## 4. Required Authoring Model

### 4.1 MapVisualTheme

Create one reusable `MapVisualTheme` ScriptableObject containing:

- Direction-mask-to-Tile-prefab entries
- Obstacle prefab list
- Spawn prefab
- Target prefab

Each Tile entry owns an explicit orthogonal direction mask. List index must not be the semantic identity. Duplicate masks and unsupported required masks are invalid authoring.

The current art contract contains the twelve masks listed in `Doc/04_MapSystem.md`. Single-direction dead ends continue to use the approved fallback and must be reported by validation.

### 4.2 Grid Node State

`GridNodeBehaviour` must expose:

- Grid Position
- Base Walkable
- Runtime Occupied
- Effective `IsWalkable = BaseWalkable && !RuntimeOccupied`
- Node Type
- Visual Root

Migrate the existing serialized `isWalkable` value into Base Walkable without losing authored Map data. Runtime Occupied starts false and is never serialized as authored terrain.

### 4.3 Generated Hierarchy

Implement the approved hierarchy:

```text
Map Root
└── NodesRoot
    └── GridNode
        └── VisualRoot
            ├── TileVisualRoot
            │   └── TileVisualInstance
            └── FeatureVisualRoot
                ├── ObstacleVisualInstance
                └── SpawnOrTargetVisualInstance
```

Generated refresh owns only these generated roots. Manually authored children outside them must survive refresh.

## 5. Authoring Operations

The designer-facing operations are:

- Generate Map
- Clear Map
- Refresh Map Visual
- Validate Map

Generate Map creates the rectangular Grid Node scaffold using Width, Height, Node Size, Grid Node prefab, and NodesRoot. It must preserve the existing explicit replacement intent before clearing an authored scaffold.

Authoring Refresh rebuilds Tile and Feature presentation from Base Walkable and Node Type. Runtime Tile Refresh uses effective Is Walkable and updates only Tile presentation.

Node-dictionary rebuilding remains available internally but is no longer a designer button.

## 6. Deterministic Presentation

- Add one authored Map Visual Seed.
- Obstacle selection uses seed, Grid Position, and visual category.
- Repeating Authoring Refresh with unchanged data produces the same result.
- Do not consume or mutate the global gameplay random stream.
- Use a stable deterministic calculation rather than a process-dependent object hash.

Changing the seed may intentionally select different variants. Ordinary refresh must not reroll them.

## 7. Runtime Occupancy Integration

- Tower placement commits Runtime Occupied instead of changing Base Walkable.
- Effective walkability changes immediately after commit.
- Map runtime refresh rebuilds Tile connectivity from effective walkability.
- Obstacle, Spawn, and Target presentation remain untouched.
- Monster path recalculation still begins only after accepted occupancy commit.
- Placement simulation remains temporary and does not mutate either authored or committed runtime state.

Any compatibility method named around `SetWalkable` must be migrated to an explicit Base Walkable or Runtime Occupied operation; do not retain an ambiguous write path.

## 8. Validation

Validate at minimum:

- Positive Width, Height, and Node Size
- Required Grid Node prefab, NodesRoot, and MapVisualTheme
- Exactly the required unique Tile masks and valid prefab references
- Valid Obstacle list, Spawn prefab, and Target prefab
- Grid Node count equal to Width multiplied by Height
- Unique in-bounds Grid Positions with no missing coordinates
- Exactly one Spawn and one Target
- Base Walkable Spawn and Target nodes
- At least one Base-Walkable Spawn-to-Target route

Validation reports errors and relevant nodes without silently repairing authored data.

## 9. Migration And Unity Authoring Checklist

- Add the approved VisualRoot, TileVisualRoot, and FeatureVisualRoot structure to the Grid Node prefab.
- Create the first MapVisualTheme asset and move the current twelve Tile references into explicit mask entries.
- Assign Obstacle, Spawn, and Target presentation assets.
- Migrate the current Map root from `generatedNodesParent` to NodesRoot without losing nodes.
- Confirm current authored walkability migrates to Base Walkable.
- Refresh and save the existing Map template/prefab only after visual and coordinate validation passes.
- Confirm Tower placement changes Runtime Occupied and does not create Obstacles.

Unless explicitly handed over, Codex owns scripts and documentation; the user owns final prefab/Inspector wiring, art-reference selection, and Unity visual acceptance.

## 10. Out Of Scope

- StageDefinition and Stage composition
- Multiple Spawn Routes or multiple Targets
- Procedural Map-data generation
- Destructible or special terrain
- Partial-neighbor refresh optimization
- New single-direction Tile art
- Runtime replacement of authored Feature presentation
- Tower placement rule redesign

## 11. Acceptance Criteria

- One Map template can switch themes without changing gameplay data.
- Authored terrain and runtime Tower occupancy are separate state.
- Generate, Clear, Authoring Refresh, Runtime Tile Refresh, and Validate follow their distinct contracts.
- Authoring Refresh produces Tile, Obstacle, Spawn, and Target presentation under the approved roots.
- Runtime occupancy refresh changes Tile presentation only.
- Deterministic variants remain stable across repeated refresh and reload.
- Current Map queries, A* pathfinding, placement simulation, occupancy commit, and Monster recalculation continue to work.
- Rebuild Node Dictionary is not exposed as a designer action.
- No manual child outside generated roots is removed by refresh.

## 12. Validation And Handoff

- Run targeted compilation for the main Unity assembly.
- Run `git diff --check` and distinguish pre-existing scene serialization whitespace from task changes.
- In Unity, generate and refresh a small test Map, then repeat refresh to confirm deterministic output.
- Validate walkable, blocked, Spawn, Target, unsupported dead-end, duplicate-coordinate, and disconnected-route cases.
- Place a Tower and confirm only Tile presentation changes while authored Feature presentation remains stable.
- Confirm existing Map prefab and scene references have no missing scripts or lost serialized data.
