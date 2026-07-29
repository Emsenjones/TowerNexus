# Tower Nexus - Camera System

---

# 1. Purpose And Ownership

Camera System owns player-controlled framing of the Active Map during battle.

It owns:

- Initial framing of each Active Map
- Eligibility and lifetime of one Camera pan gesture
- Direct-manipulation conversion from pointer movement to Camera translation
- Enforcement of the movement boundary authored by the Active Map
- Gesture cancellation and framing reset when the Active Map or battle state changes

Stage System supplies Active Map availability together with its validated movement boundary. Map System supplies the framing origin, Map plane, gameplay footprint, and authored boundary. Game Flow state and battle-local interaction owners determine whether Camera input is currently eligible.

Camera System does not own Map coordinates, UI interaction, Draft-item drag state, Tower placement, Monster behavior, combat timing, or Game Flow transitions.

---

# 2. Runtime Inputs And Outputs

Inputs:

- One Active Map
- The Map framing origin, local gameplay plane, gameplay footprint, and authored Camera movement boundary
- The active Camera view
- Whether the current Game Flow state permits battle-local Camera input
- Whether modal UI or another battlefield gesture already owns the active pointer

Output:

- A Camera position framed against the Active Map and constrained by its authored movement boundary

Camera movement never translates the Map or changes gameplay coordinates.

---

# 3. Active Map Framing

When a new Active Map becomes available, Camera System frames it from the Map Root, which is the authored center of the complete grid.

The complete rectangular gameplay footprint extends one half Node Size beyond the outermost Grid Node centers on each grid axis and may inform the initial view. Movement limits do not have to match that footprint: each Map authors a separate boundary that may include deliberate surrounding presentation space.

Camera System retains one authored default Camera pose or equivalent default framing offset independently from runtime pan state. Every initial Stage, next Stage, and retry restores that default against the newly committed Active Map. The outgoing Stage's pan displacement is never inherited and is never recaptured as the new default.

When the Active Map is replaced or released:

- Any active Camera gesture ends without applying further movement.
- The previous Map boundary is unbound before its runtime instance is released.
- A newly supplied Active Map receives the authored default framing before battle-local Camera input becomes eligible.
- No Camera input is accepted while no valid Active Map exists.

---

# 4. Pan Interaction Contract

Camera pan is a press-and-drag gesture that begins only when the primary pointer starts on an eligible battlefield region.

The first version permits Camera pan only during the Battle Game Flow state. Stage preparation, Stage Introduction, Victory, Defeat, and main-menu presentation do not permit Camera pan.

The gesture is rejected when:

- The current Game Flow state does not permit battle-local Camera input
- A modal Draft or Game Flow surface is active
- The pointer begins over an interactive UI surface
- A held Draft item or Tower placement operation owns the pointer
- Another Camera pan gesture is already active

Gesture ownership is decided when the press begins and remains stable until release or cancellation. Crossing between battlefield and UI regions does not transfer an active gesture between Camera, UI, Draft-item drag, or Tower Placement owners.

The first version supports one primary pointer gesture. Multi-pointer gestures are deferred.

---

# 5. Direct-Manipulation Movement

Camera movement preserves the subjective relationship between pointer motion and Map motion: the Map point under the pointer should remain under that pointer while the view can move freely.

For adjacent pointer samples:

```text
Previous Map Point
    = Previous Pointer Ray Intersected With The Active Map Plane

Current Map Point
    = Current Pointer Ray Intersected With The Active Map Plane

Requested Camera Translation
    = Previous Map Point - Current Map Point
```

The translation lies on the Active Map plane. Dragging the pointer in one screen direction therefore moves the Camera in the opposite world direction and makes battlefield content follow the pointer.

This mapping is derived from the Camera view and Map plane. It does not depend on a fixed world-units-per-pixel multiplier, display resolution, or an authored pan-speed scalar.

If either pointer ray cannot produce a valid Map-plane intersection, that movement sample is ignored rather than applying an estimated translation.

---

# 6. Camera Movement Bounds

After direct-manipulation translation is calculated, Camera System constrains its Camera reference position through the Active Map's authored movement boundary.

Bounds behavior is:

- Each Map supplies exactly one supported planar boundary or spatial volume.
- The boundary describes the permitted Camera movement region; it does not redefine the gameplay footprint.
- The Camera viewport may reveal content outside the gameplay footprint when the authored boundary permits it.
- Clamping discards excess movement for the current sample; it does not accumulate hidden overscroll.
- Reversing drag direction responds immediately after a clamped sample.
- The first version does not require elastic overscroll or spring-back behavior.

Camera bounds affect presentation only. They do not change Map dimensions, Grid coordinates, pathfinding, or placement queries.

---

# 7. System Boundaries

| System | Supplies To Camera System | Continues To Own |
|---|---|---|
| Game Flow System | Whether the current state permits battle-local Camera interaction | State transitions and modal Game Flow presentation |
| Stage System | Active Map availability, replacement, and validated boundary handoff | Stage composition and release |
| Map System | Framing origin, gameplay plane, gameplay footprint, and authored movement boundary | Grid state, spatial queries, and Map presentation |
| Battle HUD UI System | UI and modal interaction ownership | Draft presentation and held-item interaction |
| Tower Placement System | Whether a Draft-item placement gesture owns the pointer | Placement drag, preview, validation, and commit |

Camera System consumes availability and gesture-ownership facts without inspecting or mutating the owning system's internal state.

---

# 8. Validation

Camera validation should report or reject at minimum:

- Missing or unusable active Camera view
- Missing Active Map while Camera interaction is enabled
- Invalid Map plane, gameplay footprint, or framing origin
- Missing, ambiguous, disabled, degenerate, or unsupported Camera movement boundary
- A boundary retained from a replaced or released Map
- Pointer rays that cannot intersect the Active Map plane
- Camera pan accepted while modal UI or Tower Placement owns the gesture
- More than one active Camera pan gesture
- Stale framing or gesture state retained after Active Map replacement or release
- Runtime pan displacement inherited by an initial Stage, next Stage, or retry

Validation does not move the Map, rewrite Map data, or silently bypass an active interaction owner.

---

# 9. Approved Scope And Deferred Topics

Current scope includes:

- One active battle Camera
- Centered framing for each Active Map
- Primary-pointer press-and-drag pan
- Direct Map-plane manipulation
- Active-Map-authored Camera movement bounds
- Input exclusion for modal UI and Draft-item placement gestures
- Gesture cleanup and authored-default reframing across initial Stage, next Stage, and retry

Deferred topics include:

- Pinch or wheel zoom
- Camera rotation
- Inertial movement
- Edge scrolling
- Camera follow targets
- Cinematic paths
- Camera shake
- Minimap-driven navigation
- Multiple simultaneous Camera views
