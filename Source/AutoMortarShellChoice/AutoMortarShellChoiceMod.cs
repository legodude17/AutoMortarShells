using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AutoMortarShellChoice
{
    public class AutoMortarShellChoiceMod : Mod
    {
        private readonly Harmony harm;

        public AutoMortarShellChoiceMod(ModContentPack content) : base(content)
        {
            harm = new Harmony("legodude17.vfemechscp");
            Settings = GetSettings<AMSCSettings>();
            SmartTargeting.Mod = this;
            SmartTargeting.Patch(harm);
            if (Settings.SmartTargeting) SmartTargeting.PatchTurret(harm);
            Log.Message("Applied patches for " + harm.Id);
        }

        public AMSCSettings Settings { get; }

        public override string SettingsCategory()
        {
            return "VFE - Mechs - Auto Mortar Shell Choice";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            var rect1 = inRect.ContractedBy(10f);
            var rect2 = rect1.LeftHalf();
            var rect3 = rect1.RightHalf().LeftPartPixels(20f);
            Widgets.Label(rect2.TopPartPixels(20f), "AMSC.ToggleSmartTarget".Translate());
            if (Widgets.ButtonImage(rect3.TopPartPixels(20f),
                Settings.SmartTargeting ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex))
                if (Settings.SmartTargeting)
                {
                    SmartTargeting.PatchTurret(harm);
                    Settings.SmartTargeting = false;
                }
                else
                {
                    SmartTargeting.Unpatch(harm);
                    Settings.SmartTargeting = true;
                }

            Widgets.DrawLineHorizontal(rect2.x, rect2.y + 35f, rect1.width - 10f);

            if (Mouse.IsOver(rect1.TopPartPixels(20f)))
                TooltipHandler.TipRegion(rect1.TopPartPixels(20f), "AMSC.ToggleSmartTargetDesc".Translate());
            if (!Settings.SmartTargeting) return;

            Widgets.Label(rect2.TopPartPixels(60f).BottomPartPixels(20f), "AMSC.ToggleAutoFirefight".Translate());
            if (Widgets.ButtonImage(rect3.TopPartPixels(60f).BottomPartPixels(20f),
                Settings.AutoFirefight ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex))
                Settings.AutoFirefight = !Settings.AutoFirefight;
            if (Mouse.IsOver(rect1.TopPartPixels(50f).BottomPartPixels(20f)))
                TooltipHandler.TipRegion(rect1.TopPartPixels(60f).BottomPartPixels(20f),
                    "AMSC.ToggleAutoFirefightDesc".Translate());

            Widgets.Label(rect2.TopPartPixels(90f).BottomPartPixels(20f), "AMSC.SmartShieldTactics".Translate());
            if (Widgets.ButtonText(new Rect(rect3.x, rect2.y + 70f, 250f, 30f),
                ("AMSC.SmartShieldTactics" + Settings.SmartShieldTactics).Translate()))
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

            if (Mouse.IsOver(rect1.TopPartPixels(90f).BottomPartPixels(20f)))
                TooltipHandler.TipRegion(rect1.TopPartPixels(90).BottomPartPixels(20),
                    "AMSC.SmartShieldTacticsDesc".Translate());
        }
    }

    public class AMSCSettings : ModSettings
    {
        public bool AutoFirefight = true;
        public int SmartShieldTactics = 1;
        public bool SmartTargeting = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SmartShieldTactics, "SmartShieldTactics", 1);
            Scribe_Values.Look(ref SmartTargeting, "SmartTargeting", true);
            Scribe_Values.Look(ref AutoFirefight, "AutoFirefight", true);
        }
    }
}