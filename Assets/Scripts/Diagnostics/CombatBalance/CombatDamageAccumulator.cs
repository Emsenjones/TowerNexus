#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
internal sealed class CombatDamageAccumulator
{
    internal readonly Dictionary<string, TowerScaledDamageAggregate>
        towerScaledDamageBySignature =
            new Dictionary<string, TowerScaledDamageAggregate>();
    internal readonly Dictionary<string, TowerScaledRejectionAggregate>
        towerScaledRejectionBySignature =
            new Dictionary<string, TowerScaledRejectionAggregate>();
    internal readonly Dictionary<string, FixedBuffDamageAggregate>
        fixedBuffDamageBySignature =
            new Dictionary<string, FixedBuffDamageAggregate>();
    internal sealed class TowerScaledDamageAggregate
    {
        public TowerScaledDamageAggregate(TowerOwnedDamageResolution resolution)
        {
            Resolution = resolution;
        }

        public TowerOwnedDamageResolution Resolution { get; }
        public int ResolutionCount { get; set; }
        public int SuccessfulApplicationCount { get; set; }
        public int AppliedDamageTotal { get; set; }
    }
    internal sealed class TowerScaledRejectionAggregate
    {
        public TowerScaledRejectionAggregate(
            TowerOwnedDamageResolutionObservation observation)
        {
            Observation = observation;
        }

        public TowerOwnedDamageResolutionObservation Observation { get; }
        public int RejectionCount { get; set; }
    }
    internal sealed class FixedBuffDamageAggregate
    {
        public FixedBuffDamageAggregate(FixedBuffDamageObservation observation)
        {
            EffectName = GetAssetName(observation.EffectDefinition);
            ActionOrdinal = observation.ActionOrdinal;
            FixedDamage = observation.FixedDamage;
        }

        public string EffectName { get; }
        public int ActionOrdinal { get; }
        public int FixedDamage { get; }
        public int ResolvedTargetCount { get; set; }
        public int ResolutionCount { get; set; }
        public int SuccessfulApplicationCount { get; set; }
        public int AppliedDamageTotal { get; set; }
        public HashSet<int> SourceTowerInstanceIds { get; } =
            new HashSet<int>();
    }
    internal CombatBalanceDamageDiagnosticsJson CreateDamageDiagnosticsJson()
    {
        CombatBalanceDamageDiagnosticsJson diagnostics =
            new CombatBalanceDamageDiagnosticsJson();
        List<string> towerScaledKeys =
            new List<string>(towerScaledDamageBySignature.Keys);
        towerScaledKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < towerScaledKeys.Count; i++)
        {
            TowerScaledDamageAggregate aggregate =
                towerScaledDamageBySignature[towerScaledKeys[i]];
            TowerOwnedDamageResolution resolution = aggregate.Resolution;
            TowerDamageSourceIdentity sourceIdentity =
                resolution.DamageSourceIdentity;
            float relativeRoundingError = resolution.RawProduct > 0f
                ? Mathf.Abs(resolution.FinalDamage - resolution.RawProduct) /
                  resolution.RawProduct
                : 0f;

            diagnostics.towerScaledSignatures.Add(
                new CombatBalanceTowerScaledDamageJson
                {
                    towerInstanceId = resolution.DiagnosticSource.Id,
                    towerDisplayName = resolution.DiagnosticSource.DisplayName,
                    towerFamily = resolution.TowerFamily.ToString(),
                    level = resolution.Level,
                    levelBasicDamage = resolution.LevelBasicDamage,
                    rawDamageBonus = resolution.RawDamageBonus,
                    resolvedBasicDamage = resolution.ResolvedBasicDamage,
                    damageSourceType = sourceIdentity.SourceType.ToString(),
                    effectDefinitionName =
                        resolution.DiagnosticSource.EffectName,
                    actionOrdinal = sourceIdentity.ActionOrdinal,
                    damageScale = resolution.DamageScale,
                    rawProduct = resolution.RawProduct,
                    finalDamage = resolution.FinalDamage,
                    relativeRoundingError = relativeRoundingError,
                    resolutionCount = aggregate.ResolutionCount,
                    successfulApplicationCount =
                        aggregate.SuccessfulApplicationCount,
                    appliedDamageTotal = aggregate.AppliedDamageTotal
                });
            diagnostics.towerScaledResolutionCount +=
                aggregate.ResolutionCount;
            diagnostics.towerScaledSuccessfulApplicationCount +=
                aggregate.SuccessfulApplicationCount;
            diagnostics.towerScaledAppliedDamageTotal +=
                aggregate.AppliedDamageTotal;
            diagnostics.maximumTowerScaledRelativeRoundingError =
                Mathf.Max(
                    diagnostics.maximumTowerScaledRelativeRoundingError,
                    relativeRoundingError);
        }

        List<string> rejectionKeys =
            new List<string>(towerScaledRejectionBySignature.Keys);
        rejectionKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < rejectionKeys.Count; i++)
        {
            TowerScaledRejectionAggregate aggregate =
                towerScaledRejectionBySignature[rejectionKeys[i]];
            TowerOwnedDamageResolutionObservation observation =
                aggregate.Observation;
            TowerDamageSourceIdentity sourceIdentity =
                observation.DamageSourceIdentity;

            diagnostics.towerScaledRejections.Add(
                new CombatBalanceTowerScaledRejectionJson
                {
                    sourceTowerInstanceId = observation.DiagnosticSource.Id,
                    sourceTowerName = observation.DiagnosticSource.Name,
                    damageSourceType = sourceIdentity.SourceType.ToString(),
                    effectDefinitionName =
                        observation.DiagnosticSource.EffectName,
                    actionOrdinal = sourceIdentity.ActionOrdinal,
                    damageScale = observation.DamageScale,
                    failureReason = observation.FailureReason.ToString(),
                    rejectionCount = aggregate.RejectionCount
                });
            diagnostics.towerScaledRejectedCount +=
                aggregate.RejectionCount;
        }

        List<string> fixedBuffKeys =
            new List<string>(fixedBuffDamageBySignature.Keys);
        fixedBuffKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < fixedBuffKeys.Count; i++)
        {
            FixedBuffDamageAggregate aggregate =
                fixedBuffDamageBySignature[fixedBuffKeys[i]];

            diagnostics.fixedBuffSignatures.Add(
                new CombatBalanceFixedBuffDamageJson
                {
                    effectDefinitionName =
                        aggregate.EffectName,
                    actionOrdinal = aggregate.ActionOrdinal,
                    fixedDamage = aggregate.FixedDamage,
                    sourceTowerInstanceCount =
                        aggregate.SourceTowerInstanceIds.Count,
                    resolvedTargetCount = aggregate.ResolvedTargetCount,
                    resolutionCount = aggregate.ResolutionCount,
                    successfulApplicationCount =
                        aggregate.SuccessfulApplicationCount,
                    appliedDamageTotal = aggregate.AppliedDamageTotal
                });
            diagnostics.fixedBuffResolutionCount +=
                aggregate.ResolutionCount;
            diagnostics.fixedBuffSuccessfulApplicationCount +=
                aggregate.SuccessfulApplicationCount;
            diagnostics.fixedBuffAppliedDamageTotal +=
                aggregate.AppliedDamageTotal;
        }

        return diagnostics;
    }
    internal static string CreateTowerScaledDamageSignature(
        TowerOwnedDamageResolution resolution)
    {
        TowerDamageSourceIdentity sourceIdentity =
            resolution.DamageSourceIdentity;
        return string.Join(
            "|",
            resolution.DiagnosticSource.Id.ToString(CultureInfo.InvariantCulture),
            resolution.TowerFamily.ToString(),
            resolution.Level.ToString(CultureInfo.InvariantCulture),
            resolution.LevelBasicDamage.ToString(CultureInfo.InvariantCulture),
            resolution.RawDamageBonus.ToString("R", CultureInfo.InvariantCulture),
            resolution.ResolvedBasicDamage.ToString("R", CultureInfo.InvariantCulture),
            sourceIdentity.SourceType.ToString(),
            GetEffectIdentity(sourceIdentity.EffectDefinition),
            sourceIdentity.ActionOrdinal.ToString(CultureInfo.InvariantCulture),
            resolution.DamageScale.ToString("R", CultureInfo.InvariantCulture),
            resolution.RawProduct.ToString("R", CultureInfo.InvariantCulture),
            resolution.FinalDamage.ToString(CultureInfo.InvariantCulture));
    }
    internal static string CreateTowerScaledRejectionSignature(
        TowerOwnedDamageResolutionObservation observation)
    {
        TowerDamageSourceIdentity sourceIdentity =
            observation.DamageSourceIdentity;
        return string.Join(
            "|",
            observation.DiagnosticSource.Id.ToString(CultureInfo.InvariantCulture),
            sourceIdentity.SourceType.ToString(),
            GetEffectIdentity(sourceIdentity.EffectDefinition),
            sourceIdentity.ActionOrdinal.ToString(CultureInfo.InvariantCulture),
            observation.DamageScale.ToString("R", CultureInfo.InvariantCulture),
            observation.FailureReason.ToString());
    }
    internal void HandleTowerOwnedDamageResolutionObserved(
        TowerOwnedDamageResolutionObservation observation)
    {

        if (observation.IsResolved)
        {
            TowerOwnedDamageResolution resolution = observation.Resolution;
            string signature = CreateTowerScaledDamageSignature(resolution);

            if (!towerScaledDamageBySignature.TryGetValue(
                    signature,
                    out TowerScaledDamageAggregate aggregate))
            {
                aggregate = new TowerScaledDamageAggregate(resolution);
                towerScaledDamageBySignature.Add(signature, aggregate);
            }

            aggregate.ResolutionCount++;
            return;
        }

        string rejectionSignature =
            CreateTowerScaledRejectionSignature(observation);

        if (!towerScaledRejectionBySignature.TryGetValue(
                rejectionSignature,
                out TowerScaledRejectionAggregate rejectionAggregate))
        {
            rejectionAggregate =
                new TowerScaledRejectionAggregate(observation);
            towerScaledRejectionBySignature.Add(
                rejectionSignature,
                rejectionAggregate);
        }

        rejectionAggregate.RejectionCount++;
    }
    internal void HandleTowerOwnedDamageApplicationObserved(
        TowerOwnedDamageApplicationObservation observation)
    {
        if (observation.SuccessfulApplicationCount <= 0)
        {
            return;
        }

        TowerOwnedDamageResolution resolution = observation.Resolution;
        string signature = CreateTowerScaledDamageSignature(resolution);

        if (!towerScaledDamageBySignature.TryGetValue(
                signature,
                out TowerScaledDamageAggregate aggregate))
        {
            aggregate = new TowerScaledDamageAggregate(resolution);
            towerScaledDamageBySignature.Add(signature, aggregate);
        }

        aggregate.SuccessfulApplicationCount +=
            observation.SuccessfulApplicationCount;
        aggregate.AppliedDamageTotal +=
            resolution.FinalDamage * observation.SuccessfulApplicationCount;
    }
    internal void HandleFixedBuffDamageObserved(
        FixedBuffDamageObservation observation)
    {

        string signature = CreateFixedBuffDamageSignature(observation);

        if (!fixedBuffDamageBySignature.TryGetValue(
                signature,
                out FixedBuffDamageAggregate aggregate))
        {
            aggregate = new FixedBuffDamageAggregate(observation);
            fixedBuffDamageBySignature.Add(signature, aggregate);
        }

        aggregate.ResolutionCount++;
        aggregate.ResolvedTargetCount += observation.ResolvedTargetCount;
        aggregate.SuccessfulApplicationCount +=
            observation.SuccessfulApplicationCount;
        aggregate.AppliedDamageTotal +=
            observation.FixedDamage * observation.SuccessfulApplicationCount;

        if (CombatDiagnosticScope.Source(observation.SourceTower).Id != 0)
        {
            aggregate.SourceTowerInstanceIds.Add(
                CombatDiagnosticScope.Source(observation.SourceTower).Id);
        }
    }
    private static string GetAssetName(UnityEngine.Object asset)
    {
        return asset != null ? asset.name : string.Empty;
    }
    private static string GetDisplayName(string displayName, string fallback)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? fallback
            : displayName.Trim();
    }
    private static string GetEffectIdentity(EffectDefinition effectDefinition)
    {
        return effectDefinition != null
            ? effectDefinition.GetInstanceID().ToString(
                CultureInfo.InvariantCulture)
            : "0";
    }
    private static string CreateFixedBuffDamageSignature(
        FixedBuffDamageObservation observation)
    {
        return CreateFixedBuffDamageSignature(
            observation.EffectDefinition,
            observation.ActionOrdinal,
            observation.FixedDamage);
    }

    private static string CreateFixedBuffDamageSignature(
        EffectDefinition effectDefinition,
        int actionOrdinal,
        int fixedDamage)
    {
        return string.Join(
            "|",
            GetEffectIdentity(effectDefinition),
            actionOrdinal.ToString(CultureInfo.InvariantCulture),
            fixedDamage.ToString(CultureInfo.InvariantCulture));
    }

}
#endif
