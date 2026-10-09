using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CameraPanController : MonoBehaviour
{
    private enum PointerKind
    {
        None,
        Mouse,
        Touch
    }

    private enum PointerPhase
    {
        None,
        Pressed,
        Moved,
        Released,
        Cancelled
    }

    private struct PrimaryPointerState
    {
        public PointerKind Kind;
        public int Id;
        public Vector2 ScreenPosition;
        public PointerPhase Phase;

        public bool IsActive => Kind != PointerKind.None;
    }

    private const int MousePointerId = -1;
    private const float PositionReconcileTolerance = 0.0001f;

    [Header("Tower Inspection")]
    [SerializeField] private float tapMovementThresholdPixels = 10f;
    [SerializeField] private LayerMask towerSelectionMask = 1 << 13;
    [SerializeField] private float towerSelectionDistance = 500f;
    [Header("Camera Dependencies")]
    [SerializeField] private Camera outputCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera cinemachineGameplayCamera;
    [SerializeField] private CinemachineConfiner3D cinemachineConfiner;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private BattleHUDUI battleHUDUI;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private EventSystem eventSystem;

    private readonly List<RaycastResult> uiRaycastResults =
        new List<RaycastResult>();

    private BattleCombatBinding inspectionBattle;
    private MapGeneratorBehaviour inspectionMap;
    private TowerPlacementSubmission inspectionMembers;
    private BattleModalPauseAuthority inspectionPause;
    private bool isPanning;
    private Vector2 pressPosition;
    private TowerInstance pressedTower;
    private object pressedTowerIdentity;
    private ulong pressInputRevision;
    private bool suppressMouseUntilReleased;

    internal bool HasInspectionDependencies(BattleHUDUI hud, TowerPlacementController placement) =>
        ReferenceEquals(battleHUDUI, hud) && ReferenceEquals(towerPlacementController, placement);
    internal bool TryBindInspection(BattleCombatBinding battle, MapGeneratorBehaviour map,
        TowerPlacementSubmission members, BattleModalPauseAuthority pause, out string reason)
    {
        ClearInspectionBinding();
        reason = "Inspection binding requires the exact staged or committed Map and active Camera references.";
        if (!TryValidateStableReferences(out reason) || battle == null || map == null || members == null || pause == null ||
            (!ReferenceEquals(stagedMap, map) && !ReferenceEquals(activeMap, map)) ||
            !battleHUDUI.HasTowerInspectionBinding(battle, members, pause)) return false;
        inspectionBattle = battle; inspectionMap = map; inspectionMembers = members; inspectionPause = pause;
        battleHUDUI.OnBattlefieldInputInvalidated += CancelPointerGesture;
        towerPlacementController.BindBattlefieldInput(this);
        reason = string.Empty; return true;
    }
    internal bool IsInspectionBindingReady(BattleCombatBinding battle, bool requireCommitted) =>
        isActiveAndEnabled && ReferenceEquals(inspectionBattle, battle) && inspectionMap != null &&
        battleHUDUI != null && battleHUDUI.HasTowerInspectionBinding(battle, inspectionMembers, inspectionPause) &&
        (requireCommitted ? ReferenceEquals(activeMap, inspectionMap) :
            (ReferenceEquals(activeMap, inspectionMap) || ReferenceEquals(stagedMap, inspectionMap)));
    private bool OwnsInspectionBinding => inspectionBattle != null && battleHUDUI != null &&
        battleHUDUI.HasTowerInspectionBinding(inspectionBattle, inspectionMembers, inspectionPause);
    internal void ClearInspectionBinding()
    {
        CancelPointerGesture();
        if (battleHUDUI != null) battleHUDUI.OnBattlefieldInputInvalidated -= CancelPointerGesture;
        inspectionBattle = null; inspectionMap = null; inspectionMembers = null; inspectionPause = null;
    }
    internal void CancelPointerGesture() { EndPanGesture(PointerPhase.Cancelled); }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelPointerGesture(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelPointerGesture(); }

    private MapGeneratorBehaviour stagedMap;
    private MapCameraBoundary stagedBoundary;
    private Transform stagedDefaultPose;
    private MapGeneratorBehaviour activeMap;
    private MapCameraBoundary activeBoundary;
    private Transform activeDefaultPose;
    private Plane activeMapPlane;
    private PrimaryPointerState primaryPointer;
    private Vector3 previousPointerMapPoint;
    private bool hasPointerMapBaseline;

    public MapGeneratorBehaviour ActiveMap => activeMap;
    public MapCameraBoundary ActiveBoundary => activeBoundary;
    public Transform ActiveDefaultPose => activeDefaultPose;
    public bool IsPanning => isPanning;
    internal bool IsPointerGestureOwned => primaryPointer.IsActive;

    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(
            HandleCinemachineCameraUpdated);

        if (gameFlowController != null)
        {
            gameFlowController.OnStateChanged += HandleGameFlowStateChanged;
        }
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(
            HandleCinemachineCameraUpdated);

        if (gameFlowController != null)
        {
            gameFlowController.OnStateChanged -= HandleGameFlowStateChanged;
        }

        bool ownsWindow = OwnsInspectionBinding;
        ClearInspectionBinding();
        if (ownsWindow) battleHUDUI.CancelTowerInspection();
        ForceClearBindings();
    }

    private void Update()
    {
        if (!primaryPointer.IsActive)
        {
            TryBeginPrimaryPointerGesture();
            return;
        }

        UpdateOwnedPointerGesture();
    }

    public bool TryValidateStableReferences(out string failureReason)
    {
        if (float.IsNaN(tapMovementThresholdPixels) || float.IsInfinity(tapMovementThresholdPixels) || tapMovementThresholdPixels < 0f ||
            float.IsNaN(towerSelectionDistance) || float.IsInfinity(towerSelectionDistance) || towerSelectionDistance <= 0f || towerSelectionMask.value == 0)
        { failureReason = "Invalid Tower inspection threshold, distance or layer mask."; return false; }
        if (!isActiveAndEnabled)
        {
            failureReason = "Camera Pan Controller is disabled.";
            return false;
        }

        if (outputCamera == null)
        {
            failureReason = "Output Camera is not assigned.";
            return false;
        }

        if (cinemachineBrain == null)
        {
            failureReason = "Cinemachine Brain is not assigned.";
            return false;
        }

        if (cinemachineBrain.OutputCamera != outputCamera)
        {
            failureReason =
                "Cinemachine Brain does not drive the assigned Output Camera.";
            return false;
        }

        if (cinemachineGameplayCamera == null)
        {
            failureReason = "Cinemachine Gameplay Camera is not assigned.";
            return false;
        }

        if (cinemachineConfiner == null)
        {
            failureReason = "Cinemachine Confiner 3D is not assigned.";
            return false;
        }

        if (cinemachineConfiner.gameObject !=
            cinemachineGameplayCamera.gameObject)
        {
            failureReason =
                "Cinemachine Confiner 3D must belong to the Cinemachine " +
                "Gameplay Camera.";
            return false;
        }

        if (!Mathf.Approximately(
                cinemachineConfiner.SlowingDistance,
                0f))
        {
            failureReason =
                "Cinemachine Confiner 3D Slowing Distance must be zero.";
            return false;
        }

        LensSettings lens = cinemachineGameplayCamera.Lens;

        if (!(lens.OrthographicSize > 0f) ||
            float.IsInfinity(lens.OrthographicSize))
        {
            failureReason =
                "Cinemachine Gameplay Camera Orthographic Size must be " +
                "greater than zero.";
            return false;
        }

        if (gameFlowController == null)
        {
            failureReason = "Game Flow Controller is not assigned.";
            return false;
        }

        if (battleHUDUI == null)
        {
            failureReason = "Battle HUD UI is not assigned.";
            return false;
        }

        if (towerPlacementController == null)
        {
            failureReason = "Tower Placement Controller is not assigned.";
            return false;
        }

        if (towerPlacementController.PlacementCamera != outputCamera)
        {
            failureReason =
                "Tower Placement and Camera Pan must use the same Output Camera.";
            return false;
        }

        if (eventSystem == null)
        {
            failureReason = "Event System is not assigned.";
            return false;
        }

        if (EventSystem.current != eventSystem)
        {
            failureReason =
                "The assigned Event System is not the current Event System.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryStageMapBinding(
        MapGeneratorBehaviour candidateMap,
        MapCameraBoundary candidateBoundary,
        Transform candidateDefaultPose,
        out string failureReason)
    {
        if (!TryValidateStableReferences(out failureReason))
        {
            return false;
        }

        if (candidateMap == null ||
            candidateBoundary == null ||
            candidateDefaultPose == null)
        {
            failureReason =
                "Candidate Map, boundary, and default pose must all be assigned.";
            return false;
        }

        if (!ReferenceEquals(
                candidateMap.CameraBoundary,
                candidateBoundary) ||
            !ReferenceEquals(
                candidateMap.CameraDefaultPose,
                candidateDefaultPose))
        {
            failureReason =
                "Candidate Camera references do not match the candidate Map.";
            return false;
        }

        if (candidateMap.NodesRoot == null)
        {
            failureReason = "Candidate Map NodesRoot is not assigned.";
            return false;
        }

        BoxCollider candidateCollider =
            candidateBoundary.BoundaryCollider;

        if (candidateCollider == null ||
            !candidateBoundary.isActiveAndEnabled ||
            !candidateCollider.enabled ||
            !candidateCollider.gameObject.activeInHierarchy)
        {
            failureReason =
                "Candidate Map Camera BoxCollider is not runtime-active.";
            return false;
        }

        if (!candidateBoundary.ContainsPoint(candidateDefaultPose.position))
        {
            failureReason =
                "Candidate default Camera position is outside its boundary.";
            return false;
        }

        if (stagedBoundary != null || activeBoundary != null)
        {
            failureReason =
                "Camera runtime still owns a staged or active Map binding.";
            return false;
        }

        stagedMap = candidateMap;
        stagedBoundary = candidateBoundary;
        stagedDefaultPose = candidateDefaultPose;
        cinemachineConfiner.SlowingDistance = 0f;
        cinemachineConfiner.BoundingVolume = candidateCollider;
        cinemachineConfiner.enabled = true;

        if (!cinemachineConfiner.IsValid)
        {
            ClearMapBinding(
                candidateMap,
                candidateBoundary,
                candidateDefaultPose);
            failureReason =
                "Cinemachine Confiner 3D rejected the candidate BoxCollider.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryCommitStagedMapBinding(
        MapGeneratorBehaviour candidateMap,
        MapCameraBoundary candidateBoundary,
        Transform candidateDefaultPose,
        out string failureReason)
    {
        if (!TryValidateStableReferences(out failureReason))
        {
            return false;
        }

        if (!ReferenceEquals(stagedMap, candidateMap) ||
            !ReferenceEquals(stagedBoundary, candidateBoundary) ||
            !ReferenceEquals(stagedDefaultPose, candidateDefaultPose))
        {
            failureReason =
                "Camera commit does not match the exact staged Map binding.";
            return false;
        }

        if (candidateMap == null ||
            candidateBoundary == null ||
            candidateDefaultPose == null ||
            candidateMap.NodesRoot == null ||
            !ReferenceEquals(
                cinemachineConfiner.BoundingVolume,
                candidateBoundary.BoundaryCollider) ||
            !cinemachineConfiner.IsValid)
        {
            failureReason =
                "Cinemachine Confiner 3D no longer owns the staged boundary.";
            return false;
        }

        if (!candidateBoundary.ContainsPoint(candidateDefaultPose.position))
        {
            failureReason =
                "Staged default Camera position is no longer inside its boundary.";
            return false;
        }

        if (inspectionBattle == null || !ReferenceEquals(inspectionMap, candidateMap))
        { failureReason = "Camera commit has no matching prepared inspection Battle/Map."; return false; }
        EndPanGesture(PointerPhase.Cancelled);
        cinemachineGameplayCamera.ForceCameraPosition(
            candidateDefaultPose.position,
            candidateDefaultPose.rotation);

        activeMap = stagedMap;
        activeBoundary = stagedBoundary;
        activeDefaultPose = stagedDefaultPose;
        activeMapPlane = new Plane(
            activeMap.NodesRoot.up,
            activeMap.NodesRoot.position);

        stagedMap = null;
        stagedBoundary = null;
        stagedDefaultPose = null;

        failureReason = string.Empty;
        return true;
    }

    public bool IsCommittedMapBinding(
        MapGeneratorBehaviour mapOwner,
        MapCameraBoundary boundaryOwner,
        Transform defaultPoseOwner)
    {
        return ReferenceEquals(activeMap, mapOwner) &&
               ReferenceEquals(activeBoundary, boundaryOwner) &&
               ReferenceEquals(activeDefaultPose, defaultPoseOwner) &&
               boundaryOwner != null &&
               cinemachineConfiner != null &&
               ReferenceEquals(
                   cinemachineConfiner.BoundingVolume,
                   boundaryOwner.BoundaryCollider) &&
               cinemachineConfiner.IsValid;
    }

    public void ClearMapBinding(
        MapGeneratorBehaviour mapOwner,
        MapCameraBoundary boundaryOwner,
        Transform defaultPoseOwner)
    {
        bool clearsStaged =
            ReferenceEquals(stagedMap, mapOwner) &&
            ReferenceEquals(stagedBoundary, boundaryOwner) &&
            ReferenceEquals(stagedDefaultPose, defaultPoseOwner);
        bool clearsActive =
            ReferenceEquals(activeMap, mapOwner) &&
            ReferenceEquals(activeBoundary, boundaryOwner) &&
            ReferenceEquals(activeDefaultPose, defaultPoseOwner);

        if (!clearsStaged && !clearsActive)
        {
            return;
        }

        BoxCollider ownedCollider =
            boundaryOwner == null ? null : boundaryOwner.BoundaryCollider;

        if (clearsStaged)
        {
            stagedMap = null;
            stagedBoundary = null;
            stagedDefaultPose = null;
        }

        if ((clearsActive || clearsStaged) && ReferenceEquals(inspectionMap, mapOwner))
        {
            bool ownsWindow = OwnsInspectionBinding;
            ClearInspectionBinding();
            if (ownsWindow) battleHUDUI.ClearTowerInspectionBinding();
        }
        if (clearsActive)
        {
            EndPanGesture(PointerPhase.Cancelled);
            activeMap = null;
            activeBoundary = null;
            activeDefaultPose = null;
            activeMapPlane = default;
        }

        if (ownedCollider != null &&
            cinemachineConfiner != null &&
            ReferenceEquals(
                cinemachineConfiner.BoundingVolume,
                ownedCollider))
        {
            cinemachineConfiner.BoundingVolume = null;
            cinemachineConfiner.enabled = false;
        }
    }

    private void TryBeginPrimaryPointerGesture()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                suppressMouseUntilReleased = true;
                TryBeginPan(
                    PointerKind.Touch,
                    touch.fingerId,
                    touch.position);
                return;
            }
        }

        if (Input.touchCount > 0) { suppressMouseUntilReleased = true; return; }
        if (suppressMouseUntilReleased)
        { if (!Input.GetMouseButton(0)) suppressMouseUntilReleased = false; return; }
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginPan(
                PointerKind.Mouse,
                MousePointerId,
                Input.mousePosition);
        }
    }

    private void TryBeginPan(
        PointerKind pointerKind,
        int pointerId,
        Vector2 screenPosition)
    {
        if (!CanPanNow() ||
            IsPointerOverRaycastableUI(pointerId, screenPosition) ||
            !TryGetPointerMapPoint(screenPosition, out Vector3 mapPoint))
        {
            return;
        }

        primaryPointer = new PrimaryPointerState
        {
            Kind = pointerKind,
            Id = pointerId,
            ScreenPosition = screenPosition,
            Phase = PointerPhase.Pressed
        };
        pressPosition = screenPosition;
        pressInputRevision = battleHUDUI.BattlefieldInputRevision;
        pressedTower = FindTower(screenPosition);
        pressedTowerIdentity = pressedTower != null ? pressedTower.RuntimeIdentity : null;
        isPanning = false;
        previousPointerMapPoint = mapPoint;
        hasPointerMapBaseline = true;
    }

    private void UpdateOwnedPointerGesture()
    {
        if (!CanPanNow())
        {
            EndPanGesture(PointerPhase.Cancelled);
            return;
        }

        if (primaryPointer.Kind == PointerKind.Mouse)
        {
            if (Input.GetMouseButtonUp(0))
            {
                ReleasePointer(Input.mousePosition);
                return;
            }

            if (!Input.GetMouseButton(0))
            {
                EndPanGesture(PointerPhase.Cancelled);
                return;
            }

            MovePointer(Input.mousePosition);
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.fingerId != primaryPointer.Id)
            {
                continue;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                ReleasePointer(touch.position);
                return;
            }

            if (touch.phase == TouchPhase.Canceled)
            {
                EndPanGesture(PointerPhase.Cancelled);
                return;
            }

            MovePointer(touch.position);
            return;
        }

        EndPanGesture(PointerPhase.Cancelled);
    }

    private TowerInstance FindTower(Vector2 screenPosition)
    {
        var hits = Physics.RaycastAll(outputCamera.ScreenPointToRay(screenPosition), towerSelectionDistance,
            towerSelectionMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance != b.distance ? a.distance.CompareTo(b.distance) :
            a.collider.GetInstanceID().CompareTo(b.collider.GetInstanceID()));
        foreach (var hit in hits)
        {
            var tower = hit.collider.GetComponentInParent<TowerInstance>();
            if (tower != null && tower.isActiveAndEnabled && inspectionMembers.OwnsDeployedTower(tower)) return tower;
        }
        return null;
    }
    private void ReleasePointer(Vector2 position)
    {
        bool tap = !isPanning && (position - pressPosition).sqrMagnitude <= tapMovementThresholdPixels * tapMovementThresholdPixels;
        var target = pressedTower;
        var identity = pressedTowerIdentity;
        var battle = inspectionBattle;
        var map = inspectionMap;
        var revision = pressInputRevision;
        bool eligible = tap && CanPanNow() && pressInputRevision == battleHUDUI.BattlefieldInputRevision &&
            !IsPointerOverRaycastableUI(primaryPointer.Id, position) && target != null &&
            ReferenceEquals(target.RuntimeIdentity, pressedTowerIdentity) && FindTower(position) == target;
        eligible = eligible && revision == battleHUDUI.BattlefieldInputRevision && CanPanNow() &&
            ReferenceEquals(battle, inspectionBattle) && ReferenceEquals(map, inspectionMap) &&
            ReferenceEquals(target.RuntimeIdentity, identity);
        EndPanGesture(PointerPhase.Released);
        if (eligible && !battleHUDUI.TryOpenTowerInfo(target, out string reason))
            Debug.LogWarning("Tower inspection opening was rejected: " + reason, this);
    }
    private void MovePointer(Vector2 position)
    {
        if (pressInputRevision != battleHUDUI.BattlefieldInputRevision) { CancelPointerGesture(); return; }
        if (!isPanning)
        {
            primaryPointer.ScreenPosition = position;
            if ((position - pressPosition).sqrMagnitude <= tapMovementThresholdPixels * tapMovementThresholdPixels) return;
            isPanning = true; pressedTower = null; pressedTowerIdentity = null;
            hasPointerMapBaseline = TryGetPointerMapPoint(position, out previousPointerMapPoint);
            return;
        }
        MovePan(position);
    }

    private void MovePan(Vector2 screenPosition)
    {
        primaryPointer.ScreenPosition = screenPosition;
        primaryPointer.Phase = PointerPhase.Moved;

        if (!TryGetPointerMapPoint(screenPosition, out Vector3 mapPoint))
        {
            hasPointerMapBaseline = false;
            return;
        }

        if (!hasPointerMapBaseline)
        {
            previousPointerMapPoint = mapPoint;
            hasPointerMapBaseline = true;
            return;
        }

        Vector3 worldDelta = previousPointerMapPoint - mapPoint;
        worldDelta = Vector3.ProjectOnPlane(
            worldDelta,
            activeMap.NodesRoot.up);

        cinemachineGameplayCamera.transform.position += worldDelta;
        previousPointerMapPoint = mapPoint;
    }

    private bool CanPanNow()
    {
        return outputCamera != null && cinemachineBrain != null && cinemachineGameplayCamera != null &&
               cinemachineBrain.OutputCamera == outputCamera && activeMap != null &&
               activeBoundary != null &&
               activeDefaultPose != null &&
               cinemachineConfiner != null &&
               ReferenceEquals(
                   cinemachineConfiner.BoundingVolume,
                   activeBoundary.BoundaryCollider) &&
               cinemachineConfiner.IsValid &&
               gameFlowController != null &&
               gameFlowController.CurrentState == GameFlowState.Battle &&
               battleHUDUI != null && battleHUDUI.isActiveAndEnabled &&
               !battleHUDUI.IsDraftSessionBusy && !battleHUDUI.IsTowerInspectionBusy &&
               inspectionBattle != null && inspectionBattle.IsOpenForRead &&
               battleHUDUI.HasTowerInspectionBinding(inspectionBattle, inspectionMembers, inspectionPause) &&
               ReferenceEquals(inspectionMap, activeMap) && inspectionMembers != null && inspectionMembers.CanStartOperation &&
               inspectionPause != null && inspectionPause.CanAcquire &&
               towerPlacementController != null &&
               towerPlacementController.IsAvailableForInspection &&
               eventSystem != null &&
               EventSystem.current == eventSystem;
    }

    private bool IsPointerOverRaycastableUI(
        int pointerId,
        Vector2 screenPosition)
    {
        if (eventSystem == null || EventSystem.current != eventSystem)
        {
            return true;
        }

        PointerEventData pointerEventData =
            new PointerEventData(eventSystem)
            {
                pointerId = pointerId,
                position = screenPosition
            };

        uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, uiRaycastResults);
        bool blocked = uiRaycastResults.Exists(result => result.module is UnityEngine.UI.GraphicRaycaster);
        uiRaycastResults.Clear();
        return blocked;
    }

    private bool TryGetPointerMapPoint(
        Vector2 screenPosition,
        out Vector3 mapPoint)
    {
        mapPoint = default;

        if (outputCamera == null || activeMap == null)
        {
            return false;
        }

        Ray pointerRay = outputCamera.ScreenPointToRay(screenPosition);

        if (!activeMapPlane.Raycast(pointerRay, out float enter))
        {
            return false;
        }

        mapPoint = pointerRay.GetPoint(enter);
        return true;
    }

    private void HandleCinemachineCameraUpdated(
        CinemachineBrain updatedBrain)
    {
        if (updatedBrain != cinemachineBrain ||
            activeMap == null ||
            activeBoundary == null ||
            outputCamera == null ||
            cinemachineGameplayCamera == null ||
            !ReferenceEquals(
                updatedBrain.ActiveVirtualCamera,
                cinemachineGameplayCamera))
        {
            return;
        }

        Vector3 acceptedPosition = outputCamera.transform.position;

        if ((cinemachineGameplayCamera.transform.position -
             acceptedPosition).sqrMagnitude >
            PositionReconcileTolerance * PositionReconcileTolerance)
        {
            cinemachineGameplayCamera.ForceCameraPosition(
                acceptedPosition,
                cinemachineGameplayCamera.transform.rotation);
        }

        activeMapPlane = new Plane(
            activeMap.NodesRoot.up,
            activeMap.NodesRoot.position);

        if (IsPanning)
        {
            hasPointerMapBaseline = TryGetPointerMapPoint(
                primaryPointer.ScreenPosition,
                out previousPointerMapPoint);
        }
    }

    private void HandleGameFlowStateChanged(GameFlowState state)
    {
        if (state != GameFlowState.Battle)
        {
            if (OwnsInspectionBinding) battleHUDUI.CancelTowerInspection();
            EndPanGesture(PointerPhase.Cancelled);
        }
    }

    private void EndPanGesture(PointerPhase terminalPhase)
    {
        primaryPointer = new PrimaryPointerState
        {
            Kind = PointerKind.None,
            Id = 0,
            ScreenPosition = default,
            Phase = terminalPhase
        };
        previousPointerMapPoint = default;
        hasPointerMapBaseline = false;
        isPanning = false; pressedTower = null; pressedTowerIdentity = null;
    }

    private void ForceClearBindings()
    {
        EndPanGesture(PointerPhase.Cancelled);
        stagedMap = null;
        stagedBoundary = null;
        stagedDefaultPose = null;
        activeMap = null;
        activeBoundary = null;
        activeDefaultPose = null;
        activeMapPlane = default;

        if (cinemachineConfiner != null)
        {
            cinemachineConfiner.BoundingVolume = null;
            cinemachineConfiner.enabled = false;
        }
    }
}
