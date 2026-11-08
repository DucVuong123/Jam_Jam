using System;

namespace JamJam
{
    public static class WoolEase
    {
        const double Overshoot = 1.70158;
        const double PunchGain = 1.4;

        public static float Clamp(float t)
        {
            if (!(t > 0)) return 0;
            return t > 1 ? 1 : t;
        }

        public static float OutBack(float t)
        {
            t = Clamp(t);
            if (t <= 0 || t >= 1) return t;
            var x = t - 1.0;
            return (float)(1 + (Overshoot + 1) * x * x * x + Overshoot * x * x);
        }

        public static float Punch(float t)
        {
            t = Clamp(t);
            if (t <= 0 || t >= 1) return 0;
            var decay = 1.0 - t;
            return (float)(PunchGain * Math.Sin(3 * Math.PI * t) * decay * decay);
        }

        public static float Arc(float t)
        {
            t = Clamp(t);
            if (t <= 0 || t >= 1) return 0;
            return (float)Math.Sin(Math.PI * t);
        }
    }
}
