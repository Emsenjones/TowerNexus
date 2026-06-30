using UnityEngine;

public class DraftResult
{
    private DraftResult(
        DraftResultType resultType,
        TowerDefinition towerDefinition,
        TowerUpgradeDefinition towerUpgradeDefinition)
    {
        ResultType = resultType;
        TowerDefinition = towerDefinition;
        TowerUpgradeDefinition = towerUpgradeDefinition;
    }

    public DraftResultType ResultType { get; }
    public TowerDefinition TowerDefinition { get; }
    public TowerUpgradeDefinition TowerUpgradeDefinition { get; }

    public Object Identity
    {
        get
        {
            switch (ResultType)
            {
                case DraftResultType.TowerDraft:
                    return TowerDefinition;
                case DraftResultType.TowerUpgradeDraft:
                    return TowerUpgradeDefinition;
                default:
                    return null;
            }
        }
    }

    public string DisplayName
    {
        get
        {
            switch (ResultType)
            {
                case DraftResultType.TowerDraft:
                    return GetTowerDisplayName(TowerDefinition);
                case DraftResultType.TowerUpgradeDraft:
                    return GetUpgradeDisplayName(TowerUpgradeDefinition);
                default:
                    return string.Empty;
            }
        }
    }

    public string Description
    {
        get
        {
            switch (ResultType)
            {
                case DraftResultType.TowerDraft:
                    return TowerDefinition != null ? TowerDefinition.Description : string.Empty;
                case DraftResultType.TowerUpgradeDraft:
                    return TowerUpgradeDefinition != null ? TowerUpgradeDefinition.Description : string.Empty;
                default:
                    return string.Empty;
            }
        }
    }

    public Sprite Icon
    {
        get
        {
            switch (ResultType)
            {
                case DraftResultType.TowerDraft:
                    return TowerDefinition != null ? TowerDefinition.Icon : null;
                case DraftResultType.TowerUpgradeDraft:
                    return TowerUpgradeDefinition != null ? TowerUpgradeDefinition.Icon : null;
                default:
                    return null;
            }
        }
    }

    public bool IsValid
    {
        get
        {
            switch (ResultType)
            {
                case DraftResultType.TowerDraft:
                    return TowerDefinition != null;
                case DraftResultType.TowerUpgradeDraft:
                    return TowerUpgradeDefinition != null;
                default:
                    return false;
            }
        }
    }

    public static DraftResult CreateTowerDraft(TowerDefinition towerDefinition)
    {
        return new DraftResult(DraftResultType.TowerDraft, towerDefinition, null);
    }

    public static DraftResult CreateTowerUpgradeDraft(TowerUpgradeDefinition towerUpgradeDefinition)
    {
        return new DraftResult(DraftResultType.TowerUpgradeDraft, null, towerUpgradeDefinition);
    }

    private static string GetTowerDisplayName(TowerDefinition towerDefinition)
    {
        if (towerDefinition == null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(towerDefinition.DisplayName)
            ? towerDefinition.name
            : towerDefinition.DisplayName;
    }

    private static string GetUpgradeDisplayName(TowerUpgradeDefinition towerUpgradeDefinition)
    {
        if (towerUpgradeDefinition == null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(towerUpgradeDefinition.DisplayName)
            ? towerUpgradeDefinition.name
            : towerUpgradeDefinition.DisplayName;
    }
}
