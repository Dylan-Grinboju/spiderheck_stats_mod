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
            var titles = new List<TitleEntry>
            {
                new TitleEntry(leaders.MostWebSwings)
                {
                    TitleName = "The swinger",
                    Description = "most web swings",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings" }
                },
                new TitleEntry(leaders.HighestPoint)
                {
                    TitleName = "Sky Scraper",
                    Description = "highest point reached",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "HighestPoint" }
                },
                new TitleEntry(leaders.MostAirborneTime)
                {
                    TitleName = "Air Dancer",
                    Description = "most airborne time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostAirborneTime" }
                },
                new TitleEntry(leaders.MaxKillStreak)
                {
                    TitleName = "Serial Killer",
                    Description = $"max kill streak - {leaders.MaxKillStreak.Value.MaxKillStreak}",
                    Priority = 99, //This is fun to know so I am bumping it
                    Requirements = new HashSet<string> { "MaxKillStreak" }
                },
                new TitleEntry(leaders.MostAliveTime)
                {
                    TitleName = "Survivor",
                    Description = "most alive time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostAliveTime" }
                },
                new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Destroyer",
                    Description = "most offense",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense" }
                },
                new TitleEntry(leaders.MostDamageTaken)
                {
                    TitleName = "Punching Bag",
                    Description = "most damage taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostDamageTaken" }
                },
                new TitleEntry(leaders.LeastDamageTaken)
                {
                    TitleName = "Shadow",
                    Description = "least damage taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastDamageTaken" }
                },
                new TitleEntry(leaders.MostFriendlyFire)
                {
                    TitleName = "Confused",
                    Description = "most friendly fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostFriendlyFire" }
                },
                new TitleEntry(leaders.LeastFriendlyFire)
                {
                    TitleName = "Team Player",
                    Description = "least friendly fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastFriendlyFire" }
                },
                new TitleEntry(leaders.LeastOffense)
                {
                    TitleName = "Pacifist",
                    Description = "least offense",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense" }
                }
            };

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
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Orbital Strike",
                    Description = "highest altitude and most kills",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "HighestPoint" }
                });
            }

            if (airborneWinner == altitudeWinner)
            {
                titles.Add(new TitleEntry(leaders.MostAirborneTime)
                {
                    TitleName = "Satellite",
                    Description = "highest point for the longest time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "HighestPoint", "MostAirborneTime" }
                });
            }

            if (offenseWinner == damageWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Reckless",
                    Description = "most damage, to himself and enemies",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostDamageTaken" }
                });
            }

            if (offenseWinner == damageLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Assassin",
                    Description = "Untouchable and deadly",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken" }
                });
            }

            if (offenseLoser == damageLoser)
            {
                titles.Add(new TitleEntry(leaders.LeastOffense)
                {
                    TitleName = "Nothing Burger",
                    Description = "doesn't harm, doesn't help",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense", "LeastDamageTaken" }
                });
            }
            if (leaders.MostShieldsLost.Value.ShieldsLost > 0 && shieldsLostWinner == deathsLoser)
            {
                titles.Add(new TitleEntry(leaders.MostShieldsLost)
                {
                    TitleName = "On Death's Bed",
                    Description = "a constant near death experience",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostShieldsLost", "LeastDeaths" }
                });
            }
            if (offenseWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Destructive Power",
                    Description = "keep your distance, let it work",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostFriendlyFire" }
                });
            }

            var swingsWinner = leaders.MostWebSwings.Key;
            if (swingsWinner == airborneWinner)
            {
                titles.Add(new TitleEntry(leaders.MostWebSwings)
                {
                    TitleName = "Spider-Man",
                    Description = "most web swings and airborne time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings", "MostAirborneTime" }
                });
            }

            var altitudeLoser = leaders.LowestPoint.Key;
            if (offenseWinner == altitudeLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Lawn-mower",
                    Description = "stays low and fires up",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LowestPoint" }
                });
            }

            if (swingsWinner == offenseWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Hit & Run",
                    Description = "strike fast, escape faster",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostWebSwings" }
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

            if (offenseWinner == altitudeWinner && offenseWinner == airborneWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "ICBM",
                    Description = "strikes from above with deadly precision",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "HighestPoint", "MostAirborneTime" }
                });
            }

            if (swingsWinner == altitudeWinner && swingsWinner == damageLoser)
            {
                titles.Add(new TitleEntry(leaders.MostWebSwings)
                {
                    TitleName = "Ninja",
                    Description = "High above and untouchable",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings", "HighestPoint", "LeastDamageTaken" }
                });
            }

            if (offenseWinner == damageWinner && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Elegant Barbarian",
                    Description = "trust him",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostDamageTaken", "LeastFriendlyFire" }
                });
            }

            if (offenseWinner == damageLoser && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "MVP",
                    Description = "the team relies on you",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "LeastFriendlyFire" }
                });
            }

            if (offenseWinner == damageLoser && offenseWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Ordered Chaos",
                    Description = "a necessary evil",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "MostFriendlyFire" }
                });
            }

            if (offenseLoser == damageLoser && offenseLoser == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.LeastOffense)
                {
                    TitleName = "Traitor",
                    Description = "watch your back",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense", "LeastDamageTaken", "MostFriendlyFire" }
                });
            }

            if (swingsWinner == airborneWinner && swingsWinner == damageWinner)
            {
                titles.Add(new TitleEntry(leaders.MostWebSwings)
                {
                    TitleName = "Spooderman",
                    Description = "spidey senses not working",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings", "MostAirborneTime", "MostDamageTaken" }
                });
            }

            var altitudeLoser = leaders.LowestPoint.Key;
            var airborneLoser = leaders.LeastAirborneTime.Key;
            var swingsLoser = leaders.LeastWebSwings.Key;
            if (altitudeLoser == airborneLoser && altitudeLoser == swingsLoser)
            {
                titles.Add(new TitleEntry(leaders.LowestPoint)
                {
                    TitleName = "Basement Dweller",
                    Description = "lives in the shadows below",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LowestPoint", "LeastAirborneTime", "LeastWebSwings" }
                });
            }

            return titles;
        }

        private List<TitleEntry> CreateFourCategoryTitles(StatLeaders leaders, int defaultPriority = 40)
        {
            var titles = new List<TitleEntry>();

            var offenseWinner = leaders.MostOffense.Key;
            var damageLoser = leaders.LeastDamageTaken.Key;
            var altitudeWinner = leaders.HighestPoint.Key;
            var friendlyFireLoser = leaders.LeastFriendlyFire.Key;
            var airborneWinner = leaders.MostAirborneTime.Key;
            var swingsWinner = leaders.MostWebSwings.Key;

            if (offenseWinner == damageLoser && offenseWinner == altitudeWinner && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "God Complex",
                    Description = "untouchable perfection",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "HighestPoint", "LeastFriendlyFire" }
                });
            }

            if (altitudeWinner == airborneWinner && altitudeWinner == damageLoser && altitudeWinner == swingsWinner)
            {
                titles.Add(new TitleEntry(leaders.HighestPoint)
                {
                    TitleName = "The Untouchable",
                    Description = "master of evasion",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "HighestPoint", "MostAirborneTime", "LeastDamageTaken", "MostWebSwings" }
                });
            }

            return titles;
        }
    }
}
