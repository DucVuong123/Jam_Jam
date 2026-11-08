using System;
using UnityEngine;

namespace JamJam
{
    public static class WoolUiTheme
    {
        public static readonly Color TextLight = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color TextOutline = new Color32(0x0E, 0x18, 0x36, 0xFF);
        public static readonly Color TextDark = new Color32(0x1E, 0x2B, 0x4F, 0xFF);
        public static readonly Color Primary = new Color32(0x3C, 0xC0, 0x3A, 0xFF);
        public static readonly Color PrimaryEdge = new Color32(0x23, 0x86, 0x2A, 0xFF);
        public static readonly Color Secondary = new Color32(0xFF, 0xF3, 0xD6, 0xFF);
        public static readonly Color SecondaryEdge = new Color32(0xD9, 0xB7, 0x7A, 0xFF);
        public static readonly Color Warm = new Color32(0xFF, 0xB2, 0x1F, 0xFF);
        public static readonly Color WarmEdge = new Color32(0xC7, 0x7A, 0x0A, 0xFF);
        public static readonly Color Danger = new Color32(0xF0, 0x60, 0x3A, 0xFF);
        public static readonly Color DangerEdge = new Color32(0xB2, 0x3E, 0x1E, 0xFF);
        public static readonly Color Card = new Color32(0xFF, 0xF8, 0xEC, 0xFF);
        public static readonly Color CardEdge = new Color32(0x1E, 0x2B, 0x4F, 0xFF);
        public static readonly Color Overlay = new Color(0x0A / 255f, 0x12 / 255f, 0x30 / 255f, 0.72f);
        public static readonly Color Track = new Color32(0x0E, 0x18, 0x36, 0xFF);
        public static readonly Color Fill = new Color32(0x3C, 0xC0, 0x3A, 0xFF);
        public static readonly Color Locked = new Color32(0x2A, 0x3A, 0x66, 0xFF);
        public static readonly Color LockedEdge = new Color32(0x17, 0x22, 0x45, 0xFF);
        public static readonly Color Off = new Color32(0x5E, 0x6B, 0x8C, 0xFF);
        public static readonly Color Badge = new Color(0x0E / 255f, 0x18 / 255f, 0x36 / 255f, 0.35f);
        public static readonly Color Navy = new Color32(0x14, 0x23, 0x4A, 0xFF);
        public static readonly Vector2 ShadowOffset = new Vector2(0, -4);
        public static readonly Vector2 LogoShadowOffset = new Vector2(0, -8);
        public const float SoftAlpha = 0.85f;
        public const float ShadowAlpha = 0.5f;
        public const float OutlineSize = 3f;
        public const float LogoOutlineSize = 6f;
        public const float TitleOutline = 5f;
        public const float EdgeDepth = 10f;
        public const float CardBorder = 6f;
        public const float CardDepth = 12f;
        public const float MinTouch = 96f;
        public const float MinContrast = 4.5f;
        public const float LockedAlpha = 0.8f;

        public static void CardColors(bool unlocked, bool cleared, bool current, out Color face, out Color edge)
        {
            if (cleared) { face = Primary; edge = PrimaryEdge; return; }
            if (current) { face = Warm; edge = WarmEdge; return; }
            if (unlocked) { face = Secondary; edge = SecondaryEdge; return; }
            face = Locked;
            edge = LockedEdge;
        }

        public static float Luminance(Color color)
        {
            return 0.2126f * Channel(color.r) + 0.7152f * Channel(color.g) + 0.0722f * Channel(color.b);
        }

        public static float Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05f) / (Math.Min(la, lb) + 0.05f);
        }

        static float Channel(float c)
        {
            return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
    }
}
