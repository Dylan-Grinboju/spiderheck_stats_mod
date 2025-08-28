using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using Silk;
using Logger = Silk.Logger;

namespace StatsMod
{
    public class StatsLogger
    {
        private static StatsLogger _instance;
        public static StatsLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StatsLogger();
                    Logger.LogInfo("Stats logger created via singleton access");
                }
                return _instance;
            }
        }

        private readonly string logDirectory;

        /// <summary>
        /// Initializes the singleton StatsLogger by determining and preparing the on-disk log directory.
        /// </summary>
        /// <remarks>
        /// Attempts to derive the game base directory from UnityEngine.Application.dataPath (falling back to Environment.CurrentDirectory),
        /// sets the log directory to "{base}/Silk/Logs", and ensures the directory exists. On failure, falls back to the current directory.
        /// Initialization progress and any errors are logged via the project's logging facility.
        /// </remarks>
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
        /// <summary>
        /// Writes a timestamped text file containing a formatted report of the provided game statistics.
        /// </summary>
        /// <param name="statsSnapshot">Snapshot of the game's statistics to be formatted and saved.</param>
        /// <remarks>
        /// The file is written into the logger's configured log directory with the name pattern
        /// "Spiderheck_stats_yyyy-MM-dd_HH-mm-ss.txt". Any I/O or formatting errors are caught and logged; this method does not throw exceptions to callers.
        /// </remarks>
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

        /// <summary>
        /// Formats a GameStatsSnapshot into a human-readable multi-line statistics report.
        /// </summary>
        /// <param name="statsSnapshot">Snapshot containing game duration, enemy totals, and per-player data used to populate the report sections.</param>
        /// <returns>A single string containing the complete formatted statistics report (multiple lines separated by the environment newline).</returns>
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

                lines.Add($"  Total Players: {statsSnapshot.ActivePlayers.Count}");
                lines.Add($"  Total Player Kills: {totalKills}");
                lines.Add($"  Total Player Deaths: {totalDeaths}");
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
                    lines.Add($"    Deaths: {playerData.Deaths}");
                    lines.Add("");
                }
            }
            else
            {
                lines.Add("  No player data available");
                lines.Add("");
            }

            lines.Add("=".PadRight(60, '='));
            lines.Add("End of Statistics Report");
            lines.Add("=".PadRight(60, '='));

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Formats a <see cref="TimeSpan"/> into a human-readable string.
        /// </summary>
        /// <param name="timeSpan">The time span to format.</param>
        /// <returns>
        /// A string in "HH:MM:SS" format, or "Xd HH:MM:SS" when the span contains one or more whole days.
        /// Hours, minutes, and seconds are zero-padded to two digits; days are shown as an integer followed by 'd'.
        /// </returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
            else
                return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
    }
}
