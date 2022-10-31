using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AutoMortarShellChoice
{
    public class AutoMortarShellChoiceMod : Mod
    {
        public static AMSCSettings Settings;
        private readonly Harmony harm;

        public AutoMortarShellChoiceMod(ModContentPack content) : base(content)
        {
            harm = new Harmony("legodude17.vfemechscp");
            Settings = GetSettings<AMSCSettings>();
            SmartTargeting.Mod = this;
            ShieldUtils.Patch(harm);
            if (Settings.SmartTargeting) SmartTargeting.PatchTurret(harm);
            Log.Message("Applied patches for " + harm.Id);
        }

        public override string SettingsCategory() => "VFE - Mechs - Auto Mortar Shell Choice";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            var oldSmart = Settings.SmartTargeting;
            listing.CheckboxLabeled("AMSC.ToggleSmartTarget".Translate(), ref Settings.SmartTargeting, "AMSC.ToggleSmartTargetDesc".Translate());
            if (oldSmart != Settings.SmartTargeting)
            {
                if (Settings.SmartTargeting)
                    SmartTargeting.PatchTurret(harm);
                else
                    SmartTargeting.Unpatch(harm);
            }

            listing.GapLine(24f);
            if (Settings.SmartTargeting)
            {
                listing.CheckboxLabeled("AMSC.ToggleAutoFirefight".Translate(), ref Settings.AutoFirefight, "AMSC.ToggleAutoFirefightDesc".Translate());
                if (listing.ButtonTextLabeled("AMSC.SmartShieldTactics".Translate(), $"AMSC.SmartShieldTactics{Settings.SmartShieldTactics}".Translate(),
                        tooltip:
                        "AMSC.SmartShieldTacticsDesc".Translate()))
                {
                    var opts = new List<FloatMenuOption>();
                    for (var i = 0; i <= 3; i++)
                    {
                        var i1 = i;
                        opts.Add(new FloatMenuOption(("AMSC.SmartShieldTactics" + i1).Translate(),
                            () => Settings.SmartShieldTactics = i1));
                    }

                    Find.WindowStack.Add(new FloatMenu(opts));
                }
            }

            listing.End();
        }
    }

    public class AMSCSettings : ModSettings
    {
        public bool AutoFirefight = true;
        public int SmartShieldTactics = 1;
        public bool SmartTargeting = true;
        public bool TargetLeading = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SmartShieldTactics, "SmartShieldTactics", 1);
            Scribe_Values.Look(ref SmartTargeting, "SmartTargeting", true);
            Scribe_Values.Look(ref AutoFirefight, "AutoFirefight", true);
        }
    }
}