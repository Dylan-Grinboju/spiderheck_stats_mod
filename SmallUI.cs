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
        private bool stylesInitialized = false;
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the stats window using persisted configuration and compute its initial size.
        /// </summary>
        /// <remarks>
        /// Reads saved window position and visibility from ModConfig, creates the initial window rectangle with the scaled width, updates the window height (via UpdateWindowSize), and marks the display visibility flag. This should be called once during setup before the UI is shown.
        /// </remarks>
        public void Initialize()
        {
            float xPos = ModConfig.DisplayPositionX;
            float yPos = ModConfig.DisplayPositionY;

            windowRect = new Rect(xPos, yPos, WindowWidth, 100f);
            isDisplayVisibleAtAll = ModConfig.ShowStatsWindow;
            UpdateWindowSize();

            Logger.LogInfo("SmallUI initialized");
        }
        #endregion

        #region Display Control
        /// <summary>
        /// Toggles the small stats window's local visibility flag on or off.
        /// </summary>
        /// <remarks>
        /// This flips the internal <c>isDisplayVisible</c> state only; visibility returned by <see cref="IsVisible"/> also depends on <c>isDisplayVisibleAtAll</c>. This method does not persist the change to configuration.
        /// </remarks>
        public void ToggleDisplay()
        {
            isDisplayVisible = !isDisplayVisible;
        }

        /// <summary>
        /// Makes the stats window visible (marks it to be shown on the next GUI update).
        /// </summary>
        public void ShowDisplay()
        {
            isDisplayVisible = true;
        }

        /// <summary>
        /// Hides the statistics window by setting its visible flag to false.
        /// </summary>
        public void HideDisplay()
        {
            isDisplayVisible = false;
        }

        /// <summary>
        /// Returns true when the stats window is both globally enabled and currently shown.
        /// </summary>
        /// <returns>True if the display is enabled in configuration and currently visible; otherwise false.</returns>
        public bool IsVisible()
        {
            return isDisplayVisibleAtAll && isDisplayVisible;
        }
        #endregion

        #region Style Management
        /// <summary>
        /// Lazily creates and caches the GUI styles used by the stats window (window, header, label, value, card, error).
        /// </summary>
        /// <remarks>
        /// This method is idempotent — calling it multiple times has no effect after styles have been initialized.
        /// Styles are obtained from <c>UIManager.Instance</c> and stored on the instance for later use.
        /// </remarks>
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            windowStyle = UIManager.Instance.CreateWindowStyle(UIManager.Instance.GetDarkTexture());
            headerStyle = UIManager.Instance.CreateHeaderStyle();
            labelStyle = UIManager.Instance.CreateLabelStyle();
            valueStyle = UIManager.Instance.CreateValueStyle();
            cardStyle = UIManager.Instance.CreateCardStyle(UIManager.Instance.GetMediumTexture());
            errorStyle = UIManager.Instance.CreateErrorStyle();

            stylesInitialized = true;
        }
        #endregion

        #region GUI Drawing
        /// <summary>
        /// Unity OnGUI callback that renders and manages the movable stats window when it is enabled.
        /// </summary>
        /// <remarks>
        /// When both internal visibility flags allow display, this method ensures GUI styles are initialized,
        /// updates the window size, draws the window using <see cref="DrawStatsWindow"/>, and persists the
        /// window position to <c>ModConfig</c> if the user has moved it.
        /// </remarks>
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

        /// <summary>
        /// Renders the contents of the stats window: a vertical layout that conditionally draws
        /// the Survival Mode, Enemy Statistics, and Player Statistics sections based on ModConfig,
        /// then enables window dragging.
        /// </summary>
        /// <param name="windowID">The GUI window ID provided by Unity's GUI.Window callback (unused by this method).</param>
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
        /// <summary>
        /// Renders the "Survival Mode" UI card showing either the current session elapsed time (when a survival session is active)
        /// or the last completed game's duration (or "No games yet" if none exist).
        /// </summary>
        /// <param name="statsSnapshot">Snapshot containing survival session state and timing values (IsSurvivalActive, CurrentSessionTime, LastGameDuration).</param>
        private void DrawSurvivalModeStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Survival Mode", headerStyle);

            GUILayout.BeginHorizontal();
            if (statsSnapshot.IsSurvivalActive)
            {
                GUILayout.Label("Time:", labelStyle, GUILayout.Width(UIManager.ScaleValue(50)));
                var timerStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Green } };
                GUILayout.Label(FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle, GUILayout.MinWidth(UIManager.ScaleValue(80)));
            }
            else
            {
                GUILayout.Label("Last Game:", labelStyle, GUILayout.Width(UIManager.ScaleValue(120)));
                var statusStyle = new GUIStyle(valueStyle) { normal = { textColor = UIManager.Gray } };
                GUILayout.Label(statsSnapshot.LastGameDuration.TotalSeconds > 0 ? FormatTimeSpan(statsSnapshot.LastGameDuration) : "No games yet", statusStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(UIManager.ScaleValue(4));
        }

        /// <summary>
        /// Renders the "Enemy Statistics" card showing the total enemies killed.
        /// </summary>
        /// <param name="statsSnapshot">Snapshot containing current enemy statistics (uses <see cref="GameStatsSnapshot.EnemiesKilled"/>).</param>
        /// <remarks>
        /// If reading the snapshot fails, an inline error label is shown and the exception is logged via <c>Logger</c>.
        /// </remarks>
        private void DrawEnemyStats(GameStatsSnapshot statsSnapshot)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Enemy Statistics", headerStyle);

            try
            {
                int enemiesKilled = statsSnapshot.EnemiesKilled;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Enemies Killed:", labelStyle, GUILayout.Width(UIManager.ScaleValue(120)));
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
            GUILayout.Space(UIManager.ScaleValue(4));
        }

        /// <summary>
        /// Renders the "Player Statistics" card in the stats window showing each active player's name, deaths, and kills.
        /// </summary>
        /// <param name="statsSnapshot">A snapshot of current game statistics; used to enumerate ActivePlayers and their per-player stats. If null or contains no players, a "No players connected" message is shown.</param>
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

                        var playerNameStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.PlayerColor } };
                        GUILayout.Label(playerData.PlayerName, playerNameStyle, GUILayout.Width(UIManager.ScaleValue(115)));

                        var deathsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Deaths > 0 ? UIManager.Red : UIManager.White } };
                        GUILayout.Label(playerData.Deaths.ToString(), deathsStyle, GUILayout.Width(UIManager.ScaleValue(90)));

                        var killsStyle = new GUIStyle(valueStyle) { normal = { textColor = playerData.Kills > 0 ? UIManager.Green : UIManager.White } };
                        GUILayout.Label(playerData.Kills.ToString(), killsStyle, GUILayout.Width(UIManager.ScaleValue(60)));
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

        #region Utility Methods
        /// <summary>
        /// Formats a <see cref="TimeSpan"/> as a zero-padded "HH:MM:SS" string.
        /// </summary>
        /// <param name="timeSpan">The timespan to format. Uses the <see cref="TimeSpan.Hours"/>, <see cref="TimeSpan.Minutes"/> and <see cref="TimeSpan.Seconds"/> components.</param>
        /// <returns>A string in the form "HH:MM:SS". Note: hours come from <see cref="TimeSpan.Hours"/> (does not include days).</returns>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
        #endregion

        #region Window Size Management
        /// <summary>
        /// Recomputes and applies the UI window height based on enabled sections and active players.
        /// </summary>
        /// <remarks>
        /// Adds the base window height plus optional section heights when their corresponding
        /// ModConfig flags are enabled: play time, enemy deaths, and player stats. For the
        /// player-stats section, includes a per-player height multiplied by the current number
        /// of active players from StatsManager (if available). The computed value is assigned
        /// to <c>windowRect.height</c>.
        /// </remarks>
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

        /// <summary>
        /// Notifies the UI that a player has joined and recomputes the stats window height accordingly.
        /// </summary>
        public void OnPlayerJoined()
        {
            UpdateWindowSize();
        }

        /// <summary>
        /// Notifies the UI that a player has left and recomputes the stats window size accordingly.
        /// </summary>
        public void OnPlayerLeft()
        {
            UpdateWindowSize();
        }
        #endregion
    }
}
