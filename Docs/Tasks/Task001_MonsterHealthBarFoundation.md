# Task001 - Monster Health Bar Foundation

## 1. Task Goal

Implement the first-version Monster Health Bar foundation.

After a monster is instantiated into the battlefield, the system should automatically create a corresponding health bar UI item and keep that health bar visually linked to the monster's runtime position.

The health bar should follow the monster with a configurable world-space offset.

This task only focuses on monster health bar creation, binding, update, and cleanup.

Do not implement monster hit feedback, hit animation, hit flash, damage numbers, or advanced UI polish in this task.

---

## 2. Related System Documents

Relevant documents:

- `00_ProjectOverview.md`
- `04_MonsterSystem.md`
- `01_PlayerSystem.md`
- `02_BattleHUDUISystem.md`

Ownership reminder:

- Monster System owns individual monster health bar presentation.
- Battle HUD UI System owns global battle UI such as player HP, EXP, draft window, and pending tower UI.
- Monster health bars should not be treated as Battle HUD UI state.

---

## 3. Current Context

Monster System already owns:

- Monster spawning
- Monster runtime behavior
- Monster movement
- Monster health and damage processing
- Monster death flow
- Monster manager registration
- Monster target arrival handling

The new health bar feature should be added as a lightweight runtime presentation layer on top of the existing Monster System.

The implementation should not change tower combat behavior, projectile behavior, damage calculation rules, or pathfinding logic.

---

## 4. Required Feature Behavior

### 4.1 Health Bar Creation

When a monster is instantiated and initialized:

```text
Monster instantiated
    ↓
MonsterBehaviour initialized with MonsterDefinition
    ↓
Create one MonsterHealthBarUI item
    ↓
Bind the health bar to this MonsterBehaviour
    ↓
Health bar starts following the monster
```

Each alive monster should have one corresponding health bar UI item.

The health bar should be created automatically by runtime logic.

The designer should not need to manually place health bars in the scene for each monster.

---

### 4.2 Health Bar Position Binding

The health bar should follow the monster's runtime transform.

The position should be calculated from:

```text
monster transform position + configurable health bar offset
```

The offset must be configurable, because the correct value depends on the monster model size and visual scale in Unity.

Recommended field location:

```csharp
MonsterDefinition.healthBarOffset
```

Recommended type:

```csharp
Vector3
```

Example:

```csharp
[SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);
```

The exact default value can be adjusted if needed.

---

### 4.3 Health Value Update

The health bar should reflect the monster's current health percentage.

When the monster takes damage:

```text
MonsterBehaviour.TakeDamage(...)
    ↓
Monster current health changes
    ↓
Health bar updates fill amount
```

Recommended display logic:

```text
fillAmount = currentHealth / maxHealth
```

The value should be clamped between 0 and 1.

---

### 4.4 Health Bar Cleanup

When a monster dies or is removed after reaching the target:

```text
Monster removed from battlefield
    ↓
Destroy or recycle its health bar UI item
```

First version may use `Destroy`.

Object pooling is intentionally postponed.

The cleanup logic should prevent orphaned health bars from remaining in the scene after the monster has been destroyed.

---

## 5. Recommended Implementation Structure

### 5.1 New Class: MonsterHealthBarUI

Create a new runtime UI component:

```text
MonsterHealthBarUI.cs
```

Recommended responsibilities:

- Store a reference to the bound `MonsterBehaviour`
- Store health bar offset
- Update UI position every frame
- Update fill amount when monster health changes
- Destroy or detach itself when the bound monster becomes invalid

Recommended serialized fields:

```csharp
[SerializeField] private Image fillImage;
[SerializeField] private Vector3 worldOffset;
[SerializeField] private Camera targetCamera;
```

Possible public methods:

```csharp
public void Initialize(MonsterBehaviour monster, Vector3 offset, Camera camera);
public void Refresh(float currentHealth, float maxHealth);
public void Dispose();
```

Implementation note:

- If the health bar is implemented under a Screen Space Canvas, convert world position to screen position.
- If the health bar is implemented under a World Space Canvas, use world position directly.
- Prefer a simple and reliable implementation for the first version.

Recommended first-version approach:

```text
Screen Space Overlay Canvas + WorldToScreenPoint
```

This is easier to debug and avoids each monster needing its own world-space canvas.

---

### 5.2 New Class: MonsterHealthBarManager

Create a manager class if needed:

```text
MonsterHealthBarManager.cs
```

Recommended responsibilities:

- Hold the health bar prefab reference
- Hold the health bar parent canvas/container reference
- Create health bar UI items
- Bind health bars to monsters

Possible serialized fields:

```csharp
[SerializeField] private MonsterHealthBarUI healthBarPrefab;
[SerializeField] private RectTransform healthBarContainer;
[SerializeField] private Camera worldCamera;
```

Possible public method:

```csharp
public MonsterHealthBarUI CreateHealthBar(MonsterBehaviour monster, Vector3 offset);
```

The manager should be assigned in the scene through Inspector.

Alternative acceptable approach:

- If the project already has a suitable runtime UI manager, Codex may integrate health bar creation there.
- Keep ownership clear: monster individual health bars still belong to Monster System presentation.

---

### 5.3 MonsterBehaviour Integration

MonsterBehaviour should integrate with the health bar system.

Recommended additions:

```csharp
private MonsterHealthBarUI healthBarUI;
```

On initialization:

```csharp
healthBarUI = MonsterHealthBarManager.Instance.CreateHealthBar(this, definition.HealthBarOffset);
healthBarUI.Refresh(currentHealth, definition.MaxHealth);
```

On damage:

```csharp
healthBarUI?.Refresh(currentHealth, definition.MaxHealth);
```

On death or removal:

```csharp
healthBarUI?.Dispose();
healthBarUI = null;
```

If the current project avoids singleton managers, use the project's existing dependency style instead.

Do not introduce unnecessary global state if there is already a cleaner reference path.

---

### 5.4 MonsterDefinition Integration

Add a configurable health bar offset field to `MonsterDefinition`.

Recommended field:

```csharp
[SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);
public Vector3 HealthBarOffset => healthBarOffset;
```

This value should be editable per monster type.

---

## 6. Constraints

Do not implement the following in this task:

- Hit animation
- Hit flash
- Floating damage numbers
- Health bar animation smoothing
- Health bar color stages
- Boss health bar
- Elite monster special UI
- Object pooling
- UI style polish beyond basic functionality

Do not change:

- Tower attack logic
- Projectile hit logic
- Damage calculation rules
- Pathfinding rules
- Monster spawn wave logic unless required for clean initialization

---

## 7. Expected Files

Likely new files:

```text
Assets/Scripts/Monster/MonsterHealthBarUI.cs
Assets/Scripts/Monster/MonsterHealthBarManager.cs
```

Likely modified files:

```text
Assets/Scripts/Monster/MonsterDefinition.cs
Assets/Scripts/Monster/MonsterBehaviour.cs
```

Possible prefab/scene setup:

```text
MonsterHealthBarUI prefab
Runtime UI Canvas / HealthBarContainer
MonsterHealthBarManager scene object
```

Exact paths may be adjusted to match the existing project structure.

---

## 8. Acceptance Criteria

The task is complete when:

1. When monsters spawn, each monster automatically gets one health bar UI item.
2. The health bar follows the monster position during movement.
3. The health bar position uses a configurable offset from `MonsterDefinition`.
4. When the monster takes damage, the health bar fill amount updates correctly.
5. When the monster dies, its health bar is removed.
6. When the monster reaches the target and is removed, its health bar is removed.
7. No orphaned health bars remain after monsters are destroyed.
8. The implementation does not affect tower attack, projectile, pathfinding, or reward logic.
9. The code compiles in Unity.
10. The feature is simple enough to be tuned in Unity Inspector.

---

## 9. Manual Unity Setup Checklist

After Codex implementation, verify the following in Unity:

1. Create or assign a `MonsterHealthBarUI` prefab.
2. Add an `Image` component as the fill image.
3. Assign `fillImage` in `MonsterHealthBarUI`.
4. Create or assign a runtime UI Canvas.
5. Create a `HealthBarContainer` under the Canvas.
6. Add `MonsterHealthBarManager` to a scene object.
7. Assign health bar prefab, container, and world camera.
8. Set `healthBarOffset` in each `MonsterDefinition`.
9. Enter Play Mode and spawn monsters.
10. Confirm health bars follow monsters and update after damage.

---

## 10. Notes For Codex

Keep this task small and surgical.

Prefer a stable first-version implementation over a fancy UI system.

If there are multiple possible UI implementation approaches, choose the one that requires the least change to existing combat and monster logic.

Do not refactor unrelated systems.

Do not implement Task002 Monster Hit Feedback in this task.
