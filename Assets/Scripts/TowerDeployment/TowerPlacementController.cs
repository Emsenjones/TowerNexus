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
    private TowerPlacementSubmission submission;
    private static readonly IReadOnlyList<TowerBehaviour> NoTowers = System.Array.Empty<TowerBehaviour>();
    private IReadOnlyList<TowerBehaviour> deployedTowers => submission != null ? submission.DeployedTowers : NoTowers;
    internal void BindSubmission(TowerPlacementSubmission owner) { submission = owner; }
    internal void ConfigureSubmission(BattleRuntimeCoordinator runtime, DraftSystem draft)
    {
        submission.Bind(runtime, draft, placementValidator, deployController,
            towerUpgradeSystem, monsterManager, mapGenerator);
    }
    private readonly List<TowerBehaviour> highlightedUpgradeTargets = new List<TowerBehaviour>();
    private DraftResult currentDraftResult;
    private TowerDefinition currentTowerDefinition;
    private TowerUpgradeDefinition currentTowerUpgradeDefinition;
    private GridNodeBehaviour currentTargetNode;
    private TowerBehaviour currentLevelUpTarget;
    private TowerBehaviour currentUpgradeTarget;
    private PendingDraftUIItem currentDraftEntry;
    private bool isDragging;
    private bool isCompletingPlacement;
    public bool CanStartDraftInteraction => !isCompletingPlacement &&
        submission != null && !submission.IsBusy &&
        (battleHUDUI == null || battleHUDUI.DraftOwner == null || !battleHUDUI.DraftOwner.IsPendingMutationBusy) &&
        (towerUpgradeSystem == null || !towerUpgradeSystem.IsApplyingUpgrade);

    private bool isTowerTargetCandidateActive;
    private bool isLevelUpPreviewActive;
    private bool isBattleActive;
    private bool missingCameraWarningLogged;
    private bool missingMapGeneratorWarningLogged;

    public TowerPlacementPreview CurrentPreview => currentPreview;
    public TowerDefinition CurrentTowerDefinition => currentTowerDefinition;
    public GridNodeBehaviour CurrentTargetNode => currentTargetNode;
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
        if (!CanStartDraftInteraction) return;
        if (draftedDraftEntry == null || draftedDraftEntry.TowerDefinition != towerDefinition) return;
        BeginDraftDrag(draftedDraftEntry.DraftResult, draftedDraftEntry);
    }

    public void BeginDraftDrag(DraftResult draftResult, PendingDraftUIItem draftedDraftEntry)
    {
        if (battleHUDUI == null || !battleHUDUI.IsCurrentPendingView(draftedDraftEntry) ||
            draftedDraftEntry.DraftResult != draftResult) return;
        if (!CanStartDraftInteraction) return;
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
        placementValidator?.InvalidatePreviewCache();
        try
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
        }
        finally
        {
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

        submission?.CloseBattleGate();
        mapGenerator = activeMap;
        ApplyMapBinding();
        missingMapGeneratorWarningLogged = false;
        return true;
    }

    public void ClearActiveMap()
    {
        submission?.CloseBattleGate();
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

    public void BeginBattle() { isBattleActive = true; }

    public void CloseBattleGate()
    {
        isBattleActive = false;
        ClearUpgradeTargetHighlights();
        CancelPlacement();
    }

    private void CompletePlacement()
    {
        if (!CanStartDraftInteraction || !submission.TryBeginInteraction(out var lease)) return;
        var view = currentDraftEntry;
        isCompletingPlacement = true;
        try
        {
            TowerSubmissionResult result = CompletePlacementCore(lease);
            if (!string.IsNullOrEmpty(result.FailureReason)) Debug.LogWarning(result.FailureReason, this);
        }
        finally
        {
            try
            {
                // Consumption remains authoritative even if a post-commit observer fails.
                if (battleHUDUI != null) battleHUDUI.ReleaseConsumedPendingDraftView(view);
            }
            finally
            {
                try { CancelPlacement(); }
                finally { isCompletingPlacement = false; lease.Dispose(); }
            }
        }
    }

    private TowerSubmissionResult CompletePlacementCore(TowerPlacementSubmission.Interaction lease)
    {
        if (!isBattleActive || battleHUDUI == null || !battleHUDUI.IsCurrentPendingView(currentDraftEntry) ||
            currentDraftEntry.DraftResult != currentDraftResult)
            return TowerSubmissionResult.Reject("The current Pending view or Battle is unavailable.");
        if (battleHUDUI.IsScreenPositionInsideDraftItemInteractionArea(Input.mousePosition))
            return TowerSubmissionResult.Reject(string.Empty);
        var entry = currentDraftEntry.Entry;
        if (IsTowerUpgradeDraftDrag())
            return submission.SubmitUpgrade(entry, currentUpgradeTarget != null ? currentUpgradeTarget.TowerInstance : null, lease);
        if (isTowerTargetCandidateActive)
            return isLevelUpPreviewActive && currentLevelUpTarget != null
                ? submission.SubmitLevelUp(entry, currentLevelUpTarget.TowerInstance, lease)
                : TowerSubmissionResult.Reject(string.Empty);
        if (!TowerPlacementCandidate.TryCapture(placementValidator, currentPreview, out var candidate, out string reason))
            return TowerSubmissionResult.Reject(reason);
        return submission.SubmitDeployment(entry, candidate, lease);
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

    private bool IsTowerUpgradeDraftDrag()
    {
        return currentDraftResult != null &&
               currentDraftResult.ResultType == DraftResultType.TowerUpgradeDraft;
    }

    private void ShowAttackRangePreviewsForCurrentDrag()
    {

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
                (submission == null || !submission.OwnsDeployedTower(highlightedTarget.TowerInstance)) ||
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

    internal void BindBattleDependencies(MonsterManager monsters, AStarPathfindingService paths)
    {
        monsterManager = monsters;
        pathfindingService = paths;
    }

    private void EnsureStableRuntimeDependencies()
    {
        if (battleHUDUI == null)
        {
            Debug.LogError("Tower placement controller requires an assigned BattleHUDUI reference.", this);
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
