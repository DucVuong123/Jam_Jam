using System;

namespace JamJam
{
    public sealed class WoolSettings
    {
        public const string SoundKey="JamJam.Sound";
        public const string MusicKey="JamJam.Music";
        public const string VibrationKey="JamJam.Vibration";
        readonly IWoolPrefs prefs;

        public WoolSettings(IWoolPrefs prefs) { this.prefs=prefs ?? throw new ArgumentNullException(nameof(prefs)); }

        public bool Sound { get => Get(SoundKey); set => Set(SoundKey,value); }
        public bool Music { get => Get(MusicKey); set => Set(MusicKey,value); }
        public bool Vibration { get => Get(VibrationKey); set => Set(VibrationKey,value); }

        bool Get(string key) { return prefs.GetInt(key,1)!=0; }

        void Set(string key,bool value)
        {
            prefs.SetInt(key,value?1:0);
            prefs.Save();
        }
    }
}
