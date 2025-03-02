using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using System.Collections.Generic;

namespace StatsMod
{
    /// <summary>
    /// Handles global game statistics tracking for the mod (non-player specific)
    /// </summary>
    public class StatsTracker
    {
        // Singleton instance
        private static StatsTracker _instance;
        public static StatsTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StatsTracker();
                    Logger.LogInfo("Stats tracker created via singleton access");
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public int EnemiesKilled { get; private set; }
        public int DeathCount { get; private set; } // Keep total death count across all players

        public StatsTracker()
        {
            EnemiesKilled = 0;
            DeathCount = 0;

            // Register as singleton instance
            Instance = this;
            Logger.LogInfo("Stats tracker initialized");
        }

        public void IncrementEnemiesKilled()
        {
            EnemiesKilled++;
            Logger.LogInfo($"Enemy killed! Total: {EnemiesKilled}");
        }

        public void IncrementDeathCount(ulong playerId)
        {
            DeathCount++;

            // If PlayerTracker is initialized, let it handle the player-specific stats
            if (PlayerTracker.Instance != null)
            {
                // Try to record in the player tracker first (for known players)
                if (!PlayerTracker.Instance.TryRecordPlayerDeathById(playerId))
                {
                    // Log that this death wasn't associated with a tracked player
                    Logger.LogInfo($"Death recorded for unknown player ID: {playerId}, Overall deaths: {DeathCount}");
                }
            }
            else
            {
                Logger.LogInfo($"Player {playerId} died! Overall deaths: {DeathCount}");
            }
        }

        public string GetStatsReport()
        {
            string report = $"Global Stats Report:\n";
            report += $"Enemies Killed: {EnemiesKilled}\n";
            report += $"Total Death Count: {DeathCount}\n";

            // Get player death reports from PlayerTracker if available
            if (PlayerTracker.Instance != null)
            {
                report += "\n" + PlayerTracker.Instance.GetDetailedStatsReport();
            }

            return report;
        }
    }
}
