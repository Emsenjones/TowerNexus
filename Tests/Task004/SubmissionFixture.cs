using System;
using UnityEngine;
class SubmissionFixture
{
    internal readonly BattleRuntimeCoordinator Battle=new BattleRuntimeCoordinator();
    internal TowerPlacementSubmission Submission=>Battle.Submission;
    internal readonly DraftSystem Draft=new DraftSystem();
    internal PendingDraftCollection Owner=>Draft.PendingOwner;
    internal readonly TowerUpgradeSystem System=new TowerUpgradeSystem();
    internal TowerUpgradeDefinition Upgrade=new TowerUpgradeDefinition();
    internal readonly TowerPlacementValidator Validator=new TowerPlacementValidator();
    internal readonly TowerDeployController Deployer=new TowerDeployController();
    internal readonly MonsterManager Monsters=new MonsterManager();
    internal readonly MapGeneratorBehaviour Map=new MapGeneratorBehaviour();
    internal readonly AStarPathfindingService Path=new AStarPathfindingService();
    internal readonly TowerDefinition Definition=new TowerDefinition();
    internal TowerInstance Tower;
    internal TowerBehaviour Behaviour;
    internal TowerCombatBehaviour Combat;
    internal PendingDraftEntry Item;
    internal int EvidenceCount,NotificationCount;
    internal TowerInvestmentCommitObservation LastEvidence;
    private ulong token;
    internal SubmissionFixture(bool seed=true)
    {
        for(int x=0;x<8;x++) {var node=new GridNodeBehaviour{GridPosition=new Vector2Int(x,0),Map=Map};Map.Nodes[node.GridPosition]=node;}
        Path.ActiveMap=Map;Validator.Initialize(Map,Path);Owner.BeginBattle(1);
        Submission.Bind(Battle,Draft,Validator,Deployer,System,Monsters,Map);Submission.BeginBattle();
        System.BindUpgradeRuntime(Battle,Submission);
        System.TryBindStageLevelRules(new[]{new TowerUpgradeDefinition{RequiredTowerLevel=2}},out _);
        Submission.OnInvestmentEvidenceCommitted+=e=>{EvidenceCount++;LastEvidence=e;};
        Submission.OnTowerInvestmentCommitted+=e=>NotificationCount++;
        if(seed)
        {
            var entry=Grant(DraftResult.CreateTowerDraft(Definition));
            var result=Submission.SubmitDeployment(entry,Candidate(0));
            if(!result.IsCommitted)throw new Exception("Fixture deployment: "+result.FailureReason);
            Behaviour=Deployer.Last;Tower=Behaviour.TowerInstance;Behaviour.TryGetComponent(out Combat);
            Item=Grant(DraftResult.CreateTowerUpgradeDraft(Upgrade));
            EvidenceCount=NotificationCount=0;
        }
    }
    internal PendingDraftEntry Grant(DraftResult result)
    {
        if(!Owner.TryPrepareGrant(new[]{result},new[]{new DraftAttemptToken(1,++token)},out var grant)||!Owner.TryCommitGrant(grant))
            throw new Exception("Fixture grant");
        return grant.Entries[0];
    }
    internal TowerPlacementPreview Preview(int x,int size=1)
    {
        var p=new TowerPlacementPreview{TowerDefinition=Definition};p.transform.position=new Vector3(x,0,0);
        var anchors=new System.Collections.Generic.List<Transform>();for(int i=0;i<size;i++)anchors.Add(new Transform{position=new Vector3(x+i,0,0)});
        p.TowerAnchorSet=new TowerAnchorSet{OccupiedAnchors=anchors,CenterAnchor=new Transform{position=p.transform.position}};return p;
    }
    internal TowerPlacementCandidate Candidate(int x,int size=1)
    {
        if(!TowerPlacementCandidate.TryCapture(Validator,Preview(x,size),out var c,out var reason))throw new Exception(reason);
        return c;
    }
    internal void Stop(){Battle.IsBattleActive=false;Submission.CloseBattleGate();Owner.Stop();Submission.StopTrackedTowerCombat();}
    internal void Release(){Battle.IsBattleActive=false;Submission.CloseBattleGate();Owner.Stop();Submission.DestroyTrackedTowers();Owner.Clear();}
    internal bool Apply()=>Submission.SubmitUpgrade(Item,Tower).Outcome==TowerSubmissionOutcome.Committed;
}
