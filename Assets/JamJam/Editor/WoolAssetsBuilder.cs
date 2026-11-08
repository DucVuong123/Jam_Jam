using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static class WoolAssetsBuilder
    {
        public const string Root = "Assets/JamJam/Generated";
        static Mesh sphere;
        static Mesh cylinder;
        static Mesh rounded;
        static Mesh leaf;
        static Mesh arrow;
        static Mesh coil;
        public static readonly string[] Palette = { "#E8352F", "#FFCE1F", "#3CC03A", "#22CFE0", "#2F74EA", "#F04FAE" };
        static Material cream;
        static Material charcoal;
        static Material white;
        static Material orange;
        static Material peach;
        static Material gold;
        static Material wood;
        static Texture2D knit;
        static Texture2D normal;
        static Texture2D fabric;
        static Texture2D felt;
        static Texture2D thread;
        static Sprite kittenSprite;
        static Sprite dragonSprite;
        static Mesh arrowPlate;
        public const float BlockSideShade = 0.7f;
        public const float BraidDensity = 7f;
        public const float BraidDark = 0.7f;
        public const float LightWrap = 0.45f;
        const float ArrowRimLift = 0.012f;
        const float ArrowFaceLift = 0.03f;
        static Material spriteMaterial;

        public static void Build(out WoolArt art)
        {
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Textures");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            BuildTextures();
            NativeMesh(PrimitiveType.Sphere, "Sphere", out sphere);
            NativeMesh(PrimitiveType.Cylinder, "Cylinder", out cylinder);
            BuildRounded(); BuildLeaf(); BuildArrow(); BuildCoil();
            art = AssetDatabase.LoadAssetAtPath<WoolArt>(Root + "/WoolArt.asset");
            if (art == null) { art = ScriptableObject.CreateInstance<WoolArt>(); AssetDatabase.CreateAsset(art, Root + "/WoolArt.asset"); }
            art.Yarn = new Material[6]; art.Thread = new Material[6];
            for (var i = 0; i < 6; i++)
            {
                Mat("Yarn_" + i, Palette[i], knit, normal, YarnTiling, 0.12f, out art.Yarn[i]);
                Mat("Thread_" + i, Palette[i], thread, null, ThreadTiling, 0.1f, out art.Thread[i]);
            }
            Mat("Ivory", "#FFF8EC", null, null, Vector2.one, 0.28f, out cream);
            Mat("Ink", "#283A4A", null, null, Vector2.one, 0.28f, out charcoal);
            Mat("White", "#FFFFFF", null, null, Vector2.one, 0.28f, out white);
            Mat("DragonOrange", "#EE9B3E", knit, normal, YarnTiling, 0.12f, out orange);
            Mat("KittenPeach", "#EABF77", knit, normal, YarnTiling, 0.12f, out peach);
            Mat("Honey", "#F9C64D", knit, normal, YarnTiling, 0.12f, out gold);
            Mat("Road", "#E9C99A", felt, null, RoadTiling, 0.08f, out art.Road);
            Mat("RoadRim", "#B98755", null, null, Vector2.one, 0.1f, out art.Rim);
            Mat("Pad", "#F3E3C6", null, null, Vector2.one, 0.2f, out art.Pad);
            Mat("PadLocked", "#CDBFA8", null, null, Vector2.one, 0.2f, out art.PadLocked);
            Mat("Ground", "#F6E7CF", fabric, null, GroundTiling, 0.05f, out _);
            Mat("Cushion", "#EBDDC4", fabric, null, CushionTiling, 0.05f, out _);
            Mat("Meadow", "#BFE8C8", fabric, null, MeadowTiling, 0.05f, out _);
            Mat("Shelf", "#E2B577", null, null, Vector2.one, 0.25f, out _);
            Mat("Wood", "#E3B98A", null, null, Vector2.one, 0.3f, out wood);
            BuildSpriteAssets();
            MakeBlock(out art.Block);
            MakeSpool(out art.Spool);
            MakeSegment(out art.Segment);
            MakeDragon(out art.Dragon);
            MakeKitten(out art.Kitten);
            MakeSlot(art.Pad, out art.Slot);
            var star = new GameObject("Confetti");
            Part(star.transform, "Paper", rounded, Vector3.zero, new Vector3(1, 0.12f, 0.65f), white, out _);
            SavePrefab(star, "Confetti", out art.Confetti);
            MakeFeel(out art.Puff, out art.Sparkle);
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        static void Mat(string name, string hex, Texture2D albedo, Texture2D bump, Vector2 tiling, float smoothness, out Material material)
        {
            var path = Root + "/Materials/" + name + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BaseMap", albedo);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetTexture("_BumpMap", bump);
            material.SetTextureScale("_BumpMap", tiling);
            if (bump != null) { material.SetFloat("_BumpScale", 0.6f); material.EnableKeyword("_NORMALMAP"); }
            else material.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
        }

        public const int KnitColumns = 6;
        public const int KnitRows = 8;
        public const float KnitFloor = 0.8f;
        public const int FabricColumns = 12;
        public const int FabricRows = 16;
        public const float FeltStitchInset = 0.16f;
        public const int FeltDashes = 4;
        public const int ThreadPly = 2;
        public static readonly Vector2 YarnTiling = new Vector2(2, 2);
        public static readonly Vector2 ThreadTiling = new Vector2(6, 1);
        public static readonly Vector2 RoadTiling = new Vector2(1, 0.5f);
        public static readonly Vector2 GroundTiling = new Vector2(18, 20);
        public static readonly Vector2 CushionTiling = new Vector2(5, 4);
        public static readonly Vector2 MeadowTiling = new Vector2(18, 10);
        static readonly Color FeltColor = new Color32(0xC9, 0xCF, 0xDA, 0xFF);

        static float Stitch(float x, float y, int columns, int rows)
        {
            y *= rows; x *= columns;
            x -= Mathf.Floor(x); y -= Mathf.Floor(y);
            var p = new Vector2(x, y);
            var a = new Vector2(0.5f, 0.12f);
            var left = SegmentDistance(p, a, new Vector2(0.08f, 0.9f));
            var right = SegmentDistance(p, a, new Vector2(0.92f, 0.9f));
            var d = Mathf.Min(left, right);
            var height = Mathf.Exp(-d * d * 100);
            return height * (0.9f + 0.1f * Mathf.Sin((y * 34 + x * 14) * Mathf.PI));
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            return Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude));
        }

        static float Smooth(float edge0, float edge1, float x)
        {
            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3 - 2 * t);
        }

        static float Noise(float u, float v)
        {
            return Mathf.Repeat(Mathf.Sin(u * 1291.7f + v * 7919.3f) * 43758.547f, 1);
        }

        static Color KnitAlbedo(float u, float v)
        {
            var value = KnitFloor + (1 - KnitFloor) * Stitch(u, v, KnitColumns, KnitRows) + (Noise(u, v) - 0.5f) * 0.03f;
            return new Color(value, value, value, 1);
        }

        static Color KnitNormal(float u, float v)
        {
            const float step = 1f / 256;
            var dx = (Stitch(u + step, v, KnitColumns, KnitRows) - Stitch(u - step, v, KnitColumns, KnitRows)) * 2.4f;
            var dy = (Stitch(u, v + step, KnitColumns, KnitRows) - Stitch(u, v - step, KnitColumns, KnitRows)) * 2.4f;
            var n = new Vector3(-dx, -dy, 1).normalized;
            return new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1);
        }

        static Color Fabric(float u, float v)
        {
            var value = 0.82f + 0.18f * Stitch(u, v, FabricColumns, FabricRows) + (Noise(u, v) - 0.5f) * 0.04f;
            return new Color(value, value, value, 1);
        }

        static Color Felt(float u, float v)
        {
            var edge = Mathf.Min(u, 1 - u);
            var shade = Mathf.Lerp(0.72f, 1, Smooth(0, 0.14f, edge)) + (Noise(u, v) - 0.5f) * 0.05f;
            var line = Mathf.Min(Mathf.Abs(u - FeltStitchInset), Mathf.Abs(u - (1 - FeltStitchInset)));
            var dash = Mathf.Repeat(v * FeltDashes, 1) < 0.55f ? 1f : 0f;
            var stitch = (1 - Smooth(0.012f, 0.026f, line)) * dash;
            var color = Color.Lerp(FeltColor * shade, Color.white, stitch);
            color.a = 1;
            return color;
        }

        static Color Twist(float u, float v)
        {
            var ply = 0.5f + 0.5f * Mathf.Sin((u * ThreadPly + v) * Mathf.PI * 2);
            var value = (0.72f + 0.28f * ply) * (0.82f + 0.18f * Mathf.Sin(v * Mathf.PI));
            return new Color(value, value, value, 1);
        }

        static void BuildTextures()
        {
            WriteTexture("Knit", 256, 256, KnitAlbedo, false, out knit);
            WriteTexture("KnitNormal", 256, 256, KnitNormal, true, out normal);
            WriteTexture("Fabric", 256, 256, Fabric, false, out fabric);
            WriteTexture("FeltPath", 64, 256, Felt, false, out felt);
            WriteTexture("Thread", 64, 32, Twist, false, out thread);
        }

        static void WriteTexture(string name, int width, int height, Func<float, float, Color> pixel, bool normalMap, out Texture2D texture)
        {
            var path = Root + "/Textures/" + name + ".png";
            var source = new Texture2D(width, height, TextureFormat.RGBA32, false, normalMap);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x / (float)width, y / (float)height);
            source.SetPixels(pixels); source.Apply();
            File.WriteAllBytes(path, source.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true; importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void NativeMesh(PrimitiveType type, string name, out Mesh mesh)
        {
            var go = GameObject.CreatePrimitive(type);
            var copy = UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.DestroyImmediate(go);
            SaveMesh(copy, name, out mesh);
        }

        static void SaveMesh(Mesh source, string name, out Mesh mesh)
        {
            source.name = name;
            var path = Root + "/Meshes/" + name + ".asset";
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = source; AssetDatabase.CreateAsset(mesh, path); }
            else { EditorUtility.CopySerialized(source, mesh); UnityEngine.Object.DestroyImmediate(source); }
        }

        static void BuildRounded()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var normals = new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            const int steps = 8;
            foreach (var n in normals)
            {
                var axis = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                var other = Vector3.Cross(n, axis);
                var start = vertices.Count;
                for (var y = 0; y <= steps; y++)
                    for (var x = 0; x <= steps; x++)
                    {
                        var p = n * 0.5f + axis * (x / (float)steps - 0.5f) + other * (y / (float)steps - 0.5f);
                        var inner = new Vector3(Mathf.Clamp(p.x, -0.36f, 0.36f), Mathf.Clamp(p.y, -0.36f, 0.36f), Mathf.Clamp(p.z, -0.36f, 0.36f));
                        vertices.Add(inner + (p - inner).normalized * 0.14f);
                        uv.Add(new Vector2(x / (float)steps, y / (float)steps));
                        if (x == steps || y == steps) continue;
                        var i = start + y * (steps + 1) + x;
                        triangles.AddRange(new[] { i, i + 1, i + steps + 1, i + 1, i + steps + 2, i + steps + 1 });
                    }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            SaveMesh(mesh, "Rounded", out rounded);
        }

        static void BuildLeaf()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            const int rings = 12; const int sides = 12;
            for (var i = 0; i <= rings; i++)
            {
                var t = i / (float)rings;
                var width = Mathf.Pow(Mathf.Max(0.001f, Mathf.Sin(t * Mathf.PI)), 0.7f) * 0.5f;
                for (var j = 0; j <= sides; j++)
                {
                    var angle = j / (float)sides * Mathf.PI * 2;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * width, Mathf.Sin(angle) * width * 0.32f + Mathf.Sin(t * Mathf.PI) * 0.08f, t - 0.5f));
                    uv.Add(new Vector2(j / (float)sides, t));
                    if (i == rings || j == sides) continue;
                    var k = i * (sides + 1) + j;
                    triangles.AddRange(new[] { k, k + 1, k + sides + 1, k + 1, k + sides + 2, k + sides + 1 });
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            SaveMesh(mesh, "KnittedScale", out leaf);
        }

        static void BuildArrow()
        {
            var outline = new[] { new Vector2(-0.18f,-0.5f),new Vector2(0.18f,-0.5f),new Vector2(0.18f,0.1f),new Vector2(0.5f,0.1f),new Vector2(0,0.5f),new Vector2(-0.5f,0.1f),new Vector2(-0.18f,0.1f) };
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (var layer = 0; layer < 2; layer++)
                foreach (var p in outline) vertices.Add(new Vector3(p.x, layer == 0 ? -0.5f : 0.5f, p.y));
            var top = new[] { 0,2,1,0,6,2,6,5,2,5,4,3,5,3,2 };
            foreach (var i in top) triangles.Add(i + 7);
            for(var i=0;i<top.Length;i+=3)
            {
                triangles.Add(top[i]); triangles.Add(top[i+2]); triangles.Add(top[i+1]);
            }
            for (var i = 0; i < 7; i++)
            {
                var next = (i + 1) % 7;
                triangles.AddRange(new[] { i, next, i + 7, next, next + 7, i + 7 });
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            SaveMesh(mesh, "Arrow", out arrow);
        }

        static void BuildCoil()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            const int rings = 240; const int sides = 8;
            for (var i = 0; i <= rings; i++)
            {
                var t = i / (float)rings; var angle = t * Mathf.PI * 2 * SpoolTurns;
                for (var j = 0; j <= sides; j++)
                {
                    var a = j / (float)sides * Mathf.PI * 2;
                    var radius = SpoolCoilRadius + Mathf.Cos(a) * SpoolTube;
                    vertices.Add(new Vector3((t - 0.5f) * SpoolCoilLength + Mathf.Sin(a) * SpoolTube, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
                    uv.Add(new Vector2(t * 5, j / (float)sides));
                    if (i == rings || j == sides) continue;
                    var k = i * (sides + 1) + j;
                    triangles.AddRange(new[] { k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2 });
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            SaveMesh(mesh, "WoundThread", out coil);
        }

        public static void Part(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Material material, out Transform part)
        {
            var go = new GameObject(name); part = go.transform; part.SetParent(parent, false); part.localPosition = position; part.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void SavePrefab(GameObject go, string name, out GameObject prefab)
        {
            prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        static void ShaderMat(string name, string shaderName, out Material material)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException(shaderName + " shader is missing");
            var path = Root + "/Materials/" + name + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        public const int FeelMaxParticles = 256;
        public const int ParticleTextureSize = 64;

        static void MakeFeel(out GameObject puff, out GameObject sparkle)
        {
            ParticleTexture("SoftDot", SoftDot, out var dot);
            ParticleTexture("Star", StarShape, out var starTexture);
            ParticleMat("FeelPuff", dot, false, out var puffMaterial);
            ParticleMat("FeelSparkle", starTexture, true, out var sparkleMaterial);
            MakeParticles("FeelPuff", puffMaterial, 0.5f, 0.6f, 0f, 2.5f, out puff);
            MakeParticles("FeelSparkle", sparkleMaterial, 0.7f, 0.5f, 0.5f, 1.2f, out sparkle);
        }

        static Color SoftDot(float u, float v)
        {
            var d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2;
            return new Color(1, 1, 1, 1 - Smooth(0.15f, 1, d));
        }

        static Color StarShape(float u, float v)
        {
            var x = Mathf.Abs(u - 0.5f) * 2;
            var y = Mathf.Abs(v - 0.5f) * 2;
            var arms = Mathf.Max((1 - y) * Mathf.Exp(-x * x * 90), (1 - x) * Mathf.Exp(-y * y * 90));
            var core = 1 - Smooth(0, 0.35f, Mathf.Sqrt(x * x + y * y));
            return new Color(1, 1, 1, Mathf.Clamp01(Mathf.Max(arms, core)));
        }

        static void ParticleTexture(string name, Func<float, float, Color> pixel, out Texture2D texture)
        {
            WriteTexture(name, ParticleTextureSize, ParticleTextureSize, pixel, false, out texture);
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void ParticleMat(string name, Texture2D texture, bool additive, out Material material)
        {
            ShaderMat(name, ParticleShader, out material);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", additive ? 2 : 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
        }

        static void MakeParticles(string name, Material material, float life, float size, float gravity, float drag, out GameObject prefab)
        {
            var go = new GameObject(name);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1;
            main.maxParticles = FeelMaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = life;
            main.startSize = size;
            main.startSpeed = 0;
            main.gravityModifier = gravity;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100;
            limit.drag = drag;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
            var shrink = system.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 50;
            SavePrefab(go, name, out prefab);
        }

        static void BuildArrowPlate()
        {
            var face = new[] { new Vector2(-0.22f,-0.45f),new Vector2(0.22f,-0.45f),new Vector2(0.22f,0.09f),new Vector2(0.45f,0.09f),new Vector2(0,0.45f),new Vector2(-0.45f,0.09f),new Vector2(-0.22f,0.09f) };
            var rim = new[] { new Vector2(-0.28f,-0.51f),new Vector2(0.28f,-0.51f),new Vector2(0.28f,0.03f),new Vector2(0.62f,0.03f),new Vector2(0,0.53f),new Vector2(-0.62f,0.03f),new Vector2(-0.28f,0.03f) };
            var vertices = new List<Vector3>();
            foreach (var p in rim) vertices.Add(new Vector3(p.x, p.y, -ArrowRimLift));
            foreach (var p in face) vertices.Add(new Vector3(p.x, p.y, -ArrowFaceLift));
            var fill = new[] { 0,6,2,0,2,1,5,4,3 };
            var outline = new List<int>(); var top = new List<int>();
            foreach (var i in fill) { outline.Add(i); top.Add(i + 7); }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.subMeshCount = 2; mesh.SetTriangles(outline, 0); mesh.SetTriangles(top, 1); mesh.RecalculateNormals();
            SaveMesh(mesh, "ArrowPlate", out arrowPlate);
        }

        static void MakeBlock(out GameObject prefab)
        {
            ShaderMat("WoolBlock", "JamJam/WoolKnit", out var body);
            body.SetTexture("_BaseMap", knit); body.SetTexture("_BumpMap", normal); body.SetTextureScale("_BaseMap", Vector2.one);
            body.SetColor("_BaseColor", Color.white); body.SetFloat("_BumpScale", 0.6f);
            body.SetFloat("_SideShade", BlockSideShade); body.SetFloat("_BraidDensity", BraidDensity); body.SetFloat("_BraidDark", BraidDark); body.SetFloat("_Wrap", LightWrap);
            body.enableInstancing = true;
            BuildArrowPlate();
            WoolBlockMesh.Build(2, 1, out var preview);
            SaveMesh(preview, "KnitBlock", out var bodyMesh);
            var go = new GameObject("ArrowBlock");
            var view = go.AddComponent<WoolBlockView>();
            Part(go.transform, "Body", bodyMesh, Vector3.zero, Vector3.one, body, out view.Body);
            Part(go.transform, "Arrow", arrowPlate, new Vector3(0, WoolBlockMesh.Thickness + 0.005f, 0), new Vector3(0.56f * WoolRouting.CellSize, 1.56f * WoolRouting.CellSize, 1), charcoal, out view.Arrow);
            view.Arrow.localRotation = Quaternion.Euler(90, 0, 0);
            var arrowRenderer = view.Arrow.GetComponent<MeshRenderer>();
            arrowRenderer.sharedMaterials = new[] { charcoal, cream };
            arrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<BoxCollider>();
            SavePrefab(go, "ArrowBlock", out prefab);
        }

        public const float SpoolFlange = 0.9f;
        public const float SpoolFlangeThickness = 0.07f;
        public const float SpoolCoilRadius = 0.3f;
        public const float SpoolTube = 0.1f;
        public const float SpoolCoilLength = 0.7f;
        public const int SpoolTurns = 12;
        public const float BubbleScale = 2.14f;
        public const float BubbleOffsetY = 0.87f;

        static void MakeSpool(out GameObject prefab)
        {
            var go = new GameObject("Spool");
            Part(go.transform, "Core", cylinder, Vector3.zero, new Vector3(0.4f,0.4f,0.4f), wood, out var core); core.localRotation = Quaternion.Euler(0,0,90);
            foreach (var side in new[] { -1f, 1f })
            {
                Part(go.transform, "Flange", cylinder, new Vector3(side*0.45f,0,0), new Vector3(SpoolFlange,SpoolFlangeThickness,SpoolFlange), cream, out var flange); flange.localRotation = Quaternion.Euler(0,0,90);
            }
            Part(go.transform, "YarnWrap", coil, Vector3.zero, Vector3.one, orange, out _);
            Label(go.transform, "Counter", "8", new Vector3(0,-0.2f,-0.52f), 0.095f, white, out var counter);
            Shadow(counter, out _);
            SavePrefab(go, "Spool", out prefab);
        }

        public const float ChevronWidth = 0.87f;
        public const float ChevronSweep = 0.42f;
        public const float ChevronBand = 0.36f;
        public const float ChevronHeight = 0.24f;
        public const float ChevronBase = -0.27f;
        public const float ChevronRound = 0.25f;
        public static readonly Vector2 ChevronUv = new Vector2(1, 0.25f);
        static Mesh chevron;

        static void BuildChevron()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            const int across = 24; const int band = 8;
            var tip = Mathf.Sqrt(1 + ChevronRound * ChevronRound) - ChevronRound;
            for (var i = 0; i <= across; i++)
            {
                var u = i / (float)across * 2 - 1;
                var bend = (Mathf.Sqrt(u * u + ChevronRound * ChevronRound) - ChevronRound) / tip;
                var z = ChevronSweep * (0.5f - bend);
                var end = Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(Mathf.Abs(u), 6)));
                for (var j = 0; j <= band; j++)
                {
                    var w = j / (float)band * 2 - 1;
                    var height = Mathf.Pow(Mathf.Max(0, 1 - w * w), 0.35f) * end;
                    vertices.Add(new Vector3(u * ChevronWidth * 0.5f, ChevronBase + ChevronHeight * height, z + w * ChevronBand * 0.5f));
                    uv.Add(new Vector2((u * 0.5f + 0.5f) * ChevronUv.x, (w * 0.5f + 0.5f) * ChevronUv.y));
                    if (i == across || j == band) continue;
                    var k = i * (band + 1) + j;
                    triangles.AddRange(new[] { k, k + 1, k + band + 1, k + band + 1, k + 1, k + band + 2 });
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            SaveMesh(mesh, "Chevron", out chevron);
        }

        static void MakeSegment(out GameObject prefab)
        {
            BuildChevron();
            var go = new GameObject("DragonSegment");
            Part(go.transform, "YarnChevron", chevron, Vector3.zero, Vector3.one, orange, out var piece);
            piece.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SavePrefab(go, "DragonSegment", out prefab);
        }

        static void MakeDragon(out GameObject prefab)
        {
            var go=new GameObject("DragonHead");
            SpritePart(go.transform,"Artwork",dragonSprite,20,out _);
            SavePrefab(go,"DragonHead",out prefab);
        }

        static void MakeKitten(out GameObject prefab)
        {
            ShaderMat("Bubble", "JamJam/WoolBubble", out var bubbleMaterial);
            var go=new GameObject("Kitten");
            SpritePart(go.transform,"Body",kittenSprite,30,out _);
            Part(go.transform,"Bubble",sphere,new Vector3(0,BubbleOffsetY,0),Vector3.one*BubbleScale,bubbleMaterial,out var bubble);
            var renderer=bubble.GetComponent<MeshRenderer>(); renderer.sortingOrder=31; renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
            SavePrefab(go,"Kitten",out prefab);
        }

        static void SpritePart(Transform parent,string name,Sprite sprite,int order,out Transform part)
        {
            var go=new GameObject(name); part=go.transform; part.SetParent(parent,false);
            var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sharedMaterial=spriteMaterial; renderer.sortingOrder=order;
        }

        static void BuildSpriteAssets()
        {
            var materialPath=Root+"/Materials/WoolSprite.mat";
            spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var shader=Shader.Find("JamJam/WoolSprite");
            if(shader==null) throw new InvalidOperationException("WoolSprite shader is missing");
            if(spriteMaterial==null) { spriteMaterial=new Material(shader); AssetDatabase.CreateAsset(spriteMaterial,materialPath); }
            spriteMaterial.shader=shader; spriteMaterial.SetColor("_Color",Color.white);
            EditorUtility.SetDirty(spriteMaterial);
            SpritePath("Kitten-Idle.png","Kitten-Wool.png",out var kittenPath);
            SpritePath("Dragon-Head-Idle.png","Dragon-Head-Wool.png",out var dragonPath);
            ImportSprite(kittenPath,1.5f,new Vector2(0.5f,0.06f),Vector4.zero,out kittenSprite);
            ImportSprite(dragonPath,2.1f,new Vector2(0.35f,0.7f),Vector4.zero,out dragonSprite);
            static void SpritePath(string preferred,string fallback,out string path)
            {
                const string folder="Assets/JamJam/Art/Sprites/";
                path=File.Exists(folder+preferred)?folder+preferred:folder+fallback;
            }
        }

        static void ImportSprite(string path,float worldHeight,Vector2 pivot,Vector4 border,out Sprite sprite,bool mipmaps=false)
        {
            if(!File.Exists(path)) throw new FileNotFoundException("Sprite source is missing",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteAlignment=(int)SpriteAlignment.Custom;
            importer.SetTextureSettings(settings);
            importer.spritePivot=pivot; importer.spriteBorder=border; importer.alphaIsTransparency=true;
            importer.mipmapEnabled=mipmaps; importer.maxTextureSize=1024; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=mipmaps?FilterMode.Trilinear:FilterMode.Bilinear; importer.wrapMode=TextureWrapMode.Clamp; importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            importer.spritePixelsPerUnit=texture.height/worldHeight; importer.SaveAndReimport();
            sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null) throw new InvalidOperationException("Could not import sprite: "+path);
        }

        static void MakeSlot(Material material,out GameObject prefab)
        {
            var go=new GameObject("SpoolSlot");
            var slotMesh=UnityEngine.Object.Instantiate(rounded);
            var vertices=slotMesh.vertices;
            for(var i=0;i<vertices.Length;i++) vertices[i]=Vector3.Scale(vertices[i],new Vector3(1.4f,0.12f,0.82f));
            slotMesh.vertices=vertices; slotMesh.RecalculateBounds(); slotMesh.RecalculateNormals();
            SaveMesh(slotMesh,"Slot",out var savedMesh);
            go.AddComponent<MeshFilter>().sharedMesh=savedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            var collider=go.AddComponent<BoxCollider>(); collider.size=new Vector3(1.4f,0.3f,0.82f); collider.center=new Vector3(0,0.1f,0);
            var view=go.AddComponent<WoolSlotView>();
            Label(go.transform,"Unlock","+",new Vector3(0,0.18f,0),0.09f,white,out view.Label);
            view.Label.color=new Color(1,1,1,0.32f);
            SavePrefab(go,"SpoolSlot",out prefab);
        }

        public static void Label(Transform parent,string name,string text,Vector3 position,float scale,Material color,out TextMesh label)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.transform.localRotation=Quaternion.Euler(WoolStage.Pitch,0,0); go.transform.localScale=Vector3.one*scale;
            label=go.AddComponent<TextMesh>(); label.text=text; label.fontSize=64; label.characterSize=1; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
            WoolUiStyle.LoadFont(out var font,out var style);
            label.font=font; label.fontStyle=style; label.color=color.GetColor("_BaseColor");
            go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }

        public static readonly Vector3 CounterShadowOffset=new Vector3(0.3f,-0.45f,0.4f);

        static void Shadow(TextMesh label,out TextMesh shadow)
        {
            var go=new GameObject("Shadow"); go.transform.SetParent(label.transform,false); go.transform.localPosition=CounterShadowOffset;
            shadow=go.AddComponent<TextMesh>(); shadow.text=label.text; shadow.fontSize=label.fontSize; shadow.characterSize=label.characterSize; shadow.anchor=label.anchor; shadow.alignment=label.alignment;
            shadow.font=label.font; shadow.fontStyle=label.fontStyle; shadow.color=WoolUiTheme.TextOutline;
            go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }
    }
}
