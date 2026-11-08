using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiArtLobby()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolLobbyBuilder.ScenePath, out var scene, out var opened);
            var skinned = 0;
            try
            {
                var lobby = Object.FindFirstObjectByType<WoolLobby>();
                Expect(lobby != null, "WoolLobby exists");
                var canvas = lobby.LobbyPanel.transform.parent;
                var home = lobby.LobbyPanel.transform;
                skinned += ExpectBackground(home, "Lobby-Background") + ExpectBackground(lobby.LevelsPanel.transform, "Levels-Background");
                Expect(lobby.Kitten.GetComponent<Image>().enabled == (home.Find("Background") == null), "Procedural kitten shows only without the lobby background");
                var brand = home.Find("Brand").GetComponent<Text>();
                var logo = home.Find("Logo");
                if (WoolUiArt.TryLoad("Logo-WoolJam", out var logoArt))
                {
                    Expect(logo != null && logo.GetComponent<Image>().sprite == logoArt && logo.GetComponent<Image>().preserveAspect && !brand.enabled && brand.text == "WOOL JAM", "Logo-WoolJam replaces the hidden WOOL JAM text");
                    var breath = logo.GetComponent<WoolUiMotion>();
                    Expect(breath != null && breath.Kind == WoolUiMotionKind.Breathe && breath.Amount == WoolUiBuilder.LogoBreath && breath.Period == WoolUiBuilder.LogoBreathPeriod, "Logo breathes 1 to 1.03 over 2.4 s");
                    Expect(((RectTransform)home.Find("Tagline")).anchoredPosition == WoolUiBuilder.TaglineArtPosition, "Tagline sits under the logo");
                    skinned++;
                }
                else Expect(logo == null && brand.enabled, "WOOL JAM text shows without the logo");
                var pulse = home.Find("PlayPulse");
                Expect(pulse != null && lobby.PlayButton.transform.parent == pulse && lobby.PlayButton.GetComponent<WoolButtonJuice>() != null && pulse.GetComponent<WoolButtonJuice>() == null, "PLAY pulses on a wrapper so its juice keeps its own transform");
                var motion = pulse.GetComponent<WoolUiMotion>();
                Expect(motion != null && motion.Kind == WoolUiMotionKind.Breathe && motion.Amount == WoolUiBuilder.PlayPulseScale && motion.Period == WoolUiBuilder.PlayPulsePeriod, "PLAY pulses 1 to 1.05 over 1.2 s");
                skinned += ExpectSkin(home, "PlayPulse/Play", "Button-Green");
                ExpectIcon(home, "PlayPulse/Play", "Icon-Play", false);
                skinned += ExpectSkin(home, "Levels", "Button-Cream");
                ExpectIcon(home, "Levels", null, false);
                var round = ExpectSkin(home, "Settings", "Button-Round-Cream");
                skinned += round;
                var settings = (RectTransform)home.Find("Settings");
                if (round == 1)
                {
                    Expect(settings.sizeDelta == WoolUiBuilder.SettingsRoundSize && settings.anchoredPosition == WoolUiBuilder.SettingsRoundPosition, "Settings is a round button in the top right");
                    ExpectIcon(home, "Settings", "Icon-Settings", true);
                }
                else Expect(settings.sizeDelta == WoolUiBuilder.SettingsPillSize && settings.anchoredPosition == WoolUiBuilder.SettingsPillPosition, "Settings stays a pill without round art");
                skinned += ExpectSkin(lobby.LevelsPanel.transform, "Back", "Button-Cream");
                ExpectIcon(lobby.LevelsPanel.transform, "Back", "Icon-Back", false);
                skinned += ExpectSkin(canvas, "SettingsPanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "SettingsPanel/Card/Close", "Button-Green");
                var red = ExpectSkin(canvas, "SettingsPanel/Card/Reset", "Button-Red");
                skinned += red;
                var resetLabel = canvas.Find("SettingsPanel/Card/Reset/Label").GetComponent<Text>();
                Expect(red == 0 || (Near(resetLabel.color, WoolUiTheme.TextLight) && resetLabel.GetComponent<Outline>() != null), "Reset on red uses white text with a navy outline");
                skinned += ExpectSkin(canvas, "SettingsPanel/ConfirmPanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "SettingsPanel/ConfirmPanel/Card/Yes", "Button-Red");
                skinned += ExpectSkin(canvas, "SettingsPanel/ConfirmPanel/Card/No", "Button-Cream");
                foreach (var (toggle, icon) in new[] { (lobby.SoundToggle, "Icon-Sound"), (lobby.MusicToggle, "Icon-Music"), (lobby.VibrationToggle, "Icon-Vibration") })
                {
                    var node = (RectTransform)toggle.transform.Find("Icon");
                    var label = (RectTransform)toggle.transform.Find("Label");
                    if (!WoolUiArt.TryLoad(icon, out var sprite)) { Expect(node == null && label.anchoredPosition == WoolUiBuilder.ToggleLabelPosition, toggle.name + " has no icon without " + icon); continue; }
                    Expect(node != null && node.GetComponent<Image>().sprite == sprite && node.anchoredPosition.x + node.sizeDelta.x * 0.5f <= label.anchoredPosition.x - label.sizeDelta.x * 0.5f + 0.01f, toggle.name + " shows " + icon + " left of its label");
                }
                ExpectLobbyFallback(canvas);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var root = PrefabUtility.LoadPrefabContents(WoolUiBuilder.CardPath);
            try
            {
                var view = root.GetComponent<WoolLevelCard>();
                WoolUiStyle.LoadSprite("UiCard", out var plain);
                Expect(view.Plain == plain, "LevelCard keeps the procedural card for states without art");
                ExpectCardArt(view.ClearedArt, "LevelCard-Cleared");
                ExpectCardArt(view.CurrentArt, "LevelCard-Current");
                ExpectCardArt(view.LockedArt, "LevelCard-Locked");
                var art = view.ClearedArt != null || view.CurrentArt != null || view.LockedArt != null;
                Expect(view.Number.rectTransform.anchoredPosition == (art ? WoolUiBuilder.CardNumberArtPosition : WoolUiBuilder.CardNumberPosition), "Card number clears the left decoration");
                Expect(!art || Mathf.Abs(view.ArtScale - 256f / WoolUiBuilder.CardHeight) < 1e-3f, "Card art scales its corners to the 170 px card");
                view.Bind(0, "First Threads", true, true, false);
                ExpectBound(view, view.ClearedArt, WoolUiTheme.PrimaryEdge, "Cleared");
                view.Bind(1, "Color Traffic", true, false, true);
                ExpectBound(view, view.CurrentArt, WoolUiTheme.WarmEdge, "Current");
                view.Bind(2, "The Great Unravel", false, false, false);
                ExpectBound(view, view.LockedArt, WoolUiTheme.LockedEdge, "Locked");
                view.ClearedArt = null;
                view.Bind(0, "First Threads", true, true, false);
                ExpectBound(view, null, WoolUiTheme.PrimaryEdge, "Cleared without art");
                ExpectBadgeIcon(view.Check, "Icon-Check", "UiCheck");
                ExpectBadgeIcon(view.Lock, "Icon-Lock", "UiLock");
                ExpectBadgeIcon(view.Current, "Icon-Play", null);
                WoolUiArt.SimulateMissing = true;
                WoolUiBuilder.StyleCard(root);
                Expect(view.ClearedArt == null && view.CurrentArt == null && view.LockedArt == null && view.Face.enabled && view.Edge.sprite == plain && view.Number.rectTransform.anchoredPosition == WoolUiBuilder.CardNumberPosition, "Without art the level card is procedural");
                Expect(view.Check.transform.Find("Icon").GetComponent<Image>().sprite.name == "UiCheck" && view.Current.transform.Find("Icon") == null, "Without art the badges use the procedural icons");
                WoolUiArt.SimulateMissing = false;
                WoolUiBuilder.StyleCard(root);
                if (WoolUiArt.TryLoad("LevelCard-Current", out var current)) Expect(view.CurrentArt == current && view.Edge.sprite == current && !view.Face.enabled, "Card art comes back after the fallback");
            }
            finally
            {
                WoolUiArt.SimulateMissing = false;
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "UI ART LOBBY PASS / " + skinned + " skinned";
        }

        public static string PlayArtLobby()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var lobby = Object.FindFirstObjectByType<WoolLobby>();
            Expect(lobby != null, "Lobby is running");
            lobby.ShowLobby();
            var pulse = lobby.PlayButton.transform.parent.GetComponent<WoolUiMotion>();
            Expect(pulse != null && pulse.Running, "PLAY pulse runs while the home panel is open");
            pulse.enabled = false;
            pulse.enabled = true;
            pulse.Step(WoolUiBuilder.PlayPulsePeriod * 0.5f);
            Expect(Mathf.Abs(pulse.transform.localScale.x - (1 + WoolUiBuilder.PlayPulseScale)) < 1e-3f && lobby.PlayButton.transform.localScale.x == 1, "PLAY wrapper reaches 1.05 while the button keeps scale 1");
            var logo = lobby.LobbyPanel.transform.Find("Logo");
            if (logo != null)
            {
                var breath = logo.GetComponent<WoolUiMotion>();
                Expect(breath.Running, "Logo breathes while the home panel is open");
                breath.enabled = false;
                breath.enabled = true;
                breath.Step(WoolUiBuilder.LogoBreathPeriod * 0.5f);
                Expect(Mathf.Abs(logo.localScale.x - (1 + WoolUiBuilder.LogoBreath)) < 1e-3f, "Logo reaches 1.03 at half period");
            }
            lobby.ShowLevels();
            Expect(!pulse.Running && pulse.transform.localScale == Vector3.one, "PLAY pulse stops and resets when the home panel hides");
            var cards = lobby.CardContainer.GetComponentsInChildren<WoolLevelCard>();
            var bound = 0;
            foreach (var card in cards)
            {
                var art = card.Check.activeSelf ? card.ClearedArt : card.Lock.activeSelf ? card.LockedArt : card.CurrentArt;
                if (art == null) continue;
                Expect(card.Edge.sprite == art && !card.Face.enabled && Near(card.Edge.color, Color.white), card.name + " shows " + art.name);
                bound++;
            }
            lobby.ShowLobby();
            return "PLAY ART LOBBY PASS / " + bound + " art cards / pulse / breath";
        }

        static int ExpectBackground(Transform panel, string art)
        {
            var node = panel.Find("Background");
            if (!WoolUiArt.TryLoad(art, out var sprite))
            {
                Expect(node == null, panel.name + " has no background without " + art);
                return 0;
            }
            Expect(node != null, panel.name + " has a Background");
            var rect = (RectTransform)node;
            var fitter = node.GetComponent<AspectRatioFitter>();
            Expect(node.GetSiblingIndex() == 0 && node.GetComponent<Image>().sprite == sprite && !node.GetComponent<Image>().raycastTarget, panel.name + " draws " + art + " behind everything");
            Expect(fitter != null && fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent && Mathf.Abs(fitter.aspectRatio - sprite.rect.width / sprite.rect.height) < 1e-4f && rect.pivot == new Vector2(0.5f, 0.5f), panel.name + " background covers the screen centred");
            return 1;
        }

        static void ExpectCardArt(Sprite assigned, string art)
        {
            if (WoolUiArt.TryLoad(art, out var sprite)) Expect(assigned == sprite, "LevelCard uses " + art);
            else Expect(assigned == null, "LevelCard has no " + art);
        }

        static void ExpectBound(WoolLevelCard view, Sprite art, Color edge, string state)
        {
            if (art != null) Expect(view.Edge.sprite == art && Near(view.Edge.color, Color.white) && !view.Face.enabled && Mathf.Abs(view.Edge.pixelsPerUnitMultiplier - view.ArtScale) < 1e-4f, state + " card shows " + art.name);
            else Expect(view.Edge.sprite == view.Plain && Near(view.Edge.color, edge) && view.Face.enabled && view.Edge.pixelsPerUnitMultiplier == 1, state + " card falls back to the procedural colours");
        }

        static void ExpectBadgeIcon(GameObject badge, string art, string plain)
        {
            var node = badge.transform.Find("Icon");
            if (WoolUiArt.TryLoad(art, out var sprite)) Expect(node != null && node.GetComponent<Image>().sprite == sprite && ((RectTransform)node).sizeDelta == Vector2.one * WoolUiBuilder.CardIconSize, badge.name + " badge shows " + art);
            else if (plain == null) Expect(node == null, badge.name + " badge has no icon");
            else Expect(node != null && node.GetComponent<Image>().sprite.name == plain, badge.name + " badge keeps " + plain);
        }

        static void ExpectLobbyFallback(Transform canvas)
        {
            var active = SceneManager.GetActiveScene();
            var temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temp);
                var clone = Object.Instantiate(canvas.gameObject).transform;
                clone.name = "Canvas";
                var camera = new GameObject("Probe Camera").AddComponent<Camera>();
                WoolUiArt.SimulateMissing = true;
                WoolUiBuilder.StyleLobby(clone, camera);
                var home = clone.Find("LobbyPanel");
                var settings = (RectTransform)home.Find("Settings");
                Expect(home.Find("Background") == null && clone.Find("LevelsPanel/Background") == null && home.Find("Kitten").GetComponent<Image>().enabled, "Without art the lobby has no background and shows the procedural kitten");
                Expect(home.Find("Logo") == null && home.Find("Brand").GetComponent<Text>().enabled, "Without art the WOOL JAM text shows");
                Expect(settings.sizeDelta == WoolUiBuilder.SettingsPillSize && settings.anchoredPosition == WoolUiBuilder.SettingsPillPosition && settings.Find("Icon") == null && settings.Find("Face") != null && settings.Find("Label").GetComponent<Text>().enabled, "Without art Settings is the procedural pill");
                Expect(clone.Find("SettingsPanel/Card/Face") != null && clone.Find("SettingsPanel/Card/Sound/Icon") == null && home.Find("PlayPulse/Play/Face") != null, "Without art the settings card, toggles and PLAY are procedural");
                WoolUiArt.SimulateMissing = false;
                WoolUiBuilder.StyleLobby(clone, camera);
                if (WoolUiArt.TryLoad("Button-Round-Cream", out var round)) Expect(settings.GetComponent<Image>().sprite == round && settings.sizeDelta == WoolUiBuilder.SettingsRoundSize, "Lobby art comes back after the fallback");
            }
            finally
            {
                WoolUiArt.SimulateMissing = false;
                SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(temp, true);
            }
        }
    }
}
