using System;

namespace UnityEngine { public static class Time { public static float timeScale = 1f; } }

class PauseTests
{
    static int assertions;
    internal static void Check(bool value, string reason)
    { assertions++; if (!value) throw new Exception(reason); }
    static void Main()
    {
        foreach (float original in new[] { 1f, 0.35f, 0f })
        foreach (BattleModalKind first in new[] { BattleModalKind.Draft, BattleModalKind.TowerInspection })
        {
            float rate = original; int writes = 0;
            var pause = new BattleModalPauseAuthority(() => rate, x => { rate = x; writes++; });
            var battle = new object(); var session = new object();
            pause.BindBattle(battle);
            Check(!pause.CanAcquire && !pause.TryAcquire(battle, first, session, out _, out _), "prepared Battle is closed");
            pause.OpenBattle(battle);
            Check(pause.CanAcquire, "active Battle available");
            Check(pause.TryAcquire(battle, first, session, out var handle, out _), "acquire");
            Check(rate == 0 && writes == 1 && pause.Owns(handle) && pause.CurrentKind == first, "captured pause and kind");
            var second = first == BattleModalKind.Draft ? BattleModalKind.TowerInspection : BattleModalKind.Draft;
            Check(!pause.TryAcquire(battle, second, new object(), out _, out _) && writes == 1, "overlap cannot overwrite rate");
            var forged = new BattleModalPauseHandle(pause, battle, session, first);
            pause.Release(forged);
            Check(pause.Owns(handle) && rate == 0 && writes == 1, "identity cannot be reconstructed");
            pause.RevokeBattle(new object()); Check(pause.Owns(handle), "foreign revoke ignored");
            pause.RevokeBattle(battle);
            Check(!pause.Owns(handle) && pause.HasRetainedPause && !pause.CanAcquire && pause.CurrentKind == first, "revoked retained pause remains modal");
            bool reopened = false;
            try { pause.OpenBattle(battle); reopened = true; } catch (InvalidOperationException) { }
            Check(!reopened, "revoked Battle never reopens");
            pause.Release(handle); pause.Release(handle);
            Check(rate == original && writes == 2 && !pause.HasRetainedPause, "restore exact rate once after revocation");
            pause.CancelBattle(battle);
            var fresh = new object(); pause.BindBattle(fresh); pause.OpenBattle(fresh);
            Check(pause.TryAcquire(fresh, second, new object(), out var incoming, out _), "fresh Battle acquisition");
            pause.Release(handle); pause.CancelBattle(battle); pause.RevokeBattle(battle);
            Check(pause.Owns(incoming) && rate == 0, "outgoing release/revoke cannot affect incoming Battle");
            pause.CancelBattle(fresh); pause.CancelBattle(fresh);
            Check(rate == original && !pause.HasRetainedPause && !pause.CanAcquire, "fallback restores once and clears binding");
        }
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            int writes = 0;
            var pause = new BattleModalPauseAuthority(() => invalid, x => writes++);
            var battle = new object(); pause.BindBattle(battle); pause.OpenBattle(battle);
            Check(!pause.TryAcquire(battle, BattleModalKind.Draft, new object(), out _, out _) && writes == 0 && !pause.HasRetainedPause,
                "invalid rate rejected without state mutation");
        }
        Console.WriteLine("PASS production modal pause authority: " + assertions + " assertions");
    }
}
