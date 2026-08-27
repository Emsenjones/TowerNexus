# Task010 - Stage1 Wave Calibration

Status: Planned

Depends on: Accepted Task007 Elemental And Buff Baseline; accepted Map and Draft runtimes

Blocks: Task011, Task016, and Task017

## 1. Goal

Calibrate Stage1 as the first controlled proof that concentrated Tower growth is
required while one undeveloped Support coverage point remains useful.

## 2. Fixed Design Intent

- Archer-only Stage pool;
- five-Draft candidate budget;
- Reference: one L2 Archer Core with one Basic and one Behaviour Upgrade plus
  one L1 Archer Support;
- horizontal Anti-pattern: spend every Tower Draft opportunity on undeveloped
  L1 Archers;
- concentration Anti-pattern: omit the required second coverage point.

## 3. Stage Composition And Fixed Build Fixtures

- Derive exactly five Drafts from two Tower placements, one Core Level Up, and
  two Core Upgrade applications.
- Author the exact Archer Tower Draft Pool and every currently unlocked Archer
  Basic and Behaviour UpgradeDefinition required by the cumulative Stage policy.
- Prove continuous Archer Level 2 reachability with at least one newly eligible
  Upgrade at the required level.
- Select and record the exact Reference Upgrade pair, one coherent alternative,
  the horizontal Anti-pattern, the missing-coverage Anti-pattern, and their legal
  placement assumptions.
- Author one five-step Fixed Draft sequence for each required Build: one Tower-
  only Initial step plus four Player level-up steps. Every configured choice must
  be naturally eligible at that step and must follow normal Pending, placement,
  Level Up, Upgrade, and consumption rules.

Fixed displayed choices prove Build constructibility only. They are not natural
offer-frequency evidence and do not determine whether a player preferred a
different Build; Task017 owns those questions.

## 4. Stage Calibration And Monster Authoring

1. Author four positive Player Progress Requirements. Their sum is the resolved-
   Monster node that opens the final Draft.
2. Choose a total Monster budget greater than that sum so the completed Build
   receives a meaningful post-final-Draft pressure window.
3. Begin with existing Monster runtime templates only as candidates. Reuse a
   candidate when its HP can express the required opening, body, or late-pressure
   role. When none can, create the smallest new Stage-needed Profile instead of
   pre-authoring a complete campaign roster.
4. Every Profile accepted in this Task uses `MoveSpeed = 0.25`; every standard
   Wave authors `SpawnInterval = 2.5s`, preserving the `0.625` spatial gap.
5. Author and record the exact `MonsterWaveConfig`: ordered Wave index,
   `WaveDelay`, Monster runtime template, Count, and `SpawnInterval`.
6. Calibrate the Progress sequence, Profile HP, Wave order and Count,
   `WaveDelay`, Player Health, and Reference/alternative placement together
   until the intended Build envelope is reproducible.

A Monster Profile first accepted here becomes reusable downstream. Later Tasks
must not silently change its HP or MoveSpeed; changing it reopens this Task and
every accepted Stage that references it.

## 5. Required Runs

- at least two integrity-valid Reference runs when the margin is near a leak;
- one coherent alternative;
- horizontal Anti-pattern;
- concentration Anti-pattern;
- placement sanity check at the accepted Reference positions.

All Build-comparison runs use Fixed Draft sequences. Natural offer frequency and
the reasons a player may finish with another Build are outside this Task and
belong to Task017.

## 6. Pressure Acceptance

- opening Fodder pressure lets the Initial Tower earn early progress;
- the player can form the Core/Support structure before high pressure;
- later Waves require developed damage rather than additional undeveloped
  Towers alone;
- Reference is stable with zero or only explicitly accepted minimal leak;
- coherent alternatives may clear without matching one exact Upgrade pair;
- final Player Health permits only the reviewed leak margin;
- Progress Requirements open all five Drafts at the intended pressure nodes and
  leave reviewed post-final-Draft combat;
- the exact Stage pools, Required-Level reachability, and every required Fixed
  Build sequence are accepted with the Stage rather than by an upstream fixture;
- every used Monster Profile is identified as reused or first accepted here,
  with its HP and fixed-speed values recorded;
- the accepted Wave table exactly matches the authored `MonsterWaveConfig`;
- per-Wave Recorder evidence explains every leak and pressure step.

The archived five-L1-Archer no-damage clear is the primary failure regression.
