using RimWorld;
using Verse;

namespace AutoMortarShellChoice
{
    public interface ITargetVerifier
    {
        bool VerifyTarget(Thing target);
    }

    public class FireTargetVerifier : ITargetVerifier
    {
        public bool VerifyTarget(Thing target)
        {
            return target is Pawn p && Fireable(p);
        }

        private static bool Fireable(Pawn p)
        {
            return p.CanEverAttachFire() && p.GetStatValue(StatDefOf.ArmorRating_Heat) < 2.0f && !p.IsBurning() &&
                   p.RaceProps.IsFlesh;
        }
    }

    public class EMPTargetVerifier : ITargetVerifier
    {
        public bool VerifyTarget(Thing thing)
        {
            return thing is Pawn p && p.RaceProps.IsMechanoid || thing is Building_Turret;
        }
    }

    public class PsychicTargetVerifier : ITargetVerifier
    {
        public bool VerifyTarget(Thing thing)
        {
            return thing is Pawn p && MentalStateDefOf.Berserk.Worker.StateCanOccur(p);
        }
    }

    public class IsFleshVerifier : ITargetVerifier
    {
        public bool VerifyTarget(Thing thing)
        {
            return thing is Pawn p && p.RaceProps.IsFlesh;
        }
    }

    public class IsMechVerifier : ITargetVerifier
    {
        public bool VerifyTarget(Thing thing)
        {
            return thing is Pawn p && p.RaceProps.IsMechanoid;
        }
    }

    public class IsInsectVerifier : ITargetVerifier
    {
        public bool VerifyTarget(Thing thing)
        {
            return thing is Pawn p && p.RaceProps.IsFlesh && p.RaceProps.FleshType == FleshTypeDefOf.Insectoid;
        }
    }
}