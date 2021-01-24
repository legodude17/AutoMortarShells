using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace AutoMortarShellChoice
{
    public class WorkGiver_Rearm_AutoMortar : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Turret>()
                .Where(t => t.TryGetComp<CompAutoChangeProj>() != null);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return base.HasJobOnThing(pawn, t, forced) && t is ThingWithComps twc &&
                   twc.TryGetComp<CompAutoChangeProj>() is CompAutoChangeProj cacp && cacp.NeedsReload &&
                   pawn.CanReserve(t);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var job = new Job(RearmDefOf.SC_RearmAuto, t);
            var comp = t.TryGetComp<CompAutoChangeProj>();
            var shell = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.ClosestTouch,
                TraverseParms.For(pawn), validator: thing => comp.CanReloadFrom(thing) && pawn.CanReserve(thing));
            if (shell == null) return null;
            job.targetB = shell;
            job.count = Math.Min(shell.stackCount, comp.Props.MaxShells - comp.LoadedShells);
            return job;
        }
    }
}