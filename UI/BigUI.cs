using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using System;

namespace StatsMod
{
    public class BigUI : MonoBehaviour
    {
        #region Base Dimensions
        public float BASE_SECTION_SPACING = 25f;
        public float BASE_CARD_PADDING = 20f;
        public float BASE_PLAYER_ROW_HEIGHT = 40f;
        public float BASE_SURVIVAL_SECTION_HEIGHT = 100f;
        public float BASE_CONTENT_PADDING = 40f;
        public float BASE_MARGIN_PERCENT = 0.15f;
        public float BASE_MAX_HEIGHT_PERCENT = 0.9f;
        public float BASE_BACKGROUND_PADDING = 80f;
        #endregion

        #region Column Widths
        public float COL_WIDTH_PLAYER_NAME = 120f;
        public float COL_WIDTH_DEATHS = 90f;
        public float COL_WIDTH_KILLS = 70f;
        public float COL_WIDTH_PVP = 110f;
        public float COL_WIDTH_ENEMY_SHIELDS = 120f;
        public float COL_WIDTH_PLAYER_SHIELDS = 100f;
        public float COL_WIDTH_SHIELDS_LOST = 100f;
        public float COL_WIDTH_ALIVE_TIME = 120f;
        public float COL_WIDTH_INDENT = 30f;
        #endregion

        #region Text Labels
        public string LABEL_PLAYER = "Player";
        public string LABEL_DEATHS = "Deaths";
        public string LABEL_KILLS = "Kills";
        public string LABEL_PVP = "Friendly Kills";
        public string LABEL_ENEMY_SHIELDS = "Enemy Shields";
        public string LABEL_PLAYER_SHIELDS = "Player Shields";
        public string LABEL_SHIELDS_LOST = "Shields Lost";
        public string LABEL_ALIVE_TIME = "Alive Time";
        public string LABEL_SURVIVAL_MODE = "Survival Mode";
        public string LABEL_ENEMY_STATISTICS = "Enemy Statistics";
        public string LABEL_TIME = "Time:";
        public string LABEL_LAST_GAME = "Last Game:";
        public string LABEL_ENEMIES_KILLED = "Enemies Killed:";
        public string LABEL_NO_PLAYERS = "No players connected";
        public string LABEL_NO_GAMES = "No games yet";
        #endregion

        #region Spacing Values
        public float SPACING_BETWEEN_HEADERS = 10f;
        public float SPACING_BETWEEN_PLAYERS = 15f;
        public float SPACING_CARD_PERCENT = 0.04f;
        #endregion

        #region Other Widths
        public float WIDTH_TIME_LABEL = 100f;
        public float WIDTH_LAST_GAME_LABEL = 300f;
        public float WIDTH_ENEMIES_LABEL = 300f;
        public float WIDTH_CARD_HALF = 0.48f;
        #endregion

        // Scaled properties
        private float SectionSpacing => UIManager.ScaleValue(BASE_SECTION_SPACING);
        private float CardPadding => UIManager.ScaleValue(BASE_CARD_PADDING);
        private float PlayerRowHeight => UIManager.ScaleValue(BASE_PLAYER_ROW_HEIGHT);
        private float SurvivalSectionHeight => UIManager.ScaleValue(BASE_SURVIVAL_SECTION_HEIGHT);

        private float Total_Height = 0f;

        #region UI State
        private bool isDisplayVisible = false;
        private bool isDisplayVisibleAtAll = true;
        #endregion

        #region GUI Styles
        private GUIStyle backgroundStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle cardStyle;
        private GUIStyle errorStyle;
        private bool stylesInitialized = false;
        #endregion

        #region Initialization
        public void Initialize()
        {
            isDisplayVisibleAtAll = ModConfig.ShowStatsWindow;
            Logger.LogInfo("BigUI initialized");
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

            backgroundStyle = new GUIStyle()
            {
                normal = { background = UIManager.Instance.GetDarkTexture() }
            };

            headerStyle = UIManager.Instance.CreateHeaderStyle(UIManager.BIG_HEADER_SIZE);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle = UIManager.Instance.CreateLabelStyle(UIManager.BIG_LABEL_SIZE);
            valueStyle = UIManager.Instance.CreateValueStyle(UIManager.BIG_LABEL_SIZE);
            cardStyle = UIManager.Instance.CreateCardStyle(UIManager.Instance.GetMediumTexture());
            errorStyle = UIManager.Instance.CreateErrorStyle(UIManager.BIG_LABEL_SIZE);

            cardStyle.padding = new RectOffset((int)CardPadding, (int)CardPadding, (int)CardPadding, (int)CardPadding);

            stylesInitialized = true;
        }
        #endregion

        #region GUI Drawing
        private void OnGUI()
        {
            if (!isDisplayVisibleAtAll || !isDisplayVisible) return;

            InitializeStyles();

            Color originalColor = GUI.color;
            float opacity = ModConfig.BigUIOpacity / 100f;
            GUI.color = new Color(originalColor.r, originalColor.g, originalColor.b, opacity);

            // Calculate background rect with dynamic height based on content
            float marginX = Screen.width * BASE_MARGIN_PERCENT;
            CalculateContentHeight();
            float backgroundHeight = Mathf.Min(Total_Height + BASE_BACKGROUND_PADDING, Screen.height * BASE_MAX_HEIGHT_PERCENT);
            float backgroundY = (Screen.height - backgroundHeight) * 0.5f;

            Rect backgroundRect = new Rect(marginX, backgroundY, Screen.width - (marginX * 2), backgroundHeight);

            GUI.Box(backgroundRect, "", backgroundStyle);

            // Calculate content area within the background
            float contentPadding = BASE_CONTENT_PADDING;
            float contentWidth = backgroundRect.width - (contentPadding * 2);
            float availableContentHeight = backgroundRect.height - (contentPadding * 2);

            GUILayout.BeginArea(new Rect(backgroundRect.x + contentPadding, backgroundRect.y + contentPadding, contentWidth, availableContentHeight));

            var statsSnapshot = StatsManager.Instance.GetStatsSnapshot();

            // Draw survival and enemy stats horizontally if both are enabled
            if (ModConfig.ShowPlayTime && ModConfig.ShowEnemyDeaths)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(contentWidth * WIDTH_CARD_HALF));
                DrawSurvivalModeStats(statsSnapshot);
                GUILayout.EndVertical();

                GUILayout.Space(contentWidth * SPACING_CARD_PERCENT);

                GUILayout.BeginVertical(GUILayout.Width(contentWidth * WIDTH_CARD_HALF));
                DrawEnemyStats(statsSnapshot);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.Space(SectionSpacing);
            }
            else if (ModConfig.ShowPlayTime)
            {
                DrawSurvivalModeStats(statsSnapshot);
                GUILayout.Space(SectionSpacing);
            }
            else if (ModConfig.ShowEnemyDeaths)
            {
                DrawEnemyStats(statsSnapshot);
                GUILayout.Space(SectionSpacing);
            }

            if (ModConfig.ShowPlayers)
            {
                DrawPlayerStats(statsSnapshot);
            }

            GUILayout.EndArea();

            GUI.color = originalColor;
        }

        private void CalculateContentHeight()
        {
            float totalHeight = 0f;

            if (ModConfig.ShowPlayTime || ModConfig.ShowEnemyDeaths)
            {
                totalHeight += SurvivalSectionHeight + SectionSpacing;
            }

            if (ModConfig.ShowPlayers)
            {
                var statsSnapshot = StatsManager.Instance?.GetStatsSnapshot();
                float playerSectionHeight = UIManager.ScaleValue(120f); // Base height for player section
                if (statsSnapshot?.ActivePlayers != null)
                {
                    playerSectionHeight += statsSnapshot.ActivePlayers.Count * PlayerRowHeight;
                }
                totalHeight += playerSectionHeight;
            }

            Total_Height = totalHeight;
        }

        private void DrawSurvivalModeStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label(LABEL_SURVIVAL_MODE, headerStyle);

            GUILayout.BeginHorizontal();
            if (statsSnapshot.IsSurvivalActive)
            {
                GUILayout.Label(LABEL_TIME, labelStyle, GUILayout.Width(WIDTH_TIME_LABEL));
                var timerStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Green } };
                GUILayout.Label(FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle);
            }
            else
            {
                GUILayout.Label(LABEL_LAST_GAME, labelStyle, GUILayout.Width(WIDTH_LAST_GAME_LABEL));
                var statusStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Gray } };
                GUILayout.Label(statsSnapshot.LastGameDuration.TotalSeconds > 0 ? FormatTimeSpan(statsSnapshot.LastGameDuration) : LABEL_NO_GAMES, statusStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawEnemyStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label(LABEL_ENEMY_STATISTICS, headerStyle);

            try
            {
                int enemiesKilled = statsSnapshot.EnemiesKilled;
                GUILayout.BeginHorizontal();
                GUILayout.Label(LABEL_ENEMIES_KILLED, labelStyle, GUILayout.Width(WIDTH_ENEMIES_LABEL));
                var killsStyle = new GUIStyle(valueStyle) { normal = { textColor = enemiesKilled > 0 ? UIManager.Green : UIManager.White } };
                GUILayout.Label(enemiesKilled.ToString(), killsStyle);
                GUILayout.EndHorizontal();
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", errorStyle);
                Logger.LogError($"Error displaying enemy stats: {ex.Message}");
            }
            GUILayout.EndVertical();
        }

        private void DrawPlayerStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            try
            {
                if (statsSnapshot.ActivePlayers != null && statsSnapshot.ActivePlayers.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(LABEL_PLAYER, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PLAYER_NAME)));
                    GUILayout.Label(LABEL_DEATHS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_DEATHS)));
                    GUILayout.Label(LABEL_KILLS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_KILLS)));
                    GUILayout.Label(LABEL_PVP, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PVP)));
                    GUILayout.Label(LABEL_ENEMY_SHIELDS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_ENEMY_SHIELDS)));
                    GUILayout.Label(LABEL_PLAYER_SHIELDS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PLAYER_SHIELDS)));
                    GUILayout.Label(LABEL_SHIELDS_LOST, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_SHIELDS_LOST)));
                    GUILayout.Label(LABEL_ALIVE_TIME, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_ALIVE_TIME)));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(SPACING_BETWEEN_HEADERS);

                    foreach (var playerEntry in statsSnapshot.ActivePlayers)
                    {
                        var playerData = playerEntry.Value;

                        GUILayout.BeginHorizontal();
                        var playerNameStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.PlayerColor }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(playerData.PlayerName, playerNameStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PLAYER_NAME)));

                        var deathsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Deaths > 0 ? UIManager.Red : UIManager.White }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(playerData.Deaths.ToString(), deathsStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_DEATHS)));

                        var killsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Kills > 0 ? UIManager.Green : UIManager.White }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(playerData.Kills.ToString(), killsStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_KILLS)));

                        var friendlyKillsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.FriendlyKills > 0 ? UIManager.Orange : UIManager.White }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(playerData.FriendlyKills.ToString(), friendlyKillsStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PVP)));

                        var enemyShieldsStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(playerData.EnemyShieldsTakenDown.ToString(), enemyShieldsStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_ENEMY_SHIELDS)));

                        var friendlyShieldsStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(playerData.FriendlyShieldsHit.ToString(), friendlyShieldsStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PLAYER_SHIELDS)));

                        var shieldsLostStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(playerData.ShieldsLost.ToString(), shieldsLostStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_SHIELDS_LOST)));

                        var aliveTimeStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
                        GUILayout.Label(FormatTimeSpan(playerData.GetCurrentAliveTime()), aliveTimeStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_ALIVE_TIME)));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(SPACING_BETWEEN_PLAYERS);
                    }
                }
                else
                {
                    GUILayout.Label(LABEL_NO_PLAYERS, labelStyle);
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

        #region Utility Methods
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
        #endregion

        #region Event Handling
        public void OnPlayerJoined()
        {
            CalculateContentHeight();
        }

        public void OnPlayerLeft()
        {
            CalculateContentHeight();
        }
        #endregion
    }
}
