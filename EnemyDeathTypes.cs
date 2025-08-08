using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using System;
using Interfaces;
using UnityEngine.InputSystem;
using System.Collections.Generic;


namespace StatsMod
{
    public static class EnemyDeathHelper
    {
        public static bool WillDieToDamage(GameObject enemy)
        {
            EnemyHealthSystem enemyHealthSystem = enemy.GetComponent<EnemyHealthSystem>();
            if (enemyHealthSystem == null)
            {
                return false;
            }

            // Same checks as in EnemyHealthSystem.Damage method
            // Access IsHost property using reflection since it's protected
            var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
            bool isHost = (bool)isHostProperty.GetValue(enemyHealthSystem);

            if (!isHost)
            {
                return false;
            }

            if (enemyHealthSystem.ignoreDirectDamage)
            {
                return false;
            }

            // Access private _immuneTime field using reflection
            var immuneTimeField = AccessTools.Field(typeof(EnemyHealthSystem), "_immuneTime");
            float immuneTime = (float)immuneTimeField.GetValue(enemyHealthSystem);

            if (Time.time < immuneTime)
            {
                return false;
            }

            // If enemy has an active shield, it won't die - just lose the shield
            if (enemyHealthSystem.shield && enemyHealthSystem.shield.activeInHierarchy)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(EnemyHealthSystem), "Explode")]
    class EnemyDeathCountPatch
    {
        static void Postfix(EnemyHealthSystem __instance)
        {
            try
            {
                EnemiesTracker.Instance.IncrementEnemiesKilled();
                Logger.LogInfo("Enemy killed via Explode method.");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording enemy kill: {ex.Message}");
            }
        }
    }

    // Shotguns
    [HarmonyPatch(typeof(DamageOnParticle), "OnParticleCollision")]
    class DamageOnParticleDamagePatch
    {
        static void Prefix(DamageOnParticle __instance, GameObject other)
        {
            try
            {
                if (other == __instance.ignoreWeapon.gameObject || other == __instance.ignoreWeapon.owner.healthSystem.gameObject)
                {
                    return;
                }

                IDamageable component = other.GetComponent<IDamageable>();
                if (component != null && EnemyDeathHelper.WillDieToDamage(other))
                {
                    PlayerTracker.Instance.RecordPlayerKill(__instance.ignoreWeapon.owner.healthSystem.GetComponentInParent<PlayerInput>());
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error logging damage info: {ex.Message}");
            }
        }
    }

    // DeathCube
    [HarmonyPatch(typeof(DeathCube), "Fire")]
    class DeathCubeDamagePatch
    {
        static void Prefix(DeathCube __instance, Vector3 direction, LineRenderer laser)
        {
            try
            {
                if (!__instance.IsHost)
                {
                    return;
                }

                var laserColliderLayersField = AccessTools.Field(typeof(DeathCube), "laserColliderLayers");
                var beamWidthField = AccessTools.Field(typeof(DeathCube), "beamWidth");

                if (laserColliderLayersField == null || beamWidthField == null)
                {
                    Logger.LogWarning("Could not access DeathCube fields for laser damage tracking");
                    return;
                }

                LayerMask laserColliderLayers = (LayerMask)laserColliderLayersField.GetValue(__instance);
                float beamWidth = (float)beamWidthField.GetValue(__instance);

                RaycastHit2D[] hits = Physics2D.CircleCastAll(
                    laser.transform.position + direction * beamWidth,
                    beamWidth * 0.75f,
                    direction,
                    500f,
                    laserColliderLayers
                );

                foreach (RaycastHit2D hit in hits)
                {
                    IDamageable component = hit.transform.GetComponent<IDamageable>();
                    if (component != null && EnemyDeathHelper.WillDieToDamage(hit.transform.gameObject))
                    {
                        PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                        if (ownerPlayer != null)
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking DeathCube laser damage: {ex.Message}");
            }
        }
    }

    // DeathRay
    [HarmonyPatch(typeof(DeathRay), "Fire")]
    class DeathRayDamagePatch
    {
        static void Prefix(DeathRay __instance)
        {
            try
            {
                if (!__instance.IsHost)
                {
                    return;
                }

                var damageLayersField = AccessTools.Field(typeof(DeathRay), "damageLayers");
                var beamWidthField = AccessTools.Field(typeof(DeathRay), "beamWidth");
                var beamLengthField = AccessTools.Field(typeof(DeathRay), "beamLength");
                var pointField = AccessTools.Field(typeof(DeathRay), "point");

                if (damageLayersField == null || beamWidthField == null || beamLengthField == null || pointField == null)
                {
                    Logger.LogWarning("Could not access DeathRay fields for laser damage tracking");
                    return;
                }

                LayerMask damageLayers = (LayerMask)damageLayersField.GetValue(__instance);
                float beamWidth = (float)beamWidthField.GetValue(__instance);
                float beamLength = (float)beamLengthField.GetValue(__instance);
                Transform point = (Transform)pointField.GetValue(__instance);

                RaycastHit2D[] hits = Physics2D.CircleCastAll(
                    point.position + __instance.transform.up * beamWidth / 4f,
                    beamWidth / 4f,
                    __instance.transform.up,
                    beamLength,
                    damageLayers
                );

                foreach (RaycastHit2D hit in hits)
                {
                    IDamageable component = hit.transform.GetComponent<IDamageable>();
                    if (component != null && EnemyDeathHelper.WillDieToDamage(hit.transform.gameObject))
                    {
                        PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                        if (ownerPlayer != null)
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking DeathRay laser damage: {ex.Message}");
            }
        }
    }

    // EnergyBall
    [HarmonyPatch(typeof(EnergyBall), "OnCollisionEnter2D")]
    class EnergyBallDamagePatch
    {
        static void Prefix(EnergyBall __instance, Collision2D other)
        {
            try
            {
                if (!__instance.IsHost)
                {
                    return;
                }

                if (other.gameObject == __instance.ignore)
                {
                    return;
                }

                IDamageable component = other.gameObject.GetComponent<IDamageable>();
                if (component == null)
                {
                    return;
                }

                var damageEnemiesField = AccessTools.Field(typeof(EnergyBall), "damageEnemies");
                if (damageEnemiesField == null)
                {
                    Logger.LogWarning("Could not access EnergyBall damageEnemies field for damage tracking");
                    return;
                }

                bool damageEnemies = (bool)damageEnemiesField.GetValue(__instance);

                // Check if it's an enemy and if we should damage enemies
                if (__instance.IsEnemy(other.gameObject) && !damageEnemies)
                {
                    return;
                }

                if (__instance.IsDamageable(other.gameObject) && EnemyDeathHelper.WillDieToDamage(other.gameObject))
                {
                    PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking EnergyBall damage: {ex.Message}");
            }
        }
    }

    // ForceField
    [HarmonyPatch(typeof(ForceField), "Damage")]
    class ForceFieldDamagePatch
    {
        static void Prefix(ForceField __instance, Collision2D other)
        {
            try
            {
                IDamageable component = other.gameObject.GetComponent<IDamageable>();
                if (component == null)
                {
                    return;
                }

                // Check if this will call Damage (not just Impact)
                bool willCallDamage = !other.transform.CompareTag("Weapon");

                if (willCallDamage && EnemyDeathHelper.WillDieToDamage(other.gameObject))
                {
                    PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking ForceField damage: {ex.Message}");
            }
        }
    }

    // FriendlyWaspStinger
    [HarmonyPatch(typeof(FriendlyWaspStinger), "OnCollisionEnter2D")]
    class FriendlyWaspStingerDamagePatch
    {
        static void Prefix(FriendlyWaspStinger __instance, Collision2D other)
        {
            try
            {
                if (!__instance.IsHost)
                {
                    return;
                }

                if (other.transform.tag.Equals("Environment"))
                {
                    return;
                }

                IDamageable component = other.gameObject.GetComponent<IDamageable>();
                if (component == null)
                {
                    return;
                }

                // Check if this will call Damage (using IsDamageable method)
                if (__instance.IsDamageable(other.gameObject) && EnemyDeathHelper.WillDieToDamage(other.gameObject))
                {
                    PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking FriendlyWaspStinger damage: {ex.Message}");
            }
        }
    }

    // KhepriStaff
    [HarmonyPatch(typeof(KhepriStaff), "Zap")]
    class KhepriStaffDamagePatch
    {
        static void Prefix(KhepriStaff __instance, GameObject target)
        {
            try
            {
                if (target == __instance.owner.healthSystem.gameObject)
                {
                    return;
                }

                IDamageable component = target.GetComponent<IDamageable>();
                if (component == null)
                {
                    return;
                }

                // Check if this will call Damage (based on layer and tag conditions)
                bool willCallDamage = (target.layer == LayerMask.NameToLayer("Enemy") ||
                                     target.layer == LayerMask.NameToLayer("EnemyWeapon") ||
                                     target.CompareTag("PlayerRigidbody"));

                if (willCallDamage && EnemyDeathHelper.WillDieToDamage(target))
                {
                    PlayerInput ownerPlayer = __instance.owner.healthSystem.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking KhepriStaff damage: {ex.Message}");
            }
        }
    }

    // Laser Cannon
    [HarmonyPatch(typeof(LaserCannon), "DamageHit")]
    class LaserCannonDamagePatch
    {
        static void Prefix(LaserCannon __instance, RaycastHit2D hit, IDamageable damageable)
        {
            try
            {
                if (damageable != null && EnemyDeathHelper.WillDieToDamage(hit.collider.gameObject))
                {
                    PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking LaserCannon damage: {ex.Message}");
            }
        }
    }

    // Laser Cube
    [HarmonyPatch(typeof(LaserCube), "Beam")]
    class LaserCubeDamagePatch
    {
        static void Prefix(LaserCube __instance, Vector3 direction, LineRenderer lineRenderer)
        {
            try
            {
                if (!__instance.IsHost)
                {
                    return;
                }

                RaycastHit2D hit = Physics2D.CircleCast(
                    lineRenderer.transform.position + direction * __instance.beamWidth / 2f,
                    __instance.beamWidth / 2f,
                    direction,
                    500f,
                    __instance.laserColliderLayers
                );

                if (hit)
                {
                    if (hit.collider.gameObject == __instance.gameObject)
                    {
                        return;
                    }

                    IDamageable component = hit.collider.gameObject.GetComponent<IDamageable>();
                    if (component != null && EnemyDeathHelper.WillDieToDamage(hit.collider.gameObject))
                    {
                        PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                        if (ownerPlayer != null)
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking LaserCube damage: {ex.Message}");
            }
        }
    }

    // RailShot
    [HarmonyPatch(typeof(RailShot), "ProcessHit")]
    class RailShotDamagePatch
    {
        static void Prefix(RailShot __instance, RaycastHit2D hit, Vector2 direction)
        {
            try
            {
                
                if (!__instance.IsHost)
                {
                    return;
                }

                IDamageable component = hit.transform.GetComponent<IDamageable>();
                if (component != null && EnemyDeathHelper.WillDieToDamage(hit.transform.gameObject))
                {
                    PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking RailShot damage: {ex.Message}");
            }
        }
    }

    //no idea what is this type of damage
    // // RollerStrut
    // [HarmonyPatch(typeof(RollerStrut), "OnCollisionEnter2D")]
    // class RollerStrutDamagePatch
    // {
    //     static void Prefix(RollerStrut __instance, Collision2D other)
    //     {
    //         try
    //         {
    //             if (!__instance.IsHost)
    //             {
    //                 return;
    //             }

    //             if (other.transform.tag.Equals("Environment"))
    //             {
    //                 return;
    //             }

    //             IDamageable component = other.gameObject.GetComponent<IDamageable>();
    //             if (component == null)
    //             {
    //                 return;
    //             }

    //             // Check if this will call Damage (only for PlayerRigidbody tag)
    //             if (other.transform.tag.Equals("PlayerRigidbody") && EnemyDeathHelper.WillDieToDamage(other.gameObject))
    //             {
    //                 PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
    //                 if (ownerPlayer != null)
    //                 {
    //                     PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
    //                 }
    //             }
    //         }
    //         catch (System.Exception ex)
    //         {
    //             Logger.LogError($"Error tracking RollerStrut damage: {ex.Message}");
    //         }
    //     }
    // }

    // SawDisc
    [HarmonyPatch(typeof(SawDisc), "TryDamage")]
    class SawDiscDamagePatch
    {
        static void Prefix(SawDisc __instance, Collision2D other)
        {
            try
            {
                IDamageable component = other.gameObject.GetComponent<IDamageable>();
                if (component == null)
                {
                    return;
                }

                // Check if this will call Damage (not Impact for weapons)
                bool willCallDamage = !other.transform.CompareTag("Weapon");

                if (willCallDamage && EnemyDeathHelper.WillDieToDamage(other.gameObject))
                {
                    PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking SawDisc damage: {ex.Message}");
            }
        }
    }

    // SpiderHealthSystem
    [HarmonyPatch(typeof(SpiderHealthSystem), "AfterlifeExplosion")]
    class SpiderHealthSystemAfterlifeExplosionPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                var knockBackRadiusField = AccessTools.Field(typeof(SpiderHealthSystem), "knockBackRadius");
                var layersField = AccessTools.Field(typeof(SpiderHealthSystem), "layers");
                var deathRadiusField = AccessTools.Field(typeof(SpiderHealthSystem), "_deathRadius");

                if (knockBackRadiusField == null || layersField == null || deathRadiusField == null)
                {
                    Logger.LogWarning("Could not access SpiderHealthSystem fields for afterlife explosion tracking");
                    return;
                }

                float knockBackRadius = (float)knockBackRadiusField.GetValue(__instance);
                LayerMask layers = (LayerMask)layersField.GetValue(__instance);
                float deathRadius = (float)deathRadiusField.GetValue(__instance);

                // Simulate the same overlap detection as the original method
                Collider2D[] colliders = Physics2D.OverlapCircleAll(__instance.transform.position, knockBackRadius, layers);

                foreach (Collider2D collider2D in colliders)
                {
                    if (collider2D.gameObject == __instance.gameObject)
                    {
                        continue;
                    }

                    IDamageable component = collider2D.GetComponent<IDamageable>();
                    if (component == null)
                    {
                        continue;
                    }

                    Vector3 position = __instance.transform.position;
                    Vector2 vector = collider2D.ClosestPoint(position);
                    float num = Vector2.Distance(position, vector);

                    // Check if this will call Damage (within death radius)
                    bool willCallDamage = (num <= deathRadius);

                    if (willCallDamage && EnemyDeathHelper.WillDieToDamage(collider2D.gameObject))
                    {
                        // Player spider's afterlife explosion killing enemies - record as player kill
                        PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                        if (ownerPlayer != null)
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking SpiderHealthSystem afterlife explosion: {ex.Message}");
            }
        }
    }

    // Explosion
    [HarmonyPatch(typeof(Explosion), "KnockBack")]
    class ExplosionDamagePatch
    {
        static void Prefix(Explosion __instance)
        {
            try
            {
                var knockBackRadiusField = AccessTools.Field(typeof(Explosion), "knockBackRadius");
                var layersField = AccessTools.Field(typeof(Explosion), "layers");
                var deathRadiusField = AccessTools.Field(typeof(Explosion), "deathRadius");
                var playerDeathRadiusField = AccessTools.Field(typeof(Explosion), "_playerDeathRadius");
                var isBoomSpearField = AccessTools.Field(typeof(Explosion), "isBoomSpear");
                var ignoreDisarmField = AccessTools.Field(typeof(Explosion), "ignoreDisarm");
                var playerExplosionIDField = AccessTools.Field(typeof(Explosion), "playerExplosionID");

                if (knockBackRadiusField == null || layersField == null || deathRadiusField == null ||
                    playerDeathRadiusField == null || isBoomSpearField == null || playerExplosionIDField == null)
                {
                    Logger.LogWarning("Could not access Explosion fields for damage tracking");
                    return;
                }

                float knockBackRadius = (float)knockBackRadiusField.GetValue(__instance);
                LayerMask layers = (LayerMask)layersField.GetValue(__instance);
                float deathRadius = (float)deathRadiusField.GetValue(__instance);
                float playerDeathRadius = (float)playerDeathRadiusField.GetValue(__instance);
                bool isBoomSpear = (bool)isBoomSpearField.GetValue(__instance);
                Weapon ignoreDisarm = (Weapon)ignoreDisarmField.GetValue(__instance);
                var playerExplosionID = playerExplosionIDField.GetValue(__instance);

                // Simulate the same overlap detection as the original method
                Collider2D[] colliders = Physics2D.OverlapCircleAll(__instance.transform.position, knockBackRadius, layers);

                foreach (Collider2D collider2D in colliders)
                {
                    IDamageable componentInParent = collider2D.GetComponentInParent<IDamageable>();
                    if (componentInParent == null)
                    {
                        continue;
                    }

                    bool flag = (componentInParent is IceBlock);
                    Weapon weapon = null;
                    if (isBoomSpear)
                    {
                        weapon = (componentInParent as Weapon);
                    }

                    Vector3 position = __instance.transform.position;
                    Vector2 vector = collider2D.ClosestPoint(position);
                    float num = Vector2.Distance(position, vector);

                    // Check if this will result in Damage (not just Impact)
                    bool willDealDamage = false;

                    if (num <= deathRadius || flag)
                    {
                        // Within death radius or is ice block
                        if (collider2D.CompareTag("PlayerRigidbody") && num <= playerDeathRadius)
                        {
                            // Player within player death radius - will deal damage unless it's boom spear self-damage
                            if (isBoomSpear)
                            {
                                PlayerController playerController;
                                if (collider2D.transform.parent.parent.TryGetComponent<PlayerController>(out playerController) &&
                                    playerController != null && playerController.playerID.Value.Equals(playerExplosionID))
                                {
                                    // Self damage from boom spear - only Impact, no damage
                                    willDealDamage = false;
                                }
                                else
                                {
                                    willDealDamage = true;
                                }
                            }
                            else
                            {
                                willDealDamage = true;
                            }
                        }
                        else if (!collider2D.CompareTag("PlayerRigidbody"))
                        {
                            // Non-player within death radius - will deal damage
                            willDealDamage = true;
                        }
                    }

                    // Check if the target will die from the damage and track the kill
                    if (willDealDamage && EnemyDeathHelper.WillDieToDamage(collider2D.gameObject))
                    {
                        // Find the player who caused this explosion
                        PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                        if (ownerPlayer == null)
                        {
                            // Try to find player by explosion owner ID if available
                            PlayerController[] players = UnityEngine.Object.FindObjectsOfType<PlayerController>();
                            foreach (PlayerController player in players)
                            {
                                if (player.playerID.Value.Equals(playerExplosionID))
                                {
                                    ownerPlayer = player.GetComponent<PlayerInput>();
                                    break;
                                }
                            }
                        }

                        if (ownerPlayer != null)
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking Explosion damage: {ex.Message}");
            }
        }
    }
}
