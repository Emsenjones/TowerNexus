using System;

[Serializable]
public readonly struct MonsterBuffStateSnapshot
{
    public MonsterBuffStateSnapshot(MonsterBuffInstance buffInstance)
    {
        Definition = buffInstance != null ? buffInstance.Definition : null;
        StackCount = buffInstance != null ? buffInstance.StackCount : 0;
        Phase = buffInstance != null ? buffInstance.Phase : BuffRuntimePhase.Stacking;
        RemainingDuration = buffInstance != null ? buffInstance.RemainingDuration : 0f;
    }

    public BuffDefinition Definition { get; }
    public int StackCount { get; }
    public BuffRuntimePhase Phase { get; }
    public float RemainingDuration { get; }
}
