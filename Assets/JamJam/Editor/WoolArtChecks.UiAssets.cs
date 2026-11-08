using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiAssets()
        {
            WoolUiStyle.LoadFont(out var font, out var style);
            var lilita = File.Exists(WoolUiStyle.FontPath);
            if (lilita) Expect(AssetDatabase.GetAssetPath(font) == WoolUiStyle.FontPath && style == FontStyle.Normal, "Theme font is Lilita One Regular");
            else Expect(font != null && font.name == "LegacyRuntime" && style == FontStyle.Bold, "Fallback font is LegacyRuntime Bold");
            ExpectUiSprite("UiPill", WoolUiStyle.PillBorder, 64, 64, 0, 0);
            ExpectUiSprite("UiCard", WoolUiStyle.CardSpriteBorder, 64, 64, 0, 0);
            ExpectUiSprite("UiCircle", 0, 64, 64, 1, 1);
            ExpectUiSprite("UiCheck", 0, 52, 40, 64, 120);
            ExpectUiSprite("UiLock", 0, 64, 44, 64, 120);
            ExpectUiSprite("UiBar", 0, 64, 16, 0, 0);
            ExpectContrast(WoolUiTheme.TextLight, WoolUiTheme.TextOutline, "TextLight on TextOutline");
            ExpectContrast(WoolUiTheme.TextDark, WoolUiTheme.Secondary, "TextDark on Secondary");
            ExpectContrast(WoolUiTheme.TextDark, WoolUiTheme.Card, "TextDark on Card");
            ExpectContrast(WoolUiTheme.Primary, WoolUiTheme.TextOutline, "Primary title on TextOutline");
            ExpectContrast(WoolUiTheme.Danger, WoolUiTheme.TextOutline, "Danger title on TextOutline");
            Expect(Mathf.Abs(WoolUiTheme.Contrast(Color.white, Color.black) - 21) < 0.01f, "Contrast formula gives 21 for white on black");
            var root = PrefabUtility.LoadPrefabContents(WoolAssetsBuilder.Root + "/Prefabs/LevelCard.prefab");
            try
            {
                WoolUiStyle.LoadSprite("UiPill", out var pill);
                WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
                var view = root.GetComponent<WoolLevelCard>();
                var host = view.Content.GetComponent<Image>();
                var probe = view.Content.gameObject.AddComponent<Button>();
                WoolUiStyle.Button(probe, WoolUiTone.Primary, pill, 30);
                WoolUiStyle.Button(probe, WoolUiTone.Primary, pill, 30);
                var face = host.transform.Find("Face");
                Expect(face != null && face.GetSiblingIndex() == 0 && host.transform.Find("Face") == face, "Raised button has one Face child drawn first");
                var faceImage = face.GetComponent<Image>();
                Expect(Near(faceImage.color, WoolUiTheme.Primary) && Near(host.color, WoolUiTheme.PrimaryEdge) && host.sprite == pill && host.type == Image.Type.Sliced, "Raised button uses Primary face and PrimaryEdge lip");
                var faceRect = (RectTransform)face;
                Expect(faceRect.offsetMin == new Vector2(0, WoolUiTheme.EdgeDepth) && faceRect.offsetMax == Vector2.zero && !faceImage.raycastTarget, "Face sits 10 px above the lip");
                Expect(probe.transition == Selectable.Transition.None && probe.targetGraphic == host && host.raycastTarget, "Button keeps its raycast host without tint");
                WoolUiStyle.Light(view.Number, 64);
                WoolUiStyle.Light(view.Number, 64);
                var effects = view.Number.GetComponents<Shadow>();
                Expect(view.Number.font == font && view.Number.fontStyle == style && view.Number.fontSize == 64 && view.Number.resizeTextForBestFit, "Light text uses the theme font with best fit");
                Expect(effects.Length == 2 && effects[0] is Outline && !(effects[1] is Outline), "Light text has one Outline then one Shadow after two passes");
                Expect(Near(effects[0].effectColor, WoolUiTheme.TextOutline) && effects[0].effectDistance == new Vector2(3, -3), "Outline is TextOutline 3 px");
                Expect(Mathf.Abs(effects[1].effectColor.a - 0.5f) < 0.01f && effects[1].effectDistance == new Vector2(0, -4), "Shadow is (0,-4) at alpha 0.5");
                WoolUiStyle.Title(view.Number, WoolUiTheme.Primary, 80);
                effects = view.Number.GetComponents<Shadow>();
                Expect(effects.Length == 2 && effects[0] is Outline && !(effects[1] is Outline) && Near(effects[0].effectColor, WoolUiTheme.TextOutline) && effects[0].effectDistance == new Vector2(5, -5), "Title has one navy 5 px outline and no white outline");
                WoolUiStyle.Dark(view.Number, 40);
                Expect(view.Number.GetComponents<Shadow>().Length == 0 && Near(view.Number.color, WoolUiTheme.TextDark), "Dark text drops outline and shadow");
                WoolUiStyle.Card(host, cardSprite);
                Expect(Near(host.color, WoolUiTheme.CardEdge) && Near(face.GetComponent<Image>().color, WoolUiTheme.Card) && faceRect.offsetMin == new Vector2(6, 18) && faceRect.offsetMax == new Vector2(-6, -6), "Card has a 6 px CardEdge border");
                var badge = view.Check.GetComponent<Image>();
                WoolUiStyle.LoadSprite("UiCheck", out var check);
                WoolUiStyle.Badge(badge, WoolUiTheme.Badge, pill, check, 26);
                WoolUiStyle.Badge(badge, WoolUiTheme.Badge, pill, check, 26);
                Expect(badge.transform.Find("Icon") != null && badge.transform.Find("Icon").GetComponent<Image>().sprite == check, "Badge gets one check icon");
                WoolUiStyle.Overlay(badge, out var group);
                Expect(group != null && badge.sprite == null && Mathf.Abs(badge.color.a - 0.72f) < 0.001f, "Overlay is #0A1230 at alpha 0.72 with a CanvasGroup");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "UI ASSETS PASS / " + font.name + " / " + WoolUiStyle.SpriteNames.Length + " sprites";
        }

        static void ExpectUiSprite(string name, float border, int solidX, int solidY, int clearX, int clearY)
        {
            var path = WoolUiStyle.SpriteFolder + "/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Expect(sprite != null, name + " sprite exists");
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Expect(importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled && importer.alphaIsTransparency, name + " imports as a sprite");
            Expect(Mathf.Abs(sprite.border.x - border) < 0.01f && Mathf.Abs(sprite.border.w - border) < 0.01f, name + " border " + border + " but was " + sprite.border);
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(path));
            var solid = image.GetPixel(solidX, solidY).a;
            var clear = image.GetPixel(clearX, clearY).a;
            Object.DestroyImmediate(image);
            Expect(solid > 0.99f, name + " is solid at " + solidX + "," + solidY + " but alpha was " + solid);
            Expect(clear < 0.01f, name + " is clear at " + clearX + "," + clearY + " but alpha was " + clear);
        }

        static void ExpectContrast(Color text, Color back, string name)
        {
            var ratio = WoolUiTheme.Contrast(text, back);
            Expect(ratio >= WoolUiTheme.MinContrast, name + " contrast " + ratio.ToString("F2") + " is at least 4.5");
        }
    }
}
