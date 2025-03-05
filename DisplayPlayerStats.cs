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

        // Material design colors
        private Color headerColor = new Color(0.2f, 0.2f, 0.4f, 0.95f);
        private Color backgroundColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
        private Color textColor = new Color(0.9f, 0.9f, 0.9f);
        private Color accentColor = new Color(0.3f, 0.6f, 1.0f);
        private Color statRowColor = new Color(0.3f, 0.3f, 0.35f, 0.7f);
        private Color alternateRowColor = new Color(0.35f, 0.35f, 0.4f, 0.7f);

        // Custom GUI styles
        private GUIStyle headerStyle;
        private GUIStyle contentStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle windowStyle;

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

        private void Start()
        {
            // Initialize custom styles
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            // Window style
            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeTexture(2, 2, backgroundColor);
            windowStyle.onNormal.background = MakeTexture(2, 2, backgroundColor);

            // Header style
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = textColor;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.margin = new RectOffset(0, 0, 10, 10);

            // Content style
            contentStyle = new GUIStyle(GUI.skin.label);
            contentStyle.fontSize = 12;
            contentStyle.normal.textColor = textColor;
            contentStyle.padding = new RectOffset(5, 5, 5, 5);

            // Stat label style
            statLabelStyle = new GUIStyle(GUI.skin.label);
            statLabelStyle.fontSize = 12;
            statLabelStyle.normal.textColor = textColor;
            statLabelStyle.padding = new RectOffset(8, 0, 4, 4);

            // Stat value style
            statValueStyle = new GUIStyle(GUI.skin.label);
            statValueStyle.fontSize = 12;
            statValueStyle.normal.textColor = accentColor;
            statValueStyle.fontStyle = FontStyle.Bold;
            statValueStyle.padding = new RectOffset(0, 8, 4, 4);
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
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

            // Make sure styles are initialized
            if (headerStyle == null) InitializeStyles();

            windowRect = GUILayout.Window(0, windowRect, DrawStatsWindow, "Player Stats", windowStyle);
        }

        private void DrawStatsWindow(int windowID)
        {
            // Header with material design style
            GUI.backgroundColor = headerColor;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("PLAYER STATISTICS", headerStyle);
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            try
            {
                if (PlayerTracker.Instance != null)
                {
                    // Active player count display
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label("Active Players:", statLabelStyle, GUILayout.Width(120));
                    GUILayout.Label($"{PlayerTracker.Instance.GetActivePlayerCount()}", statValueStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    var playerStats = PlayerTracker.Instance.GetPlayerStatsList();
                    if (playerStats != null && playerStats.Count > 0)
                    {
                        // Column headers
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("PLAYER", statLabelStyle, GUILayout.Width(180));
                        GUILayout.Label("DEATHS", statLabelStyle);
                        GUILayout.EndHorizontal();

                        GUILayout.Space(5);

                        // Draw player stats with alternating row colors
                        for (int i = 0; i < playerStats.Count; i++)
                        {
                            var stat = playerStats[i];
                            GUI.backgroundColor = (i % 2 == 0) ? statRowColor : alternateRowColor;

                            GUILayout.BeginHorizontal(GUI.skin.box);
                            GUILayout.Label($"{stat.playerName}", statLabelStyle, GUILayout.Width(180));
                            GUILayout.Label($"{stat.deaths}", statValueStyle);
                            GUILayout.EndHorizontal();
                        }

                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUILayout.Label("No player stats available", contentStyle);
                    }
                }
                else
                {
                    GUILayout.Label("Player Tracker not initialized", contentStyle);
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error displaying stats: {ex.Message}", contentStyle);
                Logger.LogError($"Error displaying stats: {ex.Message}");
            }

            GUILayout.EndScrollView();

            // Info text at the bottom
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Press F1 to close", contentStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Allow dragging the window
            GUI.DragWindow();
        }
    }
}
