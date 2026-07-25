using UnityEngine;

[CreateAssetMenu(
    fileName = "MonsterDefinition",
    menuName = "Tower Nexus/Monster Definition"
)]
public class MonsterDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private string walkingBoolParameterName = "IsWalking";
    [SerializeField] private string getHitTriggerName;
    [SerializeField] private string dieTriggerName;
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool enableHitFlash = true;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Vector3 damageNumberOffset = new Vector3(0f, 1.5f, 0f);

    public string DisplayName => displayName;
    public GameObject MonsterPrefab => monsterPrefab;
    public float MoveSpeed => moveSpeed;
    public int MaxHealth => maxHealth;
    public string WalkingBoolParameterName => walkingBoolParameterName;
    public string GetHitTriggerName => getHitTriggerName;
    public string DieTriggerName => dieTriggerName;
    public float DeathDelay => deathDelay;
    public Vector3 HealthBarOffset => healthBarOffset;
    public bool EnableHitFlash => enableHitFlash;
    public Color HitFlashColor => hitFlashColor;
    public float HitFlashDuration => hitFlashDuration;
    public Vector3 DamageNumberOffset => damageNumberOffset;

    public bool IsValid()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning($"Monster definition '{name}' is invalid: monster prefab is not assigned.", this);
            return false;
        }

        if (!monsterPrefab.TryGetComponent(out MonsterBehaviour _))
        {
            Debug.LogWarning($"Monster definition '{name}' is invalid: monster prefab root is missing a MonsterBehaviour component.", monsterPrefab);
            return false;
        }

        if (monsterPrefab.GetComponentInChildren<Animator>() == null)
        {
            Debug.LogWarning($"Monster definition '{name}' is invalid: monster prefab is missing an Animator component.", monsterPrefab);
            return false;
        }

        if (moveSpeed < 0f)
        {
            Debug.LogWarning($"Monster definition '{name}' is invalid: move speed cannot be negative.", this);
            return false;
        }

        if (maxHealth <= 0)
        {
            Debug.LogWarning($"Monster definition '{name}' is invalid: max health must be greater than 0.", this);
            return false;
        }

        if (deathDelay < 0f)
        {
            Debug.LogWarning($"Monster definition '{name}' is invalid: death delay cannot be negative.", this);
            return false;
        }

        if (hitFlashDuration < 0f)
        {
            Debug.LogWarning($"Monster definition '{name}' is invalid: hit flash duration cannot be negative.", this);
            return false;
        }

        return true;
    }
}
