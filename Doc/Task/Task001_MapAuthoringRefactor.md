# Task001 - Map Authoring Refactor

Status: Script implementation complete; awaiting user Unity authoring and Play Mode acceptance

Depends on: Current completed combat and placement baseline

## 1. Goal

Refactor the Map authoring path so one reusable Map template owns authored Grid Node data and obtains all generated presentation from one interchangeable `MapVisualTheme` asset.

This task is a clean break. It targets newly created Grid Node prefabs, MapVisualTheme assets, and Map prefabs. It does not migrate or preserve the legacy Map authoring path.

Runtime topology changes remain supported: Tower occupancy refreshes Tile connectivity but never creates authored Obstacles or replaces Spawn and Target presentation.

## 2. Source Documents

- `Doc/00_ProjectOverview.md`
- `Doc/04_MapSystem.md`
- `Doc/05_MonsterSystem.md`
- `Doc/07_TowerPlacementSystem.md`

The current System documents are authoritative for Base Walkable versus Runtime Occupied state, local-space grid mapping, the generated hierarchy, deterministic presentation, validation, and authoring/runtime refresh boundaries.

## 3. Clean-Break Boundary

Task001 does not migrate old Map data or serialized references.

- Do not preserve the legacy `isWalkable`, `generatedNodesParent`, or twelve direct Tile-prefab fields.
- Do not use `FormerlySerializedAs`.
- Do not add compatibility fields, legacy lookup, migration helpers, or old prefab-name cleanup.
- Do not convert, repair, or save existing Map, Grid Node, GameManager, scene, generated-node, or ScriptableObject assets.
- After the script refactor, legacy Map assets are not a supported runtime path.

In particular, this task does not modify or migrate `prefab_GridNode.prefab`, `prefab_MapGenerator.prefab`, `prefab_GameManager.prefab`, `Main.unity`, or their existing generated nodes and references.

After script implementation and static validation are complete, the user will create and configure the new Unity assets and perform Play Mode acceptance.

## 4. Required Authoring Model

### 4.1 MapVisualTheme

Add one reusable `MapVisualTheme` ScriptableObject containing:

- Explicit direction-mask-to-Tile-prefab entries
- Obstacle prefab list
- Spawn prefab
- Target prefab

Direction masks use only the `Up`, `Down`, `Left`, and `Right` bits. The Theme must contain exactly one entry for each of the sixteen supported masks in `Doc/04_MapSystem.md` and no other entries. Single-direction masks represent dead ends.

Every Tile prefab must be non-null. The Obstacle list must be non-empty, contain no null references, and contain no duplicate references. Spawn and Target prefabs must be non-null.

Every topology mask, including each single-direction dead end, resolves through its matching Tile entry.

### 4.2 Grid Node State

`GridNodeBehaviour` exposes:

- Grid Position
- Base Walkable
- Runtime Occupied
- Effective `IsWalkable = BaseWalkable && !RuntimeOccupied`
- Node Type
- Visual Root
- Tile Visual Root
- Feature Visual Root

Base Walkable and Node Type are serialized authored state. Runtime Occupied is runtime-only, starts false, and is not saved as authored Map data.

Provide explicit operations for initialization, authored-state changes, occupancy changes, runtime-state reset, Grid Position changes, and Node Type changes. Do not retain an ambiguous `SetWalkable` operation.

### 4.3 Generated Hierarchy And Ownership

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

Before a refresh may delete generated children, each Grid Node must satisfy all of the following:

- VisualRoot belongs to that Grid Node hierarchy.
- TileVisualRoot and FeatureVisualRoot are descendants of VisualRoot.
- TileVisualRoot and FeatureVisualRoot are different objects.
- Neither root contains the other.
- No root reference points into another Grid Node or outside NodesRoot.

Refresh owns only the children of TileVisualRoot and FeatureVisualRoot. It must not remove decorations outside these roots, outside the Grid Node, or outside NodesRoot. Missing or unsafe roots are validation errors; scripts do not silently create or repair them.

## 5. MapGenerator Authoring And Query Contract

`MapGeneratorBehaviour` uses Width, Height, Node Size, Grid Node prefab, NodesRoot, MapVisualTheme, and Map Visual Seed.

The designer-facing operations are:

- Generate Map
- Clear Map
- Refresh Map Visual
- Validate Map

Node-dictionary rebuilding is private/internal and is not a designer operation.

### 5.1 Local-Space Mapping

- Grid X maps to NodesRoot local X.
- Grid Y maps to NodesRoot local Z.
- A generated node's local position is `(x * NodeSize, 0, y * NodeSize)`.
- World-position lookup first uses `NodesRoot.InverseTransformPoint(worldPosition)`, then rounds local X and local Z by Node Size.
- Translating or rotating Map Root or NodesRoot must not invalidate queries.

Task001 does not introduce multi-layer, irregular, or procedural Map rules.

### 5.2 Dictionary Lifecycle

- Build the dictionary on `OnEnable`.
- Synchronize it immediately after Generate Map and Clear Map.
- Every public Map query passes through one `EnsureNodeDictionaryValid` path before reading cached nodes.
- Public callers never need to rebuild the dictionary first.
- Duplicate coordinates may be reported during rebuilding, but rebuilding must not destroy the hierarchy evidence used by validation.
- A* obtains nodes and neighbors only through Map query APIs; it does not inspect `NodeDictionary.Count` or invoke rebuilding.

Do not introduce a generic cache framework or additional service layer.

## 6. Atomic Generation And Refresh

Generate Map, Refresh Map Visual, and Refresh Runtime Tile Visuals are destructive only after a complete preflight succeeds.

Generate Map preflight validates all required generator inputs and the Grid Node prefab hierarchy before clearing existing Grid Nodes.

Refresh preflight scans every node that would be processed and aggregates all relevant failures before deleting any visual child. At minimum it validates:

- NodesRoot and MapVisualTheme
- Every affected Grid Node and its safe root ownership
- Every required Tile mask and its prefab
- For Authoring Refresh, all required Obstacle, Spawn, and Target configuration

If preflight fails, the operation reports all collected problems, performs no deletion or instantiation, and leaves every node's existing presentation unchanged. A refresh must never leave a partially updated Map.

After successful preflight:

- Authoring Refresh rebuilds Tile and Feature presentation from Base Walkable and Node Type.
- Runtime Tile Refresh rebuilds only TileVisualRoot children from effective Is Walkable.

## 7. Deterministic Presentation

- Obstacle selection uses Map Visual Seed, Grid Position, and visual category.
- Do not use `UnityEngine.Random` or `GetHashCode()`.
- The same seed, coordinate, and category produce the same selection across refresh and reload.
- Index calculation remains valid for negative intermediate values and `int.MinValue`.
- Ordinary refresh never rerolls a variant; changing the seed may intentionally do so.

## 8. Runtime Occupancy Integration

- Placement simulation continues to use a temporary blocked set and mutates neither Base Walkable nor Runtime Occupied.
- Accepted placement commits Runtime Occupied instead of changing Base Walkable.
- Effective walkability changes immediately after the commit.
- Runtime Tile Refresh runs after occupancy commit and updates TileVisualRoot only.
- Runtime refresh never modifies FeatureVisualRoot, creates an Obstacle, or replaces Spawn or Target presentation.
- Monster path recalculation begins after occupancy commit and Runtime Tile Refresh.
- Tower removal and redeployment remain out of scope.

## 9. Reusable Map Validation

Map validation must have one programmatic entry point that:

- Does not depend on an Odin button or Unity Editor API
- Returns an explicit valid/invalid result
- Aggregates all errors and warnings
- Can be reused by Task003 Stage validation without duplicating Map rules

The Inspector Validate Map button only invokes this entry point and reports its result.

Structural validation scans the NodesRoot hierarchy directly. It must not use a deduplicated node dictionary as its source of truth.

Validation reports at minimum:

- Non-positive Width, Height, or Node Size
- Missing Grid Node prefab, NodesRoot, or MapVisualTheme
- Invalid Grid Node prefab or instance root ownership
- Tile entries with invalid bits, unsupported masks, missing masks, duplicate masks, extra masks, or null prefabs
- Empty Obstacle list, null or duplicate Obstacle prefabs, or missing Spawn/Target prefabs
- Grid Node count different from Width multiplied by Height
- Duplicate, missing, or out-of-bounds Grid Positions
- Missing or multiple Spawn nodes
- Missing or multiple Target nodes
- Spawn or Target that is not Base Walkable
- No Base-Walkable route from Spawn to Target
Validation never silently repairs authored data.

## 10. Implementation Scope

Add:

- `Assets/Scripts/Map/MapVisualTheme.cs`

Modify:

- `Assets/Scripts/Map/GridNodeBehaviour.cs`
- `Assets/Scripts/Map/MapGeneratorBehaviour.cs`
- `Assets/Scripts/Pathfinding/AStarPathfindingService.cs`
- `Assets/Scripts/TowerDeployment/TowerDeployController.cs`
- `Doc/Task/Task001_MapAuthoringRefactor.md`
- `Doc/04_MapSystem.md` only where the stable local-space and validation contracts require synchronization

Do not modify:

- Any prefab, scene, existing Map, generated Grid Node, TileVisualInstance, or ScriptableObject asset
- Task002 or Task003 implementation
- Temporary bootstrap, compatibility path, test scene, or Stage Composition runtime

## 11. Out Of Scope

- Legacy data or asset migration
- StageDefinition and Stage composition implementation
- Multiple Spawn Routes or multiple Targets
- Procedural Map-data generation
- Destructible or special terrain
- Partial-neighbor refresh optimization
- Runtime replacement of authored Feature presentation
- Tower removal or redeployment
- Tower placement rule redesign

## 12. Acceptance Criteria

- One new Map template can switch themes without changing gameplay data.
- Authored terrain and runtime Tower occupancy are separate state.
- Generate Map and both refresh paths complete atomically or leave existing content untouched.
- Root ownership validation prevents refresh from deleting content outside its owned roots.
- MapVisualTheme accepts exactly the sixteen supported masks and valid feature assets.
- Authoring Refresh produces Tile, Obstacle, Spawn, and Target presentation under the approved roots.
- Runtime occupancy refresh changes Tile presentation only.
- Deterministic variants remain stable across repeated refresh and reload.
- Every public Map query maintains its own dictionary lifecycle requirement.
- A* no longer rebuilds or inspects the dictionary directly.
- Placement simulation, occupancy commit, Runtime Tile Refresh, and Monster recalculation preserve the approved order.
- Programmatic Map validation aggregates errors and warnings and is reusable by Task003.
- Rebuild Node Dictionary is not exposed as a designer action.
- No legacy compatibility or migration path is added.

## 13. Validation And Handoff

Codex stops after script implementation and static validation.

Run:

```text
dotnet build Assembly-CSharp.csproj --no-restore -m:1 -nr:false -p:LangVersion=8.0
git diff --check
```

Within the modified scripts and Task001 documentation, confirm there is no remaining:

- `SetWalkable`
- `SetNodeWalkable`
- `generatedNodesParent`
- Twelve direct Tile-prefab fields
- External `RebuildNodeDictionary` call
- Legacy Tile-name cleanup
- Old Map compatibility or migration path

Legacy serialized fields that remain only in untouched prefab or scene YAML are not Task001 failures. Pre-existing `Main.unity` whitespace is reported separately and is not repaired in this task.

After Implementation Complete, the user owns:

- Creating the new Grid Node prefab and VisualRoot hierarchy
- Creating and configuring the MapVisualTheme asset
- Creating the new Map prefabs
- Temporarily placing one new Map prefab in a scene
- Temporarily wiring the current A*, MonsterSpawner, Tower Placement, and Tower Deployment Map references
- Unity Play Mode, Runtime Occupancy, deterministic presentation, and visual acceptance

These operations do not require Codex to modify or save `Main.unity`, `prefab_GameManager.prefab`, or another existing Unity asset.
