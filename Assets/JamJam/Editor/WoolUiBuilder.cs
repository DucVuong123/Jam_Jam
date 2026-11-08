using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public const string GameScene = "Assets/Scenes/WoolJam.unity";
        public static readonly Vector2 TopLeft = new Vector2(0, 1);
        public static readonly Vector2 TopRight = new Vector2(1, 1);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 HudPausePosition = new Vector2(100, -100);
        public static readonly Vector2 HudPauseSize = new Vector2(120, 120);
        public static readonly Vector2 BadgePosition = new Vector2(40, -205);
        public static readonly Vector2 BadgeSize = new Vector2(260, 60);
        public static readonly Vector2 CaptionSize = new Vector2(120, 60);
        public static readonly Vector2 NumberPosition = new Vector2(128, 0);
        public static readonly Vector2 NumberSize = new Vector2(132, 60);
        public const int HudTextSize = 36;
        public static readonly Vector2 ProgressIconPosition = new Vector2(62, -262);
        public static readonly Vector2 ProgressIconSize = new Vector2(44, 44);
        public static readonly Vector2 ProgressNumberPosition = new Vector2(92, -262);
        public static readonly Vector2 ProgressNumberSize = new Vector2(200, 50);
        public const int ProgressTextSize = 32;
        public static readonly Vector2 SpeedPosition = new Vector2(-130, -100);
        public static readonly Vector2 SpeedSize = new Vector2(200, 96);
        public const int SpeedTextSize = 40;
        public static readonly Vector2 UnlockSize = new Vector2(132, 76);
        public static readonly Vector2 UnlockLabelPosition = new Vector2(-15, 4);
        public static readonly Vector2 UnlockLabelSize = new Vector2(84, 56);
        public const int UnlockTextSize = 26;
        public static readonly Vector2 PlusPosition = new Vector2(43, 4);
        public static readonly Vector2 PlusSize = new Vector2(36, 36);
        public const int PlusTextSize = 30;
        public static readonly Vector2 PauseCardSize = new Vector2(820, 760);
        public static readonly Vector2 PauseTitlePosition = new Vector2(0, 220);
        public static readonly Vector2 PauseBodyPosition = new Vector2(0, 120);
        public static readonly Vector2 PauseResumePosition = new Vector2(0, -40);
        public static readonly Vector2 PauseRetryPosition = new Vector2(0, -160);
        public static readonly Vector2 TrackSize = new Vector2(790, 36);
        public static readonly Vector2 FillSize = new Vector2(778, 24);
        public static readonly Vector2 PauseHomePosition = new Vector2(0, -280);
        public static readonly Vector2 ResultHomePosition = new Vector2(0, -307);
        public static readonly Vector2 ResultContentSize = new Vector2(800, 200);
        public const float ResultSpacing = 24f;
        public const int ResultPaddingTop = 40;
        public const int ResultPaddingBottom = 50;

        [MenuItem("JamJam/Rebuild UI")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding UI");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before rebuilding UI");
            WoolUiStyle.BuildSprites();
            WoolUiArt.Import();
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            FindCanvas(out var canvas);
            StyleGame(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RebuildLobby();
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("Wool UI rebuilt");
        }

        static partial void RebuildLobby();

        public static void StyleGame(Transform canvas)
        {
            WoolUiStyle.LoadSprite("UiPill", out var pill);
            WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
            WoolUiStyle.LoadSprite("UiCircle", out var circle);
            WoolUiStyle.LoadSprite("UiBar", out var bar);
            Get(canvas, "Brand", out Text brand); WoolUiStyle.Light(brand, 56); brand.gameObject.SetActive(false);
            Get(canvas, "Goal", out Text goal); WoolUiStyle.Light(goal, 22, WoolUiTheme.SoftAlpha); goal.gameObject.SetActive(false);
            Get(canvas, "LevelBadge", out Image badge);
            Anchor(badge.rectTransform, TopLeft, new Vector2(0, 0.5f), BadgePosition, BadgeSize);
            badge.sprite = null;
            badge.enabled = false;
            var badgeFace = badge.transform.Find("Face");
            if (badgeFace != null) badgeFace.gameObject.SetActive(false);
            Get(canvas, "LevelBadge/LevelCaption", out Text caption); WoolUiStyle.Light(caption, HudTextSize);
            Anchor(caption.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, CaptionSize); caption.alignment = TextAnchor.MiddleRight;
            Get(canvas, "LevelBadge/LevelNumber", out Text number); WoolUiStyle.Light(number, HudTextSize);
            Anchor(number.rectTransform, Vector2.zero, Vector2.zero, NumberPosition, NumberSize); number.alignment = TextAnchor.MiddleLeft;
            Get(canvas, "Pause", out Button pause); WoolUiStyle.Button(pause, WoolUiTone.Warm, circle, 44); Juice(pause);
            Anchor((RectTransform)pause.transform, TopLeft, new Vector2(0.5f, 0.5f), HudPausePosition, HudPauseSize);
            Get(canvas, "ProgressTrack", out Image track); Get(canvas, "ProgressTrack/ProgressFill", out Image fill);
            WoolUiStyle.Bar(track, fill, pill, bar);
            track.rectTransform.sizeDelta = TrackSize; fill.rectTransform.sizeDelta = FillSize;
            track.gameObject.SetActive(false);
            Get(canvas, "ProgressNumber", out Text percent); WoolUiStyle.Light(percent, ProgressTextSize);
            Anchor(percent.rectTransform, TopLeft, new Vector2(0, 0.5f), ProgressNumberPosition, ProgressNumberSize); percent.alignment = TextAnchor.MiddleLeft;
            Get(canvas, "TrayCaption", out Text tray); WoolUiStyle.Light(tray, 20, WoolUiTheme.SoftAlpha); tray.gameObject.SetActive(false);
            Get(canvas, "Instructions", out Text instructions); WoolUiStyle.Light(instructions, 25, WoolUiTheme.SoftAlpha); instructions.gameObject.SetActive(false);
            Get(canvas, "Help", out Text help); WoolUiStyle.Light(help, 28, WoolUiTheme.SoftAlpha); help.gameObject.SetActive(false);
            EnsureHud(canvas, out var speed, out var speedLabel, out var unlock, out var progressIcon);
            StyleHud(speed, speedLabel, unlock, progressIcon, pill, circle);
            Panel(canvas, "PausePanel", cardSprite, out var pauseCard);
            Fit(pauseCard, PauseCardSize);
            Place(pauseCard, "Title", PauseTitlePosition); Place(pauseCard, "Body", PauseBodyPosition); Place(pauseCard, "Resume", PauseResumePosition);
            Get(pauseCard, "Title", out Text pauseTitle); WoolUiStyle.Dark(pauseTitle, 54);
            Get(pauseCard, "Body", out Text pauseBody); WoolUiStyle.Dark(pauseBody, 30);
            Get(pauseCard, "Resume", out Button resume); WoolUiStyle.Button(resume, WoolUiTone.Primary, pill, 36); Juice(resume);
            Adopt(canvas, "Retry", pauseCard, out var retryRect);
            Get(pauseCard, "Retry", out Button retry); WoolUiStyle.Button(retry, WoolUiTone.Secondary, pill, 34); Juice(retry);
            retryRect.anchorMin = retryRect.anchorMax = retryRect.pivot = new Vector2(0.5f, 0.5f);
            retryRect.sizeDelta = ((RectTransform)resume.transform).sizeDelta;
            retryRect.anchoredPosition = PauseRetryPosition;
            if (retryRect.GetSiblingIndex() != resume.transform.GetSiblingIndex() + 1) retryRect.SetSiblingIndex(resume.transform.GetSiblingIndex() + 1);
            Dock(canvas, "PausePanel", pauseCard, PauseHomePosition); Get(pauseCard, "Home", out Button pauseHome); WoolUiStyle.Button(pauseHome, WoolUiTone.Secondary, pill, 34); Juice(pauseHome);
            Panel(canvas, "ResultPanel", cardSprite, out var resultCard);
            Fit(resultCard, new Vector2(850, 800));
            foreach (var content in new[] { "WonContent", "LostContent" })
            {
                ((RectTransform)resultCard.Find(content)).sizeDelta = ResultContentSize;
                Place(resultCard, content + "/Title", new Vector2(0, 55)); Place(resultCard, content + "/Subtitle", new Vector2(0, -60));
            }
            Get(resultCard, "WonContent/Title", out Text won); WoolUiStyle.Title(won, WoolUiTheme.Primary, 80);
            Get(resultCard, "WonContent/Subtitle", out Text wonBody); WoolUiStyle.Dark(wonBody, 31);
            Get(resultCard, "LostContent/Title", out Text lost); WoolUiStyle.Title(lost, WoolUiTheme.Danger, 74);
            Get(resultCard, "LostContent/Subtitle", out Text lostBody); WoolUiStyle.Dark(lostBody, 30);
            Get(resultCard, "Next", out Button next); WoolUiStyle.Button(next, WoolUiTone.Primary, pill, 36); Juice(next);
            Get(resultCard, "PlayAgain", out Button again); WoolUiStyle.Button(again, WoolUiTone.Secondary, pill, 34); Juice(again);
            Dock(canvas, "ResultPanel", resultCard, ResultHomePosition); Get(resultCard, "Home", out Button resultHome); WoolUiStyle.Button(resultHome, WoolUiTone.Secondary, pill, 34); Juice(resultHome);
            resultCard.Find("LostContent").gameObject.SetActive(false);
            Stack(resultCard);
            ArtGame(canvas);
            WoolUiStyle.RoundPills(canvas);
        }

        public static void EnsureHud(Transform canvas, out Button speed, out Text speedLabel, out Button unlock, out Image progressIcon)
        {
            Ensure(canvas, "Speed", out Image speedImage); Ensure(canvas, "Speed", out speed); speed.targetGraphic = speedImage;
            Ensure(speed.transform, "Label", out speedLabel);
            Ensure(canvas, "UnlockTemplate", out Image unlockImage); Ensure(canvas, "UnlockTemplate", out unlock); unlock.targetGraphic = unlockImage;
            Ensure(unlock.transform, "Label", out Text _);
            Ensure(unlock.transform, "Plus", out Image plus);
            Ensure(plus.transform, "Glyph", out Text _);
            Ensure(unlock.transform, ".", out CanvasGroup _);
            Ensure(canvas, "ProgressIcon", out progressIcon);
            var pause = canvas.Find("PausePanel");
            if (pause != null)
            {
                Before(speed.transform, pause);
                Before(unlock.transform, pause);
                Before(progressIcon.transform, pause);
            }
            FindGame(canvas, out var game);
            if (game == null) return;
            game.SpeedButton = speed;
            game.SpeedLabel = speedLabel;
            game.UnlockTemplate = unlock;
            EditorUtility.SetDirty(game);
        }

        static void StyleHud(Button speed, Text speedLabel, Button unlock, Image progressIcon, Sprite pill, Sprite circle)
        {
            WoolUiStyle.Button(speed, WoolUiTone.Warm, pill, SpeedTextSize); Juice(speed);
            Anchor((RectTransform)speed.transform, TopRight, Center, SpeedPosition, SpeedSize);
            speedLabel.text = "x1";
            speedLabel.alignment = TextAnchor.MiddleCenter;
            speedLabel.raycastTarget = false;
            Anchor(speedLabel.rectTransform, Center, Center, new Vector2(0, WoolUiTheme.EdgeDepth * 0.5f), SpeedSize - WoolUiArt.LabelPad);
            var unlockRect = (RectTransform)unlock.transform;
            unlockRect.localScale = Vector3.one;
            Anchor(unlockRect, Center, Center, Vector2.zero, UnlockSize);
            WoolUiStyle.Button(unlock, WoolUiTone.Secondary, pill, UnlockTextSize); Juice(unlock);
            Get(unlock.transform, "Label", out Text label);
            label.text = "Unlock";
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            Anchor(label.rectTransform, Center, Center, UnlockLabelPosition, UnlockLabelSize);
            Get(unlock.transform, "Plus", out Image plus);
            plus.sprite = circle;
            plus.type = Image.Type.Simple;
            plus.color = WoolUiTheme.Primary;
            plus.raycastTarget = false;
            Anchor(plus.rectTransform, Center, Center, PlusPosition, PlusSize);
            Get(plus.transform, "Glyph", out Text glyph);
            WoolUiStyle.Light(glyph, PlusTextSize);
            glyph.text = "+";
            glyph.alignment = TextAnchor.MiddleCenter;
            glyph.horizontalOverflow = HorizontalWrapMode.Overflow;
            glyph.verticalOverflow = VerticalWrapMode.Overflow;
            glyph.raycastTarget = false;
            Anchor(glyph.rectTransform, Center, Center, Vector2.zero, PlusSize);
            var group = unlock.GetComponent<CanvasGroup>();
            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
            unlock.gameObject.SetActive(false);
            Anchor(progressIcon.rectTransform, TopLeft, Center, ProgressIconPosition, ProgressIconSize);
            progressIcon.sprite = circle;
            progressIcon.type = Image.Type.Simple;
            progressIcon.preserveAspect = false;
            progressIcon.color = WoolUiTheme.Warm;
            progressIcon.raycastTarget = false;
        }

        static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void Adopt(Transform canvas, string name, Transform card, out RectTransform rect)
        {
            var node = card.Find(name);
            if (node == null)
            {
                Get(canvas, name, out RectTransform loose);
                loose.SetParent(card, false);
                node = loose;
            }
            rect = (RectTransform)node;
        }

        static void Before(Transform node, Transform anchor)
        {
            if (node.parent != anchor.parent || node.GetSiblingIndex() < anchor.GetSiblingIndex()) return;
            node.SetSiblingIndex(anchor.GetSiblingIndex());
        }

        static void Ensure<T>(Transform parent, string name, out T component) where T : Component
        {
            var node = name == "." ? parent : parent.Find(name);
            if (node == null)
            {
                node = new GameObject(name, typeof(RectTransform)).transform;
                node.SetParent(parent, false);
            }
            component = node.GetComponent<T>();
            if (component == null) component = node.gameObject.AddComponent<T>();
        }

        public static void Juice(Button button)
        {
            if (button.GetComponent<WoolButtonJuice>() == null) button.gameObject.AddComponent<WoolButtonJuice>();
        }

        public static void PopIn(Transform card, CanvasGroup fade)
        {
            var pop = card.GetComponent<WoolPopIn>();
            if (pop == null) pop = card.gameObject.AddComponent<WoolPopIn>();
            pop.Fade = fade;
            pop.FromScale = WoolPopIn.DefaultFromScale;
            pop.ScaleTime = WoolPopIn.DefaultScaleTime;
            pop.FadeTime = WoolPopIn.DefaultFadeTime;
            EditorUtility.SetDirty(pop);
        }

        static void Stack(Transform card)
        {
            var layout = card.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
            layout.spacing = ResultSpacing;
            layout.padding = new RectOffset(0, 0, ResultPaddingTop, ResultPaddingBottom);
            var fitter = card.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var face = card.Find("Face");
            var element = face.GetComponent<LayoutElement>();
            if (element == null) element = face.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            var panel = card.parent.gameObject;
            var wasActive = panel.activeSelf;
            panel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card);
            panel.SetActive(wasActive);
        }

        static void Fit(Transform card, Vector2 size)
        {
            ((RectTransform)card).sizeDelta = size;
        }

        static void Place(Transform card, string path, Vector2 position)
        {
            ((RectTransform)card.Find(path)).anchoredPosition = position;
        }

        static void Dock(Transform canvas, string panel, Transform card, Vector2 position)
        {
            var home = card.Find("Home");
            if (home == null)
            {
                Get(canvas, panel + "/Home", out RectTransform loose);
                loose.SetParent(card, false);
                home = loose;
            }
            ((RectTransform)home).anchoredPosition = position;
        }

        static void Panel(Transform parent, string path, Sprite cardSprite, out Transform card)
        {
            Get(parent, path, out Image overlay);
            WoolUiStyle.Overlay(overlay, out var group);
            Get(overlay.transform, "Card", out Image cardImage);
            WoolUiStyle.Card(cardImage, cardSprite);
            PopIn(cardImage.transform, group);
            card = cardImage.transform;
        }

        static void FindCanvas(out Transform canvas)
        {
            var found = GameObject.Find("Canvas");
            if (found == null) throw new InvalidOperationException("Canvas is missing from " + SceneManager.GetActiveScene().path);
            canvas = found.transform;
        }

        static void Get<T>(Transform root, string path, out T component) where T : Component
        {
            var node = root.Find(path);
            if (node == null) throw new InvalidOperationException(path + " is missing under " + root.name);
            component = node.GetComponent<T>();
            if (component == null) throw new InvalidOperationException(path + " has no " + typeof(T).Name);
        }
    }
}
