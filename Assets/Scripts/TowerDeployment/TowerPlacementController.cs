using UnityEngine;

public class TowerPlacementController : MonoBehaviour
{
    private Camera placementCamera;
    [SerializeField] private MapGeneratorBehaviour mapGenerator;
    [SerializeField] private Transform previewParent;
    [SerializeField] private TowerPlacementValidator placementValidator;
    [SerializeField] private TowerDeployController deployController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private LayerMask placementRaycastMask = ~0;
    [SerializeField] private float placementRaycastDistance = 500f;

    private TowerPlacementPreview currentPreview;
    private TowerDefinition currentTowerDefinition;
    private GridNodeBehaviour currentTargetNode;
    private PendingTowerItemUI currentDraftedTowerEntry;
    private bool isDragging;
    private bool missingCameraWarningLogged;
    private bool missingMapGeneratorWarningLogged;

    public TowerPlacementPreview CurrentPreview => currentPreview;
    public TowerDefinition CurrentTowerDefinition => currentTowerDefinition;
    public GridNodeBehaviour CurrentTargetNode => currentTargetNode;
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

    public void BeginPlacement(TowerDefinition towerDefinition, PendingTowerItemUI draftedTowerEntry)
    {
        CancelPlacement();
        EnsureRuntimeDependencies();

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
        currentTowerDefinition = towerDefinition;
        currentDraftedTowerEntry = draftedTowerEntry;
        currentTargetNode = null;
        isDragging = true;

        UpdatePreviewPosition(Input.mousePosition);
    }

    public void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview.gameObject);
        }

        currentPreview = null;
        currentTowerDefinition = null;
        currentTargetNode = null;
        currentDraftedTowerEntry = null;
        isDragging = false;
    }

    private void CompletePlacement()
    {
        if (currentPreview != null && deployController != null)
        {
            deployController.TryDeployTower(currentPreview, currentDraftedTowerEntry);
        }

        CancelPlacement();
    }

    private void UpdatePreviewPosition(Vector3 screenPosition)
    {
        if (currentPreview == null)
        {
            CancelPlacement();
            return;
        }

        if (!TryGetWorldPosition(screenPosition, out Vector3 worldPosition))
        {
            currentTargetNode = null;
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
            currentPreview.SetWorldPosition(worldPosition);
            currentPreview.SetPlacementState(false);
            return;
        }

        if (mapGenerator.TryGetNodeByWorldPosition(worldPosition, out GridNodeBehaviour targetNode))
        {
            currentTargetNode = targetNode;
            currentPreview.SetWorldPosition(targetNode.WorldPosition);
            currentPreview.SetPlacementState(
                placementValidator != null &&
                placementValidator.CanPlaceTower(currentPreview, out _)
            );
            return;
        }

        currentTargetNode = null;
        currentPreview.SetWorldPosition(worldPosition);
        currentPreview.SetPlacementState(false);
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
