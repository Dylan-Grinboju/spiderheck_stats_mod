using UnityEngine;
using UnityEngine.InputSystem;
using Silk;
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
        public const string MostGunsKills = nameof(StatLeaders.MostGunsKills);
        public const string MostExplosionsKills = nameof(StatLeaders.MostExplosionsKills);
        public const string MostBladeKills = nameof(StatLeaders.MostBladeKills);
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

        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostGunsKills { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostExplosionsKills { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostBladeKills { get; set; }
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
            BalanceTitlePriorities();

            currentTitles = currentTitles.OrderByDescending(t => t.Priority).ToList();

            hasGameEndedTitles = currentTitles.Count > 0;
            Logger.LogInfo($"Calculated {currentTitles.Count} titles for {players.Count} players");
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

            var gunsRanked = players.OrderByDescending(p => p.Value.WeaponHits["Shotgun"] + p.Value.WeaponHits["RailShot"] + p.Value.WeaponHits["DeathRay"] + p.Value.WeaponHits["EnergyBall"] + p.Value.WeaponHits["Laser Cannon"] + p.Value.WeaponHits["SawDisc"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostGunsKills = gunsRanked[0];

            var explosionsRanked = players.OrderByDescending(p => p.Value.WeaponHits["Explosions"] + p.Value.WeaponHits["Laser Cube"] + p.Value.WeaponHits["DeathCube"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostExplosionsKills = explosionsRanked[0];

            var bladeRanked = players.OrderByDescending(p => p.Value.WeaponHits["Particle Blade"] + p.Value.WeaponHits["KhepriStaff"]).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MostBladeKills = bladeRanked[0];

            return leaders;
        }

        public void ClearTitles()
        {
            currentTitles.Clear();
            hasGameEndedTitles = false;
        }


        private void RemoveDominatedTitles()
        {
            var titlesByPlayer = currentTitles.GroupBy(t => t.Player);
            var toRemove = new List<TitleEntry>();

            foreach (var playerTitles in titlesByPlayer)
            {
                var titles = playerTitles.ToList();

                foreach (var title in titles)
                {
                    var isDominated = titles.Any(other =>
                        other != title &&
                        title.Requirements.IsSubsetOf(other.Requirements) &&
                        other.Requirements.Count > title.Requirements.Count);

                    if (isDominated)
                        toRemove.Add(title);
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
            var titles = new List<TitleEntry>
            {
                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWebSwings, Req.MostWebSwings)
                    .WithName("Peter Parker")
                    .WithDescription("Most Web Swings")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.HighestPoint, Req.HighestPoint)
                    .WithName(leaders.HighestPoint.Value.HighestPoint >= 1000 ? "1000 Meters Club" : "Sky Scraper")
                    .WithDescription($"Highest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)")
                    .WithPriority(leaders.HighestPoint.Value.HighestPoint >= 1000 ? 25 : defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostAirborneTime, Req.MostAirborneTime)
                    .WithName("Air Dancer")
                    .WithDescription("Most Airborne Time")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostKillsWhileAirborne, Req.MostKillsWhileAirborne)
                    .WithName("Sky Hunter")
                    .WithDescription("Most Kills While Airborne")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostKillsWhileSolo, Req.MostKillsWhileSolo)
                    .WithName("Lone Wolf")
                    .WithDescription($"Most Kills While Solo ({leaders.MostKillsWhileSolo.Value.KillsWhileSolo})")
                    .WithPriority(defaultPriority + 10)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWaveClutches, Req.MostWaveClutches)
                    .WithName("Clutch Master")
                    .WithDescription($"Most Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches})")
                    .WithPriority(defaultPriority + 10)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MaxKillStreak, Req.MaxKillStreak)
                    .WithName("Serial Killer")
                    .WithDescription($"Max Kill Streak ({leaders.MaxKillStreak.Value.MaxKillStreak})")
                    .WithPriority(90) // This is fun to know so bumping it
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MaxKillStreakWhileSolo, Req.MaxKillStreakWhileSolo)
                    .WithName("Solo Rampage")
                    .WithDescription($"Max Kill Streak While Solo ({leaders.MaxKillStreakWhileSolo.Value.MaxKillStreakWhileSolo})")
                    .WithPriority(95)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostAliveTime, Req.MostAliveTime)
                    .WithName("Survivor")
                    .WithDescription("Most Alive Time")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .WithName("Destroyer")
                    .WithDescription("Most Offense")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostDamageTaken, Req.MostDamageTaken)
                    .WithName("Punching Bag")
                    .WithDescription("Most Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastDamageTaken, Req.LeastDamageTaken)
                    .WithName("Shadow")
                    .WithDescription("Least Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostFriendlyFire, Req.MostFriendlyFire)
                    .WithName("Confused")
                    .WithDescription("Most Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastFriendlyFire, Req.LeastFriendlyFire)
                    .WithName("Team Player")
                    .WithDescription("Least Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                    .WithName("Pacifist")
                    .WithDescription("Least Offense")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostGunsKills, Req.MostGunsKills)
                    .WithName("Gunslinger")
                    .WithDescription("Most Gun Kills")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .WithName("Demolitionist")
                    .WithDescription("Most Explosive Kills")
                    .WithPriority(defaultPriority)
                    .Build(),

                new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .WithName("Blade Master")
                    .WithDescription("Most Blade Kills")
                    .WithPriority(defaultPriority)
                    .Build()
            };

            return titles;
        }

        private List<TitleEntry> CreateTwoCategoryTitles(StatLeaders leaders, int defaultPriority = 20)
        {
            var titles = new List<TitleEntry>();

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.HighestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.HighestPoint)
                    .WithName("Orbital Strike")
                    .WithDescription("Most Offense, Highest Point")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostAirborneTime, leaders.HighestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostAirborneTime, Req.MostAirborneTime)
                    .AndLeader(Req.HighestPoint)
                    .WithName("Satellite")
                    .WithDescription("Highest Point, Most Airborne Time")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.MostDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostDamageTaken)
                    .WithName("Glass Cannon")
                    .WithDescription("Most Offense, Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Sword and Shield")
                    .WithDescription("Most Offense, Least Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.LeastOffense, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Nothing Burger")
                    .WithDescription("Least Offense, Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (leaders.MostShieldsLost.Value.ShieldsLost > 0 && 
                TitleBuilder.SamePlayer(leaders.MostShieldsLost, leaders.LeastDeaths))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostShieldsLost, Req.MostShieldsLost)
                    .AndLeader(Req.LeastDeaths)
                    .WithName("On Death's Bed")
                    .WithDescription("Most Shields Lost, Least Deaths")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Perfectly Balanced")
                    .WithDescription("Most Offense, Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostWebSwings, leaders.MostAirborneTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWebSwings, Req.MostWebSwings)
                    .AndLeader(Req.MostAirborneTime)
                    .WithName("Spider-Man")
                    .WithDescription("Most Web Swings, Airborne Time")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LowestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LowestPoint)
                    .WithName("Lawn-mower")
                    .WithDescription("Most Offense, Lowest Point")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostWebSwings, leaders.MostOffense))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostWebSwings)
                    .WithName("Hit & Run")
                    .WithDescription("Most Offense, Web Swings")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostWaveClutches, leaders.MostKillsWhileSolo))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWaveClutches, Req.MostWaveClutches)
                    .AndLeader(Req.MostKillsWhileSolo)
                    .WithName("Last Stand Hero")
                    .WithDescription($"Most Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches}), Kills While Solo ({leaders.MostKillsWhileSolo.Value.KillsWhileSolo})")
                    .WithPriority(defaultPriority + 20)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostExplosionsKills, leaders.MostDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.MostDamageTaken)
                    .WithName("Kamikaze")
                    .WithDescription("Most Explosive Kills, Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostGunsKills, leaders.MostOffense))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostGunsKills, Req.MostGunsKills)
                    .AndLeader(Req.MostOffense)
                    .WithName("War Machine")
                    .WithDescription("Most Gun Kills, Offense")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Silent Assassin")
                    .WithDescription("Most Blade Kills, Least Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.HighestPoint))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .AndLeader(Req.HighestPoint)
                    .WithName("I have the High Ground")
                    .WithDescription("Most Blade Kills, Highest Point")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostDamageTaken, leaders.MostAliveTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostDamageTaken, Req.MostDamageTaken)
                    .AndLeader(Req.MostAliveTime)
                    .WithName("Nine Lives")
                    .WithDescription("Most Damage Taken, Alive Time")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            return titles;
        }

        private List<TitleEntry> CreateThreeCategoryTitles(StatLeaders leaders, int defaultPriority = 30)
        {
            var titles = new List<TitleEntry>();

            if (TitleBuilder.SamePlayer(leaders.MostExplosionsKills, leaders.HighestPoint, leaders.MostAirborneTime))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.MostAirborneTime)
                    .WithName("ICBM")
                    .WithDescription("Most explosion Kills, Airborne Time, Highest Point")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.HighestPoint, leaders.LeastDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostBladeKills, Req.MostBladeKills)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.LeastDamageTaken)
                    .WithName("Phantom Blade")
                    .WithDescription("Most blade kills, Highest Point, Least Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.MostDamageTaken, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.MostDamageTaken)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("Elegant Barbarian")
                    .WithDescription("Most Offense, Damage Taken, Least Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("MVP")
                    .WithDescription("Most Offense, Least Damage Taken, Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Agent of Chaos")
                    .WithDescription("Most Offense, Friendly Fire, Least Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MaxKillStreakWhileSolo, leaders.MostWaveClutches, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostFriendlyFire, Req.MostFriendlyFire)
                    .AndLeader(Req.MaxKillStreakWhileSolo)
                    .AndLeader(Req.MostWaveClutches)
                    .WithName("Ordered Chaos")
                    .WithDescription("Most Friendly Fire, Wave Clutches, Max Kill Streak While Solo")
                    .WithPriority(defaultPriority + 10)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.LeastOffense, leaders.LeastDamageTaken, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Traitor")
                    .WithDescription("Least Offense, Damage Taken, Most Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostWebSwings, leaders.MostAirborneTime, leaders.MostDamageTaken))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostWebSwings, Req.MostWebSwings)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.MostDamageTaken)
                    .WithName("Spooderman")
                    .WithDescription("Most Web Swings, Airborne Time, Damage Taken")
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
                    .WithDescription("Lowest Point, Least Airborne Time, Web Swings")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostGunsKills, leaders.MostOffense, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostGunsKills, Req.MostGunsKills)
                    .AndLeader(Req.MostOffense)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("Marksman")
                    .WithDescription("Most Gun Kills, Offense, Least Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostExplosionsKills, leaders.MostOffense, leaders.MostFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.MostOffense)
                    .AndLeader(Req.MostFriendlyFire)
                    .WithName("Mutually Assured Destruction")
                    .WithDescription("Most Explosive Kills, Offense, Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostBladeKills, leaders.MostExplosionsKills, leaders.MostGunsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostExplosionsKills, Req.MostExplosionsKills)
                    .AndLeader(Req.MostGunsKills)
                    .AndLeader(Req.MostBladeKills)
                    .WithName("Master of Arms")
                    .WithDescription("Most Explosive Kills, Gun Kills, Blade Kills")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            return titles;
        }

        private List<TitleEntry> CreateFourCategoryTitles(StatLeaders leaders, int defaultPriority = 40)
        {
            var titles = new List<TitleEntry>();

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.HighestPoint, leaders.LeastFriendlyFire))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.LeastFriendlyFire)
                    .WithName("God Complex")
                    .WithDescription("Most Offense, Least Damage Taken, Friendly Fire, Highest Point")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.HighestPoint, leaders.MostAirborneTime, leaders.LeastDamageTaken, leaders.MostWebSwings))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.HighestPoint, Req.HighestPoint)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostWebSwings)
                    .WithName("The Untouchable")
                    .WithDescription("Highest Point, Most Airborne Time, Web Swings, Least Damage Taken")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.HighestPoint, leaders.MostAirborneTime, leaders.MostExplosionsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.MostAirborneTime)
                    .AndLeader(Req.MostExplosionsKills)
                    .WithName("Nuclear Warhead")
                    .WithDescription("Most Offense, Airborne Time, Explosive Kills, Highest Point")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.LeastOffense, leaders.LeastDamageTaken, leaders.MostFriendlyFire, leaders.MostExplosionsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.LeastOffense, Req.LeastOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.MostFriendlyFire)
                    .AndLeader(Req.MostExplosionsKills)
                    .WithName("Inside Job")
                    .WithDescription("Least Offense, Damage Taken, Most Friendly Fire, Explosive Kills")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.LeastFriendlyFire, leaders.MostGunsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.LeastFriendlyFire)
                    .AndLeader(Req.MostGunsKills)
                    .WithName("Rambo")
                    .WithDescription("Most Offense, Gun Kills, Least Damage Taken, Friendly Fire")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            return titles;
        }

        private List<TitleEntry> CreateFiveCategoryTitles(StatLeaders leaders, int defaultPriority = 50)
        {
            var titles = new List<TitleEntry>();

            if (TitleBuilder.SamePlayer(leaders.MostOffense, leaders.LeastDamageTaken, leaders.HighestPoint, leaders.MostFriendlyFire, leaders.MostExplosionsKills))
            {
                titles.Add(new TitleBuilder(leaders)
                    .ForLeader(l => l.MostOffense, Req.MostOffense)
                    .AndLeader(Req.LeastDamageTaken)
                    .AndLeader(Req.HighestPoint)
                    .AndLeader(Req.MostFriendlyFire)
                    .AndLeader(Req.MostExplosionsKills)
                    .WithName("Supernova")
                    .WithDescription("Most Offense, Friendly Fire, Explosive Kills, Least Damage Taken, Highest Point")
                    .WithPriority(defaultPriority)
                    .Build());
            }

            return titles;
        }
    }
}
