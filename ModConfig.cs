using Silk;
using Logger = Silk.Logger;
using System.Collections.Generic;
using System;

namespace StatsMod
{
    public static class ModConfig
    {
        private const string ModId = StatsMod.ModId;

        // Display settings
        public static bool ShowStatsWindow => Config.GetModConfigValue<bool>(ModId, "display.showStatsWindow", true);
        public static bool ShowPlayers => Config.GetModConfigValue<bool>(ModId, "display.showPlayers", true);
        public static bool ShowKillCount => Config.GetModConfigValue<bool>(ModId, "display.showKillCount", true);
        public static bool ShowDeathCount => Config.GetModConfigValue<bool>(ModId, "display.showDeathCount", true);
        public static bool ShowPlayTime => Config.GetModConfigValue<bool>(ModId, "display.showPlayTime", true);
        public static bool ShowEnemyDeaths => Config.GetModConfigValue<bool>(ModId, "display.showEnemyDeaths", true);

        public static int DisplayPositionX => ValidatePosition(Config.GetModConfigValue<int>(ModId, "display.position.x", 10));
        public static int DisplayPositionY => ValidatePosition(Config.GetModConfigValue<int>(ModId, "display.position.y", 10));

        // Tracking settings
        public static bool TrackingEnabled => Config.GetModConfigValue<bool>(ModId, "tracking.enabled", true);
        public static bool SaveStatsToFile => Config.GetModConfigValue<bool>(ModId, "tracking.saveStatsToFile", true);

        // Keybind settings
        // public static string ToggleStatsKey => Config.GetModConfigValue<string>(ModId, "keybinds.toggleStats", "F1");

        // Methods to update config values at runtime
        public static void SetShowStats(bool value)
        {
            Config.SetModConfigValue(ModId, "display.showStats", value);
        }

        public static void SetDisplayPosition(int x, int y)
        {
            // Validate the values before setting them
            x = ValidatePosition(x);
            y = ValidatePosition(y);

            Config.SetModConfigValue(ModId, "display.position.x", x);
            Config.SetModConfigValue(ModId, "display.position.y", y);
        }

        public static void SetTrackingEnabled(bool value)
        {
            Config.SetModConfigValue(ModId, "tracking.enabled", value);
        }

        // Validation methods to ensure config values are within acceptable ranges
        private static int ValidatePosition(int value)
        {
            if (value < 0)
            {
                Logger.LogWarning($"Position value {value} is negative, clamping to 0");
                return 0;
            }
            return value;
        }
    }
}
