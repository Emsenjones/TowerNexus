using System;
using UnityEngine;

public class PlayerLevelSystem : MonoBehaviour
{
    [SerializeField] private PlayerLevelConfig levelConfig;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp;

    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int RequiredExp => GetRequiredExp();

    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelUp;

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
            Debug.LogWarning("Player level system cannot level up because level config is not assigned.", this);
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
    }
}
