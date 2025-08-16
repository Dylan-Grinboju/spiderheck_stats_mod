using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using HarmonyLib;
using System;
using Interfaces;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;


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

        // public static bool WillDieToDamage(GameObject enemy)
        // {
        //     Logger.LogInfo($"Checking if enemy will die to damage: {enemy.name}");
        //     // Try to get EnemyHealthSystem from the hit object itself, or from its parent hierarchy
        //     EnemyHealthSystem enemyHealthSystem = enemy.GetComponent<EnemyHealthSystem>();
        //     if (enemyHealthSystem == null)
        //     {
        //         enemyHealthSystem = enemy.GetComponentInParent<EnemyHealthSystem>();
        //     }
        //     Logger.LogInfo($"enemyHealthSystem: {enemyHealthSystem}");
        //     enemyHealthSystem = enemy.GetComponentInParent<EnemyHealthSystem>();
        //     Logger.LogInfo($"enemyHealthSystem2: {enemyHealthSystem}");

        //     if (enemyHealthSystem == null)
        //     {
        //         Logger.LogError($"enemyHealthSystem is null");
        //         return false;
        //     }

        //     // Same checks as in EnemyHealthSystem.Damage method
        //     // Access IsHost property using reflection since it's protected
        //     var isHostProperty = AccessTools.Property(typeof(Unity.Netcode.NetworkBehaviour), "IsHost");
        //     bool isHost = (bool)isHostProperty.GetValue(enemyHealthSystem);

        //     if (!isHost)
        //     {
        //         return false;
        //     }
        //     if (enemyHealthSystem.ignoreDirectDamage)
        //     {
        //         return false;
        //     }

        //     // Access private _immuneTime field using reflection
        //     var immuneTimeField = AccessTools.Field(typeof(EnemyHealthSystem), "_immuneTime");
        //     float immuneTime = (float)immuneTimeField.GetValue(enemyHealthSystem);

        //     if (Time.time < immuneTime)
        //     {
        //         return false;
        //     }

        //     // If enemy has an active shield, it won't die - just lose the shield
        //     if (enemyHealthSystem.shield && enemyHealthSystem.shield.activeInHierarchy)
        //     {
        //         return false;
        //     }

        //     // Check if this is a RollerStrut - check both the object and its parents
        //     RollerBrain rollerBrain = enemy.GetComponent<RollerBrain>();
        //     if (rollerBrain == null)
        //     {
        //         rollerBrain = enemy.GetComponentInParent<RollerBrain>();
        //     }
        //     if (rollerBrain == null) return true; // Not a RollerBrain, so it will die
        //     return WillRollerStrutKillCauseRollerBrainDeath(rollerBrain);


        // }

        public static bool IsFirstTimeEnemyDies(GameObject enemy)
        {
            if (enemy == null) return false;

            // Find the EnemyBrain component in the enemy or its parents
            EnemyBrain enemyBrain = enemy.GetComponent<EnemyBrain>();
            if (enemyBrain == null)
            {
                enemyBrain = enemy.GetComponentInParent<EnemyBrain>();
            }

            if (enemyBrain == null)
            {
                Logger.LogError($"Could not find EnemyHealthSystem for enemy {enemy.name}");
                return false;
            }

            // Use the EnemyBrain's GameObject ID instead of the individual part's ID
            int enemyId = enemyBrain.gameObject.GetInstanceID();
            float currentTime = Time.time;

            lock (lockObject)
            {
                // Logger.LogInfo($"entering lock for enemy {enemyId} at time {Time.time}");
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
                Logger.LogInfo($"Recording kill for enemy {enemyId}, name:{enemy.name} at time {Time.time}");
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

        private static readonly string[] namesOfEnemiesThatCanDie = new string[]
        {
            "Wasp(Clone)",
            "Wasp Shielded(Clone)",
            "PowerWasp Variant(Clone)",
            "PowerWasp Variant Shield(Clone)" ??
            "Strut1",
            "Strut2",
            "Strut3",
            "Whisp(Clone)",
            "PowerWhisp Variant(Clone)",
            "MeleeWhisp(Clone)",
            "PowerMeleeWhisp Variant(Clone)",
            "Head", //butterfly
            "Hornet_Shaman Variant(Clone)",
            "Shielded Hornet_Shaman Variant(Clone)", //not confirmed
            "Hornet Variant(Clone)", //darth maul
            "Shielded Hornet Variant(Clone)",
        };
        public static bool WillDieToDamage(GameObject enemy)
        {
            if (enemy == null)
            {
                Logger.LogError("Enemy is null, cannot check if it will die to damage.");
                return false;
            }
            // check for roller strut
            RollerBrain rollerBrain = enemy.GetComponent<RollerBrain>();
            if (rollerBrain != null)
            {
                return WillRollerStrutKillCauseRollerBrainDeath(rollerBrain);
            }
            return namesOfEnemiesThatCanDie.Contains(enemy.name);
        }

        public static PlayerInput FindPlayerInputByPlayerId(ulong playerId)
        {
            PlayerController[] playerControllers = UnityEngine.Object.FindObjectsOfType<PlayerController>();

            foreach (PlayerController controller in playerControllers)
            {
                if ((ulong)controller.playerID.Value == playerId)
                {
                    PlayerInput playerInput = controller.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        return playerInput;
                    }
                }
            }

            Logger.LogError($"Could not find PlayerController with playerID.Value: {playerId}");
            return null;
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other))
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.transform.gameObject))
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
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.transform.gameObject))
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
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.transform.gameObject))
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other.gameObject))
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
                    PlayerInput playerInput = __instance.GetComponentInParent<Weapon>().owner.healthSystem.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other.gameObject))
                        {
                            PlayerTracker.Instance.RecordPlayerKill(playerInput);
                        }
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(target))
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.collider.gameObject))
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
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(hit.collider.gameObject))
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
                        if (EnemyDeathHelper.IsFirstTimeEnemyDies(other.gameObject))
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
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(collider2D.gameObject))
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

            try
            {
                // Check if this is the host using NetworkManager
                if (!NetworkManager.Singleton.IsHost)
                {
                    return;
                }

                var knockBackRadiusField = AccessTools.Field(typeof(Explosion), "knockBackRadius");
                var layersField = AccessTools.Field(typeof(Explosion), "layers");
                var deathRadiusField = AccessTools.Field(typeof(Explosion), "deathRadius");
                var playerDeathRadiusField = AccessTools.Field(typeof(Explosion), "_playerDeathRadius");
                var isBoomSpearField = AccessTools.Field(typeof(Explosion), "isBoomSpear");
                var playerExplosionIDField = AccessTools.Field(typeof(Explosion), "playerExplosionID");
                var explosionOwnerIdField = AccessTools.Field(typeof(Explosion), "explosionOwnerId");

                if (knockBackRadiusField == null || layersField == null || deathRadiusField == null ||
                    playerDeathRadiusField == null || isBoomSpearField == null || playerExplosionIDField == null || explosionOwnerIdField == null)
                {
                    Logger.LogWarning("Could not access Explosion fields for damage tracking");
                    return;
                }

                float knockBackRadius = (float)knockBackRadiusField.GetValue(__instance);
                LayerMask layers = (LayerMask)layersField.GetValue(__instance);
                float deathRadius = (float)deathRadiusField.GetValue(__instance);
                float playerDeathRadius = (float)playerDeathRadiusField.GetValue(__instance);
                bool isBoomSpear = (bool)isBoomSpearField.GetValue(__instance);
                int playerExplosionId = (int)playerExplosionIDField.GetValue(__instance); //starts at 0
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
                                playerController.playerID.Value == playerExplosionId)
                            {
                                // Will call Impact, not Damage
                                continue;
                            }
                        }
                        // If we reach here, Damage() will be called
                    }

                    if (EnemyDeathHelper.WillDieToDamage(collider2D.gameObject))
                    {
                        // Use the more reliable explosionOwnerId instead of playerExplosionId
                        Logger.LogInfo($"explosionOwnerId: {explosionOwnerId}");
                        PlayerInput ownerPlayer = EnemyDeathHelper.FindPlayerInputByPlayerId(explosionOwnerId);

                        if (ownerPlayer != null)
                        {
                            if (EnemyDeathHelper.IsFirstTimeEnemyDies(collider2D.gameObject))
                            {
                                PlayerTracker.Instance.RecordPlayerKill(ownerPlayer);
                            }
                        }
                        else
                        {
                            Logger.LogError($"Could not find PlayerInput for explosion ownerClientId: {explosionOwnerId}");
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
                        Logger.LogInfo($"Fixed weapon {__instance.serializationWeaponName} owner to: {playerController.playerID.Value}");
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
