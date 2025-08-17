using System;
using System.Collections.Generic;
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
        private static float playerCacheRefreshInterval = 60f; // Refresh every 2 seconds
        private static readonly object playerCacheLock = new object();

        public class PlayerData
        {
            public ulong PlayerId { get; set; }
            public int Deaths { get; set; }
            public int Kills { get; set; }
            public string PlayerName { get; set; }
            public DateTime JoinTime { get; set; }

            public PlayerData(ulong id, string name = "Player")
            {
                PlayerId = id;
                Deaths = 0;
                Kills = 0;
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

            if (activePlayers.ContainsKey(player))
            {
                Logger.LogInfo($"Player already registered: {player.playerIndex}");
                return;
            }

            ulong playerId = nextLocalPlayerId++;

            string playerName = $"Player_{player.playerIndex + 1}";
            PlayerData playerData = new PlayerData(playerId, playerName);

            activePlayers[player] = playerData;
            playerIds[playerId] = player;

            RefreshPlayerCache();

            Logger.LogInfo($"Registered player ID: {playerId}, Name: {playerName}, Index: {player.playerIndex}");
        }


        public void UnregisterPlayer(PlayerInput player)
        {
            if (player == null) return;

            if (activePlayers.TryGetValue(player, out PlayerData playerData))
            {
                Logger.LogInfo($"Unregistering player ID: {playerData.PlayerId}, Deaths: {playerData.Deaths}");

                playerIds.Remove(playerData.PlayerId);
                activePlayers.Remove(player);

                RefreshPlayerCache();
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

        public void ResetPlayerStats()
        {
            foreach (var entry in activePlayers)
            {
                entry.Value.Deaths = 0;
                entry.Value.Kills = 0;
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
    }

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
    //I found that this is the function that is called when a spider dies, even if in astral
    [HarmonyPatch(typeof(SpiderHealthSystem), "DisintegrateLegsAndDestroy")]
    public class SpiderHealthSystemDisintegrateLegsAndDestroyPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                PlayerTracker.Instance.RecordPlayerDeath(__instance);
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
                PlayerTracker.Instance.UndoPlayerDeath(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in SpiderHealthSystem.DisintegrateLegsAndDestroy patch: {ex.Message}");
            }
        }
    }
}
