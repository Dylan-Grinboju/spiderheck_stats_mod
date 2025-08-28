using System;
using System.Collections.Generic;
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

        public class PlayerData
        {
            public ulong PlayerId { get; set; }
            public int Deaths { get; set; }
            public int Kills { get; set; }
            public string PlayerName { get; set; }
            public DateTime JoinTime { get; set; }
            public Color PlayerColor { get; set; }

            /// <summary>
            /// Initializes a new PlayerData instance with the given local player ID and optional display name.
            /// </summary>
            /// <param name="id">Unique local player ID assigned by the tracker.</param>
            /// <param name="name">Display name for the player. Defaults to "Player".</param>
            /// <remarks>
            /// The constructor initializes Deaths and Kills to 0, sets JoinTime to the current time, and sets PlayerColor to white.
            /// </remarks>
            public PlayerData(ulong id, string name = "Player")
            {
                PlayerId = id;
                Deaths = 0;
                Kills = 0;
                PlayerName = name;
                JoinTime = DateTime.Now;
                PlayerColor = Color.white;
            }
        }

        public PlayerTracker()
        {
            Logger.LogInfo("Player tracker initialized");
        }


        /// <summary>
        /// Registers a PlayerInput with the tracker, creating and storing per-player data (ID, name, join time, stats)
        /// and initializing the player's color if a SpiderCustomizer is present.
        /// </summary>
        /// <param name="player">The PlayerInput to register. If null or already registered, the method returns immediately.</param>
        /// <remarks>
        /// Side effects:
        /// - Allocates a new local player ID and stores a PlayerData entry in the tracker.
        /// - Attempts to read the SpiderCustomizer's private `_primaryColor` via reflection and sets PlayerData.PlayerColor when available.
        /// - Refreshes the internal player controller cache and notifies the UI via UIManager.Instance.OnPlayerJoined().
        /// - Logs registration details.
        /// </remarks>
        public void RegisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            if (activePlayers.ContainsKey(player))
            {
                Logger.LogInfo($"Player already registered: {player.playerIndex}");
                return;
            }

            ulong playerId = nextLocalPlayerId++;

            string playerName = $"Player {player.playerIndex + 1}";
            PlayerData playerData = new PlayerData(playerId, playerName);

            // Try to get the spider customizer and set the initial color
            SpiderCustomizer customizer = player.GetComponentInChildren<SpiderCustomizer>();
            if (customizer != null)
            {
                // Access the private _primaryColor field using reflection
                var primaryColorField = typeof(SpiderCustomizer).GetField("_primaryColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (primaryColorField != null)
                {
                    Color primaryColor = (Color)primaryColorField.GetValue(customizer);
                    playerData.PlayerColor = primaryColor;
                    Logger.LogInfo($"Set initial color for player {playerId} to {primaryColor}");
                }
            }

            activePlayers[player] = playerData;
            playerIds[playerId] = player;

            RefreshPlayerCache();

            UIManager.Instance?.OnPlayerJoined();

            Logger.LogInfo($"Registered player ID: {playerId}, Name: {playerName}, Index: {player.playerIndex}");
        }


        /// <summary>
        /// Unregisters a player from the tracker, removing their stats and metadata.
        /// </summary>
        /// <param name="player">The PlayerInput instance to unregister. If null or not currently registered, the method is a no-op.</param>
        /// <remarks>
        /// Side effects:
        /// - Removes the player from internal tracking dictionaries.
        /// - Refreshes the cached player controller list.
        /// - Invokes UIManager.Instance?.OnPlayerLeft() to notify the UI.
        /// </remarks>
        public void UnregisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            if (activePlayers.TryGetValue(player, out PlayerData playerData))
            {
                Logger.LogInfo($"Unregistering player ID: {playerData.PlayerId}, Deaths: {playerData.Deaths}");

                playerIds.Remove(playerData.PlayerId);
                activePlayers.Remove(player);

                RefreshPlayerCache();

                UIManager.Instance?.OnPlayerLeft();
            }
        }

        public void RecordPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            if (spiderHealth == null) return;

            try
            {
                PlayerInput playerInput = null;
                if (spiderHealth.rootObject != null)
                {
                    playerInput = spiderHealth.rootObject.GetComponentInParent<PlayerInput>();
                }

                if (playerInput != null && activePlayers.TryGetValue(playerInput, out PlayerData data))
                {
                    data.Deaths++;
                    Logger.LogInfo($"Recorded death for player ID: {data.PlayerId}, Total deaths: {data.Deaths}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording player death: {ex.Message}");
            }
        }

        public void UndoPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            if (spiderHealth == null) return;

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
                    Logger.LogInfo($"Undo Recorded death for player ID: {data.PlayerId}, Total deaths: {data.Deaths}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording player death: {ex.Message}");
            }
        }

        public void RecordPlayerKill(PlayerInput playerInput)
        {
            if (playerInput == null) return;

            try
            {
                if (activePlayers.TryGetValue(playerInput, out PlayerData data))
                {
                    data.Kills++;
                    Logger.LogInfo($"Recorded kill for player ID: {data.PlayerId}, Total kills: {data.Kills}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording player kill: {ex.Message}");
            }
        }

        //record player kill via his ID
        public void RecordPlayerKill(ulong playerId)
        {
            if (playerIds.TryGetValue(playerId, out PlayerInput playerInput))
            {
                RecordPlayerKill(playerInput);
            }
            else
            {
                Logger.LogError($"Player ID {playerId} not found in active players.");
            }
        }

        public List<(string playerName, int deaths, int kills, ulong playerId)> GetPlayerStatsList()
        {
            List<(string playerName, int deaths, int kills, ulong playerId)> result = new List<(string playerName, int deaths, int kills, ulong playerId)>();

            foreach (var entry in activePlayers)
            {
                PlayerData player = entry.Value;
                result.Add((player.PlayerName, player.Deaths, player.Kills, player.PlayerId));
            }

            return result;
        }

        /// <summary>
        /// Reset deaths and kills to zero for every player currently tracked by the manager.
        /// </summary>
        /// <remarks>
        /// Preserves other player metadata (IDs, names, join times, colors); operates only on the in-memory active player statistics.
        /// </remarks>
        public void ResetPlayerStats()
        {
            foreach (var entry in activePlayers)
            {
                entry.Value.Deaths = 0;
                entry.Value.Kills = 0;
            }
        }

        /// <summary>
        /// Returns a shallow copy of the current mapping of active PlayerInput instances to their PlayerData.
        /// </summary>
        /// <returns>
        /// A new Dictionary containing the same PlayerInput keys and references to the corresponding PlayerData objects.
        /// Modifying the returned dictionary (adding/removing entries) does not affect the tracker’s internal state;
        /// modifying the PlayerData instances themselves will affect the tracker since the copy is shallow.
        /// </returns>
        public Dictionary<PlayerInput, PlayerData> GetActivePlayers()
        {
            return new Dictionary<PlayerInput, PlayerData>(activePlayers);
        }

        /// <summary>
        /// Increments the death counter for the specified player if that player is currently tracked.
        /// </summary>
        /// <param name="player">The PlayerInput whose tracked death count should be incremented; no action is taken if the player is null or not registered.</param>
        public void IncrementPlayerDeath(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.Deaths++;
                Logger.LogInfo($"Incremented death for player ID: {data.PlayerId}, Total deaths: {data.Deaths}");
            }
        }

        /// <summary>
        /// Increments the tracked kill count for the specified player.
        /// </summary>
        /// <param name="player">The PlayerInput whose tracked kill count should be incremented. If null or not currently tracked, the call is a no-op.</param>
        public void IncrementPlayerKill(PlayerInput player)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.Kills++;
                Logger.LogInfo($"Incremented kill for player ID: {data.PlayerId}, Total kills: {data.Kills}");
            }
        }

        /// <summary>
        /// Update the tracked visual color for a registered player.
        /// </summary>
        /// <param name="player">The PlayerInput identifying the player whose color should be updated. If null or not registered, the call is a no-op.</param>
        /// <param name="color">The new Color to assign to the player's PlayerColor.</param>
        public void UpdatePlayerColor(PlayerInput player, Color color)
        {
            if (player != null && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.PlayerColor = color;
                Logger.LogInfo($"Updated color for player ID: {data.PlayerId} to {color}");
            }
        }

        /// <summary>
        /// Retrieves the current cached array of active PlayerController instances, refreshing the cache when stale.
        /// </summary>
        /// <remarks>
        /// The cache is refreshed if it is empty or if more than <c>playerCacheRefreshInterval</c> seconds have elapsed
        /// since the last update. Access is synchronized with <c>playerCacheLock</c>, so this method is safe to call concurrently.
        /// </remarks>
        /// <returns>An array of found <see cref="PlayerController"/> objects; never null (may be empty).</returns>
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
    }

    [HarmonyPatch(typeof(PlayerInput), "OnEnable")]
    public class PlayerInputEnablePatch
    {
        /// <summary>
        /// Harmony postfix for PlayerInput.OnEnable; registers the enabled PlayerInput with the StatsManager.
        /// </summary>
        /// <param name="__instance">The PlayerInput instance that was enabled.</param>
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
        /// <summary>
        /// Harmony Prefix for PlayerInput.OnDisable: unregisters the given player from the StatsManager when their input is disabled.
        /// </summary>
        /// <param name="__instance">The PlayerInput instance that is being disabled.</param>
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
        /// <summary>
        /// Harmony prefix called before SpiderHealthSystem.DisintegrateLegsAndDestroy that records a death for the player associated with the given spider instance.
        /// </summary>
        /// <param name="__instance">The SpiderHealthSystem instance whose destruction should be recorded as a player death.</param>
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
        /// <summary>
        /// Harmony prefix for SpiderHealthSystem.DisableDeathEffect that attempts to undo a previously recorded player death.
        /// </summary>
        /// <param name="__instance">The SpiderHealthSystem instance whose death effect is being disabled; used to locate and update the associated player's death count via StatsManager.</param>
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
        /// <summary>
        /// Harmony postfix applied to SpiderCustomizer.SetSpiderColor; when a spider's color is set, find the owning PlayerInput,
        /// read the spider's private `_primaryColor` field via reflection, and update that player's color in StatsManager.
        /// </summary>
        /// <param name="__instance">The SpiderCustomizer instance whose color was changed.</param>
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
}
