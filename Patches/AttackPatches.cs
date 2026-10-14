using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;

namespace StatsMod
{
    internal static class AttackReflectionCache
    {
        public static readonly AccessTools.FieldRef<EnemyHealthSystem, float> EnemyImmuneTime =
            AccessTools.FieldRefAccess<EnemyHealthSystem, float>("_immuneTime");

        public static readonly AccessTools.FieldRef<SpiderHealthSystem, float> SpiderImmuneTill =
            AccessTools.FieldRefAccess<SpiderHealthSystem, float>("_immuneTill");
    }

    // Every player attack runs inside HitLogic.BeginAttack/EndAttack, and the targets record the hit when the game
    // applies it (see the receivers below), so no hit geometry is repeated here and hits on immune targets don't count.
    [HarmonyPatch]
    class AttackScopePatch
    {
        private struct Attack
        {
            public string Weapon;
            public Func<object, PlayerInput> Owner;
        }

        private static readonly Dictionary<(Type, string), Attack> attacks = new Dictionary<(Type, string), Attack>();

        static AttackScopePatch()
        {
            Add<DamageOnParticle>("OnParticleCollision", "Shotgun", particles => HitLogic.WeaponPlayer(particles.ignoreWeapon));
            Add<RailShot>("ProcessHit", "RailShot", RailShotOwner);
            Add<DeathCube>("Fire", "DeathCube", HitLogic.WeaponPlayer);
            Add<DeathRay>("Fire", "DeathRay", HitLogic.WeaponPlayer);
            Add<LaserCannon>("DamageHit", "Laser Cannon", HitLogic.WeaponPlayer);
            Add<LaserCube>("Beam", "Laser Cube", HitLogic.WeaponPlayer);
            // Particle Blade and the other force field weapons. On an enemy's stinger, blade or strut the hit is a knockback
            Add<ForceField>("Damage", "Particle Blade", field => HitLogic.WeaponPlayer(field.GetComponentInParent<Weapon>()));
            Add<SawDisc>("TryDamage", "SawDisc", SawDiscOwner);
            Add<EnergyBall>("OnCollisionEnter2D", "EnergyBall", ball => ball.owner != null ? ball.owner.GetComponentInParent<PlayerInput>() : null);
            Add<KhepriStaff>("Zap", "KhepriStaff", HitLogic.WeaponPlayer);
            Add<Explosion>("KnockBack", "Explosions", ExplosionOwner);
            Add<SpiderHealthSystem>("AfterlifeExplosion", "Explosions", spider => spider.GetComponentInParent<PlayerInput>());
            // The snow explosion only knocks back, and does it in OnEnable, inside this RPC's Instantiate
            Add<Snowballs>("ExplodeClientRpc", "Snowball", HitLogic.WeaponPlayer);
        }

        private static void Add<T>(string method, string weapon, Func<T, PlayerInput> owner)
        {
            attacks[(typeof(T), method)] = new Attack { Weapon = weapon, Owner = instance => owner((T)instance) };
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach ((Type type, string name) in attacks.Keys.ToList())
            {
                MethodInfo method = AccessTools.Method(type, name);
                if (method != null)
                {
                    yield return method;
                }
                else
                {
                    Logger.LogWarning($"{type.Name}.{name} not found, its hits are not tracked");
                }
            }
        }

        static void Prefix(object __instance, MethodBase __originalMethod, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                Attack attack = attacks[(__originalMethod.DeclaringType, __originalMethod.Name)];
                __state = HitLogic.BeginAttack(attack.Owner(__instance), attack.Weapon);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);

        // The shooter's spider is on the ignore list; a shot reflected by a blade ignores only that blade
        private static PlayerInput RailShotOwner(RailShot shot)
        {
            foreach (GameObject ignored in shot.ignore)
            {
                if (ignored == null) continue;

                SpiderHealthSystem spider = ignored.GetComponentInParent<SpiderHealthSystem>();
                if (spider != null) return spider.GetComponentInParent<PlayerInput>();

                Weapon weapon = ignored.GetComponentInParent<Weapon>();
                if (weapon != null) return HitLogic.WeaponPlayer(weapon);
            }
            return null;
        }

        private static PlayerInput SawDiscOwner(SawDisc disc)
        {
            PlayerInput owner = HitLogic.GetDiscOwner(disc.gameObject);
            return owner != null ? owner : disc.GetComponentInParent<PlayerInput>();
        }

        // SpiderSurge's ultimate Pulse runs an explosion without an owner inside the pulse, it keeps the pulse's player
        private static PlayerInput ExplosionOwner(Explosion explosion)
        {
            PlayerInput owner = HitLogic.GetExplosionOwner(explosion);
            return owner != null ? owner : HitLogic.CurrentAttacker;
        }

        // SpiderSurge's Pulse knocks back everything around its player, the ultimate also kills.
        // SpiderSurge can load after this mod, so the Pulse is patched once its assembly is loaded.
        public static void PatchSpiderSurgePulse(Harmony harmony)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (TryPatchPulse(harmony, assembly)) return;
            }

            AssemblyLoadEventHandler onLoad = null;
            onLoad = (sender, args) =>
            {
                if (TryPatchPulse(harmony, args.LoadedAssembly))
                {
                    AppDomain.CurrentDomain.AssemblyLoad -= onLoad;
                }
            };
            AppDomain.CurrentDomain.AssemblyLoad += onLoad;
        }

        private static bool TryPatchPulse(Harmony harmony, Assembly assembly)
        {
            try
            {
                Type pulse = assembly.GetType("SpiderSurge.PulseAbility", false);
                if (pulse == null) return false;

                MethodInfo method = AccessTools.Method(pulse, "ApplyPulsePhysics");
                FieldInfo playerInput = AccessTools.Field(pulse, "playerInput");
                if (method == null || playerInput == null)
                {
                    Logger.LogWarning("SpiderSurge's PulseAbility changed, Pulse hits are not tracked");
                    return true;
                }

                attacks[(pulse, method.Name)] = new Attack { Weapon = "Pulse", Owner = ability => playerInput.GetValue(ability) as PlayerInput };
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(AttackScopePatch), nameof(Prefix)),
                    finalizer: new HarmonyMethod(typeof(AttackScopePatch), nameof(Finalizer)));
                Logger.LogInfo("Patched SpiderSurge's Pulse");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error patching SpiderSurge's Pulse: {ex.Message}");
            }
            return true;
        }
    }

    // Receivers. Enemy stingers, blades, roller struts and Khepri armor pass their hits on to EnemyHealthSystem,
    // and equipped weapons pass theirs on to the holder's SpiderHealthSystem. Each counts only when the game applies it.
    [HarmonyPatch(typeof(EnemyHealthSystem), "Impact")]
    class EnemyImpactReceiverPatch
    {
        static void Prefix(EnemyHealthSystem __instance)
        {
            try
            {
                if (HitLogic.CurrentAttacker == null || Time.time < AttackReflectionCache.EnemyImmuneTime(__instance)) return;
                HitLogic.OnEnemyHitByAttack(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording enemy knockback: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(EnemyHealthSystem), "Damage")]
    class EnemyDamageReceiverPatch
    {
        static void Prefix(EnemyHealthSystem __instance)
        {
            try
            {
                if (HitLogic.CurrentAttacker == null || __instance.ignoreDirectDamage ||
                    Time.time < AttackReflectionCache.EnemyImmuneTime(__instance)) return;
                HitLogic.OnEnemyHitByAttack(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording enemy damage: {ex.Message}");
            }
        }
    }

    // A weapon's own recoil reaches the holder as an Impact without stun, which leaves the earlier hit in place
    [HarmonyPatch(typeof(SpiderHealthSystem), "Impact")]
    class SpiderImpactReceiverPatch
    {
        static void Prefix(SpiderHealthSystem __instance, bool stun)
        {
            try
            {
                HitLogic.OnPlayerHit(__instance, replacesEarlierHit: stun);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording spider knockback: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpiderHealthSystem), "Damage")]
    class SpiderDamageReceiverPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                if (Time.time < AttackReflectionCache.SpiderImmuneTill(__instance)) return;
                HitLogic.OnPlayerHit(__instance, replacesEarlierHit: true);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording spider damage: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SpiderHealthSystem), "FixedUpdate")]
    class SpiderFootingPatch
    {
        static void Postfix(SpiderHealthSystem __instance, Stabilizer ____stabilizer)
        {
            try
            {
                HitLogic.UpdatePlayerFooting(__instance, ____stabilizer);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking spider footing: {ex.Message}");
            }
        }
    }

    // Lava. Also runs for solid lava through OnCollisionStay2D
    [HarmonyPatch(typeof(DeathZone), "OnTriggerStay2D")]
    class DeathZoneContactPatch
    {
        static void Prefix(Collider2D other, float ____disintegrateDelayMod)
        {
            try
            {
                if (other.isTrigger || !other.CompareTag("PlayerRigidbody")) return;

                SpiderHealthSystem spider = other.GetComponent<SpiderHealthSystem>();
                if (spider != null)
                {
                    HitLogic.OnPlayerTouchedDeathZone(spider, ____disintegrateDelayMod);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking lava contact: {ex.Message}");
            }
        }
    }

    // The only places that give an explosion an owner. The store goes through HitLogic.SetExplosionOwner instead
    [HarmonyPatch]
    class ExplosionOwnerPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Grenade), "ExplodeClientRpc");
            yield return AccessTools.Method(typeof(Mine), "ExplodeClientRpc");
            yield return AccessTools.Method(typeof(BoomSpear), "ExplodeClientRpc");
            yield return AccessTools.Method(typeof(BasicProjectile), "DestroyProjectileClientRpc");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
            OwnerStoreRewriter.Rewrite(instructions, original,
                AccessTools.Field(typeof(Explosion), nameof(Explosion.explosionOwnerId)),
                AccessTools.Method(typeof(HitLogic), nameof(HitLogic.SetExplosionOwner)));
    }

    // The only places that give a rocket or other projectile an owner, so its explosion can inherit it
    [HarmonyPatch]
    class ProjectileOwnerPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ProjectileLauncher), "Shoot");
            yield return AccessTools.Method(typeof(BurstProjectileLauncher), "LaunchProjectile");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
            OwnerStoreRewriter.Rewrite(instructions, original,
                AccessTools.Field(typeof(BasicProjectile), nameof(BasicProjectile.projectileOwnerId)),
                AccessTools.Method(typeof(HitLogic), nameof(HitLogic.SetProjectileOwner)));
    }

    internal static class OwnerStoreRewriter
    {
        // Turns "target.field = value" into "setter(target, value, this)"; the setter does the same store
        public static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions, MethodBase original, FieldInfo field, MethodInfo setter)
        {
            int replaced = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.StoresField(field))
                {
                    CodeInstruction loadThis = new CodeInstruction(OpCodes.Ldarg_0);
                    loadThis.labels.AddRange(instruction.labels);
                    loadThis.blocks.AddRange(instruction.blocks);
                    yield return loadThis;
                    yield return new CodeInstruction(OpCodes.Call, setter);
                    replaced++;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (replaced == 0)
            {
                Logger.LogWarning($"{original.DeclaringType?.Name}.{original.Name} no longer sets {field.Name}, its owner is not tracked");
            }
        }
    }
}
