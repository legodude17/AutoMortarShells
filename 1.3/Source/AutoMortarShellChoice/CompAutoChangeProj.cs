using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoMortarShellChoice
{
    public class CompAutoChangeProj : CompChangeableProjectile, IThingHolder
    {
        private readonly ThingOwner<Thing> container = new ThingOwner<Thing>();
        private Thing nextAmmoItem;

        public new CompProperties_AutoChangeProj Props => props as CompProperties_AutoChangeProj;

        public new ThingDef Projectile => nextAmmoItem?.def?.projectileWhenLoaded;
        public new bool Loaded => container.Any;
        public bool NeedsReload => LoadedShells < Props.MaxShells;
        public int LoadedShells => container.Any() ? container.Select(t => t.stackCount).Sum() : 0;

        public int ReloadTicks => Props.ReloadTime.SecondsToTicks();

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, container);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return container;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            container.ExposeData();
            Scribe_References.Look(ref nextAmmoItem, "nextAmmoItem");
        }

        public override void Notify_ProjectileLaunched()
        {
            nextAmmoItem.stackCount--;
            if (nextAmmoItem.stackCount == 0)
            {
                container.Remove(nextAmmoItem);
                nextAmmoItem.Destroy();
                nextAmmoItem = container.FirstOrFallback();
            }
        }

        public void ReloadFrom(Thing shell)
        {
            var num = Math.Min(Props.MaxShells - LoadedShells, shell.stackCount);
            var thing = shell.SplitOff(num);
            container.TryAddOrTransfer(thing);
            if (nextAmmoItem == null) nextAmmoItem = shell;
        }

        public bool CanReloadFrom(Thing shell)
        {
            return allowedShellsSettings.AllowedToAccept(shell) && NeedsReload;
        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra() + (Loaded
                ? "ShellLoaded".Translate(nextAmmoItem.LabelCap,
                    nextAmmoItem)
                : "ShellNotLoaded".Translate());
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;

            if (nextAmmoItem != null)
                yield return new Command_Action
                {
                    defaultLabel = "CommandExtractShell".Translate(),
                    defaultDesc = "CommandExtractShellDesc".Translate(),
                    icon = nextAmmoItem.def.uiIcon,
                    iconAngle = nextAmmoItem.def.uiIconAngle,
                    iconOffset = nextAmmoItem.def.uiIconOffset,
                    iconDrawScale = GenUI.IconDrawScale(nextAmmoItem.def),
                    action = delegate
                    {
                        container.TryDrop(nextAmmoItem, parent.Position, parent.Map, ThingPlaceMode.Near, 1,
                            out var thing);
                        if (!container.Any) nextAmmoItem = null;
                    }
                };

            yield return new Gizmo_LevelReadout
            {
                Label = "Remaining shells",
                Value = LoadedShells,
                MaxValue = Props.MaxShells
            };
        }
    }

    public class CompProperties_AutoChangeProj : CompProperties
    {
        public int MaxShells;
        public float ReloadTime;

        public CompProperties_AutoChangeProj()
        {
            compClass = typeof(CompAutoChangeProj);
        }
    }
}