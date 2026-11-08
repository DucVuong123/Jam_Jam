using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Tray()
        {
            LoadArt(out var art);
            var spool = art.Spool.transform;
            var core = spool.Find("Core");
            var flange = spool.Find("Flange");
            Expect(core != null && flange != null && spool.Find("YarnWrap") != null, "Spool has Core, Flange and YarnWrap");
            MaterialAt("Wood", out var wood);
            MaterialAt("Ivory", out var ivory);
            var flanges = 0;
            foreach (Transform child in spool) if (child.name == "Flange" && child.GetComponent<MeshRenderer>().sharedMaterial == ivory) flanges++;
            Expect(flanges == 2, "Spool has two cream flanges");
            Expect(core.GetComponent<MeshRenderer>().sharedMaterial == wood && flange.GetComponent<MeshRenderer>().sharedMaterial == ivory, "Spool wood core and cream ends");
            var counter = art.Spool.GetComponentInChildren<TextMesh>();
            Expect(counter != null && Near(counter.color, Color.white), "Spool counter is white");
            Expect(Mathf.Abs(counter.transform.localEulerAngles.x - WoolStage.Pitch) < 0.5f, "Counter tilted to the " + WoolStage.Pitch + " degree camera");
            ExpectColor(art.Slot.GetComponent<MeshRenderer>().sharedMaterial, "#F3E3C6", "Slot pad");
            var slotLabel = art.Slot.GetComponent<WoolSlotView>().Label;
            Expect(slotLabel.text == "+" && slotLabel.color.a <= 0.4f, "Locked slot shows a faint plus");
            var bubble = art.Kitten.transform.Find("Bubble");
            Expect(bubble != null, "Kitten bubble exists");
            var shader = bubble.GetComponent<MeshRenderer>().sharedMaterial.shader;
            Expect(shader.name == "JamJam/WoolBubble" && !ShaderUtil.ShaderHasError(shader), "Bubble shader compiles");
            var clone = (GameObject)PrefabUtility.InstantiatePrefab(art.Kitten); var sprite = clone.transform.Find("Body").GetComponent<SpriteRenderer>().bounds;
            var bubbleBounds = clone.transform.Find("Bubble").GetComponent<MeshRenderer>().bounds; Object.DestroyImmediate(clone);
            Expect(bubbleBounds.min.x <= sprite.min.x && bubbleBounds.max.x >= sprite.max.x && bubbleBounds.min.y <= sprite.min.y && bubbleBounds.max.y >= sprite.max.y, "Bubble contains the kitten sprite bounds");
            Expect(art.Kitten.transform.Find("Body").GetComponent<SpriteRenderer>() != null, "Kitten sprite kept");
            return "TRAY PASS";
        }
    }
}
