using System;
using UnityEngine;

// One originating Battle. Never adopt a replacement Map or renderer binding.
internal sealed class MonsterDashedPathSession
{
    private readonly MapGeneratorBehaviour map;
    private readonly AStarPathfindingService paths;
    private readonly TowerPlacementValidator validator;
    private readonly MonsterDashedPathPresenter presenter;
    private readonly Action<string> report;
    private MonsterMainRouteSnapshot formalRoute;
    private MonsterMainRouteSnapshot retainedRoute;
    private MonsterDashedPathState state;
    private object drag;
    private bool active = true;
    private bool presenterReady;
    private ulong validatorRevision;
    private Matrix4x4 spatialFrame;
    private string lastFailure;

    internal MonsterDashedPathSession(MapGeneratorBehaviour map, AStarPathfindingService paths,
        TowerPlacementValidator validator, MonsterDashedPathPresenter presenter, Action<string> report)
    {
        this.map = map;
        this.paths = paths;
        this.validator = validator;
        this.presenter = presenter;
        this.report = report;
    }

    internal bool IsActive => active;

    internal void Tick()
    {
        if (!EnsureBaseline()) return;
        Matrix4x4 frame = map.NodesRoot.localToWorldMatrix;
        if (!frame.Equals(spatialFrame)) Display(retainedRoute ?? formalRoute, state);
    }

    internal object BeginDrag()
    {
        if (!active) return null;
        drag = new object();
        state = MonsterDashedPathState.Normal;
        if (EnsureBaseline())
        {
            retainedRoute = formalRoute;
            Display(retainedRoute, state);
        }
        return drag;
    }

    internal void Preview(object token, TowerPlacementRoutePreviewResult result, bool fallback)
    {
        if (!OwnsDrag(token) || !EnsureBaseline()) return;
        if (fallback)
        {
            retainedRoute = formalRoute;
            Display(retainedRoute, MonsterDashedPathState.Normal);
            return;
        }
        if (result == null || result.Outcome == TowerPlacementRoutePreviewOutcome.TechnicalFailure)
        {
            Hide(result?.FailureReason ?? "Route preview is unavailable.");
            return;
        }
        if (!result.IsCurrentFor(validator, result.Footprint))
        {
            Hide("Route preview expired before presentation.");
            return;
        }
        switch (result.Outcome)
        {
            case TowerPlacementRoutePreviewOutcome.RouteAvailable:
                retainedRoute = result.MainRoute;
                Display(retainedRoute, MonsterDashedPathState.Normal);
                break;
            case TowerPlacementRoutePreviewOutcome.RouteBlocked:
                // Re-submit retained geometry even after a technical failure cleared the renderer.
                Display(retainedRoute ?? formalRoute, MonsterDashedPathState.Blocked);
                break;
            default:
                retainedRoute = formalRoute;
                Display(retainedRoute, MonsterDashedPathState.Normal);
                break;
        }
    }

    internal void EndDrag(object token)
    {
        if (!OwnsDrag(token)) return;
        drag = null; // Revoke before any presentation call.
        retainedRoute = null;
        state = MonsterDashedPathState.Normal;
        if (EnsureBaseline()) Display(formalRoute, state);
    }

    internal MonsterMainRouteSnapshot CaptureCommittedRoute(TowerPlacementTopologyPlan plan)
    {
        if (!BindingAvailable() || plan == null) return null;
        return new MonsterMainRouteSnapshot(map, paths, plan.AuthoritativeRoute);
    }

    internal void AcceptCommittedRoute(MonsterMainRouteSnapshot route)
    {
        if (!BindingAvailable() || route == null || !route.IsCurrentFor(map, paths)) return;
        formalRoute = route;
        retainedRoute = route;
        validatorRevision = validator.BindingRevision;
        Display(route, MonsterDashedPathState.Normal);
    }

    internal void Close()
    {
        active = false; // This must precede Clear, which can execute engine callbacks.
        drag = null;
        formalRoute = null;
        retainedRoute = null;
        presenterReady = false;
        presenter?.Clear();
    }

    private bool OwnsDrag(object token) => active && token != null && ReferenceEquals(token, drag);

    private bool BindingAvailable() => active && map != null && map.NodesRoot != null &&
        paths != null && paths.ActiveMap == map && validator != null && validator.IsConfiguredFor(map, paths);

    private bool EnsureBaseline()
    {
        try { return EnsureBaselineCore(); }
        catch (Exception exception) { Hide(exception.Message); return false; }
    }

    private bool EnsureBaselineCore()
    {
        if (!active) return false;
        if (!BindingAvailable())
        {
            Hide("Dashed path lost its originating Map/path/validator binding.");
            return false;
        }
        if (formalRoute != null && formalRoute.IsCurrentFor(map, paths) &&
            validatorRevision == validator.BindingRevision) return true;
        // A topology/binding revision invalidates retained candidate geometry, including hidden data.
        formalRoute = null;
        retainedRoute = null;
        if (!paths.TryQueryFormalMainRoute(out var route, out string reason))
        {
            Hide(reason);
            return false;
        }
        formalRoute = retainedRoute = route;
        validatorRevision = validator.BindingRevision;
        state = MonsterDashedPathState.Normal;
        Display(route, state);
        return true;
    }

    private void Display(MonsterMainRouteSnapshot route, MonsterDashedPathState requested)
    {
        if (!BindingAvailable() || route == null || !route.IsCurrentFor(map, paths))
        {
            Hide("Dashed path display route is unavailable or expired.");
            return;
        }
        try
        {
            if (presenter == null)
            {
                Hide("Dashed path presenter is missing.");
                return;
            }
            if (!presenterReady)
            {
                if (!presenter.TryInitialize(map, out string initializationReason))
                {
                    Hide(initializationReason);
                    return;
                }
                presenterReady = true;
            }
            if (!presenter.TrySetRoute(route, requested, out string reason))
            {
                Hide(reason);
                return;
            }
            state = requested;
            spatialFrame = map.NodesRoot.localToWorldMatrix;
            lastFailure = null;
        }
        catch (Exception exception) { Hide(exception.Message); }
    }

    private void Hide(string reason)
    {
        try { presenter?.Clear(); }
        catch (Exception exception) { reason = "Dashed path clear failed: " + exception.Message; }
        // Preserve valid retained route and initialization for same-topology recovery.
        reason = string.IsNullOrEmpty(reason) ? "Dashed path presentation failed." : reason;
        if (reason == lastFailure) return;
        lastFailure = reason;
        try { report?.Invoke(reason); }
        catch (Exception exception) { Debug.LogException(exception); }
    }
}
