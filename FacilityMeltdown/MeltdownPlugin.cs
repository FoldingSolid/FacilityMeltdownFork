using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FacilityMeltdown.Behaviours;
using FacilityMeltdown.Config;
using FacilityMeltdown.Equipment;
using FacilityMeltdown.Integrations;
using FacilityMeltdown.Integrations.SoundAPI;
using FacilityMeltdown.Lang;
using FacilityMeltdown.MeltdownSequence.Behaviours;
using FacilityMeltdown.Util.Attributes;
using HarmonyLib;
using LobbyCompatibility.Attributes;
using LobbyCompatibility.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LobbyCompatibility;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.SceneManagement;
using Dawn;

namespace FacilityMeltdown;

[BepInPlugin(modGUID, modName, modVersion)]
[CompatibleDependency("ainavt.lc.lethalconfig", typeof(LethalConfigIntergration))]
[CompatibleDependency("BMX.LobbyCompatibility", typeof(LobbyCompatibilityIntegration))]
[CompatibleDependency("mrov.WeatherRegistry", typeof(WeatherRegistryIntegration))]
[CompatibleDependency("me.loaforc.soundapi", typeof(SoundAPIIntegration))]
[CompatibleDependency("me.loaforc.diomedes", typeof(DiomedesIntergration))]
[BepInDependency("com.github.teamxiaolan.dawnlib")]
public class MeltdownPlugin : BaseUnityPlugin
{
    internal const string modGUID = "me.loaforc.facilitymeltdown";
    internal const string modName = "FacilityMeltdown";
    internal const string modVersion = "2.7.5";

    internal static MeltdownPlugin instance;
    internal static ManualLogSource logger;
    internal static MeltdownAssets assets { get; private set; }
    public static bool loadedFully { get; private set; } = false;

    internal static MeltdownConfig config { get; set; }
    internal static Harmony harmony = new Harmony(modGUID);

    void Awake()
    {
        if (instance == null) instance = this; // Signleton
        else return; // Make sure nothing else gets loaded.
        logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

        if (!RunLoadStep("Assets.Init", "Getting assets", () =>
        {
            assets = new MeltdownAssets();
        })) return;
        if (!RunLoadStep("LangParser.Init", "Getting possible languages", LangParser.Init)) return;
        if (!RunLoadStep("new MeltdownConfig()", "Setting up config", () => {
            bool migrateLegacyApparatusValue = !Config.ContainsKey(new ConfigDefinition("GameBalance", "ApparatusValueMin"))
                && !Config.ContainsKey(new ConfigDefinition("GameBalance", "ApparatusValueMax"));
            bool migrateLegacyClientSettings = !Config.ContainsKey(new ConfigDefinition("Audio", "MusicVolume"));

            config = new MeltdownConfig(Config, migrateLegacyApparatusValue);
            if (migrateLegacyClientSettings)
            {
                config.ImportLegacyClientSettings(Utility.CombinePaths(Paths.ConfigPath, "me.loaforc.facilitymeltdown.Client.cfg"));
            }

            logger.LogDebug($"Apparatus value range: {config.ApparatusValueMin}-{config.ApparatusValueMax}");
            Config.ClearOrphans();
        })) return;
        if (!RunLoadStep("LangParser.SetLanguage", "Setting language", () => { LangParser.SetLanguage(config.Language); })) return;
        if (!RunLoadStep("RegisterPatches", "Integrating into LethalCompany", RegisterPatches)) return;
        if (!RunLoadStep("RegisterItems", "Adding the Geiger Counter", RegisterItems))
        {
            logger.LogWarning("Failed to register the geiger counter.");
        }
        if (!RunLoadStep("CompatibleDependency.Init", "Checking for any soft dependencies", () => { CompatibleDependencyAttribute.Init(this); }))
        {
            logger.LogWarning("Doing something with soft dependencies broke, meltdown itself should be fine.");
        }

        loadedFully = true;
        logger.LogInfo(modName + ":" + modVersion + " by loaforc has loaded! have fun :3");
        logger.LogInfo(@"         .-_; ;_-.          ");
        logger.LogInfo(@"        / /     \ \        ");
        logger.LogInfo(@"       | |       | |      ");
        logger.LogInfo(@"        \ \.---./ /       ");
        logger.LogInfo(@"  .-\""""~   .---.   ~""""-.");
        logger.LogInfo(@" ,`.-~// .'`---`'. \\~-.`,");
        logger.LogInfo(@" ' `   | | \(_)/ | |   `' ");
        logger.LogInfo(@"  ,    \  \ | | /  /    , ");
        logger.LogInfo(@"  ;`'.,_\  `-'-'  /_,.'`; ");
        logger.LogInfo(@"   '-._  _.-'^'-._  _.-'    ");
    }



    bool RunLoadStep(string stepName, string descrption, Action callback)
    {
        try
        {
            callback();
        }
        catch (Exception e)
        {
            logger.LogError($"`{stepName}` caused an exception to be thrown, meltdown may or may not work, look for more errors.");
            logger.LogError(e);
            return false;
        }
        logger.LogInfo(stepName.PadRight(25) + " == " + descrption);
        return true;
    }

    void RegisterItems()
    {
        logger.LogInfo("Registering Items");
        DawnLib.DefineItem(MeltdownKeys.GeigerCounter, assets.geigerCounterItemDef, item =>
        {
            item.DefineShop(shop => shop.OverrideCost(90).OverrideInfoNode(assets.geigerCounterNode));
        });

        LethalContent.Items.OnFreeze += () =>
        {
            if (!config.OverrideApparatusValue)
                return;

            LethalContent.Items[ItemKeys.Apparatus].Item.minValue = Mathf.Min(config.ApparatusValueMin, config.ApparatusValueMax);
            LethalContent.Items[ItemKeys.Apparatus].Item.maxValue = Mathf.Max(config.ApparatusValueMin, config.ApparatusValueMax);
        };
    }

    
    void OnDisable()
    {
        if (!loadedFully)
        {
            Harmony.UnpatchID(modGUID);
            logger.LogInfo("Unpatching as something failed while loading.");
            return;
        }

        if (
            BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("den.meltdownchance") ||
            BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("PizzaProbability")
        )
        {
            logger.LogInfo("You are using a mod that makes meltdown have a random chance of occuring.");
            logger.LogInfo("Keep in mind this goes against meltdown's core design, but your choice.");
            logger.LogWarning("However; BY DESIGN these mods need to mess with the internals of meltdown");
            logger.LogWarning("so I personally will be offering low to no support with these mods installed.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void RegisterPatches() {
        foreach (Type harmonyPatchHandler in Assembly.GetExecutingAssembly().GetLoadableTypes().Where(t => t.IsClass && t.Namespace == "FacilityMeltdown.Patches")) {
            logger.LogDebug("Registering Patch handler: " + harmonyPatchHandler.Name);
            harmony.PatchAll(harmonyPatchHandler);
        }
    }
}
