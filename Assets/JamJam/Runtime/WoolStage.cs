using System;

namespace JamJam
{
    public static class WoolStage
    {
        public const float Pitch = 50f;
        public const float HalfWidth = 5.4f;
        public const float HudBand = 1.8f;
        public const float BottomBand = 0.3f;
        public const float ContentTop = 7.7f;
        public const float ContentBottom = -7.85f;
        public const float Distance = 24f;

        public static float Lift => (ContentTop + ContentBottom) * 0.5f + (HudBand - BottomBand) * 0.5f;
        public static float TargetZ => WoolRouting.TrayZ + Lift / (float)Math.Sin(Pitch * Math.PI / 180);
        public static float MinSize => Math.Max(ContentTop - Lift + HudBand, Lift - ContentBottom + BottomBand);

        public static float Size(float aspect)
        {
            if (aspect <= 0 || float.IsNaN(aspect) || float.IsInfinity(aspect)) return MinSize;
            return Math.Max(HalfWidth / aspect, MinSize);
        }
    }
}
