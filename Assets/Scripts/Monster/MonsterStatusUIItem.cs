using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonsterStatusUIItem : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform buffIconContainer;
    [SerializeField] private MonsterBuffStatusIconUI buffIconPrefab;

    private readonly List<MonsterBuffStatusIconUI> spawnedBuffIcons = new List<MonsterBuffStatusIconUI>();

    private Vector3 worldOffset;
    private Camera targetCamera;
    private RectTransform rectTransform;
    private MonsterBehaviour boundMonster;
    private bool isDisposed;
    private bool hasReportedMissingIconPrefab;

    public void Initialize(MonsterBehaviour monster, Vector3 offset, Camera camera)
    {
        CleanupSubscriptions();

        boundMonster = monster;
        worldOffset = offset;
        targetCamera = camera;
        isDisposed = false;
        rectTransform = transform as RectTransform;

        if (boundMonster == null)
        {
            Dispose();
            return;
        }

        boundMonster.OnHealthChanged += HandleMonsterHealthChanged;
        boundMonster.OnDied += HandleMonsterRemoved;
        boundMonster.OnTargetReached += HandleMonsterRemoved;
        boundMonster.OnDestroyed += HandleMonsterRemoved;
        boundMonster.OnBuffStateChanged += HandleMonsterBuffStateChanged;

        RefreshHealth(boundMonster.CurrentHealth, boundMonster.Definition != null ? boundMonster.Definition.MaxHealth : 0);
        RefreshBuffIcons(boundMonster.ActiveBuffSnapshots);
        UpdatePosition();
    }

    public void SetFillImage(Image image)
    {
        fillImage = image;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        CleanupSubscriptions();
        ClearBuffIcons();
        Destroy(gameObject);
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
        ClearBuffIcons();
    }

    private void HandleMonsterHealthChanged(MonsterBehaviour monster, int currentHealth, int maxHealth)
    {
        RefreshHealth(currentHealth, maxHealth);
    }

    private void HandleMonsterBuffStateChanged(MonsterBehaviour monster)
    {
        RefreshBuffIcons(monster.ActiveBuffSnapshots);
    }

    private void HandleMonsterRemoved(MonsterBehaviour monster)
    {
        Dispose();
    }

    private void RefreshHealth(float currentHealth, float maxHealth)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(maxHealth > 0f ? currentHealth / maxHealth : 0f);
        }
    }

    private void RefreshBuffIcons(IReadOnlyList<MonsterBuffStateSnapshot> snapshots)
    {
        ClearBuffIcons();

        if (buffIconContainer == null || snapshots == null || snapshots.Count == 0)
        {
            return;
        }

        if (buffIconPrefab == null)
        {
            if (!hasReportedMissingIconPrefab)
            {
                Debug.LogWarning("Monster status UI cannot show Buff icons because Buff Icon Prefab is not assigned.", this);
                hasReportedMissingIconPrefab = true;
            }

            return;
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            MonsterBuffStateSnapshot snapshot = snapshots[i];

            if (snapshot.Definition == null)
            {
                continue;
            }

            MonsterBuffStatusIconUI buffIcon = Instantiate(buffIconPrefab, buffIconContainer);
            buffIcon.Refresh(snapshot);
            spawnedBuffIcons.Add(buffIcon);
        }
    }

    private void ClearBuffIcons()
    {
        for (int i = 0; i < spawnedBuffIcons.Count; i++)
        {
            if (spawnedBuffIcons[i] != null)
            {
                Destroy(spawnedBuffIcons[i].gameObject);
            }
        }

        spawnedBuffIcons.Clear();
    }

    private void UpdatePosition()
    {
        if (boundMonster == null || rectTransform == null)
        {
            return;
        }

        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

        if (cameraToUse != null)
        {
            rectTransform.position = cameraToUse.WorldToScreenPoint(boundMonster.transform.position + worldOffset);
        }
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
        boundMonster.OnBuffStateChanged -= HandleMonsterBuffStateChanged;
        boundMonster = null;
    }
}
