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
        private int totalGamesPlayed = 0;

        public bool IsSurvivalActive => isSurvivalActive;
        public DateTime SurvivalStartTime => survivalStartTime;
        public TimeSpan LastGameDuration => lastGameDuration;
        public int TotalGamesPlayed => totalGamesPlayed;

        public PlayerTracker PlayerTracker => playerTracker;
        public EnemiesTracker EnemiesTracker => enemiesTracker;

        private StatsManager()
        {
            playerTracker = PlayerTracker.Instance;
            enemiesTracker = EnemiesTracker.Instance;
            Logger.LogInfo("Stats manager initialized");
        }

        public void StartSurvivalSession()
        {
            if (isSurvivalActive)
            {
                Logger.LogWarning("Attempting to start survival session while one is already active");
                return;
            }

            isSurvivalActive = true;
            survivalStartTime = DateTime.Now;
            totalGamesPlayed++;

            playerTracker.ResetPlayerStats();
            enemiesTracker.ResetEnemiesKilled();

            Logger.LogInfo($"Survival session started. Total games played: {totalGamesPlayed}");
        }

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

            Logger.LogInfo($"Survival session stopped. Duration: {FormatTimeSpan(sessionTime)}");
        }

        public TimeSpan GetCurrentSessionTime()
        {
            if (!isSurvivalActive)
                return TimeSpan.Zero;

            return DateTime.Now - survivalStartTime;
        }

        public Dictionary<PlayerInput, PlayerTracker.PlayerData> GetActivePlayers()
        {
            return playerTracker.GetActivePlayers();
        }

        public int GetTotalPlayerDeaths()
        {
            return playerTracker.GetTotalPlayerDeaths();
        }

        public int GetTotalPlayerKills()
        {
            return playerTracker.GetTotalPlayerKills();
        }

        public int GetEnemiesKilled()
        {
            return enemiesTracker.GetEnemiesKilled();
        }

        public void RegisterPlayer(PlayerInput player)
        {
            playerTracker.RegisterPlayer(player);
        }

        public void UnregisterPlayer(PlayerInput player)
        {
            playerTracker.UnregisterPlayer(player);
        }

        public void IncrementPlayerDeath(PlayerInput player)
        {
            playerTracker.IncrementPlayerDeath(player);
        }

        public void IncrementPlayerKill(PlayerInput player)
        {
            playerTracker.IncrementPlayerKill(player);
        }

        public void IncrementEnemyKilled()
        {
            enemiesTracker.IncrementEnemiesKilled();
        }

        public GameStatsSnapshot GetStatsSnapshot()
        {
            return new GameStatsSnapshot
            {
                IsSurvivalActive = isSurvivalActive,
                CurrentSessionTime = GetCurrentSessionTime(),
                LastGameDuration = lastGameDuration,
                TotalGamesPlayed = totalGamesPlayed,
                ActivePlayers = GetActivePlayers(),
                TotalPlayerDeaths = GetTotalPlayerDeaths(),
                TotalPlayerKills = GetTotalPlayerKills(),
                EnemiesKilled = GetEnemiesKilled()
            };
        }

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
