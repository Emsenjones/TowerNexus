# Task002 - Monster Hit Feedback Foundation

## 1. Task Goal

Implement the first-version Monster Hit Feedback foundation.

When a monster receives damage, it should provide immediate visual feedback through:

1. Optional hit animation trigger
2. Configurable hit flash effect

The first-version hit flash effect should temporarily change the monster model color to a configured flash color, then restore the original color after a short duration.

This task only focuses on monster hit feedback presentation.

Do not implement monster health bars, floating damage numbers, sound effects, advanced shader effects, crowd control, buff reactions, or combat rule changes in this task.

---

## 2. Related System Documents

Relevant documents:

- `00_ProjectOverview.md`
- `04_MonsterSystem.md`
- `09_ProjectileSystem.md`
- `11_BuffAndEffectSystem.md`

Ownership reminder:

- Monster System owns monster hit feedback presentation.
- Projectile System only dispatches damage or impact information.
- Buff And Effect System may apply area damage, but it should not own monster hit visuals.
- Tower Runtime Combat System should not directly control monster hit feedback.

---

## 3. Current Context

MonsterBehaviour already owns monster health and damage processing.

Current expected damage flow:

```text
Tower / Projectile / Effect
    ↓
MonsterBehaviour.TakeDamage(...)
    ↓
Monster health changes
    ↓
Monster may die
```

Hit feedback should be triggered inside or immediately after `MonsterBehaviour.TakeDamage(...)`, as long as the monster is still allowed to show hit feedback.

The implementation should not alter damage values, projectile impact rules, area damage rules, pathfinding behavior, or reward flow.

---

## 4. Required Feature Behavior

### 4.1 Hit Feedback Trigger Timing

When a monster takes valid damage:

```text
Monster receives damage
    ↓
Current health is reduced
    ↓
If monster is not dead before feedback check
        ↓
        Play hit animation feedback if configured
        Play hit flash feedback if enabled
    ↓
If health reaches 0
        ↓
        Continue existing death flow
```

The exact order can be adjusted based on current code structure, but the death flow should remain stable.

If the monster is already dead or being destroyed, hit feedback should not play.

---

### 4.2 Hit Animation

Monster hit animation should be optional and configurable.

Recommended first-version Animator parameter:

```text
GetHit
```

Recommended field location:

```csharp
MonsterDefinition.hitAnimationTriggerName
```

Recommended type:

```csharp
string
```

Example:

```csharp
[SerializeField] private string hitAnimationTriggerName = "GetHit";
public string HitAnimationTriggerName => hitAnimationTriggerName;
```

Runtime behavior:

```text
Monster receives damage
    ↓
Animator.SetTrigger(hitAnimationTriggerName)
```

If the string is null or empty, skip hit animation.

Implementation should be defensive:

- Do not crash if Animator is missing.
- Do not crash if the trigger parameter is not assigned.
- Prefer warning or safe skip over hard failure.

Design note:

The first version may use a simple trigger on the existing Animator Controller.

Future versions may improve this through a separated hit layer, but this task does not need to implement Animator layer logic.

---

### 4.3 Hit Flash

Hit flash should temporarily change the monster model color, then restore the original color.

Recommended configurable fields in `MonsterDefinition`:

```csharp
[SerializeField] private bool enableHitFlash = true;
[SerializeField] private Color hitFlashColor = Color.red;
[SerializeField] private float hitFlashDuration = 0.08f;
```

Recommended public properties:

```csharp
public bool EnableHitFlash => enableHitFlash;
public Color HitFlashColor => hitFlashColor;
public float HitFlashDuration => hitFlashDuration;
```

Runtime behavior:

```text
Monster receives damage
    ↓
Change monster renderer material color to hitFlashColor
    ↓
Wait hitFlashDuration
    ↓
Restore original material color
```

The exact values should be configurable in Unity Inspector.

---

### 4.4 Renderer Collection

The hit flash effect should support monsters with multiple renderers.

Recommended serialized field on `MonsterBehaviour` or a dedicated component:

```csharp
[SerializeField] private Renderer[] hitFlashRenderers;
```

If the array is empty, the implementation may auto-collect renderers from child objects:

```csharp
GetComponentsInChildren<Renderer>()
```

Manual assignment should still be supported because some monster prefabs may contain renderers that should not flash.

---

### 4.5 Material Handling

The implementation should avoid permanently changing shared project materials.

Do not modify `sharedMaterial.color` directly for hit flash.

Recommended approach:

- Use runtime material instances through `renderer.material`
- Store original colors before flashing
- Restore original colors after flashing

First-version acceptable behavior:

```text
On initialization:
    Cache renderers
    Cache each renderer's original material color
On hit:
    Set runtime material color to hit flash color
After duration:
    Restore cached original color
```

If a material does not expose a `_Color` property, skip it safely.

Optional URP note:

For URP Lit materials, `_BaseColor` may be used instead of `_Color`.

Implementation may support both if convenient:

```text
If material has _BaseColor, use _BaseColor.
Else if material has _Color, use _Color.
Else skip material.
```

---

## 5. Recommended Implementation Structure

### 5.1 New Class: MonsterHitFeedback

Create a dedicated component:

```text
MonsterHitFeedback.cs
```

Recommended responsibilities:

- Store Animator reference
- Store renderer references
- Cache original material colors
- Play hit animation trigger
- Play hit flash coroutine
- Stop/cleanup feedback when monster dies or is disabled

Recommended serialized fields:

```csharp
[SerializeField] private Animator animator;
[SerializeField] private Renderer[] hitFlashRenderers;
```

Recommended public methods:

```csharp
public void Initialize(MonsterDefinition definition);
public void PlayHitFeedback();
public void StopFeedback();
```

This keeps `MonsterBehaviour` from becoming too fat.

MonsterBehaviour should call:

```csharp
hitFeedback?.PlayHitFeedback();
```

when valid damage is received.

---

### 5.2 MonsterBehaviour Integration

Recommended additions to `MonsterBehaviour`:

```csharp
[SerializeField] private MonsterHitFeedback hitFeedback;
```

On initialization:

```csharp
hitFeedback ??= GetComponentInChildren<MonsterHitFeedback>();
hitFeedback?.Initialize(monsterDefinition);
```

On valid damage:

```csharp
if (!IsDead())
{
    hitFeedback?.PlayHitFeedback();
}
```

On death or cleanup:

```csharp
hitFeedback?.StopFeedback();
```

Important:

- Do not trigger hit feedback after the monster has entered Dead state.
- Do not allow a pending hit flash coroutine to restore color after the object is already disabled or destroyed in an unsafe way.

---

### 5.3 MonsterDefinition Integration

Add hit feedback presentation fields to `MonsterDefinition`.

Recommended fields:

```csharp
[Header("Hit Feedback")]
[SerializeField] private string hitAnimationTriggerName = "GetHit";
[SerializeField] private bool enableHitFlash = true;
[SerializeField] private Color hitFlashColor = Color.red;
[SerializeField] private float hitFlashDuration = 0.08f;
```

Recommended properties:

```csharp
public string HitAnimationTriggerName => hitAnimationTriggerName;
public bool EnableHitFlash => enableHitFlash;
public Color HitFlashColor => hitFlashColor;
public float HitFlashDuration => hitFlashDuration;
```

If the current `MonsterDefinition` already has `hitAnimationName`, Codex should decide whether to:

1. Rename it to `hitAnimationTriggerName`, or
2. Keep the existing field and use it as the hit trigger name

Prefer minimal disruption.

If renaming causes serialization risk, keep the existing field and add a clear property name.

---

## 6. Constraints

Do not implement the following in this task:

- Monster health bar
- Floating damage numbers
- Hit sound effects
- Knockback
- Stun
- Slow
- Buff reaction visuals
- Death dissolve
- Boss special hit effects
- Object pooling
- Shader Graph custom effects
- Material animation curves
- Damage calculation changes

Do not change:

- Tower targeting
- Projectile movement
- Projectile hit detection
- Area damage resolution
- Monster pathfinding
- Monster wave spawning
- EXP reward logic
- Player HP damage logic

---

## 7. Expected Files

Likely new files:

```text
Assets/Scripts/Monster/MonsterHitFeedback.cs
```

Likely modified files:

```text
Assets/Scripts/Monster/MonsterDefinition.cs
Assets/Scripts/Monster/MonsterBehaviour.cs
```

Possible prefab setup:

```text
Monster prefab
    └── MonsterHitFeedback component
        ├── Animator reference
        └── Hit flash renderers
```

Exact paths may be adjusted to match the existing project structure.

---

## 8. Acceptance Criteria

The task is complete when:

1. When a living monster receives valid damage, hit feedback is triggered.
2. If a hit animation trigger name is configured, the monster Animator receives the trigger.
3. If hit flash is enabled, the monster briefly changes to the configured flash color.
4. The monster color restores after the configured duration.
5. Hit flash supports multiple renderers.
6. The implementation does not permanently modify shared materials.
7. Missing Animator, missing Renderer, or missing material color property does not crash the game.
8. Dead monsters do not continue playing hit feedback.
9. Hit feedback stops safely when the monster dies or is destroyed.
10. Tower combat, projectile logic, area damage, pathfinding, and reward flow remain unchanged.
11. The code compiles in Unity.
12. The feature can be tuned in Unity Inspector per monster type.

---

## 9. Manual Unity Setup Checklist

After Codex implementation, verify the following in Unity:

1. Open each Monster prefab.
2. Add or confirm `MonsterHitFeedback` component exists.
3. Assign Animator if not auto-detected.
4. Assign hit flash renderers manually if needed.
5. Confirm monster model materials can visually change color.
6. Add `GetHit` trigger parameter to monster Animator Controller if hit animation is used.
7. Configure hit feedback fields in each `MonsterDefinition`.
8. Enter Play Mode.
9. Damage a monster through tower attack or debug damage.
10. Confirm hit animation triggers if configured.
11. Confirm hit flash turns red and restores correctly.
12. Kill a monster and confirm no feedback errors occur during death cleanup.

---

## 10. Notes For Codex

Keep this task small and surgical.

Prefer a simple, stable, configurable implementation.

Do not refactor unrelated Monster System logic unless required for clean integration.

Do not implement Task001 Monster Health Bar in this task.

If Task001 has already been implemented, avoid changing its health bar code unless absolutely necessary.

The final result should make monster damage feel immediately readable without introducing new combat rules.
