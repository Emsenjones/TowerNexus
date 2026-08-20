# Task005 - Monster Roster Baseline

Status: Complete; HP120 Normal, HP240 Tough, and HP480 Elite accepted at shared Move Speed `0.25` on 2026-08-17

Depends on: Task002 Base Combat baseline; Task003 and Task004 for regression coverage

## 1. Goal

Create the smallest useful Monster roster for Stage calibration while preserving one stable Reference Monster.

This Task uses maximum health as the sole first-pass differentiation axis. Movement-speed identity and homogeneous-Wave composition are deferred to Task007 so their effects on route exposure, projectile interception, Slow, Buff continuity, and Wave formation can be measured independently.

The ten available Monster Prefabs are an authoring pool, not a requirement to create ten numerical roles. Task005 accepts only the smallest set whose members produce distinct survival and tactical decisions. Unselected Prefabs may remain unused until a later Stage establishes a concrete role requirement.

## 2. Source Documents

- `Doc/Balance/00_StageDesignBlueprint.md`
- `Doc/Balance/01_TowerGrowthAndUpgradeIdentity.md`
- `Doc/System/07_MonsterSystem.md`
- `Doc/System/14_EffectSystem.md`
- `Doc/System/15_BuffSystem.md`

## 3. In Scope

- Reference Monster preservation
- Monster maximum-health tiers
- Acceptance of one shared first-pass movement speed
- Fragile, Normal, Tough, or Elite role candidates
- Tower TTK and attack-count breakpoints
- Route traversal time
- Elemental and Behaviour regression against each accepted Monster role

## 4. Out Of Scope

- MonsterWaveConfig
- Stage-specific Monster counts
- Monster movement-speed role differentiation and homogeneous-Wave authoring; owned by Task007
- New armor, resistance, or broad crowd-control frameworks
- Player Progress Requirements
- Final Stage difficulty

## 5. Calibration Sequence

1. Preserve the Task002 Reference Monster.
2. Define target survival time for each proposed Monster role.
3. Estimate health from effective reference damage and target survival time.
4. Check discrete shot, contact, and burst breakpoints.
5. Keep movement speed common for the first pass.
6. Hand any movement-speed identity proposal to Task007.
7. Regress Base, Behaviour, and Elemental interactions.

The higher-health Monsters used to prevent measurement ceilings in Task003 or Task004 are calibration fixtures, not automatically accepted roster roles. A fixture becomes roster content only when this Task gives it a named tactical purpose and accepts it through the full role comparison.

### 5.1 Accepted Three-Role Baseline

| Monster Role | Runtime Prefab | Max Health | Move Speed | Tactical Meaning | State |
|---|---|---:|---:|---|---|
| Reference / Normal | Bat | `120` | `0.25` | Frozen Task002 comparison unit and ordinary Wave body | Accepted |
| Tough | Dragon | `240` | `0.25` | Survives approximately twice the Reference direct-hit budget and extends Behaviour or Elemental exposure | Accepted |
| Elite | Golem | `480` | `0.25` | Durable late-Stage target that preserves specialization and Overload opportunities | Accepted |

The first pass deliberately omits a Fragile role. Stage-local Count and Wave Delay remain available to create early quantity pressure without adding another global health tier, while standard campaign Spawn Interval follows Task007's accepted spatial-gap contract. A Fragile role is added only if later Stage calibration demonstrates a tactical need that Wave timing cannot express.

The Bat, Dragon, and Golem rows above remain the historical HP-only controls used by Task005. Task007 supersedes their campaign Profile-to-Prefab mapping without reinterpreting this evidence.

These candidates use simple integer breakpoints against frozen Level 1 direct damage:

| Max Health | Archer `20` | Cannon `60` | Magic `30` | Drone `10` |
|---:|---:|---:|---:|---:|
| `120` | `6` hits | `2` hits | `4` hits | `12` hits |
| `240` | `12` hits | `4` hits | `8` hits | `24` hits |
| `480` | `24` hits | `8` hits | `16` hits | `48` hits |

These are arithmetic damage breakpoints, not accepted runtime TTK. Cadence, misses, contact opportunities, Burst structure, route exposure, and overkill remain Play Mode evidence.

## 6. Required Measurements

- Maximum health
- Movement speed
- Route traversal time
- TTK by representative Tower states
- Number of hits or bursts to resolve
- Effect of slow, lock, and Elemental state
- Whether the variant creates a distinct tactical decision

### 6.1 Phase A - Level 1 Base-Tower Screening

Use the existing Task004 straight diagnostic route and fixed Position 1. Each run contains exactly one Monster and one Level 1 Tower with no Upgrade. Keep Player Health, Map, Tower placement, and all non-roster authoring unchanged.

| Fixture Field | Fixed Value |
|---|---:|
| Route | Straight diagnostic route |
| Tower Position | Position 1 |
| Monster Count | `1` |
| Wave Delay | `4s` |
| Spawn Interval | `2.5s` (structurally retained; irrelevant for one Monster) |
| Monster Move Speed | `0.25` |
| Tower Level | `1` |
| Tower Upgrades | None |

Schema-v8 JSON records the authoritative single-Monster survival value at:

```text
monsterRuntime.instances[0].lifetimeSeconds
```

When `resolutionType = Killed`, this value is the observed TTK. When `resolutionType = Leaked`, it is the observed route traversal time and must not be mislabeled as TTK. For an exactly one-Monster run, `timing.battleDurationSeconds` independently measures the same first-Spawn-to-final-Resolution interval and should agree within frame-level timing precision. Do not subtract `spawningCompletedAtSeconds`; that field is already an offset from the first observed Spawn and is not part of Monster lifetime.

Run the matrix grouped by Monster Prefab so only the Tower changes between adjacent runs:

| Order | Monster | Tower | Recorder Run Name |
|---:|---|---|---|
| 1 | Bat `HP120` | Archer Base | `Roster_HP120_Bat_P1_Archer_Base` |
| 2 | Bat `HP120` | Cannon Base | `Roster_HP120_Bat_P1_Cannon_Base` |
| 3 | Bat `HP120` | Magic Base | `Roster_HP120_Bat_P1_Magic_Base` |
| 4 | Bat `HP120` | Drone Base | `Roster_HP120_Bat_P1_Drone_Base` |
| 5 | Dragon `HP240` | Archer Base | `Roster_HP240_Dragon_P1_Archer_Base` |
| 6 | Dragon `HP240` | Cannon Base | `Roster_HP240_Dragon_P1_Cannon_Base` |
| 7 | Dragon `HP240` | Magic Base | `Roster_HP240_Dragon_P1_Magic_Base` |
| 8 | Dragon `HP240` | Drone Base | `Roster_HP240_Dragon_P1_Drone_Base` |
| 9 | Golem `HP480` | Archer Base | `Roster_HP480_Golem_P1_Archer_Base` |
| 10 | Golem `HP480` | Cannon Base | `Roster_HP480_Golem_P1_Cannon_Base` |
| 11 | Golem `HP480` | Magic Base | `Roster_HP480_Golem_P1_Magic_Base` |
| 12 | Golem `HP480` | Drone Base | `Roster_HP480_Golem_P1_Drone_Base` |

Phase A determines whether the proposed tiers create readable survival separation under the four frozen base attack identities. It does not accept Behaviour or Elemental compatibility by itself. After reviewing these twelve reports, Phase B selects the smallest representative Core and Elemental regressions required for the surviving candidates.

#### Phase A Accepted Results

All twelve accepted schema-v8 reports used one Monster, Straight Route, Position 1, shared Move Speed `0.25`, and a Level 1 Tower without Upgrades. Every report completed spawning and reported all six integrity flags as `True`.

| Monster | Archer Base | Cannon Base | Magic Base | Drone Base | Role Reading |
|---|---|---|---|---|---|
| Bat `HP120` | Killed `16.448s`, `6` applications | Killed `15.976s`, `2` applications | Leaked with `90` HP, `34.250s` | Killed `11.359s`, `12` applications | Normal body resolved by three direct-output identities |
| Dragon `HP240` | Killed `22.627s`, `12` applications | Killed `20.232s`, `4` applications | Leaked with `210` HP, `34.232s` | Killed `19.232s`, `24` applications | Tough body preserves longer combat exposure without becoming an Elite wall |
| Golem `HP480` | Leaked with `220` HP, `34.232s` | Leaked with `300` HP, `34.237s` | Leaked with `420` HP, `34.252s` | Leaked with `70` HP, `34.245s` | Elite body survives every single Base Tower and preserves substantial interaction time |

The stable `34.23s-34.25s` leaked lifetimes confirm that all three candidates used the same route and Move Speed. The observed role separation therefore comes from Maximum Health rather than movement drift. Magic's low Straight-Route contact is a known Tower identity and does not invalidate the roster comparison.

### 6.2 Phase B - Representative Core And Elemental Regression

Phase B used the smallest representative regression rather than repeating Task003 and Task004's complete TowerFamily and Element matrices. Each run used three same-type Monsters on Straight Route at Position 1 with Spawn Interval `2.5s` and Move Speed `0.25`.

The frozen comparison builds were:

| Build | Tower State |
|---|---|
| Control | Archer Level 2; Quick Draw + Scatter Arrow |
| Fire | Archer Level 3; Control + Blazing Arrows |
| Cold | Archer Level 3; Control + Frostbound Arrows |

Accepted results:

| Monster | Control | Fire | Cold | Role Decision |
|---|---|---|---|---|
| Bat `HP120` | `3` killed, `0` leaked, ED `360`, average killed lifetime `16.704s` | `3` killed, ED `360`, average `15.604s` | `3` killed, ED `360`, average `17.312s` | Remains Normal under Core and Elemental investment |
| Dragon `HP240` | `1` killed, `2` leaked, ED `555` | `2` killed, `1` leaked, ED `710` | `2` killed, `1` leaked, ED `700` | Remains Tough while exposing meaningful Elemental conversion |
| Golem `HP480` | `0` killed, `3` leaked, ED `545` | `0` killed, ED `820` | `0` killed, ED `655` | Remains Elite and preserves long Effect/Buff interaction windows |

Fire produced `7 / 16 / 25` Periodic Ticks and maximum observed Stacks `3 / 5 / 7` for Bat, Dragon, and Golem respectively. Cold reached maximum observed Stacks `5 / 8 / 10`. One of three Golems triggered one single-source Cold Overload after approximately `18.534s`; Bat and Dragon triggered none. This is accepted because the current Buff contract permits one source to reach Maximum Stacks, while the ordinary single-source expectation is only that it must not reliably Overload every Monster. The durable Elite exposed the intended full lifecycle without collapsing matching-source cooperation into a universal result.

All nine Phase B reports completed spawning, matched the authored Tower/Upgrade formula and Buff parameter snapshots, began observing every Monster at registration and full Health, and reported all six integrity flags as `True`. No recursive reaction, Overload-without-Protection, invalid target, or negative Cold damage regression was observed.

### 6.3 Schema-v8 Monster Runtime Diagnostics

Task005 results require `CombatBalanceRunRecorder` schema v8 or later. Schema v8 preserves the complete schema-v7 Tower, Projectile, Buff, fixture, combat, timing, player, and integrity output and adds:

```text
monsterRuntime
    observedTypes
    instanceSamples
    registrationObservedInstances
    fallbackObservedInstances
    instancesObservedAtFullHealth
    instancesObservedAfterDamage
    unobservedDamageAtObservationStart
    successfulDamageApplications
    types[]
    instances[]
```

Each `types[]` record groups one observed runtime-template, Maximum Health, and Move Speed combination and records Spawned, Resolved, Killed, Leaked, Unresolved, Effective Damage, Successful Damage Applications, leaked remaining Health, and resolution/killed/leaked lifetime metrics.

Each `instances[]` record retains Spawn Ordinal, runtime-template name, Display Name, Maximum Health, Move Speed at Spawn, whether the Recorder observed the canonical Monster-registration event, Health at observation start, unobserved Damage already present at observation start, Resolution Type, final Health, Effective Damage, Successful Damage Applications, and lifetime seconds.

A `Successful Damage Application` is one observed `OnHealthChanged` event that reduced current Health. It counts direct Hits, contact damage, Effect damage, or periodic damage uniformly, but does not claim the raw requested damage or preserve overkill beyond remaining Health. Phase A contains one Base Tower and no Effect or Buff damage, so this count is the successful health-reducing hit/contact count for that run. Projectile releases remain a separate opportunity diagnostic and must not be substituted for successful damage applications.

Every accepted schema-v8 result must report `SpawningCompleted=True` plus all six integrity flags as `True`: the two existing resolution and Player-health checks, `MonsterRuntimeCountsMatch`, `MonsterRuntimeDamageMatches`, `MonsterRuntimeRegistrationCoverageMatch`, and `MonsterRuntimeStartedAtFullHealth`. The registration-coverage flag proves every lifetime began at the canonical Monster registration event rather than from a late fallback scan. The full-health flag proves no Damage occurred before observation began. A result may reconcile its internal totals while either completeness flag is `False`; such a result is diagnostic output only and its lifetime, TTK, Damage-application count, and spawn timing must not be accepted for calibration.

## 7. Ownership

| Owner | Responsibility |
|---|---|
| Monster content | Authored health, speed, Prefab, and presentation |
| Monster System | Movement, lifecycle, and resolution behavior |
| Effect and Buff Systems | Existing interactions with valid Monster targets |
| Task005 | Role justification, comparative measurements, and accepted roster |

## 8. Accepted Roster Table

| Monster Role | Runtime Prefab | Max Health | Move Speed | Accepted Tactical Meaning | Decision |
|---|---|---:|---:|---|---|
| Normal | Bat | `120` | `0.25` | Ordinary Wave body and frozen Reference comparison unit | Accept |
| Tough | Dragon | `240` | `0.25` | Extends Core and Elemental exposure while remaining resolvable by developed direct-output Towers | Accept |
| Elite | Golem | `480` | `0.25` | Survives single Base and representative Core pressure long enough to preserve specialization and Buff lifecycle value | Accept |

## 9. Execution Collaboration

- The user describes how durable or urgent each Monster role should feel and owns Unity asset authoring plus Play Mode runs.
- Codex fills the initial health and movement table from accepted Tower output, checks shot and burst breakpoints, and revises the smallest necessary parameter set.
- Task005 freezes shared Move Speed `0.25`; Task007 owns any later movement-identity proposal and regression.

## 10. Unity Authoring Checklist

- Identify the canonical Reference Monster.
- Record every Monster runtime template used by the roster.
- Preserve shared Move Speed `0.25` until Task007 accepts a deliberate revision.
- Author health tiers from measured survival targets.
- Validate all Monster runtime references.
- Test death and Target-arrival resolution.

## 11. Acceptance Criteria

- One stable Reference Monster remains available.
- Task003 and Task004 calibration fixtures do not silently replace the Reference Monster or enter the Stage roster.
- Every additional Monster role has a clear reason to exist.
- Maximum health produces understandable survival tiers.
- Movement-speed differences are deliberate rather than incidental.
- No Monster requires a new systemic mechanic merely to justify its identity.
- All accepted Monsters remain compatible with existing Effect and Buff contracts.
- Normal, Tough, and Elite identities remain ordered under the accepted representative Control, Fire, and Cold builds.
- Movement-speed differentiation is not inferred from this HP-only evidence.

## 12. Validation

- Fixed Tower-versus-Monster TTK runs
- Route traversal comparison
- Resolution and Player consequence checks
- Effect and Buff regression
- Static asset validation
- Representative Core, Fire, and Cold regression

## 13. Review Note

Health-only differentiation is sufficient for the first roster baseline, so Task005 stops with three accepted roles. Task007 may later revise Move Speed only through its independent movement, Wave-formation, Cannon-projectile, Slow, Buff-continuity, and role-preservation evidence. Task008 consumes the accepted result after Task006 route/lane implementation and Task007 regression when revisiting Stage Wave skeletons.
