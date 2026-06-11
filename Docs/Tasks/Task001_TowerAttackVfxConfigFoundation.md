# Task001_TowerAttackVfxConfigFoundation

## Objective

Introduce attack VFX configuration support to the Tower Framework.

This task only establishes data definitions and Inspector configuration support.

No runtime VFX playback, spawning, attachment, update, or cleanup should be implemented in this task.

No VFX assets need to be created.

---

## Related Documents

- 07_TowerFrameworkSystem.md
- 08_TowerRuntimeCombatSystem.md

---

## Scope

### In Scope

Add attack VFX configuration fields to AttackConfig.

Support configuration for:

- Projectile Release VFX
- Channel Beam VFX
- Periodic Area VFX

Expose these fields in the Inspector.

Ensure fields are displayed only when relevant to the selected AttackArchetype whenever practical.

---

### Out of Scope

Do not implement:

- Runtime VFX playback
- Runtime VFX spawning
- Runtime VFX destruction
- Runtime VFX attachment
- Beam visual update logic
- Particle System tuning
- VFX prefab creation
- Projectile impact VFX logic
- Projectile travel VFX logic
- Object pooling

---

## Required AttackConfig Changes

### Add Fields

Add the following optional fields to AttackConfig.

```csharp
[SerializeField]
private GameObject projectileReleaseVfxPrefab;

[SerializeField]
private GameObject channelBeamVfxPrefab;

[SerializeField]
private GameObject periodicAreaVfxPrefab;