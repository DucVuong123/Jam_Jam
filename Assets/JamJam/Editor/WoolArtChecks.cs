using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        static readonly string[] Palette = { "#E8352F", "#FFCE1F", "#3CC03A", "#22CFE0", "#2F74EA", "#F04FAE" };

        public static string Textures()
        {
            LoadArt(out var art);
            for (var i = 0; i < 6; i++)
            {
                ExpectColor(art.Yarn[i], Palette[i], "Yarn_" + i);
                ExpectColor(art.Thread[i], Palette[i], "Thread_" + i);
            }
            ExpectSize("Knit", 256, 256, false);
            ExpectSize("KnitNormal", 256, 256, true);
            ExpectSize("Fabric", 256, 256, false);
            ExpectSize("FeltPath", 64, 256, false);
            ExpectSize("Thread", 64, 32, false);
            TextureAt("Knit", out var knit);
            TextureAt("KnitNormal", out var knitNormal);
            TextureAt("Thread", out var thread);
            TextureAt("FeltPath", out var felt);
            TextureAt("Fabric", out var fabric);
            for (var i = 0; i < 6; i++)
            {
                Expect(art.Yarn[i].GetTexture("_BaseMap") == knit && art.Yarn[i].GetTexture("_BumpMap") == knitNormal, "Yarn_" + i + " knit maps");
                Expect(art.Thread[i].GetTexture("_BaseMap") == thread, "Thread_" + i + " twisted texture");
            }
            ExpectColor(art.Road, "#E9C99A", "Road");
            Expect(art.Road.GetTexture("_BaseMap") == felt, "Road felt texture");
            ExpectFelt();
            ExpectColor(art.Pad, "#F3E3C6", "Pad");
            Expect(art.PadLocked != null, "PadLocked material");
            MaterialAt("Ground", out var ground);
            ExpectColor(ground, "#F6E7CF", "Ground");
            Expect(ground.GetTexture("_BaseMap") == fabric, "Ground fabric texture");
            MaterialAt("Ivory", out var ivory);
            ExpectColor(ivory, "#FFF8EC", "Ivory");
            MaterialAt("Cushion", out _);
            MaterialAt("Wood", out _);
            return "TEXTURES PASS / 6 palette colors / 5 textures";
        }

        static void ExpectFelt()
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(WoolAssetsBuilder.Root + "/Textures/FeltPath.png"));
            var felt = new Color32(0xC9, 0xCF, 0xDA, 0xFF);
            var center = image.GetPixel(32, 128);
            var dash = image.GetPixel(10, 13);
            var gap = image.GetPixel(10, 102);
            UnityEngine.Object.DestroyImmediate(image);
            Expect(Mathf.Abs(center.r - felt.r / 255f) < 0.06f && Mathf.Abs(center.g - felt.g / 255f) < 0.06f && Mathf.Abs(center.b - felt.b / 255f) < 0.06f, "FeltPath center is felt #C9CFDA but was #" + ColorUtility.ToHtmlStringRGB(center));
            Expect(dash.r > 0.9f && dash.g > 0.9f && dash.b > 0.9f, "FeltPath stitch on dash row is white but was #" + ColorUtility.ToHtmlStringRGB(dash));
            Expect(!(gap.r > 0.9f && gap.g > 0.9f && gap.b > 0.9f), "FeltPath stitch on gap row is not white but was #" + ColorUtility.ToHtmlStringRGB(gap));
        }

        static void Expect(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("ART CHECK FAILED: " + name);
        }

        static bool Near(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
        }

        static void ExpectColor(Material material, string hex, string name)
        {
            Expect(material != null, name + " material exists");
            ColorUtility.TryParseHtmlString(hex, out var expected);
            var actual = material.GetColor("_BaseColor");
            Expect(Near(actual, expected), name + " color " + hex + " but was #" + ColorUtility.ToHtmlStringRGB(actual));
        }

        static void ExpectSize(string name, int width, int height, bool normalMap)
        {
            TextureAt(name, out var texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Expect(texture.width == width && texture.height == height, name + " size " + width + "x" + height + " but was " + texture.width + "x" + texture.height);
            Expect(importer.wrapMode == TextureWrapMode.Repeat && (importer.textureType == TextureImporterType.NormalMap) == normalMap, name + " import settings");
        }

        static void ExpectBounds(Mesh mesh, Vector3 size, string name)
        {
            var actual = mesh.bounds.size;
            Expect(Mathf.Abs(actual.x - size.x) < 0.01f && Mathf.Abs(actual.y - size.y) < 0.01f && Mathf.Abs(actual.z - size.z) < 0.01f, name + " bounds " + size + " but was " + actual);
        }

        static void ExpectOutward(Mesh mesh, string name)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var face = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
                if (face.sqrMagnitude < 1e-10f) continue;
                var normal = normals[triangles[i]] + normals[triangles[i + 1]] + normals[triangles[i + 2]];
                Expect(Vector3.Dot(face, normal) > 0, name + " triangle " + i / 3 + " faces inward");
            }
        }

        static void LoadArt(out WoolArt art)
        {
            Asset(WoolAssetsBuilder.Root + "/WoolArt.asset", out art);
        }

        static void TextureAt(string name, out Texture2D texture)
        {
            Asset(WoolAssetsBuilder.Root + "/Textures/" + name + ".png", out texture);
        }

        static void MaterialAt(string name, out Material material)
        {
            Asset(WoolAssetsBuilder.Root + "/Materials/" + name + ".mat", out material);
        }

        static void Asset<T>(string path, out T asset) where T : UnityEngine.Object
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("ART CHECK FAILED: missing asset " + path);
        }
    }
}
