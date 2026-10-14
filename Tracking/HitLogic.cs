using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;
using System.Collections.Generic;


namespace StatsMod
{
    // Weapon patches only record which player last hit a target. Kills, friendly kills and shield breaks are
    // credited when the game actually applies them, so hits that don't kill (immunity, shields, roller struts
    // with health left) can't count early, and deaths the game decides later (struts, fuses, lava) still count.
    public static class HitLogic
    {
        // The ExplodingRoller blows up 2s after losing its struts, and a knocked back target takes a moment to reach lava
        private const float KillCreditWindow = 2.5f;
        private const float DirectPlayerKillCreditWindow = 0.5f;
        private const float ShieldCreditWindow = 0.1f;
        private const float StaleHitAge = 30f;
        // A spider standing in control this long after a hit wasn't knocked anywhere
        private const float FootingRegainedTime = 0.5f;
        // After touching lava, a shield break and its 0.25s of immunity can still delay the death
        private const float DeathZoneGrace = 0.5f;

        private struct HitRecord
        {
            public PlayerInput Player;
            public string Weapon;
            public float Time;
            public float FootingSince;
            public bool InDeathZone;
            public float DeathZoneDeadline;
        }

        public struct AttackScope
        {
            internal bool Active;
            internal PlayerInput Player;
            internal string Weapon;
        }

        private static readonly Dictionary<int, HitRecord> lastEnemyHits = new Dictionary<int, HitRecord>();
        private static readonly Dictionary<int, HitRecord> lastPlayerHits = new Dictionary<int, HitRecord>();
        private static readonly Dictionary<int, HitRecord> explosionOwners = new Dictionary<int, HitRecord>();
        private static readonly Dictionary<int, HitRecord> projectileOwners = new Dictionary<int, HitRecord>();
        private static float lastPruneTime = 0f;

        private static PlayerInput attackPlayer;
        private static string attackWeapon;

        private static readonly AccessTools.FieldRef<SpiderHealthSystem, bool> SpiderDying =
            AccessTools.FieldRefAccess<SpiderHealthSystem, bool>("_dying");

        private static Dictionary<int, PlayerInput> discOwnerTracker = new Dictionary<int, PlayerInput>();
        private static readonly object discOwnerLock = new object();

        private static readonly Dictionary<SerializationEnemyName, string> enemyDisplayNames = new Dictionary<SerializationEnemyName, string>
        {
            { SerializationEnemyName.Wasp, "Wasp" },
            { SerializationEnemyName.ShieldedWasp, "Wasp" },
            { SerializationEnemyName.PowerWasp, "Power Wasp" },
            { SerializationEnemyName.PowerWaspShield, "Power Wasp" },
            { SerializationEnemyName.Roller, "Roller" },
            { SerializationEnemyName.PowerRoller, "Roller" },
            { SerializationEnemyName.ExplodingRoller, "Roller" },
            { SerializationEnemyName.Whisp, "Whisp" },
            { SerializationEnemyName.PowerWhisp, "Power Whisp" },
            { SerializationEnemyName.MeleeWhisp, "Melee Whisp" },
            { SerializationEnemyName.PowerMeleeWhisp, "Power Melee Whisp" },
            { SerializationEnemyName.Khepri, "Khepri" },
            { SerializationEnemyName.PowerKhepri, "Power Khepri" },
            { SerializationEnemyName.HornetShaman, "Hornet Shaman" },
            { SerializationEnemyName.HornetVariant, "Hornet" },
            { SerializationEnemyName.ShieldedHornet, "Hornet" },
        };

        // While a player's attack runs (a shot, an explosion, a blade hit, a pulse), every enemy or spider the game damages
        // or knocks back counts as hit by that player, since a knockback can push the target into lava.
        public static AttackScope BeginAttack(PlayerInput player, string weaponName)
        {
            AttackScope previous = new AttackScope { Active = true, Player = attackPlayer, Weapon = attackWeapon };
            attackPlayer = player;
            attackWeapon = weaponName;
            return previous;
        }

        public static void EndAttack(AttackScope previous)
        {
            if (!previous.Active) return;
            attackPlayer = previous.Player;
            attackWeapon = previous.Weapon;
        }

        public static PlayerInput CurrentAttacker => attackPlayer;

        public static void OnEnemyHitByAttack(EnemyHealthSystem enemy)
        {
            if (attackPlayer == null) return;
            RecordEnemyHit(enemy, attackPlayer, attackWeapon);
        }

        // A hit by another player is recorded. Anything else that damages or stuns the spider (an enemy, a hazard, an
        // explosion nobody owns, its own attack) replaces that record, so the earlier player isn't credited for the death.
        public static void OnPlayerHit(SpiderHealthSystem spider, bool replacesEarlierHit)
        {
            if (attackPlayer == null)
            {
                if (replacesEarlierHit) lastPlayerHits.Remove(spider.GetInstanceID());
                return;
            }

            PlayerInput victim = spider.rootObject != null ? spider.rootObject.GetComponentInParent<PlayerInput>() : null;
            if (victim == null) return;

            if (victim != attackPlayer)
            {
                StoreHit(lastPlayerHits, spider.GetInstanceID(), attackPlayer, attackWeapon);
            }
            else if (replacesEarlierHit)
            {
                lastPlayerHits.Remove(spider.GetInstanceID());
            }
        }

        // A hit spider that stands in control for a moment wasn't knocked anywhere, so a later fall into lava is its own
        public static void UpdatePlayerFooting(SpiderHealthSystem spider, Stabilizer stabilizer)
        {
            if (lastPlayerHits.Count == 0) return;

            int id = spider.GetInstanceID();
            if (!lastPlayerHits.TryGetValue(id, out HitRecord hit) || hit.InDeathZone) return;

            bool inControl = stabilizer != null && stabilizer.grounded && !spider.stunned;
            if (!inControl)
            {
                if (hit.FootingSince >= 0f)
                {
                    hit.FootingSince = -1f;
                    lastPlayerHits[id] = hit;
                }
            }
            else if (hit.FootingSince < 0f)
            {
                hit.FootingSince = Time.time;
                lastPlayerHits[id] = hit;
            }
            else if (Time.time - hit.FootingSince >= FootingRegainedTime)
            {
                lastPlayerHits.Remove(id);
            }
        }

        // A spider pushed into lava counts from when it touches it, since the insulation modifier delays the death
        public static void OnPlayerTouchedDeathZone(SpiderHealthSystem spider, float disintegrateDelay)
        {
            int id = spider.GetInstanceID();
            if (!lastPlayerHits.TryGetValue(id, out HitRecord hit) || hit.InDeathZone) return;
            if (Time.time - hit.Time > KillCreditWindow) return;

            hit.InDeathZone = true;
            hit.DeathZoneDeadline = Time.time + disintegrateDelay + DeathZoneGrace;
            lastPlayerHits[id] = hit;
        }

        // The game stores an explosion's owner as a client id that is 0 both for player 0 and for explosions nobody owns
        // (enemy explosions, weapons no player held), so the real owner is kept here, set where the game sets its own.
        public static void SetExplosionOwner(Explosion explosion, ulong ownerId, NetworkBehaviour source)
        {
            explosion.explosionOwnerId = ownerId;
            try
            {
                PlayerInput player = source is BasicProjectile projectile
                    ? GetOwner(projectileOwners, projectile.GetInstanceID())
                    : WeaponPlayer(source as Weapon);
                if (player != null)
                {
                    StoreHit(explosionOwners, explosion.GetInstanceID(), player, null);
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording explosion owner: {ex.Message}");
            }
        }

        public static void SetProjectileOwner(BasicProjectile projectile, ulong ownerId, NetworkBehaviour launcher)
        {
            projectile.projectileOwnerId = ownerId;
            try
            {
                PlayerInput player = WeaponPlayer(launcher as Weapon);
                if (player != null)
                {
                    StoreHit(projectileOwners, projectile.GetInstanceID(), player, null);
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording projectile owner: {ex.Message}");
            }
        }

        public static PlayerInput GetExplosionOwner(Explosion explosion)
        {
            return GetOwner(explosionOwners, explosion.GetInstanceID());
        }

        public static PlayerInput WeaponPlayer(Weapon weapon)
        {
            if (weapon == null) return null;

            if (weapon.owner != null && weapon.owner.healthSystem != null)
            {
                PlayerInput holder = weapon.owner.healthSystem.GetComponentInParent<PlayerInput>();
                if (holder != null) return holder;
            }

            // Thrown weapons keep the playerID WeaponEquipPatch gave them, weapons only AI spiders held have none
            return weapon.ownerWeaponClientId != ulong.MaxValue ? FindPlayerInputByPlayerId(weapon.ownerWeaponClientId) : null;
        }

        public static void OnEnemyDeath(EnemyHealthSystem enemy)
        {
            if (enemy.serializationEnemyName == SerializationEnemyName.FriendlyWasp) return;

            EnemiesTracker.Instance.IncrementEnemiesKilled();

            if (!TryTakeHit(lastEnemyHits, enemy.GetInstanceID(), KillCreditWindow, out HitRecord hit, out string reason))
            {
                Logger.LogInfo($"Enemy {enemy.name} died with no kill credited ({reason}) at time {Time.time}");
                return;
            }

            Logger.LogInfo($"Recording kill for {PlayerLabel(hit.Player)}, target:{enemy.name}, weapon:{hit.Weapon}, {Time.time - hit.Time:F2}s after the hit");
            PlayerTracker.Instance.IncrementPlayerKill(hit.Player);
            PlayerTracker.Instance.IncrementWeaponHit(hit.Player, hit.Weapon);

            if (enemyDisplayNames.TryGetValue(enemy.serializationEnemyName, out string displayName))
            {
                PlayerTracker.Instance.IncrementEnemyKillByName(hit.Player, displayName);
            }
            else
            {
                Logger.LogWarning($"No kill category for enemy {enemy.name} ({enemy.serializationEnemyName})");
            }
        }

        public static void OnEnemyShieldDestroyed(EnemyHealthSystem enemy)
        {
            if (!TryGetRecentHit(lastEnemyHits, enemy.GetInstanceID(), ShieldCreditWindow, out HitRecord hit)) return;

            Logger.LogDebug($"Recording shield hit on enemy {enemy.name} by {PlayerLabel(hit.Player)}");
            PlayerTracker.Instance.IncrementEnemyShieldsTakenDown(hit.Player);
            PlayerTracker.Instance.IncrementWeaponHit(hit.Player, hit.Weapon);
        }

        public static void OnPlayerShieldBroken(SpiderHealthSystem spider)
        {
            if (!TryGetRecentHit(lastPlayerHits, spider.GetInstanceID(), ShieldCreditWindow, out HitRecord hit)) return;

            Logger.LogDebug($"Recording friendly shield hit by {PlayerLabel(hit.Player)}");
            PlayerTracker.Instance.IncrementFriendlyShieldsHit(hit.Player);
            PlayerTracker.Instance.IncrementWeaponHit(hit.Player, hit.Weapon);
        }

        public static void OnPlayerDeath(SpiderHealthSystem spider)
        {
            int id = spider.GetInstanceID();
            if (!lastPlayerHits.TryGetValue(id, out HitRecord pending)) return;

            // Lava and falling out of the map mark the spider as dying before it dies, a damage kill happens on the hit
            float window = !SpiderDying(spider) ? DirectPlayerKillCreditWindow
                : pending.InDeathZone ? pending.DeathZoneDeadline - pending.Time
                : KillCreditWindow;
            if (!TryTakeHit(lastPlayerHits, id, window, out HitRecord hit, out _)) return;

            Logger.LogInfo($"Recording friendly kill for {PlayerLabel(hit.Player)}, weapon:{hit.Weapon}, {Time.time - hit.Time:F2}s after the hit");
            PlayerTracker.Instance.IncrementFriendlyKill(hit.Player);
            PlayerTracker.Instance.IncrementWeaponHit(hit.Player, hit.Weapon);
            PlayerTracker.Instance.IncrementEnemyKillByName(hit.Player, "Player");
        }

        private static void RecordEnemyHit(EnemyHealthSystem enemy, PlayerInput player, string weaponName)
        {
            if (enemy.serializationEnemyName != SerializationEnemyName.FriendlyWasp)
            {
                StoreHit(lastEnemyHits, enemy.GetInstanceID(), player, weaponName);
            }
        }

        private static void StoreHit(Dictionary<int, HitRecord> hits, int targetId, PlayerInput player, string weaponName)
        {
            float now = Time.time;
            if (now - lastPruneTime > StaleHitAge)
            {
                PruneStaleHits(lastEnemyHits, now);
                PruneStaleHits(lastPlayerHits, now);
                PruneStaleHits(explosionOwners, now);
                PruneStaleHits(projectileOwners, now);
                lastPruneTime = now;
            }

            hits[targetId] = new HitRecord { Player = player, Weapon = weaponName, Time = now, FootingSince = -1f };
        }

        private static void PruneStaleHits(Dictionary<int, HitRecord> hits, float now)
        {
            List<int> stale = null;
            foreach (var entry in hits)
            {
                if (now - entry.Value.Time > StaleHitAge)
                {
                    (stale ??= new List<int>()).Add(entry.Key);
                }
            }
            stale?.ForEach(id => hits.Remove(id));
        }

        private static PlayerInput GetOwner(Dictionary<int, HitRecord> owners, int id)
        {
            return owners.TryGetValue(id, out HitRecord owner) ? owner.Player : null;
        }

        private static bool TryGetRecentHit(Dictionary<int, HitRecord> hits, int targetId, float window, out HitRecord hit)
        {
            return hits.TryGetValue(targetId, out hit) && hit.Player != null && Time.time - hit.Time <= window;
        }

        private static bool TryTakeHit(Dictionary<int, HitRecord> hits, int targetId, float window, out HitRecord hit, out string reason)
        {
            if (!hits.TryGetValue(targetId, out hit))
            {
                reason = "no player hit it";
                return false;
            }

            hits.Remove(targetId);
            if (hit.Player == null)
            {
                reason = "the player who hit it left";
                return false;
            }

            float age = Time.time - hit.Time;
            if (age > window)
            {
                reason = $"last hit by {PlayerLabel(hit.Player)} with {hit.Weapon} was {age:F1}s earlier";
                return false;
            }

            reason = null;
            return true;
        }

        private static string PlayerLabel(PlayerInput player)
        {
            return player != null ? $"Player {player.playerIndex + 1}" : "unknown player";
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
    }
}
