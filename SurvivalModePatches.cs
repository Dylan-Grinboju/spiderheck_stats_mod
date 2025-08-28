using HarmonyLib;
using Silk;
using Logger = Silk.Logger;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace StatsMod
{

    [HarmonyPatch(typeof(SurvivalMode), "StartGame")]
    public class SurvivalModeStartPatch
    {
        /// <summary>
        /// Harmony postfix for SurvivalMode.StartGame. If the original StartGame call succeeded, begins a survival session via StatsManager.
        /// </summary>
        /// <param name="__instance">The SurvivalMode instance the original method was invoked on.</param>
        /// <param name="survivalConfig">The SurvivalConfig supplied to StartGame.</param>
        /// <param name="__result">True if StartGame completed successfully; the survival session is started only when this is true.</param>
        static void Postfix(SurvivalMode __instance, SurvivalConfig survivalConfig, bool __result)
        {
            try
            {
                // Only track successful game starts
                if (__result)
                {
                    StatsManager.Instance.StartSurvivalSession();
                    Logger.LogInfo("Survival mode started via StatsManager");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SurvivalMode.StartGame patch: {ex.Message}");
            }
        }
    }

    // Patch SurvivalMode.StopGameMode to track when survival sessions end
    [HarmonyPatch(typeof(SurvivalMode), "StopGameMode")]
    public class SurvivalModeStopPatch
    {
        /// <summary>
        /// Prefix patch for SurvivalMode.StopGameMode that stops a tracked survival session and opens the HUD when a game is active.
        /// </summary>
        /// <param name="__instance">The SurvivalMode instance being stopped; if its game is active this will stop the StatsManager survival session and trigger the HUD.</param>
        static void Prefix(SurvivalMode __instance)
        {
            try
            {
                if (__instance.GameModeActive())
                {
                    StatsManager.Instance.StopSurvivalSession();
                    Logger.LogInfo("Survival mode stopped via StatsManager");

                    // Automatically pull up the HUD when game ends
                    UIManager.AutoPullHUD();
                    Logger.LogInfo("Stats HUD auto-pulled after survival mode ended");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SurvivalMode.StopGameMode patch: {ex.Message}");
            }
        }
    }

}
