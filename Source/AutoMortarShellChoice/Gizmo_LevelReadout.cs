using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class Gizmo_LevelReadout : Gizmo
    {
        private const float ArrowScale = 0.5f;

        private static readonly Texture2D FullBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));

        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

        private static readonly Texture2D TargetLevelArrow =
            ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");

        public string Label;
        public float MaxValue;
        public float Value;

        public Gizmo_LevelReadout()
        {
            order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            var overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, delegate
            {
                Rect rect2;
                var rect = rect2 = overRect.AtZero().ContractedBy(6f);
                rect2.height = overRect.height / 2f;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect2, Label);
                var rect3 = rect;
                rect3.yMin = overRect.height / 2f;
                Widgets.FillableBar(rect3, Value / MaxValue, FullBarTex, EmptyBarTex, false);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect3,
                    Value.ToString("F0") + " / " + MaxValue.ToString("F0"));
                Text.Anchor = TextAnchor.UpperLeft;
            });
            return new GizmoResult(GizmoState.Clear);
        }
    }
}