using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;

namespace StatsMod;

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
                // Guard against race condition: if the player left before OnEnable completed
                // Happens when playing with controllers and in the pause menu of a match you use mouse/keyboard
            if (__instance is null || !__instance.enabled || !__instance.gameObject.activeInHierarchy)
            {
                Logger.LogDebug($"Skipping registration for inactive/destroyed player: {__instance}");
                return;
            }

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

[HarmonyPatch(typeof(SpiderHealthSystem), "Disintegrate")]
public class SpiderHealthSystemDisintegratePatch
{
    static void Prefix(SpiderHealthSystem __instance)
    {
        try
        {
            if (__instance is null || __instance.rootObject is null)
                return;

            if (ReflectionCache.SpiderImmuneTimeField is not null)
            {
                float immuneTill = (float)ReflectionCache.SpiderImmuneTimeField.GetValue(__instance);
                if (Time.time < immuneTill)
                    return;
            }

            if (__instance.HasShield())
                return;

            PlayerInput playerInput = __instance.rootObject.GetComponentInParent<PlayerInput>();
            if (playerInput is not null)
            {
                if (__instance.astralDead)
                    return;
                PlayerTracker.Instance.IncrementLavaDeaths(playerInput);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in SpiderHealthSystem.Disintegrate patch: {ex.Message}");
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
            if (playerInput is not null && ReflectionCache.PrimaryColorField is not null)
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
            if (__instance.rootObject is not null)
            {
                PlayerInput victimPlayerInput = __instance.rootObject.GetComponentInParent<PlayerInput>();
                if (victimPlayerInput is not null)
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
            if (___target is null)
            {
                return;
            }

            if (__instance.spiderController is not null && GameSessionManager.Instance.IsActive)
            {
                PlayerInput playerInput = __instance.spiderController.GetComponentInParent<PlayerInput>();
                if (playerInput is not null)
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
            if (__instance.spiderController is not null)
            {
                PlayerInput playerInput = __instance.spiderController.GetComponentInParent<PlayerInput>();
                if (playerInput is not null)
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
    private static readonly Dictionary<Stabilizer, (PlayerInput, SpiderController)> _componentCache = new();
    private static readonly object _cacheLock = new();

    static void Postfix(Stabilizer __instance)
    {
        try
        {
            if (!GameSessionManager.Instance.IsActive)
                return;

            var (playerInput, spider) = GetCachedComponents(__instance);
            if (playerInput is null || spider is null) return;

            PlayerTracker.Instance.UpdateHighestPoint(playerInput, spider);

            if (!__instance.grounded)
            {
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

    private static (PlayerInput, SpiderController) GetCachedComponents(Stabilizer stabilizer)
    {
        lock (_cacheLock)
        {
            if (_componentCache.TryGetValue(stabilizer, out var cached))
            {
                if (cached.Item1 is not null && cached.Item2 is not null) return cached;
                _componentCache.Remove(stabilizer);
            }

            if (stabilizer.TryGetComponent(out SpiderController spider) ||
                (spider = stabilizer.GetComponentInParent<SpiderController>()) is not null)
            {
                PlayerInput playerInput = spider.GetComponentInParent<PlayerInput>();
                if (playerInput is not null && spider is not null)
                {
                    var components = (playerInput, spider);
                    _componentCache[stabilizer] = components;
                    return components;
                }
            }

            return (null, null);
        }
    }

    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _componentCache.Clear();
        }
    }
}
