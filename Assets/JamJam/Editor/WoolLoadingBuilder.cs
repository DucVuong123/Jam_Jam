using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static class WoolLoadingBuilder
    {
        public const string ScenePath = "Assets/Scenes/Loading.unity";
        public const string BackgroundArt = "Loading-Background";
        public const string FillArt = "Progress-Fill";
        public const float ArtWidth = 1080;
        public const float ArtHeight = 1920;
        public const float FrameLeft = 110;
        public const float FrameRight = 966;
        public const float FrameTop = 1569;
        public const float FrameBottom = 1644;
        public const float RopeLeft = 79;
        public const float RopeRight = 997;
        public const float RopeTop = 1536;
        public const float RopeBottom = 1680;
        public const float FillInset = 4;
        public const int LabelSize = 40;
        public static readonly Color CameraColor = new Color32(0xF1, 0xCA, 0x9F, 0xFF);
        public static readonly Color FillColor = new Color32(0x6C, 0xC0, 0x4A, 0xFF);
        public static readonly Color LabelColor = Color.white;

        public static Vector2 FrameAnchorMin => new Vector2(FrameLeft / ArtWidth, 1 - FrameBottom / ArtHeight);
        public static Vector2 FrameAnchorMax => new Vector2(FrameRight / ArtWidth, 1 - FrameTop / ArtHeight);

        [MenuItem("JamJam/Rebuild Loading")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding the loading scene");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before rebuilding the loading scene");
            WoolUiArt.Import();
            if (!WoolUiArt.TryLoad(BackgroundArt, out var background)) throw new InvalidOperationException(WoolUiArt.PathOf(BackgroundArt) + " is missing");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen) scene = File.Exists(ScenePath) ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Fill(scene, background);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded && previous != scene) SceneManager.SetActiveScene(previous);
                if (!wasOpen && previous != scene && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
            WoolSceneBuilder.ApplyBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Loading scene generated");
        }

        static void Fill(Scene scene, Sprite background)
        {
            Root(scene, "Main Camera", out var cameraObject);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
            cameraObject.transform.localScale = Vector3.one;
            Component(cameraObject, out Camera camera);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraColor;
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100;
            Component(cameraObject, out UniversalAdditionalCameraData data);
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            Component(cameraObject, out AudioListener _);

            Root(scene, "Loading", out var loadingObject);
            Component(loadingObject, out WoolLoading loading);

            Root(scene, "Canvas", out var canvasObject);
            Component(canvasObject, out RectTransform canvasRect);
            Component(canvasObject, out Canvas canvas);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Component(canvasObject, out CanvasScaler scaler);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Component(canvasObject, out GraphicRaycaster _);

            Node(canvasRect, "Background", 0, out var backgroundRect);
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = Vector2.zero;
            Component(backgroundRect.gameObject, out Image backgroundImage);
            backgroundImage.sprite = background;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
            Component(backgroundRect.gameObject, out AspectRatioFitter fitter);
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = background.rect.width / background.rect.height;

            Node(backgroundRect, "Bar", 0, out var bar);
            bar.anchorMin = FrameAnchorMin;
            bar.anchorMax = FrameAnchorMax;
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = Vector2.zero;

            Node(bar, "Fill", 0, out var fill);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0, 1);
            fill.pivot = new Vector2(0, 0.5f);
            fill.anchoredPosition = new Vector2(FillInset, 0);
            fill.sizeDelta = new Vector2(0, -2 * FillInset);
            Component(fill.gameObject, out Image fillImage);
            fillImage.raycastTarget = false;
            fillImage.fillCenter = true;
            fillImage.preserveAspect = false;
            if (WoolUiArt.TryLoad(FillArt, out var fillSprite))
            {
                fillImage.sprite = fillSprite;
                fillImage.type = fillSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                fillImage.color = Color.white;
                fillImage.pixelsPerUnitMultiplier = fillSprite.rect.height / ((FrameBottom - FrameTop) - 2 * FillInset);
            }
            else
            {
                WoolUiStyle.LoadSprite("UiPill", out var pill);
                fillImage.sprite = pill;
                fillImage.type = Image.Type.Sliced;
                fillImage.color = FillColor;
                fillImage.pixelsPerUnitMultiplier = pill.rect.height / ((FrameBottom - FrameTop) - 2 * FillInset);
            }

            Node(bar, "Label", 1, out var labelRect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            Component(labelRect.gameObject, out Text label);
            WoolUiStyle.Light(label, LabelSize);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.text = "Loading 0%";

            loading.Fill = fill;
            loading.Label = label;
            loading.NextScene = WoolSession.LobbyScene;
            loading.MinimumSeconds = WoolLoading.DefaultMinimumSeconds;
            EditorUtility.SetDirty(loading);
        }

        static void Root(Scene scene, string name, out GameObject node)
        {
            node = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) { node = root; break; }
            if (node == null)
            {
                node = new GameObject(name);
                SceneManager.MoveGameObjectToScene(node, scene);
            }
            node.SetActive(true);
        }

        static void Node(Transform parent, string name, int index, out RectTransform rect)
        {
            var node = parent.Find(name);
            if (node == null)
            {
                rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false);
            }
            else rect = (RectTransform)node;
            rect.SetSiblingIndex(Mathf.Min(index, parent.childCount - 1));
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.gameObject.SetActive(true);
        }

        static void Component<T>(GameObject node, out T component) where T : Component
        {
            component = node.GetComponent<T>();
            if (component == null) component = node.AddComponent<T>();
        }
    }
}
