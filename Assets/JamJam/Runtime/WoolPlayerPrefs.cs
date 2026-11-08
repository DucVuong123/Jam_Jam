using UnityEngine;

namespace JamJam
{
    public sealed class WoolPlayerPrefs : IWoolPrefs
    {
        public int GetInt(string key,int fallback) { return PlayerPrefs.GetInt(key,fallback); }
        public void SetInt(string key,int value) { PlayerPrefs.SetInt(key,value); }
        public bool HasKey(string key) { return PlayerPrefs.HasKey(key); }
        public void DeleteKey(string key) { PlayerPrefs.DeleteKey(key); }
        public void Save() { PlayerPrefs.Save(); }
    }
}
