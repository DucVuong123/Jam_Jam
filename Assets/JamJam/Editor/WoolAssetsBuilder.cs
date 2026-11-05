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
        static Material cream;
        static Material charcoal;
        static Material white;
        static Material orange;
        static Material peach;
        static Material gold;
        static Texture2D knit;
        static Texture2D normal;
        static Sprite kittenSprite;
        static Sprite dragonSprite;
        static Sprite blockSprite;
        static Sprite arrowSprite;
        static Material spriteMaterial;

        public static void Build(out WoolArt art)
        {
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Textures");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            BuildKnit();
            NativeMesh(PrimitiveType.Sphere, "Sphere", out sphere);
            NativeMesh(PrimitiveType.Cylinder, "Cylinder", out cylinder);
            BuildRounded(); BuildLeaf(); BuildArrow(); BuildCoil();
            art = AssetDatabase.LoadAssetAtPath<WoolArt>(Root + "/WoolArt.asset");
            if (art == null) { art = ScriptableObject.CreateInstance<WoolArt>(); AssetDatabase.CreateAsset(art, Root + "/WoolArt.asset"); }
            var colors = new[] { "#21CFC2", "#4089F2", "#AC50E6", "#FFD44B", "#65C952", "#F265A2" };
            art.Yarn = new Material[6]; art.Thread = new Material[6];
            for (var i = 0; i < 6; i++)
            {
                Mat("Yarn_" + i, colors[i], true, out art.Yarn[i]);
                Mat("Thread_" + i, colors[i], false, out art.Thread[i]);
            }
            Mat("Ivory", "#FFFAE8", false, out cream);
            Mat("Ink", "#283A4A", false, out charcoal);
            Mat("White", "#FFFFFF", false, out white);
            Mat("DragonOrange", "#EE9B3E", true, out orange);
            Mat("KittenPeach", "#EABF77", true, out peach);
            Mat("Honey", "#F9C64D", true, out gold);
            Mat("Road", "#F8E6B9", false, out art.Road);
            Mat("RoadRim", "#D6B87D", false, out art.Rim);
            BuildSpriteAssets();
            MakeBlock(out art.Block);
            MakeSpool(out art.Spool);
            MakeSegment(out art.Segment);
            MakeDragon(out art.Dragon);
            MakeKitten(out art.Kitten);
            MakeSlot(art.Road, out art.Slot);
            var star = new GameObject("Confetti");
            Part(star.transform, "Paper", rounded, Vector3.zero, new Vector3(1, 0.12f, 0.65f), white, out _);
            SavePrefab(star, "Confetti", out art.Confetti);
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        static void Mat(string name, string hex, bool wool, out Material material)
        {
            var path = Root + "/Materials/" + name + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", wool ? 0.12f : 0.28f);
            if (wool)
            {
                material.SetTexture("_BaseMap", knit);
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.55f);
                material.EnableKeyword("_NORMALMAP");
                material.SetTextureScale("_BaseMap",new Vector2(2,2));
                material.SetTextureScale("_BumpMap",new Vector2(2,2));
            }
            if(!wool)
            {
                material.SetTexture("_BaseMap",null);
                material.SetTexture("_BumpMap",null);
                material.DisableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
        }

        static float Stitch(float x, float y)
        {
            y *= 7; x *= 7;
            var row = Mathf.Floor(y);
            x += ((int)row & 1) * 0.5f;
            x -= Mathf.Floor(x); y -= row;
            var p = new Vector2(x, y);
            var a = new Vector2(0.5f, 0.12f);
            var left = SegmentDistance(p, a, new Vector2(0.08f, 0.9f));
            var right = SegmentDistance(p, a, new Vector2(0.92f, 0.9f));
            var d = Mathf.Min(left, right);
            var height = Mathf.Exp(-d * d * 100);
            return height * (0.9f + 0.1f * Mathf.Sin((y * 35 + x * 14) * Mathf.PI));
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            return Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude));
        }

        static void BuildKnit()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var bump = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color[size * size];
            var normals = new Color[pixels.Length];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)size; var v = y / (float)size;
                    var h = Stitch(u, v);
                    var dx = (Stitch(u + 1f / size, v) - Stitch(u - 1f / size, v)) * 2.4f;
                    var dy = (Stitch(u, v + 1f / size) - Stitch(u, v - 1f / size)) * 2.4f;
                    var n = new Vector3(-dx, -dy, 1).normalized;
                    var value = 0.7f + h * 0.26f + Mathf.Sin(x * 2.1f + y * 1.7f) * 0.018f;
                    pixels[y * size + x] = new Color(value, value, value, 1);
                    normals[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1);
                }
            texture.SetPixels(pixels); texture.Apply(); bump.SetPixels(normals); bump.Apply();
            File.WriteAllBytes(Root + "/Textures/Knit.png", texture.EncodeToPNG());
            File.WriteAllBytes(Root + "/Textures/KnitNormal.png", bump.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(bump);
            AssetDatabase.ImportAsset(Root + "/Textures/Knit.png"); AssetDatabase.ImportAsset(Root + "/Textures/KnitNormal.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Textures/KnitNormal.png");
            importer.textureType = TextureImporterType.NormalMap; importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
            knit = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Knit.png");
            normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/KnitNormal.png");
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
                var t = i / (float)rings; var angle = t * Mathf.PI * 2 * 8;
                for (var j = 0; j <= sides; j++)
                {
                    var a = j / (float)sides * Mathf.PI * 2;
                    var radius = 0.22f + Mathf.Cos(a) * 0.034f;
                    vertices.Add(new Vector3((t - 0.5f) * 0.64f + Mathf.Sin(a) * 0.034f, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
                    uv.Add(new Vector2(t * 5, j / (float)sides));
                    if (i == rings || j == sides) continue;
                    var k = i * (sides + 1) + j;
                    triangles.AddRange(new[] { k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2 });
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
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

        static void MakeBlock(out GameObject prefab)
        {
            var go = new GameObject("ArrowBlock");
            var view = go.AddComponent<WoolBlockView>();
            SpritePart(go.transform,"YarnBody",blockSprite,0,out view.Body);
            view.Body.localPosition=new Vector3(0,0.34f,0);
            view.Body.localRotation=Quaternion.Euler(90,0,0);
            var renderer=view.Body.GetComponent<SpriteRenderer>(); renderer.drawMode=SpriteDrawMode.Tiled; renderer.size=new Vector2(2.02f,1.02f);
            SpritePart(go.transform,"Arrow",arrowSprite,1,out view.Arrow);
            view.Arrow.localPosition=new Vector3(0,0.36f,0.06f);
            view.Arrow.localRotation=Quaternion.Euler(90,0,0);
            view.Arrow.localScale=new Vector3(0.56f,0.78f,1);
            go.AddComponent<BoxCollider>();
            SavePrefab(go,"ArrowBlock",out prefab);
        }

        static void MakeSpool(out GameObject prefab)
        {
            var go = new GameObject("Spool");
            Part(go.transform, "YarnCore", cylinder, Vector3.zero, new Vector3(0.23f,0.43f,0.23f), orange, out var core); core.localRotation = Quaternion.Euler(0,0,90);
            foreach (var side in new[] { -1f, 1f })
            {
                Part(go.transform, "YarnFlange", cylinder, new Vector3(side*0.44f,0,0), new Vector3(0.5f,0.045f,0.5f), gold, out var flange); flange.localRotation = Quaternion.Euler(0,0,90);
            }
            Part(go.transform, "YarnWrap", coil, Vector3.zero, Vector3.one, orange, out _);
            Label(go.transform, "Counter", "8", new Vector3(0,0.14f,-0.62f), 0.09f, charcoal, out _);
            SavePrefab(go, "Spool", out prefab);
        }

        static void MakeSegment(out GameObject prefab)
        {
            var go = new GameObject("DragonSegment"); var combine = new List<CombineInstance>();
            foreach (var side in new[] { -1f, 1f })
                for (var row = 0; row < 3; row++)
                {
                    var matrix = Matrix4x4.TRS(new Vector3(side*(0.12f+row*0.08f),0.18f-row*0.075f,-row*0.055f),Quaternion.Euler(0,side*(32+row*8),side*row*-8),new Vector3(0.36f,0.7f,0.48f));
                    combine.Add(new CombineInstance { mesh=leaf,transform=matrix });
                }
            combine.Add(new CombineInstance { mesh=leaf,transform=Matrix4x4.TRS(new Vector3(0,0.25f,0.04f),Quaternion.identity,new Vector3(0.25f,0.7f,0.44f)) });
            var mesh = new Mesh(); mesh.CombineMeshes(combine.ToArray(),true,true); SaveMesh(mesh,"LayeredScales",out var scales);
            Part(go.transform,"YarnScales",scales,Vector3.zero,Vector3.one,orange,out _);
            SavePrefab(go,"DragonSegment",out prefab);
        }

        static void MakeDragon(out GameObject prefab)
        {
            var go=new GameObject("DragonHead");
            SpritePart(go.transform,"Artwork",dragonSprite,20,out _);
            SavePrefab(go,"DragonHead",out prefab);
        }

        static void MakeKitten(out GameObject prefab)
        {
            var go=new GameObject("Kitten");
            SpritePart(go.transform,"Body",kittenSprite,30,out _);
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
            ImportSprite("Assets/JamJam/Art/Sprites/Kitten-Wool.png",1.5f,new Vector2(0.5f,0.06f),Vector4.zero,out kittenSprite);
            ImportSprite("Assets/JamJam/Art/Sprites/Dragon-Head-Wool.png",2.1f,new Vector2(0.35f,0.7f),Vector4.zero,out dragonSprite);
            WriteArrowSprite();
            ImportSprite("Assets/JamJam/Art/Sprites/Block-Knit-Base.png",1,new Vector2(0.5f,0.5f),new Vector4(160,224,160,160),out blockSprite,true);
            ImportSprite(Root+"/Textures/Arrow2D.png",1,new Vector2(0.5f,0.5f),Vector4.zero,out arrowSprite);
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

        static void WriteArrowSprite()
        {
            const int size=256;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color[size*size];
            var polygon=new[] { new Vector2(-0.22f,-0.45f),new Vector2(0.22f,-0.45f),new Vector2(0.22f,0.09f),new Vector2(0.45f,0.09f),new Vector2(0,0.45f),new Vector2(-0.45f,0.09f),new Vector2(-0.22f,0.09f) };
            for(var y=0;y<size;y++) for(var x=0;x<size;x++)
            {
                var point=new Vector2((x+0.5f)/size-0.5f,(y+0.5f)/size-0.5f);
                var inside=false; var nearest=float.MaxValue;
                for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
                {
                    var a=polygon[i]; var b=polygon[j];
                    if((a.y>point.y)!=(b.y>point.y) && point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                    nearest=Mathf.Min(nearest,SegmentDistance(point,a,b));
                }
                if(!inside) continue;
                pixels[y*size+x]=nearest<0.018f?new Color(0.15f,0.2f,0.23f,1):Color.white;
            }
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(Root+"/Textures/Arrow2D.png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        static void MakeSlot(Material material,out GameObject prefab)
        {
            var go=new GameObject("SpoolSlot");
            var slotMesh=UnityEngine.Object.Instantiate(rounded);
            var vertices=slotMesh.vertices;
            for(var i=0;i<vertices.Length;i++) vertices[i]=Vector3.Scale(vertices[i],new Vector3(1.12f,0.12f,0.82f));
            slotMesh.vertices=vertices; slotMesh.RecalculateBounds(); slotMesh.RecalculateNormals();
            SaveMesh(slotMesh,"Slot",out var savedMesh);
            go.AddComponent<MeshFilter>().sharedMesh=savedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            var collider=go.AddComponent<BoxCollider>(); collider.size=new Vector3(1.12f,0.3f,0.82f); collider.center=new Vector3(0,0.1f,0);
            var view=go.AddComponent<WoolSlotView>();
            Label(go.transform,"Unlock","+",new Vector3(0,0.18f,0),0.09f,charcoal,out view.Label);
            SavePrefab(go,"SpoolSlot",out prefab);
        }

        public static void Label(Transform parent,string name,string text,Vector3 position,float scale,Material color,out TextMesh label)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.transform.localRotation=Quaternion.Euler(58,0,0); go.transform.localScale=Vector3.one*scale;
            label=go.AddComponent<TextMesh>(); label.text=text; label.fontSize=64; label.characterSize=1; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontStyle=FontStyle.Bold; label.color=color.GetColor("_BaseColor");
            go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }
    }
}
