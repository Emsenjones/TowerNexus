# Battle modal pause verification

Run from the repository root:

```sh
python3 Tests/TowerInfoWindow/ModalPause/run.py
python3 Tests/Reroll/Task002/run.py
python3 Tests/Reroll/Task002/ui.py
python3 Tests/Reroll/Task002/real_cards.py
python3 Tests/Reroll/Task002/build.py
python3 Tests/Task004/run.py
python3 Tests/Task003/run.py
python3 Tests/TowerStateUI/run.py
python3 Tests/MonsterDashedPath/Integration/run.py
```

`run.py` executes the complete production pause authority against an injectable
rate boundary: 81 assertions cover capture/restoration, exclusivity, invalid rates,
revocation, cleanup ownership, and fresh/outgoing identity isolation. It additionally
extracts the exact production Tower attack and zero-delay spawning methods for five
cases each, with session/native creation boundaries doubled. These cases exercise
one-shot release continuation, presentation fallback, Stop, new attack identity,
same-frame pause from spawn callbacks, and normal completion versus cancellation.
They do not instantiate Unity MonoBehaviours or emulate Animator/physics timing.

The Re-roll harness executes the whole production DraftSystem with the actual
pause authority and extracted production failure-routing methods. UI preparation
and terminal coordination are controlled boundaries. Added cases revoke authority
from Pending View preparation and inject Opening/live presentation loss and throwing
close. Existing sampling, reward, RNG, Re-roll, and Recorder assertions remain.
DraftUI and real-card suites execute production view/root-lifetime code against
explicit managed activation doubles, including child-root disable and normal
close suppression.

Build overlays include new sources without editing Unity-generated project files.
Native Play Mode/device acceptance remains pending: verify the authored root/HUD
hierarchy, same-frame attack events and contacts, complete scene freeze, resume,
UI responsiveness, non-default rate restoration, and retry/Stage cleanup.
