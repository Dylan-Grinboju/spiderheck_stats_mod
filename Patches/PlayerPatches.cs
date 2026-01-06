using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;

namespace StatsMod
{
    // Cache reflection field info for performance
    internal static class ReflectionCache
    {
        public static readonly FieldInfo PrimaryColorField = 
            typeof(SpiderCustomizer).GetField("_primaryColor", BindingFlags.NonPublic | BindingFlags.Instance);
        
        public static readonly FieldInfo SecondaryColorField = 
            typeof(SpiderCustomizer).GetField("_secondaryColor", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [HarmonyPatch(typeof(PlayerInput), "OnEnable")]
    public class PlayerInputEnablePatch
    {
        static void Postfix(PlayerInput __instance)
        {
            try
            {
                StatsManager.Instance.RegisterPlayer(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in PlayerInput.OnEnable patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerInput), "OnDisable")]
    public class PlayerInputDisablePatch
    {
        static void Prefix(PlayerInput __instance)
        {
            try
            {
                StatsManager.Instance.UnregisterPlayer(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in PlayerInput.OnDisable patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpiderHealthSystem), "DisintegrateLegsAndDestroy")]
    public class SpiderHealthSystemDisintegrateLegsAndDestroyPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                StatsManager.Instance.RecordPlayerDeath(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SpiderHealthSystem.DisintegrateLegsAndDestroy patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpiderHealthSystem), "DisableDeathEffect")]
    public class SpiderHealthSystemDisableDeathEffect
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                StatsManager.Instance.UndoPlayerDeath(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SpiderHealthSystem.DisableDeathEffect patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpiderCustomizer), "SetSpiderColor")]
    public class SpiderCustomizerSetSpiderColorPatch
    {
        static void Postfix(SpiderCustomizer __instance)
        {
            try
            {
                PlayerInput playerInput = __instance.GetComponentInParent<PlayerInput>();
                if (playerInput != null && ReflectionCache.PrimaryColorField != null)
                {
                    Color primaryColor = (Color)ReflectionCache.PrimaryColorField.GetValue(__instance);
                    StatsManager.Instance.UpdatePlayerColor(playerInput, primaryColor);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SpiderCustomizer.SetSpiderColor patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerController), "SpawnCharacter", new Type[] { typeof(Vector3), typeof(Quaternion) })]
    public class PlayerControllerSpawnCharacterPatch
    {
        static void Postfix(PlayerController __instance)
        {
            try
            {
                if (StatsManager.Instance.IsActive)
                {
                    StatsManager.Instance.RecordPlayerRespawn(__instance);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in PlayerController.SpawnCharacter patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpiderHealthSystem), "BreakShield")]
    class PlayerShieldBreakPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                if (__instance.rootObject != null)
                {
                    PlayerInput victimPlayerInput = __instance.rootObject.GetComponentInParent<PlayerInput>();
                    if (victimPlayerInput != null)
                    {
                        StatsManager.Instance.IncrementShieldsLost(victimPlayerInput);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking player shield loss: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(WebMaker), "ShootWeb")]
    public class WebMakerShootWebPatch
    {
        static void Postfix(WebMaker __instance, GameObject ___target)
        {
            try
            {
                if (___target == null)
                {
                    return;
                }

                if (__instance.spiderController != null && StatsManager.Instance.IsActive)
                {
                    PlayerInput playerInput = __instance.spiderController.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        StatsManager.Instance.IncrementWebSwings(playerInput);
                        StatsManager.Instance.StartWebSwingTimer(playerInput);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking web swing: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(WebMaker), "DisconnectWeb")]
    public class WebMakerDisconnectWebPatch
    {
        static void Prefix(WebMaker __instance)
        {
            try
            {
                if (__instance.spiderController != null)
                {
                    PlayerInput playerInput = __instance.spiderController.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        StatsManager.Instance.StopWebSwingTimer(playerInput);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping web swing timer: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Stabilizer), "FixedUpdate")]
    public class StabilizerFixedUpdatePatch
    {
        static void Postfix(Stabilizer __instance)
        {
            try
            {
                if (!StatsManager.Instance.IsActive)
                    return;

                SpiderController spider = __instance.GetComponentInParent<SpiderController>();
                if (spider == null) return;

                PlayerInput playerInput = spider.GetComponentInParent<PlayerInput>();
                if (playerInput == null) return;

                StatsManager.Instance.UpdateHighestPoint(playerInput);

                if (!__instance.grounded)
                {
                    StatsManager.Instance.StartAirborneTimer(playerInput);
                }
                else
                {
                    StatsManager.Instance.StopAirborneTimer(playerInput);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking airborne time: {ex.Message}");
            }
        }
    }
}
