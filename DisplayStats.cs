using UnityEngine;
using System.Collections.Generic;
using Silk;
using Logger = Silk.Logger;
using UnityEngine.InputSystem;
using System;

namespace StatsMod
{
    public class DisplayStats : MonoBehaviour
    {
        private static DisplayStats _instance;
        public static DisplayStats Instance { get; private set; }

        // UI properties
        private bool isDisplayVisible = false;
        private Rect windowRect;
        private Vector2 scrollPosition;

        // Custom GUI Styles
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;
        private Texture2D solidColorTexture;

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

                // Set the window position to top right
                Instance.windowRect = new Rect(Screen.width - 320, 20, 300, 350);

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

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // Create a solid color texture for the button
            solidColorTexture = new Texture2D(1, 1);
            solidColorTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f, 1f)); // Dark gray solid color
            solidColorTexture.Apply();

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                normal = { background = solidColorTexture },
                hover = { background = solidColorTexture },
                active = { background = solidColorTexture }
            };

            stylesInitialized = true;
        }

        private void OnDestroy()
        {
            if (solidColorTexture != null)
            {
                Destroy(solidColorTexture);
            }
        }

        private void OnGUI()
        {
            if (!isDisplayVisible) return;

            InitializeStyles();
            windowRect = GUILayout.Window(0, windowRect, DrawStatsWindow, "Player Stats");
        }

        private void DrawStatsWindow(int windowID)
        {
            GUILayout.Label("Player Statistics", titleStyle);
            GUILayout.Space(5);

            // Survival Mode Stats Section
            GUILayout.Label("Survival Mode Stats", headerStyle);

            string survivalTimeText = isSurvivalActive
                ? $"Current Session: {FormatTimeSpan(DateTime.Now - survivalStartTime)}"
                : "Not in Survival Mode";

            GUILayout.Label(survivalTimeText, labelStyle);
            GUILayout.Label($"Last Session Time: {FormatTimeSpan(lastSessionTime)}", labelStyle);

            GUILayout.Space(5);

            // Enemy Stats Section
            GUILayout.Label("Enemy Statistics", headerStyle);
            try
            {
                if (EnemiesTracker.Instance != null)
                {
                    GUILayout.Label($"Enemies Killed: {EnemiesTracker.Instance.GetEnemiesKilled()}", labelStyle);
                }
                else
                {
                    GUILayout.Label("Enemy Tracker not initialized", labelStyle);
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error displaying enemy stats: {ex.Message}", labelStyle);
                Logger.LogError($"Error displaying enemy stats: {ex.Message}");
            }

            // Player Stats Section
            GUILayout.Space(5);
            GUILayout.Label("Player Statistics", headerStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            try
            {
                if (PlayerTracker.Instance != null)
                {
                    var playerStats = PlayerTracker.Instance.GetPlayerStatsList();
                    if (playerStats != null && playerStats.Count > 0)
                    {
                        foreach (var stat in playerStats)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"{stat.playerName}", labelStyle, GUILayout.Width(180));
                            GUILayout.Label($"Deaths: {stat.deaths}", labelStyle);
                            GUILayout.EndHorizontal();
                            GUILayout.Space(2);
                        }
                    }
                    else
                    {
                        GUILayout.Label("No player stats available", labelStyle);
                    }
                }
                else
                {
                    GUILayout.Label("Player Tracker not initialized", labelStyle);
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error displaying stats: {ex.Message}", labelStyle);
                Logger.LogError($"Error displaying stats: {ex.Message}");
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Close", buttonStyle))
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
