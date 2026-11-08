using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiLobby()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolLobbyBuilder.ScenePath, out var scene, out var opened);
            int texts, buttons;
            try
            {
                ScanUi(scene.GetRootGameObjects(), out texts, out buttons);
                var lobby = Object.FindFirstObjectByType<WoolLobby>();
                Expect(lobby != null, "WoolLobby exists");
                ExpectPanel(lobby.SettingsPanel.transform);
                ExpectPanel(lobby.ConfirmPanel.transform);
                foreach (var toggle in new[] { lobby.SoundToggle, lobby.MusicToggle, lobby.VibrationToggle })
                    {
                    var box = (Image)toggle.targetGraphic;
                    var knob = box.transform.Find("Knob");
                    var checkRect = ((Image)toggle.graphic).rectTransform;
                    Expect(Near(box.color, WoolUiTheme.Off) && Near(((Image)toggle.graphic).color, Color.white) && ((Image)toggle.graphic).sprite.name == "UiToggleOn" && toggle.transition == Selectable.Transition.None, toggle.name + " is a green on / navy grey off switch");
                    Expect(knob != null && knob.GetSiblingIndex() == 0 && toggle.graphic.transform.GetSiblingIndex() == 1 && !knob.GetComponent<Image>().raycastTarget && !toggle.graphic.raycastTarget && checkRect.offsetMin == Vector2.zero && checkRect.offsetMax == Vector2.zero && checkRect.anchorMin == Vector2.zero && checkRect.anchorMax == Vector2.one, toggle.name + " has a left knob under a stretched check");
                }
                Camera camera = null;
                foreach (var root in scene.GetRootGameObjects()) if (root.name == "Main Camera") camera = root.GetComponent<Camera>();
                Expect(camera != null && Near(camera.backgroundColor, WoolUiTheme.Navy), "Lobby camera clears to navy #14234A");
                Expect(lobby.Kitten != null && lobby.PlayLabel == lobby.PlayButton.transform.Find("Label").GetComponent<Text>(), "Lobby wiring is unchanged");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var prefab = PrefabUtility.LoadPrefabContents(WoolUiBuilder.CardPath);
            int cardTexts, cardButtons;
            try
            {
                ScanUi(new[] { prefab }, out cardTexts, out cardButtons);
                var view = prefab.GetComponent<WoolLevelCard>();
                Expect(view.Face != null && view.Face.transform.parent == view.Content && view.Edge == view.Content.GetComponent<Image>(), "LevelCard face and edge are wired");
                Expect(view.Button.gameObject == prefab && view.Button.transform != view.Content && view.Button.GetComponent<WoolButtonJuice>() != null, "Button juice and card pulse use different transforms");
                Expect(view.Content.GetComponent<WoolButtonJuice>() == null && view.Content.GetComponent<Button>() == null, "Content has no juice and no button");
                var checkIcon = view.Check.transform.Find("Icon").GetComponent<Image>().sprite.name;
                var lockIcon = view.Lock.transform.Find("Icon").GetComponent<Image>().sprite.name;
                Expect((checkIcon == "UiCheck" || checkIcon == "Icon-Check") && (lockIcon == "UiLock" || lockIcon == "Icon-Lock"), "Cleared shows a check and locked shows a padlock");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            WoolUiTheme.CardColors(true, true, false, out var cleared, out _);
            WoolUiTheme.CardColors(true, false, true, out var current, out _);
            WoolUiTheme.CardColors(false, false, false, out var locked, out _);
            Expect(Near(cleared, WoolUiTheme.Primary) && Near(current, WoolUiTheme.Warm) && Near(locked, WoolUiTheme.Locked), "Card colors follow the token table");
            return "UI LOBBY PASS / " + (texts + cardTexts) + " texts / " + (buttons + cardButtons) + " buttons";
        }

        public static string PlayLobby()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var lobby = Object.FindFirstObjectByType<WoolLobby>();
            Expect(lobby != null, "Lobby is running");
            lobby.ShowLevels();
            var cards = lobby.CardContainer.GetComponentsInChildren<WoolLevelCard>();
            Expect(cards.Length == lobby.Levels.Length, "One card per level");
            var currentIndex = -1;
            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                Expect(card.Face != null && card.Button.GetComponent<WoolButtonJuice>() != null, "Card " + i + " is styled");
                Expect(card.Content.GetComponent<WoolButtonJuice>() == null, "Card " + i + " content has no juice");
                Expect(card.Button.GetComponent<WoolButtonJuice>().enabled == !card.Lock.activeSelf, "Card " + i + " juice is off only when locked");
                card.Animate(0.3f);
                var scale = card.Content.localScale.x;
                if (card.Current.activeSelf)
                {
                    currentIndex = i;
                    Expect(card.Pulsing && Near(card.Face.color, WoolUiTheme.Warm) && scale >= 1 && scale <= 1.0401f, "Current card is warm and pulses within 1..1.04 but scale was " + scale);
                }
                else Expect(!card.Pulsing && scale == 1, "Card " + i + " does not pulse");
                if (card.Check.activeSelf) Expect(Near(card.Face.color, WoolUiTheme.Primary), "Cleared card " + i + " is green");
                if (card.Lock.activeSelf) Expect(Near(card.Face.color, WoolUiTheme.Locked), "Locked card " + i + " is navy");
            }
            lobby.OpenSettings();
            var pop = lobby.SettingsPanel.transform.Find("Card").GetComponent<WoolPopIn>();
            Expect(pop != null && pop.Animating, "Settings card pops in");
            pop.Step(1);
            Expect(Mathf.Abs(pop.transform.localScale.x - 1) < 1e-4f && pop.Fade.alpha == 1, "Settings card settles");
            var prefs = new WoolSettings(new WoolPlayerPrefs());
            var before = prefs.Vibration;
            lobby.VibrationToggle.isOn = !before;
            Expect(prefs.Vibration == !before, "Vibration toggle still saves");
            lobby.VibrationToggle.isOn = before;
            Expect(prefs.Vibration == before, "Vibration toggle restores");
            lobby.CloseSettings();
            lobby.ShowLobby();
            return "PLAY LOBBY PASS / " + cards.Length + " cards / current " + currentIndex;
        }

        public static string PlayLobbyScroll()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var lobby = Object.FindFirstObjectByType<WoolLobby>();
            Expect(lobby != null && lobby.Scroll != null && lobby.Scroll.content == lobby.CardContainer && !lobby.Scroll.horizontal && lobby.Scroll.vertical, "Levels list is a vertical ScrollRect over the card container");
            var cards = lobby.CardContainer.GetComponentsInChildren<WoolLevelCard>();
            Expect(cards.Length == 30 && lobby.Levels.Length == 30, "Thirty level cards");
            lobby.ShowLevels();
            var current = -1;
            for (var i = 0; i < cards.Length; i++) if (cards[i].Current.activeSelf) current = i;
            Expect(current == 29, "Level 30 is current after clearing 29 levels but current was " + current);
            Expect(Visible(lobby, cards[29]) && lobby.Scroll.verticalNormalizedPosition < 0.001f, "Opening the list scrolls to level 30");
            lobby.ScrollTo(0);
            Expect(Visible(lobby, cards[0]) && lobby.Scroll.verticalNormalizedPosition > 0.999f, "ScrollTo(0) shows the first card");
            lobby.ScrollTo(29);
            cards[29].Button.onClick.Invoke();
            Expect(WoolSession.SelectedLevel == 29, "Card 30 loads level 30");
            return "PLAY LOBBY SCROLL PASS / 30 cards";
        }

        public static string PlayLobbyScrollLoaded()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.CurrentLevel == 29 && scene.State != null && scene.State.Status == RoundStatus.Playing, "Level 30 is running");
            return "PLAY LOBBY SCROLL LOADED PASS / level " + (scene.CurrentLevel + 1);
        }

        static bool Visible(WoolLobby lobby, WoolLevelCard card)
        {
            Canvas.ForceUpdateCanvases();
            var viewport = lobby.Scroll.viewport != null ? lobby.Scroll.viewport : (RectTransform)lobby.Scroll.transform;
            var outer = new Vector3[4];
            var inner = new Vector3[4];
            viewport.GetWorldCorners(outer);
            ((RectTransform)card.transform).GetWorldCorners(inner);
            return inner[0].y >= outer[0].y - 0.5f && inner[1].y <= outer[1].y + 0.5f;
        }
    }
}
