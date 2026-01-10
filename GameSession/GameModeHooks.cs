using HarmonyLib;
using Logger = Silk.Logger;
using System;
using UnityEngine.SceneManagement;

namespace StatsMod
{
    // Consolidated Harmony patches for game mode lifecycle events.
    // Handles survival and versus mode start/stop, lobby transitions.
    
    #region Survival Mode Hooks

    [HarmonyPatch(typeof(SurvivalMode), "StartGame")]
    public class SurvivalModeStartPatch
    {
        static void Postfix(SurvivalMode __instance, SurvivalConfig survivalConfig, bool __result)
        {
            try
            {
                if (__result)
                {
                    GameSessionManager.Instance.StartSurvivalSession();
                    Logger.LogInfo("Survival mode started");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SurvivalMode.StartGame patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SurvivalMode), "StopGameMode")]
    public class SurvivalModeStopPatch
    {
        static void Prefix(SurvivalMode __instance)
        {
            try
            {
                if (__instance.GameModeActive())
                {
                    GameSessionManager.Instance.StopSurvivalSession();
                    Logger.LogInfo("Survival mode stopped");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SurvivalMode.StopGameMode patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SurvivalMode), "CompleteWave")]
    public class SurvivalModeCompleteWavePatch
    {
        static void Prefix(SurvivalMode __instance)
        {
            try
            {
                GameSessionManager.Instance.CheckWaveClutch();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SurvivalMode.CompleteWave patch: {ex.Message}");
            }
        }
    }

    #endregion

    #region Versus Mode Hooks

    [HarmonyPatch(typeof(VersusMode), "StartMatch")]
    public class VersusModeStartPatch
    {
        static void Postfix(VersusMode __instance)
        {
            try
            {
                GameSessionManager.Instance.StartVersusSession();
                Logger.LogInfo("Versus mode started");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in VersusMode.StartMatch patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(VersusMode), "EndMatch")]
    public class VersusModeEndPatch
    {
        static void Prefix(VersusMode __instance)
        {
            try
            {
                GameSessionManager.Instance.StopVersusSession();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in VersusMode.EndMatch patch: {ex.Message}");
            }
        }
    }

    #endregion

    #region Lobby Transition Hooks

    [HarmonyPatch(typeof(LobbyController), "OnSceneLoaded")]
    public class LobbySceneLoadedPatch
    {
        static void Postfix(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (scene.name.Equals("Lobby"))
                {
                    bool isVersus = GameSessionManager.Instance.LastGameMode == GameMode.Versus;
                    bool isSurvivalWithNoTitles = GameSessionManager.Instance.LastGameMode == GameMode.Survival && TitlesManager.Instance.TitleCount == 0;

                    if (isVersus || isSurvivalWithNoTitles)
                    {
                        Logger.LogInfo($"Returned to lobby from {GameSessionManager.Instance.LastGameMode}, showing BigUI");
                        UIManager.AutoPullHUD();
                    }
                    else
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

    #endregion
}
