using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public class PlayerSystem { public int CurrentLevel=1, CurrentProgress, RequiredProgress=1; public event Action<int> OnLevelUp; public List<int> ProgressRequirements = new List<int>{1,2,3}; }
public class TowerPlacementController { public bool CanStartDraftInteraction=true; public void CancelPlacement(){} }
public sealed class DraftViewPreparation : IDisposable
{
    public Action<DraftResult> Selection; public List<DraftResult> Choices;
    public bool Transferred;
    public void Dispose(){}
}
public partial class BattleHUDUI
{
    public bool IsDraftOpen; public bool FailDraftPreparation;
    public Action DuringDraftPreparation, DuringDraftCommit, Reroll;
    public Action<DraftResult> Selection;
    public List<DraftResult> Choices;
    public int Remaining, Toasts;
    public bool Allowed;
    public bool TryValidateBattleReferences(out string reason){reason="";return true;}
    public void ClearStageRuntime(){Views.Clear();CloseDraft();}
    public void BindDraftOwner(DraftSystem d){DraftOwner=d;}
    public void BeginBattle(){isBattleActive=true;}
    public void StopBattle(){isBattleActive=false;CloseDraft();}
    public bool TryOpenDraft(IReadOnlyList<DraftResult> choices,Action<DraftResult> select,out string reason)
    {reason="";Choices=new List<DraftResult>(choices);Selection=select;IsDraftOpen=true;return true;}
    public void BindReroll(int remaining,bool allowed,Action callback){Remaining=remaining;Allowed=allowed;Reroll=callback;}
    public void ShowNoOtherDraftChoices(){Toasts++;}
    public bool TryPrepareDraftChoices(IReadOnlyList<DraftResult> choices,Action<DraftResult> select,
        out DraftViewPreparation prepared,out string reason)
    {
        DuringDraftPreparation?.Invoke();reason="Injected failure";prepared=null;
        if(FailDraftPreparation)return false;
        prepared=new DraftViewPreparation{Choices=new List<DraftResult>(choices),Selection=select};return true;
    }
    public bool CanCommitDraftChoices(DraftViewPreparation prepared)=>IsDraftOpen&&!prepared.Transferred;
    public void CommitDraftChoiceOwnership(DraftViewPreparation prepared)
    { Choices=prepared.Choices;Selection=prepared.Selection;prepared.Transferred=true; }
    public bool FailPresentation;
    public DraftRerollPresentation PresentDraftChoices(DraftViewPreparation prepared,out string reason)
    {
        reason="injected presentation failure";DuringDraftCommit?.Invoke();
        if(!IsDraftOpen)return DraftRerollPresentation.Cancelled;
        return FailPresentation?DraftRerollPresentation.TechnicalFailure:DraftRerollPresentation.Completed;
    }

}
class RerollTests
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    class Fixture
    {
        public DraftSystem D=new DraftSystem();public BattleHUDUI H=new BattleHUDUI();public DraftAttemptToken Token;
        public BattleRuntimeCoordinator Coordinator=new BattleRuntimeCoordinator();
#if UNITY_EDITOR
        public RecorderHarness Recorder;
#endif
        public Fixture(int budget=3,int pool=5,int seed=3)
        {
            D.BindCoordinator(Coordinator);Coordinator.Draft=D;
            Set(D,"playerSystem",new PlayerSystem());Set(D,"towerUpgradeSystem",new TowerUpgradeSystem());
            Set(D,"towerPlacementController",new TowerPlacementController());Set(D,"battleHUDUI",H);
            var towers=new List<TowerDefinition>();for(int i=0;i<pool;i++)towers.Add(new TowerDefinition{name="T"+i});
            Check(D.BindStagePools(towers,new TowerUpgradeDefinition[0],0.5f,budget),"bind");
#if UNITY_EDITOR
            Recorder=new RecorderHarness(D,budget,Coordinator.DiagnosticIdentity);
            Coordinator.BeforeDraftFailureCleanup=Recorder.CaptureTerminal;
#endif
            D.BeginBattle();Set(D,"draftRandom",new System.Random(seed));
            Check(D.TryOpenInitialTowerDraft(out Token,out var reason),"open "+reason);
        }
        public DraftRerollResult Roll()=>D.TryReroll(Token,D.ChoiceSetRevision);
    }
    static string Trace(IReadOnlyList<DraftResult> choices){string s="";foreach(var c in choices)s+=c.Identity.name+",";return s;}
    static void Main()
    {
        var f=new Fixture(1);var old=f.H.Selection;var oldChoices=f.H.Choices;var oldReroll=f.H.Reroll;
        Check(f.Roll()==DraftRerollResult.Success,"last reroll succeeds");
        Check(f.D.FreeRerollsRemaining==0&&f.H.Remaining==0&&f.D.ChoiceSetRevision==2,"one atomic spend");
        Check(Time.timeScale==0&&f.D.IsAwaitingDraft(f.Token)&&f.D.PendingDrafts.Count==0,"pause and opportunity preserved");
        DraftResult common=oldChoices.Find(c=>f.H.Choices.Exists(n=>n.Identity==c.Identity));
        Check(common!=null,"old and new sets overlap");
        old(common);Check(f.D.PendingDrafts.Count==0,"old card rejected even with same reward");
        oldReroll();Check(f.D.FreeRerollsRemaining==0,"old reroll rejected");
        Check(f.Roll()==DraftRerollResult.NoBudget,"zero budget");
        f.H.Selection(f.H.Choices[0]);Check(f.D.PendingDrafts.Count==1&&!f.H.IsDraftOpen,"new set grants one reward");
        Check(f.D.TryConfirmCommittedInitialDraft(f.Token,out _),"initial completion authorization");
        f.D.StopBattle();f.D.BeginBattle();Check(f.D.FreeRerollsRemaining==1,"retry resets configured budget");
        for(int pool=1;pool<=3;pool++)
        {
            f=new Fixture(2,pool);string before=Trace(f.H.Choices);
            Check(f.Roll()==DraftRerollResult.NoOtherCandidates,"saturated pool");
            Check(f.D.FreeRerollsRemaining==2&&Trace(f.H.Choices)==before&&f.H.Toasts==1,"no-spend toast");
            f.Roll();Check(f.H.Toasts==2,"repeated toast request");
        }
        f=new Fixture();var expired=f.Token;f.D.StopBattle();f.D.BeginBattle();
        f.D.TryOpenInitialTowerDraft(out f.Token,out _);Call(f.D,"RollBackOpening",expired);
        Check(f.D.IsAwaitingDraft(f.Token)&&Time.timeScale==0,"expired opening rollback cannot close a new session");
        f=new Fixture(0);Check(f.Roll()==DraftRerollResult.NoBudget&&f.H.IsDraftOpen,"zero authored budget still permits selection");
        Check(!f.D.BindStagePools(new TowerDefinition[0],new TowerUpgradeDefinition[0],0.5f,-1),"negative budget rejected");
        var noDraw=new Fixture(2,3,20);noDraw.Roll();
        var noRequest=new Fixture(2,3,20);
        Check(Get<System.Random>(noDraw.D,"draftRandom").Next()==Get<System.Random>(noRequest.D,"draftRandom").Next(),"noother does not advance RNG");
        var failedDraw=new Fixture(2,5,20);failedDraw.H.FailDraftPreparation=true;failedDraw.Roll();
        var successDraw=new Fixture(2,5,20);successDraw.Roll();
        Check(Get<System.Random>(failedDraw.D,"draftRandom").Next()==Get<System.Random>(successDraw.D,"draftRandom").Next(),"failed prepared generation advances same random stream");
        f=new Fixture(3,3);
        var upgrade=new TowerUpgradeDefinition{name="Upgrade",TowerFamily=TowerFamily.Archer};
        var submission=new TowerPlacementSubmission();var tower=new TowerInstance();tower.Initialize(new TowerDefinition(),null);
        Get<DeployedTowerCollection>(submission,"members").CommitAdd(new TowerBehaviour{TowerInstance=tower});
        f.D.BindSubmission(submission);Set(f.D,"upgradeDefinitions",new[]{upgrade});
        f.D.PendingOwner.TryPrepareGrant(new[]{DraftResult.CreateTowerUpgradeDraft(upgrade)},new[]{new DraftAttemptToken(1,99)},out var reservation);
        f.D.PendingOwner.TryCommitGrant(reservation);
        f.H.Selection(f.H.Choices[0]);Call(f.D,"HandleLevelUp",2);f.Token=Get<DraftAttemptToken>(f.D,"activeToken");
        Check(f.Roll()==DraftRerollResult.NoOtherCandidates,"Pending reservation excludes otherwise eligible upgrade");
        f.D.PendingOwner.TryPrepareConsumption(reservation.Entries[0],out var consumption);f.D.PendingOwner.TryCommitConsumption(consumption);
        Set(f.D,"towerDraftSlotProbability",1f);
        Check(f.Roll()==DraftRerollResult.NoOtherCandidates,"zero probability category without backfill is unreachable");
        Set(f.D,"towerDraftSlotProbability",0.5f);
        Check(f.Roll()==DraftRerollResult.Success,"released reservation becomes reachable alternative");
        f=new Fixture();var baseline=f.H.Choices;
        f.H.FailDraftPreparation=true;
        Check(f.Roll()==DraftRerollResult.PreparationFailed,"injected failure");
        Check(ReferenceEquals(f.H.Choices,baseline)&&f.D.FreeRerollsRemaining==3&&f.D.ChoiceSetRevision==1,"failed prep preserves live set");
        f.H.FailDraftPreparation=false;
        DraftRerollResult nested=DraftRerollResult.Success;
        f.H.DuringDraftPreparation=()=>{nested=f.Roll();f.H.Selection(f.H.Choices[0]);};
        Check(f.Roll()==DraftRerollResult.Success&&nested==DraftRerollResult.Unavailable&&f.D.PendingDrafts.Count==0,"nested request and selection blocked");
        f=new Fixture();f.H.DuringDraftPreparation=f.D.StopBattle;
        Check(f.Roll()==DraftRerollResult.Cancelled&&!f.H.IsDraftOpen&&f.D.FreeRerollsRemaining==3,"cancelled preparation no spend or reopen");
        f=new Fixture();f.H.DuringDraftPreparation=()=>{f.D.StopBattle();f.D.BeginBattle();};
        Check(f.Roll()==DraftRerollResult.Cancelled&&f.D.FreeRerollsRemaining==3&&!f.H.IsDraftOpen,"new battle protected");
        f=new Fixture();f.Roll();var hiddenSelection=f.H.Selection;var hiddenChoice=f.H.Choices[0];f.H.CloseDraft();hiddenSelection(hiddenChoice);
        Check(f.D.PendingDrafts.Count==0,"closed view cannot select even current set");
        f=new Fixture(3);f.Roll();f.H.Selection(f.H.Choices[0]);Call(f.D,"HandleLevelUp",2);
        f.Token=Get<DraftAttemptToken>(f.D,"activeToken");
        Check(f.H.IsDraftOpen&&f.D.FreeRerollsRemaining==2,"levelup shares initial balance");
        Check(f.Roll()==DraftRerollResult.Success&&f.D.FreeRerollsRemaining==1,"levelup consumes shared balance");
        int sameSets=0;
        for(int seed=0;seed<80;seed++)
        {
            var a=new Fixture(2,4,seed);string initial=Trace(a.H.Choices);a.Roll();string rerolled=Trace(a.H.Choices);
            var b=new Fixture(2,4,seed);b.Roll();Check(Trace(b.H.Choices)==rerolled,"seed replay");
            if(initial==rerolled){sameSets++;Check(a.D.FreeRerollsRemaining==1,"identical set still spends");}
        }
        Check(sameSets>0,"identical entire set covered");
#if UNITY_EDITOR
        f=new Fixture();Set(f.D,"useFixedDraftChoices",true);
        Check(f.Roll()==DraftRerollResult.FixedSequence&&f.D.FreeRerollsRemaining==3,"fixed sequence rejected");
        Set(f.D,"useFixedDraftChoices",false);Set(f.D,"useFixedDraftSeed",true);
        var sets=new List<DraftChoicesOpenedObservation>();f.D.OnDraftChoicesOpened+=sets.Add;
        DraftChoiceCommittedObservation selection=null;f.D.OnDraftChoiceCommitted+=x=>selection=x;
        f.Roll();f.Roll();f.H.Selection(f.H.Choices[0]);
        Check(sets.Count==2&&sets[0].DraftOrdinal==1&&sets[1].DraftOrdinal==1,"rerolls retain ordinal");
        Check(sets[0].ChoiceSetRevision==2&&sets[1].ChoiceSetRevision==3&&sets[1].BudgetAfter==1,"set observations");
        Check(selection.ChoiceSetRevision==3,"selection exact set");
        var report=f.Recorder.CreateDraftRuntimeJson();
        Check(report.observedAttemptCount==1&&report.successfulRerollCount==2&&report.committedSelectionCount==1,"recorder opportunity versus exposure");
        Check(report.originalChoiceExposureCount==3&&report.rerollChoiceExposureCount==6,"recorder original and reroll exposures");
        Check(RecorderHarness.DraftGenerationTraceIsConsistent(report),"full generation trace reconciles");
        report.attempts[0].selectedSetRevision=1;
        Check(!RecorderHarness.RerollHistoryIsConsistent(report),"reject stale selected set in evidence");
        report.attempts[0].selectedSetRevision=3;report.remainingFreeRerolls++;
        Check(!RecorderHarness.RerollHistoryIsConsistent(report),"reject budget mismatch");
        report.remainingFreeRerolls--;report.attempts[0].choiceSets[1].revision=3;
        Check(!RecorderHarness.RerollHistoryIsConsistent(report),"reject skipped revision");
        f=new Fixture();
        f.D.OnDraftChoicesOpened+=observation=>
        {
            if(observation.ChoiceSetRevision!=2)return;
            f.Recorder.CaptureTerminal();
            f.D.ClearStageRuntime();
        };
        Check(f.Roll()==DraftRerollResult.Success&&!f.H.IsDraftOpen,"committed observation cancellation cannot reopen");
        report=f.Recorder.CreateDraftRuntimeJson();
        Check(report.successfulRerollCount==1&&report.remainingFreeRerolls==2&&RecorderHarness.RerollHistoryIsConsistent(report),"terminal set and budget captured before cleanup");
        f=new Fixture();f.H.FailDraftPreparation=true;f.Roll();
        report=f.Recorder.CreateDraftRuntimeJson();
        Check(report.successfulRerollCount==0&&report.rerollRequests[0].result=="PreparationFailed"&&RecorderHarness.RerollHistoryIsConsistent(report),"failed requests separate from exposure");
#endif
        f=new Fixture();var committedSet=Get<object>(f.D,"currentChoiceSet");string committedTrace=Trace(f.H.Choices);
        f.H.DuringDraftPreparation=()=>Check(ReferenceEquals(committedSet,Get<object>(f.D,"currentChoiceSet"))&&Trace(f.H.Choices)==committedTrace,"preparation never mutates committed set");
        f.H.FailDraftPreparation=true;f.Roll();
        Check(ReferenceEquals(committedSet,Get<object>(f.D,"currentChoiceSet")),"failed preparation discards isolated set");
        f=new Fixture();f.H.FailPresentation=true;
        Check(f.Roll()==DraftRerollResult.Success&&f.Coordinator.FailureCount==1&&!f.D.IsBattleActive,"Initial committed presentation failure terminates through production route");
#if UNITY_EDITOR
        var failedPresentation=f.Recorder.CreateDraftRuntimeJson();
        Check(failedPresentation.rerollRequests.Count==1&&failedPresentation.rerollRequests[0].committed&&failedPresentation.rerollRequests[0].presentation=="TechnicalFailure","commit and presentation are separate facts");
        Check(f.Recorder.Drained&&RecorderHarness.DraftGenerationTraceIsConsistent(failedPresentation),"real observation scope drains result before finalization");
#endif
        f=new Fixture();f.H.Selection(f.H.Choices[0]);Call(f.D,"HandleLevelUp",2);f.Token=Get<DraftAttemptToken>(f.D,"activeToken");f.H.FailPresentation=true;
        Check(f.Roll()==DraftRerollResult.Success&&f.Coordinator.FailureCount==1&&!f.D.IsBattleActive,"Level-Up presentation failure uses same terminal route");
        f=new Fixture();var oldToken=f.Token;f.H.FailPresentation=true;
        f.H.DuringDraftCommit=()=>{f.D.StopBattle();f.D.BeginBattle();f.D.TryOpenInitialTowerDraft(out f.Token,out _);};
        Check(f.D.TryReroll(oldToken,1)==DraftRerollResult.Success&&f.Coordinator.FailureCount==0&&f.D.IsAwaitingDraft(f.Token),"old presentation failure never terminates replacement battle");
        f.Coordinator.FailDraftPresentation(f.D,oldToken,"late failure");
        Check(f.Coordinator.FailureCount==0,"production failure route rejects stale token");
#if UNITY_EDITOR
        f=new Fixture();int setReentries=0,resultReentries=0;
        Action reenter=()=>
        {
            Check(f.Roll()==DraftRerollResult.Unavailable,"notification nested reroll rejected at ingress");
            f.H.Selection(f.H.Choices[0]);
            Check(f.D.PendingDrafts.Count==0&&!f.D.TryRebuildPendingViews(out _),"notification selection and Pending mutation blocked");
        };
        f.D.OnDraftChoicesOpened+=o=>{if(o.ChoiceSetRevision>1){setReentries++;reenter();}};
        f.D.OnRerollRequest+=o=>{resultReentries++;reenter();};
        f.Roll();
        Check(setReentries==1&&resultReentries==1&&f.D.IsAwaitingDraft(f.Token),"guard spans both notifications and restores input afterward");
        var evidence=f.Recorder.CreateDraftRuntimeJson();
        Check(evidence.rerollRequestStartCount==1&&evidence.rerollRequests.Count==1,"no recursive observation feedback");
        evidence.rerollRequests.Clear();Check(!RecorderHarness.DraftGenerationTraceIsConsistent(evidence),"missing result detected by independent start");
        f=new Fixture();f.Roll();evidence=f.Recorder.CreateDraftRuntimeJson();evidence.rerollRequests.Add(evidence.rerollRequests[0]);
        Check(!RecorderHarness.DraftGenerationTraceIsConsistent(evidence),"duplicate result rejected");
        f=new Fixture();f.Roll();evidence=f.Recorder.CreateDraftRuntimeJson();evidence.rerollRequests[0].attemptToken="unknown";
        Check(!RecorderHarness.DraftGenerationTraceIsConsistent(evidence),"fake request/set join rejected");
        f=new Fixture();evidence=f.Recorder.CreateDraftRuntimeJson();evidence.attempts[0].choiceSets[0].displayedChoices[0].assetName="NotEligible";
        Check(!RecorderHarness.DraftGenerationTraceIsConsistent(evidence),"outside-pool display rejected");
        f=new Fixture();evidence=f.Recorder.CreateDraftRuntimeJson();evidence.attempts[0].choiceSets[0].displayedChoices[0].multiplicity++;
        Check(!RecorderHarness.DraftGenerationTraceIsConsistent(evidence),"wrong displayed weight rejected");
        f=new Fixture();var outgoingRecorder=f.Recorder;var outgoingToken=f.Token;
        f.H.DuringDraftCommit=()=>
        {
            outgoingRecorder.CaptureTerminal();f.D.StopBattle();
            f.Coordinator=new BattleRuntimeCoordinator{Draft=f.D};f.D.BindCoordinator(f.Coordinator);
            using(CombatDiagnosticScope.Enter(f.Coordinator.DiagnosticIdentity))
            {
                f.Recorder=new RecorderHarness(f.D,3,f.Coordinator.DiagnosticIdentity);
                f.D.BeginBattle();f.D.TryOpenInitialTowerDraft(out f.Token,out _);
            }
        };
        f.D.TryReroll(outgoingToken,1);
        var outgoing=outgoingRecorder.CreateDraftRuntimeJson();var incoming=f.Recorder.CreateDraftRuntimeJson();
        Check(outgoingRecorder.Drained&&outgoing.rerollRequests.Count==1&&outgoing.rerollRequests[0].presentation=="Cancelled"&&RecorderHarness.DraftGenerationTraceIsConsistent(outgoing),"old scope drains postcommit cancellation into old report");
        Check(incoming.rerollRequestStartCount==0&&incoming.rerollRequests.Count==0&&incoming.observedAttemptCount==1,"new Battle cannot receive old request outcome");
        f=new Fixture();f.H.DuringDraftPreparation=()=>{f.Recorder.CaptureTerminal();Check(!f.Recorder.Drained,"terminal waits for in-flight request");f.D.StopBattle();};
        Check(f.Roll()==DraftRerollResult.Cancelled,"precommit terminal cancellation");
        evidence=f.Recorder.CreateDraftRuntimeJson();
        Check(f.Recorder.Drained&&evidence.rerollRequests.Count==1&&!evidence.rerollRequests[0].committed&&RecorderHarness.DraftGenerationTraceIsConsistent(evidence),"cancelled final result drains with no spend");
#endif
        Console.WriteLine("PASS full DraftSystem: "+checks+" assertions");
    }
}
