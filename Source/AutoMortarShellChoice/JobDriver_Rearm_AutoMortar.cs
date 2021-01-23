using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AutoMortarShellChoice
{
    internal class JobDriver_Rearm_AutoMortar : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var comp = TargetA.Thing.TryGetComp<CompAutoChangeProj>();

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.WaitWith(TargetIndex.A, comp.ReloadTicks, true);
            yield return new Toil
            {
                initAction = () => comp.ReloadFrom(pawn.carryTracker.CarriedThing),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }

    [DefOf]
    public class RearmDefOf
    {
        public static JobDef SC_RearmAuto;
    }
}