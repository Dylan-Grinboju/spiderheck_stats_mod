using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace StatsMod
{
    public class GameStatsSnapshot
    {
        public bool IsSurvivalActive { get; set; }
        public bool IsVersusActive { get; set; }
        public GameMode GameMode { get; set; }
        public TimeSpan CurrentSessionTime { get; set; }
        public TimeSpan LastGameDuration { get; set; }
        public Dictionary<PlayerInput, PlayerTracker.PlayerData> ActivePlayers { get; set; }
        public int EnemiesKilled { get; set; }
        public List<TitleEntry> Titles { get; set; }
    }
}
