using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;
using HarmonyLib;
using Unity.Netcode;
using System.Reflection;


namespace StatsMod
{
    // All AccessTools calls are done once at static initialization to avoid per-call overhead.
    internal static class EnemyReflectionCache
    {
        // NetworkBehaviour
        public static readonly PropertyInfo IsHostProperty =
            AccessTools.Property(typeof(NetworkBehaviour), "IsHost");

        // EnemyHealthSystem fields
        public static readonly AccessTools.FieldRef<EnemyHealthSystem, bool> EnemyExploding =
            AccessTools.FieldRefAccess<EnemyHealthSystem, bool>("_exploding");

        // DiscLauncher methods
        public static readonly MethodInfo DiscLauncherLaunchDiscClientRpc =
            AccessTools.Method(typeof(DiscLauncher), "LaunchDiscClientRpc");
        public static readonly MethodInfo DiscLauncherImpact =
            AccessTools.Method(typeof(DiscLauncher), "Impact");

        // ProjectileLauncher fields and methods
        public static readonly MethodInfo ProjectileLauncherShotClientRpc =
            AccessTools.Method(typeof(ProjectileLauncher), "ShotClientRpc");
        public static readonly MethodInfo ProjectileLauncherImpact =
            AccessTools.Method(typeof(ProjectileLauncher), "Impact");
        public static readonly FieldInfo ProjectileLauncherCollider =
            AccessTools.Field(typeof(ProjectileLauncher), "_launcherCollider");
        public static readonly FieldInfo ProjectileLauncherReloadOffset =
            AccessTools.Field(typeof(ProjectileLauncher), "reloadOffset");
    }

    // Every enemy death ends here: direct damage, lava (Disintegrate), roller struts, stuck in walls.
    // Explode can be called more than once, the same checks as the original keep only the call that kills.
    [HarmonyPatch(typeof(EnemyHealthSystem), "Explode")]
    class EnemyDeathCountPatch
    {
        static void Prefix(EnemyHealthSystem __instance)
        {
            try
            {
                if (!(bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance) ||
                    __instance.ignoreIndirectDamage ||
                    EnemyReflectionCache.EnemyExploding(__instance))
                {
                    return;
                }

                HitLogic.OnEnemyDeath(__instance);
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording enemy kill: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(EnemyHealthSystem), "DestroyShield")]
    class EnemyShieldDestroyedPatch
    {
        static void Prefix(EnemyHealthSystem __instance)
        {
            try
            {
                HitLogic.OnEnemyShieldDestroyed(__instance);
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording enemy shield break: {ex.Message}");
            }
        }
    }

    // DiscLauncher - Track disc projectiles and their owners
    [HarmonyPatch(typeof(DiscLauncher), "LaunchDisc")]
    class DiscLauncherLaunchDiscPatch
    {
        static bool Prefix(DiscLauncher __instance)
        {
            try
            {
                // Replicate the entire LaunchDisc function with our addition
                if (__instance.ammo <= 0f)
                {
                    return false; // Skip original
                }

                float ammo = __instance.ammo;
                __instance.ammo = ammo - 1f;

                // Create the disc projectile
                GameObject gameObject = Object.Instantiate(__instance.discProjectile, __instance.mountedDisc.transform.position, __instance.point.rotation);
                gameObject.GetComponent<NetworkObject>().Spawn(true);
                gameObject.GetComponent<Rigidbody2D>().AddForce(__instance.transform.up * __instance.shotForce, ForceMode2D.Impulse);

                // **THIS IS OUR ADDITION** - Track the disc owner
                PlayerInput ownerPlayer = __instance.owner?.healthSystem?.GetComponentInParent<PlayerInput>();
                HitLogic.RegisterDiscOwner(gameObject, ownerPlayer);

                // Reset phase effect and visual elements
                __instance.discPhaseEffect.ResetEffect();
                __instance.mountedDisc.SetActive(false);
                __instance.targetingLasers.SetActive(false);

                if ((bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance))
                {
                    // Call LaunchDiscClientRpc using cached reflection
                    EnemyReflectionCache.DiscLauncherLaunchDiscClientRpc?.Invoke(__instance, null);
                }

                // Call Impact method using cached reflection
                EnemyReflectionCache.DiscLauncherImpact?.Invoke(__instance, new object[] { __instance.transform.up * -__instance.recoil, __instance.point.position, false, true });

                return false; // Skip the original method
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error in DiscLauncher LaunchDisc patch: {ex.Message}");
                return true; // Execute original method on error
            }
        }
    }

    // SawDisc - Clean up disc owner tracking when disc is destroyed
    [HarmonyPatch(typeof(SawDisc), "OnDesintegrateClientRpc")]
    class SawDiscCleanupPatch
    {
        static void Prefix(SawDisc __instance)
        {
            try
            {
                // Clean up the disc owner tracking when the disc is about to be destroyed
                HitLogic.CleanupDiscOwner(__instance.gameObject);
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error cleaning up SawDisc owner tracking: {ex.Message}");
            }
        }
    }

    // ParticleBladeLauncher - Track particle blade projectiles and their owners
    [HarmonyPatch(typeof(ProjectileLauncher), "Shoot")]
    class ProjectileLauncherShootPatch
    {
        static bool Prefix(ProjectileLauncher __instance)
        {
            try
            {
                // Check if this launcher fires ParticleBlade projectiles
                ParticleBlade particleBladeComponent = __instance.projectile.GetComponent<ParticleBlade>();
                if (particleBladeComponent == null)
                {
                    return true; // Not a ParticleBlade launcher, let original method run
                }

                // Replicate the entire Shoot method with our owner tracking addition
                if (Physics2D.Raycast(__instance.point.transform.position, __instance.transform.up, 0.01f, GameController.instance.worldLayers).collider)
                {
                    return false; // Skip original
                }

                if ((bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance))
                {
                    // Call ShotClientRpc using cached reflection
                    EnemyReflectionCache.ProjectileLauncherShotClientRpc?.Invoke(__instance, null);
                }

                GameObject gameObject;
                if (CustomMapEditor.ParkourActive())
                {
                    gameObject = Object.Instantiate(__instance.projectile, __instance.point.position, __instance.transform.rotation, CustomMapEditor.instance.objParent);
                }
                else
                {
                    gameObject = Object.Instantiate(__instance.projectile, __instance.point.position, __instance.transform.rotation);
                }

                BasicProjectile basicProjectileComponent = gameObject.GetComponent<BasicProjectile>();
                if (basicProjectileComponent != null)
                {
                    HitLogic.SetProjectileOwner(basicProjectileComponent, __instance.GetOwnerClientId(), __instance);
                }
                else
                {
                    Weapon weaponComponent = gameObject.GetComponent<Weapon>();
                    if (weaponComponent)
                    {
                        weaponComponent.ownerWeaponClientId = __instance.GetOwnerClientId();

                        // **THIS IS MY ADDITION** - Set the owner property for ParticleBlade tracking
                        weaponComponent.owner = __instance.owner;
                    }
                }

                gameObject.GetComponent<NetworkObject>().Spawn(true);
                PickupEffects componentInChildren = gameObject.GetComponentInChildren<PickupEffects>();
                componentInChildren?.StopFloat();

                Rigidbody2D rigidbody = gameObject.GetComponent<Rigidbody2D>();
                rigidbody.AddForce(__instance.transform.up * __instance.shotForce, ForceMode2D.Impulse);
                rigidbody.AddTorque(__instance.rotationForce, ForceMode2D.Impulse);

                // Call Impact method using cached reflection
                EnemyReflectionCache.ProjectileLauncherImpact?.Invoke(__instance, new object[] { -__instance.recoil * __instance.transform.up, __instance.point.position, false, true });

                // Handle collision ignoring
                if (EnemyReflectionCache.ProjectileLauncherCollider != null)
                {
                    Collider2D launcherCollider = (Collider2D)EnemyReflectionCache.ProjectileLauncherCollider.GetValue(__instance);
                    Physics2D.IgnoreCollision(launcherCollider, gameObject.GetComponent<Collider2D>(), true);
                }

                if (__instance.equipped)
                {
                    Physics2D.IgnoreCollision(__instance.owner?.healthSystem?.GetComponent<Collider2D>(), gameObject.GetComponent<Collider2D>(), true);
                }

                float ammo = __instance.ammo;
                __instance.ammo = ammo - 1f;

                // Set hold offset using cached reflection
                if (EnemyReflectionCache.ProjectileLauncherReloadOffset != null)
                {
                    __instance.holdOffset = (Vector2)EnemyReflectionCache.ProjectileLauncherReloadOffset.GetValue(__instance);
                }

                return false; // Skip the original method
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error in ProjectileLauncher Shoot patch: {ex.Message}");
                return true; // Execute original method on error
            }
        }
    }


    // Patch Weapon.Equip to set the correct ownerWeaponClientId after the original method runs
    [HarmonyPatch(typeof(Weapon), "Equip")]
    class WeaponEquipPatch
    {
        static void Postfix(Weapon __instance, WeaponManager wm)
        {
            try
            {
                // After the original Equip method runs, fix the ownerWeaponClientId
                if (wm != null && wm.healthSystem != null)
                {
                    PlayerController playerController = wm.healthSystem.GetComponentInParent<PlayerController>();
                    if (playerController != null)
                    {
                        // Override the ulong.MaxValue that was set by the original method
                        __instance.ownerWeaponClientId = (ulong)playerController.playerID.Value;
                    }

                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error in WeaponEquipPatch: {ex.Message}");
            }
        }
    }

}
