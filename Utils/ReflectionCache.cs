using System.Reflection;
using HarmonyLib;
using Unity.Netcode;

namespace StatsMod
{
    /// <summary>
    /// Centralized reflection bindings for game internals. All FieldInfo / MethodInfo /
    /// PropertyInfo lookups are resolved exactly once at static init to avoid per-call
    /// reflection cost on the hot paths (damage handlers, FixedUpdate patches, etc.).
    ///
    /// Previously split between ReflectionCache (PlayerPatches.cs) and EnemyReflectionCache
    /// (EnemyDeathTypes.cs). Consolidated here so there's one place to maintain.
    /// </summary>
    internal static class ReflectionCache
    {
        #region NetworkBehaviour
        public static readonly PropertyInfo IsHostProperty =
            AccessTools.Property(typeof(NetworkBehaviour), "IsHost");
        #endregion

        #region SpiderCustomizer
        public static readonly FieldInfo PrimaryColorField =
            typeof(SpiderCustomizer).GetField("_primaryColor", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly FieldInfo SecondaryColorField =
            typeof(SpiderCustomizer).GetField("_secondaryColor", BindingFlags.NonPublic | BindingFlags.Instance);
        #endregion

        #region EnemyHealthSystem
        public static readonly FieldInfo EnemyImmuneTimeField =
            AccessTools.Field(typeof(EnemyHealthSystem), "_immuneTime");
        #endregion

        #region SpiderHealthSystem
        public static readonly FieldInfo SpiderImmuneTimeField =
            AccessTools.Field(typeof(SpiderHealthSystem), "_immuneTime");

        public static readonly FieldInfo SpiderKnockBackRadius =
            AccessTools.Field(typeof(SpiderHealthSystem), "knockBackRadius");

        public static readonly FieldInfo SpiderLayers =
            AccessTools.Field(typeof(SpiderHealthSystem), "layers");

        public static readonly FieldInfo SpiderDeathRadius =
            AccessTools.Field(typeof(SpiderHealthSystem), "_deathRadius");
        #endregion

        #region DeathCube
        public static readonly FieldInfo DeathCubeLaserColliderLayers =
            AccessTools.Field(typeof(DeathCube), "laserColliderLayers");

        public static readonly FieldInfo DeathCubeBeamWidth =
            AccessTools.Field(typeof(DeathCube), "beamWidth");
        #endregion

        #region DeathRay
        public static readonly FieldInfo DeathRayDamageLayers =
            AccessTools.Field(typeof(DeathRay), "damageLayers");

        public static readonly FieldInfo DeathRayBeamWidth =
            AccessTools.Field(typeof(DeathRay), "beamWidth");

        public static readonly FieldInfo DeathRayBeamLength =
            AccessTools.Field(typeof(DeathRay), "beamLength");

        public static readonly FieldInfo DeathRayPoint =
            AccessTools.Field(typeof(DeathRay), "point");
        #endregion

        #region EnergyBall
        public static readonly FieldInfo EnergyBallDamageEnemies =
            AccessTools.Field(typeof(EnergyBall), "damageEnemies");

        public static readonly MethodInfo EnergyBallIsEnemy =
            AccessTools.Method(typeof(EnergyBall), "IsEnemy");

        public static readonly MethodInfo EnergyBallIsDamageable =
            AccessTools.Method(typeof(EnergyBall), "IsDamageable");
        #endregion

        #region DiscLauncher
        public static readonly MethodInfo DiscLauncherLaunchDiscClientRpc =
            AccessTools.Method(typeof(DiscLauncher), "LaunchDiscClientRpc");

        public static readonly MethodInfo DiscLauncherImpact =
            AccessTools.Method(typeof(DiscLauncher), "Impact");
        #endregion

        #region Explosion
        public static readonly FieldInfo ExplosionKnockBackRadius =
            AccessTools.Field(typeof(Explosion), "knockBackRadius");

        public static readonly FieldInfo ExplosionLayers =
            AccessTools.Field(typeof(Explosion), "layers");

        public static readonly FieldInfo ExplosionDeathRadius =
            AccessTools.Field(typeof(Explosion), "deathRadius");

        public static readonly FieldInfo ExplosionPlayerDeathRadius =
            AccessTools.Field(typeof(Explosion), "_playerDeathRadius");

        public static readonly FieldInfo ExplosionIsBoomSpear =
            AccessTools.Field(typeof(Explosion), "isBoomSpear");

        public static readonly FieldInfo ExplosionPlayerExplosionID =
            AccessTools.Field(typeof(Explosion), "playerExplosionID");

        public static readonly FieldInfo ExplosionOwnerId =
            AccessTools.Field(typeof(Explosion), "explosionOwnerId");
        #endregion

        #region ProjectileLauncher
        public static readonly MethodInfo ProjectileLauncherShotClientRpc =
            AccessTools.Method(typeof(ProjectileLauncher), "ShotClientRpc");

        public static readonly MethodInfo ProjectileLauncherImpact =
            AccessTools.Method(typeof(ProjectileLauncher), "Impact");

        public static readonly FieldInfo ProjectileLauncherCollider =
            AccessTools.Field(typeof(ProjectileLauncher), "_launcherCollider");

        public static readonly FieldInfo ProjectileLauncherReloadOffset =
            AccessTools.Field(typeof(ProjectileLauncher), "reloadOffset");
        #endregion
    }
}
