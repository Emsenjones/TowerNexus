# Tower state UI managed checks

Run from the repository root:

```sh
python3 Tests/TowerStateUI/run.py
```

Requires the existing `csc` and `mono` tools. Builds into a temporary directory.

The first executable compiles the production TowerStateUIItem/Manager scripts
against boundary doubles for Unity, TMP, Tower state, and deployed membership.
Fourteen cases cover initial/update display, duplicate and invalid notifications,
stopped-runtime retention, hidden reconciliation, release while hidden, stale
captured callbacks, clear during instantiation, delayed/external destruction,
destroyed-object equality, projection/visibility decisions, missing configuration,
and cleanup of static/event subscriptions.

The second executable extracts the current production Coordinator begin, UI-bind,
safe-cleanup, release-core, and stop methods without changing their bodies.
Five cases inject bind/cleanup failures and check Initial Draft continuation,
binding order, Stop retention, and UI clear before Tower destruction. Other
gameplay collaborators are doubles; this is not an end-to-end Unity battle test.

Projection uses an intentionally scaled/translated conversion double to detect
direct screen-pixel assignment. It does not prove CanvasScaler behavior, rendering,
native destruction order, Cinemachine timing, or Prefab authoring. Those require
the Unity acceptance checklist in Doc/Task/Task005_TowerStateUI.md.

Relevant existing gameplay regression: `python3 Tests/Task004/run.py`.
The numbered Tests directories predate the current UI Task documents.
