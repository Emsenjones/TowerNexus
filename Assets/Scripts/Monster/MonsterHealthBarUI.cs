using UnityEngine;
using UnityEngine.UI;

public class MonsterHealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    private Vector3 worldOffset;
    private Camera targetCamera;

    private RectTransform rectTransform;
    private MonsterBehaviour boundMonster;
    private bool isDisposed;

    public void Initialize(MonsterBehaviour monster, Vector3 offset, Camera camera)
    {
        CleanupSubscriptions();

        boundMonster = monster;
        worldOffset = offset;
        targetCamera = camera;
        isDisposed = false;
        CacheRectTransform();

        if (boundMonster == null)
        {
            Dispose();
            return;
        }

        boundMonster.OnHealthChanged += HandleMonsterHealthChanged;
        boundMonster.OnDied += HandleMonsterRemoved;
        boundMonster.OnTargetReached += HandleMonsterRemoved;
        boundMonster.OnDestroyed += HandleMonsterRemoved;

        int maxHealth = boundMonster.Definition != null ? boundMonster.Definition.MaxHealth : 0;
        Refresh(boundMonster.CurrentHealth, maxHealth);
        UpdatePosition();
    }

    public void SetFillImage(Image image)
    {
        fillImage = image;
    }

    public void Refresh(float currentHealth, float maxHealth)
    {
        if (fillImage == null)
        {
            return;
        }

        float fillAmount = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        fillImage.fillAmount = Mathf.Clamp01(fillAmount);
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        CleanupSubscriptions();

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (isDisposed)
        {
            return;
        }

        if (boundMonster == null)
        {
            Dispose();
            return;
        }

        UpdatePosition();
    }

    private void OnDestroy()
    {
        CleanupSubscriptions();
    }

    private void CacheRectTransform()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }
    }

    private void UpdatePosition()
    {
        if (boundMonster == null)
        {
            return;
        }

        CacheRectTransform();

        if (rectTransform == null)
        {
            return;
        }

        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

        if (cameraToUse == null)
        {
            return;
        }

        Vector3 screenPosition = cameraToUse.WorldToScreenPoint(boundMonster.transform.position + worldOffset);
        rectTransform.position = screenPosition;
    }

    private void HandleMonsterHealthChanged(MonsterBehaviour monster, int currentHealth, int maxHealth)
    {
        Refresh(currentHealth, maxHealth);
    }

    private void HandleMonsterRemoved(MonsterBehaviour monster)
    {
        Dispose();
    }

    private void CleanupSubscriptions()
    {
        if (boundMonster == null)
        {
            return;
        }

        boundMonster.OnHealthChanged -= HandleMonsterHealthChanged;
        boundMonster.OnDied -= HandleMonsterRemoved;
        boundMonster.OnTargetReached -= HandleMonsterRemoved;
        boundMonster.OnDestroyed -= HandleMonsterRemoved;
        boundMonster = null;
    }
}
