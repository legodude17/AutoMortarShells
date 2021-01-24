using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AutoMortarShellChoice
{
    public static class SmartTargeting
    {
        public static AutoMortarShellChoiceMod Mod;

        private static readonly List<Pair<Thing, CompProjectileInterceptor>> SHIELDS =
            new List<Pair<Thing, CompProjectileInterceptor>>();

        public static void Patch(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(Thing), "SpawnSetup"),
                postfix: new HarmonyMethod(typeof(SmartTargeting), "SpawnSetupPostfix"));
            harm.Patch(AccessTools.Method(typeof(Thing), "Destroy"),
                postfix: new HarmonyMethod(typeof(SmartTargeting), "DestroyPostfix"));
        }

        public static void PatchTurret(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryFindNewTarget"),
                new HarmonyMethod(typeof(SmartTargeting), "TryFindNewTargetPrefix"),
                new HarmonyMethod(typeof(SmartTargeting), "TryFindNewTargetPostfix"));
        }

        public static void Unpatch(Harmony harm)
        {
            harm.Unpatch(AccessTools.Method(typeof(Building_TurretGun), "TryFindNewTarget"),
                HarmonyPatchType.Prefix, harm.Id);
            harm.Unpatch(AccessTools.Method(typeof(Building_TurretGun), "TryFindNewTarget"),
                HarmonyPatchType.Postfix, harm.Id);
        }

        public static bool TryFindNewTargetPrefix(Building_TurretGun __instance, ref LocalTargetInfo __result)
        {
            var comp = __instance.TryGetComp<CompAutoChangeProj>();
            if (comp?.Projectile == null) return true;
            if (comp.Projectile.projectile.damageDef == DamageDefOf.Extinguish && Mod.Settings.AutoFirefight
            )
            {
                var fires = __instance.Map.listerThings.ThingsOfDef(ThingDefOf.Fire)
                    .Where(t => __instance.AttackVerb.CanHitTarget(t)).Select(fire => new Pair<Thing, int>(fire,
                        GenRadial
                            .RadialDistinctThingsAround(fire.Position, fire.Map,
                                comp.Projectile.projectile.explosionRadius,
                                true).Count(t => t.def == ThingDefOf.Fire))).ToList();
                fires.Sort((a, b) => a.Second - b.Second);
                __result = fires.FirstOrDefault().First;
                return false;
            }

            if (comp.Projectile.projectile.damageDef == DamageDefOf.EMP && Mod.Settings.SmartShieldTactics != 0)
            {
                var shield = __instance.Map.listerBuildings.allBuildingsNonColonist.FirstOrDefault(b =>
                    b.TryGetComp<CompProjectileInterceptor>() is CompProjectileInterceptor cpi && cpi.Active &&
                    cpi.Props.interceptAirProjectiles);
                if (shield != null)
                {
                    __result = shield;
                    return false;
                }
            }

            return true;
        }

        public static void TryFindNewTargetPostfix(Building_TurretGun __instance, ref LocalTargetInfo __result)
        {
            var comp = __instance.TryGetComp<CompAutoChangeProj>();
            if (comp?.Projectile == null) return;
            var predicates = new List<Predicate<Thing>>();
            if (comp.Projectile.projectile.damageDef != DamageDefOf.EMP && Mod.Settings.SmartShieldTactics > 1 &&
                __result.IsValid)
            {
                if (Mod.Settings.SmartShieldTactics > 2)
                    predicates.Add(t =>
                        !WillBeIntercepted(__instance.AttackVerb, __instance.Position, t, __instance.Map));
                else
                    predicates.Add(t => !IsInsideShield(t, __instance.Map));
            }

            if (comp.Projectile.projectile.damageDef == DamageDefOf.Flame && __result.IsValid && __result.HasThing)
                predicates.Add(t => t is Pawn p && p.Fireable());

            if (comp.Projectile.projectile.damageDef == DamageDefOf.EMP)
                predicates.Add(t => t.EMPable());

            var validator = predicates.Collapse();
            if (__result.IsValid && __result.HasThing && !validator(__result.Thing))
                __result = __instance.TryFindNewTarget_Copy(validator);
        }

        private static Predicate<T> Collapse<T>(this List<Predicate<T>> list) where T : class
        {
            return obj => list.All(p => p(obj));
        }

        private static bool IsInsideShield(LocalTargetInfo target, Map map)
        {
            if (SHIELDS.Count == 0) return false;
            return SHIELDS.Any(p => p.Second.Active &&
                                    p.First.Map == map &&
                                    p.First.Position.InHorDistOf(target.Cell, p.Second.Props.radius));
        }

        private static bool Fireable(this Pawn p)
        {
            return p.CanEverAttachFire() && p.GetStatValue(StatDefOf.ArmorRating_Heat) < 2.0f && !p.IsBurning() &&
                   p.RaceProps.IsFlesh;
        }

        private static bool EMPable(this Thing t)
        {
            return t is Pawn p && p.RaceProps.IsMechanoid || t is Building_Turret;
        }

        private static bool WillBeIntercepted(Verb verb, IntVec3 from, LocalTargetInfo target, Map map)
        {
            verb.TryFindShootLineFromTo(from, target, out var line);
            return line.Points().All(p => !IsInsideShield(p, map));
        }

        private static LocalTargetInfo TryFindNewTarget_Copy(this Building_TurretGun b, Predicate<Thing> validator)
        {
            var targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable |
                                  TargetScanFlags.NeedNotUnderThickRoof;
            if (b.AttackVerb.IsIncendiary()) targetScanFlags |= TargetScanFlags.NeedNonBurning;

            return (Thing) AttackTargetFinder.BestShootTargetFromCurrentPosition(b, targetScanFlags,
                validator);
        }

        public static void SpawnSetupPostfix(Thing __instance)
        {
            if (__instance.TryGetComp<CompProjectileInterceptor>() is CompProjectileInterceptor cpi &&
                cpi.Props.interceptAirProjectiles)
                SHIELDS.Add(new Pair<Thing, CompProjectileInterceptor>(__instance, cpi));
        }

        public static void DestroyPostfix(Thing __instance)
        {
            if (SHIELDS.Select(p => p.First).Contains(__instance)) SHIELDS.RemoveAll(p => p.First == __instance);
        }
    }
}