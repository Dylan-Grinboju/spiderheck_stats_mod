using UnityEngine;
using Silk;
using Logger = Silk.Logger;

namespace StatsMod
{
    public class PauseHandler : MonoBehaviour
    {
        private bool wasPaused = false;

        void Update()
        {
            bool isPaused = (Time.timeScale == 0f);

            if (isPaused && !wasPaused)
            {
                GameSessionManager.Instance.PauseTimers();
                Logger.LogInfo("Game paused - timers paused");
            }
            else if (!isPaused && wasPaused)
            {
                GameSessionManager.Instance.ResumeTimers();
                Logger.LogInfo("Game resumed - timers resumed");
            }

            wasPaused = isPaused;
        }
    }
}
