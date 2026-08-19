using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TowerPlacementController : MonoBehaviour
{
    [SerializeField] private Camera placementCamera;
    [SerializeField] private Transform previewParent;
    [SerializeField] private TowerPlacementValidator placementValidator;
    [SerializeField] private TowerDeployController deployController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private LayerMask placementRaycastMask = ~0;
    [SerializeField] private float placementRaycastDistance = 500f;

    private MapGeneratorBehaviour mapGenerator;
    private TowerPlacementPreview currentPreview;
    private readonly List<TowerBehaviour> deployedTowers = new List<TowerBehaviour>();
    private readonly List<TowerInstance> deployedTowerInstances = new List<TowerInstance>();
    private readonly List<TowerBehaviour> highlightedUpgradeTargets = new List<TowerBehaviour>();
    private DraftResult currentDraftResult;
    private TowerDefinition currentTowerDefinition;
    private TowerUpgradeDefinition currentTowerUpgradeDefinition;
    private GridNodeBehaviour currentTargetNode;
    private TowerBehaviour currentLevelUpTarget;
    private TowerBehaviour currentUpgradeTarget;
    private PendingDraftUIItem currentDraftEntry;
    private bool isDragging;
    private bool isTowerTargetCandidateActive;
    private bool isLevelUpPreviewActive;
    private bool isBattleActive;
    private bool missingCameraWarningLogged;
    private bool missingMapGeneratorWarningLogged;

    public TowerPlacementPreview CurrentPreview => currentPreview;
    public TowerDefinition CurrentTowerDefinition => currentTowerDefinition;
    public GridNodeBehaviour CurrentTargetNode => currentTargetNode;
    public IReadOnlyList<TowerInstance> DeployedTowerInstances
    {
        get
        {
            RebuildDeployedTowerInstances();
            return deployedTowerInstances;
        }
    }
    public bool IsDragging => isDragging;
    public bool IsBattleActive => isBattleActive;
    public MapGeneratorBehaviour ActiveMap => mapGenerator;
    public Camera PlacementCamera => placementCamera;

    private void Awake()
    {
        EnsureStableRuntimeDependencies();
    }

    private void Update()
    {
        if (!isBattleActive)
        {
            if (isDragging)
            {
                CancelPlacement();
            }

            return;
        }

        if (!isDragging)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        UpdatePreviewPosition(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
        {
            CompletePlacement();
        }
    }

    public void BeginPlacement(TowerDefinition towerDefinition, PendingDraftUIItem draftedDraftEntry)
    {
        BeginTowerDraftDrag(DraftResult.CreateTowerDraft(towerDefinition), draftedDraftEntry);
    }

    public void BeginDraftDrag(DraftResult draftResult, PendingDraftUIItem draftedDraftEntry)
    {
        if (!isBattleActive)
        {
            draftedDraftEntry?.RestorePendingPosition();
            return;
        }

        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Tower placement controller cannot begin draft drag: draft result is invalid.", this);
            return;
        }

        switch (draftResult.ResultType)
        {
            case DraftResultType.TowerDraft:
                BeginTowerDraftDrag(draftResult, draftedDraftEntry);
                break;
            case DraftResultType.TowerUpgradeDraft:
                BeginTowerUpgradeDrag(draftResult, draftedDraftEntry);
                break;
            default:
                Debug.LogWarning($"Tower placement controller cannot begin draft drag: unsupported draft result type '{draftResult.ResultType}'.", this);
                break;
        }
    }

    private void BeginTowerDraftDrag(DraftResult draftResult, PendingDraftUIItem draftedDraftEntry)
    {
        CancelPlacement();
        EnsureStableRuntimeDependencies();

        TowerDefinition towerDefinition = draftResult != null ? draftResult.TowerDefinition : null;

        if (towerDefinition == null)
        {
            Debug.LogWarning("Tower placement controller cannot begin placement: tower definition is null.", this);
            return;
        }

        if (towerDefinition.TowerPrefab == null)
        {
            Debug.LogWarning("Tower placement controller cannot begin placement: tower prefab is not assigned.", towerDefinition);
            return;
        }

        if (!towerDefinition.TowerPrefab.TryGetComponent(out TowerAnchorSet anchorSet))
        {
            Debug.LogWarning($"Tower placement controller cannot begin placement: tower prefab '{towerDefinition.TowerPrefab.name}' is missing TowerAnchorSet.", towerDefinition.TowerPrefab);
            return;
        }

        if (anchorSet.CenterAnchor == null)
        {
            Debug.LogWarning($"Tower placement controller cannot begin placement: tower prefab '{towerDefinition.TowerPrefab.name}' is missing a center anchor.", towerDefinition.TowerPrefab);
            return;
        }

        GameObject previewObject = Instantiate(towerDefinition.TowerPrefab, previewParent);

        if (!previewObject.TryGetComponent(out currentPreview))
        {
            currentPreview = previewObject.AddComponent<TowerPlacementPreview>();
        }

        currentPreview.Initialize(towerDefinition);
        currentDraftResult = draftResult;
        currentTowerDefinition = towerDefinition;
        currentTowerUpgradeDefinition = null;
        currentDraftEntry = draftedDraftEntry;
        currentTargetNode = null;
        isDragging = true;

        ShowAttackRangePreviewsForCurrentDrag();
        UpdatePreviewPosition(Input.mousePosition);
    }

    private void BeginTowerUpgradeDrag(DraftResult draftResult, PendingDraftUIItem draftedDraftEntry)
    {
        CancelPlacement();
        EnsureStableRuntimeDependencies();

        TowerUpgradeDefinition upgradeDefinition = draftResult != null ? draftResult.TowerUpgradeDefinition : null;

        if (upgradeDefinition == null)
        {
            Debug.LogWarning("Tower placement controller cannot begin upgrade drag: upgrade definition is null.", this);
            return;
        }

        currentDraftResult = draftResult;
        currentTowerDefinition = null;
        currentTowerUpgradeDefinition = upgradeDefinition;
        currentDraftEntry = draftedDraftEntry;
        currentTargetNode = null;
        currentUpgradeTarget = null;
        isDragging = true;

        RefreshUpgradeTargetHighlights();
        UpdatePreviewPosition(Input.mousePosition);
    }

    public void CancelPlacement()
    {
        HideAttackRangePreviewsForCurrentDrag();
        ClearUpgradeTargetHighlights();

        if (currentDraftEntry != null)
        {
            currentDraftEntry.RestorePendingPosition();
        }

        if (currentPreview != null)
        {
            Destroy(currentPreview.gameObject);
        }

        currentPreview = null;
        currentDraftResult = null;
        currentTowerDefinition = null;
        currentTowerUpgradeDefinition = null;
        currentTargetNode = null;
        currentLevelUpTarget = null;
        currentUpgradeTarget = null;
        currentDraftEntry = null;
        isDragging = false;
        isTowerTargetCandidateActive = false;
        isLevelUpPreviewActive = false;
    }

    public bool BindActiveMap(MapGeneratorBehaviour activeMap)
    {
        if (activeMap == null)
        {
            Debug.LogError(
                "Tower placement controller cannot bind a null Active Map.",
                this);
            return false;
        }

        EnsureStableRuntimeDependencies();

        if (pathfindingService == null || pathfindingService.ActiveMap != activeMap)
        {
            Debug.LogError(
                "Tower placement controller cannot bind an Active Map that differs " +
                "from the A* pathfinding binding.",
                this);
            return false;
        }

        mapGenerator = activeMap;
        ApplyMapBinding();
        missingMapGeneratorWarningLogged = false;
        return true;
    }

    public void ClearActiveMap()
    {
        CloseBattleGate();
        mapGenerator = null;
        ApplyMapBinding();
        missingMapGeneratorWarningLogged = false;
    }

    public bool CanBeginBattle(out string failureReason)
    {
        EnsureStableRuntimeDependencies();

        if (placementCamera == null)
        {
            failureReason = "Placement Camera is not assigned.";
            return false;
        }

        if (mapGenerator == null)
        {
            failureReason = "Active Map is not bound.";
            return false;
        }

        if (pathfindingService == null ||
            pathfindingService.ActiveMap != mapGenerator)
        {
            failureReason = "A* pathfinding is not bound to the same Active Map.";
            return false;
        }

        if (monsterManager == null)
        {
            failureReason = "Monster Manager is not assigned.";
            return false;
        }

        if (battleHUDUI == null)
        {
            failureReason = "Battle HUD UI is not assigned.";
            return false;
        }

        if (towerUpgradeSystem == null)
        {
            failureReason = "Tower Upgrade System is not assigned.";
            return false;
        }

        if (placementValidator == null ||
            !placementValidator.IsConfiguredFor(
                mapGenerator,
                pathfindingService))
        {
            failureReason = "Tower Placement Validator is not bound to Stage dependencies.";
            return false;
        }

        if (deployController == null ||
            !deployController.IsConfiguredFor(monsterManager))
        {
            failureReason = "Tower Deploy Controller is not bound to Stage dependencies.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void BeginBattle()
    {
        isBattleActive = true;
        RemoveNullDeployedTowerEntries();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            TowerBehaviour tower = deployedTowers[i];
            TowerCombatBehaviour combatBehaviour =
                tower != null ? tower.GetComponent<TowerCombatBehaviour>() : null;
            combatBehaviour?.BeginBattle();
        }
    }

    public void CloseBattleGate()
    {
        isBattleActive = false;
        CancelPlacement();
    }

    public void StopTrackedTowerCombat()
    {
        RemoveNullDeployedTowerEntries();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            TowerBehaviour tower = deployedTowers[i];
            TowerCombatBehaviour combatBehaviour =
                tower != null ? tower.GetComponent<TowerCombatBehaviour>() : null;
            combatBehaviour?.StopBattle();
        }
    }

    public void DestroyTrackedTowers()
    {
        CloseBattleGate();
        ClearUpgradeTargetHighlights();
        StopTrackedTowerCombat();

        for (int i = deployedTowers.Count - 1; i >= 0; i--)
        {
            TowerBehaviour tower = deployedTowers[i];

            if (tower == null)
            {
                continue;
            }

            tower.gameObject.SetActive(false);
            Destroy(tower.gameObject);
        }

        deployedTowers.Clear();
        deployedTowerInstances.Clear();
    }

    public void StopBattle()
    {
        CloseBattleGate();
        StopTrackedTowerCombat();
    }

    private void CompletePlacement()
    {
        if (!isBattleActive)
        {
            CancelPlacement();
            return;
        }

        if (battleHUDUI != null &&
            battleHUDUI.IsScreenPositionInsideDraftItemInteractionArea(Input.mousePosition))
        {
            DragCancelCurrentOperation();
            return;
        }

        if (IsTowerUpgradeDraftDrag())
        {
            CompleteTowerUpgrade();
            CancelPlacement();
            return;
        }

        if (isTowerTargetCandidateActive)
        {
            if (isLevelUpPreviewActive && currentLevelUpTarget != null)
            {
                CompleteLevelUp();
            }

            CancelPlacement();
            return;
        }

        if (currentPreview != null)
        {
            if (deployController == null)
            {
                LogPlacementRejected(
                    "TowerReadiness",
                    "Tower Deploy Controller is not assigned.");
            }
            else
            {
                TryCommitNewTowerPlacement();
            }
        }

        CancelPlacement();
    }

    private void DragCancelCurrentOperation()
    {
        CancelPlacement();
    }

    private void CompleteLevelUp()
    {
        if (towerUpgradeSystem == null ||
            currentLevelUpTarget == null ||
            currentLevelUpTarget.TowerInstance == null ||
            currentTowerDefinition == null)
        {
            return;
        }

        if (!towerUpgradeSystem.TryLevelUpTower(
                currentLevelUpTarget.TowerInstance,
                currentTowerDefinition,
                out _))
        {
            return;
        }

        bool didRefreshVisual = currentLevelUpTarget.RefreshTowerVisual();

        if (didRefreshVisual && currentLevelUpTarget.VisualController != null)
        {
            currentLevelUpTarget.VisualController.PlayTowerSpawnRefreshFeedback();
        }

        if (battleHUDUI != null && currentDraftEntry != null)
        {
            battleHUDUI.RemovePendingDraft(currentDraftEntry);
            currentDraftEntry = null;
        }
    }

    private void CompleteTowerUpgrade()
    {
        if (towerUpgradeSystem == null ||
            currentUpgradeTarget == null ||
            currentUpgradeTarget.TowerInstance == null ||
            currentTowerUpgradeDefinition == null)
        {
            return;
        }

        if (!towerUpgradeSystem.TryApplyUpgrade(
                currentUpgradeTarget.TowerInstance,
                currentTowerUpgradeDefinition,
                out string failureReason))
        {
            Debug.LogWarning(
                $"Tower placement controller failed to apply upgrade '{currentTowerUpgradeDefinition.name}' to '{currentUpgradeTarget.name}': {failureReason}",
                this);
            return;
        }

        if (currentUpgradeTarget.VisualController != null)
        {
            currentUpgradeTarget.VisualController.PlayUpgradeAppliedFeedback();
        }

        if (battleHUDUI != null && currentDraftEntry != null)
        {
            battleHUDUI.RemovePendingDraft(currentDraftEntry);
            currentDraftEntry = null;
        }
    }

    private void UpdatePreviewPosition(Vector3 screenPosition)
    {
        if (IsTowerUpgradeDraftDrag())
        {
            UpdateUpgradeTargetPosition(screenPosition);
            return;
        }

        if (currentPreview == null)
        {
            CancelPlacement();
            return;
        }

        if (!TryGetWorldPosition(screenPosition, out Vector3 worldPosition))
        {
            currentTargetNode = null;
            ClearLevelUpPreviewState(true);
            currentPreview.SetPlacementState(false);
            return;
        }

        if (mapGenerator == null)
        {
            if (!missingMapGeneratorWarningLogged)
            {
                Debug.LogWarning("Tower placement controller cannot snap preview: map generator is not assigned.", this);
                missingMapGeneratorWarningLogged = true;
            }

            currentTargetNode = null;
            ClearLevelUpPreviewState(true);
            currentPreview.SetWorldPosition(worldPosition);
            currentPreview.SetPlacementState(false);
            return;
        }

        if (mapGenerator.TryGetNodeByWorldPosition(worldPosition, out GridNodeBehaviour targetNode))
        {
            currentTargetNode = targetNode;
            currentPreview.SetWorldPosition(targetNode.WorldPosition);

            if (TryUpdateLevelUpPreview(targetNode))
            {
                return;
            }

            currentPreview.SetPlacementState(
                placementValidator != null &&
                placementValidator.CanPlaceTower(currentPreview)
            );
            return;
        }

        currentTargetNode = null;
        ClearLevelUpPreviewState(true);
        currentPreview.SetWorldPosition(worldPosition);
        currentPreview.SetPlacementState(false);
    }

    private void UpdateUpgradeTargetPosition(Vector3 screenPosition)
    {
        currentUpgradeTarget = null;
        RefreshUpgradeTargetHighlights();

        if (!TryGetWorldPosition(screenPosition, out Vector3 worldPosition))
        {
            currentTargetNode = null;
            return;
        }

        if (mapGenerator == null)
        {
            if (!missingMapGeneratorWarningLogged)
            {
                Debug.LogWarning("Tower placement controller cannot resolve upgrade target: map generator is not assigned.", this);
                missingMapGeneratorWarningLogged = true;
            }

            currentTargetNode = null;
            return;
        }

        if (!mapGenerator.TryGetNodeByWorldPosition(worldPosition, out GridNodeBehaviour targetNode))
        {
            currentTargetNode = null;
            return;
        }

        currentTargetNode = targetNode;
        currentUpgradeTarget = FindTowerOccupyingNode(targetNode);
    }

    private bool TryUpdateLevelUpPreview(GridNodeBehaviour targetNode)
    {
        TowerBehaviour hoveredTower = FindTowerOccupyingNode(targetNode);

        if (hoveredTower == null)
        {
            ClearLevelUpPreviewState(true);
            return false;
        }

        isTowerTargetCandidateActive = true;

        if (towerUpgradeSystem == null ||
            hoveredTower.TowerInstance == null ||
            !towerUpgradeSystem.CanLevelUpTower(
                hoveredTower.TowerInstance,
                currentTowerDefinition,
                out int nextLevel))
        {
            ClearLevelUpPreviewState(true);
            isTowerTargetCandidateActive = true;
            currentPreview.SetPlacementState(false);
            return true;
        }

        TowerAnchorSet targetAnchorSet = hoveredTower.GetComponent<TowerAnchorSet>();

        if (targetAnchorSet == null || targetAnchorSet.CenterAnchor == null)
        {
            ClearLevelUpPreviewState(true);
            isTowerTargetCandidateActive = true;
            currentPreview.SetPlacementState(false);
            return true;
        }

        currentPreview.SetWorldPosition(targetAnchorSet.CenterAnchor.position);
        currentPreview.SetPlacementState(true);

        if (!currentPreview.SetPreviewLevel(nextLevel))
        {
            ClearLevelUpPreviewState(false);
            isTowerTargetCandidateActive = true;
            currentPreview.SetPlacementState(false);
            return true;
        }

        currentLevelUpTarget = hoveredTower;
        isLevelUpPreviewActive = true;
        return true;
    }

    private TowerBehaviour FindTowerOccupyingNode(GridNodeBehaviour targetNode)
    {
        if (targetNode == null)
        {
            return null;
        }

        RemoveNullDeployedTowerEntries();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            TowerBehaviour tower = deployedTowers[i];

            if (tower == null || tower.TowerInstance == null)
            {
                continue;
            }

            IReadOnlyList<GridNodeBehaviour> occupiedNodes = tower.TowerInstance.OccupiedNodes;

            for (int j = 0; j < occupiedNodes.Count; j++)
            {
                if (occupiedNodes[j] == targetNode)
                {
                    return tower;
                }
            }
        }

        return null;
    }

    private void ClearLevelUpPreviewState(bool resetPreviewLevel)
    {
        if (resetPreviewLevel && isLevelUpPreviewActive && currentPreview != null)
        {
            currentPreview.ResetPreviewLevel();
        }

        currentLevelUpTarget = null;
        isTowerTargetCandidateActive = false;
        isLevelUpPreviewActive = false;
    }

    private bool TryCommitNewTowerPlacement()
    {
        string topologyFailureReason = placementValidator == null
            ? "Tower Placement Validator is not assigned."
            : string.Empty;
        IReadOnlyList<GridNodeBehaviour> diagnosticFootprint = null;
        bool? routeExists = null;

        if (placementValidator == null ||
            !placementValidator.TryCreateTopologyPlan(
                currentPreview,
                out TowerPlacementTopologyPlan topologyPlan,
                out diagnosticFootprint,
                out routeExists,
                out topologyFailureReason))
        {
            LogPlacementRejected(
                "TopologyPlan",
                topologyFailureReason,
                diagnosticFootprint,
                routeExists);
            return false;
        }

        if (!TryValidateHeldTowerDraft(out string draftFailureReason))
        {
            LogPlacementRejected(
                "DraftPreflight",
                draftFailureReason,
                topologyPlan.Footprint,
                routeExists: true);
            return false;
        }

        string revisionFailureReason = monsterManager == null
            ? "Monster Manager is not assigned."
            : string.Empty;

        if (monsterManager == null ||
            !monsterManager.TryPrepareTopologyRevision(
                topologyPlan,
                out MonsterRouteRevisionBatch revisionBatch,
                out revisionFailureReason))
        {
            LogPlacementRejected(
                "MonsterRevisionPreparation",
                revisionFailureReason,
                topologyPlan.Footprint,
                routeExists: true);
            return false;
        }

        if (!deployController.TryPrepareTower(
                currentPreview,
                topologyPlan,
                out TowerBehaviour preparedTower,
                out string towerFailureReason))
        {
            LogPlacementRejected(
                "TowerReadiness",
                towerFailureReason,
                topologyPlan.Footprint,
                routeExists: true);
            return false;
        }

        if (!TryValidatePreparedTowerForCommit(
                preparedTower,
                topologyPlan,
                out TowerCombatBehaviour preparedCombat,
                out string commitFailureReason))
        {
            DiscardPreparedTower(preparedTower);
            LogPlacementRejected(
                "CommitPreflight",
                commitFailureReason,
                topologyPlan.Footprint,
                routeExists: true);
            return false;
        }

        PendingDraftUIItem consumedDraft = currentDraftEntry;
        CommitPreparedPlacement(
            topologyPlan,
            revisionBatch,
            preparedTower,
            preparedCombat,
            consumedDraft);
        currentDraftEntry = null;

        bool hasPresentationWarning =
            !TryRunPlacementPresentation(preparedTower);
        LogPlacementAccepted(
            topologyPlan,
            revisionBatch,
            hasPresentationWarning
                ? "AcceptedWithPresentationWarning"
                : "Accepted");
        return true;
    }

    private bool TryValidateHeldTowerDraft(out string failureReason)
    {
        if (battleHUDUI == null)
        {
            failureReason = "Battle HUD UI is not assigned.";
            return false;
        }

        if (currentDraftEntry == null ||
            !battleHUDUI.OwnsPendingDraft(currentDraftEntry))
        {
            failureReason =
                "the exact held Tower Draft is not owned by the active " +
                "pending-item collection.";
            return false;
        }

        if (currentDraftResult == null ||
            !currentDraftResult.IsValid ||
            currentDraftResult.ResultType != DraftResultType.TowerDraft ||
            currentDraftEntry.DraftResult != currentDraftResult ||
            currentDraftEntry.TowerDefinition != currentTowerDefinition ||
            currentPreview == null ||
            currentPreview.TowerDefinition != currentTowerDefinition)
        {
            failureReason =
                "the held Tower Draft identity does not match the active " +
                "placement preview.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private bool TryValidatePreparedTowerForCommit(
        TowerBehaviour preparedTower,
        TowerPlacementTopologyPlan topologyPlan,
        out TowerCombatBehaviour preparedCombat,
        out string failureReason)
    {
        preparedCombat = null;

        if (preparedTower == null ||
            preparedTower.TowerInstance == null ||
            preparedTower.VisualController == null ||
            preparedTower.VisualController.CurrentTowerModelInstance == null)
        {
            failureReason = "the prepared Tower is missing required runtime state.";
            return false;
        }

        IReadOnlyList<GridNodeBehaviour> preparedFootprint =
            preparedTower.TowerInstance.OccupiedNodes;

        if (preparedFootprint.Count != topologyPlan.Footprint.Count)
        {
            failureReason =
                "the prepared Tower footprint differs from the topology plan.";
            return false;
        }

        for (int i = 0; i < topologyPlan.Footprint.Count; i++)
        {
            if (!ContainsNode(preparedFootprint, topologyPlan.Footprint[i]))
            {
                failureReason =
                    "the prepared Tower footprint differs from the topology plan.";
                return false;
            }
        }

        if (deployedTowers.Contains(preparedTower) ||
            !preparedTower.TryGetComponent(out preparedCombat) ||
            !preparedCombat.IsPreparedForBattleActivation)
        {
            failureReason =
                "the prepared Tower combat runtime is not ready for activation.";
            preparedCombat = null;
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private void CommitPreparedPlacement(
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch,
        TowerBehaviour preparedTower,
        TowerCombatBehaviour preparedCombat,
        PendingDraftUIItem consumedDraft)
    {
        for (int i = 0; i < topologyPlan.Footprint.Count; i++)
        {
            topologyPlan.Footprint[i].SetRuntimeOccupied(true);
        }

        monsterManager.ApplyPreparedMovementRevisionBatch(revisionBatch);
        deployedTowers.Add(preparedTower);
        preparedCombat.ActivatePreparedBattleRuntime();
        battleHUDUI.ConsumePendingDraft(consumedDraft);
    }

    private bool TryRunPlacementPresentation(TowerBehaviour deployedTower)
    {
        bool succeeded = true;

        try
        {
            if (mapGenerator == null ||
                !mapGenerator.RefreshRuntimeTileVisuals())
            {
                succeeded = false;
            }
        }
        catch (System.Exception exception)
        {
            succeeded = false;
            Debug.LogException(exception, this);
        }

        try
        {
            deployedTower.VisualController?.PlayTowerSpawnRefreshFeedback();
        }
        catch (System.Exception exception)
        {
            succeeded = false;
            Debug.LogException(exception, this);
        }

        return succeeded;
    }

    private void LogPlacementRejected(
        string failureStage,
        string failureReason,
        IReadOnlyList<GridNodeBehaviour> footprint = null,
        bool? routeExists = null)
    {
        Debug.LogWarning(
            $"Tower placement transaction: Outcome=Rejected; " +
            $"FootprintGridPositions={FormatGridPositions(footprint)}; " +
            $"RouteExists={FormatRouteExists(routeExists)}; " +
            $"FailureStage={failureStage}; FailureReason={failureReason}",
            this);
    }

    private void LogPlacementAccepted(
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch,
        string outcome)
    {
        Debug.Log(
            $"Tower placement transaction: Outcome={outcome}; " +
            $"FootprintNodes={topologyPlan.Footprint.Count}; " +
            $"FootprintGridPositions=" +
            $"{FormatGridPositions(topologyPlan.Footprint)}; " +
            $"RouteExists=True; " +
            $"AuthoritativeRouteNodes={topologyPlan.AuthoritativeRoute.Count}; " +
            $"AffectedMonsters={revisionBatch.AffectedMonsterCount}",
            this);

        for (int i = 0; i < revisionBatch.Entries.Count; i++)
        {
            MonsterRouteRevisionEntry entry = revisionBatch.Entries[i];
            string progressDirection =
                !entry.HasComparableRemainingDistance
                    ? "Uncompared"
                    : entry.RemainingCenterlineDistanceDelta < 0f
                        ? "Forward"
                        : entry.RemainingCenterlineDistanceDelta > 0f
                            ? "Backward"
                            : "Unchanged";

            Debug.Log(
                $"Tower placement Monster revision: " +
                $"Monster={entry.Monster.name}; " +
                $"PrePosition={entry.PrePlacementWorldPosition}; " +
                $"ProjectionGrid={entry.ReachedNode.GridPosition}; " +
                $"Displacement={entry.WorldDisplacementDistance}; " +
                $"RemainingDistanceDelta=" +
                $"{entry.RemainingCenterlineDistanceDelta}; " +
                $"ProgressDirection={progressDirection}; " +
                $"UsedFallback={entry.UsedDeterministicFallback}",
                entry.Monster);
        }
    }

    private static string FormatGridPositions(
        IReadOnlyList<GridNodeBehaviour> footprint)
    {
        if (footprint == null)
        {
            return "Unresolved";
        }

        StringBuilder builder = new StringBuilder("[");

        for (int i = 0; i < footprint.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            GridNodeBehaviour node = footprint[i];

            if (node == null)
            {
                builder.Append("Null");
                continue;
            }

            Vector2Int gridPosition = node.GridPosition;
            builder.Append('(');
            builder.Append(gridPosition.x);
            builder.Append(',');
            builder.Append(gridPosition.y);
            builder.Append(')');
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string FormatRouteExists(bool? routeExists)
    {
        return routeExists.HasValue
            ? routeExists.Value ? "True" : "False"
            : "Unresolved";
    }

    private void DiscardPreparedTower(TowerBehaviour preparedTower)
    {
        if (preparedTower == null)
        {
            return;
        }

        preparedTower.gameObject.SetActive(false);
        Destroy(preparedTower.gameObject);
    }

    private static bool ContainsNode(
        IReadOnlyList<GridNodeBehaviour> nodes,
        GridNodeBehaviour targetNode)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == targetNode)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveNullDeployedTowerEntries()
    {
        for (int i = deployedTowers.Count - 1; i >= 0; i--)
        {
            if (deployedTowers[i] == null || deployedTowers[i].TowerInstance == null)
            {
                ClearUpgradeTargetHighlight(deployedTowers[i]);
                deployedTowers.RemoveAt(i);
            }
        }
    }

    private void RebuildDeployedTowerInstances()
    {
        deployedTowerInstances.Clear();
        RemoveNullDeployedTowerEntries();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            TowerBehaviour tower = deployedTowers[i];

            if (tower != null && tower.TowerInstance != null)
            {
                deployedTowerInstances.Add(tower.TowerInstance);
            }
        }
    }

    private bool IsTowerUpgradeDraftDrag()
    {
        return currentDraftResult != null &&
               currentDraftResult.ResultType == DraftResultType.TowerUpgradeDraft;
    }

    private void ShowAttackRangePreviewsForCurrentDrag()
    {
        RemoveNullDeployedTowerEntries();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            ShowAttackRangePreview(deployedTowers[i]);
        }

        if (currentPreview != null)
        {
            ShowAttackRangePreview(currentPreview.VisualController, currentPreview.TowerDefinition);
        }
    }

    private void RefreshUpgradeTargetHighlights()
    {
        if (!IsTowerUpgradeDraftDrag() ||
            currentTowerUpgradeDefinition == null ||
            towerUpgradeSystem == null)
        {
            ClearUpgradeTargetHighlights();
            return;
        }

        RemoveNullDeployedTowerEntries();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            TowerBehaviour tower = deployedTowers[i];

            if (tower == null || tower.TowerInstance == null)
            {
                continue;
            }

            bool canApplyUpgrade = towerUpgradeSystem.CanApplyUpgrade(
                tower.TowerInstance,
                currentTowerUpgradeDefinition,
                out _
            );

            SetUpgradeTargetHighlight(tower, canApplyUpgrade);
        }

        for (int i = highlightedUpgradeTargets.Count - 1; i >= 0; i--)
        {
            TowerBehaviour highlightedTarget = highlightedUpgradeTargets[i];

            if (highlightedTarget == null ||
                highlightedTarget.TowerInstance == null ||
                !deployedTowers.Contains(highlightedTarget) ||
                !towerUpgradeSystem.CanApplyUpgrade(
                    highlightedTarget.TowerInstance,
                    currentTowerUpgradeDefinition,
                    out _))
            {
                ClearUpgradeTargetHighlight(highlightedTarget);
            }
        }
    }

    private void SetUpgradeTargetHighlight(TowerBehaviour tower, bool isHighlighted)
    {
        if (tower == null || tower.VisualController == null)
        {
            return;
        }

        tower.VisualController.SetValidUpgradeTargetHighlight(isHighlighted);

        if (isHighlighted)
        {
            if (!highlightedUpgradeTargets.Contains(tower))
            {
                highlightedUpgradeTargets.Add(tower);
            }
        }
        else
        {
            highlightedUpgradeTargets.Remove(tower);
        }
    }

    private void ClearUpgradeTargetHighlights()
    {
        for (int i = highlightedUpgradeTargets.Count - 1; i >= 0; i--)
        {
            ClearUpgradeTargetHighlight(highlightedUpgradeTargets[i]);
        }

        highlightedUpgradeTargets.Clear();
    }

    private void ClearUpgradeTargetHighlight(TowerBehaviour tower)
    {
        if (tower != null && tower.VisualController != null)
        {
            tower.VisualController.ClearValidUpgradeTargetHighlight();
        }

        highlightedUpgradeTargets.Remove(tower);
    }

    private void HideAttackRangePreviewsForCurrentDrag()
    {
        RemoveNullDeployedTowerEntries();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            TowerBehaviour tower = deployedTowers[i];

            if (tower != null && tower.VisualController != null)
            {
                tower.VisualController.HideAttackRangePreview();
            }
        }

        if (currentPreview != null && currentPreview.VisualController != null)
        {
            currentPreview.VisualController.HideAttackRangePreview();
        }
    }

    private void ShowAttackRangePreview(TowerBehaviour tower)
    {
        if (tower == null || tower.VisualController == null)
        {
            return;
        }

        TowerCombatBehaviour combatBehaviour = tower.GetComponent<TowerCombatBehaviour>();

        if (combatBehaviour == null)
        {
            return;
        }

        float resolvedAttackRange = combatBehaviour.CurrentResolvedAttackRange;

        if (resolvedAttackRange > 0f)
        {
            tower.VisualController.ShowAttackRangePreview(resolvedAttackRange);
        }
    }

    private void ShowAttackRangePreview(TowerVisualController visualController, TowerDefinition towerDefinition)
    {
        if (visualController == null || !TryGetAttackRange(towerDefinition, out float attackRange))
        {
            return;
        }

        visualController.ShowAttackRangePreview(attackRange);
    }

    private bool TryGetAttackRange(TowerDefinition towerDefinition, out float attackRange)
    {
        attackRange = 0f;

        if (towerDefinition == null ||
            !towerDefinition.TryGetCombatBehaviour(
                out TowerCombatBehaviour combatBehaviour,
                out _))
        {
            return false;
        }

        attackRange = combatBehaviour.BaseAttackRange;
        return attackRange > 0f;
    }

    private bool TryGetWorldPosition(Vector3 screenPosition, out Vector3 worldPosition)
    {
        worldPosition = default;

        if (placementCamera == null)
        {
            if (!missingCameraWarningLogged)
            {
                Debug.LogWarning("Tower placement controller cannot update preview: placement camera is not assigned.", this);
                missingCameraWarningLogged = true;
            }

            return false;
        }

        Ray ray = placementCamera.ScreenPointToRay(screenPosition);

        if (TryGetPlacementRaycastHit(ray, out RaycastHit hit))
        {
            worldPosition = hit.point;
            return true;
        }

        float fallbackGroundHeight = mapGenerator != null ? mapGenerator.transform.position.y : 0f;
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, fallbackGroundHeight, 0f));

        if (!groundPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        worldPosition = ray.GetPoint(enter);
        return true;
    }

    private bool TryGetPlacementRaycastHit(Ray ray, out RaycastHit placementHit)
    {
        placementHit = default;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            placementRaycastDistance,
            placementRaycastMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (IsCurrentPreviewHit(hit))
            {
                continue;
            }

            placementHit = hit;
            return true;
        }

        return false;
    }

    private bool IsCurrentPreviewHit(RaycastHit hit)
    {
        return currentPreview != null &&
               hit.collider != null &&
               hit.collider.transform.IsChildOf(currentPreview.transform);
    }

    private void EnsureStableRuntimeDependencies()
    {
        if (battleHUDUI == null)
        {
            Debug.LogError("Tower placement controller requires an assigned BattleHUDUI reference.", this);
        }

        if (pathfindingService == null)
        {
            pathfindingService = FindFirstObjectByType<AStarPathfindingService>();
        }

        if (monsterManager == null)
        {
            monsterManager = FindFirstObjectByType<MonsterManager>();
        }

        if (towerUpgradeSystem == null)
        {
            towerUpgradeSystem = GetComponent<TowerUpgradeSystem>();
        }

        if (towerUpgradeSystem == null)
        {
            towerUpgradeSystem = gameObject.AddComponent<TowerUpgradeSystem>();
        }

        if (placementValidator == null)
        {
            placementValidator = GetComponent<TowerPlacementValidator>();
        }

        if (placementValidator == null)
        {
            placementValidator = gameObject.AddComponent<TowerPlacementValidator>();
        }

        if (deployController == null)
        {
            deployController = GetComponent<TowerDeployController>();
        }

        if (deployController == null)
        {
            deployController = gameObject.AddComponent<TowerDeployController>();
        }

        ApplyMapBinding();
    }

    private void ApplyMapBinding()
    {
        if (placementValidator != null)
        {
            placementValidator.Initialize(mapGenerator, pathfindingService);
        }

        if (deployController == null)
        {
            return;
        }

        deployController.Initialize(monsterManager);
    }
}
