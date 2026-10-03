using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal enum MonsterDashedPathState
{
    Normal,
    Blocked
}

// Display-only. The caller owns topology freshness and Battle publication authority.
public sealed class MonsterDashedPathPresenter : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color blockedColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float surfaceHeight = 0.02f;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
    private static readonly int FlowOffsetId = Shader.PropertyToID("_FlowOffset");
    private MaterialPropertyBlock properties;
    private MapGeneratorBehaviour boundMap;
    private Vector3[] displayedPositions;
    private MonsterDashedPathState? displayedState;
    private bool initialized;
    private Color displayedColor;
    private double flowPhase;

    internal bool TryInitialize(MapGeneratorBehaviour map, out string failureReason)
    {
        Clear();
        initialized = false;
        boundMap = null;
        if (map == null || map.NodesRoot == null)
            return Fail("Dashed path requires an explicit Map and NodesRoot.", out failureReason);
        if (!TryValidateRenderer(out failureReason)) return false;
        if (!TryGetFrame(map, out _, out _, out failureReason)) return false;

        boundMap = map;
        initialized = true;
        ApplyConfiguration();
        failureReason = null;
        return true;
    }

    internal bool TrySetRoute(MonsterMainRouteSnapshot route, MonsterDashedPathState state,
        out string failureReason)
    {
        if (!TryValidateRequest(state, out failureReason)) return false;
        if (route == null || route.Map != boundMap || route.Route == null || route.Route.Count < 2)
            return Fail("Dashed path requires a Spawn-to-Target route from the bound Map.", out failureReason);
        if (!TryGetFrame(boundMap, out Vector3 normal, out Quaternion rotation, out failureReason))
        {
            Clear();
            return false;
        }

        Vector3[] positions = new Vector3[route.Route.Count];
        var unique = new HashSet<GridNodeBehaviour>();
        for (int i = 0; i < positions.Length; i++)
        {
            GridNodeBehaviour node = route.Route[i];
            if (node == null || node.MapOwner != boundMap || !unique.Add(node))
                return Fail("Dashed path contains a missing, foreign, or repeated node.", out failureReason);
            if (i > 0)
            {
                Vector2Int previous = route.Route[i - 1].GridPosition;
                long dx = Math.Abs((long)node.GridPosition.x - previous.x);
                long dy = Math.Abs((long)node.GridPosition.y - previous.y);
                if (dx + dy != 1)
                    return Fail("Dashed path nodes must be ordered orthogonal neighbors.", out failureReason);
            }
            positions[i] = node.WorldPosition + normal * surfaceHeight;
            if (!Finite(positions[i]))
                return Fail("Dashed path contains a non-finite world position.", out failureReason);
        }
        if (route.Route[0].NodeType != GridNodeType.Spawn ||
            route.Route[positions.Length - 1].NodeType != GridNodeType.Target)
            return Fail("Dashed path endpoints must be Spawn and Target.", out failureReason);

        // Compare final coordinates, not snapshot identity: the same route can move in Map space.
        bool geometryChanged = !SamePositions(displayedPositions, positions);
        lineRenderer.transform.rotation = rotation;
        ApplyConfiguration();
        if (geometryChanged)
        {
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
            displayedPositions = positions;
        }
        ApplyState(state);
        lineRenderer.enabled = true;
        failureReason = null;
        return true;
    }

    internal bool TrySetState(MonsterDashedPathState state, out string failureReason)
    {
        if (!TryValidateRequest(state, out failureReason)) return false;
        if (displayedPositions == null)
            return Fail("Dashed path has no route to display. Submit a route first.", out failureReason);
        ApplyConfiguration();
        ApplyState(state);
        failureReason = null;
        return true;
    }

    public void Clear()
    {
        displayedPositions = null;
        displayedState = null;
        flowPhase = 0d;
        properties?.Clear();
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
            lineRenderer.SetPropertyBlock(null);
        }
    }

    private void Awake() => Clear();

    private void OnDestroy()
    {
        Clear();
        properties = null;
        initialized = false;
        boundMap = null;
    }

    private bool TryValidateRequest(MonsterDashedPathState state, out string reason)
    {
        if (!initialized || boundMap == null || boundMap.NodesRoot == null)
            return Fail("Dashed path is not initialized for an available Map.", out reason);
        if (state != MonsterDashedPathState.Normal && state != MonsterDashedPathState.Blocked)
            return Fail("Dashed path display state is unsupported.", out reason);
        if (!TryValidateRenderer(out reason))
        {
            Clear();
            return false;
        }
        return true;
    }

    private bool TryValidateRenderer(out string reason)
    {
        if (lineRenderer == null || (lineRenderer.transform != transform &&
            !lineRenderer.transform.IsChildOf(transform)))
            return Fail("Dashed path requires a LineRenderer owned by its Prefab hierarchy.", out reason);
        Material material = lineRenderer.sharedMaterial;
        if (material == null || !material.HasProperty(BaseMapId) ||
            !material.HasProperty(FlowSpeedId) || !material.HasProperty(FlowOffsetId))
            return Fail("Dashed path Material must expose _BaseMap, _FlowSpeed, and _FlowOffset and consume vertex RGBA.", out reason);
        Texture texture = material.GetTexture(BaseMapId);
        if (texture == null || texture.wrapMode != TextureWrapMode.Repeat)
            return Fail("Dashed path Material requires a repeating _BaseMap Texture with transparent gaps.", out reason);
        Vector2 tiling = material.GetTextureScale(BaseMapId);
        Vector2 offset = material.GetTextureOffset(BaseMapId);
        float speed = material.GetFloat(FlowSpeedId);
        if (!Positive(lineRenderer.widthMultiplier) || !Positive(tiling.x) || !Positive(tiling.y) ||
            !Finite(offset.x) || !Finite(offset.y) || !Finite(speed) || speed < 0f ||
            !Finite(surfaceHeight) || !ValidColor(normalColor) || !ValidColor(blockedColor))
            return Fail("Dashed path requires valid authored width/texture tiling, finite nonnegative flow speed, finite height, and finite state colors with Alpha between zero and one inclusive.", out reason);
        reason = null;
        return true;
    }

    private static bool TryGetFrame(MapGeneratorBehaviour map, out Vector3 normal,
        out Quaternion rotation, out string reason)
    {
        normal = map.NodesRoot.up;
        Vector3 forward = map.NodesRoot.forward;
        rotation = Quaternion.identity;
        if (!Finite(normal) || !Finite(forward) || !Positive(normal.sqrMagnitude) ||
            !Positive(forward.sqrMagnitude) || !Positive(Vector3.Cross(normal, forward).sqrMagnitude))
        {
            reason = "Dashed path Map orientation is unusable.";
            return false;
        }
        // TransformZ makes the ribbon face the Map normal, not the Camera.
        rotation = Quaternion.LookRotation(normal, forward);
        reason = null;
        return true;
    }

    private void ApplyConfiguration()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.textureMode = LineTextureMode.Tile;
        // Material/Texture authoring owns pattern density; never overwrite the width curve or multiplier.
        lineRenderer.textureScale = Vector2.one;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.numCapVertices = 0;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        ApplyFlowOffset();
    }

    private void Update()
    {
        if (!initialized || displayedPositions == null || lineRenderer == null || !lineRenderer.enabled) return;
        if (!TryAdvanceFlow(Time.deltaTime, out string reason)) Debug.LogWarning(reason, this);
    }

    // Scaled time is supplied explicitly for contract tests; production uses Time.deltaTime.
    // Draft owns timeScale. This component never acquires or restores pause authority.
    internal bool TryAdvanceFlow(float scaledDeltaTime, out string reason)
    {
        if (!initialized || displayedPositions == null || lineRenderer == null || !lineRenderer.enabled)
        {
            reason = null;
            return true;
        }
        if (!Finite(scaledDeltaTime) || scaledDeltaTime < 0f)
            return Fail("Dashed path animation requires finite nonnegative scaled elapsed time.", out reason);
        // Also freeze in the frame where Draft acquired pause after deltaTime was computed.
        if (Time.timeScale == 0f || scaledDeltaTime == 0f)
        {
            reason = null;
            return true;
        }
        if (!TryValidateRenderer(out reason)) return false;
        if (boundMap == null || boundMap.NodesRoot == null)
            return Fail("Dashed path animation lost its bound Map.", out reason);
        float speed = lineRenderer.sharedMaterial.GetFloat(FlowSpeedId);
        if (speed > 0f)
        {
            // Speed is texture cycles per simulation second. Wrap to keep the offset bounded.
            flowPhase = (flowPhase + (double)speed * scaledDeltaTime) % 1d;
            ApplyFlowOffset();
        }
        reason = null;
        return true;
    }

    private void ApplyFlowOffset()
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        float phase = (float)flowPhase;
        properties.SetFloat(FlowOffsetId, phase >= 1f ? 0f : phase);
        lineRenderer.SetPropertyBlock(properties);
    }

    private void ApplyState(MonsterDashedPathState state)
    {
        Color color = state == MonsterDashedPathState.Blocked ? blockedColor : normalColor;
        if (displayedState == state && displayedColor.Equals(color)) return;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) });
        lineRenderer.colorGradient = gradient;
        displayedState = state;
        displayedColor = color;
    }

    private static bool ValidColor(Color color) => Finite(color.r) && Finite(color.g) &&
        Finite(color.b) && Finite(color.a) && color.a >= 0f && color.a <= 1f;

    private bool Fail(string message, out string reason)
    {
        Clear();
        reason = message;
        return false;
    }

    private static bool SamePositions(Vector3[] a, Vector3[] b)
    {
        if (a == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (!a[i].Equals(b[i])) return false;
        return true;
    }

    private static bool Positive(float value) => Finite(value) && value > 0f;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
}
