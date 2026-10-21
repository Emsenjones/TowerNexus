# Input System migration

The project uses Input System only (`activeInputHandler: 1`). UI pointer events
identify the device/contact for placement; Camera polls EnhancedTouch and balances
Enable/Disable ownership. No Input Actions changes are required. The package's
default Dynamic Update mode is used. ProgressAddController and its meta were
removed; no serialized references existed in Assets at migration time.

Run:

```sh
python3 Tests/TowerInfoWindow/Interaction/run.py
python3 Tests/Task004/run.py
python3 Tests/TowerInfoWindow/ContentRefinement/build.py
```

InputDoubles models the device boundary for managed gesture tests, not native
Input System event buffering. The build uses actual installed Unity references.

Manual Unity acceptance after restarting the editor:

- Verify Active Input Handling is New, no legacy-input startup warning or missing scripts.
- Mouse: drag camera past threshold, reverse at boundary, tap Tower to inspect;
  UI blocks camera and inspection, dragging never becomes a tap.
- Place, level up and upgrade via Held cards; preview and dashed route follow the
  pointer, release submits once, invalid/UI release preserves the card, right click cancels.
- Touch device: repeat camera and placement, add a second finger during each
  gesture, release it first; the original finger must retain ownership.
- Open a modal during placement, release while paused, close: no delayed placement.
- Lose focus/background the app or disconnect the pointing device mid-gesture:
  no unintended submission and the next fresh gesture works.
- Change Stage, end/restart battle and disable/re-enable runtime: no stale gesture.
- Space no longer adds debug progress.

Existing unrelated workspace edits are outside this change.
