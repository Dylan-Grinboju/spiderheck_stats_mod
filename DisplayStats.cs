using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using UnityEngine.InputSystem;
using System;

namespace StatsMod
{
    public class DisplayStats : MonoBehaviour
    {
        #region Constants
        private const float WINDOW_WIDTH = 300f;
        private const float WINDOW_HEIGHT = 35f;
        private const int PADDING = 8;
        private const int HEADER_SIZE = 16;
        private const int LABEL_SIZE = 14;

        // Component height constants
        private const float PLAY_TIME_HEIGHT = 45f;
        private const float ENEMY_DEATHS_HEIGHT = 45f;
        private const float PLAYER_STATS_BASE_HEIGHT = 60f;
        private const float PLAYER_STATS_PER_PLAYER_HEIGHT = 35f;

        // Colors
        private static readonly Color Blue = new Color(0.259f, 0.522f, 0.957f, 1f);
        private static readonly Color Red = new Color(1f, 0.341f, 0.133f, 1f);
        private static readonly Color Green = new Color(0.298f, 0.686f, 0.314f, 1f);
        private static readonly Color White = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color Gray = new Color(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color DarkGray = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        private static readonly Color MediumGray = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        #endregion

        #region Instance Management
        private static DisplayStats _instance;
        public static DisplayStats Instance { get; private set; }
        #endregion

        #region UI State
        private bool isDisplayVisible = false;
        private bool isDisplayVisibleAtAll = true;
        private Rect windowRect;
        #endregion

        #region GUI Styles and Resources
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle cardStyle;
        private GUIStyle errorStyle;
        private bool stylesInitialized = false;

        private Texture2D darkTexture;
        private Texture2D mediumTexture;
        #endregion

        #region Initialization
        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject statsDisplayObj = new GameObject("DisplayPlayerStats");
                _instance = statsDisplayObj.AddComponent<DisplayStats>();
                DontDestroyOnLoad(statsDisplayObj);
                Instance = _instance;

                float xPos = ModConfig.DisplayPositionX;
                float yPos = ModConfig.DisplayPositionY;

                Instance.windowRect = new Rect(xPos, yPos, WINDOW_WIDTH, 100f);
                Instance.isDisplayVisibleAtAll = ModConfig.ShowStatsWindow;
                Instance.UpdateWindowSize();

                Logger.LogInfo("Stats Display initialized with config settings");
            }
        }
        #endregion

        #region Input Handling
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                ToggleDisplay();
            }
        }

        private void ToggleDisplay()
        {
            isDisplayVisible = !isDisplayVisible;
        }

        public void AutoPullHUD()
        {
            isDisplayVisible = true;
        }

        public void HideHUD()
        {
            isDisplayVisible = false;
        }
        #endregion

        #region Style Management
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            CreateTextures();

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = darkTexture },
                padding = new RectOffset(PADDING * 2, PADDING * 2, PADDING * 2, PADDING * 2)
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = HEADER_SIZE,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Blue },
                padding = new RectOffset(PADDING, 0, 4, 2)
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = LABEL_SIZE,
                normal = { textColor = Gray },
                padding = new RectOffset(PADDING, 0, 2, 2)
            };

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = LABEL_SIZE,
                fontStyle = FontStyle.Bold,
                normal = { textColor = White },
                padding = new RectOffset(0, PADDING, 2, 2)
            };

            cardStyle = new GUIStyle()
            {
                normal = { background = mediumTexture },
                padding = new RectOffset(PADDING, PADDING, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            errorStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = LABEL_SIZE,
                normal = { textColor = Red }
            };

            stylesInitialized = true;
        }

        private void CreateTextures()
        {
            darkTexture = CreateColorTexture(DarkGray);
            mediumTexture = CreateColorTexture(MediumGray);
        }

        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            if (darkTexture != null) Destroy(darkTexture);
            if (mediumTexture != null) Destroy(mediumTexture);
        }
        #endregion

        #region GUI Drawing
        private void OnGUI()
        {
            if (!isDisplayVisibleAtAll || !isDisplayVisible) return;

            InitializeStyles();
            UpdateWindowSize();

            Vector2 oldPosition = new Vector2(windowRect.x, windowRect.y);
            windowRect = GUI.Window(0, windowRect, DrawStatsWindow, "", windowStyle);

            Vector2 newPosition = new Vector2(windowRect.x, windowRect.y);
            if (oldPosition != newPosition)
            {
                ModConfig.SetDisplayPosition((int)newPosition.x, (int)newPosition.y);
            }
        }

        private void DrawStatsWindow(int windowID)
        {
            GUILayout.BeginVertical();

            var statsSnapshot = StatsManager.Instance.GetStatsSnapshot();

            if (ModConfig.ShowPlayTime)
            {
                DrawSurvivalModeStats(statsSnapshot);
            }

            if (ModConfig.ShowEnemyDeaths)
            {
                DrawEnemyStats(statsSnapshot);
            }

            if (ModConfig.ShowPlayers)
            {
                DrawPlayerStats(statsSnapshot);
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        #endregion

        private void DrawSurvivalModeStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Survival Mode", headerStyle);

            GUILayout.BeginHorizontal();
            if (statsSnapshot.IsSurvivalActive)
            {
                GUILayout.Label("Time:", labelStyle, GUILayout.Width(50));
                var timerStyle = new GUIStyle(valueStyle) { normal = { textColor = Green } };
                GUILayout.Label(FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle, GUILayout.MinWidth(80));
            }
            else
            {
                GUILayout.Label("Last Game:", labelStyle, GUILayout.Width(120));
                var statusStyle = new GUIStyle(valueStyle) { normal = { textColor = Gray } };
                GUILayout.Label(statsSnapshot.LastGameDuration.TotalSeconds > 0 ? FormatTimeSpan(statsSnapshot.LastGameDuration) : "No games yet", statusStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void DrawEnemyStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Enemy Statistics", headerStyle);

            try
            {
                int enemiesKilled = statsSnapshot.EnemiesKilled;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Enemies Killed:", labelStyle, GUILayout.Width(120));
                var killsStyle = new GUIStyle(valueStyle) { normal = { textColor = enemiesKilled > 0 ? Green : White } };
                GUILayout.Label(enemiesKilled.ToString(), killsStyle);
                GUILayout.EndHorizontal();
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", errorStyle);
                Logger.LogError($"Error displaying enemy stats: {ex.Message}");
            }
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void DrawPlayerStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            try
            {
                if (statsSnapshot.ActivePlayers != null && statsSnapshot.ActivePlayers.Count > 0)
                {
                    // Header row
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Player", headerStyle, GUILayout.Width(95));
                    GUILayout.Label("Deaths", headerStyle, GUILayout.Width(100));
                    GUILayout.Label("Kills", headerStyle, GUILayout.Width(60));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);

                    foreach (var playerEntry in statsSnapshot.ActivePlayers)
                    {
                        var playerData = playerEntry.Value;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", valueStyle, GUILayout.Width(5));

                        var playerNameStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.PlayerColor } };
                        GUILayout.Label(playerData.PlayerName, playerNameStyle, GUILayout.Width(115));

                        var deathsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Deaths > 0 ? Red : White } };
                        GUILayout.Label(playerData.Deaths.ToString(), deathsStyle, GUILayout.Width(90));

                        var killsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Kills > 0 ? Green : White } };
                        GUILayout.Label(playerData.Kills.ToString(), killsStyle, GUILayout.Width(60));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(8);
                    }
                }
                else
                {
                    GUILayout.Label("No players connected", labelStyle);
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", errorStyle);
                Logger.LogError($"Error displaying player stats: {ex.Message}");
            }
            GUILayout.EndVertical();
        }

        #region Utility Methods
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
        #endregion

        #region Window Size Management
        private void UpdateWindowSize()
        {
            float totalHeight = WINDOW_HEIGHT;

            if (ModConfig.ShowPlayTime)
            {
                totalHeight += PLAY_TIME_HEIGHT;
            }

            if (ModConfig.ShowEnemyDeaths)
            {
                totalHeight += ENEMY_DEATHS_HEIGHT;
            }

            if (ModConfig.ShowPlayers)
            {
                totalHeight += PLAYER_STATS_BASE_HEIGHT;

                var statsSnapshot = StatsManager.Instance?.GetStatsSnapshot();
                if (statsSnapshot?.ActivePlayers != null)
                {
                    totalHeight += statsSnapshot.ActivePlayers.Count * PLAYER_STATS_PER_PLAYER_HEIGHT;
                }
            }

            windowRect.height = totalHeight;
        }

        public void OnPlayerJoined()
        {
            UpdateWindowSize();
        }

        public void OnPlayerLeft()
        {
            UpdateWindowSize();
        }
        #endregion
    }
}
