using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "MonsterDefinition",
    menuName = "Tower Nexus/Monster Definition"
)]
public class MonsterDefinition : ScriptableObject
{
    [SerializeField] private string monsterId;
    [SerializeField] private string displayName;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int expReward;
    [SerializeField] private int damageToPlayer;
    [SerializeField] private string walkingBoolParameterName = "IsWalking";
    [SerializeField] private string getHitTriggerName;
    [SerializeField] private string dieTriggerName;
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool enableHitFlash = true;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.08f;

    public string MonsterId => monsterId;
    public string DisplayName => displayName;
    public GameObject MonsterPrefab => monsterPrefab;
    public float MoveSpeed => moveSpeed;
    public int MaxHealth => maxHealth;
    public int ExpReward => expReward;
    public int DamageToPlayer => damageToPlayer;
    public string WalkingBoolParameterName => walkingBoolParameterName;
    public string GetHitTriggerName => getHitTriggerName;
    public string DieTriggerName => dieTriggerName;
    public float DeathDelay => deathDelay;
    public Vector3 HealthBarOffset => healthBarOffset;
    public bool EnableHitFlash => enableHitFlash;
    public Color HitFlashColor => hitFlashColor;
    public float HitFlashDuration => hitFlashDuration;

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(monsterId))
        {
            Debug.LogWarning("Monster definition is invalid: monster id is missing.", this);
            return false;
        }

        if (monsterPrefab == null)
        {
            Debug.LogWarning($"Monster definition '{monsterId}' is invalid: monster prefab is not assigned.", this);
            return false;
        }

        if (!monsterPrefab.TryGetComponent(out MonsterBehaviour _))
        {
            Debug.LogWarning($"Monster definition '{monsterId}' is invalid: monster prefab root is missing a MonsterBehaviour component.", monsterPrefab);
            return false;
        }

        if (monsterPrefab.GetComponentInChildren<Animator>() == null)
        {
            Debug.LogWarning($"Monster definition '{monsterId}' is invalid: monster prefab is missing an Animator component.", monsterPrefab);
            return false;
        }

        if (moveSpeed < 0f)
        {
            Debug.LogWarning($"Monster definition '{monsterId}' is invalid: move speed cannot be negative.", this);
            return false;
        }

        if (maxHealth <= 0)
        {
            Debug.LogWarning($"Monster definition '{monsterId}' is invalid: max health must be greater than 0.", this);
            return false;
        }

        if (deathDelay < 0f)
        {
            Debug.LogWarning($"Monster definition '{monsterId}' is invalid: death delay cannot be negative.", this);
            return false;
        }

        if (hitFlashDuration < 0f)
        {
            Debug.LogWarning($"Monster definition '{monsterId}' is invalid: hit flash duration cannot be negative.", this);
            return false;
        }

        return true;
    }
}
