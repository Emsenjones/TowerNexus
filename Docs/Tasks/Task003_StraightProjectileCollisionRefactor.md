

# Task003 – StraightProjectile Collision Refactor

## Objective

Refactor StraightProjectile runtime behavior to match the latest TowerFrameworkSystem and TowerRuntimeCombatSystem definitions.

StraightProjectile should no longer depend on reaching a stored target position before evaluating a hit.

Instead:

- The selected target is used only to determine projectile launch direction.
- After launch, the projectile travels independently.
- The projectile continuously performs hit detection during flight.
- The projectile may hit any valid monster encountered during flight.
- The projectile is automatically destroyed when its maximum lifetime expires.

---

## Scope

Included:

- StraightProjectile runtime flight logic.
- StraightProjectile hit detection logic.
- Projectile lifetime handling validation.
- Runtime integration with MonsterManager.

Excluded:

- ArcProjectile behavior.
- ChannelBeam behavior.
- PeriodicArea behavior.
- Damage calculation changes.
- Buff and Effect changes.
- Projectile VFX.
- Projectile SFX.
- Object Pool implementation.

---

## Current Problem

Current StraightProjectile behavior is based on a target-position snapshot.

Flow:

```text
Tower selects target
    ↓
Store target position
    ↓
Projectile flies toward stored position
    ↓
Projectile reaches stored position
    ↓
Check whether original target is still nearby
```

This can result in:

```text
Monster moves away
    ↓
Projectile reaches old location
    ↓
Projectile misses
    ↓
Projectile destroyed
```

This behavior does not match the current design direction.

---

## Desired Runtime Behavior

```text
Tower selects target
    ↓
Target determines launch direction
    ↓
Projectile launched
    ↓
Projectile flies independently
    ↓
Every frame:
    Check nearby monsters
    ↓
Hit nearest valid monster inside hit threshold
    ↓
Apply damage
    ↓
Generate impact event
    ↓
Destroy projectile
```

If no monster is hit:

```text
Projectile continues flying
    ↓
MaxLifetime reached
    ↓
Destroy projectile
```

---

## Implementation Requirements

### Projectile Flight

StraightProjectile should continue using:

```text
ProjectileSpeed
LaunchDirection
MaxLifetime
```

Projectile movement remains direction-based.

Projectile flight must not depend on target position after launch.

---

### Hit Detection

Hit detection belongs to ProjectileSystem.

Projectile should continuously check for valid monsters during flight.

Hit condition:

```text
Distance(projectile, monster)
<=
HitDistanceThreshold
```

If multiple monsters satisfy the condition:

```text
Hit nearest valid monster
```

---

### Monster Query Source

Do not use:

```csharp
FindObjectsByType<MonsterBehaviour>()
```

Use MonsterManager runtime ownership instead.

Preferred source:

```text
MonsterManager.GetAliveMonsters()
```

or equivalent runtime monster collection already maintained by MonsterManager.

ProjectileSystem should not perform scene-wide object searches every frame.

---

### Lifetime Handling

Existing MaxLifetime behavior should remain unchanged.

When:

```text
elapsedLifetime >= MaxLifetime
```

Projectile must:

```text
DestroyProjectile()
```

regardless of whether a hit occurred.

---

### Impact Flow

On hit:

```text
Projectile
    ↓
Create ProjectileImpactContext
    ↓
Dispatch Damage
    ↓
Trigger EffectConfig
    ↓
Destroy Projectile
```

Existing impact pipeline should remain unchanged.

---

## Files Expected To Change

Primary:

```text
Assets/Scripts/Projectile/ProjectileBehaviour.cs
```

Possible:

```text
Assets/Scripts/Monster/MonsterManager.cs
```

Only if a suitable runtime monster query API does not already exist.

---

## Verification

### Straight Projectile Hit

```text
Spawn Archer Tower
    ↓
Fire projectile
    ↓
Projectile passes near monster
    ↓
Monster receives damage
```

Expected:

```text
Projectile impacts successfully
```

---

### Hit Different Monster

Scenario:

```text
Target A selected
    ↓
Monster B enters projectile path
```

Expected:

```text
Projectile may hit Monster B
```

because hit detection is flight-based.

---

### No Hit

Scenario:

```text
Projectile never enters hit threshold
```

Expected:

```text
Projectile survives until MaxLifetime
    ↓
Projectile destroyed automatically
```

---

### Performance Validation

Expected:

```text
No FindObjectsByType
No scene-wide monster searches per frame
```

Projectile uses MonsterManager runtime collection.

---

## Completion Criteria

- StraightProjectile no longer depends on stored target position impact.
- Projectile uses direction-based independent flight.
- Projectile continuously performs hit detection during flight.
- Projectile can hit any valid monster encountered during flight.
- Projectile lifetime handling remains functional.
- MonsterManager runtime collection is used instead of scene-wide searches.
- Existing impact pipeline remains unchanged.
- Unity compiles without errors.