# Tower Nexus - Map System

Document Set: System

---

# 1. Purpose And Ownership

Map System owns the grid-based battlefield used by runtime gameplay.

It owns:

- Authored Grid Node data
- Base and runtime walkability state
- Node and spatial queries
- Spawn and Target node identity
- MapVisualTheme-driven presentation
- One authored 3D Camera movement boundary
- Authoring and runtime visual refresh
- Reusable Map-template authoring
- Map validation

Stage System selects and creates the current Map. Tower Placement System requests runtime occupancy changes. Monster System consumes Map topology for pathfinding. Camera System consumes the Map framing origin, plane, and authored 3D Camera movement boundary for battle-local framing and movement bounds.

Map System does not own Stage flow, Tower placement rules, pathfinding algorithms, Monster behavior, Draft generation, Camera movement, or combat.

---

# 2. Grid Contract

The Map is an authored rectangular grid on the NodesRoot local XZ plane.

- Grid X maps to NodesRoot local X.
- Grid Y maps to NodesRoot local Z.
- Local Y remains available for height.
- Node Size controls center-to-center spacing.
- Orthogonal neighbors are traversable when effectively walkable.
- Diagonal traversal is disabled.

```text
Node Local Position = (Grid X * Node Size, 0, Grid Y * Node Size)
```

Map Root is the default framing origin for the complete grid. Generate Map translates NodesRoot so the midpoint between the first and last Grid Node centers coincides with Map Root, without changing any Grid Position or Grid Node local position.

The complete gameplay footprint extends one half Node Size beyond the outermost Grid Node centers on each grid axis. Camera System may use this footprint for initial framing, but Camera movement limits come from the separately authored 3D Camera movement boundary.

World-position queries convert through NodesRoot local space before resolving the coordinate. Map Root or NodesRoot may be translated or rotated without changing grid identity or neighbor semantics.

Every finite position inside the complete gameplay footprint belongs to exactly
one physical Grid cell for every Map world-position lookup. In each Map-
local grid axis, an internal cell owns the half-open interval from one half Node
Size below its center up to, but not including, one half Node Size above its
center. An exact internal boundary therefore belongs to the cell on its higher-
coordinate side; the Map's maximum outer boundary remains included in the last
cell. Local Y does not affect this membership, and a position outside the
complete gameplay footprint or containing a non-finite component resolves to no
physical Grid cell. Tower preview, existing-Tower targeting, footprint-anchor
lookup, and Monster placement-route classification share this rule.

Equivalent engines must preserve the grid-local coordinate frame, spacing, and neighbor semantics even when their world axes differ. Multi-layer, irregular, and procedural Map rules are outside the current contract.

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

A Grid Node's World Position remains its center and the stable spatial reference for topology queries. Monster System may resolve a bounded movement target inside a walkable Grid using the Map's local XZ frame. That movement target does not change Grid Position, World Position, node identity, walkability, or neighbor relationships. Spawn and Target movement targets remain their exact Grid centers.

---

# 4. Authored Hierarchy Contract

```text
Map Root
├── NodesRoot
│   └── GridNode
│       └── VisualRoot
│           ├── TileVisualRoot
│           │   └── TileVisualInstance
│           └── FeatureVisualRoot
│               ├── ObstacleVisualInstance
│               └── SpawnOrTargetVisualInstance
├── MapCameraBoundary
│   -> MapCameraBoundary adapter
│   -> BoxCollider
└── CameraDefaultPose
```

Ownership rules:

- `NodesRoot` owns generated Grid Nodes.
- `VisualRoot` owns presentation generated from MapVisualTheme.
- `TileVisualRoot` owns topology-dependent Tile presentation.
- `FeatureVisualRoot` owns authored static feature presentation.
- Manually authored decorations that must survive refresh stay outside generated roots.
- The `MapCameraBoundary` child GameObject owns both the Map-owned boundary adapter and exactly one 3D BoxCollider.
- The Map owner keeps explicit references to that adapter and to one direct child named `CameraDefaultPose`; Stage System receives the complete Map, boundary, and default-pose identity through those references rather than hierarchy discovery.
- The BoxCollider is a trigger on a dedicated non-gameplay Layer. It does not participate in pathfinding, Tower placement, Monster collision, or runtime occupancy.
- The volume defines permitted Camera reference positions rather than the rectangular gameplay footprint. It may include deliberate surrounding presentation space.
- `CameraDefaultPose` is independent from the moving Camera hierarchy and authors the world pose restored for a fresh instance of this Map. Its position must lie inside the BoxCollider.
- The boundary and default-pose GameObjects are separate from NodesRoot and survive Generate, Clear, and visual-refresh operations.

Before generated children may be removed, VisualRoot must belong to its Grid Node, TileVisualRoot and FeatureVisualRoot must be distinct descendants of VisualRoot, neither may contain the other, and no root may point into another Grid Node or outside NodesRoot.

Refresh owns only TileVisualRoot children and FeatureVisualRoot children. Invalid ownership stops the refresh; it is never repaired by deleting or recreating roots.

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

The current art set supports all sixteen masks:

| Walkable Directions | Shape |
|---|---|
| Up + Down + Left + Right | Four-way |
| None | Isolated |
| Up | Dead end |
| Down | Dead end |
| Left | Dead end |
| Right | Dead end |
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

Tile entries use only the Up, Down, Left, and Right bits. Each of the sixteen masks appears exactly once; additional, duplicate, and invalid-bit entries are invalid. Every Tile entry has a valid prefab.

The Obstacle list is non-empty and contains no null or duplicate references. Spawn and Target visuals are valid references.

## 5.2 Deterministic Variants

Obstacle and future Tile variants are deterministic from:

- Map Visual Seed
- Grid Position
- Visual category

The same seed and authored Grid Node data produce the same visual result. Ordinary refresh never rerolls variants; changing the seed may intentionally do so.

Deterministic selection does not consume the gameplay random stream or depend on a process-specific object hash, and it produces a valid selection index for every supported integer input.

---

# 6. Authoring Refresh

Authoring refresh rebuilds both Tile and Feature presentation from authored state.

Before changing presentation, authoring refresh validates all inputs needed by every affected Grid Node, including root ownership, Tile resolution, and Feature assets. If any error exists, it reports the collected failures and leaves the entire existing presentation unchanged.

After a successful preflight, for every Grid Node:

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

Candidate occupancy simulation is read-only. It may query the ordered Spawn-to-Target route that would exist after the proposed blockers, but it does not change authored Base Walkable state, active Runtime Occupied state, Tile presentation, or Monster route state.

For accepted Tower placement, one authoritative new Spawn-to-Target Route and
all living-Monster route revisions are prepared against the same simulated
topology before Runtime Occupied changes. Monster System determines whether
each Monster can continue that Route from its physical current Grid, must reach
it through a walkable Grid connector, or requires relocation to the nearest
eligible recovery Grid. Tower Placement System coordinates the commit; Map
System supplies topology, cell membership, and occupancy state but does not
classify, move, or relocate Monsters.

After one committed occupancy change:

1. Effective Is Walkable changes.
2. The prepared Monster route-continuation, rejoin, or relocation revision
   becomes active before gameplay advances to another frame.
3. Pathfinding and placement consume the new state.
4. Tile connection presentation refreshes using neighboring effective Is Walkable states.
5. Feature presentation remains unchanged.

Runtime refresh must not:

- Create an Obstacle because a Tower occupies a node
- Replace an authored Obstacle variant
- Remove or replace Spawn or Target presentation
- Modify Base Walkable

Runtime refresh also completes a full Tile-only preflight before removing any Tile presentation. A failure leaves all existing Tile and Feature presentation unchanged; partial refresh is invalid. Because runtime Tile refresh is post-commit presentation, its failure does not roll back accepted Runtime Occupied state, Monster revisions, or held-Draft consumption.

The current version may refresh all Tile visuals after one topology change. Partial refresh of the changed nodes and orthogonal neighbors is an optimization that must preserve identical results.

---

# 8. Map Authoring Workflow

The approved handcrafted workflow is:

1. Create a Map Root.
2. Configure Width, Height, Node Size, Grid Node template, NodesRoot, MapVisualTheme, and Map Visual Seed.
3. Generate the rectangular Grid Node scaffold. When Camera authoring references are absent, the Inspector Generate action may adopt one unambiguous valid direct child or create the missing `MapCameraBoundary` and `CameraDefaultPose` scaffold before Grid preflight.
4. Author `CameraDefaultPose` independently from the moving Camera hierarchy, then size and position the BoxCollider so that pose and the complete permitted Camera reference-position region lie inside it, including any intentional decorative margin outside the gameplay footprint.
5. Edit each node's Base Walkable and Node Type.
6. Refresh generated Map presentation.
7. Validate the Map.
8. Save the completed Map Root as a reusable Map template referenced by StageDefinition.

The authoring surface contains:

- Generate Map
- Clear Map
- Refresh Map Visual
- Validate Map

Generate Map replaces an existing generated Grid scaffold and therefore requires explicit author intent. Its Inspector authoring path resolves Camera authoring before destructive Grid work. Valid referenced Camera authoring is preserved. An absent reference may adopt exactly one valid direct-child candidate; no candidate permits creation of the corresponding default scaffold; reserved-name children without their required component, wrong-name or nested adapters, and multiple candidates stop generation without creating another. Generate Map never replaces existing Camera authoring, derives final BoxCollider dimensions from Grid size, or moves either Camera authoring object during later generation. After the authoring scaffold is present, generation validates the complete Camera configuration and Grid Node template hierarchy before removing an existing Grid scaffold.

The reusable programmatic Generate Map operation and Validate Map are reporting-only with respect to Camera authoring. They never create, assign, replace, reparent, resize, or otherwise repair it.

The current runtime loads the authored Map template. It does not regenerate Map data procedurally from a separate Map definition.

---

# 9. Spatial Query Boundary

Map System provides:

- Coordinate-to-node and position-to-node lookup
- Unique physical-cell membership for finite Map-local XZ positions
- Orthogonal neighbor lookup
- Effective walkability queries
- Spawn and Target queries
- Temporary topology evaluation support

World-position lookup resolves through NodesRoot local space. Public queries maintain their own internal lookup readiness; consumers do not rebuild or inspect Map lookup storage.

Monster System chooses and executes the pathfinding algorithm. Tower Placement System chooses when to simulate or commit occupancy.

---

# 10. Stage Boundary

The reusable Map template contains:

- Dimensions and Node Size
- Grid Nodes and authored state
- MapVisualTheme reference
- Visual Seed
- Generated Tile and Feature presentation
- One `MapCameraBoundary` child GameObject with its adapter and 3D BoxCollider
- One direct-child `CameraDefaultPose` independently authored from runtime Camera movement

StageDefinition references one Map template. Stage System creates it and establishes the Active Map before Monster, Draft, and Placement runtime begins.

Map System does not choose the Stage, Wave content, Draft pools, or battle transition.

Camera System may consume the Active Map's framing origin, plane, gameplay footprint, and authored 3D Camera movement boundary. Map System does not decide when Camera input is available or how the view moves inside that boundary.

---

# 11. Validation

Map validation is available through one reusable programmatic operation that returns valid/invalid state together with aggregated errors and warnings. Authoring UI reports this result, and Stage validation reuses the same operation rather than duplicating Map rules.

Structural validation scans the NodesRoot hierarchy directly so duplicate coordinates and malformed nodes cannot be hidden by lookup storage.

Map validation should report at minimum:

- Non-positive Width, Height, or Node Size
- Missing Grid Node template or NodesRoot
- Missing or externally owned explicit `MapCameraBoundary` adapter reference
- Missing, misnamed, externally owned, nested, or duplicated `MapCameraBoundary` child GameObject
- Missing or multiple BoxColliders on the boundary GameObject
- A BoxCollider that is disabled, inactive, degenerate, outside its Map ownership hierarchy, not a trigger, or configured on a gameplay Layer
- Missing, misnamed, nested, duplicated, inactive, or externally referenced `CameraDefaultPose`
- A default Camera position outside the authored BoxCollider volume
- Missing MapVisualTheme
- Missing, duplicate, extra, invalid-bit, or otherwise unsupported Tile masks
- Null Tile prefabs; empty, null-containing, or duplicate Obstacle lists; missing Spawn or Target presentation references
- Invalid or externally owned Visual, Tile, or Feature roots
- Grid Node count different from Width multiplied by Height
- Duplicate, missing, or out-of-bounds Grid Positions
- Missing or multiple Spawn nodes
- Missing or multiple Target nodes
- Spawn or Target that is not Base Walkable
- No Base-Walkable route from Spawn to Target
Warnings identify the relevant Map or Grid Node and never silently rewrite authored content.

---

# 12. Approved Scope And Deferred Topics

Current scope includes rectangular handcrafted Maps, one Spawn, one Target, deterministic theme-based presentation, runtime Tower occupancy, read-only candidate-topology queries, bounded grid-local Monster movement targets, Tile-only runtime refresh, Map-template Stage composition, a complete rectangular gameplay footprint, and one Map-authored 3D Camera movement boundary.

Deferred topics include Multiple Spawn Routes, multiple Targets, special terrain, destructible terrain, runtime authored-feature replacement, multi-layer terrain, and procedural Map-data generation.
