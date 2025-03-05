using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace StatsMod
{
    // SilkMod Attribute with with the format: name, authors, mod version, silk version, and identifier
    [SilkMod("Stats Mod", new[] { "thmrgnd" }, "1.0.0", "0.4.0", "Stats_Mod")]
    public class StatsMod : SilkMod
    {
        // Static instance for easy access - make this public static to ensure accessibility
        public static StatsMod Instance { get; private set; }

        // Stats tracking system
        public StatsTracker Stats { get; private set; }

        // Static utility methods for safe stats access
        public static void SafeIncrementEnemiesKilled()
        {
            StatsTracker.Instance.IncrementEnemiesKilled();
        }

        public static string SafeGetStatsReport()
        {
            return StatsTracker.Instance.GetStatsReport();
        }

        // Called by Silk when Unity loads this mod
        public override void Initialize()
        {
            // Set static instance FIRST before doing anything else
            Instance = this;
            Logger.LogInfo("Stats Mod instance set");

            // Initialize stats tracker
            Stats = new StatsTracker();

            // Log mod started
            Logger.LogInfo("Initializing Stats Mod...");

            // Ensure PlayerTracker is initialized before UI
            var tracker = PlayerTracker.Instance;
            Logger.LogInfo("Player tracker initialized");

            // Initialize the player stats display
            DisplayPlayerStats.Initialize();
            Logger.LogInfo("Player stats display initialized");

            // Create and apply Harmony patches
            Harmony harmony = new Harmony("com.thmrgnd.StatsMod");
            harmony.PatchAll();

            // Log all successfully applied patches for debugging
            Logger.LogInfo("Applied patches:");
            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched: {method.DeclaringType?.Name}.{method.Name}");
            }

            Logger.LogInfo("Harmony patches applied.");
        }

        public override void Unload()
        {
            Logger.LogInfo("Unloading Stats Mod...");
            Harmony.UnpatchID("com.thmrgnd.StatsMod");
            // Set instance to null LAST
            Instance = null;
        }
    }

    [HarmonyPatch(typeof(EnemyHealthSystem), "Explode")]
    class EnemyDeathCountPatch
    {
        static void Postfix(EnemyHealthSystem __instance)
        {
            try
            {
                StatsMod.SafeIncrementEnemiesKilled();
                Logger.LogInfo("Enemy killed via Explode method.");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording enemy kill: {ex.Message}");
            }
        }
    }
}