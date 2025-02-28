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

    // Add patch for the correct Explode method
    [HarmonyPatch(typeof(EnemyHealthSystem), "Explode")]
    class ExplodeEnemyPatch
    {
        static void Postfix(EnemyHealthSystem __instance)
        {
            // First check if instance exists before doing anything
            if (StatsMod.Instance == null)
            {
                Logger.LogWarning("StatsMod instance is null - enemy kill not recorded. This might happen during game initialization or shutdown.");
                return; // Exit early if instance is null
            }

            if (StatsMod.Instance.Stats == null)
            {
                Logger.LogWarning("StatsMod.Stats instance is null - enemy kill not recorded");
                return; // Exit early if stats is null
            }

            Logger.LogInfo("Enemy killed via Explode method.");
            StatsMod.Instance.Stats.IncrementEnemiesKilled();
        }
    }
}