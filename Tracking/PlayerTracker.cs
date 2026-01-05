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
        private static PlayerTracker _instance;
        public static PlayerTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PlayerTracker();
                    Logger.LogInfo("Player tracker created via singleton access");
                }
                return _instance;
            }
        }

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
            public int EnemyShieldsTakenDown { get; set; }
            public int FriendlyShieldsHit { get; set; }
            public int ShieldsLost { get; set; }
            public int KillStreak { get; set; }
            public int MaxKillStreak { get; set; }
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
                EnemyShieldsTakenDown = 0;
                FriendlyShieldsHit = 0;
                ShieldsLost = 0;
                KillStreak = 0;
                MaxKillStreak = 0;
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
                var primaryColorField = typeof(SpiderCustomizer).GetField("_primaryColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (primaryColorField != null)
                {
                    Color primaryColor = (Color)primaryColorField.GetValue(customizer);
                    playerData.PlayerColor = primaryColor;
                }
                var secondaryColorField = typeof(SpiderCustomizer).GetField("_secondaryColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (secondaryColorField != null)
                {
                    Color secondaryColor = (Color)secondaryColorField.GetValue(customizer);
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
                    StopAliveTimer(data);
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
                entry.Value.EnemyShieldsTakenDown = 0;
                entry.Value.FriendlyShieldsHit = 0;
                entry.Value.ShieldsLost = 0;
                entry.Value.WebSwings = 0;
                entry.Value.WebSwingTime = TimeSpan.Zero;
                entry.Value.AirborneTime = TimeSpan.Zero;
                entry.Value.KillStreak = 0;
                entry.Value.MaxKillStreak = 0;
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
                if (data.CurrentWebSwingStartTime.HasValue)
                {
                    TimeSpan swingSession = DateTime.Now - data.CurrentWebSwingStartTime.Value;
                    data.WebSwingTime += swingSession;
                    data.CurrentWebSwingStartTime = null;
                }
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
                if (data.CurrentAirborneStartTime.HasValue)
                {
                    TimeSpan airborneSession = DateTime.Now - data.CurrentAirborneStartTime.Value;
                    data.AirborneTime += airborneSession;
                    data.CurrentAirborneStartTime = null;
                }
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
                if (entry.Value.CurrentAliveStartTime.HasValue)
                {
                    TimeSpan aliveSession = DateTime.Now - entry.Value.CurrentAliveStartTime.Value;
                    entry.Value.TotalAliveTime += aliveSession;
                    entry.Value.CurrentAliveStartTime = null;
                }
            }
        }

        public void StopAllWebSwingTimers()
        {
            foreach (var entry in activePlayers)
            {
                if (entry.Value.CurrentWebSwingStartTime.HasValue)
                {
                    TimeSpan swingSession = DateTime.Now - entry.Value.CurrentWebSwingStartTime.Value;
                    entry.Value.WebSwingTime += swingSession;
                    entry.Value.CurrentWebSwingStartTime = null;
                }
            }
        }

        public void StopAllAirborneTimers()
        {
            foreach (var entry in activePlayers)
            {
                if (entry.Value.CurrentAirborneStartTime.HasValue)
                {
                    TimeSpan airborneSession = DateTime.Now - entry.Value.CurrentAirborneStartTime.Value;
                    entry.Value.AirborneTime += airborneSession;
                    entry.Value.CurrentAirborneStartTime = null;
                }
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

        private void StopAliveTimer(PlayerData data)
        {
            if (data.CurrentAliveStartTime.HasValue)
            {
                TimeSpan aliveSession = DateTime.Now - data.CurrentAliveStartTime.Value;
                data.TotalAliveTime += aliveSession;
                data.CurrentAliveStartTime = null;
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

    [HarmonyPatch(typeof(PlayerInput), "OnEnable")]
    public class PlayerInputEnablePatch
    {
        static void Postfix(PlayerInput __instance)
        {
            try
            {
                StatsManager.Instance.RegisterPlayer(__instance);
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
                StatsManager.Instance.UnregisterPlayer(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in PlayerInput.OnDisable patch: {ex.Message}");
            }
        }
    }
    //I found that this is the function that is called when a spider dies, even if in astral
    [HarmonyPatch(typeof(SpiderHealthSystem), "DisintegrateLegsAndDestroy")]
    public class SpiderHealthSystemDisintegrateLegsAndDestroyPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                StatsManager.Instance.RecordPlayerDeath(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SpiderHealthSystem.DisintegrateLegsAndDestroy patch: {ex.Message}");
            }
        }
    }

    //If the Astral player passed the round, the spider gets revived, and then we uncount the death. 
    //Didn't find a simpler approach to this
    [HarmonyPatch(typeof(SpiderHealthSystem), "DisableDeathEffect")]
    public class SpiderHealthSystemDisableDeathEffect
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                StatsManager.Instance.UndoPlayerDeath(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SpiderHealthSystem.DisableDeathEffect patch: {ex.Message}");
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
                if (playerInput != null)
                {
                    // Access the private _primaryColor field using reflection
                    var primaryColorField = typeof(SpiderCustomizer).GetField("_primaryColor", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (primaryColorField != null)
                    {
                        Color primaryColor = (Color)primaryColorField.GetValue(__instance);
                        StatsManager.Instance.UpdatePlayerColor(playerInput, primaryColor);
                    }
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
                if (StatsManager.Instance.IsActive)
                {
                    StatsManager.Instance.RecordPlayerRespawn(__instance);
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
                if (__instance.rootObject != null)
                {
                    PlayerInput victimPlayerInput = __instance.rootObject.GetComponentInParent<PlayerInput>();
                    if (victimPlayerInput != null)
                    {
                        StatsManager.Instance.IncrementShieldsLost(victimPlayerInput);
                    }
                }
            }
            catch (System.Exception ex)
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
                if (___target == null)
                {
                    return;
                }

                if (__instance.spiderController != null && StatsManager.Instance.IsActive)
                {
                    PlayerInput playerInput = __instance.spiderController.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        StatsManager.Instance.IncrementWebSwings(playerInput);
                        StatsManager.Instance.StartWebSwingTimer(playerInput);
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
                if (__instance.spiderController != null)
                {
                    PlayerInput playerInput = __instance.spiderController.GetComponentInParent<PlayerInput>();
                    if (playerInput != null)
                    {
                        StatsManager.Instance.StopWebSwingTimer(playerInput);
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
        static void Postfix(Stabilizer __instance)
        {
            try
            {
                if (!StatsManager.Instance.IsActive)
                    return;

                SpiderController spider = __instance.GetComponentInParent<SpiderController>();
                if (spider == null) return;

                PlayerInput playerInput = spider.GetComponentInParent<PlayerInput>();
                if (playerInput == null) return;

                StatsManager.Instance.UpdateHighestPoint(playerInput);

                if (!__instance.grounded)
                {
                    StatsManager.Instance.StartAirborneTimer(playerInput);
                }
                else
                {
                    StatsManager.Instance.StopAirborneTimer(playerInput);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking airborne time: {ex.Message}");
            }
        }
    }
}
