using System;
namespace UnityEngine
{
    public static class Time { public static float timeScale = 1f; }
}
enum TowerAttackState { Idle, WaitingForAnimationRelease }
class Binding { public bool IsUsable = true; }
partial class AttackHarness
{
    bool isBattleActive = true, isRuntimeSessionActive = true, isPreparedForBattleActivation;
    bool hasResolvedStatsCache, hasExplicitInitialization, hasCompletedSubtypeInitialization;
    object currentTarget, towerInstance, towerDefinition;
    float attackCycleTimer;
    Binding battleBinding = new Binding();
    TowerAttackState attackState;
    ulong attackRevision, deferredReleaseRevision;
    bool hasDeferredRelease;
    bool IsWaitingForAnimationRelease => attackState == TowerAttackState.WaitingForAnimationRelease;
    bool TryValidateExplicitOwner() => true;
    bool EnsureRuntimeSession() => isBattleActive && isRuntimeSessionActive && battleBinding.IsUsable;
    bool SetAttackAnimatorTrigger() => false;
    bool CanScheduleCombat() => true;
    void OnAnimationRelease() { Releases++; SetIdle(); }
    void UpdateAttackCycle() { Cycles++; }
    void OnOwnedRuntimeUpdate() { OwnedTicks++; }
    void DetectEnemies() { Detections++; }
    void OnCombatUpdate() { Schedules++; }
    void OnCombatCleanup() { }
    void ForceCleanupOwnedProjectiles() { }
    internal int Releases, Cycles, OwnedTicks, Detections, Schedules;
    internal void Wait() { SetWaitingForAnimationRelease(); }
    internal void Frame() { Update(); }
    internal void Stop() { StopBattle(); }
    internal void Start() { isBattleActive = isRuntimeSessionActive = true; }
}
class AttackTests
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Main()
    {
        var h = new AttackHarness(); h.Wait(); UnityEngine.Time.timeScale = 0;
        h.OnAttackAnimationRelease(); h.OnAttackAnimationRelease(); h.Frame();
        Check(h.Releases == 0 && h.Cycles == 0 && h.Detections == 0 && h.OwnedTicks == 0, "paused event and ready work do not advance");
        UnityEngine.Time.timeScale = 1; h.Frame(); h.OnAttackAnimationRelease();
        Check(h.Releases == 1, "one-shot animation event resumes exactly once");
        h = new AttackHarness(); h.Wait(); UnityEngine.Time.timeScale = 0;
        h.OnTowerPresentationReplaced(); UnityEngine.Time.timeScale = 1; h.Frame();
        Check(h.Releases == 1, "presentation replacement fallback preserves paused release");
        h = new AttackHarness(); h.Wait(); UnityEngine.Time.timeScale = 0;
        h.OnAttackAnimationRelease(); h.Stop(); h.Start(); UnityEngine.Time.timeScale = 1; h.Frame();
        Check(h.Releases == 0, "stop discards outgoing deferred event before a new runtime");
        h = new AttackHarness(); h.Wait(); UnityEngine.Time.timeScale = 0;
        h.OnAttackAnimationRelease(); h.Wait(); UnityEngine.Time.timeScale = 1; h.Frame();
        Check(h.Releases == 0, "new attack identity cannot consume an old release fact");
        Console.WriteLine("PASS production attack method boundaries: " + checks + " cases");
    }
}
