# Tower Nexus - Map System

---

# 1. Purpose And Ownership

Map System owns the grid-based battlefield used by runtime gameplay.

It owns:

- Authored Grid Node data
- Base and runtime walkability state
- Node and spatial queries
- Spawn and Target node identity
- MapVisualTheme-driven presentation
- Authoring and runtime visual refresh
- Reusable Map-template authoring
- Map validation

Stage System selects and creates the current Map. Tower Placement System requests runtime occupancy changes. Monster System consumes Map topology for pathfinding.

Map System does not own Stage flow, Tower placement rules, pathfinding algorithms, Monster behavior, Draft generation, or combat.

---

# 2. Grid Contract

The Map is an authored rectangular grid on the gameplay XZ plane.

- Grid X maps to world X.
- Grid Y maps to world Z.
- World Y remains available for height.
- Node Size controls center-to-center spacing.
- Orthogonal neighbors are traversable when effectively walkable.
- Diagonal traversal is disabled.

```text
World Position = (Grid X * Node Size, 0, Grid Y * Node Size)
```

Equivalent engines must preserve grid coordinates, spacing, and neighbor semantics even when their world axes differ.

---

# 3. Grid Node State

Each Grid Node stores or exposes:

| State | Contract |
|---|---|
| Grid Position | Unique integer coordinate inside authored bounds |
| World Position | Center position derived from grid mapping |
| Base Walkable | Authored terrain state saved in the Map template |
| Runtime Occupied | Temporary runtime blocking state |
| Is Walkable | Effective state consumed by pathfinding and placement |
| Node Type | Normal, Spawn, or Target |
| Visual Root | Owner of generated presentation for this node |

Effective walkability is:

```text
Is Walkable = Base Walkable AND NOT Runtime Occupied
```

Runtime occupancy never changes Base Walkable and must not be saved back into the authored Map.

Spawn and Target nodes must be Base Walkable. The current Map contract contains exactly one Spawn and one Target.

---

# 4. Authored Hierarchy Contract

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

Ownership rules:

- `NodesRoot` owns generated Grid Nodes.
- `VisualRoot` owns presentation generated from MapVisualTheme.
- `TileVisualRoot` owns topology-dependent Tile presentation.
- `FeatureVisualRoot` owns authored static feature presentation.
- Manually authored decorations that must survive refresh stay outside generated roots.

Tile and Feature presentation have intentionally different refresh lifecycles.

---

# 5. MapVisualTheme

MapVisualTheme is one interchangeable set of Map presentation assets:

- Direction-aware Tile Visual Entries
- Obstacle visual list
- Spawn visual
- Target visual

Changing the theme and performing authoring refresh replaces generated presentation without changing coordinates, Base Walkable, Node Type, or gameplay rules.

## 5.1 Tile Connection Entries

Each Tile entry declares its supported orthogonal walkable-direction mask. List position alone is not semantic identity.

The current art set supports twelve masks:

| Walkable Directions | Shape |
|---|---|
| Up + Down + Left + Right | Four-way |
| None | Isolated |
| Up + Down | Vertical |
| Left + Right | Horizontal |
| Up + Left | Corner |
| Up + Right | Corner |
| Down + Left | Corner |
| Down + Right | Corner |
| Down + Left + Right | Three-way |
| Up + Left + Right | Three-way |
| Up + Down + Right | Three-way |
| Up + Down + Left | Three-way |

Single-direction dead ends are not represented by the current set. Map content must avoid them or use an explicitly approved fallback.

## 5.2 Deterministic Variants

Obstacle and future Tile variants are deterministic from:

- Map Visual Seed
- Grid Position
- Visual category

The same seed and authored Grid Node data produce the same visual result. Ordinary refresh never rerolls variants; changing the seed may intentionally do so.

---

# 6. Authoring Refresh

Authoring refresh rebuilds both Tile and Feature presentation from authored state.

For every Grid Node:

1. Clear generated Tile presentation.
2. Resolve the direction mask from neighboring Base Walkable states.
3. Create the matching Tile visual under TileVisualRoot.
4. Clear generated Feature presentation.
5. If Base Walkable is false, create one deterministic Obstacle visual.
6. If Node Type is Spawn, create the Spawn visual.
7. If Node Type is Target, create the Target visual.

Spawn or Target combined with an authored Obstacle is invalid because both node types require Base Walkable.

Generated presentation begins at its authored local origin; presentation templates own internal visual offsets.

---

# 7. Runtime Occupancy And Tile Refresh

Approved runtime blockers such as placed Towers change Runtime Occupied.

After one committed occupancy change:

1. Effective Is Walkable changes.
2. Pathfinding and placement consume the new state.
3. Tile connection presentation refreshes using neighboring effective Is Walkable states.
4. Feature presentation remains unchanged.

Runtime refresh must not:

- Create an Obstacle because a Tower occupies a node
- Replace an authored Obstacle variant
- Remove or replace Spawn or Target presentation
- Modify Base Walkable

The current version may refresh all Tile visuals after one topology change. Partial refresh of the changed nodes and orthogonal neighbors is an optimization that must preserve identical results.

---

# 8. Map Authoring Workflow

The approved handcrafted workflow is:

1. Create a Map Root.
2. Configure Width, Height, Node Size, Grid Node template, NodesRoot, MapVisualTheme, and Map Visual Seed.
3. Generate the rectangular Grid Node scaffold.
4. Edit each node's Base Walkable and Node Type.
5. Refresh generated Map presentation.
6. Validate the Map.
7. Save the completed Map Root as a reusable Map template referenced by StageDefinition.

The authoring surface contains:

- Generate Map
- Clear Map
- Refresh Map Visual
- Validate Map

Generate Map replaces an existing generated scaffold and therefore requires explicit author intent. Runtime node lookup rebuilding is internal lifecycle behavior and is not exposed as a designer action.

The current runtime loads the authored Map template. It does not regenerate Map data procedurally from a separate Map definition.

---

# 9. Spatial Query Boundary

Map System provides:

- Coordinate-to-node and position-to-node lookup
- Orthogonal neighbor lookup
- Effective walkability queries
- Spawn and Target queries
- Temporary topology evaluation support

Monster System chooses and executes the pathfinding algorithm. Tower Placement System chooses when to simulate or commit occupancy.

---

# 10. Stage Boundary

The reusable Map template contains:

- Dimensions and Node Size
- Grid Nodes and authored state
- MapVisualTheme reference
- Visual Seed
- Generated Tile and Feature presentation

StageDefinition references one Map template. Stage System creates it and establishes the Active Map before Monster, Draft, and Placement runtime begins.

Map System does not choose the Stage, Wave content, Draft pools, or battle transition.

---

# 11. Validation

Map validation should report at minimum:

- Non-positive Width, Height, or Node Size
- Missing Grid Node template or NodesRoot
- Missing MapVisualTheme
- Missing, duplicate, or unsupported Tile masks
- Invalid Obstacle, Spawn, or Target presentation references
- Grid Node count different from Width multiplied by Height
- Duplicate, missing, or out-of-bounds Grid Positions
- Missing or multiple Spawn nodes
- Missing or multiple Target nodes
- Spawn or Target that is not Base Walkable
- No Base-Walkable route from Spawn to Target

Warnings identify the relevant Map or Grid Node and never silently rewrite authored content.

---

# 12. Approved Scope And Deferred Topics

Current scope includes rectangular handcrafted Maps, one Spawn, one Target, deterministic theme-based presentation, runtime Tower occupancy, Tile-only runtime refresh, and Map-template Stage composition.

Deferred topics include single-direction Tile assets, Multiple Spawn Routes, multiple Targets, special terrain, destructible terrain, runtime authored-feature replacement, multi-layer terrain, and procedural Map-data generation.
