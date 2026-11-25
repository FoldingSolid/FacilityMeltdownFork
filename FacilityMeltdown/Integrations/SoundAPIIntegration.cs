using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using FacilityMeltdown.API;
using FacilityMeltdown.MeltdownSequence.Behaviours;
using loaforcsSoundAPI;
using loaforcsSoundAPI.Core.Data;
using loaforcsSoundAPI.SoundPacks.Data.Conditions;
using UnityEngine;

namespace FacilityMeltdown.Integrations.SoundAPI;

internal static class SoundAPIIntegration
{
    public static bool Enabled { get; private set; }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void Initialize()
    {
        Enabled = true;
        try
        {
            RegisterConditions();
        }
        catch (Exception)
        {
            MeltdownPlugin.logger.LogWarning("Failed to register SoundAPI conditions; SoundAPI v2 may not be installed.");
        }
    }

    // Keep SoundAPI types out of the normal plugin load path: this integration is optional.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void RegisterConditions() => loaforcsSoundAPI.SoundAPI.RegisterAll(Assembly.GetExecutingAssembly());
}

[SoundAPICondition("FacilityMeltdown:in_meltdown", false, null)]
public class InMeltdownCondition : Condition
{
    public bool? Value { get; private set; }

    public override bool Evaluate(IContext context) => MeltdownAPI.MeltdownStarted == Value.GetValueOrDefault(true);
}

[SoundAPICondition("FacilityMeltdown:time_left", false, null)]
public class MeltdownTimeLeftCondition : Condition
{
    public string Value { get; private set; }

    public override bool Evaluate(IContext context)
    {
        return MeltdownHandler.Instance != null && EvaluateRangeOperator(MeltdownHandler.Instance.TimeLeftUntilMeltdown, Value);
    }

    public override List<IValidatable.ValidationResult> Validate()
    {
        return ValidateRangeOperator(Value, out IValidatable.ValidationResult error)
            ? new List<IValidatable.ValidationResult>()
            : new List<IValidatable.ValidationResult> { error };
    }
}
