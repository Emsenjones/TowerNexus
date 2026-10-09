# TowerInfoWindow interaction integration checks

Run from the repository root:

```sh
python3 Tests/TowerInfoWindow/Interaction/run.py
python3 Tests/TowerInfoWindow/Interaction/assets.py
python3 Tests/TowerInfoWindow/Presentation/run.py
python3 Tests/Task004/run.py
python3 Tests/Task005/run.py
python3 Tests/Reroll/Task002/run.py
python3 Tests/TowerInfoWindow/ModalPause/run.py
python3 Tests/TowerInfoWindow/ContentRefinement/build.py
```

Input runs the complete production CameraPanController with explicit native Input,
UI raycast, Physics, Map/Camera/Cinemachine and HUD endpoints doubled. Its 102
assertions cover staged versus committed admission, failed Camera commit rollback,
press/release, threshold crossings, dragging back, UI rejection, same-target/runtime
identity, synchronous modal invalidation, touch duplication/additional touches,
focus loss, revocation, nearest deployed hit filtering and rendered-boundary reversal.

assets.py performs 25 static serialized-reference/layer/selection-proxy checks.
It does not prove native asset import, collision size, rendering or device behavior.

The production HUD presentation suite has 109 assertions, including protected
placement cleanup and failed-Opening invalidation. Production Submission has 33
contracts, including rejected direct/debug investment and post-preparation modal
revocation. Whole Draft has 556 Editor / 505 player assertions, including inspection
admission exclusion and synchronous Opening input invalidation. Existing Effects
has 27 checks; shared pause has 81 assertions plus five attack/five spawn cases.

Complete runtime compilation uses actual Unity references in both conditional
modes. Native mouse/touch, proxy coverage for all level models, UI blocking/Close,
Cinemachine bounds and complete Stage/Retry/terminal lifecycle remain pending.
