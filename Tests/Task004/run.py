#!/usr/bin/env python3
import tempfile
from pathlib import Path
from build import build,root
def method(source,name):
 import re
 match=re.search(r'^    (?:private|public|internal) [^\n]*\b'+name+r'\(',source,re.M)
 begin=source.index('{',match.end());i=begin+1;depth=1
 while depth:
  depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[match.start():i]
def block(source,start):
 a=source.index(start);begin=source.index('{',a);i=begin+1;depth=1
 while depth:
  depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[a:i]
with tempfile.TemporaryDirectory(prefix='towernexus-task004-') as folder:
 source=(root/'Assets/Scripts/TowerDeployment/TowerPlacementController.cs').read_text()
 a=source.index('    public bool CanStartDraftInteraction');predicate=source[a:source.index(';',a)+1]
 generated=Path(folder)/'InteractionHarness.cs'
 generated.write_text('using System; using UnityEngine;\npartial class InteractionHarness {\n'+predicate+'\n'+
     method(source,'CompletePlacement')+'\n'+method(source,'CompletePlacementCore')+'\n}')
 recorder=(root/'Assets/Scripts/Diagnostics/CombatBalance/CombatRecordingSession.cs').read_text()
 bindings=Path(folder)/'RecorderBindings.cs'
 bindings.write_text('partial class RecorderBindingHarness { void Subscribe() { '+
     block(recorder,'        if (subscribedSubmission == null && battleRuntimeCoordinator != null)')+
     '} void Unsubscribe() { '+block(recorder,'        if (subscribedSubmission != null)')+'} }')
 runtime=(root/'Assets/Scripts/TowerDeployment/BattleRuntimeCoordinator.cs').read_text()
 generated.write_text(generated.read_text()+'\npartial class ReleaseRoutingHarness {\n'+method(runtime,'ReleasePreparedBattleRuntime')+'\n}')
 build(folder,[root/'Tests/Task004/SubmissionTests.cs' ,root/'Tests/Task004/InteractionTests.cs',generated,bindings],'SubmissionTests')
