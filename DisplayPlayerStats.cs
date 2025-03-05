using UnityEngine;
using System.Collections.Generic;
using Silk;
using Logger = Silk.Logger;
using UnityEngine.InputSystem; // Add this line for newer Unity versions

namespace StatsMod
{
    public class DisplayPlayerStats : MonoBehaviour
    {
        // Singleton instance
        private static DisplayPlayerStats _instance;
        public static DisplayPlayerStats Instance { get; private set; }

        // UI properties
        private bool isDisplayVisible = false;
        private Rect windowRect = new Rect(20, 20, 250, 300);
        private Vector2 scrollPosition;

        // Initialize as a singleton when the game starts
        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject statsDisplayObj = new GameObject("DisplayPlayerStats");
                _instance = statsDisplayObj.AddComponent<DisplayPlayerStats>();
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
    }
}
