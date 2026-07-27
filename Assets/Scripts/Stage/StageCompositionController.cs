using System;
using UnityEngine;

public class StageCompositionController : MonoBehaviour
{
    [SerializeField] private Transform stageRuntimeRoot;
    [SerializeField] private BattleRuntimeCoordinator battleRuntimeCoordinator;

    private GameObject activeMapObject;
    private StageDefinition candidateStage;
    private GameObject candidateMapObject;
    private MapGeneratorBehaviour candidateMap;
    private bool isLifecycleOperationInProgress;
    private bool isPreparationInProgress;
    private bool releaseRequested;

    public StageDefinition ActiveStage { get; private set; }
    public MapGeneratorBehaviour ActiveMap { get; private set; }
    public bool IsCompositionReady { get; private set; }
    public bool IsPreparationInProgress => isPreparationInProgress;
    public bool HasBattleBegun { get; private set; }
    public BattleRuntimeCoordinator BattleRuntimeCoordinator =>
        battleRuntimeCoordinator;

    private void OnDisable()
    {
        ReleaseStage();
    }

    public bool TryPrepareStage(StageDefinition selectedStage)
    {
        if (isLifecycleOperationInProgress)
        {
            Debug.LogError(
                "Stage composition controller rejected a reentrant Stage " +
                "preparation request.",
                this);
            return false;
        }

        isLifecycleOperationInProgress = true;
        isPreparationInProgress = true;
        bool commitStarted = false;
        bool preparationSucceeded = false;

        try
        {
            if (ValidatePreparationRequest(selectedStage))
            {
                commitStarted = true;
                ReleaseStageRuntimeCore();

                if (CanContinueLifecycleOperation() &&
                    TryCreateCandidateMap(selectedStage) &&
                    CanContinueLifecycleOperation())
                {
                    if (!battleRuntimeCoordinator.TryPrepareBattleRuntime(
                            candidateMap,
                            selectedStage.MonsterWaveConfig,
                            selectedStage.PlayerMaxHealth,
                            selectedStage.TowerDraftPool,
                            selectedStage.TowerUpgradeDraftPool))
                    {
                        Debug.LogError(
                            $"Stage '{GetStageName(selectedStage)}' failed while " +
                            "preparing Battle runtime dependencies.",
                            this);
                    }
                    else if (!CanContinueLifecycleOperation())
                    {
                        Debug.LogWarning(
                            $"Stage '{GetStageName(selectedStage)}' preparation " +
                            "was cancelled by a deferred release.",
                            this);
                    }
                    else if (!battleRuntimeCoordinator.IsBattlePrepared ||
                             battleRuntimeCoordinator.IsBattleActive)
                    {
                        Debug.LogError(
                            $"Stage '{GetStageName(selectedStage)}' reached an " +
                            "invalid prepared Battle coordinator state.",
                            this);
                    }
                    else
                    {
                        CommitCandidateStage();
                        preparationSucceeded = true;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            preparationSucceeded = FinalizeLifecycleOperation(
                preparationSucceeded,
                commitStarted);
        }

        return preparationSucceeded;
    }

    public bool TryBeginPreparedStage()
    {
        if (isLifecycleOperationInProgress)
        {
            Debug.LogError(
                "Stage composition controller rejected a reentrant Stage begin request.",
                this);
            return false;
        }

        if (!IsCompositionReady ||
            ActiveStage == null ||
            ActiveMap == null ||
            activeMapObject == null)
        {
            Debug.LogError(
                "Stage composition controller cannot begin because no valid Stage " +
                "composition is prepared.",
                this);
            return false;
        }

        if (HasBattleBegun)
        {
            Debug.LogError(
                $"Stage '{GetStageName(ActiveStage)}' has already begun.",
                this);
            return false;
        }

        if (battleRuntimeCoordinator == null ||
            !battleRuntimeCoordinator.IsBattlePrepared ||
            battleRuntimeCoordinator.IsBattleActive)
        {
            Debug.LogError(
                $"Stage '{GetStageName(ActiveStage)}' cannot begin because its " +
                "Battle runtime is not waiting in a valid prepared state.",
                this);
            ReleaseStageRuntimeCore();
            return false;
        }

        isLifecycleOperationInProgress = true;
        bool beginSucceeded = false;

        // Publish the begun fact before entering callback-producing Battle startup.
        // Technical startup failure clears it through the rollback path below.
        HasBattleBegun = true;

        try
        {
            if (!battleRuntimeCoordinator.BeginPreparedBattle())
            {
                Debug.LogError(
                    $"Stage '{GetStageName(ActiveStage)}' failed while beginning " +
                    "its prepared Battle runtime.",
                    this);
            }
            else if (CanContinueLifecycleOperation())
            {
                beginSucceeded = true;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            beginSucceeded = FinalizeLifecycleOperation(
                beginSucceeded,
                true);
        }

        return beginSucceeded;
    }

    public void ReleaseStage()
    {
        if (isLifecycleOperationInProgress)
        {
            releaseRequested = true;
            return;
        }

        ReleaseStageRuntimeCore();
    }

    private bool ValidatePreparationRequest(StageDefinition selectedStage)
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "Stage composition controller cannot prepare a Stage while disabled.",
                this);
            return false;
        }

        if (selectedStage == null)
        {
            Debug.LogError(
                "Stage composition controller cannot prepare a null Stage Definition.",
                this);
            return false;
        }

        if (!HasRequiredReferences(out string missingReference))
        {
            Debug.LogError(
                $"Stage composition controller cannot prepare Stage " +
                $"'{GetStageName(selectedStage)}': {missingReference}",
                this);
            return false;
        }

        if (!battleRuntimeCoordinator.TryValidatePreparationReferences(
                out string coordinatorFailureReason))
        {
            Debug.LogError(
                $"Stage composition controller cannot prepare Stage " +
                $"'{GetStageName(selectedStage)}': {coordinatorFailureReason}",
                this);
            return false;
        }

        StageValidationResult validation = selectedStage.ValidateStage();
        LogStageValidation(selectedStage, validation);
        return validation.IsValid;
    }

    private bool TryCreateCandidateMap(StageDefinition selectedStage)
    {
        candidateStage = selectedStage;
        GameObject mapObject = Instantiate(
            selectedStage.MapTemplate,
            stageRuntimeRoot,
            false);
        candidateMapObject = mapObject;
        mapObject.name = $"{selectedStage.MapTemplate.name}_Runtime";
        mapObject.SetActive(true);

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

        candidateMap = rootMapOwners[0];
        return true;
    }

    private void CommitCandidateStage()
    {
        ActiveStage = candidateStage;
        ActiveMap = candidateMap;
        activeMapObject = candidateMapObject;

        candidateStage = null;
        candidateMap = null;
        candidateMapObject = null;

        HasBattleBegun = false;
        IsCompositionReady = true;
    }

    private bool CanContinueLifecycleOperation()
    {
        return !releaseRequested && isActiveAndEnabled;
    }

    private bool FinalizeLifecycleOperation(
        bool operationSucceeded,
        bool commitStarted)
    {
        isPreparationInProgress = false;
        isLifecycleOperationInProgress = false;

        bool deferredReleaseRequested = releaseRequested;
        releaseRequested = false;

        if (deferredReleaseRequested ||
            (commitStarted && !operationSucceeded))
        {
            ReleaseStageRuntimeCore();
        }

        return operationSucceeded && !deferredReleaseRequested;
    }

    private void ReleaseStageRuntimeCore()
    {
        IsCompositionReady = false;
        HasBattleBegun = false;

        battleRuntimeCoordinator?.ReleasePreparedBattleRuntime();

        GameObject committedMapObjectToDestroy = activeMapObject;
        GameObject candidateMapObjectToDestroy = candidateMapObject;

        activeMapObject = null;
        candidateMapObject = null;
        candidateMap = null;
        candidateStage = null;
        ActiveMap = null;
        ActiveStage = null;

        DestroyOwnedMapObject(candidateMapObjectToDestroy);

        if (committedMapObjectToDestroy != candidateMapObjectToDestroy)
        {
            DestroyOwnedMapObject(committedMapObjectToDestroy);
        }
    }

    private void DestroyOwnedMapObject(GameObject mapObject)
    {
        if (mapObject == null)
        {
            return;
        }

        mapObject.SetActive(false);
        Destroy(mapObject);
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
