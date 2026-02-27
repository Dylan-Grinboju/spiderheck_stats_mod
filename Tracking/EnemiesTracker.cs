using Silk;
using Logger = Silk.Logger;

namespace StatsMod;

public class EnemiesTracker
{
    private static EnemiesTracker _instance;
    public static EnemiesTracker Instance
    {
        get
        {
            _instance ??= new EnemiesTracker();
            return _instance;
        }
    }

    public int EnemiesKilled { get; private set; }

    public EnemiesTracker()
    {
        EnemiesKilled = 0;
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
}
