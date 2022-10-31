using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AutoMortarShellChoice
{
    public static class ShieldUtils
    {
        private static readonly List<Pair<Thing, CompProjectileInterceptor>> SHIELDS =
            new List<Pair<Thing, CompProjectileInterceptor>>();


        public static void Patch(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(Thing), "SpawnSetup"),
                postfix: new HarmonyMethod(typeof(ShieldUtils), "SpawnSetupPostfix"));
            harm.Patch(AccessTools.Method(typeof(Thing), "Destroy"),
                postfix: new HarmonyMethod(typeof(ShieldUtils), "DestroyPostfix"));
        }

        public static bool IsInsideShield(LocalTargetInfo target, Map map)
        {
            if (SHIELDS.Count == 0) return false;
            return SHIELDS.Any(p => p.Second.Active &&
                                    p.First.Map == map &&
                                    p.First.Position.InHorDistOf(target.Cell, p.Second.Props.radius));
        }

        public static bool IsInsideShield(this Thing t)
        {
            return IsInsideShield(t, t.Map);
        }

        public static Thing FirstShieldInRange(this Building_Turret turret)
        {
            return SHIELDS.FirstOrDefault(p =>
                p.First.Map == turret.Map && p.First.Position.InHorDistOf(turret.Position,
                    turret.AttackVerb.verbProps.range) &&
                p.Second.Active &&
                p.Second.Props.interceptAirProjectiles).First;
        }

        public static bool WillShotBeIntercepted(this Verb verb, IntVec3 from, LocalTargetInfo target, Map map)
        {
            verb.TryFindShootLineFromTo(from, target, out var line);
            return line.Points().All(p => !IsInsideShield(p, map));
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