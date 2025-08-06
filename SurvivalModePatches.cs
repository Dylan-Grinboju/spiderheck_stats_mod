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
        static void Postfix(SurvivalMode __instance, SurvivalConfig survivalConfig, bool __result)
        {
            try
            {
                // Only track successful game starts
                if (__result && DisplayStats.Instance != null)
                {
                    Logger.LogInfo("Survival mode started - timer tracking initiated");
                    DisplayStats.Instance.StartSurvivalTimer();
                    if (EnemiesTracker.Instance != null)
                    {
                        EnemiesTracker.Instance.ResetEnemiesKilled();
                        Logger.LogInfo("Enemies killed count reset");
                    }
                    if (PlayerTracker.Instance != null)
                    {
                        PlayerTracker.Instance.ResetPlayerStats();
                        Logger.LogInfo("Player deaths count reset");
                    }
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
        static void Prefix(SurvivalMode __instance)
        {
            try
            {
                // Use the public GameModeActive() method instead of accessing the private field
                if (DisplayStats.Instance != null && __instance.GameModeActive())
                {
                    DisplayStats.Instance.StopSurvivalTimer();
                    Logger.LogInfo($"Survival mode stopped, timer tracking completed");

                    // Automatically pull up the HUD with enlarged size when game ends
                    DisplayStats.Instance.AutoPullHUD();
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
