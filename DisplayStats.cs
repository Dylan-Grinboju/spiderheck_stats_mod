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
        // UI Size Constants
        private const float SIZE_SCALE_FACTOR = 1.5f;
        private const float WINDOW_WIDTH = 300f;
        private const float BASE_WINDOW_HEIGHT = 30f;

        // Font Sizes
        private const int HEADER_FONT_SIZE = 16;
        private const int LABEL_FONT_SIZE = 14;

        // Spacing Constants
        private const float SECTION_SPACING = 4f;
        private const float PLAYER_ROW_SPACING = 8f;
        private const float CARD_PADDING = 8f;
        private const float CARD_MARGIN = 2f;
        private const float SEPARATOR_HEIGHT = 1f;
        private const float SEPARATOR_MARGIN = 8f;
        private const float TITLE_BAR_HEIGHT = 20f;

        // Material Design Colors
        private static readonly Color MaterialPrimary = new Color(0.259f, 0.522f, 0.957f, 1f);
        private static readonly Color MaterialAccent = new Color(1f, 0.341f, 0.133f, 1f);
        private static readonly Color MaterialSurface = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        private static readonly Color MaterialSurfaceVariant = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        private static readonly Color MaterialOnSurface = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color MaterialOnSurfaceVariant = new Color(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color MaterialPositive = new Color(0.298f, 0.686f, 0.314f, 1f);
        private static readonly Color MaterialSeparator = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        #endregion

        #region Instance Management
        private static DisplayStats _instance;
        public static DisplayStats Instance { get; private set; }
        #endregion

        #region UI State
        private bool isDisplayVisible = false;
        private bool isEnlarged = false;
        private Rect windowRect;
        private Rect normalWindowRect;
        private Rect enlargedWindowRect;
        #endregion

        #region GUI Styles and Resources
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle cardStyle;
        private GUIStyle separatorStyle;
        private bool stylesInitialized = false;

        private Texture2D surfaceTexture;
        private Texture2D surfaceVariantTexture;
        private Texture2D separatorTexture;
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

                Instance.normalWindowRect = new Rect(xPos, yPos, WINDOW_WIDTH, BASE_WINDOW_HEIGHT);
                Instance.enlargedWindowRect = new Rect(
                    xPos - (WINDOW_WIDTH * (SIZE_SCALE_FACTOR - 1) / 2),
                    yPos - (BASE_WINDOW_HEIGHT * (SIZE_SCALE_FACTOR - 1) / 2),
                    WINDOW_WIDTH * SIZE_SCALE_FACTOR,
                    BASE_WINDOW_HEIGHT * SIZE_SCALE_FACTOR);

                Instance.windowRect = Instance.normalWindowRect;
                Instance.isDisplayVisible = ModConfig.ShowStats;

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
                if (isDisplayVisible && isEnlarged)
                {
                    SetSizeMode(false);
                }
                else
                {
                    ToggleDisplay(false);
                }
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                if (isDisplayVisible && !isEnlarged)
                {
                    SetSizeMode(true);
                }
                else
                {
                    ToggleDisplay(true);
                }
            }
        }

        private void ToggleDisplay(bool enlarged)
        {
            isDisplayVisible = !isDisplayVisible;
            if (isDisplayVisible)
            {
                SetSizeMode(enlarged);
            }
        }

        private void SetSizeMode(bool enlarged)
        {
            isEnlarged = enlarged;
            stylesInitialized = false;
            UpdateWindowSize();
        }

        public void AutoPullHUD()
        {
            isDisplayVisible = true;
            SetSizeMode(true);
        }

        public void HideHUD()
        {
            isDisplayVisible = false;
            SetSizeMode(false);
        }
        #endregion

        #region Style Management
        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            CreateMaterialTextures();

            int basePadding = Mathf.RoundToInt(CARD_PADDING);
            int largePadding = basePadding * 2;

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = surfaceTexture },
                padding = new RectOffset(largePadding, largePadding, largePadding,
                                       isEnlarged ? largePadding + basePadding : largePadding),
                border = new RectOffset(2, 2, 2, 2)
            };

            headerStyle = CreateScaledLabelStyle(HEADER_FONT_SIZE, FontStyle.Bold, MaterialPrimary, 6, 0, 4, 2, 0, 0, 4, 2);
            labelStyle = CreateScaledLabelStyle(LABEL_FONT_SIZE, FontStyle.Normal, MaterialOnSurfaceVariant, basePadding, 0, 2, 2, 0, 0, 1, 1);
            valueStyle = CreateScaledLabelStyle(LABEL_FONT_SIZE, FontStyle.Bold, MaterialOnSurface, 0, basePadding, 2, 2, 0, 0, 1, 1);

            cardStyle = new GUIStyle()
            {
                normal = { background = surfaceVariantTexture },
                padding = new RectOffset(basePadding, basePadding, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            separatorStyle = new GUIStyle()
            {
                normal = { background = separatorTexture },
                fixedHeight = SEPARATOR_HEIGHT,
                margin = new RectOffset(Mathf.RoundToInt(SEPARATOR_MARGIN), Mathf.RoundToInt(SEPARATOR_MARGIN),
                                       Mathf.RoundToInt(SEPARATOR_MARGIN), Mathf.RoundToInt(SEPARATOR_MARGIN))
            };

            stylesInitialized = true;
        }

        private GUIStyle CreateScaledLabelStyle(int baseFontSize, FontStyle fontStyle, Color textColor,
                                              int paddingLeft, int paddingRight, int paddingTop, int paddingBottom,
                                              int marginLeft, int marginRight, int marginTop, int marginBottom)
        {
            float fontScale = isEnlarged ? SIZE_SCALE_FACTOR : 1f;
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(baseFontSize * fontScale),
                fontStyle = fontStyle,
                alignment = fontStyle == FontStyle.Bold ? TextAnchor.MiddleLeft : TextAnchor.UpperLeft,
                normal = { textColor = textColor },
                padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom),
                margin = new RectOffset(marginLeft, marginRight, marginTop, marginBottom)
            };
        }

        private void CreateMaterialTextures()
        {
            surfaceTexture = CreateSingleColorTexture(MaterialSurface);
            surfaceVariantTexture = CreateSingleColorTexture(MaterialSurfaceVariant);
            separatorTexture = CreateSingleColorTexture(MaterialSeparator);
        }

        private Texture2D CreateSingleColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            if (surfaceTexture != null) Destroy(surfaceTexture);
            if (surfaceVariantTexture != null) Destroy(surfaceVariantTexture);
            if (separatorTexture != null) Destroy(separatorTexture);
        }
        #endregion

        #region GUI Drawing
        private void OnGUI()
        {
            if (!isDisplayVisible || !ModConfig.ShowStats) return;

            InitializeStyles();
            UpdateWindowSize();

            Vector2 oldPosition = new Vector2(windowRect.x, windowRect.y);
            windowRect = GUI.Window(0, windowRect, DrawStatsWindow, "Game Statistics", windowStyle);

            Vector2 newPosition = new Vector2(windowRect.x, windowRect.y);
            if (oldPosition != newPosition)
            {
                ModConfig.SetDisplayPosition((int)newPosition.x, (int)newPosition.y);
                UpdateWindowRectsPosition(newPosition);
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

            if (ModConfig.ShowKillCount)
            {
                DrawEnemyStats(statsSnapshot);
            }

            if (ModConfig.ShowDeathCount)
            {
                DrawPlayerStats(statsSnapshot);
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        #endregion

        private void DrawSurvivalModeStats(GameStatsSnapshot statsSnapshot)
        {
            // Survival Mode Stats Card
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Survival Mode", headerStyle);

            // Single row showing either current timer or last game duration
            GUILayout.BeginHorizontal();
            if (statsSnapshot.IsSurvivalActive)
            {
                GUILayout.Label("Time:", labelStyle, GUILayout.Width(GetScaledWidth(50)));
                GUIStyle timerStyle = new GUIStyle(valueStyle)
                {
                    normal = { textColor = MaterialPositive }
                };
                GUILayout.Label(FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle, GUILayout.MinWidth(GetScaledWidth(80)));
            }
            else
            {
                GUILayout.Label("Last Game:", labelStyle, GUILayout.Width(GetScaledWidth(120)));
                GUIStyle statusStyle = new GUIStyle(valueStyle)
                {
                    normal = { textColor = MaterialOnSurfaceVariant }
                };
                GUILayout.Label(statsSnapshot.LastGameDuration.TotalSeconds > 0 ? FormatTimeSpan(statsSnapshot.LastGameDuration) : "No games yet", statusStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void DrawEnemyStats(GameStatsSnapshot statsSnapshot)
        {
            // Enemy Statistics Card
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Enemy Statistics", headerStyle);

            try
            {
                int enemiesKilled = statsSnapshot.EnemiesKilled;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Enemies Killed:", labelStyle, GUILayout.Width(GetScaledWidth(120)));
                GUIStyle killsStyle = new GUIStyle(valueStyle)
                {
                    normal = { textColor = enemiesKilled > 0 ? MaterialPositive : MaterialOnSurface }
                };
                GUILayout.Label(enemiesKilled.ToString(), killsStyle);
                GUILayout.EndHorizontal();
            }
            catch (System.Exception ex)
            {
                GUIStyle errorStyle = new GUIStyle(labelStyle)
                {
                    normal = { textColor = MaterialAccent }
                };
                GUILayout.Label($"Error: {ex.Message}", errorStyle);
                Logger.LogError($"Error displaying enemy stats: {ex.Message}");
            }
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void DrawPlayerStats(GameStatsSnapshot statsSnapshot)
        {
            // Player Statistics Card
            GUILayout.BeginVertical(cardStyle);
            try
            {
                if (statsSnapshot.ActivePlayers != null && statsSnapshot.ActivePlayers.Count > 0)
                {
                    // Header row
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Player", headerStyle, GUILayout.Width(GetScaledWidth(95)));
                    GUILayout.Label("Deaths", headerStyle, GUILayout.Width(GetScaledWidth(100)));
                    GUILayout.Label("Kills", headerStyle, GUILayout.Width(GetScaledWidth(60)));
                    GUILayout.EndHorizontal();

                    // Add a subtle separator
                    GUILayout.Box("", separatorStyle);

                    foreach (var playerEntry in statsSnapshot.ActivePlayers)
                    {
                        var playerData = playerEntry.Value;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", valueStyle, GUILayout.Width(GetScaledWidth(5)));

                        GUIStyle playerNameStyle = new GUIStyle(valueStyle)
                        {
                            normal = { textColor = playerData.PlayerColor }
                        };
                        GUILayout.Label(playerData.PlayerName, playerNameStyle, GUILayout.Width(GetScaledWidth(115)));

                        GUIStyle deathsStyle = new GUIStyle(valueStyle)
                        {
                            normal = { textColor = playerData.Deaths > 0 ? MaterialAccent : MaterialOnSurface }
                        };
                        GUILayout.Label(playerData.Deaths.ToString(), deathsStyle, GUILayout.Width(GetScaledWidth(90)));

                        GUIStyle killsStyle = new GUIStyle(valueStyle)
                        {
                            normal = { textColor = playerData.Kills > 0 ? MaterialPositive : MaterialOnSurface }
                        };
                        GUILayout.Label(playerData.Kills.ToString(), killsStyle, GUILayout.Width(GetScaledWidth(60)));
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
                GUIStyle errorStyle = new GUIStyle(labelStyle)
                {
                    normal = { textColor = MaterialAccent }
                };
                GUILayout.Label($"Error: {ex.Message}", errorStyle);
                Logger.LogError($"Error displaying player stats: {ex.Message}");
            }
            GUILayout.EndVertical();
        }

        #region Utility Methods
        private float GetScaledWidth(float baseWidth)
        {
            return baseWidth * (isEnlarged ? SIZE_SCALE_FACTOR : 1f);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }
        #endregion

        #region Window Size Management
        private void UpdateWindowSize()
        {
            int playerCount = 0;
            try
            {
                var statsSnapshot = StatsManager.Instance.GetStatsSnapshot();
                playerCount = statsSnapshot.ActivePlayers?.Count ?? 0;
            }
            catch (System.Exception)
            {
                playerCount = 0;
            }

            float baseHeight = CalculateBaseWindowHeight(playerCount);

            if (isEnlarged)
            {
                enlargedWindowRect.height = baseHeight * SIZE_SCALE_FACTOR;
                windowRect = enlargedWindowRect;
            }
            else
            {
                normalWindowRect.height = baseHeight;
                windowRect = normalWindowRect;
            }
        }

        private float CalculateBaseWindowHeight(int playerCount)
        {
            float height = GetBaseWindowChrome();
            int visibleSections = 0;

            if (ModConfig.ShowPlayTime)
            {
                height += GetBaseSectionHeight();
                visibleSections++;
            }

            if (ModConfig.ShowKillCount)
            {
                height += GetBaseSectionHeight();
                visibleSections++;
            }

            if (ModConfig.ShowDeathCount)
            {
                height += GetBasePlayerStatsHeight(playerCount);
                visibleSections++;
            }

            if (visibleSections > 0)
            {
                height += visibleSections * SECTION_SPACING;
            }
            else
            {
                height += 20f; // Minimum content
            }

            return height;
        }

        private float GetBaseWindowChrome()
        {
            // Base window chrome without scaling
            return TITLE_BAR_HEIGHT + BASE_WINDOW_HEIGHT;
        }

        private float GetBaseSectionHeight()
        {
            // Base height for simple sections (Survival Mode and Enemy Stats)
            float cardOverhead = CARD_PADDING + 4f;
            float headerHeight = HEADER_FONT_SIZE + 12f; // Header + padding/margin
            float rowHeight = LABEL_FONT_SIZE + 6f; // Row + padding/margin
            return cardOverhead + headerHeight + rowHeight;
        }

        private float GetBasePlayerStatsHeight(int playerCount)
        {
            float cardOverhead = CARD_PADDING + 4f;

            if (playerCount == 0)
            {
                float noPlayersHeight = LABEL_FONT_SIZE + 6f;
                return cardOverhead + noPlayersHeight;
            }
            else
            {
                float headerRowHeight = HEADER_FONT_SIZE + 12f;
                float separatorHeight = SEPARATOR_HEIGHT + (SEPARATOR_MARGIN * 2);
                float playerRowHeight = LABEL_FONT_SIZE + 6f + PLAYER_ROW_SPACING;
                float allPlayersHeight = playerCount * playerRowHeight;

                return cardOverhead + headerRowHeight + separatorHeight + allPlayersHeight;
            }
        }

        private void UpdateWindowRectsPosition(Vector2 newPosition)
        {
            normalWindowRect.x = newPosition.x;
            normalWindowRect.y = newPosition.y;

            enlargedWindowRect.x = newPosition.x - (WINDOW_WIDTH * (SIZE_SCALE_FACTOR - 1) / 2);
            enlargedWindowRect.y = newPosition.y - (normalWindowRect.height * (SIZE_SCALE_FACTOR - 1) / 2);
        }
        #endregion
    }
}
