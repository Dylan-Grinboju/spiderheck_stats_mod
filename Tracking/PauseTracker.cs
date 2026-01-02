using UnityEngine;
using Silk;
using Logger = Silk.Logger;

namespace StatsMod
{
    public class PauseTracker : MonoBehaviour
    {
        private bool wasPaused = false;

        void Update()
        {
            bool isPaused = (Time.timeScale == 0f);

            if (isPaused && !wasPaused)
            {
                StatsManager.Instance.PauseTimers();
                Logger.LogInfo("Game paused - timers paused");
            }
            else if (!isPaused && wasPaused)
            {
                StatsManager.Instance.ResumeTimers();
                Logger.LogInfo("Game resumed - timers resumed");
            }

            wasPaused = isPaused;
        }
    }
}
