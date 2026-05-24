using System;
using UnityEngine;

public class PlayerSystem : MonoBehaviour
{
    [SerializeField] private PlayerLevelConfig levelConfig;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp;
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;
    [SerializeField] private bool isDead;

    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int RequiredExp => GetRequiredExp();
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDead;

    private void Awake()
    {
        EnsureValidRuntimeState();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureValidRuntimeState();

        currentExp += amount;

        if (!TryLevelUp())
        {
            OnExpChanged?.Invoke(currentExp, RequiredExp);
        }
    }

    public void DebugAddExp(int amount)
    {
        AddExp(amount);
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0 || isDead)
        {
            return;
        }

        EnsureValidRuntimeState();

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth > 0)
        {
            return;
        }

        isDead = true;
        OnPlayerDead?.Invoke();
    }

    private bool TryLevelUp()
    {
        bool leveledUp = false;

        while (TryGetRequiredExpForCurrentLevel(out int requiredExp) && currentExp >= requiredExp)
        {
            currentExp -= requiredExp;
            currentLevel++;
            leveledUp = true;

            OnLevelUp?.Invoke(currentLevel);
            OnLevelChanged?.Invoke(currentLevel);
            OnExpChanged?.Invoke(currentExp, RequiredExp);
        }

        return leveledUp;
    }

    private int GetRequiredExp()
    {
        return TryGetRequiredExpForCurrentLevel(out int requiredExp) ? requiredExp : 0;
    }

    private bool TryGetRequiredExpForCurrentLevel(out int requiredExp)
    {
        requiredExp = 0;

        if (levelConfig == null)
        {
            Debug.LogWarning("Player system cannot level up because level config is not assigned.", this);
            return false;
        }

        return levelConfig.TryGetRequiredExpForLevel(currentLevel, out requiredExp);
    }

    private void EnsureValidRuntimeState()
    {
        if (currentLevel < 1)
        {
            Debug.LogWarning($"Player level was invalid ({currentLevel}) and has been reset to 1.", this);
            currentLevel = 1;
        }

        if (currentExp < 0)
        {
            Debug.LogWarning($"Player EXP was invalid ({currentExp}) and has been reset to 0.", this);
            currentExp = 0;
        }

        if (maxHealth <= 0)
        {
            Debug.LogWarning($"Player max health was invalid ({maxHealth}) and has been reset to 1.", this);
            maxHealth = 1;
        }

        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = maxHealth;
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
    }
}
