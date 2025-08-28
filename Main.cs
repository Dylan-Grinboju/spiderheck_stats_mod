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

        // Called by Silk when Unity loads this mod
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

        private void SetupConfiguration()
        {
            // Define default configuration values
            var defaultConfig = new Dictionary<string, object>
            {
                { "display", new Dictionary<string, object>
                    {
                        { "showStatsWindow", true },
                        { "showPlayers", true },
                        { "showKillCount", true },
                        { "showDeathCount", true },
                        { "showPlayTime", true },
                        { "showEnemyDeaths", true },
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