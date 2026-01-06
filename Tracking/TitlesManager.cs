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
        private static TitlesManager _instance;
        public static TitlesManager Instance => _instance ?? (_instance = new TitlesManager());

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
            for (int i = 0; i < currentTitles.Count; i++)
            {
                var currentTitle = currentTitles[i];
                for (int j = 0; j < currentTitles.Count; j++)
                {
                    if (i != j && currentTitles[j].Player != currentTitle.Player)
                    {
                        currentTitles[j].Priority += 5;
                    }
                }
            }
        }

        private List<TitleEntry> CreateOneCategoryTitles(StatLeaders leaders, int defaultPriority = 10)
        {
            var titles = new List<TitleEntry>
            {
                new TitleEntry(leaders.MostWebSwings)
                {
                    TitleName = "Peter Parker",
                    Description = "Most Web Swings",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostWebSwings" }
                },
                new TitleEntry(leaders.HighestPoint)
                {
                    TitleName = leaders.HighestPoint.Value.HighestPoint >= 1000 ? "1000 Meters Club" : "Sky Scraper",
                    Description = $"Highest Point ({leaders.HighestPoint.Value.HighestPoint:F1}m)",
                    Priority = leaders.HighestPoint.Value.HighestPoint >= 1000 ? 25 : defaultPriority,
                    Requirements = new HashSet<string> { "HighestPoint" }
                },
                new TitleEntry(leaders.MostAirborneTime)
                {
                    TitleName = "Air Dancer",
                    Description = "Most Airborne Time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostAirborneTime" }
                },
                new TitleEntry(leaders.MostKillsWhileAirborne)
                {
                    TitleName = "Sky Hunter",
                    Description = "Most Kills While Airborne",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostKillsWhileAirborne" }
                },
                new TitleEntry(leaders.MostKillsWhileSolo)
                {
                    TitleName = "Lone Wolf",
                    Description = $"Most Kills While Solo ({leaders.MostKillsWhileSolo.Value.KillsWhileSolo})",
                    Priority = defaultPriority + 10,
                    Requirements = new HashSet<string> { "MostKillsWhileSolo" }
                },
                new TitleEntry(leaders.MostWaveClutches)
                {
                    TitleName = "Clutch Master",
                    Description = $"Most Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches})",
                    Priority = defaultPriority + 10,
                    Requirements = new HashSet<string> { "MostWaveClutches" }
                },
                new TitleEntry(leaders.MaxKillStreak)
                {
                    TitleName = "Serial Killer",
                    Description = $"Max Kill Streak ({leaders.MaxKillStreak.Value.MaxKillStreak})",
                    Priority = 90, //This is fun to know so I am bumping it
                    Requirements = new HashSet<string> { "MaxKillStreak" }
                },
                new TitleEntry(leaders.MaxKillStreakWhileSolo)
                {
                    TitleName = "Solo Rampage",
                    Description = $"Max Kill Streak While Solo ({leaders.MaxKillStreakWhileSolo.Value.MaxKillStreakWhileSolo})",
                    Priority = 95,
                    Requirements = new HashSet<string> { "MaxKillStreakWhileSolo" }
                },
                new TitleEntry(leaders.MostAliveTime)
                {
                    TitleName = "Survivor",
                    Description = "Most Alive Time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostAliveTime" }
                },
                new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Destroyer",
                    Description = "Most Offense",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense" }
                },
                new TitleEntry(leaders.MostDamageTaken)
                {
                    TitleName = "Punching Bag",
                    Description = "Most Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostDamageTaken" }
                },
                new TitleEntry(leaders.LeastDamageTaken)
                {
                    TitleName = "Shadow",
                    Description = "Least Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastDamageTaken" }
                },
                new TitleEntry(leaders.MostFriendlyFire)
                {
                    TitleName = "Confused",
                    Description = "Most Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostFriendlyFire" }
                },
                new TitleEntry(leaders.LeastFriendlyFire)
                {
                    TitleName = "Team Player",
                    Description = "Least Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastFriendlyFire" }
                },
                new TitleEntry(leaders.LeastOffense)
                {
                    TitleName = "Pacifist",
                    Description = "Least Offense",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense" }
                },
                new TitleEntry(leaders.MostGunsKills)
                {
                    TitleName = "Gunslinger",
                    Description = "Most Gun Kills",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostGunsKills" }
                },
                new TitleEntry(leaders.MostExplosionsKills)
                {
                    TitleName = "Demolitionist",
                    Description = "Most Explosive Kills",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostExplosionsKills" }
                },
                new TitleEntry(leaders.MostBladeKills)
                {
                    TitleName = "Blade Master",
                    Description = "Most Blade Kills",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostBladeKills" }
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
            var explosionsWinner = leaders.MostExplosionsKills.Key;
            var gunsWinner = leaders.MostGunsKills.Key;
            var bladeWinner = leaders.MostBladeKills.Key;
            var clutchWinner = leaders.MostWaveClutches.Key;
            var soloKillsWinner = leaders.MostKillsWhileSolo.Key;

            if (offenseWinner == altitudeWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Orbital Strike",
                    Description = "Most Offense, Highest Point",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "HighestPoint" }
                });
            }

            if (airborneWinner == altitudeWinner)
            {
                titles.Add(new TitleEntry(leaders.MostAirborneTime)
                {
                    TitleName = "Satellite",
                    Description = "Highest Point, Most Airborne Time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "HighestPoint", "MostAirborneTime" }
                });
            }

            if (offenseWinner == damageWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Glass Cannon",
                    Description = "Most Offense, Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostDamageTaken" }
                });
            }

            if (offenseWinner == damageLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Sword and Shield",
                    Description = "Most Offense, Least Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken" }
                });
            }

            if (offenseLoser == damageLoser)
            {
                titles.Add(new TitleEntry(leaders.LeastOffense)
                {
                    TitleName = "Nothing Burger",
                    Description = "Least Offense, Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense", "LeastDamageTaken" }
                });
            }
            if (leaders.MostShieldsLost.Value.ShieldsLost > 0 && shieldsLostWinner == deathsLoser)
            {
                titles.Add(new TitleEntry(leaders.MostShieldsLost)
                {
                    TitleName = "On Death's Bed",
                    Description = "Most Shields Lost, Least Deaths",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostShieldsLost", "LeastDeaths" }
                });
            }
            if (offenseWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Perfectly Balanced",
                    Description = "Most Offense, Friendly Fire",
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
                    Description = "Most Web Swings, Airborne Time",
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
                    Description = "Most Offense, Lowest Point",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LowestPoint" }
                });
            }

            if (swingsWinner == offenseWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Hit & Run",
                    Description = "Most Offense, Web Swings",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostWebSwings" }
                });
            }
            if (clutchWinner == soloKillsWinner)
            {
                titles.Add(new TitleEntry(leaders.MostWaveClutches)
                {
                    TitleName = "Last Stand Hero",
                    Description = $"Most Wave Clutches ({leaders.MostWaveClutches.Value.WaveClutches}), Kills While Solo ({leaders.MostKillsWhileSolo.Value.KillsWhileSolo})",
                    Priority = defaultPriority + 20,
                    Requirements = new HashSet<string> { "MostWaveClutches", "MostKillsWhileSolo" }
                });
            }
            if (explosionsWinner == damageWinner)
            {
                titles.Add(new TitleEntry(leaders.MostExplosionsKills)
                {
                    TitleName = "Kamikaze",
                    Description = "Most Explosive Kills, Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostExplosionsKills", "MostDamageTaken" }
                });
            }

            if (gunsWinner == offenseWinner)
            {
                titles.Add(new TitleEntry(leaders.MostGunsKills)
                {
                    TitleName = "War Machine",
                    Description = "Most Gun Kills, Offense",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostGunsKills", "MostOffense" }
                });
            }

            if (bladeWinner == damageLoser)
            {
                titles.Add(new TitleEntry(leaders.MostBladeKills)
                {
                    TitleName = "Silent Assassin",
                    Description = "Most Blade Kills, Least Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostBladeKills", "LeastDamageTaken" }
                });
            }

            if (bladeWinner == altitudeWinner)
            {
                titles.Add(new TitleEntry(leaders.MostBladeKills)
                {
                    TitleName = "I have the High Ground",
                    Description = "Most Blade Kills, Highest Point",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostBladeKills", "HighestPoint" }
                });
            }

            var aliveTimeWinner = leaders.MostAliveTime.Key;
            if (damageWinner == aliveTimeWinner)
            {
                titles.Add(new TitleEntry(leaders.MostDamageTaken)
                {
                    TitleName = "Nine Lives",
                    Description = "Most Damage, Alive Time",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostDamageTaken", "MostAliveTime" }
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
            var altitudeLoser = leaders.LowestPoint.Key;
            var airborneLoser = leaders.LeastAirborneTime.Key;
            var swingsLoser = leaders.LeastWebSwings.Key;
            var explosionsWinner = leaders.MostExplosionsKills.Key;
            var gunsWinner = leaders.MostGunsKills.Key;
            var bladeWinner = leaders.MostBladeKills.Key;
            var maxKillStreakWhileSoloWinner = leaders.MaxKillStreakWhileSolo.Key;
            var mostWaveClutchesWinner = leaders.MostWaveClutches.Key;


            if (explosionsWinner == altitudeWinner && explosionsWinner == airborneWinner)
            {
                titles.Add(new TitleEntry(leaders.MostExplosionsKills)
                {
                    TitleName = "ICBM",
                    Description = "Most explosion Kills, Airborne Time, Highest Point",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "explosionsWinner", "HighestPoint", "MostAirborneTime" }
                });
            }

            if (bladeWinner == altitudeWinner && bladeWinner == damageLoser)
            {
                titles.Add(new TitleEntry(leaders.MostBladeKills)
                {
                    TitleName = "Phantom Blade",
                    Description = "Most blade kills, Highest Point, Least Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostBladeKills", "HighestPoint", "LeastDamageTaken" }
                });
            }

            if (offenseWinner == damageWinner && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Elegant Barbarian",
                    Description = "Most Offense, Damage Taken, Least Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "MostDamageTaken", "LeastFriendlyFire" }
                });
            }

            if (offenseWinner == damageLoser && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "MVP",
                    Description = "Most Offense, Least Damage Taken, Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "LeastFriendlyFire" }
                });
            }

            if (offenseWinner == damageLoser && offenseWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Agent of Chaos",
                    Description = "Most Offense, Friendly Fire, Least Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "MostFriendlyFire" }
                });
            }

            if (maxKillStreakWhileSoloWinner == mostWaveClutchesWinner && maxKillStreakWhileSoloWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.MostFriendlyFire)
                {
                    TitleName = "Ordered Chaos",
                    Description = "Most Friendly Fire, Wave Clutches, Max Kill Streak While Solo",
                    Priority = defaultPriority + 10,
                    Requirements = new HashSet<string> { "MostFriendlyFire", "MaxKillStreakWhileSolo", "MostWaveClutches" }
                });
            }


            if (offenseLoser == damageLoser && offenseLoser == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.LeastOffense)
                {
                    TitleName = "Traitor",
                    Description = "Least Offense, Damage Taken, Most Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense", "LeastDamageTaken", "MostFriendlyFire" }
                });
            }

            if (swingsWinner == airborneWinner && swingsWinner == damageWinner)
            {
                titles.Add(new TitleEntry(leaders.MostWebSwings)
                {
                    TitleName = "Spooderman",
                    Description = "Most Web Swings, Airborne Time, Damage Taken",
                    Priority = defaultPriority + 10,
                    Requirements = new HashSet<string> { "MostWebSwings", "MostAirborneTime", "MostDamageTaken" }
                });
            }

            if (altitudeLoser == airborneLoser && altitudeLoser == swingsLoser)
            {
                titles.Add(new TitleEntry(leaders.LowestPoint)
                {
                    TitleName = "Basement Dweller",
                    Description = "Lowest Point, Least Airborne Time, Web Swings",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LowestPoint", "LeastAirborneTime", "LeastWebSwings" }
                });
            }

            if (gunsWinner == offenseWinner && gunsWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry(leaders.MostGunsKills)
                {
                    TitleName = "Marksman",
                    Description = "Most Gun Kills, Offense, Least Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostGunsKills", "MostOffense", "LeastFriendlyFire" }
                });
            }

            if (explosionsWinner == offenseWinner && explosionsWinner == friendlyFireWinner)
            {
                titles.Add(new TitleEntry(leaders.MostExplosionsKills)
                {
                    TitleName = "Mutually Assured Destruction",
                    Description = "Most Explosive Kills, Offense, Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostExplosionsKills", "MostOffense", "MostFriendlyFire" }
                });
            }

            if (bladeWinner == explosionsWinner && explosionsWinner == gunsWinner)
            {
                titles.Add(new TitleEntry(leaders.MostExplosionsKills)
                {
                    TitleName = "Master of Arms",
                    Description = "Most Explosive Kills, Gun Kills, Blade Kills",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostExplosionsKills", "MostGunsKills", "MostBladeKills" }
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
            var explosionsWinner = leaders.MostExplosionsKills.Key;
            var gunsWinner = leaders.MostGunsKills.Key;
            var bladeWinner = leaders.MostBladeKills.Key;
            var offenseLoser = leaders.LeastOffense.Key;
            var friendlyFireWinner = leaders.MostFriendlyFire.Key;

            if (offenseWinner == damageLoser && offenseWinner == altitudeWinner && offenseWinner == friendlyFireLoser)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "God Complex",
                    Description = "Most Offense, Least Damage Taken, Friendly Fire, Highest Point",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "HighestPoint", "LeastFriendlyFire" }
                });
            }

            if (altitudeWinner == airborneWinner && altitudeWinner == damageLoser && altitudeWinner == swingsWinner)
            {
                titles.Add(new TitleEntry(leaders.HighestPoint)
                {
                    TitleName = "The Untouchable",
                    Description = "Highest Point, Most Airborne Time, Web Swings, Least Damage Taken",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "HighestPoint", "MostAirborneTime", "LeastDamageTaken", "MostWebSwings" }
                });
            }

            if (offenseWinner == altitudeWinner && offenseWinner == airborneWinner && offenseWinner == explosionsWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Nuclear Warhead",
                    Description = "Most Offense, Airborne Time, Explosive Kills, Highest Point",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "HighestPoint", "MostAirborneTime", "MostExplosionsKills" }
                });
            }

            if (offenseLoser == damageLoser && offenseLoser == friendlyFireWinner && offenseLoser == explosionsWinner)
            {
                titles.Add(new TitleEntry(leaders.LeastOffense)
                {
                    TitleName = "Inside Job",
                    Description = "Least Offense, Damage Taken, Most Friendly Fire, Explosive Kills",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "LeastOffense", "LeastDamageTaken", "MostFriendlyFire", "MostExplosionsKills" }
                });
            }

            if (offenseWinner == damageLoser && offenseWinner == friendlyFireLoser && offenseWinner == gunsWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Rambo",
                    Description = "Most Offense, Gun Kills, Least Damage Taken, Friendly Fire",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "LeastFriendlyFire", "MostGunsKills" }
                });
            }

            return titles;
        }

        private List<TitleEntry> CreateFiveCategoryTitles(StatLeaders leaders, int defaultPriority = 50)
        {
            var titles = new List<TitleEntry>();

            var offenseWinner = leaders.MostOffense.Key;
            var damageLoser = leaders.LeastDamageTaken.Key;
            var altitudeWinner = leaders.HighestPoint.Key;
            var friendlyFireWinner = leaders.MostFriendlyFire.Key;
            var explosionsWinner = leaders.MostExplosionsKills.Key;

            if (offenseWinner == damageLoser && offenseWinner == altitudeWinner && offenseWinner == friendlyFireWinner && offenseWinner == explosionsWinner)
            {
                titles.Add(new TitleEntry(leaders.MostOffense)
                {
                    TitleName = "Supernova",
                    Description = "Most Offense, Friendly Fire, Explosive Kills, Least Damage Taken, Highest Point",
                    Priority = defaultPriority,
                    Requirements = new HashSet<string> { "MostOffense", "LeastDamageTaken", "HighestPoint", "friendlyFireWinner", "MostExplosionsKills" }
                });
            }

            return titles;
        }
    }
}
