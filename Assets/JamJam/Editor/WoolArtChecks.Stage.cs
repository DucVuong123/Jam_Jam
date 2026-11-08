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
            Expect(SceneManager.GetActiveScene().path == WoolArtBuilder.ScenePath, "WoolJam scene is active");
            var camera = Camera.main;
            Expect(camera != null && camera.orthographic, "Orthographic main camera");
            Expect(Mathf.Abs(camera.transform.eulerAngles.x - WoolStage.Pitch) < 0.5f, "Camera pitch " + WoolStage.Pitch + " but was " + camera.transform.eulerAngles.x.ToString("F2"));
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
            Expect(ground != null && ground.GetComponent<Renderer>().sharedMaterial == groundMaterial, "Cream fabric ground");
            Expect(cushion != null && cushion.GetComponent<Renderer>().sharedMaterial == cushionMaterial, "Deeper cream board cushion");
            ExpectColor(groundMaterial, "#F6E7CF", "Ground");
            MaterialAt("Meadow", out var meadowMaterial);
            ExpectColor(meadowMaterial, "#BFE8C8", "Meadow");
            var meadow = GameObject.Find("Meadow");
            Expect(meadow != null && meadow.GetComponent<Renderer>() != null && meadow.GetComponent<Renderer>().sharedMaterial == meadowMaterial, "Meadow uses Meadow.mat");
            Expect(meadow.GetComponent<Collider>() == null && meadow.GetComponent<Renderer>().shadowCastingMode == ShadowCastingMode.Off, "Meadow has no collider and casts no shadow");
            MaterialAt("Shelf", out var shelfMaterial);
            ExpectColor(shelfMaterial, "#E2B577", "Shelf");
            var shelf = GameObject.Find("Tray Shelf");
            Expect(shelf != null && shelf.GetComponent<Renderer>() != null && shelf.GetComponent<Renderer>().sharedMaterial == shelfMaterial, "Tray Shelf uses Shelf.mat");
            Expect(shelf.GetComponent<Collider>() == null, "Tray Shelf has no collider");
            ColorUtility.TryParseHtmlString("#F6E7CF", out var cream);
            Expect(Near(camera.backgroundColor, cream), "Camera clears to cream #F6E7CF but was #" + ColorUtility.ToHtmlStringRGB(camera.backgroundColor));
            Expect(vignette.color.value.r > vignette.color.value.b, "Warm vignette color");
            var controller = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            Expect(controller != null && controller.Cushion == cushion.transform, "WoolScene sizes the Board Cushion");
            var sun = GameObject.Find("Soft Sun");
            Expect(sun != null && sun.GetComponent<Light>().shadowStrength <= 0.4f, "Soft low-strength sun shadow");
            Expect(GameObject.Find("Canvas") != null, "UI canvas still present");
            var points = new List<Vector3>();
            var routes = 0;
            for (var i = 1; ; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<WoolLevel>(WoolAssetsBuilder.Root + "/Levels/Level_" + i + ".asset");
                if (level == null) break;
                foreach (var p in level.Path) { points.Add(p + new Vector3(0.66f, 0, 0.66f)); points.Add(p - new Vector3(0.66f, 0, 0.66f)); }
                if (level.Path.Length > 1)
                {
                    var last = level.Path[level.Path.Length - 1];
                    var head = last + (last - level.Path[level.Path.Length - 2]).normalized * 0.85f;
                    points.Add(head + new Vector3(1.1f, 0, 1.1f));
                    points.Add(head - new Vector3(1.1f, 0, 1.1f));
                }
                var data = level.Data;
                for (var b = 0; b < data.Blocks.Length; b++)
                    for (var slot = 0; slot < 6; slot++)
                    {
                        Expect(WoolRouting.TryFind(data, new[] { new BlockState { Definition = data.Blocks[b] } }, 0, slot, out var route, out _), "Level " + i + " block " + b + " has a ring route to slot " + slot);
                        var inner = new Vector2(data.Blocks[b].Width - WoolBlockMesh.Inset * 2, data.Blocks[b].Height - WoolBlockMesh.Inset * 2);
                        var radius = 0.5f * WoolRouting.CellSize * inner.magnitude;
                        foreach (var r in route)
                            for (var k = 0; k < 8; k++)
                            {
                                var angle = k * Mathf.PI * 0.25f;
                                points.Add(new Vector3(r.X + Mathf.Cos(angle) * radius, 0, r.Z + Mathf.Sin(angle) * radius));
                            }
                        routes++;
                    }
            }
            for (var shape = 0; shape < 5; shape++)
            {
                WoolPaths.Sample((WoolPathShape)shape, out var sampled);
                foreach (var s in sampled) { points.Add(new Vector3(s.X + 0.66f, 0, s.Z + 0.66f)); points.Add(new Vector3(s.X - 0.66f, 0, s.Z - 0.66f)); }
                var end = new Vector3(sampled[sampled.Count - 1].X, 0, sampled[sampled.Count - 1].Z);
                var before = new Vector3(sampled[sampled.Count - 2].X, 0, sampled[sampled.Count - 2].Z);
                var kitten = end + (end - before).normalized * 0.85f;
                points.Add(kitten + new Vector3(1.1f, 0, 1.1f));
                points.Add(kitten - new Vector3(1.1f, 0, 1.1f));
            }
            Expect(points.Count > 0 && routes > 0, "Level paths and ring routes found");
            for (var slot = 0; slot < 6; slot++) points.Add(WoolScene.SlotPosition(slot) + new Vector3(slot < 3 ? -0.7f : 0.7f, 0, -0.62f));
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            var oldSize = camera.orthographicSize;
            var rotation = Quaternion.Euler(WoolStage.Pitch, 0, 0);
            var position = new Vector3(0, 0, WoolStage.TargetZ) - rotation * Vector3.forward * WoolStage.Distance;
            try
            {
                foreach (var aspect in new[] { 1080f / 1920f, 1080f / 2400f, 0.75f })
                {
                    var size = WoolStage.Size(aspect);
                    camera.aspect = aspect;
                    camera.orthographicSize = size;
                    camera.transform.SetPositionAndRotation(position, rotation);
                    var low = WoolStage.BottomBand / (2 * size);
                    var high = 1 - WoolStage.HudBand / (2 * size);
                    foreach (var point in points)
                    {
                        var view = camera.WorldToViewportPoint(point);
                        Expect(view.x > 0.02f && view.x < 0.98f && view.y > low && view.y < high, "Point " + point + " outside frame at aspect " + aspect.ToString("F3") + " viewport " + view);
                    }
                }
            }
            finally
            {
                camera.ResetAspect();
                camera.orthographicSize = oldSize;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
            }
            return "STAGE PASS / " + points.Count + " framed points at 1080x1920, 1080x2400 and 3:4 / 5 path shapes";
        }
    }
}
