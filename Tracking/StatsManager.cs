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
        private static readonly Lazy<StatsManager> _lazy = new Lazy<StatsManager>(() => new StatsManager());
        public static StatsManager Instance => _lazy.Value;

        private PlayerTracker playerTracker;
        private EnemiesTracker enemiesTracker;

        private bool isSurvivalActive = false;
        private DateTime survivalStartTime;
        private TimeSpan lastGameDuration;

        private bool isPaused = false;
        private DateTime pauseStartTime;

        private bool hasPendingTitles = false;
        private List<TitleEntry> lastGameTitles = new List<TitleEntry>();

        public bool IsSurvivalActive => isSurvivalActive;
        public bool HasPendingTitles => hasPendingTitles;
        public List<TitleEntry> LastGameTitles => new List<TitleEntry>(lastGameTitles);

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

            playerTracker.ResetPlayerStats();
            enemiesTracker.ResetEnemiesKilled();
            playerTracker.StartAllAliveTimers();

            // Clear pending titles from previous game
            hasPendingTitles = false;
            lastGameTitles.Clear();
            UIManager.ClearTitlesForNewGame();

            Logger.LogInfo($"Survival session started");
        }

        public void StopSurvivalSession()
        {
            if (!isSurvivalActive)
            {
                Logger.LogWarning("Attempting to stop survival session when none is active");
                return;
            }

            playerTracker.StopAllAliveTimers();
            playerTracker.StopAllWebSwingTimers();
            playerTracker.StopAllAirborneTimers();

            TimeSpan sessionTime = DateTime.Now - survivalStartTime;
            lastGameDuration = sessionTime;
            isSurvivalActive = false;
            isPaused = false;

            GameStatsSnapshot statsSnapshot = GetStatsSnapshot();

            // Calculate titles for this game
            TitlesManager.Instance.CalculateAndStoreTitles(statsSnapshot);
            var titlesUI = UIManager.Instance?.GetTitlesUI();
            if (titlesUI != null)
            {
                lastGameTitles = titlesUI.GetCurrentTitles();
                hasPendingTitles = lastGameTitles.Count > 0;
                statsSnapshot.Titles = lastGameTitles;
            }

            if (ModConfig.SaveStatsToFile)
            {
                StatsLogger.Instance.LogGameStats(statsSnapshot);
            }

            Logger.LogInfo($"Survival session stopped. Duration: {FormatTimeSpan(sessionTime)}");
        }

        public TimeSpan GetCurrentSessionTime()
        {
            if (!isSurvivalActive)
                return TimeSpan.Zero;

            DateTime endTime = isPaused ? pauseStartTime : DateTime.Now;
            return endTime - survivalStartTime;
        }

        public void PauseTimers()
        {
            if (isPaused || !isSurvivalActive)
                return;

            isPaused = true;
            pauseStartTime = DateTime.Now;
            playerTracker.PauseTimers();
        }

        public void ResumeTimers()
        {
            if (!isPaused || !isSurvivalActive)
                return;

            TimeSpan pausedDuration = DateTime.Now - pauseStartTime;
            survivalStartTime = survivalStartTime.Add(pausedDuration);
            isPaused = false;
            playerTracker.ResumeTimers();
        }

        public void RegisterPlayer(PlayerInput player)
        {
            playerTracker.RegisterPlayer(player);
        }

        public void UnregisterPlayer(PlayerInput player)
        {
            playerTracker.UnregisterPlayer(player);
        }

        public void IncrementPlayerKill(PlayerInput player, string weaponName)
        {
            playerTracker.IncrementPlayerKill(player);
            playerTracker.IncrementWeaponHit(player, weaponName);
        }

        public void IncrementFriendlyKill(PlayerInput player, string weaponName)
        {
            playerTracker.IncrementFriendlyKill(player);
            playerTracker.IncrementWeaponHit(player, weaponName);
        }

        public void IncrementEnemyShieldsTakenDown(PlayerInput player, string weaponName)
        {
            playerTracker.IncrementEnemyShieldsTakenDown(player);
            playerTracker.IncrementWeaponHit(player, weaponName);
        }

        public void IncrementFriendlyShieldsHit(PlayerInput player, string weaponName)
        {
            playerTracker.IncrementFriendlyShieldsHit(player);
            playerTracker.IncrementWeaponHit(player, weaponName);
        }

        public void IncrementShieldsLost(PlayerInput player)
        {
            playerTracker.IncrementShieldsLost(player);
        }

        public void IncrementWebSwings(PlayerInput player)
        {
            playerTracker.IncrementWebSwings(player);
        }

        public void StartWebSwingTimer(PlayerInput player)
        {
            playerTracker.StartWebSwingTimer(player);
        }

        public void StopWebSwingTimer(PlayerInput player)
        {
            playerTracker.StopWebSwingTimer(player);
        }

        public void StartAirborneTimer(PlayerInput player)
        {
            playerTracker.StartAirborneTimer(player);
        }

        public void StopAirborneTimer(PlayerInput player)
        {
            playerTracker.StopAirborneTimer(player);
        }

        public void UpdatePlayerColor(PlayerInput player, Color color)
        {
            playerTracker.UpdatePlayerColor(player, color);
        }

        public void UpdateHighestPoint(PlayerInput player)
        {
            playerTracker.UpdateHighestPoint(player);
        }

        public void IncrementEnemyKilled()
        {
            enemiesTracker.IncrementEnemiesKilled();
        }

        public void RecordPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            playerTracker.RecordPlayerDeath(spiderHealth);
        }

        public void UndoPlayerDeath(SpiderHealthSystem spiderHealth)
        {
            playerTracker.UndoPlayerDeath(spiderHealth);
        }

        public void RecordPlayerRespawn(PlayerController playerController)
        {
            playerTracker.RecordPlayerRespawn(playerController);
        }

        public GameStatsSnapshot GetStatsSnapshot()
        {
            return new GameStatsSnapshot
            {
                IsSurvivalActive = isSurvivalActive,
                CurrentSessionTime = GetCurrentSessionTime(),
                LastGameDuration = lastGameDuration,
                ActivePlayers = new Dictionary<PlayerInput, PlayerTracker.PlayerData>(playerTracker.GetActivePlayers()),
                EnemiesKilled = enemiesTracker.EnemiesKilled
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
        public Dictionary<PlayerInput, PlayerTracker.PlayerData> ActivePlayers { get; set; }
        public int EnemiesKilled { get; set; }
        public List<TitleEntry> Titles { get; set; }
    }
}
