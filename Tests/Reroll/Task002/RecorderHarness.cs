using System;
using System.Collections.Generic;
using UnityEngine;
partial class RecorderHarness
{
    internal int rerollRequestStartCount;
    internal readonly List<long> startedRerollRequestIds = new List<long>();
    internal readonly CombatDraftAccumulator draftAccumulator = new CombatDraftAccumulator();
    internal readonly List<CombatBalanceRerollRequestJson> rerollRequests = new List<CombatBalanceRerollRequestJson>();
    internal List<CombatBalanceDraftItemJson> towerDraftPoolSnapshot = new List<CombatBalanceDraftItemJson>();
    internal List<CombatBalanceDraftItemJson> towerUpgradeDraftPoolSnapshot = new List<CombatBalanceDraftItemJson>();
    internal bool accepting=true;
    internal bool AcceptEvent => accepting && ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity ?? identity);
    internal bool isTrackingRun=true,terminalCaptured,configuredFixedDraftSeedEnabled;
    internal object identity;
    internal CombatDiagnosticScope.Lease lease;
    internal bool Drained;
    internal int configuredDraftChoiceCount,configuredDraftSeed,configuredFixedDraftStepCount;
    internal int terminalLevel,terminalProgress,terminalRequired,initialFreeRerolls,terminalFreeRerolls;
    internal float configuredTowerDraftSlotProbability;
    internal string configuredGenerationContractVersion,configuredDraftRandomAlgorithmVersion,configuredDraftGenerationMode="Natural";
    internal PlayerSystem playerSystem=new PlayerSystem();
    internal DraftSystem draftSystem;
    internal void CaptureDraftFixture(){}
    internal void CaptureTerminal()
    {
        terminalFreeRerolls=draftSystem.FreeRerollsRemaining;terminalCaptured=true;
        lease.Closing=true;
        if(lease.Depth==0)lease.Drained();
    }
    internal int ResolveDraftResolutionNode(DraftChoicesOpenedObservation observation)=>0;
    internal float GetRunActiveTimeSeconds()=>0;
    internal RecorderHarness(DraftSystem d,int budget,object observationIdentity)
    {
        draftSystem=d;initialFreeRerolls=budget;identity=observationIdentity;
        lease=CombatDiagnosticScope.Acquire(identity);
        lease.Drained=()=>{Drained=true;accepting=false;};
        d.OnDraftChoicesOpened+=HandleDraftChoicesOpened;
        d.OnDraftChoiceCommitted+=HandleDraftChoiceCommitted;
        d.OnRerollRequestStarted+=HandleRerollRequestStarted;
        d.OnRerollRequest+=HandleRerollRequest;
    }
}
