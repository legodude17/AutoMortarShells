using Verse;

namespace AutoMortarShellChoice
{
    public class Verb_Auto : Verb_Shoot
    {
        public override ThingDef Projectile
        {
            get
            {
                if (caster.TryGetComp<CompAutoChangeProj>() is CompAutoChangeProj cacp && cacp.Loaded)
                    return cacp.Projectile;
                return base.Projectile;
            }
        }

        public override bool Available()
        {
            if (caster.TryGetComp<CompAutoChangeProj>() is CompAutoChangeProj cacp && !cacp.Loaded)
                return false;
            return base.Available();
        }

        protected override bool TryCastShot()
        {
            if (caster.TryGetComp<CompAutoChangeProj>() is CompAutoChangeProj cacp)
            {
                if (!cacp.Loaded) return false;
                var flag = base.TryCastShot();
                cacp.Notify_ProjectileLaunched();
                return flag;
            }

            return false;
        }
    }
}