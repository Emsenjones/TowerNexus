# TowerInfoWindow content refinement verification

Run from the repository root:

```sh
python3 Tests/TowerInfoWindow/ContentRefinement/run.py
python3 Tests/TowerInfoWindow/Presentation/run.py
python3 Tests/TowerInfoWindow/ContentRefinement/build.py
python3 Tests/Task005/run.py
python3 Tests/Task004/run.py
python3 Tests/TowerInfoWindow/ModalPause/run.py
```

ContentRefinement runs 33 assertions. It compiles exact production Tower
initialization/counter, Monster health/death/reset/transaction and Buff lifecycle
execution methods, whole TowerKillSource, Battle binding, Effect context, Buff
request/instance and pending Overload classes, and the real tower-owned damage
transaction. Native feedback, Buff mutation plumbing, membership, and Effect
damage execution are explicit doubles. It checks last-blow attribution, nested
death, identity reuse, source loss, deferred Overload, A/B reaction versus lifecycle
ownership, source-less damage, arrival/cleanup, reentrant reset, callback failures,
committed damage evidence, paired transaction cleanup and counter saturation.

The Presentation suite separately runs 105 assertions with real View/Item/Snapshot/
HUD/pause code. build.py compiles the complete runtime in player and UNITY_EDITOR
modes with the installed Unity/TMP references and firstpass assembly. It does not
modify Unity's generated project files or invoke native Play Mode/a player build.

These checks do not establish Unity hierarchy/Animator/physics, actual released
entity contacts, Buff timing, visual layout or device input acceptance. Record
native checks and manual reference migration separately in Task002-1; production
tap input and complete Battle lifecycle are Task003 responsibilities.
