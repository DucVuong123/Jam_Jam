using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public const string CardPath = WoolAssetsBuilder.Root + "/Prefabs/LevelCard.prefab";

        static partial void RebuildLobby()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPath);
            try
            {
                StyleCard(root);
                PrefabUtility.SaveAsPrefabAsset(root, CardPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            var scene = EditorSceneManager.OpenScene(WoolLobbyBuilder.ScenePath, OpenSceneMode.Single);
            FindCanvas(out var canvas);
            var cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null) throw new System.InvalidOperationException("Main Camera is missing from " + WoolLobbyBuilder.ScenePath);
            StyleLobby(canvas, cameraObject.GetComponent<Camera>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void StyleLobby(Transform canvas, Camera camera)
        {
            WoolUiStyle.LoadSprite("UiPill", out var pill);
            WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = WoolUiTheme.Navy;
            EditorUtility.SetDirty(camera);
            Get(canvas, "LobbyPanel/Settings", out Button settings); WoolUiStyle.Button(settings, WoolUiTone.Secondary, pill, 32); Juice(settings);
            Get(canvas, "LobbyPanel/Brand", out Text brand); WoolUiStyle.Logo(brand, 130);
            Get(canvas, "LobbyPanel/Tagline", out Text tagline); WoolUiStyle.Light(tagline, 30, WoolUiTheme.SoftAlpha);
            Get(canvas, canvas.Find("LobbyPanel/PlayPulse") != null ? "LobbyPanel/PlayPulse/Play" : "LobbyPanel/Play", out Button play); WoolUiStyle.Button(play, WoolUiTone.Primary, pill, 60); Juice(play);
            Get(canvas, "LobbyPanel/Levels", out Button levels); WoolUiStyle.Button(levels, WoolUiTone.Secondary, pill, 44); Juice(levels);
            Get(canvas, "LevelsPanel/Back", out Button back); WoolUiStyle.Button(back, WoolUiTone.Secondary, pill, 32); Juice(back);
            Get(canvas, "LevelsPanel/Title", out Text title); WoolUiStyle.Light(title, 72);
            Panel(canvas, "SettingsPanel", cardSprite, out var card);
            Get(card, "Title", out Text settingsTitle); WoolUiStyle.Dark(settingsTitle, 60);
            foreach (var name in new[] { "Sound", "Music", "Vibration" })
            {
                Get(card, name, out Toggle toggle);
                WoolUiStyle.Toggle(toggle, pill, 34);
            }
            Get(card, "Reset", out Button reset); WoolUiStyle.Button(reset, WoolUiTone.Secondary, pill, 32); Juice(reset);
            Get(card, "Close", out Button close); WoolUiStyle.Button(close, WoolUiTone.Primary, pill, 36); Juice(close);
            Panel(canvas, "SettingsPanel/ConfirmPanel", cardSprite, out var confirm);
            Get(confirm, "Title", out Text confirmTitle); WoolUiStyle.Dark(confirmTitle, 50);
            Get(confirm, "Body", out Text confirmBody); WoolUiStyle.Dark(confirmBody, 30);
            Get(confirm, "Yes", out Button yes); WoolUiStyle.Button(yes, WoolUiTone.Danger, pill, 34); Juice(yes);
            Get(confirm, "No", out Button no); WoolUiStyle.Button(no, WoolUiTone.Secondary, pill, 34); Juice(no);
            Get(canvas, "LevelsPanel/Scroll", out ScrollRect scroll);
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            EditorUtility.SetDirty(scroll);
            var lobby = Object.FindFirstObjectByType<WoolLobby>();
            if (lobby == null) throw new System.InvalidOperationException("WoolLobby is missing from the lobby scene");
            lobby.Scroll = scroll;
            EditorUtility.SetDirty(lobby);
            ArtLobby(canvas);
            WoolUiStyle.RoundPills(canvas);
        }

        public static void StyleCard(GameObject root)
        {
            WoolUiStyle.LoadSprite("UiPill", out var pill);
            WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
            WoolUiStyle.LoadSprite("UiCheck", out var check);
            WoolUiStyle.LoadSprite("UiLock", out var padlock);
            var view = root.GetComponent<WoolLevelCard>();
            var edge = view.Content.GetComponent<Image>();
            WoolUiStyle.Raised(edge, cardSprite, WoolUiTheme.Secondary, WoolUiTheme.SecondaryEdge, new Vector4(0, WoolUiTheme.EdgeDepth, 0, 0), out var face);
            edge.raycastTarget = true;
            view.Face = face;
            view.Edge = edge;
            var button = view.Button;
            if (button.gameObject != root)
            {
                var moved = root.GetComponent<Button>();
                if (moved == null) moved = root.AddComponent<Button>();
                moved.interactable = button.interactable;
                Object.DestroyImmediate(button);
                button = moved;
                view.Button = moved;
            }
            var stale = view.Content.GetComponent<WoolButtonJuice>();
            if (stale != null) Object.DestroyImmediate(stale);
            button.targetGraphic = edge;
            button.transition = Selectable.Transition.None;
            Juice(button);
            WoolUiStyle.Light(view.Number, 64);
            WoolUiStyle.Light(view.Title, 38);
            WoolUiStyle.Badge(view.Check.GetComponent<Image>(), WoolUiTheme.Badge, pill, check, 26);
            WoolUiStyle.Badge(view.Lock.GetComponent<Image>(), WoolUiTheme.Badge, pill, padlock, 26);
            WoolUiArt.TryLoad("Icon-Play", out var play);
            WoolUiStyle.Badge(view.Current.GetComponent<Image>(), WoolUiTheme.Primary, pill, play, 28);
            ArtCard(view, cardSprite, pill);
            WoolUiStyle.RoundPills(root.transform);
            EditorUtility.SetDirty(view);
        }
    }
}
