using System;
using System.Collections.Generic;
using UnityEngine;
class SAMPLER
{
    private TowerPlacementSubmission submission=new TowerPlacementSubmission();
    private List<TowerUpgradeDefinition> upgradeDefinitions;
    private TowerUpgradeSystem towerUpgradeSystem = new TowerUpgradeSystem();
    private TowerPlacementController towerPlacementController = new TowerPlacementController();
    private BattleHUDUI battleHUDUI = new BattleHUDUI();
    private readonly PendingDraftCollection owner = new PendingDraftCollection();
    private IReadOnlyList<PendingDraftEntry> PendingDrafts => owner.Held;
    private List<DraftResult> draftPool = new List<DraftResult>(), towerDraftCandidatePool = new List<DraftResult>(),
        upgradeDraftCandidatePool = new List<DraftResult>(), draftChoices = new List<DraftResult>();
    private HashSet<UnityEngine.Object> displayedIdentities = new HashSet<UnityEngine.Object>();
    private Random draftRandom;
    private int draftChoiceCount = 3;
    private float towerDraftSlotProbability = 0.35f;
    private int CountEligibleTowerInstancesForUpgrade(TowerUpgradeDefinition upgrade, IReadOnlyList<TowerInstance> towers) => 3;
    public string Trace(int seed, List<TowerUpgradeDefinition> upgrades, List<TowerDefinition> towers, int state)
    {
        upgradeDefinitions = upgrades; draftRandom = new Random(seed); owner.BeginBattle(1);
        foreach (var tower in towers) towerDraftCandidatePool.Add(DraftResult.CreateTowerDraft(tower));
        owner.TryPrepareGrant(new[]{DraftResult.CreateTowerUpgradeDraft(upgrades[0]),
            DraftResult.CreateTowerUpgradeDraft(upgrades[2])},
            new[]{new DraftAttemptToken(1,1),new DraftAttemptToken(1,2)}, out var grant);
        if (state > 0) owner.TryCommitGrant(grant);
        if (state > 1) { owner.TryPrepareConsumption(owner.Held[0],out var ticket); owner.TryCommitConsumption(ticket); }
        foreach(var entry in owner.Held) battleHUDUI.Views.Add(new PendingDraftUIItem { Entry = entry });
        string weights = "";
        foreach(var upgrade in upgrades) weights += CountPendingReservedCapacityForUpgrade(upgrade)+",";
        AddTowerUpgradeDraftCandidates(); GenerateLevelUpDraftChoices(); ShuffleDraftChoices();
        string choices = "";
        foreach(var choice in draftChoices) choices += choice.Identity.name+",";
        return weights+"|"+choices+"|"+draftRandom.Next();
    }
