using System;
using UnityEngine;
class SubmissionTests
{
    static int count;
    static void Check(bool b,string s){if(!b)throw new Exception(s);}
    static void Run(string s,Action a){a();count++;Console.WriteLine("PASS "+s);}
    static void NoDeployment(SubmissionFixture f,PendingDraftEntry entry,TowerSubmissionResult r,int x=2)
    {Check(!r.IsCommitted&&!entry.IsConsumed&&f.Submission.DeployedTowerInstances.Count==0&&f.Map.GetNode(new Vector2Int(x,0)).IsWalkable&&f.Monsters.Applied==0,"no partial deployment: "+r.FailureReason);}
    static void Main()
    {
        Run("complete deployment and stable ordered read-only queries",()=>{
            var f=new SubmissionFixture(false);var view=f.Submission.DeployedTowerInstances;
            for(int x=0;x<2;x++){var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));Check(f.Submission.SubmitDeployment(e,f.Candidate(x)).IsCommitted&&e.IsConsumed,"deploy");}
            Check(ReferenceEquals(view,f.Submission.DeployedTowerInstances)&&view.Count==2&&f.EvidenceCount==2,"stable view");
            Check(((System.Collections.Generic.ICollection<TowerInstance>)view).IsReadOnly,"read only");
        });
        Run("complete Level Up entry consumes once and refreshes baseline",()=>{
            var f=new SubmissionFixture();var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));
            Check(f.Submission.SubmitLevelUp(e,f.Tower).IsCommitted&&e.IsConsumed&&f.Tower.CurrentLevel==2&&f.Combat.LevelBaselines==1,"level commit");
            Check(!f.Submission.SubmitLevelUp(e,f.Tower).IsCommitted&&f.EvidenceCount==1,"no replay");
        });
        Run("complete Upgrade entry and committed technical failure distinction",()=>{
            var f=new SubmissionFixture();f.Combat.Result=RequiredUpgradeRefreshResult.TechnicalFailure;
            var r=f.Submission.SubmitUpgrade(f.Item,f.Tower);
            Check(r.Outcome==TowerSubmissionOutcome.CommittedWithTechnicalFailure&&r.IsCommitted&&f.Item.IsConsumed&&f.EvidenceCount==1&&f.Battle.FailureCount==1,"truthful outcome");
        });
        Run("invalid target and wrong reward type reject before mutation",()=>{
            var f=new SubmissionFixture();var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));
            Check(!f.Submission.SubmitLevelUp(e,new TowerInstance()).IsCommitted&&!f.Submission.SubmitUpgrade(e,f.Tower).IsCommitted&&!e.IsConsumed,"target/type");
        });
        Run("blocked route rejects despite earlier preview cache hit",()=>{
            var f=new SubmissionFixture(false);var p=f.Preview(2);Check(f.Validator.CanPlaceTower(p),"preview");
            int n=f.Path.Searches;f.Path.Blocked=true;var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));
            NoDeployment(f,e,f.Submission.SubmitDeployment(e,f.Candidate(2)));Check(f.Path.Searches>n,"fresh final path");
        });
        Run("empty, mismatched definition and old-frame candidates reject",()=>{
            var f=new SubmissionFixture(false);var e=f.Grant(DraftResult.CreateTowerDraft(new TowerDefinition()));
            NoDeployment(f,e,f.Submission.SubmitDeployment(e,f.Candidate(2)));
            var p=f.Preview(2,0);Check(!TowerPlacementCandidate.TryCapture(f.Validator,p,out _,out _),"empty");
            e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));var c=f.Candidate(2);Time.frameCount++;
            NoDeployment(f,e,f.Submission.SubmitDeployment(e,c));
        });
        Run("candidate immutable copy and prepared geometry mismatch reject",()=>{
            var f=new SubmissionFixture(false);var p=f.Preview(2,2);TowerPlacementCandidate.TryCapture(f.Validator,p,out var c,out _);
            p.TowerAnchorSet.OccupiedAnchors[0].position=new Vector3(7,0,0);Check(c.Footprint[0]==f.Map.GetNode(new Vector2Int(2,0)),"snapshot");
            f.Deployer.Mutate=t=>t.transform.localScale=new Vector3(2,2,2);var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));
            NoDeployment(f,e,f.Submission.SubmitDeployment(e,c));Check(f.Deployer.Last.gameObject.DestroyCount==1,"discard prepared");
        });
        Run("multi-cell offset anchor pose retains world rotation and local scale",()=>{
            var f=new SubmissionFixture(false);var p=f.Preview(2,2);
            p.transform.position=new Vector3(1.5f,0,0);p.transform.rotation=new Quaternion{angle=37};p.transform.localScale=new Vector3(2,1,2);
            TowerPlacementCandidate.TryCapture(f.Validator,p,out var c,out _);
            f.Deployer.Mutate=t=>{t.TryGetComponent(out TowerAnchorSet a);a.CenterAnchor.position=new Vector3(2,0,0);};
            var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));
            Check(f.Submission.SubmitDeployment(e,c).IsCommitted&&f.Deployer.Last.TowerInstance.OccupiedNodes.Count==2&&
                f.Deployer.Last.transform.position.x==1.5f&&f.Deployer.Last.transform.rotation.angle==37&&f.Deployer.Last.transform.localScale.x==2,"coherent pose");
        });
        Run("foreign Map nodes rejected even with equal coordinates",()=>{
            var f=new SubmissionFixture(false);var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));var c=f.Candidate(2);
            f.Map.Nodes[new Vector2Int(2,0)]=new GridNodeBehaviour{GridPosition=new Vector2Int(2,0)};
            NoDeployment(f,e,f.Submission.SubmitDeployment(e,c));
        });
        Run("readiness failures preserve all precommit state",()=>{
            foreach(int failure in new[]{0,1,2}){var f=new SubmissionFixture(false);var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));
                if(failure==0)f.Monsters.Ready=false;if(failure==1)f.Deployer.Ready=false;
                if(failure==2)f.Deployer.Mutate=t=>t.VisualController.CurrentTowerModelInstance=null;
                NoDeployment(f,e,f.Submission.SubmitDeployment(e,f.Candidate(2)));}
        });
        Run("Stop and rebind during deployment readiness invalidate commit",()=>{
            foreach(bool stop in new[]{true,false}){var f=new SubmissionFixture(false);var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));
                f.Deployer.OnPrepare=()=>{if(stop)f.Stop();else f.Validator.Initialize(f.Map,f.Path);};
                NoDeployment(f,e,f.Submission.SubmitDeployment(e,f.Candidate(2)));Check(f.Deployer.Last.gameObject.DestroyCount==1,"discard");}
        });
        Run("Stop during Level and Upgrade preflight cannot consume",()=>{
            foreach(bool upgrade in new[]{true,false}){var f=new SubmissionFixture();var e=upgrade?f.Item:f.Grant(DraftResult.CreateTowerDraft(f.Definition));
                f.Combat.OnPrepare=f.Stop;var r=upgrade?f.Submission.SubmitUpgrade(e,f.Tower):f.Submission.SubmitLevelUp(e,f.Tower);
                Check(!r.IsCommitted&&!e.IsConsumed&&f.Tower.CurrentLevel==1&&f.Tower.AppliedUpgrades.Count==0,"stopped readiness");}
        });
        Run("same-context rebind during Level and Upgrade readiness expires operation",()=>{
            foreach(bool upgrade in new[]{true,false}){
                var f=new SubmissionFixture();var e=upgrade?f.Item:f.Grant(DraftResult.CreateTowerDraft(f.Definition));
                f.Combat.OnPrepare=()=>{f.Submission.Bind(f.Battle,f.Draft,f.Validator,f.Deployer,f.System,f.Monsters,f.Map);f.Submission.BeginBattle();};
                var r=upgrade?f.Submission.SubmitUpgrade(e,f.Tower):f.Submission.SubmitLevelUp(e,f.Tower);
                Check(!r.IsCommitted&&!e.IsConsumed&&f.Tower.CurrentLevel==1&&f.Tower.AppliedUpgrades.Count==0,"binding revision");
            }
        });
        Run("outer interaction lease remains busy through UI cleanup and cannot replay",()=>{
            var f=new SubmissionFixture();Check(f.Submission.TryBeginInteraction(out var lease),"lease");
            Check(f.Submission.SubmitUpgrade(f.Item,f.Tower,lease).IsCommitted,"authorized call does not block itself");
            Check(f.Submission.IsBusy&&!f.Submission.CanStartOperation&&!f.System.ApplyDebugUpgrade(f.Tower,new TowerUpgradeDefinition()).IsCommitted,"cleanup guard");
            Check(!f.Submission.SubmitUpgrade(f.Item,f.Tower,lease).IsCommitted,"lease single submit");lease.Dispose();Check(!f.Submission.IsBusy,"released");
        });
        Run("refresh and notifications reject nested public entries",()=>{
            var f=new SubmissionFixture();Action nested=()=>{
                Check(!f.Apply()&&!f.System.ApplyDebugUpgrade(f.Tower,new TowerUpgradeDefinition()).IsCommitted&&!f.Submission.CanStartOperation,"nested rejected");};
            f.Combat.OnRefresh=nested;f.Submission.OnTowerInvestmentCommitted+=e=>nested();Check(f.Apply(),"outer accepted");
        });
        Run("standalone Stop retains members and read-only query has no side effects",()=>{
            var f=new SubmissionFixture();var view=f.Submission.DeployedTowerInstances;f.Combat.OnStop=f.Stop;f.Stop();
            Check(view.Count==1&&!f.Item.IsConsumed&&!f.Submission.OwnsDeployedTower(f.Tower),"retained but closed");
        });
        Run("release detaches first and isolates stop/disable failures",()=>{
            var f=new SubmissionFixture();var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));f.Submission.SubmitDeployment(e,f.Candidate(2));var second=f.Deployer.Last;
            f.Combat.OnStop=()=>{Check(f.Submission.DeployedTowerInstances.Count==0,"detached before stop");f.Release();throw new Exception("stop");};
            f.Behaviour.gameObject.OnDisable=()=>{f.Release();throw new Exception("disable");};f.Release();f.Release();
            Check(f.Behaviour.gameObject.DestroyCount==1&&second.gameObject.DestroyCount==1,"destroy each exactly once");
        });
        Run("investment evidence precedes synchronous release for all intents",()=>{
            foreach(int kind in new[]{0,1,2}){var f=new SubmissionFixture();var entry=kind==2?f.Item:f.Grant(DraftResult.CreateTowerDraft(f.Definition));
                f.Submission.OnTowerInvestmentCommitted+=e=>{Check(f.EvidenceCount==1&&entry.IsConsumed,"evidence first");f.Release();};
                var r=kind==0?f.Submission.SubmitDeployment(entry,f.Candidate(2)):kind==1?f.Submission.SubmitLevelUp(entry,f.Tower):f.Submission.SubmitUpgrade(entry,f.Tower);
                Check(r.IsCommitted&&f.EvidenceCount==1&&f.Submission.DeployedTowerInstances.Count==0,"retained fact");}
        });
        Run("same Submission across Stage reset invalidates old entries and targets",()=>{
            var f=new SubmissionFixture();var identity=f.Submission;var old=f.Item;f.Release();f.Owner.BeginBattle(2);f.Battle.IsBattleActive=true;
            f.Submission.Bind(f.Battle,f.Draft,f.Validator,f.Deployer,f.System,f.Monsters,f.Map);f.Submission.BeginBattle();
            Check(ReferenceEquals(identity,f.Submission)&&!f.Submission.SubmitUpgrade(old,f.Tower).IsCommitted,"old Stage authority");
        });
        Run("production UI entry keeps lease through throwing cleanup",InteractionHarness.TestCleanup);
        Run("production Recorder bindings subscribe once across Stage/re-enable",RecorderBindingHarness.TestSubscriptions);
        Run("deployment lifecycle callbacks see immutable evidence before release",()=>{
            for(int callback=0;callback<3;callback++){
                var f=new SubmissionFixture(false);var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));bool released=false;
                Action release=()=>{Check(f.EvidenceCount==1&&e.IsConsumed,"evidence before callback");released=true;f.Release();};
                if(callback==0)f.Submission.OnTowerDeploymentCommitted+=t=>release();
                if(callback==1)f.Submission.OnPlacementRouteRevisionCommitted+=(t,p,b)=>release();
                if(callback==2)f.Monsters.OnRoute=release;
                Check(f.Submission.SubmitDeployment(e,f.Candidate(2)).IsCommitted&&released&&f.EvidenceCount==1,"callback release");
            }
        });
        Run("production release routing preserves deferred Stage cancellation",ReleaseRoutingHarness.TestRouting);
        Console.WriteLine(count+" complete Submission managed contracts passed.");
    }
}
