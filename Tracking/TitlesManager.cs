using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatsMod
{
    public class TitleEntry
    {
        public TitleEntry(KeyValuePair<PlayerInput, PlayerTracker.PlayerData> playerData)
        {
            Player = playerData.Key;
            PlayerName = playerData.Value.PlayerName;
            PrimaryColor = playerData.Value.PlayerColor;
            SecondaryColor = playerData.Value.SecondaryColor;
        }

        public string TitleName { get; set; }
        public string Description { get; set; }
        public string PlayerName { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public PlayerInput Player { get; set; }
        public int Priority { get; set; }
        public HashSet<string> Requirements { get; set; } = new HashSet<string>();
    }

    public class TitleBuilder
    {
        private readonly StatLeaders _leaders;
        private KeyValuePair<PlayerInput, PlayerTracker.PlayerData> _primaryLeader;
        private readonly HashSet<string> _requirements = new HashSet<string>();
        private string _titleName;
        private string _description;
        private int _priority;

        public TitleBuilder(StatLeaders leaders)
        {
            _leaders = leaders;
        }

        public TitleBuilder ForLeader(Func<StatLeaders, KeyValuePair<PlayerInput, PlayerTracker.PlayerData>> leaderSelector, string requirementName)
        {
            _primaryLeader = leaderSelector(_leaders);
            _requirements.Add(requirementName);
            return this;
        }
        public TitleBuilder AndLeader(string requirementName)
        {
            _requirements.Add(requirementName);
            return this;
        }

        public TitleBuilder WithName(string name)
        {
            _titleName = name;
            return this;
        }

        public TitleBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public TitleBuilder WithPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        public TitleEntry Build()
        {
            return new TitleEntry(_primaryLeader)
            {
                TitleName = _titleName,
                Description = _description,
                Priority = _priority,
                Requirements = new HashSet<string>(_requirements)
            };
        }

        public static bool SamePlayer(params KeyValuePair<PlayerInput, PlayerTracker.PlayerData>[] leaders)
        {
            if (leaders.Length == 0) return false;
            var firstPlayer = leaders[0].Key;
            return leaders.All(l => l.Key == firstPlayer);
        }
    }

    public static class Req
    {
        public const string MostWebSwings = nameof(StatLeaders.MostWebSwings);
        public const string LeastWebSwings = nameof(StatLeaders.LeastWebSwings);
        public const string HighestPoint = nameof(StatLeaders.HighestPoint);
        public const string LowestPoint = nameof(StatLeaders.LowestPoint);
        public const string MostAirborneTime = nameof(StatLeaders.MostAirborneTime);
        public const string LeastAirborneTime = nameof(StatLeaders.LeastAirborneTime);
        public const string MostKillsWhileAirborne = nameof(StatLeaders.MostKillsWhileAirborne);
        public const string MostKillsWhileSolo = nameof(StatLeaders.MostKillsWhileSolo);
        public const string MostWaveClutches = nameof(StatLeaders.MostWaveClutches);
        public const string MaxKillStreak = nameof(StatLeaders.MaxKillStreak);
        public const string MaxKillStreakWhileSolo = nameof(StatLeaders.MaxKillStreakWhileSolo);
        public const string MostAliveTime = nameof(StatLeaders.MostAliveTime);
        public const string MostOffense = nameof(StatLeaders.MostOffense);
        public const string LeastOffense = nameof(StatLeaders.LeastOffense);
        public const string MostDamageTaken = nameof(StatLeaders.MostDamageTaken);
        public const string LeastDamageTaken = nameof(StatLeaders.LeastDamageTaken);
        public const string MostFriendlyFire = nameof(StatLeaders.MostFriendlyFire);
        public const string LeastFriendlyFire = nameof(StatLeaders.LeastFriendlyFire);
        public const string MostShieldsLost = nameof(StatLeaders.MostShieldsLost);
        public const string LeastShieldsLost = nameof(StatLeaders.LeastShieldsLost);
        public const string MostDeaths = nameof(StatLeaders.MostDeaths);
        public const string LeastDeaths = nameof(StatLeaders.LeastDeaths);
        public const string MostLavaDeaths = nameof(StatLeaders.MostLavaDeaths);
        public const string LeastLavaDeaths = nameof(StatLeaders.LeastLavaDeaths);
        public const string MostGunsKills = nameof(StatLeaders.MostGunsKills);
        public const string MostExplosionsKills = nameof(StatLeaders.MostExplosionsKills);
        public const string MostBladeKills = nameof(StatLeaders.MostBladeKills);
        public const string MostHornetKills = nameof(StatLeaders.MostHornetKills);
        public const string MostWhispKills = nameof(StatLeaders.MostWhispKills);
        public const string MostKhepriKills = nameof(StatLeaders.MostKhepriKills);
    }

    public class StatLeaders
    {
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostWebSwings { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastWebSwings { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> HighestPoint { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LowestPoint { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostAirborneTime { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastAirborneTime { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostKillsWhileAirborne { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostKillsWhileSolo { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostWaveClutches { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MaxKillStreak { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MaxKillStreakWhileSolo { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostAliveTime { get; set; }

        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostOffense { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastOffense { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostDamageTaken { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastDamageTaken { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostFriendlyFire { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastFriendlyFire { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostShieldsLost { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastShieldsLost { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostDeaths { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastDeaths { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostLavaDeaths { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastLavaDeaths { get; set; }

        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostGunsKills { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostExplosionsKills { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostBladeKills { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostHornetKills { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostWhispKills { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostKhepriKills { get; set; }
    }

    public class TitlesManager
    {
        private static readonly Lazy<TitlesManager> _lazy = new Lazy<TitlesManager>(() => new TitlesManager());
        public static TitlesManager Instance => _lazy.Value;

        private List<TitleEntry> currentTitles = new List<TitleEntry>();
        private bool hasGameEndedTitles = false;

        public List<TitleEntry> CurrentTitles => new List<TitleEntry>(currentTitles);
        public bool HasGameEndedTitles => hasGameEndedTitles;
        public int TitleCount => currentTitles.Count;
        public event Action OnTitlesUpdated;

        public void CalculateAndStoreTitles(GameStatsSnapshot snapshot)
        {
            currentTitles.Clear();

            if (snapshot?.ActivePlayers == null || snapshot.ActivePlayers.Count <= 1)
            {
                hasGameEndedTitles = false;
                return;
            }

            var players = snapshot.ActivePlayers.ToList();
            var leaders = CalculateStatLeaders(players);

            var oneCategoryTitles = CreateOneCategoryTitles(leaders);
            currentTitles.AddRange(oneCategoryTitles);

            var twoCategoryTitles = CreateTwoCategoryTitles(leaders);
            currentTitles.AddRange(twoCategoryTitles);

            var threeCategoryTitles = CreateThreeCategoryTitles(leaders);
            currentTitles.AddRange(threeCategoryTitles);

            var fourCategoryTitles = CreateFourCategoryTitles(leaders);
            currentTitles.AddRange(fourCategoryTitles);

            var fiveCategoryTitles = CreateFiveCategoryTitles(leaders);
            currentTitles.AddRange(fiveCategoryTitles);

            RemoveDominatedTitles();

            foreach (var player in players)
            {
                if (!currentTitles.Any(t => t.Player == player.Key))
                {
                    currentTitles.Add(new TitleEntry(player)
                    {
                        TitleName = "Average Joe",
                        Description = "Participated",
                        Priority = 100
                    });
                }
            }
            BalanceTitlePriorities();

            currentTitles = currentTitles.OrderByDescending(t => t.Priority).ToList();

            hasGameEndedTitles = currentTitles.Count > 0;
            Logger.LogInfo($"Calculated {currentTitles.Count} titles for {players.Count} players");
            OnTitlesUpdated?.Invoke();
        }

        private StatLeaders CalculateStatLeaders(List<KeyValuePair<PlayerInput, PlayerTracker.PlayerData>> players)
        {
            var leaders = new StatLeaders();

            var swingsRanked = players.OrderByDescending(p => p.Value.WebSwings).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostWebSwings = swingsRanked[0];
            leaders.LeastWebSwings = swingsRanked[swingsRanked.Count - 1];

            var altitudeRanked = players.OrderByDescending(p => p.Value.HighestPoint).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.HighestPoint = altitudeRanked[0];
            leaders.LowestPoint = altitudeRanked[altitudeRanked.Count - 1];

            var airborneRanked = players.OrderByDescending(p => p.Value.AirborneTime).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostAirborneTime = airborneRanked[0];
            leaders.LeastAirborneTime = airborneRanked[airborneRanked.Count - 1];

            var airborneKillsRanked = players.OrderByDescending(p => p.Value.KillsWhileAirborne).ThenByDescending(p => p.Value.Kills).ToList();
            leaders.MostKillsWhileAirborne = airborneKillsRanked[0];

            var soloKillsRanked = players.OrderByDescending(p => p.Value.KillsWhileSolo).ThenByDescending(p => p.Value.Kills).ToList();
            leaders.MostKillsWhileSolo = soloKillsRanked[0];

            var waveClutchesRanked = players.OrderByDescending(p => p.Value.WaveClutches).ThenByDescending(p => p.Value.Kills).ToList();
            leaders.MostWaveClutches = waveClutchesRanked[0];

            var streakRanked = players.OrderByDescending(p => p.Value.MaxKillStreak).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MaxKillStreak = streakRanked[0];

            var streakWhileSoloRanked = players.OrderByDescending(p => p.Value.MaxKillStreakWhileSolo).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MaxKillStreakWhileSolo = streakWhileSoloRanked[0];

            var aliveTimeRanked = players.OrderByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostAliveTime = aliveTimeRanked[0];

            var offenseRanked = players.OrderByDescending(p => p.Value.Kills + p.Value.EnemyShieldsTakenDown).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostOffense = offenseRanked[0];
            leaders.LeastOffense = offenseRanked[offenseRanked.Count - 1];

            var damageTakenRanked = players.OrderByDescending(p => p.Value.Deaths + p.Value.ShieldsLost).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostDamageTaken = damageTakenRanked[0];
            leaders.LeastDamageTaken = damageTakenRanked[damageTakenRanked.Count - 1];

            var friendlyFireRanked = players.OrderByDescending(p => p.Value.FriendlyKills + p.Value.FriendlyShieldsHit).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostFriendlyFire = friendlyFireRanked[0];
            leaders.LeastFriendlyFire = friendlyFireRanked[friendlyFireRanked.Count - 1];

            var shieldsLostRanked = players.OrderByDescending(p => p.Value.ShieldsLost).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostShieldsLost = shieldsLostRanked[0];
            leaders.LeastShieldsLost = shieldsLostRanked[shieldsLostRanked.Count - 1];

            var deathsRanked = players.OrderByDescending(p => p.Value.Deaths).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostDeaths = deathsRanked[0];
            leaders.LeastDeaths = deathsRanked[deathsRanked.Count - 1];

            var lavaDeathsRanked = players.OrderByDescending(p => p.Value.LavaDeaths).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostLavaDeaths = lavaDeathsRanked[0];
            leaders.LeastLavaDeaths = lavaDeathsRanked[lavaDeathsRanked.Count - 1];

            var gunsRanked = players.OrderByDescending(p => p.Value.WeaponHits["Shotgun"] + p.Value.WeaponHits["RailShot"] + p.Value.WeaponHits["DeathRay"] + p.Value.WeaponHits["EnergyBall"] + p.Value.WeaponHits["Laser Cannon"] + p.Value.WeaponHits["SawDisc"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostGunsKills = gunsRanked[0];

            var explosionsRanked = players.OrderByDescending(p => p.Value.WeaponHits["Explosions"] + p.Value.WeaponHits["Laser Cube"] + p.Value.WeaponHits["DeathCube"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostExplosionsKills = explosionsRanked[0];

            var bladeRanked = players.OrderByDescending(p => p.Value.WeaponHits["Particle Blade"] + p.Value.WeaponHits["KhepriStaff"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostBladeKills = bladeRanked[0];

            var hornetsRanked = players.OrderByDescending(p => p.Value.EnemyKills["Hornet"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostHornetKills = hornetsRanked[0];

            var whispsRanked = players.OrderByDescending(p => p.Value.EnemyKills["Whisp"] + p.Value.EnemyKills["Power Whisp"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostWhispKills = whispsRanked[0];

            var kheprisRanked = players.OrderByDescending(p => p.Value.EnemyKills["Khepri"] + p.Value.EnemyKills["Power Khepri"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostKhepriKills = kheprisRanked[0];

            return leaders;
        }

        public void ClearTitles()
        {
            currentTitles.Clear();
            hasGameEndedTitles = false;
            OnTitlesUpdated?.Invoke();
        }


        private void RemoveDominatedTitles()
        {
            var titlesByPlayer = currentTitles.GroupBy(t => t.Player);
            var toRemove = new HashSet<TitleEntry>();

            foreach (var playerTitles in titlesByPlayer)
            {
                // Sort titles by requirement count (descending) for optimization
                // Titles with more requirements can only dominate titles with fewer requirements
                var titles = playerTitles.OrderByDescending(t => t.Requirements.Count).ToList();

                for (int i = 0; i < titles.Count; i++)
                {
                    if (toRemove.Contains(titles[i]))
                        continue;

                    var currentRequirementCount = titles[i].Requirements.Count;

                    for (int j = 0; j < i; j++)
                    {
                        if (titles[j].Requirements.Count <= currentRequirementCount)
                            break;

                        if (titles[i].Requirements.IsSubsetOf(titles[j].Requirements))
                        {
                            toRemove.Add(titles[i]);
                            break;
                        }
                    }
                }
            }

            currentTitles.RemoveAll(t => toRemove.Contains(t));
        }

        // To prevent one player from dominating all titles due to sheer number of categories,
        // we increase the priority of titles held by other players. Makes it more fun.
        private void BalanceTitlePriorities()
        {
            var titlesByPlayer = currentTitles.GroupBy(t => t.Player)
                                              .ToDictionary(g => g.Key, g => g.Count());

            foreach (var title in currentTitles)
            {
                int otherPlayersTitleCount = titlesByPlayer
                    .Where(kvp => kvp.Key != title.Player)
                    .Sum(kvp => kvp.Value);

                title.Priority += otherPlayersTitleCount * 5;
            }
        }

        private List<TitleEntry> CreateOneCategoryTitles(StatLeaders leaders, int defaultPriority = 10)
        {
            var titles = new List<TitleEntry>();

            if (leaders.MostWebSwings.Value.WebSwings > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWebSwings, Req.MostWebSwings)
                    .WithName("Peter Parker")
                    .WithDescription($"Most Web Swings ({leaders.MostWebSwings.Value.WebSwings})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.HighestPoint.Value.HighestPoint > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.HighestPoint, Req.HighestPoint)
                    .WithName(leaders.HighestPoint.Value.HighestPoint >= 1000 ? "1000 Meters Club" : "Sky Scraper")
                    .WithDescription($"Highest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(leaders.HighestPoint.Value.HighestPoint >= 1000 ? 25 : defaultPriority)
                    .Build());
            }

            if (leaders.MostAirborneTime.Value.AirborneTime > TimeSpan.Zero)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostAirborneTime, Req.MostAirborneTime)
                    .WithName("Air Dancer")
                    .WithDescription($"Most Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MostKillsWhileAirborne.Value.KillsWhileAirborne > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostKillsWhileAirborne, Req.MostKillsWhileAirborne)
                    .WithName("Sky Hunter")
                    .WithDescription($"Most Kills While Airborne ({leaders.MostKillsWhileAirborne.Value.KillsWhileAirborne})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MostKillsWhileSolo.Value.KillsWhileSolo > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostKillsWhileSolo, Req.MostKillsWhileSolo)
                    .WithName("Lone Wolf")
                    .WithDescription($"Most Kills While Solo ({leaders.MostKillsWhileSolo.Value.KillsWhileSolo})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MostWaveClutches.Value.WaveClutches > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWaveClutches, Req.MostWaveClutches)
                    .WithName("Clutch Master")
                    .WithDescription($"Most Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MaxKillStreak.Value.MaxKillStreak > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MaxKillStreak, Req.MaxKillStreak)
                    .WithName("Serial Killer")
                    .WithDescription($"Max Kill Streak ({leaders.MaxKillStreak.Value.MaxKillStreak})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MaxKillStreakWhileSolo.Value.MaxKillStreakWhileSolo > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MaxKillStreakWhileSolo, Req.MaxKillStreakWhileSolo)
                    .WithName("Solo Rampage")
                    .WithDescription($"Max Kill Streak While Solo ({leaders.MaxKillStreakWhileSolo.Value.MaxKillStreakWhileSolo})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MostAliveTime.Value.TotalAliveTime > TimeSpan.Zero)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostAliveTime, Req.MostAliveTime)
                    .WithName("Survivor")
                    .WithDescription($"Most Alive Time ({leaders.MostAliveTime.Value.TotalAliveTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if ((leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown) > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .WithName("Destroyer")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if ((leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost) > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostDamageTaken, Req.MostDamageTaken)
                    .WithName("Punching Bag")
                    .WithDescription($"Most Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            titles.Add(new TitleBuilder(leaders)
                .ForLeader(l => l.LeastDamageTaken, Req.LeastDamageTaken)
                .WithName("Shadow")
                .WithDescription($"Least Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})")
                .WithPriority(defaultPriority)
                .Build());

            if ((leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit) > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostFriendlyFire, Req.MostFriendlyFire)
                    .WithName("Confused")
                    .WithDescription($"Most Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            titles.Add(new TitleBuilder(leaders)
                .ForLeader(l => l.LeastFriendlyFire, Req.LeastFriendlyFire)
                .WithName("Team Player")
                .WithDescription($"Least Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})")
                .WithPriority(defaultPriority)
                .Build());

            titles.Add(new TitleBuilder(leaders)
                .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                .WithName("Pacifist")
                .WithDescription($"Least Offense ({leaders.LeastOffense.Value.Kills + leaders.LeastOffense.Value.EnemyShieldsTakenDown})")
                .WithPriority(defaultPriority)
                .Build());

            var gunKills = leaders.MostGunsKills.Value.WeaponHits["Shotgun"] +
                           leaders.MostGunsKills.Value.WeaponHits["RailShot"] +
                           leaders.MostGunsKills.Value.WeaponHits["DeathRay"] +
                           leaders.MostGunsKills.Value.WeaponHits["EnergyBall"] +
                           leaders.MostGunsKills.Value.WeaponHits["Laser Cannon"] +
                           leaders.MostGunsKills.Value.WeaponHits["SawDisc"];

            if (gunKills > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostGunsKills, Req.MostGunsKills)
                    .WithName("Gunslinger")
                    .WithDescription($"Most Gun Kills ({gunKills})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            var explosionKills = leaders.MostExplosionsKills.Value.WeaponHits["Explosions"] +
                                 leaders.MostExplosionsKills.Value.WeaponHits["Laser Cube"] +
                                 leaders.MostExplosionsKills.Value.WeaponHits["DeathCube"];

            if (explosionKills > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .WithName("Demolitionist")
                    .WithDescription($"Most Explosive Kills ({explosionKills})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            var bladeKills = leaders.MostBladeKills.Value.WeaponHits["Particle Blade"] +
                             leaders.MostBladeKills.Value.WeaponHits["KhepriStaff"];

            if (bladeKills > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .WithName("Blade Master")
                    .WithDescription($"Most Blade Kills ({bladeKills})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MostLavaDeaths.Value.LavaDeaths > 0)
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostLavaDeaths, Req.MostLavaDeaths)
                    .WithName("Slippery")
                    .WithDescription($"Most Lava Deaths ({leaders.MostLavaDeaths.Value.LavaDeaths})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            titles.Add(new TitleBuilder(leaders)
                .ForLeader(l => l.LeastLavaDeaths, Req.LeastLavaDeaths)
                .WithName("Floor is Lava")
                .WithDescription($"Least Lava Deaths ({leaders.LeastLavaDeaths.Value.LavaDeaths})")
                .WithPriority(defaultPriority)
                .Build());

            return titles;
        }

        private List<TitleEntry> CreateTwoCategoryTitles(StatLeaders leaders, int defaultPriority = 20)
        {
            var titles = new List<TitleEntry>();

            bool hasMostOffense = (leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown) > 0;
            bool hasHighestPoint = leaders.HighestPoint.Value.HighestPoint > 0;
            bool hasMostAirborneTime = leaders.MostAirborneTime.Value.AirborneTime > TimeSpan.Zero;
            bool hasMostDamageTaken = (leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost) > 0;
            bool hasMostShieldsLost = leaders.MostShieldsLost.Value.ShieldsLost > 0;
            bool hasMostFriendlyFire = (leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit) > 0;
            bool hasMostWebSwings = leaders.MostWebSwings.Value.WebSwings > 0;
            bool hasMostWaveClutches = leaders.MostWaveClutches.Value.WaveClutches > 0;
            bool hasMostKillsWhileSolo = leaders.MostKillsWhileSolo.Value.KillsWhileSolo > 0;
            bool hasMostAliveTime = leaders.MostAliveTime.Value.TotalAliveTime > TimeSpan.Zero;

            var expl = leaders.MostExplosionsKills.Value.WeaponHits;
            bool hasMostExplosionsKills = (expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]) > 0;

            var guns = leaders.MostGunsKills.Value.WeaponHits;
            bool hasMostGunsKills = (guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"]) > 0;

            var blades = leaders.MostBladeKills.Value.WeaponHits;
            bool hasMostBladeKills = (blades["Particle Blade"] + blades["KhepriStaff"]) > 0;

            bool hasMostHornetKills = leaders.MostHornetKills.Value.EnemyKills["Hornet"] > 0;
            bool hasMostWhispKills = (leaders.MostWhispKills.Value.EnemyKills["Whisp"] + leaders.MostWhispKills.Value.EnemyKills["Power Whisp"]) > 0;
            bool hasMostKhepriKills = (leaders.MostKhepriKills.Value.EnemyKills["Khepri"] + leaders.MostKhepriKills.Value.EnemyKills["Power Khepri"]) > 0;
            bool hasMostLavaDeaths = leaders.MostLavaDeaths.Value.LavaDeaths > 0;
            bool hasMostKillsWhileAirborne = leaders.MostKillsWhileAirborne.Value.KillsWhileAirborne > 0;
            bool hasMaxKillStreak = leaders.MaxKillStreak.Value.MaxKillStreak > 0;

            if (hasMostOffense && hasHighestPoint && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.HighestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.HighestPoint)
                    .WithName("Orbital Strike")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostAirborneTime && hasHighestPoint && TitleBuilder.SamePlayer(leaders.MostAirborneTime, leaders.HighestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostAirborneTime, Req.MostAirborneTime)
                    .AndLeader(Req.HighestPoint)
                    .WithName("Satellite")
                    .WithDescription($"Highest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && hasMostDamageTaken && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.MostDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostDamageTaken)
                    .WithName("Glass Cannon")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Sword and Shield")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.LeastOffense, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("Nothing Burger")
                    .WithDescription($"Least Offense ({leaders.LeastOffense.Value.Kills + leaders.LeastOffense.Value.EnemyShieldsTakenDown})\nLeast Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostShieldsLost &&
                TitleBuilder.SamePlayer(leaders.MostShieldsLost, leaders.LeastDeaths))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostShieldsLost, Req.MostShieldsLost)
                    .AndLeader(Req.LeastDeaths)
                    .WithName("On Death's Bed")
                    .WithDescription($"Most Shields Lost ({leaders.MostShieldsLost.Value.ShieldsLost})\nLeast Deaths ({leaders.LeastDeaths.Value.Deaths})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && hasMostFriendlyFire && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Perfectly Balanced")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostWebSwings && hasMostAirborneTime && TitleBuilder.SamePlayer(leaders.MostWebSwings, leaders.MostAirborneTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWebSwings, Req.MostWebSwings)
                    .AndLeader(Req.MostAirborneTime)
                    .WithName("Spider-Man")
                    .WithDescription($"Most Web Swings ({leaders.MostWebSwings.Value.WebSwings})\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LowestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LowestPoint)
                    .WithName("Lawn-mower")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nLowest Point ({leaders.LowestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostWebSwings && hasMostOffense && TitleBuilder.SamePlayer(leaders.MostWebSwings, leaders.MostOffense))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostWebSwings)
                    .WithName("Hit & Run")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Web Swings ({leaders.MostWebSwings.Value.WebSwings})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostWaveClutches && hasMostKillsWhileSolo && TitleBuilder.SamePlayer(leaders.MostWaveClutches, leaders.MostKillsWhileSolo))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWaveClutches, Req.MostWaveClutches)
                    .AndLeader(Req.MostKillsWhileSolo)
                    .WithName("Last Stand Hero")
                    .WithDescription($"Most Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches})\nMost Kills While Solo ({leaders.MostKillsWhileSolo.Value.KillsWhileSolo})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostExplosionsKills && hasMostDamageTaken && TitleBuilder.SamePlayer(leaders.MostExplosionsKills, leaders.MostDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.MostDamageTaken)
                    .WithName("Kamikaze")
                    .WithDescription($"Most Explosive Kills ({expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]})\nMost Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostGunsKills && hasMostOffense && TitleBuilder.SamePlayer(leaders.MostGunsKills, leaders.MostOffense))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostGunsKills, Req.MostGunsKills)
                    .AndLeader(Req.MostOffense)
                    .WithName("War Machine")
                    .WithDescription($"Most Gun Kills ({guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"]})\nMost Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostBladeKills && TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Silent Assassin")
                    .WithDescription($"Most Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostBladeKills && hasHighestPoint && TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.HighestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .AndLeader(Req.HighestPoint)
                    .WithName("I have the High Ground")
                    .WithDescription($"Most Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostDamageTaken && hasMostAliveTime && TitleBuilder.SamePlayer(leaders.MostDamageTaken, leaders.MostAliveTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostDamageTaken, Req.MostDamageTaken)
                    .AndLeader(Req.MostAliveTime)
                    .WithName("Nine Lives")
                    .WithDescription($"Most Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})\nMost Alive Time ({leaders.MostAliveTime.Value.TotalAliveTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostHornetKills && hasMostBladeKills && TitleBuilder.SamePlayer(leaders.MostHornetKills, leaders.MostBladeKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostHornetKills, Req.MostHornetKills)
                    .AndLeader(Req.MostBladeKills)
                    .WithName("Jedi Master")
                    .WithDescription($"Most Hornets Killed ({leaders.MostHornetKills.Value.EnemyKills["Hornet"]})\nMost Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostWhispKills && hasMostGunsKills && TitleBuilder.SamePlayer(leaders.MostWhispKills, leaders.MostGunsKills))
            {
                var whispKills = leaders.MostWhispKills.Value.EnemyKills["Whisp"] + leaders.MostWhispKills.Value.EnemyKills["Power Whisp"];
                var gunKills = guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"];

                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWhispKills, Req.MostWhispKills)
                    .AndLeader(Req.MostGunsKills)
                    .WithName("Sharpshooter")
                    .WithDescription($"Most Whisps Killed ({whispKills})\nMost Gun Kills ({gunKills})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostKhepriKills && hasMostDamageTaken && TitleBuilder.SamePlayer(leaders.MostKhepriKills, leaders.MostDamageTaken))
            {
                var khepriKills = leaders.MostKhepriKills.Value.EnemyKills["Khepri"] + leaders.MostKhepriKills.Value.EnemyKills["Power Khepri"];

                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostKhepriKills, Req.MostKhepriKills)
                    .AndLeader(Req.MostDamageTaken)
                    .WithName("Pharaoh")
                    .WithDescription($"Most Khepris Killed ({khepriKills})\nMost Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }


            if (hasMostAliveTime && TitleBuilder.SamePlayer(leaders.MostAliveTime, leaders.LowestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostAliveTime, Req.MostAliveTime)
                    .AndLeader(Req.LowestPoint)
                    .WithName("Cockroach")
                    .WithDescription($"Most Alive Time ({leaders.MostAliveTime.Value.TotalAliveTime.TotalSeconds:F1}s)\nLowest Point ({leaders.LowestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostLavaDeaths && hasHighestPoint && TitleBuilder.SamePlayer(leaders.MostLavaDeaths, leaders.HighestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostLavaDeaths, Req.MostLavaDeaths)
                    .AndLeader(Req.HighestPoint)
                    .WithName("Icarus")
                    .WithDescription($"Most Lava Deaths ({leaders.MostLavaDeaths.Value.LavaDeaths})\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostAliveTime && TitleBuilder.SamePlayer(leaders.LeastLavaDeaths, leaders.MostAliveTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastLavaDeaths, Req.LeastLavaDeaths)
                    .AndLeader(Req.MostAliveTime)
                    .WithName("Firewalker")
                    .WithDescription($"Least Lava Deaths ({leaders.LeastLavaDeaths.Value.LavaDeaths})\nMost Alive Time ({leaders.MostAliveTime.Value.TotalAliveTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostKillsWhileAirborne && TitleBuilder.SamePlayer(leaders.MostKillsWhileAirborne, leaders.LowestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostKillsWhileAirborne, Req.MostKillsWhileAirborne)
                    .AndLeader(Req.LowestPoint)
                    .WithName("Gravity Police")
                    .WithDescription($"Most Kills While Airborne ({leaders.MostKillsWhileAirborne.Value.KillsWhileAirborne})\nLowest Point ({leaders.LowestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.LeastWebSwings, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastWebSwings, Req.LeastWebSwings)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Slow and Steady")
                    .WithDescription($"Least Web Swings ({leaders.LeastWebSwings.Value.WebSwings})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMaxKillStreak && hasMostOffense && TitleBuilder.SamePlayer(leaders.MaxKillStreak, leaders.MostOffense))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MaxKillStreak, Req.MaxKillStreak)
                    .AndLeader(Req.MostOffense)
                    .WithName("Overkill")
                    .WithDescription($"Max Kill Streak ({leaders.MaxKillStreak.Value.MaxKillStreak})\nMost Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostShieldsLost && hasMostAliveTime && TitleBuilder.SamePlayer(leaders.MostShieldsLost, leaders.MostAliveTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostShieldsLost, Req.MostShieldsLost)
                    .AndLeader(Req.MostAliveTime)
                    .WithName("Insurance Policy")
                    .WithDescription($"Most Shields Lost ({leaders.MostShieldsLost.Value.ShieldsLost})\nMost Alive Time ({leaders.MostAliveTime.Value.TotalAliveTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }
            return titles;
        }

        private List<TitleEntry> CreateThreeCategoryTitles(StatLeaders leaders, int defaultPriority = 30)
        {
            var titles = new List<TitleEntry>();

            bool hasMostOffense = (leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown) > 0;
            bool hasHighestPoint = leaders.HighestPoint.Value.HighestPoint > 0;
            bool hasMostAirborneTime = leaders.MostAirborneTime.Value.AirborneTime > TimeSpan.Zero;
            bool hasMostDamageTaken = (leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost) > 0;
            bool hasMostFriendlyFire = (leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit) > 0;
            bool hasMostWebSwings = leaders.MostWebSwings.Value.WebSwings > 0;
            bool hasMostWaveClutches = leaders.MostWaveClutches.Value.WaveClutches > 0;
            bool hasMaxKillStreakWhileSolo = leaders.MaxKillStreakWhileSolo.Value.MaxKillStreakWhileSolo > 0;
            bool hasMostKillsWhileSolo = leaders.MostKillsWhileSolo.Value.KillsWhileSolo > 0;

            var expl = leaders.MostExplosionsKills.Value.WeaponHits;
            bool hasMostExplosionsKills = (expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]) > 0;

            var guns = leaders.MostGunsKills.Value.WeaponHits;
            bool hasMostGunsKills = (guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"]) > 0;

            var blades = leaders.MostBladeKills.Value.WeaponHits;
            bool hasMostBladeKills = (blades["Particle Blade"] + blades["KhepriStaff"]) > 0;

            bool hasMostHornetKills = leaders.MostHornetKills.Value.EnemyKills["Hornet"] > 0;

            if (hasMostExplosionsKills && hasHighestPoint && hasMostAirborneTime && TitleBuilder.SamePlayer(leaders.MostExplosionsKills, leaders.HighestPoint, leaders.MostAirborneTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.MostAirborneTime)
                    .WithName("ICBM")
                    .WithDescription($"Most Explosive Kills ({expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]})\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostBladeKills && hasHighestPoint && TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.HighestPoint, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Phantom Blade")
                    .WithDescription($"Most Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && hasMostDamageTaken && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.MostDamageTaken, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostDamageTaken)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("Elegant Barbarian")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})\nLeast Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("MVP")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})\nLeast Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && hasMostFriendlyFire && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Agent of Chaos")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMaxKillStreakWhileSolo && hasMostWaveClutches && hasMostFriendlyFire && TitleBuilder.SamePlayer(leaders.MaxKillStreakWhileSolo, leaders.MostWaveClutches, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostFriendlyFire, Req.MostFriendlyFire)
                    .AndLeader(Req.MaxKillStreakWhileSolo)
                    .AndLeader(Req.MostWaveClutches)
                    .WithName("Ordered Chaos")
                    .WithDescription($"Most Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})\nMost Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches})\nMax Kill Streak While Solo ({leaders.MaxKillStreakWhileSolo.Value.MaxKillStreakWhileSolo})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostFriendlyFire && TitleBuilder.SamePlayer(leaders.LeastOffense, leaders.LeastDamageTaken, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Traitor")
                    .WithDescription($"Least Offense ({leaders.LeastOffense.Value.Kills + leaders.LeastOffense.Value.EnemyShieldsTakenDown})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostDamageTaken && TitleBuilder.SamePlayer(leaders.MostDamageTaken, leaders.LeastOffense, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostDamageTaken, Req.MostDamageTaken)
                    .AndLeader(Req.LeastOffense)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("AFK")
                    .WithDescription($"Most Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})\nLeast Offense ({leaders.LeastOffense.Value.Kills + leaders.LeastOffense.Value.EnemyShieldsTakenDown})\nLeast Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostWebSwings && hasMostAirborneTime && hasMostDamageTaken && TitleBuilder.SamePlayer(leaders.MostWebSwings, leaders.MostAirborneTime, leaders.MostDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWebSwings, Req.MostWebSwings)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.MostDamageTaken)
                    .WithName("Spooderman")
                    .WithDescription($"Most Web Swings ({leaders.MostWebSwings.Value.WebSwings})\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nMost Damage Taken ({leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority + 10)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.LowestPoint, leaders.LeastAirborneTime, leaders.LeastWebSwings))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LowestPoint, Req.LowestPoint)
                    .AndLeader(Req.LeastAirborneTime)
                    .AndLeader(Req.LeastWebSwings)
                    .WithName("Basement Dweller")
                    .WithDescription($"Lowest Point ({leaders.LowestPoint.Value.HighestPoint:F1}m)\nLeast Airborne Time ({leaders.LeastAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nLeast Web Swings ({leaders.LeastWebSwings.Value.WebSwings})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostGunsKills && hasMostOffense && TitleBuilder.SamePlayer(leaders.MostGunsKills, leaders.MostOffense, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostGunsKills, Req.MostGunsKills)
                    .AndLeader(Req.MostOffense)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("Marksman")
                    .WithDescription($"Most Gun Kills ({guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"]})\nMost Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nLeast Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostExplosionsKills && hasMostOffense && hasMostFriendlyFire && TitleBuilder.SamePlayer(leaders.MostExplosionsKills, leaders.MostOffense, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.MostOffense)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Mutually Assured Destruction")
                    .WithDescription($"Most Explosive Kills ({expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]})\nMost Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostBladeKills && hasMostExplosionsKills && hasMostGunsKills && TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.MostExplosionsKills, leaders.MostGunsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.MostGunsKills)
                    .AndLeader(Req.MostBladeKills)
                    .WithName("Master of Arms")
                    .WithDescription($"Most Explosive Kills ({expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]})\nMost Gun Kills ({guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"]})\nMost Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostHornetKills && hasMostBladeKills && hasMostFriendlyFire && TitleBuilder.SamePlayer(leaders.MostHornetKills, leaders.MostBladeKills, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostHornetKills, Req.MostHornetKills)
                    .AndLeader(Req.MostBladeKills)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Sith Lord")
                    .WithDescription($"Most Hornets Killed ({leaders.MostHornetKills.Value.EnemyKills["Hornet"]})\nMost Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMaxKillStreakWhileSolo && hasMostKillsWhileSolo && hasMostWaveClutches && TitleBuilder.SamePlayer(leaders.MaxKillStreakWhileSolo, leaders.MostKillsWhileSolo, leaders.MostWaveClutches))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MaxKillStreakWhileSolo, Req.MaxKillStreakWhileSolo)
                    .AndLeader(Req.MostKillsWhileSolo)
                    .AndLeader(Req.MostWaveClutches)
                    .WithName("One Man Army")
                    .WithDescription($"Max Kill Streak While Solo ({leaders.MaxKillStreakWhileSolo.Value.MaxKillStreakWhileSolo})\nMost Kills While Solo ({leaders.MostKillsWhileSolo.Value.KillsWhileSolo})\nMost Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches})")
                    .WithPriority(defaultPriority + 10)
                    .Build());
            }

            if (hasMostWebSwings && hasMostAirborneTime && hasMostBladeKills && TitleBuilder.SamePlayer(leaders.MostWebSwings, leaders.MostAirborneTime, leaders.MostBladeKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWebSwings, Req.MostWebSwings)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.MostBladeKills)
                    .WithName("Iron Spider")
                    .WithDescription($"Most Web Swings ({leaders.MostWebSwings.Value.WebSwings})\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nMost Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            return titles;
        }

        private List<TitleEntry> CreateFourCategoryTitles(StatLeaders leaders, int defaultPriority = 40)
        {
            var titles = new List<TitleEntry>();

            bool hasMostOffense = (leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown) > 0;
            bool hasHighestPoint = leaders.HighestPoint.Value.HighestPoint > 0;
            bool hasMostAirborneTime = leaders.MostAirborneTime.Value.AirborneTime > TimeSpan.Zero;
            bool hasMostFriendlyFire = (leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit) > 0;
            bool hasMostWebSwings = leaders.MostWebSwings.Value.WebSwings > 0;

            var expl = leaders.MostExplosionsKills.Value.WeaponHits;
            bool hasMostExplosionsKills = (expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]) > 0;

            var guns = leaders.MostGunsKills.Value.WeaponHits;
            bool hasMostGunsKills = (guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"]) > 0;

            var blades = leaders.MostBladeKills.Value.WeaponHits;
            bool hasMostBladeKills = (blades["Particle Blade"] + blades["KhepriStaff"]) > 0;

            bool hasMostHornetKills = leaders.MostHornetKills.Value.EnemyKills["Hornet"] > 0;
            bool hasMostLavaDeaths = leaders.MostLavaDeaths.Value.LavaDeaths > 0;
            bool hasMostAliveTime = leaders.MostAliveTime.Value.TotalAliveTime > TimeSpan.Zero;
            bool hasMostKillsWhileAirborne = leaders.MostKillsWhileAirborne.Value.KillsWhileAirborne > 0;

            if (hasMostOffense && hasHighestPoint && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.HighestPoint, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("God Complex")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})\nLeast Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasHighestPoint && hasMostAirborneTime && hasMostWebSwings && TitleBuilder.SamePlayer(leaders.HighestPoint, leaders.MostAirborneTime, leaders.LeastDamageTaken, leaders.MostWebSwings))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.HighestPoint, Req.HighestPoint)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostWebSwings)
                    .WithName("The Untouchable")
                    .WithDescription($"Highest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nMost Web Swings ({leaders.MostWebSwings.Value.WebSwings})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && hasHighestPoint && hasMostAirborneTime && hasMostExplosionsKills && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.HighestPoint, leaders.MostAirborneTime, leaders.MostExplosionsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.MostExplosionsKills)
                    .WithName("Nuclear Warhead")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nMost Explosive Kills ({expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]})\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostFriendlyFire && hasMostExplosionsKills && TitleBuilder.SamePlayer(leaders.LeastOffense, leaders.LeastDamageTaken, leaders.MostFriendlyFire, leaders.MostExplosionsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostFriendlyFire)
                    .AndLeader(Req.MostExplosionsKills)
                    .WithName("Inside Job")
                    .WithDescription($"Least Offense ({leaders.LeastOffense.Value.Kills + leaders.LeastOffense.Value.EnemyShieldsTakenDown})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})\nMost Explosive Kills ({expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostOffense && hasMostGunsKills && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.LeastFriendlyFire, leaders.MostGunsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.LeastFriendlyFire)
                    .AndLeader(Req.MostGunsKills)
                    .WithName("Rambo")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Gun Kills ({guns["Shotgun"] + guns["RailShot"] + guns["DeathRay"] + guns["EnergyBall"] + guns["Laser Cannon"] + guns["SawDisc"]})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})\nLeast Friendly Fire ({leaders.LeastFriendlyFire.Value.FriendlyKills + leaders.LeastFriendlyFire.Value.FriendlyShieldsHit})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostHornetKills && hasMostBladeKills && hasMostFriendlyFire && hasMostLavaDeaths && TitleBuilder.SamePlayer(leaders.MostHornetKills, leaders.MostBladeKills, leaders.MostFriendlyFire, leaders.MostLavaDeaths))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostHornetKills, Req.MostHornetKills)
                    .AndLeader(Req.MostBladeKills)
                    .AndLeader(Req.MostFriendlyFire)
                    .AndLeader(Req.MostLavaDeaths)
                    .WithName("Darth Vader")
                    .WithDescription($"Most Hornets Killed ({leaders.MostHornetKills.Value.EnemyKills["Hornet"]})\nMost Blade Kills ({blades["Particle Blade"] + blades["KhepriStaff"]})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})\nMost Lava Deaths ({leaders.MostLavaDeaths.Value.LavaDeaths})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasMostAliveTime && TitleBuilder.SamePlayer(leaders.LowestPoint, leaders.LeastAirborneTime, leaders.LeastWebSwings, leaders.MostAliveTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LowestPoint, Req.LowestPoint)
                    .AndLeader(Req.LeastAirborneTime)
                    .AndLeader(Req.LeastWebSwings)
                    .AndLeader(Req.MostAliveTime)
                    .WithName("Bunker")
                    .WithDescription($"Lowest Point ({leaders.LowestPoint.Value.HighestPoint:F1}m)\nLeast Airborne Time ({leaders.LeastAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nLeast Web Swings ({leaders.LeastWebSwings.Value.WebSwings})\nMost Alive Time ({leaders.MostAliveTime.Value.TotalAliveTime.TotalSeconds:F1}s)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (hasHighestPoint && hasMostAirborneTime && hasMostKillsWhileAirborne && hasMostWebSwings && TitleBuilder.SamePlayer(leaders.HighestPoint, leaders.MostAirborneTime, leaders.MostKillsWhileAirborne, leaders.MostWebSwings))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.HighestPoint, Req.HighestPoint)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.MostKillsWhileAirborne)
                    .AndLeader(Req.MostWebSwings)
                    .WithName("Air Superiority")
                    .WithDescription($"Highest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)\nMost Airborne Time ({leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds:F1}s)\nMost Kills While Airborne ({leaders.MostKillsWhileAirborne.Value.KillsWhileAirborne})\nMost Web Swings ({leaders.MostWebSwings.Value.WebSwings})")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            return titles;
        }

        private List<TitleEntry> CreateFiveCategoryTitles(StatLeaders leaders, int defaultPriority = 50)
        {
            var titles = new List<TitleEntry>();

            bool hasMostOffense = (leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown) > 0;
            bool hasHighestPoint = leaders.HighestPoint.Value.HighestPoint > 0;
            bool hasMostFriendlyFire = (leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit) > 0;

            var expl = leaders.MostExplosionsKills.Value.WeaponHits;
            bool hasMostExplosionsKills = (expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]) > 0;

            if (hasMostOffense && hasHighestPoint && hasMostFriendlyFire && hasMostExplosionsKills && TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.HighestPoint, leaders.MostFriendlyFire, leaders.MostExplosionsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.MostFriendlyFire)
                    .AndLeader(Req.MostExplosionsKills)
                    .WithName("Supernova")
                    .WithDescription($"Most Offense ({leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown})\nMost Friendly Fire ({leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit})\nMost Explosive Kills ({expl["Explosions"] + expl["Laser Cube"] + expl["DeathCube"]})\nLeast Damage Taken ({leaders.LeastDamageTaken.Value.Deaths + leaders.LeastDamageTaken.Value.ShieldsLost})\nHighest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            return titles;
        }
    }
}
