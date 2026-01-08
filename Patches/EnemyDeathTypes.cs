using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;
using HarmonyLib;
using Interfaces;
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

        // DeathCube fields
        public static readonly FieldInfo DeathCubeLaserColliderLayers =
            AccessTools.Field(typeof(DeathCube), "laserColliderLayers");
        public static readonly FieldInfo DeathCubeBeamWidth =
            AccessTools.Field(typeof(DeathCube), "beamWidth");

        // DeathRay fields
        public static readonly FieldInfo DeathRayDamageLayers =
            AccessTools.Field(typeof(DeathRay), "damageLayers");
        public static readonly FieldInfo DeathRayBeamWidth =
            AccessTools.Field(typeof(DeathRay), "beamWidth");
        public static readonly FieldInfo DeathRayBeamLength =
            AccessTools.Field(typeof(DeathRay), "beamLength");
        public static readonly FieldInfo DeathRayPoint =
            AccessTools.Field(typeof(DeathRay), "point");

        // EnergyBall fields and methods
        public static readonly FieldInfo EnergyBallDamageEnemies =
            AccessTools.Field(typeof(EnergyBall), "damageEnemies");
        public static readonly MethodInfo EnergyBallIsEnemy =
            AccessTools.Method(typeof(EnergyBall), "IsEnemy");
        public static readonly MethodInfo EnergyBallIsDamageable =
            AccessTools.Method(typeof(EnergyBall), "IsDamageable");

        // DiscLauncher methods
        public static readonly MethodInfo DiscLauncherLaunchDiscClientRpc =
            AccessTools.Method(typeof(DiscLauncher), "LaunchDiscClientRpc");
        public static readonly MethodInfo DiscLauncherImpact =
            AccessTools.Method(typeof(DiscLauncher), "Impact");

        // SpiderHealthSystem fields
        public static readonly FieldInfo SpiderKnockBackRadius =
            AccessTools.Field(typeof(SpiderHealthSystem), "knockBackRadius");
        public static readonly FieldInfo SpiderLayers =
            AccessTools.Field(typeof(SpiderHealthSystem), "layers");
        public static readonly FieldInfo SpiderDeathRadius =
            AccessTools.Field(typeof(SpiderHealthSystem), "_deathRadius");

        // Explosion fields
        public static readonly FieldInfo ExplosionKnockBackRadius =
            AccessTools.Field(typeof(Explosion), "knockBackRadius");
        public static readonly FieldInfo ExplosionLayers =
            AccessTools.Field(typeof(Explosion), "layers");
        public static readonly FieldInfo ExplosionDeathRadius =
            AccessTools.Field(typeof(Explosion), "deathRadius");
        public static readonly FieldInfo ExplosionPlayerDeathRadius =
            AccessTools.Field(typeof(Explosion), "_playerDeathRadius");
        public static readonly FieldInfo ExplosionIsBoomSpear =
            AccessTools.Field(typeof(Explosion), "isBoomSpear");
        public static readonly FieldInfo ExplosionPlayerExplosionID =
            AccessTools.Field(typeof(Explosion), "playerExplosionID");
        public static readonly FieldInfo ExplosionOwnerId =
            AccessTools.Field(typeof(Explosion), "explosionOwnerId");

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

    [HarmonyPatch(typeof(EnemyHealthSystem), "Explode")]
    class EnemyDeathCountPatch
    {
        static void Postfix(EnemyHealthSystem __instance)
        {
            try
            {
                EnemiesTracker.Instance.IncrementEnemiesKilled();
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording enemy kill: {ex.Message}");
            }
        }
    }

    // Shotgun
    [HarmonyPatch(typeof(DamageOnParticle), "OnParticleCollision")]
    class DamageOnParticleDamagePatch
    {
        static void Prefix(DamageOnParticle __instance, GameObject other)
        {
            try
            {
                if (other == __instance.ignoreWeapon?.gameObject || other == __instance.ignoreWeapon?.owner?.healthSystem?.gameObject)
                    return;

                IDamageable component = other.GetComponent<IDamageable>();
                if (component != null)
                {
                    PlayerInput playerInput = __instance.ignoreWeapon?.owner?.healthSystem?.GetComponentInParent<PlayerInput>();
                    HitLogic.RecordHit(other, playerInput, "Shotgun");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error logging shotgun damage info: {ex.Message}");
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
                if (!(bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance)) return;

                IDamageable component = hit.transform.GetComponent<IDamageable>();
                if (component != null)
                {
                    PlayerInput ownerPlayer = FindOwnerPlayerFromIgnoreList(__instance.ignore.ToArray());
                    HitLogic.RecordHit(hit.transform.gameObject, ownerPlayer, "RailShot");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking RailShot damage: {ex.Message}");
            }
        }

        private static PlayerInput FindOwnerPlayerFromIgnoreList(GameObject[] ignoreList)
        {
            int playerCount = 0;
            PlayerInput ownerPlayer = null;

            foreach (GameObject ignoreObj in ignoreList)
            {
                if (ignoreObj != null)
                {
                    SpiderHealthSystem spiderHealth = ignoreObj.GetComponent<SpiderHealthSystem>()
                        ?? ignoreObj.GetComponentInParent<SpiderHealthSystem>();

                    if (spiderHealth != null)
                    {
                        playerCount++;
                        if (ownerPlayer == null)
                            ownerPlayer = spiderHealth.GetComponentInParent<PlayerInput>();
                    }
                }
            }

            //not sure if needed, implemented this because this is a list and i was suspicious
            if (playerCount > 1)
                Logger.LogError($"RailShot: Found {playerCount} players in ignore list, expected only 1");
            if (ownerPlayer == null)
                Logger.LogError("RailShot: Could not find owner player in ignore list");

            return ownerPlayer;
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
                if (!(bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance)) return;

                if (EnemyReflectionCache.DeathCubeLaserColliderLayers == null || EnemyReflectionCache.DeathCubeBeamWidth == null)
                {
                    return;
                }

                LayerMask laserColliderLayers = (LayerMask)EnemyReflectionCache.DeathCubeLaserColliderLayers.GetValue(__instance);
                float beamWidth = (float)EnemyReflectionCache.DeathCubeBeamWidth.GetValue(__instance);

                RaycastHit2D[] hits = Physics2D.CircleCastAll(
                    laser.transform.position + direction * beamWidth,
                    beamWidth * 0.75f,
                    direction,
                    500f,
                    laserColliderLayers
                );

                PlayerInput ownerPlayer = __instance.owner?.GetComponentInParent<PlayerInput>();
                foreach (RaycastHit2D hit in hits)
                {
                    IDamageable component = hit.transform.GetComponent<IDamageable>();
                    if (component != null)
                    {
                        HitLogic.RecordHit(hit.transform.gameObject, ownerPlayer, "DeathCube");
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
                if (!(bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance)) return;

                if (EnemyReflectionCache.DeathRayDamageLayers == null || EnemyReflectionCache.DeathRayBeamWidth == null ||
                    EnemyReflectionCache.DeathRayBeamLength == null || EnemyReflectionCache.DeathRayPoint == null)
                {
                    return;
                }

                LayerMask damageLayers = (LayerMask)EnemyReflectionCache.DeathRayDamageLayers.GetValue(__instance);
                float beamWidth = (float)EnemyReflectionCache.DeathRayBeamWidth.GetValue(__instance);
                float beamLength = (float)EnemyReflectionCache.DeathRayBeamLength.GetValue(__instance);
                Transform point = (Transform)EnemyReflectionCache.DeathRayPoint.GetValue(__instance);

                RaycastHit2D[] hits = Physics2D.CircleCastAll(
                    point.position + __instance.transform.up * beamWidth / 4f,
                    beamWidth / 4f,
                    __instance.transform.up,
                    beamLength,
                    damageLayers
                );

                PlayerInput ownerPlayer = __instance.owner?.GetComponentInParent<PlayerInput>();
                foreach (RaycastHit2D hit in hits)
                {
                    IDamageable component = hit.transform.GetComponent<IDamageable>();
                    if (component != null)
                    {
                        HitLogic.RecordHit(hit.transform.gameObject, ownerPlayer, "DeathRay");
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
                if (!(bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance)) return;

                if (other.gameObject == __instance.ignore) return;

                IDamageable component = other.gameObject.GetComponent<IDamageable>();
                if (component == null) return;

                if (EnemyReflectionCache.EnergyBallDamageEnemies == null ||
                    EnemyReflectionCache.EnergyBallIsEnemy == null ||
                    EnemyReflectionCache.EnergyBallIsDamageable == null)
                {
                    return;
                }

                bool damageEnemies = (bool)EnemyReflectionCache.EnergyBallDamageEnemies.GetValue(__instance);

                bool isEnemy = (bool)EnemyReflectionCache.EnergyBallIsEnemy.Invoke(__instance, new object[] { other.gameObject });
                if (isEnemy && !damageEnemies) return;

                bool isDamageable = (bool)EnemyReflectionCache.EnergyBallIsDamageable.Invoke(__instance, new object[] { other.gameObject });
                if (isDamageable)
                {
                    PlayerInput ownerPlayer = __instance.owner?.GetComponentInParent<PlayerInput>();
                    HitLogic.RecordHit(other.gameObject, ownerPlayer, "EnergyBall");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking EnergyBall damage: {ex.Message}");
            }
        }
    }

    // Particle Blade
    [HarmonyPatch(typeof(ForceField), "Damage")]
    class ForceFieldDamagePatch
    {
        static void Prefix(ForceField __instance, Collision2D other)
        {
            try
            {
                IDamageable component = other.gameObject.GetComponent<IDamageable>();
                if (component == null) return;

                bool willCallDamage = !other.transform.CompareTag("Weapon");
                if (willCallDamage)
                {
                    Weapon parentWeapon = __instance.GetComponentInParent<Weapon>();
                    if (parentWeapon != null && parentWeapon.owner != null)
                    {
                        PlayerInput playerInput = parentWeapon.owner?.healthSystem?.GetComponentInParent<PlayerInput>();
                        HitLogic.RecordHit(other.gameObject, playerInput, "Particle Blade");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking ForceField damage: {ex.Message}");
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
                if (target == __instance.owner?.healthSystem?.gameObject) return;

                IDamageable component = target.GetComponent<IDamageable>();
                if (component == null) return;

                bool willCallDamage = target.layer == LayerMask.NameToLayer("Enemy") ||
                                     target.layer == LayerMask.NameToLayer("EnemyWeapon") ||
                                     target.CompareTag("PlayerRigidbody");

                if (willCallDamage)
                {
                    PlayerInput ownerPlayer = __instance.owner?.healthSystem?.GetComponentInParent<PlayerInput>();
                    HitLogic.RecordHit(target, ownerPlayer, "KhepriStaff");
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
                if (damageable != null)
                {
                    PlayerInput ownerPlayer = __instance.owner?.GetComponentInParent<PlayerInput>();
                    HitLogic.RecordHit(hit.collider.gameObject, ownerPlayer, "Laser Cannon");
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
                if (!(bool)EnemyReflectionCache.IsHostProperty.GetValue(__instance)) return;

                RaycastHit2D hit = Physics2D.CircleCast(
                    lineRenderer.transform.position + direction * __instance.beamWidth / 2f,
                    __instance.beamWidth / 2f,
                    direction,
                    500f,
                    __instance.laserColliderLayers
                );

                if (hit && hit.collider.gameObject != __instance.gameObject)
                {
                    IDamageable component = hit.collider.gameObject.GetComponent<IDamageable>();
                    if (component != null)
                    {
                        PlayerInput ownerPlayer = __instance.owner?.GetComponentInParent<PlayerInput>();
                        HitLogic.RecordHit(hit.collider.gameObject, ownerPlayer, "Laser Cube");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking LaserCube damage: {ex.Message}");
            }
        }
    }

    // SawDisc
    [HarmonyPatch(typeof(SawDisc), "TryDamage")]
    class SawDiscDamagePatch
    {
        static void Prefix(SawDisc __instance, Collision2D other)
        {
            try
            {
                IDamageable component = other.gameObject.GetComponent<IDamageable>();
                if (component == null) return;

                bool willCallDamage = !other.transform.CompareTag("Weapon");
                if (willCallDamage)
                {
                    // First try to get the owner from disc tracking
                    PlayerInput ownerPlayer = HitLogic.GetDiscOwner(__instance.gameObject);

                    // Fallback to getting from parent (may not work for projectiles)
                    if (ownerPlayer == null)
                    {
                        ownerPlayer = __instance.GetComponentInParent<PlayerInput>();
                    }

                    HitLogic.RecordHit(other.gameObject, ownerPlayer, "SawDisc");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking SawDisc damage: {ex.Message}");
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

    // SpiderHealthSystem
    [HarmonyPatch(typeof(SpiderHealthSystem), "AfterlifeExplosion")]
    class SpiderHealthSystemAfterlifeExplosionPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                if (EnemyReflectionCache.SpiderKnockBackRadius == null || EnemyReflectionCache.SpiderLayers == null || EnemyReflectionCache.SpiderDeathRadius == null)
                {
                    Logger.LogWarning("Could not access SpiderHealthSystem fields for afterlife explosion tracking");
                    return;
                }

                float knockBackRadius = (float)EnemyReflectionCache.SpiderKnockBackRadius.GetValue(__instance);
                LayerMask layers = (LayerMask)EnemyReflectionCache.SpiderLayers.GetValue(__instance);
                float deathRadius = (float)EnemyReflectionCache.SpiderDeathRadius.GetValue(__instance);

                Collider2D[] colliders = Physics2D.OverlapCircleAll(__instance.transform.position, knockBackRadius, layers);
                PlayerInput ownerPlayer = __instance.GetComponentInParent<PlayerInput>();

                foreach (Collider2D collider2D in colliders)
                {
                    if (collider2D.gameObject == __instance.gameObject) continue;

                    IDamageable component = collider2D.GetComponent<IDamageable>();
                    if (component == null) continue;

                    Vector3 position = __instance.transform.position;
                    Vector2 vector = collider2D.ClosestPoint(position);
                    float distance = Vector2.Distance(position, vector);

                    if (distance <= deathRadius)
                    {
                        HitLogic.RecordHit(collider2D.gameObject, ownerPlayer, "Explosions");
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
                if (!NetworkManager.Singleton.IsHost) return;

                var fields = GetExplosionFields(__instance);
                if (fields == null) return;

                var colliders = Physics2D.OverlapCircleAll(__instance.transform.position, fields.knockBackRadius, fields.layers);

                foreach (Collider2D collider2D in colliders)
                {
                    IDamageable componentInParent = collider2D.GetComponentInParent<IDamageable>();
                    if (componentInParent == null) continue;

                    if (WillCallExplosionDamage(__instance.transform.position, collider2D, fields))
                    {
                        PlayerInput ownerPlayer = HitLogic.FindPlayerInputByPlayerId(fields.explosionOwnerId);
                        HitLogic.RecordHit(collider2D.gameObject, ownerPlayer, "Explosions");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking Explosion damage: {ex.Message}");
            }
        }

        private static ExplosionFields GetExplosionFields(Explosion instance)
        {
            if (EnemyReflectionCache.ExplosionKnockBackRadius == null || EnemyReflectionCache.ExplosionLayers == null || EnemyReflectionCache.ExplosionDeathRadius == null || EnemyReflectionCache.ExplosionPlayerDeathRadius == null || EnemyReflectionCache.ExplosionIsBoomSpear == null || EnemyReflectionCache.ExplosionPlayerExplosionID == null || EnemyReflectionCache.ExplosionOwnerId == null)
            {
                Logger.LogWarning("Could not access Explosion fields for damage tracking");
                return null;
            }

            return new ExplosionFields
            {
                knockBackRadius = (float)EnemyReflectionCache.ExplosionKnockBackRadius.GetValue(instance),
                layers = (LayerMask)EnemyReflectionCache.ExplosionLayers.GetValue(instance),
                deathRadius = (float)EnemyReflectionCache.ExplosionDeathRadius.GetValue(instance),
                playerDeathRadius = (float)EnemyReflectionCache.ExplosionPlayerDeathRadius.GetValue(instance),
                isBoomSpear = (bool)EnemyReflectionCache.ExplosionIsBoomSpear.GetValue(instance),
                playerExplosionId = (int)EnemyReflectionCache.ExplosionPlayerExplosionID.GetValue(instance),
                explosionOwnerId = (ulong)EnemyReflectionCache.ExplosionOwnerId.GetValue(instance)
            };
        }

        private static bool WillCallExplosionDamage(Vector3 explosionPosition, Collider2D collider, ExplosionFields fields)
        {
            bool isIceBlock = (collider.GetComponentInParent<IDamageable>() is IceBlock);
            Vector2 closestPoint = collider.ClosestPoint(explosionPosition);
            float distance = Vector2.Distance(explosionPosition, closestPoint);

            if (distance > fields.deathRadius && !isIceBlock) return false;
            if (collider.CompareTag("PlayerRigidbody") && distance > fields.playerDeathRadius) return false;

            if (fields.isBoomSpear && collider.CompareTag("PlayerRigidbody"))
            {
                PlayerController playerController;
                if (collider.transform.parent.parent.TryGetComponent<PlayerController>(out playerController) &&
                    playerController != null &&
                    playerController.playerID.Value == fields.playerExplosionId)
                {
                    return false; // Will call Impact, not Damage
                }
            }

            return true;
        }

        private class ExplosionFields
        {
            public float knockBackRadius;
            public LayerMask layers;
            public float deathRadius;
            public float playerDeathRadius;
            public bool isBoomSpear;
            public int playerExplosionId;
            public ulong explosionOwnerId;
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
                    basicProjectileComponent.projectileOwnerId = __instance.GetOwnerClientId();
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
