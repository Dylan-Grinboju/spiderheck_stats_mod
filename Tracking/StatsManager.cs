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
        private bool isVersusActive = false;
        private DateTime sessionStartTime;
        private TimeSpan lastGameDuration;
        private GameMode currentGameMode = GameMode.None;
        private GameMode lastGameMode = GameMode.None;

        private bool isPaused = false;
        private DateTime pauseStartTime;

        private List<TitleEntry> lastGameTitles = new List<TitleEntry>();

        public bool IsActive => isSurvivalActive || isVersusActive;
        public GameMode LastGameMode => lastGameMode;

        private StatsManager()
        {
            playerTracker = PlayerTracker.Instance;
            enemiesTracker = EnemiesTracker.Instance;
            Logger.LogInfo("Stats manager initialized");
        }

        public void StartSurvivalSession()
        {
            StartSession(GameMode.Survival);
        }

        public void StartVersusSession()
        {
            StartSession(GameMode.Versus);
        }

        private void StartSession(GameMode mode)
        {
            if (IsActive)
            {
                Logger.LogWarning($"Attempting to start {mode} session while one is already active");
                return;
            }

            isSurvivalActive = mode == GameMode.Survival;
            isVersusActive = mode == GameMode.Versus;
            currentGameMode = mode;
            sessionStartTime = DateTime.Now;

            playerTracker.ResetPlayerStats();
            enemiesTracker.ResetEnemiesKilled();
            playerTracker.StartAllAliveTimers();

            lastGameTitles.Clear();
            UIManager.ClearTitlesForNewGame();

            Logger.LogInfo($"{mode} session started");
        }

        public void StopSurvivalSession()
        {
            StopSession(GameMode.Survival);
        }

        public void StopVersusSession()
        {
            StopSession(GameMode.Versus);
        }

        private void StopSession(GameMode mode)
        {
            bool isCorrectMode = (mode == GameMode.Survival && isSurvivalActive) ||
                                 (mode == GameMode.Versus && isVersusActive);

            if (!isCorrectMode)
            {
                Logger.LogWarning($"Attempting to stop {mode} session when none is active");
                return;
            }

            playerTracker.StopAllAliveTimers();
            playerTracker.StopAllWebSwingTimers();
            playerTracker.StopAllAirborneTimers();

            TimeSpan sessionTime = DateTime.Now - sessionStartTime;
            lastGameDuration = sessionTime;
            isSurvivalActive = false;
            isVersusActive = false;
            isPaused = false;

            GameStatsSnapshot statsSnapshot = GetStatsSnapshot();

            TitlesManager.Instance.CalculateAndStoreTitles(statsSnapshot);
            var titlesUI = UIManager.Instance?.GetTitlesUI();
            if (titlesUI != null)
            {
                lastGameTitles = titlesUI.GetCurrentTitles();
                statsSnapshot.Titles = lastGameTitles;
            }

            if (ModConfig.SaveStatsToFile)
            {
                StatsLogger.Instance.LogGameStats(statsSnapshot);
            }

            lastGameMode = currentGameMode;
            currentGameMode = GameMode.None;
            Logger.LogInfo($"{mode} session stopped. Duration: {FormatTimeSpan(sessionTime)}");
        }

        public TimeSpan GetCurrentSessionTime()
        {
            if (!IsActive)
                return TimeSpan.Zero;

            DateTime endTime = isPaused ? pauseStartTime : DateTime.Now;
            return endTime - sessionStartTime;
        }

        public void PauseTimers()
        {
            if (isPaused || !IsActive)
                return;

            isPaused = true;
            pauseStartTime = DateTime.Now;
            playerTracker.PauseTimers();
        }

        public void ResumeTimers()
        {
            if (!isPaused || !IsActive)
                return;

            TimeSpan pausedDuration = DateTime.Now - pauseStartTime;
            sessionStartTime = sessionStartTime.Add(pausedDuration);
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

        public void IncrementWaveClutch(PlayerInput player)
        {
            playerTracker.IncrementWaveClutch(player);
        }

        public void CheckWaveClutch()
        {
            if (!isSurvivalActive)
                return;

            var players = playerTracker.GetActivePlayers();
            if (players.Count < 2)
                return;

            PlayerInput clutchPlayer = playerTracker.GetOnlyAlivePlayer();
            if (clutchPlayer != null)
            {
                playerTracker.IncrementWaveClutch(clutchPlayer);
                Logger.LogInfo($"Wave clutch recorded for player");
            }
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
                IsVersusActive = isVersusActive,
                GameMode = currentGameMode,
                CurrentSessionTime = GetCurrentSessionTime(),
                LastGameDuration = lastGameDuration,
                ActivePlayers = new Dictionary<PlayerInput, PlayerTracker.PlayerData>(playerTracker.GetActivePlayers()),
                EnemiesKilled = enemiesTracker.EnemiesKilled
            };
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return TimeFormatUtils.FormatTimeSpan(timeSpan);
        }
    }
}

