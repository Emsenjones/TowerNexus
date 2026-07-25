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

            IReadOnlyList<MonsterSpawnEntry> spawnEntries = wave.SpawnEntries;

            if (spawnEntries == null || spawnEntries.Count == 0)
            {
                result.AddError(
                    $"Wave {waveIndex} must contain at least one Spawn Entry.");
                continue;
            }

            for (int entryIndex = 0; entryIndex < spawnEntries.Count; entryIndex++)
            {
                ValidateSpawnEntry(
                    result,
                    spawnEntries[entryIndex],
                    waveIndex,
                    entryIndex);
            }
        }

        return result;
    }

    private static void ValidateSpawnEntry(
        MonsterWaveValidationResult result,
        MonsterSpawnEntry spawnEntry,
        int waveIndex,
        int entryIndex)
    {
        string entryLabel = $"Wave {waveIndex} Spawn Entry {entryIndex}";

        if (spawnEntry == null)
        {
            result.AddError($"{entryLabel} is missing.");
            return;
        }

        if (spawnEntry.MonsterDefinition == null)
        {
            result.AddError($"{entryLabel} has no Monster Definition.");
        }
        else if (!spawnEntry.MonsterDefinition.IsValid())
        {
            result.AddError(
                $"{entryLabel} Monster Definition " +
                $"'{spawnEntry.MonsterDefinition.name}' failed owner validation.");
        }

        if (spawnEntry.Count <= 0)
        {
            result.AddError(
                $"{entryLabel} Count must be greater than zero; found {spawnEntry.Count}.");
        }

        if (spawnEntry.SpawnInterval < 0f)
        {
            result.AddError(
                $"{entryLabel} Spawn Interval cannot be negative; " +
                $"found {spawnEntry.SpawnInterval}.");
        }
    }
}
