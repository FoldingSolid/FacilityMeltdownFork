using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dawn;
using Diomedes.Client;
using Diomedes.Client.API;
using Diomedes.Client.GameMode.Bingo;
using FacilityMeltdown.API;
using UnityEngine;

namespace FacilityMeltdown.Integrations;

internal static class DiomedesIntergration
{
    sealed class TriggerMeltdownChallenge : BingoChallenge
    {
        public NamespacedKey<DawnMoonInfo> MoonKey;
        readonly string moonName;

        public TriggerMeltdownChallenge(string moonName)
        {
            this.moonName = moonName;
            MeltdownAPI.OnMeltdownStart += OnMeltdownStart;
        }

        void OnMeltdownStart()
        {
            DawnMoonInfo moonInfo = SelectableLevelExtensions.GetDawnInfo(StartOfRound.Instance.currentLevel);
            if (MoonKey == null || (moonInfo != null && moonInfo.Key == MoonKey))
            {
                Complete();
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            MeltdownAPI.OnMeltdownStart -= OnMeltdownStart;
        }

        public override BingoUIState CreateUI() => new BingoUIState { Name = $"Trigger Meltdown on {moonName}" };
    }

    public static readonly NamespacedKey<ChallengeInfo> TriggerMeltdown = NamespacedKey<ChallengeInfo>.From("facility_meltdown", "trigger_meltdown");
    public static readonly NamespacedKey<ChallengeGroup> MeltdownGroup = NamespacedKey<ChallengeGroup>.From("facility_meltdown", "meltdown_bingo");
    public static bool Enabled { get; private set; }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void Initialize()
    {
        Enabled = true;
        DiomedesClientAPI.DefineBingoChallengeGroup(MeltdownGroup, builder => builder.WithMaxAmount(1));

        List<(NamespacedKey<DawnMoonInfo> moonKey, string moonName)> challengeMoons = new()
        {
            (null, "any moon"),
            (MoonKeys.Embrion, "Embrion"),
            (MoonKeys.Artifice, "Artifice"),
            (MoonKeys.Titan, "Titan")
        };

        foreach ((NamespacedKey<DawnMoonInfo> moonKey, string moonName) in challengeMoons)
        {
            DiomedesClientAPI.DefineBingoChallenge(
                TriggerMeltdown,
                () => new TriggerMeltdownChallenge(moonName) { MoonKey = moonKey },
                builder => builder.AddToGroup(MeltdownGroup).AddToGroup(DiomedesKeys.ApparatusGroup));
        }
    }
}
