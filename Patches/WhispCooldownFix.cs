using HarmonyLib;
using Logger = Silk.Logger;
using System;
using UnityEngine;

namespace WhispCooldownFix
{
    [HarmonyPatch(typeof(WhispBrain), "Start")]
    public class WhispCooldownStartPatch
    {
        static void Postfix(WhispBrain __instance)
        {
            try
            {
                var shotCooldownTillField = AccessTools.Field(typeof(WhispBrain), "_shotCooldownTill");
                var shotCooldownField = AccessTools.Field(typeof(WhispBrain), "shotCooldown");

                if (shotCooldownTillField != null && shotCooldownField != null)
                {
                    float shotCooldown = (float)shotCooldownField.GetValue(__instance);
                    shotCooldownTillField.SetValue(__instance, Time.time + shotCooldown);
                }
                else
                {
                    Logger.LogWarning("WhispBrain patch: Could not access required fields for cooldown initialization");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in WhispBrain.Start patch: {ex.Message}");
            }
        }
    }
}
