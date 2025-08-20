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

        // Called by Silk when Unity loads this mod
        public override void Initialize()
        {
            Instance = this;
            Logger.LogInfo("Stats Mod instance set");
            Logger.LogInfo("Initializing Stats Mod...");
            var tracker = PlayerTracker.Instance;
            Logger.LogInfo("Player tracker initialized");
            DisplayStats.Initialize();
            Logger.LogInfo("Player stats display initialized");

            Harmony harmony = new Harmony("com.StatsMod");
            harmony.PatchAll();

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
            Harmony.UnpatchID("com.StatsMod");
            Instance = null;
        }
    }
}