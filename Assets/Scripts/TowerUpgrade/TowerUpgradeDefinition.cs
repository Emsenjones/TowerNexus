using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TowerUpgradeDefinition",
    menuName = "Tower Nexus/Tower Upgrade Definition"
)]
public class TowerUpgradeDefinition : ScriptableObject
{
    [TitleGroup("Identity")]
    [SerializeField] private string displayName;
    [TitleGroup("Identity")]
    [TextArea]
    [SerializeField] private string description;
    [TitleGroup("Identity")]
    [SerializeField] private Sprite icon;

    [TitleGroup("Compatibility")]
    [SerializeField] private TowerFamily towerFamily;
    [TitleGroup("Progression")]
    [MinValue(1)]
    [SerializeField] private int requiredTowerLevel = 1;

    [TitleGroup("Basic Layer")]
    [ShowIf(nameof(IsBasicStatUpgrade))]
    [SerializeField] private List<TowerUpgradeStatDelta> basicStatDeltas = new List<TowerUpgradeStatDelta>();

    [TitleGroup("Behaviour Layer")]
    [ShowIf(nameof(IsBehaviourPackageUpgrade))]
    [SerializeField] private string behaviourPackageId;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public TowerFamily TowerFamily => towerFamily;
    public int RequiredTowerLevel => requiredTowerLevel;
    public IReadOnlyList<TowerUpgradeStatDelta> BasicStatDeltas => basicStatDeltas;
    public string BehaviourPackageId => behaviourPackageId;

    public bool IsValid()
    {
        return ValidateContent(true);
    }

    private bool ValidateContent(bool logWarnings)
    {
        bool isValid = true;

        if (requiredTowerLevel < 1)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: required tower level must be at least 1.");
            isValid = false;
        }

        if (!IsRequiredLevelContentValid(logWarnings))
        {
            isValid = false;
        }

        if (!AreBasicStatDeltasValid(logWarnings))
        {
            isValid = false;
        }

        return isValid;
    }

    private bool IsRequiredLevelContentValid(bool logWarnings)
    {
        bool isValid = true;
        bool hasBasicStatDeltas = basicStatDeltas != null && basicStatDeltas.Count > 0;
        bool hasBehaviourPackageId = !string.IsNullOrWhiteSpace(behaviourPackageId);

        if (IsBasicStatUpgrade())
        {
            if (!hasBasicStatDeltas)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Basic layer upgrades need at least one additive stat delta.");
                isValid = false;
            }

            if (hasBehaviourPackageId)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Basic layer upgrades should not define a behaviour package identifier.");
                isValid = false;
            }
        }
        else if (IsBehaviourPackageUpgrade())
        {
            if (!hasBehaviourPackageId)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Behaviour layer upgrades need one behaviour package identifier.");
                isValid = false;
            }

            if (hasBasicStatDeltas)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Behaviour layer upgrades should not define Basic stat deltas in v1.");
                isValid = false;
            }
        }
        else if (IsFutureSynergyUpgrade())
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Required Tower Level {requiredTowerLevel} upgrade authoring data is not defined in v1.");
            isValid = false;
        }

        return isValid;
    }

    private bool AreBasicStatDeltasValid(bool logWarnings)
    {
        if (basicStatDeltas == null)
        {
            return true;
        }

        bool isValid = true;
        HashSet<TowerUpgradeBasicStatType> configuredStatTypes = new HashSet<TowerUpgradeBasicStatType>();

        for (int i = 0; i < basicStatDeltas.Count; i++)
        {
            TowerUpgradeStatDelta statDelta = basicStatDeltas[i];

            if (statDelta == null)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: stat delta at index {i} is missing.");
                isValid = false;
                continue;
            }

            if (!statDelta.IsCompatibleWithTowerFamily(towerFamily))
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: {statDelta.StatType} is not compatible with {towerFamily} upgrades.");
                isValid = false;
            }

            if (statDelta.RequiresWholeNumberAdditiveValue() && !statDelta.HasWholeNumberAdditiveValue())
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: {statDelta.StatType} additive value should be a whole number.");
                isValid = false;
            }

            if (!configuredStatTypes.Add(statDelta.StatType))
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: duplicate stat delta for {statDelta.StatType}.");
                isValid = false;
            }
        }

        return isValid;
    }

    private bool IsBasicStatUpgrade()
    {
        return requiredTowerLevel == 1;
    }

    private bool IsBehaviourPackageUpgrade()
    {
        return requiredTowerLevel == 2;
    }

    private bool IsFutureSynergyUpgrade()
    {
        return requiredTowerLevel >= 3;
    }

    private void Warn(bool logWarnings, string message)
    {
        if (logWarnings)
        {
            Debug.LogWarning(message, this);
        }
    }

    private string GetDebugName()
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        return name;
    }
}
