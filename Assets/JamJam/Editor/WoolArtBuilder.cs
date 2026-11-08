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
        public const float Saturation = 15f;
        public const float BloomThreshold = 1.1f;
        public const float BloomIntensity = 0.25f;
        public const float VignetteIntensity = 0.22f;
        public const float SunIntensity = 1.15f;
        public const float PostExposure = 0.3f;
        public const float Contrast = 18f;
        public const float ShadowStrength = 0.3f;
        public static readonly Color Ambient = new Color(0.72f, 0.7f, 0.7f);
        public static readonly Color Cream = new Color32(0xF6, 0xE7, 0xCF, 0xFF);
        public static readonly Color VignetteColor = new Color(0.25f, 0.17f, 0.1f);
        public static readonly Vector3 MeadowPosition = new Vector3(0, -0.035f, 9.9f);
        public static readonly Vector3 MeadowScale = new Vector3(35, 0.01f, 20.2f);
        public static readonly Vector3 ShelfPosition = new Vector3(0, -0.02f, WoolRouting.TrayZ - 0.05f);
        public static readonly Vector3 ShelfScale = new Vector3(10.1f, 0.14f, 1.3f);

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
            var controller = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            if (controller == null) throw new InvalidOperationException("WoolScene is missing from " + ScenePath);
            controller.Cushion = cushion.transform;
            var cushionPosition = cushion.transform.position;
            cushion.transform.position = new Vector3(cushionPosition.x, cushionPosition.y, WoolRouting.BoardCenterZ);
            EditorUtility.SetDirty(cushion.transform);
            if (controller.Levels != null && controller.Levels.Length > 0 && controller.Levels[0] != null)
            {
                var first = controller.Levels[0].Data;
                cushion.transform.localScale = new Vector3(first.Columns * WoolRouting.CellSize + WoolScene.CushionPad, cushion.transform.localScale.y, first.Rows * WoolRouting.CellSize + WoolScene.CushionPad);
                EditorUtility.SetDirty(cushion.transform);
            }
            EditorUtility.SetDirty(controller);
            EnsureMeadow(out _);
            EnsureShelf(out _);
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
            camera.orthographic = true; camera.orthographicSize = WoolStage.Size(1080f / 1920f);
            camera.transform.rotation = Quaternion.Euler(WoolStage.Pitch, 0, 0);
            camera.transform.position = new Vector3(0, 0, WoolStage.TargetZ) - camera.transform.forward * WoolStage.Distance;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Cream;
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

        public static void EnsureMeadow(out GameObject meadow)
        {
            meadow = GameObject.Find("Meadow");
            if (meadow == null)
            {
                meadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meadow.name = "Meadow";
            }
            var collider = meadow.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            meadow.transform.SetPositionAndRotation(MeadowPosition, Quaternion.identity);
            meadow.transform.localScale = MeadowScale;
            var renderer = meadow.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(WoolAssetsBuilder.Root + "/Materials/Meadow.mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorUtility.SetDirty(meadow.transform); EditorUtility.SetDirty(renderer);
        }

        public static void EnsureShelf(out GameObject shelf)
        {
            shelf = GameObject.Find("Tray Shelf");
            if (shelf == null) shelf = new GameObject("Tray Shelf");
            var collider = shelf.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            shelf.transform.SetPositionAndRotation(ShelfPosition, Quaternion.identity);
            shelf.transform.localScale = ShelfScale;
            var filter = shelf.GetComponent<MeshFilter>();
            if (filter == null) filter = shelf.AddComponent<MeshFilter>();
            filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(WoolAssetsBuilder.Root + "/Meshes/Rounded.asset");
            var renderer = shelf.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = shelf.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(WoolAssetsBuilder.Root + "/Materials/Shelf.mat");
            EditorUtility.SetDirty(shelf.transform); EditorUtility.SetDirty(filter); EditorUtility.SetDirty(renderer);
        }

        static void Profile(out VolumeProfile profile)
        {
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
            Effect<ColorAdjustments>(profile, out var adjust); adjust.saturation.Override(Saturation); adjust.postExposure.Override(PostExposure); adjust.contrast.Override(Contrast);
            Effect<Bloom>(profile, out var bloom); bloom.threshold.Override(BloomThreshold); bloom.intensity.Override(BloomIntensity); bloom.scatter.Override(0.6f);
            Effect<Tonemapping>(profile, out var tone); tone.mode.Override(TonemappingMode.Neutral);
            Effect<Vignette>(profile, out var vignette); vignette.intensity.Override(VignetteIntensity); vignette.smoothness.Override(0.45f); vignette.color.Override(VignetteColor);
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
