using System.IO;
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Feel()
        {
            LoadArt(out var art);
            ExpectParticles(art.Puff, "FeelPuff", "SoftDot", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ExpectParticles(art.Sparkle, "FeelSparkle", "Star", (int)UnityEngine.Rendering.BlendMode.One);
            ExpectAlpha("SoftDot", 32, 32, 0.95f, 1.01f);
            ExpectAlpha("SoftDot", 0, 0, -0.01f, 0.01f);
            ExpectAlpha("Star", 32, 32, 0.95f, 1.01f);
            ExpectAlpha("Star", 48, 32, 0.3f, 1.01f);
            ExpectAlpha("Star", 48, 48, -0.01f, 0.05f);
            WoolHaptics.Create(false, out var off);
            WoolHaptics.Create(true, out var on);
            Expect(off is WoolNullHaptics && on is WoolNullHaptics, "Editor and disabled haptics use the null implementation");
            on.Play(new long[] { 12 }, new[] { 90 });
            on.Play(null, null);
            return "FEEL PASS / 2 particle prefabs / null haptics";
        }

        static void ExpectParticles(GameObject prefab, string name, string texture, int dstBlend)
        {
            Expect(prefab != null && prefab.name == name, name + " prefab assigned to WoolArt");
            var system = prefab.GetComponent<ParticleSystem>();
            Expect(system != null, name + " has a ParticleSystem");
            var main = system.main;
            Expect(!main.playOnAwake && main.maxParticles == WoolAssetsBuilder.FeelMaxParticles && main.simulationSpace == ParticleSystemSimulationSpace.World, name + " main module");
            Expect(!system.emission.enabled && !system.shape.enabled, name + " emits only from code");
            var particleRenderer = prefab.GetComponent<ParticleSystemRenderer>();
            var material = particleRenderer.sharedMaterial;
            Expect(material != null && material.shader.name == WoolAssetsBuilder.ParticleShader && !ShaderUtil.ShaderHasError(material.shader), name + " particle shader compiles");
            Expect(!main.loop, name + " does not loop");
            Expect(particleRenderer.sortingOrder == 50, name + " renderer sortingOrder is 50");
            Expect(Mathf.RoundToInt(material.GetFloat("_Surface")) == 1 && Mathf.RoundToInt(material.GetFloat("_ZWrite")) == 0 && material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), name + " material is transparent without depth write");
            TextureAt(texture, out var image);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image));
            Expect(image.width == WoolAssetsBuilder.ParticleTextureSize && image.height == WoolAssetsBuilder.ParticleTextureSize && importer.wrapMode == TextureWrapMode.Clamp && importer.alphaIsTransparency, texture + " is a 64x64 clamped alpha texture");
            Expect(material.GetTexture("_BaseMap") == image && material.renderQueue == (int)UnityEngine.Rendering.RenderQueue.Transparent && Mathf.RoundToInt(material.GetFloat("_DstBlend")) == dstBlend, name + " transparent blend " + dstBlend);
        }

        static void ExpectAlpha(string texture, int x, int y, float min, float max)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(WoolAssetsBuilder.Root + "/Textures/" + texture + ".png"));
            var alpha = image.GetPixel(x, y).a;
            Object.DestroyImmediate(image);
            Expect(alpha >= min && alpha <= max, texture + " alpha at " + x + "," + y + " in " + min + ".." + max + " but was " + alpha);
        }
    }
}
