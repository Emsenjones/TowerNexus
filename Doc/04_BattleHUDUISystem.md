# Tower Nexus - Battle HUD UI System

---

# 1. Purpose And Ownership

Battle HUD UI System owns battle-local presentation and player interaction surfaces.

It presents:

- Player level and level progress
- Current player health
- Draft choices
- Selected but unconsumed Draft items
- Drag, placement, and Tower-target feedback
- Battle notifications approved by future designs

It observes gameplay state and forwards player intent. It does not own Player state, Draft generation, placement validation, Tower Upgrade rules, Map topology, Monster runtime, combat results, or Game Flow transitions.

---

# 2. Battle UI Composition

One authored Battle UI layer may group the battle HUD, Monster status presentation, and damage-number presentation.

The layer is a composition boundary, not a runtime owner or gameplay service locator. Gameplay systems communicate only with the presentation capability they require.

The Draft Window remains an authored part of the battle UI while closed. Opening a Draft creates transient choice items; closing it removes only those transient items and returns the window to its closed state.

Monster status displays and damage numbers remain owned by Monster System even when rendered on the same UI surface.

Main menu, Stage Introduction, Stage Victory, and Stage Defeat presentation belong to Game Flow System. Sharing one visual canvas or screen with battle-local UI does not make those surfaces part of Battle HUD UI System.

---

# 3. Player Runtime Display

The first-version HUD displays:

| Information | Source |
|---|---|
| Current Player Level | Player System |
| Progress Toward Next Level | Player System |
| Current Health | Player System |

Presentation updates when the owning gameplay state changes. The HUD must not derive level progression, calculate damage, or decide defeat.

---

# 4. Draft Window

Draft System supplies one active set of choices. Battle HUD UI System presents that set and returns one player selection.

```text
Draft Choices Supplied
    -> Open Draft Window
    -> Present Distinct Choices
    -> Player Selects One Choice
    -> Return Selection Intent
    -> Close Draft Window
```

The UI cannot create, replace, reroll, weight, or validate Draft candidates unless a future Draft rule explicitly grants that action.

---

# 5. Draft Item Interaction Area

The Draft Item Interaction Area displays selected rewards that have not yet been consumed.

It supports:

- Tower Draft items
- Tower Upgrade Draft items
- Drag interaction entry
- Return-to-area drag cancellation
- Removal after successful consumption

Unconsumed Tower Upgrade Draft items represent pending upgrade capacity and are readable by Draft System during later candidate generation.

Releasing any currently dragged Draft item back inside the interaction area cancels the current drag operation. Cancellation:

- Returns the item to the held-item flow
- Does not invoke placement validation
- Does not invoke Tower level-up or upgrade validation
- Does not consume the item

This rule applies to all present and future draggable Draft item types.

---

# 6. World Interaction Feedback

Battle HUD UI System presents feedback requested by gameplay owners without deciding validity.

## 6.1 Placement Feedback

The UI may distinguish:

- Valid placement
- Invalid placement
- Route-blocking rejection
- Cancelled placement

Tower Placement System owns the result.

## 6.2 Tower Target Feedback

While a Tower-related Draft item is dragged, the UI may present eligible, ineligible, or neutral Tower targets.

Tower Upgrade System owns TowerFamily, level, duplicate, layer-capacity, and maximum-level eligibility. Tower visual presentation owns Tower-local highlighting when that feedback is rendered on the Tower.

---

# 7. Interaction Results

The HUD reports intent or presentation completion; it does not report gameplay success before the owning system accepts the action.

```text
Drag Tower Draft
    -> Placement Or Existing-Tower Intent
    -> Gameplay Validation
    -> Accepted: Consume Item
    -> Rejected Or Cancelled: Keep Item
```

```text
Drag Tower Upgrade Draft
    -> Existing-Tower Intent
    -> Upgrade Validation
    -> Accepted: Consume Item
    -> Rejected Or Cancelled: Keep Item
```

---

# 8. Validation

Battle UI authoring validation should report at minimum:

- Missing player information presentation
- Missing Draft Window or Draft choice container
- Missing Draft Item Interaction Area
- Missing interaction feedback references required by current content
- Missing Monster status or damage-number presentation required by the authored composition

Validation must not create gameplay state or silently replace authored UI.

---

# 9. Approved Scope And Deferred Topics

Current scope includes player information, Draft presentation, held Draft items, drag cancellation, placement feedback, and Tower target feedback.

Game Flow System owns battle-result and Stage-transition presentation, including distinct Victory and Defeat interactions. Those surfaces are outside Battle HUD UI System rather than deferred Battle HUD features.

Deferred Battle HUD topics include:

- Wave and boss warnings
- Pause flow
- Minimap
- Player skills
- Multiplayer status
- General notification feed

Future UI must preserve the same presentation-versus-gameplay ownership boundary.
