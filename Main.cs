using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public static void SafeIncrementDeathCount()
        {
            StatsTracker.Instance.IncrementDeathCount();
        }

        public static void SafeDecreaseDeathCount()
        {
            StatsTracker.Instance.DecreaseDeathCount();
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
    class ExplodeEnemyPatch
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

    [HarmonyPatch(typeof(SpiderHealthSystem), "ExplodeInDirection")]
    class PlayerDeathCountPatch
    {
        static void Postfix(SpiderHealthSystem __instance)
        {
            try
            {
                // We'll use Postfix instead of Prefix and assume this is a death
                // since we can't directly check if it's shielded
                StatsMod.SafeIncrementDeathCount();
                Logger.LogInfo("Player death recorded via SpiderHealthSystem.Disintegrate method");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording player death: {ex.Message}");
            }
        }
    }

}