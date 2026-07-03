using System.Collections.Generic;

public static class EffectBindingExecutor
{
    public static void ExecuteBindings(
        IReadOnlyList<EffectBinding> effectBindings,
        EffectTriggerContext triggerContext)
    {
        if (effectBindings == null || effectBindings.Count == 0)
        {
            return;
        }

        for (int i = 0; i < effectBindings.Count; i++)
        {
            EffectBinding effectBinding = effectBindings[i];

            if (effectBinding == null || effectBinding.TriggerType != triggerContext.TriggerType)
            {
                continue;
            }

            EffectExecutor.Execute(effectBinding.EffectDefinition, triggerContext);
        }
    }
}
