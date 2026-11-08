using System;

namespace JamJam
{
    public sealed class WoolProgress
    {
        public const string ClearedKey="JamJam.Cleared";
        public const string LegacyKey="JamJam.Level";
        readonly IWoolPrefs prefs;
        public int LevelCount { get; }
        public int Cleared { get; private set; }
        public int Current => Math.Min(Cleared,LevelCount-1);

        public WoolProgress(IWoolPrefs prefs,int levelCount)
        {
            if(prefs==null) throw new ArgumentNullException(nameof(prefs));
            if(levelCount<1) throw new ArgumentOutOfRangeException(nameof(levelCount));
            this.prefs=prefs;
            LevelCount=levelCount;
            if(!prefs.HasKey(ClearedKey) && prefs.HasKey(LegacyKey))
            {
                prefs.SetInt(ClearedKey,Clamp(prefs.GetInt(LegacyKey,0)));
                prefs.DeleteKey(LegacyKey);
                prefs.Save();
            }
            Cleared=Clamp(prefs.GetInt(ClearedKey,0));
        }

        public bool IsUnlocked(int index) { return index>=0 && index<LevelCount && index<=Cleared; }
        public bool IsCleared(int index) { return index>=0 && index<Cleared; }

        public bool MarkCleared(int index)
        {
            if(index!=Cleared || index>=LevelCount) return false;
            Cleared++;
            prefs.SetInt(ClearedKey,Cleared);
            prefs.Save();
            return true;
        }

        public void Reset()
        {
            Cleared=0;
            prefs.DeleteKey(ClearedKey);
            prefs.Save();
        }

        int Clamp(int value) { return Math.Max(0,Math.Min(LevelCount,value)); }
    }
}
