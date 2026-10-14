using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Silk.Logger;

namespace StatsMod
{
    // Attacks that can knock back as well as damage run inside HitLogic.BeginAttack/EndAttack, and the targets
    // record the hit when the game applies it (see the receivers below), so no hit geometry is repeated here.
    internal static class AttackReflectionCache
    {
        public static readonly AccessTools.FieldRef<EnemyHealthSystem, float> EnemyImmuneTime =
            AccessTools.FieldRefAccess<EnemyHealthSystem, float>("_immuneTime");

        public static readonly AccessTools.FieldRef<SpiderHealthSystem, float> SpiderImmuneTill =
            AccessTools.FieldRefAccess<SpiderHealthSystem, float>("_immuneTill");
    }

    // Particle Blade and the other force field weapons. On an enemy's stinger, blade or strut the hit is a knockback
    [HarmonyPatch(typeof(ForceField), "Damage")]
    class ForceFieldAttackPatch
    {
        static void Prefix(ForceField __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                __state = HitLogic.BeginAttack(HitLogic.WeaponPlayer(__instance.GetComponentInParent<Weapon>()), "Particle Blade");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking ForceField hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
    }

    [HarmonyPatch(typeof(SawDisc), "TryDamage")]
    class SawDiscAttackPatch
    {
        static void Prefix(SawDisc __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                PlayerInput owner = HitLogic.GetDiscOwner(__instance.gameObject);
                if (owner == null)
                {
                    owner = __instance.GetComponentInParent<PlayerInput>();
                }
                __state = HitLogic.BeginAttack(owner, "SawDisc");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking SawDisc hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
    }

    [HarmonyPatch(typeof(EnergyBall), "OnCollisionEnter2D")]
    class EnergyBallAttackPatch
    {
        static void Prefix(EnergyBall __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                PlayerInput owner = __instance.owner != null ? __instance.owner.GetComponentInParent<PlayerInput>() : null;
                __state = HitLogic.BeginAttack(owner, "EnergyBall");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking EnergyBall hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
    }

    [HarmonyPatch(typeof(KhepriStaff), "Zap")]
    class KhepriStaffAttackPatch
    {
        static void Prefix(KhepriStaff __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                __state = HitLogic.BeginAttack(HitLogic.WeaponPlayer(__instance), "KhepriStaff");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking KhepriStaff hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
    }

    // Kills inside deathRadius, knocks back the rest of knockBackRadius
    [HarmonyPatch(typeof(Explosion), "KnockBack")]
    class ExplosionAttackPatch
    {
        static void Prefix(Explosion __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                // SpiderSurge's ultimate Pulse runs an explosion without an owner inside the pulse, it keeps the pulse's player
                PlayerInput owner = HitLogic.GetExplosionOwner(__instance);
                if (owner == null)
                {
                    owner = HitLogic.CurrentAttacker;
                }
                if (owner != null)
                {
                    __state = HitLogic.BeginAttack(owner, "Explosions");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking Explosion hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
    }

    [HarmonyPatch(typeof(SpiderHealthSystem), "AfterlifeExplosion")]
    class AfterlifeExplosionAttackPatch
    {
        static void Prefix(SpiderHealthSystem __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                __state = HitLogic.BeginAttack(__instance.GetComponentInParent<PlayerInput>(), "Explosions");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking afterlife explosion hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
    }

    // The snow explosion only knocks back, and does it in OnEnable, inside this RPC's Instantiate
    [HarmonyPatch(typeof(Snowballs), "ExplodeClientRpc")]
    class SnowballAttackPatch
    {
        static void Prefix(Snowballs __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                __state = HitLogic.BeginAttack(HitLogic.WeaponPlayer(__instance), "Snowball");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking Snowball hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
    }

    // SpiderSurge's Pulse ability knocks back everything around its player, the ultimate also kills
    [HarmonyPatch]
    class SpiderSurgePulseAttackPatch
    {
        private static readonly Type PulseType = AccessTools.TypeByName("SpiderSurge.PulseAbility");
        private static readonly FieldInfo PlayerInputField = PulseType != null ? AccessTools.Field(PulseType, "playerInput") : null;

        static bool Prepare() => TargetMethod() != null && PlayerInputField != null;

        static MethodBase TargetMethod() => PulseType != null ? AccessTools.Method(PulseType, "ApplyPulsePhysics") : null;

        static void Prefix(object __instance, out HitLogic.AttackScope __state)
        {
            __state = default;
            try
            {
                __state = HitLogic.BeginAttack(PlayerInputField.GetValue(__instance) as PlayerInput, "Pulse");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking Pulse hit: {ex.Message}");
            }
        }

        static void Finalizer(HitLogic.AttackScope __state) => HitLogic.EndAttack(__state);
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

    [HarmonyPatch(typeof(SpiderHealthSystem), "Impact")]
    class SpiderImpactReceiverPatch
    {
        static void Prefix(SpiderHealthSystem __instance)
        {
            try
            {
                if (HitLogic.CurrentAttacker == null) return;
                HitLogic.OnPlayerHitByAttack(__instance);
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
                if (HitLogic.CurrentAttacker == null || Time.time < AttackReflectionCache.SpiderImmuneTill(__instance)) return;
                HitLogic.OnPlayerHitByAttack(__instance);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording spider damage: {ex.Message}");
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
