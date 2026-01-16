using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;
using System.Collections.Generic;


namespace StatsMod
{
    public static class HitLogic
    {
        private static readonly float cleanupInterval = 10f;
        private static HashSet<int> recentlyKilledEnemies = new HashSet<int>();
        private static float lastEnemiesCleanupTime = 0f;
        private static readonly object recentlyKilledLock = new object();
        private static HashSet<int> recentlyKilledPlayers = new HashSet<int>();
        private static float lastPlayersCleanupTime = 0f;
        private static readonly object recentlyKilledPlayersLock = new object();

        private static Dictionary<int, int> rollerBrainHealthTracker = new Dictionary<int, int>();
        private static readonly object rollerBrainLock = new object();

        private static Dictionary<int, PlayerInput> discOwnerTracker = new Dictionary<int, PlayerInput>();
        private static readonly object discOwnerLock = new object();

        private static readonly HashSet<string> namesOfTargetsThatCanDie = new HashSet<string>
        {
            "Wasp(Clone)",
            "Wasp Shielded(Clone)",
            "PowerWasp Variant(Clone)",
            "PowerWasp Variant Shield(Clone)",
            "Strut1",
            "Strut2",
            "Strut3",
            "Roller(Clone)",
            "Whisp(Clone)",
            "PowerWhisp Variant(Clone)",
            "MeleeWhisp(Clone)",
            "PowerMeleeWhisp Variant(Clone)",
            "Khepri (Clone)",
            "PowerKhepri Variant(Clone)",
            "Hornet_Shaman Variant(Clone)",
            "Shielded Hornet_Shaman Variant(Clone)",
            "Hornet Variant(Clone)",
            "Shielded Hornet Variant(Clone)",
            "Player(Clone)",
            // "Wasp Friendly(Clone)", don't count either as friendly or enemy
        };

        // Maps game enemy names to display names for tracking
        private static readonly Dictionary<string, string> enemyDisplayNames = new Dictionary<string, string>
        {
            { "Wasp(Clone)", "Wasp" },
            { "Wasp Shielded(Clone)", "Wasp" },
            { "PowerWasp Variant(Clone)", "Power Wasp" },
            { "PowerWasp Variant Shield(Clone)", "Power Wasp" },
            { "Strut1", "Roller" },
            { "Strut2", "Roller" },
            { "Strut3", "Roller" },
            { "Roller(Clone)", "Roller" },
            { "PowerRoller Variant(Clone)", "Roller" },
            { "Whisp(Clone)", "Whisp" },
            { "PowerWhisp Variant(Clone)", "Power Whisp" },
            { "MeleeWhisp(Clone)", "Melee Whisp" },
            { "PowerMeleeWhisp Variant(Clone)", "Power Melee Whisp" },
            { "Khepri (Clone)", "Khepri" },
            { "PowerKhepri Variant(Clone)", "Power Khepri" },
            { "Hornet_Shaman Variant(Clone)", "Hornet Shaman" },
            { "Shielded Hornet_Shaman Variant(Clone)", "Hornet Shaman" },
            { "Hornet Variant(Clone)", "Hornet" },
            { "Shielded Hornet Variant(Clone)", "Hornet" },
            { "Player(Clone)", "Player" },
        };

        private static bool IsFirstTimeDeath(GameObject target, int instanceId, HashSet<int> recentlyKilledSet, ref float lastCleanupTime, object lockObject, string entityType)
        {
            lock (lockObject)
            {
                float currentTime = Time.time;
                if (currentTime - lastCleanupTime > cleanupInterval)
                {
                    recentlyKilledSet.Clear();
                }
                lastCleanupTime = currentTime;

                if (recentlyKilledSet.Contains(instanceId))
                {
                    return false;
                }

                recentlyKilledSet.Add(instanceId);
                Logger.LogInfo($"Recording death for {entityType} {instanceId}, name:{target.name} at time {Time.time}");
                return true;
            }
        }

        public static void TryRecordKill(GameObject target, PlayerInput player, string weaponName)
        {
            if (target == null) return;

            EnemyBrain enemyBrain = target.GetComponentOrParent<EnemyBrain>();

            if (enemyBrain != null)
            {
                RollerBrain rollerBrain = enemyBrain.GetComponentInParent<RollerBrain>();
                if (rollerBrain != null && !WillRollerStrutKillCauseRollerBrainDeath(rollerBrain))
                {
                    return;
                }

                int enemyId = enemyBrain.gameObject.GetInstanceID();

                if (IsFirstTimeDeath(target, enemyId, recentlyKilledEnemies, ref lastEnemiesCleanupTime, recentlyKilledLock, "enemy"))
                {
                    Logger.LogInfo($"Recording kill for player {player.name}, target:{target.name}");
                    PlayerTracker.Instance.IncrementPlayerKill(player);
                    PlayerTracker.Instance.IncrementWeaponHit(player, weaponName);

                    // Track kill by enemy type
                    string targetName = enemyBrain.gameObject.name;
                    if (enemyDisplayNames.TryGetValue(targetName, out string displayName))
                    {
                        PlayerTracker.Instance.IncrementEnemyKillByName(player, displayName);
                    }
                }
                return;
            }

            SpiderHealthSystem spiderHealth = target.GetComponentOrParent<SpiderHealthSystem>();

            if (spiderHealth == null)
            {
                Logger.LogError($"Could not find SpiderHealthSystem or EnemyBrain for target {target.name}");
                return;
            }

            //Don't count self kills
            if (spiderHealth.transform.root == player.transform.root)
                return;
            int playerId = spiderHealth.gameObject.GetInstanceID();
            if (IsFirstTimeDeath(target, playerId, recentlyKilledPlayers, ref lastPlayersCleanupTime, recentlyKilledPlayersLock, "player"))
            {
                Logger.LogInfo($"Recording friendly kill for player {playerId}, name:{target.name}");
                PlayerTracker.Instance.IncrementFriendlyKill(player);
                PlayerTracker.Instance.IncrementWeaponHit(player, weaponName);
                PlayerTracker.Instance.IncrementEnemyKillByName(player, "Player");
            }
        }

        public static bool WillRollerStrutKillCauseRollerBrainDeath(RollerBrain rollerBrain)
        {
            int rollerBrainId = rollerBrain.gameObject.GetInstanceID();

            lock (rollerBrainLock)
            {
                if (!rollerBrainHealthTracker.ContainsKey(rollerBrainId))
                {
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
                    rollerBrainHealthTracker[rollerBrainId] = currentAliveStrutCount - 1;
                }
                else
                {
                    rollerBrainHealthTracker[rollerBrainId]--;
                }

                bool willCauseMainDeath = rollerBrainHealthTracker[rollerBrainId] < rollerBrain.minStrutCount;
                if (willCauseMainDeath)
                {
                    rollerBrainHealthTracker.Remove(rollerBrainId);
                }

                return willCauseMainDeath;
            }
        }

        public static void RegisterDiscOwner(GameObject discProjectile, PlayerInput owner)
        {
            if (discProjectile == null || owner == null) return;

            int discId = discProjectile.GetInstanceID();
            lock (discOwnerLock)
            {
                discOwnerTracker[discId] = owner;
            }
        }

        public static PlayerInput GetDiscOwner(GameObject discProjectile)
        {
            if (discProjectile == null) return null;

            int discId = discProjectile.GetInstanceID();
            lock (discOwnerLock)
            {
                if (discOwnerTracker.TryGetValue(discId, out PlayerInput owner))
                {
                    return owner;
                }
            }
            return null;
        }

        public static void CleanupDiscOwner(GameObject discProjectile)
        {
            if (discProjectile == null) return;

            int discId = discProjectile.GetInstanceID();
            lock (discOwnerLock)
            {
                discOwnerTracker.Remove(discId);
            }
        }

        public static PlayerInput FindPlayerInputByPlayerId(ulong playerId)
        {
            return PlayerTracker.FindPlayerInputByPlayerId(playerId);
        }

        public static bool IsTargetImmune(GameObject target)
        {
            if (target == null) return false;

            EnemyHealthSystem enemyHealthSystem = target.GetComponentOrParent<EnemyHealthSystem>();

            if (enemyHealthSystem != null && ReflectionCache.EnemyImmuneTimeField != null)
            {
                float immuneTime = (float)ReflectionCache.EnemyImmuneTimeField.GetValue(enemyHealthSystem);
                if (Time.time < immuneTime)
                {
                    return true;
                }
            }

            SpiderHealthSystem spiderHealthSystem = target.GetComponentOrParent<SpiderHealthSystem>();

            if (spiderHealthSystem != null && ReflectionCache.SpiderImmuneTimeField != null)
            {
                float immuneTime = (float)ReflectionCache.SpiderImmuneTimeField.GetValue(spiderHealthSystem);
                if (Time.time < immuneTime)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasActiveShield(GameObject target)
        {
            if (target == null) return false;

            EnemyHealthSystem enemyHealthSystem = target.GetComponentOrParent<EnemyHealthSystem>();

            if (enemyHealthSystem != null)
            {
                return enemyHealthSystem.shield != null && enemyHealthSystem.shield.activeInHierarchy;
            }

            SpiderHealthSystem spiderHealthSystem = target.GetComponentOrParent<SpiderHealthSystem>();

            if (spiderHealthSystem != null)
            {
                return spiderHealthSystem.HasShield();
            }

            return false;
        }

        public static void RecordShieldHit(GameObject target, PlayerInput playerInput, string weaponName)
        {
            if (target == null || playerInput == null) return;
            SpiderHealthSystem spiderHealth = target.GetComponentOrParent<SpiderHealthSystem>();

            if (spiderHealth != null && spiderHealth.rootObject != null)
            {
                PlayerInput victimPlayerInput = spiderHealth.rootObject.GetComponentInParent<PlayerInput>();
                if (victimPlayerInput != null && victimPlayerInput != playerInput)
                {
                    Logger.LogDebug($"Recording shield hit on player {victimPlayerInput.name} by player {playerInput.name}");
                    PlayerTracker.Instance.IncrementFriendlyShieldsHit(playerInput);
                    PlayerTracker.Instance.IncrementWeaponHit(playerInput, weaponName);
                }
            }
            else
            {
                RollerStrut strutComponent = target.GetComponentOrParent<RollerStrut>();

                if (strutComponent == null)
                {
                    Logger.LogDebug($"Recording shield hit on enemy {target.name} by player {playerInput.name}");
                    PlayerTracker.Instance.IncrementEnemyShieldsTakenDown(playerInput);
                    PlayerTracker.Instance.IncrementWeaponHit(playerInput, weaponName);
                }
            }
        }

        public static void RecordHit(GameObject target, PlayerInput playerInput, string weaponName)
        {
            if (target == null || playerInput == null) return;
            string targetName = target.GetComponentInParent<SpiderHealthSystem>() != null
                ? target.transform.root.name
                : target.gameObject.name;
            if (!namesOfTargetsThatCanDie.Contains(targetName))
            {
                return;
            }

            if (IsTargetImmune(target))
            {
                return;
            }

            if (HasActiveShield(target))
            {
                RecordShieldHit(target, playerInput, weaponName);
                return;
            }
            TryRecordKill(target, playerInput, weaponName);
        }
    }
}
