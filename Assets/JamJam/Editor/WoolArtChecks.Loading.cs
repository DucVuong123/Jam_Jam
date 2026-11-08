using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public const float LoadingLimitSeconds = 4f;
        const string LoadingArmKey = "WoolJam.LoadingArm";
        const string LoadingPauseKey = "WoolJam.LoadingPause";
        static readonly List<Vector3> loadingSamples = new List<Vector3>();
        static bool loadingArmed;
        static bool loadingPaused;
        static float loadingPauseAt;
        static float loadingLast;
        static float loadingLobbyElapsed = -1;
        static float loadingLobbyReal = -1;
        static float loadingStall;

        public static string Loading()
        {
            Expect(File.Exists(WoolLoadingBuilder.ScenePath), "Loading.unity exists");
            var scenes = EditorBuildSettings.scenes;
            Expect(scenes.Length >= 3 && scenes[0].path == WoolLoadingBuilder.ScenePath && scenes[0].enabled, "Loading.unity is the first enabled build scene");
            Expect(scenes[1].path == WoolLobbyBuilder.ScenePath && scenes[1].enabled && scenes[2].path == "Assets/Scenes/WoolJam.unity" && scenes[2].enabled, "Build order is Loading, Lobby, WoolJam");
            var path = WoolUiArt.PathOf(WoolLoadingBuilder.BackgroundArt);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Expect(importer != null, path + " is imported");
            WoolUiArt.Settings(WoolLoadingBuilder.BackgroundArt, out var border, out var maxSize);
            Expect(maxSize == 2048 && border == Vector4.zero && WoolUiArt.Matches(importer, border, maxSize), "Loading-Background imports as a single clamped bilinear sprite without mipmaps at max 2048");
            Expect(WoolUiArt.TryLoad(WoolLoadingBuilder.BackgroundArt, out var art) && art.rect.width == WoolLoadingBuilder.ArtWidth && art.rect.height == WoolLoadingBuilder.ArtHeight, "Loading-Background is a 1080x1920 sprite");
            var scene = SceneManager.GetSceneByPath(WoolLoadingBuilder.ScenePath);
            var opened = !(scene.IsValid() && scene.isLoaded);
            if (opened) scene = EditorSceneManager.OpenScene(WoolLoadingBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                WoolLoading loading = null;
                Camera camera = null;
                CanvasScaler scaler = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (loading == null) loading = root.GetComponentInChildren<WoolLoading>(true);
                    if (camera == null) camera = root.GetComponentInChildren<Camera>(true);
                    if (scaler == null) scaler = root.GetComponentInChildren<CanvasScaler>(true);
                }
                Expect(loading != null && loading.Fill != null && loading.Label != null, "WoolLoading has Fill and Label wired");
                Expect(loading.NextScene == WoolSession.LobbyScene && Mathf.Approximately(loading.MinimumSeconds, WoolLoading.DefaultMinimumSeconds), "WoolLoading loads Lobby after at least " + WoolLoading.DefaultMinimumSeconds + "s");
                Expect(camera != null && camera.orthographic && camera.clearFlags == CameraClearFlags.SolidColor && Near(camera.backgroundColor, WoolLoadingBuilder.CameraColor), "Main Camera is orthographic solid felt #" + ColorUtility.ToHtmlStringRGB(WoolLoadingBuilder.CameraColor));
                Expect(scaler != null && scaler.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution == new Vector2(1080, 1920) && Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f), "Canvas scales with 1080x1920 match 0.5");
                var background = scaler.transform.Find("Background") as RectTransform;
                Expect(background != null && background.GetComponent<Image>().sprite == art, "Background shows Loading-Background");
                var fitter = background.GetComponent<AspectRatioFitter>();
                Expect(fitter != null && fitter.aspectMode == AspectRatioFitter.AspectMode.FitInParent && Mathf.Abs(fitter.aspectRatio - 1080f / 1920f) < 0.0001f && background.pivot == new Vector2(0.5f, 0.5f), "Background fits inside the screen");
                var bar = loading.Fill.parent as RectTransform;
                Expect(bar != null && bar.parent == background && bar.name == "Bar" && loading.Label.transform.parent == bar, "Fill and Label live in Background/Bar");
                Expect((bar.anchorMin - WoolLoadingBuilder.FrameAnchorMin).sqrMagnitude < 1e-8f && (bar.anchorMax - WoolLoadingBuilder.FrameAnchorMax).sqrMagnitude < 1e-8f && bar.sizeDelta == Vector2.zero, "Bar anchors match the stitched frame");
                var fillImage = loading.Fill.GetComponent<Image>();
                Expect(fillImage != null && (WoolUiArt.IsArt(fillImage.sprite, WoolLoadingBuilder.FillArt) && fillImage.type == Image.Type.Sliced || !File.Exists(WoolUiArt.PathOf(WoolLoadingBuilder.FillArt)) && Near(fillImage.color, WoolLoadingBuilder.FillColor)), "Fill uses Progress-Fill");
                Expect(loading.Fill.pivot == new Vector2(0, 0.5f) && loading.Fill.anchorMin == Vector2.zero && loading.Fill.anchorMax == new Vector2(0, 1), "Fill grows from the left");
                WoolUiStyle.LoadFont(out var font, out _);
                Expect(loading.Label.font == font && loading.Label.fontSize == WoolLoadingBuilder.LabelSize && Near(loading.Label.color, WoolLoadingBuilder.LabelColor) && loading.Label.alignment == TextAnchor.MiddleCenter && loading.Label.text == "Loading 0%", "Label is white Lilita One 'Loading 0%' centered on the bar");
                var outline = loading.Label.GetComponent<Outline>();
                Expect(outline != null && Near(outline.effectColor, WoolUiTheme.TextOutline) && outline.effectDistance.x >= 1, "Label has the navy light-text outline");
                ExpectLoadingLayout(background, loading, 1080, 1920, out var tall);
                ExpectLoadingLayout(background, loading, 1080, 2400, out var taller);
                ExpectLoadingLayout(background, loading, 1440, 1920, out var wider);
                return "LOADING PASS / frame " + WoolLoadingBuilder.FrameLeft + ".." + WoolLoadingBuilder.FrameRight + " x " + WoolLoadingBuilder.FrameTop + ".." + WoolLoadingBuilder.FrameBottom + " / 1080x1920 " + tall + " / 1080x2400 " + taller + " / 1440x1920 " + wider;
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void ExpectLoadingLayout(RectTransform background, WoolLoading source, float width, float height, out string summary)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Screen", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                SceneManager.MoveGameObjectToScene(root.gameObject, preview);
                var scale = Mathf.Sqrt(width / 1080f * (height / 1920f));
                var canvas = new Vector2(width / scale, height / scale);
                root.sizeDelta = canvas;
                var copy = ((GameObject)Object.Instantiate(background.gameObject, root, false)).GetComponent<RectTransform>();
                copy.gameObject.SetActive(false);
                copy.gameObject.SetActive(true);
                var bar = (RectTransform)copy.Find("Bar");
                var fill = (RectTransform)bar.Find("Fill");
                var label = bar.Find("Label").GetComponent<Text>();
                var loading = root.gameObject.AddComponent<WoolLoading>();
                loading.Fill = fill;
                loading.Label = label;
                ArtRect(copy, bar, out var frame);
                Expect(Mathf.Abs(frame.xMin - WoolLoadingBuilder.FrameLeft) < 0.5f && Mathf.Abs(frame.xMax - WoolLoadingBuilder.FrameRight) < 0.5f && Mathf.Abs(frame.yMin - WoolLoadingBuilder.FrameTop) < 0.5f && Mathf.Abs(frame.yMax - WoolLoadingBuilder.FrameBottom) < 0.5f, "Bar tracks the stitched frame at " + width + "x" + height + " but was " + frame);
                Expect(Mathf.Abs(copy.rect.width / copy.rect.height - 1080f / 1920f) < 0.001f && copy.rect.width <= canvas.x + 0.5f && copy.rect.height <= canvas.y + 0.5f && (Mathf.Abs(copy.rect.width - canvas.x) < 0.5f || Mathf.Abs(copy.rect.height - canvas.y) < 0.5f), "Background fits inside " + width + "x" + height + " but was " + copy.rect.size + " in " + canvas);
                var outer = new Vector3[4];
                copy.GetWorldCorners(outer);
                var ropeMin = new Vector2(outer[0].x + (outer[2].x - outer[0].x) * WoolLoadingBuilder.RopeLeft / WoolLoadingBuilder.ArtWidth, outer[2].y - (outer[2].y - outer[0].y) * WoolLoadingBuilder.RopeBottom / WoolLoadingBuilder.ArtHeight);
                var ropeMax = new Vector2(outer[0].x + (outer[2].x - outer[0].x) * WoolLoadingBuilder.RopeRight / WoolLoadingBuilder.ArtWidth, outer[2].y - (outer[2].y - outer[0].y) * WoolLoadingBuilder.RopeTop / WoolLoadingBuilder.ArtHeight);
                Expect(ropeMin.x >= -canvas.x * 0.5f - 0.5f && ropeMax.x <= canvas.x * 0.5f + 0.5f && ropeMin.y >= -canvas.y * 0.5f - 0.5f && ropeMax.y <= canvas.y * 0.5f + 0.5f, "Whole stitched frame is on screen at " + width + "x" + height + " but was " + ropeMin + ".." + ropeMax + " in " + canvas);
                var parts = new List<string>();
                foreach (var value in new[] { 0.02f, 0.5f, 1f })
                {
                    loading.Show(value);
                    ArtRect(copy, fill, out var rect);
                    Expect(rect.xMin >= WoolLoadingBuilder.FrameLeft - 0.5f && rect.xMax <= WoolLoadingBuilder.FrameRight + 0.5f && rect.yMin >= WoolLoadingBuilder.FrameTop - 0.5f && rect.yMax <= WoolLoadingBuilder.FrameBottom + 0.5f, "Fill at " + value + " stays inside the frame at " + width + "x" + height + " but was " + rect);
                    Expect(rect.height > (WoolLoadingBuilder.FrameBottom - WoolLoadingBuilder.FrameTop) * 0.8f, "Fill fills the frame height at " + width + "x" + height);
                    Expect(label.text == "Loading " + Mathf.RoundToInt(value * 100) + "%", "Label shows " + Mathf.RoundToInt(value * 100) + "%");
                    if (value >= 1) Expect(Mathf.Abs(rect.xMax - (WoolLoadingBuilder.FrameRight - WoolLoadingBuilder.FillInset * WoolLoadingBuilder.ArtWidth / copy.rect.width)) < 0.5f, "Full fill reaches the right of the frame at " + width + "x" + height);
                    parts.Add(value.ToString("0.##") + ":" + rect.xMin.ToString("F1") + ".." + rect.xMax.ToString("F1"));
                }
                summary = string.Join(" ", parts);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static void ArtRect(RectTransform background, RectTransform target, out Rect rect)
        {
            var outer = new Vector3[4];
            var inner = new Vector3[4];
            background.GetWorldCorners(outer);
            target.GetWorldCorners(inner);
            var w = outer[2].x - outer[0].x;
            var h = outer[2].y - outer[0].y;
            var xMin = (inner[0].x - outer[0].x) / w * WoolLoadingBuilder.ArtWidth;
            var xMax = (inner[2].x - outer[0].x) / w * WoolLoadingBuilder.ArtWidth;
            var yMin = (outer[2].y - inner[2].y) / h * WoolLoadingBuilder.ArtHeight;
            var yMax = (outer[2].y - inner[0].y) / h * WoolLoadingBuilder.ArtHeight;
            rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        [InitializeOnLoadMethod]
        static void HookLoading()
        {
            EditorApplication.playModeStateChanged -= ArmOnPlay;
            EditorApplication.playModeStateChanged += ArmOnPlay;
        }

        static void ArmOnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(LoadingArmKey, false)) return;
            SessionState.EraseBool(LoadingArmKey);
            StartLoadingSampler(SessionState.GetFloat(LoadingPauseKey, 0));
        }

        public static string PlayLoadingArm(float pauseAt)
        {
            if (EditorApplication.isPlaying) return StartLoadingSampler(pauseAt);
            Expect(SceneManager.GetActiveScene().path == WoolLoadingBuilder.ScenePath, "Open Loading.unity before arming");
            SessionState.SetBool(LoadingArmKey, true);
            SessionState.SetFloat(LoadingPauseKey, pauseAt);
            return "PLAY LOADING ARMED FOR NEXT PLAY / pause at " + pauseAt;
        }

        static string StartLoadingSampler(float pauseAt)
        {
            var loading = Object.FindFirstObjectByType<WoolLoading>();
            Expect(loading != null && SceneManager.GetActiveScene().path == WoolLoadingBuilder.ScenePath, "Play Mode started from Loading.unity");
            EditorApplication.update -= SampleLoading;
            loadingSamples.Clear();
            loadingSamples.Add(new Vector3(loading.Elapsed, loading.Displayed, Time.realtimeSinceStartup));
            loadingPauseAt = pauseAt;
            loadingPaused = false;
            loadingLast = loading.Elapsed;
            loadingLobbyElapsed = -1;
            loadingLobbyReal = -1;
            loadingArmed = true;
            EditorApplication.update += SampleLoading;
            return "PLAY LOADING ARMED / " + Mathf.RoundToInt(loading.Displayed * 100) + "% at " + loading.Elapsed.ToString("F2") + "s";
        }

        static void SampleLoading()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= SampleLoading;
                return;
            }
            var loading = Object.FindFirstObjectByType<WoolLoading>();
            if (loading != null)
            {
                var last = loadingSamples[loadingSamples.Count - 1];
                if (loading.Elapsed != last.x || loading.Displayed != last.y) loadingSamples.Add(new Vector3(loading.Elapsed, loading.Displayed, Time.realtimeSinceStartup));
                loadingLast = loading.Elapsed;
                if (loadingPauseAt > 0 && loading.Displayed >= loadingPauseAt)
                {
                    loadingPauseAt = 0;
                    loadingPaused = true;
                    EditorApplication.isPaused = true;
                }
                return;
            }
            if (SceneManager.GetActiveScene().name != WoolSession.LobbyScene) return;
            loadingLobbyElapsed = loadingLast;
            loadingLobbyReal = Time.realtimeSinceStartup - loadingSamples[Mathf.Min(1, loadingSamples.Count - 1)].z;
            loadingStall = loadingSamples.Count > 1 ? loadingSamples[1].z - loadingSamples[0].z : 0;
            EditorApplication.update -= SampleLoading;
        }

        public static string PlayLoading()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            Expect(loadingArmed, "Run PlayLoadingArm first");
            Expect(loadingLobbyElapsed >= 0, "Lobby became active after loading (" + loadingSamples.Count + " samples, last " + loadingSamples[loadingSamples.Count - 1].y.ToString("F2") + ")");
            Expect(SceneManager.GetActiveScene().name == WoolSession.LobbyScene && Object.FindFirstObjectByType<WoolLobby>() != null && Object.FindFirstObjectByType<WoolLoading>() == null, "Lobby is the active scene");
            var middle = 0;
            for (var i = 1; i < loadingSamples.Count; i++)
            {
                Expect(loadingSamples[i].y >= loadingSamples[i - 1].y - 0.0001f && loadingSamples[i].x >= loadingSamples[i - 1].x, "Loading progress never goes backwards");
                if (loadingSamples[i].y > 0.05f && loadingSamples[i].y < 0.95f) middle++;
            }
            Expect(middle >= 3, "Loading progress advances through the middle (" + middle + " samples)");
            Expect(loadingSamples[loadingSamples.Count - 1].y >= 0.999f, "Loading reaches 100% before activating Lobby");
            Expect(loadingLobbyElapsed >= WoolLoading.DefaultMinimumSeconds - 0.05f && loadingLobbyElapsed <= LoadingLimitSeconds, "Lobby activates between " + WoolLoading.DefaultMinimumSeconds + "s and " + LoadingLimitSeconds + "s but took " + loadingLobbyElapsed.ToString("F2") + "s");
            Expect(loadingPaused || loadingLobbyReal <= LoadingLimitSeconds + 0.5f, "Lobby activates within " + LoadingLimitSeconds + "s real time after the first frame but took " + loadingLobbyReal.ToString("F2") + "s");
            loadingArmed = false;
            return "PLAY LOADING PASS / " + loadingSamples.Count + " samples / " + middle + " mid / lobby at " + loadingLobbyElapsed.ToString("F2") + "s (real " + loadingLobbyReal.ToString("F2") + "s after a " + loadingStall.ToString("F2") + "s first frame" + (loadingPaused ? ", paused" : "") + ")";
        }
    }
}
