using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public static readonly Vector2 TrackArtSize = new Vector2(780, 56);
        public const float FillArtHeight = 40f;
        public const float FillInset = 20f;
        public static readonly Vector2 BannerSize = new Vector2(680, 170);
        public const float BannerY = 150f;
        public static readonly Vector2 BannerTitleSize = new Vector2(480, 100);
        public static readonly Vector2 ResultTitleSize = new Vector2(760, 100);
        public const float BannerTitleLift = 10f;
        public const float ResultSubtitleY = -40f;
        public const float CharactersSize = 420f;
        public const float CharactersY = 300f;
        public const float RaysSize = 800f;
        public const float RaysAlpha = 0.5f;
        public const float RaysLift = 140f;
        public const float RaysSpeed = 20f;
        public const float BobHeight = 12f;
        public const float BobPeriod = 1.6f;
        public const float SwayAngle = 4f;
        public const float SwayPeriod = 2f;

        static void ArtGame(Transform canvas)
        {
            Get(canvas, "Pause", out Button pause); WoolUiArt.Round(pause, "Button-Round-Orange", "Icon-Settings");
            Get(canvas, "ProgressTrack", out Image track); Get(canvas, "ProgressTrack/ProgressFill", out Image fill);
            ArtBar(track, fill);
            Get(canvas, "Speed", out Button speed); WoolUiArt.Button(speed, WoolUiTone.Warm, "Icon-Play");
            Get(canvas, "UnlockTemplate", out Button unlock); WoolUiArt.Button(unlock, WoolUiTone.Secondary, null);
            Get(canvas, "ProgressIcon", out Image progressIcon);
            if (WoolUiArt.TryLoad("Icon-YarnBall", out var yarnBall))
            {
                progressIcon.sprite = yarnBall;
                progressIcon.preserveAspect = true;
                progressIcon.color = Color.white;
            }
            Get(canvas, "PausePanel/Card", out Image pauseCard); ArtPanel(pauseCard);
            Get(pauseCard.transform, "Retry", out Button retry); WoolUiArt.Button(retry, WoolUiTone.Secondary, "Icon-Retry");
            Get(pauseCard.transform, "Resume", out Button resume); WoolUiArt.Button(resume, WoolUiTone.Primary, "Icon-Play");
            Get(pauseCard.transform, "Home", out Button pauseHome); WoolUiArt.Button(pauseHome, WoolUiTone.Secondary, "Icon-Home");
            Get(canvas, "ResultPanel/Card", out Image resultCard); ArtPanel(resultCard);
            Get(resultCard.transform, "Next", out Button next); WoolUiArt.Button(next, WoolUiTone.Primary, "Icon-Play");
            Get(resultCard.transform, "PlayAgain", out Button again); WoolUiArt.Button(again, WoolUiTone.Secondary, "Icon-Retry");
            Get(resultCard.transform, "Home", out Button resultHome); WoolUiArt.Button(resultHome, WoolUiTone.Secondary, "Icon-Home");
            MatchWidth(pauseHome, resume);
            MatchWidth(resultHome, again);
            ArtResult(resultCard.transform, "WonContent", "Banner-Ribbon-Green", "Result-Win-Characters", WoolUiMotionKind.Bob, BobHeight, BobPeriod);
            ArtResult(resultCard.transform, "LostContent", "Banner-Ribbon-Red", "Result-Lose-Kitten", WoolUiMotionKind.Sway, SwayAngle, SwayPeriod);
            ArtRays(canvas, resultCard.rectTransform);
        }

        static void MatchWidth(Button target, Button source)
        {
            var rect = (RectTransform)target.transform;
            var size = rect.sizeDelta;
            size.x = ((RectTransform)source.transform).sizeDelta.x;
            rect.sizeDelta = size;
            WoolUiArt.Icon(target, target.transform.Find("Icon") != null ? "Icon-Home" : null, false);
        }

        static void ArtPanel(Image card)
        {
            if (!WoolUiArt.Card(card)) card.pixelsPerUnitMultiplier = 1;
        }

        static void ArtBar(Image track, Image fill)
        {
            var rect = fill.rectTransform;
            if (WoolUiArt.TryLoad("Progress-Track", out var trackArt) && WoolUiArt.TryLoad("Progress-Fill", out var fillArt))
            {
                track.rectTransform.sizeDelta = TrackArtSize;
                WoolUiArt.Skin(track, trackArt, WoolUiArt.FitHeight(track, trackArt));
                rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                rect.anchoredPosition = new Vector2(FillInset, 0);
                rect.sizeDelta = new Vector2(0, FillArtHeight);
                WoolUiArt.Skin(fill, fillArt, WoolUiArt.FitHeight(fill, fillArt));
                return;
            }
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        static void ArtResult(Transform card, string content, string banner, string figure, WoolUiMotionKind kind, float amount, float period)
        {
            var root = card.Find(content);
            Get(root, "Title", out Text title);
            Get(root, "Subtitle", out Text subtitle);
            WoolUiArt.Picture(root, "Characters", figure, new Vector2(0, CharactersY), Vector2.one * CharactersSize, out var figureRect);
            if (figureRect != null)
            {
                figureRect.SetSiblingIndex(0);
                Motion(figureRect.gameObject, kind, amount, period);
            }
            WoolUiArt.Picture(root, "Banner", banner, new Vector2(0, BannerY), BannerSize, out var bannerRect);
            if (bannerRect == null)
            {
                title.rectTransform.sizeDelta = ResultTitleSize;
                return;
            }
            bannerRect.SetSiblingIndex(figureRect != null ? 1 : 0);
            var image = bannerRect.GetComponent<Image>();
            image.pixelsPerUnitMultiplier = WoolUiArt.FitHeight(image, image.sprite);
            WoolUiStyle.Title(title, WoolUiTheme.TextLight, title.fontSize);
            title.rectTransform.anchoredPosition = new Vector2(0, BannerY + BannerTitleLift);
            title.rectTransform.sizeDelta = BannerTitleSize;
            subtitle.rectTransform.anchoredPosition = new Vector2(0, ResultSubtitleY);
        }

        static void ArtRays(Transform canvas, RectTransform card)
        {
            var top = card.anchoredPosition.y + card.rect.height * 0.5f;
            WoolUiArt.Picture(card.parent, "Rays", "Result-Rays", new Vector2(0, top + RaysLift), Vector2.one * RaysSize, out var rays);
            if (rays != null)
            {
                rays.SetSiblingIndex(0);
                Motion(rays.gameObject, WoolUiMotionKind.Spin, RaysSpeed, 1);
                rays.GetComponent<Image>().color = new Color(1, 1, 1, RaysAlpha);
                rays.gameObject.SetActive(false);
            }
            FindGame(canvas, out var game);
            if (game == null) return;
            game.ResultRays = rays != null ? rays.gameObject : null;
            EditorUtility.SetDirty(game);
        }

        static void Motion(GameObject target, WoolUiMotionKind kind, float amount, float period)
        {
            var motion = target.GetComponent<WoolUiMotion>();
            if (motion == null) motion = target.AddComponent<WoolUiMotion>();
            motion.Kind = kind;
            motion.Amount = amount;
            motion.Period = period;
            EditorUtility.SetDirty(motion);
        }

        static void FindGame(Transform canvas, out WoolScene game)
        {
            game = null;
            foreach (var root in canvas.gameObject.scene.GetRootGameObjects())
            {
                game = root.GetComponentInChildren<WoolScene>(true);
                if (game != null) return;
            }
        }
    }
}
