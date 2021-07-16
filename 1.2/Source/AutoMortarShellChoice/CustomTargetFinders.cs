using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AutoMortarShellChoice
{
    public interface ITargetFinder
    {
        LocalTargetInfo FindTarget(IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> verifier,
            float minDistance = 0f, float maxDistance = 9999f);

        bool ShouldUse(IAttackTargetSearcher searcher);
    }

    public class EMPTargetFinder : ITargetFinder
    {
        public LocalTargetInfo FindTarget(IAttackTargetSearcher searcher, TargetScanFlags flags,
            Predicate<Thing> verifier,
            float minDistance = 0, float maxDistance = 9999)
        {
            if (!(searcher.Thing is Building_Turret turret)) return LocalTargetInfo.Invalid;
            return turret.FirstShieldInRange() ?? LocalTargetInfo.Invalid;
        }

        public bool ShouldUse(IAttackTargetSearcher searcher)
        {
            if (!(searcher.Thing is Building_Turret)) return false;
            return AutoMortarShellChoiceMod.Settings.SmartShieldTactics != 0;
        }
    }

    public class FireFinder : ITargetFinder
    {
        public LocalTargetInfo FindTarget(IAttackTargetSearcher searcher, TargetScanFlags flags,
            Predicate<Thing> verifier,
            float minDistance = 0, float maxDistance = 9999)
        {
            if (!(searcher.Thing is Building_Turret turret)) return LocalTargetInfo.Invalid;
            var comp = turret.GetCompAutoChangeProj();
            if (comp == null) return LocalTargetInfo.Invalid;
            var fires = turret.Map.listerThings.ThingsOfDef(ThingDefOf.Fire)
                .Where(t => turret.AttackVerb.CanHitTarget(t)).Select(fire => new Pair<Thing, int>(fire,
                    GenRadial
                        .RadialDistinctThingsAround(fire.Position, fire.Map,
                            comp.Projectile.projectile.explosionRadius,
                            true).Count(t => t.def == ThingDefOf.Fire))).ToList();
            fires.SortBy(p => p.Second);
            return fires.FirstOrDefault().First;
        }

        public bool ShouldUse(IAttackTargetSearcher searcher)
        {
            if (!(searcher.Thing is Building_Turret)) return false;
            return AutoMortarShellChoiceMod.Settings.AutoFirefight;
        }
    }

    public class ClusterFinder : ITargetFinder
    {
        public LocalTargetInfo FindTarget(IAttackTargetSearcher searcher, TargetScanFlags flags,
            Predicate<Thing> verifier,
            float minDistance = 0, float maxDistance = 9999)
        {
            if (!(searcher.Thing is Building_Turret turret)) return LocalTargetInfo.Invalid;
            var comp = turret.GetCompAutoChangeProj();
            if (comp == null) return LocalTargetInfo.Invalid;
            var locations = turret.Map.attackTargetsCache.targetsHostileToFaction[turret.Faction]
                .Select(t => t.Thing as Pawn)
                .Where(t => verifier(t)).Select(p =>
                    AutoMortarShellChoiceMod.Settings.TargetLeading
                        ? CalculateIntercept(p, turret.Position, comp.Projectile.projectile.SpeedTilesPerTick)
                        : p.Position);
            return BestClusterPos(locations, comp.Projectile.projectile.explosionRadius);
        }

        public bool ShouldUse(IAttackTargetSearcher searcher)
        {
            return searcher.Thing is Building_Turret;
        }

        public static IntVec3 CalculateIntercept(Pawn p, IntVec3 fireFrom, float speed)
        {
            var times = p.pather.curPath.nodes.Select(c => p.pather.CostToMoveIntoCell(c)).ToList();
            var times2 = new List<int>(times.Count) {times[0]};
            for (var i = 1; i < times.Count; i++) times2[i] = times[i] + times2[i - 1];
            return p.pather.curPath.nodes.Zip(times2, (c, t) => new Pair<IntVec3, int>(c, t)).MinBy(pair =>
                Mathf.Abs(pair.First.DistanceTo(fireFrom) / speed - pair.Second)).First;
        }

        public static IntVec3 BestClusterPos(IEnumerable<IntVec3> locations, float radius)
        {
            // TODO: Proper cluster finding
            return locations.RandomElement();
        }
    }
}