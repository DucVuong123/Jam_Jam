using System;
using System.Collections.Generic;

namespace JamJam
{
    public enum WoolPathShape { S, U, Zigzag, Hook, Spiral }

    public static class WoolPaths
    {
        public const int Samples = 20;
        public const float MinX = -4.0f;
        public const float MaxX = 3.8f;
        public const float MinZ = 0.67f;
        public const float MaxZ = 8.09f;
        static readonly float[][] Controls =
        {
            new[] { -4f, 8f, 2.8f, 8f, 3.75f, 6.8f, 2.8f, 5.55f, -2.8f, 5.55f, -3.75f, 4.3f, -2.8f, 3.05f, 2.5f, 3.05f, 3.35f, 1.8f, 2.15f, 0.75f, -1.65f, 0.75f },
            new[] { -3.9f, 8f, -3.9f, 2f, -2.9f, 0.85f, 2.6f, 0.85f, 3.6f, 2f, 3.6f, 6.9f, 2.6f, 7.9f, -0.4f, 7.9f, -1.2f, 7.1f, -1.2f, 4.1f, -0.5f, 3.25f, 0.6f, 3.25f },
            new[] { -3.9f, 8f, -3.9f, 1.9f, -2.6f, 0.85f, -1.3f, 1.9f, -1.3f, 7f, 0f, 8f, 1.2f, 7f, 1.2f, 1.9f, 2.4f, 0.85f, 3.6f, 1.9f, 3.6f, 6.4f },
            new[] { -3.9f, 8f, 1.6f, 8f, 3.4f, 7f, 3.6f, 4.2f, 3.4f, 1.75f, 2.4f, 0.85f, -2.6f, 0.85f, -3.7f, 1.9f, -3.6f, 4.1f, -2.6f, 5f, 0.6f, 5f },
            new[] { -3.9f, 8f, 2.6f, 8f, 3.6f, 7f, 3.6f, 1.8f, 2.6f, 0.85f, -2.8f, 0.85f, -3.8f, 1.8f, -3.8f, 4.6f, -2.8f, 5.6f, -1.4f, 5.6f, -0.8f, 4.8f, -0.8f, 3.9f, -0.2f, 3.3f, 0.6f, 3.3f }
        };

        public static WoolPathShape ForLevel(int level)
        {
            return (WoolPathShape)(((level - 1) % 5 + 5) % 5);
        }

        public static void Control(WoolPathShape shape, out IReadOnlyList<RoutePoint> points)
        {
            var values = Controls[(int)shape];
            var result = new RoutePoint[values.Length / 2];
            for (var i = 0; i < result.Length; i++) result[i] = new RoutePoint(values[i * 2], values[i * 2 + 1]);
            points = result;
        }

        public static void Sample(WoolPathShape shape, out IReadOnlyList<RoutePoint> points)
        {
            Control(shape, out var control);
            var result = new List<RoutePoint>((control.Count - 1) * Samples + 1);
            for (var i = 0; i < control.Count - 1; i++)
            {
                var a = control[Math.Max(0, i - 1)];
                var b = control[i];
                var c = control[i + 1];
                var d = control[Math.Min(control.Count - 1, i + 2)];
                for (var sample = 0; sample < Samples; sample++)
                {
                    var t = sample / 20f;
                    result.Add(new RoutePoint(Spline(a.X, b.X, c.X, d.X, t), Spline(a.Z, b.Z, c.Z, d.Z, t)));
                }
            }
            result.Add(control[control.Count - 1]);
            points = result;
        }

        static float Spline(float a, float b, float c, float d, float t)
        {
            var p0 = (float)(2 * b);
            var p1 = (float)((float)(-a + c) * t);
            var q = (float)((float)((float)((float)(2 * a) - (float)(5 * b)) + (float)(4 * c)) - d);
            var p2 = (float)((float)(q * t) * t);
            var r = (float)((float)((float)(-a + (float)(3 * b)) - (float)(3 * c)) + d);
            var p3 = (float)((float)((float)(r * t) * t) * t);
            var sum = (float)((float)((float)(p0 + p1) + p2) + p3);
            return (float)(0.5f * sum);
        }

        public static float Length(IReadOnlyList<RoutePoint> points)
        {
            var length = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                var dx = points[i - 1].X - points[i].X;
                var dy = 0f;
                var dz = points[i - 1].Z - points[i].Z;
                var sum = (float)((float)((float)(dx * dx) + (float)(dy * dy)) + (float)(dz * dz));
                length = (float)(length + (float)Math.Sqrt(sum));
            }
            return length;
        }
    }
}
