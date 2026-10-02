# Task003 - Monster Dashed Path Integration

Document Set: Task
Status: Planned; implementation and runtime acceptance have not begun.
Dependencies: [Task001 - Monster Path Route Queries](Task001_MonsterPathRouteQueries.md) and [Task002 - Monster Dashed Path Presentation](Task002_MonsterDashedPathPresentation.md).
Next: End-to-end acceptance of Monster dashed-line path.

## 1. Goal

Connect the approved main-route query and renderer to Tower Draft dragging,
final deployment results, and the exact Battle/Active Map lifetime. Deliver the
complete player experience without changing placement acceptance, Draft
ownership, Monster movement rules, or existing Camera/input authority.

## 2. Source Documents

- [Tower Placement System](../System/09_TowerPlacementSystem.md), Sections 7.5 and 8: complete state transitions and cleanup.
- [Stage System](../System/02_StageSystem.md), Section 3.1: establishment, Battle end, retry, and replacement.
- [Battle HUD UI System](../System/04_BattleHUDUISystem.md), Section 6.1: rendering and modal/input behavior.
- [Monster System](../System/07_MonsterSystem.md), Sections 6.2 and 6.3: authoritative main route and Monster-specific movement.
- [Map System](../System/05_MapSystem.md), Section 10.1: candidate invalidation.
- [Project Overview](../System/00_ProjectOverview.md): cross-system ownership summary.

## 3. Scope

- Establish the formal route in Solid at Battle start before Monster spawning.
- Connect Tower Draft begin, snapped movement, candidate query results, fallback
  intent, release, cancellation, and deployment outcome to the display state.
- Keep the current drag's last displayed valid route for blocked feedback.
- Refresh formal route presentation after committed topology changes.
- Bind all route, retained-state, and presentation requests to the originating
  Battle/Active Map; clear them at the appropriate lifecycle boundary.
- Complete the real scene/asset setup and end-to-end acceptance matrix.

## 4. Out Of Scope

Tower Upgrade Draft route preview, changes to same-family level-up eligibility,
Wave start or timing changes, new pause ownership, Monster lane/connector/
relocation changes, preview-authorized gameplay commit, route animations,
multiple Spawn/Target support, and unrelated architecture refactoring.

## 5. Ownership And Current Integration Points

Tower Placement System owns the state machine and selected/retained route.
Monster System supplies formal and candidate route data from Task001. Battle
HUD UI System renders the supplied state through Task002. Stage/Battle
coordination manages activation, stop, and release for that exact runtime.

Inspect these existing integration points when planning the implementation:

- `Assets/Scripts/TowerDeployment/TowerPlacementController.cs`: Tower Draft drag, existing-Tower intent, release/cancel, and Map binding.
- `Assets/Scripts/TowerDeployment/TowerPlacementSubmission.cs`: final gameplay acceptance and committed route publication.
- `Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs`: Battle begin, gate closure, and release.
- `Assets/Scripts/Stage/StageCompositionController.cs`: prepared Stage and Map replacement.
- `Assets/Scripts/TowerDeployment/BattleHUDUI.cs`: HUD presentation and Pending interaction integration.

Use existing commit and lifecycle boundaries. Prefer the already committed
route result when updating successful-deployment presentation; do not install
candidate data as gameplay authority or create a second placement transaction.
The final affected-file plan must use verified paths and settle any narrow
presentation binding before coding.

## 6. State And Route Contract

| Event Or Condition | Required State And Route |
|---|---|
| Preparing or Stage Introduction | No line |
| Enter Battle, before Initial Draft selection or Monster spawning | Solid; formal route |
| Begin Tower Draft drag in Pending area | Valid Preview; formal route, seeded as the retained valid route |
| Complete otherwise valid footprint with a surviving route | Valid Preview; candidate route becomes the retained valid route |
| Complete otherwise valid footprint blocks the route | Blocked Preview; same geometry as immediately before the blocking result |
| Move between consecutive blocking footprints | Remain Blocked Preview; do not update geometry |
| Move from blocked to a valid candidate | Valid Preview; replace geometry with that candidate route |
| Pending area, outside Map, partial footprint out-of-bounds, overlap, or unavailable terrain | Valid Preview; formal route |
| Existing-Tower level-up targeting, eligible or ineligible | Valid Preview; formal route; occupancy unchanged |
| Successful deployment | End drag; Solid with the committed formal route |
| Failed release, release in Pending area, or other cancellation | End drag; Solid with unchanged formal route; held reward preserved where rejected/cancelled |
| Tower Upgrade Draft targeting | Does not begin Tower Draft route preview; formal route remains Solid |
| Draft Window opens/closes or pauses simulation | Window action itself preserves the underlying route/state; modal input remains exclusive |
| Battle end, stop/release, rollback, or outgoing Map replacement | Remove line and discard outgoing route/retained state |
| Retry or next Stage | Establish fresh state for the new Battle/Map; never inherit red state or retained geometry |

White preview feedback does not establish deployability. Existing Tower-local
feedback and final submission validation remain authoritative. Only a confirmed
route-blocking result causes red; an unavailable query or technical binding
failure cannot masquerade as route blocking.

Each fallback displays the formal route and makes it the most recently
shown valid-preview route. A later blocking candidate retains that shape.
Retained state belongs only to the current drag and topology. If committed
Map topology changes during a drag, discard the old-topology candidate and
retained data, establish the new formal baseline, and re-evaluate the current
intent before presenting a current candidate result.

## 7. Lifecycle And Gameplay Invariants

1. No preview query or renderer update changes occupancy, Tiles, Monster paths,
   Draft rewards, Tower gameplay state, or Wave authorization.
2. Final placement remains a fresh authoritative preflight and commit. Under
   unchanged topology and footprint, the accepted formal route matches the
   last valid candidate preview.
3. Accepted Level Up/Upgrade keeps the same occupancy and formal main route.
   A visual failure cannot undo deployment, reward consumption, or upgrade.
4. Cleanup covers success, rejection, explicit cancel, return to Pending,
   presentation teardown, Battle terminal paths, release, and replacement.
   Repeated cleanup cannot restore stale state or damage a newer binding.
5. Revoke outgoing display/drag request authority before cleanup can trigger
   external callbacks. An old callback cannot adopt the latest Battle or Map.
6. Keep only one active line and one Tower Draft route-preview owner. Subscribe
   and release observers at the existing owner boundary; do not add global
   route state or general event infrastructure.
7. Rendering and interaction continue to respect the existing modal Draft
   input and pause contracts. No independent pause token is introduced.

## 8. Unity Authoring Checklist

- Verify explicit binding among the current placement owner, route provider,
  dashed-line presenter, and Stage/Battle lifetime owner.
- Apply Task002's reviewed assets and presentation parameters to the authored
  battle composition; list required references and validate their availability.
- Check representative campaign Maps, multi-cell Towers, and existing-Tower
  level-up targets; do not alter content balance to manufacture acceptance.
- Confirm no duplicate presenter is created on repeated begins or retries.
- Confirm Draft overlay, Tower drag, range previews, and Camera pan retain their
  intended ordering and input ownership.

Inspector wiring, Unity import/reserialization, and Play Mode remain user-owned
unless explicitly delegated. Record any required manual setup and unexecuted
native checks explicitly.

## 9. Acceptance Criteria

- Every row in Section 6 works in the real battle interaction.
- Starting a drag and immediately blocking uses the initial formal geometry.
- Repeated blocking changes only color/state; returning to validity updates
  immediately, and red never persists after drag completion.
- All non-route invalid cases and both eligible/ineligible level-up targets
  display the formal white preview with appropriate independent Tower feedback.
- Success replaces the formal line only after accepted gameplay commit;
  rejection/cancellation does not consume the held reward or alter topology.
- Initial Draft and first-Wave timing remain unchanged; the line does not wait
  for a Monster to spawn.
- Stop, Victory, Defeat, Technical Failure, Retry, and next Stage produce no
  outgoing geometry, red state, stale requests, or duplicate presentation.
- Live Monsters preserve the existing lane and route-revision contracts; the
  line is the shared main route rather than a per-Monster trajectory.

## 10. Validation And Evidence

- Compile the runtime assembly using Task001's established static gate; compile
  the Editor assembly if Editor code changes.
- Exercise the state machine and lifecycle guards with focused executable
  fixtures, including valid-to-blocked-to-blocked-to-valid, fallback-to-blocked,
  drag end, topology invalidation, and delayed outgoing requests after rebinding.
- Run relevant query and submission regression harnesses after inspecting their
  scope. Existing `Tests/Task002` and `Tests/Task004` names refer to earlier
  architecture tasks, not this feature's document numbering.
- Perform native Play Mode acceptance for Section 6 and the acceptance criteria,
  including visibility, modal behavior, input, deployment, and Stage transitions.
- Record geometry retention and preview/committed-route equality as evidence;
  screenshots alone do not prove mutation-free queries or lifecycle authority.
- Run `git diff --check` on changed files.
- Report static compilation, executable fixtures, Editor/import, Play Mode,
  and any target-build/device checks separately. Unexecuted checks stay pending.

## 11. Review And Status

Planned contract only. Review the concrete integration and lifecycle plan before
coding. The feature is accepted only after the end-to-end evidence is complete;
finishing Task001 or Task002 alone is not completion of the player experience.
