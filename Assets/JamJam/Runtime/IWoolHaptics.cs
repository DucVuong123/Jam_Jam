using System;

namespace JamJam
{
    public interface IWoolHaptics : IDisposable
    {
        void Play(long[] timings, int[] amplitudes);
    }
}
