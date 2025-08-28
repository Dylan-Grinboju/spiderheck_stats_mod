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

        #region Initialization
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

        public void ToggleSmallUI()
        {
            if (smallUI != null)
            {
                smallUI.ToggleDisplay();
            }
        }

        public void ToggleBigUI()
        {
            if (bigUI != null)
            {
                bigUI.ToggleDisplay();
            }
        }

        public void ShowSmallUI()
        {
            if (smallUI != null)
            {
                smallUI.ShowDisplay();
            }
        }

        public void HideSmallUI()
        {
            if (smallUI != null)
            {
                smallUI.HideDisplay();
            }
        }

        public void ShowBigUI()
        {
            if (bigUI != null)
            {
                bigUI.ShowDisplay();
            }
        }

        public void HideBigUI()
        {
            if (bigUI != null)
            {
                bigUI.HideDisplay();
            }
        }
        #endregion

        #region Shared Texture Management
        private void CreateSharedTextures()
        {
            darkTexture = CreateColorTexture(DarkGray);
            mediumTexture = CreateColorTexture(MediumGray);
            lightTexture = CreateColorTexture(LightGray);
        }

        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public Texture2D GetDarkTexture() => darkTexture;
        public Texture2D GetMediumTexture() => mediumTexture;
        public Texture2D GetLightTexture() => lightTexture;
        #endregion

        #region Shared Style Creation
        public GUIStyle CreateWindowStyle(Texture2D backgroundTexture)
        {
            return new GUIStyle(GUI.skin.window)
            {
                normal = { background = backgroundTexture },
                padding = new RectOffset(PADDING * 2, PADDING * 2, PADDING * 2, PADDING * 2)
            };
        }

        public GUIStyle CreateHeaderStyle(int fontSize = HEADER_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Blue },
                padding = new RectOffset(PADDING, 0, 4, 2)
            };
        }

        public GUIStyle CreateLabelStyle(int fontSize = LABEL_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Gray },
                padding = new RectOffset(PADDING, 0, 2, 2)
            };
        }

        public GUIStyle CreateValueStyle(int fontSize = LABEL_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = White },
                padding = new RectOffset(0, PADDING, 2, 2)
            };
        }

        public GUIStyle CreateCardStyle(Texture2D backgroundTexture)
        {
            return new GUIStyle()
            {
                normal = { background = backgroundTexture },
                padding = new RectOffset(PADDING, PADDING, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };
        }

        public GUIStyle CreateErrorStyle(int fontSize = LABEL_SIZE)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Red }
            };
        }
        #endregion

        #region Event Handling
        public void OnPlayerJoined()
        {
            smallUI?.OnPlayerJoined();
            bigUI?.OnPlayerJoined();
        }

        public void OnPlayerLeft()
        {
            smallUI?.OnPlayerLeft();
            bigUI?.OnPlayerLeft();
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            if (darkTexture != null) Destroy(darkTexture);
            if (mediumTexture != null) Destroy(mediumTexture);
            if (lightTexture != null) Destroy(lightTexture);
        }
        #endregion

        #region Public Interface for External Classes
        public static void AutoPullHUD()
        {
            Instance?.ShowBigUI();
            Instance?.HideSmallUI();
        }

        public static void HideHUD()
        {
            Instance?.HideSmallUI();
        }
        #endregion
    }
}
