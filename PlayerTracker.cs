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
        }


        public void UnregisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            if (activePlayers.TryGetValue(player, out PlayerData playerData))
            {
                Logger.LogInfo($"Unregistering player ID: {playerData.PlayerId}, Deaths: {playerData.Deaths}");

                // Remove from tracking dictionaries
                playerIds.Remove(playerData.PlayerId);
                activePlayers.Remove(player);
            }
        }

        public void RecordPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            if (spiderHealth == null) return;

            try
            {
                // Try to find the PlayerInput component attached to the spider
                PlayerInput playerInput = null;
                //TODO: check obsidian
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

                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording player death: {ex.Message}");
            }
        }

        // New method to get player stats directly as a list
        public List<(string playerName, int deaths, ulong playerId)> GetPlayerStatsList()
        {
            List<(string playerName, int deaths, ulong playerId)> result = new List<(string playerName, int deaths, ulong playerId)>();

            foreach (var entry in activePlayers)
            {
                PlayerData player = entry.Value;
                result.Add((player.PlayerName, player.Deaths, player.PlayerId));
            }

            return result;
        }

        // New method to get active player count
        public int GetActivePlayerCount()
        {
            return activePlayers.Count;
        }

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

            return report;
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
