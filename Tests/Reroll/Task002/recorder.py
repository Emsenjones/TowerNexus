"""Extract unchanged production recorder methods and DTOs, double only surrounding combat runtime."""
import re

def block(source, pattern):
    match=re.search(pattern,source,re.M)
    if not match: raise ValueError(pattern)
    begin=source.index('{',match.end());depth=1;end=begin+1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[match.start():end]

def generate(root,folder):
    source=(root/'Assets/Scripts/Diagnostics/CombatBalance/CombatRecordingSession.cs').read_text()
    methods=['HandleDraftChoicesOpened','HandleDraftChoiceCommitted','HandleRerollRequest','HandleRerollRequestStarted',
             'ResolveNaturalMultiplicity','ResolveDisplayedMultiplicity','CreateDraftItemJson',
             'CreateDraftRuntimeJson','DraftGenerationTraceIsConsistent','RerollHistoryIsConsistent']
    harness=(root/'Tests/Reroll/Task002/RecorderHarness.cs').read_text()
    code=harness+'\npartial class RecorderHarness {\n'+'\n'.join(block(source,r'^    internal (?:static )?[^\n]*\b'+name+r'\(') for name in methods)+'\n}'
    dto=(root/'Assets/Scripts/Diagnostics/CombatBalance/CombatBalanceRunJsonReport.cs').read_text()
    for name in ['CombatBalanceDraftRuntimeJson','CombatBalanceDraftChoiceSetJson','CombatBalanceDraftAttemptJson',
                 'CombatBalanceRerollRequestJson','CombatBalanceDraftItemJson','CombatBalancePendingDraftJson',
                 'CombatBalanceInvestmentCommitJson','CombatBalanceInvestmentRuntimeJson']:
        code+='\n'+block(dto,r'^internal (?:sealed )?class '+name+r'\b[^\n]*')
    code+='\n'+(root/'Assets/Scripts/Diagnostics/CombatBalance/CombatDraftAccumulator.cs').read_text().replace('using System;','').replace('using System.Collections.Generic;','').replace('using System.Globalization;','').replace('using UnityEngine;','')
    out=folder/'Recorder.cs';out.write_text('#if UNITY_EDITOR\n'+code+'\n#endif')
    return out
