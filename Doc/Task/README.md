# Tower Nexus Active Task Series

Active Series: `CombatMathV2`

This directory contains the active Task001-Task014 execution sequence for the
Tower Level and damage-formula refactor followed by a complete combat and Stage
recalibration.

The previous Task series is preserved unchanged in:

`Doc/Task/Archive/CombatMathV1_2026-08-21/`

Archived documents and their `Doc/GamePlayRecord` JSON files remain historical
evidence. They do not own current CombatMathV2 values. In particular, the prior
Stage1 five-Level-1-Archer clear is retained as the motivating horizontal-growth
failure case.

## Active Order

| Task | Responsibility |
|---|---|
| Task001 | Combat damage formula and Tower Level BasicDamage refactor |
| Task002 | Level 1 Base Tower baseline |
| Task003 | Tower Level BasicDamage curve |
| Task004 | Non-Elemental Upgrade baseline |
| Task005 | Elemental and Buff baseline |
| Task006 | Fixed-speed Monster Health roster |
| Task007 | Campaign progression and controlled calibration fixtures |
| Task008 | Stage1 Wave calibration |
| Task009 | Stage2 Wave calibration |
| Task010 | Stage3 Wave calibration |
| Task011 | Stage4 Wave calibration |
| Task012 | Stage5 Wave calibration |
| Task013 | Stage6 Wave calibration |
| Task014 | Fast-Monster identity and reviewed Wave substitutions |

Task order is an acceptance dependency, not only a filename order. A later Task
must not compensate for a failed earlier contract through unrelated Stage values.

