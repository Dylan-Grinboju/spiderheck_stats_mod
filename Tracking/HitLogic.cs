using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;


namespace StatsMod
{
    public static class HitLogic
    {
        private static float cleanupInterval = 10f;
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

        private static readonly string[] namesOfTargetsThatCanDie = new string[]
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

        private static bool IsFirstTimeKill(GameObject target, int instanceId, HashSet<int> recentlyKilledSet, ref float lastCleanupTime, object lockObject, string entityType)
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
                Logger.LogInfo($"Recording kill for {entityType} {instanceId}, name:{target.name} at time {Time.time}");
                return true;
            }
        }

        public static void TryRecordKill(GameObject target, PlayerInput player, string weaponName)
        {
            if (target == null) return;

            EnemyBrain enemyBrain = target.GetComponent<EnemyBrain>();
            if (enemyBrain == null)
            {
                enemyBrain = target.GetComponentInParent<EnemyBrain>();
            }

            if (enemyBrain != null)
            {
                Logger.LogDebug($"Target {target.name} is an enemy of type {enemyBrain.name}");
                RollerBrain rollerBrain = enemyBrain.GetComponentInParent<RollerBrain>();
                if (rollerBrain != null)
                {
                    Logger.LogDebug($"Target {target.name} is part of RollerBrain {rollerBrain.gameObject.name}, checking strut death handling");
                    if (!WillRollerStrutKillCauseRollerBrainDeath(rollerBrain))
                        return;
                }

                int enemyId = enemyBrain.gameObject.GetInstanceID();

                if (IsFirstTimeKill(target, enemyId, recentlyKilledEnemies, ref lastEnemiesCleanupTime, recentlyKilledLock, "enemy"))
                {
                    Logger.LogInfo($"Recording kill for player {player.name}, target:{target.name}");
                    StatsManager.Instance.IncrementPlayerKill(player, weaponName);
                }
                return;
            }

            SpiderHealthSystem spiderHealth = target.GetComponent<SpiderHealthSystem>();
            if (spiderHealth == null)
            {
                spiderHealth = target.GetComponentInParent<SpiderHealthSystem>();
            }

            if (spiderHealth == null)
            {
                Logger.LogError($"Could not find SpiderHealthSystem or EnemyBrain for target {target.name}");
                return;
            }

            //Don't count self kills
            if (spiderHealth.transform.root == player.transform.root)
                return;
            int playerId = spiderHealth.gameObject.GetInstanceID();
            if (IsFirstTimeKill(target, playerId, recentlyKilledPlayers, ref lastPlayersCleanupTime, recentlyKilledPlayersLock, "player"))
            {
                Logger.LogInfo($"Recording friendly kill for player {playerId}, name:{target.name}");
                StatsManager.Instance.IncrementFriendlyKill(player, weaponName);
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
                Logger.LogInfo($"Registered disc owner for disc {discId}, owner: {owner.name}");
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
                if (discOwnerTracker.Remove(discId))
                {
                    Logger.LogInfo($"Cleaned up disc owner for disc {discId}");
                }
            }
        }

        public static PlayerInput FindPlayerInputByPlayerId(ulong playerId)
        {
            return PlayerTracker.FindPlayerInputByPlayerId(playerId);
        }

        public static bool IsTargetImmune(GameObject target)
        {
            if (target == null) return false;

            EnemyHealthSystem enemyHealthSystem = target.GetComponent<EnemyHealthSystem>();
            if (enemyHealthSystem == null)
            {
                enemyHealthSystem = target.GetComponentInParent<EnemyHealthSystem>();
            }

            if (enemyHealthSystem != null)
            {
                var immuneTimeField = AccessTools.Field(typeof(EnemyHealthSystem), "_immuneTime");
                if (immuneTimeField != null)
                {
                    float immuneTime = (float)immuneTimeField.GetValue(enemyHealthSystem);
                    if (Time.time < immuneTime)
                    {
                        return true;
                    }
                }
            }

            SpiderHealthSystem spiderHealthSystem = target.GetComponent<SpiderHealthSystem>();
            if (spiderHealthSystem == null)
            {
                spiderHealthSystem = target.GetComponentInParent<SpiderHealthSystem>();
            }

            if (spiderHealthSystem != null)
            {
                var immuneTimeField = AccessTools.Field(typeof(SpiderHealthSystem), "_immuneTime");
                if (immuneTimeField != null)
                {
                    float immuneTime = (float)immuneTimeField.GetValue(spiderHealthSystem);
                    if (Time.time < immuneTime)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasActiveShield(GameObject target)
        {
            if (target == null) return false;

            EnemyHealthSystem enemyHealthSystem = target.GetComponent<EnemyHealthSystem>();
            if (enemyHealthSystem == null)
            {
                enemyHealthSystem = target.GetComponentInParent<EnemyHealthSystem>();
            }

            if (enemyHealthSystem != null)
            {
                return enemyHealthSystem.shield != null && enemyHealthSystem.shield.activeInHierarchy;
            }

            SpiderHealthSystem spiderHealthSystem = target.GetComponent<SpiderHealthSystem>();
            if (spiderHealthSystem == null)
            {
                spiderHealthSystem = target.GetComponentInParent<SpiderHealthSystem>();
            }

            if (spiderHealthSystem != null)
            {
                return spiderHealthSystem.HasShield();
            }

            return false;
        }

        public static void RecordShieldHit(GameObject target, PlayerInput playerInput, string weaponName)
        {
            if (target == null || playerInput == null) return;
            SpiderHealthSystem spiderHealth = target.GetComponent<SpiderHealthSystem>();
            if (spiderHealth == null)
            {
                spiderHealth = target.GetComponentInParent<SpiderHealthSystem>();
            }

            if (spiderHealth != null && spiderHealth.rootObject != null)
            {
                PlayerInput victimPlayerInput = spiderHealth.rootObject.GetComponentInParent<PlayerInput>();
                if (victimPlayerInput != null && victimPlayerInput != playerInput)
                {
                    Logger.LogDebug($"Recording shield hit on player {victimPlayerInput.name} by player {playerInput.name}");
                    StatsManager.Instance.IncrementFriendlyShieldsHit(playerInput, weaponName);
                }
            }
            else
            {
                RollerStrut strutComponent = target.GetComponent<RollerStrut>();
                if (strutComponent == null)
                {
                    strutComponent = target.GetComponentInParent<RollerStrut>();
                }

                if (strutComponent == null)
                {
                    Logger.LogDebug($"Recording shield hit on enemy {target.name} by player {playerInput.name}");
                    StatsManager.Instance.IncrementEnemyShieldsTakenDown(playerInput, weaponName);
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
            Logger.LogDebug($"Recording hit on target {target.name} by player {playerInput.name}");

            if (IsTargetImmune(target))
            {
                Logger.LogDebug($"Target {target.name} is currently immune, not recording hit");
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
