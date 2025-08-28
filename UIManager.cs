using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using UnityEngine.InputSystem;

namespace StatsMod
{
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        private static UIManager _instance;
        public static UIManager Instance { get; private set; }
        #endregion

        #region UI Components
        private SmallUI smallUI;
        private BigUI bigUI;
        #endregion

        #region Shared Constants
        public const int PADDING = 8;
        public const int HEADER_SIZE = 16;
        public const int LABEL_SIZE = 14;
        public const int BIG_HEADER_SIZE = 24;
        public const int BIG_LABEL_SIZE = 18;
        #endregion

        #region Shared Colors
        public static readonly Color Blue = new Color(0.259f, 0.522f, 0.957f, 1f);
        public static readonly Color Red = new Color(1f, 0.341f, 0.133f, 1f);
        public static readonly Color Green = new Color(0.298f, 0.686f, 0.314f, 1f);
        public static readonly Color White = new Color(0.9f, 0.9f, 0.9f, 1f);
        public static readonly Color Gray = new Color(0.7f, 0.7f, 0.7f, 1f);
        public static readonly Color DarkGray = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        public static readonly Color MediumGray = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        public static readonly Color LightGray = new Color(0.35f, 0.35f, 0.35f, 0.95f);
        #endregion

        #region Shared Textures
        private Texture2D darkTexture;
        private Texture2D mediumTexture;
        private Texture2D lightTexture;
        #endregion

        #region UI Scaling
        private static float _uiScaleFactor = 1.0f;
        private static bool _scalingInitialized = false;

        public static float UIScaleFactor
        {
            get
            {
                if (!_scalingInitialized) InitializeScaling();
                return _uiScaleFactor;
            }
        }

        /// <summary>
        /// Computes and sets the global UI scale factor based on configuration and current screen resolution.
        /// </summary>
        /// <remarks>
        /// If ModConfig.AutoScale is true, the scale is derived from the smaller ratio of the current
        /// screen dimensions to a 1920x1080 base, multiplied by ModConfig.UIScale and clamped to [0.5, 3.0].
        /// If AutoScale is false, ModConfig.UIScale is used directly. Sets the private fields
        /// <c>_uiScaleFactor</c> and <c>_scalingInitialized</c>.
        /// </remarks>
        private static void InitializeScaling()
        {
            if (ModConfig.AutoScale)
            {
                // Base resolution: 1920x1080
                float baseWidth = 1920f;
                float baseHeight = 1080f;

                // Get current screen resolution
                float currentWidth = Screen.width;
                float currentHeight = Screen.height;

                // Calculate scale based on the smaller dimension to maintain readability
                float widthScale = currentWidth / baseWidth;
                float heightScale = currentHeight / baseHeight;
                float autoScale = Mathf.Min(widthScale, heightScale);

                // Apply manual scale multiplier
                _uiScaleFactor = autoScale * ModConfig.UIScale;

                // Clamp to reasonable values
                _uiScaleFactor = Mathf.Clamp(_uiScaleFactor, 0.5f, 3.0f);

                Logger.LogInfo($"UI Auto-scaling initialized: Screen {currentWidth}x{currentHeight}, Scale factor: {_uiScaleFactor:F2}");
            }
            else
            {
                _uiScaleFactor = ModConfig.UIScale;
                Logger.LogInfo($"UI Manual scaling: Scale factor: {_uiScaleFactor:F2}");
            }

            _scalingInitialized = true;
        }

        /// <summary>
        /// Scales a numeric value by the current UI scale factor.
        /// </summary>
        /// <param name="value">The value to scale (e.g., size, padding, length) in unscaled units.</param>
        /// <returns>The input multiplied by <see cref="UIScaleFactor"/>.</returns>
        public static float ScaleValue(float value)
        {
            return value * UIScaleFactor;
        }

        /// <summary>
        /// Scales an integer by the current UI scale factor and returns the nearest integer.
        /// </summary>
        /// <param name="value">The integer value to scale (typically pixel or size units).</param>
        /// <returns>The value multiplied by <c>UIScaleFactor</c>, rounded to the nearest integer.</returns>
        public static int ScaleInt(int value)
        {
            return Mathf.RoundToInt(value * UIScaleFactor);
        }

        /// <summary>
        /// Scales a base font size by the current UI scale factor and returns the nearest integer size.
        /// </summary>
        /// <param name="baseFontSize">Unscaled font size (in points) to be adjusted by the UI scale factor.</param>
        /// <returns>The scaled font size rounded to the nearest integer.</returns>
        public static int ScaleFont(int baseFontSize)
        {
            return Mathf.RoundToInt(baseFontSize * UIScaleFactor);
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Ensures a single persistent UIManager instance exists and initializes its UI components.
        /// </summary>
        /// <remarks>
        /// If no instance exists, this creates a GameObject named "UIManager", attaches a UIManager component,
        /// marks it to persist across scene loads, assigns the singleton Instance, and initializes internal UI components.
        /// Safe to call multiple times; subsequent calls are no-ops when an instance already exists.
        /// </remarks>
        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject uiManagerObj = new GameObject("UIManager");
                _instance = uiManagerObj.AddComponent<UIManager>();
                DontDestroyOnLoad(uiManagerObj);
                Instance = _instance;

                Instance.InitializeComponents();
                Logger.LogInfo("UIManager initialized");
            }
        }

        /// <summary>
        /// Creates shared UI textures, constructs child GameObjects for the SmallUI and BigUI components,
        /// attaches the corresponding components, and calls their Initialize methods.
        /// </summary>
        /// <remarks>
        /// This sets the created objects as children of the UIManager's transform and stores references
        /// in the private fields `smallUI` and `bigUI`. Intended for internal initialization only.
        /// </remarks>
        private void InitializeComponents()
        {
            CreateSharedTextures();

            GameObject smallUIObj = new GameObject("SmallUI");
            smallUIObj.transform.SetParent(transform);
            smallUI = smallUIObj.AddComponent<SmallUI>();
            smallUI.Initialize();

            GameObject bigUIObj = new GameObject("BigUI");
            bigUIObj.transform.SetParent(transform);
            bigUI = bigUIObj.AddComponent<BigUI>();
            bigUI.Initialize();
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// Called every frame by Unity. Polls the current keyboard (new Input System) and invokes
        /// F1/F2 handlers when those keys are pressed this frame.
        /// </summary>
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                HandleF1Press();
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                HandleF2Press();
            }
        }

        /// <summary>
        /// Handles an F1 key press: ensures the Big UI is hidden and toggles the Small UI's visibility.
        /// </summary>
        private void HandleF1Press()
        {
            bool isSmallVisible = smallUI != null && smallUI.IsVisible();
            HideBigUI();
            if (isSmallVisible)
            {
                HideSmallUI();
            }
            else
            {
                ShowSmallUI();
            }
        }
        /// <summary>
        /// Handles the F2 key press: hides the SmallUI and toggles the visibility of the BigUI.
        /// </summary>
        /// <remarks>
        /// If BigUI is currently visible it will be hidden; otherwise it will be shown. SmallUI is always hidden as part of this action.
        /// </remarks>
        private void HandleF2Press()
        {
            bool isBigVisible = bigUI != null && bigUI.IsVisible();
            HideSmallUI();
            if (isBigVisible)
            {
                HideBigUI();
            }
            else
            {
                ShowBigUI();
            }
        }

        /// <summary>
        /// Toggles the visibility of the SmallUI component.
        /// </summary>
        /// <remarks>
        /// If the SmallUI instance has not been created or is null, this method does nothing.
        /// </remarks>
        public void ToggleSmallUI()
        {
            if (smallUI != null)
            {
                smallUI.ToggleDisplay();
            }
        }

        /// <summary>
        /// Toggles the visibility of the BigUI panel managed by this UIManager; no-op if the BigUI component is not initialized.
        /// </summary>
        public void ToggleBigUI()
        {
            if (bigUI != null)
            {
                bigUI.ToggleDisplay();
            }
        }

        /// <summary>
        /// Shows the Small UI (compact HUD) if it has been created; does nothing when the SmallUI instance is not available.
        /// </summary>
        public void ShowSmallUI()
        {
            if (smallUI != null)
            {
                smallUI.ShowDisplay();
            }
        }

        /// <summary>
        /// Hides the SmallUI panel if it exists; does nothing if the SmallUI has not been initialized.
        /// </summary>
        public void HideSmallUI()
        {
            if (smallUI != null)
            {
                smallUI.HideDisplay();
            }
        }

        /// <summary>
        /// Makes the Big UI visible if it has been created.
        /// </summary>
        /// <remarks>
        /// If the BigUI component is not present, this method does nothing.
        /// </remarks>
        public void ShowBigUI()
        {
            if (bigUI != null)
            {
                bigUI.ShowDisplay();
            }
        }

        /// <summary>
        /// Hides the persistent "big" HUD panel managed by this UIManager.
        /// </summary>
        /// <remarks>
        /// If the BigUI component has not been created or initialized, this method does nothing (null-safe).
        /// </remarks>
        public void HideBigUI()
        {
            if (bigUI != null)
            {
                bigUI.HideDisplay();
            }
        }
        #endregion

        #region Shared Texture Management
        /// <summary>
        /// Creates and assigns the shared 1x1 background textures used by the UI (dark, medium, and light).
        /// </summary>
        private void CreateSharedTextures()
        {
            darkTexture = CreateColorTexture(DarkGray);
            mediumTexture = CreateColorTexture(MediumGray);
            lightTexture = CreateColorTexture(LightGray);
        }

        /// <summary>
        /// Creates a 1x1 Texture2D filled with the specified color.
        /// </summary>
        /// <param name="color">Color to fill the single pixel of the texture.</param>
        /// <returns>A new 1x1 Texture2D whose only pixel is set to <paramref name="color"/>.</returns>
        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>
/// Gets the shared 1x1 dark-colored Texture2D used for UI backgrounds and fills.
/// </summary>
/// <returns>The dark texture (may be null if textures have not been created or have been destroyed).</returns>
public Texture2D GetDarkTexture() => darkTexture;
        /// <summary>
/// Returns the shared medium background texture used by UI styles.
/// </summary>
/// <returns>The 1x1 <see cref="Texture2D"/> filled with the manager's medium color, or <c>null</c> if textures have not been created.</returns>
public Texture2D GetMediumTexture() => mediumTexture;
        /// <summary>
/// Gets the shared 1x1 light-colored Texture2D used as a background for UI elements.
/// </summary>
/// <returns>The shared light texture (created and owned by UIManager); do not destroy this texture. />
public Texture2D GetLightTexture() => lightTexture;
        #endregion

        #region Shared Style Creation
        /// <summary>
        /// Creates a window <see cref="GUIStyle"/> based on <see cref="GUI.skin.window"/>, applying the provided background texture and scaled padding.
        /// </summary>
        /// <param name="backgroundTexture">Texture to use as the window background.</param>
        /// <returns>A configured <see cref="GUIStyle"/> with the given background and padding adjusted for the current UI scale.</returns>
        public GUIStyle CreateWindowStyle(Texture2D backgroundTexture)
        {
            return new GUIStyle(GUI.skin.window)
            {
                normal = { background = backgroundTexture },
                padding = new RectOffset(ScaleInt(PADDING * 2), ScaleInt(PADDING * 2), ScaleInt(PADDING * 2), ScaleInt(PADDING * 2))
            };
        }

        /// <summary>
        /// Creates a GUIStyle configured for header text: bold, blue, and scaled for the current UI scale.
        /// </summary>
        /// <param name="fontSize">Base font size to scale (defaults to <see cref="HEADER_SIZE"/>).</param>
        /// <returns>A <see cref="GUIStyle"/> with scaled font size, bold style, blue text color, and scaled padding suitable for headers.</returns>
        public GUIStyle CreateHeaderStyle(int fontSize = HEADER_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaleFont(fontSize),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Blue },
                padding = new RectOffset(ScaleInt(PADDING), 0, ScaleInt(4), ScaleInt(2))
            };
        }

        /// <summary>
        /// Creates a label <see cref="GUIStyle"/> with scaled font size and standardized padding and color used across the UI.
        /// </summary>
        /// <param name="fontSize">Base (unscaled) font size to use; the value is scaled by the current UIScaleFactor before assignment.</param>
        /// <returns>A <see cref="GUIStyle"/> configured for labels (gray text, scaled font, and scaled padding).</returns>
        public GUIStyle CreateLabelStyle(int fontSize = LABEL_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaleFont(fontSize),
                normal = { textColor = Gray },
                padding = new RectOffset(ScaleInt(PADDING), 0, ScaleInt(2), ScaleInt(2))
            };
        }

        /// <summary>
        /// Creates a GUIStyle intended for displaying prominent numeric or value text (bold, white).
        /// </summary>
        /// <param name="fontSize">Base font size prior to applying the UI scale (defaults to LABEL_SIZE).</param>
        /// <returns>A GUIStyle with a bold font, white text color, scaled font size, and right-side padding appropriate for value display.</returns>
        public GUIStyle CreateValueStyle(int fontSize = LABEL_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaleFont(fontSize),
                fontStyle = FontStyle.Bold,
                normal = { textColor = White },
                padding = new RectOffset(0, ScaleInt(PADDING), ScaleInt(2), ScaleInt(2))
            };
        }

        /// <summary>
        /// Creates a GUIStyle suitable for a "card" container with the provided background texture and scaled padding/margins.
        /// </summary>
        /// <param name="backgroundTexture">1x1 or tiled texture used as the card background. May be null for no background.</param>
        /// <returns>A GUIStyle with the texture applied to <c>normal.background</c> and padding/margin values scaled by the current UIScaleFactor.</returns>
        public GUIStyle CreateCardStyle(Texture2D backgroundTexture)
        {
            return new GUIStyle()
            {
                normal = { background = backgroundTexture },
                padding = new RectOffset(ScaleInt(PADDING), ScaleInt(PADDING), ScaleInt(4), ScaleInt(4)),
                margin = new RectOffset(ScaleInt(2), ScaleInt(2), ScaleInt(2), ScaleInt(2))
            };
        }

        /// <summary>
        /// Creates a GUIStyle configured for error text (red color) with a scaled font size.
        /// </summary>
        /// <param name="fontSize">Base font size to scale by the current UI scale factor (defaults to LABEL_SIZE).</param>
        /// <returns>A GUIStyle based on <c>GUI.skin.label</c> with the font size scaled and the text color set to the manager's error color.</returns>
        public GUIStyle CreateErrorStyle(int fontSize = LABEL_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaleFont(fontSize),
                normal = { textColor = Red }
            };
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Notify UI components that a player has joined; forwards the event to both SmallUI and BigUI if they exist.
        /// </summary>
        public void OnPlayerJoined()
        {
            smallUI?.OnPlayerJoined();
            bigUI?.OnPlayerJoined();
        }

        /// <summary>
        /// Notifies managed UI components that a player has left, forwarding the event to both SmallUI and BigUI if present.
        /// </summary>
        public void OnPlayerLeft()
        {
            smallUI?.OnPlayerLeft();
            bigUI?.OnPlayerLeft();
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Unity callback invoked when this GameObject is destroyed; releases any generated Texture2D resources (dark, medium, light) to free GPU/managed memory.
        /// </summary>
        private void OnDestroy()
        {
            if (darkTexture != null) Destroy(darkTexture);
            if (mediumTexture != null) Destroy(mediumTexture);
            if (lightTexture != null) Destroy(lightTexture);
        }
        #endregion

        #region Public Interface for External Classes
        /// <summary>
        /// Shows the BigUI and hides the SmallUI on the current UIManager instance.
        /// </summary>
        /// <remarks>
        /// If the UIManager instance does not exist, the call is a no-op.
        /// </remarks>
        public static void AutoPullHUD()
        {
            Instance?.ShowBigUI();
            Instance?.HideSmallUI();
        }

        /// <summary>
        /// Hides the small HUD (SmallUI) if the UIManager instance exists.
        /// </summary>
        /// <remarks>
        /// Safe to call when no UIManager instance is available; it will be a no-op in that case.
        /// </remarks>
        public static void HideHUD()
        {
            Instance?.HideSmallUI();
        }
        #endregion
    }
}
