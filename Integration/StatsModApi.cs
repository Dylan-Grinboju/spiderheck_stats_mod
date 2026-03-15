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
    private static readonly object _syncRoot = new();
    private static readonly List<string> externalStatLines = new();
    private static readonly List<TitleEntry> externalTitles = new();

    /// <summary>
    /// Registers a list of custom raw stat strings to be displayed on the stats screen.
    /// </summary>
    /// <param name="lines">Lines of text to append to the end-game stats menu.</param>
    public static void RegisterCustomStats(List<string> lines)
    {
        if (lines == null || lines.Count == 0) return;

        lock (_syncRoot)
        {
            externalStatLines.AddRange(lines);
        }

        Logger.LogInfo($"StatsModApi: Registered {lines.Count} external stat lines");
    }

    /// <summary>
    /// Registers a custom title for the post-game victory screen.
    /// </summary>
    /// <param name="titleName">The display name of the title.</param>
    /// <param name="description">A subtitle/description explaining the title. Can be expanded upon dynamically by native stats if any are passed as requirements.</param>
    /// <param name="requirementNames">A list of stat keys that must be met to get this title. Can be native stats (e.g. "MostWebSwings") or entirely custom ones.</param>
    /// <param name="leader">Explicitly assigns the player who achieved this title. If null, the mod attempts to dynamically resolve the player by checking the 'requirementNames' against native stats. NOTE: If your title uses ONLY custom requirements, you MUST provide a non-null leader here to prevent the title from being rejected.</param>
    /// <param name="leaderHasStat">Confirms the explicitly-requested leader actually achieved your custom stat (typically passed as true if the mod already did validation).</param>
    /// <param name="bonusPriority">An additional boost to the title's display priority score (Base priority is 10 per requirement).</param>
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
            title = new TitleEntry()
            {
                TitleName = titleName,
                Description = description,
                Priority = requirements.Count * 10 + bonusPriority,
                Requirements = requirements
            };
            Logger.LogInfo($"StatsModApi: Registered title '{titleName}' (player will be resolved from requirements)");
        }

        lock (_syncRoot)
        {
            externalTitles.Add(title);
        }
    }

    public static List<string> GetExternalStats()
    {
        lock (_syncRoot)
        {
            return new List<string>(externalStatLines);
        }
    }

    public static List<TitleEntry> GetExternalTitles()
    {
        lock (_syncRoot)
        {
            return externalTitles.ConvertAll(t => new TitleEntry
            {
                TitleName = t.TitleName,
                Description = t.Description,
                PlayerName = t.PlayerName,
                PrimaryColor = t.PrimaryColor,
                SecondaryColor = t.SecondaryColor,
                Player = t.Player,
                Priority = t.Priority,
                Requirements = t.Requirements != null ? new HashSet<string>(t.Requirements) : new HashSet<string>()
            });
        }
    }

    public static void ClearExternalData()
    {
        lock (_syncRoot)
        {
            externalStatLines.Clear();
            externalTitles.Clear();
        }
    }
}
