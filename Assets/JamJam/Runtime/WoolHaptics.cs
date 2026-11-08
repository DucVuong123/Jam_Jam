using System;
using System.Collections.Generic;
using UnityEngine;

namespace JamJam
{
    public static class WoolHaptics
    {
        public static void Create(bool enabled, out IWoolHaptics haptics)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (enabled)
            {
                haptics = new WoolAndroidHaptics();
                return;
            }
#endif
            haptics = new WoolNullHaptics();
        }
    }

    public sealed class WoolNullHaptics : IWoolHaptics
    {
        public void Play(long[] timings, int[] amplitudes) { }
        public void Dispose() { }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    sealed class WoolAndroidHaptics : IWoolHaptics
    {
        readonly AndroidJavaObject vibrator;
        readonly int sdk;
        readonly bool amplitude;

        public WoolAndroidHaptics()
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator != null && sdk >= 26) amplitude = vibrator.Call<bool>("hasAmplitudeControl");
            }
            catch (Exception)
            {
                vibrator = null;
                amplitude = false;
            }
        }

        public void Play(long[] timings, int[] amplitudes)
        {
            if (timings == null || amplitudes == null || timings.Length == 0 || timings.Length != amplitudes.Length) return;
            try
            {
                if (vibrator == null)
                {
                    Handheld.Vibrate();
                    return;
                }
                if (sdk >= 26 && amplitude)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var wave = effect.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                        vibrator.Call("vibrate", wave);
                    return;
                }
                var pattern = ToOnOff(timings, amplitudes);
                if (pattern == null) return;
                if (sdk >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var wave = effect.CallStatic<AndroidJavaObject>("createWaveform", pattern, -1))
                        vibrator.Call("vibrate", wave);
                    return;
                }
                vibrator.Call("vibrate", pattern, -1);
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            if (vibrator != null) vibrator.Dispose();
        }

        static long[] ToOnOff(long[] timings, int[] amplitudes)
        {
            var result = new List<long>();
            var on = false;
            long duration = 0;
            var any = false;
            for (var i = 0; i < timings.Length; i++)
            {
                if (timings[i] <= 0) continue;
                var segment = amplitudes[i] > 0;
                if (segment == on)
                {
                    duration += timings[i];
                    continue;
                }
                result.Add(duration);
                on = segment;
                duration = timings[i];
                any |= segment;
            }
            if (!any) return null;
            result.Add(duration);
            return result.ToArray();
        }
    }
#endif
}
