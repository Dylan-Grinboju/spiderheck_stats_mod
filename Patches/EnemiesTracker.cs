using UnityEngine;
using Silk;
using Logger = Silk.Logger;
using System.Collections.Generic;
using HarmonyLib;


namespace StatsMod
{

    public class EnemiesTracker
    {
        private static EnemiesTracker _instance;
        public static EnemiesTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EnemiesTracker();
                    Logger.LogInfo("Enemies tracker created via singleton access");
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public int EnemiesKilled { get; private set; }

        public EnemiesTracker()
        {
            EnemiesKilled = 0;

            Instance = this;
            Logger.LogInfo("Enemies tracker initialized");
        }

        public void IncrementEnemiesKilled()
        {
            EnemiesKilled++;
        }

        public void ResetEnemiesKilled()
        {
            EnemiesKilled = 0;
        }

        public int GetEnemiesKilled()
        {
            return EnemiesKilled;
        }

    }


    [HarmonyPatch(typeof(EnemyHealthSystem), "Explode")]
    class EnemyDeathCountPatch
    {
        static void Postfix(EnemyHealthSystem __instance)
        {
            try
            {
                EnemiesTracker.Instance.IncrementEnemiesKilled();
                Logger.LogInfo("Enemy killed via Explode method.");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error recording enemy kill: {ex.Message}");
            }
        }
    }
}
