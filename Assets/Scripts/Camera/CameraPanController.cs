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
    public bool IsPanning => primaryPointer.IsActive;

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
                TryBeginPan(
                    PointerKind.Touch,
                    touch.fingerId,
                    touch.position);
                return;
            }
        }

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
                EndPanGesture(PointerPhase.Released);
                return;
            }

            if (!Input.GetMouseButton(0))
            {
                EndPanGesture(PointerPhase.Cancelled);
                return;
            }

            MovePan(Input.mousePosition);
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
                EndPanGesture(PointerPhase.Released);
                return;
            }

            if (touch.phase == TouchPhase.Canceled)
            {
                EndPanGesture(PointerPhase.Cancelled);
                return;
            }

            MovePan(touch.position);
            return;
        }

        EndPanGesture(PointerPhase.Cancelled);
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
        return activeMap != null &&
               activeBoundary != null &&
               activeDefaultPose != null &&
               cinemachineConfiner != null &&
               ReferenceEquals(
                   cinemachineConfiner.BoundingVolume,
                   activeBoundary.BoundaryCollider) &&
               cinemachineConfiner.IsValid &&
               gameFlowController != null &&
               gameFlowController.CurrentState == GameFlowState.Battle &&
               battleHUDUI != null &&
               !battleHUDUI.IsDraftOpen &&
               towerPlacementController != null &&
               !towerPlacementController.IsDragging &&
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
        bool blocked = uiRaycastResults.Count > 0;
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
