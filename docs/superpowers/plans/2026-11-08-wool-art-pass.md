# Art pass gameplay Wool Jam

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Mục tiêu: đổi toàn bộ art màn `WoolJam.unity` sang phong cách len bão hòa trên nền navy vân vải: rồng chevron, block 3D mặt đan len, ống chỉ 3D, sợi chỉ xoắn, bong bóng quanh mèo, camera 62°, Volume hậu kỳ. Không đổi luật chơi, level data, vị trí lưới/slot/đường.

**Kiến trúc:** texture/mesh/material/prefab sinh bằng code trong `WoolAssetsBuilder` (ghi đè tại chỗ, giữ GUID); mesh block sinh runtime và cache theo (W,H) trong `WoolBlockMesh`; block dùng chung một material `JamJam/WoolKnit` (instanced `_BaseColor`) tô màu bằng `MaterialPropertyBlock`; menu mới `JamJam/Rebuild Art` (`WoolArtBuilder`) chạy builder asset rồi patch camera/ánh sáng/nền/Volume vào scene có sẵn; `WoolSceneBuilder.Build` gọi cùng `WoolArtBuilder.Apply`. Kiểm tra editor nằm trong `WoolArtChecks` (partial, mỗi task một file), gọi qua `unity command eval`.

**Tech:** Unity 6000.3.19f1, URP 17.3 (PC: Forward+ có SSAO; Mobile: Forward), C# 9, R3 1.3.1, uGUI `Text` legacy, harness .NET.

**Spec:** `docs/superpowers/specs/2026-11-08-wool-art-pass-design.md`

## Ràng buộc chung

- Palette len theo index level: 0 Đỏ `#E8352F`, 1 Vàng `#FFCE1F`, 2 Xanh lá `#3CC03A`, 3 Cyan `#22CFE0`, 4 Xanh dương `#2F74EA`, 5 Hồng `#F04FAE`; shade mặt bên ~70% value (`_SideShade = 0.7`). Cam chỉ cho đầu rồng/mèo.
- Nền navy `#14234A` vân vải; đường nỉ `#C9CFDA` có hai đường chỉ khâu trắng đứt nét; pad khay `#A9B6CF`, ô khóa có `+` mờ; cushion navy sáng hơn nền một bậc (`#1F3566`).
- Mũi tên trắng ngà `#FFF8EC` viền sẫm mỏng; block dày ~0.35; chevron rộng ~1.0 (thế giới); camera pitch ~62°, giữ điểm nhìn `(0,0,0.8)`.
- Volume: Color Adjustments saturation tăng nhẹ, Bloom threshold cao intensity thấp, Tonemapping Neutral, vignette nhẹ.
- Không comment trong source/shader. Method trả reference dùng interface/abstract; factory `void` + `out` (summary check trả `string` theo yêu cầu user). Timer lặp dùng R3 (plan này không thêm timer).
- Giữ `.meta`/GUID: mọi asset đã có (prefab `ArrowBlock`, `DragonSegment`, `Spool`, `SpoolSlot`, `Kitten`; material `Yarn_i`, `Thread_i`, `Road`, `RoadRim`, `Ground`, `Ivory`, `Ink`, `White`; texture `Knit.png`, `KnitNormal.png`; mesh `Slot`, `WoundThread`, `Sphere`, `Cylinder`) được ghi đè tại chỗ, không xóa-tạo lại.
- KHÔNG chạy `JamJam/Build Wool Jam Scene` / `WoolSceneBuilder.Build()` / `WoolEditorChecks.RunBatch` trên project chính. Task 1–4 dựng asset bằng `WoolAssetsBuilder.Build(out _)` (không chạm level/scene); Task 5 trở đi dùng `JamJam/Rebuild Art`.
- Không stage/commit; mỗi task kết thúc bằng "Dừng, báo user".
- Số dòng trong header **Trước** là số dòng của file hiện tại (2026-11-08) trước khi bắt đầu plan; khi làm task sau, tìm theo nội dung Trước (nội dung không bị task trước sửa).
- Lệnh Unity đi qua Editor đang mở: `unity recompile`, `unity command eval --caller plugin --skill unity-cli --no-banner '<C#>'`, `unity command start|stop --caller plugin --skill unity-cli`, `unity command game_view --caller plugin --skill unity-cli ...`, `unity command get --caller plugin --skill unity-cli --level error --tail 50`.
- Biến shell dùng trong plan (Git Bash): `SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"`.

## Review Focus

- Đổi Body sprite → mesh làm hỏng click/collider hoặc quy ước `view.Arrow.up` mà `WoolEditorChecks.InspectMotion` dùng → `WoolArtChecks.Blocks()` (mũi tên mặc định chỉ +Z) và `WoolArtChecks.PlayBlocks()` (click bị chặn chỉ rung, click hợp lệ bay, collider đúng `W-0.035 × 0.55 × H-0.035`). Task 2.
- `MaterialPropertyBlock` không ăn màu (block trắng) hoặc phá batching làm draw call tăng >2× → `PlayBlocks()` so màu MPB với `Yarn_i`; Task 1 bước 0 ghi baseline `UnityStats`, Task 7 so lại.
- Builder tạo lại asset làm đổi GUID hoặc lỡ ghi level → Task 1 bước 0 chụp `guid:` của mọi `.meta` và md5 level; kiểm lại ở Task 1, 5, 7 (`comm -23` rỗng, `md5sum -c` OK).
- `Rebuild Art` không idempotent (tạo Volume trùng, đổi UI, scene khác nhau giữa hai lần chạy) → Task 5 chạy hai lần, `cmp` scene, `Stage()` kiểm đúng một Volume, diff `m_Name`/`m_Text` chỉ thêm `Art Volume`.
- Pitch 62° làm cắt đường/khay/board ở 1080×1920 hoặc 1080×2400 → `WoolArtChecks.Stage()` chiếu mọi điểm đường (±rim), slot, góc board vào viewport ở cả hai tỉ lệ. Task 5.

## Bản đồ file

| File | Việc |
|---|---|
| `Assets/JamJam/Runtime/WoolArt.cs` | Sửa — thêm `Pad`, `PadLocked` (Task 1) |
| `Assets/JamJam/Editor/WoolAssetsBuilder.cs` | Sửa — texture/material (T1), block (T2), chevron (T3), ống chỉ/slot/bong bóng/label (T4), sprite fallback (T6) |
| `Assets/JamJam/Editor/WoolArtChecks.cs` | Tạo — helper + `Textures()` (T1) |
| `Assets/JamJam/Runtime/WoolBlockMesh.cs` | Tạo — mesh hộp bo góc + cache (T2) |
| `Assets/JamJam/Shaders/WoolKnit.shader` | Tạo — shader block instanced (T2) |
| `Assets/JamJam/Editor/WoolArtChecks.Blocks.cs` | Tạo — `Blocks()` (T2) |
| `Assets/JamJam/Editor/WoolArtChecks.Play.cs` | Tạo — `PlayBlocks()` (T2) |
| `Assets/JamJam/Runtime/WoolScene.cs` | Sửa — hiển thị block (T2), sợi chỉ + UV đường (T3), pad (T4) |
| `Assets/JamJam/Editor/WoolEditorChecks.cs` | Sửa — check batch theo block mesh (T2) và bong bóng (T4) |
| `Assets/JamJam/Editor/WoolArtChecks.Chevron.cs` | Tạo — `Chevron()` (T3) |
| `Assets/JamJam/Shaders/WoolBubble.shader` | Tạo — bong bóng fresnel (T4) |
| `Assets/JamJam/Editor/WoolArtChecks.Tray.cs` | Tạo — `Tray()` (T4) |
| `Assets/JamJam/Editor/WoolArtBuilder.cs` | Tạo — menu `JamJam/Rebuild Art`, `Apply` (T5) |
| `Assets/JamJam/Editor/WoolArtChecks.Stage.cs` | Tạo — `Stage()` (T5) |
| `Assets/JamJam/Editor/WoolSceneBuilder.cs` | Sửa — dùng `WoolArtBuilder.Apply` (T5) |
| `docs/wool-character-prompts.md` | Tạo — prompt sprite nhân vật (T6) |
| Asset sinh mới | `Generated/Textures/Fabric.png`, `FeltPath.png`, `Thread.png`; `Generated/Materials/Pad.mat`, `PadLocked.mat`, `Cushion.mat`, `Wood.mat`, `WoolBlock.mat`, `Bubble.mat`; `Generated/Meshes/KnitBlock.asset`, `ArrowPlate.asset`, `Chevron.asset`; `Generated/WoolPost.asset` |

Đã xác minh mọi file/asset "Tạo" chưa tồn tại (2026-11-08). `WoolJam.unity` hiện không có Volume và camera chưa có `UniversalAdditionalCameraData`.

---

### Task 1: Palette, texture procedural, material

**Files:** `WoolArt.cs`, `WoolAssetsBuilder.cs` (dòng 18–25, 39, 45–59, 74–98, 100–149), tạo `WoolArtChecks.cs`.

**Interfaces — Produces:**
- `WoolArt.Pad`, `WoolArt.PadLocked : Material`.
- `WoolAssetsBuilder`: `public static readonly string[] Palette`; hằng `KnitColumns`, `KnitRows`, `KnitFloor`, `FabricColumns`, `FabricRows`, `FeltStitchInset`, `FeltDashes`, `ThreadPly`, `YarnTiling`, `ThreadTiling`, `RoadTiling`, `GroundTiling`, `CushionTiling`; `static void Mat(string name,string hex,Texture2D albedo,Texture2D bump,Vector2 tiling,float smoothness,out Material material)`; `static void WriteTexture(string name,int width,int height,Func<float,float,Color> pixel,bool normalMap,out Texture2D texture)`; static `knit`, `normal`, `fabric`, `felt`, `thread`, `wood`.
- Texture `Knit.png` 256², `KnitNormal.png` 256² (normal map), `Fabric.png` 256², `FeltPath.png` 64×256, `Thread.png` 64×32 — tất cả wrap Repeat.
- Material `Ground.mat` (navy + Fabric), `Cushion.mat`, `Pad.mat`, `PadLocked.mat`, `Wood.mat`; `Road.mat` trắng + FeltPath; `RoadRim.mat` `#9AA4B8`; `Ivory.mat` `#FFF8EC`.
- `WoolArtChecks.Textures() : string`; helper `Expect`, `ExpectColor`, `ExpectSize`, `ExpectBounds`, `ExpectOutward`, `Near`, `LoadArt`, `TextureAt`, `MaterialAt`, `Asset<T>`.

- [ ] **Bước 0: Baseline (trước mọi thay đổi)**

```bash
SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"
mkdir -p "$SCRATCH/art"
cd "/d/Unity Project/Jam_Jam"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders --include=*.meta | sort > "$SCRATCH/art/guids.before.txt"
md5sum Assets/JamJam/Generated/Levels/*.asset > "$SCRATCH/art/levels.md5"
cp Assets/Scenes/WoolJam.unity "$SCRATCH/art/WoolJam.before.unity"
```

Mở `WoolJam.unity` trong Editor, Game view hiển thị. `unity command start --caller plugin --skill unity-cli`, rồi:

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(0); "ok"'
unity command eval --caller plugin --skill unity-cli --no-banner '"batches " + UnityEditor.UnityStats.batches + " draw " + UnityEditor.UnityStats.drawCalls + " setpass " + UnityEditor.UnityStats.setPassCalls'
unity command game_view --caller plugin --skill unity-cli --source camera --overlay true --width 1080 --height 1920 --output-path "$SCRATCH/art/before-1080x1920.png"
unity command stop --caller plugin --skill unity-cli
```

Ghi dòng `batches/draw/setpass` vào `$SCRATCH/art/drawcalls.txt` (nhãn `before L1`).

- [ ] **Bước 1: Thêm field vào `WoolArt.cs`**

**Trước**
```17:17:Assets/JamJam/Runtime/WoolArt.cs
        public Material Rim;
```

**Sau**
```csharp
        public Material Rim;
        public Material Pad;
        public Material PadLocked;
```

- [ ] **Bước 2: Viết check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.cs`**

```csharp
using System;
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
            ExpectColor(art.Road, "#FFFFFF", "Road");
            Expect(art.Road.GetTexture("_BaseMap") == felt, "Road felt texture");
            ExpectColor(art.Pad, "#A9B6CF", "Pad");
            Expect(art.PadLocked != null, "PadLocked material");
            MaterialAt("Ground", out var ground);
            ExpectColor(ground, "#14234A", "Ground");
            Expect(ground.GetTexture("_BaseMap") == fabric, "Ground fabric texture");
            MaterialAt("Ivory", out var ivory);
            ExpectColor(ivory, "#FFF8EC", "Ivory");
            MaterialAt("Cushion", out _);
            MaterialAt("Wood", out _);
            return "TEXTURES PASS / 6 palette colors / 5 textures";
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
```

- [ ] **Bước 3: Chạy RED**

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.Textures()'
```
Kỳ vọng: compile 0 lỗi; eval ném `ART CHECK FAILED: Yarn_0 color #E8352F but was #21CFC2`.

- [ ] **Bước 4: Sửa field trong `WoolAssetsBuilder.cs`**

**Trước**
```18:25:Assets/JamJam/Editor/WoolAssetsBuilder.cs
        static Material cream;
        static Material charcoal;
        static Material white;
        static Material orange;
        static Material peach;
        static Material gold;
        static Texture2D knit;
        static Texture2D normal;
```

**Sau**
```csharp
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
```

- [ ] **Bước 5: Gọi bộ sinh texture mới**

**Trước**
```39:39:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            BuildKnit();
```

**Sau**
```csharp
            BuildTextures();
```

- [ ] **Bước 6: Palette và material**

**Trước**
```45:59:Assets/JamJam/Editor/WoolAssetsBuilder.cs
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
```

**Sau**
```csharp
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
            Mat("Road", "#FFFFFF", felt, null, RoadTiling, 0.08f, out art.Road);
            Mat("RoadRim", "#9AA4B8", null, null, Vector2.one, 0.1f, out art.Rim);
            Mat("Pad", "#A9B6CF", null, null, Vector2.one, 0.2f, out art.Pad);
            Mat("PadLocked", "#7C89A6", null, null, Vector2.one, 0.2f, out art.PadLocked);
            Mat("Ground", "#14234A", fabric, null, GroundTiling, 0.05f, out _);
            Mat("Cushion", "#1F3566", fabric, null, CushionTiling, 0.05f, out _);
            Mat("Wood", "#E3B98A", null, null, Vector2.one, 0.3f, out wood);
```

- [ ] **Bước 7: `Mat` nhận texture/tiling**

**Trước**
```74:98:Assets/JamJam/Editor/WoolAssetsBuilder.cs
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
```

**Sau**
```csharp
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
```

- [ ] **Bước 8: Texture procedural**

**Trước**
```100:149:Assets/JamJam/Editor/WoolAssetsBuilder.cs
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
```

**Sau**
```csharp
        public const int KnitColumns = 6;
        public const int KnitRows = 8;
        public const float KnitFloor = 0.62f;
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
            var shade = Mathf.Lerp(0.72f, 1, Mathf.SmoothStep(0, 0.14f, edge)) + (Noise(u, v) - 0.5f) * 0.05f;
            var line = Mathf.Min(Mathf.Abs(u - FeltStitchInset), Mathf.Abs(u - (1 - FeltStitchInset)));
            var dash = Mathf.Repeat(v * FeltDashes, 1) < 0.55f ? 1f : 0f;
            var stitch = (1 - Mathf.SmoothStep(0.012f, 0.026f, line)) * dash;
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
```

- [ ] **Bước 9: Dựng asset (không chạm level/scene) và chạy GREEN**

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolAssetsBuilder.Build(out var art); JamJam.Editor.WoolArtChecks.Textures()'
```
Kỳ vọng: compile 0 lỗi; in `TEXTURES PASS / 6 palette colors / 5 textures`.

- [ ] **Bước 10: Kiểm GUID/level**

```bash
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders --include=*.meta | sort > "$SCRATCH/art/guids.now.txt"
comm -23 "$SCRATCH/art/guids.before.txt" "$SCRATCH/art/guids.now.txt"
md5sum -c "$SCRATCH/art/levels.md5"
unity command get --caller plugin --skill unity-cli --level error --tail 50
```
Kỳ vọng: `comm` không in gì (chỉ có asset mới được thêm); ba level `OK`; không lỗi console mới (lỗi SearchDatabase lúc khởi động có sẵn bỏ qua).

- [ ] **Bước 11: Dừng, báo user** (màu mới, texture mới, GUID giữ nguyên).

---

### Task 2: Block 3D + MaterialPropertyBlock

**Files:** tạo `WoolBlockMesh.cs`, `WoolKnit.shader`, `WoolArtChecks.Blocks.cs`, `WoolArtChecks.Play.cs`; sửa `WoolAssetsBuilder.cs` (28–29, 271–285, 346–348, 368–390), `WoolScene.cs` (41, 111–115, 215–218), `WoolEditorChecks.cs` (125–126).

**Interfaces — Consumes:** Task 1 (`knit`, `normal`, `cream`, `charcoal`). **Produces:**
- `public static class WoolBlockMesh { const float Thickness=0.35f, Inset=0.04f, Corner=0.22f, Bevel=0.06f, Border=0.12f; static void Get(int width,int height,out Mesh mesh); static void Build(int width,int height,out Mesh mesh); }` — pivot ở đáy, UV0 = vân đan (đơn vị ô), UV1 = (cạnh viền 0→1, chu vi).
- Shader `JamJam/WoolKnit` (`_BaseMap`, `_BumpMap`, `_BumpScale`, `_BaseColor` instanced, `_SideShade`, `_BraidDensity`, `_BraidDark`, `_Wrap`; pass ForwardLit, ShadowCaster, DepthOnly, DepthNormals).
- `WoolAssetsBuilder`: `static void ShaderMat(string name,string shaderName,out Material material)`; hằng `BlockSideShade`, `BraidDensity`, `BraidDark`, `LightWrap`, `ArrowRimLift`, `ArrowFaceLift`; mesh `KnitBlock.asset`, `ArrowPlate.asset` (2 submesh: viền Ink, mặt Ivory), material `WoolBlock.mat`.
- Prefab `ArrowBlock`: `YarnBody` (MeshFilter+MeshRenderer `WoolBlock`), `Arrow` (MeshRenderer `{Ink, Ivory}`, rotation `Euler(90,0,0)`), `BoxCollider`.
- `WoolArtChecks.Blocks()`, `WoolArtChecks.PlayBlocks()`.

- [ ] **Bước 1: Tạo `Assets/JamJam/Runtime/WoolBlockMesh.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace JamJam
{
    public static class WoolBlockMesh
    {
        public const float Thickness = 0.35f;
        public const float Inset = 0.04f;
        public const float Corner = 0.22f;
        public const float Bevel = 0.06f;
        public const float Border = 0.12f;
        const int CornerSteps = 5;
        const int BevelSteps = 3;
        const int Count = 4 * (CornerSteps + 1);
        static readonly Dictionary<(int, int), Mesh> cache = new Dictionary<(int, int), Mesh>();

        public static void Get(int width, int height, out Mesh mesh)
        {
            if (cache.TryGetValue((width, height), out mesh) && mesh != null) return;
            Build(width, height, out mesh);
            cache[(width, height)] = mesh;
        }

        public static void Build(int width, int height, out Mesh mesh)
        {
            var halfX = width * 0.5f - Inset;
            var halfZ = height * 0.5f - Inset;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var edge = new List<Vector2>();
            var triangles = new List<int>();
            var bottom = Ring(halfX, halfZ, 0, 0, 0, true, 1, vertices, normals, uv, edge);
            var shoulder = Ring(halfX, halfZ, 0, Thickness - Bevel, 0, true, 1, vertices, normals, uv, edge);
            Connect(bottom, shoulder, triangles);
            var lower = Ring(halfX, halfZ, 0, Thickness - Bevel, 0, false, 0, vertices, normals, uv, edge);
            for (var step = 1; step <= BevelSteps; step++)
            {
                var tilt = step / (float)BevelSteps * Mathf.PI * 0.5f;
                var upper = Ring(halfX, halfZ, Bevel * (1 - Mathf.Cos(tilt)), Thickness - Bevel + Bevel * Mathf.Sin(tilt), tilt, false, 0, vertices, normals, uv, edge);
                Connect(lower, upper, triangles);
                lower = upper;
            }
            var inner = Ring(halfX, halfZ, Bevel + Border, Thickness, Mathf.PI * 0.5f, false, 1, vertices, normals, uv, edge);
            Connect(lower, inner, triangles);
            var center = vertices.Count;
            vertices.Add(new Vector3(0, Thickness, 0));
            normals.Add(Vector3.up);
            uv.Add(Vector2.zero);
            edge.Add(new Vector2(1, 0));
            for (var k = 0; k < Count; k++) triangles.AddRange(new[] { center, inner + k + 1, inner + k });
            mesh = new Mesh { name = "KnitBlock_" + width + "x" + height };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, edge);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        static int Ring(float halfX, float halfZ, float inset, float y, float tilt, bool side, float edgeX, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<Vector2> edge)
        {
            var start = vertices.Count;
            var radius = Mathf.Max(0.01f, Corner - inset);
            var perimeter = 0f;
            var previous = Vector3.zero;
            for (var k = 0; k <= Count; k++)
            {
                var index = k % Count;
                var quadrant = index / (CornerSteps + 1);
                var angle = (quadrant + index % (CornerSteps + 1) / (float)CornerSteps) * Mathf.PI * 0.5f;
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var corner = new Vector3((quadrant == 0 || quadrant == 3 ? 1 : -1) * (halfX - Corner), y, (quadrant < 2 ? 1 : -1) * (halfZ - Corner));
                var outline = corner + direction * Corner;
                if (k > 0) perimeter += Vector3.Distance(previous, outline);
                previous = outline;
                var point = corner + direction * radius;
                vertices.Add(point);
                normals.Add((direction * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt)).normalized);
                uv.Add(side ? new Vector2(perimeter, y) : new Vector2(point.x, point.z));
                edge.Add(new Vector2(edgeX, perimeter));
            }
            return start;
        }

        static void Connect(int lower, int upper, List<int> triangles)
        {
            for (var k = 0; k < Count; k++)
                triangles.AddRange(new[] { lower + k, upper + k, lower + k + 1, lower + k + 1, upper + k, upper + k + 1 });
        }
    }
}
```

- [ ] **Bước 2: Viết check — tạo `Assets/JamJam/Editor/WoolArtChecks.Blocks.cs`**

```csharp
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Blocks()
        {
            WoolBlockMesh.Build(2, 1, out var wide);
            WoolBlockMesh.Build(1, 2, out var tall);
            var inset = WoolBlockMesh.Inset * 2;
            ExpectBounds(wide, new Vector3(2 - inset, WoolBlockMesh.Thickness, 1 - inset), "Block 2x1");
            ExpectBounds(tall, new Vector3(1 - inset, WoolBlockMesh.Thickness, 2 - inset), "Block 1x2");
            Expect(Mathf.Abs(wide.bounds.min.y) < 0.001f, "Block pivot at base");
            Expect(wide.uv2.Length == wide.vertexCount && wide.tangents.Length == wide.vertexCount, "Block edge UV and tangents");
            ExpectOutward(wide, "Block 2x1");
            ExpectOutward(tall, "Block 1x2");
            UnityEngine.Object.DestroyImmediate(wide);
            UnityEngine.Object.DestroyImmediate(tall);
            WoolBlockMesh.Get(2, 1, out var first);
            WoolBlockMesh.Get(2, 1, out var second);
            WoolBlockMesh.Get(1, 2, out var other);
            Expect(first == second && first != other, "Block mesh cache keyed by size");
            LoadArt(out var art);
            var view = art.Block.GetComponent<WoolBlockView>();
            var filter = view.Body.GetComponent<MeshFilter>();
            var body = view.Body.GetComponent<MeshRenderer>();
            Expect(filter != null && filter.sharedMesh != null && body != null, "ArrowBlock body is a mesh");
            Expect(body.sharedMaterial.shader.name == "JamJam/WoolKnit" && body.sharedMaterial.enableInstancing, "Block material is instanced WoolKnit");
            Expect(!ShaderUtil.ShaderHasError(body.sharedMaterial.shader), "WoolKnit compiles");
            var arrow = view.Arrow.GetComponent<MeshRenderer>();
            Expect(arrow != null && arrow.sharedMaterials.Length == 2, "Arrow plate has rim and face");
            ExpectColor(arrow.sharedMaterials[1], "#FFF8EC", "Arrow face");
            Expect(Vector3.Dot(view.Arrow.up, Vector3.forward) > 0.99f, "Arrow points along +Z by default");
            Expect(art.Block.GetComponent<BoxCollider>() != null, "Block collider kept");
            return "BLOCKS PASS / " + first.vertexCount + " vertices per block";
        }
    }
}
```

- [ ] **Bước 3: Tạo `Assets/JamJam/Editor/WoolArtChecks.Play.cs`**

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string PlayBlocks()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null, "WoolScene is running");
            Physics.SyncTransforms();
            var tint = new MaterialPropertyBlock();
            var views = UnityEngine.Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None);
            foreach (var view in views)
            {
                var definition = scene.State.Blocks[view.Index].Definition;
                var collider = view.GetComponent<BoxCollider>();
                Expect(Vector3.Distance(collider.size, new Vector3(definition.Width - 0.035f, 0.55f, definition.Height - 0.035f)) < 0.001f, "Collider size of block " + view.Index);
                var filter = view.Body.GetComponent<MeshFilter>();
                Expect(filter != null && filter.sharedMesh != null, "Block " + view.Index + " body mesh");
                var size = filter.sharedMesh.bounds.size;
                Expect(Mathf.Abs(size.x - (definition.Width - WoolBlockMesh.Inset * 2)) < 0.01f && Mathf.Abs(size.z - (definition.Height - WoolBlockMesh.Inset * 2)) < 0.01f, "Block " + view.Index + " mesh matches " + definition.Width + "x" + definition.Height);
                view.Body.GetComponent<Renderer>().GetPropertyBlock(tint);
                var expected = scene.Art.Yarn[definition.Color].GetColor("_BaseColor");
                var actual = tint.GetColor("_BaseColor");
                Expect(Near(actual, expected) || Near(actual, expected.linear), "Block " + view.Index + " tint");
            }
            var blocked = false;
            foreach (var view in views)
            {
                if (scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                Expect(view.Shake > 0 && scene.State.Blocks[view.Index].Phase == 0, "Blocked block " + view.Index + " only shakes");
                blocked = true;
                break;
            }
            Expect(blocked, "A blocked block was clicked through EventSystem");
            var moved = false;
            foreach (var view in views)
            {
                if (!scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                Expect(scene.State.Blocks[view.Index].Phase == 1, "Clear block " + view.Index + " starts its flight");
                moved = true;
                break;
            }
            Expect(moved, "A clear block was clicked through EventSystem");
            return "PLAY BLOCKS PASS / " + views.Length + " blocks";
        }

        static void Click(WoolBlockView view, out bool hit)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(view.transform.position + Vector3.up * 0.3f), button = PointerEventData.InputButton.Left };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            hit = results.Count > 0 && results[0].gameObject == view.gameObject;
            if (hit) ExecuteEvents.Execute(view.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
```

- [ ] **Bước 4: Chạy RED**

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.Blocks()'
```
Kỳ vọng: phần mesh qua; ném `ART CHECK FAILED: ArrowBlock body is a mesh` (prefab còn sprite).

- [ ] **Bước 5: Tạo `Assets/JamJam/Shaders/WoolKnit.shader`**

```hlsl
Shader "JamJam/WoolKnit"
{
    Properties
    {
        _BaseMap("Knit",2D)="white" {}
        [Normal] _BumpMap("Knit Normal",2D)="bump" {}
        _BumpScale("Normal Scale",Float)=0.6
        _BaseColor("Tint",Color)=(1,1,1,1)
        _SideShade("Side Shade",Range(0,1))=0.7
        _BraidDensity("Braid Density",Float)=7
        _BraidDark("Braid Dark",Range(0,1))=0.62
        _Wrap("Light Wrap",Range(0,1))=0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half _BumpScale;
            half _SideShade;
            half _BraidDensity;
            half _BraidDark;
            half _Wrap;
        CBUFFER_END
        UNITY_INSTANCING_BUFFER_START(WoolKnitProps)
            UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
        UNITY_INSTANCING_BUFFER_END(WoolKnitProps)
        struct DepthAttributes
        {
            float4 positionOS:POSITION;
            float3 normalOS:NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct DepthVaryings
        {
            float4 positionCS:SV_POSITION;
            float3 normalWS:TEXCOORD0;
        };
        DepthVaryings DepthVert(DepthAttributes input)
        {
            DepthVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
            output.normalWS=TransformObjectToWorldNormal(input.normalOS);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            struct Attributes
            {
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
                float4 tangentOS:TANGENT;
                float2 uv:TEXCOORD0;
                float2 edge:TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float2 uv:TEXCOORD0;
                float2 edge:TEXCOORD1;
                float3 positionWS:TEXCOORD2;
                float3 normalWS:TEXCOORD3;
                float4 tangentWS:TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                VertexPositionInputs position=GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal=GetVertexNormalInputs(input.normalOS,input.tangentOS);
                output.positionCS=position.positionCS;
                output.positionWS=position.positionWS;
                output.normalWS=normal.normalWS;
                output.tangentWS=float4(normal.tangentWS,input.tangentOS.w*GetOddNegativeScale());
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                output.edge=input.edge;
                return output;
            }
            half Braid(float2 edge)
            {
                float a=frac(edge.y*_BraidDensity+edge.x*0.5);
                float b=frac(edge.y*_BraidDensity-edge.x*0.5+0.5);
                float ridge=max(1.0-abs(a*2.0-1.0),1.0-abs(b*2.0-1.0));
                half braid=lerp(_BraidDark,1.0h,smoothstep(0.25,0.85,ridge));
                half groove=lerp(1.0h,_BraidDark,smoothstep(0.8,1.0,edge.x));
                return edge.x<0.999?braid*groove:1.0h;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 tint=UNITY_ACCESS_INSTANCED_PROP(WoolKnitProps,_BaseColor);
                half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*tint.rgb;
                float3 normalWS=normalize(input.normalWS);
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_BumpScale);
                float3 bitangent=input.tangentWS.w*cross(normalWS,input.tangentWS.xyz);
                float3 shadedNormal=normalize(TransformTangentToWorld(normalTS,half3x3(input.tangentWS.xyz,bitangent,normalWS)));
                albedo*=lerp(_SideShade,1.0h,saturate(normalWS.y));
                albedo*=Braid(input.edge);
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half wrap=saturate((dot(shadedNormal,light.direction)+_Wrap)/(1.0+_Wrap));
                half3 lighting=light.color*wrap*light.shadowAttenuation+SampleSH(shadedNormal);
                return half4(albedo*lighting,1.0h);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float4 ShadowVert(DepthAttributes input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS=TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS=TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,_LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z=min(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z=max(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }
            half4 ShadowFrag():SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half4 DepthFrag(DepthVaryings input):SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            half4 NormalFrag(DepthVaryings input):SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS),0.0);
            }
            ENDHLSL
        }
    }
}
```

Ghi chú: nếu Unity báo `redefinition of '_LightDirection'` thì xóa dòng `float3 _LightDirection;` (URP 17.3 khai báo biến này trong `ShadowCasterPass.hlsl`, không phải `Shadows.hlsl`).

- [ ] **Bước 6: Field builder cho block**

**Trước**
```28:29:Assets/JamJam/Editor/WoolAssetsBuilder.cs
        static Sprite blockSprite;
        static Sprite arrowSprite;
```

**Sau**
```csharp
        static Mesh arrowPlate;
        public const float BlockSideShade = 0.7f;
        public const float BraidDensity = 7f;
        public const float BraidDark = 0.62f;
        public const float LightWrap = 0.45f;
        const float ArrowRimLift = 0.012f;
        const float ArrowFaceLift = 0.03f;
```

- [ ] **Bước 7: `MakeBlock` mesh + mũi tên nổi**

**Trước**
```271:285:Assets/JamJam/Editor/WoolAssetsBuilder.cs
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
```

**Sau**
```csharp
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
            Part(go.transform, "YarnBody", bodyMesh, Vector3.zero, Vector3.one, body, out view.Body);
            Part(go.transform, "Arrow", arrowPlate, new Vector3(0, WoolBlockMesh.Thickness + 0.005f, 0), new Vector3(0.56f, 1.56f, 1), charcoal, out view.Arrow);
            view.Arrow.localRotation = Quaternion.Euler(90, 0, 0);
            var arrowRenderer = view.Arrow.GetComponent<MeshRenderer>();
            arrowRenderer.sharedMaterials = new[] { charcoal, cream };
            arrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<BoxCollider>();
            SavePrefab(go, "ArrowBlock", out prefab);
        }
```

- [ ] **Bước 8: Bỏ sprite block/mũi tên 2D** (file `Block-Knit-Base.png`, `Arrow2D.png`, `Block2D.png` giữ nguyên trên đĩa).

**Trước**
```346:348:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            WriteArrowSprite();
            ImportSprite("Assets/JamJam/Art/Sprites/Block-Knit-Base.png",1,new Vector2(0.5f,0.5f),new Vector4(160,224,160,160),out blockSprite,true);
            ImportSprite(Root+"/Textures/Arrow2D.png",1,new Vector2(0.5f,0.5f),Vector4.zero,out arrowSprite);
```

**Sau:** xóa ba dòng (không có dòng thay thế).

**Trước**
```368:390:Assets/JamJam/Editor/WoolAssetsBuilder.cs

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
```

**Sau:** xóa toàn bộ đoạn (dòng trống 368 và method `WriteArrowSprite`).

- [ ] **Bước 9: `WoolScene` — field MPB**

**Trước**
```41:41:Assets/JamJam/Runtime/WoolScene.cs
        readonly List<Mesh> roadMeshes = new List<Mesh>();
```

**Sau**
```csharp
        readonly List<Mesh> roadMeshes = new List<Mesh>();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock blockTint;
```

- [ ] **Bước 10: `WoolScene` — chọn mesh theo kích thước + tô màu MPB** (dòng 116–124 về hướng mũi tên, collider, vị trí giữ nguyên)

**Trước**
```111:115:Assets/JamJam/Runtime/WoolScene.cs
                var body=view.Body.GetComponent<SpriteRenderer>();
                body.size=new Vector2(data.Width*Cell+0.02f,data.Height*Cell+0.02f);
                var color=Art.Yarn[data.Color].GetColor("_BaseColor");
                Color.RGBToHSV(color,out var hue,out var saturation,out var value);
                body.color=Color.HSVToRGB(hue,Mathf.Min(1,saturation*1.25f),Mathf.Min(1,value*1.12f));
```

**Sau**
```csharp
                WoolBlockMesh.Get(data.Width, data.Height, out var bodyMesh);
                view.Body.GetComponent<MeshFilter>().sharedMesh = bodyMesh;
                blockTint ??= new MaterialPropertyBlock();
                blockTint.SetColor(BaseColor, Art.Yarn[data.Color].GetColor(BaseColor));
                view.Body.GetComponent<Renderer>().SetPropertyBlock(blockTint);
```

- [ ] **Bước 11: `WoolScene` — bỏ sortingOrder khi bay** (mesh opaque dùng depth)

**Trước**
```215:218:Assets/JamJam/Runtime/WoolScene.cs
                if (block.Phase == 1)
                {
                    view.Body.GetComponent<SpriteRenderer>().sortingOrder=10;
                    view.Arrow.GetComponent<SpriteRenderer>().sortingOrder=11;
```

**Sau**
```csharp
                if (block.Phase == 1)
                {
```

- [ ] **Bước 12: `WoolEditorChecks` — check batch theo block mesh**

**Trước**
```125:126:Assets/JamJam/Editor/WoolEditorChecks.cs
                if(art.Kitten.GetComponentInChildren<SpriteRenderer>()==null || art.Dragon.GetComponentInChildren<SpriteRenderer>()==null || art.Block.GetComponent<WoolBlockView>().Body.GetComponent<SpriteRenderer>()==null)
                    throw new InvalidOperationException("Character or block sprite is missing");
```

**Sau**
```csharp
                if(art.Kitten.GetComponentInChildren<SpriteRenderer>()==null || art.Dragon.GetComponentInChildren<SpriteRenderer>()==null || art.Block.GetComponent<WoolBlockView>().Body.GetComponent<MeshRenderer>()==null)
                    throw new InvalidOperationException("Character sprite or block mesh is missing");
```

- [ ] **Bước 13: GREEN editor**

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolAssetsBuilder.Build(out var art); JamJam.Editor.WoolArtChecks.Textures() + " | " + JamJam.Editor.WoolArtChecks.Blocks()'
```
Kỳ vọng: compile 0 lỗi; `TEXTURES PASS ... | BLOCKS PASS / N vertices per block`.

- [ ] **Bước 14: GREEN Play Mode**

```bash
unity command start --caller plugin --skill unity-cli
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(1); "ok"'
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.PlayBlocks()'
unity command eval --caller plugin --skill unity-cli --no-banner '"batches " + UnityEditor.UnityStats.batches + " draw " + UnityEditor.UnityStats.drawCalls + " setpass " + UnityEditor.UnityStats.setPassCalls'
unity command stop --caller plugin --skill unity-cli
```
Kỳ vọng: `PLAY BLOCKS PASS / 15 blocks` (level 2 có 15 block, gồm khối 1×2); ghi số draw call vào `drawcalls.txt` (nhãn `T2 L2`). Không lỗi console mới. Harness: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet` → `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions`.

- [ ] **Bước 15: Dừng, báo user.**

---

### Task 3: Thân rồng chevron, đường nỉ, sợi chỉ xoắn

**Files:** tạo `WoolArtChecks.Chevron.cs`; sửa `WoolAssetsBuilder.cs` (300–313), `WoolScene.cs` (52, 137, 351–360).

**Interfaces — Consumes:** Task 1 (`Road.mat` + FeltPath, `Thread_i` + Thread, `orange`). **Produces:** hằng `ChevronWidth`, `ChevronSweep`, `ChevronBand`, `ChevronHeight`, `ChevronBase`, `ChevronRound`, `ChevronUv`; mesh `Chevron.asset`; prefab `DragonSegment` con `YarnChevron`; `WoolScene` hằng `ThreadStartWidth`, `ThreadEndWidth`; UV đường v = khoảng cách thật; `WoolArtChecks.Chevron()`.

- [ ] **Bước 1: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.Chevron.cs`**

```csharp
using System;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Chevron()
        {
            LoadArt(out var art);
            var filter = art.Segment.GetComponentInChildren<MeshFilter>();
            Expect(filter != null && filter.name.StartsWith("Yarn", StringComparison.Ordinal), "Segment renderer keeps the Yarn prefix");
            var mesh = filter.sharedMesh;
            Expect(mesh != null && mesh.name == "Chevron", "Segment uses the Chevron mesh");
            Expect(Mathf.Abs(mesh.bounds.size.x * 1.15f - 1f) < 0.05f, "Chevron world width ~1.0 but was " + mesh.bounds.size.x * 1.15f);
            Expect(mesh.tangents.Length == mesh.vertexCount, "Chevron tangents for the knit normal map");
            Expect(mesh.bounds.max.y < 0 && mesh.bounds.min.y < -0.25f, "Chevron sits on the road under the 0.31 lift");
            ExpectOutward(mesh, "Chevron");
            return "CHEVRON PASS / " + mesh.vertexCount + " vertices";
        }
    }
}
```

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.Chevron()'
```
Kỳ vọng RED: `ART CHECK FAILED: Segment uses the Chevron mesh`.

- [ ] **Bước 2: Mesh chevron và prefab `DragonSegment`** (tip chữ V hướng +Z = chiều đi của rồng; dải dày `ChevronBand` theo Z nên các mảnh lồng nhau thành xương cá; mép hạ xuống `ChevronBase` để kẽ tối)

**Trước**
```300:313:Assets/JamJam/Editor/WoolAssetsBuilder.cs
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
```

**Sau**
```csharp
        public const float ChevronWidth = 0.87f;
        public const float ChevronSweep = 0.3f;
        public const float ChevronBand = 0.36f;
        public const float ChevronHeight = 0.24f;
        public const float ChevronBase = -0.27f;
        public const float ChevronRound = 0.35f;
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
            Part(go.transform, "YarnChevron", chevron, Vector3.zero, Vector3.one, orange, out _);
            SavePrefab(go, "DragonSegment", out prefab);
        }
```

`BuildLeaf` (dòng 42) giữ lại vì `WoolEditorChecks.RunBatch` vẫn kiểm `KnittedScale.asset`; `LayeredScales.asset` thôi được dùng nhưng không xóa.

- [ ] **Bước 3: `WoolScene` — hằng độ dày sợi chỉ**

**Trước**
```52:52:Assets/JamJam/Runtime/WoolScene.cs
        const float Cell = 1f;
```

**Sau**
```csharp
        const float Cell = 1f;
        const float ThreadStartWidth = 0.1f;
        const float ThreadEndWidth = 0.07f;
```

- [ ] **Bước 4: `WoolScene` — LineRenderer dày hơn, texture Tile** (đường cong võng dòng 284–289 giữ nguyên)

**Trước**
```137:137:Assets/JamJam/Runtime/WoolScene.cs
                line.positionCount = 20; line.startWidth = 0.045f; line.endWidth = 0.025f;
```

**Sau**
```csharp
                line.positionCount = 20; line.startWidth = ThreadStartWidth; line.endWidth = ThreadEndWidth; line.textureMode = LineTextureMode.Tile;
```

- [ ] **Bước 5: `WoolScene` — UV đường theo chiều dài thật**

**Trước**
```351:360:Assets/JamJam/Runtime/WoolScene.cs
            var vertices = new Vector3[path.Count * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(path.Count - 1) * 6];
            for (var i = 0; i < path.Count; i++)
            {
                var tangent = path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)];
                var right = Vector3.Cross(Vector3.up, tangent).normalized * halfWidth;
                vertices[i * 2] = path[i] - right + Vector3.up * height;
                vertices[i * 2 + 1] = path[i] + right + Vector3.up * height;
                uv[i * 2] = new Vector2(0, i * 0.15f); uv[i * 2 + 1] = new Vector2(1, i * 0.15f);
```

**Sau**
```csharp
            var vertices = new Vector3[path.Count * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(path.Count - 1) * 6];
            var distance = 0f;
            for (var i = 0; i < path.Count; i++)
            {
                if (i > 0) distance += Vector3.Distance(path[i - 1], path[i]);
                var tangent = path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)];
                var right = Vector3.Cross(Vector3.up, tangent).normalized * halfWidth;
                vertices[i * 2] = path[i] - right + Vector3.up * height;
                vertices[i * 2 + 1] = path[i] + right + Vector3.up * height;
                uv[i * 2] = new Vector2(0, distance); uv[i * 2 + 1] = new Vector2(1, distance);
```

- [ ] **Bước 6: GREEN**

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolAssetsBuilder.Build(out var art); JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Blocks()'
unity command start --caller plugin --skill unity-cli
unity command eval --caller plugin --skill unity-cli --no-banner 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(0); for(var i=0;i<s.State.Blocks.Count;i++) if(s.State.CanSelect(i)&&s.State.HasVisibleColor(s.State.Blocks[i].Definition.Color)){s.Select(i);break;} "selected"'
```
Đợi ~2 giây (block bay tới khay, ống chỉ bắt đầu hút), rồi:
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'var lines=UnityEngine.Object.FindObjectsByType<UnityEngine.LineRenderer>(UnityEngine.FindObjectsSortMode.None); var tile=0; var on=0; foreach(var l in lines){ if(l.textureMode==UnityEngine.LineTextureMode.Tile&&l.startWidth>=0.09f) tile++; if(l.enabled) on++; } var road=0f; foreach(var f in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshFilter>(UnityEngine.FindObjectsSortMode.None)) if(f.name=="DragonPath") road=UnityEngine.Mathf.Max(road,f.sharedMesh.uv[f.sharedMesh.uv.Length-1].y); var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); "threads tile " + tile + "/" + lines.Length + " enabled " + on + " roadV " + road.ToString("F2") + " pathLength " + s.Levels[s.CurrentLevel].Data.PathLength.ToString("F2")'
unity command game_view --caller plugin --skill unity-cli --source camera --overlay true --width 1080 --height 1920 --output-path "$SCRATCH/art/t3-collecting.png"
unity command stop --caller plugin --skill unity-cli
```
Kỳ vọng: `CHEVRON PASS ... | BLOCKS PASS ...`; `threads tile 6/6 enabled ≥1 roadV X pathLength X` với `roadV` bằng `pathLength` (sai lệch ≤0.01). Xem ảnh: chevron lồng nhau liền mạch, hai đường khâu trắng dọc đường, sợi chỉ có vân xoắn.

- [ ] **Bước 7: Dừng, báo user** kèm ảnh `t3-collecting.png`.

---

### Task 4: Ống chỉ, pad khay, bong bóng mèo

**Files:** tạo `WoolBubble.shader`, `WoolArtChecks.Tray.cs`; sửa `WoolAssetsBuilder.cs` (66, 243–248, 255, 287–298, 322–327, 404, 411), `WoolScene.cs` (265), `WoolEditorChecks.cs` (127–128).

**Interfaces — Consumes:** Task 1 (`wood`, `cream`, `white`, `art.Pad`, `art.PadLocked`), Task 2 (`ShaderMat`). **Produces:** hằng `CameraPitch = 62f`, `SpoolFlange`, `SpoolCoilRadius`, `SpoolTube`, `SpoolCoilLength`, `SpoolTurns`, `BubbleScale`; prefab `Spool` con `Core` (Wood), `Flange`×2 (Ivory), `YarnWrap` (nhuộm), `Counter` (TextMesh trắng); prefab `SpoolSlot` dùng `Pad`, nhãn `+` trắng 55%; prefab `Kitten` thêm `Bubble` (sphere, `Bubble.mat`, shader `JamJam/WoolBubble`, sortingOrder 31); `WoolArtChecks.Tray()`.

- [ ] **Bước 1: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.Tray.cs`**

```csharp
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
            Expect(core.GetComponent<MeshRenderer>().sharedMaterial == wood && flange.GetComponent<MeshRenderer>().sharedMaterial == ivory, "Spool wood core and cream ends");
            var counter = art.Spool.GetComponentInChildren<TextMesh>();
            Expect(counter != null && Near(counter.color, Color.white), "Spool counter is white");
            Expect(Mathf.Abs(counter.transform.localEulerAngles.x - 62f) < 0.5f, "Counter tilted to the 62 degree camera");
            ExpectColor(art.Slot.GetComponent<MeshRenderer>().sharedMaterial, "#A9B6CF", "Slot pad");
            var slotLabel = art.Slot.GetComponent<WoolSlotView>().Label;
            Expect(slotLabel.text == "+" && slotLabel.color.a < 0.7f, "Locked slot shows a faint plus");
            var bubble = art.Kitten.transform.Find("Bubble");
            Expect(bubble != null, "Kitten bubble exists");
            var shader = bubble.GetComponent<MeshRenderer>().sharedMaterial.shader;
            Expect(shader.name == "JamJam/WoolBubble" && !ShaderUtil.ShaderHasError(shader), "Bubble shader compiles");
            Expect(art.Kitten.transform.Find("Body").GetComponent<SpriteRenderer>() != null, "Kitten sprite kept");
            return "TRAY PASS";
        }
    }
}
```

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.Tray()'
```
Kỳ vọng RED: `ART CHECK FAILED: Spool has Core, Flange and YarnWrap`.

- [ ] **Bước 2: Tạo `Assets/JamJam/Shaders/WoolBubble.shader`**

```hlsl
Shader "JamJam/WoolBubble"
{
    Properties
    {
        _Tint("Tint",Color)=(0.75,0.9,1,0.06)
        _RimColor("Rim",Color)=(0.85,0.95,1,0.8)
        _RimPower("Rim Power",Range(0.5,8))=2.5
        _HighlightColor("Highlight",Color)=(1,1,1,0.9)
        _HighlightDirection("Highlight Direction",Vector)=(-0.45,0.55,0.7,0)
        _HighlightPower("Highlight Power",Range(4,256))=48
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Bubble"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _RimColor;
                half _RimPower;
                half4 _HighlightColor;
                float4 _HighlightDirection;
                half _HighlightPower;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                float3 normalOS:NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 normalWS:TEXCOORD0;
                float3 viewWS:TEXCOORD1;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.viewWS=GetWorldSpaceNormalizeViewDir(positionWS);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 normalWS=normalize(input.normalWS);
                float3 viewWS=normalize(input.viewWS);
                half rim=pow(1.0h-saturate(dot(normalWS,viewWS)),_RimPower);
                float3 normalVS=TransformWorldToViewDir(normalWS,true);
                half spot=pow(saturate(dot(normalVS,normalize(_HighlightDirection.xyz))),_HighlightPower);
                half3 color=lerp(_Tint.rgb,_RimColor.rgb,rim);
                color=lerp(color,_HighlightColor.rgb,spot);
                half alpha=saturate(_Tint.a+rim*_RimColor.a+spot*_HighlightColor.a);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
```

- [ ] **Bước 3: Cuộn len dày hơn**

**Trước**
```243:248:Assets/JamJam/Editor/WoolAssetsBuilder.cs
                var t = i / (float)rings; var angle = t * Mathf.PI * 2 * 8;
                for (var j = 0; j <= sides; j++)
                {
                    var a = j / (float)sides * Mathf.PI * 2;
                    var radius = 0.22f + Mathf.Cos(a) * 0.034f;
                    vertices.Add(new Vector3((t - 0.5f) * 0.64f + Mathf.Sin(a) * 0.034f, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
```

**Sau**
```csharp
                var t = i / (float)rings; var angle = t * Mathf.PI * 2 * SpoolTurns;
                for (var j = 0; j <= sides; j++)
                {
                    var a = j / (float)sides * Mathf.PI * 2;
                    var radius = SpoolCoilRadius + Mathf.Cos(a) * SpoolTube;
                    vertices.Add(new Vector3((t - 0.5f) * SpoolCoilLength + Mathf.Sin(a) * SpoolTube, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
```

**Trước**
```255:255:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
```

**Sau**
```csharp
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
```

- [ ] **Bước 4: Ống chỉ ngang, đầu kem, lõi gỗ, số trắng dưới ống**

**Trước**
```287:298:Assets/JamJam/Editor/WoolAssetsBuilder.cs
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
```

**Sau**
```csharp
        public const float CameraPitch = 62f;
        public const float SpoolFlange = 0.58f;
        public const float SpoolCoilRadius = 0.2f;
        public const float SpoolTube = 0.05f;
        public const float SpoolCoilLength = 0.74f;
        public const int SpoolTurns = 9;
        public const float BubbleScale = 1.75f;

        static void MakeSpool(out GameObject prefab)
        {
            var go = new GameObject("Spool");
            Part(go.transform, "Core", cylinder, Vector3.zero, new Vector3(0.17f,0.43f,0.17f), wood, out var core); core.localRotation = Quaternion.Euler(0,0,90);
            foreach (var side in new[] { -1f, 1f })
            {
                Part(go.transform, "Flange", cylinder, new Vector3(side*0.43f,0,0), new Vector3(SpoolFlange,0.05f,SpoolFlange), cream, out var flange); flange.localRotation = Quaternion.Euler(0,0,90);
            }
            Part(go.transform, "YarnWrap", coil, Vector3.zero, Vector3.one, orange, out _);
            Label(go.transform, "Counter", "8", new Vector3(0,-0.22f,-0.62f), 0.12f, white, out _);
            SavePrefab(go, "Spool", out prefab);
        }
```

- [ ] **Bước 5: Bong bóng quanh mèo**

**Trước**
```322:327:Assets/JamJam/Editor/WoolAssetsBuilder.cs
        static void MakeKitten(out GameObject prefab)
        {
            var go=new GameObject("Kitten");
            SpritePart(go.transform,"Body",kittenSprite,30,out _);
            SavePrefab(go,"Kitten",out prefab);
        }
```

**Sau**
```csharp
        static void MakeKitten(out GameObject prefab)
        {
            ShaderMat("Bubble", "JamJam/WoolBubble", out var bubbleMaterial);
            var go=new GameObject("Kitten");
            SpritePart(go.transform,"Body",kittenSprite,30,out _);
            Part(go.transform,"Bubble",sphere,new Vector3(0,0.72f,0),Vector3.one*BubbleScale,bubbleMaterial,out var bubble);
            var renderer=bubble.GetComponent<MeshRenderer>(); renderer.sortingOrder=31; renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
            SavePrefab(go,"Kitten",out prefab);
        }
```

- [ ] **Bước 6: Slot dùng Pad, dấu `+` mờ, label theo pitch camera**

**Trước**
```66:66:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            MakeSlot(art.Road, out art.Slot);
```

**Sau**
```csharp
            MakeSlot(art.Pad, out art.Slot);
```

**Trước**
```404:404:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            Label(go.transform,"Unlock","+",new Vector3(0,0.18f,0),0.09f,charcoal,out view.Label);
```

**Sau**
```csharp
            Label(go.transform,"Unlock","+",new Vector3(0,0.18f,0),0.12f,white,out view.Label);
            view.Label.color=new Color(1,1,1,0.55f);
```

**Trước**
```411:411:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            go.transform.localRotation=Quaternion.Euler(58,0,0); go.transform.localScale=Vector3.one*scale;
```

**Sau**
```csharp
            go.transform.localRotation=Quaternion.Euler(CameraPitch,0,0); go.transform.localScale=Vector3.one*scale;
```

- [ ] **Bước 7: `WoolScene` — pad mở/khóa**

**Trước**
```265:265:Assets/JamJam/Runtime/WoolScene.cs
                slots[i].GetComponent<Renderer>().sharedMaterial = i < State.OpenSlots ? Art.Rim : Art.Road;
```

**Sau**
```csharp
                slots[i].GetComponent<Renderer>().sharedMaterial = i < State.OpenSlots ? Art.Pad : Art.PadLocked;
```

- [ ] **Bước 8: `WoolEditorChecks` — cho phép MeshRenderer `Bubble` trong Kitten**

**Trước**
```127:128:Assets/JamJam/Editor/WoolEditorChecks.cs
                if(art.Kitten.GetComponentsInChildren<MeshRenderer>().Length!=0 || art.Dragon.GetComponentsInChildren<MeshRenderer>().Length!=0)
                    throw new InvalidOperationException("Legacy character model is still present");
```

**Sau**
```csharp
                foreach(var renderer in art.Kitten.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.name!="Bubble") throw new InvalidOperationException("Legacy character model is still present");
                if(art.Dragon.GetComponentsInChildren<MeshRenderer>().Length!=0) throw new InvalidOperationException("Legacy character model is still present");
```

- [ ] **Bước 9: GREEN**

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolAssetsBuilder.Build(out var art); JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Blocks() + " | " + JamJam.Editor.WoolArtChecks.Textures()'
unity command start --caller plugin --skill unity-cli
unity command eval --caller plugin --skill unity-cli --no-banner 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(0); for(var i=0;i<s.State.Blocks.Count;i++) if(s.State.CanSelect(i)&&s.State.HasVisibleColor(s.State.Blocks[i].Definition.Color)){s.Select(i);break;} "selected"'
```
Đợi ~2 giây rồi:
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); var spools=0; var pads=""; foreach(UnityEngine.Transform c in s.World){ if(c.name=="Spool(Clone)"&&c.gameObject.activeSelf) spools++; if(c.name=="SpoolSlot(Clone)") pads+=c.GetComponent<UnityEngine.Renderer>().sharedMaterial.name=="Pad"?"O":"L"; } "spools " + spools + " pads " + pads + " remaining " + s.State.Remaining + "/" + s.State.Total'
unity command game_view --caller plugin --skill unity-cli --source camera --overlay true --width 1080 --height 1920 --output-path "$SCRATCH/art/t4-tray.png"
unity command stop --caller plugin --skill unity-cli
```
Kỳ vọng: `TRAY PASS | CHEVRON PASS | BLOCKS PASS | TEXTURES PASS`; `spools 1 pads OOOOLL remaining R/T` với `R < T` (4 pad mở, 2 pad khóa; ống chỉ đang hút len). Ảnh: ống chỉ nằm ngang đầu kem, cuộn len cùng màu, số trắng trên nền navy, bong bóng trong suốt viền sáng quanh mèo.

- [ ] **Bước 10: Dừng, báo user** kèm ảnh `t4-tray.png`.

---

### Task 5: Môi trường + menu `JamJam/Rebuild Art`

**Files:** tạo `WoolArtChecks.Stage.cs`, `WoolArtBuilder.cs`; sửa `WoolSceneBuilder.cs` (41–56, 68–75).

**Interfaces — Consumes:** Task 1 (`Ground.mat`, `Cushion.mat`), Task 4 (`WoolAssetsBuilder.CameraPitch`). **Produces:**
- `public static class WoolArtBuilder { const string ScenePath; const string ProfilePath; const float CameraSize, CameraDistance, Saturation, BloomThreshold, BloomIntensity, VignetteIntensity, SunIntensity, ShadowStrength; static readonly Vector3 CameraTarget; static readonly Color Ambient; [MenuItem("JamJam/Rebuild Art")] static void Rebuild(); static void Apply(Camera camera,Light light,Renderer ground,Renderer cushion,Volume volume); }`.
- Asset `Generated/WoolPost.asset` (VolumeProfile: ColorAdjustments, Bloom, Tonemapping Neutral, Vignette); GameObject `Art Volume` (global) trong `WoolJam.unity`; camera có `UniversalAdditionalCameraData.renderPostProcessing = true`.
- `WoolArtChecks.Stage()`.

- [ ] **Bước 1: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.Stage.cs`**

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Stage()
        {
            Expect(SceneManager.GetActiveScene().path == "Assets/Scenes/WoolJam.unity", "WoolJam scene is active");
            var camera = Camera.main;
            Expect(camera != null && camera.orthographic, "Orthographic main camera");
            Expect(Mathf.Abs(camera.transform.eulerAngles.x - 62f) < 0.5f, "Camera pitch 62 but was " + camera.transform.eulerAngles.x.ToString("F2"));
            Expect(camera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData) && cameraData.renderPostProcessing, "Camera post-processing enabled");
            var volumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            Expect(volumes.Length == 1 && volumes[0].isGlobal && volumes[0].sharedProfile != null, "Exactly one global art volume");
            var profile = volumes[0].sharedProfile;
            Expect(profile.TryGet<Tonemapping>(out var tone) && tone.mode.overrideState && tone.mode.value == TonemappingMode.Neutral, "Neutral tonemapping");
            Expect(profile.TryGet<Bloom>(out var bloom) && bloom.threshold.value >= 1f && bloom.intensity.value <= 0.4f, "High threshold low bloom");
            Expect(profile.TryGet<ColorAdjustments>(out var adjust) && adjust.saturation.overrideState && adjust.saturation.value > 0, "Saturation boost");
            Expect(profile.TryGet<Vignette>(out var vignette) && vignette.intensity.value > 0 && vignette.intensity.value <= 0.35f, "Soft vignette");
            MaterialAt("Ground", out var groundMaterial);
            MaterialAt("Cushion", out var cushionMaterial);
            var ground = GameObject.Find("Knitted Ground");
            var cushion = GameObject.Find("Board Cushion");
            Expect(ground != null && ground.GetComponent<Renderer>().sharedMaterial == groundMaterial, "Navy fabric ground");
            Expect(cushion != null && cushion.GetComponent<Renderer>().sharedMaterial == cushionMaterial, "Lighter navy board cushion");
            var sun = GameObject.Find("Soft Sun");
            Expect(sun != null && sun.GetComponent<Light>().shadowStrength <= 0.4f, "Soft low-strength sun shadow");
            Expect(GameObject.Find("Canvas") != null, "UI canvas still present");
            var points = new List<Vector3>();
            for (var i = 1; ; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<WoolLevel>(WoolAssetsBuilder.Root + "/Levels/Level_" + i + ".asset");
                if (level == null) break;
                foreach (var p in level.Path) { points.Add(p + new Vector3(0.66f, 0, 0.66f)); points.Add(p - new Vector3(0.66f, 0, 0.66f)); }
            }
            Expect(points.Count > 0, "Level paths found");
            for (var slot = 0; slot < 6; slot++) points.Add(WoolScene.SlotPosition(slot) + new Vector3(slot < 3 ? -0.56f : 0.56f, 0, -0.62f));
            foreach (var x in new[] { -4.35f, 4.35f })
                foreach (var z in new[] { -8.2f, -1.9f }) points.Add(new Vector3(x, 0, z));
            try
            {
                foreach (var aspect in new[] { 1080f / 1920f, 1080f / 2400f })
                {
                    camera.aspect = aspect;
                    foreach (var point in points)
                    {
                        var view = camera.WorldToViewportPoint(point);
                        Expect(view.x > 0.02f && view.x < 0.98f && view.y > 0.12f && view.y < 0.84f, "Point " + point + " outside frame at aspect " + aspect.ToString("F3") + " viewport " + view);
                    }
                }
            }
            finally { camera.ResetAspect(); }
            return "STAGE PASS / " + points.Count + " framed points at 1080x1920 and 1080x2400";
        }
    }
}
```

Khoảng viewport `y ∈ (0.12, 0.84)` lấy từ khung hiện tại (đỉnh đường ở 0.814, đáy board ở 0.141 với pitch 52°/size 9.9) để chừa header/footer UI.

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.Stage()'
```
Kỳ vọng RED (scene WoolJam đang mở): `ART CHECK FAILED: Camera pitch 62 but was 52.13`.

- [ ] **Bước 2: Tạo `Assets/JamJam/Editor/WoolArtBuilder.cs`**

```csharp
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace JamJam.Editor
{
    public static class WoolArtBuilder
    {
        public const string ScenePath = "Assets/Scenes/WoolJam.unity";
        public const string ProfilePath = WoolAssetsBuilder.Root + "/WoolPost.asset";
        public const float CameraSize = 11.1f;
        public const float CameraDistance = 24f;
        public const float Saturation = 15f;
        public const float BloomThreshold = 1.1f;
        public const float BloomIntensity = 0.25f;
        public const float VignetteIntensity = 0.22f;
        public const float SunIntensity = 1f;
        public const float ShadowStrength = 0.3f;
        public static readonly Vector3 CameraTarget = new Vector3(0, 0, 0.8f);
        public static readonly Color Ambient = new Color(0.66f, 0.7f, 0.8f);
        static readonly Color Navy = new Color32(0x14, 0x23, 0x4A, 0xFF);

        [MenuItem("JamJam/Rebuild Art")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding art");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before rebuilding art");
            WoolAssetsBuilder.Build(out _);
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Find("Main Camera", out var cameraObject);
            Find("Soft Sun", out var sunObject);
            Find("Knitted Ground", out var ground);
            Find("Board Cushion", out var cushion);
            var volume = UnityEngine.Object.FindFirstObjectByType<Volume>();
            if (volume == null) volume = new GameObject("Art Volume").AddComponent<Volume>();
            Apply(cameraObject.GetComponent<Camera>(), sunObject.GetComponent<Light>(), ground.GetComponent<Renderer>(), cushion.GetComponent<Renderer>(), volume);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Wool art rebuilt");
        }

        public static void Apply(Camera camera, Light light, Renderer ground, Renderer cushion, Volume volume)
        {
            camera.orthographic = true; camera.orthographicSize = CameraSize;
            camera.transform.rotation = Quaternion.Euler(WoolAssetsBuilder.CameraPitch, 0, 0);
            camera.transform.position = CameraTarget - camera.transform.forward * CameraDistance;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Navy;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 80;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            light.type = LightType.Directional; light.intensity = SunIntensity; light.color = new Color(1, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft; light.shadowStrength = ShadowStrength; light.shadowBias = 0.03f;
            light.transform.rotation = Quaternion.Euler(58, -28, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Ambient; RenderSettings.ambientIntensity = 1;
            RenderSettings.skybox = null;
            ground.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(WoolAssetsBuilder.Root + "/Materials/Ground.mat");
            cushion.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(WoolAssetsBuilder.Root + "/Materials/Cushion.mat");
            Profile(out var profile);
            volume.name = "Art Volume"; volume.isGlobal = true; volume.priority = 1; volume.sharedProfile = profile;
            EditorUtility.SetDirty(camera); EditorUtility.SetDirty(camera.transform); EditorUtility.SetDirty(light); EditorUtility.SetDirty(light.transform);
            EditorUtility.SetDirty(ground); EditorUtility.SetDirty(cushion); EditorUtility.SetDirty(volume);
        }

        static void Profile(out VolumeProfile profile)
        {
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
            Effect<ColorAdjustments>(profile, out var adjust); adjust.saturation.Override(Saturation);
            Effect<Bloom>(profile, out var bloom); bloom.threshold.Override(BloomThreshold); bloom.intensity.Override(BloomIntensity); bloom.scatter.Override(0.6f);
            Effect<Tonemapping>(profile, out var tone); tone.mode.Override(TonemappingMode.Neutral);
            Effect<Vignette>(profile, out var vignette); vignette.intensity.Override(VignetteIntensity); vignette.smoothness.Override(0.45f); vignette.color.Override(new Color(0.02f, 0.04f, 0.1f));
            EditorUtility.SetDirty(profile);
            foreach (var component in profile.components) EditorUtility.SetDirty(component);
        }

        static void Effect<T>(VolumeProfile profile, out T effect) where T : VolumeComponent
        {
            if (profile.TryGet(out effect)) return;
            effect = profile.Add<T>();
            effect.name = typeof(T).Name;
            effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(effect, profile);
        }

        static void Find(string name, out GameObject found)
        {
            found = GameObject.Find(name);
            if (found == null) throw new InvalidOperationException(name + " is missing from " + ScenePath);
        }
    }
}
```

- [ ] **Bước 3: `WoolSceneBuilder.Build` dùng cùng môi trường** (chỉ sửa code, KHÔNG chạy `Build`)

**Trước**
```41:56:Assets/JamJam/Editor/WoolSceneBuilder.cs
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=9.9f;
            camera.transform.position=new Vector3(0,19,-14); camera.transform.LookAt(new Vector3(0,0,0.8f));
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0.97f,0.91f,0.78f); camera.nearClipPlane=0.1f; camera.farClipPlane=80;
            cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<PhysicsRaycaster>();
            var lightObject=new GameObject("Soft Sun"); var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional;
            light.intensity=1.05f; light.color=new Color(1,0.94f,0.83f); light.shadows=LightShadows.Soft; light.shadowStrength=0.35f; light.shadowBias=0.03f;
            light.transform.rotation=Quaternion.Euler(48,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(0.5f,0.55f,0.6f); RenderSettings.ambientIntensity=0.8f;
            RenderSettings.skybox=null;
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name="Knitted Ground";
            ground.transform.position=new Vector3(0,-0.12f,0); ground.transform.localScale=new Vector3(35,0.16f,40);
            var floor=new Material(art.Road); floor.name="PastelGround"; floor.SetColor("_BaseColor",new Color(0.86f,0.8f,0.67f)); floor.SetTextureScale("_BaseMap",new Vector2(15,20)); floor.SetTextureScale("_BumpMap",new Vector2(15,20));
            SaveMaterial(floor,"Ground",out var floorMaterial); ground.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(WoolAssetsBuilder.Root+"/Meshes/Rounded.asset");
            WoolAssetsBuilder.Part(null,"Board Cushion",mesh,new Vector3(0,-0.01f,-5.05f),new Vector3(8.7f,0.18f,6.3f),art.Rim,out _);
```

**Sau**
```csharp
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<PhysicsRaycaster>();
            var lightObject=new GameObject("Soft Sun"); var light=lightObject.AddComponent<Light>();
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name="Knitted Ground";
            ground.transform.position=new Vector3(0,-0.12f,0); ground.transform.localScale=new Vector3(35,0.16f,40);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(WoolAssetsBuilder.Root+"/Meshes/Rounded.asset");
            WoolAssetsBuilder.Part(null,"Board Cushion",mesh,new Vector3(0,-0.01f,-5.05f),new Vector3(8.7f,0.18f,6.3f),art.Rim,out var cushion);
            var volume=new GameObject("Art Volume").AddComponent<Volume>();
            WoolArtBuilder.Apply(camera,light,ground.GetComponent<Renderer>(),cushion.GetComponent<Renderer>(),volume);
```

**Trước**
```68:75:Assets/JamJam/Editor/WoolSceneBuilder.cs
        static void SaveMaterial(Material source,string name,out Material material)
        {
            var path=WoolAssetsBuilder.Root+"/Materials/"+name+".mat";
            material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=source; AssetDatabase.CreateAsset(material,path); }
            else { EditorUtility.CopySerialized(source,material); UnityEngine.Object.DestroyImmediate(source); }
        }

```

**Sau:** xóa method `SaveMaterial` và dòng trống sau nó (không còn nơi gọi).

- [ ] **Bước 4: Compile** — `unity recompile`. Kỳ vọng 0 lỗi, không cảnh báo mới trong file đã sửa.

- [ ] **Bước 5: Chạy `Rebuild Art` lần 1** (lưu mọi scene đang mở trước)

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtBuilder.Rebuild(); JamJam.Editor.WoolArtChecks.Stage() + " | " + JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Blocks() + " | " + JamJam.Editor.WoolArtChecks.Textures()'
cp Assets/Scenes/WoolJam.unity "$SCRATCH/art/WoolJam.rebuild1.unity"
md5sum Assets/JamJam/Generated/Materials/*.mat Assets/JamJam/Generated/Textures/*.png Assets/JamJam/Generated/Meshes/*.asset Assets/JamJam/Generated/WoolPost.asset > "$SCRATCH/art/assets.rebuild1.md5"
```
Kỳ vọng: log `Wool art rebuilt`; năm dòng PASS.

- [ ] **Bước 6: Chạy lần 2 — idempotent, GUID, level, UI**

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtBuilder.Rebuild(); JamJam.Editor.WoolArtChecks.Stage()'
cmp Assets/Scenes/WoolJam.unity "$SCRATCH/art/WoolJam.rebuild1.unity" && echo SCENE SAME
md5sum -c --quiet "$SCRATCH/art/assets.rebuild1.md5" && echo ASSETS SAME
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders --include=*.meta | sort > "$SCRATCH/art/guids.now.txt"
comm -23 "$SCRATCH/art/guids.before.txt" "$SCRATCH/art/guids.now.txt"
md5sum -c "$SCRATCH/art/levels.md5"
diff <(grep "m_Name:" "$SCRATCH/art/WoolJam.before.unity" | sort) <(grep "m_Name:" Assets/Scenes/WoolJam.unity | sort)
diff <(grep "m_Text:" "$SCRATCH/art/WoolJam.before.unity") <(grep "m_Text:" Assets/Scenes/WoolJam.unity)
```
Kỳ vọng: `STAGE PASS`; `SCENE SAME`; `ASSETS SAME`; `comm` rỗng; ba level `OK`; diff `m_Name` chỉ có dòng `>` (thêm `Art Volume` và các `m_Name: ` rỗng của component mới `Volume`/`UniversalAdditionalCameraData`), không có dòng `<`; diff `m_Text` rỗng. Prefab có thể khác fileID nội bộ giữa hai lần (SaveAsPrefabAsset) nhưng GUID giữ nguyên và các check prefab PASS.

- [ ] **Bước 7: Dừng, báo user.**

---

### Task 6: Tài liệu prompt nhân vật + builder fallback sprite mới

**Files:** tạo `docs/wool-character-prompts.md`; sửa `WoolAssetsBuilder.cs` (344–345).

**Interfaces — Produces:** builder dùng `Assets/JamJam/Art/Sprites/Kitten-Idle.png` / `Dragon-Head-Idle.png` nếu tồn tại, ngược lại `Kitten-Wool.png` / `Dragon-Head-Wool.png`. Các trạng thái Scared/Happy/Fire chỉ được tài liệu hóa, dùng ở phần 2.

- [ ] **Bước 1: Fallback trong `BuildSpriteAssets`**

**Trước**
```344:345:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            ImportSprite("Assets/JamJam/Art/Sprites/Kitten-Wool.png",1.5f,new Vector2(0.5f,0.06f),Vector4.zero,out kittenSprite);
            ImportSprite("Assets/JamJam/Art/Sprites/Dragon-Head-Wool.png",2.1f,new Vector2(0.35f,0.7f),Vector4.zero,out dragonSprite);
```

**Sau**
```csharp
            SpritePath("Kitten-Idle.png","Kitten-Wool.png",out var kittenPath);
            SpritePath("Dragon-Head-Idle.png","Dragon-Head-Wool.png",out var dragonPath);
            ImportSprite(kittenPath,1.5f,new Vector2(0.5f,0.06f),Vector4.zero,out kittenSprite);
            ImportSprite(dragonPath,2.1f,new Vector2(0.35f,0.7f),Vector4.zero,out dragonSprite);
            static void SpritePath(string preferred,string fallback,out string path)
            {
                const string folder="Assets/JamJam/Art/Sprites/";
                path=File.Exists(folder+preferred)?folder+preferred:folder+fallback;
            }
```

- [ ] **Bước 2: Tạo `docs/wool-character-prompts.md`**

```markdown
# Prompt sprite nhân vật Wool Jam

Tài liệu cho user tự tạo sprite nhân vật mới (AI image hoặc vẽ tay) rồi thả vào `Assets/JamJam/Art/Sprites/`. Builder (`JamJam/Rebuild Art`) tự dùng file mới nếu tồn tại; nếu chưa có thì giữ `Kitten-Wool.png` và `Dragon-Head-Wool.png`.

## Quy cách chung

- PNG 32-bit, nền trong suốt hoàn toàn, không bóng đổ xuống nền, không chữ, không viền trắng quanh nhân vật.
- 1024×1024 px, nhân vật nằm giữa, chừa lề trong suốt ~6% mỗi cạnh.
- Phong cách: đồ chơi len 3D dễ thương, bề mặt đan len/nỉ thấy rõ sợi, ánh sáng studio mềm từ trên-trái, màu bão hòa, đường viền sẫm mỏng giúp đọc rõ trên nền navy `#14234A`.
- Không dùng các màu len gameplay (đỏ `#E8352F`, vàng `#FFCE1F`, xanh lá `#3CC03A`, cyan `#22CFE0`, xanh dương `#2F74EA`, hồng `#F04FAE`) làm màu chủ đạo của nhân vật; nhân vật dùng tông cam/kem.
- Các trạng thái của cùng nhân vật phải cùng kích thước, cùng tỉ lệ, cùng vị trí chân/cổ để đổi sprite không bị nhảy.

## Mèo con

Pivot `(0.5, 0.06)`: chân mèo chạm điểm cách đáy ảnh 6%, chính giữa theo chiều ngang. Mèo cao ~85% chiều cao ảnh. Builder hiển thị cao 1.5 đơn vị, bên trong bong bóng trong suốt.

| File | Trạng thái | Dùng ở |
|---|---|---|
| `Kitten-Idle.png` | ngồi, nhìn thẳng, mỉm cười nhẹ | phần 1 (idle) |
| `Kitten-Scared.png` | ngồi co lại, tai cụp, mắt to lo lắng | phần 2 |
| `Kitten-Happy.png` | hai chân trước giơ lên, mắt cười híp | phần 2 |

Prompt gốc (thêm mô tả trạng thái ở cuối):

> Cute chibi orange tabby kitten made of knitted wool yarn, visible knit stitches and soft felt texture, 3D toy render, sitting, front view, big glossy eyes, small pink nose, cream belly and paws, soft studio lighting from top left, saturated warm colors, thin dark outline, centered, full body, transparent background, no shadow, no text, 1024x1024

- Idle: `sitting calmly, gentle smile, looking at the viewer`
- Scared: `crouching, ears flattened, worried wide eyes, tiny sweat drop`
- Happy: `both front paws raised in joy, eyes closed in a big smile`

## Đầu rồng

Pivot `(0.35, 0.7)`: điểm nối cổ với thân len nằm cách mép trái 35% và cách đáy 70%. Đầu rồng nhìn sang phải theo cùng hướng và bố cục với `Dragon-Head-Wool.png` hiện tại, vì `WoolScene` cộng thêm 55° khi xoay đầu theo hướng đường. Builder hiển thị cao 2.1 đơn vị.

| File | Trạng thái | Dùng ở |
|---|---|---|
| `Dragon-Head-Idle.png` | ngậm miệng, mắt mở, nhìn về phía trước | phần 1 (idle) |
| `Dragon-Head-Fire.png` | há miệng, phun lửa nhỏ màu cam-vàng | phần 2 |

Prompt gốc:

> Cute chibi dragon head made of knitted orange wool yarn, visible knit stitches, small cream horns and yellow felt spikes, big friendly eyes, 3D toy render, three-quarter side view facing right, soft studio lighting from top left, saturated warm colors, thin dark outline, head and short neck only, transparent background, no shadow, no text, 1024x1024

- Idle: `mouth closed, calm confident expression`
- Fire: `mouth wide open breathing a small stylized orange and yellow flame toward the right`

## Kiểm tra sau khi thả file

1. Copy file đúng tên vào `Assets/JamJam/Art/Sprites/`.
2. Chạy menu `JamJam/Rebuild Art` (không chạy `Build Wool Jam Scene`).
3. Play `WoolJam.unity`: mèo đứng giữa bong bóng, chân ở đáy bong bóng; đầu rồng nối liền với đốt chevron đầu tiên và xoay mượt theo đường.
```

- [ ] **Bước 3: Kiểm fallback bằng file tạm** (asset tạm mới, không đụng GUID cũ)

```bash
unity recompile
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolAssetsBuilder.Build(out var art); UnityEditor.AssetDatabase.GetAssetPath(art.Kitten.GetComponentInChildren<UnityEngine.SpriteRenderer>().sprite)'
cp Assets/JamJam/Art/Sprites/Kitten-Wool.png Assets/JamJam/Art/Sprites/Kitten-Idle.png
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEditor.AssetDatabase.Refresh(); JamJam.Editor.WoolAssetsBuilder.Build(out var art); UnityEditor.AssetDatabase.GetAssetPath(art.Kitten.GetComponentInChildren<UnityEngine.SpriteRenderer>().sprite)'
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEditor.AssetDatabase.DeleteAsset("Assets/JamJam/Art/Sprites/Kitten-Idle.png"); JamJam.Editor.WoolAssetsBuilder.Build(out var art); UnityEditor.AssetDatabase.GetAssetPath(art.Kitten.GetComponentInChildren<UnityEngine.SpriteRenderer>().sprite)'
```
Kỳ vọng lần lượt: `.../Kitten-Wool.png`, `.../Kitten-Idle.png`, `.../Kitten-Wool.png`. Sau đó `comm -23` GUID (lệnh Task 5 bước 6) rỗng.

- [ ] **Bước 4: Dừng, báo user.**

---

### Task 7: Kiểm thử tổng + vòng tinh chỉnh hình ảnh

**Files:** không sửa source; chỉ đổi giá trị các hằng tinh chỉnh (bảng dưới) nếu cần, rồi chạy lại `JamJam/Rebuild Art`. Tạo script `$SCRATCH/compare.py`, ảnh `docs/art-gameplay-*.png`, `docs/art-compare-*.png`.

**Knob được phép chỉnh** (không đụng `WoolGame`, `WoolRouting`, vị trí lưới/slot/đường, collider, palette):

| Nơi | Hằng |
|---|---|
| `WoolAssetsBuilder` | `KnitColumns`, `KnitRows`, `KnitFloor`, `FabricColumns`, `FabricRows`, `FeltStitchInset`, `FeltDashes`, `ThreadPly`, `YarnTiling`, `ThreadTiling`, `RoadTiling`, `GroundTiling`, `CushionTiling`, `BlockSideShade`, `BraidDensity`, `BraidDark`, `LightWrap`, `ArrowRimLift`, `ArrowFaceLift`, `ChevronWidth`, `ChevronSweep`, `ChevronBand`, `ChevronHeight`, `ChevronBase`, `ChevronRound`, `ChevronUv`, `SpoolFlange`, `SpoolCoilRadius`, `SpoolTube`, `SpoolCoilLength`, `SpoolTurns`, `BubbleScale`; màu `RoadRim`, `PadLocked`, `Cushion`, `Wood` trong khối `Mat(...)` |
| `WoolBlockMesh` | `Corner` (phải > `Bevel + Border`), `Bevel`, `Border` (giữ `Thickness≈0.35`, `Inset`) |
| `WoolArtBuilder` | `CameraSize`, `CameraDistance`, `Saturation`, `BloomThreshold` (≥1), `BloomIntensity`, `VignetteIntensity`, `SunIntensity`, `ShadowStrength`, `Ambient` (giữ pitch 62° ± 3°) |
| `WoolScene` | `ThreadStartWidth`, `ThreadEndWidth` |
| `WoolBubble.shader` | default `_Tint`, `_RimColor`, `_RimPower`, `_HighlightPower` |

- [ ] **Bước 1: Tạo `$SCRATCH/compare.py`**

```python
import sys
from PIL import Image


def fit(path, height):
    image = Image.open(path).convert("RGB")
    width = round(image.width * height / image.height)
    return image.resize((width, height), Image.LANCZOS)


output = sys.argv[1]
height = 1280
gap = 24
images = [fit(path, height) for path in sys.argv[2:]]
canvas = Image.new("RGB", (sum(image.width for image in images) + gap * (len(images) - 1), height), (20, 35, 74))
x = 0
for image in images:
    canvas.paste(image, (x, 0))
    x += image.width + gap
canvas.save(output)
print(output, canvas.size)
```

- [ ] **Bước 2: Chụp Play Mode** (`WoolJam.unity` mở, Game view hiển thị). Với mỗi trạng thái chụp hai cỡ:

```bash
unity command start --caller plugin --skill unity-cli
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(1); "ok"'
unity command game_view --caller plugin --skill unity-cli --source camera --overlay true --width 1080 --height 1920 --output-path docs/art-gameplay-start.png
unity command game_view --caller plugin --skill unity-cli --source camera --overlay true --width 1080 --height 2400 --output-path docs/art-gameplay-start-tall.png
unity command eval --caller plugin --skill unity-cli --no-banner '"batches " + UnityEditor.UnityStats.batches + " draw " + UnityEditor.UnityStats.drawCalls + " setpass " + UnityEditor.UnityStats.setPassCalls'
unity command eval --caller plugin --skill unity-cli --no-banner 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); for(var i=0;i<s.State.Blocks.Count;i++) if(s.State.CanSelect(i)&&s.State.HasVisibleColor(s.State.Blocks[i].Definition.Color)){s.Select(i);break;} "selected"'
```
Đợi ~2 giây (có sợi chỉ đang hút), chụp `docs/art-gameplay-mid.png` và `docs/art-gameplay-mid-tall.png` bằng hai lệnh `game_view` như trên. Rồi thắng level:
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'var g=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().State; for(var f=0;f<30000&&g.Status==JamJam.RoundStatus.Playing;f++){ for(var b=0;b<g.Blocks.Count;b++) if(g.CanSelect(b)&&g.HasVisibleColor(g.Blocks[b].Definition.Color)){g.Select(b);break;} g.Step(0.02f);} g.Status'
```
Kỳ vọng `Won`; đợi ~1 giây, chụp `docs/art-gameplay-won.png` và `docs/art-gameplay-won-tall.png`. `unity command stop --caller plugin --skill unity-cli`.

So `batches/draw/setpass` (nhãn `final L2`) với baseline Task 1 (L1) và Task 2 (L2): `batches` cuối ≤ 2 × baseline L1. Nếu vượt: kiểm `WoolBlock.mat` có `enableInstancing`, arrow có tắt shadow, rồi báo user (không đổi gameplay để giảm draw call).

- [ ] **Bước 3: Ghép ảnh so sánh**

```bash
PY="$USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe"
REF="$SCRATCH/ref/store"
"$PY" -I "$SCRATCH/compare.py" docs/art-compare-start.png "$SCRATCH/art/before-1080x1920.png" docs/art-gameplay-start.png docs/art-gameplay-start-tall.png "$REF/shot5.png"
"$PY" -I "$SCRATCH/compare.py" docs/art-compare-mid.png docs/art-gameplay-mid.png docs/art-gameplay-mid-tall.png "$REF/shot7.png"
"$PY" -I "$SCRATCH/compare.py" docs/art-compare-won.png docs/art-gameplay-won.png docs/art-gameplay-won-tall.png "$REF/shot4.png"
```
Kỳ vọng: ba file in kích thước, cao 1280.

- [ ] **Bước 4: Checklist tự đánh giá** (đọc ba ảnh compare bằng Read):
  - Len bão hòa đọc rõ trên nền navy; không màu nào chìm vào nền (xanh dương `#2F74EA` là nguy cơ cao nhất).
  - Chevron thấy rõ ở cỡ điện thoại, các mảnh lồng nhau thành dải xương cá, có kẽ tối giữa mảnh.
  - Block lộ mặt bên dày (shade ~70%), có viền bện quanh mặt trên, khe giữa các block.
  - Mũi tên trắng ngà tương phản, viền sẫm thấy được, đúng hướng.
  - Số ống chỉ trắng đọc được; ống chỉ nằm ngang, đầu kem, lõi gỗ.
  - Đường nỉ xám bạc có hai đường chỉ khâu đứt nét; pad xanh xám, ô khóa có `+` mờ; cushion tách khỏi nền.
  - Bloom chỉ chạm viền sáng, không lóa UI; vignette nhẹ.
  Mục nào chưa đạt: chỉnh knob trong bảng, `unity recompile`, chạy `JamJam.Editor.WoolArtBuilder.Rebuild()`, chụp lại. Tối đa 3 vòng, sau đó báo user kèm ảnh để user quyết.

- [ ] **Bước 5: Kiểm gameplay không đổi**

```bash
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolEditorChecks.ValidateLevels(); System.IO.File.ReadAllText("Logs/WoolJam.verify.txt")'
unity command start --caller plugin --skill unity-cli
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(2); "ok"'
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.PlayBlocks()'
unity command eval --caller plugin --skill unity-cli --no-banner 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(0); var g=s.State; for(var i=0;i<30000&&g.Status==JamJam.RoundStatus.Playing;i++) g.Step(0.02f); g.Status'
unity command stop --caller plugin --skill unity-cli
```
Kỳ vọng: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions`; file verify có `LEVEL 1/2/3 SOLUTION PASS`; `PLAY BLOCKS PASS / 24 blocks` ở level 3; level 1 không chạm block → `Lost` và ResultPanel hiện (thua vẫn kích hoạt). Ghi chú: `ValidateLevels` ghi thêm dòng vào `Logs/WoolJam.verify.txt`, không chạm asset.

- [ ] **Bước 6: Kiểm asset cuối**

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtChecks.Stage() + " | " + JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Blocks() + " | " + JamJam.Editor.WoolArtChecks.Textures() + " | sprite shader error " + UnityEditor.ShaderUtil.ShaderHasError(UnityEngine.Shader.Find("JamJam/WoolSprite"))'
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders --include=*.meta | sort > "$SCRATCH/art/guids.now.txt"
comm -23 "$SCRATCH/art/guids.before.txt" "$SCRATCH/art/guids.now.txt"
md5sum -c "$SCRATCH/art/levels.md5"
git status --short
unity command get --caller plugin --skill unity-cli --level error --tail 50
```
Kỳ vọng: năm PASS + `sprite shader error False`; `comm` rỗng; level `OK`; `git status` không có file bị xóa (` D`); không lỗi console mới ngoài lỗi SearchDatabase khởi động có sẵn.

- [ ] **Bước 7: Dừng, báo user** — gửi ba ảnh `docs/art-compare-*.png`, bảng draw call trước/sau, danh sách knob đã chỉnh (nếu có). User duyệt cuối; không commit.

## Tóm tắt

- Tạo 2 runtime/editor builder (`WoolBlockMesh`, `WoolArtBuilder` với menu `JamJam/Rebuild Art`), 2 shader URP (`WoolKnit` instanced cho block, `WoolBubble` fresnel), 6 file check partial `WoolArtChecks` và tài liệu `docs/wool-character-prompts.md`.
- `WoolAssetsBuilder`: palette spec, 5 texture procedural tileable (len đan + normal, vải nền, dải nỉ có chỉ khâu, sợi xoắn), material mới (Pad, PadLocked, Cushion, Wood, WoolBlock, Bubble), mesh block/mũi tên nổi/chevron, ống chỉ ngang, bong bóng mèo, label theo pitch 62°, fallback sprite `*-Idle.png`. Mọi asset cũ ghi đè tại chỗ, GUID giữ nguyên.
- `WoolScene` chỉ đổi phần hiển thị: mesh block theo (W,H) + MPB, bỏ sortingOrder sprite, sợi chỉ dày dạng Tile, UV đường theo chiều dài, pad mở/khóa. Không đổi chọn block, nudge, `ScaleAt`, route, FIFO, collider.
- `WoolSceneBuilder.Build` dùng chung `WoolArtBuilder.Apply` nhưng không được chạy; `WoolEditorChecks.RunBatch` cập nhật cho block mesh và bong bóng.
- Kiểm thử: check editor RED→GREEN mỗi task, Play Mode click/bay/hút len/thắng/thua, harness 153/4121/18, Validate All Levels, GUID/level/scene diff, idempotent hai lần, draw call ≤2×, ảnh so sánh với ảnh store.
