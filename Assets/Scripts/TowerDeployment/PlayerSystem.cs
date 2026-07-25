using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSystem : MonoBehaviour
{
    [SerializeField] private PlayerLevelConfig levelConfig;
    [SerializeField] private int currentLevel = 1;
    [FormerlySerializedAs("currentExp")]
    [SerializeField] private int currentProgress;
    private int maxHealth = 1;
    [SerializeField] private int currentHealth = 10;
    [FormerlySerializedAs("isDead")]
    [SerializeField] private bool isDefeated;

    private readonly List<int> resolvedLevelUps = new List<int>();
    private bool isBattleActive;

    public int CurrentLevel => currentLevel;
    public int CurrentProgress => currentProgress;
    public int RequiredProgress => GetRequiredProgress();
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDefeated => isDefeated;
    public bool IsBattleActive => isBattleActive;

    public event Action OnBattleStateInitialized;
    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnProgressChanged;
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDefeated;

    private void Awake()
    {
        EnsureValidConfigurationAndState();
        isBattleActive = false;
    }

    public bool TryInitializeFreshBattle(int stageMaxHealth)
    {
        if (stageMaxHealth <= 0)
        {
            Debug.LogError(
                $"Player system cannot initialize a fresh battle with invalid " +
                $"maximum health {stageMaxHealth}.",
                this);
            return false;
        }

        isBattleActive = false;
        maxHealth = stageMaxHealth;
        currentLevel = 1;
        currentProgress = 0;
        currentHealth = maxHealth;
        isDefeated = false;
        resolvedLevelUps.Clear();
        OnBattleStateInitialized?.Invoke();
        return true;
    }

    public void BeginBattle()
    {
        if (isDefeated)
        {
            Debug.LogWarning("Player system cannot begin a battle while the player is defeated. Initialize fresh battle state first.", this);
            return;
        }

        isBattleActive = true;
    }

    public void StopBattle()
    {
        isBattleActive = false;
    }

    public bool TryResolveMonster(bool reachedTarget)
    {
        if (!isBattleActive || isDefeated)
        {
            return false;
        }

        EnsureValidConfigurationAndState();
        resolvedLevelUps.Clear();

        currentProgress += 1;
        ResolveLevelThresholds(resolvedLevelUps);

        bool healthChanged = reachedTarget;

        if (healthChanged)
        {
            currentHealth = Mathf.Max(0, currentHealth - 1);
        }

        bool enteredDefeat = currentHealth <= 0;

        if (enteredDefeat)
        {
            isDefeated = true;
        }

        PublishResolvedState(healthChanged, enteredDefeat);
        return true;
    }

    public void DebugAddProgress(int amount)
    {
        if (amount <= 0 || !isBattleActive || isDefeated)
        {
            return;
        }

        EnsureValidConfigurationAndState();
        resolvedLevelUps.Clear();
        currentProgress += amount;
        ResolveLevelThresholds(resolvedLevelUps);

        OnProgressChanged?.Invoke(currentProgress, RequiredProgress);

        for (int i = 0; i < resolvedLevelUps.Count; i++)
        {
            int resolvedLevel = resolvedLevelUps[i];
            OnLevelChanged?.Invoke(resolvedLevel);
            OnLevelUp?.Invoke(resolvedLevel);
        }
    }

    private void PublishResolvedState(bool healthChanged, bool enteredDefeat)
    {
        OnProgressChanged?.Invoke(currentProgress, RequiredProgress);

        for (int i = 0; i < resolvedLevelUps.Count; i++)
        {
            OnLevelChanged?.Invoke(resolvedLevelUps[i]);
        }

        if (healthChanged)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (enteredDefeat)
        {
            OnPlayerDefeated?.Invoke();
            return;
        }

        for (int i = 0; i < resolvedLevelUps.Count; i++)
        {
            OnLevelUp?.Invoke(resolvedLevelUps[i]);
        }
    }

    private void ResolveLevelThresholds(List<int> levelUps)
    {
        while (TryGetRequiredProgressForCurrentLevel(out int requiredProgress) &&
               currentProgress >= requiredProgress)
        {
            currentProgress -= requiredProgress;
            currentLevel++;
            levelUps.Add(currentLevel);
        }
    }

    private int GetRequiredProgress()
    {
        return TryGetRequiredProgressForCurrentLevel(out int requiredProgress)
            ? requiredProgress
            : 0;
    }

    private bool TryGetRequiredProgressForCurrentLevel(out int requiredProgress)
    {
        requiredProgress = 0;

        if (levelConfig == null)
        {
            Debug.LogWarning("Player system cannot level up because level config is not assigned.", this);
            return false;
        }

        return levelConfig.TryGetRequiredProgressForLevel(currentLevel, out requiredProgress);
    }

    private void EnsureValidConfigurationAndState()
    {
        if (currentLevel < 1)
        {
            Debug.LogWarning($"Player level was invalid ({currentLevel}) and has been reset to 1.", this);
            currentLevel = 1;
        }

        if (currentProgress < 0)
        {
            Debug.LogWarning($"Player progress was invalid ({currentProgress}) and has been reset to 0.", this);
            currentProgress = 0;
        }

        if (maxHealth <= 0)
        {
            Debug.LogWarning($"Player max health was invalid ({maxHealth}) and has been reset to 1.", this);
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0 && !isDefeated)
        {
            currentHealth = maxHealth;
        }
    }
}
