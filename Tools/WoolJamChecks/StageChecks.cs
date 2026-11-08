using System;
using JamJam;

static class StageChecks
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
        Check(Math.Abs(WoolStage.Size(1080f/1920f) - 9.6f) < 1e-4f, "stage size 1080x1920");
        Check(Math.Abs(WoolStage.Size(1080f/2400f) - 12f) < 1e-4f, "stage size 1080x2400");
        Check(Math.Abs(WoolStage.Size(0.75f) - WoolStage.MinSize) < 1e-4f, "stage size tablet uses MinSize");
        Check(Math.Abs(WoolStage.Lift - 0.675f) < 1e-4f, "stage lift");
        Check(Math.Abs(WoolStage.TargetZ - (WoolRouting.TrayZ + 0.675f / (float)Math.Sin(50 * Math.PI / 180))) < 1e-4f, "stage target");
        Check(Math.Abs(WoolStage.MinSize - 8.825f) < 1e-3f, "stage min size");
        Check(WoolStage.ContentTop - WoolStage.Lift + WoolStage.HudBand <= WoolStage.Size(1080f/1920f) + 1e-4f, "content top below HUD at 1080x1920");
        Check(WoolStage.Lift - WoolStage.ContentBottom + WoolStage.BottomBand <= WoolStage.Size(1080f/1920f) + 1e-4f, "content bottom above bottom band at 1080x1920");
    }
}
