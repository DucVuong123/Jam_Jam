using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public static readonly Vector2 LogoPosition = new Vector2(0, 760);
        public static readonly Vector2 LogoSize = new Vector2(700, 350);
        public static readonly Vector2 TaglineArtPosition = new Vector2(0, 635);
        public static readonly Vector2 TaglinePosition = new Vector2(0, 470);
        public const float LogoBreath = 0.03f;
        public const float LogoBreathPeriod = 2.4f;
        public const float PlayPulseScale = 0.05f;
        public const float PlayPulsePeriod = 1.2f;
        public static readonly Vector2 SettingsRoundPosition = new Vector2(400, 830);
        public static readonly Vector2 SettingsRoundSize = new Vector2(120, 120);
        public static readonly Vector2 SettingsPillPosition = new Vector2(340, 830);
        public static readonly Vector2 SettingsPillSize = new Vector2(240, 100);
        public const float ToggleIconX = -266f;
        public const float ToggleIconSize = 64f;
        public static readonly Vector2 ToggleLabelArtPosition = new Vector2(-40, 0);
        public static readonly Vector2 ToggleLabelPosition = new Vector2(-110, 0);
        public const float CardHeight = 170f;
        public const float CardIconSize = 52f;
        public const float CardBadgeArtX = 250f;
        public const float CardBadgeX = 310f;
        public static readonly Vector2 CardNumberArtPosition = new Vector2(-260, 0);
        public static readonly Vector2 CardNumberArtSize = new Vector2(120, 110);
        public static readonly Vector2 CardNumberPosition = new Vector2(-330, 0);
        public static readonly Vector2 CardNumberSize = new Vector2(140, 110);
        public static readonly Vector2 CardTitleArtPosition = new Vector2(-10, 0);
        public static readonly Vector2 CardTitleArtSize = new Vector2(340, 90);
        public static readonly Vector2 CardTitlePosition = new Vector2(-20, 0);
        public static readonly Vector2 CardTitleSize = new Vector2(440, 90);

        static void ArtLobby(Transform canvas)
        {
            Get(canvas, "LobbyPanel", out RectTransform home);
            Get(canvas, "LevelsPanel", out RectTransform levels);
            WoolUiArt.Background(home, "Lobby-Background");
            WoolUiArt.Background(levels, "Levels-Background");
            Get(home, "Kitten", out Image kitten);
            kitten.enabled = home.Find("Background") == null;
            Get(home, "Brand", out Text brand);
            Get(home, "Tagline", out Text tagline);
            WoolUiArt.Picture(home, "Logo", "Logo-WoolJam", LogoPosition, LogoSize, out var logo);
            brand.enabled = logo == null;
            tagline.rectTransform.anchoredPosition = logo != null ? TaglineArtPosition : TaglinePosition;
            if (logo != null)
            {
                logo.SetSiblingIndex(brand.transform.GetSiblingIndex() + 1);
                Motion(logo.gameObject, WoolUiMotionKind.Breathe, LogoBreath, LogoBreathPeriod);
            }
            Get(home, "Settings", out Button settings);
            var settingsRect = (RectTransform)settings.transform;
            settingsRect.anchoredPosition = SettingsRoundPosition;
            settingsRect.sizeDelta = SettingsRoundSize;
            if (!WoolUiArt.Round(settings, "Button-Round-Cream", "Icon-Settings"))
            {
                settingsRect.anchoredPosition = SettingsPillPosition;
                settingsRect.sizeDelta = SettingsPillSize;
                WoolUiArt.Button(settings, WoolUiTone.Secondary, "Icon-Settings");
            }
            PlayPulse(home, out var play);
            WoolUiArt.Button(play, WoolUiTone.Primary, "Icon-Play");
            Get(home, "Levels", out Button levelsButton); WoolUiArt.Button(levelsButton, WoolUiTone.Secondary, null);
            Get(levels, "Back", out Button back); WoolUiArt.Button(back, WoolUiTone.Secondary, "Icon-Back");
            Get(canvas, "SettingsPanel/Card", out Image card); ArtPanel(card);
            ArtToggle(card.transform, "Sound", "Icon-Sound");
            ArtToggle(card.transform, "Music", "Icon-Music");
            ArtToggle(card.transform, "Vibration", "Icon-Vibration");
            Get(card.transform, "Reset", out Button reset);
            if (WoolUiArt.Button(reset, WoolUiTone.Danger, null)) { Get(reset.transform, "Label", out Text resetLabel); WoolUiStyle.Light(resetLabel, 32); }
            Get(card.transform, "Close", out Button close); WoolUiArt.Button(close, WoolUiTone.Primary, null);
            Get(canvas, "SettingsPanel/ConfirmPanel/Card", out Image confirm); ArtPanel(confirm);
            Get(confirm.transform, "Yes", out Button yes); WoolUiArt.Button(yes, WoolUiTone.Danger, null);
            Get(confirm.transform, "No", out Button no); WoolUiArt.Button(no, WoolUiTone.Secondary, null);
        }

        static void PlayPulse(Transform home, out Button play)
        {
            var wrapper = home.Find("PlayPulse");
            if (wrapper == null)
            {
                Get(home, "Play", out RectTransform loose);
                var rect = new GameObject("PlayPulse", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(home, false);
                rect.SetSiblingIndex(loose.GetSiblingIndex());
                rect.anchorMin = loose.anchorMin;
                rect.anchorMax = loose.anchorMax;
                rect.pivot = loose.pivot;
                rect.anchoredPosition = loose.anchoredPosition;
                rect.sizeDelta = loose.sizeDelta;
                loose.SetParent(rect, false);
                loose.anchoredPosition = Vector2.zero;
                wrapper = rect;
            }
            Get(wrapper, "Play", out play);
            Motion(wrapper.gameObject, WoolUiMotionKind.Breathe, PlayPulseScale, PlayPulsePeriod);
        }

        static void ArtToggle(Transform card, string name, string icon)
        {
            Get(card, name, out RectTransform row);
            Get(row, "Label", out Text label);
            if (!WoolUiArt.TryLoad(icon, out var sprite))
            {
                WoolUiArt.Remove(row, "Icon");
                label.rectTransform.anchoredPosition = ToggleLabelPosition;
                return;
            }
            WoolUiArt.Child(row, "Icon", out var rect, out var image);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            rect.anchoredPosition = new Vector2(ToggleIconX, 0);
            rect.sizeDelta = Vector2.one * ToggleIconSize;
            label.rectTransform.anchoredPosition = ToggleLabelArtPosition;
        }

        static void ArtCard(WoolLevelCard view, Sprite plain, Sprite pill)
        {
            WoolUiArt.TryLoad("LevelCard-Cleared", out var cleared);
            WoolUiArt.TryLoad("LevelCard-Current", out var current);
            WoolUiArt.TryLoad("LevelCard-Locked", out var locked);
            view.ClearedArt = cleared;
            view.CurrentArt = current;
            view.LockedArt = locked;
            view.Plain = plain;
            var sample = current != null ? current : cleared != null ? cleared : locked;
            var art = sample != null;
            view.ArtScale = art ? sample.rect.height / CardHeight : 1;
            view.Edge.pixelsPerUnitMultiplier = 1;
            view.Face.enabled = true;
            if (current != null)
            {
                view.Edge.sprite = current;
                view.Edge.color = Color.white;
                view.Edge.pixelsPerUnitMultiplier = view.ArtScale;
                view.Face.enabled = false;
            }
            view.Number.rectTransform.anchoredPosition = art ? CardNumberArtPosition : CardNumberPosition;
            view.Number.rectTransform.sizeDelta = art ? CardNumberArtSize : CardNumberSize;
            view.Title.rectTransform.anchoredPosition = art ? CardTitleArtPosition : CardTitlePosition;
            view.Title.rectTransform.sizeDelta = art ? CardTitleArtSize : CardTitleSize;
            foreach (var badge in new[] { view.Check, view.Lock, view.Current })
            {
                var rect = (RectTransform)badge.transform;
                rect.anchoredPosition = new Vector2(art ? CardBadgeArtX : CardBadgeX, rect.anchoredPosition.y);
            }
            ArtBadge(view.Check, WoolUiTheme.Badge, pill, "Icon-Check", 26);
            ArtBadge(view.Lock, WoolUiTheme.Badge, pill, "Icon-Lock", 26);
            ArtBadge(view.Current, WoolUiTheme.Primary, pill, "Icon-Play", 28);
        }

        static void ArtBadge(GameObject badge, Color color, Sprite pill, string icon, int labelSize)
        {
            if (!WoolUiArt.TryLoad(icon, out var sprite)) return;
            WoolUiStyle.Badge(badge.GetComponent<Image>(), color, pill, sprite, labelSize);
            Get(badge.transform, "Icon", out RectTransform rect);
            rect.sizeDelta = Vector2.one * CardIconSize;
        }
    }
}
