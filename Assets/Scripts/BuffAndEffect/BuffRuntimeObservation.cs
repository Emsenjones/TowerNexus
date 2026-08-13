using UnityEngine;

public enum BuffRuntimeObservationType
{
    ApplicationAttempt = 0,
    LifecycleEvent = 1
}

public readonly struct BuffRuntimeObservation
{
    private BuffRuntimeObservation(
        BuffRuntimeObservationType observationType,
        float observationTime,
        MonsterBehaviour ownerMonster,
        BuffDefinition buffDefinition,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        BuffApplyResult applyResult,
        BuffEventType lifecycleEvent,
        BuffRemovalReason removalReason,
        bool hadStateBefore,
        int stackCountBefore,
        BuffRuntimePhase phaseBefore,
        bool hasStateAfter,
        int stackCountAfter,
        BuffRuntimePhase phaseAfter,
        bool reachedMaximumStacks)
    {
        ObservationType = observationType;
        ObservationTime = observationTime;
        OwnerMonster = ownerMonster;
        BuffDefinition = buffDefinition;
        SourceTower = sourceTower;
        SourceUpgrade = sourceUpgrade;
        ApplyResult = applyResult;
        LifecycleEvent = lifecycleEvent;
        RemovalReason = removalReason;
        HadStateBefore = hadStateBefore;
        StackCountBefore = stackCountBefore;
        PhaseBefore = phaseBefore;
        HasStateAfter = hasStateAfter;
        StackCountAfter = stackCountAfter;
        PhaseAfter = phaseAfter;
        ReachedMaximumStacks = reachedMaximumStacks;
    }

    public BuffRuntimeObservationType ObservationType { get; }
    public float ObservationTime { get; }
    public MonsterBehaviour OwnerMonster { get; }
    public BuffDefinition BuffDefinition { get; }
    public TowerInstance SourceTower { get; }
    public TowerUpgradeDefinition SourceUpgrade { get; }
    public BuffApplyResult ApplyResult { get; }
    public BuffEventType LifecycleEvent { get; }
    public BuffRemovalReason RemovalReason { get; }
    public bool HadStateBefore { get; }
    public int StackCountBefore { get; }
    public BuffRuntimePhase PhaseBefore { get; }
    public bool HasStateAfter { get; }
    public int StackCountAfter { get; }
    public BuffRuntimePhase PhaseAfter { get; }
    public bool ReachedMaximumStacks { get; }

    public static BuffRuntimeObservation CreateApplicationAttempt(
        BuffApplyRequest request,
        MonsterBehaviour ownerMonster,
        BuffApplyResult applyResult,
        bool hadStateBefore,
        int stackCountBefore,
        BuffRuntimePhase phaseBefore,
        bool hasStateAfter,
        int stackCountAfter,
        BuffRuntimePhase phaseAfter,
        bool reachedMaximumStacks)
    {
        return new BuffRuntimeObservation(
            BuffRuntimeObservationType.ApplicationAttempt,
            Time.time,
            ownerMonster,
            request.BuffDefinition,
            request.SourceTower,
            request.SourceUpgrade,
            applyResult,
            default,
            BuffRemovalReason.None,
            hadStateBefore,
            stackCountBefore,
            phaseBefore,
            hasStateAfter,
            stackCountAfter,
            phaseAfter,
            reachedMaximumStacks);
    }

    public static BuffRuntimeObservation CreateLifecycleEvent(
        MonsterBuffInstance buffInstance,
        BuffEventType lifecycleEvent,
        BuffRemovalReason removalReason = BuffRemovalReason.None,
        bool hasStateAfter = true)
    {
        int stackCount = buffInstance != null ? buffInstance.StackCount : 0;
        BuffRuntimePhase phase = buffInstance != null
            ? buffInstance.Phase
            : BuffRuntimePhase.Stacking;

        return new BuffRuntimeObservation(
            BuffRuntimeObservationType.LifecycleEvent,
            Time.time,
            buffInstance != null ? buffInstance.Owner : null,
            buffInstance != null ? buffInstance.Definition : null,
            buffInstance != null ? buffInstance.SourceTower : null,
            buffInstance != null ? buffInstance.SourceUpgrade : null,
            default,
            lifecycleEvent,
            removalReason,
            buffInstance != null,
            stackCount,
            phase,
            hasStateAfter && buffInstance != null,
            hasStateAfter ? stackCount : 0,
            phase,
            lifecycleEvent == BuffEventType.Overload);
    }
}
