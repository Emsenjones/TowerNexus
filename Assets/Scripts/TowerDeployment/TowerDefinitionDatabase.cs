using System.Collections.Generic;
using UnityEngine;

public class TowerDefinitionDatabase : MonoBehaviour
{
    [SerializeField] private List<TowerDefinition> towerDefinitions = new List<TowerDefinition>();

    public IReadOnlyList<TowerDefinition> GetAllTowers()
    {
        return towerDefinitions;
    }
}
