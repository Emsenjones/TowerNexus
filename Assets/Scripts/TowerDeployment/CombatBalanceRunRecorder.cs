#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum PlacementRouteForcedRelocationExpectation
{
    RequireZero = 0,
    RequireDiagnosed = 1,
    Ignore = 2
}

[DisallowMultipleComponent]
public sealed class CombatBalanceRunRecorder : MonoBehaviour
{
    [Header("Run Identity")]
    [FormerlySerializedAs("runLabel")]
    [SerializeField] private string runName;

    [Header("Placement Route Fixture")]
    [SerializeField] private PlacementRouteForcedRelocationExpectation
        placementRouteForcedRelocationExpectation =
            PlacementRouteForcedRelocationExpectation.RequireZero;

    [Header("Runtime References")]
    [SerializeField] private BattleRuntimeCoordinator battleRuntimeCoordinator;
    [SerializeField] private StageCompositionController stageCompositionController;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private DraftSystem draftSystem;
    [SerializeField] private BattleHUDUI battleHUDUI;



    private CombatRecordingSession session;
    private PlayerSystem subscribedPlayer;
    private void OnEnable()
    {
        if (battleRuntimeCoordinator == null)
        {
            var candidates = FindObjectsByType<BattleRuntimeCoordinator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (candidates.Length == 1) battleRuntimeCoordinator = candidates[0];
        }
        if (battleRuntimeCoordinator == null) { Debug.LogWarning("Assign one BattleRuntimeCoordinator before recording.", this); return; }
        if (stageCompositionController != null && stageCompositionController.BattleRuntimeCoordinator != battleRuntimeCoordinator)
            stageCompositionController = null;
        if (stageCompositionController == null)
        {
            var stages = FindObjectsByType<StageCompositionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var stage in stages)
                if (stage.BattleRuntimeCoordinator == battleRuntimeCoordinator) { stageCompositionController = stage; break; }
        }
        subscribedPlayer = battleRuntimeCoordinator.DiagnosticPlayer;
        if (subscribedPlayer != null) subscribedPlayer.OnBattleStateInitialized += BeginSession;
        // Mid-Battle enabling waits for the next full initialization; no partial acceptance export.
    }
    private void BeginSession()
    {
        session?.Cancel();
        try { session = new CombatRecordingSession(this, battleRuntimeCoordinator, stageCompositionController, runName, placementRouteForcedRelocationExpectation); }
        catch (Exception error) { Debug.LogException(error, this); session = null; }
    }
    private void Update() { session?.Tick(); }
    private void OnDisable()
    {
        if (subscribedPlayer != null) subscribedPlayer.OnBattleStateInitialized -= BeginSession;
        subscribedPlayer = null; session?.Cancel(); session = null;
    }
    [ContextMenu("Log Current Balance Summary")]
    private void LogCurrentBalanceSummary() { session?.ManualSnapshot(); }
    [ContextMenu("Reset Balance Recorder")]
    private void ResetBalanceRecorder() { session?.Cancel(); session = null; Debug.Log("Recorder reset; waiting for next Battle initialization.", this); }
}
#endif
