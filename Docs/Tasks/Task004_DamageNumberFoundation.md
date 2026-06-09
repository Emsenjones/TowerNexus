

# Task004_DamageNumberFoundation

## Objective

Implement the first version of the Monster Damage Number System.

Reference:

```text
04_MonsterSystem.md
```

Relevant section:

```text
6. Monster Visual Feedback
6.4 Monster Damage Number System
```

MonsterSystem is the source of truth.

---

## Scope

Implement:

- DamageNumberManager
- DamageNumberUI
- DamageNumberTweenStep
- DOTween integration
- Preview functionality
- Monster damage flow integration
- Scripts required for manual DamageNumberUI prefab setup in Unity

---

## Out of Scope

Do not implement:

- Object Pool
- Critical Damage Number
- Healing Number
- Shield Number
- Damage Number Merge Rules
- Damage Number Stacking Rules
- Elemental Damage Styles
- Custom Tween Curves

---

## Runtime Flow

Expected runtime flow:

```text
Monster takes damage
→ DamageNumberManager.ShowDamage(...)
→ Create DamageNumberUI
→ Play tween step list
→ Destroy UI after completion
```

Detailed behavior should follow MonsterSystem.

---

## Prefab

Do not create the prefab in code.

The DamageNumberUI prefab will be created manually in Unity.

Codex should only implement the scripts and serialized fields required for manual prefab setup.

Detailed requirements should follow MonsterSystem.

---

## Acceptance Criteria

- Damage number appears when Monster takes damage.
- Position tween works.
- Scale tween works.
- Fade tween works.
- Preview button works.
- DOTween integration works.
- No compile errors.
- Unity enters Play Mode successfully.

---

Follow MonsterSystem as the source of truth.