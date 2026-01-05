using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Logger = Silk.Logger;

namespace StatsMod
{
    public class StatsLogger
    {
        private static readonly Lazy<StatsLogger> _lazy = new Lazy<StatsLogger>(() => new StatsLogger());
        public static StatsLogger Instance => _lazy.Value;

        private readonly string logDirectory;

        private StatsLogger()
        {
            try
            {
                string gameDirectory = Path.GetDirectoryName(UnityEngine.Application.dataPath);
                if (string.IsNullOrEmpty(gameDirectory))
                    gameDirectory = Environment.CurrentDirectory;

                logDirectory = Path.Combine(gameDirectory, "Silk", "Logs");

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                    Logger.LogInfo($"Created stats log directory: {logDirectory}");
                }
            }
            catch (Exception ex)
            {
                logDirectory = Environment.CurrentDirectory;
                Logger.LogError($"Failed to create Silk/Logs directory, using current directory: {ex.Message}");
            }

            Logger.LogInfo($"Stats logger initialized. Log directory: {logDirectory}");
        }
        public void LogGameStats(GameStatsSnapshot statsSnapshot)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"Spiderheck_stats_{timestamp}.txt";
                string filePath = Path.Combine(logDirectory, fileName);

                string formattedStats = FormatGameStats(statsSnapshot);

                File.WriteAllText(filePath, formattedStats);

                Logger.LogInfo($"Game stats logged to: {fileName}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to log game stats: {ex.Message}");
            }
        }

        private string FormatGameStats(GameStatsSnapshot statsSnapshot)
        {
            var lines = new List<string>
            {
                "=".PadRight(60, '='),
                "SPIDERHECK SURVIVAL MODE STATISTICS",
                "=".PadRight(60, '='),
                "",
                "GAME INFORMATION:",
                $"  Game End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"  Game Duration: {FormatTimeSpan(statsSnapshot.LastGameDuration)}",
                "",
                "ENEMY STATISTICS:",
                $"  Total Enemies Killed: {statsSnapshot.EnemiesKilled}",
                "",
                "PLAYER STATISTICS:"
            };
            if (statsSnapshot.ActivePlayers != null && statsSnapshot.ActivePlayers.Any())
            {
                var sortedPlayers = statsSnapshot.ActivePlayers
                    .OrderByDescending(p => p.Value.Kills)
                    .ThenBy(p => p.Value.Deaths)
                    .ToList();

                int totalKills = statsSnapshot.ActivePlayers.Sum(p => p.Value.Kills);
                int totalDeaths = statsSnapshot.ActivePlayers.Sum(p => p.Value.Deaths);
                int totalFriendlyKills = statsSnapshot.ActivePlayers.Sum(p => p.Value.FriendlyKills);
                int totalEnemyShieldsTakenDown = statsSnapshot.ActivePlayers.Sum(p => p.Value.EnemyShieldsTakenDown);
                int totalFriendlyShieldsHit = statsSnapshot.ActivePlayers.Sum(p => p.Value.FriendlyShieldsHit);
                int totalShieldsLost = statsSnapshot.ActivePlayers.Sum(p => p.Value.ShieldsLost);

                lines.Add($"  Total Players: {statsSnapshot.ActivePlayers.Count}");
                lines.Add($"  Total Player Kills: {totalKills}");
                lines.Add($"  Total Friendly Kills (PvP): {totalFriendlyKills}");
                lines.Add($"  Total Player Deaths: {totalDeaths}");
                lines.Add($"  Total Enemy Shields Taken Down: {totalEnemyShieldsTakenDown}");
                lines.Add($"  Total Friendly Shields Hit: {totalFriendlyShieldsHit}");
                lines.Add($"  Total Shields Lost: {totalShieldsLost}");
                lines.Add("");

                lines.Add("  Individual Player Performance:");
                lines.Add("  " + "-".PadRight(50, '-'));

                foreach (var playerEntry in sortedPlayers)
                {
                    var playerData = playerEntry.Value;

                    lines.Add($"  {playerData.PlayerName}:");
                    lines.Add($"    Player ID: {playerData.PlayerId}");
                    lines.Add($"    Color: R={playerData.PlayerColor.r:F2}, G={playerData.PlayerColor.g:F2}, B={playerData.PlayerColor.b:F2}, A={playerData.PlayerColor.a:F2}");
                    lines.Add($"    Kills: {playerData.Kills}");
                    lines.Add($"    Kills While Airborne: {playerData.KillsWhileAirborne}");
                    lines.Add($"    Kills While Solo: {playerData.KillsWhileSolo}");
                    lines.Add($"    Max Kill Streak: {playerData.MaxKillStreak}");
                    lines.Add($"    Friendly Kills (PvP): {playerData.FriendlyKills}");
                    lines.Add($"    Deaths: {playerData.Deaths}");
                    lines.Add($"    Enemy Shields Taken Down: {playerData.EnemyShieldsTakenDown}");
                    lines.Add($"    Friendly Shields Hit: {playerData.FriendlyShieldsHit}");
                    lines.Add($"    Shields Lost: {playerData.ShieldsLost}");
                    lines.Add($"    Alive Time: {FormatTimeSpan(playerData.GetCurrentAliveTime())}");
                    lines.Add($"    Web Swings: {playerData.WebSwings}");
                    lines.Add($"    Time Swinging: {FormatTimeSpan(playerData.GetCurrentWebSwingTime())}");
                    lines.Add($"    Time Airborne: {FormatTimeSpan(playerData.GetCurrentAirborneTime())}");
                    lines.Add($"    Highest Point: {playerData.HighestPoint:F1}m");

                    // Weapon hits breakdown
                    if (playerData.WeaponHits != null && playerData.WeaponHits.Any())
                    {
                        lines.Add($"    Weapon Hits (Kills + Shield Hits):");
                        var sortedWeaponHits = playerData.WeaponHits
                            .Where(w => w.Value > 0)
                            .OrderByDescending(w => w.Value)
                            .ToList();

                        if (sortedWeaponHits.Any())
                        {
                            foreach (var weapon in sortedWeaponHits)
                            {
                                lines.Add($"      {weapon.Key}: {weapon.Value}");
                            }
                        }
                        else
                        {
                            lines.Add($"      No weapon hits recorded");
                        }
                    }

                    lines.Add("");
                }
            }
            else
            {
                lines.Add("  No player data available");
                lines.Add("");
            }

            // Add titles section
            if (statsSnapshot.Titles != null && statsSnapshot.Titles.Count > 0)
            {
                lines.Add("TITLES AWARDED:");
                foreach (var title in statsSnapshot.Titles)
                {
                    lines.Add($"  {title.TitleName}: {title.PlayerName}");
                }
                lines.Add("");
            }

            lines.Add("=".PadRight(60, '='));
            lines.Add("End of Statistics Report");
            lines.Add("=".PadRight(60, '='));

            return string.Join(Environment.NewLine, lines);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
            else
                return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
    }
}
