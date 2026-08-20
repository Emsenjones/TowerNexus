# Task001 - Stage1 To Stage6 Greybox Map Prefabs

Status: Completed; six Map Prefabs are accepted as the v0.2 greybox baseline after Unity Validate Map checks and static structure review

Depends on: Existing Map, Stage, Tower Placement, Camera, Tower footprint, and Tower attack-range authoring contracts

## 1. Goal

Author six independently usable greybox Map Prefabs that spatially support the Stage1-Stage6 design blueprint.

Each Map must provide a valid Spawn-to-Target route, legal Reference Build placement, meaningful local attack-coverage zones, and the Stage-specific spatial geometry required by its intended experience.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/02_StageSystem.md`
- `Doc/System/05_MapSystem.md`
- `Doc/System/06_CameraSystem.md`
- `Doc/System/09_TowerPlacementSystem.md`
- `Doc/System/10_TowerFrameworkSystem.md`
- `Doc/System/11_TowerRuntimeCombatSystem.md`

## 3. Current Authored Tower Inputs

Grid Node spacing is one world unit. Current base Tower inputs are:

| TowerFamily | Base Attack Range | Occupied Grid Nodes |
|---|---:|---:|
| Archer | 2 | 2 |
| Cannon | 3 | 3 |
| Magic | 1.5 | 1 |
| Drone | 4 | 4 unique nodes |

Range and footprint values are implementation inputs, not final balance claims. Task002 may later revise base attack identity; any accepted Attack Range change must return to Task001 for a Map coverage regression.

## 4. Current v0.2 Map Planning Table

| Stage | GridSize | ReferenceTowerCount |
|---|---:|---:|
| Stage1 | 8x8 | 2 |
| Stage2 | 10x10 | 3 |
| Stage3 | 12x12 | 4 |
| Stage4 | 14x14 | 5 |
| Stage5 | 14x14 | 5 |
| Stage6 | 14x14 | 5 |

These values are intentional first-pass estimates. They exist so the first six greyboxes can be authored and observed before further correction.

The v0.2 Map baseline does not require the Map to support a player spending every Draft on a new Tower. Map acceptance covers the Reference Build and reasonable alternative placements. The all-deployment edge case is excluded from initial Map sizing.

The Blueprint Reference Core contains one Basic and one Behaviour Upgrade; it is not the maximum legal single-Tower Upgrade stack. Task009-Task014 may use a legal over-concentrated build to prove that missing coverage or cooperation matters, but that later balance run does not retroactively require Task001 to size the Map for every possible Draft allocation.

## 5. Attack-Range And Route Guardrails

Map size alone does not determine useful coverage. Base Walkable topology and route folding must create distinct combat zones.

- A longest-range Base Tower in its best ordinary position must not trivially cover both the primary entrance zone and the Target zone.
- Stage2-Stage6 should provide at least three recognizable route zones: early, middle, and late.
- Cannon and Drone should retain long-range advantages without becoming placement-independent.
- Magic must have route-adjacent positions where its smaller range creates persistent local contact.
- Stage6 must provide one strong shared-target segment where two matching Elemental Cores can overlap, while leaving other route segments outside that pair's full coverage.
- Range-increasing Basic Upgrades receive a separate stress check. A strong range specialization may broaden one position substantially, but should not erase the complete route-placement problem.

## 6. In Scope

- One Map Prefab for each Stage
- v0.2 Grid sizes from 8x8 through 14x14
- Spawn Grid and Target Grid
- Authored Base Walkable topology
- One intended best Monster route
- Runtime-fillable Grid Nodes that preserve a legal route
- Reference Build placement
- At least one reasonable alternative placement pattern
- Stage-specific attack-range, contact, pursuit, and overlap geometry
- Map Camera Boundary authoring
- Map validation and route checks

## 7. Out Of Scope

- Guaranteeing capacity when every Draft is spent on a new Tower
- Tower, Monster, Effect, Buff, or Upgrade value tuning
- Player Progress Requirement authoring
- Final Draft pool authoring
- Final MonsterWaveConfig tuning
- Final art polish
- Runtime system refactors unless a separately reviewed authoring blocker is found

## 8. Ownership

| Owner | Responsibility |
|---|---|
| Stage Design Blueprint | Stage experience, Reference Build, required capability, and Anti-pattern |
| Task001 | Candidate size, range/route guardrails, Map implementation, and evidence |
| Map Prefab | Grid, Spawn, Target, Base Walkable topology, visual boundary, and Camera Boundary |
| Tower Framework | Authored Tower footprint |
| Tower Runtime Combat | Authored Base Attack Range and effective attack behavior |
| Map System | Validation, walkability, spatial queries, and runtime topology |
| Tower Placement | Footprint and route-preservation validation |
| Camera System | Active Map framing and authored movement bounds |

## 9. Execution Collaboration

- The user owns Unity Map Prefab authoring, Spawn and Target placement, Base Walkable editing, route shaping, Camera Boundary authoring, and Play Mode placement checks.
- Codex reviews the authored geometry against Reference Tower count, attack ranges, footprint constraints, and Stage experience.
- The v0.2 values are the next testable shot. The user records where route, footprint, camera, or coverage validation misses; Codex then proposes the smallest size or topology correction.

## 10. Unity Authoring Checklist

- Create or update Stage1-Stage6 Map Prefabs.
- Author the exact v0.2 Grid size from the planning table.
- Set one valid Spawn and one valid Target.
- Author Base Walkable state and the intended best route.
- Place the complete structural Reference Build.
- Place at least one reasonable alternative arrangement.
- Confirm all tested placements preserve a route.
- Observe Base Attack Range previews at strong early, middle, and late positions.
- Stress-check available range-increasing Basic Upgrades.
- Confirm Stage3 has useful Magic contact positions.
- Confirm Stage4 has meaningful Drone pursuit coverage without trivial full-route coverage.
- Confirm Stage6 contains overlapping Core positions on one shared-target route segment.
- Author and validate one Map Camera Boundary per Map.
- Refresh authored Map visuals after topology changes.

## 11. Acceptance Criteria

- Six Map Prefabs exist and validate.
- Stage1-Stage6 Grid sizes are respectively 8x8, 10x10, 12x12, 14x14, 14x14, and 14x14.
- Stage1-Stage6 Reference Tower counts are respectively 2, 3, 4, 5, 5, and 5.
- Every Map has a valid Spawn-to-Target route.
- Every v0.1 Reference Build has at least one legal, strategically useful layout.
- At least one reasonable alternative arrangement is legal per Stage.
- Long-range Towers remain meaningfully position-dependent.
- Stage-specific spatial intent is observable.
- No Tower value or Wave tuning is used to hide an invalid Map.
- Failure to support an all-deployment board does not by itself fail this Task.

## 12. Validation

- Tower footprint verification against authored Prefabs
- Unity authoring validation for every Map
- Play Mode route validation before and after Reference placement
- Reference and alternative placement checks
- Base and range-upgraded coverage-preview checks
- Magic contact and Drone pursuit observation
- Stage6 matching-Core overlap check
- Camera framing and pan-boundary validation
- Path-scoped document and asset diff review

## 13. Review Note

Task001 completion freezes the six Maps only as usable v0.2 greybox baselines. The user ran Validate Map on every Stage1-Stage6 Prefab without errors; static review confirmed the planned dimensions, expected node counts, one Spawn, one Target, and one referenced MapCameraBoundary per Map. These values are not final balance claims. Task002 must return any accepted Attack Range change for Map coverage regression, and Task009-Task014 may later propose local route or placement refinements when Stage calibration reveals a specific spatial problem. Task003's higher-health calibration fixture does not change Map acceptance because it changes measurement sensitivity rather than geometry.
