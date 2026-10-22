# Exit Stage Confirmation local verification

Run from the repository root:

```sh
python3 Tests/ExitStageConfirmation/Presentation/run.py
python3 Tests/TowerInfoWindow/Presentation/run.py
python3 Tests/TowerInfoWindow/ModalPause/run.py
python3 Tests/TowerInfoWindow/Interaction/run.py
python3 Tests/Reroll/Task002/run.py
python3 Tests/TowerInfoWindow/ContentRefinement/build.py
```

The exit suite runs 138 assertions with the complete production
`ConfirmExitStageWindow`, `BattleHUDUI.ExitConfirmation`, pause authority, combat
binding, and inspection partial including the independent LateUpdate dispatcher.
It shares the existing inspection suite's explicit Unity/membership doubles and
production HUD OnDisable extraction. Assertions in the inspection suite are not
removed or weakened. Generated method boundaries remain identified in its runner.

Coverage includes hidden startup, author text, pause before activation, exact
rate restoration, three-way pause exclusion, local Draft/inspection/placement
admission, missing references, callback reentry, activation rollback, independent
frame checks and sibling cleanup failure, view/HUD disable and destruction, stale
callbacks, listener preservation, and failure containment.

Title and description text objects are independent authored fixture content;
the production view has no text bindings or text-reference validation. Tests
retain initial-copy and author-edited-copy preservation across opening/cleanup.

Request tests execute current-request validation in AwaitingTransition, immutable
source/action identity, accepted ownership handshake, button locking with a
blocking/visible mask, duplicate suppression, rejected and throwing requests,
revocation, delayed cleanup, synchronous fresh binding on the same HUD/view, and
accepted execution failure without restoring the outgoing session. Broken accepted
handlers are reported against the captured Battle rather than treated as ordinary
rejection.

Evidence recorded for Task004:

- Exit local suite: 138 assertions.
- Existing inspection presentation: 109 assertions, unchanged.
- Pause: 81 assertions; extracted spawning and attack boundaries: five cases each.
- Whole Camera integration: 128 assertions with input/physics/Cinemachine/HUD endpoints doubled.
- Whole DraftSystem: 556 Editor and 505 player assertions.
- Existing Submission/placement: 34 managed contracts.
- Complete runtime Editor/player compilation with installed Unity references.

Rendering, raycasting, native activation order, input dispatch, and real Stage
transition execution are not simulated faithfully by these doubles. No Game Flow
or production coordinator exit binding is implemented in Task004. Task005/Task006
and Unity/iPad layout, touch, full scene pause, and end-to-end Stage switching
acceptance remain pending. Compilation is not a native acceptance result.
