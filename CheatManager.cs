using UnityEngine;
using UnityEngine.InputSystem;
using Silk;
using Logger = Silk.Logger;
using System.Linq;

namespace StatsMod
{
    public class CheatManager : MonoBehaviour
    {
        private static CheatManager _instance;
        public static CheatManager Instance { get; private set; }

        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject cheatManagerObj = new GameObject("CheatManager");
                _instance = cheatManagerObj.AddComponent<CheatManager>();
                Instance = _instance;
                DontDestroyOnLoad(cheatManagerObj);
                Logger.LogInfo("CheatManager initialized");
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f5Key.wasPressedThisFrame)
            {
                GiveExtraLife();
            }
        }

        private void GiveExtraLife()
        {
            try
            {
                // Check if we're in survival mode first
                SurvivalMode survivalMode = FindObjectOfType<SurvivalMode>();
                if (survivalMode == null || !survivalMode.GameModeActive())
                {
                    Logger.LogInfo("F5 cheat: Not in active survival mode, ignoring");
                    return;
                }

                // Find all active players
                PlayerInput[] allPlayers = FindObjectsOfType<PlayerInput>();

                if (allPlayers == null || allPlayers.Length == 0)
                {
                    Logger.LogWarning("No players found to give extra life to");
                    return;
                }

                int reviveCount = 0;
                // Give life to all players
                foreach (PlayerInput player in allPlayers)
                {
                    if (player != null)
                    {
                        // Find the SpiderHealthSystem component for this player
                        SpiderHealthSystem spiderHealth = player.GetComponentInChildren<SpiderHealthSystem>();

                        if (spiderHealth != null)
                        {
                            // Call DisableDeathEffect to essentially "revive" the player
                            // This simulates what happens when a player gets revived in survival mode
                            spiderHealth.DisableDeathEffect();
                            reviveCount++;
                            Logger.LogInfo($"Gave extra life to player {player.playerIndex}");
                        }
                        else
                        {
                            Logger.LogWarning($"Could not find SpiderHealthSystem for player {player.playerIndex}");
                        }
                    }
                }

                if (reviveCount > 0)
                {
                    Logger.LogInfo($"F5 cheat activated - gave extra life to {reviveCount} player(s)");
                }
                else
                {
                    Logger.LogWarning("F5 cheat: No players could be revived");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error giving extra life: {ex.Message}");
            }
        }
    }
}
