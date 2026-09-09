# Task005 Battle binding contracts

Run `python3 Tests/Task005/run.py` from the repository root (Mono csc/mono).

The harness compiles the complete production BattleCombatBinding,
BuffRemovalPermission, EffectTriggerContext, EffectTargetResolver, EffectExecutor,
and ElementalApplication/TowerOwnedHitTransaction. It also extracts current
production revocation, Stop and Release entry methods into a lifecycle harness.

27 assertions cover bound radius/direct/nested targeting, Vortex binding handoff,
source-less FixedBuff, unchanged TowerScaled source requirements, stale identities
and reused-manager separation, dead-owner active/closed effects, restricted Removed
permissions and runtime reuse, closure between actions/targets, health-callback Stop
and exceptions with committed evidence, missing binding output clearing, one-time
broken-dependency failure, and revocation before evidence/deferred release.

Unity objects, Monster health/storage, Buff application/storage, damage math and the
Vortex entity are boundary doubles. These tests do not establish real Buff stacking,
Overload finalization, native transforms, entity destruction, component recovery,
physics, damage formula correctness or fresh Recorder export. Runtime/Editor builds
check all migrated production producer APIs; native coverage is listed in Task005.

Task001–004 suites retain their existing scope. Task004's Release harness substitutes
revocation; this suite exercises the production revocation method with a real binding.
