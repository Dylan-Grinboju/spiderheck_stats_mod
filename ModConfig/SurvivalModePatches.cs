using HarmonyLib;
using Logger = Silk.Logger;
using System;
using UnityEngine.SceneManagement;

namespace StatsMod
{

    [HarmonyPatch(typeof(SurvivalMode), "StartGame")]
    public class SurvivalModeStartPatch
    {
        static void Postfix(SurvivalMode __instance, SurvivalConfig survivalConfig, bool __result)
        {
            try
            {
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
        static void Prefix(SurvivalMode __instance)
        {
            try
            {
                if (__instance.GameModeActive())
                {
                    StatsManager.Instance.StopSurvivalSession();
                    Logger.LogInfo("Survival mode stopped via StatsManager");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SurvivalMode.StopGameMode patch: {ex.Message}");
            }
        }
    }

    // Patch to detect when players return to lobby (vs restarting/continuing)
    [HarmonyPatch(typeof(LobbyController), "OnSceneLoaded")]
    public class LobbySceneLoadedPatch
    {
        static void Postfix(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (scene.name.Equals("Lobby"))
                {
                    // Check if we have pending titles from a finished game
                    if (StatsManager.Instance.HasPendingTitles)
                    {
                        Logger.LogInfo("Returned to lobby with pending titles, showing TitlesUI");
                        UIManager.AutoShowTitles();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in LobbyController.OnSceneLoaded patch: {ex.Message}");
            }
        }
    }

}
