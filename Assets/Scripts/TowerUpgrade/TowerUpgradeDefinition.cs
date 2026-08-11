using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
    [TitleGroup("Behaviour Layer/Archer Explosive Arrow")]
    [ShowIf(nameof(IsArcherExplosiveArrow))]
    [SerializeField] private EffectDefinition explosiveArrowEffect;
    [FormerlySerializedAs("twinOrbsCount")]
    [TitleGroup("Behaviour Layer/Magic Multi Orbs")]
    [ShowIf(nameof(IsMagicMultiOrbs))]
    [MinValue(2)]
    [SerializeField] private int multiOrbsCount = 2;
    [FormerlySerializedAs("twinDronesCount")]
    [TitleGroup("Behaviour Layer/Drone Multi Drones")]
    [ShowIf(nameof(IsDroneMultiDrones))]
    [MinValue(2)]
    [SerializeField] private int overrideMaximumDroneCount = 2;
    [TitleGroup("Behaviour Layer/Cannon Explosive Shell")]
    [ShowIf(nameof(IsCannonExplosiveShell))]
    [SerializeField] private EffectDefinition explosiveShellEffect;
    [FormerlySerializedAs("twinShellsMaxInitialShellCount")]
    [TitleGroup("Behaviour Layer/Cannon Multi Shells")]
    [ShowIf(nameof(IsCannonMultiShells))]
    [MinValue(2)]
    [SerializeField] private int multiShellsMaxInitialShellCount = 2;
    [TitleGroup("Behaviour Layer/Cannon Multi Shells")]
    [ShowIf(nameof(IsCannonMultiShells))]
    [LabelText("Additional Shell Damage")]
    [MinValue(1)]
    [SerializeField] private int multiShellsAdditionalDamage = 3;
    [TitleGroup("Behaviour Layer/Cannon Bouncing Shell")]
    [ShowIf(nameof(IsCannonBouncingShell))]
    [MinValue(0.01f)]
    [SerializeField] private float bounceSearchRadius = 1f;
    [TitleGroup("Behaviour Layer/Cannon Bouncing Shell")]
    [ShowIf(nameof(IsCannonBouncingShell))]
    [MinValue(1)]
    [SerializeField] private int maxBounceCount = 1;
    [TitleGroup("Behaviour Layer/Cannon Bouncing Shell")]
    [ShowIf(nameof(IsCannonBouncingShell))]
    [MinValue(0f)]
    [SerializeField] private float bounceArcHeight = 1f;
    [TitleGroup("Behaviour Layer/Cannon Bouncing Shell")]
    [ShowIf(nameof(IsCannonBouncingShell))]
    [SerializeField] private TargetSelectionType bounceTargetSelectionType = TargetSelectionType.Nearest;
    [TitleGroup("Behaviour Layer/Cannon Bouncing Shell")]
    [ShowIf(nameof(IsCannonBouncingShell))]
    [LabelText("Bounce Damage")]
    [MinValue(1)]
    [SerializeField] private int bounceDamage = 3;
    [TitleGroup("Behaviour Layer/Magic Arcane Detonation")]
    [ShowIf(nameof(IsMagicArcaneDetonation))]
    [SerializeField] private EffectDefinition arcaneDetonationEffect;
    [FormerlySerializedAs("magicArcaneFieldVfxPrefab")]
    [TitleGroup("Behaviour Layer/Magic Arcane Field")]
    [ShowIf(nameof(IsMagicArcaneField))]
    [Required]
    [SerializeField] private GameObject magicArcaneFieldPrefab;
    [TitleGroup("Behaviour Layer/Drone Blast Rounds")]
    [ShowIf(nameof(IsDroneBlastRounds))]
    [SerializeField] private EffectDefinition blastRoundsEffect;
    [TitleGroup("Behaviour Layer/Drone Final Dive")]
    [ShowIf(nameof(IsDroneFinalDive))]
    [MinValue(0.01f)]
    [SerializeField] private float finalDiveHitThreshold = 1f;
    [TitleGroup("Behaviour Layer/Drone Final Dive")]
    [ShowIf(nameof(IsDroneFinalDive))]
    [SerializeField] private EffectDefinition finalDiveExplosionEffect;

    [TitleGroup("Elemental Layer")]
    [ShowIf(nameof(IsElementalLayerUpgrade))]
    [SerializeField] private ElementType elementType;
    [TitleGroup("Elemental Layer")]
    [ShowIf(nameof(IsElementalLayerUpgrade))]
    [SerializeField] private EffectDefinition elementalApplyEffect;

    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public TowerFamily TowerFamily => towerFamily;
    public int RequiredTowerLevel => requiredTowerLevel;
    public TowerUpgradeLayer UpgradeLayer => upgradeLayer;
    public IReadOnlyList<TowerUpgradeStatDelta> BasicStatDeltas => basicStatDeltas;
    public TowerBehaviourPackageType BehaviourPackageType => behaviourPackageType;
    public int PiercingMaxHitCount => Mathf.Max(1, piercingMaxHitCount);
    public float ScatterAngleOffset => Mathf.Max(0f, scatterAngleOffset);
    public EffectDefinition ExplosiveArrowEffect => explosiveArrowEffect;
    public int MultiOrbsCount => Mathf.Max(2, multiOrbsCount);
    public int OverrideMaximumDroneCount => Mathf.Max(2, overrideMaximumDroneCount);
    public EffectDefinition ExplosiveShellEffect => explosiveShellEffect;
    public int MultiShellsMaxInitialShellCount => Mathf.Max(2, multiShellsMaxInitialShellCount);
    public int MultiShellsAdditionalDamage => Mathf.Max(1, multiShellsAdditionalDamage);
    public float BounceSearchRadius => Mathf.Max(0.01f, bounceSearchRadius);
    public int MaxBounceCount => Mathf.Max(1, maxBounceCount);
    public float BounceArcHeight => Mathf.Max(0f, bounceArcHeight);
    public TargetSelectionType BounceTargetSelectionType => bounceTargetSelectionType;
    public int BounceDamage => Mathf.Max(1, bounceDamage);
    public EffectDefinition ArcaneDetonationEffect => arcaneDetonationEffect;
    public GameObject MagicArcaneFieldPrefab => magicArcaneFieldPrefab;
    public EffectDefinition BlastRoundsEffect => blastRoundsEffect;
    public float FinalDiveHitThreshold => Mathf.Max(0.01f, finalDiveHitThreshold);
    public EffectDefinition FinalDiveExplosionEffect => finalDiveExplosionEffect;
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

        if (!IsLayerContentValid(logWarnings))
        {
            isValid = false;
        }

        if (!AreBasicStatDeltasValid(logWarnings))
        {
            isValid = false;
        }

        return isValid;
    }

    private bool IsLayerContentValid(bool logWarnings)
    {
        bool isValid = true;
        bool hasBasicStatDeltas = basicStatDeltas != null && basicStatDeltas.Count > 0;
        bool hasBehaviourPackageType = behaviourPackageType != TowerBehaviourPackageType.None;

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
        }
        else if (IsBehaviourLayerUpgrade())
        {
            if (!hasBehaviourPackageType)
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Behaviour layer upgrades need a behaviour package type.");
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
        }

        return isValid;
    }

    private bool IsBehaviourPackageCompatibleWithTowerFamily(bool logWarnings)
    {
        switch (behaviourPackageType)
        {
            case TowerBehaviourPackageType.ArcherPiercingArrow:
            case TowerBehaviourPackageType.ArcherScatterArrow:
            case TowerBehaviourPackageType.ArcherExplosiveArrow:
                return WarnIfBehaviourPackageTowerFamilyMismatch(logWarnings, TowerFamily.Archer);
            case TowerBehaviourPackageType.MagicMultiOrbs:
            case TowerBehaviourPackageType.MagicArcaneDetonation:
            case TowerBehaviourPackageType.MagicArcaneField:
                return WarnIfBehaviourPackageTowerFamilyMismatch(logWarnings, TowerFamily.Magic);
            case TowerBehaviourPackageType.DroneMultiDrones:
            case TowerBehaviourPackageType.DroneBlastRounds:
            case TowerBehaviourPackageType.DroneFinalDive:
                return WarnIfBehaviourPackageTowerFamilyMismatch(logWarnings, TowerFamily.Drone);
            case TowerBehaviourPackageType.CannonExplosiveShell:
            case TowerBehaviourPackageType.CannonMultiShells:
            case TowerBehaviourPackageType.CannonBouncingShell:
                return WarnIfBehaviourPackageTowerFamilyMismatch(logWarnings, TowerFamily.Cannon);
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

        Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' uses behaviour package '{behaviourPackageType}' with TowerFamily '{towerFamily}'; expected TowerFamily '{expectedTowerFamily}'.");
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

        if (IsArcherExplosiveArrow() &&
            !ValidateRequiredAreaEffect(explosiveArrowEffect, "Explosive Arrow", logWarnings))
        {
            isValid = false;
        }

        if (IsMagicMultiOrbs() && multiOrbsCount < 2)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Multi Orbs count must be at least 2.");
            isValid = false;
        }

        if (IsDroneMultiDrones() && overrideMaximumDroneCount < 2)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Multi Drones override maximum count must be at least 2.");
            isValid = false;
        }

        if (IsCannonExplosiveShell() &&
            !ValidateRequiredAreaEffect(explosiveShellEffect, "Explosive Shell", logWarnings))
        {
            isValid = false;
        }

        if (IsCannonMultiShells() && multiShellsMaxInitialShellCount < 2)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Multi Shells maximum initial Shell count must be at least 2.");
            isValid = false;
        }

        if (IsCannonMultiShells() && multiShellsAdditionalDamage <= 0)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Multi Shells additional damage must be greater than zero.");
            isValid = false;
        }

        if (IsCannonBouncingShell() && bounceSearchRadius <= 0f)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Bouncing Shell search radius must be greater than zero.");
            isValid = false;
        }

        if (IsCannonBouncingShell() && maxBounceCount <= 0)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Bouncing Shell max bounce count must be greater than zero.");
            isValid = false;
        }

        if (IsCannonBouncingShell() && bounceArcHeight < 0f)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Bouncing Shell arc height cannot be negative.");
            isValid = false;
        }

        if (IsCannonBouncingShell() && bounceDamage <= 0)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Bouncing Shell damage must be greater than zero.");
            isValid = false;
        }

        if (IsMagicArcaneDetonation() &&
            !ValidateRequiredAreaEffect(arcaneDetonationEffect, "Arcane Detonation", logWarnings))
        {
            isValid = false;
        }

        if (IsMagicArcaneField() && !ValidateMagicArcaneFieldPrefab(logWarnings))
        {
            isValid = false;
        }

        if (IsDroneBlastRounds() &&
            !ValidateRequiredAreaEffect(blastRoundsEffect, "Blast Rounds", logWarnings))
        {
            isValid = false;
        }

        if (IsDroneFinalDive() && finalDiveHitThreshold <= 0f)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Final Dive hit threshold must be greater than zero.");
            isValid = false;
        }

        if (IsDroneFinalDive() &&
            !ValidateRequiredAreaEffect(finalDiveExplosionEffect, "Final Dive explosion", logWarnings))
        {
            isValid = false;
        }

        return isValid;
    }

    private bool ValidateRequiredAreaEffect(
        EffectDefinition effectDefinition,
        string packageDisplayName,
        bool logWarnings)
    {
        if (effectDefinition == null)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: {packageDisplayName} requires an EffectDefinition.");
            return false;
        }

        bool isValid = effectDefinition.IsValid();

        if (!isValid)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: {packageDisplayName} EffectDefinition failed validation.");
        }

        if (effectDefinition.Radius <= 0f)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: {packageDisplayName} requires an area EffectDefinition with radius greater than zero.");
            isValid = false;
        }

        return isValid;
    }

    private bool ValidateMagicArcaneFieldPrefab(bool logWarnings)
    {
        if (magicArcaneFieldPrefab == null)
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Magic Arcane Field requires a runtime prefab.");
            return false;
        }

        if (!magicArcaneFieldPrefab.TryGetComponent(out MagicArcaneFieldBehaviour fieldBehaviour))
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Magic Arcane Field runtime prefab requires MagicArcaneFieldBehaviour on its root.");
            return false;
        }

        if (!fieldBehaviour.IsAuthoredConfigurationValid())
        {
            Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: Magic Arcane Field prefab has invalid authored data.");
            return false;
        }

        return true;
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

            if (!statDelta.HasFiniteAdditiveValue())
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: {statDelta.StatType} additive value must be finite.");
                isValid = false;
            }

            if (statDelta.RequiresUnitIntervalAdditiveValue() &&
                !statDelta.HasUnitIntervalAdditiveValue())
            {
                Warn(logWarnings, $"Tower upgrade definition '{GetDebugName()}' is invalid: {statDelta.StatType} additive value must be finite and between zero and one inclusive.");
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

    private bool IsArcherPiercingArrow()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.ArcherPiercingArrow;
    }

    private bool IsArcherScatterArrow()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.ArcherScatterArrow;
    }

    private bool IsArcherExplosiveArrow()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.ArcherExplosiveArrow;
    }

    private bool IsMagicMultiOrbs()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.MagicMultiOrbs;
    }

    private bool IsDroneMultiDrones()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.DroneMultiDrones;
    }

    private bool IsCannonExplosiveShell()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.CannonExplosiveShell;
    }

    private bool IsCannonMultiShells()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.CannonMultiShells;
    }

    private bool IsCannonBouncingShell()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.CannonBouncingShell;
    }

    private bool IsMagicArcaneDetonation()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.MagicArcaneDetonation;
    }

    private bool IsMagicArcaneField()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.MagicArcaneField;
    }

    private bool IsDroneBlastRounds()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.DroneBlastRounds;
    }

    private bool IsDroneFinalDive()
    {
        return IsBehaviourLayerUpgrade() &&
               behaviourPackageType == TowerBehaviourPackageType.DroneFinalDive;
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
