using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace StatsMod
{
    // SilkMod Attribute with with the format: name, authors, mod version, silk version, and identifier
    [SilkMod("Stats Mod", new[] { "Dylan" }, "0.1.0", "0.4.0", "Stats_Mod")]
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


    // [HarmonyPatch(typeof(ModifierManager), "GetModLevel")]
    // public class GetEveryMod
    // {
    //     [HarmonyPostfix]
    //     public static void Postfix(ref int __result)
    //     {
    //         // Override the return value to always be 1
    //         __result = 1;
    //     }
    // }

    // [HarmonyPatch(typeof(PlayerController), "Update")]
    // public class F2KeyPressHandler
    // {
    //     private static bool wasF2Pressed = false;

    //     [HarmonyPostfix]
    //     public static void Postfix()
    //     {
    //         // Check if F2 key was just pressed this frame
    //         bool isF2Pressed = Keyboard.current != null && Keyboard.current.f2Key.isPressed;

    //         if (isF2Pressed && !wasF2Pressed)
    //         {
    //             Logger.LogInfo("F2 key pressed - Disabling death effect");

    //             // Find all SpiderHealthSystem instances and call DisableDeathEffect
    //             SpiderHealthSystem[] healthSystems = GameObject.FindObjectsOfType<SpiderHealthSystem>();

    //             foreach (var healthSystem in healthSystems)
    //             {
    //                 if (healthSystem != null)
    //                 {
    //                     healthSystem.DisableDeathEffect();
    //                     Logger.LogInfo("Death effect disabled for a spider");
    //                 }
    //             }
    //         }

    //         wasF2Pressed = isF2Pressed;
    //     }
    // }
}