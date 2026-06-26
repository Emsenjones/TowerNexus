using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class AttackRangePreviewMotion : MonoBehaviour
{
    [SerializeField] private List<AttackRangePreviewMotionTarget> motionTargets = new List<AttackRangePreviewMotionTarget>();

    private readonly List<Tween> activeTweens = new List<Tween>();
    private readonly List<MotionTargetState> targetStates = new List<MotionTargetState>();

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    private void OnDestroy()
    {
        StopAndRestore();
    }

    [ContextMenu("Preview Motion")]
    public void Play()
    {
        StopAndRestore();
        CacheTargetStates();
        ApplyStartValues();
        CreateTweens();
    }

    [ContextMenu("Stop Motion")]
    public void StopAndRestore()
    {
        KillActiveTweens();
        RestoreTargetStates();
    }

    private void CacheTargetStates()
    {
        targetStates.Clear();

        for (int i = 0; i < motionTargets.Count; i++)
        {
            AttackRangePreviewMotionTarget motionTarget = motionTargets[i];

            if (motionTarget == null || motionTarget.TargetObject == null)
            {
                continue;
            }

            Transform targetTransform = motionTarget.TargetObject.transform;
            targetStates.Add(new MotionTargetState(
                motionTarget,
                targetTransform,
                targetTransform.localPosition,
                targetTransform.localRotation,
                targetTransform.localScale,
                CacheMaterialBindings(motionTarget.TargetObject)
            ));
        }
    }

    private void ApplyStartValues()
    {
        for (int i = 0; i < targetStates.Count; i++)
        {
            MotionTargetState state = targetStates[i];
            IReadOnlyList<AttackRangePreviewMotionStep> steps = state.MotionTarget.Steps;

            for (int j = 0; j < steps.Count; j++)
            {
                AttackRangePreviewMotionStep step = steps[j];

                if (step == null || !step.Enabled)
                {
                    continue;
                }

                ApplyStartValue(state, step);
            }
        }
    }

    private void CreateTweens()
    {
        for (int i = 0; i < targetStates.Count; i++)
        {
            MotionTargetState state = targetStates[i];
            IReadOnlyList<AttackRangePreviewMotionStep> steps = state.MotionTarget.Steps;

            for (int j = 0; j < steps.Count; j++)
            {
                AttackRangePreviewMotionStep step = steps[j];

                if (step == null || !step.Enabled)
                {
                    continue;
                }

                CreateTweens(state, step);
            }
        }
    }

    private void ApplyStartValue(MotionTargetState state, AttackRangePreviewMotionStep step)
    {
        switch (step.MotionType)
        {
            case AttackRangePreviewMotionType.LocalPosition:
                state.TargetTransform.localPosition = step.StartVector;
                break;
            case AttackRangePreviewMotionType.LocalRotation:
                state.TargetTransform.localEulerAngles = step.StartVector;
                break;
            case AttackRangePreviewMotionType.LocalScale:
                state.TargetTransform.localScale = step.StartVector;
                break;
            case AttackRangePreviewMotionType.MaterialAlpha:
                SetMaterialAlpha(state.MaterialBindings, step.StartAlpha);
                break;
        }
    }

    private void CreateTweens(MotionTargetState state, AttackRangePreviewMotionStep step)
    {
        switch (step.MotionType)
        {
            case AttackRangePreviewMotionType.LocalPosition:
                AddConfiguredTween(
                    state.TargetTransform.DOLocalMove(step.TargetVector, step.Duration),
                    step
                );
                break;
            case AttackRangePreviewMotionType.LocalRotation:
                AddConfiguredTween(
                    state.TargetTransform.DOLocalRotate(step.TargetVector, step.Duration, RotateMode.FastBeyond360),
                    step
                );
                break;
            case AttackRangePreviewMotionType.LocalScale:
                AddConfiguredTween(
                    state.TargetTransform.DOScale(step.TargetVector, step.Duration),
                    step
                );
                break;
            case AttackRangePreviewMotionType.MaterialAlpha:
                CreateMaterialAlphaTweens(state.MaterialBindings, step);
                break;
        }
    }

    private void CreateMaterialAlphaTweens(IReadOnlyList<MaterialColorBinding> materialBindings, AttackRangePreviewMotionStep step)
    {
        for (int i = 0; i < materialBindings.Count; i++)
        {
            MaterialColorBinding binding = materialBindings[i];

            if (binding.Material == null)
            {
                continue;
            }

            AddConfiguredTween(
                DOTween.To(
                    () => GetMaterialAlpha(binding),
                    alpha => SetMaterialAlpha(binding, alpha),
                    step.TargetAlpha,
                    step.Duration
                ),
                step
            );
        }
    }

    private void AddConfiguredTween(Tween tween, AttackRangePreviewMotionStep step)
    {
        if (tween == null)
        {
            return;
        }

        tween.SetTarget(this);
        tween.SetDelay(step.Delay);
        tween.SetEase(step.EaseType);
        tween.SetLoops(step.Loops, step.LoopType);
        activeTweens.Add(tween);
    }

    private void KillActiveTweens()
    {
        for (int i = 0; i < activeTweens.Count; i++)
        {
            Tween tween = activeTweens[i];

            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }

        activeTweens.Clear();
    }

    private void RestoreTargetStates()
    {
        for (int i = 0; i < targetStates.Count; i++)
        {
            MotionTargetState state = targetStates[i];

            if (state.TargetTransform == null)
            {
                continue;
            }

            state.TargetTransform.localPosition = state.InitialLocalPosition;
            state.TargetTransform.localRotation = state.InitialLocalRotation;
            state.TargetTransform.localScale = state.InitialLocalScale;
            RestoreMaterialBindings(state.MaterialBindings);
        }

        targetStates.Clear();
    }

    private static List<MaterialColorBinding> CacheMaterialBindings(GameObject targetObject)
    {
        List<MaterialColorBinding> bindings = new List<MaterialColorBinding>();

        if (targetObject == null)
        {
            return bindings;
        }

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            Material[] materials = targetRenderer.materials;

            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];

                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_BaseColor"))
                {
                    bindings.Add(new MaterialColorBinding(material, "_BaseColor", material.GetColor("_BaseColor")));
                }
                else if (material.HasProperty("_Color"))
                {
                    bindings.Add(new MaterialColorBinding(material, "_Color", material.GetColor("_Color")));
                }
            }
        }

        return bindings;
    }

    private static float GetMaterialAlpha(MaterialColorBinding binding)
    {
        if (binding.Material == null)
        {
            return 0f;
        }

        Color color = binding.Material.GetColor(binding.ColorPropertyName);
        return color.a;
    }

    private static void SetMaterialAlpha(IReadOnlyList<MaterialColorBinding> materialBindings, float alpha)
    {
        for (int i = 0; i < materialBindings.Count; i++)
        {
            SetMaterialAlpha(materialBindings[i], alpha);
        }
    }

    private static void SetMaterialAlpha(MaterialColorBinding binding, float alpha)
    {
        if (binding.Material == null)
        {
            return;
        }

        Color color = binding.Material.GetColor(binding.ColorPropertyName);
        color.a = Mathf.Clamp01(alpha);
        binding.Material.SetColor(binding.ColorPropertyName, color);
    }

    private static void RestoreMaterialBindings(IReadOnlyList<MaterialColorBinding> materialBindings)
    {
        for (int i = 0; i < materialBindings.Count; i++)
        {
            MaterialColorBinding binding = materialBindings[i];

            if (binding.Material == null)
            {
                continue;
            }

            binding.Material.SetColor(binding.ColorPropertyName, binding.InitialColor);
        }
    }

    private readonly struct MotionTargetState
    {
        public MotionTargetState(
            AttackRangePreviewMotionTarget motionTarget,
            Transform targetTransform,
            Vector3 initialLocalPosition,
            Quaternion initialLocalRotation,
            Vector3 initialLocalScale,
            List<MaterialColorBinding> materialBindings)
        {
            MotionTarget = motionTarget;
            TargetTransform = targetTransform;
            InitialLocalPosition = initialLocalPosition;
            InitialLocalRotation = initialLocalRotation;
            InitialLocalScale = initialLocalScale;
            MaterialBindings = materialBindings;
        }

        public AttackRangePreviewMotionTarget MotionTarget { get; }
        public Transform TargetTransform { get; }
        public Vector3 InitialLocalPosition { get; }
        public Quaternion InitialLocalRotation { get; }
        public Vector3 InitialLocalScale { get; }
        public List<MaterialColorBinding> MaterialBindings { get; }
    }

    private readonly struct MaterialColorBinding
    {
        public MaterialColorBinding(Material material, string colorPropertyName, Color initialColor)
        {
            Material = material;
            ColorPropertyName = colorPropertyName;
            InitialColor = initialColor;
        }

        public Material Material { get; }
        public string ColorPropertyName { get; }
        public Color InitialColor { get; }
    }
}

[Serializable]
public class AttackRangePreviewMotionTarget
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private List<AttackRangePreviewMotionStep> steps = new List<AttackRangePreviewMotionStep>
    {
        AttackRangePreviewMotionStep.CreateScaleStep(Vector3.one, new Vector3(1.04f, 1.04f, 1.04f), 0.8f, 0f, Ease.InOutSine, LoopType.Yoyo),
        AttackRangePreviewMotionStep.CreateMaterialAlphaStep(0.55f, 0.9f, 0.8f, 0f, Ease.InOutSine, LoopType.Yoyo)
    };

    public GameObject TargetObject => targetObject;
    public IReadOnlyList<AttackRangePreviewMotionStep> Steps => steps;
}

[Serializable]
public class AttackRangePreviewMotionStep
{
    [SerializeField] private bool enabled = true;
    [SerializeField] private AttackRangePreviewMotionType motionType = AttackRangePreviewMotionType.LocalScale;
    [HideIf(nameof(IsMaterialAlphaStep))]
    [SerializeField] private Vector3 startVector = Vector3.one;
    [HideIf(nameof(IsMaterialAlphaStep))]
    [SerializeField] private Vector3 targetVector = Vector3.one;
    [ShowIf(nameof(IsMaterialAlphaStep))]
    [SerializeField] [Range(0f,1f)]private float startAlpha = 1f;
    [ShowIf(nameof(IsMaterialAlphaStep))]
    [SerializeField] [Range(0f,1f)]private float targetAlpha = 1f;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float delay;
    [SerializeField] private Ease easeType = Ease.InOutSine;
    [SerializeField] private int loops = -1;
    [SerializeField] private LoopType loopType = LoopType.Yoyo;

    public bool Enabled => enabled;
    public AttackRangePreviewMotionType MotionType => motionType;
    public Vector3 StartVector => startVector;
    public Vector3 TargetVector => targetVector;
    public float StartAlpha => Mathf.Clamp01(startAlpha);
    public float TargetAlpha => Mathf.Clamp01(targetAlpha);
    public float Duration => Mathf.Max(0f, duration);
    public float Delay => Mathf.Max(0f, delay);
    public Ease EaseType => easeType;
    public int Loops => loops == 0 ? -1 : loops;
    public LoopType LoopType => loopType;

    private bool IsMaterialAlphaStep => motionType == AttackRangePreviewMotionType.MaterialAlpha;

    public static AttackRangePreviewMotionStep CreateScaleStep(
        Vector3 startScale,
        Vector3 targetScale,
        float duration,
        float delay,
        Ease easeType,
        LoopType loopType)
    {
        return new AttackRangePreviewMotionStep
        {
            motionType = AttackRangePreviewMotionType.LocalScale,
            startVector = startScale,
            targetVector = targetScale,
            duration = duration,
            delay = delay,
            easeType = easeType,
            loopType = loopType
        };
    }

    public static AttackRangePreviewMotionStep CreateMaterialAlphaStep(
        float startAlpha,
        float targetAlpha,
        float duration,
        float delay,
        Ease easeType,
        LoopType loopType)
    {
        return new AttackRangePreviewMotionStep
        {
            motionType = AttackRangePreviewMotionType.MaterialAlpha,
            startAlpha = startAlpha,
            targetAlpha = targetAlpha,
            duration = duration,
            delay = delay,
            easeType = easeType,
            loopType = loopType
        };
    }
}

public enum AttackRangePreviewMotionType
{
    LocalPosition,
    LocalRotation,
    LocalScale,
    MaterialAlpha
}
