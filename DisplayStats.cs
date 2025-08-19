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

        // HUD size management
        private Rect normalWindowRect;
        private Rect enlargedWindowRect;
        private bool isEnlarged = false;
        private float sizeScaleFactor = 1.6f;

        // Material Design Colors
        private static readonly Color MaterialPrimary = new Color(0.259f, 0.522f, 0.957f, 1f); // Blue
        private static readonly Color MaterialPrimaryDark = new Color(0.196f, 0.427f, 0.859f, 1f); // Darker Blue
        private static readonly Color MaterialAccent = new Color(1f, 0.341f, 0.133f, 1f); // Orange
        private static readonly Color MaterialSurface = new Color(0.18f, 0.18f, 0.18f, 0.95f); // Dark surface with transparency
        private static readonly Color MaterialSurfaceVariant = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        private static readonly Color MaterialOnSurface = new Color(0.9f, 0.9f, 0.9f, 1f); // Light text
        private static readonly Color MaterialOnSurfaceVariant = new Color(0.7f, 0.7f, 0.7f, 1f); // Muted text
        private static readonly Color MaterialPositive = new Color(0.298f, 0.686f, 0.314f, 1f); // Green
        private static readonly Color MaterialWarning = new Color(1f, 0.757f, 0.027f, 1f); // Amber

        // Custom GUI Styles
        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle buttonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle cardStyle;
        private GUIStyle separatorStyle;
        private bool stylesInitialized = false;

        // Material Design textures
        private Texture2D surfaceTexture;
        private Texture2D surfaceVariantTexture;
        private Texture2D primaryTexture;
        private Texture2D primaryDarkTexture;
        private Texture2D separatorTexture;

        // Survival Mode tracking
        private bool isSurvivalActive = false;
        private DateTime survivalStartTime;

        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject statsDisplayObj = new GameObject("DisplayPlayerStats");
                _instance = statsDisplayObj.AddComponent<DisplayStats>();
                DontDestroyOnLoad(statsDisplayObj);
                Instance = _instance;

                // Set the window position to top right - normal size
                Instance.normalWindowRect = new Rect(Screen.width - 380, 20, 360, 400);
                // Set enlarged size
                Instance.enlargedWindowRect = new Rect(Screen.width - (int)(380 * Instance.sizeScaleFactor), 20,
                    360 * Instance.sizeScaleFactor, 400 * Instance.sizeScaleFactor);
                // Start with normal size
                Instance.windowRect = Instance.normalWindowRect;

                Logger.LogInfo("Stats Display initialized");
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            // F1 key - toggle UI in small size, or make large UI small
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                if (isDisplayVisible && isEnlarged)
                {
                    // If display is visible and large, make it small
                    isEnlarged = false;
                    windowRect = normalWindowRect;
                    stylesInitialized = false;
                }
                else
                {
                    // Toggle display in small size
                    ToggleDisplaySmall();
                }
            }

            // F2 key - toggle UI in large size, or make small UI large
            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame)
            {
                if (isDisplayVisible && !isEnlarged)
                {
                    // If display is visible and small, make it large
                    isEnlarged = true;
                    windowRect = enlargedWindowRect;
                    stylesInitialized = false;
                }
                else
                {
                    // Toggle display in large size
                    ToggleDisplayLarge();
                }
            }
        }

        private void ToggleDisplaySmall()
        {
            isDisplayVisible = !isDisplayVisible;
            if (isDisplayVisible)
            {
                // Show in small size
                windowRect = normalWindowRect;
                isEnlarged = false;
                stylesInitialized = false;
            }
        }

        private void ToggleDisplayLarge()
        {
            isDisplayVisible = !isDisplayVisible;
            if (isDisplayVisible)
            {
                // Show in large size
                windowRect = enlargedWindowRect;
                isEnlarged = true;
                stylesInitialized = false;
            }
        }
        public void AutoPullHUD()
        {
            isDisplayVisible = true;
            isEnlarged = true;
            windowRect = enlargedWindowRect;
            stylesInitialized = false;
            Logger.LogInfo("Stats HUD automatically pulled up with enlarged size - player stats preserved");
        }

        public void HideHUD()
        {
            isDisplayVisible = false;
            isEnlarged = false;
            stylesInitialized = false;
            Logger.LogInfo("Stats HUD hidden - player stats preserved until new game");
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // Create Material Design textures
            CreateMaterialTextures();

            // Calculate scale factor based on current size mode
            float fontScale = isEnlarged ? sizeScaleFactor : 1f;

            // Material Design window style
            windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = surfaceTexture },
                padding = new RectOffset(16, 16, 24, 16),
                border = new RectOffset(2, 2, 2, 2)
            };

            // Title style - Material Design headline
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22 * fontScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = MaterialOnSurface },
                padding = new RectOffset(0, 0, 8, 12),
                margin = new RectOffset(0, 0, 0, 8)
            };

            // Section header style
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16 * fontScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = MaterialPrimary },
                padding = new RectOffset(6, 0, 4, 2),
                margin = new RectOffset(0, 0, 4, 2)
            };

            // Regular label style
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * fontScale),
                normal = { textColor = MaterialOnSurfaceVariant },
                padding = new RectOffset(8, 0, 2, 2),
                margin = new RectOffset(0, 0, 1, 1)
            };

            // Value/data style
            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * fontScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = MaterialOnSurface },
                padding = new RectOffset(0, 8, 2, 2),
                margin = new RectOffset(0, 0, 1, 1)
            };

            // Primary button style
            primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(14 * fontScale),
                fontStyle = FontStyle.Bold,
                normal = {
                    background = primaryTexture,
                    textColor = Color.white
                },
                hover = {
                    background = primaryDarkTexture,
                    textColor = Color.white
                },
                active = {
                    background = primaryDarkTexture,
                    textColor = Color.white
                },
                padding = new RectOffset(16, 16, 8, 8),
                margin = new RectOffset(4, 4, 4, 4)
            };

            // Secondary button style
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(14 * fontScale),
                normal = {
                    background = surfaceVariantTexture,
                    textColor = MaterialOnSurface
                },
                hover = {
                    background = surfaceVariantTexture,
                    textColor = MaterialPrimary
                },
                active = {
                    background = surfaceVariantTexture,
                    textColor = MaterialPrimary
                },
                padding = new RectOffset(16, 16, 8, 8),
                margin = new RectOffset(4, 4, 4, 4)
            };

            // Card-like container style
            cardStyle = new GUIStyle()
            {
                normal = { background = surfaceVariantTexture },
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            // Separator style
            separatorStyle = new GUIStyle()
            {
                normal = { background = separatorTexture },
                fixedHeight = 1,
                margin = new RectOffset(8, 8, 8, 8)
            };

            stylesInitialized = true;
        }

        private void CreateMaterialTextures()
        {
            // Surface texture
            surfaceTexture = new Texture2D(1, 1);
            surfaceTexture.SetPixel(0, 0, MaterialSurface);
            surfaceTexture.Apply();

            // Surface variant texture
            surfaceVariantTexture = new Texture2D(1, 1);
            surfaceVariantTexture.SetPixel(0, 0, MaterialSurfaceVariant);
            surfaceVariantTexture.Apply();

            // Primary texture
            primaryTexture = new Texture2D(1, 1);
            primaryTexture.SetPixel(0, 0, MaterialPrimary);
            primaryTexture.Apply();

            // Primary dark texture
            primaryDarkTexture = new Texture2D(1, 1);
            primaryDarkTexture.SetPixel(0, 0, MaterialPrimaryDark);
            primaryDarkTexture.Apply();

            // Separator texture
            separatorTexture = new Texture2D(1, 1);
            separatorTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            separatorTexture.Apply();
        }

        private void OnDestroy()
        {
            if (surfaceTexture != null) Destroy(surfaceTexture);
            if (surfaceVariantTexture != null) Destroy(surfaceVariantTexture);
            if (primaryTexture != null) Destroy(primaryTexture);
            if (primaryDarkTexture != null) Destroy(primaryDarkTexture);
            if (separatorTexture != null) Destroy(separatorTexture);
        }

        private void OnGUI()
        {
            if (!isDisplayVisible) return;

            InitializeStyles();

            // Apply the Material Design window style
            GUI.Window(0, windowRect, DrawStatsWindow, "Game Statistics", windowStyle);
        }

        private void DrawStatsWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Survival Mode Stats Card
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Survival Mode", headerStyle);

            // Single row showing either "Inactive" or the current timer
            GUILayout.BeginHorizontal();
            if (isSurvivalActive)
            {
                GUILayout.Label("Time:", labelStyle, GUILayout.Width(GetScaledWidth(50)));
                GUIStyle timerStyle = new GUIStyle(valueStyle)
                {
                    normal = { textColor = MaterialPositive }
                };
                GUILayout.Label(FormatTimeSpan(DateTime.Now - survivalStartTime), timerStyle, GUILayout.MinWidth(GetScaledWidth(80)));
            }
            else
            {
                GUILayout.Label("Status:", labelStyle, GUILayout.Width(GetScaledWidth(60)));
                GUIStyle statusStyle = new GUIStyle(valueStyle)
                {
                    normal = { textColor = MaterialOnSurfaceVariant }
                };
                GUILayout.Label("Inactive", statusStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(4);

            // Enemy Statistics Card
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("Enemy Statistics", headerStyle);

            try
            {
                if (EnemiesTracker.Instance != null)
                {
                    int enemiesKilled = EnemiesTracker.Instance.GetEnemiesKilled();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Enemies Killed:", labelStyle, GUILayout.Width(GetScaledWidth(120)));
                    GUIStyle killsStyle = new GUIStyle(valueStyle)
                    {
                        normal = { textColor = enemiesKilled > 0 ? MaterialPositive : MaterialOnSurface }
                    };
                    GUILayout.Label(enemiesKilled.ToString(), killsStyle);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("Enemy Tracker not initialized", labelStyle);
                }
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

            // Player Statistics Card with Scroll View
            GUILayout.BeginVertical(cardStyle);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(GetScaledHeight(140)));
            try
            {
                if (PlayerTracker.Instance != null)
                {
                    var playerStats = PlayerTracker.Instance.GetPlayerStatsList();
                    if (playerStats != null && playerStats.Count > 0)
                    {
                        // Header row
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Player", headerStyle, GUILayout.Width(GetScaledWidth(130)));
                        GUILayout.Label("Deaths", headerStyle, GUILayout.Width(GetScaledWidth(100)));
                        GUILayout.Label("Kills", headerStyle, GUILayout.Width(GetScaledWidth(60)));
                        GUILayout.EndHorizontal();

                        // Add a subtle separator
                        GUILayout.Box("", separatorStyle);

                        foreach (var stat in playerStats)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("", valueStyle, GUILayout.Width(GetScaledWidth(5)));
                            GUILayout.Label(stat.playerName, valueStyle, GUILayout.Width(GetScaledWidth(145)));

                            GUIStyle deathsStyle = new GUIStyle(valueStyle)
                            {
                                normal = { textColor = stat.deaths > 0 ? MaterialAccent : MaterialOnSurface }
                            };
                            GUILayout.Label(stat.deaths.ToString(), deathsStyle, GUILayout.Width(GetScaledWidth(100)));

                            GUIStyle killsStyle = new GUIStyle(valueStyle)
                            {
                                normal = { textColor = stat.kills > 0 ? MaterialPositive : MaterialOnSurface }
                            };
                            GUILayout.Label(stat.kills.ToString(), killsStyle, GUILayout.Width(GetScaledWidth(60)));
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
                GUIStyle errorStyle = new GUIStyle(labelStyle)
                {
                    normal = { textColor = MaterialAccent }
                };
                GUILayout.Label($"Error: {ex.Message}", errorStyle);
                Logger.LogError($"Error displaying player stats: {ex.Message}");
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private float GetScaledWidth(float baseWidth)
        {
            float scale = isEnlarged ? sizeScaleFactor : 1f;
            return baseWidth * scale;
        }

        private float GetScaledHeight(float baseHeight)
        {
            float scale = isEnlarged ? sizeScaleFactor : 1f;
            return baseHeight * scale;
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

            TimeSpan sessionTime = DateTime.Now - survivalStartTime;
            isSurvivalActive = false;

            Logger.LogInfo($"Survival mode timer stopped. Session time: {FormatTimeSpan(sessionTime)}");
        }
    }
}
