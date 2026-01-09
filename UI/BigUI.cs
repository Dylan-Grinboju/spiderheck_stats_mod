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
        public float COL_WIDTH_PLAYER_SHIELDS = 110f;
        public float COL_WIDTH_SHIELDS_LOST = 100f;
        public float COL_WIDTH_ALIVE_TIME = 120f;
        public float COL_WIDTH_KILL_STREAK = 100f;
        public float COL_WIDTH_INDENT = 30f;
        #endregion

        #region Text Labels
        public string LABEL_PLAYER = "Player";
        public string LABEL_DEATHS = "Deaths";
        public string LABEL_KILLS = "Kills";
        public string LABEL_PVP = "Friendly Kills";
        public string LABEL_ENEMY_SHIELDS = "Enemy Shields";
        public string LABEL_PLAYER_SHIELDS = "Friendly Shields";
        public string LABEL_SHIELDS_LOST = "Shields Lost";
        public string LABEL_ALIVE_TIME = "Alive Time";
        public string LABEL_KILL_STREAK = "Max Kill Streak";
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
        public float SPACING_ENEMIES_VALUE = 25f;
        #endregion

        #region Other Widths
        public float WIDTH_TIME_LABEL = 150f;
        public float WIDTH_LAST_GAME_LABEL = 300f;

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
        private GUIStyle timerStyle;
        private GUIStyle statusStyle;
        private GUIStyle killsGreenStyle;
        private GUIStyle killsWhiteStyle;
        private GUIStyle deathsRedStyle;
        private GUIStyle deathsWhiteStyle;
        private GUIStyle friendlyKillsOrangeStyle;
        private GUIStyle friendlyKillsWhiteStyle;
        private GUIStyle centeredWhiteStyle;
        private GUIStyle dynamicPlayerNameStyle;

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

            timerStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Green } };
            statusStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Gray } };
            killsGreenStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Green }, alignment = TextAnchor.MiddleCenter };
            killsWhiteStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
            deathsRedStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Red }, alignment = TextAnchor.MiddleCenter };
            deathsWhiteStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
            friendlyKillsOrangeStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Orange }, alignment = TextAnchor.MiddleCenter };
            friendlyKillsWhiteStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
            centeredWhiteStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.White }, alignment = TextAnchor.MiddleCenter };
            dynamicPlayerNameStyle = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleCenter };


            stylesInitialized = true;
        }

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

            var statsSnapshot = GameSessionManager.Instance.GetStatsSnapshot();

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
                var statsSnapshot = GameSessionManager.Instance?.GetStatsSnapshot();
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
                GUILayout.Label(TimeFormatUtils.FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle);
            }
            else
            {
                GUILayout.Label(LABEL_LAST_GAME, labelStyle, GUILayout.Width(WIDTH_LAST_GAME_LABEL));
                GUILayout.Label(statsSnapshot.LastGameDuration.TotalSeconds > 0 ? TimeFormatUtils.FormatTimeSpan(statsSnapshot.LastGameDuration) : LABEL_NO_GAMES, statusStyle);
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
                GUILayout.Label(LABEL_ENEMIES_KILLED, labelStyle, GUILayout.ExpandWidth(false));
                GUILayout.Space(SPACING_ENEMIES_VALUE);
                GUILayout.Label(enemiesKilled.ToString(), enemiesKilled > 0 ? killsGreenStyle : killsWhiteStyle, GUILayout.ExpandWidth(false));
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
                    GUILayout.Label(LABEL_KILLS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_KILLS)));
                    GUILayout.Label(LABEL_DEATHS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_DEATHS)));
                    GUILayout.Label(LABEL_KILL_STREAK, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_KILL_STREAK)));
                    GUILayout.Label(LABEL_PVP, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PVP)));
                    GUILayout.Label(LABEL_ENEMY_SHIELDS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_ENEMY_SHIELDS)));
                    GUILayout.Label(LABEL_SHIELDS_LOST, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_SHIELDS_LOST)));
                    GUILayout.Label(LABEL_PLAYER_SHIELDS, headerStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PLAYER_SHIELDS)));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(SPACING_BETWEEN_HEADERS);

                    foreach (var playerEntry in statsSnapshot.ActivePlayers)
                    {
                        var playerData = playerEntry.Value;

                        GUILayout.BeginHorizontal();
                        // Player name style needs dynamic color per-player
                        dynamicPlayerNameStyle.normal.textColor = playerData.PlayerColor;
                        GUILayout.Label(playerData.PlayerName, dynamicPlayerNameStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PLAYER_NAME)));

                        GUILayout.Label(playerData.Kills.ToString(), playerData.Kills > 0 ? killsGreenStyle : killsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_KILLS)));
                        GUILayout.Label(playerData.Deaths.ToString(), playerData.Deaths > 0 ? deathsRedStyle : deathsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_DEATHS)));
                        GUILayout.Label(playerData.MaxKillStreak.ToString(), playerData.MaxKillStreak > 0 ? killsGreenStyle : killsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_KILL_STREAK)));
                        GUILayout.Label(playerData.FriendlyKills.ToString(), playerData.FriendlyKills > 0 ? friendlyKillsOrangeStyle : friendlyKillsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PVP)));
                        GUILayout.Label(playerData.EnemyShieldsTakenDown.ToString(), playerData.EnemyShieldsTakenDown > 0 ? killsGreenStyle : killsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_ENEMY_SHIELDS)));
                        GUILayout.Label(playerData.ShieldsLost.ToString(), playerData.ShieldsLost > 0 ? deathsRedStyle : deathsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_SHIELDS_LOST)));
                        GUILayout.Label(playerData.FriendlyShieldsHit.ToString(), playerData.FriendlyShieldsHit > 0 ? friendlyKillsOrangeStyle : friendlyKillsWhiteStyle, GUILayout.Width(UIManager.ScaleValue(COL_WIDTH_PLAYER_SHIELDS)));
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
