using System;
using R3;
using UnityEngine;

namespace JamJam
{
    public enum WoolUiMotionKind { Spin, Bob, Sway, Breathe }

    public sealed class WoolUiMotion : MonoBehaviour
    {
        public WoolUiMotionKind Kind;
        public float Amount;
        public float Period = 1;
        IDisposable clock;
        RectTransform rect;
        Vector2 rest;
        double last;

        public bool Running => clock != null;
        public float Elapsed { get; private set; }

        void OnEnable()
        {
            rect = (RectTransform)transform;
            rest = rect.anchoredPosition;
            Elapsed = 0;
            Apply();
            last = Time.unscaledTimeAsDouble;
            clock?.Dispose();
            clock = Observable.Interval(TimeSpan.FromSeconds(1d / 60d), UnityTimeProvider.UpdateIgnoreTimeScale).Subscribe(_ => Tick());
        }

        void Tick()
        {
            var now = Time.unscaledTimeAsDouble;
            Step(Mathf.Min((float)(now - last), 0.1f));
            last = now;
        }

        public void Step(float delta)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            Elapsed += delta;
            Apply();
        }

        void Apply()
        {
            Pose(Kind, Amount, Period, Elapsed, out var offset, out var angle, out var scale);
            rect.anchoredPosition = rest + offset;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            rect.localScale = Vector3.one * scale;
        }

        void OnDisable()
        {
            clock?.Dispose();
            clock = null;
            Elapsed = 0;
            if (rect == null) return;
            rect.anchoredPosition = rest;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void Pose(WoolUiMotionKind kind, float amount, float period, float time, out Vector2 offset, out float angle, out float scale)
        {
            offset = Vector2.zero;
            angle = 0;
            scale = 1;
            var phase = period > 0 ? Mathf.Repeat(time, period) / period : 0;
            var rise = 0.5f * (1 - Mathf.Cos(phase * Mathf.PI * 2));
            switch (kind)
            {
                case WoolUiMotionKind.Spin: angle = -Mathf.Repeat(amount * time, 360); return;
                case WoolUiMotionKind.Bob: offset = new Vector2(0, amount * rise); return;
                case WoolUiMotionKind.Sway: angle = amount * Mathf.Sin(phase * Mathf.PI * 2); return;
                default: scale = 1 + amount * rise; return;
            }
        }
    }
}
