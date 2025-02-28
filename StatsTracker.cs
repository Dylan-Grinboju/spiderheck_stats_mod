using UnityEngine;
using Silk;
using Logger = Silk.Logger;

namespace StatsMod
{
    /// <summary>
    /// Handles all game statistics tracking for the mod
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
        public int DeathCount { get; private set; }

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

        public void IncrementDeathCount()
        {
            DeathCount++;
            Logger.LogInfo($"Player died! Total deaths: {DeathCount}");
        }

        public string GetStatsReport()
        {
            return $"Stats Report:\nEnemies Killed: {EnemiesKilled}\nDeath Count: {DeathCount}";
        }
    }
}
