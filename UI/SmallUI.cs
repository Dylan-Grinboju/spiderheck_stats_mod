using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using System;

namespace StatsMod
{
    public class SmallUI : MonoBehaviour
    {
        #region Constants
        private const float BASE_WINDOW_WIDTH = 300f;
        private const float BASE_WINDOW_HEIGHT = 35f;

        private const float BASE_PLAY_TIME_HEIGHT = 45f;
        private const float BASE_ENEMY_DEATHS_HEIGHT = 45f;
        private const float BASE_PLAYER_STATS_BASE_HEIGHT = 60f;
        private const float BASE_PLAYER_STATS_PER_PLAYER_HEIGHT = 35f;

        // Scaled properties
        private float WindowWidth => UIManager.ScaleValue(BASE_WINDOW_WIDTH);
        private float WindowHeight => UIManager.ScaleValue(BASE_WINDOW_HEIGHT);
        private float PlayTimeHeight => UIManager.ScaleValue(BASE_PLAY_TIME_HEIGHT);
        private float EnemyDeathsHeight => UIManager.ScaleValue(BASE_ENEMY_DEATHS_HEIGHT);
        private float PlayerStatsBaseHeight => UIManager.ScaleValue(BASE_PLAYER_STATS_BASE_HEIGHT);
        private float PlayerStatsPerPlayerHeight => UIManager.ScaleValue(BASE_PLAYER_STATS_PER_PLAYER_HEIGHT);
        #endregion

        #region UI State
        private bool isDisplayVisible = false;
        private bool isDisplayVisibleAtAll = true;
        private Rect windowRect;
        #endregion

        #region GUI Styles
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle cardStyle;
        private GUIStyle errorStyle;
        private GUIStyle timerStyle;
        private GUIStyle statusStyle;
        private GUIStyle killsGreenStyle;
        private GUIStyle killsWhiteStyle;
        private GUIStyle deathsRedStyle;
        private GUIStyle deathsWhiteStyle;
        private GUIStyle playerColorStyle; // Reused for dynamic player colors
        private bool stylesInitialized = false;
        #endregion

        #region Initialization
        public void Initialize()
        {
            float xPos = ModConfig.DisplayPositionX;
            float yPos = ModConfig.DisplayPositionY;

            windowRect = new Rect(xPos, yPos, WindowWidth, 100f);
            isDisplayVisibleAtAll = ModConfig.ShowStatsWindow;
            UpdateWindowSize();
            isDisplayVisible = isDisplayVisibleAtAll;

            Logger.LogInfo("SmallUI initialized");
        }
        #endregion

        #region Display Control
        public void ToggleDisplay()
        {
            isDisplayVisible = !isDisplayVisible;
        }

        public void ShowDisplay()
        {
            isDisplayVisible = true;
        }

        public void HideDisplay()
        {
            isDisplayVisible = false;
        }

        public bool IsVisible()
        {
            return isDisplayVisibleAtAll && isDisplayVisible;
        }
        #endregion

        #region Style Management
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            windowStyle = UIManager.Instance.CreateWindowStyle(UIManager.Instance.GetDarkTexture());
            headerStyle = UIManager.Instance.CreateHeaderStyle();
            labelStyle = UIManager.Instance.CreateLabelStyle();
            valueStyle = UIManager.Instance.CreateValueStyle();
            cardStyle = UIManager.Instance.CreateCardStyle(UIManager.Instance.GetMediumTexture());
            errorStyle = UIManager.Instance.CreateErrorStyle();

            // Cache colored styles to avoid per-frame allocations
            timerStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Green } };
            statusStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Gray } };
            killsGreenStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Green } };
            killsWhiteStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White } };
            deathsRedStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Red } };
            deathsWhiteStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White } };
            playerColorStyle = new GUIStyle(valueStyle); // Will set color dynamically

            stylesInitialized = true;
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

        #region Drawing Methods
        private void DrawSurvivalModeStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Survival Mode", headerStyle);

            GUILayout.BeginHorizontal();
            if (statsSnapshot.IsSurvivalActive)
            {
                GUILayout.Label("Time:", labelStyle, GUILayout.Width(UIManager.ScaleValue(50)));
                GUILayout.Label(TimeFormatUtils.FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle, GUILayout.MinWidth(UIManager.ScaleValue(80)));
            }
            else
            {
                GUILayout.Label("Last Game:", labelStyle, GUILayout.Width(UIManager.ScaleValue(120)));
                GUILayout.Label(statsSnapshot.LastGameDuration.TotalSeconds > 0 ? TimeFormatUtils.FormatTimeSpan(statsSnapshot.LastGameDuration) : "No games yet", statusStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(UIManager.ScaleValue(4));
        }

        private void DrawEnemyStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Enemy Statistics", headerStyle);

            try
            {
                int enemiesKilled = statsSnapshot.EnemiesKilled;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Enemies Killed:", labelStyle, GUILayout.Width(UIManager.ScaleValue(120)));
                GUILayout.Label(enemiesKilled.ToString(), enemiesKilled > 0 ? killsGreenStyle : killsWhiteStyle);
                GUILayout.EndHorizontal();
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", errorStyle);
                Logger.LogError($"Error displaying enemy stats: {ex.Message}");
            }
            GUILayout.EndVertical();
            GUILayout.Space(UIManager.ScaleValue(4));
        }

        private void DrawPlayerStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            try
            {
                if (statsSnapshot.ActivePlayers != null && statsSnapshot.ActivePlayers.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Player", headerStyle, GUILayout.Width(UIManager.ScaleValue(95)));
                    GUILayout.Label("Deaths", headerStyle, GUILayout.Width(UIManager.ScaleValue(100)));
                    GUILayout.Label("Kills", headerStyle, GUILayout.Width(UIManager.ScaleValue(60)));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(UIManager.ScaleValue(4));

                    foreach (var playerEntry in statsSnapshot.ActivePlayers)
                    {
                        var playerData = playerEntry.Value;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", valueStyle, GUILayout.Width(UIManager.ScaleValue(5)));

                        // Reuse cached style, just update the color
                        playerColorStyle.normal.textColor = playerData.PlayerColor;
                        GUILayout.Label(playerData.PlayerName, playerColorStyle, GUILayout.Width(UIManager.ScaleValue(115)));

                        GUILayout.Label(playerData.Deaths.ToString(), playerData.Deaths > 0 ? deathsRedStyle : deathsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(90)));
                        GUILayout.Label(playerData.Kills.ToString(), playerData.Kills > 0 ? killsGreenStyle : killsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(60)));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(UIManager.ScaleValue(8));
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
        #endregion



        #region Window Size Management
        private void UpdateWindowSize()
        {
            float totalHeight = WindowHeight;

            if (ModConfig.ShowPlayTime)
            {
                totalHeight += PlayTimeHeight;
            }

            if (ModConfig.ShowEnemyDeaths)
            {
                totalHeight += EnemyDeathsHeight;
            }

            if (ModConfig.ShowPlayers)
            {
                totalHeight += PlayerStatsBaseHeight;

                var statsSnapshot = StatsManager.Instance?.GetStatsSnapshot();
                if (statsSnapshot?.ActivePlayers != null)
                {
                    totalHeight += statsSnapshot.ActivePlayers.Count * PlayerStatsPerPlayerHeight;
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
