using Silk;
using Logger = Silk.Logger;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StatsMod
{
    public static class ModConfig
    {
        private const string ModId = StatsMod.ModId;

        // Display settings
        public static bool ShowStatsWindow => Config.GetModConfigValue<bool>(ModId, "display.showStatsWindow", true);
        public static bool ShowPlayers => Config.GetModConfigValue<bool>(ModId, "display.showPlayers", true);
        // public static bool ShowKillCount => Config.GetModConfigValue<bool>(ModId, "display.showKillCount", true);
        // public static bool ShowDeathCount => Config.GetModConfigValue<bool>(ModId, "display.showDeathCount", true);
        public static bool ShowPlayTime => Config.GetModConfigValue<bool>(ModId, "display.showPlayTime", true);
        public static bool ShowEnemyDeaths => Config.GetModConfigValue<bool>(ModId, "display.showEnemyDeaths", true);

        public static int DisplayPositionX => ValidatePositionX(Config.GetModConfigValue<int>(ModId, "display.position.x", 10));
        public static int DisplayPositionY => ValidatePositionY(Config.GetModConfigValue<int>(ModId, "display.position.y", 10));

        // UI Scaling settings
        public static bool AutoScale => Config.GetModConfigValue<bool>(ModId, "display.autoScale", true);
        public static float UIScale => ValidateScale(Config.GetModConfigValue<float>(ModId, "display.uiScale", 1.0f));

        // Tracking settings
        public static bool TrackingEnabled => Config.GetModConfigValue<bool>(ModId, "tracking.enabled", true);
        public static bool SaveStatsToFile => Config.GetModConfigValue<bool>(ModId, "tracking.saveStatsToFile", true);

        // Keybind settings
        // public static string ToggleStatsKey => Config.GetModConfigValue<string>(ModId, "keybinds.toggleStats", "F1");

        /// <summary>
        /// Sets whether the stats display is shown and persists the choice to the mod configuration.
        /// </summary>
        /// <param name="value">True to show the stats window; false to hide it. The setting is saved to the mod config key <c>display.showStats</c>.</param>
        public static void SetShowStats(bool value)
        {
            Config.SetModConfigValue(ModId, "display.showStats", value);
        }

        /// <summary>
        /// Update and persist the on-screen position of the stats display.
        /// </summary>
        /// <param name="x">Target X coordinate in pixels; will be clamped to [0, Screen.width] if out of range.</param>
        /// <param name="y">Target Y coordinate in pixels; will be clamped to [0, Screen.height] if out of range.</param>
        /// <remarks>
        /// Validated coordinates are written to the mod configuration keys "display.position.x" and
        /// "display.position.y". Out-of-range inputs are clamped and a warning is logged.
        /// </remarks>
        public static void SetDisplayPosition(int x, int y)
        {
            // Validate the values before setting them
            x = ValidatePositionX(x);
            y = ValidatePositionY(y);

            Config.SetModConfigValue(ModId, "display.position.x", x);
            Config.SetModConfigValue(ModId, "display.position.y", y);
        }

        /// <summary>
        /// Enables or disables runtime tracking and persists the setting to the mod configuration.
        /// </summary>
        /// <param name="value">True to enable tracking; false to disable. Stored under config key "tracking.enabled".</param>
        public static void SetTrackingEnabled(bool value)
        {
            Config.SetModConfigValue(ModId, "tracking.enabled", value);
        }

        /// <summary>
        /// Clamp an X screen coordinate to the valid horizontal range [0, Screen.width].
        /// </summary>
        /// <param name="value">The requested X coordinate to validate.</param>
        /// <returns>The input clamped to the range 0..Screen.width.</returns>
        private static int ValidatePositionX(int value)
        {
            if (value < 0)
            {
                Logger.LogWarning($"Position value {value} is negative, clamping to 0");
                return 0;
            }
            if (Screen.width < value)
            {
                Logger.LogWarning($"Position value {value} exceeds screen width {Screen.width}, clamping to {Screen.width}");
                return Screen.width;
            }
            return value;
        }

        /// <summary>
        /// Clamp a Y-coordinate to the valid vertical screen bounds.
        /// </summary>
        /// <param name="value">Desired Y position in pixels; may be outside the current screen height.</param>
        /// <returns>The input value clamped to the range [0, Screen.height]. Logs a warning when clamping occurs.</returns>
        private static int ValidatePositionY(int value)
        {
            if (value < 0)
            {
                Logger.LogWarning($"Position value {value} is negative, clamping to 0");
                return 0;
            }
            if (Screen.height < value)
            {
                Logger.LogWarning($"Position value {value} exceeds screen height {Screen.height}, clamping to {Screen.height}");
                return Screen.height;
            }
            return value;
        }

        /// <summary>
        /// Validates and clamps a UI scale value to the supported range.
        /// </summary>
        /// <param name="value">Requested UI scale factor.</param>
        /// <returns>The clamped UI scale within the range [0.5, 3.0]. Logs a warning if the input was out of range.</returns>
        private static float ValidateScale(float value)
        {
            if (value < 0.5f)
            {
                Logger.LogWarning($"UI Scale value {value} is too small, clamping to 0.5");
                return 0.5f;
            }
            if (value > 3.0f)
            {
                Logger.LogWarning($"UI Scale value {value} is too large, clamping to 3.0");
                return 3.0f;
            }
            return value;
        }
    }
}
