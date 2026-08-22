# Task006 - Drone Burst Elemental Opportunity Refactor

Status: Planned

Depends on: Completed Task005 Elemental Stack Contribution Damage Authority

Blocks: Task007-Task016

## 1. Goal

Make one Drone Burst the normal Elemental contribution boundary. A Burst may
release and damage through multiple Projectiles, but only its opening Projectile
may carry ordinary Elemental application eligibility.

This replaces hidden stack-rate control through a long Buff cooldown with an
explicit Drone attack-topology contract. It does not change direct projectile
damage, Burst ammunition, Burst timing, target selection, or Buff stacking rules.

## 2. Burst Opener Contract

Each Drone instance owns its own Burst state. When a new Burst begins:

1. the opening shot slot is the only slot allowed to create an
   Elemental-eligible Projectile;
2. successful Projectile creation freezes that eligibility onto the released
   Projectile;
3. later Projectiles in the same Burst are Elemental-ineligible;
4. a missed, technically cleaned-up, or otherwise unresolved eligible Projectile
   does not transfer eligibility to a later shot;
5. the next eligibility is created only when the same Drone begins a genuinely
   new Burst after its remaining shot count is exhausted and its Inter-Burst
   Cooldown completes.

Eligibility is resolved at the actual hit boundary. The eligible Direction
Projectile applies Elemental state to the Monster it actually hits, which need
not be the intended target snapshot used to establish launch direction.

## 3. Retargeting Invariant

Ordinary Drone retargeting preserves Burst phase, remaining shot count, and
timer. It never reloads a Burst, bypasses Inter-Burst Cooldown, or grants a new
Elemental contribution.

If a target becomes invalid during Between Shots, the remaining Projectiles may
be released toward a replacement target but remain in the same Burst. If the
opening Projectile already consumed or lost the Burst's eligibility, no
replacement target can receive a newly invented opener opportunity.

## 4. Behaviour Composition

### 4.1 Blast Rounds

Blast Rounds inherits the eligibility of the Projectile that produced its
impact:

- an ineligible later Projectile grants no direct or Blast Rounds Elemental
  application opportunity;
- an eligible opening Projectile may apply Elemental state to its surviving
  direct-hit Monster;
- every valid Monster resolved by that eligible Projectile's Blast Rounds area
  result receives its own Elemental application opportunity;
- when the same Monster appears in both direct and area results, ordinary
  source-scoped Buff cooldown authority handles the repeated request;
- Blast Rounds damage and resulting Buff/reaction damage never recursively
  create further Elemental opportunities.

### 4.2 Multiple Drones

Primary and additional Drone entities own independent Burst state. Each active
Drone may therefore release one Elemental-eligible opener per own Burst. They
remain Attack Entities of the same Tower instance, so their successful
applications still share that Tower's per-Monster, per-Buff source cooldown
entry.

### 4.3 Final Dive

Final Dive remains a separate reviewed completion boundary. Its optional direct
target and valid explosion targets retain their explicit Elemental opportunities
and do not consume or recreate an ordinary Burst opener.

## 5. Element-Neutral Rule

The Burst opener rule applies uniformly to Burning, Chilled, Electrified, and
Windcut. Drone runtime must not branch on ElementType or inspect Buff-specific
StackApplied, Overload, or Protection behavior.

The source Tower's currently applied Elemental Upgrade remains the content
authority when an eligible hit executes. Projectile eligibility only answers
whether that hit may submit the application request.

## 6. Diagnostics

Recorder evidence must make the reviewed boundary auditable. The accepted
diagnostic shape records at least:

- Drone instance identity and owning Tower instance identity;
- Bursts started;
- eligible opening Projectiles successfully released;
- eligible opening Projectile direct hits;
- Elemental opportunities from eligible Blast Rounds resolved targets;
- ordinary later-Projectile direct hits without Elemental eligibility;
- Final Dive Elemental opportunities separately;
- total Buff application attempts and their results.

Task006 bumps Recorder schema `15` to `16`. Schema `16` adds the Burst identity,
opener eligibility, direct-hit opportunity, and Blast-target opportunity fields
required by this contract. The schema bump and runtime change ship in the same
checkpoint.

## 7. Required Regressions

- one naked Elemental Drone completes at least two Bursts against a surviving
  target; application attempts match resolved opener opportunities rather than
  total projectile hits;
- a target dies during Between Shots and retargeting preserves the Burst without
  granting another opportunity;
- an opener miss does not transfer eligibility;
- Blast Rounds from an opener grants opportunities to every valid resolved area
  target, while Blast Rounds from later shots grants none;
- primary plus additional Drones each own one opportunity per own Burst without
  becoming separate Buff cooldown sources;
- Final Dive behavior remains independent;
- all four ElementTypes follow the same boundary;
- resolution, damage, Buff, Tower deployment, and diagnostic integrity pass.

Static acceptance additionally requires runtime and Editor builds and
path-scoped `git diff --check`.

## 8. Out Of Scope

- changing Burst Count, Burst Interval, Burst Cooldown, battery duration, or
  Drone capacity;
- final Elemental cooldown or StackApplied DamageScale balance;
- per-target Burst reloads or Elemental eligibility refresh on retarget;
- per-Tower BuffDefinition copies;
- changing Archer, Cannon, or Magic Elemental opportunities;
- Task007 final Elemental calibration.
