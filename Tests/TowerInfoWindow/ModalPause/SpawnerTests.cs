using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine
{
    public static class Time { public static float timeScale=1f; }
    public class WaitForSeconds { public WaitForSeconds(float seconds) { } }
}
class MonsterBehaviour { }
class MonsterWaveEntry
{
    public int Count=2;
    public float WaveDelay, SpawnInterval;
    public MonsterBehaviour MonsterRuntimeTemplate=new MonsterBehaviour();
    public bool HasValidSpawnData() => true;
}
class MonsterWaveConfig { public IReadOnlyList<MonsterWaveEntry> Waves=new[]{new MonsterWaveEntry()}; }
partial class SpawnerHarness
{
    MonsterWaveConfig waveConfig=new MonsterWaveConfig();
    internal bool isBattleActive=true;
    internal int Spawns, Completes, Cancels, StartedWaves;
    internal Action<int,MonsterWaveEntry> OnWaveSpawningStarted, OnWaveSpawningCompleted;
    internal Action<int,int,MonsterBehaviour> OnMonsterSpawnedFromWave;
    bool TrySpawnMonster(MonsterBehaviour template, out MonsterBehaviour monster, out string reason)
    { Spawns++;monster=new MonsterBehaviour();reason="";return true; }
    void FailSpawnExecution(string reason) { throw new Exception(reason); }
    void CancelSpawnExecution() { Cancels++; }
    void CompleteSpawnExecution() { Completes++; }
    internal IEnumerator Routine() => SpawnWavesRoutine();
}
class SpawnerTests
{
    static void Check(bool condition, string reason) { if(!condition)throw new Exception(reason); }
    static void Main()
    {
        var h=new SpawnerHarness(); var routine=h.Routine(); UnityEngine.Time.timeScale=0;
        Check(routine.MoveNext()&&h.Spawns==0&&h.Completes==0,"zero-delay wave cannot spawn while paused");
        h.OnWaveSpawningStarted=(i,w)=>{h.StartedWaves++;UnityEngine.Time.timeScale=0;};
        UnityEngine.Time.timeScale=1;
        Check(routine.MoveNext()&&h.StartedWaves==1&&h.Spawns==0,"pause in wave-start callback blocks first spawn");
        h.OnMonsterSpawnedFromWave=(i,n,m)=>{if(n==0)UnityEngine.Time.timeScale=0;};
        UnityEngine.Time.timeScale=1;
        Check(routine.MoveNext()&&h.Spawns==1&&h.Completes==0,"pause in spawn callback blocks zero-interval next spawn");
        UnityEngine.Time.timeScale=1;
        Check(!routine.MoveNext()&&h.Spawns==2&&h.Completes==1,"resume finishes retained wave once");
        h=new SpawnerHarness();routine=h.Routine();UnityEngine.Time.timeScale=0;
        routine.MoveNext();h.isBattleActive=false;
        Check(!routine.MoveNext()&&h.Spawns==0&&h.Completes==0&&h.Cancels==1,"stop during pause cancels without normal completion");
        Console.WriteLine("PASS production zero-delay spawning method: 5 cases");
    }
}
