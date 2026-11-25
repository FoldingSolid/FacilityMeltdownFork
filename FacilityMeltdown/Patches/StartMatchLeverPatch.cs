using FacilityMeltdown.MeltdownSequence.Behaviours;
using HarmonyLib;

namespace FacilityMeltdown.Patches;

[HarmonyPatch(typeof(StartMatchLever))]
internal static class StartMatchLeverPatch
{
    [HarmonyPostfix, HarmonyPatch(nameof(StartMatchLever.EndGame))]
    static void ShortenMeltdownTimer()
    {
        if (MeltdownHandler.Instance != null && MeltdownPlugin.config.ShortenMeltdownTimerOnShipLeave)
        {
            MeltdownHandler.Instance.meltdownTimer = 3f;
        }
    }
}
