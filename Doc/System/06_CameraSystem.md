# Tower Nexus - Camera System

Document Set: System

---

# 1. Purpose And Ownership

Camera System owns player-controlled framing of the Active Map during battle.

It owns:

- Initial framing of each Active Map
- Eligibility and lifetime of one Camera pan gesture
- Direct-manipulation conversion from pointer movement to Camera translation
- Enforcement of the 3D movement boundary authored by the Active Map
- Gesture cancellation and framing reset when the Active Map or battle state changes

Stage System supplies one committed Active Map, its validated 3D movement boundary, and its authored default pose as one identity. Map System supplies the Map plane and authored 3D volume. Game Flow state and battle-local interaction owners determine whether Camera input is currently eligible.

Camera System does not own Map coordinates, UI interaction, Draft-item drag state, Tower placement, Monster behavior, combat timing, or Game Flow transitions.

---

# 2. Runtime Inputs And Outputs

Inputs:

- One Active Map
- The Map's authored default pose, NodesRoot plane, and 3D Camera movement boundary
- The active Camera view
- Whether the current Game Flow state permits battle-local Camera input
- Whether modal UI or another battlefield gesture already owns the active pointer

Output:

- A Camera position framed against the Active Map and constrained by its authored 3D movement boundary

Camera movement never translates the Map or changes gameplay coordinates.

---

# 3. Active Map Framing

Every Map owns one default Camera pose outside the moving Camera hierarchy. Every initial Stage, next Stage, and retry restores the newly instantiated Map's exact authored pose. The outgoing Stage's pan displacement is never inherited and is never recaptured as the new default.

The Map plane passes through NodesRoot and uses NodesRoot's authored up direction. Movement limits are independent from the rectangular gameplay footprint: each Map authors a separate boundary that may include deliberate surrounding presentation space.

When the Active Map is replaced or released:

- Any active Camera gesture ends without applying further movement.
- The previous Map boundary is unbound before its runtime instance is released.
- A newly supplied Active Map receives the authored default framing before battle-local Camera input becomes eligible.
- No Camera input is accepted while no valid Active Map exists.

---

# 4. Pan Interaction Contract

Camera pan is a press-and-drag gesture that begins only when one primary mouse pointer or one touch identity starts on an eligible battlefield region.

The first version permits Camera pan only during the Battle Game Flow state. Stage preparation, Stage Introduction, Victory, Defeat, and main-menu presentation do not permit Camera pan.

The gesture is rejected when:

- The current Game Flow state does not permit battle-local Camera input
- A modal Draft or Game Flow surface is active
- The pointer begins over an interactive UI surface
- A held Draft item or Tower placement operation owns the pointer
- Another Camera pan gesture is already active
- No active event-routing authority exists for checking the pressing pointer against UI

Gesture ownership is decided when the press begins and remains stable until release or cancellation. Crossing between battlefield and UI regions does not transfer an active gesture between Camera, UI, Draft-item drag, or Tower Placement owners.

The accepted mouse or touch identity owns the gesture through movement, release, or cancellation. Other mouse or touch inputs are ignored while it owns Camera Pan. UI rejection is evaluated for that exact pressing pointer identity.

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

If either pointer ray cannot produce a valid Map-plane intersection, no estimated translation is applied and the direct-manipulation baseline is rebuilt safely.

---

# 6. Camera Movement Bounds

After direct-manipulation translation is calculated, Camera System constrains its Camera reference position through the Active Map's authored 3D movement boundary.

Bounds behavior is:

- Each Map supplies exactly one supported 3D spatial volume.
- The volume describes the permitted Camera reference-position region; it does not redefine the gameplay footprint.
- The Camera viewport may reveal content outside the gameplay footprint when the authored boundary permits it.
- Clamping discards excess movement for the current sample; it does not accumulate hidden overscroll.
- Reversing drag direction responds immediately after a clamped sample.
- The authoritative rendered Camera position is reconciled back into the logical Camera position after boundary resolution, then the active pointer-to-Map baseline is rebuilt.
- The first version does not require elastic overscroll or spring-back behavior.

Camera bounds affect presentation only. They do not change Map dimensions, Grid coordinates, pathfinding, or placement queries.

---

# 7. System Boundaries

| System | Supplies To Camera System | Continues To Own |
|---|---|---|
| Game Flow System | Whether the current state permits battle-local Camera interaction | State transitions and modal Game Flow presentation |
| Stage System | Exact Active Map, boundary, and default-pose binding identity | Stage composition and release |
| Map System | NodesRoot plane, authored default pose, and authored 3D movement boundary | Grid state, spatial queries, and Map presentation |
| Battle HUD UI System | UI and modal interaction ownership | Draft presentation and held-item interaction |
| Tower Placement System | Whether a Draft-item placement gesture owns the pointer | Placement drag, preview, validation, and commit |

Camera System consumes availability and gesture-ownership facts without inspecting or mutating the owning system's internal state.

---

# 8. Validation

Camera validation should report or reject at minimum:

- Missing or unusable active Camera view
- Missing or unusable event-routing authority
- Missing Active Map while Camera interaction is enabled
- Invalid Map plane, gameplay footprint, or framing origin
- Missing, ambiguous, disabled, degenerate, or unsupported 3D Camera movement boundary
- A boundary retained from a replaced or released Map
- Pointer rays that cannot intersect the Active Map plane
- Camera pan accepted while modal UI or Tower Placement owns the gesture
- More than one active Camera pan gesture
- Stale framing or gesture state retained after Active Map replacement or release
- Runtime pan displacement inherited by an initial Stage, next Stage, or retry
- Tower Placement using a different output Camera

Validation does not move the Map, rewrite Map data, or silently bypass an active interaction owner.

---

# 9. Approved Scope And Deferred Topics

Current scope includes:

- One active battle Camera
- Centered framing for each Active Map
- Primary-pointer press-and-drag pan
- Direct Map-plane manipulation
- Active-Map-authored 3D Camera movement bounds
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
