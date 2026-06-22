# Task003 - Tower Framework Integration Validation

---

# 1. Source Of Truth

Primary system document:

- `Docs/07_TowerFrameworkSystem.md`

Depends on:

- `Docs/Task/Task001_TowerFrameworkSchemaRefactor.md`
- `Docs/Task/Task002_FourTowerConfigAssetMigration.md`

This task is a compatibility and validation pass after the Tower Framework schema and config assets are migrated.

---

# 2. Goal

Ensure the refactored Tower Framework can be consumed by existing systems without introducing broad Phase 2 runtime work.

This task should make the project compile or reach the closest available validation signal after the framework refactor.

---

# 3. Integration Surfaces

Review and update references in:

- `Assets/Scripts/TowerRuntimeCombat/TowerCombatBehaviour.cs`
- `Assets/Scripts/TowerRuntimeCombat/TowerAttackState.cs`
- `Assets/Scripts/Projectile/ProjectileBehaviour.cs`
- `Assets/Scripts/TowerDeployment/TowerDraftSystem.cs`
- `Assets/Scripts/TowerDeployment/TowerDraftUI.cs`
- `Assets/Scripts/TowerDeployment/PendingTowerItemUI.cs`
- `Assets/Scripts/TowerDeployment/TowerPlacementController.cs`
- `Assets/Scripts/TowerDeployment/TowerDeployController.cs`

The goal is integration compatibility, not final runtime behavior.

---

# 4. Implementation Scope

## 4.1 Runtime Enum Compatibility

Update code references from old enum values to new framework values:

| Old | New |
|---|---|
| `StraightProjectile` | `DirectionProjectile` |
| `ArcProjectile` | `ArcProjectile` |
| `ChannelBeam` | `MagicOrb` |
| `PeriodicArea` | `Drone` |
| `Watch` | `Drone` |

Compatibility expectations:

- Archer and Cannon should keep mapping to projectile behavior.
- Magic and Drone may initially be unsupported by runtime and emit clear warnings until Phase 2.
- Avoid implementing Magic Orb and Drone runtime here.

## 4.2 TowerCombatBehaviour Compile Compatibility

Minimum acceptable changes:

- Replace old enum references.
- Keep DirectionProjectile and ArcProjectile projectile release path compiling.
- For MagicOrb and Drone branches, either:
  - no-op with a clear warning, or
  - route to explicit placeholder methods that do not implement full behavior.

Do not carry forward old ChannelBeam or PeriodicArea behavior as the current Magic/Drone implementation.

## 4.3 ProjectileBehaviour Compile Compatibility

Minimum acceptable changes:

- Replace `StraightProjectile` references with `DirectionProjectile`.
- Keep ArcProjectile path compiling.
- Do not implement Tracking flight in this task.
- If Tracking-related enum support is introduced now, unsupported Tracking should fail gracefully with clear logging until Phase 2.

## 4.4 Draft And Placement Compatibility

Check that draft and placement systems still consume TowerDefinition data after:

- adding `towerCategory`
- adding `towerLevelConfigs`
- migrating current tower assets

Expected behavior:

- Draft can still display tower names/icons from TowerDefinition.
- Placement can still instantiate TowerDefinition tower prefab.
- TowerDefinition validation still checks TowerAnchorSet.

No new deploy, level-up, or upgrade workflow should be added here.

---

# 5. Out Of Scope

Do not implement:

- Magic Orb orbit/contact runtime.
- Drone launch/hover/return/recharge runtime.
- Tracking projectile flight.
- Tower upgrade structure.
- Upgrade application.
- Draft upgrade selection.
- PlayerSystem changes.
- Object pooling.

---

# 6. Acceptance Criteria

- No compile errors remain from Tower Framework enum or field refactors.
- Archer and Cannon framework references map to DirectionProjectile and ArcProjectile.
- Magic and Drone do not accidentally use old ChannelBeam or PeriodicArea runtime behavior as current implementation.
- TowerDefinition consumers continue to compile.
- ProjectileBehaviour compiles with the renamed projectile archetype.
- Draft and placement code still compile against TowerDefinition.
- Any unsupported Phase 2 behavior logs clear warnings or has explicit TODO placeholders.

---

# 7. Suggested Validation

Run targeted searches:

```text
rg -n "StraightProjectile|ChannelBeam|PeriodicArea|Watch" Assets/Scripts
rg -n "DirectionProjectile|MagicOrb|Drone" Assets/Scripts
git diff --check -- Assets/Scripts Assets/Configs
```

Run the best available compile validation:

```text
dotnet build TowerNexus.sln --no-restore
```

If local `dotnet build` is not trustworthy because of SDK or Unity project constraints, document the exact error and use targeted source searches as fallback validation.
