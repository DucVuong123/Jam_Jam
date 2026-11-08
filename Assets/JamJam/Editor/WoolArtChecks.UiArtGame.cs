using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiArtGame()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolUiBuilder.GameScene, out var scene, out var opened);
            var skinned = 0;
            try
            {
                var game = Object.FindFirstObjectByType<WoolScene>();
                Expect(game != null, "WoolScene exists");
                var canvas = game.PausePanel.transform.parent;
                Expect(!canvas.Find("LevelBadge").GetComponent<Image>().enabled, "Level badge image is off so the level line reads as plain HUD text");
                var round = ExpectSkin(canvas, "Pause", "Button-Round-Orange");
                skinned += round;
                ExpectIcon(canvas, "Pause", round == 1 ? "Icon-Settings" : null, true);
                skinned += ExpectSkin(canvas, "PausePanel/Card/Retry", "Button-Cream");
                ExpectIcon(canvas, "PausePanel/Card/Retry", "Icon-Retry", false);
                skinned += ExpectSkin(canvas, "Speed", "Button-Orange");
                ExpectIcon(canvas, "Speed", "Icon-Play", false);
                skinned += ExpectSkin(canvas, "UnlockTemplate", "Button-Cream");
                var yarnIcon = canvas.Find("ProgressIcon").GetComponent<Image>();
                if (WoolUiArt.TryLoad("Icon-YarnBall", out var yarnBall)) { Expect(yarnIcon.sprite == yarnBall && Near(yarnIcon.color, Color.white), "ProgressIcon shows Icon-YarnBall"); skinned++; }
                else Expect(yarnIcon.sprite != null && yarnIcon.sprite.name == "UiCircle" && Near(yarnIcon.color, WoolUiTheme.Warm), "ProgressIcon is a warm circle without Icon-YarnBall");
                skinned += ExpectSkin(canvas, "PausePanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "PausePanel/Card/Resume", "Button-Green");
                ExpectIcon(canvas, "PausePanel/Card/Resume", "Icon-Play", false);
                skinned += ExpectSkin(canvas, "PausePanel/Card/Home", "Button-Cream");
                ExpectIcon(canvas, "PausePanel/Card/Home", "Icon-Home", false);
                skinned += ExpectSkin(canvas, "ResultPanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "ResultPanel/Card/Next", "Button-Green");
                ExpectIcon(canvas, "ResultPanel/Card/Next", "Icon-Play", false);
                skinned += ExpectSkin(canvas, "ResultPanel/Card/PlayAgain", "Button-Cream");
                ExpectIcon(canvas, "ResultPanel/Card/PlayAgain", "Icon-Retry", false);
                Expect(((RectTransform)canvas.Find("PausePanel/Card/Home")).sizeDelta.x == ((RectTransform)canvas.Find("PausePanel/Card/Resume")).sizeDelta.x && ((RectTransform)canvas.Find("ResultPanel/Card/Home")).sizeDelta.x == ((RectTransform)canvas.Find("ResultPanel/Card/PlayAgain")).sizeDelta.x, "Home is as wide as the primary button beside it");
                skinned += ExpectSkin(canvas, "ResultPanel/Card/Home", "Button-Cream");
                ExpectIcon(canvas, "ResultPanel/Card/Home", "Icon-Home", false);
                var bar = WoolUiArt.TryLoad("Progress-Track", out _) && WoolUiArt.TryLoad("Progress-Fill", out _);
                if (bar)
                {
                    skinned += ExpectSkin(canvas, "ProgressTrack", "Progress-Track") + ExpectSkin(canvas, "ProgressTrack/ProgressFill", "Progress-Fill");
                    var rect = game.ProgressFill.rectTransform;
                    Expect(((RectTransform)rect.parent).sizeDelta == WoolUiBuilder.TrackArtSize && rect.anchorMin == new Vector2(0, 0.5f) && rect.anchorMax == new Vector2(0, 0.5f) && rect.pivot == new Vector2(0, 0.5f) && rect.anchoredPosition == new Vector2(WoolUiBuilder.FillInset, 0) && rect.sizeDelta.y == WoolUiBuilder.FillArtHeight, "Progress fill grows from the left of the track");
                }
                else Expect(game.ProgressFill.type == Image.Type.Filled && game.ProgressFill.rectTransform.pivot == new Vector2(0.5f, 0.5f), "Progress fill stays the procedural Filled bar");
                ExpectResult(game.WonContent.transform, "Banner-Ribbon-Green", "Result-Win-Characters", WoolUiMotionKind.Bob, WoolUiBuilder.BobHeight, WoolUiBuilder.BobPeriod);
                ExpectResult(game.LostContent.transform, "Banner-Ribbon-Red", "Result-Lose-Kitten", WoolUiMotionKind.Sway, WoolUiBuilder.SwayAngle, WoolUiBuilder.SwayPeriod);
                if (WoolUiArt.TryLoad("Result-Rays", out var rays))
                {
                    var node = game.ResultRays;
                    Expect(node != null && node.transform.parent == game.ResultPanel.transform && node.transform.GetSiblingIndex() == 0 && !node.activeSelf && node.GetComponent<Image>().sprite == rays, "Rays sit behind the result card, hidden until a win");
                    var spin = node.GetComponent<WoolUiMotion>();
                    Expect(spin != null && spin.Kind == WoolUiMotionKind.Spin && spin.Amount == WoolUiBuilder.RaysSpeed, "Rays spin at 20 degrees per second");
                    Expect(Mathf.Abs(node.GetComponent<Image>().color.a - WoolUiBuilder.RaysAlpha) < 1e-4f && Near(node.GetComponent<Image>().color, new Color(1, 1, 1, WoolUiBuilder.RaysAlpha)), "Rays are half transparent so they stay behind the HUD");
                    skinned++;
                }
                else Expect(game.ResultRays == null && game.ResultPanel.transform.Find("Rays") == null, "No rays without Result-Rays");
                ExpectGameFallback(canvas);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            return "UI ART GAME PASS / " + skinned + " skinned";
        }

        public static string PlayArt()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null, "WoolScene is playing");
            var fill = scene.ProgressFill;
            string mode;
            if (fill.type == Image.Type.Filled)
            {
                scene.ShowProgress(0);
                var empty = fill.fillAmount;
                scene.ShowProgress(0.5f);
                var half = fill.fillAmount;
                scene.ShowProgress(1);
                Expect(empty == 0 && Mathf.Abs(half - 0.5f) < 1e-4f && fill.fillAmount == 1, "Filled bar shows 0, 50 and 100 percent");
                mode = "filled";
            }
            else
            {
                var rect = fill.rectTransform;
                var span = ((RectTransform)rect.parent).rect.width - 2 * rect.anchoredPosition.x;
                var left = rect.anchoredPosition.x;
                scene.ShowProgress(0);
                Expect(rect.rect.width == 0, "0 percent shows no fill");
                var sprite = fill.sprite;
                var minWidth = (sprite.border.x + sprite.border.z) / (sprite.pixelsPerUnit * fill.pixelsPerUnitMultiplier);
                scene.ShowProgress(0.001f);
                Expect(rect.rect.width >= minWidth - 0.01f && rect.rect.width <= span, "A tiny progress still shows a full capsule");
                scene.ShowProgress(0.5f);
                Expect(Mathf.Abs(rect.rect.width - span * 0.5f) < 0.5f && rect.anchoredPosition.x == left, "50 percent fills half the track from the left");
                scene.ShowProgress(1);
                Expect(Mathf.Abs(rect.rect.width - span) < 0.5f && rect.anchoredPosition.x == left, "100 percent fills the whole track");
                scene.ShowProgress(2);
                Expect(Mathf.Abs(rect.rect.width - span) < 0.5f, "Progress above 100 percent is clamped");
                mode = "width " + span.ToString("F0");
            }
            WoolUiMotion.Pose(WoolUiMotionKind.Spin, WoolUiBuilder.RaysSpeed, 1, 1, out _, out var spin, out _);
            WoolUiMotion.Pose(WoolUiMotionKind.Bob, WoolUiBuilder.BobHeight, WoolUiBuilder.BobPeriod, WoolUiBuilder.BobPeriod * 0.5f, out var top, out _, out _);
            WoolUiMotion.Pose(WoolUiMotionKind.Bob, WoolUiBuilder.BobHeight, WoolUiBuilder.BobPeriod, WoolUiBuilder.BobPeriod, out var rest, out _, out _);
            WoolUiMotion.Pose(WoolUiMotionKind.Sway, WoolUiBuilder.SwayAngle, WoolUiBuilder.SwayPeriod, WoolUiBuilder.SwayPeriod * 0.25f, out _, out var sway, out _);
            Expect(Mathf.Abs(spin + 20) < 1e-3f, "Rays turn 20 degrees clockwise in one second but turned " + spin);
            Expect(Mathf.Abs(top.y - 12) < 1e-3f && rest.magnitude < 1e-3f, "Characters bob 12 px over 1.6 s");
            Expect(Mathf.Abs(sway - 4) < 1e-3f, "Lost kitten sways 4 degrees");
            Expect(scene.ResultRays == null || !scene.ResultRays.activeInHierarchy, "Rays are hidden while playing");
            return "PLAY ART PASS / " + mode + " / motion";
        }

        public static string PlayArtResult(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene.ResultPanel.activeSelf, "Run PlayResultLate first");
            if (scene.ResultRays != null)
            {
                Expect(scene.ResultRays.activeSelf == win, "Rays show only on a win");
                var spin = scene.ResultRays.GetComponent<WoolUiMotion>();
                if (win) Expect(spin.Running && spin.Elapsed > 0 && Mathf.Abs(Mathf.DeltaAngle(scene.ResultRays.transform.localEulerAngles.z, -WoolUiBuilder.RaysSpeed * spin.Elapsed)) < 0.5f, "Rays spin while the win card is open");
            }
            var content = win ? scene.WonContent : scene.LostContent;
            var figure = content.transform.Find("Characters");
            if (figure != null)
            {
                var motion = figure.GetComponent<WoolUiMotion>();
                Expect(motion.Running && motion.Kind == (win ? WoolUiMotionKind.Bob : WoolUiMotionKind.Sway), (win ? "Win characters bob" : "Lost kitten sways") + " while the card is open");
            }
            return "PLAY ART RESULT PASS / " + (win ? "Won" : "Lost");
        }

        static int ExpectSkin(Transform root, string path, string art)
        {
            var node = root.Find(path);
            Expect(node != null, path + " exists");
            var image = node.GetComponent<Image>();
            if (!WoolUiArt.TryLoad(art, out var sprite))
            {
                Expect(!WoolUiArt.IsArt(image.sprite), path + " stays procedural without " + art);
                return 0;
            }
            Expect(image.sprite == sprite && Near(image.color, Color.white) && node.Find("Face") == null, path + " uses " + art + " without a Face");
            Expect(image.type == (sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple), path + " slices " + art + " only when it has a border");
            return 1;
        }

        static void ExpectIcon(Transform root, string path, string icon, bool round)
        {
            var node = root.Find(path);
            var iconNode = (RectTransform)node.Find("Icon");
            var label = node.Find("Label").GetComponent<Text>();
            if (icon == null || !WoolUiArt.TryLoad(icon, out var sprite))
            {
                Expect(iconNode == null && label.enabled, path + " has no icon and shows its label");
                return;
            }
            Expect(iconNode != null && iconNode.GetComponent<Image>().sprite == sprite && !iconNode.GetComponent<Image>().raycastTarget, path + " shows " + icon);
            Expect(Near(iconNode.GetComponent<Image>().color, icon == "Icon-Retry" ? WoolUiTheme.TextDark : Color.white), path + " tints Icon-Retry dark and leaves other icons white");
            if (round) Expect(!label.enabled && iconNode.anchoredPosition.x == 0, path + " centres " + icon + " and hides its label");
            else Expect(label.enabled && iconNode.anchoredPosition.x + iconNode.sizeDelta.x * 0.5f <= label.rectTransform.anchoredPosition.x - label.rectTransform.sizeDelta.x * 0.5f + 0.01f, path + " shows " + icon + " left of its label");
        }

        static void ExpectResult(Transform content, string banner, string figure, WoolUiMotionKind kind, float amount, float period)
        {
            var title = content.Find("Title").GetComponent<Text>();
            var bannerNode = content.Find("Banner");
            if (!WoolUiArt.TryLoad(banner, out var bannerArt)) Expect(bannerNode == null && title.rectTransform.sizeDelta == WoolUiBuilder.ResultTitleSize, content.name + " has no banner without " + banner);
            else
            {
                Expect(bannerNode != null && bannerNode.GetComponent<Image>().sprite == bannerArt && bannerNode.GetComponent<Image>().type == Image.Type.Sliced, content.name + " title sits on " + banner);
                Expect(bannerNode.GetSiblingIndex() < title.transform.GetSiblingIndex() && Near(title.color, WoolUiTheme.TextLight) && title.GetComponent<Outline>() != null, content.name + " title is white with a navy outline over the banner");
                Expect(Mathf.Abs(title.rectTransform.anchoredPosition.y - (WoolUiBuilder.BannerY + WoolUiBuilder.BannerTitleLift)) < 0.01f, content.name + " title is centred on the banner");
            }
            var figureNode = content.Find("Characters");
            if (!WoolUiArt.TryLoad(figure, out var figureArt))
            {
                Expect(figureNode == null, content.name + " has no characters without " + figure);
                return;
            }
            Expect(figureNode != null && figureNode.GetComponent<Image>().sprite == figureArt && figureNode.GetSiblingIndex() == 0 && ((RectTransform)figureNode).sizeDelta.y == WoolUiBuilder.CharactersSize, content.name + " shows " + figure + " behind the banner");
            var motion = figureNode.GetComponent<WoolUiMotion>();
            Expect(motion != null && motion.Kind == kind && motion.Amount == amount && motion.Period == period, content.name + " characters move as " + kind);
        }

        static void ExpectGameFallback(Transform canvas)
        {
            var active = SceneManager.GetActiveScene();
            var temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temp);
                var clone = Object.Instantiate(canvas.gameObject).transform;
                clone.name = "Canvas";
                WoolUiArt.SimulateMissing = true;
                WoolUiBuilder.StyleGame(clone);
                var pause = clone.Find("Pause");
                Expect(pause.Find("Face") != null && pause.Find("Icon") == null && pause.Find("Label").GetComponent<Text>().enabled && !WoolUiArt.IsArt(pause.GetComponent<Image>().sprite), "Without art Pause is the procedural circle with its label");
                var fill = clone.Find("ProgressTrack/ProgressFill").GetComponent<Image>();
                Expect(fill.type == Image.Type.Filled && fill.sprite.name == "UiBar" && fill.rectTransform.pivot == new Vector2(0.5f, 0.5f) && fill.rectTransform.sizeDelta == WoolUiBuilder.FillSize, "Without art the progress fill is the procedural Filled bar");
                var card = clone.Find("ResultPanel/Card");
                Expect(card.Find("Face") != null && Near(card.GetComponent<Image>().color, WoolUiTheme.CardEdge) && card.GetComponent<Image>().pixelsPerUnitMultiplier == 1, "Without art the result card is the raised cream card");
                Expect(clone.Find("ResultPanel/Rays") == null && card.Find("WonContent/Banner") == null && card.Find("WonContent/Characters") == null && card.Find("LostContent/Banner") == null, "Without art the result card has no rays, banner or characters");
                Expect(Near(card.Find("WonContent/Title").GetComponent<Text>().color, WoolUiTheme.Primary) && clone.Find("PausePanel/Card/Retry/Icon") == null && !clone.Find("LevelBadge").GetComponent<Image>().enabled, "Without art the titles and Retry are procedural and the badge stays hidden");
                var speed = clone.Find("Speed");
                Expect(speed.Find("Face") != null && speed.Find("Icon") == null && !WoolUiArt.IsArt(speed.GetComponent<Image>().sprite) && speed.Find("Label").GetComponent<Text>().enabled, "Without art Speed is the procedural warm pill with its label");
                var unlock = clone.Find("UnlockTemplate");
                Expect(unlock.Find("Face") != null && !WoolUiArt.IsArt(unlock.GetComponent<Image>().sprite) && !unlock.gameObject.activeSelf, "Without art UnlockTemplate is the procedural cream pill");
                var progressIcon = clone.Find("ProgressIcon").GetComponent<Image>();
                Expect(progressIcon.sprite.name == "UiCircle" && Near(progressIcon.color, WoolUiTheme.Warm), "Without art ProgressIcon is a warm circle");
                WoolUiArt.SimulateMissing = false;
                WoolUiBuilder.StyleGame(clone);
                if (WoolUiArt.TryLoad("Button-Round-Orange", out var round)) Expect(pause.GetComponent<Image>().sprite == round && pause.Find("Face") == null, "Art comes back after the fallback");
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
