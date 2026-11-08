namespace JamJam
{
    public interface IWoolPrefs
    {
        int GetInt(string key,int fallback);
        void SetInt(string key,int value);
        bool HasKey(string key);
        void DeleteKey(string key);
        void Save();
    }
}
