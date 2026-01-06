using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Silk;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;

namespace StatsMod
{
    public class PlayerTracker
    {
        private static readonly Lazy<PlayerTracker> _lazy = new Lazy<PlayerTracker>(() => new PlayerTracker());
        public static PlayerTracker Instance => _lazy.Value;

        private Dictionary<PlayerInput, PlayerData> activePlayers = new Dictionary<PlayerInput, PlayerData>();

        private Dictionary<ulong, PlayerInput> playerIds = new Dictionary<ulong, PlayerInput>();

        private ulong nextLocalPlayerId = 0;

        // Cache for expensive player lookups
        private static PlayerController[] cachedPlayerControllers;
        private static float lastPlayerCacheUpdate = 0f;
        private static float playerCacheRefreshInterval = 60f; // Refresh every 60 seconds
        private static readonly object playerCacheLock = new object();

        private bool isPaused = false;

        public class PlayerData
        {
            public ulong PlayerId { get; set; }
            public int Deaths { get; set; }
            public int Kills { get; set; }
            public int KillsWhileAirborne { get; set; }
            public int KillsWhileSolo { get; set; }
            public int FriendlyKills { get; set; }
            public int WaveClutches { get; set; }
            public int EnemyShieldsTakenDown { get; set; }
            public int FriendlyShieldsHit { get; set; }
            public int ShieldsLost { get; set; }
            public int KillStreak { get; set; }
            public int MaxKillStreak { get; set; }
            public int KillStreakWhileSolo { get; set; }
            public int MaxKillStreakWhileSolo { get; set; }
            public string PlayerName { get; set; }
            public DateTime JoinTime { get; set; }
            public Color PlayerColor { get; set; }
            public DateTime? CurrentAliveStartTime { get; set; }
            public TimeSpan TotalAliveTime { get; set; }
            public bool WasAliveWhenPaused { get; set; }
            public int WebSwings { get; set; }
            public TimeSpan WebSwingTime { get; set; }
            public DateTime? CurrentWebSwingStartTime { get; set; }
            public bool WasSwingingWhenPaused { get; set; }
            public TimeSpan AirborneTime { get; set; }
            public DateTime? CurrentAirborneStartTime { get; set; }
            public bool WasAirborneWhenPaused { get; set; }
            public float HighestPoint { get; set; }
            public Color SecondaryColor { get; set; }
            public Dictionary<string, int> WeaponHits { get; set; }

            public PlayerData(ulong id, string name = "Player")
            {
                PlayerId = id;
                Deaths = 0;
                Kills = 0;
                KillsWhileAirborne = 0;
                KillsWhileSolo = 0;
                FriendlyKills = 0;
                WaveClutches = 0;
                EnemyShieldsTakenDown = 0;
                FriendlyShieldsHit = 0;
                ShieldsLost = 0;
                KillStreak = 0;
                MaxKillStreak = 0;
                KillStreakWhileSolo = 0;
                MaxKillStreakWhileSolo = 0;
                PlayerName = name;
                JoinTime = DateTime.Now;
                PlayerColor = Color.white;
                CurrentAliveStartTime = null;
                TotalAliveTime = TimeSpan.Zero;
                WasAliveWhenPaused = false;
                WebSwings = 0;
                WebSwingTime = TimeSpan.Zero;
                CurrentWebSwingStartTime = null;
                WasSwingingWhenPaused = false;
                AirborneTime = TimeSpan.Zero;
                CurrentAirborneStartTime = null;
                WasAirborneWhenPaused = false;
                HighestPoint = 0f;
                SecondaryColor = Color.white;
                WeaponHits = new Dictionary<string, int>
                {
                    { "Shotgun", 0 },
                    { "RailShot", 0 },
                    { "DeathCube", 0 },
                    { "DeathRay", 0 },
                    { "EnergyBall", 0 },
                    { "Particle Blade", 0 },
                    { "KhepriStaff", 0 },
                    { "Laser Cannon", 0 },
                    { "Laser Cube", 0 },
                    { "SawDisc", 0 },
                    { "Explosions", 0 }
                };
            }

            public TimeSpan GetCurrentAliveTime()
            {
                if (CurrentAliveStartTime.HasValue)
                {
                    return TotalAliveTime + (DateTime.Now - CurrentAliveStartTime.Value);
                }
                return TotalAliveTime;
            }

            public TimeSpan GetCurrentWebSwingTime()
            {
                if (CurrentWebSwingStartTime.HasValue)
                {
                    return WebSwingTime + (DateTime.Now - CurrentWebSwingStartTime.Value);
                }
                return WebSwingTime;
            }

            public TimeSpan GetCurrentAirborneTime()
            {
                if (CurrentAirborneStartTime.HasValue)
                {
                    return AirborneTime + (DateTime.Now - CurrentAirborneStartTime.Value);
                }
                return AirborneTime;
            }

            public void StopAliveTimer()
            {
                if (CurrentAliveStartTime.HasValue)
                {
                    TotalAliveTime += DateTime.Now - CurrentAliveStartTime.Value;
                    CurrentAliveStartTime = null;
                }
            }

            public void StopWebSwingTimer()
            {
                if (CurrentWebSwingStartTime.HasValue)
                {
                    WebSwingTime += DateTime.Now - CurrentWebSwingStartTime.Value;
                    CurrentWebSwingStartTime = null;
                }
            }

            public void StopAirborneTimer()
            {
                if (CurrentAirborneStartTime.HasValue)
                {
                    AirborneTime += DateTime.Now - CurrentAirborneStartTime.Value;
                    CurrentAirborneStartTime = null;
                }
            }
        }

        public PlayerTracker()
        {
            Logger.LogInfo("Player tracker initialized");
        }


        public void RegisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            if (activePlayers.ContainsKey(player))
            {
                return;
            }

            ulong playerId = nextLocalPlayerId++;

            string playerName = $"Player {player.playerIndex + 1}";
            PlayerData playerData = new PlayerData(playerId, playerName);

            // Try to get the spider customizer and set the initial colors
            SpiderCustomizer customizer = player.GetComponentInChildren<SpiderCustomizer>();
            if (customizer != null)
            {
                if (ReflectionCache.PrimaryColorField != null)
                {
                    Color primaryColor = (Color)ReflectionCache.PrimaryColorField.GetValue(customizer);
                    playerData.PlayerColor = primaryColor;
                }
                if (ReflectionCache.SecondaryColorField != null)
                {
                    Color secondaryColor = (Color)ReflectionCache.SecondaryColorField.GetValue(customizer);
                    playerData.SecondaryColor = secondaryColor;
                }
            }

            activePlayers[player] = playerData;
            playerIds[playerId] = player;

            RefreshPlayerCache();

            UIManager.Instance?.OnPlayerJoined();
        }


        public void UnregisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            if (activePlayers.TryGetValue(player, out PlayerData playerData))
            {
                playerIds.Remove(playerData.PlayerId);
                activePlayers.Remove(player);

                RefreshPlayerCache();

                UIManager.Instance?.OnPlayerLeft();
            }
        }

        public void RecordPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            if (spiderHealth == null)
            {
                Logger.LogWarning("RecordPlayerDeath called with null spiderHealth");
                return;
            }

            try
            {
                PlayerInput playerInput = null;
                if (spiderHealth.rootObject != null)
                {
                    playerInput = spiderHealth.rootObject.GetComponentInParent<PlayerInput>();
                }
                else
                {
                    Logger.LogWarning("SpiderHealthSystem has null rootObject");
                }

                if (playerInput != null && activePlayers.TryGetValue(playerInput, out PlayerData data))
                {
                    data.Deaths++;
                    data.KillStreak = 0;
                    data.KillStreakWhileSolo = 0;
                    data.StopAliveTimer();
                    StopWebSwingTimer(playerInput);
                }
                else
                {
                    Logger.LogWarning($"Could not find player data for death event");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording player death: {ex.Message}");
            }
        }

        public void UndoPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            if (spiderHealth == null)
            {
                Logger.LogWarning("UndoPlayerDeath called with null spiderHealth");
                return;
            }

            try
            {
                PlayerInput playerInput = null;
                if (spiderHealth.rootObject != null)
                {
                    playerInput = spiderHealth.rootObject.GetComponentInParent<PlayerInput>();
                }

                if (playerInput != null && activePlayers.TryGetValue(playerInput, out PlayerData data))
                {
                    data.Deaths--;
                    StartAliveTimer(playerInput);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error undoing player death: {ex.Message}");
            }
        }

        public void ResetPlayerStats()
        {
            foreach (var entry in activePlayers)
            {
                entry.Value.Deaths = 0;
                entry.Value.Kills = 0;
                entry.Value.KillsWhileAirborne = 0;
                entry.Value.KillsWhileSolo = 0;
                entry.Value.FriendlyKills = 0;
                entry.Value.WaveClutches = 0;
                entry.Value.EnemyShieldsTakenDown = 0;
                entry.Value.FriendlyShieldsHit = 0;
                entry.Value.ShieldsLost = 0;
                entry.Value.WebSwings = 0;
                entry.Value.WebSwingTime = TimeSpan.Zero;
                entry.Value.AirborneTime = TimeSpan.Zero;
                entry.Value.KillStreak = 0;
                entry.Value.MaxKillStreak = 0;
                entry.Value.KillStreakWhileSolo = 0;
                entry.Value.MaxKillStreakWhileSolo = 0;
                entry.Value.TotalAliveTime = TimeSpan.Zero;
                entry.Value.HighestPoint = 0f;
                foreach (var weaponKey in entry.Value.WeaponHits.Keys.ToList())
                {
                    entry.Value.WeaponHits[weaponKey] = 0;
                }
            }
        }

        public Dictionary<PlayerInput, PlayerData> GetActivePlayers()
        {
            return new Dictionary<PlayerInput, PlayerData>(activePlayers);
        }

        public bool IsOnlyOnePlayerAlive()
        {
            int aliveCount = activePlayers.Count(p => p.Value.CurrentAliveStartTime.HasValue);
            return aliveCount == 1;
        }

        public PlayerInput GetOnlyAlivePlayer()
        {
            if (!IsOnlyOnePlayerAlive())
                return null;

            return activePlayers.FirstOrDefault(p => p.Value.CurrentAliveStartTime.HasValue).Key;
        }

        public void IncrementPlayerKill(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.Kills++;
                data.KillStreak++;
                if (data.KillStreak > data.MaxKillStreak)
                {
                    data.MaxKillStreak = data.KillStreak;
                }
                
                SpiderController spider = player.GetComponentInChildren<SpiderController>();
                if (spider != null)
                {
                    Stabilizer stabilizer = spider.GetComponentInChildren<Stabilizer>();
                    if (stabilizer != null && !stabilizer.grounded)
                    {
                        data.KillsWhileAirborne++;
                    }
                }

                if (IsOnlyOnePlayerAlive())
                {
                    data.KillsWhileSolo++;
                    data.KillStreakWhileSolo++;
                    if (data.KillStreakWhileSolo > data.MaxKillStreakWhileSolo)
                    {
                        data.MaxKillStreakWhileSolo = data.KillStreakWhileSolo;
                    }
                }
            }
        }

        public void IncrementFriendlyKill(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.FriendlyKills++;
            }
        }

        public void IncrementWaveClutch(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.WaveClutches++;
            }
        }

        public void IncrementEnemyShieldsTakenDown(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.EnemyShieldsTakenDown++;
            }
        }

        public void IncrementFriendlyShieldsHit(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.FriendlyShieldsHit++;
            }
        }

        public void IncrementShieldsLost(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.ShieldsLost++;
            }
        }

        public void IncrementWeaponHit(PlayerInput player, string weaponName)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                if (data.WeaponHits.ContainsKey(weaponName))
                {
                    data.WeaponHits[weaponName]++;
                }
                else
                {
                    Logger.LogError($"Unknown weapon name: {weaponName}. This weapon is not pre-populated in WeaponHits dictionary.");
                }
            }
        }

        public void IncrementWebSwings(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.WebSwings++;
            }
        }

        public void StartWebSwingTimer(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                if (data.CurrentWebSwingStartTime.HasValue)
                {
                    return;
                }
                data.CurrentWebSwingStartTime = DateTime.Now;
            }
        }

        public void StopWebSwingTimer(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.StopWebSwingTimer();
            }
        }

        public void StartAirborneTimer(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                if (data.CurrentAirborneStartTime.HasValue)
                {
                    return;
                }
                data.CurrentAirborneStartTime = DateTime.Now;
            }
        }

        public void StopAirborneTimer(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.StopAirborneTimer();
            }
        }

        public void UpdatePlayerColor(PlayerInput player, Color color)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.PlayerColor = color;
            }
        }

        public void UpdateHighestPoint(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                SpiderController spider = player.GetComponentInChildren<SpiderController>();
                if (spider != null && spider.bodyRigidbody2D != null)
                {
                    float currentY = spider.bodyRigidbody2D.position.y;
                    if (currentY > data.HighestPoint)
                    {
                        data.HighestPoint = currentY;
                    }
                }
            }
        }

        public static PlayerController[] GetCachedPlayerControllers()
        {
            lock (playerCacheLock)
            {
                float currentTime = Time.time;
                if (cachedPlayerControllers == null || currentTime - lastPlayerCacheUpdate > playerCacheRefreshInterval)
                {
                    cachedPlayerControllers = UnityEngine.Object.FindObjectsOfType<PlayerController>();
                    lastPlayerCacheUpdate = currentTime;
                }
                return cachedPlayerControllers;
            }
        }

        public static PlayerInput FindPlayerInputByPlayerId(ulong playerId)
        {
            PlayerController[] playerControllers = GetCachedPlayerControllers();

            foreach (PlayerController controller in playerControllers)
            {
                if (controller != null && (ulong)controller.playerID.Value == playerId)
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


        public static void RefreshPlayerCache()
        {
            lock (playerCacheLock)
            {
                cachedPlayerControllers = null;
                lastPlayerCacheUpdate = 0f;
            }
        }

        public void StartAliveTimer(PlayerInput playerInput)
        {
            if (playerInput != null && activePlayers.TryGetValue(playerInput, out PlayerData data))
            {
                if (data.CurrentAliveStartTime.HasValue)
                {
                    return;
                }
                data.CurrentAliveStartTime = DateTime.Now;

                if (!IsOnlyOnePlayerAlive())
                {
                    foreach (var entry in activePlayers)
                    {
                        entry.Value.KillStreakWhileSolo = 0;
                    }
                }
            }
            else
            {
                Logger.LogError($"Failed to start alive timer - player not found or null");
            }
        }

        public void StartAllAliveTimers()
        {
            foreach (var entry in activePlayers)
            {
                if (!entry.Value.CurrentAliveStartTime.HasValue)
                {
                    entry.Value.CurrentAliveStartTime = DateTime.Now;
                    entry.Value.TotalAliveTime = TimeSpan.Zero;
                }
            }
        }

        public void StopAllAliveTimers()
        {
            foreach (var entry in activePlayers)
            {
                entry.Value.StopAliveTimer();
            }
        }

        public void StopAllWebSwingTimers()
        {
            foreach (var entry in activePlayers)
            {
                entry.Value.StopWebSwingTimer();
            }
        }

        public void StopAllAirborneTimers()
        {
            foreach (var entry in activePlayers)
            {
                entry.Value.StopAirborneTimer();
            }
        }

        public void PauseTimers()
        {
            if (isPaused)
                return;

            isPaused = true;
            foreach (var entry in activePlayers)
            {
                if (entry.Value.CurrentAliveStartTime.HasValue)
                {
                    TimeSpan aliveSession = DateTime.Now - entry.Value.CurrentAliveStartTime.Value;
                    entry.Value.TotalAliveTime += aliveSession;
                    entry.Value.CurrentAliveStartTime = null;
                    entry.Value.WasAliveWhenPaused = true;
                }
                else
                {
                    entry.Value.WasAliveWhenPaused = false;
                }

                if (entry.Value.CurrentWebSwingStartTime.HasValue)
                {
                    TimeSpan swingSession = DateTime.Now - entry.Value.CurrentWebSwingStartTime.Value;
                    entry.Value.WebSwingTime += swingSession;
                    entry.Value.CurrentWebSwingStartTime = null;
                    entry.Value.WasSwingingWhenPaused = true;
                }
                else
                {
                    entry.Value.WasSwingingWhenPaused = false;
                }

                if (entry.Value.CurrentAirborneStartTime.HasValue)
                {
                    TimeSpan airborneSession = DateTime.Now - entry.Value.CurrentAirborneStartTime.Value;
                    entry.Value.AirborneTime += airborneSession;
                    entry.Value.CurrentAirborneStartTime = null;
                    entry.Value.WasAirborneWhenPaused = true;
                }
                else
                {
                    entry.Value.WasAirborneWhenPaused = false;
                }
            }
        }

        public void ResumeTimers()
        {
            if (!isPaused)
                return;

            isPaused = false;
            foreach (var entry in activePlayers)
            {
                if (entry.Value.WasAliveWhenPaused)
                {
                    entry.Value.CurrentAliveStartTime = DateTime.Now;
                    entry.Value.WasAliveWhenPaused = false;
                }

                if (entry.Value.WasSwingingWhenPaused)
                {
                    entry.Value.CurrentWebSwingStartTime = DateTime.Now;
                    entry.Value.WasSwingingWhenPaused = false;
                }

                if (entry.Value.WasAirborneWhenPaused)
                {
                    entry.Value.CurrentAirborneStartTime = DateTime.Now;
                    entry.Value.WasAirborneWhenPaused = false;
                }
            }
        }


        public void RecordPlayerRespawn(PlayerController playerController)
        {
            if (playerController == null)
            {
                Logger.LogWarning("RecordPlayerRespawn called with null playerController");
                return;
            }

            try
            {
                PlayerInput playerInput = playerController.GetComponentInParent<PlayerInput>();
                if (playerInput != null)
                {
                    StartAliveTimer(playerInput);
                }
                else
                {
                    Logger.LogError($"Could not find PlayerInput for respawn event");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording player respawn: {ex.Message}");
            }
        }
    }
}
