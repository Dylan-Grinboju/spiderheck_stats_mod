using Silk;
using System.Collections.Generic;

namespace StatsMod
{
    public static class ModConfig
    {
        private const string ModId = StatsMod.ModId;

        // Display settings
        public static bool ShowStats => Config.GetModConfigValue<bool>(ModId, "display.showStats", true);
        public static bool ShowKillCount => Config.GetModConfigValue<bool>(ModId, "display.showKillCount", true);
        public static bool ShowDeathCount => Config.GetModConfigValue<bool>(ModId, "display.showDeathCount", true);
        public static bool ShowTimeAlive => Config.GetModConfigValue<bool>(ModId, "display.showTimeAlive", true);

        public static int DisplayPositionX => Config.GetModConfigValue<int>(ModId, "display.position.x", 10);
        public static int DisplayPositionY => Config.GetModConfigValue<int>(ModId, "display.position.y", 10);

        // Tracking settings
        public static bool TrackingEnabled => Config.GetModConfigValue<bool>(ModId, "tracking.enabled", true);
        public static bool TrackEnemyTypes => Config.GetModConfigValue<bool>(ModId, "tracking.trackEnemyTypes", true);
        public static bool SaveStatsToFile => Config.GetModConfigValue<bool>(ModId, "tracking.saveStatsToFile", true);
        public static bool ResetStatsOnNewGame => Config.GetModConfigValue<bool>(ModId, "tracking.resetStatsOnNewGame", false);

        // Keybind settings
        public static string ToggleStatsKey => Config.GetModConfigValue<string>(ModId, "keybinds.toggleStats", "F1");

        // Methods to update config values at runtime
        public static void SetShowStats(bool value)
        {
            Config.SetModConfigValue(ModId, "display.showStats", value);
        }

        public static void SetDisplayPosition(int x, int y)
        {
            Config.SetModConfigValue(ModId, "display.position.x", x);
            Config.SetModConfigValue(ModId, "display.position.y", y);
        }

        public static void SetTrackingEnabled(bool value)
        {
            Config.SetModConfigValue(ModId, "tracking.enabled", value);
        }
    }
}
