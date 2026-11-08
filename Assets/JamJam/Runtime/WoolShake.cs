using System;

namespace JamJam
{
    public sealed class WoolShake
    {
        public const float MaxOffset = 0.35f;
        public const float MaxRoll = 2f;
        public const float Frequency = 18f;
        public const float Decay = 1.6f;
        double time;

        public float Trauma { get; private set; }

        public void Add(float amount)
        {
            if (!(amount > 0)) return;
            Trauma = Math.Min(1f, Trauma + amount);
        }

        public void Reset()
        {
            Trauma = 0;
            time = 0;
        }

        public void Step(float delta, out float x, out float y, out float roll)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            time += delta;
            Trauma = Math.Max(0f, Trauma - Decay * delta);
            var amplitude = Trauma * Trauma;
            x = 0;
            y = 0;
            roll = 0;
            if (amplitude <= 0) return;
            var phase = time * Frequency;
            x = MaxOffset * amplitude * Noise(phase, 1);
            y = MaxOffset * amplitude * Noise(phase, 2);
            roll = MaxRoll * amplitude * Noise(phase, 3);
        }

        static float Noise(double phase, int seed)
        {
            var cell = Math.Floor(phase);
            var t = phase - cell;
            var smooth = t * t * (3 - 2 * t);
            var a = Hash(cell, seed);
            var b = Hash(cell + 1, seed);
            return (float)(a + (b - a) * smooth);
        }

        static double Hash(double cell, int seed)
        {
            var value = Math.Sin(cell * 127.1 + seed * 311.7) * 43758.5453;
            return (value - Math.Floor(value)) * 2 - 1;
        }
    }
}
