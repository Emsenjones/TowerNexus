using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MonsterWaveValidationResult
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
    fileName = "MonsterWaveConfig",
    menuName = "Tower Nexus/Monster Wave Config"
)]
public class MonsterWaveConfig : ScriptableObject
{
    [SerializeField] private List<MonsterWaveEntry> waves = new List<MonsterWaveEntry>();

    public IReadOnlyList<MonsterWaveEntry> Waves => waves;

    public bool TryGetExpectedMonsterCount(out int expectedMonsterCount)
    {
        expectedMonsterCount = 0;

        if (waves == null || waves.Count == 0)
        {
            return false;
        }

        long totalMonsterCount = 0;

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            MonsterWaveEntry wave = waves[waveIndex];

            if (wave == null || wave.Count <= 0)
            {
                return false;
            }

            totalMonsterCount += wave.Count;

            if (totalMonsterCount > int.MaxValue)
            {
                return false;
            }
        }

        expectedMonsterCount = (int)totalMonsterCount;
        return expectedMonsterCount > 0;
    }

    public MonsterWaveValidationResult ValidateWaveConfig()
    {
        MonsterWaveValidationResult result = new MonsterWaveValidationResult();

        if (waves == null || waves.Count == 0)
        {
            result.AddError("Waves must contain at least one Wave.");
            return result;
        }

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            MonsterWaveEntry wave = waves[waveIndex];

            if (wave == null)
            {
                result.AddError($"Wave entry {waveIndex} is missing.");
                continue;
            }

            if (wave.WaveDelay < 0f)
            {
                result.AddError(
                    $"Wave {waveIndex} has negative Wave Delay {wave.WaveDelay}.");
            }

            ValidateWaveSpawnData(result, wave, waveIndex);
        }

        return result;
    }

    private static void ValidateWaveSpawnData(
        MonsterWaveValidationResult result,
        MonsterWaveEntry wave,
        int waveIndex)
    {
        string waveLabel = $"Wave {waveIndex}";
        MonsterBehaviour monsterRuntimeTemplate =
            wave.MonsterRuntimeTemplate;

        if (monsterRuntimeTemplate == null)
        {
            result.AddError($"{waveLabel} has no Monster Runtime Template.");
        }
        else if (monsterRuntimeTemplate.transform.parent != null)
        {
            result.AddError(
                $"{waveLabel} Monster Runtime Template " +
                $"'{monsterRuntimeTemplate.name}' " +
                "must reference MonsterBehaviour on the prefab root.");
        }
        else if (!monsterRuntimeTemplate.TryValidateAuthoredConfiguration(
                     out string monsterFailureReason))
        {
            result.AddError(
                $"{waveLabel} Monster Runtime Template " +
                $"'{monsterRuntimeTemplate.name}' " +
                $"failed owner validation: {monsterFailureReason}");
        }

        if (wave.Count <= 0)
        {
            result.AddError(
                $"{waveLabel} Count must be greater than zero; " +
                $"found {wave.Count}.");
        }

        if (wave.SpawnInterval < 0f)
        {
            result.AddError(
                $"{waveLabel} Spawn Interval cannot be negative; " +
                $"found {wave.SpawnInterval}.");
        }
    }
}
