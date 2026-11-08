using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static class WoolUiArt
    {
        public const string Folder = "Assets/JamJam/Art/UI";
        public const string PreviewName = "Preview";
        public const int BackgroundMaxSize = 2048;
        public const int SpriteMaxSize = 1024;
        public const float CardPixelsPerUnit = 1.6f;
        public const float IconScale = 0.62f;
        public const float RoundIconScale = 0.56f;
        public const float IconGap = 6f;
        public const float LabelTail = 0.3f;
        public static readonly Vector2 LabelPad = new Vector2(25, 10);
        public static bool SimulateMissing;
        public static readonly string[] Names =
        {
            "Badge-Level", "Banner-Ribbon-Green", "Banner-Ribbon-Red", "Button-Cream", "Button-Green", "Button-Orange", "Button-Red",
            "Button-Round-Cream", "Button-Round-Orange", "Icon-Back", "Icon-Check", "Icon-Home", "Icon-Lock", "Icon-Music", "Icon-Pause",
            "Icon-Play", "Icon-Retry", "Icon-Settings", "Icon-Sound", "Icon-Star", "Icon-Vibration", "Icon-YarnBall", "LevelCard-Cleared",
            "LevelCard-Current", "LevelCard-Locked", "Levels-Background", "Loading-Background", "Lobby-Background", "Lobby-Characters", "Logo-WoolJam",
            "Panel-Card", "Progress-Fill", "Progress-Track", "Result-Lose-Kitten", "Result-Rays", "Result-Win-Characters"
        };
        public static readonly string[] Backgrounds = { "Lobby-Background", "Levels-Background", "Loading-Background" };
        public static readonly string[] Decorated = { "Badge-Level", "Panel-Card", "LevelCard-Cleared", "LevelCard-Current" };
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "Badge-Level", new Vector4(96, 72, 64, 96) },
            { "Banner-Ribbon-Green", new Vector4(224, 0, 224, 0) },
            { "Banner-Ribbon-Red", new Vector4(224, 0, 224, 0) },
            { "Button-Cream", new Vector4(88, 88, 88, 88) },
            { "Button-Green", new Vector4(88, 88, 88, 88) },
            { "Button-Orange", new Vector4(88, 88, 88, 88) },
            { "Button-Red", new Vector4(88, 88, 88, 88) },
            { "Panel-Card", new Vector4(280, 160, 280, 248) },
            { "Progress-Track", new Vector4(56, 40, 56, 40) },
            { "Progress-Fill", new Vector4(64, 40, 64, 40) },
            { "LevelCard-Cleared", new Vector4(176, 0, 184, 0) },
            { "LevelCard-Current", new Vector4(192, 0, 104, 0) },
            { "LevelCard-Locked", new Vector4(104, 0, 104, 0) }
        };

        public static IReadOnlyCollection<string> Sliced => Borders.Keys;

        public static string PathOf(string name)
        {
            return Folder + "/" + name + ".png";
        }

        public static void Settings(string name, out Vector4 border, out int maxSize)
        {
            if (!Borders.TryGetValue(name, out border)) border = Vector4.zero;
            maxSize = Array.IndexOf(Backgrounds, name) >= 0 ? BackgroundMaxSize : SpriteMaxSize;
        }

        public static void Import()
        {
            foreach (var name in Names)
            {
                var path = PathOf(name);
                if (!File.Exists(path)) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                Settings(name, out var border, out var maxSize);
                if (Matches(importer, border, maxSize)) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = maxSize;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        public static bool Matches(TextureImporter importer, Vector4 border, int maxSize)
        {
            return importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled
                && importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp && importer.filterMode == FilterMode.Bilinear
                && importer.maxTextureSize == maxSize && importer.spriteBorder == border;
        }

        public static bool TryLoad(string name, out Sprite sprite)
        {
            sprite = null;
            if (SimulateMissing) return false;
            var path = PathOf(name);
            if (!File.Exists(path)) return false;
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null;
        }

        public static bool IsArt(Sprite sprite)
        {
            return sprite != null && AssetDatabase.GetAssetPath(sprite).StartsWith(Folder + "/", StringComparison.Ordinal);
        }

        public static bool IsArt(Sprite sprite, string name)
        {
            return IsArt(sprite) && AssetDatabase.GetAssetPath(sprite) == PathOf(name);
        }

        public static float FitHeight(Image image, Sprite art)
        {
            var height = image.rectTransform.rect.height;
            return height > 0 ? Mathf.Max(0.01f, art.rect.height / height) : 1f;
        }

        public static void Skin(Image image, Sprite art, float multiplier)
        {
            var face = image.transform.Find("Face");
            if (face != null) UnityEngine.Object.DestroyImmediate(face.gameObject);
            var sliced = art.border != Vector4.zero;
            image.sprite = art;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = multiplier;
        }

        public static bool Button(Button button, WoolUiTone tone, string icon)
        {
            var name = tone == WoolUiTone.Primary ? "Button-Green" : tone == WoolUiTone.Danger ? "Button-Red" : tone == WoolUiTone.Secondary ? "Button-Cream" : tone == WoolUiTone.Warm ? "Button-Orange" : null;
            var host = button.GetComponent<Image>();
            Sprite art = null;
            var skinned = name != null && TryLoad(name, out art);
            if (skinned) Skin(host, art, FitHeight(host, art));
            Icon(button, icon, false);
            if (tone == WoolUiTone.Secondary && icon == "Icon-Retry")
            {
                var node = button.transform.Find("Icon");
                if (node != null) node.GetComponent<Image>().color = WoolUiTheme.TextDark;
            }
            return skinned;
        }

        public static bool Round(Button button, string round, string icon)
        {
            var host = button.GetComponent<Image>();
            var skinned = TryLoad(round, out var art);
            if (skinned) Skin(host, art, 1);
            Icon(button, icon, skinned);
            return skinned;
        }

        public static void Icon(Button button, string icon, bool round)
        {
            var rect = (RectTransform)button.transform;
            var labelNode = rect.Find("Label");
            var label = labelNode != null ? labelNode.GetComponent<Text>() : null;
            var node = rect.Find("Icon");
            var size = rect.sizeDelta;
            var lift = WoolUiTheme.EdgeDepth * 0.5f;
            if (icon == null || !TryLoad(icon, out var sprite))
            {
                if (node == null) return;
                UnityEngine.Object.DestroyImmediate(node.gameObject);
                if (label == null) return;
                label.enabled = true;
                label.rectTransform.anchoredPosition = new Vector2(0, lift);
                label.rectTransform.sizeDelta = size - LabelPad;
                return;
            }
            Child(rect, "Icon", out var iconRect, out var image);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            if (round)
            {
                iconRect.anchoredPosition = new Vector2(0, lift);
                iconRect.sizeDelta = Vector2.one * size.y * RoundIconScale;
                if (label != null) label.enabled = false;
                return;
            }
            var side = size.y * IconScale;
            var center = -size.x * 0.5f + size.y * 0.5f;
            iconRect.anchoredPosition = new Vector2(center, lift);
            iconRect.sizeDelta = Vector2.one * side;
            if (label == null) return;
            label.enabled = true;
            var left = center + side * 0.5f + IconGap;
            var right = size.x * 0.5f - size.y * LabelTail;
            label.rectTransform.anchoredPosition = new Vector2((left + right) * 0.5f, lift);
            label.rectTransform.sizeDelta = new Vector2(right - left, size.y - LabelPad.y);
        }

        public static bool Card(Image card)
        {
            if (!TryLoad("Panel-Card", out var art)) return false;
            Skin(card, art, CardPixelsPerUnit);
            return true;
        }

        public static void Child(Transform parent, string name, out RectTransform rect, out Image image)
        {
            var node = parent.Find(name);
            if (node == null)
            {
                rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                rect.SetParent(parent, false);
            }
            else rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
        }

        public static void Picture(Transform parent, string name, string art, Vector2 position, Vector2 size, out RectTransform rect)
        {
            rect = null;
            if (!TryLoad(art, out var sprite))
            {
                Remove(parent, name);
                return;
            }
            Child(parent, name, out rect, out var image);
            Skin(image, sprite, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Background(Transform panel, string art)
        {
            if (!TryLoad(art, out var sprite))
            {
                Remove(panel, "Background");
                return;
            }
            Child(panel, "Background", out var rect, out var image);
            rect.SetSiblingIndex(0);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var fitter = rect.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        public static void Remove(Transform parent, string name)
        {
            var node = parent.Find(name);
            if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject);
        }
    }
}
