using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using System;

namespace StatsMod
{
    public class BigUI : MonoBehaviour
    {
        #region Constants
        private const float BASE_SECTION_SPACING = 25f;
        private const float BASE_CARD_PADDING = 20f;
        private const float BASE_PLAYER_ROW_HEIGHT = 40f;
        private const float BASE_SURVIVAL_SECTION_HEIGHT = 100f;

        // Scaled properties
        private float SectionSpacing => UIManager.ScaleValue(BASE_SECTION_SPACING);
        private float CardPadding => UIManager.ScaleValue(BASE_CARD_PADDING);
        private float PlayerRowHeight => UIManager.ScaleValue(BASE_PLAYER_ROW_HEIGHT);
        private float SurvivalSectionHeight => UIManager.ScaleValue(BASE_SURVIVAL_SECTION_HEIGHT);

        private float Total_Height = 0f;
        #endregion

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
        /// <summary>
        /// Initializes the BigUI component by reading the configuration to determine whether the stats window is allowed to be shown.
        /// </summary>
        /// <remarks>
        /// Reads ModConfig.ShowStatsWindow and stores the result in <c>isDisplayVisibleAtAll</c>. Also emits an informational log entry.
        /// </remarks>
        public void Initialize()
        {
            isDisplayVisibleAtAll = ModConfig.ShowStatsWindow;
            Logger.LogInfo("BigUI initialized");
        }
        #endregion

        #region Display Control
        /// <summary>
        /// Toggles the in-memory visibility state of the stats window.
        /// </summary>
        public void ToggleDisplay()
        {
            isDisplayVisible = !isDisplayVisible;
        }

        /// <summary>
        /// Marks the stats window as visible. The window will actually be shown only if display is allowed by configuration (see <see cref="IsVisible"/>).
        /// </summary>
        public void ShowDisplay()
        {
            isDisplayVisible = true;
        }

        /// <summary>
        /// Hides the stats window for the current session by setting the in-memory visible flag to false.
        /// </summary>
        /// <remarks>
        /// This does not modify persistent configuration (e.g., ModConfig.ShowStatsWindow) or the overall allowance
        /// for the UI to be displayed; it only toggles the runtime visibility state.
        /// </remarks>
        public void HideDisplay()
        {
            isDisplayVisible = false;
        }

        /// <summary>
        /// Indicates whether the stats window should be shown (both globally allowed and currently toggled on).
        /// </summary>
        /// <returns>True if the UI is permitted to display by config and is currently toggled visible; otherwise false.</returns>
        public bool IsVisible()
        {
            return isDisplayVisibleAtAll && isDisplayVisible;
        }
        #endregion

        #region Style Management
        /// <summary>
        /// Lazily creates and configures the GUIStyle instances used by the UI (background, header, label, value, card, error).
        /// </summary>
        /// <remarks>
        /// This method is idempotent — it returns immediately if styles have already been initialized. Styles are created via UIManager and the card style's padding is set using the scaled CardPadding value. Marks the styles as initialized when complete.
        /// </remarks>
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            backgroundStyle = new GUIStyle()
            {
                normal = { background = UIManager.Instance.GetDarkTexture() }
            };

            headerStyle = UIManager.Instance.CreateHeaderStyle(UIManager.BIG_HEADER_SIZE);
            labelStyle = UIManager.Instance.CreateLabelStyle(UIManager.BIG_LABEL_SIZE);
            valueStyle = UIManager.Instance.CreateValueStyle(UIManager.BIG_LABEL_SIZE);
            cardStyle = UIManager.Instance.CreateCardStyle(UIManager.Instance.GetMediumTexture());
            errorStyle = UIManager.Instance.CreateErrorStyle(UIManager.BIG_LABEL_SIZE);

            cardStyle.padding = new RectOffset((int)CardPadding, (int)CardPadding, (int)CardPadding, (int)CardPadding);

            stylesInitialized = true;
        }
        #endregion

        #region GUI Drawing
        /// <summary>
        /// Renders the statistics window using Unity IMGUI when the UI is enabled.
        /// </summary>
        /// <remarks>
        /// Only draws when both the global allow flag and the local visibility flag are true.
        /// The method ensures styles are initialized, computes the dynamic content height, and
        /// lays out a centered background box with padded content. It queries the current
        /// StatsSnapshot and conditionally renders the Survival, Enemy, and Player sections
        /// according to ModConfig:
        /// - If both ShowPlayTime and ShowEnemyDeaths are enabled, Survival and Enemy stats
        ///   are shown side-by-side in two columns.
        /// - If only one is enabled, that section is shown full-width.
        /// - Player stats are rendered below when ShowPlayers is enabled.
        /// </remarks>
        private void OnGUI()
        {
            if (!isDisplayVisibleAtAll || !isDisplayVisible) return;

            InitializeStyles();

            // Calculate background rect with dynamic height based on content
            float marginX = Screen.width * 0.2f;
            CalculateContentHeight();
            float backgroundHeight = Mathf.Min(Total_Height + 80f, Screen.height * 0.8f); // Add padding and cap at 80% of screen
            float backgroundY = (Screen.height - backgroundHeight) * 0.5f; // Center vertically

            Rect backgroundRect = new Rect(marginX, backgroundY, Screen.width - (marginX * 2), backgroundHeight);

            GUI.Box(backgroundRect, "", backgroundStyle);

            // Calculate content area within the background
            float contentPadding = 40f;
            float contentWidth = backgroundRect.width - (contentPadding * 2);
            float availableContentHeight = backgroundRect.height - (contentPadding * 2);

            GUILayout.BeginArea(new Rect(backgroundRect.x + contentPadding, backgroundRect.y + contentPadding, contentWidth, availableContentHeight));

            var statsSnapshot = StatsManager.Instance.GetStatsSnapshot();

            // Draw survival and enemy stats horizontally if both are enabled
            if (ModConfig.ShowPlayTime && ModConfig.ShowEnemyDeaths)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(contentWidth * 0.48f));
                DrawSurvivalModeStats(statsSnapshot);
                GUILayout.EndVertical();

                GUILayout.Space(contentWidth * 0.04f); // 4% spacing between cards

                GUILayout.BeginVertical(GUILayout.Width(contentWidth * 0.48f));
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
        }

        /// <summary>
        /// Recalculates and updates <c>Total_Height</c> to match the currently enabled UI sections and active players.
        /// </summary>
        /// <remarks>
        /// Adds the scaled survival/enemy section height (when either <c>ModConfig.ShowPlayTime</c> or
        /// <c>ModConfig.ShowEnemyDeaths</c> is enabled) and the player section height (when
        /// <c>ModConfig.ShowPlayers</c> is enabled). The player section uses a base scaled height plus one
        /// row per active player obtained from <c>StatsManager.Instance.GetStatsSnapshot()</c>, with row
        /// heights scaled via <c>PlayerRowHeight</c>.
        /// </remarks>
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

        /// <summary>
        /// Renders the "Survival Mode" card into the current GUILayout area, showing either the active session timer or the last game duration (or "No games yet") depending on whether survival mode is active.
        /// </summary>
        /// <param name="statsSnapshot">Snapshot of current statistics used to determine survival activity and durations.</param>
        private void DrawSurvivalModeStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Survival Mode", headerStyle);

            GUILayout.BeginHorizontal();
            if (statsSnapshot.IsSurvivalActive)
            {
                GUILayout.Label("Time:", labelStyle, GUILayout.Width(100));
                var timerStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Green } };
                GUILayout.Label(FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle);
            }
            else
            {
                GUILayout.Label("Last Game:", labelStyle, GUILayout.Width(300));
                var statusStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Gray } };
                GUILayout.Label(statsSnapshot.LastGameDuration.TotalSeconds > 0 ? FormatTimeSpan(statsSnapshot.LastGameDuration) : "No games yet", statusStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Renders the "Enemy Statistics" card showing the total enemies killed.
        /// </summary>
        /// <param name="statsSnapshot">The snapshot containing enemy statistics (expects <c>EnemiesKilled</c>); may be null or incomplete — rendering failures are caught and shown as an error label.</param>
        private void DrawEnemyStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Enemy Statistics", headerStyle);

            try
            {
                int enemiesKilled = statsSnapshot.EnemiesKilled;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Enemies Killed:", labelStyle, GUILayout.Width(300));
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

        /// <summary>
        /// Renders the "Player Statistics" card showing each active player's name, deaths, and kills.
        /// </summary>
        /// <param name="statsSnapshot">Snapshot containing ActivePlayers and their per-player stats used to populate the rows.</param>
        /// <remarks>
        /// If no players are connected a "No players connected" message is shown. Any exception thrown while rendering
        /// is caught: an error label is displayed and the error is logged.
        /// </remarks>
        private void DrawPlayerStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            try
            {
                if (statsSnapshot.ActivePlayers != null && statsSnapshot.ActivePlayers.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Player", headerStyle, GUILayout.Width(UIManager.ScaleValue(180)));
                    GUILayout.Label("Deaths", headerStyle, GUILayout.Width(UIManager.ScaleValue(150)));
                    GUILayout.Label("Kills", headerStyle, GUILayout.Width(UIManager.ScaleValue(120)));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    foreach (var playerEntry in statsSnapshot.ActivePlayers)
                    {
                        var playerData = playerEntry.Value;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", valueStyle, GUILayout.Width(UIManager.ScaleValue(5)));

                        var playerNameStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.PlayerColor } };
                        GUILayout.Label(playerData.PlayerName, playerNameStyle, GUILayout.Width(UIManager.ScaleValue(205)));

                        var deathsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Deaths > 0 ? UIManager.Red : UIManager.White } };
                        GUILayout.Label(playerData.Deaths.ToString(), deathsStyle, GUILayout.Width(UIManager.ScaleValue(140)));

                        var killsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Kills > 0 ? UIManager.Green : UIManager.White } };
                        GUILayout.Label(playerData.Kills.ToString(), killsStyle, GUILayout.Width(UIManager.ScaleValue(120)));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(15);
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

        #region Utility Methods
        /// <summary>
        /// Formats a TimeSpan as a zero-padded hours:minutes:seconds string (HH:MM:SS).
        /// </summary>
        /// <param name="timeSpan">The TimeSpan to format.</param>
        /// <returns>A string representing the timespan in "HH:MM:SS" format.</returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Recalculates the UI content height when a player joins so the window layout accounts for the new player row.
        /// </summary>
        public void OnPlayerJoined()
        {
            CalculateContentHeight();
        }

        /// <summary>
        /// Notifies the UI that a player has left and recalculates the window's content height.
        /// </summary>
        /// <remarks>
        /// Call this when a player disconnects so the layout updates to reflect the reduced player list.
        /// </remarks>
        public void OnPlayerLeft()
        {
            CalculateContentHeight();
        }
        #endregion
    }
}
