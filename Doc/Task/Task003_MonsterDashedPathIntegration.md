# Task003 - Monster Dashed Path Integration

Document Set: Task
Status: Integration code implemented; managed/static evidence recorded below.
Scene Prefab wiring and native end-to-end acceptance remain pending.
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

- Establish the formal route in Normal at Battle start before Monster spawning.
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
relocation changes, preview-authorized gameplay commit, animations beyond Task002's texture flow,
multiple Spawn/Target support, and unrelated architecture refactoring.

## 5. Ownership And Current Integration Points

Tower Placement System owns the state machine and selected/retained route.
Monster System supplies formal and candidate route data from Task001. Battle
HUD UI System renders the supplied state through Task002. Stage/Battle
coordination manages activation, stop, and release for that exact runtime.
It receives Task002's LineRenderer Prefab reference explicitly, creates one
world-space presenter instance for the current Battle under the Map
presentation root, and destroys that instance on release. The instance is not
parented to the Camera or screen-space Canvas. Camera pan does not trigger a
route query or geometry update. Material/Prefab authoring follows Task002's
functional code and remains a prerequisite for native visual acceptance.
Task002 uses scaled battle time for texture flow and does not acquire pause
ownership. Preserve its phase on ordinary route/state submissions; clear resets
it. Map spatial changes require resubmitting the retained route/state even for
the same snapshot; Camera pan alone does not.

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
| Enter Battle, before Initial Draft selection or Monster spawning | Normal; formal route |
| Begin Tower Draft drag in Pending area | Normal; formal route, seeded as the retained valid route |
| Complete otherwise valid footprint with a surviving route | Normal; candidate route becomes the retained valid route |
| Complete otherwise valid footprint blocks the route | Blocked; same geometry as immediately before the blocking result |
| Move between consecutive blocking footprints | Remain Blocked; do not update geometry |
| Move from blocked to a valid candidate | Normal; replace geometry with that candidate route |
| Pending area, outside Map, partial footprint out-of-bounds, overlap, or unavailable terrain | Normal; formal route |
| Existing-Tower level-up targeting, eligible or ineligible | Normal; formal route; occupancy unchanged |
| Successful deployment | End drag; Normal with the committed formal route |
| Failed release, release in Pending area, or other cancellation | End drag; Normal with unchanged formal route; held reward preserved where rejected/cancelled |
| Tower Upgrade Draft targeting | Does not begin Tower Draft route preview; formal route remains Normal |
| Draft Window opens/closes or pauses simulation | Preserve route/state and freeze flow. Block map movement/submission; release/cancel during modal is finalized as cancellation on close. A held drag resumes; modal input remains exclusive |
| Battle end, stop/release, rollback, or outgoing Map replacement | Remove line and discard outgoing route/retained state |
| Retry or next Stage | Establish fresh state for the new Battle/Map; never inherit red state, retained geometry, or flow phase |

White preview feedback does not establish deployability. Existing Tower-local
feedback and final submission validation remain authoritative. Only a confirmed
route-blocking result requests Blocked; an unavailable query or technical binding
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
6. Technical query/display failure hides and diagnoses without red classification.
   Keep the valid retained route for same-topology recovery: a later blocked
   result re-submits that geometry before tinting. Binding/topology revision
   changes discard it and establish a new formal baseline. Deduplicate repeated
   failure reasons.
7. Keep only one active line and one Tower Draft route-preview owner. Subscribe
   and release observers at the existing owner boundary; do not add global
   route state or general event infrastructure.
8. Rendering and interaction continue to respect the existing modal Draft
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
  immediately, and Blocked never persists after drag completion.
- All non-route invalid cases and both eligible/ineligible level-up targets
  display the formal route in Normal with appropriate independent Tower feedback.
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

Integration logic is implemented following the reviewed plan. The feature is
accepted only after the end-to-end evidence is complete;
finishing Task001 or Task002 alone is not completion of the player experience.


### Reviewed Implementation Boundaries

- `MonsterDashedPathSession` owns one immutable Map/path/validator/presenter
  binding and independent drag identities. It does not adopt a replacement
  binding. Formal route cache uses snapshot revisions plus validator revision.
- `BattleRuntimeCoordinator` creates the Prefab at Battle begin before Initial
  Draft/spawner activation. `RevokeCombatAuthority` closes the path session before
  cleanup/evidence callbacks, including deferred Release requests. Stop hides;
  release destroys the root and instance. Cleanup cannot resurrect the session.
- Placement uses one rich query for Tower and path feedback; explicit Pending
  area and level-up targets use formal route in Normal. Upgrade Draft does not begin
  route preview. Controller Update still services topology/spatial invalidation
  during modal, but suppresses input/completion and defers modal release cleanup.
- Submission's Operation captures its originating path session. After occupancy
  and movement revision commit, snapshot capture is separately guarded before
  combat activation or external observers; post-commit display publication is
  also guarded. Both failures preserve existing committed notifications. The
  authoritative plan supplies route nodes; no second post-commit A* is required.
- Map space is refreshed by comparing NodesRoot's matrix without modifying
  shared `hasChanged`. Technical hiding preserves valid same-topology route
  data and throttles identical diagnostics; blocked recovery explicitly restores
  geometry. Topology or validator/path revision invalidates that retained data.

### Evidence And Remaining Acceptance

- 18 production Session/Presenter managed cases passed with query/spatial/native
  renderer doubles; 53 Presenter managed cases passed.
- 31 actual Submission managed cases passed, including display capture and
  publication faults, origin-session replacement, extracted production modal
  input/completion, and earliest authority revocation.
- 29 route-query cases and 32,192 baseline/current campaign observations passed
  with identical ordered results. Runtime assembly compilation passed with zero
  warnings/errors, including the new source through a temporary compile import
  when absent from Unity's generated project. None substitutes for native Play Mode.
- Changed-file whitespace checks passed.
- Setup and evidence limits:
  [Integration README](../../Tests/MonsterDashedPath/Integration/README.md).
- Required manual scene reference: `BattleRuntimeCoordinator.monsterDashedPathPrefab`
  -> authored `Prefab_DashedLinePath` GameObject asset with a Presenter on its root.
  Missing root Presenter is explicitly diagnosed during instance creation. Scene YAML, Inspector
  wiring, Unity import, Play Mode and target-build/device checks remain user-owned.
