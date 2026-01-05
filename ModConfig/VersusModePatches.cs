using HarmonyLib;
using Silk;
using Logger = Silk.Logger;
using System;

namespace StatsMod
{

    [HarmonyPatch(typeof(VersusMode), "StartMatch")]
    public class VersusModeStartPatch
    {
        static void Postfix(VersusMode __instance)
        {
            try
            {
                StatsManager.Instance.StartVersusSession();
                Logger.LogInfo("Versus mode started via StatsManager");
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
                StatsManager.Instance.StopVersusSession();
                Logger.LogInfo("Versus mode stopped via StatsManager");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in VersusMode.EndMatch patch: {ex.Message}");
            }
        }
    }

}
