using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

namespace StatsMod
{
    // SilkMod Attribute with the format: name, authors, mod version, silk version, and identifier
    [SilkMod("Stats Mod", new string[] { "Dylan" }, "2.1.0", "0.7.0", "Stats_Mod", 1)]
    public class StatsMod : SilkMod
    {
        public static StatsMod Instance { get; private set; }
        public const string ModId = "Stats_Mod";

        // Get version from assembly at runtime
        private static string _version;
        public static string Version
        {
            get
            {
                if (_version == null)
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    _version = $"{version.Major}.{version.Minor}.{version.Build}";
                }
                return _version;
            }
        }

        // Called by Silk when Unity loads this mod
        public override void Initialize()
        {
            Instance = this;
            Logger.LogInfo("Initializing Stats Mod...");

            // Initialize configuration with default values first
            SetupConfiguration();

            if (ModConfig.CheckForUpdates)
            {
                // Check for updates asynchronously
                try
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(15000);
                        await ModUpdater.CheckForUpdatesAsync();
                    });
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Update check failed: {ex.Message}");
                }
            }
            else
            {
                Logger.LogInfo("Update checking is disabled in configuration.");
            }

            if (!ModConfig.TrackingEnabled)
            {
                Logger.LogInfo("Stats Mod tracking is disabled in configuration. Mod components will not be initialized.");
                return;
            }


            UIManager.Initialize();
            Logger.LogInfo("UI Manager initialized");

            GameObject pauseHandlerObj = new GameObject("PauseHandler");
            pauseHandlerObj.AddComponent<PauseHandler>();
            DontDestroyOnLoad(pauseHandlerObj);
            Logger.LogInfo("Pause handler initialized");

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
                        { "showPlayTime", true },
                        { "showEnemyDeaths", true },
                        { "autoScale", true },
                        { "uiScale", 1.0f },
                        { "bigUIOpacity", 100f },
                        { "smallUIShowBackground", false },
                        { "position", new Dictionary<string, object>
                            {
                                { "x", 10 },
                                { "y", 10 }
                            }
                        },
                        { "bigUI", new Dictionary<string, object>
                            {
                                { "columns", new Dictionary<string, object>
                                    {
                                        // Computed stats
                                        { "totalOffence", true },
                                        { "totalFriendlyHits", true },
                                        { "totalHitsTaken", true },
                                        // Core stats
                                        { "kills", false },
                                        { "deaths", false },
                                        { "maxKillStreak", true },
                                        { "currentKillStreak", false },
                                        { "maxSoloKillStreak", false },
                                        { "currentSoloKillStreak", false },
                                        { "friendlyKills", false },
                                        { "enemyShields", false },
                                        { "shieldsLost", false },
                                        { "friendlyShields", false },
                                        { "aliveTime", true },
                                        { "waveClutches", true },
                                        { "webSwings", false },
                                        { "webSwingTime", false },
                                        { "airborneTime", false },
                                        { "lavaDeaths", false },
                                        // Enemy kills
                                        { "enemyKills", new Dictionary<string, object>
                                            {
                                                { "wasp", false },
                                                { "powerWasp", false },
                                                { "roller", false },
                                                { "whisp", false },
                                                { "powerWhisp", false },
                                                { "meleeWhisp", false },
                                                { "powerMeleeWhisp", false },
                                                { "khepri", false },
                                                { "powerKhepri", false },
                                                { "hornetShaman", false },
                                                { "hornet", false },
                                            }
                                        },
                                        // Weapon kills
                                        { "weaponKills", new Dictionary<string, object>
                                            {
                                                { "shotgun", false },
                                                { "railShot", false },
                                                { "deathCube", false },
                                                { "deathRay", false },
                                                { "energyBall", false },
                                                { "particleBlade", false },
                                                { "khepriStaff", false },
                                                { "laserCannon", false },
                                                { "laserCube", false },
                                                { "sawDisc", false },
                                                { "explosions", false }
                                            }
                                        }
                                    }
                                }
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
                { "updater", new Dictionary<string, object>
                    {
                        { "checkForUpdates", true }
                    }
                },
                { "titles", new Dictionary<string, object>
                    {
                        { "enabled", true },
                        { "revealDelaySeconds", 2.0f }
                    }
                },
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