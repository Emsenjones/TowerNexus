using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class StageValidationResult
{
    private readonly List<string> errors = new List<string>();
    private readonly List<string> warnings = new List<string>();

    public bool IsValid => errors.Count == 0;
    public IReadOnlyList<string> Errors => errors;
    public IReadOnlyList<string> Warnings => warnings;

    internal void AddError(string message)
    {
        errors.Add(message);
    }

    internal void AddWarning(string message)
    {
        warnings.Add(message);
    }
}

[CreateAssetMenu(
    fileName = "StageDefinition",
    menuName = "Tower Nexus/Stage Definition"
)]
public class StageDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [MinValue(1)]
    [SerializeField] private int playerMaxHealth = 10;
    [Tooltip("Resolved Monster progress required for each Player level transition in this Stage.")]
    [MinValue(1)]
    [SerializeField] private List<int> playerProgressRequirements =
        new List<int>();
    [Required]
    [SerializeField] private GameObject mapTemplate;
    [Required]
    [SerializeField] private MonsterWaveConfig monsterWaveConfig;
    [SerializeField] private List<TowerDefinition> towerDraftPool = new List<TowerDefinition>();
    [SerializeField] private List<TowerUpgradeDefinition> towerUpgradeDraftPool =
        new List<TowerUpgradeDefinition>();
    [Range(0f, 1f)]
    [SerializeField] private float towerDraftSlotProbability = 0.5f;
    [MinValue(0)]
    [SerializeField] private int freeRerollCount;
    [SerializeField] private bool showStageIntroduction;
    [SerializeField] private List<TowerDefinition> introducedTowers =
        new List<TowerDefinition>();
    [SerializeField] private List<TowerUpgradeDefinition> introducedTowerUpgrades =
        new List<TowerUpgradeDefinition>();

    public string DisplayName => displayName;
    public int PlayerMaxHealth => playerMaxHealth;
    public IReadOnlyList<int> PlayerProgressRequirements =>
        playerProgressRequirements;
    public GameObject MapTemplate => mapTemplate;
    public MonsterWaveConfig MonsterWaveConfig => monsterWaveConfig;
    public IReadOnlyList<TowerDefinition> TowerDraftPool => towerDraftPool;
    public IReadOnlyList<TowerUpgradeDefinition> TowerUpgradeDraftPool => towerUpgradeDraftPool;
    public float TowerDraftSlotProbability => towerDraftSlotProbability;
    public int FreeRerollCount => freeRerollCount;
    public bool ShowStageIntroduction => showStageIntroduction;
    public IReadOnlyList<TowerDefinition> IntroducedTowers => introducedTowers;
    public IReadOnlyList<TowerUpgradeDefinition> IntroducedTowerUpgrades =>
        introducedTowerUpgrades;

    public StageValidationResult ValidateStage()
    {
        StageValidationResult result = new StageValidationResult();
        ValidatePlayerConfiguration(result);
        ValidateMapTemplate(result);
        ValidateMonsterWaveConfig(result);
        ValidateDraftPools(result);
        ValidateDraftProbability(result);
        if (freeRerollCount < 0) result.AddError("Free Re-roll Count must be non-negative.");
        ValidateIntroduction(result);
        return result;
    }

    private void ValidateDraftProbability(StageValidationResult result)
    {
        if (float.IsNaN(towerDraftSlotProbability) ||
            float.IsInfinity(towerDraftSlotProbability) ||
            towerDraftSlotProbability < 0f ||
            towerDraftSlotProbability > 1f)
        {
            result.AddError(
                "Tower Draft Slot Probability must be finite and within the " +
                $"inclusive range [0, 1], but is {towerDraftSlotProbability}.");
        }
    }

    [Button("Validate Stage")]
    private void ValidateStageFromInspector()
    {
        LogValidationResult(ValidateStage());
    }

    private void ValidatePlayerConfiguration(StageValidationResult result)
    {
        if (playerMaxHealth <= 0)
        {
            result.AddError(
                $"Player Max Health must be positive, but is {playerMaxHealth}.");
        }

        if (playerProgressRequirements == null ||
            playerProgressRequirements.Count == 0)
        {
            result.AddError("Player Progress Requirements must contain at least one entry.");
            return;
        }

        for (int i = 0; i < playerProgressRequirements.Count; i++)
        {
            int requirement = playerProgressRequirements[i];

            if (requirement <= 0)
            {
                result.AddError(
                    $"Player Progress Requirement entry {i} must be positive, " +
                    $"but is {requirement}.");
            }
        }
    }

    private void ValidateMapTemplate(StageValidationResult result)
    {
        if (mapTemplate == null)
        {
            result.AddError("Map template is not assigned.");
            return;
        }

        MapGeneratorBehaviour[] rootMapOwners =
            mapTemplate.GetComponents<MapGeneratorBehaviour>();
        MapGeneratorBehaviour[] allMapOwners =
            mapTemplate.GetComponentsInChildren<MapGeneratorBehaviour>(true);

        if (rootMapOwners.Length != 1)
        {
            result.AddError(
                $"Map template '{mapTemplate.name}' must contain exactly one " +
                "MapGeneratorBehaviour on its root.");
            return;
        }

        if (allMapOwners.Length != 1)
        {
            result.AddError(
                $"Map template '{mapTemplate.name}' must contain exactly one " +
                "MapGeneratorBehaviour in its complete hierarchy.");
            return;
        }

        MapValidationResult mapValidation = rootMapOwners[0].ValidateMap();

        for (int i = 0; i < mapValidation.Errors.Count; i++)
        {
            result.AddError($"Map '{mapTemplate.name}': {mapValidation.Errors[i]}");
        }

        for (int i = 0; i < mapValidation.Warnings.Count; i++)
        {
            result.AddWarning($"Map '{mapTemplate.name}': {mapValidation.Warnings[i]}");
        }
    }

    private void ValidateMonsterWaveConfig(StageValidationResult result)
    {
        if (monsterWaveConfig == null)
        {
            result.AddError("Monster Wave Config is not assigned.");
            return;
        }

        MonsterWaveValidationResult waveValidation =
            monsterWaveConfig.ValidateWaveConfig();

        for (int i = 0; i < waveValidation.Errors.Count; i++)
        {
            result.AddError(
                $"Monster Wave Config '{monsterWaveConfig.name}': " +
                waveValidation.Errors[i]);
        }

        for (int i = 0; i < waveValidation.Warnings.Count; i++)
        {
            result.AddWarning(
                $"Monster Wave Config '{monsterWaveConfig.name}': " +
                waveValidation.Warnings[i]);
        }
    }

    private void ValidateDraftPools(StageValidationResult result)
    {
        HashSet<TowerDefinition> configuredTowers = new HashSet<TowerDefinition>();
        HashSet<TowerFamily> configuredTowerFamilies = new HashSet<TowerFamily>();
        Dictionary<TowerFamily, List<TowerDefinition>> configuredTowersByFamily =
            new Dictionary<TowerFamily, List<TowerDefinition>>();

        if (towerDraftPool == null)
        {
            result.AddError("Tower Draft pool is null.");
        }
        else
        {
            for (int i = 0; i < towerDraftPool.Count; i++)
            {
                TowerDefinition towerDefinition = towerDraftPool[i];

                if (towerDefinition == null)
                {
                    result.AddError($"Tower Draft pool entry {i} is missing.");
                    continue;
                }

                if (!configuredTowers.Add(towerDefinition))
                {
                    result.AddError(
                        $"Tower Draft pool contains duplicate definition " +
                        $"'{towerDefinition.name}'.");
                    continue;
                }

                configuredTowerFamilies.Add(towerDefinition.TowerFamily);

                if (!configuredTowersByFamily.TryGetValue(
                        towerDefinition.TowerFamily,
                        out List<TowerDefinition> familyDefinitions))
                {
                    familyDefinitions = new List<TowerDefinition>();
                    configuredTowersByFamily.Add(
                        towerDefinition.TowerFamily,
                        familyDefinitions);
                }

                familyDefinitions.Add(towerDefinition);

                if (!towerDefinition.IsValid())
                {
                    result.AddError(
                        $"Tower definition '{towerDefinition.name}' failed owner validation.");
                }
            }
        }

        HashSet<TowerUpgradeDefinition> configuredUpgrades =
            new HashSet<TowerUpgradeDefinition>();

        if (towerUpgradeDraftPool == null)
        {
            result.AddError("Tower Upgrade Draft pool is null.");
            return;
        }

        for (int i = 0; i < towerUpgradeDraftPool.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = towerUpgradeDraftPool[i];

            if (upgradeDefinition == null)
            {
                result.AddError($"Tower Upgrade Draft pool entry {i} is missing.");
                continue;
            }

            if (!configuredUpgrades.Add(upgradeDefinition))
            {
                result.AddError(
                    $"Tower Upgrade Draft pool contains duplicate definition " +
                    $"'{upgradeDefinition.name}'.");
                continue;
            }

            if (!upgradeDefinition.IsValid())
            {
                result.AddError(
                    $"Tower Upgrade definition '{upgradeDefinition.name}' failed owner validation.");
            }

            if (!configuredTowerFamilies.Contains(upgradeDefinition.TowerFamily))
            {
                result.AddError(
                    $"Tower Upgrade definition '{upgradeDefinition.name}' targets " +
                    $"TowerFamily '{upgradeDefinition.TowerFamily}', which is not represented " +
                    "by the Stage Tower Draft pool.");
            }
        }

        Dictionary<TowerFamily, int> stageMaximumTowerLevels =
            new Dictionary<TowerFamily, int>();

        if (!TowerUpgradeSystem.TryResolveStageMaximumTowerLevels(
                towerUpgradeDraftPool,
                stageMaximumTowerLevels,
                out string levelRuleFailureReason))
        {
            result.AddError(
                $"Tower Upgrade Draft pool cannot establish Stage Tower Level rules: " +
                levelRuleFailureReason);
            return;
        }

        foreach (KeyValuePair<TowerFamily, int> pair in stageMaximumTowerLevels)
        {
            if (!configuredTowersByFamily.TryGetValue(
                    pair.Key,
                    out List<TowerDefinition> familyDefinitions))
            {
                continue;
            }

            for (int i = 0; i < familyDefinitions.Count; i++)
            {
                TowerDefinition towerDefinition = familyDefinitions[i];
                int maximumConfiguredLevel = towerDefinition.GetMaxConfiguredLevel();

                if (maximumConfiguredLevel >= pair.Value)
                {
                    continue;
                }

                result.AddError(
                    $"Tower definition '{towerDefinition.name}' only configures through " +
                    $"Level {maximumConfiguredLevel}, but Stage Upgrade content for " +
                    $"TowerFamily '{pair.Key}' requires Level {pair.Value}.");
            }
        }
    }

    private void ValidateIntroduction(StageValidationResult result)
    {
        HashSet<TowerDefinition> configuredTowers = new HashSet<TowerDefinition>();

        if (towerDraftPool != null)
        {
            for (int i = 0; i < towerDraftPool.Count; i++)
            {
                TowerDefinition towerDefinition = towerDraftPool[i];

                if (towerDefinition != null)
                {
                    configuredTowers.Add(towerDefinition);
                }
            }
        }

        HashSet<TowerUpgradeDefinition> configuredUpgrades =
            new HashSet<TowerUpgradeDefinition>();

        if (towerUpgradeDraftPool != null)
        {
            for (int i = 0; i < towerUpgradeDraftPool.Count; i++)
            {
                TowerUpgradeDefinition upgradeDefinition =
                    towerUpgradeDraftPool[i];

                if (upgradeDefinition != null)
                {
                    configuredUpgrades.Add(upgradeDefinition);
                }
            }
        }

        bool hasPresentableContent = false;
        HashSet<TowerDefinition> uniqueIntroducedTowers =
            new HashSet<TowerDefinition>();

        if (introducedTowers == null)
        {
            result.AddError("Introduced Towers list is null.");
        }
        else
        {
            for (int i = 0; i < introducedTowers.Count; i++)
            {
                TowerDefinition towerDefinition = introducedTowers[i];

                if (towerDefinition == null)
                {
                    result.AddError($"Introduced Towers entry {i} is missing.");
                    continue;
                }

                if (!uniqueIntroducedTowers.Add(towerDefinition))
                {
                    result.AddError(
                        $"Introduced Towers contains duplicate definition " +
                        $"'{towerDefinition.name}'.");
                }

                if (!configuredTowers.Contains(towerDefinition))
                {
                    result.AddError(
                        $"Introduced Tower '{towerDefinition.name}' is not present " +
                        "in the Stage Tower Draft pool.");
                    continue;
                }

                hasPresentableContent = true;
            }
        }

        HashSet<TowerUpgradeDefinition> uniqueIntroducedUpgrades =
            new HashSet<TowerUpgradeDefinition>();

        if (introducedTowerUpgrades == null)
        {
            result.AddError("Introduced Tower Upgrades list is null.");
        }
        else
        {
            for (int i = 0; i < introducedTowerUpgrades.Count; i++)
            {
                TowerUpgradeDefinition upgradeDefinition =
                    introducedTowerUpgrades[i];

                if (upgradeDefinition == null)
                {
                    result.AddError(
                        $"Introduced Tower Upgrades entry {i} is missing.");
                    continue;
                }

                if (!uniqueIntroducedUpgrades.Add(upgradeDefinition))
                {
                    result.AddError(
                        $"Introduced Tower Upgrades contains duplicate definition " +
                        $"'{upgradeDefinition.name}'.");
                }

                if (!configuredUpgrades.Contains(upgradeDefinition))
                {
                    result.AddError(
                        $"Introduced Tower Upgrade '{upgradeDefinition.name}' is not " +
                        "present in the Stage Tower Upgrade Draft pool.");
                    continue;
                }

                hasPresentableContent = true;
            }
        }

        if (showStageIntroduction && !hasPresentableContent)
        {
            result.AddWarning(
                "Stage Introduction is enabled but contains no presentable Tower " +
                "or Tower Upgrade content.");
        }
    }

    private void LogValidationResult(StageValidationResult result)
    {
        string stageName = GetDebugName();

        for (int i = 0; i < result.Errors.Count; i++)
        {
            Debug.LogError(
                $"Stage '{stageName}' validation failed: {result.Errors[i]}",
                this);
        }

        for (int i = 0; i < result.Warnings.Count; i++)
        {
            Debug.LogWarning(
                $"Stage '{stageName}' validation warning: {result.Warnings[i]}",
                this);
        }

        if (result.IsValid)
        {
            Debug.Log(
                $"Stage '{stageName}' validation succeeded with " +
                $"{result.Warnings.Count} warning(s).",
                this);
        }
    }

    private string GetDebugName()
    {
        return string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}
