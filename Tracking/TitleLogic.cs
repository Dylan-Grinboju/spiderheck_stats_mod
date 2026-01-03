using UnityEngine;
using UnityEngine.InputSystem;
using Silk;
using Logger = Silk.Logger;
using System.Collections.Generic;
using System.Linq;

namespace StatsMod
{
    public class TitleEntry
    {
        public string TitleName { get; set; }
        public string Description { get; set; }
        public string PlayerName { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public PlayerInput Player { get; set; }
    }

    public class TitleLogic
    {
        private static TitleLogic _instance;
        public static TitleLogic Instance => _instance ?? (_instance = new TitleLogic());

        private List<TitleEntry> currentTitles = new List<TitleEntry>();
        private bool hasGameEndedTitles = false;

        public List<TitleEntry> CurrentTitles => new List<TitleEntry>(currentTitles);
        public bool HasGameEndedTitles => hasGameEndedTitles;
        public int TitleCount => currentTitles.Count;

        public void CalculateTitles(GameStatsSnapshot snapshot)
        {
            currentTitles.Clear();

            if (snapshot?.ActivePlayers == null || snapshot.ActivePlayers.Count <= 1)
            {
                hasGameEndedTitles = false;
                return;
            }

            var players = snapshot.ActivePlayers.ToList();

            var mvpTitle = CalculateMVP(players);
            if (mvpTitle != null)
                currentTitles.Add(mvpTitle);

            var swingerTitle = CalculateSwinger(players);
            if (swingerTitle != null)
                currentTitles.Add(swingerTitle);

            hasGameEndedTitles = currentTitles.Count > 0;
            Logger.LogInfo($"Calculated {currentTitles.Count} titles for {players.Count} players");
        }

        public void ClearTitles()
        {
            currentTitles.Clear();
            hasGameEndedTitles = false;
        }

        private TitleEntry CalculateMVP(List<KeyValuePair<PlayerInput, PlayerTracker.PlayerData>> players)
        {
            if (players.Count == 0) return null;

            var scores = players.Select(p => new
            {
                Player = p.Key,
                Data = p.Value,
                Score = p.Value.Kills - p.Value.Deaths - (2 * p.Value.FriendlyKills)
            }).OrderByDescending(p => p.Score).ToList();

            if (scores.Count >= 2 && scores[0].Score == scores[1].Score)
                return null;

            var winner = scores[0];
            return new TitleEntry
            {
                TitleName = "MVP",
                Description = "best overall performance",
                PlayerName = winner.Data.PlayerName,
                PrimaryColor = winner.Data.PlayerColor,
                SecondaryColor = winner.Data.SecondaryColor,
                Player = winner.Player
            };
        }

        private TitleEntry CalculateSwinger(List<KeyValuePair<PlayerInput, PlayerTracker.PlayerData>> players)
        {
            if (players.Count == 0) return null;

            var scores = players.Select(p => new
            {
                Player = p.Key,
                Data = p.Value,
                Score = p.Value.WebSwings
            }).OrderByDescending(p => p.Score).ToList();

            if (scores[0].Score == 0) return null;
            if (scores.Count >= 2 && scores[0].Score == scores[1].Score)
                return null;

            var winner = scores[0];
            return new TitleEntry
            {
                TitleName = "The Swinger",
                Description = "most web swings",
                PlayerName = winner.Data.PlayerName,
                PrimaryColor = winner.Data.PlayerColor,
                SecondaryColor = winner.Data.SecondaryColor,
                Player = winner.Player
            };
        }
    }
}
