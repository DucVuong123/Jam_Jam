using System;

namespace JamJam
{
    public static class WoolHapticPattern
    {
        static readonly long[] LightTimings = { 12 };
        static readonly int[] LightAmplitudes = { 90 };
        static readonly long[] SoftTimings = { 15 };
        static readonly int[] SoftAmplitudes = { 110 };
        static readonly long[] MediumTimings = { 30 };
        static readonly int[] MediumAmplitudes = { 170 };
        static readonly long[] WarningTimings = { 0, 18, 40, 18 };
        static readonly int[] WarningAmplitudes = { 0, 140, 0, 140 };
        static readonly long[] WinTimings = { 30, 60, 30, 60, 60 };
        static readonly int[] WinAmplitudes = { 160, 0, 200, 0, 255 };
        static readonly long[] HeavyTimings = { 90 };
        static readonly int[] HeavyAmplitudes = { 255 };

        public static void Get(WoolFeelKind kind, out long[] timings, out int[] amplitudes)
        {
            switch (kind)
            {
                case WoolFeelKind.BlockLaunched: timings = LightTimings; amplitudes = LightAmplitudes; return;
                case WoolFeelKind.SpoolArrived:
                case WoolFeelKind.SlotUnlocked: timings = SoftTimings; amplitudes = SoftAmplitudes; return;
                case WoolFeelKind.SpoolCompleted: timings = MediumTimings; amplitudes = MediumAmplitudes; return;
                case WoolFeelKind.BlockBlocked:
                case WoolFeelKind.TrayFull:
                case WoolFeelKind.Danger: timings = WarningTimings; amplitudes = WarningAmplitudes; return;
                case WoolFeelKind.Won: timings = WinTimings; amplitudes = WinAmplitudes; return;
                case WoolFeelKind.Lost: timings = HeavyTimings; amplitudes = HeavyAmplitudes; return;
                default: timings = Array.Empty<long>(); amplitudes = Array.Empty<int>(); return;
            }
        }

        public static int Strength(WoolFeelKind kind)
        {
            Get(kind, out _, out var amplitudes);
            var peak = 0;
            foreach (var amplitude in amplitudes) peak = Math.Max(peak, amplitude);
            return peak;
        }
    }
}
