using UnityEngine;
using System.Collections.Generic;
using Silk;
using Logger = Silk.Logger;
using UnityEngine.InputSystem; // Add this line for newer Unity versions
using System;

namespace StatsMod
{
    public class DisplayStats : MonoBehaviour
    {
        private static DisplayStats _instance;
        public static DisplayStats Instance { get; private set; }

        // UI properties
        private bool isDisplayVisible = false;
        private Rect windowRect = new Rect(20, 20, 300, 400);
        private Vector2 scrollPosition;

        // Survival Mode tracking
        private bool isSurvivalActive = false;
        private DateTime survivalStartTime;
        private TimeSpan lastSessionTime = TimeSpan.Zero;

        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject statsDisplayObj = new GameObject("DisplayPlayerStats");
                _instance = statsDisplayObj.AddComponent<DisplayStats>();
                DontDestroyOnLoad(statsDisplayObj);
                Instance = _instance;
                Logger.LogInfo("Stats Display initialized");
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                isDisplayVisible = !isDisplayVisible;
                Logger.LogInfo($"Stats display {(isDisplayVisible ? "shown" : "hidden")}");
            }
        }

        private void OnGUI()
        {
            if (!isDisplayVisible) return;

            windowRect = GUILayout.Window(0, windowRect, DrawStatsWindow, "Player Stats");
        }

        private void DrawStatsWindow(int windowID)
        {
            GUILayout.Label("Player Statistics");
            GUILayout.Space(10);

            // Survival Mode Stats Section
            GUILayout.Label("Survival Mode Stats", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            string survivalTimeText = isSurvivalActive
                ? $"Current Session: {FormatTimeSpan(DateTime.Now - survivalStartTime)}"
                : "Not in Survival Mode";

            GUILayout.Label(survivalTimeText);
            GUILayout.Label($"Last Session Time: {FormatTimeSpan(lastSessionTime)}");

            GUILayout.Space(10);

            // Enemy Stats Section
            GUILayout.Label("Enemy Statistics", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            try
            {
                if (EnemiesTracker.Instance != null)
                {
                    GUILayout.Label($"Enemies Killed: {EnemiesTracker.Instance.GetEnemiesKilled()}");
                }
                else
                {
                    GUILayout.Label("Enemy Tracker not initialized");
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error displaying enemy stats: {ex.Message}");
                Logger.LogError($"Error displaying enemy stats: {ex.Message}");
            }

            // Player Stats Section
            GUILayout.Space(10);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            try
            {
                if (PlayerTracker.Instance != null)
                {
                    GUILayout.Label($"Active Players: {PlayerTracker.Instance.GetActivePlayerCount()}");
                    GUILayout.Space(10);

                    var playerStats = PlayerTracker.Instance.GetPlayerStatsList();
                    if (playerStats != null && playerStats.Count > 0)
                    {
                        foreach (var stat in playerStats)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"{stat.playerName}", GUILayout.Width(180));
                            GUILayout.Label($"Deaths: {stat.deaths}");
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);
                        }
                    }
                    else
                    {
                        GUILayout.Label("No player stats available");
                    }
                }
                else
                {
                    GUILayout.Label("Player Tracker not initialized");
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error displaying stats: {ex.Message}");
                Logger.LogError($"Error displaying stats: {ex.Message}");
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Close"))
            {
                isDisplayVisible = false;
            }

            GUI.DragWindow();
        }

        // Helper method for formatting TimeSpan in a readable way
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }

        public void StartSurvivalTimer()
        {
            isSurvivalActive = true;
            survivalStartTime = DateTime.Now;
            Logger.LogInfo("Survival mode timer started");
        }

        public void StopSurvivalTimer()
        {
            if (!isSurvivalActive) return;

            isSurvivalActive = false;
            // Store only the last session time, not a cumulative total
            lastSessionTime = DateTime.Now - survivalStartTime;

            Logger.LogInfo($"Survival mode timer stopped. Session time: {FormatTimeSpan(lastSessionTime)}");
        }
    }
}
