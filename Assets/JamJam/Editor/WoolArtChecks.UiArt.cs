using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public const int SliceCoverTolerance = 3;
        public const float SliceColorCut = 0.45f;
        public const float SliceDecorTolerance = 0.1f;

        public static string UiArt()
        {
            var used = 0;
            var sliced = 0;
            foreach (var name in WoolUiArt.Names)
            {
                var path = WoolUiArt.PathOf(name);
                if (!File.Exists(path)) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                WoolUiArt.Settings(name, out var border, out var maxSize);
                Expect(WoolUiArt.Matches(importer, border, maxSize), name + " imports as a clamped single sprite without mipmaps at max " + maxSize + " with border " + border);
                Expect(WoolUiArt.TryLoad(name, out var sprite) && sprite.border == border && WoolUiArt.IsArt(sprite, name), name + " loads as a sprite with its border");
                used++;
                if (border == Vector4.zero) continue;
                ExpectSlices(name, border, Array.IndexOf(WoolUiArt.Decorated, name) >= 0);
                sliced++;
            }
            var preview = (TextureImporter)AssetImporter.GetAtPath(WoolUiArt.PathOf(WoolUiArt.PreviewName));
            Expect(preview == null || preview.textureType != TextureImporterType.Sprite, "Preview.png keeps its import settings");
            foreach (var name in WoolUiArt.Sliced)
                if (File.Exists(WoolUiArt.PathOf(name))) Expect(WoolUiArt.TryLoad(name, out var art) && art.border != Vector4.zero, name + " has a 9-slice border");
            var root = PrefabUtility.LoadPrefabContents(WoolUiBuilder.CardPath);
            try
            {
                WoolUiStyle.LoadSprite("UiPill", out var pill);
                var view = root.GetComponent<WoolLevelCard>();
                var badge = view.Current.GetComponent<Image>();
                var probe = view.Current.AddComponent<Button>();
                var rect = (RectTransform)probe.transform;
                var label = rect.Find("Label").GetComponent<Text>();
                WoolUiArt.SimulateMissing = true;
                Expect(!WoolUiArt.TryLoad("Button-Cream", out _), "SimulateMissing hides every art file");
                WoolUiStyle.Button(probe, WoolUiTone.Secondary, pill, 30);
                Expect(!WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry") && rect.Find("Face") != null && rect.Find("Icon") == null && badge.sprite == pill, "Without art the button stays procedural");
                WoolUiArt.SimulateMissing = false;
                if (WoolUiArt.TryLoad("Button-Cream", out var cream) && WoolUiArt.TryLoad("Icon-Retry", out var retry))
                {
                    WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry");
                    WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry");
                    var icon = (RectTransform)rect.Find("Icon");
                    var icons = 0;
                    foreach (Transform child in rect) if (child.name == "Icon") icons++;
                    Expect(badge.sprite == cream && badge.type == Image.Type.Sliced && Near(badge.color, Color.white) && rect.Find("Face") == null, "Art button uses Button-Cream sliced in white without a Face");
                    Expect(Mathf.Abs(badge.pixelsPerUnitMultiplier - cream.rect.height / rect.rect.height) < 1e-4f, "Art button scales its corners to its height");
                    Expect(icons == 1 && icon.GetComponent<Image>().sprite == retry && !icon.GetComponent<Image>().raycastTarget && icon.GetComponent<Image>().preserveAspect, "Art button has one Icon-Retry child");
                    Expect(icon.anchoredPosition.x + icon.sizeDelta.x * 0.5f <= label.rectTransform.anchoredPosition.x - label.rectTransform.sizeDelta.x * 0.5f + 0.01f && label.enabled, "Icon sits left of the label");
                    WoolUiArt.SimulateMissing = true;
                    WoolUiStyle.Button(probe, WoolUiTone.Secondary, pill, 30);
                    WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry");
                    Expect(rect.Find("Icon") == null && rect.Find("Face") != null && label.rectTransform.anchoredPosition == new Vector2(0, WoolUiTheme.EdgeDepth * 0.5f) && label.rectTransform.sizeDelta == rect.sizeDelta - WoolUiArt.LabelPad, "Losing the art restores the procedural button and label");
                    WoolUiArt.SimulateMissing = false;
                }
                if (WoolUiArt.TryLoad("Button-Round-Orange", out var round) && WoolUiArt.TryLoad("Icon-Pause", out var pause))
                {
                    WoolUiArt.Round(probe, "Button-Round-Orange", "Icon-Pause");
                    var icon = (RectTransform)rect.Find("Icon");
                    Expect(badge.sprite == round && badge.type == Image.Type.Simple && badge.preserveAspect && !label.enabled && icon.anchoredPosition.x == 0 && icon.GetComponent<Image>().sprite == pause, "Round art button centres its icon and hides the label");
                }
                if (WoolUiArt.TryLoad("Panel-Card", out var panel))
                {
                    var host = view.Content.GetComponent<Image>();
                    Expect(WoolUiArt.Card(host) && host.sprite == panel && host.transform.Find("Face") == null && Mathf.Abs(host.pixelsPerUnitMultiplier - WoolUiArt.CardPixelsPerUnit) < 1e-4f, "Panel-Card replaces the raised card");
                }
            }
            finally
            {
                WoolUiArt.SimulateMissing = false;
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "UI ART PASS / " + used + " images / " + sliced + " sliced";
        }

        static void ExpectSlices(string name, Vector4 border, bool decorated)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(WoolUiArt.PathOf(name)));
            var width = image.width;
            var height = image.height;
            var pixels = image.GetPixels32();
            UnityEngine.Object.DestroyImmediate(image);
            var cx = width / 2;
            var cy = height / 2;
            var body = pixels[cy * width + cx];
            var cols = new int[width];
            var rows = new int[height];
            var farCols = new int[width];
            var farRows = new int[height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var p = pixels[y * width + x];
                    if (p.a <= 127) continue;
                    cols[x]++;
                    rows[y]++;
                    if ((Mathf.Abs(p.r - body.r) + Mathf.Abs(p.g - body.g) + Mathf.Abs(p.b - body.b)) / 255f <= SliceColorCut) continue;
                    farCols[x]++;
                    farRows[y]++;
                }
            for (var x = (int)border.x; x < width - (int)border.z; x++)
            {
                Expect(Mathf.Abs(cols[x] - cols[cx]) <= SliceCoverTolerance, name + " column " + x + " between the borders matches the centre silhouette");
                Expect(!decorated || farCols[x] / (float)height <= farCols[cx] / (float)height + SliceDecorTolerance, name + " column " + x + " between the borders has no decoration");
            }
            if (border.y + border.w == 0) return;
            for (var y = (int)border.y; y < height - (int)border.w; y++)
            {
                Expect(Mathf.Abs(rows[y] - rows[cy]) <= SliceCoverTolerance, name + " row " + y + " between the borders matches the centre silhouette");
                Expect(!decorated || farRows[y] / (float)width <= farRows[cy] / (float)width + SliceDecorTolerance, name + " row " + y + " between the borders has no decoration");
            }
        }
    }
}
