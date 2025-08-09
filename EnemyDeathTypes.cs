using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using System;
using Interfaces;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;


namespace StatsMod
{
    public static class EnemyDeathHelper
    {
        private static HashSet<int> recentlyKilledEnemies = new HashSet<int>();
        private static float lastCleanupTime = 0f;
        private static float cleanupInterval = 10f; // Clean up every 10 seconds
        private static readonly object lockObject = new object(); // Lock for thread safety

        private static Dictionary<int, int> rollerBrainHealthTracker = new Dictionary<int, int>();
        private static readonly object rollerBrainLock = new object(); // Lock for RollerStrut health tracking

        public static bool WillDieToDamage(GameObject enemy)
        {


            // Try to get EnemyHealthSystem from the hit object itself, or from its parent hierarchy
            EnemyHealthSystem enemyHealthSystem = enemy.GetComponent<EnemyHealthSystem>();
            if (enemyHealthSystem == null)
            {
                enemyHealthSystem = enemy.GetComponentInParent<EnemyHealthSystem>();
            }

            if (enemyHealthSystem == null)
            {
                Logger.LogError($"enemyHealthSystem is null");
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

            // Check if this is a RollerStrut - check both the object and its parents
            RollerBrain rollerBrain = enemy.GetComponent<RollerBrain>();
            if (rollerBrain == null)
            {
                rollerBrain = enemy.GetComponentInParent<RollerBrain>();
            }
            if (rollerBrain == null) return true; // Not a RollerBrain, so it will die
            return WillRollerStrutKillCauseRollerBrainDeath(rollerBrain);


        }

        public static bool IsFirstTimeEnemyDies(GameObject enemy, PlayerInput player)
        {
            if (enemy == null || player == null) return false;

            // Find the EnemyHealthSystem component in the enemy or its parents
            EnemyHealthSystem enemyHealthSystem = enemy.GetComponent<EnemyHealthSystem>();
            if (enemyHealthSystem == null)
            {
                enemyHealthSystem = enemy.GetComponentInParent<EnemyHealthSystem>();
            }

            if (enemyHealthSystem == null)
            {
                Logger.LogError($"Could not find EnemyHealthSystem for enemy {enemy.name}");
                return false;
            }

            // Use the EnemyHealthSystem's GameObject ID instead of the individual part's ID
            int enemyId = enemyHealthSystem.gameObject.GetInstanceID();
            float currentTime = Time.time;

            lock (lockObject)
            {
                Logger.LogInfo($"entering lock for enemy {enemyId} at time {Time.time}");
                // Clean up the set periodically
                if (currentTime - lastCleanupTime > cleanupInterval)
                {
                    recentlyKilledEnemies.Clear();
                    lastCleanupTime = currentTime;
                }

                // Check if this enemy was already killed recently
                if (recentlyKilledEnemies.Contains(enemyId))
                {
                    return false; // Already recorded this kill
                }

                // Record this kill
                recentlyKilledEnemies.Add(enemyId);
                Logger.LogInfo($"Recording kill for enemy {enemyId} at time {Time.time}");
                return true;
            }
        }

        public static bool WillRollerStrutKillCauseRollerBrainDeath(RollerBrain rollerBrain)
        {
            // Use the rollerBrain's GameObject ID for tracking
            int rollerBrainId = rollerBrain.gameObject.GetInstanceID();

            lock (rollerBrainLock)
            {
                // Check if we've seen this brain before
                if (!rollerBrainHealthTracker.ContainsKey(rollerBrainId))
                {
                    // First time seeing this brain - get its actual health from the game
                    int currentAliveStrutCount = 0;
                    if (rollerBrain.struts != null)
                    {
                        for (int i = 0; i < rollerBrain.struts.transform.childCount; i++)
                        {
                            if (rollerBrain.struts.transform.GetChild(i).gameObject.activeSelf)
                            {
                                currentAliveStrutCount++;
                            }
                        }
                    }
                    // Initialize with current alive strut count
                    rollerBrainHealthTracker[rollerBrainId] = currentAliveStrutCount - 1;
                    Logger.LogInfo($"First time seeing RollerBrain {rollerBrainId}, initialized with health: {currentAliveStrutCount}");
                }
                else
                {
                    // We've seen this strut before - decrement its tracked health
                    rollerBrainHealthTracker[rollerBrainId]--;
                    Logger.LogInfo($"Decremented brain {rollerBrainId} health to: {rollerBrainHealthTracker[rollerBrainId]}");
                }

                // Check if this will cause the main enemy to die
                bool willCauseMainDeath = rollerBrainHealthTracker[rollerBrainId] < rollerBrain.minStrutCount;

                // Clean up the tracker if the main enemy will die
                if (willCauseMainDeath)
                {
                    rollerBrainHealthTracker.Remove(rollerBrainId);
                    Logger.LogInfo($"Brain {rollerBrainId} died");
                }

                return willCauseMainDeath;
            }
        }

        // private static readonly string[] namesOfEnemiesThatCanDie = new string[]
        // {
        //     "Wasp(Clone)",
        //     "Wasp Shielded(Clone)",
        //     "PowerWasp Variant(Clone)",
        //     // "PowerWasp Shielded Variant(Clone)" ??
        //     "Strut1",
        //     //power strut?
        //     "Whisp(Clone)",
        //     "PowerWhisp Variant(Clone)",
        //     "MeleeWhisp(Clone)",
        //     "PowerMeleeWhisp Variant(Clone)",
        //     "Head", //butterfly
        //     "Hornet_Shaman Variant(Clone)",
        //     //shielded?
        //     "Hornet Variant(Clone)", //darth maul
        //     //shielded?
        // };
        // public static bool WillDieToDamage(GameObject enemy)
        // {
        //     if (enemy == null)
        //     {
        //         Logger.LogError("Enemy is null, cannot check if it will die to damage.");
        //         return false;
        //     }

        //     return namesOfEnemiesThatCanDie.Contains(enemy.name);
        // }
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
                if (other == __instance.ignoreWeapon.gameObject || other == __instance.ignoreWeapon.owner.healthSystem.gameObject)
                {
                    return;
                }

                IDamageable component = other.GetComponent<IDamageable>();
                if (component != null && EnemyDeathHelper.WillDieToDamage(other))
                {
                    // Logger.LogInfo($"will die to shotgun: {other.name}");
                    PlayerInput playerInput = __instance.ignoreWeapon.owner.healthSystem.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other, playerInput))
                        {
                            // Logger.LogInfo($"Recording kill with shotgun for {other.name}");
                            PlayerTracker.Instance.RecordPlayerKill(playerInput);
                        }
                        else
                        {
                            Logger.LogInfo($"Duplicate kill prevented for {other.name}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error logging damage info: {ex.Message}");
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

                // Access IsHost property using reflection since it's protected
                var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
                bool isHost = (bool)isHostProperty.GetValue(__instance);

                if (!isHost)
                {
                    return;
                }
                Logger.LogInfo($"Shot with railgun: {hit.transform.gameObject.name}");

                IDamageable component = hit.transform.GetComponent<IDamageable>();
                if (component != null && EnemyDeathHelper.WillDieToDamage(hit.transform.gameObject))
                {
                    Logger.LogInfo($"will die with railgun: {hit.transform.gameObject.name}");

                    // Find the player through the ignore list
                    PlayerInput ownerPlayer = null;
                    int playerCount = 0;

                    foreach (GameObject ignoreObj in __instance.ignore)
                    {
                        if (ignoreObj != null)
                        {
                            // Check if this object has a SpiderHealthSystem (player)
                            SpiderHealthSystem spiderHealth = ignoreObj.GetComponent<SpiderHealthSystem>();
                            if (spiderHealth == null)
                            {
                                spiderHealth = ignoreObj.GetComponentInParent<SpiderHealthSystem>();
                            }

                            if (spiderHealth != null)
                            {
                                playerCount++;
                                if (ownerPlayer == null)
                                {
                                    ownerPlayer = spiderHealth.GetComponentInParent<PlayerInput>();
                                }
                            }
                        }
                    }

                    if (playerCount > 1)
                    {
                        Logger.LogError($"RailShot: Found {playerCount} players in ignore list, expected only 1");
                    }

                    if (ownerPlayer != null)
                    {
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.transform.gameObject, ownerPlayer))
                        {
                            Logger.LogInfo($"railgun recorder kill: {hit.transform.gameObject.name}");

                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                    else
                    {
                        Logger.LogError($"RailShot: Could not find owner player in ignore list");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking RailShot damage: {ex.Message}");
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
                // Access IsHost property using reflection since it's protected
                var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
                bool isHost = (bool)isHostProperty.GetValue(__instance);

                if (!isHost)
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
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.transform.gameObject, ownerPlayer))
                            {
                                PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                            }
                        }
                        Logger.LogInfo($"ownerPlayer is null in DeathCube: {ownerPlayer == null}");
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
                // Access IsHost property using reflection since it's protected
                var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
                bool isHost = (bool)isHostProperty.GetValue(__instance);

                if (!isHost)
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
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.transform.gameObject, ownerPlayer))
                            {
                                PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                            }
                        }
                        Logger.LogInfo($"ownerPlayer is null in DeathRay: {ownerPlayer == null}");

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
                // Access IsHost property using reflection since it's protected
                var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
                bool isHost = (bool)isHostProperty.GetValue(__instance);

                if (!isHost)
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
                    PlayerInput ownerPlayer = __instance.owner.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other.gameObject, ownerPlayer))
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                    Logger.LogInfo($"ownerPlayer is null in EnergyBall: {ownerPlayer == null}");

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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other.gameObject, ownerPlayer))
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                    Logger.LogInfo($"ownerPlayer is null in ForceField: {ownerPlayer == null}");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error tracking ForceField damage: {ex.Message}");
            }
        }
    }

    // KhepriStaff
    //not the energy ball itself, maybe the staff has damage when using it as melee practically
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(target, ownerPlayer))
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                    Logger.LogInfo($"ownerPlayer is null in KhepriStaff: {ownerPlayer == null}");
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
                    PlayerInput ownerPlayer = __instance.owner.GetComponentInParent<PlayerInput>();
                    if (ownerPlayer != null)
                    {
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.collider.gameObject, ownerPlayer))
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                    Logger.LogInfo($"ownerPlayer is null in LaserCannon: {ownerPlayer == null}");
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
                // Access IsHost property using reflection since it's protected
                var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
                bool isHost = (bool)isHostProperty.GetValue(__instance);

                if (!isHost)
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
                        PlayerInput ownerPlayer = __instance.owner.GetComponentInParent<PlayerInput>();
                        if (ownerPlayer != null)
                        {
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.collider.gameObject, ownerPlayer))
                            {
                                PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                            }
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other.gameObject, ownerPlayer))
                        {
                            PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                        }
                    }
                    Logger.LogInfo($"ownerPlayer is null in SawDisc: {ownerPlayer == null}");
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
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(collider2D.gameObject, ownerPlayer))
                            {
                                PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                            }
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
            Logger.LogError($"Explosion1");

            try
            {
                // Access IsHost property using reflection since it's protected
                var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
                bool isHost = (bool)isHostProperty.GetValue(__instance);

                if (!isHost)
                {
                    return;
                }
                Logger.LogError($"Explosion2");

                var knockBackRadiusField = AccessTools.Field(typeof(Explosion), "knockBackRadius");
                var layersField = AccessTools.Field(typeof(Explosion), "layers");
                var deathRadiusField = AccessTools.Field(typeof(Explosion), "deathRadius");
                var playerDeathRadiusField = AccessTools.Field(typeof(Explosion), "_playerDeathRadius");
                var isBoomSpearField = AccessTools.Field(typeof(Explosion), "isBoomSpear");
                var explosionOwnerIdField = AccessTools.Field(typeof(Explosion), "explosionOwnerId");

                if (knockBackRadiusField == null || layersField == null || deathRadiusField == null ||
                    playerDeathRadiusField == null || isBoomSpearField == null || explosionOwnerIdField == null)
                {
                    Logger.LogWarning("Could not access Explosion fields for damage tracking");
                    return;
                }

                float knockBackRadius = (float)knockBackRadiusField.GetValue(__instance);
                LayerMask layers = (LayerMask)layersField.GetValue(__instance);
                float deathRadius = (float)deathRadiusField.GetValue(__instance);
                float playerDeathRadius = (float)playerDeathRadiusField.GetValue(__instance);
                bool isBoomSpear = (bool)isBoomSpearField.GetValue(__instance);
                ulong explosionOwnerId = (ulong)explosionOwnerIdField.GetValue(__instance);

                foreach (Collider2D collider2D in Physics2D.OverlapCircleAll(__instance.transform.position, knockBackRadius, layers))
                {
                    IDamageable componentInParent = collider2D.GetComponentInParent<IDamageable>();
                    if (componentInParent == null)
                    {
                        continue;
                    }

                    bool flag = (componentInParent is IceBlock);
                    Vector3 position = __instance.transform.position;
                    Vector2 vector = collider2D.ClosestPoint(position);
                    float num = Vector2.Distance(position, vector);

                    // Check conditions that lead to Damage() call
                    bool willCallDamage = false;

                    if (num > deathRadius && !flag)
                    {
                        // Will call Impact, not Damage
                        continue;
                    }
                    else if (collider2D.CompareTag("PlayerRigidbody") && num > playerDeathRadius)
                    {
                        // Will call Impact, not Damage
                        continue;
                    }
                    else
                    {
                        // This is the else block where Damage() is called
                        if (isBoomSpear)
                        {
                            // Check if this is the boom spear owner (will call Impact instead of Damage)
                            PlayerController playerController;
                            if (collider2D.CompareTag("PlayerRigidbody") &&
                                collider2D.transform.parent.parent.TryGetComponent<PlayerController>(out playerController) &&
                                playerController != null &&
                                (ulong)playerController.playerID.Value == explosionOwnerId)
                            {
                                // Will call Impact, not Damage
                                continue;
                            }
                        }
                        Logger.LogError($"Explosion3");

                        // If we reach here, Damage() will be called
                        willCallDamage = true;
                    }

                    if (willCallDamage && EnemyDeathHelper.WillDieToDamage(collider2D.gameObject))
                    {
                        Logger.LogError($"Explosion4");

                        // Find the player who owns this explosion
                        var allPlayers = UnityEngine.Object.FindObjectsOfType<PlayerInput>();
                        foreach (var player in allPlayers)
                        {
                            var playerController = player.GetComponent<PlayerController>();
                            if (playerController != null && (ulong)playerController.playerID.Value == explosionOwnerId)
                            {
                                PlayerTracker.Instance.RecordPlayerKill(player);
                                break;
                            }
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
