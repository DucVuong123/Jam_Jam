using System;
using R3;
using UnityEngine;

namespace JamJam
{
    public sealed class WoolPopIn : MonoBehaviour
    {
        public const float DefaultFromScale = 0.8f;
        public const float DefaultScaleTime = 0.3f;
        public const float DefaultFadeTime = 0.2f;
        public CanvasGroup Fade;
        public float FromScale = DefaultFromScale;
        public float ScaleTime = DefaultScaleTime;
        public float FadeTime = DefaultFadeTime;
        IDisposable clock;
        float elapsed;

        public bool Animating => clock != null;

        void OnEnable()
        {
            elapsed = 0;
            Apply();
            clock?.Dispose();
            clock = Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_ => Step(Time.unscaledDeltaTime));
        }

        public void Step(float delta)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            elapsed += delta;
            Apply();
            if (elapsed >= ScaleTime && elapsed >= FadeTime) Stop();
        }

        void Apply()
        {
            var t = ScaleTime > 0 ? WoolEase.Clamp(elapsed / ScaleTime) : 1;
            transform.localScale = Vector3.one * (t >= 1 ? 1 : FromScale + (1 - FromScale) * WoolEase.OutBack(t));
            if (Fade != null) Fade.alpha = FadeTime > 0 ? WoolEase.Clamp(elapsed / FadeTime) : 1;
        }

        void Stop()
        {
            clock?.Dispose();
            clock = null;
        }

        void OnDisable()
        {
            Stop();
            transform.localScale = Vector3.one;
            if (Fade != null) Fade.alpha = 1;
        }
    }
}
