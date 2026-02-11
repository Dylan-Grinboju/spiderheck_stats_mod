using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Silk;
using Logger = Silk.Logger;

namespace StatsMod
{
    /// <summary>
    /// Manages game session lifecycle, timing, and state.
    /// Coordinates between trackers and handles session start/stop events.
    /// </summary>
    public class GameSessionManager
    {
        private static readonly Lazy<GameSessionManager> _lazy = new Lazy<GameSessionManager>(() => new GameSessionManager());
        public static GameSessionManager Instance => _lazy.Value;

        private readonly PlayerTracker playerTracker;
        private readonly EnemiesTracker enemiesTracker;

        private bool isSurvivalActive = false;
        private bool isVersusActive = false;
        private DateTime sessionStartTime;
        private TimeSpan lastGameDuration;
        private GameMode currentGameMode = GameMode.None;
        private GameMode lastGameMode = GameMode.None;

        private bool isPaused = false;
        private DateTime pauseStartTime;

        private List<TitleEntry> lastGameTitles = new List<TitleEntry>();
        private readonly List<string> mapsPlayed = new List<string>();
        private readonly List<string> perksChosen = new List<string>();
        private int painLevel = -1;

        public bool IsActive => isSurvivalActive || isVersusActive;
        public GameMode CurrentGameMode => currentGameMode;
        public GameMode LastGameMode => lastGameMode;
        public string CurrentMapName => mapsPlayed.Count > 0 ? mapsPlayed[mapsPlayed.Count - 1] : "Unknown";

        private GameSessionManager()
        {
            playerTracker = PlayerTracker.Instance;
            enemiesTracker = EnemiesTracker.Instance;
            Logger.LogInfo("Game session manager initialized");
        }

        #region Session Lifecycle

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
            mapsPlayed.Clear();
            perksChosen.Clear();
            painLevel = -1;
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
            Logger.LogInfo($"{mode} session stopped. Duration: {TimeFormatUtils.FormatTimeSpan(sessionTime)}");
        }

        #endregion

        #region Timing

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

        #endregion

        #region Map and Perk Tracking

        public void RecordMap(string mapName)
        {
            if (!IsActive) return;
            if (string.IsNullOrEmpty(mapName)) return;

            mapsPlayed.Add(mapName);
            Logger.LogInfo($"Map recorded: {mapName}");
        }

        public void RecordPerk(string perkName)
        {
            if (!IsActive) return;
            if (string.IsNullOrEmpty(perkName)) return;

            perksChosen.Add(perkName);
            Logger.LogInfo($"Perk recorded: {perkName}");
        }

        public void RecordPainLevel()
        {
            if (!IsActive) return;

            try
            {
                if (PainLevelsScreen.instance != null)
                {
                    painLevel = PainLevelsScreen.instance.GetPainLevel();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not read pain level: {ex.Message}");
            }
        }

        #endregion

        #region Wave Clutch (Survival-specific)

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

        #endregion

        #region Stats Snapshot

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
                EnemiesKilled = enemiesTracker.EnemiesKilled,
                PainLevel = painLevel,
                MapsPlayed = new List<string>(mapsPlayed),
                PerksChosen = new List<string>(perksChosen)
            };
        }

        #endregion
    }
}
