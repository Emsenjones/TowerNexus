using System.Collections.Generic;
using UnityEngine;

public class TowerPlacementController : MonoBehaviour
{
    private Camera placementCamera;
    [SerializeField] private MapGeneratorBehaviour mapGenerator;
    [SerializeField] private Transform previewParent;
    [SerializeField] private TowerPlacementValidator placementValidator;
    [SerializeField] private TowerDeployController deployController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private LayerMask placementRaycastMask = ~0;
    [SerializeField] private float placementRaycastDistance = 500f;

    private TowerPlacementPreview currentPreview;
    private readonly List<TowerBehaviour> deployedTowers = new List<TowerBehaviour>();
    private readonly List<TowerInstance> deployedTowerInstances = new List<TowerInstance>();
    private DraftResult currentDraftResult;
    private TowerDefinition currentTowerDefinition;
    private TowerUpgradeDefinition currentTowerUpgradeDefinition;
    private GridNodeBehaviour currentTargetNode;
    private TowerBehaviour currentLevelUpTarget;
    private TowerBehaviour currentUpgradeTarget;
    private PendingDraftUI currentDraftEntry;
    private bool isDragging;
    private bool isTowerTargetCandidateActive;
    private bool isLevelUpPreviewActive;
    private bool missingCameraWarningLogged;
    private bool missingMapGeneratorWarningLogged;

    public TowerPlacementPreview CurrentPreview => currentPreview;
    public TowerDefinition CurrentTowerDefinition => currentTowerDefinition;
    public GridNodeBehaviour CurrentTargetNode => currentTargetNode;
    public IReadOnlyList<TowerInstance> DeployedTowerInstances
    {
        get
        {
            RegisterExistingDeployedTowers();
            RebuildDeployedTowerInstances();
            return deployedTowerInstances;
        }
    }
    public bool IsDragging => isDragging;

    private void Awake()
    {
        if (placementCamera == null)
            placementCamera = Camera.main;

        EnsureRuntimeDependencies();
    }

    private void Update()
    {
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

    public void BeginPlacement(TowerDefinition towerDefinition)
    {
        BeginPlacement(towerDefinition, null);
    }

    public void BeginPlacement(TowerDefinition towerDefinition, PendingDraftUI draftedDraftEntry)
    {
        BeginTowerDraftDrag(DraftResult.CreateTowerDraft(towerDefinition), draftedDraftEntry);
    }

    public void BeginDraftDrag(DraftResult draftResult, PendingDraftUI draftedDraftEntry)
    {
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

    private void BeginTowerDraftDrag(DraftResult draftResult, PendingDraftUI draftedDraftEntry)
    {
        CancelPlacement();
        EnsureRuntimeDependencies();
        RegisterExistingDeployedTowers();

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

    private void BeginTowerUpgradeDrag(DraftResult draftResult, PendingDraftUI draftedDraftEntry)
    {
        CancelPlacement();
        EnsureRuntimeDependencies();
        RegisterExistingDeployedTowers();

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

        UpdatePreviewPosition(Input.mousePosition);
    }

    public void CancelPlacement()
    {
        HideAttackRangePreviewsForCurrentDrag();

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

    private void CompletePlacement()
    {
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

        if (currentPreview != null && deployController != null)
        {
            if (deployController.TryDeployTower(currentPreview, currentDraftEntry, out TowerBehaviour deployedTower))
            {
                RegisterDeployedTower(deployedTower);
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

        currentLevelUpTarget.RefreshTowerVisual();

        if (battleHUDUI != null && currentDraftEntry != null)
        {
            battleHUDUI.RemovePendingDraft(currentDraftEntry);
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

        if (battleHUDUI != null && currentDraftEntry != null)
        {
            battleHUDUI.RemovePendingDraft(currentDraftEntry);
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
                placementValidator.CanPlaceTower(currentPreview, out _)
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

    private void RegisterDeployedTower(TowerBehaviour tower)
    {
        if (tower == null ||
            tower.TowerInstance == null ||
            tower.TowerInstance.OccupiedNodes.Count == 0 ||
            deployedTowers.Contains(tower))
        {
            return;
        }

        deployedTowers.Add(tower);
    }

    private void RegisterExistingDeployedTowers()
    {
        RemoveNullDeployedTowerEntries();

        TowerBehaviour[] towers = FindObjectsByType<TowerBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < towers.Length; i++)
        {
            RegisterDeployedTower(towers[i]);
        }
    }

    private void RemoveNullDeployedTowerEntries()
    {
        for (int i = deployedTowers.Count - 1; i >= 0; i--)
        {
            if (deployedTowers[i] == null || deployedTowers[i].TowerInstance == null)
            {
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
        RegisterExistingDeployedTowers();

        for (int i = 0; i < deployedTowers.Count; i++)
        {
            ShowAttackRangePreview(deployedTowers[i]);
        }

        if (currentPreview != null)
        {
            ShowAttackRangePreview(currentPreview.VisualController, currentPreview.TowerDefinition);
        }
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
        if (tower == null)
        {
            return;
        }

        ShowAttackRangePreview(tower.VisualController, tower.TowerInstance != null ? tower.TowerInstance.TowerDefinition : null);
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

        if (towerDefinition == null || towerDefinition.AttackConfig == null)
        {
            return false;
        }

        attackRange = towerDefinition.AttackConfig.AttackRange;
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

    private void EnsureRuntimeDependencies()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGeneratorBehaviour>();
        }

        if (battleHUDUI == null)
        {
            battleHUDUI = FindFirstObjectByType<BattleHUDUI>();
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

        placementValidator.Initialize(mapGenerator, pathfindingService, monsterManager);

        if (deployController == null)
        {
            deployController = GetComponent<TowerDeployController>();
        }

        if (deployController == null)
        {
            deployController = gameObject.AddComponent<TowerDeployController>();
        }

        deployController.Initialize(placementValidator, mapGenerator, battleHUDUI, monsterManager);
    }
}
