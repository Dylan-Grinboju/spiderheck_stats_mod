using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace StatsMod
{
    // SilkMod Attribute with with the format: name, authors, mod version, silk version, and identifier
    [SilkMod("Stats Mod", new string[] { "Dylan" }, "0.1.2", "0.6.1", "Stats_Mod", 1)]
    public class StatsMod : SilkMod
    {
        public static StatsMod Instance { get; private set; }
        public const string ModId = "Stats_Mod";

        /// <summary>
        /// Entry point called by Silk when Unity loads the mod; initializes configuration, optional tracking subsystems, and Harmony patches.
        /// </summary>
        /// <remarks>
        /// This method sets the singleton Instance, ensures the mod configuration exists (via SetupConfiguration), and only proceeds with runtime initialization if ModConfig.TrackingEnabled is true.
        /// When enabled it: ensures PlayerTracker is created, initializes the UI manager, creates a Harmony instance ("com.StatsMod") and applies all patches, then logs each patched method.
        /// If tracking is disabled, the method returns early and skips subsystem initialization and patching.
        /// </remarks>
        public override void Initialize()
        {
            Instance = this;
            Logger.LogInfo("Initializing Stats Mod...");

            // Initialize configuration with default values first
            SetupConfiguration();

            // Check if tracking is enabled before initializing mod components
            if (!ModConfig.TrackingEnabled)
            {
                Logger.LogInfo("Stats Mod tracking is disabled in configuration. Mod components will not be initialized.");
                return;
            }


            var tracker = PlayerTracker.Instance;
            Logger.LogInfo("Player tracker initialized");
            UIManager.Initialize();
            Logger.LogInfo("UI Manager initialized");

            Harmony harmony = new Harmony("com.StatsMod");
            harmony.PatchAll();

            Logger.LogInfo("Applied patches:");
            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched: {method.DeclaringType?.Name}.{method.Name}");
            }

            Logger.LogInfo("Harmony patches applied.");
        }

        /// <summary>
        /// Creates and loads the mod's default configuration values (display and tracking sections), ensuring a config file exists.
        /// </summary>
        /// <remarks>
        /// Constructs a nested default configuration dictionary (display options like window visibility, UI scale and position; and tracking options)
        /// and passes it to <c>Config.LoadModConfig(ModId, defaultConfig)</c>, which will create the YAML config file if absent.
        /// </remarks>
        private void SetupConfiguration()
        {
            // Define default configuration values
            var defaultConfig = new Dictionary<string, object>
            {
                { "display", new Dictionary<string, object>
                    {
                        { "showStatsWindow", true },
                        { "showPlayers", true },
                        // { "showKillCount", true },
                        // { "showDeathCount", true },
                        { "showPlayTime", true },
                        { "showEnemyDeaths", true },
                        { "autoScale", true },
                        { "uiScale", 1.0f },
                        { "position", new Dictionary<string, object>
                            {
                                { "x", 10 },
                                { "y", 10 }
                            }
                        }
                    }
                },
                { "tracking", new Dictionary<string, object>
                    {
                        { "enabled", true },
                        { "saveStatsToFile", true },
                    }
                },
                // { "keybinds", new Dictionary<string, object>
                //     {
                //         { "toggleStats", "F1" },
                //         { "resetStats", "F2" }
                //     }
                // }
            };

            // Load the configuration (this will create the YAML file if it doesn't exist)
            Config.LoadModConfig(ModId, defaultConfig);
            Logger.LogInfo("Configuration loaded");
        }

        /// <summary>
        /// Unloads the mod: removes Harmony patches if tracking was enabled and clears the singleton instance.
        /// </summary>
        /// <remarks>
        /// If ModConfig.TrackingEnabled is true, Harmony.UnpatchID("com.StatsMod") is called to remove applied patches.
        /// The method always resets <c>Instance</c> to <c>null</c>.
        /// </remarks>
        public override void Unload()
        {
            Logger.LogInfo("Unloading Stats Mod...");

            // Only unpatch if tracking was enabled and patches were applied
            if (ModConfig.TrackingEnabled)
            {
                Harmony.UnpatchID("com.StatsMod");
                Logger.LogInfo("Harmony patches removed.");
            }
            else
            {
                Logger.LogInfo("No patches to remove - tracking was disabled.");
            }

            Instance = null;
        }
    }
}