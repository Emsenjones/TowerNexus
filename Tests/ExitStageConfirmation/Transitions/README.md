# Exit Stage Game Flow transition verification

Run from the repository root:

```sh
python3 Tests/ExitStageConfirmation/Transitions/run.py
python3 Tests/ExitStageConfirmation/Presentation/run.py
python3 Tests/TowerInfoWindow/ContentRefinement/build.py
```

The transition suite passes 156 assertions against whole production Game Flow,
Stage composition, HUD exit view/session, Battle binding, and modal pause. Exact
production coordinator acceptance, terminal claim, lifecycle finalization and
release methods are extracted without editing their bodies. Other coordinator
preparation/start and gameplay dependencies are explicit doubles.

Tests start at a real HUD button submission, not a fabricated request accepted
outside AwaitingTransition. Test-only prepare/cleanup callbacks provide the
production wiring reserved for Task006. They exercise successful and rejected
handoffs, same/nonzero Stage restart, Introduction, configuration transport,
synchronous replacement on the same HUD/view/coordinator, accepted preparation
failure, exact old-source rejection, reentrant Start/exit/terminal reports, deferred
release finalization, release-time preparation rejection, and protected menu events.

Failure tests cover state-observer exceptions, disable in cleanup, deferred cleanup
while disabled followed by re-enable, one bounded recovery attempt, and persistent
retirement failure that blocks input without retry loops or unsafe menu publication.
New-Battle synchronous startup-result buffering and existing Defeat Retry, Victory
Next Stage/final Stage, and result-only public admission are also covered.

`IsRuntimeRetired` means logical ownership release and inactive/detached Maps, not
waiting for Unity's frame-end native Destroy. Full native hierarchy activation,
raycasting, physics, Camera, gameplay resets, Initial Draft, and device behavior are
not represented by the doubles. Runtime compilation uses installed Unity references;
it is separate from Unity Play Mode or iPad acceptance. Production bind/unbind,
Prefab wiring, and end-to-end gameplay verification remain Task006.
