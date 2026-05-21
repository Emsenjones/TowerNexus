using UnityEngine;
using Sirenix.OdinInspector;

public class MonsterBehaviour : MonoBehaviour
{
    [SerializeField] private MonsterDefinition definition;
    [ShowInInspector, ReadOnly] private int currentHealth;
    [ShowInInspector, ReadOnly] private float currentMoveSpeed;
    [SerializeField] private Animator animator;

    public MonsterDefinition Definition => definition;
    public int CurrentHealth => currentHealth;
    public float CurrentMoveSpeed => currentMoveSpeed;
    public Animator Animator => animator;

    public void Initialize(MonsterDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("Monster behaviour cannot initialize: monster definition is null.", this);
            return;
        }

        this.definition = definition;
        currentHealth = definition.MaxHealth;
        currentMoveSpeed = definition.MoveSpeed;
        CacheAnimator();
    }

    private void Awake()
    {
        CacheAnimator();
    }

    private void CacheAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
}
