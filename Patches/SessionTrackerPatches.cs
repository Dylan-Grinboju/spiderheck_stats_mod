using System;
using HarmonyLib;
using Silk;
using Logger = Silk.Logger;

namespace StatsMod;

[HarmonyPatch(typeof(LevelController), "LoadLevelWithTransition")]
public class LevelControllerLoadLevelPatch
{
    static void Postfix(LevelController __instance, GameLevel level)
    {
        try
        {
            if (GameSessionManager.Instance.IsActive && level is not null)
            {
                string mapName = string.IsNullOrEmpty(level.label) ? level.name : level.label;
                GameSessionManager.Instance.RecordMap(mapName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error tracking map load: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(SurvivalMode), "ChoosePerkAndLevel")]
public class SurvivalModeChoosePerkPatch
{
    static void Prefix(SurvivalMode __instance, Modifier modifier)
    {
        try
        {
            if (GameSessionManager.Instance.IsActive && modifier is not null && modifier.data is not null)
            {
                string perkName = string.IsNullOrEmpty(modifier.data.title) ? modifier.data.key : modifier.data.title;
                GameSessionManager.Instance.RecordPerk(perkName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error tracking perk selection: {ex.Message}");
        }
    }
}
