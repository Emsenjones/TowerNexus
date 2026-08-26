# Tower Nexus Active Task Series

Active Series: `CombatMathV2`

This directory contains the active Task001-Task016 execution sequence for the
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
| Task005 | Elemental StackApplied contribution damage authority |
| Task006 | Drone Burst Elemental opportunity boundary |
| Task007 | Absolute Elemental package baseline and matching-source cooperation |
| Task007A | Configurable Elemental stack contribution implementation checkpoint |
| Task007B | Primary-only Elemental opportunity boundary implementation checkpoint |
| Task007C | Shared Electric/Wind Tower-hit reaction implementation checkpoint |
| Task008 | Fixed-speed Monster Health roster |
| Task009 | Campaign progression and controlled calibration fixtures |
| Task010 | Stage1 Wave calibration |
| Task011 | Stage2 Wave calibration |
| Task012 | Stage3 Wave calibration |
| Task013 | Stage4 Wave calibration |
| Task014 | Stage5 Wave calibration |
| Task015 | Stage6 Wave calibration |
| Task016 | Fast-Monster identity and reviewed Wave substitutions |

Task order is an acceptance dependency, not only a filename order. A later Task
must not compensate for a failed earlier contract through unrelated Stage values.

Task007A, Task007B, and Task007C are implementation checkpoints created while
Task007 exposed stack-contribution, application-topology, and shared-hit-
reaction boundaries. Task007C ordinary schema-20 gameplay smoke is accepted for
Task007 continuation; its remaining edge-transaction fixtures are explicitly
deferred rather than reported as passed. Task007 now owns the final numerical
acceptance before Stage calibration begins.
