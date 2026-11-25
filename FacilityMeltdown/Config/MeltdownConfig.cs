using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using GameNetcodeStuff;
using HarmonyLib;
using System.Runtime.Serialization;
using FacilityMeltdown.Lang;
using TMPro;
using FacilityMeltdown.Util.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;

namespace FacilityMeltdown.Config
{
    
    
    internal class MeltdownConfig : LoafSyncedConfig<MeltdownConfig>
    {

        internal MeltdownConfig(ConfigFile file, bool migrateLegacyApparatusValue) : base(file)
        {
            BindProperty<string>(
                typeof(MeltdownConfig).GetProperty(nameof(Language)),
                "Language",
                new ConfigDescription(
                    "What language should FacilityMeltdown use? NOTE: This only affects Facility Meltdown and will not change the rest of the game's language.\n" +
                    "Some languages may also need FontPatcher (https://thunderstore.io/c/lethal-company/p/LeKAKiD/FontPatcher/).\n" +
                    "Languages available: " + string.Join(", ", LangParser.languages.Keys)
                )
            );

            if (migrateLegacyApparatusValue)
                CopySetting(file, file, "GameBalance", "ApparatusValue", nameof(ApparatusValueMin));

            if (migrateLegacyApparatusValue)
                CopySetting(file, file, "GameBalance", "ApparatusValue", nameof(ApparatusValueMax));
        }

        [ConfigGroup("GameBalance")]
        [ConfigDesc("Override the apparatus scrap value. Disable only when another mod must control that value.")]
        public bool OverrideApparatusValue { get; private set; } = true;

        [ConfigDesc("Lowest possible apparatus scrap value when the override is enabled, before any optional WeatherRegistry multiplier. Set this equal to ApparatusValueMax for a fixed value.")]
        [ConfigRange(0, 1000)]
        public int ApparatusValueMin { get; private set; } = 240;

        [ConfigDesc("Highest possible apparatus scrap value when the override is enabled, before any optional WeatherRegistry multiplier. The selected value is inclusive and is deterministic for the current moon.")]
        [ConfigRange(0, 1000)]
        public int ApparatusValueMax { get; private set; } = 240;

        [ConfigDesc("Number of eligible enemies to spawn when a meltdown begins. Set to 0 to disable bonus spawns.")]
        [ConfigRange(0, 10)]
        public int MonsterSpawnAmount { get; private set; } = 5;

        [ConfigDesc("Flash emergency lights during the meltdown. Disabled leaves facility lights in their normal powered-off state.")]
        public bool EmergencyLights { get; private set; } = true;

        [ConfigDesc("Players required within 15 metres to remove the apparatus. The requirement is capped at living-player count and is ignored after 9 PM.")]
        [ConfigRange(1, 10)]
        public int MinPeopleToPullApparatus { get; private set; } = 2;

        [ConfigDesc("Make juvenile Maneaters cry and flee from the apparatus when the meltdown starts.")]
        public bool ScareBabyManeaters { get; private set; } = true;

        [ConfigDesc("Set the remaining countdown to 3 seconds when the ship leaves so the explosion resolves promptly.")]
        public bool ShortenMeltdownTimerOnShipLeave { get; private set; } = true;


        [ConfigDesc("Comma-separated enemy names excluded from meltdown spawns. Whitespace is ignored; modded enemies are supported.")]
        public string DisallowedEnemies { get; private set; } = "Centipede,Hoarding bug";

        [ConfigDesc("Cooldown in seconds for the reactor-health terminal command. Does not affect the vanilla scan command.")]
        [ConfigRange(0, 20)]
        public float ShipScanCooldown { get; private set; } = 15f;

        [ConfigDesc("Reactor-health reading rounding interval in percentage points. Higher values are less precise; does not affect the vanilla scan command.")]
        [ConfigRange(1, 100)]
        public float ShipScanAccuracy { get; private set; } = 15f;

        [ConfigGroup("Integrations")]
        [ConfigDesc("Apply WeatherRegistry's current scrap-value multiplier to the overridden apparatus value. Has no effect unless WeatherRegistry is installed.")]
        public bool WeatherRegistryIntegration { get; private set; } = true;
        
        [ConfigGroup("GameBalance")]
        [ConfigDesc("Length of the meltdown sequence in seconds. Changes apply to the next meltdown; an active countdown keeps its original duration.")]
        [ConfigRange(60, 5 * 60)]
        [RequiresRestart(false)]
        public int MeltdownTime { get; private set; } = 120;

        [ConfigGroup("Audio")]
        [ConfigDesc("Volume percentage for the meltdown music and warning announcements.")]
        [ConfigRange(0, 100)]
        [RequiresRestart(false)]
        public int MusicVolume { get; private set; } = 100;

        [ConfigDesc("Allow meltdown music to play while the local player is outside the facility.")]
        [RequiresRestart(false)]
        public bool MusicPlaysOutside { get; private set; } = true;

        [ConfigGroup("Visuals")]
        [ConfigDesc("Enable camera shake caused by the meltdown. Does not affect other game camera effects.")]
        [RequiresRestart(false)]
        public bool ScreenShake { get; private set; } = true;

        [ConfigDesc("Enable interior particle effects during the meltdown. Does not affect the final fireball.")]
        [RequiresRestart(false)]
        public bool ParticleEffects { get; private set; } = true;

        [ConfigDesc("Show a local HUD countdown while a meltdown is active. The countdown uses the host's configured meltdown duration.")]
        [RequiresRestart(false)]
        public bool ShowCountdownTimer { get; private set; } = true;

        [ConfigIgnore]
        [RequiresRestart]
        public string Language { get; private set; } = "en";

        internal void ImportLegacyClientSettings(string legacyConfigPath)
        {
            if (!File.Exists(legacyConfigPath))
                return;

            ConfigFile legacyConfig = new ConfigFile(legacyConfigPath, saveOnInit: false);
            CopySetting(legacyConfig, configFile, "Audio", nameof(MusicVolume), nameof(MusicVolume));
            CopySetting(legacyConfig, configFile, "Audio", nameof(MusicPlaysOutside), nameof(MusicPlaysOutside));
            CopySetting(legacyConfig, configFile, "Visuals", nameof(ScreenShake), nameof(ScreenShake));
            CopySetting(legacyConfig, configFile, "Visuals", nameof(ParticleEffects), nameof(ParticleEffects));
            CopySetting(legacyConfig, configFile, "Visuals", nameof(ShowCountdownTimer), nameof(ShowCountdownTimer));
            CopySetting(legacyConfig, configFile, "Language", nameof(Language), nameof(Language));
            configFile.Save();

            MeltdownPlugin.logger.LogInfo("Imported local settings from the legacy Facility Meltdown client config into the main config.");
        }

        void CopySetting(ConfigFile source, ConfigFile destination, string sourceSection, string sourceKey, string destinationPropertyName)
        {
            ConfigDefinition sourceDefinition = new ConfigDefinition(sourceSection, sourceKey);
            if (!source.ContainsKey(sourceDefinition))
                return;

            (PropertyInfo property, object entry) = configEntries.FirstOrDefault(configEntry => configEntry.Item1.Name == destinationPropertyName);
            if (property == null)
                return;

            if (property.PropertyType == typeof(int))
            {
                ConfigEntry<int> sourceEntry = source.Bind(sourceDefinition, 0);
                ConfigEntry<int> destinationEntry = (ConfigEntry<int>)entry;
                destinationEntry.Value = sourceEntry.Value;
                property.SetValue(this, destinationEntry.Value);
            }
            else if (property.PropertyType == typeof(bool))
            {
                ConfigEntry<bool> sourceEntry = source.Bind(sourceDefinition, false);
                ConfigEntry<bool> destinationEntry = (ConfigEntry<bool>)entry;
                destinationEntry.Value = sourceEntry.Value;
                property.SetValue(this, destinationEntry.Value);
            }
            else if (property.PropertyType == typeof(string))
            {
                ConfigEntry<string> sourceEntry = source.Bind(sourceDefinition, string.Empty);
                ConfigEntry<string> destinationEntry = (ConfigEntry<string>)entry;
                destinationEntry.Value = sourceEntry.Value;
                property.SetValue(this, destinationEntry.Value);
            }
        }
        
        public List<string> GetDisallowedEnemies()
        {
            return DisallowedEnemies.Split(',')
                .Select(enemy => enemy.Trim())
                .Where(enemy => !string.IsNullOrEmpty(enemy))
                .ToList();
        }
    }
}
