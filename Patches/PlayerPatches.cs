using System;
using System.Collections.Generic;
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
        
        public static readonly FieldInfo EnemyImmuneTimeField = 
            AccessTools.Field(typeof(EnemyHealthSystem), "_immuneTime");
        
        public static readonly FieldInfo SpiderImmuneTimeField = 
            AccessTools.Field(typeof(SpiderHealthSystem), "_immuneTime");
    }

    [HarmonyPatch(typeof(PlayerInput), "OnEnable")]
    public class PlayerInputEnablePatch
    {
        static void Postfix(PlayerInput __instance)
        {
            try
            {
                PlayerTracker.Instance.RegisterPlayer(__instance);
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
                PlayerTracker.Instance.UnregisterPlayer(__instance);
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
                PlayerTracker.Instance.RecordPlayerDeath(__instance);
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
                PlayerTracker.Instance.UndoPlayerDeath(__instance);
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
                    PlayerTracker.Instance.UpdatePlayerColor(playerInput, primaryColor);
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
                if (GameSessionManager.Instance.IsActive)
                {
                    PlayerTracker.Instance.RecordPlayerRespawn(__instance);
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
                        PlayerTracker.Instance.IncrementShieldsLost(victimPlayerInput);
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

                if (__instance.spiderController != null && GameSessionManager.Instance.IsActive)
                {
                    PlayerInput playerInput = __instance.spiderController.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        PlayerTracker.Instance.IncrementWebSwings(playerInput);
                        PlayerTracker.Instance.StartWebSwingTimer(playerInput);
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
                        PlayerTracker.Instance.StopWebSwingTimer(playerInput);
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
        // Cache PlayerInput references to avoid expensive GetComponentInParent calls every FixedUpdate
        private static readonly Dictionary<Stabilizer, PlayerInput> _playerInputCache = new Dictionary<Stabilizer, PlayerInput>();
        private static readonly object _cacheLock = new object();

        static void Postfix(Stabilizer __instance)
        {
            try
            {
                if (!GameSessionManager.Instance.IsActive)
                    return;

                PlayerInput playerInput = GetCachedPlayerInput(__instance);
                if (playerInput == null) return;

                if (!__instance.grounded)
                {
                    PlayerTracker.Instance.UpdateHighestPoint(playerInput);
                    PlayerTracker.Instance.StartAirborneTimer(playerInput);
                }
                else
                {
                    PlayerTracker.Instance.StopAirborneTimer(playerInput);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking airborne time: {ex.Message}");
            }
        }

        private static PlayerInput GetCachedPlayerInput(Stabilizer stabilizer)
        {
            lock (_cacheLock)
            {
                if (_playerInputCache.TryGetValue(stabilizer, out PlayerInput cached))
                {
                    // Verify the cached reference is still valid
                    if (cached != null) return cached;
                    _playerInputCache.Remove(stabilizer);
                }

                // Cache miss - do the expensive lookup once
                if (stabilizer.TryGetComponent(out SpiderController spider) || 
                    (spider = stabilizer.GetComponentInParent<SpiderController>()) != null)
                {
                    PlayerInput playerInput = spider.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        _playerInputCache[stabilizer] = playerInput;
                        return playerInput;
                    }
                }

                return null;
            }
        }

        // Call this when players are removed to clean up cache
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _playerInputCache.Clear();
            }
        }
    }
}
