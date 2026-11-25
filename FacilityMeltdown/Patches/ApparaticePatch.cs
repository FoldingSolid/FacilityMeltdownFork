using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FacilityMeltdown.API;
using FacilityMeltdown.Behaviours;
using FacilityMeltdown.Integrations;
using FacilityMeltdown.MeltdownSequence.Behaviours;
using FacilityMeltdown.Util;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FacilityMeltdown.Patches
{
    [HarmonyPatch(typeof(LungProp))]
    static class ApparaticePatch
    {
        [HarmonyPrefix, HarmonyPatch(nameof(LungProp.EquipItem)), HarmonyWrapSafe]
        internal static void BeginMeltdownSequence(LungProp __instance, ref bool ___isLungDocked)
        {
            if (!__instance.IsHost) return;
            if (!___isLungDocked) return;
            if (MeltdownAPI.MeltdownStarted) return;

            GameObject meltdown = GameObject.Instantiate(MeltdownPlugin.assets.meltdownHandlerPrefab);
            meltdown.GetComponent<MeltdownHandler>().causingLungProp = __instance;
            meltdown.GetComponent<NetworkObject>().Spawn(false);
        }

        [HarmonyFinalizer, HarmonyPatch(nameof(LungProp.EquipItem)), HarmonyWrapSafe]
        static void RecoverDockedApparatus(LungProp __instance)
        {
            if (__instance == null || !__instance.IsHost || !__instance.isLungDocked)
                return;

            MeltdownPlugin.logger.LogError("Another mod prevented the apparatus from being removed. Clearing its docked state to prevent the round from becoming stuck.");
            __instance.isLungDocked = false;
        }



        [HarmonyPrefix, HarmonyPatch(nameof(LungProp.Start)), HarmonyWrapSafe]
        internal static void AddRadiationSource(LungProp __instance)
        {
            if (!StartOfRound.Instance.inShipPhase)
            {
                MeltdownMoonMapper.EnsureMeltdownMoonMapper();
                MeltdownInteriorMapper.EnsureMeltdownInteriorMapper();
            }

            RadiationSource source = __instance.gameObject.AddComponent<RadiationSource>();
            source.radiationAmount = 80;
            source.radiationDistance = 60;

            if (MeltdownPlugin.config.OverrideApparatusValue)
            {
                int minimumValue = Mathf.Min(MeltdownPlugin.config.ApparatusValueMin, MeltdownPlugin.config.ApparatusValueMax);
                int maximumValue = Mathf.Max(MeltdownPlugin.config.ApparatusValueMin, MeltdownPlugin.config.ApparatusValueMax);
                int seed = unchecked(StartOfRound.Instance.randomMapSeed * 397 ^ (int)__instance.NetworkObjectId);
                int apparatusValue = new System.Random(seed).Next(minimumValue, maximumValue + 1);

                if (WeatherRegistryIntegration.Enabled)
                {
                    try
                    {
                        apparatusValue = Mathf.RoundToInt(apparatusValue * WeatherRegistryIntegration.GetWeatherMultiplier());
                    }
                    catch (Exception ex)
                    {
                        MeltdownPlugin.logger.LogWarning(ex.ToString());
                    }
                }

                __instance.SetScrapValue(apparatusValue);
            }

            MeltdownPlugin.logger.LogDebug(__instance.scrapValue);
        }
    }
}
