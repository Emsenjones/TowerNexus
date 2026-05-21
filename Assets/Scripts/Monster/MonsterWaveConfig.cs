using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MonsterWaveConfig",
    menuName = "Tower Nexus/Monster Wave Config"
)]
public class MonsterWaveConfig : ScriptableObject
{
    [SerializeField] private List<MonsterWaveEntry> waves = new List<MonsterWaveEntry>();

    public IReadOnlyList<MonsterWaveEntry> Waves => waves;
}
