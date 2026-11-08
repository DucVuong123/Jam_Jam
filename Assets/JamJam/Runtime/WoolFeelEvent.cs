namespace JamJam
{
    public enum WoolFeelKind { BlockLaunched, BlockBlocked, SpoolArrived, YarnConsumed, SpoolCompleted, SlotUnlocked, TrayFull, Danger, Won, Lost }

    public readonly struct WoolFeelEvent
    {
        public readonly WoolFeelKind Kind;
        public readonly int Index;
        public readonly int Color;

        public WoolFeelEvent(WoolFeelKind kind, int index, int color)
        {
            Kind = kind;
            Index = index;
            Color = color;
        }
    }
}
