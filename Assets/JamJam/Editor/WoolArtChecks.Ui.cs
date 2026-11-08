using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiGame()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolUiBuilder.GameScene, out var scene, out var opened);
            int texts, buttons;
            try
            {
                ScanUi(scene.GetRootGameObjects(), out texts, out buttons);
                var game = Object.FindFirstObjectByType<WoolScene>();
                Expect(game != null, "WoolScene exists");
                ExpectPanel(game.PausePanel.transform);
                ExpectPanel(game.ResultPanel.transform);
                Expect(game.HomeButtons.Length == 2, "Two Home buttons are wired");
                ExpectHome(game.PausePanel.transform, game.HomeButtons);
                ExpectHome(game.ResultPanel.transform, game.HomeButtons);
                ExpectHud(game);
                var fill = game.ProgressFill;
                Expect((fill.type == Image.Type.Filled && fill.sprite != null && fill.sprite.name == "UiBar") || (fill.type == Image.Type.Sliced && WoolUiArt.IsArt(fill.sprite, "Progress-Fill")), "Progress fill is the Filled UiBar or the sliced Progress-Fill art");
                var won = game.WonContent.transform.Find("Title").GetComponent<Text>();
                var lost = game.LostContent.transform.Find("Title").GetComponent<Text>();
                var wonBanner = game.WonContent.transform.Find("Banner") != null;
                var lostBanner = game.LostContent.transform.Find("Banner") != null;
                Expect(Near(won.color, wonBanner ? WoolUiTheme.TextLight : WoolUiTheme.Primary) && Near(lost.color, lostBanner ? WoolUiTheme.TextLight : WoolUiTheme.Danger), "Result titles are white on a banner, otherwise Primary and Danger");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            LoadArt(out var art);
            WoolUiStyle.LoadFont(out var font, out _);
            var counter = art.Spool.GetComponentInChildren<TextMesh>();
            Expect(counter.name == "Counter" && counter.font == font && Near(counter.color, Color.white), "Spool counter uses the theme font in white");
            var shadow = counter.transform.Find("Shadow");
            Expect(shadow != null && shadow.GetComponent<TextMesh>().font == font && Near(shadow.GetComponent<TextMesh>().color, WoolUiTheme.TextOutline) && shadow.localPosition == WoolAssetsBuilder.CounterShadowOffset, "Spool counter has a navy shadow behind it");
            return "UI GAME PASS / " + texts + " texts / " + buttons + " buttons / spool counter shadow";
        }

        const float UnlockMinHeight = 72f;
        static readonly string[] HiddenTexts = { "Brand", "WOOL JAM", "Goal", "UNWIND THE DRAGON. SAVE THE KITTEN.", "TrayCaption", "THREAD TRAY", "Instructions", "TAP A CLEAR ARROW TO COLLECT ITS THREAD", "Help", "Need room? Tap a + slot" };

        static void ExpectHud(WoolScene game)
        {
            var canvas = game.PausePanel.transform.parent;
            for (var i = 0; i < HiddenTexts.Length; i += 2)
            {
                var node = canvas.Find(HiddenTexts[i]);
                Expect(node != null && !node.gameObject.activeSelf, HiddenTexts[i] + " is hidden");
                Expect(node.GetComponent<Text>().text == HiddenTexts[i + 1], HiddenTexts[i] + " keeps its text " + HiddenTexts[i + 1]);
            }
            var track = canvas.Find("ProgressTrack");
            Expect(track != null && !track.gameObject.activeSelf, "ProgressTrack is hidden");
            var card = game.PausePanel.transform.Find("Card");
            Expect(game.RetryButton.transform.parent == card && canvas.Find("Retry") == null, "Retry sits in PausePanel/Card");
            var resume = (RectTransform)card.Find("Resume");
            var retry = (RectTransform)game.RetryButton.transform;
            var home = (RectTransform)card.Find("Home");
            Expect(((RectTransform)card).sizeDelta == WoolUiBuilder.PauseCardSize, "Pause card is 820x760");
            Expect(retry.sizeDelta == resume.sizeDelta && retry.anchoredPosition == WoolUiBuilder.PauseRetryPosition && resume.anchoredPosition == WoolUiBuilder.PauseResumePosition && home.anchoredPosition == WoolUiBuilder.PauseHomePosition, "Pause card stacks Resume, Retry and Home");
            var pause = (RectTransform)game.PauseButton.transform;
            Expect(pause.anchorMin == WoolUiBuilder.TopLeft && pause.anchorMax == WoolUiBuilder.TopLeft && pause.anchoredPosition == WoolUiBuilder.HudPausePosition && pause.sizeDelta == WoolUiBuilder.HudPauseSize, "Pause sits top left at (100, -100)");
            var badge = (RectTransform)canvas.Find("LevelBadge");
            Expect(badge.anchorMin == WoolUiBuilder.TopLeft && badge.anchoredPosition == WoolUiBuilder.BadgePosition && badge.sizeDelta == WoolUiBuilder.BadgeSize && !badge.GetComponent<Image>().enabled, "Level line sits top left without a badge image");
            Expect(badge.Find("Face") == null || !badge.Find("Face").gameObject.activeSelf, "Level badge face is hidden");
            var percent = game.ProgressNumber.rectTransform;
            Expect(percent.anchorMin == WoolUiBuilder.TopLeft && percent.anchoredPosition == WoolUiBuilder.ProgressNumberPosition && game.ProgressNumber.fontSize == WoolUiBuilder.ProgressTextSize, "Progress number sits top left");
            var icon = canvas.Find("ProgressIcon");
            Expect(icon != null && icon.GetComponent<Image>() != null && ((RectTransform)icon).anchoredPosition == WoolUiBuilder.ProgressIconPosition, "ProgressIcon sits beside the progress number");
            Expect(game.SpeedButton != null && game.SpeedButton.name == "Speed" && game.SpeedButton.transform.parent == canvas, "Speed button is wired");
            var speed = (RectTransform)game.SpeedButton.transform;
            Expect(speed.anchorMin == WoolUiBuilder.TopRight && speed.anchoredPosition == WoolUiBuilder.SpeedPosition && speed.sizeDelta == WoolUiBuilder.SpeedSize, "Speed sits top right at (-130, -100) and is 200x96");
            Expect(game.SpeedLabel != null && game.SpeedLabel.transform.parent == speed && game.SpeedLabel.text == "x1", "Speed label reads x1");
            var unlock = game.UnlockTemplate;
            Expect(unlock != null && unlock.name == "UnlockTemplate" && !unlock.gameObject.activeSelf && unlock.transform.parent == canvas, "UnlockTemplate is wired and saved inactive");
            Expect(((RectTransform)unlock.transform).sizeDelta == WoolUiBuilder.UnlockSize, "UnlockTemplate is " + WoolUiBuilder.UnlockSize);
            var label = unlock.transform.Find("Label");
            Expect(label != null && label.GetComponent<Text>().text == "Unlock", "UnlockTemplate label reads Unlock");
            var plus = unlock.transform.Find("Plus");
            Expect(plus != null && Near(plus.GetComponent<Image>().color, WoolUiTheme.Primary) && plus.Find("Glyph").GetComponent<Text>().text == "+", "UnlockTemplate shows a green plus");
            Expect(unlock.GetComponent<CanvasGroup>() != null, "UnlockTemplate can fade");
            var overlay = game.PausePanel.transform.GetSiblingIndex();
            Expect(speed.GetSiblingIndex() < overlay && unlock.transform.GetSiblingIndex() < overlay && icon.GetSiblingIndex() < overlay, "New HUD objects draw under the pause overlay");
        }

        static void OpenForCheck(string path, out Scene scene, out bool opened)
        {
            scene = SceneManager.GetSceneByPath(path);
            opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        static void ScanUi(GameObject[] roots, out int texts, out int buttons)
        {
            WoolUiStyle.LoadFont(out var font, out var style);
            texts = 0;
            buttons = 0;
            foreach (var root in roots)
            {
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                {
                    var name = NodePath(text.transform);
                    Expect(text.font == font && text.fontStyle == style, name + " uses the theme font");
                    var outline = text.GetComponent<Outline>();
                    var white = text.color.r > 0.99f && text.color.g > 0.99f && text.color.b > 0.99f;
                    Expect(!white || outline != null, name + " is white and has an Outline");
                    var back = outline != null ? outline.effectColor : Backdrop(text.transform);
                    var ratio = WoolUiTheme.Contrast(text.color, back);
                    Expect(ratio >= WoolUiTheme.MinContrast, name + " contrast " + ratio.ToString("F2") + " is at least 4.5");
                    texts++;
                }
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    var name = NodePath(button.transform);
                    var size = ((RectTransform)button.transform).rect.size;
                    Expect(button.GetComponent<WoolButtonJuice>() != null, name + " has WoolButtonJuice");
                    var unlock = button.name == "UnlockTemplate" || button.name.StartsWith("Unlock_", System.StringComparison.Ordinal);
                    Expect(size.x >= WoolUiTheme.MinTouch && size.y >= (unlock ? UnlockMinHeight : WoolUiTheme.MinTouch), name + " is at least 96x" + (unlock ? UnlockMinHeight : WoolUiTheme.MinTouch) + " but was " + size);
                    Expect(button.transition == Selectable.Transition.None && (button.targetGraphic.transform.Find("Face") != null || WoolUiArt.IsArt(((Image)button.targetGraphic).sprite)), name + " is a raised or art button without tint");
                    buttons++;
                }
            }
        }

        static void ExpectHome(Transform panel, Button[] homes)
        {
            var card = (RectTransform)panel.Find("Card");
            var home = (RectTransform)card.Find("Home");
            Expect(home != null && System.Array.IndexOf(homes, home.GetComponent<Button>()) >= 0, panel.name + " Home is in the card and wired");
            Expect(panel.Find("Home") == null, panel.name + " has no loose Home");
            var rect = home.rect;
            var min = (Vector2)home.localPosition + rect.min;
            var max = (Vector2)home.localPosition + rect.max;
            Expect(min.x >= card.rect.xMin && max.x <= card.rect.xMax && min.y >= card.rect.yMin && max.y <= card.rect.yMax, panel.name + " Home lies inside the card");
        }

        static void ExpectPanel(Transform panel)
        {
            var overlay = panel.GetComponent<Image>();
            var group = panel.GetComponent<CanvasGroup>();
            Expect(overlay != null && overlay.sprite == null && Near(overlay.color, WoolUiTheme.Overlay) && Mathf.Abs(overlay.color.a - 0.72f) < 0.001f, panel.name + " overlay is #0A1230 at 0.72");
            var card = panel.Find("Card");
            var pop = card.GetComponent<WoolPopIn>();
            Expect(group != null && pop != null && pop.Fade == group, panel.name + " card pops in and fades its overlay");
            Expect(Mathf.Abs(pop.FromScale - 0.8f) < 1e-4f && Mathf.Abs(pop.ScaleTime - 0.3f) < 1e-4f && Mathf.Abs(pop.FadeTime - 0.2f) < 1e-4f, panel.name + " pop-in is 0.8 to 1 in 0.3 s with a 0.2 s fade");
            var cardImage = card.GetComponent<Image>();
            if (WoolUiArt.IsArt(cardImage.sprite)) Expect(WoolUiArt.IsArt(cardImage.sprite, "Panel-Card") && Near(cardImage.color, Color.white) && card.Find("Face") == null, panel.name + " card uses the Panel-Card art");
            else Expect(Near(cardImage.color, WoolUiTheme.CardEdge) && card.Find("Face") != null && Near(card.Find("Face").GetComponent<Image>().color, WoolUiTheme.Card), panel.name + " card is cream with a navy border");
            Expect(card.localScale == Vector3.one, panel.name + " card is saved at scale 1");
        }

        static Color Backdrop(Transform node)
        {
            for (var parent = node.parent; parent != null; parent = parent.parent)
            {
                var face = parent.Find("Face");
                if (face != null && face != node && face.GetComponent<Image>() != null) return face.GetComponent<Image>().color;
                var image = parent.GetComponent<Image>();
                if (image != null && image.color.a > 0.5f) return image.color;
            }
            return WoolUiTheme.Navy;
        }

        static string NodePath(Transform node)
        {
            var path = node.name;
            for (var parent = node.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }
    }
}
