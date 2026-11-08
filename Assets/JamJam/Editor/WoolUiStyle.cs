using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public enum WoolUiTone { Primary, Secondary, Warm, Danger }

    public static class WoolUiStyle
    {
        public const string FontPath = "Assets/JamJam/Art/Fonts/LilitaOne-Regular.ttf";
        public const string SpriteFolder = WoolAssetsBuilder.Root + "/Textures/UI";
        public const int SpriteSize = 128;
        public const float PillRadius = 60f;
        public const float PillBorder = 62f;
        public const float CardRadius = 32f;
        public const float CardSpriteBorder = 40f;
        public const int BarWidth = 1040;
        public const int BarHeight = 32;
        public const float BarShade = 0.85f;
        public const int ToggleWidth = 260;
        public const int ToggleHeight = 140;
        public const float ToggleKnobRadius = 54f;
        public static readonly string[] SpriteNames = { "UiPill", "UiCard", "UiCircle", "UiCheck", "UiLock", "UiBar", "UiToggleOn" };

        public static void LoadFont(out Font font, out FontStyle style)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            style = FontStyle.Normal;
            if (font != null) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            style = FontStyle.Bold;
        }

        public static void BuildSprites()
        {
            if (!AssetDatabase.IsValidFolder(SpriteFolder)) AssetDatabase.CreateFolder(WoolAssetsBuilder.Root + "/Textures", "UI");
            WriteSprite("UiPill", SpriteSize, SpriteSize, PillBorder, (x, y) => Solid(RoundedBox(x, y, SpriteSize * 0.5f, SpriteSize * 0.5f, PillRadius)));
            WriteSprite("UiCard", SpriteSize, SpriteSize, CardSpriteBorder, (x, y) => Solid(RoundedBox(x, y, SpriteSize * 0.5f, SpriteSize * 0.5f, CardRadius)));
            WriteSprite("UiCircle", SpriteSize, SpriteSize, 0, (x, y) => Solid(new Vector2(x, y).magnitude - (SpriteSize * 0.5f - 1)));
            WriteSprite("UiCheck", SpriteSize, SpriteSize, 0, (x, y) => Solid(Mathf.Min(Segment(x, y, -36, 2, -12, -24), Segment(x, y, -12, -24, 38, 30)) - 12));
            WriteSprite("UiLock", SpriteSize, SpriteSize, 0, (x, y) => Solid(Lock(x, y)));
            WriteSprite("UiBar", BarWidth, BarHeight, 0, Bar);
            WriteSprite("UiToggleOn", ToggleWidth, ToggleHeight, 0, ToggleOn);
        }

        public static void LoadSprite(string name, out Sprite sprite)
        {
            var path = SpriteFolder + "/" + name + ".png";
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException(path + " is missing; run JamJam/Rebuild UI");
        }

        public static void Light(Text text, int size) { Light(text, size, 1); }

        public static void Light(Text text, int size, float alpha)
        {
            Font(text, size);
            text.color = new Color(1, 1, 1, alpha);
            Effects(text, new[] { WoolUiTheme.TextOutline }, new[] { WoolUiTheme.OutlineSize }, WoolUiTheme.ShadowOffset);
        }

        public static void Logo(Text text, int size)
        {
            Font(text, size);
            text.color = WoolUiTheme.TextLight;
            Effects(text, new[] { WoolUiTheme.TextOutline }, new[] { WoolUiTheme.LogoOutlineSize }, WoolUiTheme.LogoShadowOffset);
        }

        public static void Title(Text text, Color fill, int size)
        {
            Font(text, size);
            text.color = fill;
            Effects(text, new[] { WoolUiTheme.TextOutline }, new[] { WoolUiTheme.TitleOutline }, WoolUiTheme.ShadowOffset);
        }

        public static void Dark(Text text, int size)
        {
            Font(text, size);
            text.color = WoolUiTheme.TextDark;
            Effects(text, Array.Empty<Color>(), Array.Empty<float>(), Vector2.zero);
        }

        public static void Raised(Image host, Sprite sprite, Color face, Color edge, Vector4 inset, out Image faceImage)
        {
            host.sprite = sprite;
            host.type = Image.Type.Sliced;
            host.color = edge;
            var node = host.transform.Find("Face");
            RectTransform rect;
            if (node == null)
            {
                rect = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                rect.SetParent(host.transform, false);
            }
            else rect = (RectTransform)node;
            rect.SetSiblingIndex(0);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset.x, inset.y);
            rect.offsetMax = new Vector2(-inset.z, -inset.w);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            faceImage = rect.GetComponent<Image>();
            faceImage.sprite = sprite;
            faceImage.type = Image.Type.Sliced;
            faceImage.color = face;
            faceImage.raycastTarget = false;
        }

        public static void Button(Button button, WoolUiTone tone, Sprite sprite, int labelSize)
        {
            Tone(tone, out var face, out var edge, out var light);
            var host = button.GetComponent<Image>();
            Raised(host, sprite, face, edge, new Vector4(0, WoolUiTheme.EdgeDepth, 0, 0), out _);
            host.raycastTarget = true;
            button.targetGraphic = host;
            button.transition = Selectable.Transition.None;
            var node = button.transform.Find("Label");
            if (node == null) return;
            var label = node.GetComponent<Text>();
            label.rectTransform.anchoredPosition = new Vector2(0, WoolUiTheme.EdgeDepth * 0.5f);
            if (light) Light(label, labelSize); else Dark(label, labelSize);
        }

        public static void Card(Image card, Sprite sprite)
        {
            var border = WoolUiTheme.CardBorder;
            Raised(card, sprite, WoolUiTheme.Card, WoolUiTheme.CardEdge, new Vector4(border, border + WoolUiTheme.CardDepth, border, border), out _);
        }

        public static void Overlay(Image overlay, out CanvasGroup group)
        {
            overlay.sprite = null;
            overlay.type = Image.Type.Simple;
            overlay.color = WoolUiTheme.Overlay;
            overlay.raycastTarget = true;
            group = overlay.GetComponent<CanvasGroup>();
            if (group == null) group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        public static void RoundPills(Transform root)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || image.sprite.name != "UiPill" || image.type != Image.Type.Sliced) continue;
                var rect = image.rectTransform.rect;
                var side = Mathf.Min(rect.width, rect.height);
                image.pixelsPerUnitMultiplier = side > 0 ? Mathf.Max(1f, PillBorder * 2 / side) : 1f;
            }
        }

        public static void Bar(Image track, Image fill, Sprite pill, Sprite bar)
        {
            track.sprite = pill;
            track.type = Image.Type.Sliced;
            track.color = WoolUiTheme.Track;
            fill.sprite = bar;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.preserveAspect = false;
            fill.color = WoolUiTheme.Fill;
        }

        public static void Toggle(Toggle toggle, Sprite pill, int labelSize)
        {
            var row = toggle.GetComponent<Image>();
            row.sprite = pill;
            row.type = Image.Type.Sliced;
            row.color = WoolUiTheme.Secondary;
            toggle.transition = Selectable.Transition.None;
            var box = (Image)toggle.targetGraphic;
            box.sprite = pill;
            box.type = Image.Type.Sliced;
            box.color = WoolUiTheme.Off;
            var check = (Image)toggle.graphic;
            LoadSprite("UiToggleOn", out var on);
            LoadSprite("UiCircle", out var circle);
            check.sprite = on;
            check.type = Image.Type.Simple;
            check.preserveAspect = false;
            check.color = Color.white;
            check.raycastTarget = false;
            var checkRect = check.rectTransform;
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
            var boxRect = box.rectTransform;
            var knobNode = box.transform.Find("Knob");
            RectTransform knobRect;
            if (knobNode == null)
            {
                knobRect = new GameObject("Knob", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                knobRect.SetParent(box.transform, false);
            }
            else knobRect = (RectTransform)knobNode;
            var knobSize = ToggleKnobRadius * boxRect.sizeDelta.y / ToggleHeight * 2;
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(knobSize, knobSize);
            knobRect.anchoredPosition = new Vector2(-(boxRect.sizeDelta.x - boxRect.sizeDelta.y) * 0.5f, 0);
            var knob = knobRect.GetComponent<Image>();
            knob.sprite = circle;
            knob.type = Image.Type.Simple;
            knob.color = Color.white;
            knob.raycastTarget = false;
            knobRect.SetSiblingIndex(0);
            check.transform.SetSiblingIndex(1);
            var label = toggle.transform.Find("Label");
            if (label != null) Dark(label.GetComponent<Text>(), labelSize);
        }

        public static void Badge(Image badge, Color color, Sprite pill, Sprite icon, int labelSize)
        {
            badge.sprite = pill;
            badge.type = Image.Type.Sliced;
            badge.color = color;
            var label = badge.transform.Find("Label").GetComponent<Text>();
            var node = badge.transform.Find("Icon");
            if (icon == null)
            {
                if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject);
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = new Vector2(170, 60);
            }
            else
            {
                RectTransform rect;
                if (node == null)
                {
                    rect = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    rect.SetParent(badge.transform, false);
                }
                else rect = (RectTransform)node;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-58, 0);
                rect.sizeDelta = new Vector2(44, 44);
                var image = rect.GetComponent<Image>();
                image.sprite = icon;
                image.type = Image.Type.Simple;
                image.color = WoolUiTheme.TextLight;
                image.raycastTarget = false;
                label.rectTransform.anchoredPosition = new Vector2(22, 0);
                label.rectTransform.sizeDelta = new Vector2(118, 60);
            }
            Light(label, labelSize);
        }

        public static void Tone(WoolUiTone tone, out Color face, out Color edge, out bool light)
        {
            switch (tone)
            {
                case WoolUiTone.Primary: face = WoolUiTheme.Primary; edge = WoolUiTheme.PrimaryEdge; light = true; return;
                case WoolUiTone.Warm: face = WoolUiTheme.Warm; edge = WoolUiTheme.WarmEdge; light = true; return;
                case WoolUiTone.Danger: face = WoolUiTheme.Danger; edge = WoolUiTheme.DangerEdge; light = true; return;
                default: face = WoolUiTheme.Secondary; edge = WoolUiTheme.SecondaryEdge; light = false; return;
            }
        }

        static void Font(Text text, int size)
        {
            LoadFont(out var font, out var style);
            text.font = font;
            text.fontStyle = style;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, size / 2);
            text.resizeTextMaxSize = size;
        }

        static void Effects(Text text, Color[] outlines, float[] sizes, Vector2 shadow)
        {
            var wanted = outlines.Length + (shadow != Vector2.zero ? 1 : 0);
            var existing = text.GetComponents<Shadow>();
            var match = existing.Length == wanted;
            for (var i = 0; match && i < existing.Length; i++) match = (i < outlines.Length) == (existing[i] is Outline);
            if (!match)
            {
                foreach (var effect in existing) UnityEngine.Object.DestroyImmediate(effect);
                for (var i = 0; i < outlines.Length; i++) text.gameObject.AddComponent<Outline>();
                if (shadow != Vector2.zero) text.gameObject.AddComponent<Shadow>();
                existing = text.GetComponents<Shadow>();
            }
            for (var i = 0; i < outlines.Length; i++)
            {
                existing[i].effectColor = outlines[i];
                existing[i].effectDistance = new Vector2(sizes[i], -sizes[i]);
                existing[i].useGraphicAlpha = true;
            }
            if (shadow == Vector2.zero) return;
            var last = existing[outlines.Length];
            var tint = WoolUiTheme.TextOutline;
            last.effectColor = new Color(tint.r, tint.g, tint.b, WoolUiTheme.ShadowAlpha);
            last.effectDistance = shadow;
            last.useGraphicAlpha = true;
        }

        static void WriteSprite(string name, int width, int height, float border, Func<float, float, Color> pixel)
        {
            var path = SpriteFolder + "/" + name + ".png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x + 0.5f - width * 0.5f, y + 0.5f - height * 0.5f);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        static Color Solid(float distance)
        {
            return new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance));
        }

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            var qx = Mathf.Abs(x) - (halfWidth - radius);
            var qy = Mathf.Abs(y) - (halfHeight - radius);
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
        }

        static float Segment(float x, float y, float ax, float ay, float bx, float by)
        {
            var p = new Vector2(x - ax, y - ay);
            var ab = new Vector2(bx - ax, by - ay);
            return (p - ab * Mathf.Clamp01(Vector2.Dot(p, ab) / ab.sqrMagnitude)).magnitude;
        }

        static float Lock(float x, float y)
        {
            var body = RoundedBox(x, y + 20, 40, 32, 10);
            var arc = y > 14 ? Mathf.Abs(new Vector2(x, y - 14).magnitude - 26) : Mathf.Min(Segment(x, y, -26, 14, -26, -4), Segment(x, y, 26, 14, 26, -4));
            return Mathf.Min(body, arc - 8);
        }

        static Color ToggleOn(float x, float y)
        {
            var alpha = Mathf.Clamp01(0.5f - RoundedBox(x, y, ToggleWidth * 0.5f, ToggleHeight * 0.5f, ToggleHeight * 0.5f));
            var knob = Mathf.Clamp01(0.5f - (new Vector2(x - (ToggleWidth * 0.5f - ToggleHeight * 0.5f), y).magnitude - ToggleKnobRadius));
            var green = WoolUiTheme.Primary;
            return new Color(Mathf.Lerp(green.r, 1, knob), Mathf.Lerp(green.g, 1, knob), Mathf.Lerp(green.b, 1, knob), alpha);
        }

        static Color Bar(float x, float y)
        {
            var alpha = Mathf.Clamp01(0.5f - RoundedBox(x, y, BarWidth * 0.5f, BarHeight * 0.5f, BarHeight * 0.5f));
            var shade = y > 2 && y < 10 ? 1f : BarShade;
            return new Color(shade, shade, shade, alpha);
        }
    }
}
