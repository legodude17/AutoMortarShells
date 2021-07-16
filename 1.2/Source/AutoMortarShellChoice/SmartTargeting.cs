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

        private static readonly Dictionary<Thing, CompAutoChangeProj> compCache =
            new Dictionary<Thing, CompAutoChangeProj>();

        private static readonly Dictionary<Def, ITargetVerifier> verifierCache =
            new Dictionary<Def, ITargetVerifier>();

        private static readonly Dictionary<Def, ITargetFinder> finderCache = new Dictionary<Def, ITargetFinder>();

        public static CompAutoChangeProj GetCompAutoChangeProj(this Thing t)
        {
            if (compCache.TryGetValue(t, out var comp)) return comp;
            comp = t.TryGetComp<CompAutoChangeProj>();
            compCache.Add(t, comp);
            return comp;
        }

        public static void PatchTurret(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryFindNewTarget"),
                postfix: new HarmonyMethod(typeof(SmartTargeting), "TryFindNewTargetPostfix"));
        }

        public static void Unpatch(Harmony harm)
        {
            harm.Unpatch(AccessTools.Method(typeof(Building_TurretGun), "TryFindNewTarget"),
                HarmonyPatchType.Postfix, harm.Id);
        }

        public static ITargetVerifier GetVerifier(this Def def)
        {
            if (verifierCache.TryGetValue(def, out var verify)) return verify;
            var ext = def.GetModExtension<SmartTargetProps>();
            verify = ext?.TargetVerifier == null
                ? null
                : (ITargetVerifier) Activator.CreateInstance(ext.TargetVerifier);
            verifierCache.Add(def, verify);
            return verify;
        }

        public static ITargetFinder GetFinder(this Def def)
        {
            if (finderCache.TryGetValue(def, out var finder)) return finder;
            var ext = def.GetModExtension<SmartTargetProps>();
            finder = ext?.TargetFinder == null ? null : (ITargetFinder) Activator.CreateInstance(ext.TargetFinder);
            finderCache.Add(def, finder);
            return finder;
        }

        public static void TryFindNewTargetPostfix(Building_TurretGun __instance, ref LocalTargetInfo __result)
        {
            var comp = __instance.GetCompAutoChangeProj();
            if (comp?.Projectile == null) return;
            var predicates = new List<Predicate<Thing>>();
            if (comp.Projectile.projectile.damageDef != DamageDefOf.EMP &&
                AutoMortarShellChoiceMod.Settings.SmartShieldTactics > 1 &&
                __result.IsValid)
            {
                if (AutoMortarShellChoiceMod.Settings.SmartShieldTactics > 2)
                    predicates.Add(t =>
                        !__instance.AttackVerb.WillShotBeIntercepted(__instance.Position, t, __instance.Map));
                else
                    predicates.Add(t => !t.IsInsideShield());
            }

            var verify1 = GetVerifier(comp.Projectile?.projectile?.damageDef);
            var verify2 = GetVerifier(comp.Projectile);

            if (verify1 != null) predicates.Add(verify1.VerifyTarget);
            if (verify2 != null) predicates.Add(verify2.VerifyTarget);

            var validator = predicates.Collapse();
            var finder = GetFinder(comp.Projectile) ?? GetFinder(comp.Projectile?.projectile?.damageDef);
            if (__result.IsValid && __result.HasThing && !validator(__result.Thing) ||
                finder != null && finder.ShouldUse(__instance))
                __result = __instance.TryFindNewTarget_Copy(validator, finder);
        }

        private static Predicate<T> Collapse<T>(this List<Predicate<T>> list) where T : class
        {
            return obj => list.All(p => p(obj));
        }

        private static LocalTargetInfo TryFindNewTarget_Copy(this Building_TurretGun b, Predicate<Thing> validator,
            ITargetFinder finder)
        {
            var targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable |
                                  TargetScanFlags.NeedNotUnderThickRoof;
            if (b.AttackVerb.IsIncendiary()) targetScanFlags |= TargetScanFlags.NeedNonBurning;
            LocalTargetInfo result;
            if (finder != null && finder.ShouldUse(b) &&
                (result = finder.FindTarget(b, targetScanFlags, validator)) != null && result.IsValid) return result;

            return (Thing) AttackTargetFinder.BestShootTargetFromCurrentPosition(b, targetScanFlags,
                validator);
        }
    }

    public class SmartTargetProps : DefModExtension
    {
        public Type TargetFinder;
        public Type TargetVerifier;

        public override IEnumerable<string> ConfigErrors()
        {
            if (TargetVerifier != null && !typeof(ITargetVerifier).IsAssignableFrom(TargetVerifier))
                yield return "TargetVerifier must implement ITargetVerifier";
            if (TargetFinder != null && !typeof(ITargetFinder).IsAssignableFrom(TargetFinder))
                yield return "TargetFinder must implement ITargetFinder";
            foreach (var error in base.ConfigErrors()) yield return error;
        }
    }
}