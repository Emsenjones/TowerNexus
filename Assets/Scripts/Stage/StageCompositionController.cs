using System;
using UnityEngine;

public class StageCompositionController : MonoBehaviour
{
    [SerializeField] private StageDefinition initialStageDefinition;
    [SerializeField] private Transform stageRuntimeRoot;
    [SerializeField] private BattleRuntimeCoordinator battleRuntimeCoordinator;
    [SerializeField] private bool composeInitialStageOnStart = true;

    private GameObject activeMapObject;
    private bool isComposing;

    public StageDefinition ActiveStage { get; private set; }
    public MapGeneratorBehaviour ActiveMap { get; private set; }
    public bool IsCompositionReady { get; private set; }
    public bool IsComposing => isComposing;

    private void Start()
    {
        if (!composeInitialStageOnStart)
        {
            return;
        }

        if (!ComposeStage(initialStageDefinition))
        {
            Debug.LogError(
                "Stage composition controller failed to compose its initial Stage.",
                this);
        }
    }

    private void OnDisable()
    {
        CleanupComposedStageRuntime();
    }

    public bool ComposeStage(StageDefinition selectedStage)
    {
        if (isComposing)
        {
            Debug.LogError(
                "Stage composition controller rejected a reentrant composition request.",
                this);
            return false;
        }

        if (!ValidateCompositionRequest(selectedStage))
        {
            return false;
        }

        isComposing = true;

        try
        {
            CleanupComposedStageRuntime();

            if (!TryCreateActiveMap(selectedStage))
            {
                CleanupComposedStageRuntime();
                return false;
            }

            if (!battleRuntimeCoordinator.TryPrepareBattleRuntime(
                    ActiveMap,
                    selectedStage.MonsterWaveConfig,
                    selectedStage.PlayerMaxHealth,
                    selectedStage.TowerDraftPool,
                    selectedStage.TowerUpgradeDraftPool))
            {
                Debug.LogError(
                    $"Stage '{GetStageName(selectedStage)}' failed while preparing " +
                    "Battle runtime dependencies.",
                    this);
                CleanupComposedStageRuntime();
                return false;
            }

            IsCompositionReady = true;

            if (!battleRuntimeCoordinator.BeginPreparedBattle())
            {
                Debug.LogError(
                    $"Stage '{GetStageName(selectedStage)}' failed while committing " +
                    "the prepared battle runtime.",
                    this);
                CleanupComposedStageRuntime();
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            CleanupComposedStageRuntime();
            return false;
        }
        finally
        {
            isComposing = false;
        }
    }

    public void ReleaseStage()
    {
        if (isComposing)
        {
            Debug.LogWarning(
                "Stage composition controller cannot release its Stage during composition.",
                this);
            return;
        }

        CleanupComposedStageRuntime();
    }

    private bool ValidateCompositionRequest(StageDefinition selectedStage)
    {
        if (selectedStage == null)
        {
            Debug.LogError(
                "Stage composition controller cannot compose a null Stage Definition.",
                this);
            return false;
        }

        if (!HasRequiredReferences(out string missingReference))
        {
            Debug.LogError(
                $"Stage composition controller cannot compose Stage " +
                $"'{GetStageName(selectedStage)}': {missingReference}",
                this);
            return false;
        }

        StageValidationResult validation = selectedStage.ValidateStage();
        LogStageValidation(selectedStage, validation);
        return validation.IsValid;
    }

    private bool TryCreateActiveMap(StageDefinition selectedStage)
    {
        GameObject mapObject = Instantiate(
            selectedStage.MapTemplate,
            stageRuntimeRoot,
            false);
        mapObject.name = $"{selectedStage.MapTemplate.name}_Runtime";
        mapObject.SetActive(true);
        activeMapObject = mapObject;
        ActiveStage = selectedStage;

        MapGeneratorBehaviour[] rootMapOwners =
            mapObject.GetComponents<MapGeneratorBehaviour>();
        MapGeneratorBehaviour[] allMapOwners =
            mapObject.GetComponentsInChildren<MapGeneratorBehaviour>(true);

        if (rootMapOwners.Length != 1 || allMapOwners.Length != 1)
        {
            Debug.LogError(
                $"Stage '{GetStageName(selectedStage)}' instantiated Map " +
                $"'{mapObject.name}' without exactly one root MapGeneratorBehaviour.",
                mapObject);
            return false;
        }

        MapValidationResult validation = rootMapOwners[0].ValidateMap();

        for (int i = 0; i < validation.Errors.Count; i++)
        {
            Debug.LogError(
                $"Stage '{GetStageName(selectedStage)}' instantiated Map validation " +
                $"failed: {validation.Errors[i]}",
                mapObject);
        }

        for (int i = 0; i < validation.Warnings.Count; i++)
        {
            Debug.LogWarning(
                $"Stage '{GetStageName(selectedStage)}' instantiated Map validation " +
                $"warning: {validation.Warnings[i]}",
                mapObject);
        }

        if (!validation.IsValid)
        {
            return false;
        }

        ActiveMap = rootMapOwners[0];
        return true;
    }

    private void CleanupComposedStageRuntime()
    {
        IsCompositionReady = false;

        battleRuntimeCoordinator?.ReleasePreparedBattleRuntime();

        GameObject mapObjectToDestroy = activeMapObject;
        activeMapObject = null;
        ActiveMap = null;
        ActiveStage = null;

        if (mapObjectToDestroy == null)
        {
            return;
        }

        mapObjectToDestroy.SetActive(false);
        Destroy(mapObjectToDestroy);
    }

    private bool HasRequiredReferences(out string failureReason)
    {
        if (stageRuntimeRoot == null)
        {
            failureReason = "Stage Runtime Root is not assigned.";
            return false;
        }

        if (battleRuntimeCoordinator == null)
        {
            failureReason = "Battle Runtime Coordinator is not assigned.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private void LogStageValidation(
        StageDefinition selectedStage,
        StageValidationResult validation)
    {
        string stageName = GetStageName(selectedStage);

        for (int i = 0; i < validation.Errors.Count; i++)
        {
            Debug.LogError(
                $"Stage '{stageName}' composition validation failed: " +
                validation.Errors[i],
                selectedStage);
        }

        for (int i = 0; i < validation.Warnings.Count; i++)
        {
            Debug.LogWarning(
                $"Stage '{stageName}' composition validation warning: " +
                validation.Warnings[i],
                selectedStage);
        }
    }

    private static string GetStageName(StageDefinition stageDefinition)
    {
        if (stageDefinition == null)
        {
            return "<null>";
        }

        return string.IsNullOrEmpty(stageDefinition.DisplayName)
            ? stageDefinition.name
            : stageDefinition.DisplayName;
    }
}
