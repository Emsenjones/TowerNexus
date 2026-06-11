# Task002_TowerAttackVfxRuntimeFoundation

## Objective

Implement the first runtime foundation for tower attack visual effects.

This task connects AttackConfig VFX references to Tower Runtime Combat and provides reusable runtime VFX control behavior.

The goal is to support:

- Projectile Release VFX
- ChannelBeam VFX
- PeriodicArea VFX

without changing existing combat behavior.

---

## Related Documents

- 07_TowerFrameworkSystem.md
- 08_TowerRuntimeCombatSystem.md
- 09_ProjectileSystem.md

---

## Scope

### In Scope

Implement runtime support for:

- Projectile release VFX playback
- ChannelBeam VFX lifecycle
- PeriodicArea VFX lifecycle
- Reusable BeamVfxBehaviour

Support the following VFX lifecycle operations where relevant:

```text
Spawn
Bind
Update
Stop
Destroy
```

### Out of Scope

- Projectile impact VFX
- Projectile travel VFX
- Particle collision driven combat logic
- Beam damage logic
- Beam target searching
- Beam range validation
- Beam cooldown logic
- Object pooling
- VFX asset creation
- Particle tuning
- Material tuning
- Shader authoring

---

## Runtime Ownership Rules

Tower Runtime Combat owns:

```text
When to spawn VFX
When to stop VFX
Which target is bound
Which attack state is active
```

VFX components own visual presentation only.

They must never own:

```text
Damage
Target search
Target selection
Range checks
Cooldown logic
Attack state transitions
```

---

# Part 1: Projectile Release VFX

Supported Archetypes:

```text
StraightProjectile
ArcProjectile
```

Runtime Flow:

```text
Attack Animation Event
    ↓
FireProjectile()
    ↓
Spawn projectile
    ↓
Spawn projectileReleaseVfxPrefab at attackOrigin
```

Projectile release VFX should be spawned at attackOrigin position and use attackOrigin rotation when practical.

If projectile spawning fails or projectile configuration is invalid, projectile release VFX should not create a separate gameplay result.

Projectile release VFX is presentation-only and must not affect:

- Projectile launch direction
- Projectile target snapshot
- Projectile damage
- Projectile hit detection
- Projectile lifetime

---

# Part 2: ChannelBeam Runtime VFX

Create:

```text
BeamVfxBehaviour
```

This component is intended to be attached to:

```text
channelBeamVfxPrefab
```

The prefab may be an empty GameObject with BeamVfxBehaviour and Inspector-configured visual references.

## BeamVfxBehaviour Responsibilities

Own:

```text
Visual binding
Visual updates
Visual positioning
Visual stop / cleanup helper
```

Must not own:

```text
Damage
Target validation
Target search
Range checks
Combat state
Cooldown logic
```

Suggested API:

```csharp
public void Initialize(
    Transform startAnchor,
    Transform targetAnchor)
```

Exact naming may differ.

Codex may propose an alternative API if responsibility boundaries remain unchanged.

BeamVfxBehaviour must not search for anchors automatically.

It should only use anchors passed in by TowerCombatBehaviour.

Optional references:

```text
startVfxRoot
beamVisualRoot
hitVfxRoot
lineRenderer
```

All optional references must be null-safe.

If lineRenderer is assigned, it may be used to connect startAnchor and targetAnchor.

If lineRenderer is not assigned, the component may still update startVfxRoot and hitVfxRoot only.

## Runtime Update

While active:

```text
startVfxRoot follows startAnchor
hitVfxRoot follows targetAnchor
```

If a LineRenderer is assigned, it may be updated as:

```text
LineRenderer position 0 = startAnchor.position
LineRenderer position 1 = targetAnchor.position
```

Alternative beam visual implementations are allowed if they preserve the same responsibility boundary.

## Invalid Target Handling

If:

```text
targetAnchor == null
```

BeamVfxBehaviour must:

```text
Remain safe
Avoid exceptions
Avoid target searching
Avoid selecting a replacement target
```

BeamVfxBehaviour should wait for TowerCombatBehaviour to stop it.

## Stop Behavior

Provide a stop entry point.

Example:

```csharp
public void StopAndDestroy()
```

Exact naming may differ.

When stopped:

```text
Beam visual ends
Runtime object is cleaned up
```

No gameplay logic should occur.

---

# Part 3: ChannelBeam Combat Integration

Channel start:

```text
Instantiate beam VFX
Initialize beam VFX
Bind attackOrigin
Bind current target HitAnchor if available
Fallback to current target transform if HitAnchor is unavailable
```

TowerCombatBehaviour owns the target binding decision.

BeamVfxBehaviour should not search for a new target if the current target becomes invalid.

During channeling, TowerCombatBehaviour remains responsible for:

```text
Damage ticks
Duration checks
Target validity
Cooldown transitions
```

BeamVfxBehaviour only updates visuals.

Channel end:

```text
Stop beam VFX
Destroy beam VFX
Clear active beam reference
```

ChannelBeam VFX should end when any of the following happens:

```text
Channel duration expires
Target dies
Target leaves valid state
Channel is interrupted
Tower exits Channeling state
```

---

# Part 4: PeriodicArea Runtime VFX

Start:

```text
Spawn area VFX
Attach to attackOrigin
Store active area VFX reference
```

Only one active PeriodicArea VFX instance should exist per tower.

PeriodicArea VFX should use attackOrigin as its visual center because PeriodicArea range checks are also centered on attackOrigin.position.

Runtime should avoid spawning duplicate PeriodicArea VFX instances while one is already active.

Active state uses authored prefab scale. No runtime scaling logic is required.

End:

```text
Stop area VFX
Destroy area VFX
Clear active area VFX reference
```

---

## Runtime State Ownership

TowerCombatBehaviour owns:

```text
Starting projectile release VFX
Starting ChannelBeam VFX
Stopping ChannelBeam VFX
Starting PeriodicArea VFX
Stopping PeriodicArea VFX
Target binding decisions
Attack state transitions
Damage timing
```

BeamVfxBehaviour owns:

```text
Beam visual positioning
Beam visual reference updates
Beam visual cleanup helper
```

PeriodicArea VFX owns:

```text
Looping area visual presentation only
```

VFX objects must not become independent combat controllers.

---

## Safety Rules

- All VFX references are optional
- Null VFX references must not produce warnings or errors
- Runtime VFX failures must not block combat execution
- VFX logic must not change existing combat behavior
- Missing optional BeamVfxBehaviour visual references must be safe
- Existing towers without VFX configured must continue working

---

## Acceptance Criteria

### Projectile Release

StraightProjectile and ArcProjectile can optionally play:

```text
projectileReleaseVfxPrefab
```

without affecting combat logic.

### ChannelBeam

```text
Spawn beam VFX
Bind start
Bind target
Update visuals
Stop correctly
Clear active beam reference
```

### PeriodicArea

```text
Spawn looping VFX
Attach to attackOrigin
Use authored prefab scale
Avoid duplicate active instances
Stop correctly
Clear active area VFX reference
```

### Safety

The implementation supports:

- Null VFX references
- Missing optional BeamVfxBehaviour visual references
- Missing target anchor fallback to target transform
- Existing towers without VFX configured

### Exclusions Verified

- No projectile impact VFX
- No projectile travel VFX
- No particle collision driven combat logic
- No object pooling
- No damage logic moved into VFX components
- No target searching moved into VFX components
- No final VFX asset creation or particle tuning

---

## Implementation Plan Requirement

Before implementation, Codex should provide:

- Files to modify
- New runtime components
- Public APIs
- Runtime ownership boundaries
- How activeBeamVfx and activePeriodicAreaVfx references are stored and cleared
- How duplicate PeriodicArea VFX instances are prevented
- How BeamVfxBehaviour remains null-safe when optional references are missing

Implementation should not begin until the plan is reviewed.
