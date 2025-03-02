using System;
using System.Collections.Generic;
using HarmonyLib;
using Silk;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;

namespace StatsMod
{
    /// <summary>
    /// Tracks all active players in the game and manages player-specific stats
    /// </summary>
    public class PlayerTracker
    {
        // Singleton instance
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

        // Dictionary to track active players by their PlayerInput component
        private Dictionary<PlayerInput, PlayerData> activePlayers = new Dictionary<PlayerInput, PlayerData>();

        // Dictionary to track players by their unique ID
        private Dictionary<ulong, PlayerInput> playerIds = new Dictionary<ulong, PlayerInput>();

        // Dictionary to track deaths for players who are no longer active (historical data)
        private Dictionary<ulong, int> pastPlayerDeaths = new Dictionary<ulong, int>();

        // Counter for generating unique IDs for local players
        private ulong nextLocalPlayerId = 1000;

        // Class to store player-specific data
        public class PlayerData
        {
            public ulong PlayerId { get; set; }
            public int Deaths { get; set; }
            public string PlayerName { get; set; }
            public DateTime JoinTime { get; set; }

            public PlayerData(ulong id, string name = "Player")
            {
                PlayerId = id;
                Deaths = 0;
                PlayerName = name;
                JoinTime = DateTime.Now;
            }
        }

        public PlayerTracker()
        {
            Logger.LogInfo("Player tracker initialized");
        }

        /// <summary>
        /// Register a player when they join the game
        /// </summary>
        public void RegisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            // Check if this player is already registered
            if (activePlayers.ContainsKey(player))
            {
                Logger.LogInfo($"Player already registered: {player.playerIndex}");
                return;
            }

            // Generate a local ID
            ulong playerId = nextLocalPlayerId++;

            // Create player data with a name based on player index
            string playerName = $"Player_{player.playerIndex}";
            PlayerData playerData = new PlayerData(playerId, playerName);

            // Register player
            activePlayers[player] = playerData;
            playerIds[playerId] = player;

            Logger.LogInfo($"Registered player ID: {playerId}, Name: {playerName}, Index: {player.playerIndex}");

            // Update stats display
            UpdateTrackerDisplay();
        }

        /// <summary>
        /// Unregister a player when they leave the game
        /// </summary>
        public void UnregisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            if (activePlayers.TryGetValue(player, out PlayerData playerData))
            {
                Logger.LogInfo($"Unregistering player ID: {playerData.PlayerId}, Deaths: {playerData.Deaths}");

                // Store player's death count for historical records
                pastPlayerDeaths[playerData.PlayerId] = playerData.Deaths;

                // Remove from tracking dictionaries
                playerIds.Remove(playerData.PlayerId);
                activePlayers.Remove(player);

                // Update stats display
                UpdateTrackerDisplay();
            }
        }

        /// <summary>
        /// Try to record a death for a player by ID, returns true if the player was found
        /// </summary>
        public bool TryRecordPlayerDeathById(ulong playerId)
        {
            // Try to find the player by ID
            if (playerIds.TryGetValue(playerId, out PlayerInput player) && activePlayers.TryGetValue(player, out PlayerData data))
            {
                data.Deaths++;
                Logger.LogInfo($"Recorded death for player ID: {playerId}, Total deaths: {data.Deaths}");

                // Update stats display
                UpdateTrackerDisplay();
                return true;
            }

            // If not an active player, still track it in historical data
            if (pastPlayerDeaths.ContainsKey(playerId))
            {
                pastPlayerDeaths[playerId]++;
                Logger.LogInfo($"Recorded death for inactive player ID: {playerId}, Total deaths: {pastPlayerDeaths[playerId]}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Record a death for a specific player by ID (creates historical record if player not found)
        /// </summary>
        public void RecordPlayerDeath(ulong playerId)
        {
            if (!TryRecordPlayerDeathById(playerId))
            {
                // Create a new historical entry
                pastPlayerDeaths[playerId] = 1;
                Logger.LogWarning($"Created new death record for unknown player ID: {playerId}");
            }

            // Update the global death counter
            StatsTracker.Instance.IncrementDeathCount(playerId);
        }

        /// <summary>
        /// Record a death for a player based on their SpiderHealthSystem
        /// </summary>
        public void RecordPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            if (spiderHealth == null) return;

            try
            {
                // Try to find the PlayerInput component attached to the spider
                PlayerInput playerInput = null;

                // Use GetComponentInParent if rootObject is available
                if (spiderHealth.rootObject != null)
                {
                    playerInput = spiderHealth.rootObject.GetComponentInParent<PlayerInput>();
                }
                // Fallback to using the gameObject directly
                else if (spiderHealth.gameObject != null)
                {
                    playerInput = spiderHealth.gameObject.GetComponentInParent<PlayerInput>();
                }

                if (playerInput != null && activePlayers.TryGetValue(playerInput, out PlayerData data))
                {
                    // Found the player, record death
                    data.Deaths++;
                    Logger.LogInfo($"Recorded death for player ID: {data.PlayerId}, Total deaths: {data.Deaths}");

                    // Update StatsTracker as well for compatibility
                    StatsTracker.Instance.IncrementDeathCount(data.PlayerId);

                    // Update stats display
                    UpdateTrackerDisplay();
                    return;
                }

                // Fallback to instance ID if all else fails
                ulong fallbackId;
                if (spiderHealth.rootObject != null)
                {
                    fallbackId = (ulong)spiderHealth.rootObject.GetInstanceID();
                }
                else
                {
                    fallbackId = (ulong)spiderHealth.gameObject.GetInstanceID();
                }

                Logger.LogWarning($"Using fallback ID for player death: {fallbackId}");
                StatsTracker.Instance.IncrementDeathCount(fallbackId);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording player death: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the total number of deaths for a specific player ID (active or historical)
        /// </summary>
        public int GetPlayerDeathCount(ulong playerId)
        {
            // Check active players first
            if (playerIds.TryGetValue(playerId, out PlayerInput player) &&
                activePlayers.TryGetValue(player, out PlayerData data))
            {
                return data.Deaths;
            }

            // Check historical records
            if (pastPlayerDeaths.TryGetValue(playerId, out int deaths))
            {
                return deaths;
            }

            return 0;
        }

        /// <summary>
        /// Get the number of active players
        /// </summary>
        public int GetPlayerCount()
        {
            return activePlayers.Count;
        }

        /// <summary>
        /// Get a player's data by their ID
        /// </summary>
        public PlayerData GetPlayerData(ulong playerId)
        {
            if (playerIds.TryGetValue(playerId, out PlayerInput player) &&
                activePlayers.TryGetValue(player, out PlayerData data))
            {
                return data;
            }
            return null;
        }

        /// <summary>
        /// Get a detailed stats report for all players
        /// </summary>
        public string GetDetailedStatsReport()
        {
            string report = $"Player Stats Report (Active Players: {activePlayers.Count})\n";
            report += "----------------------------------------\n";

            foreach (var entry in activePlayers)
            {
                PlayerData player = entry.Value;
                TimeSpan playTime = DateTime.Now - player.JoinTime;
                report += $"{player.PlayerName} (ID: {player.PlayerId}):\n";
                report += $"  Deaths: {player.Deaths}\n";
                report += $"  Play time: {playTime.Hours:00}:{playTime.Minutes:00}:{playTime.Seconds:00}\n";
                report += "----------------------------------------\n";
            }

            if (pastPlayerDeaths.Count > 0)
            {
                report += "\nPreviously Active Players:\n";
                report += "----------------------------------------\n";
                foreach (var entry in pastPlayerDeaths)
                {
                    report += $"Player ID {entry.Key}: {entry.Value} deaths\n";
                }
                report += "----------------------------------------\n";
            }

            return report;
        }

        // Display stats in the game UI - placeholder for future implementation
        private void UpdateTrackerDisplay()
        {
            // This would update a UI display with the latest player stats
            // Will be implemented in the future
        }
    }

    // Harmony patches to hook into PlayerInput lifecycle events
    [HarmonyPatch(typeof(PlayerInput), "OnEnable")]
    public class PlayerInputEnablePatch
    {
        static void Postfix(PlayerInput __instance)
        {
            try
            {
                PlayerTracker.Instance.RegisterPlayer(__instance);
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
                PlayerTracker.Instance.UnregisterPlayer(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in PlayerInput.OnDisable patch: {ex.Message}");
            }
        }
    }

    // Update the existing player death patch to use the PlayerTracker
    [HarmonyPatch(typeof(SpiderHealthSystem), "ExplodeInDirection")]
    public class UpdatedPlayerDeathPatch
    {
        static void Postfix(SpiderHealthSystem __instance)
        {
            try
            {
                PlayerTracker.Instance.RecordPlayerDeath(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in updated player death patch: {ex.Message}");
            }
        }
    }
}
