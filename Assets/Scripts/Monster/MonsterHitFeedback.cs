using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterHitFeedback : MonoBehaviour
{
    private const string BaseColorPropertyName = "_BaseColor";
    private const string ColorPropertyName = "_Color";

    private static readonly int BaseColorPropertyId = Shader.PropertyToID(BaseColorPropertyName);
    private static readonly int ColorPropertyId = Shader.PropertyToID(ColorPropertyName);

    [SerializeField] private Animator animator;
    [SerializeField] private Renderer[] hitFlashRenderers;
    [SerializeField] private string getHitTriggerName;
    [SerializeField] private bool enableHitFlash = true;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.08f;

    private readonly List<MaterialColorBinding> colorBindings = new List<MaterialColorBinding>();
    private Coroutine flashRoutine;

    public bool TryInitialize(out string failureReason)
    {
        StopFeedback();
        CacheAnimator();
        CacheRenderers();
        CacheOriginalColors();

        if (!TryValidateAuthoredConfiguration(out failureReason))
        {
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryValidateAuthoredConfiguration(out string failureReason)
    {
        if (hitFlashDuration < 0f)
        {
            failureReason =
                $"Hit Flash Duration cannot be negative; found {hitFlashDuration}.";
            return false;
        }

        Animator resolvedAnimator =
            animator != null ? animator : GetComponentInChildren<Animator>(true);

        if (!string.IsNullOrEmpty(getHitTriggerName) &&
            resolvedAnimator == null)
        {
            failureReason =
                "Get-Hit Animator Trigger requires an Animator.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void PlayHitFeedback()
    {
        PlayHitAnimation();
        PlayHitFlash();
    }

    public void StopFeedback()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreOriginalColors();
    }

    private void OnDisable()
    {
        StopFeedback();
    }

    private void OnDestroy()
    {
        StopFeedback();
    }

    private void CacheAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void CacheRenderers()
    {
        if (hitFlashRenderers != null && hitFlashRenderers.Length > 0)
        {
            return;
        }

        hitFlashRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void CacheOriginalColors()
    {
        colorBindings.Clear();

        if (hitFlashRenderers == null)
        {
            return;
        }

        for (int rendererIndex = 0; rendererIndex < hitFlashRenderers.Length; rendererIndex++)
        {
            Renderer targetRenderer = hitFlashRenderers[rendererIndex];

            if (targetRenderer == null)
            {
                continue;
            }

            Material[] materials = targetRenderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];

                if (material == null)
                {
                    continue;
                }

                if (TryGetColorProperty(material, out int propertyId, out Color originalColor))
                {
                    colorBindings.Add(new MaterialColorBinding(targetRenderer, materialIndex, propertyId, originalColor));
                }
            }
        }
    }

    private bool TryGetColorProperty(Material material, out int propertyId, out Color color)
    {
        if (material.HasProperty(BaseColorPropertyId))
        {
            propertyId = BaseColorPropertyId;
            color = material.GetColor(BaseColorPropertyId);
            return true;
        }

        if (material.HasProperty(ColorPropertyId))
        {
            propertyId = ColorPropertyId;
            color = material.GetColor(ColorPropertyId);
            return true;
        }

        propertyId = 0;
        color = Color.white;
        return false;
    }

    private void PlayHitAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(getHitTriggerName))
        {
            return;
        }

        if (!HasAnimatorTrigger(getHitTriggerName))
        {
            return;
        }

        animator.SetTrigger(getHitTriggerName);
    }

    private bool HasAnimatorTrigger(string triggerName)
    {
        if (animator.runtimeAnimatorController == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayHitFlash()
    {
        if (!enableHitFlash ||
            hitFlashDuration <= 0f ||
            colorBindings.Count == 0)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            RestoreOriginalColors();
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        ApplyColor(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        RestoreOriginalColors();
        flashRoutine = null;
    }

    private void ApplyColor(Color color)
    {
        for (int i = 0; i < colorBindings.Count; i++)
        {
            colorBindings[i].ApplyColor(color);
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < colorBindings.Count; i++)
        {
            colorBindings[i].RestoreOriginalColor();
        }
    }

    private sealed class MaterialColorBinding
    {
        private readonly Renderer targetRenderer;
        private readonly int materialIndex;
        private readonly int propertyId;
        private readonly Color originalColor;
        private readonly MaterialPropertyBlock propertyBlock;

        public MaterialColorBinding(Renderer targetRenderer, int materialIndex, int propertyId, Color originalColor)
        {
            this.targetRenderer = targetRenderer;
            this.materialIndex = materialIndex;
            this.propertyId = propertyId;
            this.originalColor = originalColor;
            propertyBlock = new MaterialPropertyBlock();
        }

        public void ApplyColor(Color color)
        {
            SetColor(color);
        }

        public void RestoreOriginalColor()
        {
            SetColor(originalColor);
        }

        private void SetColor(Color color)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
            propertyBlock.SetColor(propertyId, color);
            targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
        }
    }
}
