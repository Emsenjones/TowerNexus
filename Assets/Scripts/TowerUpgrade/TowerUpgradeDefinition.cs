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
    [TitleGroup("Progression")]
    [SerializeField] private TowerUpgradeLayer upgradeLayer = TowerUpgradeLayer.Basic;

    [TitleGroup("Basic Layer")]
    [ShowIf(nameof(IsBasicLayerUpgrade))]
    [SerializeField] private List<TowerUpgradeStatDelta> basicStatDeltas = new List<TowerUpgradeStatDelta>();

    [TitleGroup("Behaviour Layer")]
    [ShowIf(nameof(IsBehaviourLayerUpgrade))]
    [SerializeField] private TowerBehaviourPackageType behaviourPackageType;
    [TitleGroup("Behaviour Layer/Archer Piercing Arrow")]
    [ShowIf(nameof(IsArcherPiercingArrow))]
    [MinValue(1)]
    [SerializeField] private int piercingMaxHitCount = 3;
    [TitleGroup("Behaviour Layer/Archer Scatter Arrow")]
    [ShowIf(nameof(IsArcherScatterArrow))]
    [MinValue(0f)]
    [SerializeField] private float scatterAngleOffset = 15f;
    [TitleGroup("Behaviour Layer/Magic Twin Orbs")]
    [ShowIf(nameof(IsMagicTwinOrbs))]
    [MinValue(1)]
    [SerializeField] private int twinOrbsCount = 2;
    [TitleGroup("Behaviour Layer/Magic Twin Orbs")]
    [ShowIf(nameof(IsMagicTwinOrbs))]
    [SerializeField] private float twinOrbsStartingAngleOffset = 180f;
    [TitleGroup("Behaviour Layer/Drone Twin Drones")]
    [ShowIf(nameof(IsDroneTwinDrones))]
    [MinValue(1)]
    [SerializeField] private int twinDronesCount = 2;
    [TitleGroup("Behaviour Layer/Drone Twin Drones")]
    [ShowIf(nameof(IsDroneTwinDrones))]
    [MinValue(0f)]
    [SerializeField] private float twinDronesTakeOffDelay = 0.15f;

    [TitleGroup("Elemental Layer")]
    [ShowIf(nameof(IsElementalLayerUpgrade))]
    [SerializeField] private ElementType elementType;
    [TitleGroup("Elemental Layer")]
    [ShowIf(nameof(IsElementalLayerUpgrade))]
    [SerializeField] private EffectDefinition elementalApplyEffect;

    [TitleGroup("Effect Bindings")]
    [ShowIf(nameof(CanAuthorEffectBindings))]
    [SerializeField] private List<EffectBinding> effectBindings = new List<EffectBinding>();

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public TowerFamily TowerFamily => towerFamily;
    public int RequiredTowerLevel => requiredTowerLevel;
    public TowerUpgradeLayer UpgradeLayer => upgradeLayer;
    public IReadOnlyList<TowerUpgradeStatDelta> BasicStatDeltas => basicStatDeltas;
    public TowerBehaviourPackageType BehaviourPackageType => behaviourPackageType;
    public IReadOnlyList<EffectBinding> EffectBindings => effectBindings;
    public int PiercingMaxHitCount => Mathf.Max(1, piercingMaxHitCount);
    public float ScatterAngleOffset => Mathf.Max(0f, scatterAngleOffset);
    public int TwinOrbsCount => Mathf.Clamp(twinOrbsCount, 1, 2);
    public float TwinOrbsStartingAngleOffset => twinOrbsStartingAngleOffset;
    public int TwinDronesCount => Mathf.Clamp(twinDronesCount, 1, 2);
    public float TwinDronesTakeOffDelay => Mathf.Max(0f, twinDronesTakeOffDelay);
    public ElementType ElementType => elementType;
    public EffectDefinition ElementalApplyEffect => elementalApplyEffect;

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

        if (!AreEffectBindingsValid(logWarnings))
        {
            isValid = false;
        }

        return isValid;
    }

    private bool IsRequiredLevelContentValid(bool logWarnings)
    {
        bool isValid = true;
        bool hasBasicStatDeltas = basicStatDeltas != null && basicStatDeltas.Count > 0;
        bool hasBehaviourPackageType = behaviourPackageType != TowerBehaviourPackageType.None;
        bool hasEffectBindings = HasEffectBindings();

        if (IsBasicLayerUpgrade())
        {
            if (!hasBasicStatDeltas)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Basic layer upgrades need at least one additive stat delta.");
                isValid = false;
            }

            if (hasBehaviourPackageType)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Basic layer upgrades should not define a behaviour package type.");
                isValid = false;
            }

            if (hasEffectBindings)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Basic layer upgrades should not define Effect bindings.");
                isValid = false;
            }
        }
        else if (IsBehaviourLayerUpgrade())
        {
            if (!hasBehaviourPackageType && !hasEffectBindings)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Behaviour layer upgrades need a behaviour package type, at least one Effect binding, or both.");
                isValid = false;
            }

            if (hasBasicStatDeltas)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Behaviour layer upgrades should not define Basic stat deltas in v1.");
                isValid = false;
            }

            if (hasBehaviourPackageType && !IsBehaviourPackageCompatibleWithTowerFamily(logWarnings))
            {
                isValid = false;
            }

            if (hasBehaviourPackageType && !AreBehaviourPackageParametersValid(logWarnings))
            {
                isValid = false;
            }
        }
        else if (IsElementalLayerUpgrade())
        {
            if (hasBasicStatDeltas)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Elemental layer upgrades should not define Basic stat deltas.");
                isValid = false;
            }

            if (hasBehaviourPackageType)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Elemental layer upgrades should not define a behaviour package type.");
                isValid = false;
            }

            if (elementType == ElementType.None)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Elemental layer upgrades need a non-None element type.");
                isValid = false;
            }

            if (elementalApplyEffect == null)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Elemental layer upgrades need a direct Elemental apply effect.");
                isValid = false;
            }

            if (hasEffectBindings)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Elemental layer upgrades must not define generic Effect bindings.");
                isValid = false;
            }
        }

        return isValid;
    }

    private bool IsBehaviourPackageCompatibleWithTowerFamily(bool logWarnings)
    {
        switch (behaviourPackageType)
        {
            case TowerBehaviourPackageType.ArcherPiercingArrow:
            case TowerBehaviourPackageType.ArcherScatterArrow:
                return WarnIfBehaviourPackageTowerFamilyMismatch(logWarnings, TowerFamily.Archer);
            case TowerBehaviourPackageType.MagicTwinOrbs:
                return WarnIfBehaviourPackageTowerFamilyMismatch(logWarnings, TowerFamily.Magic);
            case TowerBehaviourPackageType.DroneTwinDrones:
                return WarnIfBehaviourPackageTowerFamilyMismatch(logWarnings, TowerFamily.Drone);
            case TowerBehaviourPackageType.None:
                return true;
            default:
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: unsupported behaviour package type '{behaviourPackageType}'.");
                return false;
        }
    }

    private bool WarnIfBehaviourPackageTowerFamilyMismatch(bool logWarnings, TowerFamily expectedTowerFamily)
    {
        if (towerFamily == expectedTowerFamily)
        {
            return true;
        }

        Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: behaviour package '{behaviourPackageType}' requires TowerFamily '{expectedTowerFamily}'.");
        return false;
    }

    private bool AreBehaviourPackageParametersValid(bool logWarnings)
    {
        bool isValid = true;

        if (IsArcherPiercingArrow() && piercingMaxHitCount < 1)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Piercing Arrow max hit count must be at least 1.");
            isValid = false;
        }

        if (IsArcherScatterArrow() && scatterAngleOffset < 0f)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Scatter Arrow angle offset cannot be negative.");
            isValid = false;
        }

        if (IsMagicTwinOrbs() && (twinOrbsCount < 1 || twinOrbsCount > 2))
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Twin Orbs count must be between 1 and 2 in v1.");
            isValid = false;
        }

        if (IsDroneTwinDrones() && (twinDronesCount < 1 || twinDronesCount > 2))
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Twin Drones count must be between 1 and 2 in v1.");
            isValid = false;
        }

        if (IsDroneTwinDrones() && twinDronesTakeOffDelay < 0f)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Twin Drones takeoff delay cannot be negative.");
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

    private bool AreEffectBindingsValid(bool logWarnings)
    {
        if (effectBindings == null)
        {
            return true;
        }

        bool isValid = true;

        for (int i = 0; i < effectBindings.Count; i++)
        {
            EffectBinding effectBinding = effectBindings[i];

            if (effectBinding == null)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Effect binding at index {i} is missing.");
                isValid = false;
                continue;
            }

            if (!effectBinding.IsValid())
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Effect binding at index {i} is missing a valid EffectDefinition.");
                isValid = false;
            }
        }

        return isValid;
    }

    private bool HasEffectBindings()
    {
        return effectBindings != null && effectBindings.Count > 0;
    }

    private bool IsBasicLayerUpgrade()
    {
        return upgradeLayer == TowerUpgradeLayer.Basic;
    }

    private bool IsBehaviourLayerUpgrade()
    {
        return upgradeLayer == TowerUpgradeLayer.Behaviour;
    }

    private bool IsElementalLayerUpgrade()
    {
        return upgradeLayer == TowerUpgradeLayer.Elemental;
    }

    private bool CanAuthorEffectBindings()
    {
        return IsBehaviourLayerUpgrade();
    }

    private bool IsArcherPiercingArrow()
    {
        return behaviourPackageType == TowerBehaviourPackageType.ArcherPiercingArrow;
    }

    private bool IsArcherScatterArrow()
    {
        return behaviourPackageType == TowerBehaviourPackageType.ArcherScatterArrow;
    }

    private bool IsMagicTwinOrbs()
    {
        return behaviourPackageType == TowerBehaviourPackageType.MagicTwinOrbs;
    }

    private bool IsDroneTwinDrones()
    {
        return behaviourPackageType == TowerBehaviourPackageType.DroneTwinDrones;
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
