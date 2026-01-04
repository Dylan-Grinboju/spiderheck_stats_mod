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
        public int Priority { get; set; }
        public HashSet<string> Requirements { get; set; } = new HashSet<string>();
    }

    public class StatLeaders
    {
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostWebSwings { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastWebSwings { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> HighestPoint { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LowestPoint { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MostAirborneTime { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> LeastAirborneTime { get; set; }
        public KeyValuePair<PlayerInput, PlayerTracker.PlayerData> MaxKillStreak { get; set; }
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
            var leaders = CalculateStatLeaders(players);

            var oneCategoryTitles = CreateOneCategoryTitles(leaders);
            currentTitles.AddRange(oneCategoryTitles);

            var twoCategoryTitles = CreateTwoCategoryTitles(leaders);
            currentTitles.AddRange(twoCategoryTitles);

            var threeCategoryTitles = CreateThreeCategoryTitles(leaders);
            currentTitles.AddRange(threeCategoryTitles);

            var fourCategoryTitles = CreateFourCategoryTitles(leaders);
            currentTitles.AddRange(fourCategoryTitles);

            RemoveDominatedTitles();

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

            var streakRanked = players.OrderByDescending(p => p.Value.MaxKillStreak).ThenByDescending(p => p.Value.TotalAliveTime).ToList();
            leaders.MaxKillStreak = streakRanked[0];

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


        private List<TitleEntry> CreateOneCategoryTitles(StatLeaders leaders, int defaultPriority = 10)
        {
            var titles = new List<TitleEntry>();

            if (leaders.MostWebSwings.Value.WebSwings > 0)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "The swinger",
                    Description = "most web swings",
                    PlayerName = leaders.MostWebSwings.Value.PlayerName,
                    PrimaryColor = leaders.MostWebSwings.Value.PlayerColor,
                    SecondaryColor = leaders.MostWebSwings.Value.SecondaryColor,
                    Player = leaders.MostWebSwings.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings" }
                });
            }

            titles.Add(new TitleEntry
            {
                TitleName = "Sky Scraper",
                Description = "highest point reached",
                PlayerName = leaders.HighestPoint.Value.PlayerName,
                PrimaryColor = leaders.HighestPoint.Value.PlayerColor,
                SecondaryColor = leaders.HighestPoint.Value.SecondaryColor,
                Player = leaders.HighestPoint.Key,
                Priority = defaultPriority,
                Requirements = new HashSet<string> { "HighestPoint" }
            });

            if (leaders.MostAirborneTime.Value.AirborneTime.TotalSeconds > 0)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Air Dancer",
                    Description = "most airborne time",
                    PlayerName = leaders.MostAirborneTime.Value.PlayerName,
                    PrimaryColor = leaders.MostAirborneTime.Value.PlayerColor,
                    SecondaryColor = leaders.MostAirborneTime.Value.SecondaryColor,
                    Player = leaders.MostAirborneTime.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostAirborneTime" }
                });
            }

            if (leaders.MaxKillStreak.Value.MaxKillStreak > 0)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Serial Killer",
                    Description = "max kill streak",
                    PlayerName = leaders.MaxKillStreak.Value.PlayerName,
                    PrimaryColor = leaders.MaxKillStreak.Value.PlayerColor,
                    SecondaryColor = leaders.MaxKillStreak.Value.SecondaryColor,
                    Player = leaders.MaxKillStreak.Key,
                    Priority = 99, //This is fun to know so I am bumping it
                    Requirements = new HashSet<string> { "MaxKillStreak" }
                });
            }

            titles.Add(new TitleEntry
            {
                TitleName = "Survivor",
                Description = "most alive time",
                PlayerName = leaders.MostAliveTime.Value.PlayerName,
                PrimaryColor = leaders.MostAliveTime.Value.PlayerColor,
                SecondaryColor = leaders.MostAliveTime.Value.SecondaryColor,
                Player = leaders.MostAliveTime.Key,
                Priority = defaultPriority,
                Requirements = new HashSet<string> { "MostAliveTime" }
            });

            var mostOffenseValue = leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown;
            if (mostOffenseValue > 0)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Destroyer",
                    Description = "most offense",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense" }
                });
            }

            var mostDamageValue = leaders.MostDamageTaken.Value.Deaths + leaders.MostDamageTaken.Value.ShieldsLost;
            if (mostDamageValue > 0)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Punching Bag",
                    Description = "most damage taken",
                    PlayerName = leaders.MostDamageTaken.Value.PlayerName,
                    PrimaryColor = leaders.MostDamageTaken.Value.PlayerColor,
                    SecondaryColor = leaders.MostDamageTaken.Value.SecondaryColor,
                    Player = leaders.MostDamageTaken.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostDamageTaken" }
                });
            }

            titles.Add(new TitleEntry
            {
                TitleName = "Shadow",
                Description = "least damage taken",
                PlayerName = leaders.LeastDamageTaken.Value.PlayerName,
                PrimaryColor = leaders.LeastDamageTaken.Value.PlayerColor,
                SecondaryColor = leaders.LeastDamageTaken.Value.SecondaryColor,
                Player = leaders.LeastDamageTaken.Key,
                Priority = defaultPriority,
                Requirements = new HashSet<string> { "LeastDamageTaken" }
            });

            var mostFriendlyFireValue = leaders.MostFriendlyFire.Value.FriendlyKills + leaders.MostFriendlyFire.Value.FriendlyShieldsHit;
            if (mostFriendlyFireValue > 0)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Confused",
                    Description = "most friendly fire",
                    PlayerName = leaders.MostFriendlyFire.Value.PlayerName,
                    PrimaryColor = leaders.MostFriendlyFire.Value.PlayerColor,
                    SecondaryColor = leaders.MostFriendlyFire.Value.SecondaryColor,
                    Player = leaders.MostFriendlyFire.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostFriendlyFire" }
                });
            }

            titles.Add(new TitleEntry
            {
                TitleName = "Team Player",
                Description = "least friendly fire",
                PlayerName = leaders.LeastFriendlyFire.Value.PlayerName,
                PrimaryColor = leaders.LeastFriendlyFire.Value.PlayerColor,
                SecondaryColor = leaders.LeastFriendlyFire.Value.SecondaryColor,
                Player = leaders.LeastFriendlyFire.Key,
                Priority = defaultPriority,
                Requirements = new HashSet<string> { "LeastFriendlyFire" }
            });

            var leastOffenseValue = leaders.LeastOffense.Value.Kills + leaders.LeastOffense.Value.EnemyShieldsTakenDown;
            if (leastOffenseValue == 0)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Pacifist",
                    Description = "least offense",
                    PlayerName = leaders.LeastOffense.Value.PlayerName,
                    PrimaryColor = leaders.LeastOffense.Value.PlayerColor,
                    SecondaryColor = leaders.LeastOffense.Value.SecondaryColor,
                    Player = leaders.LeastOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense" }
                });
            }

            return titles;
        }

        private List<TitleEntry> CreateTwoCategoryTitles(StatLeaders leaders, int defaultPriority = 20)
        {
            var titles = new List<TitleEntry>();

            var friendlyFireWinner = leaders.MostFriendlyFire.Key;
            var offenseWinner = leaders.MostOffense.Key;
            var offenseLoser = leaders.LeastOffense.Key;
            var altitudeWinner = leaders.HighestPoint.Key;
            var damageWinner = leaders.MostDamageTaken.Key;
            var damageLoser = leaders.LeastDamageTaken.Key;
            var shieldsLostWinner = leaders.MostShieldsLost.Key;
            var deathsLoser = leaders.LeastDeaths.Key;
            var airborneWinner = leaders.MostAirborneTime.Key;

            if (offenseWinner == altitudeWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Orbital Strike",
                    Description = "highest altitude and most kills",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "HighestPoint" }
                });
            }

            if (airborneWinner == altitudeWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Satellite",
                    Description = "highest point for the longest time",
                    PlayerName = leaders.MostAirborneTime.Value.PlayerName,
                    PrimaryColor = leaders.MostAirborneTime.Value.PlayerColor,
                    SecondaryColor = leaders.MostAirborneTime.Value.SecondaryColor,
                    Player = leaders.MostAirborneTime.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "HighestPoint", "MostAirborneTime" }
                });
            }

            if (offenseWinner == damageWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Reckless",
                    Description = "most damage, to himself and enemies",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostDamageTaken" }
                });
            }

            if (offenseWinner == damageLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Assassin",
                    Description = "Untouchable and deadly",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken" }
                });
            }

            if (offenseLoser == damageLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Nothing Burger",
                    Description = "doesn't harm, doesn't help",
                    PlayerName = leaders.LeastOffense.Value.PlayerName,
                    PrimaryColor = leaders.LeastOffense.Value.PlayerColor,
                    SecondaryColor = leaders.LeastOffense.Value.SecondaryColor,
                    Player = leaders.LeastOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense", "LeastDamageTaken" }
                });
            }
            if (leaders.MostShieldsLost.Value.ShieldsLost > 0 && shieldsLostWinner == deathsLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "On Death's Bed",
                    Description = "a constant near death experience",
                    PlayerName = leaders.MostShieldsLost.Value.PlayerName,
                    PrimaryColor = leaders.MostShieldsLost.Value.PlayerColor,
                    SecondaryColor = leaders.MostShieldsLost.Value.SecondaryColor,
                    Player = leaders.MostShieldsLost.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostShieldsLost", "LeastDeaths" }
                });
            }
            if (offenseWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Destructive Power",
                    Description = "keep your distance, let it work",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostFriendlyFire" }
                });
            }

            var swingsWinner = leaders.MostWebSwings.Key;
            if (leaders.MostWebSwings.Value.WebSwings > 0 && swingsWinner == airborneWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Spider-Man",
                    Description = "most web swings and airborne time",
                    PlayerName = leaders.MostWebSwings.Value.PlayerName,
                    PrimaryColor = leaders.MostWebSwings.Value.PlayerColor,
                    SecondaryColor = leaders.MostWebSwings.Value.SecondaryColor,
                    Player = leaders.MostWebSwings.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings", "MostAirborneTime" }
                });
            }

            var altitudeLoser = leaders.LowestPoint.Key;
            if (offenseWinner == altitudeLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Lawn-mower",
                    Description = "stays low and fires up",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LowestPoint" }
                });
            }

            return titles;
        }

        private List<TitleEntry> CreateThreeCategoryTitles(StatLeaders leaders, int defaultPriority = 30)
        {
            var titles = new List<TitleEntry>();

            var offenseWinner = leaders.MostOffense.Key;
            var offenseLoser = leaders.LeastOffense.Key;
            var altitudeWinner = leaders.HighestPoint.Key;
            var airborneWinner = leaders.MostAirborneTime.Key;
            var damageWinner = leaders.MostDamageTaken.Key;
            var damageLoser = leaders.LeastDamageTaken.Key;
            var friendlyFireWinner = leaders.MostFriendlyFire.Key;
            var friendlyFireLoser = leaders.LeastFriendlyFire.Key;
            var swingsWinner = leaders.MostWebSwings.Key;

            if (leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown > 0 &&
                offenseWinner == altitudeWinner && offenseWinner == airborneWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "ICBM",
                    Description = "highest altitude, airborne time and most kills",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "HighestPoint", "MostAirborneTime" }
                });
            }

            if (leaders.MostWebSwings.Value.WebSwings > 0 &&
                swingsWinner == altitudeWinner && swingsWinner == damageLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Ninja",
                    Description = "avoids everything",
                    PlayerName = leaders.MostWebSwings.Value.PlayerName,
                    PrimaryColor = leaders.MostWebSwings.Value.PlayerColor,
                    SecondaryColor = leaders.MostWebSwings.Value.SecondaryColor,
                    Player = leaders.MostWebSwings.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings", "HighestPoint", "LeastDamageTaken" }
                });
            }

            if (offenseWinner == damageWinner && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Elegant Barbarian",
                    Description = "trust him",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostDamageTaken", "LeastFriendlyFire" }
                });
            }

            if (offenseWinner == damageLoser && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "MVP",
                    Description = "the team relies on you",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "LeastFriendlyFire" }
                });
            }

            if (offenseWinner == damageLoser && offenseWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Ordered Chaos",
                    Description = "a necessary evil",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "MostFriendlyFire" }
                });
            }

            if (offenseLoser == damageLoser && offenseLoser == friendlyFireWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Traitor",
                    Description = "watch your back",
                    PlayerName = leaders.LeastOffense.Value.PlayerName,
                    PrimaryColor = leaders.LeastOffense.Value.PlayerColor,
                    SecondaryColor = leaders.LeastOffense.Value.SecondaryColor,
                    Player = leaders.LeastOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense", "LeastDamageTaken", "MostFriendlyFire" }
                });
            }

            if (swingsWinner == airborneWinner && swingsWinner == damageWinner)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "Spooderman",
                    Description = "spidey senses not working",
                    PlayerName = leaders.MostWebSwings.Value.PlayerName,
                    PrimaryColor = leaders.MostWebSwings.Value.PlayerColor,
                    SecondaryColor = leaders.MostWebSwings.Value.SecondaryColor,
                    Player = leaders.MostWebSwings.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings", "MostAirborneTime", "MostDamageTaken" }
                });
            }

            return titles;
        }

        private List<TitleEntry> CreateFourCategoryTitles(StatLeaders leaders, int defaultPriority = 50)
        {
            var titles = new List<TitleEntry>();

            var offenseWinner = leaders.MostOffense.Key;
            var damageLoser = leaders.LeastDamageTaken.Key;
            var altitudeWinner = leaders.HighestPoint.Key;
            var friendlyFireLoser = leaders.LeastFriendlyFire.Key;

            if (leaders.MostOffense.Value.Kills + leaders.MostOffense.Value.EnemyShieldsTakenDown > 0 &&
                offenseWinner == damageLoser && offenseWinner == altitudeWinner && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry
                {
                    TitleName = "God Complex",
                    Description = "untouchable perfection",
                    PlayerName = leaders.MostOffense.Value.PlayerName,
                    PrimaryColor = leaders.MostOffense.Value.PlayerColor,
                    SecondaryColor = leaders.MostOffense.Value.SecondaryColor,
                    Player = leaders.MostOffense.Key,
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "HighestPoint", "LeastFriendlyFire" }
                });
            }

            return titles;
        }
    }
}
