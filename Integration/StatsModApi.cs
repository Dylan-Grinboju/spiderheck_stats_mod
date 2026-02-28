using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;

namespace StatsMod;

/// <summary>
/// Public API for external mods to register custom stats and titles.
/// External titles participate in the domination/priority pipeline alongside native titles.
/// </summary>
public static class StatsModApi
{
    private static readonly List<string> externalStatLines = new();
    private static readonly List<TitleEntry> externalTitles = new();

    public static void RegisterCustomStats(List<string> lines)
    {
        if (lines == null || lines.Count == 0) return;
        externalStatLines.AddRange(lines);
        Logger.LogInfo($"StatsModApi: Registered {lines.Count} external stat lines");
    }

    public static void RegisterCustomTitle(string titleName, string description, string[] requirementNames, PlayerInput leader, bool leaderHasStat, int bonusPriority)
    {
        if (string.IsNullOrEmpty(titleName) || requirementNames == null || requirementNames.Length == 0) return;

        var requirements = new HashSet<string>(requirementNames);
        TitleEntry title;

        if (leader != null)
        {
            if (!leaderHasStat) return;

            var players = PlayerTracker.Instance.GetActivePlayers();
            if (!players.TryGetValue(leader, out var playerData))
            {
                Logger.LogWarning($"StatsModApi: Could not find player data for title '{titleName}'");
                return;
            }

            var kvp = new KeyValuePair<PlayerInput, PlayerTracker.PlayerData>(leader, playerData);
            title = new TitleEntry(kvp)
            {
                TitleName = titleName,
                Description = description,
                Priority = requirements.Count * 10 + bonusPriority,
                Requirements = requirements
            };
            Logger.LogInfo($"StatsModApi: Registered title '{titleName}' for {playerData.PlayerName}");
        }
        else
        {
            title = new TitleEntry(default)
            {
                TitleName = titleName,
                Description = description,
                Priority = requirements.Count * 10 + bonusPriority,
                Requirements = requirements
            };
            Logger.LogInfo($"StatsModApi: Registered title '{titleName}' (player will be resolved from requirements)");
        }

        externalTitles.Add(title);
    }

    public static List<string> GetExternalStats() => new(externalStatLines);

    public static List<TitleEntry> GetExternalTitles() => new(externalTitles);

    public static void ClearExternalData()
    {
        externalStatLines.Clear();
        externalTitles.Clear();
    }
}
