# TowerInfoWindow presentation verification

Run from the repository root:

```sh
python3 Tests/TowerInfoWindow/Presentation/run.py
python3 Tests/TowerInfoWindow/ModalPause/run.py
python3 Tests/Reroll/Task002/run.py
python3 Tests/Reroll/Task002/ui.py
python3 Tests/Reroll/Task002/real_cards.py
python3 Tests/Reroll/Task002/build.py
python3 Tests/Task005/run.py
python3 Tests/Task004/run.py
```

Presentation runs 74 assertions against the whole production TowerInfoWindow,
BattleHUDUI inspection partial, TowerInspectionSnapshot, BattleCombatBinding and
BattleModalPauseAuthority. Exact production Combat query and level/Upgrade baseline
commit methods and HUD OnDisable execute against explicit external boundaries.
Membership, native graphics, component creation, hierarchy activation and destruction
are doubles. No assertion claims to verify Unity rendering/physics or device input.

Tests verify snapshot freshness after same-target mutation, inactive-root opening,
ordered latest data/icons, missing-icon policy, generated-only cleanup, stale callbacks,
outer-operation Bind rejection, Clear/Cancel semantics, modal pause identity,
disable/throwing hide and HUD cleanup exception isolation. The read-only query is
checked to reject unavailable/invalid data without reporting Battle failure.

Manual assembly and native acceptance are listed in Task002 Sections 9/10.
Task003 connects the real coordinator dependencies and player tap entry; this suite
supplies explicit test bindings instead of adding a production debug opening path.
