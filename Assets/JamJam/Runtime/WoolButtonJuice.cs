using System;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float PressedScale = 0.92f;
        public const float PressTime = 0.06f;
        public const float ReleaseTime = 0.18f;
        IDisposable clock;
        IWoolHaptics haptics;
        WoolSettings settings;
        bool vibration;
        bool pressed;
        bool overshoot;
        float from = 1;
        float to = 1;
        float elapsed;
        float duration;

        public bool Animating => clock != null;
        public bool Pressed => pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            var selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return;
            pressed = true;
            Buzz();
            Move(PressedScale, PressTime, false);
        }

        public void OnPointerUp(PointerEventData eventData) { Release(); }

        public void OnPointerExit(PointerEventData eventData) { Release(); }

        public void Step(float delta)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            elapsed += delta;
            var t = duration > 0 ? WoolEase.Clamp(elapsed / duration) : 1;
            var k = overshoot ? WoolEase.OutBack(t) : t;
            transform.localScale = Vector3.one * (t >= 1 ? to : from + (to - from) * k);
            if (t >= 1) Stop();
        }

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            Move(1, ReleaseTime, true);
        }

        void Move(float target, float time, bool back)
        {
            from = transform.localScale.x;
            to = target;
            elapsed = 0;
            duration = time;
            overshoot = back;
            if (clock == null) clock = Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_ => Step(Time.unscaledDeltaTime));
        }

        void Stop()
        {
            clock?.Dispose();
            clock = null;
        }

        void Buzz()
        {
            if (settings == null) settings = new WoolSettings(new WoolPlayerPrefs());
            var enabled = settings.Vibration;
            if (haptics == null || enabled != vibration)
            {
                haptics?.Dispose();
                WoolHaptics.Create(enabled, out haptics);
                vibration = enabled;
            }
            WoolHapticPattern.Get(WoolFeelKind.BlockLaunched, out var timings, out var amplitudes);
            haptics.Play(timings, amplitudes);
        }

        void OnDisable()
        {
            Stop();
            pressed = false;
            transform.localScale = Vector3.one;
        }

        void OnDestroy()
        {
            haptics?.Dispose();
            haptics = null;
        }
    }
}
