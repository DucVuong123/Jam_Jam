namespace JamJam
{
    public static class WoolSession
    {
        public const string LobbyScene="Lobby";
        public const string GameScene="WoolJam";
        public static int SelectedLevel { get; set; } = -1;
        public static void Clear() { SelectedLevel=-1; }
    }
}
