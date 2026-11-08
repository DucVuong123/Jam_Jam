using System;
using System.Collections.Generic;
using JamJam;

sealed class MemoryPrefs : IWoolPrefs
{
    readonly Dictionary<string,int> values=new Dictionary<string,int>();
    public int Saves { get; private set; }
    public int GetInt(string key,int fallback) { return values.TryGetValue(key,out var value)?value:fallback; }
    public void SetInt(string key,int value) { values[key]=value; }
    public bool HasKey(string key) { return values.ContainsKey(key); }
    public void DeleteKey(string key) { values.Remove(key); }
    public void Save() { Saves++; }
}

static class ProgressChecks
{
    static int passed;
    public static int Assertions => passed;

    static void Check(bool value,string name)
    {
        if(!value) throw new InvalidOperationException(name);
        passed++;
    }

    public static void Run()
    {
        var prefs=new MemoryPrefs();
        var progress=new WoolProgress(prefs,3);
        Check(progress.Cleared==0 && progress.Current==0,"Fresh progress starts at level one");
        Check(progress.IsUnlocked(0) && !progress.IsUnlocked(1) && !progress.IsCleared(0),"Only level one is open at first");
        Check(!progress.MarkCleared(1) && progress.Cleared==0,"Locked level cannot be cleared");
        Check(progress.MarkCleared(0) && progress.Cleared==1 && progress.IsUnlocked(1) && progress.IsCleared(0),"Winning level one opens level two");
        Check(!progress.MarkCleared(0) && progress.Cleared==1,"Replaying a cleared level keeps progress");
        Check(new WoolProgress(prefs,3).Cleared==1,"Progress persists across instances");
        progress.MarkCleared(1);
        Check(progress.MarkCleared(2) && progress.Cleared==3 && progress.IsCleared(2) && progress.Current==2,"Winning the last level clears it and keeps Current in range");
        Check(!progress.MarkCleared(3) && progress.Cleared==3,"Cannot clear past the last level");
        Check(!progress.IsUnlocked(-1) && !progress.IsUnlocked(3) && !progress.IsCleared(-1) && !progress.MarkCleared(-1),"Out of range indices are rejected");

        var settings=new WoolSettings(prefs);
        Check(settings.Sound && settings.Music && settings.Vibration,"Settings default to on");
        settings.Music=false;
        Check(!new WoolSettings(prefs).Music && new WoolSettings(prefs).Sound,"Settings persist");
        progress.Reset();
        Check(progress.Cleared==0 && new WoolProgress(prefs,3).Cleared==0,"Reset locks everything again");
        Check(!new WoolSettings(prefs).Music,"Reset keeps settings");

        var legacy=new MemoryPrefs();
        legacy.SetInt(WoolProgress.LegacyKey,2);
        Check(new WoolProgress(legacy,3).Cleared==2 && !legacy.HasKey(WoolProgress.LegacyKey) && legacy.GetInt(WoolProgress.ClearedKey,-1)==2,"Legacy level key migrates once");
        legacy.SetInt(WoolProgress.LegacyKey,0);
        Check(new WoolProgress(legacy,3).Cleared==2,"Legacy key is ignored after migration");

        var high=new MemoryPrefs(); high.SetInt(WoolProgress.LegacyKey,99);
        Check(new WoolProgress(high,3).Cleared==3,"Legacy value above range is clamped");
        var low=new MemoryPrefs(); low.SetInt(WoolProgress.LegacyKey,-5);
        Check(new WoolProgress(low,3).Cleared==0,"Legacy value below range is clamped");
        var shrink=new MemoryPrefs(); shrink.SetInt(WoolProgress.ClearedKey,5);
        var shrunk=new WoolProgress(shrink,3);
        Check(shrunk.Cleared==3 && shrunk.Current==2,"Stored progress is clamped to level count");
    }
}
