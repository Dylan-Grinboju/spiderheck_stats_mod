using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Silk;
using Logger = Silk.Logger;

namespace StatsMod
{
    public class StatsManager
    {
        private static StatsManager _instance;
        public static StatsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StatsManager();
                    Logger.LogInfo("Stats manager created via singleton access");
                }
                return _instance;
            }
        }

        private PlayerTracker playerTracker;
        private EnemiesTracker enemiesTracker;

        private bool isSurvivalActive = false;
        private DateTime survivalStartTime;
        private TimeSpan lastGameDuration;

        public bool IsSurvivalActive => isSurvivalActive;
        public DateTime SurvivalStartTime => survivalStartTime;
        public TimeSpan LastGameDuration => lastGameDuration;

        public PlayerTracker PlayerTracker => playerTracker;
        public EnemiesTracker EnemiesTracker => enemiesTracker;

        /// <summary>
        /// Private constructor for the singleton StatsManager; obtains references to the PlayerTracker and EnemiesTracker singletons and performs initialization logging.
        /// </summary>
        private StatsManager()
        {
            playerTracker = PlayerTracker.Instance;
            enemiesTracker = EnemiesTracker.Instance;
            Logger.LogInfo("Stats manager initialized");
        }

        /// <summary>
        /// Starts a survival session if none is active, recording the start time and resetting player and enemy trackers.
        /// </summary>
        /// <remarks>
        /// If a session is already active, the method returns without modifying state.
        /// </remarks>
        public void StartSurvivalSession()
        {
            if (isSurvivalActive)
            {
                Logger.LogWarning("Attempting to start survival session while one is already active");
                return;
            }

            isSurvivalActive = true;
            survivalStartTime = DateTime.Now;

            playerTracker.ResetPlayerStats();
            enemiesTracker.ResetEnemiesKilled();

            Logger.LogInfo($"Survival session started");
        }

        /// <summary>
        /// Stops the currently active survival session, records its duration, captures a stats snapshot,
        /// and optionally persists the snapshot to disk.
        /// </summary>
        /// <remarks>
        /// If no survival session is active this method logs a warning and returns without changing state.
        /// When a session is stopped this updates the manager's last game duration and clears the active flag,
        /// obtains a <see cref="GameStatsSnapshot"/>, and — if <c>ModConfig.SaveStatsToFile</c> is true — asks
        /// <c>StatsLogger</c> to persist the snapshot. The method also logs the final session duration.
        /// </remarks>
        public void StopSurvivalSession()
        {
            if (!isSurvivalActive)
            {
                Logger.LogWarning("Attempting to stop survival session when none is active");
                return;
            }

            TimeSpan sessionTime = DateTime.Now - survivalStartTime;
            lastGameDuration = sessionTime;
            isSurvivalActive = false;

            GameStatsSnapshot statsSnapshot = GetStatsSnapshot();

            if (ModConfig.SaveStatsToFile)
            {
                StatsLogger.Instance.LogGameStats(statsSnapshot);
            }

            Logger.LogInfo($"Survival session stopped. Duration: {FormatTimeSpan(sessionTime)}");
        }

        /// <summary>
        /// Returns the elapsed time for the currently active survival session.
        /// </summary>
        /// <returns>
        /// The duration of the current survival session as a <see cref="TimeSpan"/>. If no survival session is active, returns <see cref="TimeSpan.Zero"/>.
        /// </returns>
        public TimeSpan GetCurrentSessionTime()
        {
            if (!isSurvivalActive)
                return TimeSpan.Zero;

            return DateTime.Now - survivalStartTime;
        }

        /// <summary>
        /// Returns the current mapping of active players to their tracked player data.
        /// </summary>
        /// <returns>A dictionary where keys are active PlayerInput instances and values are the corresponding PlayerTracker.PlayerData for each active player.</returns>
        public Dictionary<PlayerInput, PlayerTracker.PlayerData> GetActivePlayers()
        {
            return playerTracker.GetActivePlayers();
        }

        /// <summary>
        /// Gets the current total number of enemies killed in the active session.
        /// </summary>
        /// <returns>The number of enemies killed tracked by the EnemiesTracker.</returns>
        public int GetEnemiesKilled()
        {
            return enemiesTracker.GetEnemiesKilled();
        }

        /// <summary>
        /// Registers a player with the stats manager so the player's statistics are tracked during sessions.
        /// </summary>
        /// <param name="player">The PlayerInput to register (must not be null).</param>
        public void RegisterPlayer(PlayerInput player)
        {
            playerTracker.RegisterPlayer(player);
        }

        /// <summary>
        /// Stops tracking the specified player so they are removed from active player lists and no longer contribute to ongoing statistics.
        /// </summary>
        /// <param name="player">The player to unregister.</param>
        public void UnregisterPlayer(PlayerInput player)
        {
            playerTracker.UnregisterPlayer(player);
        }

        /// <summary>
        /// Increments the death count for the specified player in the player tracker.
        /// </summary>
        /// <param name="player">The player whose death count to increment.</param>
        public void IncrementPlayerDeath(PlayerInput player)
        {
            playerTracker.IncrementPlayerDeath(player);
        }

        /// <summary>
        /// Increments the kill count for the specified player in the player tracker.
        /// </summary>
        /// <param name="player">The player whose kill count should be incremented.</param>
        public void IncrementPlayerKill(PlayerInput player)
        {
            playerTracker.IncrementPlayerKill(player);
        }

        /// <summary>
        /// Updates the tracked color for a given player in the player tracker.
        /// </summary>
        /// <param name="player">The player whose color will be updated.</param>
        /// <param name="color">The new color to assign to the player.</param>
        public void UpdatePlayerColor(PlayerInput player, Color color)
        {
            playerTracker.UpdatePlayerColor(player, color);
        }

        /// <summary>
        /// Increments the global count of enemies killed for the current session.
        /// </summary>
        /// <remarks>
        /// Delegates the increment operation to the internal EnemiesTracker singleton.
        /// </remarks>
        public void IncrementEnemyKilled()
        {
            enemiesTracker.IncrementEnemiesKilled();
        }

        /// <summary>
        /// Record a player's death using their SpiderHealthSystem.
        /// </summary>
        /// <param name="spiderHealth">The player's <see cref="SpiderHealthSystem"/> instance representing the death to record.</param>
        public void RecordPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            playerTracker.RecordPlayerDeath(spiderHealth);
        }

        /// <summary>
        /// Reverses a previously recorded player death for the player associated with the provided SpiderHealthSystem.
        /// </summary>
        /// <param name="spiderHealth">The SpiderHealthSystem instance identifying the player whose death should be undone.</param>
        public void UndoPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            playerTracker.UndoPlayerDeath(spiderHealth);
        }

        /// <summary>
        /// Builds a snapshot of current game statistics reflecting the manager's state.
        /// </summary>
        /// <remarks>
        /// The returned GameStatsSnapshot will have the following populated:
        /// IsSurvivalActive, CurrentSessionTime, LastGameDuration, ActivePlayers, and EnemiesKilled.
        /// Other snapshot properties (totals such as TotalGamesPlayed, TotalPlayerDeaths, TotalPlayerKills) are left at their default values and must be populated elsewhere if needed.
        /// </remarks>
        /// <returns>A GameStatsSnapshot containing the current survival state, session timing, active players, and enemies-killed count.</returns>
        public GameStatsSnapshot GetStatsSnapshot()
        {
            return new GameStatsSnapshot
            {
                IsSurvivalActive = isSurvivalActive,
                CurrentSessionTime = GetCurrentSessionTime(),
                LastGameDuration = lastGameDuration,
                ActivePlayers = GetActivePlayers(),
                EnemiesKilled = GetEnemiesKilled()
            };
        }

        /// <summary>
        /// Formats a <see cref="TimeSpan"/> as a fixed-width "HH:MM:SS" string with two-digit components.
        /// </summary>
        /// <param name="timeSpan">The timespan to format. Note: days and fractional seconds are not represented; hours are taken from <see cref="TimeSpan.Hours"/>.</param>
        /// <returns>A string in the form "HH:MM:SS".</returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
    }

    public class GameStatsSnapshot
    {
        public bool IsSurvivalActive { get; set; }
        public TimeSpan CurrentSessionTime { get; set; }
        public TimeSpan LastGameDuration { get; set; }
        public int TotalGamesPlayed { get; set; }
        public Dictionary<PlayerInput, PlayerTracker.PlayerData> ActivePlayers { get; set; }
        public int TotalPlayerDeaths { get; set; }
        public int TotalPlayerKills { get; set; }
        public int EnemiesKilled { get; set; }
    }
}
