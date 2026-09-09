using System;
using System.IO;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Collections.Generic;
using UnityEngine;
static class LifecycleTests
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static void Main()
    {
        var oldBattle = new object(); var nextBattle = new object();
        var old = CombatDiagnosticScope.Acquire(oldBattle); int finalFacts = 0, exports = 0;
        old.Drained = () => { Check(finalFacts == 3, "freeze after final resolution, hit finally and cleanup"); exports++; old.Dispose(); };
        using (CombatDiagnosticScope.Enter(oldBattle))
        {
            using (CombatDiagnosticScope.Enter(oldBattle))
            {
                old.Closing = true; finalFacts++; // terminal + pre-cleanup capture
                var next = CombatDiagnosticScope.Acquire(nextBattle);
                using (CombatDiagnosticScope.Enter(nextBattle))
                    Check(ReferenceEquals(CombatDiagnosticScope.CurrentIdentity, nextBattle), "synchronous Retry identity");
                Check(ReferenceEquals(CombatDiagnosticScope.CurrentIdentity, oldBattle), "restore old cleanup identity");
                next.Dispose(); finalFacts++;
            }
            Check(exports == 0, "nested return does not freeze outer hit");
            finalFacts++;
        }
        Check(exports == 1 && !CombatDiagnosticScope.Enabled(oldBattle), "exactly one drain and detached lease");
        Check(CombatDiagnosticScope.CurrentIdentity == null, "ambient restored");
        Check(CombatDiagnosticScope.Acquire(null) == null, "no anonymous session");
        var lease = CombatDiagnosticScope.Acquire(oldBattle);
        Check(CombatDiagnosticScope.Acquire(oldBattle) == null, "duplicate recorder rejected");
        bool committed = false;
        using (CombatDiagnosticScope.Enter(oldBattle))
        {
            string result = CombatDiagnosticScope.Capture<string>(oldBattle, () => throw new Exception("capture injection"));
            committed = true;
            Check(result == null && lease.Failed, "failed capture cannot be passing evidence");
        }
        Check(committed && Debug.Errors == 1, "capture exception isolated from gameplay");
        var tower = new TowerInstance { name = "Tower", TowerDefinition = new TowerDefinition { DisplayName = "Archer" } };
        using (CombatDiagnosticScope.Enter(oldBattle))
        {
            var captured = CombatDiagnosticScope.Source(tower); tower.Destroyed = true;
            Check(CombatDiagnosticScope.Source(tower).Id == captured.Id && captured.Id != 0, "cached identity survives destruction");
            Check(CombatDiagnosticScope.Source(null).Id == 0, "source-less buff allowed");
            var monster = new MonsterBehaviour { CurrentNode = new GridNodeBehaviour { GridPosition = new Vector2Int { x = 3, y = 4 } } };
            CombatDiagnosticScope.CaptureTarget(monster);
            monster.CurrentNode = null;
            var target = CombatDiagnosticScope.Target(monster);
            Check(target.Id == 19 && target.HasNode && target.Position.x == 3 && target.Position.y == 4, "pre-hit route identity survives cleanup");
        }
        lease.Dispose();
        int captures = 0; var clock = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
            if (CombatDiagnosticScope.Enabled(oldBattle)) CombatDiagnosticScope.Capture(oldBattle, () => ++captures);
        clock.Stop(); Check(captures == 0, "disabled producer never invokes capture");
        Console.WriteLine("100000 disabled gates: " + clock.Elapsed.TotalMilliseconds.ToString("F3") + " ms (managed harness; not Unity profiling)");
        string dir = Path.Combine(Path.GetTempPath(), "task006-" + Guid.NewGuid());
        try
        {
            var path = Path.Combine(dir, "run.json");
            CombatReportExporter.Enqueue(path, "{\"frozen\":1}", "Victory");
            CombatReportExporter.Enqueue(path, "{\"frozen\":2}", "Defeat");
            Check(!Directory.Exists(dir), "no synchronous file I/O");
            UnityEditor.EditorApplication.Flush();
            Check(File.ReadAllText(path) == "{\"frozen\":1}" && File.ReadAllText(Path.Combine(dir,"run_1.json")) == "{\"frozen\":2}", "detached queued payloads never overwrite");
            var occupied = Path.Combine(dir,"occupied");File.WriteAllText(occupied,"file");
            CombatReportExporter.Enqueue(Path.Combine(occupied,"bad.json"), "{}", "Victory");
            UnityEditor.EditorApplication.Flush();
            Check(Debug.Warnings == 1, "export failure isolated");
        }
        finally { Directory.Delete(dir,true); }
        Console.WriteLine(checks + " Task006 lifecycle/export assertions passed.");
    }
}
namespace UnityEditor { static class EditorApplication { internal static Action delayCall; internal static void Flush() { var action=delayCall;delayCall=null;action?.Invoke(); } } }
namespace UnityEngine { static class Debug { internal static int Errors,Warnings; internal static void LogException(Exception e) { Errors++; } internal static void LogWarning(string s) { Warnings++; } internal static void Log(string s) {} } }
class EffectDefinition { internal string name; }
class TowerDefinition { internal string name; internal string DisplayName; internal string TowerFamily = "Archer"; }
class TowerInstance
{
    internal bool Destroyed; internal string name; internal TowerDefinition TowerDefinition;
    internal int GetInstanceID() { if (Destroyed) throw new Exception("native object destroyed"); return 17; }
}

namespace UnityEngine { struct Vector2Int { internal int x,y; } }
class MonsterBehaviour { internal GridNodeBehaviour CurrentNode; internal int GetInstanceID() => 19; }
class GridNodeBehaviour { internal Vector2Int GridPosition; }
