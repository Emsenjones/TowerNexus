using UnityEngine;

public enum TowerInvestmentCommitKind
{
    Deployment = 0,
    LevelUp = 1,
    Upgrade = 2
}

public readonly struct TowerInvestmentCommitObservation
{
    public TowerInvestmentCommitObservation(
        TowerInvestmentCommitKind kind,
        DraftAttemptToken draftAttemptToken,
        DraftResult draftResult,
        TowerInstance towerInstance,
        int previousLevel,
        int currentLevel)
    {
        Kind = kind;
        DraftAttemptToken = draftAttemptToken;
        DraftResult = draftResult;
        TowerInstance = towerInstance;
        PreviousLevel = previousLevel;
        CurrentLevel = currentLevel;
        TowerInstanceId = towerInstance != null ? towerInstance.GetInstanceID() : 0;
        TowerDefinition definition = towerInstance != null ? towerInstance.TowerDefinition : null;
        TowerDisplayName = definition != null ? definition.DisplayName :
            (towerInstance != null ? towerInstance.name : string.Empty);
        TowerFamilyName = definition != null ? definition.TowerFamily.ToString() : string.Empty;
        DraftResultTypeName = draftResult != null ? draftResult.ResultType.ToString() : string.Empty;
        DraftAssetName = draftResult?.Identity != null ? draftResult.Identity.name : string.Empty;
        UpgradeLayerName = draftResult?.TowerUpgradeDefinition != null
            ? draftResult.TowerUpgradeDefinition.UpgradeLayer.ToString() : string.Empty;
    }

    public TowerInvestmentCommitKind Kind { get; }
    public DraftAttemptToken DraftAttemptToken { get; }
    public DraftResult DraftResult { get; }
    public TowerInstance TowerInstance { get; }
    public int PreviousLevel { get; }
    public int CurrentLevel { get; }
    public int TowerInstanceId { get; }
    public string TowerDisplayName { get; }
    public string TowerFamilyName { get; }
    public string DraftResultTypeName { get; }
    public string DraftAssetName { get; }
    public string UpgradeLayerName { get; }
}
